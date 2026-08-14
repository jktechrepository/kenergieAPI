using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Depense
{
    public class CreateCategorieDepenseDto
    {
        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(100)]
        public string NomCategorie { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class UpdateCategorieDepenseDto
    {
        [MaxLength(100)]
        public string? NomCategorie { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool? Statut { get; set; }
    }

    public class CategorieDepenseResponseDto
    {
        public int IdCategorieDepense { get; set; }
        public int IdSociete { get; set; }
        public string NomCategorie { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool Statut { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
