using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    public static class DepenseStatuts
    {
        public const string EnAttente = "EnAttente";
        public const string Validee = "Validee";
        public const string Annulee = "Annulee";
    }

    public class Depense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDepense { get; set; }

        [Required]
        public int IdSociete { get; set; }

        public int? IdCategorieDepense { get; set; }

        [Required]
        [MaxLength(200)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Beneficiaire { get; set; }

        [MaxLength(100)]
        public string? ReferencePiece { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [MaxLength(3)]
        public string? CodeDeviseMontant { get; set; }

        [MaxLength(3)]
        public string? CodeDevisePrincipale { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TauxVersDevisePrincipale { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MontantDevisePrincipale { get; set; }

        [MaxLength(50)]
        public string? ModePaiement { get; set; }

        [Required]
        public DateTime DateDepense { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string Statut { get; set; } = DepenseStatuts.EnAttente;

        [Required]
        public int IdUtilisateurCreateur { get; set; }

        public int? IdUtilisateurValidateur { get; set; }

        public DateTime? DateValidation { get; set; }

        public int? IdCabine { get; set; }

        public int? IdAxe { get; set; }

        [MaxLength(500)]
        public string? MotifAnnulation { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdSociete))]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdCategorieDepense))]
        public CategorieDepense? CategorieDepense { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdUtilisateurCreateur))]
        public Utilisateur? UtilisateurCreateur { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdUtilisateurValidateur))]
        public Utilisateur? UtilisateurValidateur { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdCabine))]
        public Cabine? Cabine { get; set; }

        [JsonIgnore]
        [ForeignKey(nameof(IdAxe))]
        public Axe? Axe { get; set; }
    }
}
