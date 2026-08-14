using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kenergie.Tests
{
    public class RapportFinancierUsdEnrichmentTests
    {
        [Fact]
        public async Task BuildEquivalentUsdAsync_ConvertsCdfToUsd_WithLatestRate()
        {
            await using var context = CreateInMemoryContext();
            SeedSocieteWithUsd(context, societeId: 1, principale: "CDF", tauxCdfUsd: 0.0004m);

            var service = CreateService(context);
            var result = await service.BuildEquivalentUsdAsync(1, 1_000_000m);

            Assert.True(result.ConversionUsdDisponible);
            Assert.Equal(400m, result.MontantEquivalentUsd);
            Assert.Equal(0.0004m, result.TauxVersUsd);
        }

        [Fact]
        public async Task BuildEquivalentUsdAsync_WhenUsdIsPrincipal_ReturnsSameAmountWithRateOne()
        {
            await using var context = CreateInMemoryContext();
            SeedSocieteWithUsd(context, societeId: 1, principale: "USD", tauxCdfUsd: 0.0004m);

            var service = CreateService(context);
            var result = await service.BuildEquivalentUsdAsync(1, 250.75m);

            Assert.True(result.ConversionUsdDisponible);
            Assert.Equal(250.75m, result.MontantEquivalentUsd);
            Assert.Equal(1m, result.TauxVersUsd);
        }

        [Fact]
        public async Task BuildEquivalentUsdAsync_WhenRateMissing_ReturnsUnavailable()
        {
            await using var context = CreateInMemoryContext();
            context.Societes.Add(new Societe
            {
                IdSociete = 1,
                Nom = "Test SA",
                CodeDevisePrincipale = "CDF"
            });
            context.DevisesMonetaires.Add(new DeviseMonetaire
            {
                IdDeviseMonetaire = 1,
                IdSociete = 1,
                CodeDevise = "USD",
                Libelle = "Dollar US",
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.BuildEquivalentUsdAsync(1, 1000m);

            Assert.False(result.ConversionUsdDisponible);
            Assert.Null(result.MontantEquivalentUsd);
        }

        [Fact]
        public async Task SumEquivalentUsdAsync_MultiSociete_SumsPerSocieteConversions()
        {
            await using var context = CreateInMemoryContext();
            SeedSocieteWithUsd(context, societeId: 1, principale: "CDF", tauxCdfUsd: 0.0004m);
            SeedSocieteWithUsd(context, societeId: 2, principale: "CDF", tauxCdfUsd: 0.0005m);

            var service = CreateService(context);
            var result = await service.SumEquivalentUsdAsync(new[]
            {
                (1, 1_000_000m),
                (2, 2_000_000m)
            });

            Assert.True(result.ConversionUsdDisponible);
            Assert.Equal(1400m, result.MontantEquivalentUsd);
            Assert.Null(result.TauxVersUsd);
        }

        [Fact]
        public async Task BuildStatistiquesFinancieresSyntheseUsdAsync_PopulatesAllSynthesisFields()
        {
            await using var context = CreateInMemoryContext();
            SeedSocieteWithUsd(context, societeId: 1, principale: "CDF", tauxCdfUsd: 0.001m);

            var service = CreateService(context);
            var synthese = await service.BuildStatistiquesFinancieresSyntheseUsdAsync(
                1, chiffreAffaires: 1000m, montantArrieres: 2000m, montantPaye: 3000m, montantDu: 4000m);

            Assert.True(synthese.ChiffreAffaires!.ConversionUsdDisponible);
            Assert.Equal(1m, synthese.ChiffreAffaires.MontantEquivalentUsd);
            Assert.Equal(2m, synthese.MontantArrieres!.MontantEquivalentUsd);
            Assert.Equal(3m, synthese.MontantPaye!.MontantEquivalentUsd);
            Assert.Equal(4m, synthese.MontantDu!.MontantEquivalentUsd);
        }

        private static RapportFinancierUsdEnrichmentService CreateService(KenergieDbContext context)
        {
            var deviseConversion = new DeviseConversionService(context);
            return new RapportFinancierUsdEnrichmentService(
                context,
                deviseConversion,
                NullLogger<RapportFinancierUsdEnrichmentService>.Instance);
        }

        private static KenergieDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }

        private static void SeedSocieteWithUsd(
            KenergieDbContext context,
            int societeId,
            string principale,
            decimal tauxCdfUsd)
        {
            if (!context.Societes.Any(s => s.IdSociete == societeId))
            {
                context.Societes.Add(new Societe
                {
                    IdSociete = societeId,
                    Nom = $"Societe {societeId}",
                    CodeDevisePrincipale = principale
                });
            }

            if (!context.DevisesMonetaires.Any(d => d.IdSociete == societeId && d.CodeDevise == "USD"))
            {
                context.DevisesMonetaires.Add(new DeviseMonetaire
                {
                    IdDeviseMonetaire = societeId * 10,
                    IdSociete = societeId,
                    CodeDevise = "USD",
                    Libelle = "Dollar US",
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            if (principale != "USD")
            {
                context.TauxChanges.Add(new TauxChange
                {
                    IdTauxChange = societeId,
                    IdSociete = societeId,
                    CodeDeviseSource = principale,
                    CodeDeviseCible = "USD",
                    Taux = tauxCdfUsd,
                    DateEffet = DateTime.UtcNow.AddDays(-1),
                    DateCreation = DateTime.UtcNow
                });
            }

            context.SaveChanges();
        }
    }
}
