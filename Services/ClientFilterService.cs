using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Communication;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    /// <summary>
    /// Service de filtrage des clients selon des critères de ciblage
    /// </summary>
    public class ClientFilterService : IClientFilterService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<ClientFilterService> _logger;

        public ClientFilterService(
            KenergieDbContext context,
            ILogger<ClientFilterService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Client>> GetClientsByCriteriaAsync(CriteresCiblageDto? criteres)
        {
            // ✅ TOUJOURS filtrer par Statut = true (clients actifs uniquement)
            var query = _context.Clients
                .Where(c => c.Statut == true)
                .AsQueryable();

            // Si aucun critère, retourner tous les clients actifs
            if (criteres == null)
            {
                return await query.ToListAsync();
            }

            // Si une liste spécifique d'IDs clients est fournie, utiliser uniquement celle-ci
            if (criteres.ListeIdClients != null && criteres.ListeIdClients.Length > 0)
            {
                query = query.Where(c => criteres.ListeIdClients.Contains(c.IdClient));
                return await query.ToListAsync();
            }

            // Filtrer par catégories clients (via les usages -> catégories)
            if (criteres.IdCategorieClients != null && criteres.IdCategorieClients.Length > 0)
            {
                query = query.Where(c => 
                    c.ClientsUsages != null &&
                    c.ClientsUsages.Any(cu => cu.Usage != null && 
                                            cu.Usage.CategorieClient != null &&
                                            criteres.IdCategorieClients.Contains(cu.Usage.CategorieClient.IdCategorie)));
            }

            // Filtrer par IsActif (si spécifié)
            if (criteres.ClientsActifs.HasValue)
            {
                if (criteres.ClientsActifs.Value)
                {
                    query = query.Where(c => c.IsActif == true);
                }
                else
                {
                    query = query.Where(c => c.IsActif == false);
                }
            }

            // Filtrer par société (via les usages -> catégories)
            if (criteres.IdSociete.HasValue)
            {
                query = query.Where(c => 
                    c.ClientsUsages != null &&
                    c.ClientsUsages.Any(cu => cu.Usage != null && 
                                            cu.Usage.CategorieClient != null &&
                                            cu.Usage.CategorieClient.IdSociete == criteres.IdSociete.Value));
            }

            // Filtrer par usage (via les libellés d'usage)
            if (criteres.Usage != null && criteres.Usage.Length > 0)
            {
                query = query.Where(c => 
                    c.ClientsUsages != null &&
                    c.ClientsUsages.Any(cu => cu.Usage != null && 
                                            cu.Usage.Libelle != null &&
                                            criteres.Usage.Contains(cu.Usage.Libelle)));
            }

            // ✨ NOUVEAU : Filtrer par nombre de factures en arriérés
            if (criteres.NombreFacturesArrieresMin.HasValue || criteres.NombreFacturesArrieresMax.HasValue)
            {
                // Récupérer d'abord les IDs des clients qui correspondent aux autres critères
                var clientsIdsFiltres = await query.Select(c => c.IdClient).ToListAsync();

                if (clientsIdsFiltres.Any())
                {
                    // Définir les bornes min et max
                    var minArrieres = criteres.NombreFacturesArrieresMin ?? 0;
                    var maxArrieres = criteres.NombreFacturesArrieresMax ?? int.MaxValue;

                    // Compter les factures en arriérés par client
                    var clientsAvecArrieres = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                     cf.MontantDu.HasValue &&
                                     cf.MontantDu.Value > 0 &&
                                     clientsIdsFiltres.Contains(cf.IdClient))
                        .GroupBy(cf => cf.IdClient)
                        .Select(g => new { IdClient = g.Key, NombreArrieres = g.Count() })
                        .ToListAsync();

                    // Créer un dictionnaire pour accès rapide
                    var dictArrieres = clientsAvecArrieres
                        .ToDictionary(x => x.IdClient, x => x.NombreArrieres);

                    // Filtrer les clients selon min/max
                    var clientsIdsValides = new List<int>();

                    foreach (var idClient in clientsIdsFiltres)
                    {
                        var nombreArrieres = dictArrieres.GetValueOrDefault(idClient, 0);

                        // Vérifier si le client correspond aux critères
                        if (nombreArrieres >= minArrieres && nombreArrieres <= maxArrieres)
                        {
                            clientsIdsValides.Add(idClient);
                        }
                    }

                    // Si aucun client ne correspond, retourner une liste vide
                    if (!clientsIdsValides.Any())
                    {
                        _logger.LogInformation(
                            "✅ Filtrage clients: 0 client(s) trouvé(s) avec les critères de factures en arriérés (min: {Min}, max: {Max})",
                            minArrieres, maxArrieres);
                        return new List<Client>();
                    }

                    // Filtrer la query avec les IDs valides
                    query = query.Where(c => clientsIdsValides.Contains(c.IdClient));
                }
                else
                {
                    // Aucun client ne correspond aux autres critères, retourner liste vide
                    _logger.LogInformation(
                        "✅ Filtrage clients: 0 client(s) trouvé(s) (aucun client ne correspond aux critères de base)");
                    return new List<Client>();
                }
            }

            var clients = await query
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .ToListAsync();

            _logger.LogInformation(
                "✅ Filtrage clients: {Count} client(s) trouvé(s) avec les critères spécifiés",
                clients.Count);

            return clients;
        }
    }
}

