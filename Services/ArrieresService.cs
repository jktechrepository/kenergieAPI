using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour le suivi et le calcul des arriérés de factures
    /// </summary>
    public class ArrieresService
    {
        private readonly KenergieDbContext _context;
        private readonly IClientFactureRepository _clientFactureRepository;

        public ArrieresService(KenergieDbContext context, IClientFactureRepository clientFactureRepository)
        {
            _context = context;
            _clientFactureRepository = clientFactureRepository;
        }

        /// <summary>
        /// Récupère toutes les factures impayées d'un client (version optimisée avec ClientFacture)
        /// Utilise ClientFacture comme source principale pour éviter les N+1 queries
        /// </summary>
        public async Task<ArrieresClientDto?> GetArrieresByClientAsync(int idClient)
        {
            var client = await _context.Clients
                .FirstOrDefaultAsync(c => c.IdClient == idClient);

            if (client == null)
                return null;

            // ✨ NOUVEAU : Utiliser ClientFacture directement (1 seule requête au lieu de N+1)
            var clientFactures = await _clientFactureRepository.GetByClientWithArrieresAsync(idClient);

            if (!clientFactures.Any())
                return null;

            var facturesImpayees = new List<FactureImpayeeDto>();

            foreach (var clientFacture in clientFactures)
            {
                var dto = await ConvertClientFactureToDtoAsync(clientFacture);
                facturesImpayees.Add(dto);
            }

            var totalArrieres = facturesImpayees.Sum(f => f.MontantDu);
            var montantTotalFactures = facturesImpayees.Sum(f => f.MontantTotal ?? 0);
            var montantTotalPaye = facturesImpayees.Sum(f => f.MontantPaye ?? 0);

            return new ArrieresClientDto
            {
                IdClient = client.IdClient,
                NomClient = client.NomClient,
                Telephone = client.Telephone,
                EmailClient = client.EmailClient,
                NombreFacturesImpayees = facturesImpayees.Count,
                TotalArrieres = totalArrieres,
                MontantTotalFactures = montantTotalFactures,
                MontantTotalPaye = montantTotalPaye,
                FacturesImpayees = facturesImpayees
            };
        }

        /// <summary>
        /// Convertit une ClientFacture en FactureImpayeeDto
        /// </summary>
        private async Task<FactureImpayeeDto> ConvertClientFactureToDtoAsync(ClientFacture clientFacture)
        {
            Facture? facture = null;
            if (clientFacture.IdFacture.HasValue)
            {
                facture = await _context.Factures
                    .Include(f => f.Usage)
                    .FirstOrDefaultAsync(f => f.IdFacture == clientFacture.IdFacture.Value);
            }

            var joursRetard = clientFacture.DateEmission.HasValue
                ? (DateTime.Now - clientFacture.DateEmission.Value).Days
                : (int?)null;

            return new FactureImpayeeDto
            {
                IdFacture = clientFacture.IdFacture ?? 0,
                IdClientFacture = clientFacture.IdClientFacture,
                NumeroFacture = facture?.NumeroFacture ?? (clientFacture.EstArrierePreExistant ? "ARRIERE-PRE-EXISTANT" : null),
                DateEmission = clientFacture.DateEmission ?? facture?.DateEmission,
                MoisEmission = facture?.MoisEmission ?? (clientFacture.Mois != null && int.TryParse(clientFacture.Mois, out var mois) ? mois : 0),
                AnneesEmission = facture?.AnneesEmission ?? clientFacture.Annees ?? 0,
                MontantTotal = clientFacture.Montant ?? 0,
                MontantPaye = clientFacture.MontantPaye ?? 0,
                MontantDu = clientFacture.MontantDu ?? 0,
                JoursRetard = joursRetard,
                NomCategorie = facture?.Usage?.Libelle ?? clientFacture.Description ?? "Arriéré pré-existant"
            };
        }

        /// <summary>
        /// Récupère toutes les factures impayées d'un client (version optimisée avec ClientFacture)
        /// </summary>
        public async Task<IEnumerable<FactureImpayeeDto>> GetFacturesImpayeesByClientAsync(int idClient)
        {
            // ✨ NOUVEAU : Utiliser ClientFacture directement (1 seule requête)
            var clientFactures = await _clientFactureRepository.GetByClientWithArrieresAsync(idClient);

            var facturesImpayees = new List<FactureImpayeeDto>();

            foreach (var clientFacture in clientFactures)
            {
                var dto = await ConvertClientFactureToDtoAsync(clientFacture);
                facturesImpayees.Add(dto);
            }

            return facturesImpayees;
        }

        /// <summary>
        /// Récupère les factures impayées d'un client avec pagination, filtres et tri (version optimisée avec ClientFacture)
        /// </summary>
        public async Task<PagedResult<FactureImpayeeDto>> GetFacturesImpayeesByClientPagedAsync(int idClient, PagedRequest request)
        {
            request ??= new PagedRequest();

            // ✨ NOUVEAU : Utiliser ClientFacture directement (1 seule requête)
            var clientFactures = await _clientFactureRepository.GetByClientWithArrieresAsync(idClient);

            var facturesImpayees = new List<FactureImpayeeDto>();

            foreach (var clientFacture in clientFactures)
            {
                var dto = await ConvertClientFactureToDtoAsync(clientFacture);
                facturesImpayees.Add(dto);
            }

            // Recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                facturesImpayees = facturesImpayees.Where(f =>
                    (f.NumeroFacture ?? string.Empty).ToLower().Contains(term) ||
                    (f.NomCategorie ?? string.Empty).ToLower().Contains(term)
                ).ToList();
            }

            // Tri
            var sortedFactures = request.SortBy?.ToLower() switch
            {
                "dateemission" or "date" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                    : facturesImpayees.OrderBy(f => f.DateEmission ?? DateTime.MinValue),
                "montantdu" or "montant" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.MontantDu)
                    : facturesImpayees.OrderBy(f => f.MontantDu),
                "joursretard" or "retard" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.JoursRetard ?? 0)
                    : facturesImpayees.OrderBy(f => f.JoursRetard ?? 0),
                "numerofacture" or "numero" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.NumeroFacture ?? string.Empty)
                    : facturesImpayees.OrderBy(f => f.NumeroFacture ?? string.Empty),
                _ => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                    : facturesImpayees.OrderBy(f => f.DateEmission ?? DateTime.MinValue)
            };

            var total = facturesImpayees.Count;
            var data = sortedFactures
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<FactureImpayeeDto>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Récupère un rapport global de tous les arriérés pour une société
        /// </summary>
        public async Task<ArrieresGlobalDto> GetArrieresGlobalAsync(int idSociete)
        {
            // Récupérer uniquement les clients de la société spécifiée
            // Les clients sont liés à la société via leurs usages -> catégories
            var clients = await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Where(c => c.Statut == true &&
                            c.ClientsUsages != null &&
                            c.ClientsUsages.Any(cu => cu.Usage != null && 
                                                      cu.Usage.CategorieClient != null && 
                                                      cu.Usage.CategorieClient.IdSociete == idSociete))
                .ToListAsync();

            var clientsAvecArrieres = new List<ArrieresClientDto>();

            foreach (var client in clients)
            {
                var arrieres = await GetArrieresByClientAsync(client.IdClient);
                if (arrieres != null && arrieres.NombreFacturesImpayees > 0)
                {
                    clientsAvecArrieres.Add(arrieres);
                }
            }

            var nombreClientsAvecArrieres = clientsAvecArrieres.Count;
            var nombreTotalFacturesImpayees = clientsAvecArrieres.Sum(c => c.NombreFacturesImpayees);
            var totalArrieres = clientsAvecArrieres.Sum(c => c.TotalArrieres);
            var montantTotalFactures = clientsAvecArrieres.Sum(c => c.MontantTotalFactures ?? 0);
            var montantTotalPaye = clientsAvecArrieres.Sum(c => c.MontantTotalPaye ?? 0);

            return new ArrieresGlobalDto
            {
                NombreClientsAvecArrieres = nombreClientsAvecArrieres,
                NombreTotalFacturesImpayees = nombreTotalFacturesImpayees,
                TotalArrieres = totalArrieres,
                MontantTotalFactures = montantTotalFactures,
                MontantTotalPaye = montantTotalPaye,
                ClientsAvecArrieres = clientsAvecArrieres.OrderByDescending(c => c.TotalArrieres).ToList()
            };
        }

        /// <summary>
        /// Récupère les clients avec arriérés triés par montant décroissant pour une société donnée
        /// </summary>
        public async Task<IEnumerable<ArrieresClientDto>> GetClientsAvecArrieresAsync(int idSociete)
        {
            // Récupérer uniquement les clients de la société spécifiée
            // Les clients sont liés à la société via leurs usages -> catégories
            var clients = await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Where(c => c.Statut == true &&
                            c.ClientsUsages != null &&
                            c.ClientsUsages.Any(cu => cu.Usage != null && 
                                                      cu.Usage.CategorieClient != null && 
                                                      cu.Usage.CategorieClient.IdSociete == idSociete))
                .ToListAsync();

            var result = new List<ArrieresClientDto>();

            foreach (var client in clients)
            {
                var arrieres = await GetArrieresByClientAsync(client.IdClient);
                if (arrieres != null && arrieres.NombreFacturesImpayees > 0)
                {
                    result.Add(arrieres);
                }
            }

            return result.OrderByDescending(c => c.TotalArrieres);
        }

        /// <summary>
        /// Récupère toutes les factures payées d'un client (entièrement payées)
        /// Basée sur la table Paiement : une facture est payée si Montant <= somme des paiements validés
        /// </summary>
        public async Task<IEnumerable<FacturePayeeDto>> GetFacturesPayeesByClientAsync(int idClient)
        {
            var client = await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                .FirstOrDefaultAsync(c => c.IdClient == idClient);

            if (client == null)
                return Enumerable.Empty<FacturePayeeDto>();

            // Récupérer tous les usages du client
            var clientUsages = client.ClientsUsages?.ToList() ?? new List<ClientUsage>();

            if (clientUsages.Count == 0)
                return Enumerable.Empty<FacturePayeeDto>();

            // Récupérer les IDs des usages du client
            var usagesIds = clientUsages.Select(cu => cu.IdUsage).ToList();

            // Récupérer toutes les factures de tous les usages du client
            var factures = await _context.Factures
                .Include(f => f.Usage)
                .Where(f => usagesIds.Contains(f.IdUsage) && f.Statut == true)
                .ToListAsync();

            var facturesPayees = new List<FacturePayeeDto>();

            foreach (var facture in factures)
            {
                // Trouver le ClientUsage correspondant pour obtenir nombreBatiment
                var clientUsage = clientUsages.FirstOrDefault(cu => cu.IdUsage == facture.IdUsage);
                var nombreBatiment = clientUsage?.nombreBatiment ?? 1;

                // Calculer le montant total payé par ce client pour cette facture
                var montantPaye = await _context.Paiements
                    .Where(p => p.IdFacture == facture.IdFacture && 
                               p.IdClient == idClient && 
                               p.Statut != null &&
                               (p.Statut == "Validé" || 
                                p.Statut.ToLower() == "true"))
                    .SumAsync(p => p.MontantPaye);

                // Multiplier le montant de la facture par nombreBatiment
                var montantTotal = (facture.Montant ?? 0) * nombreBatiment;

                // Une facture est payée si le montant total <= montant payé
                if (montantTotal > 0 && montantPaye >= montantTotal)
                {
                    // Trouver la date du dernier paiement qui a complété la facture
                    var datePaiementComplet = await _context.Paiements
                        .Where(p => p.IdFacture == facture.IdFacture && 
                                   p.IdClient == idClient && 
                                   p.Statut == "Validé")
                        .OrderByDescending(p => p.DatePaiement)
                        .Select(p => p.DatePaiement)
                        .FirstOrDefaultAsync();

                    facturesPayees.Add(new FacturePayeeDto
                    {
                        IdFacture = facture.IdFacture,
                        NumeroFacture = facture.NumeroFacture,
                        DateEmission = facture.DateEmission,
                        MoisEmission = facture.MoisEmission,
                        AnneesEmission = facture.AnneesEmission,
                        MontantTotal = montantTotal,
                        MontantPaye = montantPaye,
                        DatePaiementComplet = datePaiementComplet != default ? datePaiementComplet : (DateTime?)null,
                        NomCategorie = facture.Usage?.Libelle // Afficher le libellé de l'usage
                    });
                }
            }

            return facturesPayees;
        }

        /// <summary>
        /// Récupère les factures payées d'un client avec pagination, filtres et tri
        /// Basée sur la table Paiement : une facture est payée si Montant <= somme des paiements validés
        /// </summary>
        public async Task<PagedResult<FacturePayeeDto>> GetFacturesPayeesByClientPagedAsync(int idClient, PagedRequest request)
        {
            request ??= new PagedRequest();

            var client = await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                .FirstOrDefaultAsync(c => c.IdClient == idClient);

            if (client == null)
                return new PagedResult<FacturePayeeDto>(Enumerable.Empty<FacturePayeeDto>(), 0, request.PageNumber, request.PageSize);

            // Récupérer tous les usages du client
            var clientUsages = client.ClientsUsages?.ToList() ?? new List<ClientUsage>();

            if (clientUsages.Count == 0)
                return new PagedResult<FacturePayeeDto>(Enumerable.Empty<FacturePayeeDto>(), 0, request.PageNumber, request.PageSize);

            // Récupérer les IDs des usages du client
            var usagesIds = clientUsages.Select(cu => cu.IdUsage).ToList();

            // Récupérer toutes les factures de tous les usages du client
            var factures = await _context.Factures
                .Include(f => f.Usage)
                .Where(f => usagesIds.Contains(f.IdUsage) && f.Statut == true)
                .ToListAsync();

            var facturesPayees = new List<FacturePayeeDto>();

            foreach (var facture in factures)
            {
                // Trouver le ClientUsage correspondant pour obtenir nombreBatiment
                var clientUsage = clientUsages.FirstOrDefault(cu => cu.IdUsage == facture.IdUsage);
                var nombreBatiment = clientUsage?.nombreBatiment ?? 1;

                // Calculer le montant total payé par ce client pour cette facture
                var montantPaye = await _context.Paiements
                    .Where(p => p.IdFacture == facture.IdFacture && 
                               p.IdClient == idClient && 
                               p.Statut != null &&
                               (p.Statut == "Validé" || 
                                p.Statut.ToLower() == "true"))
                    .SumAsync(p => p.MontantPaye);

                // Multiplier le montant de la facture par nombreBatiment
                var montantTotal = (facture.Montant ?? 0) * nombreBatiment;

                // Une facture est payée si le montant total <= montant payé
                if (montantTotal > 0 && montantPaye >= montantTotal)
                {
                    // Trouver la date du dernier paiement qui a complété la facture
                    var datePaiementComplet = await _context.Paiements
                        .Where(p => p.IdFacture == facture.IdFacture && 
                                   p.IdClient == idClient && 
                                   p.Statut == "Validé")
                        .OrderByDescending(p => p.DatePaiement)
                        .Select(p => p.DatePaiement)
                        .FirstOrDefaultAsync();

                    facturesPayees.Add(new FacturePayeeDto
                    {
                        IdFacture = facture.IdFacture,
                        NumeroFacture = facture.NumeroFacture,
                        DateEmission = facture.DateEmission,
                        MoisEmission = facture.MoisEmission,
                        AnneesEmission = facture.AnneesEmission,
                        MontantTotal = montantTotal,
                        MontantPaye = montantPaye,
                        DatePaiementComplet = datePaiementComplet != default ? datePaiementComplet : (DateTime?)null,
                        NomCategorie = facture.Usage?.Libelle // Afficher le libellé de l'usage
                    });
                }
            }

            // Recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                facturesPayees = facturesPayees.Where(f =>
                    (f.NumeroFacture ?? string.Empty).ToLower().Contains(term) ||
                    (f.NomCategorie ?? string.Empty).ToLower().Contains(term)
                ).ToList();
            }

            // Tri
            var sortedFactures = request.SortBy?.ToLower() switch
            {
                "dateemission" or "date" => request.SortDescending
                    ? facturesPayees.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                    : facturesPayees.OrderBy(f => f.DateEmission ?? DateTime.MinValue),
                "datepaiementcomplet" or "datepaiement" => request.SortDescending
                    ? facturesPayees.OrderByDescending(f => f.DatePaiementComplet ?? DateTime.MinValue)
                    : facturesPayees.OrderBy(f => f.DatePaiementComplet ?? DateTime.MinValue),
                "montanttotal" or "montant" => request.SortDescending
                    ? facturesPayees.OrderByDescending(f => f.MontantTotal ?? 0)
                    : facturesPayees.OrderBy(f => f.MontantTotal ?? 0),
                "numerofacture" or "numero" => request.SortDescending
                    ? facturesPayees.OrderByDescending(f => f.NumeroFacture ?? string.Empty)
                    : facturesPayees.OrderBy(f => f.NumeroFacture ?? string.Empty),
                _ => request.SortDescending
                    ? facturesPayees.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                    : facturesPayees.OrderBy(f => f.DateEmission ?? DateTime.MinValue)
            };

            var total = facturesPayees.Count;
            var data = sortedFactures
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<FacturePayeeDto>(data, total, request.PageNumber, request.PageSize);
        }
    }
}

