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
    public class StatistiquesServiceFinancialStatsTests
    {
        [Fact]
        public async Task GetStatistiquesFinancieres_IncludesInactiveIsActifClient_InChiffreAffaires()
        {
            const int societeId = 1;
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.Paiements.Add(new Paiement
            {
                IdClient = 2,
                MontantPaye = 750m,
                DatePaiement = now,
                IsDeleted = false,
                Statut = "Validé"
            });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesFinancieresAsync(societeId);

            Assert.Equal(750m, stats.ChiffreAffaires);
        }

        [Fact]
        public async Task GetStatistiquesGenerales_TotalPaiements_UsesCurrentMonthOnly()
        {
            const int societeId = 1;
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.Paiements.AddRange(
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 250m,
                    DatePaiement = now,
                    IsDeleted = false,
                    Statut = "Validé"
                },
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 1_000_000m,
                    DatePaiement = lastMonth,
                    IsDeleted = false,
                    Statut = "Validé"
                });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesGeneralesAsync(societeId);

            Assert.Equal(250m, stats.TotalPaiements);
            Assert.Equal(1, stats.TotalPaiementsCount);
        }

        [Fact]
        public async Task GetStatistiquesGenerales_ActiveHeadcount_ExcludesIsActifFalse()
        {
            const int societeId = 1;
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.Paiements.Add(new Paiement
            {
                IdClient = 2,
                MontantPaye = 200m,
                DatePaiement = now,
                IsDeleted = false,
                Statut = "Validé"
            });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesGeneralesAsync(societeId);

            Assert.Equal(1, stats.TotalClients);
            Assert.Equal(200m, stats.TotalPaiements);
        }

        [Fact]
        public async Task GetStatistiquesGenerales_ExcludesDeletedPayments()
        {
            const int societeId = 1;
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.Paiements.AddRange(
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 100m,
                    DatePaiement = now,
                    IsDeleted = false,
                    Statut = "Validé"
                },
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 999m,
                    DatePaiement = now,
                    IsDeleted = true,
                    Statut = "Validé"
                });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesGeneralesAsync(societeId);

            Assert.Equal(100m, stats.TotalPaiements);
        }

        [Fact]
        public async Task GetStatistiquesFinancieres_MontantPaye_UsesCurrentMonthOnly()
        {
            const int societeId = 1;
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.Paiements.AddRange(
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 300m,
                    DatePaiement = now,
                    IsDeleted = false,
                    Statut = "Validé",
                    MethodePaiement = "Espace"
                },
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 1_000_000m,
                    DatePaiement = lastMonth,
                    IsDeleted = false,
                    Statut = "Validé",
                    MethodePaiement = "Mobile"
                });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesFinancieresAsync(societeId);

            Assert.Equal(300m, stats.MontantPaye);
            Assert.Equal(300m, stats.ChiffreAffaires);
            Assert.Equal(300m, stats.RepartitionPaiements.Sum(r => r.MontantTotal));
        }

        [Fact]
        public async Task GetStatistiquesConsolidees_PaymentTotals_UseCurrentMonth_EvenWithYtdQueryParams()
        {
            const int societeId = 1;
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);
            var yearStart = new DateTime(now.Year, 1, 1);

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.Paiements.AddRange(
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 500m,
                    DatePaiement = now,
                    IsDeleted = false,
                    Statut = "Validé",
                    MethodePaiement = "Espace"
                },
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 2_000_000m,
                    DatePaiement = lastMonth,
                    IsDeleted = false,
                    Statut = "Validé",
                    MethodePaiement = "Mobile"
                });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesConsolideesAsync(
                societeId,
                debut: yearStart,
                fin: now);

            Assert.Equal(500m, stats.Generales.TotalPaiements);
            Assert.Equal(500m, stats.Financieres.MontantPaye);
            Assert.Equal(500m, stats.Financieres.ChiffreAffaires);
            Assert.Equal(500m, stats.Financieres.RepartitionPaiements.Sum(r => r.MontantTotal));
        }

        [Fact]
        public async Task GetStatistiquesFinancieres_IncludesClientWithStatutFalse_WhenLinkedToSociete()
        {
            const int societeId = 1;
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            var clientStatutFalse = context.Clients.Single(c => c.IdClient == 2);
            clientStatutFalse.Statut = false;
            context.ClientUsages.Single(cu => cu.IdClient == 2).Statut = false;

            context.Paiements.Add(new Paiement
            {
                IdClient = 2,
                MontantPaye = 400m,
                DatePaiement = now,
                IsDeleted = false,
                Statut = "Validé"
            });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesFinancieresAsync(societeId);

            Assert.Equal(400m, stats.ChiffreAffaires);
        }

        [Fact]
        public async Task GetStatistiquesOperationnelles_ClientActivite_SplitsByIsActifOnFinancialScope()
        {
            const int societeId = 1;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesOperationnellesAsync(societeId);

            Assert.Equal(1, stats.ClientActivite.NombreClientsActifs);
            Assert.Equal(1, stats.ClientActivite.NombreClientsInactifs);
            Assert.Equal(2, stats.ClientActivite.TotalClients);
        }

        private static StatistiquesService CreateStatistiquesService(KenergieDbContext context)
        {
            var signalR = new Mock<ISignalRStatistiquesService>();
            var scope = new SocieteClientScopeService(context, NullLogger<SocieteClientScopeService>.Instance);
            return new StatistiquesService(
                context,
                NullLogger<StatistiquesService>.Instance,
                signalR.Object,
                scope);
        }

        private static KenergieDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }

        private static void SeedSocieteWithClients(KenergieDbContext context, int societeId)
        {
            context.Societes.Add(new Societe { IdSociete = societeId, Nom = "Test SA" });

            context.CategorieClients.Add(new CategorieClient
            {
                IdCategorie = 1,
                IdSociete = societeId,
                NomCategorie = "Résidentiel",
                Statut = true
            });

            context.Usages.Add(new Usage
            {
                IdUsage = 1,
                IdCategorieClient = 1,
                Libelle = "Domestique",
                Statut = true
            });

            context.Clients.AddRange(
                new Client
                {
                    IdClient = 1,
                    NomClient = "Client Actif",
                    AdresseClient = "Adresse 1",
                    IsActif = true,
                    Statut = true
                },
                new Client
                {
                    IdClient = 2,
                    NomClient = "Client Inactif IsActif",
                    AdresseClient = "Adresse 2",
                    IsActif = false,
                    Statut = true
                });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = 1, nombreBatiment = 1, Statut = true },
                new ClientUsage { IdClient = 2, IdUsage = 1, nombreBatiment = 1, Statut = true });

            context.SaveChanges();
        }
    }
}
