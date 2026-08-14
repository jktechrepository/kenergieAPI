using System.Text.Json.Serialization;
using Kenergie.Models.DTOs.Devise;

namespace Kenergie.Models.DTOs
{
    public class FinancierDashboardDto
    {
        public GlobalFinancierStatistiquesDto GlobalStatistiques { get; set; } = new();
        public List<SocieteFinancierSummaryDto> SocietesFinancieres { get; set; } = new();
        public List<TransactionRecenteDto> TransactionsRecentes { get; set; } = new();
        public List<AlerteFinanciereDto> AlertesFinancieres { get; set; } = new();
        public List<TopAgentCollecteurDto> Top10AgentsCollecteurs { get; set; } = new();
        public TendancesFinancieresDto Tendances { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
        public string? CodeDevisePrincipale { get; set; }
    }

    public class GlobalFinancierStatistiquesDto
    {
        public decimal ChiffreAffairesTotal { get; set; }
        public decimal MontantTotalEncaisse { get; set; }
        public decimal MontantTotalArrieres { get; set; }
        public decimal TotalGeneralArriere { get; set; }
        public decimal MontantMoisPrecedent { get; set; }
        public decimal TauxRecouvrementGlobal { get; set; }
        public int NombreTotalTransactions { get; set; }
        public decimal MoyenneTransaction { get; set; }
        public int NombreFactures { get; set; }
        public decimal ChiffreAffairesJournalier { get; set; }

        /// <summary>
        /// Total des dépenses validées du mois en cours (devise principale).
        /// </summary>
        public decimal MontantTotalDepensesMois { get; set; }

        /// <summary>
        /// Total des dépenses validées du jour (devise principale).
        /// </summary>
        public decimal MontantTotalDepensesJournalier { get; set; }

        /// <summary>
        /// Encaissements du mois moins dépenses du mois (devise principale).
        /// </summary>
        public decimal ResultatNetMois { get; set; }

        /// <summary>
        /// Nombre de dépenses soumises en attente de validation.
        /// </summary>
        public int NombreDepensesEnAttente { get; set; }

        /// <summary>
        /// Équivalents USD indicatifs des montants synthèse (taux du jour).
        /// </summary>
        public GlobalFinancierSyntheseUsdDto? SyntheseUsd { get; set; }
    }

    public class SocieteFinancierSummaryDto
    {
        public int IdSociete { get; set; }
        public string NomSociete { get; set; } = string.Empty;
        public decimal ChiffreAffaires { get; set; }
        public decimal MontantEncaisse { get; set; }
        public decimal MontantArrieres { get; set; }
        public decimal TauxRecouvrement { get; set; }
        public int NombreTransactions { get; set; }
        public string StatutFinancier { get; set; } = string.Empty;
    }

    public class TransactionRecenteDto
    {
        public int IdTransaction { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string NomSociete { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public DateTime DateTransaction { get; set; }
        public string TypeTransaction { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
    }

    public class AlerteFinanciereDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdSociete { get; set; }
        public string NomSociete { get; set; } = string.Empty;
        public decimal MontantConcerne { get; set; }
        public bool EstLue { get; set; }
    }

    public class TendancesFinancieresDto
    {
        public List<TendanceMensuelleDto> ChiffreAffaires { get; set; } = new();
        public List<TendanceMensuelleDto> Encaissements { get; set; } = new();
        public List<TendanceMensuelleDto> TauxRecouvrement { get; set; } = new();
    }
}
