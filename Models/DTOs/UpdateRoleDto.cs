using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    public class UpdateRoleDto
    {
        [Required]
        public int IdRole { get; set; }
        
        [Required]
        [StringLength(50)]
        public string? Nom { get; set; }
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        [Range(1, 10)]
        public int Niveau { get; set; }
    }
}
