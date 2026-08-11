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
    public class ClientFactureDetteAnterieurTests
    {
        [Fact]
        public async Task GetArrieresConsolidesGlobaux_DetteAnterieur_ExcludesLatestPeriodPerClient()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;
            var moisPrecedentStr = moisPrecedent.ToString("D2");
            var moisActuelStr = now.Month.ToString("D2");

            await using var context = CreateInMemoryContext();
            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client Test",
                AdresseClient = "Adresse",
                CodeCons = "C001"
            });

            context.ClientFactures.AddRange(
                CreateFacture(1, moisPrecedentStr, anneePrecedente, 8_000m, 8_000m),
                CreateFacture(1, "02", anneePrecedente, 10_000m, 10_000m),
                CreateFacture(1, moisActuelStr, now.Year, 2_000m, 2_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(moisFacturePrecedentSeulement: true);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(20_000m, client.TotalGeneral);
            Assert.Equal(18_000m, client.DetteAnterieur);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_DetteAnterieur_NormalizesSingleDigitMonth()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;
            var moisPrecedentSansZero = moisPrecedent.ToString();

            await using var context = CreateInMemoryContext();
            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client Test",
                AdresseClient = "Adresse",
                CodeCons = "C001"
            });

            context.ClientFactures.AddRange(
                CreateFacture(1, moisPrecedentSansZero, anneePrecedente, 5_000m, 5_000m),
                CreateFacture(1, "01", anneePrecedente, 3_000m, 3_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(moisFacturePrecedentSeulement: true);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(8_000m, client.TotalGeneral);
            Assert.Equal(3_000m, client.DetteAnterieur);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_DetteAnterieur_IsZero_WhenSinglePeriodOnly()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;

            await using var context = CreateInMemoryContext();
            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client Test",
                AdresseClient = "Adresse",
                CodeCons = "C001"
            });

            context.ClientFactures.Add(
                CreateFacture(1, moisPrecedent.ToString("D2"), anneePrecedente, 4_000m, 4_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(moisFacturePrecedentSeulement: true);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(4_000m, client.TotalGeneral);
            Assert.Equal(0m, client.DetteAnterieur);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_DetteAnterieur_IsZero_WhenAllClientsMode()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;

            await using var context = CreateInMemoryContext();
            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client Test",
                AdresseClient = "Adresse",
                CodeCons = "C001"
            });

            context.ClientFactures.Add(
                CreateFacture(1, moisPrecedent.ToString("D2"), anneePrecedente, 4_000m, 4_000m));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(moisFacturePrecedentSeulement: false);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(4_000m, client.TotalGeneral);
            Assert.Equal(0m, client.DetteAnterieur);
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
