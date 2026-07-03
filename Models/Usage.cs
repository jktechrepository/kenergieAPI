using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant un usage (type d'utilisation)
    /// Un usage appartient à une catégorie de client
    /// </summary>
    public class Usage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsage { get; set; }

        /// <summary>
        /// Libellé de l'usage (ex: "Résidentiel", "Commercial", "Industriel")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'usage
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Statut de l'usage (true = actif, false = inactive)
        /// </summary>
        public bool? Statut { get; set; } = true;

        /// <summary>
        /// Identifiant de la catégorie de client à laquelle appartient cet usage
        /// Un usage appartient à une seule catégorie
        /// </summary>
        [Required]
        public int IdCategorieClient { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public CategorieClient? CategorieClient { get; set; }

        /// <summary>
        /// Collection des clients ayant cet usage (relation many-to-many)
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<ClientUsage>? ClientsUsages { get; set; } = new List<ClientUsage>();

        /// <summary>
        /// Collection des factures liées à cet usage
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Facture>? Factures { get; set; } = new List<Facture>();
    }
}
