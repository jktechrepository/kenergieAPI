using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kenergie.Models
{
    /// <summary>
    /// Modèle représentant un signalement de panne déposé par un client.
    /// </summary>
    public class PanneSignalement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPanneSignalement { get; set; }

        /// <summary>
        /// Description détaillée du signalement.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Statut du signalement (true = actif/ouvert, false = clôturé).
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Type de panne ou problème signalé
        /// </summary>
        [MaxLength(200)]
        public string? TypePanne { get; set; }

        /// <summary>
        /// Niveau d'importance du signalement (ex: Faible, Moyen, Élevé, Critique)
        /// </summary>
        [MaxLength(50)]
        public string? NiveauImportance { get; set; }

        /// <summary>
        /// Risques principaux identifiés liés à ce signalement
        /// </summary>
        [MaxLength(500)]
        public string? RisquesPrincipaux { get; set; }
    }
}

