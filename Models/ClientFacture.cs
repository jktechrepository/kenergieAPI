using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant la relation entre un Client et une Facture
    /// Permet de gérer les arriérés pré-existants et d'optimiser les calculs
    /// </summary>
    public class ClientFacture
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdClientFacture { get; set; }

        /// <summary>
        /// Identifiant de la facture (NULL pour arriérés pré-existants)
        /// </summary>
        public int? IdFacture { get; set; }

        /// <summary>
        /// Identifiant du client associé à cette facture client
        /// </summary>
        [Required]
        public int IdClient { get; set; }

        /// <summary>
        /// Montant total de la facture pour ce client (déjà multiplié par nombreBatiment)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Montant { get; set; }

        /// <summary>
        /// Snapshot du nombre de bâtiments au moment de la facture (évite les recalculs)
        /// </summary>
        public int? nombreBatiment { get; set; }

        /// <summary>
        /// Montant déjà payé par ce client pour cette facture (pré-calculé pour performance)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantPaye { get; set; }

        /// <summary>
        /// Montant restant dû par ce client pour cette facture (pré-calculé pour performance)
        /// Calculé : Montant - MontantPaye
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantDu { get; set; }

        /// <summary>
        /// Mois d'émission (format: "01", "02", ..., "12" ou "Janvier", "Février", etc.)
        /// </summary>
        [MaxLength(20)]
        public string? Mois { get; set; }

        /// <summary>
        /// Année d'émission
        /// </summary>
        [Range(2000, 2100)]
        public int? Annees { get; set; }

        /// <summary>
        /// Date d'émission de la facture (plus fiable que Mois/Annees pour tri et filtrage)
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateEmission { get; set; }

        /// <summary>
        /// Indique si c'est un arriéré pré-existant (avant l'arrivée du système informatisé)
        /// </summary>
        public bool EstArrierePreExistant { get; set; } = false;

        /// <summary>
        /// Description/libellé pour les arriérés pré-existants
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Statut de la facture client (actif/inactif) - permet le soft delete
        /// </summary>
        public bool Statut { get; set; } = true;

        // Attributs Techniques
        /// <summary>
        /// Date de création de l'enregistrement
        /// </summary>
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

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
        [ForeignKey("IdFacture")]
        public Facture? Facture { get; set; }
    }
}
