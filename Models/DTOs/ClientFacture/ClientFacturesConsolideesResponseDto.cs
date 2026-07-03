using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO de réponse avec toutes les factures consolidées d'un client
    /// Contient les factures groupées par période (mois/année) avec totaux consolidés
    /// </summary>
    public class ClientFacturesConsolideesResponseDto
    {
        /// <summary>
        /// Identifiant du client
        /// </summary>
        [JsonPropertyOrder(1)]
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        [JsonPropertyOrder(2)]
        public string? NomClient { get; set; }

        /// <summary>
        /// Code consommateur du client
        /// </summary>
        [JsonPropertyOrder(3)]
        public string? CodeCons { get; set; }

        /// <summary>
        /// Montant total global (toutes périodes confondues)
        /// </summary>
        [JsonPropertyOrder(4)]
        public decimal MontantTotalGlobal { get; set; }

        /// <summary>
        /// Montant payé total global (toutes périodes confondues)
        /// </summary>
        [JsonPropertyOrder(5)]
        public decimal MontantPayeTotalGlobal { get; set; }

        /// <summary>
        /// Montant dû total global (toutes périodes confondues)
        /// </summary>
        [JsonPropertyOrder(6)]
        public decimal MontantDuTotalGlobal { get; set; }

        /// <summary>
        /// Nombre total de factures (toutes périodes confondues)
        /// </summary>
        [JsonPropertyOrder(7)]
        public int NombreTotalFactures { get; set; }

        /// <summary>
        /// Nombre total de périodes (mois/année) avec factures
        /// </summary>
        [JsonPropertyOrder(8)]
        public int NombreTotalPeriodes { get; set; }

        /// <summary>
        /// Liste des factures consolidées par période (mois/année)
        /// </summary>
        [JsonPropertyOrder(9)]
        public List<ClientFactureConsolideeDto> FacturesConsolidees { get; set; } = new List<ClientFactureConsolideeDto>();
    }
}
