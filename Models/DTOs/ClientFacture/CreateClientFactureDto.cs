using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour la création d'une ClientFacture (facture système)
    /// </summary>
    public class CreateClientFactureDto
    {
        /// <summary>
        /// Identifiant de la facture (obligatoire pour facture système)
        /// </summary>
        [Required(ErrorMessage = "L'ID de la facture est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la facture doit être valide")]
        public int IdFacture { get; set; }

        /// <summary>
        /// Identifiant du client (obligatoire)
        /// </summary>
        [Required(ErrorMessage = "L'ID du client est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID du client doit être valide")]
        public int IdClient { get; set; }

        /// <summary>
        /// Montant total pour ce client (déjà multiplié par nombreBatiment)
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal? Montant { get; set; }

        /// <summary>
        /// Snapshot du nombre de bâtiments au moment de la facture
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de bâtiments doit être au moins 1")]
        public int? nombreBatiment { get; set; }

        /// <summary>
        /// Mois d'émission (format: "01", "02", ..., "12" ou "Janvier", etc.)
        /// </summary>
        [MaxLength(20)]
        public string? Mois { get; set; }

        /// <summary>
        /// Année d'émission
        /// </summary>
        [Range(2000, 2100, ErrorMessage = "L'année doit être entre 2000 et 2100")]
        public int? Annees { get; set; }

        /// <summary>
        /// Date d'émission de la facture
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateEmission { get; set; }
    }
}
