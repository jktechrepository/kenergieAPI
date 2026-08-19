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
    public class ClientDashboardServiceTests
    {
        [Fact]
        public async Task GetDashboardDataAsync_ScopesToConnectedClient_Only()
        {
            await using var context = CreateContext();
            SeedTwoClientsWithFactures(context);

            var currentUser = CreateCurrentUser(userId: 101);
            var service = CreateService(context, currentUser);

            var dashboard = await service.GetDashboardDataAsync();

            Assert.Equal(10_000m, dashboard.Statistiques.MontantTotalFactures);
            Assert.Equal(4_000m, dashboard.Statistiques.MontantTotalPaye);
            Assert.Equal(6_000m, dashboard.Statistiques.MontantTotalDu);
            Assert.Equal(2, dashboard.Statistiques.NombreFactures);
            Assert.Equal(1, dashboard.Statistiques.NombreFacturesPayees);
            Assert.Equal(1, dashboard.Statistiques.NombreFacturesEnRetard);
            Assert.Equal(40m, dashboard.Statistiques.TauxRecouvrement);

            Assert.Equal(2, dashboard.FacturesRecentes.Count);
            Assert.All(dashboard.FacturesRecentes, f =>
                Assert.True(f.Reference.StartsWith("CF-") || f.Reference.StartsWith("ARR-") || f.IdFacture > 0));

            Assert.Single(dashboard.PaiementsRecents);
            Assert.Equal(4_000m, dashboard.PaiementsRecents[0].MontantPaye);

            Assert.Equal(6_000m, dashboard.ResumeClient.SoldeActuel);
            Assert.Equal("Soldeur", dashboard.ResumeClient.StatutCompte);
            Assert.Equal(1, dashboard.ResumeClient.NombreServicesActifs);
            Assert.Equal(0, dashboard.ResumeClient.LimiteCredit);

            Assert.Single(dashboard.AlertesClient);
            Assert.Equal(6_000m, dashboard.AlertesClient[0].MontantConcerne);

            Assert.NotEmpty(dashboard.Consommations);
            Assert.Equal(10_000m, dashboard.Consommations.Sum(c => c.MontantTotal));
        }

        [Fact]
        public async Task GetClientStatistiquesAsync_DoesNotLeakOtherClientData()
        {
            await using var context = CreateContext();
            SeedTwoClientsWithFactures(context);

            var serviceA = CreateService(context, CreateCurrentUser(userId: 101));
            var serviceB = CreateService(context, CreateCurrentUser(userId: 102));

            var statsA = await serviceA.GetClientStatistiquesAsync();
            var statsB = await serviceB.GetClientStatistiquesAsync();

            Assert.Equal(10_000m, statsA.MontantTotalFactures);
            Assert.Equal(2, statsA.NombreFactures);

            Assert.Equal(50_000m, statsB.MontantTotalFactures);
            Assert.Equal(1, statsB.NombreFactures);
            Assert.Equal(50_000m, statsB.MontantTotalDu);
        }

        [Fact]
        public async Task ResolveClient_Throws_WhenUserHasNoIdClient()
        {
            await using var context = CreateContext();
            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 200,
                NomComplet = "Sans client",
                MotDePasseHash = "x",
                IdClient = null
            });
            await context.SaveChangesAsync();

            var service = CreateService(context, CreateCurrentUser(userId: 200));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetDashboardDataAsync());
        }

        private static void SeedTwoClientsWithFactures(KenergieDbContext context)
        {
            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client A", AdresseClient = "Adr A", CodeCons = "A001" },
                new Client { IdClient = 2, NomClient = "Client B", AdresseClient = "Adr B", CodeCons = "B001" });

            context.Utilisateurs.AddRange(
                new Utilisateur
                {
                    IdUtilisateur = 101,
                    NomComplet = "User A",
                    MotDePasseHash = "x",
                    IdClient = 1,
                    DateCreation = DateTime.UtcNow.AddDays(-10)
                },
                new Utilisateur
                {
                    IdUtilisateur = 102,
                    NomComplet = "User B",
                    MotDePasseHash = "x",
                    IdClient = 2,
                    DateCreation = DateTime.UtcNow.AddDays(-5)
                });

            var overdue = DateTime.Now.AddDays(-45);
            var recent = DateTime.Now.AddDays(-5);

            context.ClientFactures.AddRange(
                new ClientFacture
                {
                    IdClient = 1,
                    Mois = overdue.Month.ToString("D2"),
                    Annees = overdue.Year,
                    DateEmission = overdue,
                    Montant = 6_000m,
                    MontantPaye = 0m,
                    MontantDu = 6_000m,
                    Statut = true
                },
                new ClientFacture
                {
                    IdClient = 1,
                    Mois = recent.Month.ToString("D2"),
                    Annees = recent.Year,
                    DateEmission = recent,
                    Montant = 4_000m,
                    MontantPaye = 4_000m,
                    MontantDu = 0m,
                    Statut = true
                },
                new ClientFacture
                {
                    IdClient = 2,
                    Mois = overdue.Month.ToString("D2"),
                    Annees = overdue.Year,
                    DateEmission = overdue,
                    Montant = 50_000m,
                    MontantPaye = 0m,
                    MontantDu = 50_000m,
                    Statut = true
                },
                // Soft-deleted for client A — must be ignored
                new ClientFacture
                {
                    IdClient = 1,
                    Mois = "01",
                    Annees = recent.Year,
                    DateEmission = recent.AddMonths(-2),
                    Montant = 999m,
                    MontantPaye = 0m,
                    MontantDu = 999m,
                    Statut = false
                });

            context.Paiements.AddRange(
                new Paiement
                {
                    IdClient = 1,
                    MontantPaye = 4_000m,
                    DatePaiement = recent,
                    MethodePaiement = "Espèces",
                    Statut = "Validé",
                    IsDeleted = false,
                    ReferenceTransaction = "PAY-A-1"
                },
                new Paiement
                {
                    IdClient = 2,
                    MontantPaye = 1_000m,
                    DatePaiement = recent,
                    MethodePaiement = "Espèces",
                    Statut = "Validé",
                    IsDeleted = false,
                    ReferenceTransaction = "PAY-B-1"
                });

            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = 1,
                nombreBatiment = 1,
                Statut = true
            });

            context.SaveChanges();
        }

        private static ClientDashboardService CreateService(KenergieDbContext context, ICurrentUserService currentUser)
        {
            return new ClientDashboardService(
                context,
                NullLogger<ClientDashboardService>.Instance,
                currentUser);
        }

        private static ICurrentUserService CreateCurrentUser(int userId)
        {
            var mock = new Mock<ICurrentUserService>();
            mock.SetupGet(c => c.UserId).Returns(userId);
            mock.Setup(c => c.GetUserId()).Returns(userId);
            mock.SetupGet(c => c.IsAuthenticated).Returns(true);
            return mock.Object;
        }

        private static KenergieDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }
    }
}
