using Kenergie.Hubs;
using Kenergie.Services.Repositories;
using Kenergie.Models.DTOs.Statistiques;
using Microsoft.AspNetCore.SignalR;

namespace Kenergie.Services
{
    /// <summary>
    /// Service SignalR pour les statistiques en temps réel
    /// </summary>
    public class SignalRStatistiquesService : ISignalRStatistiquesService
    {
        private readonly IHubContext<DashboardHub> _hubContext;
        private readonly ILogger<SignalRStatistiquesService> _logger;

        public SignalRStatistiquesService(
            IHubContext<DashboardHub> hubContext,
            ILogger<SignalRStatistiquesService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Notifier une mise à jour des statistiques générales
        /// </summary>
        public async Task NotifyStatistiquesGeneralesUpdatedAsync(int societeId, StatistiquesGeneralesDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesGeneralesUpdated", new
                    {
                        societeId = societeId,
                        statistiques = statistiquesData,
                        timestamp = DateTime.UtcNow,
                        type = "generales_update"
                    });

                _logger.LogInformation($"📊 Statistiques générales update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques générales update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour des statistiques financières
        /// </summary>
        public async Task NotifyStatistiquesFinancieresUpdatedAsync(int societeId, StatistiquesFinancieresDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesFinancieresUpdated", new
                    {
                        societeId = societeId,
                        statistiques = statistiquesData,
                        timestamp = DateTime.UtcNow,
                        type = "financieres_update"
                    });

                _logger.LogInformation($"💰 Statistiques financières update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques financières update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour des statistiques opérationnelles
        /// </summary>
        public async Task NotifyStatistiquesOperationnellesUpdatedAsync(int societeId, StatistiquesOperationnellesDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesOperationnellesUpdated", new
                    {
                        societeId = societeId,
                        statistiques = statistiquesData,
                        timestamp = DateTime.UtcNow,
                        type = "operationnelles_update"
                    });

                _logger.LogInformation($"🏢 Statistiques opérationnelles update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques opérationnelles update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour des statistiques de performance
        /// </summary>
        public async Task NotifyStatistiquesPerformanceUpdatedAsync(int societeId, StatistiquesPerformanceDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesPerformanceUpdated", new
                    {
                        societeId = societeId,
                        statistiques = statistiquesData,
                        timestamp = DateTime.UtcNow,
                        type = "performance_update"
                    });

                _logger.LogInformation($"⚡ Statistiques performance update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques performance update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour des statistiques consolidées
        /// </summary>
        public async Task NotifyStatistiquesConsolideesUpdatedAsync(int societeId, StatistiquesConsolideesDto statistiquesData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesConsolideesUpdated", new
                    {
                        societeId = societeId,
                        statistiques = statistiquesData,
                        timestamp = DateTime.UtcNow,
                        type = "consolidees_update"
                    });

                _logger.LogInformation($"📈 Statistiques consolidées update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques consolidées update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour de l'évolution mensuelle
        /// </summary>
        public async Task NotifyEvolutionMensuelleUpdatedAsync(int societeId, object evolutionData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("EvolutionMensuelleUpdated", new
                    {
                        societeId = societeId,
                        evolution = evolutionData,
                        timestamp = DateTime.UtcNow,
                        type = "evolution_mensuelle_update"
                    });

                _logger.LogInformation($"📈 Evolution mensuelle update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending evolution mensuelle update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour de la répartition des paiements
        /// </summary>
        public async Task NotifyRepartitionPaiementsUpdatedAsync(int societeId, object repartitionData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("RepartitionPaiementsUpdated", new
                    {
                        societeId = societeId,
                        repartition = repartitionData,
                        timestamp = DateTime.UtcNow,
                        type = "repartition_paiements_update"
                    });

                _logger.LogInformation($"💳 Repartition paiements update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending repartition paiements update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour de la répartition des clients par catégorie
        /// </summary>
        public async Task NotifyRepartitionClientsParCategorieUpdatedAsync(int societeId, object repartitionData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("RepartitionClientsParCategorieUpdated", new
                    {
                        societeId = societeId,
                        repartition = repartitionData,
                        timestamp = DateTime.UtcNow,
                        type = "repartition_clients_categorie_update"
                    });

                _logger.LogInformation($"👥 Repartition clients par catégorie update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending repartition clients par catégorie update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour de la répartition des clients par axe
        /// </summary>
        public async Task NotifyRepartitionClientsParAxeUpdatedAsync(int societeId, object repartitionData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("RepartitionClientsParAxeUpdated", new
                    {
                        societeId = societeId,
                        repartition = repartitionData,
                        timestamp = DateTime.UtcNow,
                        type = "repartition_clients_axe_update"
                    });

                _logger.LogInformation($"📍 Repartition clients par axe update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending repartition clients par axe update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour des statistiques de factures du mois
        /// </summary>
        public async Task NotifyStatistiquesFacturesMoisUpdatedAsync(int societeId, object facturesMoisData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesFacturesMoisUpdated", new
                    {
                        societeId = societeId,
                        facturesMois = facturesMoisData,
                        timestamp = DateTime.UtcNow,
                        type = "factures_mois_update"
                    });

                _logger.LogInformation($"📄 Statistiques factures du mois update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques factures du mois update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour de l'activité des clients
        /// </summary>
        public async Task NotifyClientActiviteUpdatedAsync(int societeId, object clientActiviteData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("ClientActiviteUpdated", new
                    {
                        societeId = societeId,
                        clientActivite = clientActiviteData,
                        timestamp = DateTime.UtcNow,
                        type = "client_activite_update"
                    });

                _logger.LogInformation($"🔄 Client activité update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending client activité update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour du taux de recouvrement global
        /// </summary>
        public async Task NotifyTauxRecouvrementGlobalUpdatedAsync(int societeId, decimal tauxRecouvrement)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("TauxRecouvrementGlobalUpdated", new
                    {
                        societeId = societeId,
                        tauxRecouvrement = tauxRecouvrement,
                        timestamp = DateTime.UtcNow,
                        type = "taux_recouvrement_global_update"
                    });

                _logger.LogInformation($"📊 Taux recouvrement global update sent to society {societeId}: {tauxRecouvrement}%");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending taux recouvrement global update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour du taux de recouvrement par catégorie
        /// </summary>
        public async Task NotifyTauxRecouvrementParCategorieUpdatedAsync(int societeId, object tauxRecouvrementData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("TauxRecouvrementParCategorieUpdated", new
                    {
                        societeId = societeId,
                        tauxRecouvrement = tauxRecouvrementData,
                        timestamp = DateTime.UtcNow,
                        type = "taux_recouvrement_categorie_update"
                    });

                _logger.LogInformation($"📊 Taux recouvrement par catégorie update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending taux recouvrement par catégorie update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour des top agents
        /// </summary>
        public async Task NotifyTopAgentsUpdatedAsync(int societeId, object topAgentsData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("TopAgentsUpdated", new
                    {
                        societeId = societeId,
                        topAgents = topAgentsData,
                        timestamp = DateTime.UtcNow,
                        type = "top_agents_update"
                    });

                _logger.LogInformation($"🏆 Top agents update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending top agents update to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier une mise à jour de la performance mensuelle
        /// </summary>
        public async Task NotifyPerformanceMensuelleUpdatedAsync(int societeId, object performanceData)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("PerformanceMensuelleUpdated", new
                    {
                        societeId = societeId,
                        performance = performanceData,
                        timestamp = DateTime.UtcNow,
                        type = "performance_mensuelle_update"
                    });

                _logger.LogInformation($"📈 Performance mensuelle update sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending performance mensuelle update to society {societeId}");
            }
        }

        /// <summary>
        /// Envoyer une notification personnalisée de statistiques
        /// </summary>
        public async Task SendStatistiquesNotificationAsync(int societeId, string title, string message, string type = "info")
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesNotification", new
                    {
                        societeId = societeId,
                        title = title,
                        message = message,
                        type = type,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation($"📢 Statistiques notification sent to society {societeId}: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques notification to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier un changement de statut des statistiques
        /// </summary>
        public async Task NotifyStatistiquesStatusChangeAsync(int societeId, string entityType, int entityId, string newStatus)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesStatusChanged", new
                    {
                        societeId = societeId,
                        entityType = entityType,
                        entityId = entityId,
                        newStatus = newStatus,
                        timestamp = DateTime.UtcNow
                    });

                _logger.LogInformation($"🔄 Statistiques status change sent to society {societeId}: {entityType} {entityId} -> {newStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques status change to society {societeId}");
            }
        }

        /// <summary>
        /// Notifier un rafraîchissement manuel des statistiques demandé
        /// </summary>
        public async Task NotifyStatistiquesRefreshRequestedAsync(int societeId, string requestedBy)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesRefreshRequested", new
                    {
                        societeId = societeId,
                        requestedBy = requestedBy,
                        timestamp = DateTime.UtcNow,
                        type = "manual_refresh"
                    });

                _logger.LogInformation($"🔄 Statistiques refresh requested for society {societeId} by {requestedBy}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques refresh request to society {societeId}");
            }
        }

        /// <summary>
        /// Envoyer un message de test de connexion pour les statistiques
        /// </summary>
        public async Task SendStatistiquesConnectionTestAsync(int societeId, string message)
        {
            try
            {
                await _hubContext.Clients
                    .Group($"statistiques_updates_{societeId}")
                    .SendAsync("StatistiquesConnectionTest", new
                    {
                        societeId = societeId,
                        message = message,
                        timestamp = DateTime.UtcNow,
                        type = "connection_test"
                    });

                _logger.LogInformation($"🔌 Statistiques connection test sent to society {societeId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending statistiques connection test to society {societeId}");
            }
        }
    }
}
