namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant les arriérés d'un client pour une période spécifique (mois/année)
    /// </summary>
    public class ArriereParPeriodeDto
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
        /// Nombre d'usages différents dans cette période
        /// </summary>
        public int NombreUsages { get; set; }

        /// <summary>
        /// Nombre de factures dans cette période
        /// </summary>
        public int NombreFactures { get; set; }

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
        /// Détail des factures individuelles par usage (liste des ClientFacture de cette période avec MontantDu > 0)
        /// </summary>
        public List<ClientFactureDto> DetailFactures { get; set; } = new List<ClientFactureDto>();
    }
}
