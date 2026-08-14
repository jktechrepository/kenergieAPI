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
    public class StatistiquesTopAgentsTests
    {
        [Fact]
        public async Task GetStatistiquesPerformance_TopAgents_UsesCurrentMonthPaymentsOnly()
        {
            const int societeId = 1;
            const int caissierUserId = 10;
            var now = DateTime.Now;
            var lastMonth = now.AddMonths(-1);

            await using var context = CreateInMemoryContext();
            SeedSocieteWithCaissier(context, societeId, caissierUserId);

            context.Paiements.AddRange(
                new Paiement
                {
                    IdClient = 1,
                    IdUtilisateur = caissierUserId,
                    MontantPaye = 500m,
                    DatePaiement = now,
                    IsDeleted = false,
                    Statut = "Validé"
                },
                new Paiement
                {
                    IdClient = 1,
                    IdUtilisateur = caissierUserId,
                    MontantPaye = 9000m,
                    DatePaiement = lastMonth,
                    IsDeleted = false,
                    Statut = "Validé"
                });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesPerformanceAsync(societeId);

            var top = Assert.Single(stats.TopAgents);
            Assert.Equal(1, top.IdAgent);
            Assert.Equal(500m, top.MontantCollecte);
            Assert.Equal(1, top.NombrePaiements);
        }

        [Fact]
        public async Task GetStatistiquesPerformance_TopAgents_ExcludesAgentsWithZeroCollectionThisMonth()
        {
            const int societeId = 1;
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            SeedSocieteWithCaissier(context, societeId, caissierUserId: 10);

            context.Agents.Add(new Agent
            {
                IdAgent = 2,
                IdSociete = societeId,
                NomComplet = "Caissier Sans Collecte",
                DateNaissance = new DateTime(1990, 1, 1),
                Statut = true
            });
            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 11,
                IdAgent = 2,
                IdSociete = societeId,
                NomComplet = "User 2",
                MotDePasseHash = "hash",
                Statut = true
            });
            context.UserRoles.Add(new UserRole
            {
                IdUtilisateur = 11,
                IdRole = 1,
                Statut = true
            });

            context.Paiements.Add(new Paiement
            {
                IdClient = 1,
                IdUtilisateur = 10,
                MontantPaye = 300m,
                DatePaiement = now,
                IsDeleted = false,
                Statut = "Validé"
            });
            await context.SaveChangesAsync();

            var service = CreateStatistiquesService(context);
            var stats = await service.GetStatistiquesPerformanceAsync(societeId);

            Assert.Single(stats.TopAgents);
            Assert.Equal(300m, stats.TopAgents[0].MontantCollecte);
        }

        private static StatistiquesService CreateStatistiquesService(KenergieDbContext context)
        {
            var signalR = new Mock<ISignalRStatistiquesService>();
            var scope = new SocieteClientScopeService(context, NullLogger<SocieteClientScopeService>.Instance);
            var usdEnrichment = new RapportFinancierUsdEnrichmentService(
                context,
                new DeviseConversionService(context),
                NullLogger<RapportFinancierUsdEnrichmentService>.Instance);
            return new StatistiquesService(
                context,
                NullLogger<StatistiquesService>.Instance,
                signalR.Object,
                scope,
                usdEnrichment);
        }

        private static KenergieDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }

        private static void SeedSocieteWithCaissier(KenergieDbContext context, int societeId, int caissierUserId)
        {
            context.Societes.Add(new Societe { IdSociete = societeId, Nom = "Test SA" });

            context.Roles.Add(new Role
            {
                IdRole = 1,
                Nom = "Caissier",
                Statut = true
            });

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

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client Test",
                AdresseClient = "Adresse",
                IsActif = true,
                Statut = true
            });

            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = 1,
                nombreBatiment = 1,
                Statut = true
            });

            context.Agents.Add(new Agent
            {
                IdAgent = 1,
                IdSociete = societeId,
                NomComplet = "Caissier Top",
                DateNaissance = new DateTime(1990, 1, 1),
                Statut = true
            });

            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = caissierUserId,
                IdAgent = 1,
                IdSociete = societeId,
                NomComplet = "Caissier User",
                MotDePasseHash = "hash",
                Statut = true
            });

            context.UserRoles.Add(new UserRole
            {
                IdUtilisateur = caissierUserId,
                IdRole = 1,
                Statut = true
            });

            context.SaveChanges();
        }
    }
}
