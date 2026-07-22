using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Taux de change entre deux devises pour une société, avec date d'effet.
    /// </summary>
    public class TauxChange
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTauxChange { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDeviseSource { get; set; } = string.Empty;

        [Required]
        [MaxLength(3)]
        public string CodeDeviseCible { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,6)")]
        public decimal Taux { get; set; }

        [Required]
        public DateTime DateEffet { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSociete")]
        public Societe? Societe { get; set; }
    }
}
