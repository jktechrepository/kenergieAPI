using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour la création d'un paiement de facture
    /// </summary>
    public class CreatePaiementDto
    {
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

        /// <summary>
        /// Statut du paiement (optionnel, par défaut "Validé")
        /// </summary>
        [MaxLength(20, ErrorMessage = "Le statut ne peut pas dépasser 20 caractères")]
        public string? Statut { get; set; }

        /// <summary>
        /// Identifiant du client (optionnel, peut être déduit de la facture)
        /// </summary>
        public int? IdClient { get; set; }

        /// <summary>
        /// Identifiant de la facture (obligatoire pour les paiements de factures système)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la facture doit être valide")]
        public int? IdFacture { get; set; }

        /// <summary>
        /// Identifiant de la ClientFacture (obligatoire pour les paiements d'arriérés pré-existants)
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "L'ID de la ClientFacture doit être valide")]
        public int? IdClientFacture { get; set; }
    }
}

