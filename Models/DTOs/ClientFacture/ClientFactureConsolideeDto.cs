namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant une facture consolidée pour un client (regroupement par période)
    /// Permet d'afficher un total consolidé pour toutes les factures d'un client pour une période donnée (mois/année)
    /// </summary>
    public class ClientFactureConsolideeDto
    {
        /// <summary>
        /// Mois d'émission (format: "01", "02", ..., "12")
        /// </summary>
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année d'émission
        /// </summary>
        public int Annees { get; set; }

        /// <summary>
        /// Date d'émission (la plus récente parmi les factures de cette période)
        /// </summary>
        public DateTime? DateEmission { get; set; }

        /// <summary>
        /// Montant total consolidé (somme de tous les Montant des factures de cette période)
        /// </summary>
        public decimal MontantTotal { get; set; }

        /// <summary>
        /// Montant payé total consolidé (somme de tous les MontantPaye des factures de cette période)
        /// </summary>
        public decimal MontantPayeTotal { get; set; }

        /// <summary>
        /// Montant dû total consolidé (somme de tous les MontantDu des factures de cette période)
        /// </summary>
        public decimal MontantDuTotal { get; set; }

        /// <summary>
        /// Détail des factures individuelles par usage (liste des ClientFacture de cette période)
        /// </summary>
        public List<ClientFactureDto> DetailFactures { get; set; } = new List<ClientFactureDto>();

        /// <summary>
        /// Identifiant du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        public string? NomClient { get; set; }

        /// <summary>
        /// Code consommateur du client
        /// </summary>
        public string? CodeCons { get; set; }

        /// <summary>
        /// Nombre de factures dans cette période
        /// </summary>
        public int NombreFactures { get; set; }

        /// <summary>
        /// Nombre d'usages différents dans cette période
        /// </summary>
        public int NombreUsages { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Liste des périodes disponibles pour ce client (mois/année)
        /// Utile pour permettre au frontend de naviguer entre les périodes sans faire un autre appel.
        /// </summary>
        public List<PeriodeClientFactureDto> PeriodesDisponibles { get; set; } = new List<PeriodeClientFactureDto>();
    }
}
