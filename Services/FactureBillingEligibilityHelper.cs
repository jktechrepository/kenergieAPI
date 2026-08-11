namespace Kenergie.Services
{
    /// <summary>
    /// Éligibilité à la facturation selon la date d'enregistrement du client.
    /// Règle : un client enregistré le 15 ou après dans le mois M ne reçoit pas la facture de M.
    /// </summary>
    public static class FactureBillingEligibilityHelper
    {
        public const int RegistrationBillingCutoffDay = 15;

        /// <summary>
        /// Indique si le client peut recevoir une facture pour la période donnée.
        /// </summary>
        public static bool IsClientEligibleForBillingPeriod(
            DateTime dateCreation,
            int moisEmission,
            int anneesEmission)
        {
            var registrationDate = dateCreation.Date;

            if (registrationDate.Year != anneesEmission || registrationDate.Month != moisEmission)
                return true;

            return registrationDate.Day < RegistrationBillingCutoffDay;
        }

        /// <summary>
        /// Surcharge acceptant le mois sous forme de chaîne ("05", "5", etc.).
        /// </summary>
        public static bool IsClientEligibleForBillingPeriod(
            DateTime dateCreation,
            string? mois,
            int anneesEmission)
        {
            if (!TryParseMoisNumero(mois, out var moisNumero))
                return true;

            return IsClientEligibleForBillingPeriod(dateCreation, moisNumero, anneesEmission);
        }

        /// <summary>
        /// Message d'erreur métier pour une création refusée.
        /// </summary>
        public static string BuildIneligibilityMessage(
            DateTime dateCreation,
            string? mois,
            int anneesEmission)
        {
            return $"Ce client a été enregistré le {dateCreation:dd/MM/yyyy} (à partir du {RegistrationBillingCutoffDay} du mois). " +
                   $"Il ne peut pas recevoir la facture de {mois}/{anneesEmission}.";
        }

        private static bool TryParseMoisNumero(string? mois, out int numero)
        {
            numero = 0;
            if (string.IsNullOrWhiteSpace(mois))
                return false;

            var trimmed = mois.Trim();
            if (trimmed.Length == 1 && char.IsDigit(trimmed[0]))
            {
                numero = int.Parse(trimmed);
                return numero is >= 1 and <= 12;
            }

            if (trimmed.Length == 2 && char.IsDigit(trimmed[0]) && char.IsDigit(trimmed[1]))
            {
                numero = int.Parse(trimmed);
                return numero is >= 1 and <= 12;
            }

            return false;
        }
    }
}
