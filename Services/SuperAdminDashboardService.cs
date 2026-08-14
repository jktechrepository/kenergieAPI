using Kenergie.Models.DTOs;
using Kenergie.Models;
using Kenergie.Data;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour calculer les statistiques du dashboard Super-Admin
    /// </summary>
    public class SuperAdminDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<SuperAdminDashboardService> _logger;
        private readonly IRapportFinancierUsdEnrichmentService _usdEnrichment;

        public SuperAdminDashboardService(
            KenergieDbContext context,
            ILogger<SuperAdminDashboardService> logger,
            IRapportFinancierUsdEnrichmentService usdEnrichment)
        {
            _context = context;
            _logger = logger;
            _usdEnrichment = usdEnrichment;
        }

        /// <summary>
        /// Récupère toutes les données du dashboard Super-Admin
        /// </summary>
        public async Task<SuperAdminDashboardDto> GetDashboardDataAsync()
        {
            try
            {
                var globalStats = await GetGlobalStatisticsAsync();
                var societesSummary = await GetSocietesSummariesAsync();
                var topCA = await GetTop5SocietesCAAsync();
                var topRecouvrement = await GetTop5SocietesRecouvrementAsync();
                var alertes = await GetAlertesCritiquesAsync();
                var tendances = await GetTendancesMensuellesAsync();
                var utilisateursStats = await GetUtilisateursStatistiquesAsync();

                return new SuperAdminDashboardDto
                {
                    GlobalStatistiques = globalStats,
                    Societes = societesSummary,
                    Top5SocietesCA = topCA,
                    Top5SocietesRecouvrement = topRecouvrement,
                    AlertesCritiques = alertes,
                    Tendances = tendances,
                    UtilisateursStatistiques = utilisateursStats,
                    DateGeneration = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du dashboard Super-Admin");
                throw;
            }
        }

        #region Méthodes de calcul des statistiques

        /// <summary>
        /// Calcule les statistiques globales
        /// </summary>
        private async Task<GlobalStatistiquesDto> GetGlobalStatisticsAsync()
        {
            var totalSocietes = await _context.Societes.CountAsync();
            var societesActives = await _context.Societes.CountAsync(s => s.Statut == true);
            var totalClients = await _context.Clients.CountAsync();
            var clientsActifs = await _context.Clients.CountAsync(c => c.IsActif && c.Statut);

            var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finMois = debutMois.AddMonths(1).AddTicks(-1);

            var chiffreAffairesGlobal = await _context.Paiements
                .Where(p => !p.IsDeleted &&
                            p.DatePaiement >= debutMois &&
                            p.DatePaiement <= finMois)
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            var montantTotalPaiementsGlobal = await _context.Paiements
                .Where(p => !p.IsDeleted)
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            var montantTotalArrieresGlobal = await _context.ClientFactures
                .Where(cf => cf.Statut == true &&
                             cf.MontantDu.HasValue &&
                             cf.MontantDu.Value > 0)
                .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

            var totalFactures = await _context.ClientFactures.CountAsync(cf => cf.Statut == true);
            var totalPaiements = await _context.Paiements.CountAsync(p => !p.IsDeleted);

            var facturesMoisPrecedent = await _context.ClientFactures
                .Where(cf => cf.Statut == true &&
                             cf.Mois == debutMois.AddMonths(-1).Month.ToString("00") &&
                             cf.Annees == debutMois.AddMonths(-1).Year)
                .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant ?? 0));

            var tauxRecouvrementGlobal = facturesMoisPrecedent > 0
                ? Math.Round((chiffreAffairesGlobal / facturesMoisPrecedent) * 100, 2)
                : (chiffreAffairesGlobal > 0 ? 100 : 0);

            var syntheseItems = await BuildGlobalStatistiquesSyntheseItemsAsync(debutMois, finMois);

            return new GlobalStatistiquesDto
            {
                TotalSocietes = totalSocietes,
                SocietesActives = societesActives,
                TotalClients = totalClients,
                ClientsActifs = clientsActifs,
                ChiffreAffairesGlobal = chiffreAffairesGlobal,
                MontantTotalArrieresGlobal = montantTotalArrieresGlobal,
                MontantTotalPaiementsGlobal = montantTotalPaiementsGlobal,
                TauxRecouvrementGlobal = tauxRecouvrementGlobal,
                TotalFactures = totalFactures,
                TotalPaiements = totalPaiements,
                SyntheseUsd = await _usdEnrichment.BuildGlobalStatistiquesSyntheseUsdAsync(syntheseItems)
            };
        }

        private async Task<List<(int IdSociete, decimal ChiffreAffaires, decimal MontantArrieres, decimal MontantPaiements)>> BuildGlobalStatistiquesSyntheseItemsAsync(
            DateTime debutMois,
            DateTime finMois)
        {
            var societes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();
            var items = new List<(int IdSociete, decimal ChiffreAffaires, decimal MontantArrieres, decimal MontantPaiements)>();

            foreach (var societe in societes)
            {
                var clientIds = await GetSocieteClientIdsAsync(societe.IdSociete);
                if (!clientIds.Any())
                {
                    items.Add((societe.IdSociete, 0, 0, 0));
                    continue;
                }

                var chiffreAffaires = await _context.Paiements
                    .Where(p => !p.IsDeleted &&
                                p.DatePaiement >= debutMois &&
                                p.DatePaiement <= finMois &&
                                p.IdClient.HasValue &&
                                clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                var montantArrieres = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                                 cf.MontantDu.HasValue &&
                                 cf.MontantDu.Value > 0 &&
                                 clientIds.Contains(cf.IdClient))
                    .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                var montantPaiements = await _context.Paiements
                    .Where(p => !p.IsDeleted &&
                                p.IdClient.HasValue &&
                                clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                items.Add((societe.IdSociete, chiffreAffaires, montantArrieres, montantPaiements));
            }

            return items;
        }

        private async Task<List<int>> GetSocieteClientIdsAsync(int idSociete)
        {
            var categorieIds = await _context.CategorieClients
                .Where(cc => cc.IdSociete == idSociete)
                .Select(cc => cc.IdCategorie)
                .ToListAsync();

            if (!categorieIds.Any())
                return new List<int>();

            var usageIds = await _context.Usages
                .Where(u => categorieIds.Contains(u.IdCategorieClient))
                .Select(u => u.IdUsage)
                .ToListAsync();

            if (!usageIds.Any())
                return new List<int>();

            return await _context.ClientUsages
                .Where(cu => usageIds.Contains(cu.IdUsage))
                .Select(cu => cu.IdClient)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Calcule les résumés des sociétés
        /// </summary>
        private async Task<List<SocieteSummaryDto>> GetSocietesSummariesAsync()
        {
            var societes = await _context.Societes
                .Include(s => s.CategorieClients)
                .ThenInclude(cc => cc.Usages)
                .ThenInclude(u => u.ClientsUsages)
                .ThenInclude(cu => cu.Client)
                .ToListAsync();

            var summaries = new List<SocieteSummaryDto>();

            foreach (var societe in societes)
            {
                var clients = societe.CategorieClients?
                    .SelectMany(cc => cc.Usages?
                        .SelectMany(u => u.ClientsUsages?
                            .Select(cu => cu.Client) ?? new List<Client>()) ?? new List<Client>()) ?? new List<Client>();

                var clientIds = clients?.Where(c => c != null).Select(c => c!.IdClient).ToList() ?? new List<int>();

                // Chiffre d'affaires du mois
                var caMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.DatePaiement.Month == DateTime.Now.Month && 
                           p.DatePaiement.Year == DateTime.Now.Year && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                // Taux de recouvrement
                var currentMonthNum = DateTime.Now.Month.ToString();
                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                    .ToListAsync();
                
                facturesMois = facturesMois
                    .Where(f => f.Mois.Contains(currentMonthNum))
                    .ToList();

                var tauxRecouvrement = facturesMois.Any() 
                    ? (decimal)facturesMois.Count(f => f.Statut) / facturesMois.Count * 100 
                    : 0;

                summaries.Add(new SocieteSummaryDto
                {
                    IdSociete = societe.IdSociete,
                    Nom = societe.Nom,
                    ChiffreAffairesMois = caMois,
                    TauxRecouvrement = tauxRecouvrement,
                    NombreClientsActifs = clientIds.Count,
                    Type = "Privée",
                    Statut = societe.Statut ?? true
                });
            }

            return summaries;
        }

        /// <summary>
        /// Calcule le top 5 des sociétés par chiffre d'affaires
        /// </summary>
        private async Task<List<TopSocieteDto>> GetTop5SocietesCAAsync()
        {
            var caMoisDernier = await _context.Paiements
                .Where(p => !p.IsDeleted && p.DatePaiement.Month == DateTime.Now.Month && 
                       p.DatePaiement.Year == DateTime.Now.Year)
                .ToListAsync();

            // Grouper par société et calculer le total
            var caBySocieteDict = new Dictionary<int, (decimal ChiffreAffaires, string NomSociete)>();
            
            foreach (var paiement in caMoisDernier.Where(p => p.Client != null))
            {
                var clientId = paiement.Client!.IdClient;
                var societeNom = await GetSocieteNomFromClientId(clientId);
                
                if (caBySocieteDict.ContainsKey(clientId))
                {
                    var existing = caBySocieteDict[clientId];
                    caBySocieteDict[clientId] = (existing.ChiffreAffaires + paiement.MontantPaye, existing.NomSociete);
                }
                else
                {
                    caBySocieteDict[clientId] = (paiement.MontantPaye, societeNom);
                }
            }

            return caBySocieteDict
                .OrderByDescending(kvp => kvp.Value.ChiffreAffaires)
                .Take(5)
                .Select((kvp, index) => new TopSocieteDto
                {
                    Rang = index + 1,
                    IdSociete = kvp.Key,
                    Nom = kvp.Value.NomSociete ?? $"Société {kvp.Key}",
                    Valeur = kvp.Value.ChiffreAffaires,
                    VariationMoisPrecedent = 0 // Simplifié pour l'instant
                })
                .ToList();
        }

        /// <summary>
        /// Calcule le top 5 des sociétés par taux de recouvrement
        /// </summary>
        private async Task<List<TopSocieteDto>> GetTop5SocietesRecouvrementAsync()
        {
            var societes = await _context.Societes
                .Include(s => s.CategorieClients)
                .ThenInclude(cc => cc.Usages)
                .ThenInclude(u => u.ClientsUsages)
                .ThenInclude(cu => cu.Client)
                .ToListAsync();

            var recouvrementBySociete = new List<TopSocieteDto>();

            foreach (var societe in societes)
            {
                var clients = societe.CategorieClients?
                    .SelectMany(cc => cc.Usages?
                        .SelectMany(u => u.ClientsUsages?
                            .Select(cu => cu.Client) ?? new List<Client>()) ?? new List<Client>()) ?? new List<Client>();

                var clientIds = clients?.Where(c => c != null).Select(c => c!.IdClient).ToList() ?? new List<int>();

                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                    .ToListAsync();
                
                facturesMois = facturesMois
                    .Where(f => f.Mois.Contains(DateTime.Now.Month.ToString()))
                    .ToList();

                var tauxRecouvrement = facturesMois.Any() 
                    ? (decimal)facturesMois.Count(f => f.Statut) / facturesMois.Count * 100 
                    : 0;

                recouvrementBySociete.Add(new TopSocieteDto
                {
                    Rang = 0, // Sera défini après le tri
                    IdSociete = societe.IdSociete,
                    Nom = societe.Nom,
                    Valeur = tauxRecouvrement,
                    VariationMoisPrecedent = 0
                });
            }

            var top5 = recouvrementBySociete
                .OrderByDescending(r => r.Valeur)
                .Take(5)
                .Select((r, index) => new TopSocieteDto
                {
                    Rang = index + 1,
                    IdSociete = r.IdSociete,
                    Nom = r.Nom,
                    Valeur = r.Valeur,
                    VariationMoisPrecedent = r.VariationMoisPrecedent
                })
                .ToList();

            return top5;
        }

        /// <summary>
        /// Calcule les alertes critiques
        /// </summary>
        private async Task<List<AlerteCritiqueDto>> GetAlertesCritiquesAsync()
        {
            var alertes = new List<AlerteCritiqueDto>();

            // Alertes de recouvrement bas
            var societes = await _context.Societes
                .Include(s => s.CategorieClients)
                .ThenInclude(cc => cc.Usages)
                .ThenInclude(u => u.ClientsUsages)
                .ThenInclude(cu => cu.Client)
                .ToListAsync();

            foreach (var societe in societes)
            {
                var clients = societe.CategorieClients?
                    .SelectMany(cc => cc.Usages?
                        .SelectMany(u => u.ClientsUsages?
                            .Select(cu => cu.Client) ?? new List<Client>()) ?? new List<Client>()) ?? new List<Client>();

                var clientIds = clients?.Where(c => c != null).Select(c => c!.IdClient).ToList() ?? new List<int>();

                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                    .ToListAsync();
                
                facturesMois = facturesMois
                    .Where(f => f.Mois.Contains(DateTime.Now.Month.ToString()))
                    .ToList();

                var tauxRecouvrement = facturesMois.Any() 
                    ? (decimal)facturesMois.Count(f => f.Statut) / facturesMois.Count * 100 
                    : 0;

                if (tauxRecouvrement < 50) // Seuil critique
                {
                    alertes.Add(new AlerteCritiqueDto
                    {
                        TypeAlerte = "Recouvrement",
                        Description = $"Taux de recouvrement critique pour {societe.Nom}: {tauxRecouvrement:F1}%",
                        NiveauCriticite = "Élevée",
                        DateAlerte = DateTime.Now,
                        IdSociete = societe.IdSociete,
                        NomSociete = societe.Nom,
                        Statut = "Non lue"
                    });
                }
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        /// <summary>
        /// Calcule les tendances mensuelles
        /// </summary>
        private async Task<TendancesDto> GetTendancesMensuellesAsync()
        {
            var caTendances = await GetTendanceMensuelleAsync(async (mois, annee) =>
            {
                return await _context.Paiements
                    .Where(p => !p.IsDeleted && p.DatePaiement.Month == mois && p.DatePaiement.Year == annee)
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
            });

            var recouvrementTendances = await GetTendanceMensuelleAsync(async (mois, annee) =>
            {
                var factures = await _context.ClientFactures
                    .Where(f => f.Annees == annee)
                    .ToListAsync();
                
                factures = factures
                    .Where(f => f.Mois.Contains(mois.ToString()))
                    .ToList();

                return factures.Any() ? (decimal)factures.Count(f => f.Statut) / factures.Count * 100 : 0;
            });

            return new TendancesDto
            {
                EvolutionChiffreAffaires = caTendances,
                EvolutionTauxRecouvrement = recouvrementTendances
            };
        }

        /// <summary>
        /// Calcule les statistiques des utilisateurs par rôle
        /// </summary>
        private async Task<UtilisateursStatistiquesDto> GetUtilisateursStatistiquesAsync()
        {
            var utilisateurs = await _context.Utilisateurs
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var repartitionParRole = utilisateurs
                .Where(u => u.UserRoles.Any())
                .SelectMany(u => u.UserRoles.Select(ur => ur.Role))
                .GroupBy(role => role?.Nom)
                .Select(g => new UtilisateurParRoleDto
                {
                    Role = g.Key ?? "Non défini",
                    NombreUtilisateurs = g.Distinct().Count(),
                    Pourcentage = utilisateurs.Count > 0 ? (decimal)g.Distinct().Count() / utilisateurs.Count * 100 : 0
                })
                .OrderByDescending(r => r.NombreUtilisateurs)
                .ToList();

            return new UtilisateursStatistiquesDto
            {
                TotalUtilisateurs = utilisateurs.Count,
                RepartitionParRole = repartitionParRole,
                UtilisateursConnectes = 0, // À implémenter avec un système de suivi de connexion
                UtilisateursActifsMois = utilisateurs.Count(u => u.DateCreation.Month == DateTime.Now.Month && 
                                                              u.DateCreation.Year == DateTime.Now.Year)
            };
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Calcule le taux de recouvrement pour une liste de factures
        /// </summary>
        private decimal CalculateTauxRecouvrementSociete(List<ClientFacture> factures)
        {
            if (!factures.Any()) return 0;

            var montantTotal = factures.Sum(f => (f.MontantDevisePrincipale ?? f.Montant ?? 0));
            var montantRecouvre = factures.Where(f => f.Statut).Sum(f => (f.MontantPayeDevisePrincipale ?? f.MontantPaye ?? 0));

            return montantTotal > 0 ? (montantRecouvre / montantTotal) * 100 : 0;
        }

        /// <summary>
        /// Calcule la variation du chiffre d'affaires par rapport au mois précédent
        /// </summary>
        private async Task<decimal> CalculateTauxRecouvrementGlobal(int? idSociete, decimal caActuel)
        {
            if (!idSociete.HasValue) return 0;

            var caMoisPrecedent = await _context.Paiements
                .Where(p => !p.IsDeleted && p.DatePaiement.Month == DateTime.Now.AddMonths(-1).Month && 
                       p.DatePaiement.Year == DateTime.Now.AddMonths(-1).Year && p.IdClient.HasValue)
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            return caMoisPrecedent > 0 ? ((caActuel - caMoisPrecedent) / caMoisPrecedent) * 100 : 0;
        }

        /// <summary>
        /// Calcule la variation du taux de recouvrement par rapport au mois précédent
        /// </summary>
        private async Task<decimal> CalculateVariationTauxRecouvrementPrecedent(int? idSociete, decimal tauxActuel)
        {
            if (!idSociete.HasValue) return 0;

            var facturesPrecedent = await _context.ClientFactures
                .Where(f => f.Annees == DateTime.Now.AddMonths(-1).Year)
                .ToListAsync();
            
            facturesPrecedent = facturesPrecedent
                .Where(f => f.Mois.Contains(DateTime.Now.AddMonths(-1).Month.ToString()))
                .ToList();

            var tauxPrecedent = CalculateTauxRecouvrementSociete(facturesPrecedent);
            
            return tauxPrecedent > 0 ? tauxActuel - tauxPrecedent : 0;
        }

        /// <summary>
        /// Récupère le nom de la société à partir de l'ID du client
        /// </summary>
        private async Task<string?> GetSocieteNomFromClientId(int? idClient)
        {
            if (!idClient.HasValue) return null;

            var client = await _context.Clients
                .Include(c => c.ClientsUsages)
                .ThenInclude(cu => cu.Usage)
                .ThenInclude(u => u.CategorieClient)
                .ThenInclude(cc => cc.Societe)
                .FirstOrDefaultAsync(c => c.IdClient == idClient);

            return client?.ClientsUsages?
                .Select(cu => cu.Usage?.CategorieClient?.Societe?.Nom)
                .FirstOrDefault();
        }

        /// <summary>
        /// Génère les tendances mensuelles pour un calculateur donné
        /// </summary>
        private async Task<List<TendanceMensuelleDto>> GetTendanceMensuelleAsync(Func<int, int, Task<decimal>> calculateur)
        {
            var tendances = new List<TendanceMensuelleDto>();
            decimal valeurPrecedente = 0;

            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var mois = date.Month;
                var annee = date.Year;
                var valeur = await calculateur(mois, annee);

                var variation = valeurPrecedente > 0 ? ((valeur - valeurPrecedente) / valeurPrecedente) * 100 : 0;

                tendances.Add(new TendanceMensuelleDto
                {
                    Mois = $"{annee}-{mois.ToString()}",
                    Annee = annee,
                    Valeur = valeur,
                    Variation = variation
                });

                valeurPrecedente = valeur;
            }

            return tendances;
        }

        #endregion
    }
}
