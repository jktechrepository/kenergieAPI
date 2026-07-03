using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant une cabine
    /// </summary>
    public class Cabine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCabine { get; set; }

        /// <summary>
        /// Nom de la cabine
        /// </summary>
        [MaxLength(200)]
        public string? Nom { get; set; }

        /// <summary>
        /// Code unique de la cabine (utilisé pour générer CodeCons)
        /// </summary>
        [MaxLength(50)]
        public string? CodeCabine { get; set; }

        /// <summary>
        /// Adresse de la cabine
        /// </summary>
        [MaxLength(500)]
        public string? Adresse { get; set; }

        /// <summary>
        /// Description de la cabine
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Identifiant de la société à laquelle appartient cette cabine
        /// </summary>
        [Required]
        public int IdSociete { get; set; }

        /// <summary>
        /// Statut de la cabine (actif/inactif) pour soft delete
        /// </summary>
        public bool Statut { get; set; } = true;

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }
    }
}
