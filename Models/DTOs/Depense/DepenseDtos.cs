using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Depense
{
    public class CreateDepenseDto
    {
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
        [Range(0.01, double.MaxValue)]
        public decimal Montant { get; set; }

        [MaxLength(3)]
        public string? CodeDeviseMontant { get; set; }

        [MaxLength(50)]
        public string? ModePaiement { get; set; }

        public DateTime? DateDepense { get; set; }

        public int? IdCabine { get; set; }

        public int? IdAxe { get; set; }
    }

    public class UpdateDepenseDto
    {
        public int? IdCategorieDepense { get; set; }

        [MaxLength(200)]
        public string? Libelle { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(200)]
        public string? Beneficiaire { get; set; }

        [MaxLength(100)]
        public string? ReferencePiece { get; set; }

        [MaxLength(50)]
        public string? ModePaiement { get; set; }

        public DateTime? DateDepense { get; set; }

        public int? IdCabine { get; set; }

        public int? IdAxe { get; set; }
    }

    public class AnnulerDepenseDto
    {
        [MaxLength(500)]
        public string? MotifAnnulation { get; set; }
    }

    public class DepenseResponseDto
    {
        public int IdDepense { get; set; }
        public int IdSociete { get; set; }
        public int? IdCategorieDepense { get; set; }
        public string? NomCategorie { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Beneficiaire { get; set; }
        public string? ReferencePiece { get; set; }
        public decimal Montant { get; set; }
        public string? CodeDeviseMontant { get; set; }
        public string? CodeDevisePrincipale { get; set; }
        public decimal? TauxVersDevisePrincipale { get; set; }
        public decimal? MontantDevisePrincipale { get; set; }
        public string? ModePaiement { get; set; }
        public DateTime DateDepense { get; set; }
        public string Statut { get; set; } = string.Empty;
        public int IdUtilisateurCreateur { get; set; }
        public string? NomCreateur { get; set; }
        public int? IdUtilisateurValidateur { get; set; }
        public string? NomValidateur { get; set; }
        public DateTime? DateValidation { get; set; }
        public int? IdCabine { get; set; }
        public int? IdAxe { get; set; }
        public string? MotifAnnulation { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class SyntheseDepenseDto
    {
        public decimal MontantTotal { get; set; }
        public int NombreDepenses { get; set; }
        public int NombreValidees { get; set; }
        public int NombreEnAttente { get; set; }
    }

    public class DepenseMoisResponseDto
    {
        public int Mois { get; set; }
        public int Annee { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public List<DepenseResponseDto> Depenses { get; set; } = new();
        public SyntheseDepenseDto SyntheseDepense { get; set; } = new();
    }
}
