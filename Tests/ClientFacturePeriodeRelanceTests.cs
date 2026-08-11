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
    public class ClientFacturePeriodeRelanceTests
    {
        [Fact]
        public async Task GetArrieresConsolidesGlobaux_SelectsClientsByCustomPeriod()
        {
            await using var context = CreateInMemoryContext();
            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client Avril", AdresseClient = "A" },
                new Client { IdClient = 2, NomClient = "Client Mai", AdresseClient = "B" });

            context.ClientFactures.AddRange(
                CreateFacture(1, "04", 2025, 5_000m, 5_000m),
                CreateFacture(1, "03", 2025, 2_000m, 2_000m),
                CreateFacture(2, "05", 2025, 3_000m, 3_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(
                moisFacturePrecedentSeulement: true,
                mois: "04",
                annee: 2025);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(1, client.IdClient);
            Assert.Equal(7_000m, client.TotalGeneral);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_CustomPeriod_AcceptsSingleDigitMonth()
        {
            await using var context = CreateInMemoryContext();
            context.Clients.Add(
                new Client { IdClient = 1, NomClient = "Client", AdresseClient = "A" });

            context.ClientFactures.Add(
                CreateFacture(1, "4", 2025, 4_000m, 4_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var resultWithZero = await service.GetArrieresConsolidesGlobauxAsync(
                moisFacturePrecedentSeulement: true, mois: "04", annee: 2025);
            var resultWithoutZero = await service.GetArrieresConsolidesGlobauxAsync(
                moisFacturePrecedentSeulement: true, mois: "4", annee: 2025);

            Assert.Single(resultWithZero.ArrieresParClient);
            Assert.Single(resultWithoutZero.ArrieresParClient);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_DefaultPeriod_UsesCalendarMMinusOne()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;

            await using var context = CreateInMemoryContext();
            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client M-1", AdresseClient = "A" },
                new Client { IdClient = 2, NomClient = "Client autre", AdresseClient = "B" });

            context.ClientFactures.AddRange(
                CreateFacture(1, moisPrecedent.ToString("D2"), anneePrecedente, 5_000m, 5_000m),
                CreateFacture(2, "01", anneePrecedente, 3_000m, 3_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(moisFacturePrecedentSeulement: true);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(1, client.IdClient);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_MoisSeul_UsesCurrentYear()
        {
            var now = DateTime.Now;

            await using var context = CreateInMemoryContext();
            context.Clients.Add(
                new Client { IdClient = 1, NomClient = "Client", AdresseClient = "A" });

            context.ClientFactures.Add(
                CreateFacture(1, "04", now.Year, 6_000m, 6_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(
                moisFacturePrecedentSeulement: true,
                mois: "04");

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(6_000m, client.TotalGeneral);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_IgnoresPeriodParams_WhenAllClientsMode()
        {
            await using var context = CreateInMemoryContext();
            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client 1", AdresseClient = "A" },
                new Client { IdClient = 2, NomClient = "Client 2", AdresseClient = "B" });

            context.ClientFactures.AddRange(
                CreateFacture(1, "01", 2020, 1_000m, 1_000m),
                CreateFacture(2, "05", 2025, 2_000m, 2_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(
                moisFacturePrecedentSeulement: false,
                mois: "01",
                annee: 2020);

            Assert.Equal(2, result.ArrieresParClient.Count);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_Throws_WhenAnneeWithoutMois()
        {
            var service = CreateService(CreateInMemoryContext());

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetArrieresConsolidesGlobauxAsync(
                    moisFacturePrecedentSeulement: true,
                    annee: 2025));

            Assert.Contains("mois", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_Throws_WhenInvalidMois()
        {
            var service = CreateService(CreateInMemoryContext());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetArrieresConsolidesGlobauxAsync(
                    moisFacturePrecedentSeulement: true,
                    mois: "13",
                    annee: 2025));
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_Throws_WhenInvalidAnnee()
        {
            var service = CreateService(CreateInMemoryContext());

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.GetArrieresConsolidesGlobauxAsync(
                    moisFacturePrecedentSeulement: true,
                    mois: "04",
                    annee: 1999));
        }

        private static ClientFacture CreateFacture(
            int idClient,
            string mois,
            int annees,
            decimal montant,
            decimal montantDu)
        {
            return new ClientFacture
            {
                IdClient = idClient,
                Mois = mois,
                Annees = annees,
                Montant = montant,
                MontantDu = montantDu,
                MontantPaye = montant - montantDu,
                Statut = true,
                DateCreation = DateTime.UtcNow
            };
        }

        private static ClientFactureService CreateService(KenergieDbContext context)
        {
            return new ClientFactureService(
                context,
                new Mock<IDeviseConversionService>().Object,
                NullLogger<ClientFactureService>.Instance);
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
