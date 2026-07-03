using Microsoft.AspNetCore.SignalR;
using Kenergie.Hubs;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Statistiques;
using KenergieAPI.Services.Repositories;

namespace Kenergie.Services
{
    /// <summary>
    /// Service d'extension pour envoyer les notifications SignalR via DashboardHub
    /// </summary>
    public class SignalRNotificationService : ISignalRNotificationService
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<SignalRNotificationService> _logger;

        public SignalRNotificationService(
            IHubContext<DashboardHub> hubContext,
            ILogger<SignalRNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        #region Dashboard Notifications

        /// <summary>
        /// Notifier la mise à jour complète du dashboard
        /// </summary>
        public async Task NotifyDashboardUpdatedAsync(int societeId, DashboardDto dashboardData)
        {
            try
            {
                await _hubContext.Clients.Group($"dashboard_societe_{societeId}").SendAsync("DashboardUpdated", new
                {
                    societeId = societeId,
                    dashboard = dashboardData,
                    timestamp = DateTime.UtcNow,
                    type = "full_update"
                });

                _logger.LogInformation($"📊 Dashboard updated notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending dashboard updated notification for society {societeId}");
            }
        }

        /// <summary>
        /// Notifier un nouveau paiement
        /// </summary>
        public async Task NotifyNewPaiementAsync(int societeId, object paiementData)
        {
            try
            {
                await _hubContext.Clients.Group($"dashboard_societe_{societeId}").SendAsync("NewPaiement", new
                {
                    societeId = societeId,
                    paiement = paiementData,
                    timestamp = DateTime.UtcNow,
                    type = "new_paiement"
                });

                _logger.LogInformation($"💰 New paiement notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending new paiement notification for society {societeId}");
            }
        }

        /// <summary>
        /// Notifier un nouveau client
        /// </summary>
        public async Task NotifyNewClientAsync(int societeId, object clientData)
        {
            try
            {
                await _hubContext.Clients.Group($"dashboard_societe_{societeId}").SendAsync("NewClient", new
                {
                    societeId = societeId,
                    client = clientData,
                    timestamp = DateTime.UtcNow,
                    type = "new_client"
                });

                _logger.LogInformation($"👤 New client notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending new client notification for society {societeId}");
            }
        }

        /// <summary>
        /// Notifier un changement de statut du dashboard
        /// </summary>
        public async Task NotifyDashboardStatusChangeAsync(int societeId, string entityType, int entityId, string newStatus)
        {
            try
            {
                await _hubContext.Clients.Group($"dashboard_societe_{societeId}").SendAsync("DashboardStatusChanged", new
                {
                    societeId = societeId,
                    entityType = entityType,
                    entityId = entityId,
                    newStatus = newStatus,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"🔄 Dashboard status change notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending dashboard status change notification for society {societeId}");
            }
        }

        #endregion

        #region Statistiques Notifications

        /// <summary>
        /// Notifier la mise à jour des statistiques générales
        /// </summary>
        public async Task NotifyStatistiquesGeneralesUpdatedAsync(int societeId, StatistiquesGeneralesDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients.Group($"statistiques_updates_{societeId}").SendAsync("StatistiquesGeneralesUpdated", new
                {
                    societeId = societeId,
                    statistiques = statistiquesData,
                    timestamp = DateTime.UtcNow,
                    type = "generales_update"
                });

                _logger.LogInformation($"📈 General statistics updated notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending general statistics updated notification for society {societeId}");
            }
        }

        /// <summary>
        /// Notifier la mise à jour des statistiques financières
        /// </summary>
        public async Task NotifyStatistiquesFinancieresUpdatedAsync(int societeId, StatistiquesFinancieresDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients.Group($"statistiques_updates_{societeId}").SendAsync("StatistiquesFinancieresUpdated", new
                {
                    societeId = societeId,
                    statistiques = statistiquesData,
                    timestamp = DateTime.UtcNow,
                    type = "financieres_update"
                });

                _logger.LogInformation($"💰 Financial statistics updated notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending financial statistics updated notification for society {societeId}");
            }
        }

