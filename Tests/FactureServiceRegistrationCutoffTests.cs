using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    public class FactureServiceRegistrationCutoffTests
    {
        private const int IdUsage = 1;
        private const int IdTypePermanent = 1;
        private const int BillingYear = 2026;
        private const int BillingMonth = 5;

        [Theory]
        [InlineData(14, true)]
        [InlineData(15, false)]
        [InlineData(20, false)]
        public void IsClientEligibleForBillingPeriod_SameMonth_RespectsCutoffDay(int day, bool expectedEligible)
        {
            var dateCreation = new DateTime(BillingYear, BillingMonth, day);
            var result = FactureBillingEligibilityHelper.IsClientEligibleForBillingPeriod(
                dateCreation, BillingMonth, BillingYear);

            Assert.Equal(expectedEligible, result);
        }

        [Fact]
        public void IsClientEligibleForBillingPeriod_DifferentMonth_IsEligible()
        {
            var dateCreation = new DateTime(BillingYear, BillingMonth, 20);
            var result = FactureBillingEligibilityHelper.IsClientEligibleForBillingPeriod(
                dateCreation, BillingMonth + 1, BillingYear);

            Assert.True(result);
        }

        [Fact]
        public async Task CreateAsync_IncludesClientRegisteredBefore15th()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client early",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = new DateTime(BillingYear, BillingMonth, 14)
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            await service.CreateAsync(CreateFactureTemplate());

            var count = await context.ClientFactures.CountAsync(cf => cf.IdClient == 1);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreateAsync_ExcludesClientRegisteredOn15th()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client cutoff",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = new DateTime(BillingYear, BillingMonth, 15)
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            await service.CreateAsync(CreateFactureTemplate());

            var count = await context.ClientFactures.CountAsync(cf => cf.IdClient == 1);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task CreateAsync_ExcludesClientRegisteredAfter15th()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client late",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = new DateTime(BillingYear, BillingMonth, 20)
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            await service.CreateAsync(CreateFactureTemplate());

            var count = await context.ClientFactures.CountAsync(cf => cf.IdClient == 1);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task CreateAsync_IncludesLateClient_ForDifferentBillingMonth()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client late May",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = new DateTime(BillingYear, BillingMonth, 20)
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var facture = CreateFactureTemplate();
            facture.MoisEmission = BillingMonth + 1;
            await service.CreateAsync(facture);

            var count = await context.ClientFactures.CountAsync(cf => cf.IdClient == 1);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreatePreExistantAsync_Throws_WhenClientRegisteredOn15thSameMonth()
        {
            await using var context = CreateInMemoryContext();

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client cutoff",
                AdresseClient = "A",
                Statut = true,
                IsActif = true,
                DateCreation = new DateTime(BillingYear, BillingMonth, 15)
            });
            await context.SaveChangesAsync();

            var service = CreateClientFactureService(context);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreatePreExistantAsync(1, 500m, "05", BillingYear));

            Assert.Contains("15", ex.Message);
        }

        private static Facture CreateFactureTemplate()
        {
            return new Facture
            {
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                MoisEmission = BillingMonth,
                AnneesEmission = BillingYear,
                Montant = 1_000m,
                Statut = true
            };
        }

        private static void SeedInfrastructure(KenergieDbContext context)
        {
            context.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Societe test",
                Type = "Privée",
                Statut = true
            });
            context.CategorieClients.Add(new CategorieClient
            {
                IdCategorie = 1,
                NomCategorie = "Domestique",
                IdSociete = 1,
                Statut = true
            });
            context.Usages.Add(new Usage
            {
                IdUsage = IdUsage,
                Libelle = "Usage test",
                IdCategorieClient = 1
            });
            context.TypeDeCourants.Add(new TypeDeCourant
            {
                IdTypeDeCourant = IdTypePermanent,
                Libelle = "Permanent",
                Statut = true
            });
        }

        private static Mock<IDeviseConversionService> CreateDeviseServiceMock()
        {
            var mock = new Mock<IDeviseConversionService>();
            mock.Setup(s => s.GetCodeDevisePrincipaleAsync(It.IsAny<int>())).ReturnsAsync("CDF");
            mock.Setup(s => s.ConvertirVersPrincipaleAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<DateTime>()))
                .ReturnsAsync((int _, string code, decimal montant, DateTime date) => new ConversionResult
                {
                    CodeDeviseSource = code,
                    CodeDeviseCible = "CDF",
                    Taux = 1m,
                    MontantSource = montant,
                    MontantConverti = montant,
                    DateReference = date
                });
            return mock;
        }

        private static FactureService CreateFactureService(KenergieDbContext context)
        {
            return new FactureService(
                context,
                new Mock<IClientFactureRepository>().Object,
                CreateDeviseServiceMock().Object,
                NullLogger<FactureService>.Instance);
        }

        private static ClientFactureService CreateClientFactureService(KenergieDbContext context)
        {
            var deviseService = new Mock<IDeviseConversionService>().Object;
            return new ClientFactureService(context, deviseService, NullLogger<ClientFactureService>.Instance);
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
