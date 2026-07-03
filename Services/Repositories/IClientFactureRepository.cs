using Kenergie.Models;
using Kenergie.Models.DTOs.ClientFacture;
using Kenergie.Models.DTOs.Pagination;

namespace Kenergie.Services.Repositories
{
    /// <summary>
    /// Interface pour le repository ClientFacture
    /// </summary>
    public interface IClientFactureRepository
    {
        // CRUD de base
        Task<ClientFacture?> GetByIdAsync(int idClientFacture);
        Task<ClientFacture> CreateAsync(ClientFacture clientFacture);
        Task<ClientFacture?> UpdateAsync(ClientFacture clientFacture);
        Task<bool> DeleteAsync(int idClientFacture);
        Task<bool> ExistsAsync(int idClientFacture);

        // Requêtes par Client
        Task<IEnumerable<ClientFacture>> GetByClientAsync(int idClient);
        Task<IEnumerable<ClientFacture>> GetByClientWithArrieresAsync(int idClient); // MontantDu > 0
        Task<IEnumerable<ClientFacture>> GetPreExistantsByClientAsync(int idClient); // EstArrierePreExistant = true
        
        // Requêtes globales (tous les clients)
        Task<IEnumerable<ClientFacture>> GetAllArrieresAsync(); // Tous les arriérés (MontantDu > 0) sans filtre client

        // Requêtes par Facture
        Task<IEnumerable<ClientFacture>> GetByFactureAsync(int idFacture);

        // Requêtes combinées
        Task<ClientFacture?> GetByClientAndFactureAsync(int idClient, int idFacture);
        Task<IEnumerable<ClientFacture>> GetByClientAndMoisAnneeAsync(int idClient, string mois, int annee);

        // Requêtes par société, année et mois
        Task<IEnumerable<ClientFacture>> GetBySocieteAnneeMoisWithArrieresAsync(int idSociete, int annees, string mois);

        // ✨ NOUVEAU : Statistiques consolidées par société, année et mois
        /// <summary>
        /// Récupère les factures avec arriérés d'une société pour une période donnée avec statistiques consolidées
        /// </summary>
        Task<ClientFactureConsolideDto> GetBySocieteAnneeMoisWithStatsAsync(int idSociete, int annees, string mois);

        // Création d'arriéré pré-existant
        Task<ClientFacture> CreatePreExistantAsync(int idClient, decimal montant, string mois, int annees, string? description = null, DateTime? dateEmission = null);

        // Mise à jour des montants
        Task<bool> UpdateMontantPayeAsync(int idClientFacture, decimal montantPaye);
        Task<bool> RecalculateMontantDuAsync(int idClientFacture);

        // ✨ NOUVEAU : Vue consolidée
        /// <summary>
        /// Récupère les factures d'un client groupées par période (mois/année) avec totaux consolidés
        /// </summary>
        Task<ClientFacturesConsolideesResponseDto> GetClientFacturesConsolideesAsync(int idClient);

        /// <summary>
        /// Récupère la facture consolidée d'un client pour une période spécifique
        /// </summary>
        Task<ClientFactureConsolideeDto?> GetClientFactureConsolideeByPeriodeAsync(int idClient, string mois, int annee);

        /// <summary>
        /// ✨ NOUVEAU : Récupère les arriérés d'un client groupés par période (mois/année) avec totaux consolidés
        /// Seules les factures avec MontantDu > 0 sont incluses
        /// </summary>
        Task<ArrieresConsolidesResponseDto> GetArrieresConsolidesByClientAsync(int idClient);

        /// <summary>
        /// ✨ NOUVEAU : Récupère un rapport global des arriérés consolidés pour tous les clients
        /// Retourne les totaux globaux et la liste des arriérés par client groupés par période
        /// </summary>
        /// <param name="moisFacturePrecedentSeulement">Si true, filtre uniquement les clients facturés le mois précédent (défaut: true)</param>
        /// <param name="idAxe">Optionnel: filtre par axe spécifique</param>
        /// <param name="idTypeDeCourant">Optionnel: filtre par type de courant (ClientUsage actif)</param>
        /// <param name="mois">Optionnel: mois de la période de relance (ex. "04" ou "4"). Défaut: M-1 calendaire.</param>
        /// <param name="annee">Optionnel: année de la période de relance. Défaut: année de M-1, ou année courante si mois seul.</param>
        Task<ArrieresConsolidesGlobauxResponseDto> GetArrieresConsolidesGlobauxAsync(
            bool moisFacturePrecedentSeulement = true, 
            int? idAxe = null,
            int? idTypeDeCourant = null,
            string? mois = null,
            int? annee = null);

        /// <summary>
        /// Récupère le rapport des client-factures agrégées par mois/année
        /// Correspond à la requête SQL de reporting des factures clients avec jointures multiples
        /// </summary>
        /// <param name="mois">Mois de facturation (optionnel, défaut: mois-1)</param>
        /// <param name="annees">Année de facturation (optionnel, défaut: année courante)</param>
        /// <param name="axe">Filtre optionnel par nom d'axe</param>
        /// <param name="usage">Filtre optionnel par libellé d'usage</param>
        /// <param name="limit">Nombre maximum de résultats (défaut: 200)</param>
        /// <returns>Liste des factures clients agrégées avec informations client, axe et usage</returns>
        Task<IEnumerable<ClientFactureReportDto>> GetClientFacturesReportAsync(
            string? mois = null, 
            int? annees = null, 
            string? axe = null,
            string? usage = null,
            int limit = 200);

        /// <summary>
        /// Récupère le rapport des client-factures agrégées par mois/année avec pagination
        /// Correspond à la requête SQL de reporting des factures clients avec jointures multiples
        /// </summary>
        /// <param name="request">Paramètres de pagination</param>
        /// <param name="mois">Mois de facturation (optionnel, défaut: mois-1)</param>
        /// <param name="annees">Année de facturation (optionnel, défaut: année courante)</param>
        /// <param name="axe">Filtre optionnel par nom d'axe</param>
        /// <param name="usage">Filtre optionnel par libellé d'usage</param>
        /// <returns>Résultat paginé des factures clients agrégées avec informations client, axe et usage</returns>
        Task<PagedResult<ClientFactureReportDto>> GetClientFacturesReportPagedAsync(
            PagedRequest request,
            string? mois = null, 
            int? annees = null, 
            string? axe = null,
            string? usage = null);
    }
}
