namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO simple représentant une période (mois/année) pour les factures client.
    /// </summary>
    public class PeriodeClientFactureDto
    {
        /// <summary>
        /// Mois (format: "01".."12")
        /// </summary>
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année
        /// </summary>
        public int Annees { get; set; }
    }
}

