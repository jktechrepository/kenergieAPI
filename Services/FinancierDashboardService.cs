using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class FinancierDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<FinancierDashboardService> _logger;

        public FinancierDashboardService(KenergieDbContext context, ILogger<FinancierDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<FinancierDashboardDto> GetDashboardDataAsync()
        {
            try
            {
                // Exécuter les requêtes séquentiellement pour éviter les problèmes de concurrence DbContext
                var globalStatistiques = await GetGlobalFinancierStatistiquesAsync();
                var societesFinancieres = await GetSocietesFinancieresAsync();
                var transactionsRecentes = await GetTransactionsRecentesAsync();
                var alertesFinancieres = await GetAlertesFinancieresAsync();
                var tendances = await GetTendancesFinancieresAsync();
                var top10AgentsCollecteurs = await GetTop10AgentsCollecteursAsync();

                return new FinancierDashboardDto
                {
                    GlobalStatistiques = globalStatistiques,
                    SocietesFinancieres = societesFinancieres,
                    TransactionsRecentes = transactionsRecentes,
                    AlertesFinancieres = alertesFinancieres,
                    Top10AgentsCollecteurs = top10AgentsCollecteurs,
                    Tendances = tendances,
                    DateGeneration = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du dashboard Financier");
                
                // Retourner un dashboard vide en cas d'erreur pour éviter l'erreur 500
                return new FinancierDashboardDto
                {
                    GlobalStatistiques = new GlobalFinancierStatistiquesDto(),
                    SocietesFinancieres = new List<SocieteFinancierSummaryDto>(),
                    TransactionsRecentes = new List<TransactionRecenteDto>(),
                    AlertesFinancieres = new List<AlerteFinanciereDto>(),
                    Top10AgentsCollecteurs = new List<TopAgentCollecteurDto>(),
                    Tendances = new TendancesFinancieresDto(),
                    DateGeneration = DateTime.UtcNow
                };
            }
        }

        public async Task<GlobalFinancierStatistiquesDto> GetGlobalFinancierStatistiquesAsync()
        {
            try
            {
                var toutesSocietes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();
                
                var caTotal = 0m;
                var montantEncaisse = 0m;
                var montantArrieres = 0m;
                var nombreTransactions = 0;
                var montantMoisPrecedentTotal = 0m;
                var nombreFacturesTotal = 0;
                var caJournalierTotal = 0m;

                foreach (var societe in toutesSocietes)
                {
                    var societeStats = await GetSocieteFinancierStatsAsync(societe.IdSociete);
                    caTotal += societeStats.ChiffreAffaires;
                    montantEncaisse += societeStats.MontantEncaisse; // Maintenant cohérent avec caTotal
                    montantArrieres += societeStats.MontantArrieres;
                    nombreTransactions += societeStats.NombreTransactions;
                    montantMoisPrecedentTotal += societeStats.Item6; // MontantMoisPrecedent
                    nombreFacturesTotal += societeStats.Item7; // NombreFactures
                    
                    // Calcul du chiffre d'affaires journalier pour cette société
                    var caJournalierSociete = await GetChiffreAffairesJournalierSocieteAsync(societe.IdSociete);
                    caJournalierTotal += caJournalierSociete;
                }

                // Calcul du TotalGeneralArriere (même formule que DashboardService)
                var montantTotalFacturesGlobal = await _context.ClientFactures
                    .Where(f => f.Statut == true)
                    .SumAsync(f => f.Montant ?? 0);

                var montantTotalPaiementsGlobal = await _context.Paiements
                    .Where(p => !p.IsDeleted)
                    .SumAsync(p => p.MontantPaye);

                var totalGeneralArriere = montantTotalFacturesGlobal - montantTotalPaiementsGlobal;

                // Taux de recouvrement global (même formule que DashboardService)
                var tauxRecouvrementGlobal = montantMoisPrecedentTotal > 0
                    ? Math.Round((caTotal / montantMoisPrecedentTotal) * 100, 2)
                    : 0;

                return new GlobalFinancierStatistiquesDto
                {
                    ChiffreAffairesTotal = caTotal,
                    MontantTotalEncaisse = montantEncaisse, // Maintenant égal à ChiffreAffairesTotal
                    MontantTotalArrieres = montantArrieres,
                    TotalGeneralArriere = totalGeneralArriere, // Ajout de la propriété
                    MontantMoisPrecedent = montantMoisPrecedentTotal, // Ajout de la propriété
                    TauxRecouvrementGlobal = tauxRecouvrementGlobal,
                    NombreTotalTransactions = nombreTransactions,
                    MoyenneTransaction = nombreTransactions > 0 ? caTotal / nombreTransactions : 0,
                    NombreFactures = nombreFacturesTotal, // Ajout de la propriété
                    ChiffreAffairesJournalier = caJournalierTotal // Ajout de la nouvelle propriété
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques globales financières");
                return new GlobalFinancierStatistiquesDto();
            }
        }

        public async Task<List<SocieteFinancierSummaryDto>> GetSocietesFinancieresAsync()
        {
            try
            {
                var societes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();
                var result = new List<SocieteFinancierSummaryDto>();

                foreach (var societe in societes)
                {
                    try
                    {
                        var stats = await GetSocieteFinancierStatsAsync(societe.IdSociete);
                        
                        result.Add(new SocieteFinancierSummaryDto
                        {
                            IdSociete = societe.IdSociete,
                            NomSociete = societe.Nom,
                            ChiffreAffaires = stats.ChiffreAffaires,
                            MontantEncaisse = stats.MontantEncaisse,
                            MontantArrieres = stats.MontantArrieres,
                            TauxRecouvrement = stats.TauxRecouvrement,
                            NombreTransactions = stats.NombreTransactions,
                            StatutFinancier = GetStatutFinancier(stats.TauxRecouvrement, stats.MontantArrieres)
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement de la société {SocieteId}", societe.IdSociete);
                        // Continuer avec les autres sociétés
                    }
                }

                return result.OrderByDescending(s => s.ChiffreAffaires).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sociétés financières");
                return new List<SocieteFinancierSummaryDto>();
            }
        }

        public async Task<List<TransactionRecenteDto>> GetTransactionsRecentesAsync()
        {
            try
            {
                var transactions = await _context.Paiements
                    .Include(p => p.Client)
                    .Where(p => !p.IsDeleted && p.DatePaiement >= DateTime.Now.AddDays(-7))
                    .OrderByDescending(p => p.DatePaiement)
                    .Take(20)
                    .ToListAsync();

                var result = new List<TransactionRecenteDto>();
                
                foreach (var paiement in transactions)
                {
                    try
                    {
                        var societe = await GetSocieteFromClientIdAsync(paiement.IdClient);
                        
                        result.Add(new TransactionRecenteDto
                        {
                            IdTransaction = paiement.IdPaiement,
                            Reference = $"PAY-{paiement.IdPaiement:D6}",
                            NomClient = paiement.Client?.NomClient ?? "Client inconnu",
                            NomSociete = societe?.Nom ?? "Société inconnue",
                            Montant = paiement.MontantPaye,
                            DateTransaction = paiement.DatePaiement,
                            TypeTransaction = "Paiement",
                            Statut = paiement.Statut == "Validé" ? "Validé" : "En attente"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement du paiement {PaiementId}", paiement.IdPaiement);
                        // Continuer avec les autres paiements
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des transactions récentes");
                return new List<TransactionRecenteDto>();
            }
        }

        public async Task<List<AlerteFinanciereDto>> GetAlertesFinancieresAsync()
        {
            try
            {
                var alertes = new List<AlerteFinanciereDto>();
                var societes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();

                foreach (var societe in societes)
                {
                    try
                    {
                        var stats = await GetSocieteFinancierStatsAsync(societe.IdSociete);
                        
                        // Alerte si taux de recouvrement < 70%
                        if (stats.TauxRecouvrement < 70)
                        {
                            alertes.Add(new AlerteFinanciereDto
                            {
                                IdAlerte = alertes.Count + 1,
                                TypeAlerte = "Recouvrement faible",
                                Description = $"Taux de recouvrement critique pour {societe.Nom}: {stats.TauxRecouvrement:F1}%",
                                NiveauCriticite = stats.TauxRecouvrement < 50 ? "Élevée" : "Moyenne",
                                DateAlerte = DateTime.Now,
                                IdSociete = societe.IdSociete,
                                NomSociete = societe.Nom,
                                MontantConcerne = stats.MontantArrieres,
                                EstLue = false
                            });
                        }

                        // Alerte si arriérés > 1000000
                        if (stats.MontantArrieres > 1000000)
                        {
                            alertes.Add(new AlerteFinanciereDto
                            {
                                IdAlerte = alertes.Count + 1,
                                TypeAlerte = "Arriérés élevés",
                                Description = $"Montant d'arriérés important pour {societe.Nom}: {stats.MontantArrieres:N0} FC",
                                NiveauCriticite = stats.MontantArrieres > 5000000 ? "Élevée" : "Moyenne",
                                DateAlerte = DateTime.Now,
                                IdSociete = societe.IdSociete,
                                NomSociete = societe.Nom,
                                MontantConcerne = stats.MontantArrieres,
                                EstLue = false
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors du traitement des alertes pour la société {SocieteId}", societe.IdSociete);
                        // Continuer avec les autres sociétés
                    }
                }

                return alertes.OrderByDescending(a => a.DateAlerte).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des alertes financières");
                return new List<AlerteFinanciereDto>();
            }
        }

        public async Task<TendancesFinancieresDto> GetTendancesFinancieresAsync()
        {
            try
            {
                var toutesSocietes = await _context.Societes
                    .Where(s => s.Statut == true)
                    .ToListAsync();

                // Utiliser le pattern existant avec calculateurs
                var chiffreAffaires = await GetTendanceMensuelleAsync(
                    async (mois, annee) => await CalculerChiffreAffairesMoisAsync(mois, annee, toutesSocietes));

                var encaissements = await GetTendanceMensuelleAsync(
                    async (mois, annee) => await CalculerChiffreAffairesMoisAsync(mois, annee, toutesSocietes));

                var tauxRecouvrement = await GetTendanceMensuelleAsync(
                    async (mois, annee) => await CalculerTauxRecouvrementMoisAsync(mois, annee, toutesSocietes));

                return new TendancesFinancieresDto
                {
                    ChiffreAffaires = chiffreAffaires,
                    Encaissements = encaissements,
                    TauxRecouvrement = tauxRecouvrement
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des tendances financières");
                return new TendancesFinancieresDto();
            }
        }

        #region Méthodes utilitaires

        private async Task<(decimal ChiffreAffaires, decimal MontantEncaisse, decimal MontantArrieres, 
                          decimal TauxRecouvrement, int NombreTransactions, decimal MontantMoisPrecedent, int NombreFactures)> GetSocieteFinancierStatsAsync(int idSociete)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                if (!clientIds.Any())
                {
                    return (0, 0, 0, 0, 0, 0, 0);
                }

                // Périodes pour le calcul (même logique que DashboardService)
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
                var debutMoisPrecedent = debutMois.AddMonths(-1);
                var finMoisPrecedent = debutMois.AddDays(-1).AddHours(23).AddMinutes(59);

                // Normalisation du mois (gère les formats "1" et "01")
                var moisActuelNormalise = NormaliserMois(DateTime.Now.Month.ToString());
                var moisPrecedentNormalise = NormaliserMois(debutMoisPrecedent.Month.ToString());

                // Factures du mois en cours (même logique que DashboardService)
                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && 
                               f.Statut == true &&
                               f.Mois == moisActuelNormalise &&
                               f.Annees == DateTime.Now.Year)
                    .ToListAsync();

                var nombreFactures = facturesMois.Count; // Même logique que DashboardService

                // Chiffre d'affaires du mois (paiements du mois en cours)
                var caMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMois && 
                           p.DatePaiement <= finMois &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);

                // Montant encaissé du mois (même période que chiffre d'affaires pour cohérence)
                var montantEncaisseMois = caMois; // COHÉRENT : même valeur que chiffre d'affaires

                // Factures du mois précédent (pour calcul du taux de recouvrement)
                var facturesMoisPrecedent = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && 
                               f.Statut == true &&
                               f.Mois == moisPrecedentNormalise &&
                               f.Annees == debutMoisPrecedent.Year)
                    .SumAsync(f => f.Montant ?? 0);

                // Total des factures (toutes périodes) pour calcul des arriérés
                var facturesTotal = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && f.Statut == true)
                    .SumAsync(f => f.Montant ?? 0);

                // Total encaissé (toutes périodes) pour calcul des arriérés
                var totalEncaisseHistorique = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);

                // Arriérés (calcul standard)
                var montantArrieres = facturesTotal - totalEncaisseHistorique;

                // Nombre de transactions du mois
                var nombreTransactions = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMois && 
                           p.DatePaiement <= finMois &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .CountAsync();

                // Taux de recouvrement (même formule que DashboardService)
                var tauxRecouvrement = facturesMoisPrecedent > 0
                    ? Math.Round((caMois / facturesMoisPrecedent) * 100, 2)
                    : 0;

                return (caMois, montantEncaisseMois, montantArrieres, tauxRecouvrement, nombreTransactions, facturesMoisPrecedent, nombreFactures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des statistiques financières pour la société {SocieteId}", idSociete);
                return (0, 0, 0, 0, 0, 0, 0);
            }
        }

        private async Task<List<Client>> GetSocieteClientsAsync(int idSociete)
        {
            try
            {
                // Récupérer les IDs des catégories de clients pour cette société
                var categorieIds = await _context.CategorieClients
                    .Where(cc => cc.IdSociete == idSociete)
                    .Select(cc => cc.IdCategorie)
                    .ToListAsync();

                if (!categorieIds.Any())
                {
                    _logger.LogWarning("Aucune catégorie de client trouvée pour la société {SocieteId}", idSociete);
                    return new List<Client>();
                }

                // Récupérer les IDs des usages pour ces catégories
                var usageIds = await _context.Usages
                    .Where(u => categorieIds.Contains(u.IdCategorieClient))
                    .Select(u => u.IdUsage)
                    .ToListAsync();

                if (!usageIds.Any())
                {
                    _logger.LogWarning("Aucun usage trouvé pour les catégories de la société {SocieteId}", idSociete);
                    return new List<Client>();
                }

                // Récupérer les clients qui ont ces usages
                var clientIds = await _context.ClientUsages
                    .Where(cu => usageIds.Contains(cu.IdUsage))
                    .Select(cu => cu.IdClient)
                    .Distinct()
                    .ToListAsync();

                var clients = await _context.Clients
                    .Where(c => clientIds.Contains(c.IdClient))
                    .ToListAsync();

                return clients.Where(c => c != null).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients de la société {SocieteId}", idSociete);
                return new List<Client>();
            }
        }

        private async Task<Societe?> GetSocieteFromClientIdAsync(int? clientId)
        {
            if (!clientId.HasValue) return null;

            try
            {
                // Récupérer les ClientUsage pour ce client
                var clientUsages = await _context.ClientUsages
                    .Where(cu => cu.IdClient == clientId)
                    .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                    .ThenInclude(cc => cc.Societe)
                    .FirstOrDefaultAsync();

                return clientUsages?.Usage?.CategorieClient?.Societe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la société pour le client {ClientId}", clientId);
                return null;
            }
        }

        private string GetStatutFinancier(decimal tauxRecouvrement, decimal montantArrieres)
        {
            if (tauxRecouvrement >= 90 && montantArrieres < 100000) return "Excellent";
            if (tauxRecouvrement >= 80 && montantArrieres < 500000) return "Bon";
            if (tauxRecouvrement >= 70 && montantArrieres < 1000000) return "Moyen";
            return "Critique";
        }

        private async Task<decimal> GetChiffreAffairesMoisAsync(int mois, int annee)
        {
            var toutesSocietes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();
            var total = 0m;

            foreach (var societe in toutesSocietes)
            {
                var clients = await GetSocieteClientsAsync(societe.IdSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var caMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.DatePaiement.Month == mois && 
                           p.DatePaiement.Year == annee && p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);

                total += caMois;
            }

            return total;
        }

        private async Task<decimal> GetEncaissementsMoisAsync(int mois, int annee)
        {
            var toutesSocietes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();
            var total = 0m;

            foreach (var societe in toutesSocietes)
            {
                var clients = await GetSocieteClientsAsync(societe.IdSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var encaissements = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.DatePaiement.Month == mois && 
                           p.DatePaiement.Year == annee && p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);

                total += encaissements;
            }

            return total;
        }

        private async Task<decimal> GetTauxRecouvrementMoisAsync(int mois, int annee)
        {
            var toutesSocietes = await _context.Societes.Where(s => s.Statut == true).ToListAsync();
            var totalFactures = 0m;
            var totalEncaissements = 0m;

            foreach (var societe in toutesSocietes)
            {
                var clients = await GetSocieteClientsAsync(societe.IdSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && f.Statut == true && 
                           f.Mois.Contains($"{mois:D2}") && f.Annees == annee)
                    .SumAsync(f => f.Montant ?? 0);

                var encaissementsMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.DatePaiement.Month == mois && 
                           p.DatePaiement.Year == annee && p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);

                totalFactures += facturesMois;
                totalEncaissements += encaissementsMois;
            }

            return totalFactures > 0 ? (totalEncaissements / totalFactures) * 100 : 0;
        }

        private async Task<List<TendanceMensuelleDto>> GetTendanceMensuelleAsync(Func<int, int, Task<decimal>> calculateur)
        {
            var tendances = new List<TendanceMensuelleDto>();
            decimal valeurPrecedente = 0m;

            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.Now.AddMonths(-i);
                var mois = date.Month;
                var annee = date.Year;
                var valeur = await calculateur(mois, annee);

                var variation = valeurPrecedente > 0 ? ((valeur - valeurPrecedente) / valeurPrecedente) * 100 : 0m;

                tendances.Add(new TendanceMensuelleDto
                {
                    Mois = $"{annee}-{mois:D2}", // Format cohérent: "2024-03"
                    Annee = annee,
                    Valeur = valeur,
                    Variation = variation
                });

                valeurPrecedente = valeur;
            }

            return tendances;
        }

        private async Task<decimal> CalculerChiffreAffairesMoisAsync(int mois, int annee, List<Societe> societes)
        {
            var debutMois = new DateTime(annee, mois, 1);
            var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
            
            var total = 0m;
            
            foreach (var societe in societes)
            {
                var clients = await GetSocieteClientsAsync(societe.IdSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();
                
                if (!clientIds.Any()) continue;
                
                var caSociete = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= finMois &&
                               p.IdClient.HasValue && 
                               clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);
                    
                total += caSociete;
            }
            
            return total;
        }

        private async Task<decimal> CalculerTauxRecouvrementMoisAsync(int mois, int annee, List<Societe> societes)
        {
            var debutMois = new DateTime(annee, mois, 1);
            var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
            
            var totalFactures = 0m;
            var totalPaiements = 0m;
            
            foreach (var societe in societes)
            {
                var clients = await GetSocieteClientsAsync(societe.IdSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();
                
                if (!clientIds.Any()) continue;
                
                // Factures créées dans le mois
                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && 
                               f.Statut == true &&
                               f.DateCreation >= debutMois && 
                               f.DateCreation <= finMois)
                    .SumAsync(f => f.Montant ?? 0);
                    
                // Paiements du mois
                var paiementsMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= finMois &&
                               p.IdClient.HasValue && 
                               clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);
                    
                totalFactures += facturesMois;
                totalPaiements += paiementsMois;
            }
            
            return totalFactures > 0 ? (totalPaiements / totalFactures) * 100 : 0m;
        }

        /// <summary>
        /// Normalise le format du mois pour gérer les deux formats: "1" et "01"
        /// </summary>
        /// <param name="mois">Mois en entrée (ex: "1", "01")</param>
        /// <returns>Mois normalisé au format "01", "02", ..., "12"</returns>
        private static string NormaliserMois(string mois)
        {
            if (string.IsNullOrWhiteSpace(mois))
                return mois;

            // Si c'est déjà au format "01", "02", etc., le retourner tel quel
            if (mois.Length == 2 && char.IsDigit(mois[0]) && char.IsDigit(mois[1]))
                return mois;

            // Si c'est un chiffre simple "1", "2", ..., "9", le convertir en "01", "02", ..., "09"
            if (mois.Length == 1 && char.IsDigit(mois[0]))
            {
                var moisNum = int.Parse(mois);
                if (moisNum >= 1 && moisNum <= 9)
                    return $"0{moisNum}";
            }

            // Si c'est "10", "11", "12", le retourner tel quel
            if (mois.Length == 2 && char.IsDigit(mois[0]) && char.IsDigit(mois[1]))
            {
                var moisNum = int.Parse(mois);
                if (moisNum >= 10 && moisNum <= 12)
                    return mois;
            }

            // Sinon, retourner la valeur originale
            return mois;
        }

        /// <summary>
        /// Récupère le top 10 des agents collecteurs du jour (toutes sociétés confondues)
        /// </summary>
        private async Task<List<TopAgentCollecteurDto>> GetTop10AgentsCollecteursAsync()
        {
            try
            {
                // Période pour le calcul (jour en cours uniquement)
                var debutJour = DateTime.Today; // 00:00:00
                var finJour = debutJour.AddDays(1).AddMilliseconds(-1); // 23:59:59

                // Étape 1: Récupérer les paiements du jour en cours
                var paiementsParAgent = await _context.Paiements
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && 
                               p.IdUtilisateur.HasValue &&
                               p.MontantPaye > 0 &&
                               p.DatePaiement >= debutJour && 
                               p.DatePaiement <= finJour)
                    .Select(p => new 
                    {
                        p.IdPaiement,
                        p.MontantPaye,
                        p.IdUtilisateur,
                        DatePaiement = p.DatePaiement
                    })
                    .ToListAsync();

                // Étape 2: Récupérer les utilisateurs avec leurs agents
                var utilisateurIds = paiementsParAgent
                    .Where(p => p.IdUtilisateur.HasValue)
                    .Select(p => p.IdUtilisateur!.Value)
                    .Distinct()
                    .ToList();

                var utilisateursAvecAgents = await _context.Utilisateurs
                    .AsNoTracking()
                    .Include(u => u.Agent)
                    .Where(u => utilisateurIds.Contains(u.IdUtilisateur) &&
                               u.IdAgent.HasValue &&
                               u.Agent != null &&
                               u.Agent.Statut == true)
                    .Select(u => new 
                    {
                        u.IdUtilisateur,
                        u.IdAgent,
                        Agent = u.Agent!
                    })
                    .ToListAsync();

                // Étape 3: Joindre en mémoire et agréger
                var result = paiementsParAgent
                    .Where(p => p.IdUtilisateur.HasValue)
                    .Join(utilisateursAvecAgents,
                          p => p.IdUtilisateur!.Value,
                          u => u.IdUtilisateur,
                          (p, u) => new { Paiement = p, Agent = u.Agent })
                    .GroupBy(x => x.Agent)
                    .Select(g => new TopAgentCollecteurDto
                    {
                        IdAgent = g.Key.IdAgent,
                        Matricule = g.Key.Matricule,
                        NomComplet = g.Key.NomComplet,
                        MontantCollecte = g.Sum(x => x.Paiement.MontantPaye),
                        NombrePaiements = g.Count()
                    })
                    .OrderByDescending(dto => dto.MontantCollecte)
                    .Take(10)
                    .ToList();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du top 10 des agents collecteurs du jour");
                return new List<TopAgentCollecteurDto>();
            }
        }

        /// <summary>
        /// Récupère le chiffre d'affaires journalier pour une société (même logique que mensuel mais pour le jour en cours)
        /// </summary>
        private async Task<decimal> GetChiffreAffairesJournalierSocieteAsync(int idSociete)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                if (!clientIds.Any())
                {
                    return 0m;
                }

                // Période pour le calcul (jour en cours uniquement)
                var debutJour = DateTime.Today; // 00:00:00
                var finJour = debutJour.AddDays(1).AddMilliseconds(-1); // 23:59:59

                // Chiffre d'affaires du jour (paiements du jour en cours)
                var caJournalier = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                               p.DatePaiement >= debutJour && 
                               p.DatePaiement <= finJour &&
                               p.IdClient.HasValue && 
                               clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => p.MontantPaye);

                return caJournalier;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul du chiffre d'affaires journalier pour la société {SocieteId}", idSociete);
                return 0m;
            }
        }

        #endregion
    }
}
