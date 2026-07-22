using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs
{
    public class CaissierDashboardDto
    {
        public CaissierStatistiquesDto StatistiquesJournalieres { get; set; } = new();
        public List<PaiementEnCoursDto> PaiementsEnCours { get; set; } = new();
        public List<PaiementRecentDto> PaiementsRecents { get; set; } = new();
        public List<RecetteJournaliereDto> RecettesJournalieres { get; set; } = new();
        public List<AlerteCaissierDto> AlertesCaissier { get; set; } = new();
        public ResumeCaisseDto ResumeCaisse { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
        public string? CodeDevisePrincipale { get; set; }
    }

    public class CaissierStatistiquesDto
    {
        public decimal TotalRecettes { get; set; }
        public int NombreTransactions { get; set; }
        public decimal MoyenneTransaction { get; set; }
        public decimal PlusGrosMontant { get; set; }
        public decimal PlusPetitMontant { get; set; }
        public int NombreClients { get; set; }
        public decimal TotalArrieres { get; set; }
    }

    public class PaiementEnCoursDto
    {
        public int IdPaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public decimal MontantAPaye { get; set; }
        public decimal MontantVerse { get; set; }
        public decimal ResteAPayer { get; set; }
        public DateTime DatePaiement { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
    }

    public class PaiementRecentDto
    {
        public int IdPaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public decimal MontantPaye { get; set; }
        public DateTime DatePaiement { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string UtilisateurEnregistrement { get; set; } = string.Empty;
    }

    public class RecetteJournaliereDto
    {
        public DateTime Date { get; set; }
        public decimal MontantTotal { get; set; }
        public int NombreTransactions { get; set; }
        public decimal RecetteEspece { get; set; }
        public decimal RecetteMobileMoney { get; set; }
        public decimal RecetteVirement { get; set; }
        public decimal RecetteCarte { get; set; }
    }

    public class AlerteCaissierDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdClient { get; set; }
        public string NomClient { get; set; } = string.Empty;
        public decimal MontantConcerne { get; set; }
        public bool EstLue { get; set; }
    }

    public class ResumeCaisseDto
    {
        public decimal SoldeInitial { get; set; }
        public decimal TotalEntrees { get; set; }
        public decimal TotalSorties { get; set; }
        public decimal SoldeFinal { get; set; }
        public decimal Ecart { get; set; }
        public DateTime DateCloture { get; set; }
        public string StatutCaisse { get; set; } = string.Empty;
    }
}
