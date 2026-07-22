using System.Text.RegularExpressions;

namespace Kenergie.Helpers
{
    /// <summary>
    /// Normalisation et détection CASH vs FlexPay.
    /// </summary>
    public static class MethodePaiementHelper
    {
        public static string Normalize(string? methode)
        {
            if (string.IsNullOrWhiteSpace(methode))
                return string.Empty;

            var m = methode.Trim().ToUpperInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");

            return m switch
            {
                "ORANGE_MONEY" or "AIRTEL_MONEY" or "MPESA" or "MOBILEMONEY" or "MOBILE_MONEY" or "MM"
                    => Models.MethodeFlexPay.MobileMoney,
                "CARD" or "CARTE" or "VISA" or "MASTERCARD" or "CARTE_BANCAIRE" or "BANQUE"
                    => Models.MethodeFlexPay.CarteBancaire,
                _ => m
            };
        }

        public static bool IsFlexPay(string? methode)
        {
            var n = Normalize(methode);
            return n == Models.MethodeFlexPay.MobileMoney || n == Models.MethodeFlexPay.CarteBancaire;
        }

        public static bool IsMobileMoney(string? methode) =>
            Normalize(methode) == Models.MethodeFlexPay.MobileMoney;

        public static bool IsCarte(string? methode) =>
            Normalize(methode) == Models.MethodeFlexPay.CarteBancaire;

        public static string ToDisplayMethode(string? methode)
        {
            return Normalize(methode) switch
            {
                Models.MethodeFlexPay.MobileMoney => "Mobile Money",
                Models.MethodeFlexPay.CarteBancaire => "Carte",
                _ => methode?.Trim() ?? string.Empty
            };
        }

        /// <summary>Normalise un téléphone RDC en 243XXXXXXXXX si possible.</summary>
        public static string? NormalizePhoneRdCongo(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            var digits = Regex.Replace(phone, @"[^\d]", "");
            if (digits.StartsWith("0") && digits.Length == 10)
                digits = "243" + digits.Substring(1);
            if (digits.StartsWith("243") && digits.Length >= 12)
                return digits;
            return digits.Length >= 9 ? digits : null;
        }
    }

    public static class FlexPayUrlHelper
    {
        public static string ResolveCallbackUrl(string callbackBaseUrl)
        {
            var baseUrl = (callbackBaseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return string.Empty;

            if (baseUrl.EndsWith("/callback", StringComparison.OrdinalIgnoreCase))
                return baseUrl;

            if (baseUrl.Contains("/api/FlexPay", StringComparison.OrdinalIgnoreCase))
                return baseUrl.TrimEnd('/') + "/callback";

            return baseUrl + "/api/FlexPay/callback";
        }

        public static string ResolveApproveUrl(string callbackUrl) =>
            ReplaceTerminal(callbackUrl, "approve");

        public static string ResolveCancelUrl(string callbackUrl) =>
            ReplaceTerminal(callbackUrl, "cancel");

        public static string ResolveDeclineUrl(string callbackUrl) =>
            ReplaceTerminal(callbackUrl, "decline");

        private static string ReplaceTerminal(string callbackUrl, string segment)
        {
            if (string.IsNullOrWhiteSpace(callbackUrl))
                return string.Empty;
            if (callbackUrl.EndsWith("/callback", StringComparison.OrdinalIgnoreCase))
                return callbackUrl[..^"/callback".Length] + "/" + segment;
            return callbackUrl.TrimEnd('/') + "/" + segment;
        }

        public static string? ResolvePaymentUrl(string? paymentUrl, string? redirectUrl, string? url) =>
            !string.IsNullOrWhiteSpace(paymentUrl) ? paymentUrl
            : !string.IsNullOrWhiteSpace(redirectUrl) ? redirectUrl
            : !string.IsNullOrWhiteSpace(url) ? url
            : null;
    }
}
