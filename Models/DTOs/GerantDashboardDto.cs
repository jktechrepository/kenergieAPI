using System.Text.Json.Serialization;
using Kenergie.Models.DTOs.Devise;

namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant le dashboard Gérant avec vue spécifique à sa société
    /// </summary>
    public class GerantDashboardDto
    {
        /// <summary>
        /// Statistiques générales de la société du gérant
        /// </summary>
        public SocieteStatistiquesDto SocieteStatistiques { get; set; } = new();

        /// <summary>
        /// Statistiques des clients de la société
        /// </summary>
        public ClientsStatistiquesDto ClientsStatistiques { get; set; } = new();

        /// <summary>
        /// Top 5 des clients par chiffre d'affaires
        /// </summary>
        public List<TopClientDto> Top5ClientsCA { get; set; } = new();

        /// <summary>
        /// Top 5 des clients avec le plus d'arriérés
        /// </summary>
        public List<TopClientDto> Top5ClientsArrieres { get; set; } = new();

        /// <summary>
        /// Alertes importantes pour la société
        /// </summary>
        public List<AlerteSocieteDto> AlertesSociete { get; set; } = new();

        /// <summary>
        /// Tendances sur les 12 derniers mois pour la société
        /// </summary>
        public TendancesDto Tendances { get; set; } = new();

        /// <summary>
        /// Statistiques des paiements récents
        /// </summary>
        public PaiementsStatistiquesDto PaiementsStatistiques { get; set; } = new();

        /// <summary>
        /// Date de génération du dashboard
        /// </summary>
        public DateTime DateGeneration { get; set; } = DateTime.UtcNow;
        public string? CodeDevisePrincipale { get; set; }
    }

    /// <summary>
    /// Statistiques générales de la société
    /// </summary>
    public class SocieteStatistiquesDto
    {
        /// <summary>
        /// Nom de la société
        /// </summary>
        public string NomSociete { get; set; } = string.Empty;

        /// <summary>
        /// Nombre total de clients
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// Nombre de clients actifs
        /// </summary>
        public int ClientsActifs { get; set; }

        /// <summary>
        /// Chiffre d'affaires du mois en cours
        /// </summary>
        public decimal ChiffreAffairesMois { get; set; }

        /// <summary>
        /// Montant total des arriérés
        /// </summary>
        public decimal MontantTotalArrieres { get; set; }

        /// <summary>
        /// Total des dépenses validées du mois en cours (devise principale).
        /// </summary>
        public decimal MontantDepensesMois { get; set; }

        /// <summary>
        /// Nombre de dépenses en attente de validation pour la société.
        /// </summary>
        public int NombreDepensesAValider { get; set; }

        /// <summary>
        /// Montant indicatif des dépenses en attente (non comptabilisé).
        /// </summary>
        public decimal MontantDepensesEnAttente { get; set; }

        /// <summary>
        /// Taux de recouvrement du mois
        /// </summary>
        public decimal TauxRecouvrement { get; set; }

        /// <summary>
        /// Variation du CA par rapport au mois précédent
        /// </summary>
        public decimal VariationCAMoisPrecedent { get; set; }

        /// <summary>
        /// Nombre total de factures du mois
        /// </summary>
        public int TotalFacturesMois { get; set; }

        /// <summary>
        /// Nombre de factures payées du mois
        /// </summary>
        public int FacturesPayeesMois { get; set; }

        /// <summary>
        /// Équivalents USD indicatifs des montants synthèse (taux du jour).
        /// </summary>
        public SocieteStatistiquesSyntheseUsdDto? SyntheseUsd { get; set; }
    }

    /// <summary>
    /// Statistiques des clients
    /// </summary>
    public class ClientsStatistiquesDto
    {
        /// <summary>
        /// Nombre total de clients
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// Nombre de clients actifs (avec factures ce mois)
        /// </summary>
        public int ClientsActifs { get; set; }

        /// <summary>
        /// Nombre de nouveaux clients ce mois
        /// </summary>
        public int NouveauxClientsMois { get; set; }

        /// <summary>
        /// Nombre de clients avec des arriérés
        /// </summary>
        public int ClientsAvecArrieres { get; set; }

        /// <summary>
        /// Pourcentage de clients avec des arriérés
        /// </summary>
        public decimal PourcentageClientsAvecArrieres { get; set; }

        /// <summary>
        /// Répartition des clients par catégorie
        /// </summary>
        public List<ClientsParCategorieDto> RepartitionParCategorie { get; set; } = new();
    }

    /// <summary>
    /// DTO pour le top des clients
    /// </summary>
    public class TopClientDto
    {
        /// <summary>
        /// Rang dans le classement
        /// </summary>
        public int Rang { get; set; }

        /// <summary>
        /// Identifiant du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Valeur (CA ou arriérés)
        /// </summary>
        public decimal Valeur { get; set; }

        /// <summary>
        /// Variation par rapport au mois précédent
        /// </summary>
        public decimal VariationMoisPrecedent { get; set; }
    }

    /// <summary>
    /// Alerte spécifique à la société
    /// </summary>
    public class AlerteSocieteDto
    {
        /// <summary>
        /// Identifiant de l'alerte
        /// </summary>
        public int IdAlerte { get; set; }

        /// <summary>
        /// Type d'alerte
        /// </summary>
        public string TypeAlerte { get; set; } = string.Empty;

        /// <summary>
        /// Niveau de criticité (Faible, Moyenne, Élevée, Critique)
        /// </summary>
        public string NiveauCriticite { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'alerte
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Date de déclenchement de l'alerte
        /// </summary>
        public DateTime DateAlerte { get; set; }

        /// <summary>
        /// Statut de l'alerte (Non lue, En cours, Résolue)
        /// </summary>
        public string Statut { get; set; } = "Non lue";

        /// <summary>
        /// Identifiant du client concerné (si applicable)
        /// </summary>
        public int? IdClient { get; set; }

        /// <summary>
        /// Nom du client concerné (si applicable)
        /// </summary>
        public string? NomClient { get; set; }
    }

    /// <summary>
    /// Statistiques des paiements
    /// </summary>
    public class PaiementsStatistiquesDto
    {
        /// <summary>
        /// Montant total des paiements du jour
        /// </summary>
        public decimal PaiementsJour { get; set; }

        /// <summary>
        /// Montant total des paiements de la semaine
        /// </summary>
        public decimal PaiementsSemaine { get; set; }

        /// <summary>
        /// Montant total des paiements du mois
        /// </summary>
        public decimal PaiementsMois { get; set; }

        /// <summary>
        /// Nombre de paiements du jour
        /// </summary>
        public int NombrePaiementsJour { get; set; }

        /// <summary>
        /// Nombre de paiements de la semaine
        /// </summary>
        public int NombrePaiementsSemaine { get; set; }

        /// <summary>
        /// Nombre de paiements du mois
        /// </summary>
        public int NombrePaiementsMois { get; set; }

        /// <summary>
        /// Moyenne des paiements journaliers ce mois
        /// </summary>
        public decimal MoyennePaiementsJournaliers { get; set; }
    }

    /// <summary>
    /// Répartition des clients par catégorie
    /// </summary>
    public class ClientsParCategorieDto
    {
        /// <summary>
        /// Nom de la catégorie
        /// </summary>
        public string Categorie { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de clients dans cette catégorie
        /// </summary>
        public int NombreClients { get; set; }

        /// <summary>
        /// Pourcentage par rapport au total
        /// </summary>
        public decimal Pourcentage { get; set; }
    }
}