        /// <summary>
        /// Notifier un changement de statut des statistiques
        /// </summary>
        public async Task NotifyStatistiquesStatusChangeAsync(int societeId, string entityType, int entityId, string newStatus)
        {
            try
            {
                await _hubContext.Clients.Group($"statistiques_updates_{societeId}").SendAsync("StatistiquesStatusChanged", new
                {
                    societeId = societeId,
                    entityType = entityType,
                    entityId = entityId,
                    newStatus = newStatus,
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation($"🔄 Statistics status change notification sent for society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistics status change notification for society {societeId}");
            }
        }

        #region ISignalRNotificationService Implementation

        /// <summary>
        /// Envoyer une notification en temps réel à un utilisateur spécifique
        /// </summary>
        public async Task SendNotificationToUserAsync(int userId, Notification notification)
        {
            try
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
                _logger.LogInformation($"📱 Notification sent to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending notification to user {userId}");
            }
        }

        /// <summary>
        /// Envoyer une notification en temps réel à plusieurs utilisateurs
        /// </summary>
        public async Task SendNotificationToUsersAsync(List<int> userIds, Notification notification)
        {
            try
            {
                var userNames = userIds.Select(id => id.ToString()).ToArray();
                await _hubContext.Clients.Users(userNames).SendAsync("ReceiveNotification", notification);
                _logger.LogInformation($"📱 Notification sent to {userIds.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending notification to multiple users");
            }
        }

        /// <summary>
        /// Envoyer une notification à tous les utilisateurs d'une société
        /// </summary>
        public async Task SendNotificationToSocieteAsync(int societeId, Notification notification)
        {
            try
            {
                await _hubContext.Clients.Group($"societe_{societeId}").SendAsync("ReceiveNotification", notification);
                _logger.LogInformation($"📱 Notification sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending notification to society {societeId}");
            }
        }

        /// <summary>
        /// Envoyer une notification à tous les utilisateurs d'une classe
        /// </summary>
        public async Task SendNotificationToClasseAsync(int classeId, Notification notification)
        {
            try
            {
                await _hubContext.Clients.Group($"classe_{classeId}").SendAsync("ReceiveNotification", notification);
                _logger.LogInformation($"📱 Notification sent to class {classeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending notification to class {classeId}");
            }
        }

        /// <summary>
        /// Envoyer une notification à tous les utilisateurs connectés
        /// </summary>
        public async Task SendNotificationToAllAsync(Notification notification)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                _logger.LogInformation($"📱 Notification sent to all users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending notification to all users");
            }
        }

        /// <summary>
        /// Envoyer une notification personnalisée à un utilisateur
        /// </summary>
        public async Task SendCustomNotificationAsync(int userId, string title, string message, string type = "info")
        {
            try
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveCustomNotification", new { title, message, type });
                _logger.LogInformation($"📱 Custom notification sent to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending custom notification to user {userId}");
            }
        }

        /// <summary>
        /// Notifier un changement de statut (présence, paiement, etc.)
        /// </summary>
        public async Task NotifyStatusChangeAsync(int userId, string entityType, int entityId, string newStatus)
        {
            try
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("StatusChanged", new { entityType, entityId, newStatus });
                _logger.LogInformation($"📱 Status change notification sent to user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending status change notification to user {userId}");
            }
        }

        /// <summary>
        /// Notifier un nouveau message
        /// </summary>
        public async Task NotifyNewMessageAsync(int recipientId, int senderId, string senderName, string messageContent)
        {
            try
            {
                await _hubContext.Clients.User(recipientId.ToString()).SendAsync("NewMessage", new { senderId, senderName, messageContent });
                _logger.LogInformation($"📱 New message notification sent to user {recipientId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending new message notification to user {recipientId}");
            }
        }

        /// <summary>
        /// Notifier une nouvelle note publiée
        /// </summary>
        public async Task NotifyNewGradeAsync(int studentId, string courseName, decimal? grade)
        {
            try
            {
                await _hubContext.Clients.User(studentId.ToString()).SendAsync("NewGrade", new { courseName, grade });
                _logger.LogInformation($"📱 New grade notification sent to student {studentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending new grade notification to student {studentId}");
            }
        }

        /// <summary>
        /// Notifier la mise à jour du dashboard Super-Admin
        /// </summary>
        public async Task NotifySuperAdminDashboardUpdatedAsync(object dashboardData)
        {
            try
            {
                await _hubContext.Clients.Group("super_admin_dashboard").SendAsync("SuperAdminDashboardUpdated", new
                {
                    dashboard = dashboardData,
                    timestamp = DateTime.UtcNow,
                    type = "full_update"
                });

                _logger.LogInformation("🔔 Super Admin dashboard update notification sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending Super Admin dashboard update notification");
            }
        }

        /// <summary>
        /// Notifier une nouvelle alerte critique pour le Super-Admin
        /// </summary>
        public async Task NotifySuperAdminAlerteCritiqueAsync(object alerteData)
        {
            try
            {
                await _hubContext.Clients.Group("super_admin_dashboard").SendAsync("SuperAdminAlerteCritique", new
                {
                    alerte = alerteData,
                    timestamp = DateTime.UtcNow,
                    type = "alerte_critique"
                });

                _logger.LogInformation("🚨 Super Admin critical alert notification sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending Super Admin critical alert notification");
            }
        }

        /// <summary>
        /// Notifier un changement dans les statistiques globales
        /// </summary>
        public async Task NotifySuperAdminStatistiquesUpdatedAsync(object statistiquesData)
        {
            try
            {
                await _hubContext.Clients.Group("super_admin_dashboard").SendAsync("SuperAdminStatistiquesUpdated", new
                {
                    statistiques = statistiquesData,
                    timestamp = DateTime.UtcNow,
                    type = "statistiques_update"
                });

                _logger.LogInformation("📊 Super Admin statistics update notification sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending Super Admin statistics update notification");
            }
        }

        #endregion

        #endregion
    }
}
