using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.Configuration;
using Kenergie.Models.DTOs.FlexPay;
using Kenergie.Services;
using Kenergie.Services.FlexPay;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    public class PaiementElectroniqueConfirmationTests
    {
        [Fact]
        public async Task ProcessCallbackAsync_Code0WithoutProviderReference_DoesNotCreatePaiement()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);

            var service = CreateService(context);
            var result = await service.ProcessCallbackAsync(
                new FlexPayCallbackDto
                {
                    Code = "0",
                    OrderNumber = pending.OrderNumber,
                    Reference = pending.Reference,
                    Amount = pending.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Currency = pending.CodeDevisePaiement
                },
                "{}",
                null,
                "test");

            Assert.False(result.Success);
            Assert.Contains("ProviderReference", result.Message ?? string.Empty);

            var reloaded = await context.PaiementsElectroniquesEnAttente.FindAsync(pending.IdPaiementElectroniqueEnAttente);
            Assert.Equal(StatutPaiementElectronique.EnAttente, reloaded!.Statut);
            Assert.Equal(0, await context.Paiements.CountAsync());
        }

        [Fact]
        public async Task ProcessCallbackAsync_Code0WithProviderReference_CreatesPaiement()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);

            var service = CreateService(context);
            var result = await service.ProcessCallbackAsync(
                new FlexPayCallbackDto
                {
                    Code = "0",
                    OrderNumber = pending.OrderNumber,
                    Reference = pending.Reference,
                    ProviderReference = "OP-123456",
                    Amount = pending.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Currency = pending.CodeDevisePaiement
                },
                "{}",
                null,
                "test");

            Assert.True(result.Success);
            Assert.NotNull(result.IdPaiement);

            var reloaded = await context.PaiementsElectroniquesEnAttente.FindAsync(pending.IdPaiementElectroniqueEnAttente);
            Assert.Equal(StatutPaiementElectronique.Finalise, reloaded!.Statut);
            Assert.Equal(1, await context.Paiements.CountAsync());
        }

        [Fact]
        public async Task ProcessCallbackAsync_CrossCurrencyPending_CreatesPaiementWithInvoiceAmount()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(
                context,
                MethodeFlexPay.MobileMoney,
                pendingMontant: 0.50m,
                pendingMontantFacture: 1000m,
                codeDevisePaiement: "USD",
                codeDeviseFacture: "CDF",
                tauxFactureVersPaiement: 0.0005m);

            var service = CreateService(context);
            var result = await service.ProcessCallbackAsync(
                new FlexPayCallbackDto
                {
                    Code = "0",
                    OrderNumber = pending.OrderNumber,
                    Reference = pending.Reference,
                    ProviderReference = "OP-USD-1",
                    Amount = "0.50",
                    Currency = "USD"
                },
                "{}",
                null,
                "test");

            Assert.True(result.Success);

            var paiement = await context.Paiements.SingleAsync();
            Assert.Equal(1000m, paiement.MontantPaye);
            Assert.Equal(0.50m, paiement.MontantPayeDevisePaiement);
            Assert.Equal("CDF", paiement.CodeDeviseFacture);
            Assert.Equal("USD", paiement.CodeDevisePaiement);
            Assert.Equal(0.0005m, paiement.TauxFactureVersDevisePaiement);
        }

        [Fact]
        public async Task ProcessCallbackAsync_Code0WithSnakeCaseProviderReferenceInRawJson_CreatesPaiement()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);

            var rawJson =
                $"{{\"code\":\"0\",\"reference\":\"{pending.Reference}\",\"provider_reference\":\"7KI81020PHS\",\"orderNumber\":\"{pending.OrderNumber}\",\"amount\":\"{pending.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture)}\",\"currency\":\"{pending.CodeDevisePaiement}\"}}";

            var dto = System.Text.Json.JsonSerializer.Deserialize<FlexPayCallbackDto>(
                rawJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new FlexPayCallbackDto();

            var service = CreateService(context);
            var result = await service.ProcessCallbackAsync(dto, rawJson, null, "test");

            Assert.True(result.Success);
            Assert.NotNull(result.IdPaiement);

            var reloaded = await context.PaiementsElectroniquesEnAttente.FindAsync(pending.IdPaiementElectroniqueEnAttente);
            Assert.Equal(StatutPaiementElectronique.Finalise, reloaded!.Statut);
            Assert.Equal(1, await context.Paiements.CountAsync());
        }

        [Fact]
        public void FlexPayCallbackDto_NormalizeFromRawJson_ReadsProviderReferenceSnakeCase()
        {
            var dto = new FlexPayCallbackDto { Code = "0" };
            dto.NormalizeFromRawJson("{\"code\":\"0\",\"provider_reference\":\"OP-SNAKE\",\"orderNumber\":\"ORD1\"}");

            Assert.Equal("OP-SNAKE", dto.ProviderReference);
            Assert.Equal("ORD1", dto.OrderNumber);
        }

        [Fact]
        public async Task ProcessCallbackAsync_CodeNotZero_MarksPendingEchec()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);

            var service = CreateService(context);
            var result = await service.ProcessCallbackAsync(
                new FlexPayCallbackDto
                {
                    Code = "1",
                    OrderNumber = pending.OrderNumber,
                    Reference = pending.Reference
                },
                "{}",
                null,
                "test");

            Assert.False(result.Success);
            var reloaded = await context.PaiementsElectroniquesEnAttente.FindAsync(pending.IdPaiementElectroniqueEnAttente);
            Assert.Equal(StatutPaiementElectronique.Echec, reloaded!.Statut);
            Assert.Equal(0, await context.Paiements.CountAsync());
        }

        [Fact]
        public async Task ProcessCallbackAsync_SecondConfirmedCallback_ReturnsAlreadyProcessed()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);
            var service = CreateService(context);

            var payload = new FlexPayCallbackDto
            {
                Code = "0",
                OrderNumber = pending.OrderNumber,
                Reference = pending.Reference,
                ProviderReference = "OP-999",
                Amount = pending.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Currency = pending.CodeDevisePaiement
            };

            await service.ProcessCallbackAsync(payload, "{}", null, "test");
            var second = await service.ProcessCallbackAsync(payload, "{}", null, "test");

            Assert.True(second.AlreadyProcessed);
            Assert.Equal(1, await context.Paiements.CountAsync());
        }

        [Fact]
        public async Task VerifierAsync_WhenFlexPayPending_DoesNotCreatePaiement()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);

            var flexPayHttp = new Mock<IFlexPayHttpService>();
            flexPayHttp
                .Setup(h => h.VerifierTransactionAsync(It.IsAny<string>(), pending.OrderNumber!, default))
                .ReturnsAsync(new FlexPayCheckResult
                {
                    IsPending = true,
                    IsConfirmed = false,
                    Code = "1",
                    TransactionStatus = "PENDING",
                    Message = "En attente"
                });

            var service = CreateService(context, flexPayHttp.Object);
            var result = await service.VerifierAsync(pending.OrderNumber!);

            Assert.False(result.Success);
            Assert.Contains("attente", result.Message ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await context.Paiements.CountAsync());
        }

        [Fact]
        public async Task VerifierAsync_WhenFlexPayConfirmed_CreatesPaiement()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);

            var flexPayHttp = new Mock<IFlexPayHttpService>();
            flexPayHttp
                .Setup(h => h.VerifierTransactionAsync(It.IsAny<string>(), pending.OrderNumber!, default))
                .ReturnsAsync(new FlexPayCheckResult
                {
                    IsConfirmed = true,
                    IsPending = false,
                    Code = "0",
                    TransactionStatus = "SUCCESS",
                    ProviderReference = "OP-VERIF",
                    Amount = pending.Montant.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Currency = pending.CodeDevisePaiement
                });

            var service = CreateService(context, flexPayHttp.Object);
            var result = await service.VerifierAsync(pending.OrderNumber!);

            Assert.True(result.Success);
            Assert.Equal(1, await context.Paiements.CountAsync());
        }

        [Fact]
        public async Task GetPendingForCallerAsync_ClientOwner_ReturnsPending()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);
            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 10,
                NomComplet = "User Client A",
                MotDePasseHash = "x",
                IdClient = 1
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var dto = await service.GetPendingForCallerAsync(
                pending.IdPaiementElectroniqueEnAttente,
                idSocieteFilter: null,
                idUtilisateur: 10,
                isClientRole: true);

            Assert.NotNull(dto);
            Assert.Equal(pending.IdPaiementElectroniqueEnAttente, dto!.IdPending);
        }

        [Fact]
        public async Task GetPendingForCallerAsync_ClientOther_ThrowsUnauthorized()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);
            context.Clients.Add(new Client
            {
                IdClient = 2,
                NomClient = "Autre",
                AdresseClient = "Adr",
                Statut = true,
                IsActif = true
            });
            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 20,
                NomComplet = "User Client B",
                MotDePasseHash = "x",
                IdClient = 2
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.GetPendingForCallerAsync(
                    pending.IdPaiementElectroniqueEnAttente,
                    idSocieteFilter: null,
                    idUtilisateur: 20,
                    isClientRole: true));
        }

        [Fact]
        public async Task GetPendingForCallerAsync_Staff_FiltersBySociete()
        {
            await using var context = CreateInMemoryContext();
            var pending = await SeedPendingAsync(context, MethodeFlexPay.MobileMoney);
            var service = CreateService(context);

            var ok = await service.GetPendingForCallerAsync(
                pending.IdPaiementElectroniqueEnAttente,
                idSocieteFilter: 1,
                idUtilisateur: null,
                isClientRole: false);
            Assert.NotNull(ok);

            var missing = await service.GetPendingForCallerAsync(
                pending.IdPaiementElectroniqueEnAttente,
                idSocieteFilter: 999,
                idUtilisateur: null,
                isClientRole: false);
            Assert.Null(missing);
        }

        [Fact]
        public async Task InitierAsync_WhenPaymentCurrencyDiffersFromInvoice_ConvertsAndStoresSnapshot()
        {
            await using var context = CreateInMemoryContext();
            await SeedBaseDataAsync(context, 1000m, "CDF");

            var flexPayHttp = new Mock<IFlexPayHttpService>();
            flexPayHttp
                .Setup(h => h.InitierMobileMoneyAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    0.50m,
                    "USD",
                    It.IsAny<string>(),
                    default))
                .ReturnsAsync(new FlexPayInitResult
                {
                    Accepted = true,
                    Code = "0",
                    Message = "OK",
                    OrderNumber = "FP-INIT-USD"
                });

            var service = CreateService(context, flexPayHttp.Object);
            var dto = new InitierPaiementElectroniqueDto
            {
                IdClientFacture = 1,
                Methode = MethodeFlexPay.MobileMoney,
                Telephone = "243900000000",
                CodeDevisePaiement = "USD",
                Montant = 1000m
            };

            var result = await service.InitierAsync(dto, idSociete: 1, idUtilisateur: null);

            Assert.Equal("USD", result.CodeDevisePaiement);
            Assert.Equal(0.50m, result.MontantFlexPay);

            var pending = await context.PaiementsElectroniquesEnAttente.SingleAsync();
            Assert.Equal(1000m, pending.MontantFacture);
            Assert.Equal(0.50m, pending.Montant);
            Assert.Equal("CDF", pending.CodeDeviseFacture);
            Assert.Equal("USD", pending.CodeDevisePaiement);
            Assert.Equal(0.0005m, pending.TauxFactureVersPaiement);
        }

        private static PaiementElectroniqueService CreateService(
            KenergieDbContext context,
            IFlexPayHttpService? flexPayHttp = null)
        {
            var infoPaiement = new Mock<IInfoPaiementSocieteService>();
            infoPaiement
                .Setup(s => s.GetActiveEntityForSocieteAsync(It.IsAny<int>()))
                .ReturnsAsync(new InfoPaiementSociete
                {
                    IdSociete = 1,
                    CodeMarchand = "M1",
                    ApiToken = "token",
                    Statut = true
                });

            var deviseMock = new Mock<IDeviseConversionService>();
            deviseMock.Setup(s => s.GetCodeDevisePrincipaleAsync(It.IsAny<int>())).ReturnsAsync("CDF");
            deviseMock
                .Setup(s => s.ConvertirAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
                .ReturnsAsync((int _, string source, string cible, decimal montant, DateTime date) =>
                {
                    source = source.ToUpperInvariant();
                    cible = cible.ToUpperInvariant();
                    decimal taux = source == cible
                        ? 1m
                        : source == "CDF" && cible == "USD"
                            ? 0.0005m
                            : source == "USD" && cible == "CDF"
                                ? 2000m
                                : 1m;

                    return new ConversionResult
                    {
                        CodeDeviseSource = source,
                        CodeDeviseCible = cible,
                        Taux = taux,
                        MontantSource = montant,
                        MontantConverti = Math.Round(montant * taux, 2, MidpointRounding.AwayFromZero),
                        DateReference = date
                    };
                });
            deviseMock
                .Setup(s => s.ConvertirVersPrincipaleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
                .ReturnsAsync((int _, string code, decimal montant, DateTime date) => new ConversionResult
                {
                    CodeDeviseSource = code,
                    CodeDeviseCible = "CDF",
                    Taux = 1m,
                    MontantSource = montant,
                    MontantConverti = montant,
                    DateReference = date
                });

            var paiementRepo = new PaiementService(
                context,
                new ClientFactureService(context, deviseMock.Object, NullLogger<ClientFactureService>.Instance),
                deviseMock.Object);

            var postFinalization = new Mock<IPaiementFlexPayPostFinalizationService>();
            postFinalization
                .Setup(s => s.NotifyAfterFinalizationAsync(It.IsAny<PaiementElectroniqueEnAttente>(), It.IsAny<Paiement>()))
                .Returns(Task.CompletedTask);

            var options = Options.Create(new FlexPayOptions
            {
                Enabled = true,
                CallbackBaseUrl = "https://api.test.local/api/FlexPay/callback",
                RequireProviderReferenceForMobileMoney = true,
                MinSecondsBeforeFinalize = 0,
                MontantTolerance = 0.05m
            });

            return new PaiementElectroniqueService(
                context,
                deviseMock.Object,
                flexPayHttp ?? new Mock<IFlexPayHttpService>().Object,
                infoPaiement.Object,
                paiementRepo,
                postFinalization.Object,
                options,
                NullLogger<PaiementElectroniqueService>.Instance);
        }

        private static async Task<PaiementElectroniqueEnAttente> SeedPendingAsync(
            KenergieDbContext context,
            string methode,
            decimal pendingMontant = 500m,
            decimal pendingMontantFacture = 500m,
            string codeDevisePaiement = "CDF",
            string codeDeviseFacture = "CDF",
            decimal? tauxFactureVersPaiement = 1m)
        {
            var cf = await SeedBaseDataAsync(context, 1000m, codeDeviseFacture);
            var facture = await context.Factures.SingleAsync();

            var pending = new PaiementElectroniqueEnAttente
            {
                IdSociete = 1,
                IdClient = 1,
                IdClientFacture = cf.IdClientFacture,
                IdFacture = facture.IdFacture,
                Montant = pendingMontant,
                MontantFacture = pendingMontantFacture,
                CodeDevisePaiement = codeDevisePaiement,
                CodeDeviseFacture = codeDeviseFacture,
                CodeDevisePrincipale = "CDF",
                TauxFactureVersPaiement = tauxFactureVersPaiement,
                TauxFactureVersDevisePrincipale = codeDeviseFacture == "CDF" ? 1m : 2000m,
                MontantFactureDevisePrincipale = codeDeviseFacture == "CDF"
                    ? pendingMontantFacture
                    : Math.Round(pendingMontantFacture * 2000m, 2, MidpointRounding.AwayFromZero),
                Methode = methode,
                Reference = "KE-TESTREF001",
                OrderNumber = "FP-ORDER-001",
                Statut = StatutPaiementElectronique.EnAttente,
                HoldExpireAt = DateTime.UtcNow.AddMinutes(15),
                DateCreation = DateTime.UtcNow.AddSeconds(-30)
            };
            context.PaiementsElectroniquesEnAttente.Add(pending);
            await context.SaveChangesAsync();
            return pending;
        }

        private static async Task<ClientFacture> SeedBaseDataAsync(
            KenergieDbContext context,
            decimal factureMontant,
            string codeDeviseFacture)
        {
            context.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Test SA",
                Type = "Privée",
                Statut = true,
                CodeDevisePrincipale = "CDF"
            });
            context.CategorieClients.Add(new CategorieClient
            {
                IdCategorie = 1,
                IdSociete = 1,
                NomCategorie = "Domestique",
                Statut = true
            });
            context.Usages.Add(new Usage { IdUsage = 1, IdCategorieClient = 1, Libelle = "Res", Statut = true });
            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client",
                AdresseClient = "Adresse",
                Statut = true,
                IsActif = true
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = 1,
                Statut = true,
                nombreBatiment = 1
            });

            var facture = new Facture
            {
                IdUsage = 1,
                MoisEmission = 5,
                AnneesEmission = 2026,
                Montant = factureMontant,
                Statut = true,
                CodeDevisePrix = codeDeviseFacture
            };
            context.Factures.Add(facture);
            await context.SaveChangesAsync();

            var cf = new ClientFacture
            {
                IdClient = 1,
                IdFacture = facture.IdFacture,
                Montant = factureMontant,
                MontantPaye = 0,
                MontantDu = factureMontant,
                Mois = "05",
                Annees = 2026,
                CodeDevisePrix = codeDeviseFacture,
                Statut = true
            };
            context.ClientFactures.Add(cf);
            await context.SaveChangesAsync();

            return cf;
        }

        private static KenergieDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }
    }
}
