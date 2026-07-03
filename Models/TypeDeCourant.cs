using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant un type de courant électrique
    /// Permet de différencier la tarification selon le type de distribution
    /// </summary>
    public class TypeDeCourant
    {
        [Key]
        public int IdTypeDeCourant { get; set; }

        /// <summary>
        /// Libellé du type de courant (ex: "Permanent", "Non Permanent")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;

        /// <summary>
        /// Description détaillée du type de courant
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Statut du type de courant (true = actif, false = inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Date de création du type de courant
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        // Navigation properties
        /// <summary>
        /// Lignes client–usage associées à ce type de courant
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<ClientUsage>? ClientUsages { get; set; } = new List<ClientUsage>();

        /// <summary>
        /// Collection des factures liées à ce type de courant
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Facture>? Factures { get; set; } = new List<Facture>();
    }
}
