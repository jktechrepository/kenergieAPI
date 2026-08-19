using Kenergie.Models.DTOs.Devise;

namespace Kenergie.Services.Repositories
{
    public interface IRapportFinancierUsdEnrichmentService
    {
        Task<EquivalentUsdDto> BuildEquivalentUsdAsync(int idSociete, decimal montant, DateTime? date = null);

        Task<EquivalentUsdDto> SumEquivalentUsdAsync(
            IReadOnlyList<(int IdSociete, decimal Montant)> items,
            DateTime? date = null);

        Task<StatistiquesGeneralesSyntheseUsdDto> BuildStatistiquesGeneralesSyntheseUsdAsync(
            int idSociete,
            decimal totalArrieres,
            decimal totalPaiements,
            DateTime? date = null);

        Task<StatistiquesFinancieresSyntheseUsdDto> BuildStatistiquesFinancieresSyntheseUsdAsync(
            int idSociete,
            decimal chiffreAffaires,
            decimal montantArrieres,
            decimal montantPaye,
            decimal montantDu,
            DateTime? date = null);

        Task<GlobalFinancierSyntheseUsdDto> BuildGlobalFinancierSyntheseUsdAsync(
            IReadOnlyList<(int IdSociete, decimal ChiffreAffaires, decimal MontantEncaisse, decimal MontantArrieres, decimal TotalGeneralArriere, decimal ChiffreAffairesJournalier)> items,
            IReadOnlyList<(int IdSociete, decimal Montant)>? depensesMoisItems = null,
            IReadOnlyList<(int IdSociete, decimal Montant)>? facturesMoisPrecedentItems = null,
            DateTime? date = null);

        Task<GlobalStatistiquesSyntheseUsdDto> BuildGlobalStatistiquesSyntheseUsdAsync(
            IReadOnlyList<(int IdSociete, decimal ChiffreAffaires, decimal MontantArrieres, decimal MontantPaiements)> items,
            DateTime? date = null);

        Task<SocieteStatistiquesSyntheseUsdDto> BuildSocieteStatistiquesSyntheseUsdAsync(
            int idSociete,
            decimal chiffreAffairesMois,
            decimal montantTotalArrieres,
            decimal montantDepensesMois,
            decimal montantTotalFacturesMoisPrecedent,
            DateTime? date = null);

        Task<DashboardSyntheseUsdDto> BuildDashboardSyntheseUsdAsync(
            int idSociete,
            decimal paiementsDuMois,
            decimal totalGeneralArriere,
            decimal montantTotalFacturesMoisPrecedent,
            decimal montantTotalDepensesMois,
            DateTime? date = null);
    }
}
