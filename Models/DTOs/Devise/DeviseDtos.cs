using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Devise
{
    public class CreateDeviseDto
    {
        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(3)]
        [MinLength(3)]
        public string CodeDevise { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Symbole { get; set; }

        public bool Statut { get; set; } = true;

        public bool EstDevisePrincipale { get; set; } = false;
    }

    public class UpdateDeviseDto
    {
        [Required]
        [MaxLength(100)]
        public string Libelle { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Symbole { get; set; }

        public bool Statut { get; set; } = true;

        public bool EstDevisePrincipale { get; set; } = false;
    }

    public class DeviseDto
    {
        public int IdDeviseMonetaire { get; set; }
        public int IdSociete { get; set; }
        public string CodeDevise { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public string? Symbole { get; set; }
        public bool Statut { get; set; }
        public bool EstDevisePrincipale { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class CreateTauxChangeDto
    {
        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(3)]
        [MinLength(3)]
        public string CodeDeviseSource { get; set; } = string.Empty;

        [Required]
        [MaxLength(3)]
        [MinLength(3)]
        public string CodeDeviseCible { get; set; } = string.Empty;

        [Required]
        [Range(typeof(decimal), "0.000001", "999999999999.999999")]
        public decimal Taux { get; set; }

        public DateTime? DateEffet { get; set; }
    }

    public class TauxChangeDto
    {
        public int IdTauxChange { get; set; }
        public int IdSociete { get; set; }
        public string CodeDeviseSource { get; set; } = string.Empty;
        public string CodeDeviseCible { get; set; } = string.Empty;
        public decimal Taux { get; set; }
        public DateTime DateEffet { get; set; }
        public DateTime DateCreation { get; set; }
    }

    public class PreviewConversionDto
    {
        public int IdSociete { get; set; }
        public string CodeDeviseSource { get; set; } = string.Empty;
        public string CodeDevisePrincipale { get; set; } = string.Empty;
        public DateTime DatePaiement { get; set; }
        public decimal Taux { get; set; }
        public decimal MontantSource { get; set; }
        public decimal MontantConverti { get; set; }
    }
}
