using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant un axe
    /// </summary>
    public class Axe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdAxe { get; set; }

        /// <summary>
        /// Nom de l'axe
        /// </summary>
        [MaxLength(200)]
        public string? NomAxe { get; set; }

        /// <summary>
        /// Code unique de l'axe (utilisé pour générer CodeCons)
        /// </summary>
        [MaxLength(50)]
        public string? CodeAxe { get; set; }

        /// <summary>
        /// Description de l'axe
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Identifiant de la cabine à laquelle appartient cet axe
        /// </summary>
        [Required]
        public int IdCabine { get; set; }

        /// <summary>
        /// Statut de l'axe (actif/inactif) pour soft delete
        /// </summary>
        public bool Statut { get; set; } = true;

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Cabine? Cabine { get; set; }
    }
}
