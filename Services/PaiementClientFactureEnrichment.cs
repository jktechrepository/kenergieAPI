using Kenergie.Models;

namespace Kenergie.Services
{
    /// <summary>
    /// Alignement lecture Paiement ↔ ClientFacture (mêmes montants que GET .../client/{id}/arrieres).
    /// </summary>
    public static class PaiementClientFactureEnrichment
    {
        public static void Apply(Paiement paiement, ClientFacture clientFacture)
        {
            paiement.IdClientFacture = clientFacture.IdClientFacture;
            paiement.MontantAPaye = clientFacture.Montant;
            paiement.ResteAPaye = clientFacture.MontantDu;
            paiement.MontantAPayeDevisePrincipale = clientFacture.MontantDevisePrincipale;
            paiement.ResteAPayeDevisePrincipale = clientFacture.MontantDuDevisePrincipale;
        }

        /// <summary>
        /// Recalcule les montants consolidés ClientFacture à partir du taux snapshoté.
        /// </summary>
        public static void RecalculateDevisePrincipaleBalances(ClientFacture clientFacture)
        {
            var taux = clientFacture.TauxVersDevisePrincipale ?? 1m;
            clientFacture.MontantPayeDevisePrincipale = Math.Round(
                (clientFacture.MontantPaye ?? 0) * taux, 2, MidpointRounding.AwayFromZero);
            clientFacture.MontantDuDevisePrincipale = Math.Round(
                (clientFacture.MontantDu ?? 0) * taux, 2, MidpointRounding.AwayFromZero);
            if (clientFacture.Montant.HasValue && !clientFacture.MontantDevisePrincipale.HasValue)
            {
                clientFacture.MontantDevisePrincipale = Math.Round(
                    clientFacture.Montant.Value * taux, 2, MidpointRounding.AwayFromZero);
            }
        }

        public static ClientFacture? Resolve(
            Paiement paiement,
            IReadOnlyDictionary<int, ClientFacture> byId,
            IReadOnlyDictionary<(int IdClient, int IdFacture), ClientFacture> byClientAndFacture)
        {
            if (paiement.IdClientFacture.HasValue &&
                byId.TryGetValue(paiement.IdClientFacture.Value, out var fromId))
            {
                return fromId;
            }

            if (paiement.IdFacture.HasValue &&
                paiement.IdClient.HasValue &&
                byClientAndFacture.TryGetValue((paiement.IdClient.Value, paiement.IdFacture.Value), out var fromPair))
            {
                return fromPair;
            }

            return null;
        }

        public static IReadOnlyDictionary<(int IdClient, int IdFacture), ClientFacture> IndexByClientAndFacture(
            IEnumerable<ClientFacture> clientFactures)
        {
            return clientFactures
                .Where(cf => cf.IdFacture.HasValue)
                .GroupBy(cf => (cf.IdClient, cf.IdFacture!.Value))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(cf => cf.IdClientFacture).First());
        }
    }
}
