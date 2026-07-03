using Kenergie.Models.DTOs.Sync;

namespace Kenergie.Services
{
    /// <summary>
    /// Interface pour le service de synchronisation offline
    /// Gère la synchronisation des clients, arriérés, suppressions et paiements
    /// </summary>
    public interface ISyncService
    {
        /// <summary>
        /// Fournit les informations initiales pour démarrer la synchronisation
        /// </summary>
        /// <param name="societeId">ID de la société du JWT</param>
        /// <returns>Informations de bootstrap avec snapshot et watermark</returns>
        Task<SyncBootstrapDto> GetBootstrapAsync(int societeId);

        /// <summary>
        /// Récupère les clients avec pagination cursor et delta sync
        /// </summary>
        /// <param name="societeId">ID de la société du JWT</param>
        /// <param name="request">Paramètres de pagination et filtres</param>
        /// <returns>Page de clients synchronisés</returns>
        Task<SyncPageDto<ClientSyncDto>> GetClientsAsync(int societeId, SyncRequestDto request);

        /// <summary>
        /// Récupère les arriérés avec pagination cursor et delta sync
        /// </summary>
        /// <param name="societeId">ID de la société du JWT</param>
        /// <param name="request">Paramètres de pagination et filtres</param>
        /// <returns>Page d'arriérés synchronisés</returns>
        Task<SyncPageDto<ArrearSyncDto>> GetArrearsAsync(int societeId, SyncArrearsRequestDto request);

        /// <summary>
        /// Récupère les suppressions depuis la dernière synchronisation
        /// </summary>
        /// <param name="societeId">ID de la société du JWT</param>
        /// <param name="request">Paramètres de suppression</param>
        /// <returns>Liste des IDs supprimés</returns>
        Task<SyncDeletionsDto> GetDeletionsAsync(int societeId, SyncDeletionsRequestDto request);

        /// <summary>
        /// Traite un batch de paiements offline avec idempotence
        /// </summary>
        /// <param name="societeId">ID de la société du JWT</param>
        /// <param name="userId">ID de l'utilisateur connecté</param>
        /// <param name="request">Batch de paiements à traiter</param>
        /// <returns>Résultats détaillés du traitement</returns>
        Task<PaymentBatchResultDto> ProcessPaymentsBatchAsync(int societeId, int userId, PaymentBatchRequestDto request);
    }
}
