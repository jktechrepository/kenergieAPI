namespace Kenergie.Models.Configuration
{
    /// <summary>
    /// Options FlexPay (paiement électronique Mobile Money / carte).
    /// </summary>
    public class FlexPayOptions
    {
        public const string SectionName = "FlexPay";

        public bool Enabled { get; set; } = false;

        /// <summary>TTL hold / pending (minutes).</summary>
        public int HoldMinutes { get; set; } = 15;

        /// <summary>URL publique du callback (HTTPS). Ex: https://api.example.com/api/FlexPay/callback</summary>
        public string CallbackBaseUrl { get; set; } = string.Empty;

        public string MobileMoneyUrl { get; set; } = "https://backend.flexpay.cd/api/rest/v1/paymentService";

        public string CardPaymentUrl { get; set; } = "https://cardpayment.flexpay.cd/v1.1/pay";

        public string CheckTransactionUrl { get; set; } = "https://apicheck.flexpaie.com/api/rest/v1/check";

        /// <summary>Écart max accepté entre montant callback et montant attendu.</summary>
        public decimal MontantTolerance { get; set; } = 0.05m;

        /// <summary>
        /// Mobile Money : exiger providerReference dans le callback avant finalisation.
        /// </summary>
        public bool RequireProviderReferenceForMobileMoney { get; set; } = true;

        /// <summary>
        /// Délai minimal (secondes) entre initiation et callback avant finalisation (0 = désactivé).
        /// </summary>
        public int MinSecondsBeforeFinalize { get; set; } = 0;

        /// <summary>Forcer l'URL de production en développement (tests).</summary>
        public bool ForceProductionCallbackInDev { get; set; } = false;
    }
}
