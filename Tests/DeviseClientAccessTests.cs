using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    public class DeviseClientAccessTests
    {
        [Fact]
        public async Task GetDevisesActivesAsync_WithSocieteFilter_ReturnsOnlyThatSociete()
        {
            await using var context = CreateContext();
            await SeedTwoSocietesWithDevisesAsync(context);

            var service = CreateService(context);
            var devises = (await service.GetDevisesActivesAsync(1)).ToList();

            Assert.NotEmpty(devises);
            Assert.All(devises, d => Assert.Equal(1, d.IdSociete));
            Assert.Contains(devises, d => d.CodeDevise == "CDF");
            Assert.DoesNotContain(devises, d => d.IdSociete == 2);
        }

        [Fact]
        public async Task ResolveSocieteFromClientUsage_ReturnsLinkedSociete()
        {
            await using var context = CreateContext();
            await SeedTwoSocietesWithDevisesAsync(context);

            context.Clients.Add(new Client
            {
                IdClient = 10,
                NomClient = "Client A",
                AdresseClient = "Adr",
                Statut = true,
                IsActif = true
            });
            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = 100,
                NomComplet = "User Client",
                MotDePasseHash = "x",
                IdClient = 10
            });
            context.ClientUsages.Add(new ClientUsage
            {
                IdClient = 10,
                IdUsage = 1,
                Statut = true,
                nombreBatiment = 1
            });
            await context.SaveChangesAsync();

            var idClient = await context.Utilisateurs
                .AsNoTracking()
                .Where(u => u.IdUtilisateur == 100)
                .Select(u => u.IdClient)
                .FirstOrDefaultAsync();

            Assert.Equal(10, idClient);

            var idSociete = await context.ClientUsages
                .AsNoTracking()
                .Where(cu => cu.IdClient == idClient!.Value && cu.Statut)
                .Select(cu => (int?)cu.Usage!.CategorieClient!.IdSociete)
                .FirstOrDefaultAsync();

            Assert.Equal(1, idSociete);

            var service = CreateService(context);
            var devises = (await service.GetDevisesActivesAsync(idSociete)).ToList();
            Assert.All(devises, d => Assert.Equal(1, d.IdSociete));
            Assert.DoesNotContain(devises, d => d.CodeDevise == "EUR");
        }

        private static async Task SeedTwoSocietesWithDevisesAsync(KenergieDbContext context)
        {
            context.Societes.AddRange(
                new Societe { IdSociete = 1, Nom = "SA1", Type = "Privée", Statut = true, CodeDevisePrincipale = "CDF" },
                new Societe { IdSociete = 2, Nom = "SA2", Type = "Privée", Statut = true, CodeDevisePrincipale = "CDF" });

            context.CategorieClients.Add(new CategorieClient
            {
                IdCategorie = 1,
                IdSociete = 1,
                NomCategorie = "Dom",
                Statut = true
            });
            context.Usages.Add(new Usage { IdUsage = 1, IdCategorieClient = 1, Libelle = "Res", Statut = true });

            context.DevisesMonetaires.AddRange(
                new DeviseMonetaire
                {
                    IdSociete = 1,
                    CodeDevise = "CDF",
                    Libelle = "Franc",
                    Symbole = "FC",
                    Statut = true
                },
                new DeviseMonetaire
                {
                    IdSociete = 1,
                    CodeDevise = "USD",
                    Libelle = "Dollar",
                    Symbole = "$",
                    Statut = true
                },
                new DeviseMonetaire
                {
                    IdSociete = 2,
                    CodeDevise = "EUR",
                    Libelle = "Euro",
                    Symbole = "€",
                    Statut = true
                });

            await context.SaveChangesAsync();
        }

        private static DeviseService CreateService(KenergieDbContext context)
        {
            var conversion = new Mock<IDeviseConversionService>();
            return new DeviseService(context, conversion.Object);
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
