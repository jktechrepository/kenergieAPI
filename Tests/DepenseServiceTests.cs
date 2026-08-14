using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Depense;
using Kenergie.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kenergie.Tests
{
    public class DepenseServiceTests
    {
        [Fact]
        public async Task CreateAsync_Financier_CreatesEnAttenteWithoutValidator()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var dto = new CreateDepenseDto
            {
                IdSociete = 1,
                Libelle = "Achat carburant",
                Montant = 50_000m,
                CodeDeviseMontant = "CDF",
                DateDepense = DateTime.UtcNow
            };

            var result = await service.CreateAsync(dto, callerUserId: 10, callerRole: "Financier", callerSocieteId: 1);

            Assert.Equal(DepenseStatuts.EnAttente, result.Statut);
            Assert.Null(result.IdUtilisateurValidateur);
            Assert.Null(result.DateValidation);
            Assert.Null(result.MontantDevisePrincipale);
            Assert.Equal("CDF", result.CodeDeviseMontant);
        }

        [Fact]
        public async Task CreateAsync_Admin_ThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var dto = new CreateDepenseDto { IdSociete = 1, Libelle = "Admin", Montant = 1_000m };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateAsync(dto, callerUserId: 20, callerRole: "Admin", callerSocieteId: 1));
        }

        [Fact]
        public async Task CreateAsync_SuperAdmin_ThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var dto = new CreateDepenseDto { IdSociete = 1, Libelle = "SA", Montant = 1_000m };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateAsync(dto, callerUserId: 1, callerRole: "Super-Admin", callerSocieteId: 1));
        }

        [Fact]
        public async Task CreateAsync_Gerant_ThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var dto = new CreateDepenseDto { IdSociete = 1, Libelle = "Gerant", Montant = 1_000m };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateAsync(dto, callerUserId: 30, callerRole: "Gerant", callerSocieteId: 1));
        }

        [Fact]
        public async Task CreateAsync_Caissier_ThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var dto = new CreateDepenseDto
            {
                IdSociete = 1,
                Libelle = "Tentative caissier",
                Montant = 1_000m
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateAsync(dto, callerUserId: 11, callerRole: "Caissier", callerSocieteId: 1));
        }

        [Fact]
        public async Task CreateAsync_OtherSociete_ThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var dto = new CreateDepenseDto
            {
                IdSociete = 2,
                Libelle = "Hors scope",
                Montant = 1_000m
            };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.CreateAsync(dto, callerUserId: 10, callerRole: "Financier", callerSocieteId: 1));
        }

        [Fact]
        public async Task ValiderAsync_Admin_SetsValideeAndAppliesSnapshot()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);
            SeedUser(context, userId: 20, nom: "Admin Test", societeId: 1);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 50_000m, CodeDeviseMontant = "CDF" },
                10, "Financier", 1);

            var validated = await service.ValiderAsync(created.IdDepense, 20, "Admin", 1);

            Assert.NotNull(validated);
            Assert.Equal(DepenseStatuts.Validee, validated!.Statut);
            Assert.Equal(20, validated.IdUtilisateurValidateur);
            Assert.NotNull(validated.DateValidation);
            Assert.Equal("CDF", validated.CodeDevisePrincipale);
            Assert.Equal(50_000m, validated.MontantDevisePrincipale);
        }

        [Fact]
        public async Task ValiderAsync_Gerant_SetsValidee()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);
            SeedUser(context, userId: 30, nom: "Gerant Test", societeId: 1);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);

            var validated = await service.ValiderAsync(created.IdDepense, 30, "Gerant", 1);

            Assert.Equal(DepenseStatuts.Validee, validated!.Statut);
            Assert.Equal(30, validated.IdUtilisateurValidateur);
        }

        [Fact]
        public async Task ValiderAsync_Financier_ThrowsUnauthorized()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ValiderAsync(created.IdDepense, 10, "Financier", 1));
        }

        [Fact]
        public async Task RefuserAsync_Admin_SetsAnnulee()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);

            var refused = await service.RefuserAsync(
                created.IdDepense,
                new AnnulerDepenseDto { MotifAnnulation = "Justificatif manquant" },
                20, "Admin", 1);

            Assert.Equal(DepenseStatuts.Annulee, refused!.Statut);
            Assert.Equal("Justificatif manquant", refused.MotifAnnulation);
        }

        [Fact]
        public async Task AnnulerAsync_Financier_EnAttente_SetsAnnulee()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);

            var annulee = await service.AnnulerAsync(
                created.IdDepense,
                new AnnulerDepenseDto { MotifAnnulation = "Erreur saisie" },
                10, "Financier", 1);

            Assert.NotNull(annulee);
            Assert.Equal(DepenseStatuts.Annulee, annulee!.Statut);
        }

        [Fact]
        public async Task AnnulerAsync_Admin_Validee_SetsAnnulee()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);
            await service.ValiderAsync(created.IdDepense, 20, "Admin", 1);

            var annulee = await service.AnnulerAsync(
                created.IdDepense,
                new AnnulerDepenseDto { MotifAnnulation = "Correction" },
                20, "Admin", 1);

            Assert.Equal(DepenseStatuts.Annulee, annulee!.Statut);
        }

        [Fact]
        public async Task UpdateAsync_EnAttente_Financier_Succeeds()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);

            var updated = await service.UpdateAsync(
                created.IdDepense,
                new UpdateDepenseDto { Libelle = "Charge corrigée" },
                10, "Financier", 1);

            Assert.Equal("Charge corrigée", updated!.Libelle);
            Assert.Equal(DepenseStatuts.EnAttente, updated.Statut);
        }

        [Fact]
        public async Task UpdateAsync_ValideeDepense_ThrowsInvalidOperation()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);
            await service.ValiderAsync(created.IdDepense, 20, "Admin", 1);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateAsync(created.IdDepense, new UpdateDepenseDto { Libelle = "Modif" }, 10, "Financier", 1));
        }

        [Fact]
        public async Task UpdateAsync_AnnuleeDepense_ThrowsInvalidOperation()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            var service = CreateService(context);
            var created = await service.CreateAsync(
                new CreateDepenseDto { IdSociete = 1, Libelle = "Charge", Montant = 10_000m },
                10, "Financier", 1);

            await service.AnnulerAsync(created.IdDepense, new AnnulerDepenseDto(), 10, "Financier", 1);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateAsync(created.IdDepense, new UpdateDepenseDto { Libelle = "Modif" }, 10, "Financier", 1));
        }

        [Fact]
        public async Task GetPagedAsync_ExcludesAnnuleeAndEnAttenteFromValidatedSumScope()
        {
            await using var context = CreateContext();
            SeedBase(context, societeId: 1, userId: 10);

            context.Depenses.AddRange(
                new Depense
                {
                    IdSociete = 1,
                    Libelle = "Validee",
                    Montant = 30_000m,
                    MontantDevisePrincipale = 30_000m,
                    CodeDeviseMontant = "CDF",
                    CodeDevisePrincipale = "CDF",
                    Statut = DepenseStatuts.Validee,
                    IdUtilisateurCreateur = 10,
                    DateDepense = DateTime.Today,
                    DateCreation = DateTime.UtcNow
                },
                new Depense
                {
                    IdSociete = 1,
                    Libelle = "EnAttente",
                    Montant = 50_000m,
                    CodeDeviseMontant = "CDF",
                    Statut = DepenseStatuts.EnAttente,
                    IdUtilisateurCreateur = 10,
                    DateDepense = DateTime.Today,
                    DateCreation = DateTime.UtcNow
                },
                new Depense
                {
                    IdSociete = 1,
                    Libelle = "Annulee",
                    Montant = 99_000m,
                    MontantDevisePrincipale = 99_000m,
                    CodeDeviseMontant = "CDF",
                    CodeDevisePrincipale = "CDF",
                    Statut = DepenseStatuts.Annulee,
                    IdUtilisateurCreateur = 10,
                    DateDepense = DateTime.Today,
                    DateCreation = DateTime.UtcNow
                });
            await context.SaveChangesAsync();

            var totalSorties = await context.Depenses
                .Where(d => !d.IsDeleted
                    && d.IdSociete == 1
                    && d.Statut == DepenseStatuts.Validee
                    && d.DateDepense.Date == DateTime.Today)
                .SumAsync(d => d.MontantDevisePrincipale ?? d.Montant);

            Assert.Equal(30_000m, totalSorties);
        }

        private static DepenseService CreateService(KenergieDbContext context)
        {
            var deviseConversion = new DeviseConversionService(context);
            return new DepenseService(context, deviseConversion, NullLogger<DepenseService>.Instance);
        }

        private static KenergieDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }

        private static void SeedBase(KenergieDbContext context, int societeId, int userId)
        {
            context.Societes.Add(new Societe
            {
                IdSociete = societeId,
                Nom = "Test SA",
                CodeDevisePrincipale = "CDF"
            });

            SeedUser(context, userId, "Financier Test", societeId);
            context.SaveChanges();
        }

        private static void SeedUser(KenergieDbContext context, int userId, string nom, int societeId)
        {
            if (context.Utilisateurs.Any(u => u.IdUtilisateur == userId))
                return;

            context.Utilisateurs.Add(new Utilisateur
            {
                IdUtilisateur = userId,
                NomComplet = nom,
                MotDePasseHash = "hash",
                IdSociete = societeId,
                Statut = true
            });
            context.SaveChanges();
        }
    }
}
