namespace Kenergie.Services
{
    /// <summary>
    /// Éligibilité à la facturation selon la date effective de démarrage du client.
    /// Date effective = DateDerniereReactivation ?? DateCreation.
    /// Règles :
    /// - aucune facture pour une période strictement antérieure au mois de démarrage ;
    /// - démarrage le 15 ou après dans le mois M → pas de facture de M ;
    /// - les mois suivants sont autorisés.
    /// </summary>
    public static class FactureBillingEligibilityHelper
    {
        public const int RegistrationBillingCutoffDay = 15;

        /// <summary>
        /// Date à partir de laquelle le client peut être facturé.
        /// </summary>
        public static DateTime GetEffectiveBillingStartDate(
            DateTime dateCreation,
            DateTime? dateDerniereReactivation)
        {
            var creation = dateCreation.Date;
            if (!dateDerniereReactivation.HasValue)
                return creation;

            var reactivation = dateDerniereReactivation.Value.Date;
            return reactivation > creation ? reactivation : creation;
        }

        /// <summary>
        /// Indique si le client peut recevoir une facture pour la période donnée.
        /// </summary>
        public static bool IsClientEligibleForBillingPeriod(
            DateTime dateCreation,
            int moisEmission,
            int anneesEmission)
        {
            return IsClientEligibleForBillingPeriod(dateCreation, null, moisEmission, anneesEmission);
        }

        public static bool IsClientEligibleForBillingPeriod(
            DateTime dateCreation,
            DateTime? dateDerniereReactivation,
            int moisEmission,
            int anneesEmission)
        {
            var effectiveStart = GetEffectiveBillingStartDate(dateCreation, dateDerniereReactivation);
            var billingPeriod = new DateTime(anneesEmission, moisEmission, 1);
            var startPeriod = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);

            if (billingPeriod < startPeriod)
                return false;

            if (billingPeriod > startPeriod)
                return true;

            return effectiveStart.Day < RegistrationBillingCutoffDay;
        }

        /// <summary>
        /// Surcharge acceptant le mois sous forme de chaîne ("05", "5", etc.).
        /// </summary>
        public static bool IsClientEligibleForBillingPeriod(
            DateTime dateCreation,
            string? mois,
            int anneesEmission)
        {
            return IsClientEligibleForBillingPeriod(dateCreation, null, mois, anneesEmission);
        }

        public static bool IsClientEligibleForBillingPeriod(
            DateTime dateCreation,
            DateTime? dateDerniereReactivation,
            string? mois,
            int anneesEmission)
        {
            if (!TryParseMoisNumero(mois, out var moisNumero))
                return true;

            return IsClientEligibleForBillingPeriod(
                dateCreation, dateDerniereReactivation, moisNumero, anneesEmission);
        }

        /// <summary>
        /// Message d'erreur métier pour une création refusée.
        /// </summary>
        public static string BuildIneligibilityMessage(
            DateTime dateCreation,
            string? mois,
            int anneesEmission)
        {
            return BuildIneligibilityMessage(dateCreation, null, mois, anneesEmission);
        }

        public static string BuildIneligibilityMessage(
            DateTime dateCreation,
            DateTime? dateDerniereReactivation,
            string? mois,
            int anneesEmission)
        {
            var moisAffiche = string.IsNullOrWhiteSpace(mois) ? "?" : mois.Trim();
            var effectiveStart = GetEffectiveBillingStartDate(dateCreation, dateDerniereReactivation);
            var usesReactivation = dateDerniereReactivation.HasValue
                && dateDerniereReactivation.Value.Date > dateCreation.Date;

            if (TryParseMoisNumero(mois, out var moisNumero))
            {
                var billingPeriod = new DateTime(anneesEmission, moisNumero, 1);
                var startPeriod = new DateTime(effectiveStart.Year, effectiveStart.Month, 1);

                if (billingPeriod < startPeriod)
                {
                    if (usesReactivation)
                    {
                        return $"Ce client a été réactivé le {effectiveStart:dd/MM/yyyy}. " +
                               $"Il ne peut pas recevoir une facture pour une période antérieure à sa réactivation ({moisAffiche}/{anneesEmission}).";
                    }

                    return $"Ce client a été enregistré le {dateCreation:dd/MM/yyyy}. " +
                           $"Il ne peut pas recevoir une facture pour une période antérieure à son enregistrement ({moisAffiche}/{anneesEmission}).";
                }
            }

            if (usesReactivation)
            {
                return $"Ce client a été réactivé le {effectiveStart:dd/MM/yyyy} (à partir du {RegistrationBillingCutoffDay} du mois). " +
                       $"Il ne peut pas recevoir la facture de {moisAffiche}/{anneesEmission}.";
            }

            return $"Ce client a été enregistré le {dateCreation:dd/MM/yyyy} (à partir du {RegistrationBillingCutoffDay} du mois). " +
                   $"Il ne peut pas recevoir la facture de {moisAffiche}/{anneesEmission}.";
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
