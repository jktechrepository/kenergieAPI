using Kenergie.Models;
using Kenergie.Services.Notifications;
using Kenergie.Services.Repositories;
using KenergieAPI.Services.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kenergie.Tests
{
    public class NotificationSenderReceiveNotificationTests
    {
        [Fact]
        public async Task SendAsync_InAppPersisted_CallsSendNotificationToUserAsync()
        {
            var firebase = new Mock<IFirebaseNotificationService>();
            var signalR = new Mock<ISignalRNotificationService>();
            var sms = new Mock<ISmsNotificationService>();
            var email = new Mock<IEmailService>();
            var notifRepo = new Mock<INotificationRepository>();

            var persisted = new Notification
            {
                IdNotification = 42,
                Titre = "Paiement reçu",
                Contenu = "Votre paiement a été confirmé",
                TypeNotification = "Paiement",
                IdDestinataire = 7,
                EstLue = false,
                EstActive = true
            };

            notifRepo
                .Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ReturnsAsync(persisted);

            var sender = new NotificationSender(
                firebase.Object,
                signalR.Object,
                sms.Object,
                email.Object,
                notifRepo.Object,
                NullLogger<NotificationSender>.Instance);

            var utilisateur = new Utilisateur
            {
                IdUtilisateur = 7,
                NomComplet = "Client Test",
                MotDePasseHash = "x"
            };

            var dispatch = new NotificationDispatchResult(
                new NotificationContext
                {
                    Kind = NotificationKind.Paiement,
                    UtilisateurDestinataire = utilisateur
                },
                new NotificationMessage
                {
                    InApp = new InAppNotificationMessage
                    {
                        IsEnabled = true,
                        Title = "Paiement reçu",
                        Content = "Votre paiement a été confirmé",
                        Type = "Paiement"
                    }
                });

            await sender.SendAsync(dispatch);

            signalR.Verify(
                s => s.SendNotificationToUserAsync(
                    7,
                    It.Is<Notification>(n => n.IdNotification == 42 && n.Titre == "Paiement reçu")),
                Times.Once);

            signalR.Verify(
                s => s.SendCustomNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task SendAsync_InAppPersistFails_FallsBackToSendCustomNotificationAsync()
        {
            var firebase = new Mock<IFirebaseNotificationService>();
            var signalR = new Mock<ISignalRNotificationService>();
            var sms = new Mock<ISmsNotificationService>();
            var email = new Mock<IEmailService>();
            var notifRepo = new Mock<INotificationRepository>();

            notifRepo
                .Setup(r => r.CreateAsync(It.IsAny<Notification>()))
                .ThrowsAsync(new InvalidOperationException("db down"));

            var sender = new NotificationSender(
                firebase.Object,
                signalR.Object,
                sms.Object,
                email.Object,
                notifRepo.Object,
                NullLogger<NotificationSender>.Instance);

            var utilisateur = new Utilisateur
            {
                IdUtilisateur = 7,
                NomComplet = "Client Test",
                MotDePasseHash = "x"
            };

            var dispatch = new NotificationDispatchResult(
                new NotificationContext
                {
                    Kind = NotificationKind.Paiement,
                    UtilisateurDestinataire = utilisateur
                },
                new NotificationMessage
                {
                    InApp = new InAppNotificationMessage
                    {
                        IsEnabled = true,
                        Title = "Titre",
                        Content = "Corps",
                        Type = "INFO"
                    }
                });

            await sender.SendAsync(dispatch);

            signalR.Verify(
                s => s.SendCustomNotificationAsync(7, "Titre", "Corps", "INFO"),
                Times.Once);

            signalR.Verify(
                s => s.SendNotificationToUserAsync(It.IsAny<int>(), It.IsAny<Notification>()),
                Times.Never);
        }
    }
}
