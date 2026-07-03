using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant un client
    /// </summary>
    public class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Adresse complète du client
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string AdresseClient { get; set; } = string.Empty;

        /// <summary>
        /// Numéro de téléphone du client
        /// </summary>
        [MaxLength(20)]
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client
        /// </summary>
        [EmailAddress(ErrorMessage = "L'email doit être valide")]
        [MaxLength(256)]
        public string? EmailClient { get; set; }

        /// <summary>
        /// Genre du client (M, F, Autre)
        /// </summary>
        [MaxLength(10)]
        public string? GenreClient { get; set; }

        /// <summary>
        /// Code consommateur du client
        /// </summary>
        [MaxLength(100)]
        public string? CodeCons { get; set; }

        /// <summary>
        /// Statut du client (actif/inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Indique si le client est actif (champ métier, par défaut vrai)
        /// </summary>
        public bool IsActif { get; set; } = true;

        /// <summary>
        /// Date de création du client
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification (pour delta sync)
        /// </summary>
        [JsonIgnore]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Indique si le client est supprimé (soft delete pour sync)
        /// </summary>
        [JsonIgnore]
        public bool? IsDeleted { get; set; } = false;

        /// <summary>
        /// Identifiant de l'axe auquel appartient ce client
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public int? IdAxe { get; set; }

        // Navigation properties
        /// <summary>
        /// Collection des usages du client (relation many-to-many via ClientUsage)
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public ICollection<ClientUsage>? ClientsUsages { get; set; } = new List<ClientUsage>();

        /// <summary>
        /// Axe auquel appartient ce client
        /// </summary>
        [JsonIgnore]
        [ValidateNever]
        public Axe? Axe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Utilisateur>? Utilisateurs { get; set; } = new List<Utilisateur>();
    }
}

