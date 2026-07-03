using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant les arriérés d'un client consolidés par période (mois/année)
    /// Permet d'afficher les arriérés groupés par période avec totaux consolidés
    /// </summary>
    public class ArrieresConsolidesResponseDto
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
        /// Total général de tous les arriérés (somme de tous les montantDuTotal)
        /// </summary>
        [JsonPropertyOrder(4)]
        public decimal TotalGeneral { get; set; }

        /// <summary>
        /// Nombre total de factures avec arriérés (toutes périodes confondues)
        /// </summary>
        [JsonPropertyOrder(5)]
        public int NombreTotalFactures { get; set; }

        /// <summary>
        /// Nombre total de périodes (mois/année) avec arriérés
        /// </summary>
        [JsonPropertyOrder(6)]
        public int NombreTotalPeriodes { get; set; }

        /// <summary>
        /// Total de la dette antérieure : somme des montantDuTotal de toutes les périodes
        /// sauf la dernière période de facturation du client (Mois/Annees la plus récente).
        /// Calculé uniquement sur l'endpoint global lorsque moisFacturePrecedentSeulement=true.
        /// </summary>
        [JsonPropertyOrder(7)]
        public decimal DetteAnterieur { get; set; }

        /// <summary>
        /// 🆕 NOUVEAU : Adresse du client (récupérée depuis la table Client)
        /// </summary>
        [JsonPropertyOrder(8)]
        public string? AdresseClient { get; set; }

        /// <summary>
        /// 🆕 NOUVEAU : Catégorie du client (récupérée via ClientUsage → Usage → CategorieClient)
        /// </summary>
        [JsonPropertyOrder(9)]
        public string? CategorieClient { get; set; }

        /// <summary>
        /// 🆕 NOUVEAU : Libellé de l'usage principal du client (récupéré via ClientUsage → Usage)
        /// </summary>
        [JsonPropertyOrder(10)]
        public string? LibelleUsage { get; set; }

        /// <summary>
        /// Liste des arriérés groupés par période (mois/année)
        /// </summary>
        [JsonPropertyOrder(11)]
        public List<ArriereParPeriodeDto> ArrieresParPeriode { get; set; } = new List<ArriereParPeriodeDto>();

        /// <summary>
        /// ✨ NOUVEAU : Liste des périodes disponibles (mois/année) qui ont des arriérés (MontantDu > 0)
        /// Permet au frontend de proposer un sélecteur de périodes rapidement.
        /// </summary>
        [JsonPropertyOrder(12)]
        public List<PeriodeClientFactureDto> PeriodesDisponibles { get; set; } = new List<PeriodeClientFactureDto>();
    }
}
