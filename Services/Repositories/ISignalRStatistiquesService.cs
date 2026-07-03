using Kenergie.Models.DTOs.Statistiques;

namespace Kenergie.Services.Repositories
{
    /// <summary>
    /// Interface du service SignalR pour les statistiques en temps réel
    /// </summary>
    public interface ISignalRStatistiquesService
    {
        /// <summary>
        /// Notifier une mise à jour des statistiques générales
        /// </summary>
        Task NotifyStatistiquesGeneralesUpdatedAsync(int societeId, StatistiquesGeneralesDto statistiquesData);

        /// <summary>
        /// Notifier une mise à jour des statistiques financières
        /// </summary>
        Task NotifyStatistiquesFinancieresUpdatedAsync(int societeId, StatistiquesFinancieresDto statistiquesData);

        /// <summary>
        /// Notifier une mise à jour des statistiques opérationnelles
        /// </summary>
        Task NotifyStatistiquesOperationnellesUpdatedAsync(int societeId, StatistiquesOperationnellesDto statistiquesData);

        /// <summary>
        /// Notifier une mise à jour des statistiques de performance
        /// </summary>
        Task NotifyStatistiquesPerformanceUpdatedAsync(int societeId, StatistiquesPerformanceDto statistiquesData);

        /// <summary>
        /// Notifier une mise à jour des statistiques consolidées
        /// </summary>
        Task NotifyStatistiquesConsolideesUpdatedAsync(int societeId, StatistiquesConsolideesDto statistiquesData);

        /// <summary>
        /// Notifier une mise à jour de l'évolution mensuelle
        /// </summary>
        Task NotifyEvolutionMensuelleUpdatedAsync(int societeId, object evolutionData);

        /// <summary>
        /// Notifier une mise à jour de la répartition des paiements
        /// </summary>
        Task NotifyRepartitionPaiementsUpdatedAsync(int societeId, object repartitionData);

        /// <summary>
        /// Notifier une mise à jour de la répartition des clients par catégorie
        /// </summary>
        Task NotifyRepartitionClientsParCategorieUpdatedAsync(int societeId, object repartitionData);

        /// <summary>
        /// Notifier une mise à jour de la répartition des clients par axe
        /// </summary>
        Task NotifyRepartitionClientsParAxeUpdatedAsync(int societeId, object repartitionData);

        /// <summary>
        /// Notifier une mise à jour des statistiques de factures du mois
        /// </summary>
        Task NotifyStatistiquesFacturesMoisUpdatedAsync(int societeId, object facturesMoisData);

        /// <summary>
        /// Notifier une mise à jour de l'activité des clients
        /// </summary>
        Task NotifyClientActiviteUpdatedAsync(int societeId, object clientActiviteData);

        /// <summary>
        /// Notifier une mise à jour du taux de recouvrement global
        /// </summary>
        Task NotifyTauxRecouvrementGlobalUpdatedAsync(int societeId, decimal tauxRecouvrement);

        /// <summary>
        /// Notifier une mise à jour du taux de recouvrement par catégorie
        /// </summary>
        Task NotifyTauxRecouvrementParCategorieUpdatedAsync(int societeId, object tauxRecouvrementData);

        /// <summary>
        /// Notifier une mise à jour des top agents
        /// </summary>
        Task NotifyTopAgentsUpdatedAsync(int societeId, object topAgentsData);

        /// <summary>
        /// Notifier une mise à jour de la performance mensuelle
        /// </summary>
        Task NotifyPerformanceMensuelleUpdatedAsync(int societeId, object performanceData);

        /// <summary>
        /// Envoyer une notification personnalisée de statistiques
        /// </summary>
        Task SendStatistiquesNotificationAsync(int societeId, string title, string message, string type = "info");

        /// <summary>
        /// Notifier un changement de statut des statistiques
        /// </summary>
        Task NotifyStatistiquesStatusChangeAsync(int societeId, string entityType, int entityId, string newStatus);

        /// <summary>
        /// Notifier un rafraîchissement manuel des statistiques demandé
        /// </summary>
        Task NotifyStatistiquesRefreshRequestedAsync(int societeId, string requestedBy);

        /// <summary>
        /// Envoyer un message de test de connexion pour les statistiques
        /// </summary>
        Task SendStatistiquesConnectionTestAsync(int societeId, string message);
    }
}
