using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs
{
    public class TechnicienDashboardDto
    {
        public TechnicienStatistiquesDto Statistiques { get; set; } = new();
        public List<InterventionEnCoursDto> InterventionsEnCours { get; set; } = new();
        public List<InterventionRecenteDto> InterventionsRecentes { get; set; } = new();
        public List<PanneSignaleeDto> PannesSignalees { get; set; } = new();
        public List<AlerteTechnicienDto> AlertesTechnicien { get; set; } = new();
        public PerformanceTechnicienDto Performance { get; set; } = new();
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
    }

    public class TechnicienStatistiquesDto
    {
        public int TotalInterventions { get; set; }
        public int InterventionsAujourdhui { get; set; }
        public int InterventionsCetteSemaine { get; set; }
        public int InterventionsCeMois { get; set; }
        public decimal TauxResolution { get; set; }
        public int MoyenneInterventionsJour { get; set; }
        public int PannesActives { get; set; }
        public int ClientsIntervenus { get; set; }
    }

    public class InterventionEnCoursDto
    {
        public int IdIntervention { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string TypePanne { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priorite { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime? DateFinPrevue { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Societe { get; set; } = string.Empty;
        public string Localisation { get; set; } = string.Empty;
    }

    public class InterventionRecenteDto
    {
        public int IdIntervention { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string TypePanne { get; set; } = string.Empty;
        public DateTime DateIntervention { get; set; }
        public DateTime? DateFin { get; set; }
        public string Duree { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public string Technicien { get; set; } = string.Empty;
        public string Societe { get; set; } = string.Empty;
    }

    public class PanneSignaleeDto
    {
        public int IdPanne { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string NomClient { get; set; } = string.Empty;
        public string TypePanne { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priorite { get; set; } = string.Empty;
        public DateTime DateSignalement { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string Societe { get; set; } = string.Empty;
        public int TempsAttente { get; set; }
    }

    public class AlerteTechnicienDto
    {
        public int IdAlerte { get; set; }
        public string TypeAlerte { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NiveauCriticite { get; set; } = string.Empty;
        public DateTime DateAlerte { get; set; }
        public int IdClient { get; set; }
        public string NomClient { get; set; } = string.Empty;
        public int IdIntervention { get; set; }
        public string ReferenceIntervention { get; set; } = string.Empty;
        public bool EstLue { get; set; }
    }

    public class PerformanceTechnicienDto
    {
        public decimal TauxResolution { get; set; }
        public int InterventionsTerminees { get; set; }
        public int InterventionsEnRetard { get; set; }
        public decimal TempsMoyenIntervention { get; set; }
        public List<PerformanceParTypeDto> PerformanceParType { get; set; } = new();
        public List<PerformanceMensuelleDto> PerformanceMensuelle { get; set; } = new();
    }

    public class PerformanceParTypeDto
    {
        public string TypePanne { get; set; } = string.Empty;
        public int NombreInterventions { get; set; }
        public decimal TauxResolution { get; set; }
        public decimal TempsMoyenResolution { get; set; }
    }

    public class PerformanceMensuelleDto
    {
        public string Mois { get; set; } = string.Empty;
        public int Annee { get; set; }
        public int NombreInterventions { get; set; }
        public decimal TauxResolution { get; set; }
        public decimal TempsMoyenResolution { get; set; }
    }
}
