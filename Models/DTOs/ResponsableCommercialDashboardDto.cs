using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO principal pour le Dashboard Responsable Commercial
    /// </summary>
    public class ResponsableCommercialDashboardDto
    {
        /// <summary>
        /// Statistiques financières globales (héritées du FinancierDashboard)
        /// </summary>
        public GlobalFinancierStatistiquesDto GlobalStatistiques { get; set; } = new();

        /// <summary>
        /// Statistiques commerciales spécifiques
        /// </summary>
        public CommercialStatsDto CommercialStats { get; set; } = new();

        /// <summary>
        /// Performance des agents Direction Commercial
        /// </summary>
        public List<AgentPerformanceDto> AgentsPerformance { get; set; } = new();

        /// <summary>
        /// Acquisitions récentes de nouveaux clients
        /// </summary>
        public List<ClientAcquisitionDto> ClientAcquisitions { get; set; } = new();

        /// <summary>
        /// Prospects et opportunités commerciales
        /// </summary>
        public List<ProspectDto> Prospects { get; set; } = new();

        /// <summary>
        /// Tendances commerciales sur 12 mois
        /// </summary>
        public TendancesCommercialesDto TendancesCommerciales { get; set; } = new();

        /// <summary>
        /// Top 10 des agents collecteurs du jour
        /// </summary>
        public List<TopAgentCollecteurDto> Top10AgentsCollecteurs { get; set; } = new();
    }

    /// <summary>
    /// Statistiques commerciales spécifiques au Responsable Commercial
    /// </summary>
    public class CommercialStatsDto
    {
        /// <summary>
        /// Total des clients actifs
        /// </summary>
        public int TotalClientsActifs { get; set; }

        /// <summary>
        /// Nombre de nouveaux clients ce mois
        /// </summary>
        public int NouveauxClientsMois { get; set; }

        /// <summary>
        /// Taux de conversion (%)
        /// </summary>
        public decimal TauxConversion { get; set; }

        /// <summary>
        /// Total des agents Direction Commercial
        /// </summary>
        public int TotalAgentsDirection { get; set; }

        /// <summary>
        /// Chiffre d'affaires commercial du mois
        /// </summary>
        public decimal ChiffreAffairesCommercial { get; set; }

        /// <summary>
        /// Nombre de prospects en cours
        /// </summary>
        public int ProspectsEnCours { get; set; }

        /// <summary>
        /// Valeur moyenne des contrats
        /// </summary>
        public decimal ValeurMoyenneContrat { get; set; }
    }

    /// <summary>
    /// Performance d'un agent Direction Commercial
    /// </summary>
    public class AgentPerformanceDto
    {
        /// <summary>
        /// Identifiant de l'agent
        /// </summary>
        public int IdAgent { get; set; }

        /// <summary>
        /// Nom complet de l'agent
        /// </summary>
        public string NomAgent { get; set; } = string.Empty;

        /// <summary>
        /// Matricule de l'agent
        /// </summary>
        public string Matricule { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de clients gérés
        /// </summary>
        public int ClientsGeres { get; set; }

        /// <summary>
        /// Recouvrement du mois
        /// </summary>
        public decimal RecouvrementMois { get; set; }

        /// <summary>
        /// Nombre de nouveaux clients ce mois
        /// </summary>
        public int NouveauxClientsMois { get; set; }

        /// <summary>
        /// Taux d'atteinte de l'objectif (%)
        /// </summary>
        public decimal TauxAtteinteObjectif { get; set; }

        /// <summary>
        /// Taux de conversion (%)
        /// </summary>
        public decimal TauxConversion { get; set; }

        /// <summary>
        /// Statut de performance
        /// </summary>
        public string StatutPerformance { get; set; } = string.Empty;

        /// <summary>
        /// Date de dernière activité
        /// </summary>
        public DateTime DerniereActivite { get; set; }
    }

    /// <summary>
    /// Acquisition d'un nouveau client
    /// </summary>
    public class ClientAcquisitionDto
    {
        /// <summary>
        /// Identifiant du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Téléphone du client
        /// </summary>
        public string Telephone { get; set; } = string.Empty;

        /// <summary>
        /// Email du client
        /// </summary>
        public string EmailClient { get; set; } = string.Empty;

        /// <summary>
        /// Agent responsable
        /// </summary>
        public string AgentResponsable { get; set; } = string.Empty;

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Montant du premier contrat
        /// </summary>
        public decimal MontantPremierContrat { get; set; }

        /// <summary>
        /// Type de courant
        /// </summary>
        public string TypeDeCourant { get; set; } = string.Empty;

        /// <summary>
        /// Société
        /// </summary>
        public string Societe { get; set; } = string.Empty;
    }

    /// <summary>
    /// Prospect commercial
    /// </summary>
    public class ProspectDto
    {
        /// <summary>
        /// Identifiant du prospect
        /// </summary>
        public int IdProspect { get; set; }

        /// <summary>
        /// Nom du prospect
        /// </summary>
        public string NomProspect { get; set; } = string.Empty;

        /// <summary>
        /// Téléphone du prospect
        /// </summary>
        public string Telephone { get; set; } = string.Empty;

        /// <summary>
        /// Email du prospect
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Agent assigné
        /// </summary>
        public string AgentAssigné { get; set; } = string.Empty;

        /// <summary>
        /// Statut du prospect
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>
        /// Potentiel estimé
        /// </summary>
        public decimal PotentielEstime { get; set; }

        /// <summary>
        /// Date du dernier contact
        /// </summary>
        public DateTime DateDernierContact { get; set; }

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Priorité
        /// </summary>
        public string Priorite { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tendances commerciales sur 12 mois
    /// </summary>
    public class TendancesCommercialesDto
    {
        /// <summary>
        /// Nouveaux clients par mois
        /// </summary>
        public List<MoisStatistiqueDto> NouveauxClientsParMois { get; set; } = new();

        /// <summary>
        /// Chiffre d'affaires par mois
        /// </summary>
        public List<MoisStatistiqueDto> ChiffreAffairesParMois { get; set; } = new();

        /// <summary>
        /// Taux de conversion par mois
        /// </summary>
        public List<MoisStatistiqueDto> TauxConversionParMois { get; set; } = new();
    }

    /// <summary>
    /// Statistiques mensuelles
    /// </summary>
    public class MoisStatistiqueDto
    {
        /// <summary>
        /// Nom du mois
        /// </summary>
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année
        /// </summary>
        public int Annee { get; set; }

        /// <summary>
        /// Numéro du mois (1-12)
        /// </summary>
        public int MoisNumero { get; set; }

        /// <summary>
        /// Nombre (pour les clients)
        /// </summary>
        public int Nombre { get; set; }

        /// <summary>
        /// Valeur (pour les montants)
        /// </summary>
        public decimal Valeur { get; set; }

        /// <summary>
        /// Variation en pourcentage par rapport au mois précédent
        /// </summary>
        public decimal VariationPourcentage { get; set; }
    }
}
