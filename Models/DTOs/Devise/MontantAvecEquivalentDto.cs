namespace Kenergie.Models.DTOs.Devise
{
    /// <summary>
    /// Équivalent USD d'un montant consolidé en devise principale.
    /// </summary>
    public class EquivalentUsdDto
    {
        public decimal? MontantEquivalentUsd { get; set; }
        public decimal? TauxVersUsd { get; set; }
        public DateTime? DateTaux { get; set; }
        public bool ConversionUsdDisponible { get; set; }
    }

    /// <summary>
    /// Montant en devise principale avec son équivalent USD indicatif.
    /// </summary>
    public class MontantAvecEquivalentDto
    {
        public decimal Montant { get; set; }
        public string? CodeDevise { get; set; }
        public decimal? MontantEquivalentUsd { get; set; }
        public decimal? TauxVersUsd { get; set; }
        public DateTime? DateTaux { get; set; }
        public bool ConversionUsdDisponible { get; set; }
    }

    public class StatistiquesGeneralesSyntheseUsdDto
    {
        public EquivalentUsdDto? TotalArrieres { get; set; }
        public EquivalentUsdDto? TotalPaiements { get; set; }
    }

    public class StatistiquesFinancieresSyntheseUsdDto
    {
        public EquivalentUsdDto? ChiffreAffaires { get; set; }
        public EquivalentUsdDto? MontantArrieres { get; set; }
        public EquivalentUsdDto? MontantPaye { get; set; }
        public EquivalentUsdDto? MontantDu { get; set; }
    }

    public class GlobalFinancierSyntheseUsdDto
    {
        public EquivalentUsdDto? ChiffreAffairesTotal { get; set; }
        public EquivalentUsdDto? MontantTotalEncaisse { get; set; }
        public EquivalentUsdDto? MontantTotalArrieres { get; set; }
        public EquivalentUsdDto? TotalGeneralArriere { get; set; }
        public EquivalentUsdDto? ChiffreAffairesJournalier { get; set; }
        public EquivalentUsdDto? MontantTotalDepensesMois { get; set; }
        public EquivalentUsdDto? MontantTotalFacturesMoisPrecedent { get; set; }
    }

    public class GlobalStatistiquesSyntheseUsdDto
    {
        public EquivalentUsdDto? ChiffreAffairesGlobal { get; set; }
        public EquivalentUsdDto? MontantTotalArrieresGlobal { get; set; }
        public EquivalentUsdDto? MontantTotalPaiementsGlobal { get; set; }
    }

    public class SocieteStatistiquesSyntheseUsdDto
    {
        public EquivalentUsdDto? ChiffreAffairesMois { get; set; }
        public EquivalentUsdDto? MontantTotalArrieres { get; set; }
        public EquivalentUsdDto? MontantDepensesMois { get; set; }
        public EquivalentUsdDto? MontantTotalFacturesMoisPrecedent { get; set; }
    }

    public class DashboardSyntheseUsdDto
    {
        public EquivalentUsdDto? PaiementsDuMois { get; set; }
        public EquivalentUsdDto? TotalGeneralArriere { get; set; }
        public EquivalentUsdDto? MontantTotalFacturesMoisPrecedent { get; set; }
        public EquivalentUsdDto? MontantTotalDepensesMois { get; set; }
    }
}
