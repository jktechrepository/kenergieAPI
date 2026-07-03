using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Facture
{
    /// <summary>
    /// DTO pour la création en masse de factures
    /// </summary>
    public class BulkCreateFactureDto
    {
        /// <summary>
        /// Liste des factures à créer
        /// </summary>
        [Required(ErrorMessage = "La liste des factures est obligatoire")]
        [MinLength(1, ErrorMessage = "Au moins une facture doit être fournie")]
        [MaxLength(100, ErrorMessage = "Maximum 100 factures par requête")]
        public List<CreateFactureItemDto> Factures { get; set; } = new List<CreateFactureItemDto>();
    }

    /// <summary>
    /// DTO pour un élément de facture dans la création en masse
    /// </summary>
    public class CreateFactureItemDto
    {
        /// <summary>
        /// Numéro de la facture (optionnel, généré automatiquement si absent)
        /// </summary>
        [MaxLength(100, ErrorMessage = "Le numéro de facture ne peut pas dépasser 100 caractères")]
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Montant de la facture
        /// </summary>
        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }

        /// <summary>
        /// Date d'émission de la facture (optionnel, par défaut maintenant)
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateEmission { get; set; }

        /// <summary>
        /// Mois d'émission de la facture (1-12)
        /// </summary>
        [Required(ErrorMessage = "Le mois d'émission est obligatoire")]
        [Range(1, 12, ErrorMessage = "Le mois doit être entre 1 et 12")]
        public int MoisEmission { get; set; }

        /// <summary>
        /// Année d'émission de la facture
        /// </summary>
        [Required(ErrorMessage = "L'année d'émission est obligatoire")]
        [Range(2000, 2100, ErrorMessage = "L'année doit être entre 2000 et 2100")]
        public int AnneesEmission { get; set; }

        /// <summary>
        /// Identifiant de l'usage (obligatoire)
        /// </summary>
        [Required(ErrorMessage = "L'identifiant de l'usage est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'identifiant de l'usage doit être valide")]
        public int IdUsage { get; set; }

        /// <summary>
        /// Identifiant du type de courant (optionnel, si spécifié filtre les clients par type)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'identifiant du type de courant doit être valide")]
        public int? IdTypeDeCourant { get; set; }

        /// <summary>
        /// Statut de la facture (optionnel, par défaut true)
        /// </summary>
        public bool? Statut { get; set; }

        /// <summary>
        /// Indique si la facture doit être diffusée immédiatement (optionnel, par défaut false)
        /// </summary>
        public bool? EstDiffusee { get; set; }
    }
}
