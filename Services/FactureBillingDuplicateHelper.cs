using Kenergie.Data;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    /// <summary>
    /// Détection des clients déjà facturés pour une période / usage / type de courant.
    /// </summary>
    public static class FactureBillingDuplicateHelper
    {
        public static (string MoisPad, string MoisSansPad) GetMoisVariants(int moisEmission)
        {
            return (moisEmission.ToString("D2"), moisEmission.ToString());
        }

        /// <summary>
        /// Retourne les IdClient ayant déjà une ClientFacture active pour la même période,
        /// le même usage et le même type (facture système ou arriéré pré-existant sur usage compatible).
        /// </summary>
        public static async Task<HashSet<int>> GetClientIdsAlreadyBilledAsync(
            KenergieDbContext context,
            int idUsage,
            int moisEmission,
            int anneesEmission,
            int? idTypeDeCourant,
            CancellationToken cancellationToken = default)
        {
            var (moisPad, moisSansPad) = GetMoisVariants(moisEmission);

            var systemClientIds = await context.ClientFactures
                .AsNoTracking()
                .Where(cf => cf.Statut == true
                    && cf.Annees == anneesEmission
                    && (cf.Mois == moisPad || cf.Mois == moisSansPad)
                    && cf.IdFacture != null
                    && cf.Facture != null
                    && cf.Facture.Statut == true
                    && cf.Facture.IdUsage == idUsage
                    && (!idTypeDeCourant.HasValue || cf.Facture.IdTypeDeCourant == idTypeDeCourant))
                .Select(cf => cf.IdClient)
                .Distinct()
                .ToListAsync(cancellationToken);

            var preExistantClientIds = await context.ClientFactures
                .AsNoTracking()
                .Where(cf => cf.Statut == true
                    && cf.EstArrierePreExistant
                    && cf.IdFacture == null
                    && cf.Annees == anneesEmission
                    && (cf.Mois == moisPad || cf.Mois == moisSansPad)
                    && context.ClientUsages.Any(cu =>
                        cu.IdClient == cf.IdClient
                        && cu.IdUsage == idUsage
                        && cu.Statut == true
                        && (!idTypeDeCourant.HasValue || cu.IdTypeDeCourant == idTypeDeCourant)))
                .Select(cf => cf.IdClient)
                .Distinct()
                .ToListAsync(cancellationToken);

            var result = new HashSet<int>(systemClientIds);
            foreach (var id in preExistantClientIds)
                result.Add(id);

            return result;
        }
    }
}
