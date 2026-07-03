using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour enregistrer un paiement sur un arriéré pré-existant
    /// </summary>
    public class CreatePaiementArrierePreExistantDto
    {
        /// <summary>
        /// Identifiant de la ClientFacture (arriéré pré-existant)
        /// </summary>
        [Required(ErrorMessage = "L'ID de la ClientFacture est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la ClientFacture doit être valide")]
        public int IdClientFacture { get; set; }

        /// <summary>
        /// Montant payé
        /// </summary>
        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Date du paiement (optionnel, par défaut maintenant)
        /// </summary>
        [DataType(DataType.DateTime)]
        public DateTime? DatePaiement { get; set; }

        /// <summary>
        /// Méthode de paiement (Espèces, Mobile Money, Virement, Carte, etc.)
        /// </summary>
        [MaxLength(50, ErrorMessage = "La méthode de paiement ne peut pas dépasser 50 caractères")]
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Référence de la transaction (numéro de transaction, référence virement, etc.)
        /// </summary>
        [MaxLength(100, ErrorMessage = "La référence de transaction ne peut pas dépasser 100 caractères")]
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Commentaire ou note sur le paiement
        /// </summary>
        [MaxLength(500, ErrorMessage = "Le commentaire ne peut pas dépasser 500 caractères")]
        public string? Commentaire { get; set; }
    }
}
