namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant les statistiques du dashboard pour une société
    /// </summary>
    public class DashboardDto
    {
        /// <summary>
        /// Total des agents de la société
        /// </summary>
        public int TotalAgents { get; set; }

        /// <summary>
        /// Total des clients actifs de la société
        /// </summary>
        public int TotalClientsActifs { get; set; }

        /// <summary>
        /// Paiements du mois
        /// </summary>
        public decimal PaiementsDuMois { get; set; }

        /// <summary>
        /// Total général des arriérés
        /// </summary>
        public decimal TotalGeneralArriere { get; set; }

        /// <summary>
        /// Collecte du mois avec variations
        /// </summary>
        public CollecteMoisDto CollecteMois { get; set; } = new();

        /// <summary>
        /// Facturation du mois avec variations
        /// </summary>
        public FactureMoisDto FactureMois { get; set; } = new();

        /// <summary>
        /// Répartition des clients par catégorie
        /// </summary>
        public List<RepartitionClientParCategorieDto> RepartitionClientsParCategorie { get; set; } = new();

        /// <summary>
        /// Top 10 des agents collecteurs
        /// </summary>
        public List<TopAgentCollecteurDto> Top10AgentsCollecteurs { get; set; } = new();
    }

    /// <summary>
    /// DTO représentant un agent collecteur dans le top 5
    /// </summary>
    public class TopAgentCollecteurDto
    {
        public int IdAgent { get; set; }
        public string? Matricule { get; set; }
        public string? NomComplet { get; set; }
        public decimal MontantCollecte { get; set; }
        public int NombrePaiements { get; set; }
    }

    /// <summary>
    /// DTO représentant la collecte mensuelle
    /// </summary>
    public class CollecteMoisDto
    {
        public string MoisLabel { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public decimal MontantMoisPrecedent { get; set; }
        public decimal VariationPourcentage { get; set; }
        public int NombrePaiements { get; set; }
        public decimal TicketMoyen { get; set; }
        public decimal VariationTicketMoyen { get; set; }
    }

    /// <summary>
    /// DTO représentant la facturation mensuelle
    /// </summary>
    public class FactureMoisDto
    {
        public string MoisLabel { get; set; } = string.Empty;
        public decimal MontantTotalFactures { get; set; }
        public decimal MontantTotalFacturesMoisPrecedent { get; set; }
        public decimal VariationPourcentage { get; set; }
        public int NombreFactures { get; set; }
        public int NombreFacturesMoisPrecedent { get; set; }
        public decimal FactureMoyenne { get; set; }
        public decimal FactureMoyenneMoisPrecedent { get; set; }
        public decimal VariationFactureMoyenne { get; set; }
        public decimal TauxRecouvrementEstime { get; set; }
    }

    /// <summary>
    /// DTO représentant la répartition des clients par catégorie
    /// </summary>
    public class RepartitionClientParCategorieDto
    {
        public int IdCategorie { get; set; }
        public string NomCategorie { get; set; } = string.Empty;
        public int NombreClients { get; set; }
        public decimal Pourcentage { get; set; }
    }
}

