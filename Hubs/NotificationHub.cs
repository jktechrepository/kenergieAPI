using System.Security.Claims;
using KenergieAPI.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KenergieAPI.Hubs
{
    /// <summary>
    /// Hub SignalR pour les notifications en temps réel
    /// </summary>
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly INotificationRepository _notificationRepository;

        public NotificationHub(
            ILogger<NotificationHub> logger,
            INotificationRepository notificationRepository)
        {
            _logger = logger;
            _notificationRepository = notificationRepository;
        }

        /// <summary>
        /// Appelé quand un client se connecte
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, "all_users");

                _logger.LogInformation(
                    "User {UserName} (ID: {UserId}) connected to NotificationHub. ConnectionId: {ConnectionId}",
                    userName, userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Appelé quand un client se déconnecte
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation(
                    "User {UserName} (ID: {UserId}) disconnected from NotificationHub. ConnectionId: {ConnectionId}",
                    userName, userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Permet à un utilisateur de rejoindre un groupe spécifique
        /// </summary>
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {User} joined group: {Group}", Context.User?.Identity?.Name, groupName);
        }

        /// <summary>
        /// Permet à un utilisateur de quitter un groupe spécifique
        /// </summary>
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {User} left group: {Group}", Context.User?.Identity?.Name, groupName);
        }

        /// <summary>
        /// Marquer une notification comme lue (persiste EstLue / DateLecture en base).
        /// Réservé au destinataire de la notification.
        /// </summary>
        public async Task MarkNotificationAsRead(int notificationId)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId) || userId <= 0)
            {
                _logger.LogWarning("MarkNotificationAsRead: utilisateur non authentifié");
                await Clients.Caller.SendAsync("NotificationMarkFailed", new
                {
                    notificationId,
                    reason = "unauthorized"
                });
                return;
            }

            var notification = await _notificationRepository.GetByIdAsync(notificationId);
            if (notification == null)
            {
                _logger.LogWarning(
                    "MarkNotificationAsRead: notification {NotificationId} introuvable (user {UserId})",
                    notificationId, userId);
                await Clients.Caller.SendAsync("NotificationMarkFailed", new
                {
                    notificationId,
                    reason = "not_found"
                });
                return;
            }

            if (!notification.IdDestinataire.HasValue || notification.IdDestinataire.Value != userId)
            {
                _logger.LogWarning(
                    "MarkNotificationAsRead: accès refusé notification {NotificationId} pour user {UserId}",
                    notificationId, userId);
                await Clients.Caller.SendAsync("NotificationMarkFailed", new
                {
                    notificationId,
                    reason = "forbidden"
                });
                return;
            }

            if (notification.EstLue)
            {
                await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
                return;
            }

            var success = await _notificationRepository.MarquerCommeLueAsync(notificationId);
            if (!success)
            {
                await Clients.Caller.SendAsync("NotificationMarkFailed", new
                {
                    notificationId,
                    reason = "not_found"
                });
                return;
            }

            _logger.LogInformation(
                "User {UserId} marked notification {NotificationId} as read (persisted)",
                userId, notificationId);
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }

        /// <summary>
        /// Obtenir le statut de connexion
        /// </summary>
        public async Task GetConnectionStatus()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            await Clients.Caller.SendAsync("ConnectionStatus", new
            {
                IsConnected = true,
                UserId = userId,
                UserName = userName,
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
