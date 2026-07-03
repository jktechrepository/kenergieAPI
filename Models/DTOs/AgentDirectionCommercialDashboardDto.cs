using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO principal pour le Dashboard Agent Direction Commercial
    /// Vue simplifiée adaptée aux agents de terrain
    /// </summary>
    public class AgentDirectionCommercialDashboardDto
    {
        /// <summary>
        /// Statistiques personnelles de l'agent
        /// </summary>
        public AgentStatsDto AgentStats { get; set; } = new();

        /// <summary>
        /// Performance personnelle du mois
        /// </summary>
        public AgentPerformancePersonnelDto Performance { get; set; } = new();

        /// <summary>
        /// Clients gérés par l'agent
        /// </summary>
        public List<ClientAgentDto> ClientsGeres { get; set; } = new();

        /// <summary>
        /// Prospects assignés à l'agent
        /// </summary>
        public List<ProspectAgentDto> Prospects { get; set; } = new();

        /// <summary>
        /// Tâches et rappels du jour
        /// </summary>
        public List<TacheDto> TachesDuJour { get; set; } = new();

        /// <summary>
        /// Objectifs du mois et progression
        /// </summary>
        public ObjectifsMoisDto ObjectifsMois { get; set; } = new();

        /// <summary>
        /// Activités récentes
        /// </summary>
        public List<ActiviteRecenteDto> ActivitesRecentes { get; set; } = new();
    }

    /// <summary>
    /// Statistiques personnelles de l'agent
    /// </summary>
    public class AgentStatsDto
    {
        /// <summary>
        /// Total des clients gérés
        /// </summary>
        public int TotalClientsGeres { get; set; }

        /// <summary>
        /// Nouveaux clients ce mois
        /// </summary>
        public int NouveauxClientsMois { get; set; }

        /// <summary>
        /// Recouvrement du mois
        /// </summary>
        public decimal RecouvrementMois { get; set; }

        /// <summary>
        /// Taux de conversion personnel
        /// </summary>
        public decimal TauxConversionPersonnel { get; set; }

        /// <summary>
        /// Nombre de visites réalisées ce mois
        /// </summary>
        public int VisitesMois { get; set; }

        /// <summary>
        /// Nombre de prospects en cours
        /// </summary>
        public int ProspectsEnCours { get; set; }

        /// <summary>
        /// Valeur moyenne des contrats signés
        /// </summary>
        public decimal ValeurMoyenneContrat { get; set; }

        /// <summary>
        /// Classement dans l'équipe
        /// </summary>
        public int ClassementEquipe { get; set; }

        /// <summary>
        /// Total d'agents dans l'équipe
        /// </summary>
        public int TotalAgentsEquipe { get; set; }
    }

    /// <summary>
    /// Performance personnelle détaillée
    /// </summary>
    public class AgentPerformancePersonnelDto
    {
        /// <summary>
        /// Identifiant de l'agent
        /// </summary>
        public int IdAgent { get; set; }

        /// <summary>
        /// Nom de l'agent
        /// </summary>
        public string NomAgent { get; set; } = string.Empty;

        /// <summary>
        /// Objectif de recouvrement mensuel
        /// </summary>
        public decimal ObjectifRecouvrement { get; set; }

        /// <summary>
        /// Recouvrement réalisé
        /// </summary>
        public decimal RecouvrementRealise { get; set; }

        /// <summary>
        /// Taux d'atteinte de l'objectif
        /// </summary>
        public decimal TauxAtteinteObjectif { get; set; }

        /// <summary>
        /// Objectif de nouveaux clients
        /// </summary>
        public int ObjectifNouveauxClients { get; set; }

        /// <summary>
        /// Nouveaux clients obtenus
        /// </summary>
        public int NouveauxClientsObtenus { get; set; }

        /// <summary>
        /// Taux d'atteinte des nouveaux clients
        /// </summary>
        public decimal TauxAtteinteNouveauxClients { get; set; }

        /// <summary>
        /// Note de performance
        /// </summary>
        public string NotePerformance { get; set; } = string.Empty;

        /// <summary>
        /// Progression par rapport au mois précédent
        /// </summary>
        public decimal ProgressionMoisPrecedent { get; set; }
    }

    /// <summary>
    /// Client géré par l'agent
    /// </summary>
    public class ClientAgentDto
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
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Adresse du client
        /// </summary>
        public string Adresse { get; set; } = string.Empty;

        /// <summary>
        /// Statut du client
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>
        /// Date de dernière visite
        /// </summary>
        public DateTime? DerniereVisite { get; set; }

        /// <summary>
        /// Montant total des factures
        /// </summary>
        public decimal MontantTotalFactures { get; set; }

        /// <summary>
        /// Montant payé
        /// </summary>
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Montant restant dû
        /// </summary>
        public decimal MontantDu { get; set; }

        /// <summary>
        /// Date du dernier paiement
        /// </summary>
        public DateTime? DernierPaiement { get; set; }

        /// <summary>
        /// Type de courant
        /// </summary>
        public string TypeDeCourant { get; set; } = string.Empty;

        /// <summary>
        /// Priorité de suivi
        /// </summary>
        public string PrioriteSuivi { get; set; } = string.Empty;
    }

    /// <summary>
    /// Prospect assigné à l'agent
    /// </summary>
    public class ProspectAgentDto
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
        /// Adresse du prospect
        /// </summary>
        public string Adresse { get; set; } = string.Empty;

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
        public DateTime? DateDernierContact { get; set; }

        /// <summary>
        /// Prochaine action prévue
        /// </summary>
        public string ProchaineAction { get; set; } = string.Empty;

        /// <summary>
        /// Date de la prochaine action
        /// </summary>
        public DateTime? DateProchaineAction { get; set; }

        /// <summary>
        /// Priorité
        /// </summary>
        public string Priorite { get; set; } = string.Empty;

        /// <summary>
        /// Notes du prospect
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime DateCreation { get; set; }
    }

    /// <summary>
    /// Tâche ou rappel pour l'agent
    /// </summary>
    public class TacheDto
    {
        /// <summary>
        /// Identifiant de la tâche
        /// </summary>
        public int IdTache { get; set; }

        /// <summary>
        /// Titre de la tâche
        /// </summary>
        public string Titre { get; set; } = string.Empty;

        /// <summary>
        /// Description de la tâche
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type de tâche
        /// </summary>
        public string TypeTache { get; set; } = string.Empty;

        /// <summary>
        /// Priorité
        /// </summary>
        public string Priorite { get; set; } = string.Empty;

        /// <summary>
        /// Heure prévue
        /// </summary>
        public TimeSpan? HeurePrevue { get; set; }

        /// <summary>
        /// Statut de la tâche
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>
        /// Client ou prospect concerné
        /// </summary>
        public string EntiteConcernee { get; set; } = string.Empty;

        /// <summary>
        /// Date de création
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Date d'échéance
        /// </summary>
        public DateTime? DateEcheance { get; set; }
    }

    /// <summary>
    /// Objectifs du mois et progression
    /// </summary>
    public class ObjectifsMoisDto
    {
        /// <summary>
        /// Mois concerné
        /// </summary>
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année concernée
        /// </summary>
        public int Annee { get; set; }

        /// <summary>
        /// Objectif de recouvrement
        /// </summary>
        public decimal ObjectifRecouvrement { get; set; }

        /// <summary>
        /// Recouvrement actuel
        /// </summary>
        public decimal RecouvrementActuel { get; set; }

        /// <summary>
        /// Progression en pourcentage
        /// </summary>
        public decimal ProgressionRecouvrement { get; set; }

        /// <summary>
        /// Objectif de nouveaux clients
        /// </summary>
        public int ObjectifNouveauxClients { get; set; }

        /// <summary>
        /// Nouveaux clients actuels
        /// </summary>
        public int NouveauxClientsActuels { get; set; }

        /// <summary>
        /// Progression en pourcentage
        /// </summary>
        public decimal ProgressionNouveauxClients { get; set; }

        /// <summary>
        /// Objectif de visites
        /// </summary>
        public int ObjectifVisites { get; set; }

        /// <summary>
        /// Visites réalisées
        /// </summary>
        public int VisitesRealisees { get; set; }

        /// <summary>
        /// Progression en pourcentage
        /// </summary>
        public decimal ProgressionVisites { get; set; }

        /// <summary>
        /// Jours restants dans le mois
        /// </summary>
        public int JoursRestants { get; set; }
    }

    /// <summary>
    /// Activité récente de l'agent
    /// </summary>
    public class ActiviteRecenteDto
    {
        /// <summary>
        /// Identifiant de l'activité
        /// </summary>
        public int IdActivite { get; set; }

        /// <summary>
        /// Type d'activité
        /// </summary>
        public string TypeActivite { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'activité
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Entité concernée (client/prospect)
        /// </summary>
        public string EntiteConcernee { get; set; } = string.Empty;

        /// <summary>
        /// Montant concerné (si applicable)
        /// </summary>
        public decimal? MontantConcerne { get; set; }

        /// <summary>
        /// Date de l'activité
        /// </summary>
        public DateTime DateActivite { get; set; }

        /// <summary>
        /// Statut de l'activité
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>
        /// Commentaires supplémentaires
        /// </summary>
        public string Commentaires { get; set; } = string.Empty;
    }
}
