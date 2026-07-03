using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant un rapport global des arriérés consolidés pour tous les clients
    /// Contient les totaux globaux et la liste des arriérés par client groupés par période
    /// </summary>
    public class ArrieresConsolidesGlobauxResponseDto
    {
        /// <summary>
        /// Total général global de tous les arriérés (tous clients confondus)
        /// </summary>
        [JsonPropertyOrder(1)]
        public decimal TotalGeneralGlobal { get; set; }

        /// <summary>
        /// Nombre total de clients avec arriérés
        /// </summary>
        [JsonPropertyOrder(2)]
        public int NombreTotalClients { get; set; }

        /// <summary>
        /// Nombre total de factures avec arriérés (tous clients confondus)
        /// </summary>
        [JsonPropertyOrder(3)]
        public int NombreTotalFactures { get; set; }

        /// <summary>
        /// Nombre total de périodes distinctes (mois/année) avec arriérés (tous clients confondus)
        /// </summary>
        [JsonPropertyOrder(4)]
        public int NombreTotalPeriodes { get; set; }

        /// <summary>
        /// Liste des arriérés consolidés par client
        /// Chaque élément contient les arriérés du client groupés par période
        /// </summary>
        [JsonPropertyOrder(5)]
        public List<ArrieresConsolidesResponseDto> ArrieresParClient { get; set; } = new List<ArrieresConsolidesResponseDto>();

        /// <summary>
        /// ✨ NOUVEAU : Liste des périodes disponibles (mois/année) qui ont des arriérés (MontantDu > 0)
        /// Permet au dashboard / frontend de proposer un filtre de périodes sans recalcul.
        /// </summary>
        [JsonPropertyOrder(6)]
        public List<PeriodeClientFactureDto> PeriodesDisponibles { get; set; } = new List<PeriodeClientFactureDto>();
    }
}
