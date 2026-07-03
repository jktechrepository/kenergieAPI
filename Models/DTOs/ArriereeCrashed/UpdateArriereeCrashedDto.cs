using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.ArriereeCrashed
{
    /// <summary>
    /// DTO pour mettre à jour une ligne ArriereeCrashed
    /// </summary>
    public class UpdateArriereeCrashedDto
    {
        /// <summary>
        /// Code consommateur corrigé
        /// </summary>
        [MaxLength(100)]
        public string? CodeCons { get; set; }

        /// <summary>
        /// Montant corrigé (doit être un nombre valide)
        /// </summary>
        [MaxLength(50)]
        public string? Montant { get; set; }

        /// <summary>
        /// Mois corrigé (1-12)
        /// </summary>
        [MaxLength(10)]
        public string? Mois { get; set; }

        /// <summary>
        /// Année corrigée
        /// </summary>
        [MaxLength(10)]
        public string? Annees { get; set; }

        /// <summary>
        /// Identifiant du client (si connu)
        /// </summary>
        public int? IdClient { get; set; }

        /// <summary>
        /// Statut (EN_ATTENTE, CORRIGE, IGNORE)
        /// </summary>
        [MaxLength(20)]
        public string? Statut { get; set; }
    }
}
