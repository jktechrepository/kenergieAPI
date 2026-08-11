using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.FlexPay
{
    public class CreateInfoPaiementSocieteDto
    {
        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(100)]
        public string CodeMarchand { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ApiToken { get; set; } = string.Empty;

        public bool ActifMobileMoney { get; set; } = true;

        public bool ActifCarteBancaire { get; set; } = false;

        public bool Statut { get; set; } = true;
    }

    public class UpdateInfoPaiementSocieteDto
    {
        [Required]
        [MaxLength(100)]
        public string CodeMarchand { get; set; } = string.Empty;

        /// <summary>Si null ou vide, le token existant est conservé.</summary>
        [MaxLength(500)]
        public string? ApiToken { get; set; }

        public bool ActifMobileMoney { get; set; } = true;

        public bool ActifCarteBancaire { get; set; } = false;

        public bool Statut { get; set; } = true;
    }

    public class InfoPaiementSocieteDto
    {
        public int IdInfoPaiementSociete { get; set; }
        public int IdSociete { get; set; }
        public string CodeMarchand { get; set; } = string.Empty;
        public bool HasApiToken { get; set; }
        public bool ActifMobileMoney { get; set; }
        public bool ActifCarteBancaire { get; set; }
        public bool Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class InitierPaiementElectroniqueDto
    {
        /// <summary>Requis pour Super-Admin si la claim société est absente.</summary>
        public int? IdSociete { get; set; }

        public int? IdClientFacture { get; set; }
        public int? IdFacture { get; set; }
        public int? IdClient { get; set; }

        /// <summary>MOBILE_MONEY ou CARTE_BANCAIRE</summary>
        [Required]
        public string Methode { get; set; } = "MOBILE_MONEY";

        /// <summary>Obligatoire pour Mobile Money.</summary>
        [MaxLength(20)]
        public string? Telephone { get; set; }

        /// <summary>CDF ou USD. Défaut = devise de la facture si CDF/USD.</summary>
        [MaxLength(3)]
        public string? CodeDevisePaiement { get; set; }

        /// <summary>Montant partiel optionnel (≤ montant dû). Sinon = MontantDu.</summary>
        [Range(0.01, double.MaxValue)]
        public decimal? Montant { get; set; }
    }

    public class PaiementElectroniquePendingDto
    {
        public int IdPending { get; set; }
        public string? OrderNumberFlexPay { get; set; }
        public string ReferenceFlexPay { get; set; } = string.Empty;
        public decimal MontantFlexPay { get; set; }
        public string CodeDevisePaiement { get; set; } = "CDF";
        public string Methode { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime HoldExpireAt { get; set; }
        public string? PaymentUrl { get; set; }
        public bool FlexPayAccepted { get; set; }
        public int? IdPaiementFinalise { get; set; }
        /// <summary>True uniquement si statut == Finalise.</summary>
        public bool EstConfirme { get; set; }
        public DateTime? DateFinalisation { get; set; }
        public string? Message { get; set; }
    }

    public class FlexPayCallbackDto
    {
        public string? Code { get; set; }
        public string? Reference { get; set; }
        public string? ProviderReference { get; set; }
        public string? OrderNumber { get; set; }
        public string? Amount { get; set; }
        public string? AmountCustomer { get; set; }
        public string? Phone { get; set; }
        public string? Currency { get; set; }
        public string? CreatedAt { get; set; }
        public string? Channel { get; set; }
    }

    public class FlexPayCallbackResponseDto
    {
        public bool Success { get; set; }
        public bool AlreadyProcessed { get; set; }
        public string? Message { get; set; }
        public int? IdPaiement { get; set; }
    }

    public class FlexPayInitResult
    {
        public bool Accepted { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? OrderNumber { get; set; }
        public string? PaymentUrl { get; set; }
    }

    public class FlexPayCheckResult
    {
        public bool Success { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsPending { get; set; }
        public string Code { get; set; } = "1";
        public string? TransactionStatus { get; set; }
        public string? ProviderReference { get; set; }
        public string? OrderNumber { get; set; }
        public string? Reference { get; set; }
        public string? Amount { get; set; }
        public string? Currency { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RawJson { get; set; } = string.Empty;
    }
}
