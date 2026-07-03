using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs
{
    public class ClientDashboardDto
    {
        public ClientStatistiquesDto Statistiques { get; set; } = new();
        public List<FactureRecenteDto> FacturesRecentes { get; set; } = new();
        public List<PaiementClientRecentDto> PaiementsRecents { get; set; } = new();
        public List<ConsommationDto> Consommations { get; set; } = new();
        public List<AlerteClientDto> AlertesClient { get; set; } = new();
        public ResumeClientDto ResumeClient { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class ClientStatistiquesDto
    {
        public decimal MontantTotalFactures { get; set; }
        public decimal MontantTotalPaye { get; set; }
        public decimal MontantTotalDu { get; set; }
        public int NombreFactures { get; set; }
        public int NombreFacturesPayees { get; set; }
        public int NombreFacturesEnRetard { get; set; }
        public decimal TauxRecouvrement { get; set; }
        public decimal ConsommationTotale { get; set; }
        public decimal ConsommationMoyenneMensuelle { get; set; }
    }

    public class FactureRecenteDto
    {
        public int IdFacture { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string MoisAnnee { get; set; } = string.Empty;
        public decimal MontantTotal { get; set; }
        public decimal MontantPaye { get; set; }
        public decimal MontantDu { get; set; }
        public DateTime DateEmission { get; set; }
        public DateTime DateEcheance { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int JoursRetard { get; set; }
    }

    public class PaiementClientRecentDto
    {
        public int IdPaiement { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal MontantPaye { get; set; }
        public DateTime DatePaiement { get; set; }
        public string MethodePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string ReferenceFacture { get; set; } = string.Empty;
    }

    public class ConsommationDto
    {
        public int IdConsommation { get; set; }
        public string Reference { get; set; } = string.Empty;
        public decimal Consommation { get; set; }
        public string Unite { get; set; } = string.Empty;
        public DateTime DateConsommation { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal MontantTotal { get; set; }
        public string TypeConsommation { get; set; } = string.Empty;
    }

    public class AlerteClientDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdFacture { get; set; }
        public string ReferenceFacture { get; set; } = string.Empty;
        public decimal MontantConcerne { get; set; }
        public bool EstLue { get; set; }
    }

    public class ResumeClientDto
    {
        public decimal SoldeActuel { get; set; }
        public decimal LimiteCredit { get; set; }
        public decimal CreditDisponible { get; set; }
        public DateTime DerniereConnexion { get; set; }
        public string StatutCompte { get; set; } = string.Empty;
        public int NombreServicesActifs { get; set; }
        public DateTime ProchaineFacture { get; set; }
    }
}
