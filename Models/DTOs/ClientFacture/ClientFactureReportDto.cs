using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour le rapport des client-factures agrégées par mois/année
    /// Correspond à la requête SQL de reporting des factures clients
    /// </summary>
    public class ClientFactureReportDto
    {
        /// <summary>
        /// Code consommateur du client
        /// </summary>
        [JsonPropertyName("CODECONS")]
        public string CodeCons { get; set; } = string.Empty;

        /// <summary>
        /// Nom du client
        /// </summary>
        [JsonPropertyName("NOM_DU_CLIENT")]
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Nom de l'axe auquel appartient le client
        /// </summary>
        [JsonPropertyName("AXE")]
        public string Axe { get; set; } = string.Empty;

        /// <summary>
        /// Libellé de l'usage de la facture
        /// </summary>
        [JsonPropertyName("USAGE")]
        public string Usage { get; set; } = string.Empty;

        /// <summary>
        /// Montant total agrégé des factures pour ce client/usage/période
        /// </summary>
        [JsonPropertyName("MONTANT")]
        public decimal Montant { get; set; }

        /// <summary>
        /// Mois de facturation (format: "01", "02", ..., "12")
        /// </summary>
        [JsonPropertyName("MOIS")]
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année de facturation
        /// </summary>
        [JsonPropertyName("ANNEES")]
        public int Annees { get; set; }
    }
}
