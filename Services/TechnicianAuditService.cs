using Kenergie.Data;
using Kenergie.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kenergie.Services
{
    /// <summary>
    /// Service d'audit spécialisé pour les actions des techniciens
    /// Fournit un monitoring renforcé et des alertes pour les actions sensibles
    /// </summary>
    public class TechnicianAuditService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<TechnicianAuditService> _logger;
        private readonly INotificationService _notificationService;

        // Actions sensibles qui nécessitent une alerte immédiate
        private readonly HashSet<string> _sensitiveActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "DELETE", "UPDATE", "CREATE"
        };

        // Entités sensibles pour les techniciens
        private readonly HashSet<string> _sensitiveEntities = new(StringComparer.OrdinalIgnoreCase)
        {
            "CLIENT", "FACTURE", "AGENT", "SOCIETE", "UTILISATEUR"
        };

        public TechnicianAuditService(
            KenergieDbContext context,
            ILogger<TechnicianAuditService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Enregistre une action de technicien avec audit détaillé
        /// </summary>
        public async Task LogTechnicianActionAsync(int userId, string userName, string action, 
            string entityType, int? entityId, object data, int? societeId = null)
        {
            try
            {
                var isSensitive = IsSensitiveAction(action, entityType);
                var auditLevel = isSensitive ? "HIGH" : "NORMAL";

                // Créer l'entrée d'audit
                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserName = userName,
                    UserRole = "Technicien",
                    Action = action,
                    TableName = entityType,
                    RecordId = entityId ?? 0,
                    OldValues = null, // Pas de valeurs anciennes pour les actions de technicien
                    NewValues = $"TechnicianAction_{auditLevel}: {System.Text.Json.JsonSerializer.Serialize(data)}",
                    DateAction = DateTime.UtcNow,
                    IdSociete = societeId
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "🔧 AUDIT TECHNICIEN [{Level}]: User {UserId} ({UserName}) - {Action} {EntityType}{EntityId} - Société: {SocieteId}",
                    auditLevel, userId, userName, action, entityType, entityId.HasValue ? $" #{entityId}" : "", societeId);

                // Alertes pour les actions sensibles
                if (isSensitive)
                {
                    await AlertSensitiveAction(userId, userName, action, entityType, entityId, societeId);
                }

                // Monitoring des tendances
                await MonitorActionTrends(userId, action, entityType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'audit de l'action du technicien {UserId}", userId);
            }
        }

        /// <summary>
        /// Vérifie si une action est sensible et nécessite une attention particulière
        /// </summary>
        private bool IsSensitiveAction(string action, string entityType)
        {
            return (_sensitiveActions.Contains(action) && _sensitiveEntities.Contains(entityType)) ||
                   (action == "DELETE" && entityType != "PanneSignalement"); // Les suppressions sont toujours sensibles sauf pour les pannes
        }

        /// <summary>
        /// Envoie une alerte pour une action sensible
        /// </summary>
        private async Task AlertSensitiveAction(int userId, string userName, string action, 
            string entityType, int? entityId, int? societeId)
        {
            try
            {
                var alertMessage = $"🚡 ACTION SENSIBLE TECHNICIEN: {userName} (ID: {userId}) a effectué {action} sur {entityType}";
                
                if (entityId.HasValue)
                    alertMessage += $" #{entityId}";
                
                if (societeId.HasValue)
                    alertMessage += $" - Société: {societeId}";

                _logger.LogWarning(alertMessage);

                // Envoyer une notification aux administrateurs
                if (_notificationService != null)
                {
                    await _notificationService.SendAlertToAdminsAsync(alertMessage, "TECHNICIAN_SENSITIVE_ACTION");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'envoi d'alerte pour action sensible du technicien {UserId}", userId);
            }
        }

        /// <summary>
        /// Monitor les tendances d'actions pour détecter les comportements anormaux
        /// </summary>
        private async Task MonitorActionTrends(int userId, string action, string entityType)
        {
            try
            {
                // Compter les actions du technicien dans la dernière heure
                var oneHourAgo = DateTime.UtcNow.AddHours(-1);
                var recentActionsCount = await _context.AuditLogs
                    .CountAsync(a => a.UserId == userId && 
                                   a.DateAction >= oneHourAgo && 
                                   a.NewValues.StartsWith("TechnicianAction_"));

                // Seuil d'alerte : plus de 50 actions par heure
                if (recentActionsCount > 50)
                {
                    var alertMessage = $"📊 ACTIVITÉ ANORMALE: Le technicien {userId} a effectué {recentActionsCount} actions dans la dernière heure";
                    _logger.LogWarning(alertMessage);

                    if (_notificationService != null)
                    {
                        await _notificationService.SendAlertToAdminsAsync(alertMessage, "TECHNICIAN_HIGH_ACTIVITY");
                    }
                }

                // Monitoring des suppressions multiples
                if (action == "DELETE")
                {
                    var deletionsLastHour = await _context.AuditLogs
                        .CountAsync(a => a.UserId == userId && 
                                       a.Action == "DELETE" && 
                                       a.DateAction >= oneHourAgo);

                    if (deletionsLastHour > 5)
                    {
                        var alertMessage = $"🗑️ SUPPRESSIONS MULTIPLES: Le technicien {userId} a supprimé {deletionsLastHour} entités dans la dernière heure";
                        _logger.LogWarning(alertMessage);

                        if (_notificationService != null)
                        {
                            await _notificationService.SendAlertToAdminsAsync(alertMessage, "TECHNICIAN_MULTIPLE_DELETES");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du monitoring des tendances pour le technicien {UserId}", userId);
            }
        }

        /// <summary>
        /// Génère un rapport d'activité pour un technicien sur une période donnée
        /// </summary>
        public async Task<TechnicianActivityReport> GenerateActivityReportAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var auditLogs = await _context.AuditLogs
                    .Where(a => a.UserId == userId && 
                               a.DateAction >= startDate && 
                               a.DateAction <= endDate &&
                               a.NewValues.StartsWith("TechnicianAction_"))
                    .ToListAsync();

                var report = new TechnicianActivityReport
                {
                    UserId = userId,
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalActions = auditLogs.Count,
                    SensitiveActions = auditLogs.Count(a => a.NewValues.Contains("HIGH")),
                    ActionsByType = auditLogs
                        .GroupBy(a => a.Action)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    ActionsByEntity = auditLogs
                        .GroupBy(a => a.TableName)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    HourlyDistribution = auditLogs
                        .GroupBy(a => a.DateAction.Hour)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la génération du rapport d'activité pour le technicien {UserId}", userId);
                return new TechnicianActivityReport { UserId = userId };
            }
        }
    }

    /// <summary>
    /// Rapport d'activité d'un technicien
    /// </summary>
    public class TechnicianActivityReport
    {
        public int UserId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalActions { get; set; }
        public int SensitiveActions { get; set; }
        public Dictionary<string, int> ActionsByType { get; set; } = new();
        public Dictionary<string, int> ActionsByEntity { get; set; } = new();
        public Dictionary<int, int> HourlyDistribution { get; set; } = new();
    }

    /// <summary>
    /// Interface pour le service de notification (à implémenter selon votre système)
    /// </summary>
    public interface INotificationService
    {
        Task SendAlertToAdminsAsync(string message, string alertType);
    }
}
