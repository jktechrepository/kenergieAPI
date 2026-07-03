using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant une facture
    /// </summary>
    public class Facture
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFacture { get; set; }

        /// <summary>
        /// Numéro de la facture
        /// </summary>
        [MaxLength(100)]
        [Column("numero_facture")]
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Montant de la facture
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Montant { get; set; }

        /// <summary>
        /// Date d'émission de la facture
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateEmission { get; set; }

        /// <summary>
        /// Statut de la facture (active/inactive)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Indique si la facture a déjà été diffusée aux clients
        /// </summary>
        public bool EstDiffusee { get; set; } = false;

        /// <summary>
        /// Date de dernière diffusion de la facture
        /// </summary>
        public DateTime? DateDiffusion { get; set; }

        /// <summary>
        /// Mois d'émission de la facture (1-12)
        /// </summary>
        [Range(1, 12)]
        public int MoisEmission { get; set; }

        /// <summary>
        /// Année d'émission de la facture
        /// </summary>
        [Range(2000, 2100)]
        public int AnneesEmission { get; set; }

        /// <summary>
        /// Identifiant de l'usage (la facturation se fait désormais par usage)
        /// </summary>
        [Required]
        public int IdUsage { get; set; }

        /// <summary>
        /// Identifiant du type de courant pour la tarification affinée
        /// </summary>
        [ForeignKey("TypeDeCourant")]
        [Column("IdTypeDeCourant")]
        public int? IdTypeDeCourant { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Usage? Usage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public TypeDeCourant? TypeDeCourant { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Paiement>? Paiements { get; set; } = new List<Paiement>();
    }
}

