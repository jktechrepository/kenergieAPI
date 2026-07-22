using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant un paiement de facture
    /// </summary>
    public class Paiement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPaiement { get; set; }

        /// <summary>
        /// Identifiant de la facture payée (NULL pour les arriérés pré-existants)
        /// </summary>
        public int? IdFacture { get; set; }

        /// <summary>
        /// Identifiant du client qui effectue le paiement
        /// </summary>
        public int? IdClient { get; set; }

        /// <summary>
        /// Identifiant de la ClientFacture pour les arriérés pré-existants (NULL pour les factures système)
        /// </summary>
        public int? IdClientFacture { get; set; }

        /// <summary>
        /// Indique si ce paiement concerne un arriéré pré-existant
        /// </summary>
        public bool EstPaiementArriere { get; set; } = false;

        /// <summary>
        /// Montant payé
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Code devise du paiement (doit correspondre à la devise de la ClientFacture en phase 1).
        /// </summary>
        [MaxLength(3)]
        public string? CodeDevisePaiement { get; set; }

        /// <summary>
        /// Snapshot de la devise principale société au moment du paiement.
        /// </summary>
        [MaxLength(3)]
        public string? CodeDevisePrincipale { get; set; }

        /// <summary>
        /// Taux figé vers la devise principale.
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal? TauxVersDevisePrincipale { get; set; }

        /// <summary>
        /// Montant payé consolidé en devise principale.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantPayeDevisePrincipale { get; set; }

        /// <summary>
        /// Montant facturé sur la ligne ClientFacture (aligné sur ClientFacture.Montant).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantAPaye { get; set; }

        /// <summary>
        /// Montant à payer consolidé en devise principale.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantAPayeDevisePrincipale { get; set; }

        /// <summary>
        /// Reste dû sur la ligne ClientFacture (aligné sur ClientFacture.MontantDu).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ResteAPaye { get; set; }

        /// <summary>
        /// Reste à payer consolidé en devise principale.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ResteAPayeDevisePrincipale { get; set; }

        /// <summary>
        /// Date du paiement
        /// </summary>
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DatePaiement { get; set; } = DateTime.Now;

        /// <summary>
        /// Méthode de paiement (Espèces, Mobile Money, Virement, Carte, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? MethodePaiement { get; set; }

        /// <summary>
        /// Référence de la transaction (numéro de transaction, référence virement, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? ReferenceTransaction { get; set; }

        /// <summary>
        /// Commentaire ou note sur le paiement
        /// </summary>
        [MaxLength(500)]
        public string? Commentaire { get; set; }

        /// <summary>
        /// Statut du paiement (En attente, Validé, Rejeté, etc.)
        /// </summary>
        [MaxLength(20)]
        public string Statut { get; set; } = "Validé";

        /// <summary>
        /// Indique si le paiement est supprimé (soft delete)
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Identifiant de l'utilisateur qui a enregistré le paiement
        /// (colonne IdUtilisateurEnregistrement en base)
        /// </summary>
        [Column("IdUtilisateur")]
        public int? IdUtilisateur { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification (pour delta sync)
        /// </summary>
        [JsonIgnore]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Identifiant unique de la demande client (pour idempotence offline)
        /// </summary>
        [MaxLength(36)]
        public string? ClientRequestId { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdFacture")]
        public Facture? Facture { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClient")]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdUtilisateur")]
        public Utilisateur? Utilisateur { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClientFacture")]
        public ClientFacture? ClientFacture { get; set; }
    }
}

