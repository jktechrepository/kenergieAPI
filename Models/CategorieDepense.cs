using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    public class CategorieDepense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCategorieDepense { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(100)]
        public string NomCategorie { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool Statut { get; set; } = true;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ForeignKey(nameof(IdSociete))]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        public ICollection<Depense>? Depenses { get; set; }
    }
}
