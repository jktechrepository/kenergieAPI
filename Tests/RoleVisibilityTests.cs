using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kenergie.Tests
{
    public class RoleVisibilityTests
    {
        [Fact]
        public async Task GetVisibleForCallerAsync_Niveau1_ReturnsAllExceptClient()
        {
            await using var context = CreateContext();
            SeedRoles(context);
            var service = new RoleService(context);

            var roles = (await service.GetVisibleForCallerAsync(1)).ToList();

            Assert.DoesNotContain(roles, r => r.Nom == "Client");
            Assert.Contains(roles, r => r.Nom == "Super-Admin");
            Assert.Contains(roles, r => r.Nom == "Admin");
            Assert.Contains(roles, r => r.Nom == "Caissier");
            Assert.Equal(6, roles.Count);
        }

        [Fact]
        public async Task GetVisibleForCallerAsync_Niveau2_ExcludesSuperAdminAndClient()
        {
            await using var context = CreateContext();
            SeedRoles(context);
            var service = new RoleService(context);

            var roles = (await service.GetVisibleForCallerAsync(2)).ToList();
            var names = roles.Select(r => r.Nom).ToList();

            Assert.DoesNotContain("Super-Admin", names);
            Assert.DoesNotContain("Client", names);
            Assert.Contains("Admin", names);
            Assert.Contains("Gerant", names);
            Assert.All(roles, r => Assert.True(r.Niveau != null && r.Niveau >= 2));
        }

        [Fact]
        public async Task GetVisibleForCallerAsync_Niveau5_ExcludesLevels1To4AndClient()
        {
            await using var context = CreateContext();
            SeedRoles(context);
            var service = new RoleService(context);

            var roles = (await service.GetVisibleForCallerAsync(5)).ToList();
            var names = roles.Select(r => r.Nom).ToList();

            Assert.DoesNotContain("Super-Admin", names);
            Assert.DoesNotContain("Admin", names);
            Assert.DoesNotContain("Gerant", names);
            Assert.DoesNotContain("Financier", names);
            Assert.DoesNotContain("Client", names);
            Assert.Contains("Caissier", names);
            Assert.Contains("Technicien", names);
            Assert.All(roles, r => Assert.True(r.Niveau != null && r.Niveau >= 5));
        }

        [Fact]
        public async Task GetVisibleForCallerAsync_Admin_ExcludesSuperAdminWithNullNiveau()
        {
            await using var context = CreateContext();
            context.Roles.AddRange(
                new Role { IdRole = 1, Nom = "Super-Admin", Niveau = null, Statut = true },
                new Role { IdRole = 2, Nom = "Admin", Niveau = 2, Statut = true },
                new Role { IdRole = 3, Nom = "Gerant", Niveau = 3, Statut = true },
                new Role { IdRole = 7, Nom = "Client", Niveau = 7, Statut = true }
            );
            await context.SaveChangesAsync();

            var service = new RoleService(context);
            var roles = (await service.GetVisibleForCallerAsync(2)).ToList();
            var names = roles.Select(r => r.Nom).ToList();

            Assert.DoesNotContain("Super-Admin", names);
            Assert.DoesNotContain("Client", names);
            Assert.Contains("Admin", names);
        }

        [Fact]
        public async Task GetVisibleForCallerAsync_Admin_ExcludesSuperAdminWithCorruptNiveau5()
        {
            await using var context = CreateContext();
            context.Roles.AddRange(
                new Role { IdRole = 1, Nom = "Super-Admin", Niveau = 5, Statut = true },
                new Role { IdRole = 2, Nom = "Admin", Niveau = 2, Statut = true },
                new Role { IdRole = 5, Nom = "Caissier", Niveau = 5, Statut = true },
                new Role { IdRole = 7, Nom = "Client", Niveau = 7, Statut = true }
            );
            await context.SaveChangesAsync();

            var service = new RoleService(context);
            var roles = (await service.GetVisibleForCallerAsync(2)).ToList();
            var names = roles.Select(r => r.Nom).ToList();

            Assert.DoesNotContain("Super-Admin", names);
            Assert.DoesNotContain("Client", names);
            Assert.Contains("Admin", names);
            Assert.Contains("Caissier", names);
        }

        [Fact]
        public async Task GetAllAsync_ByRoleName_UsesNiveauAndExcludesClient()
        {
            await using var context = CreateContext();
            SeedRoles(context);
            var service = new RoleService(context);

            var roles = (await service.GetAllAsync("Admin")).ToList();
            var names = roles.Select(r => r.Nom).ToList();

            Assert.DoesNotContain("Super-Admin", names);
            Assert.DoesNotContain("Client", names);
            Assert.Contains("Admin", names);
        }

        private static KenergieDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<KenergieDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new KenergieDbContext(options);
        }

        private static void SeedRoles(KenergieDbContext context)
        {
            context.Roles.AddRange(
                new Role { IdRole = 1, Nom = "Super-Admin", Niveau = 1, Statut = true },
                new Role { IdRole = 2, Nom = "Admin", Niveau = 2, Statut = true },
                new Role { IdRole = 3, Nom = "Gerant", Niveau = 3, Statut = true },
                new Role { IdRole = 4, Nom = "Financier", Niveau = 4, Statut = true },
                new Role { IdRole = 5, Nom = "Caissier", Niveau = 5, Statut = true },
                new Role { IdRole = 6, Nom = "Technicien", Niveau = 6, Statut = true },
                new Role { IdRole = 7, Nom = "Client", Niveau = 7, Statut = true }
            );
            context.SaveChanges();
        }
    }
}
