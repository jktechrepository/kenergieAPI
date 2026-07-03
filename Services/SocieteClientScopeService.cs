using Kenergie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    /// <summary>
    /// Définit les périmètres clients d'une société (financier vs effectif actif).
    /// </summary>
    public class SocieteClientScopeService : ISocieteClientScopeService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<SocieteClientScopeService> _logger;

        public SocieteClientScopeService(
            KenergieDbContext context,
            ILogger<SocieteClientScopeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<int>> GetFinancialClientIdsAsync(int idSociete)
        {
            try
            {
                var linkedClientIds = await GetLinkedClientIdsAsync(idSociete, activeLinksOnly: false);
                if (!linkedClientIds.Any())
                {
                    return new List<int>();
                }

                return await _context.Clients
                    .Where(c => linkedClientIds.Contains(c.IdClient)
                        && (!c.IsDeleted.HasValue || !c.IsDeleted.Value))
                    .Select(c => c.IdClient)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients financiers de la société {SocieteId}", idSociete);
                return new List<int>();
            }
        }

        /// <inheritdoc />
        public async Task<List<int>> GetActiveClientIdsAsync(int idSociete)
        {
            try
            {
                var linkedClientIds = await GetLinkedClientIdsAsync(idSociete, activeLinksOnly: true);
                if (!linkedClientIds.Any())
                {
                    return new List<int>();
                }

                return await _context.Clients
                    .Where(c => linkedClientIds.Contains(c.IdClient)
                        && c.IsActif == true
                        && c.Statut == true
                        && (!c.IsDeleted.HasValue || !c.IsDeleted.Value))
                    .Select(c => c.IdClient)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients actifs de la société {SocieteId}", idSociete);
                return new List<int>();
            }
        }

        /// <summary>
        /// Clients liés à la société via Catégorie → Usage → ClientUsage.
        /// Périmètre financier : toute liaison (ClientUsage actif ou non), sans filtre Client.Statut/IsActif.
        /// Périmètre actif : liaisons ClientUsage actives uniquement.
        /// </summary>
        private async Task<List<int>> GetLinkedClientIdsAsync(int idSociete, bool activeLinksOnly)
        {
            var categorieIds = await _context.CategorieClients
                .Where(cc => cc.IdSociete == idSociete && cc.Statut != false)
                .Select(cc => cc.IdCategorie)
                .ToListAsync();

            if (!categorieIds.Any())
            {
                return new List<int>();
            }

            var usageIds = await _context.Usages
                .Where(u => categorieIds.Contains(u.IdCategorieClient) && u.Statut == true)
                .Select(u => u.IdUsage)
                .ToListAsync();

            if (!usageIds.Any())
            {
                return new List<int>();
            }

            var clientUsagesQuery = _context.ClientUsages
                .Where(cu => usageIds.Contains(cu.IdUsage));

            if (activeLinksOnly)
            {
                clientUsagesQuery = clientUsagesQuery.Where(cu => cu.Statut == true);
            }

            return await clientUsagesQuery
                .Select(cu => cu.IdClient)
                .Distinct()
                .ToListAsync();
        }
    }
}
