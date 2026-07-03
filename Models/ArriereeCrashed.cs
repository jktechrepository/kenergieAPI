using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant une ligne d'arriérée qui a échoué lors de l'import Excel
    /// Permet de stocker les données des arriérées échouées pour correction et réessai ultérieur
    /// </summary>
    public class ArriereeCrashed
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdArriereeCrashed { get; set; }

        /// <summary>
        /// Numéro de ligne dans le fichier Excel (pour référence)
        /// </summary>
        [Required]
        public int NumeroLigne { get; set; }

        /// <summary>
        /// Code consommateur (données brutes)
        /// </summary>
        [MaxLength(100)]
        public string? CodeCons { get; set; }

        /// <summary>
        /// Montant de l'arriérée (données brutes)
        /// </summary>
        [MaxLength(50)]
        public string? Montant { get; set; }

        /// <summary>
        /// Mois de l'arriérée (données brutes)
        /// </summary>
        [MaxLength(10)]
        public string? Mois { get; set; }

        /// <summary>
        /// Année de l'arriérée (données brutes)
        /// </summary>
        [MaxLength(10)]
        public string? Annees { get; set; }

        /// <summary>
        /// Identifiant du client si trouvé (peut être null si CodeCons non trouvé)
        /// </summary>
        public int? IdClient { get; set; }

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
        /// Type d'erreur (CODE_CONS_NOT_FOUND, VALIDATION, DATABASE, DUPLICATE, etc.)
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
        /// Identifiant de la ClientFacture créée si la correction a réussi
        /// </summary>
        public int? IdClientFactureCree { get; set; }

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
        [ForeignKey("IdClient")]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClientFactureCree")]
        public ClientFacture? ClientFactureCree { get; set; }
    }
}
