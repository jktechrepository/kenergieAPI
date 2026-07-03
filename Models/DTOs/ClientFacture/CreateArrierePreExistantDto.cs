using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour la création d'un arriéré pré-existant (avant informatisation)
    /// </summary>
    public class CreateArrierePreExistantDto
    {
        /// <summary>
        /// Identifiant du client (obligatoire)
        /// </summary>
        [Required(ErrorMessage = "L'ID du client est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du client doit être valide")]
        public int IdClient { get; set; }

        /// <summary>
        /// Montant de l'arriéré
        /// </summary>
        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }

        /// <summary>
        /// Mois d'émission (format: "01", "02", ..., "12" ou "Janvier", etc.)
        /// </summary>
        [Required(ErrorMessage = "Le mois est obligatoire")]
        [MaxLength(20)]
        public string Mois { get; set; } = string.Empty;

        /// <summary>
        /// Année d'émission
        /// </summary>
        [Required(ErrorMessage = "L'année est obligatoire")]
        [Range(2000, 2100, ErrorMessage = "L'année doit être entre 2000 et 2100")]
        public int Annees { get; set; }

        /// <summary>
        /// Date d'émission (optionnel, par défaut maintenant)
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateEmission { get; set; }

        /// <summary>
        /// Description/libellé de l'arriéré pré-existant
        /// </summary>
        [MaxLength(500, ErrorMessage = "La description ne peut pas dépasser 500 caractères")]
        public string? Description { get; set; }
    }
}
