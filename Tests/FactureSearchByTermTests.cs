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
    public class FactureSearchByTermTests
    {
        [Fact]
        public async Task ResolveFactureBySearchTermAsync_ReturnsFacture_ByNumeroFactureExact()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Factures.Add(new Facture
            {
                IdUsage = 1,
                NumeroFacture = "FAC-DOM-CP-0526-0001",
                MoisEmission = 5,
                AnneesEmission = 2026,
                Statut = true
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("FAC-DOM-CP-0526-0001");

            Assert.NotNull(result);
            Assert.Equal("FAC-DOM-CP-0526-0001", result!.NumeroFacture);
        }

        [Fact]
        public async Task ResolveFactureBySearchTermAsync_PrioritizesNumeroFacture_OverCodeCons()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client test",
                AdresseClient = "Adresse",
                CodeCons = "SHARED-TERM",
                Statut = true
            });

            context.Factures.AddRange(
                new Facture
                {
                    IdUsage = 1,
                    NumeroFacture = "SHARED-TERM",
                    MoisEmission = 4,
                    AnneesEmission = 2026,
                    Statut = true
                },
                new Facture
                {
                    IdUsage = 1,
                    NumeroFacture = "FAC-OTHER-0002",
                    MoisEmission = 5,
                    AnneesEmission = 2026,
                    Statut = true
                });
            await context.SaveChangesAsync();

            var otherFactureId = await context.Factures
                .Where(f => f.NumeroFacture == "FAC-OTHER-0002")
                .Select(f => f.IdFacture)
                .SingleAsync();

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = otherFactureId,
                Statut = true,
                Mois = "05",
                Annees = 2026
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("SHARED-TERM");

            Assert.NotNull(result);
            Assert.Equal("SHARED-TERM", result!.NumeroFacture);
        }

        [Fact]
        public async Task ResolveFactureBySearchTermAsync_ReturnsLatestFacture_ByCodeCons()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "MUKENGE KAZAMWALI",
                AdresseClient = "Adresse",
                CodeCons = "A/a3/0116",
                Statut = true
            });

            context.Factures.AddRange(
                new Facture
                {
                    IdUsage = 1,
                    NumeroFacture = "FAC-OLD-0426-0001",
                    MoisEmission = 4,
                    AnneesEmission = 2026,
                    DateEmission = new DateTime(2026, 4, 1),
                    Statut = true
                },
                new Facture
                {
                    IdUsage = 1,
                    NumeroFacture = "FAC-NEW-0526-0001",
                    MoisEmission = 5,
                    AnneesEmission = 2026,
                    DateEmission = new DateTime(2026, 5, 1),
                    Statut = true
                });
            await context.SaveChangesAsync();

            var factureIds = await context.Factures
                .OrderBy(f => f.IdFacture)
                .Select(f => f.IdFacture)
                .ToListAsync();

            context.ClientFactures.AddRange(
                new ClientFacture
                {
                    IdClient = 1,
                    IdFacture = factureIds[0],
                    Statut = true,
                    Mois = "04",
                    Annees = 2026
                },
                new ClientFacture
                {
                    IdClient = 1,
                    IdFacture = factureIds[1],
                    Statut = true,
                    Mois = "05",
                    Annees = 2026
                });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("A/a3/0116");

            Assert.NotNull(result);
            Assert.Equal("FAC-NEW-0526-0001", result!.NumeroFacture);
        }

        [Fact]
        public async Task ResolveFactureBySearchTermAsync_ReturnsFacture_ByNomClientCaseInsensitive()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "BWAMI TAKUBUSOGA",
                AdresseClient = "Adresse",
                CodeCons = "A/a1/0236",
                Statut = true
            });

            context.Factures.Add(new Facture
            {
                IdUsage = 1,
                NumeroFacture = "FAC-DOM-0526-0099",
                MoisEmission = 5,
                AnneesEmission = 2026,
                Statut = true
            });
            await context.SaveChangesAsync();

            var factureId = await context.Factures.Select(f => f.IdFacture).SingleAsync();

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = factureId,
                Statut = true,
                Mois = "05",
                Annees = 2026
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("bwami takubusoga");

            Assert.NotNull(result);
            Assert.Equal("FAC-DOM-0526-0099", result!.NumeroFacture);
        }

        [Fact]
        public async Task ResolveFactureBySearchTermAsync_ReturnsNull_WhenClientHasNoLinkedFacture()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Sans facture",
                AdresseClient = "Adresse",
                CodeCons = "A/a1/9999",
                Statut = true
            });

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = null,
                EstArrierePreExistant = true,
                Statut = true,
                Mois = "01",
                Annees = 2025
            });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("A/a1/9999");

            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveFactureBySearchTermAsync_ReturnsNull_WhenNoMatch()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("INEXISTANT");

            Assert.Null(result);
        }

        [Fact]
        public async Task ResolveFactureBySearchTermAsync_ReturnsLatestAmongHomonyms_ByNomClient()
        {
            await using var context = CreateInMemoryContext();
            SeedUsage(context, idUsage: 1);

            context.Clients.AddRange(
                new Client
                {
                    IdClient = 1,
                    NomClient = "JEAN KABONGO",
                    AdresseClient = "A",
                    CodeCons = "A/a1/0001",
                    Statut = true
                },
                new Client
                {
                    IdClient = 2,
                    NomClient = "JEAN KABONGO",
                    AdresseClient = "B",
                    CodeCons = "A/a1/0002",
                    Statut = true
                });

            context.Factures.AddRange(
                new Facture
                {
                    IdUsage = 1,
                    NumeroFacture = "FAC-OLD-0426-0001",
                    MoisEmission = 4,
                    AnneesEmission = 2026,
                    Statut = true
                },
                new Facture
                {
                    IdUsage = 1,
                    NumeroFacture = "FAC-NEW-0526-0002",
                    MoisEmission = 5,
                    AnneesEmission = 2026,
                    Statut = true
                });
            await context.SaveChangesAsync();

            var factureIds = await context.Factures
                .OrderBy(f => f.IdFacture)
                .Select(f => f.IdFacture)
                .ToListAsync();

            context.ClientFactures.AddRange(
                new ClientFacture
                {
                    IdClient = 1,
                    IdFacture = factureIds[0],
                    Statut = true,
                    Mois = "04",
                    Annees = 2026
                },
                new ClientFacture
                {
                    IdClient = 2,
                    IdFacture = factureIds[1],
                    Statut = true,
                    Mois = "05",
                    Annees = 2026
                });
            await context.SaveChangesAsync();

            var service = CreateFactureService(context);
            var result = await service.ResolveFactureBySearchTermAsync("jean kabongo");

            Assert.NotNull(result);
            Assert.Equal("FAC-NEW-0526-0002", result!.NumeroFacture);
        }

        private static void SeedUsage(KenergieDbContext context, int idUsage)
        {
            if (!context.Societes.Any())
            {
                context.Societes.Add(new Societe
                {
                    IdSociete = 1,
                    Nom = "Societe test",
                    Type = "Privée",
                    Statut = true
                });
            }

            if (!context.CategorieClients.Any())
            {
                context.CategorieClients.Add(new CategorieClient
                {
                    IdCategorie = 1,
                    NomCategorie = "Domestique",
                    IdSociete = 1,
                    Statut = true
                });
            }

            context.Usages.Add(new Usage
            {
                IdUsage = idUsage,
                Libelle = "Usage test",
                IdCategorieClient = 1
            });
        }

        private static FactureService CreateFactureService(KenergieDbContext context)
        {
            var clientFactureRepo = new Mock<IClientFactureRepository>().Object;
            return new FactureService(
                context,
                clientFactureRepo,
                new Mock<IDeviseConversionService>().Object,
                NullLogger<FactureService>.Instance);
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
