using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Configuration marchand FlexPay par société.
    /// </summary>
    public class InfoPaiementSociete
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInfoPaiementSociete { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        [MaxLength(100)]
        public string CodeMarchand { get; set; } = string.Empty;

        /// <summary>Token API FlexPay — ne jamais exposer en clair dans les réponses admin.</summary>
        [Required]
        [MaxLength(500)]
        [JsonIgnore]
        public string ApiToken { get; set; } = string.Empty;

        public bool ActifMobileMoney { get; set; } = true;

        public bool ActifCarteBancaire { get; set; } = false;

        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSociete")]
        public Societe? Societe { get; set; }
    }

    /// <summary>
    /// Statuts d'un paiement électronique en attente.
    /// </summary>
    public static class StatutPaiementElectronique
    {
        public const string EnAttente = "EnAttente";
        public const string Finalise = "Finalise";
        public const string Echec = "Echec";
        public const string Expire = "Expire";
    }

    /// <summary>
    /// Méthodes FlexPay normalisées.
    /// </summary>
    public static class MethodeFlexPay
    {
        public const string MobileMoney = "MOBILE_MONEY";
        public const string CarteBancaire = "CARTE_BANCAIRE";
    }

    /// <summary>
    /// Paiement électronique initié, non encore confirmé par callback.
    /// </summary>
    public class PaiementElectroniqueEnAttente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPaiementElectroniqueEnAttente { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdClient { get; set; }

        public int? IdClientFacture { get; set; }

        public int? IdFacture { get; set; }

        public int? IdUtilisateur { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [MaxLength(3)]
        public string CodeDevisePaiement { get; set; } = "CDF";

        [MaxLength(30)]
        public string Methode { get; set; } = MethodeFlexPay.MobileMoney;

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OrderNumber { get; set; }

        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        [MaxLength(20)]
        public string Statut { get; set; } = StatutPaiementElectronique.EnAttente;

        /// <summary>Idempotence : renseigné après finalisation.</summary>
        public int? IdPaiementFinalise { get; set; }

        public DateTime HoldExpireAt { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateFinalisation { get; set; }

        [MaxLength(500)]
        public string? MessageErreur { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdSociete")]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClient")]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdClientFacture")]
        public ClientFacture? ClientFacture { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdFacture")]
        public Facture? Facture { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdPaiementFinalise")]
        public Paiement? PaiementFinalise { get; set; }
    }

    /// <summary>
    /// Trace d'une transaction FlexPay.
    /// </summary>
    public class TransactionFlexPay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdTransactionFlexPay { get; set; }

        public int IdPaiementElectroniqueEnAttente { get; set; }

        public int IdSociete { get; set; }

        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OrderNumber { get; set; }

        /// <summary>"1" = MM, "2" = Carte</summary>
        [MaxLength(5)]
        public string TypeFlexPay { get; set; } = "1";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        public int NombreCallbacks { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdPaiementElectroniqueEnAttente")]
        public PaiementElectroniqueEnAttente? Pending { get; set; }
    }

    /// <summary>
    /// Audit d'un webhook FlexPay.
    /// </summary>
    public class CallbackFlexPay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCallbackFlexPay { get; set; }

        [MaxLength(100)]
        public string? OrderNumber { get; set; }

        [MaxLength(100)]
        public string? Reference { get; set; }

        [MaxLength(20)]
        public string? Code { get; set; }

        public string? PayloadJson { get; set; }

        [MaxLength(1000)]
        public string? HeadersJson { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public bool TraiteAvecSucces { get; set; }

        [MaxLength(500)]
        public string? MessageTraitement { get; set; }

        public DateTime DateReception { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Hold anti-doublon pendant TTL.
    /// </summary>
    public class PaiementHold
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPaiementHold { get; set; }

        public int IdSociete { get; set; }

        /// <summary>Clé métier : CF-{idClientFacture} ou F-{idFacture}-C-{idClient}</summary>
        [MaxLength(100)]
        public string CleRessource { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telephone { get; set; }

        public int? IdPaiementElectroniqueEnAttente { get; set; }

        public DateTime ExpireAt { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public bool EstLibere { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdPaiementElectroniqueEnAttente")]
        public PaiementElectroniqueEnAttente? Pending { get; set; }
    }
}
