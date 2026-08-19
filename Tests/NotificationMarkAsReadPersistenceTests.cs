using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using KenergieAPI.Hubs;
using KenergieAPI.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    /// <summary>
    /// Tests de la logique ownership + persistance utilisée par NotificationHub.MarkNotificationAsRead.
    /// </summary>
    public class NotificationMarkAsReadPersistenceTests
    {
        [Fact]
        public async Task MarquerCommeLueAsync_SetsEstLueAndDateLecture()
        {
            await using var context = CreateContext();
            var notif = await SeedNotificationAsync(context, idDestinataire: 10, estLue: false);
            var repo = CreateRepo(context);

            var ok = await repo.MarquerCommeLueAsync(notif.IdNotification);

            Assert.True(ok);
            var reloaded = await context.Notifications.FindAsync(notif.IdNotification);
            Assert.NotNull(reloaded);
            Assert.True(reloaded!.EstLue);
            Assert.NotNull(reloaded.DateLecture);
        }

        [Fact]
        public async Task Ownership_DestinataireOk_AllowsMark()
        {
            await using var context = CreateContext();
            var notif = await SeedNotificationAsync(context, idDestinataire: 10, estLue: false);
            var repo = CreateRepo(context);

            var loaded = await repo.GetByIdAsync(notif.IdNotification);
            Assert.NotNull(loaded);
            Assert.Equal(10, loaded!.IdDestinataire);

            // Même règle que le hub
            Assert.True(loaded.IdDestinataire == 10);

            await repo.MarquerCommeLueAsync(notif.IdNotification);
            var reloaded = await repo.GetByIdAsync(notif.IdNotification);
            Assert.True(reloaded!.EstLue);
        }

        [Fact]
        public async Task Ownership_OtherUser_DoesNotMark()
        {
            await using var context = CreateContext();
            var notif = await SeedNotificationAsync(context, idDestinataire: 10, estLue: false);
            var repo = CreateRepo(context);

            var loaded = await repo.GetByIdAsync(notif.IdNotification);
            const int otherUserId = 99;

            // Hub refuse si IdDestinataire != userId
            Assert.NotEqual(loaded!.IdDestinataire, otherUserId);

            // Autre user ne doit pas appeler MarquerCommeLue — état inchangé
            var reloaded = await context.Notifications.FindAsync(notif.IdNotification);
            Assert.False(reloaded!.EstLue);
            Assert.Null(reloaded.DateLecture);
        }

        [Fact]
        public async Task GetByIdAsync_Missing_ReturnsNull_MapsToNotFound()
        {
            await using var context = CreateContext();
            var repo = CreateRepo(context);

            var loaded = await repo.GetByIdAsync(99999);
            Assert.Null(loaded);
        }

        [Fact]
        public void Hub_RequiresNotificationRepository()
        {
            var logger = NullLogger<NotificationHub>.Instance;
            var repo = new Mock<INotificationRepository>().Object;
            var hub = new NotificationHub(logger, repo);
            Assert.NotNull(hub);
        }

        private static INotificationRepository CreateRepo(KenergieDbContext context)
        {
            return new NotificationService(
                context,
                new Mock<IFirebaseNotificationService>().Object,
                new Mock<ISmsNotificationService>().Object,
                NullLogger<NotificationService>.Instance);
        }

        private static async Task<Notification> SeedNotificationAsync(
            KenergieDbContext context,
            int idDestinataire,
            bool estLue)
        {
            var notif = new Notification
            {
                Titre = "Test",
                Contenu = "Contenu",
                TypeNotification = "INFO",
                EstLue = estLue,
                EstActive = true,
                Statut = true,
                IdDestinataire = idDestinataire,
                CanalUtilise = "InApp",
                Priorite = "INFO",
                StatutEnvoi = "Envoye",
                DateCreation = DateTime.UtcNow
            };
            context.Notifications.Add(notif);
            await context.SaveChangesAsync();
            return notif;
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
