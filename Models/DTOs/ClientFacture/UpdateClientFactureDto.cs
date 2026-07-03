using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour la mise à jour d'une ClientFacture
    /// </summary>
    public class UpdateClientFactureDto
    {
        /// <summary>
        /// Montant total (optionnel, ne sera mis à jour que si fourni)
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal? Montant { get; set; }

        /// <summary>
        /// Montant payé (optionnel, ne sera mis à jour que si fourni)
        /// </summary>
        [Range(0, double.MaxValue, ErrorMessage = "Le montant payé ne peut pas être négatif")]
        public decimal? MontantPaye { get; set; }

        /// <summary>
        /// Mois d'émission (optionnel)
        /// </summary>
        [MaxLength(20)]
        public string? Mois { get; set; }

        /// <summary>
        /// Année d'émission (optionnel)
        /// </summary>
        [Range(2000, 2100, ErrorMessage = "L'année doit être entre 2000 et 2100")]
        public int? Annees { get; set; }

        /// <summary>
        /// Date d'émission (optionnel)
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateEmission { get; set; }

        /// <summary>
        /// Description (optionnel, surtout pour arriérés pré-existants)
        /// </summary>
        [MaxLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        public string? Description { get; set; }

        /// <summary>
        /// Statut (optionnel)
        /// </summary>
        public bool? Statut { get; set; }
    }
}
