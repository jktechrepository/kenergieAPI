using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    public class FactureServiceIsActifTests
    {
        [Fact]
        public async Task CreateAsync_ExcludesClientFacture_WhenClientIsActifFalse()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.AddRange(
                new Client
                {
                    IdClient = 1,
                    NomClient = "Client inactif",
                    AdresseClient = "Adresse A",
                    Statut = true,
                    IsActif = false
                },
                new Client
                {
                    IdClient = 2,
                    NomClient = "Client actif",
                    AdresseClient = "Adresse B",
                    Statut = true,
                    IsActif = true
                });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = 1, Statut = true, nombreBatiment = 1 },
                new ClientUsage { IdClient = 2, IdUsage = 1, Statut = true, nombreBatiment = 1 });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var created = await service.CreateAsync(new Facture
            {
                IdUsage = 1,
                Montant = 1_000m,
                MoisEmission = 5,
                AnneesEmission = 2026,
                Statut = true
            });

            var clientFactures = await context.ClientFactures
                .Where(cf => cf.IdFacture == created.IdFacture)
                .ToListAsync();

            var clientFacture = Assert.Single(clientFactures);
            Assert.Equal(2, clientFacture.IdClient);
            Assert.Equal(1_000m, clientFacture.Montant);
        }

        [Fact]
        public async Task CreateAsync_CreatesClientFacture_WhenClientIsActifTrue()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client actif",
                AdresseClient = "Adresse",
                Statut = true,
                IsActif = true
            });

            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = 1,
                Statut = true,
                nombreBatiment = 2
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var created = await service.CreateAsync(new Facture
            {
                IdUsage = 1,
                Montant = 500m,
                MoisEmission = 6,
                AnneesEmission = 2026,
                Statut = true
            });

            var clientFacture = await context.ClientFactures
                .SingleAsync(cf => cf.IdFacture == created.IdFacture);

            Assert.Equal(1, clientFacture.IdClient);
            Assert.Equal(1_000m, clientFacture.Montant);
            Assert.Equal(2, clientFacture.nombreBatiment);
        }

        [Fact]
        public async Task CreateAsync_CreatesNoClientFacture_WhenAllClientsIsActifFalse()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client inactif",
                AdresseClient = "Adresse",
                Statut = true,
                IsActif = false
            });

            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = 1,
                Statut = true,
                nombreBatiment = 1
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var created = await service.CreateAsync(new Facture
            {
                IdUsage = 1,
                Montant = 500m,
                MoisEmission = 6,
                AnneesEmission = 2026,
                Statut = true
            });

            var count = await context.ClientFactures.CountAsync(cf => cf.IdFacture == created.IdFacture);
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task GetTotalClientsByUsageAsync_CountsOnlyIsActifTrueClients()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.AddRange(
                new Client
                {
                    IdClient = 1,
                    NomClient = "Inactif",
                    AdresseClient = "A",
                    Statut = true,
                    IsActif = false
                },
                new Client
                {
                    IdClient = 2,
                    NomClient = "Actif",
                    AdresseClient = "B",
                    Statut = true,
                    IsActif = true
                });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = 1, Statut = true, nombreBatiment = 1 },
                new ClientUsage { IdClient = 2, IdUsage = 1, Statut = true, nombreBatiment = 1 });
            await context.SaveChangesAsync();

            var service = CreateNotificationService(context);
            var total = await service.GetTotalClientsByUsageAsync(1);

            Assert.Equal(1, total);
        }

        [Fact]
        public async Task DiffuserFactureAUsageAsync_TargetsOnlyIsActifTrueClients()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.AddRange(
                new Client
                {
                    IdClient = 1,
                    NomClient = "Inactif",
                    AdresseClient = "A",
                    Statut = true,
                    IsActif = false
                },
                new Client
                {
                    IdClient = 2,
                    NomClient = "Actif sans user",
                    AdresseClient = "B",
                    Statut = true,
                    IsActif = true
                });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = 1, Statut = true, nombreBatiment = 1 },
                new ClientUsage { IdClient = 2, IdUsage = 1, Statut = true, nombreBatiment = 1 });

            var facture = new Facture
            {
                IdUsage = 1,
                Montant = 100m,
                MoisEmission = 5,
                AnneesEmission = 2026,
                Statut = true
            };
            context.Factures.Add(facture);
            await context.SaveChangesAsync();

            var service = CreateNotificationService(context);
            var successCount = await service.DiffuserFactureAUsageAsync(facture, idUsage: 1);

            // Aucun utilisateur lié : 0 diffusions, mais le client inactif ne doit pas entrer dans le périmètre
            Assert.Equal(0, successCount);
            Assert.Equal(1, await service.GetTotalClientsByUsageAsync(1));
        }

        private static void SeedUsage(KenergieDbContext context, int idUsage)
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
                IdUsage = idUsage,
                Libelle = "Usage test",
                IdCategorieClient = 1
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
            var clientFactureRepo = new Mock<IClientFactureRepository>().Object;
            return new FactureService(
                context,
                clientFactureRepo,
                CreateDeviseServiceMock().Object,
                NullLogger<FactureService>.Instance);
        }

        private static FactureNotificationService CreateNotificationService(KenergieDbContext context)
        {
            var config = new ConfigurationBuilder().Build();
            var sender = new Mock<Services.Notifications.INotificationSender>().Object;
            return new FactureNotificationService(
                context,
                sender,
                NullLogger<FactureNotificationService>.Instance,
                config);
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
