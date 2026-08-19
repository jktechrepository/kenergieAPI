using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Paiement;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    public class PaiementSocietePagedFilterTests
    {
        [Fact]
        public async Task GetBySocietePagedWithFiltersAsync_WithoutDateFilters_ReturnsCurrentMonthOnly()
        {
            await using var context = CreateContext();
            await SeedSocieteChainAsync(context);

            var now = DateTime.Now;
            var previousMonth = now.AddMonths(-1);

            context.Paiements.AddRange(
                CreatePaiement(1, 100m, new DateTime(now.Year, now.Month, 15)),
                CreatePaiement(2, 200m, new DateTime(previousMonth.Year, previousMonth.Month, 10)));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetBySocietePagedWithFiltersAsync(1, new PaiementPagedRequest());

            Assert.Equal(1, result.NombreTotalPaiement);
            Assert.Equal(100m, result.MontantTotalPaiement);
            Assert.Single(result.Data);
            Assert.Equal(100m, result.Data.First().MontantPaye);
        }

        [Fact]
        public async Task GetBySocietePagedWithFiltersAsync_WithMoisAndAnnee_ReturnsTargetMonthOnly()
        {
            await using var context = CreateContext();
            await SeedSocieteChainAsync(context);

            var june2026 = new DateTime(2026, 6, 15);
            var august2026 = new DateTime(2026, 8, 10);

            context.Paiements.AddRange(
                CreatePaiement(1, 150m, june2026),
                CreatePaiement(2, 250m, august2026));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetBySocietePagedWithFiltersAsync(1, new PaiementPagedRequest
            {
                Mois = 6,
                Annee = 2026
            });

            Assert.Equal(1, result.NombreTotalPaiement);
            Assert.Equal(150m, result.MontantTotalPaiement);
            Assert.Equal(june2026, result.Data.First().DatePaiement);
        }

        [Fact]
        public async Task GetBySocietePagedWithFiltersAsync_WithWideDateRange_ReturnsAllPayments()
        {
            await using var context = CreateContext();
            await SeedSocieteChainAsync(context);

            var now = DateTime.Now;
            var previousMonth = now.AddMonths(-1);

            context.Paiements.AddRange(
                CreatePaiement(1, 100m, new DateTime(now.Year, now.Month, 5)),
                CreatePaiement(2, 200m, new DateTime(previousMonth.Year, previousMonth.Month, 5)));
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetBySocietePagedWithFiltersAsync(1, new PaiementPagedRequest
            {
                DateDebut = new DateTime(2020, 1, 1),
                DateFin = new DateTime(2030, 12, 31)
            });

            Assert.Equal(2, result.NombreTotalPaiement);
            Assert.Equal(300m, result.MontantTotalPaiement);
        }

        private static PaiementService CreateService(KenergieDbContext context)
        {
            var deviseMock = new Mock<IDeviseConversionService>();
            deviseMock.Setup(s => s.GetCodeDevisePrincipaleAsync(It.IsAny<int>())).ReturnsAsync("CDF");

            return new PaiementService(
                context,
                new ClientFactureService(context, deviseMock.Object, NullLogger<ClientFactureService>.Instance),
                deviseMock.Object);
        }

        private static async Task SeedSocieteChainAsync(KenergieDbContext context)
        {
            context.Societes.Add(new Societe { IdSociete = 1, Nom = "Test SA", Type = "Privée", Statut = true });
            context.CategorieClients.Add(new CategorieClient
            {
                IdCategorie = 1,
                IdSociete = 1,
                NomCategorie = "Domestique",
                Statut = true
            });
            context.Usages.Add(new Usage { IdUsage = 1, IdCategorieClient = 1, Libelle = "Res", Statut = true });
            context.Factures.Add(new Facture
            {
                IdFacture = 1,
                IdUsage = 1,
                MoisEmission = 1,
                AnneesEmission = 2026,
                Statut = true,
                Montant = 1000m
            });
            await context.SaveChangesAsync();
        }

        private static Paiement CreatePaiement(int id, decimal montant, DateTime datePaiement) =>
            new()
            {
                IdPaiement = id,
                IdFacture = 1,
                MontantPaye = montant,
                DatePaiement = datePaiement,
                Statut = "Validé",
                IsDeleted = false
            };

        private static KenergieDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }
    }
}
