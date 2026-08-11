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
    public class ClientFactureTypeDeCourantTests
    {
        [Fact]
        public async Task GetArrieresConsolidesGlobaux_DetailFactures_IncludesTypeDeCourant()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;
            const int idTypePermanent = 1;

            await using var context = CreateInMemoryContext();
            SeedTypeDeCourantInfrastructure(context, idTypePermanent, "Permanent");

            context.Clients.Add(new Client
            {
                IdClient = 1,
                NomClient = "Client CP",
                AdresseClient = "Adresse"
            });

            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 1,
                IdUsage = 1,
                IdTypeDeCourant = idTypePermanent,
                Statut = true,
                nombreBatiment = 1
            });

            var facture = new Facture
            {
                IdFacture = 10,
                IdUsage = 1,
                IdTypeDeCourant = idTypePermanent,
                NumeroFacture = "FAC-001",
                MoisEmission = moisPrecedent,
                AnneesEmission = anneePrecedente,
                Statut = true
            };
            context.Factures.Add(facture);

            context.ClientFactures.Add(new ClientFacture
            {
                IdClient = 1,
                IdFacture = 10,
                Mois = moisPrecedent.ToString("D2"),
                Annees = anneePrecedente,
                Montant = 5_000m,
                MontantDu = 5_000m,
                MontantPaye = 0m,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(moisFacturePrecedentSeulement: true);

            var client = Assert.Single(result.ArrieresParClient);
            var detail = Assert.Single(client.ArrieresParPeriode.Single().DetailFactures);
            Assert.Equal(idTypePermanent, detail.IdTypeDeCourant);
            Assert.Equal("Permanent", detail.TypeDeCourant);
        }

        [Fact]
        public async Task GetArrieresConsolidesGlobaux_FiltersClientsByIdTypeDeCourant()
        {
            var now = DateTime.Now;
            var moisPrecedent = now.Month == 1 ? 12 : now.Month - 1;
            var anneePrecedente = now.Month == 1 ? now.Year - 1 : now.Year;
            const int idTypePermanent = 1;
            const int idTypeNonPermanent = 2;

            await using var context = CreateInMemoryContext();
            SeedTypeDeCourantInfrastructure(context, idTypePermanent, "Permanent");
            context.TypeDeCourants.Add(new TypeDeCourant
            {
                IdTypeDeCourant = idTypeNonPermanent,
                Libelle = "Non Permanent",
                Statut = true
            });

            context.Clients.AddRange(
                new Client { IdClient = 1, NomClient = "Client CP", AdresseClient = "A1" },
                new Client { IdClient = 2, NomClient = "Client CD", AdresseClient = "A2" });

            context.ClientUsages.AddRange(
                new ClientUsage
                {
                    IdClient = 1,
                    IdUsage = 1,
                    IdTypeDeCourant = idTypePermanent,
                    Statut = true,
                    nombreBatiment = 1
                },
                new ClientUsage
                {
                    IdClient = 2,
                    IdUsage = 1,
                    IdTypeDeCourant = idTypeNonPermanent,
                    Statut = true,
                    nombreBatiment = 1
                });

            context.Factures.AddRange(
                new Facture
                {
                    IdFacture = 10,
                    IdUsage = 1,
                    IdTypeDeCourant = idTypePermanent,
                    MoisEmission = moisPrecedent,
                    AnneesEmission = anneePrecedente,
                    Statut = true
                },
                new Facture
                {
                    IdFacture = 11,
                    IdUsage = 1,
                    IdTypeDeCourant = idTypeNonPermanent,
                    MoisEmission = moisPrecedent,
                    AnneesEmission = anneePrecedente,
                    Statut = true
                });

            context.ClientFactures.AddRange(
                new ClientFacture
                {
                    IdClient = 1,
                    IdFacture = 10,
                    Mois = moisPrecedent.ToString("D2"),
                    Annees = anneePrecedente,
                    Montant = 1_000m,
                    MontantDu = 1_000m,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                },
                new ClientFacture
                {
                    IdClient = 2,
                    IdFacture = 11,
                    Mois = moisPrecedent.ToString("D2"),
                    Annees = anneePrecedente,
                    Montant = 2_000m,
                    MontantDu = 2_000m,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var result = await service.GetArrieresConsolidesGlobauxAsync(
                moisFacturePrecedentSeulement: true,
                idTypeDeCourant: idTypePermanent);

            var client = Assert.Single(result.ArrieresParClient);
            Assert.Equal(1, client.IdClient);
        }

        private static void SeedTypeDeCourantInfrastructure(
            KenergieDbContext context,
            int idTypeDeCourant,
            string libelle)
        {
            context.Societes.Add(new Societe { IdSociete = 1, Nom = "Test SA" });
            context.CategorieClients.Add(new CategorieClient
            {
                IdCategorie = 1,
                IdSociete = 1,
                NomCategorie = "Domestique",
                Statut = true
            });
            context.Usages.Add(new Usage
            {
                IdUsage = 1,
                IdCategorieClient = 1,
                Libelle = "Residentiel",
                Statut = true
            });
            context.TypeDeCourants.Add(new TypeDeCourant
            {
                IdTypeDeCourant = idTypeDeCourant,
                Libelle = libelle,
                Statut = true
            });
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
