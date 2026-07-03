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
    public class FactureServiceDuplicateSkipTests
    {
        private const int IdUsage = 1;
        private const int IdTypePermanent = 1;

        [Fact]
        public async Task CreateAsync_SecondCall_SkipsClientsAlreadyBilledSamePeriodUsageType()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client A", AdresseClient = "A", Statut = true, IsActif = true },
                new Client { IdClient = 2, NomClient = "Client B", AdresseClient = "B", Statut = true, IsActif = true });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = IdUsage, IdTypeDeCourant = IdTypePermanent, Statut = true, nombreBatiment = 1 },
                new ClientUsage { IdClient = 2, IdUsage = IdUsage, IdTypeDeCourant = IdTypePermanent, Statut = true, nombreBatiment = 1 });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var factureDto = CreateFactureTemplate();

            var first = await service.CreateAsync(factureDto);
            var afterFirst = await context.ClientFactures.CountAsync(cf => cf.Annees == 2026 && cf.Mois == "05");
            Assert.Equal(2, afterFirst);

            var second = await service.CreateAsync(CloneFactureTemplate(factureDto));
            var afterSecond = await context.ClientFactures
                .Where(cf => cf.Annees == 2026 && (cf.Mois == "05" || cf.Mois == "5"))
                .ToListAsync();

            Assert.Equal(2, afterSecond.Count);
            Assert.Equal(2, afterSecond.Select(cf => cf.IdClient).Distinct().Count());
            Assert.NotEqual(first.IdFacture, second.IdFacture);
        }

        [Fact]
        public async Task CreateAsync_SecondCall_CreatesClientFacture_ForNewClientOnly()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client A", AdresseClient = "A", Statut = true, IsActif = true },
                new Client { IdClient = 2, NomClient = "Client B", AdresseClient = "B", Statut = true, IsActif = true });

            context.ClientUsages.Add(
                new ClientUsage { IdClient = 1, IdUsage = IdUsage, IdTypeDeCourant = IdTypePermanent, Statut = true, nombreBatiment = 1 });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            await service.CreateAsync(CreateFactureTemplate());

            context.ClientUsages.Add(
                new ClientUsage { IdClient = 2, IdUsage = IdUsage, IdTypeDeCourant = IdTypePermanent, Statut = true, nombreBatiment = 1 });
            await context.SaveChangesAsync();

            await service.CreateAsync(CloneFactureTemplate(CreateFactureTemplate()));

            var client2Lines = await context.ClientFactures
                .Where(cf => cf.IdClient == 2 && cf.Annees == 2026)
                .ToListAsync();
            Assert.Single(client2Lines);
        }

        [Fact]
        public async Task CreateAsync_SkipsDuplicate_WhenExistingClientFactureUsesSingleDigitMonth()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client A",
                AdresseClient = "A",
                Statut = true,
                IsActif = true
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });

            var existingFacture = new Facture
            {
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                MoisEmission = 5,
                AnneesEmission = 2026,
                Montant = 100m,
                Statut = true,
                NumeroFacture = "FAC-EXIST-001"
            };
            context.Factures.Add(existingFacture);
            await context.SaveChangesAsync();

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = existingFacture.IdFacture,
                Mois = "5",
                Annees = 2026,
                Montant = 100m,
                MontantDu = 100m,
                MontantPaye = 0m,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            await service.CreateAsync(CreateFactureTemplate());

            var count = await context.ClientFactures.CountAsync(cf => cf.IdClient == 1 && cf.Annees == 2026);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreateAsync_SkipsDuplicate_WhenPreExistantArriereSamePeriod()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client A",
                AdresseClient = "A",
                Statut = true,
                IsActif = true
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });
            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = null,
                Mois = "05",
                Annees = 2026,
                Montant = 50m,
                MontantDu = 50m,
                MontantPaye = 0m,
                EstArrierePreExistant = true,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            await service.CreateAsync(CreateFactureTemplate());

            Assert.Equal(1, await context.ClientFactures.CountAsync(cf => cf.IdClient == 1));
        }

        [Fact]
        public async Task DiffuserFactureAUsageAsync_OnlyNotifiesClientsWithClientFactureOnFacture()
        {
            await using var context = CreateInMemoryContext();
            SeedInfrastructure(context);

            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "A", AdresseClient = "A", Statut = true, IsActif = true },
                new Client { IdClient = 2, NomClient = "B", AdresseClient = "B", Statut = true, IsActif = true });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = IdUsage, IdTypeDeCourant = IdTypePermanent, Statut = true, nombreBatiment = 1 },
                new ClientUsage { IdClient = 2, IdUsage = IdUsage, IdTypeDeCourant = IdTypePermanent, Statut = true, nombreBatiment = 1 });

            var facture = new Facture
            {
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                MoisEmission = 5,
                AnneesEmission = 2026,
                Montant = 100m,
                Statut = true
            };
            context.Factures.Add(facture);
            await context.SaveChangesAsync();

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = facture.IdFacture,
                Mois = "05",
                Annees = 2026,
                Montant = 100m,
                MontantDu = 100m,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var notificationService = CreateNotificationService(context);
            var successCount = await notificationService.DiffuserFactureAUsageAsync(facture, IdUsage);

            Assert.Equal(0, successCount);
        }

        private static Facture CreateFactureTemplate()
        {
            return new Facture
            {
                IdUsage = IdUsage,
                IdTypeDeCourant = IdTypePermanent,
                MoisEmission = 5,
                AnneesEmission = 2026,
                Montant = 1_000m,
                Statut = true
            };
        }

        private static Facture CloneFactureTemplate(Facture template)
        {
            return new Facture
            {
                IdUsage = template.IdUsage,
                IdTypeDeCourant = template.IdTypeDeCourant,
                MoisEmission = template.MoisEmission,
                AnneesEmission = template.AnneesEmission,
                Montant = template.Montant,
                Statut = template.Statut
            };
        }

        private static void SeedInfrastructure(KenergieDbContext context)
        {
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

        private static FactureService CreateFactureService(KenergieDbContext context)
        {
            return new FactureService(context, new Mock<IClientFactureRepository>().Object, NullLogger<FactureService>.Instance);
        }

        private static FactureNotificationService CreateNotificationService(KenergieDbContext context)
        {
            return new FactureNotificationService(
                context,
                new Mock<Services.Notifications.INotificationSender>().Object,
                NullLogger<FactureNotificationService>.Instance,
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
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
