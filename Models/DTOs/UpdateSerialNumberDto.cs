using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    public class UpdateSerialNumberDto
    {
        [Required(ErrorMessage = "Le numéro de série est obligatoire")]
        [MaxLength(100)]
        public string SerialNumber { get; set; } = string.Empty;
    }
}

