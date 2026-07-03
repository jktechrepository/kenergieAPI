using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle pour suivre les statistiques de diffusion de factures
    /// </summary>
    public class DiffusionStatistique
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDiffusionStatistique { get; set; }

        /// <summary>
        /// Identifiant de la facture diffusée
        /// </summary>
        [Required]
        public int IdFacture { get; set; }

        /// <summary>
        /// Identifiant de la catégorie de clients
        /// </summary>
        [Required]
        public int IdCategorie { get; set; }

        /// <summary>
        /// Nombre total de clients dans la catégorie
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// Nombre de clients notifiés avec succès
        /// </summary>
        public int ClientsNotifies { get; set; }

        /// <summary>
        /// Nombre de clients en échec
        /// </summary>
        public int ClientsEchecs { get; set; }

        /// <summary>
        /// Statistiques par canal
        /// </summary>
        [MaxLength(1000)]
        public string? StatistiquesCanaux { get; set; } // JSON

        /// <summary>
        /// Date de début de la diffusion
        /// </summary>
        public DateTime DateDebut { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de fin de la diffusion
        /// </summary>
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Durée en secondes
        /// </summary>
        public double? DureeSecondes { get; set; }

        /// <summary>
        /// Statut de la diffusion (En cours, Terminée, Erreur)
        /// </summary>
        [MaxLength(20)]
        public string Statut { get; set; } = "En cours";

        /// <summary>
        /// Identifiant de l'utilisateur qui a lancé la diffusion
        /// </summary>
        public int? IdUtilisateurLanceur { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdFacture")]
        public Facture? Facture { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdCategorie")]
        public CategorieClient? CategorieClient { get; set; }
    }
}

