namespace Kenergie.Services
{
    /// <summary>
    /// Périmètres clients d'une société pour KPI financiers vs effectifs actifs.
    /// </summary>
    public interface ISocieteClientScopeService
    {
        /// <summary>
        /// Clients rattachés à la société pour KPI financiers (IsActif/Statut ignorés, liaisons ClientUsage actives ou non, hors soft-delete).
        /// </summary>
        Task<List<int>> GetFinancialClientIdsAsync(int idSociete);

        /// <summary>
        /// Clients actifs opérationnels (IsActif, Statut, liaisons actives, hors soft-delete).
        /// </summary>
        Task<List<int>> GetActiveClientIdsAsync(int idSociete);
    }
}
