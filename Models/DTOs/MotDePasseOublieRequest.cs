using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    public class MotDePasseOublieRequest
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; } = string.Empty;
    }
}

