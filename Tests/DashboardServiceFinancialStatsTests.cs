using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kenergie.Tests
{
    /// <summary>
    /// Non-régression du contrat Dashboard : périmètre actif (headcount) vs financier (montants / lignes factures).
    /// Voir docs/ANALYSE_NON_REGRESSION_KPI_DASHBOARD_CLIENTS_FACTURES.md.
    /// Ne pas affaiblir ces assertions lors d'un alignement KPI (préférer de nouveaux champs DISTINCT).
    /// </summary>
    public class DashboardServiceFinancialStatsTests
    {
        /// <summary>
        /// CONTRAT : client IsActif=false compte dans collecte + nombreFactures + montants factures,
        /// mais PAS dans TotalClientsActifs.
        /// </summary>
        [Fact]
        public async Task GetDashboardDataAsync_IncludesInactiveClientInFinancialKpis_ButNotInHeadcount()
        {
            const int societeId = 1;
            var now = DateTime.Now;
            var moisNormalise = now.Month <= 9 ? $"0{now.Month}" : now.Month.ToString();

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            var inactiveClientId = 2;

            context.Paiements.Add(new Paiement
            {
                IdClient = inactiveClientId,
                MontantPaye = 500m,
                DatePaiement = now,
                IsDeleted = false,
                Statut = "Validé"
            });

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = inactiveClientId,
                Montant = 1000m,
                Mois = moisNormalise,
                Annees = now.Year,
                Statut = true
            });

            await context.SaveChangesAsync();

            var service = CreateDashboardService(context);
            var dashboard = await service.GetDashboardDataAsync(societeId);

            // Headcount = actifs uniquement (client 1)
            Assert.Equal(1, dashboard.TotalClientsActifs);
            // KPI financiers incluent l'inactif (client 2)
            Assert.Equal(500m, dashboard.CollecteMois.Montant);
            Assert.Equal(500m, dashboard.PaiementsDuMois);
            Assert.Equal(1, dashboard.CollecteMois.NombrePaiements);
            Assert.Equal(1000m, dashboard.FactureMois.MontantTotalFactures);
            Assert.Equal(1, dashboard.FactureMois.NombreFactures);
        }

        /// <summary>
        /// CONTRAT : Statut client false + ClientUsage inactif restent dans le périmètre financier (collecte).
        /// </summary>
        [Fact]
        public async Task GetDashboardDataAsync_IncludesClientStatutFalse_WithInactiveClientUsageLink()
        {
            const int societeId = 1;
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            var client = context.Clients.Single(c => c.IdClient == 2);
            client.Statut = false;
            context.ClientUsages.Single(cu => cu.IdClient == 2).Statut = false;

            context.Paiements.Add(new Paiement
            {
                IdClient = 2,
                MontantPaye = 350m,
                DatePaiement = now,
                IsDeleted = false,
                Statut = "Validé"
            });
            await context.SaveChangesAsync();

            var service = CreateDashboardService(context);
            var dashboard = await service.GetDashboardDataAsync(societeId);

            Assert.Equal(350m, dashboard.CollecteMois.Montant);
            Assert.Equal(1, dashboard.TotalClientsActifs);
        }

        /// <summary>
        /// CONTRAT : TotalGeneralArriere limité aux clients de la société (pas de fuite hors société).
        /// </summary>
        [Fact]
        public async Task GetDashboardDataAsync_TotalGeneralArriere_ScopedToSocieteClients()
        {
            const int societeId = 1;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithClients(context, societeId);

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 2,
                Montant = 1000m,
                Statut = true
            });

            context.Paiements.Add(new Paiement
            {
                IdClient = 2,
                MontantPaye = 300m,
                DatePaiement = DateTime.Now,
                IsDeleted = false,
                Statut = "Validé"
            });

            // Client hors société (ne doit pas impacter l'arriéré société)
            context.Clients.Add(new Client
            {
                IdClient = 99,
                NomClient = "Hors société",
                AdresseClient = "Autre",
                IsActif = true,
                Statut = true
            });
            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 99,
                Montant = 5000m,
                Statut = true
            });

            await context.SaveChangesAsync();

            var service = CreateDashboardService(context);
            var dashboard = await service.GetDashboardDataAsync(societeId);

            Assert.Equal(700m, dashboard.TotalGeneralArriere);
        }

        private static DashboardService CreateDashboardService(KenergieDbContext context)
        {
            var scope = new SocieteClientScopeService(context, NullLogger<SocieteClientScopeService>.Instance);
            return new DashboardService(context, NullLogger<DashboardService>.Instance, scope);
        }

        private static KenergieDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }

        /// <summary>
        /// Seed : client 1 actif + usage actif ; client 2 inactif mais lié (usage actif) → financier oui, headcount non.
        /// </summary>
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
                    NomClient = "Client Inactif",
                    AdresseClient = "Adresse 2",
                    IsActif = false,
                    Statut = false
                });

            context.ClientUsages.AddRange(
                new ClientUsage { IdClient = 1, IdUsage = 1, nombreBatiment = 1, Statut = true },
                new ClientUsage { IdClient = 2, IdUsage = 1, nombreBatiment = 1, Statut = true });

            context.SaveChanges();
        }
    }
}
