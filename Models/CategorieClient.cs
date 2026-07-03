using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant une catégorie de clients
    /// </summary>
    public class CategorieClient
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCategorie { get; set; }

        /// <summary>
        /// Nom de la catégorie
        /// </summary>
        [MaxLength(100)]
        public string? NomCategorie { get; set; }

        /// <summary>
        /// Description de la catégorie
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Statut de la catégorie (true = active, false = inactive)
        /// </summary>
        public bool? Statut { get; set; } = true;

        /// <summary>
        /// Identifiant de la société à laquelle appartient cette catégorie
        /// </summary>
        [Required]
        public int IdSociete { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        /// <summary>
        /// Collection des usages appartenant à cette catégorie (relation one-to-many)
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Usage>? Usages { get; set; } = new List<Usage>();
    }
}

