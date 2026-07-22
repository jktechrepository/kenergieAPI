using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Devise monétaire gérée par société.
    /// </summary>
    public class DeviseMonetaire
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDeviseMonetaire { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>
        /// Code ISO (3 caractères), ex. CDF, USD, EUR.
        /// </summary>
        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Symbole { get; set; }

        /// <summary>
        /// Devise active (true) ou désactivée (false).
        /// </summary>
        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSociete")]
        public Societe? Societe { get; set; }
    }
}
