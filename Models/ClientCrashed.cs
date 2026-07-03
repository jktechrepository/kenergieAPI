using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant une ligne de client qui a échoué lors de l'import Excel
    /// Permet de stocker les données des lignes échouées pour correction et réessai ultérieur
    /// </summary>
    public class ClientCrashed
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdClientCrashed { get; set; }

        /// <summary>
        /// Identifiant de la société pour laquelle l'import a été effectué
        /// </summary>
        [Required]
        public int IdSociete { get; set; }

        /// <summary>
        /// Numéro de ligne dans le fichier Excel (pour référence)
        /// </summary>
        [Required]
        public int NumeroLigne { get; set; }

        /// <summary>
        /// Nom du client (données brutes)
        /// </summary>
        [MaxLength(200)]
        public string? NomClient { get; set; }

        /// <summary>
        /// Adresse du client (données brutes)
        /// </summary>
        [MaxLength(500)]
        public string? AdresseClient { get; set; }

        /// <summary>
        /// Téléphone du client (données brutes)
        /// </summary>
        [MaxLength(20)]
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client (données brutes)
        /// </summary>
        [MaxLength(256)]
        public string? EmailClient { get; set; }

        /// <summary>
        /// Genre du client (données brutes)
        /// </summary>
        [MaxLength(10)]
        public string? GenreClient { get; set; }

        /// <summary>
        /// Code consommateur (données brutes)
        /// </summary>
        [MaxLength(100)]
        public string? CodeCons { get; set; }

        /// <summary>
        /// Libellés des usages (format: "Usage1, Usage2" ou JSON)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? LibelleUsage { get; set; }

        /// <summary>
        /// Toutes les données brutes en JSON (pour référence complète)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? DonneesBrutesJson { get; set; }

        /// <summary>
        /// Message d'erreur détaillé
        /// </summary>
        [Required]
        [Column(TypeName = "TEXT")]
        public string MessageErreur { get; set; } = string.Empty;

        /// <summary>
        /// Type d'erreur (VALIDATION, DATABASE, USAGE, EMAIL, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? TypeErreur { get; set; }

        /// <summary>
        /// Liste des erreurs en JSON (pour plusieurs erreurs)
        /// </summary>
        [Column(TypeName = "TEXT")]
        public string? ErreursJson { get; set; }

        /// <summary>
        /// Statut de la ligne échouée
        /// EN_ATTENTE : En attente de correction
        /// CORRIGE : Corrigé et prêt à être réessayé
        /// IGNORE : Ignoré/désactivé
        /// </summary>
        [MaxLength(20)]
        public string Statut { get; set; } = "EN_ATTENTE";

        /// <summary>
        /// Identifiant du client créé si la correction a réussi
        /// </summary>
        public int? IdClientCree { get; set; }

        /// <summary>
        /// Date de création de l'enregistrement
        /// </summary>
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de correction (quand la ligne a été corrigée)
        /// </summary>
        [JsonIgnore]
        public DateTime? DateCorrection { get; set; }

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSociete")]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClientCree")]
        public Client? ClientCree { get; set; }
    }
}
