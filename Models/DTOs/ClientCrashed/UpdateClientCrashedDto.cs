using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.ClientCrashed
{
    /// <summary>
    /// DTO pour mettre à jour une ligne ClientCrashed
    /// </summary>
    public class UpdateClientCrashedDto
    {
        [MaxLength(200)]
        public string? NomClient { get; set; }

        [MaxLength(500)]
        public string? AdresseClient { get; set; }

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [MaxLength(256)]
        [EmailAddress(ErrorMessage = "L'email doit être valide")]
        public string? EmailClient { get; set; }

        [MaxLength(10)]
        public string? GenreClient { get; set; }

        [MaxLength(100)]
        public string? CodeCons { get; set; }

        public string? LibelleUsage { get; set; }

        [MaxLength(20)]
        public string? Statut { get; set; } // EN_ATTENTE, CORRIGE, IGNORE
    }
}
