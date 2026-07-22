using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class CaissierDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<CaissierDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public CaissierDashboardService(
            KenergieDbContext context, 
            ILogger<CaissierDashboardService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<CaissierDashboardDto> GetDashboardDataAsync(int? idUtilisateur = null)
        {
            try
            {
                var societeId = _currentUserService.GetSocieteId();
                if (societeId == 0)
                {
                    _logger.LogWarning("ID de société non trouvé pour le caissier");
                    throw new UnauthorizedAccessException("ID de société non trouvé");
                }

                // Si idUtilisateur n'est pas spécifié, utiliser l'utilisateur connecté
                var targetUserId = idUtilisateur ?? _currentUserService.GetUserId();

                _logger.LogInformation("Génération du dashboard Caissier pour la société {SocieteId}, utilisateur {TargetUserId}", societeId, targetUserId);

                // Exécuter les requêtes séquentiellement pour éviter les problèmes de concurrence DbContext
                var statistiquesJournalieres = await GetStatistiquesJournalieresAsync(societeId, targetUserId);
                var paiementsEnCours = await GetPaiementsEnCoursAsync(societeId, targetUserId);
                var paiementsRecents = await GetPaiementsRecentsAsync(societeId, targetUserId);
                var recettesJournalieres = await GetRecettesJournalieresAsync(societeId, targetUserId);
                var alertesCaissier = await GetAlertesCaissierAsync(societeId, targetUserId);
                var resumeCaisse = await GetResumeCaisseAsync(societeId, targetUserId);

                return new CaissierDashboardDto
                {
                    StatistiquesJournalieres = statistiquesJournalieres,
                    PaiementsEnCours = paiementsEnCours,
                    PaiementsRecents = paiementsRecents,
                    RecettesJournalieres = recettesJournalieres,
                    AlertesCaissier = alertesCaissier,
                    ResumeCaisse = resumeCaisse,
                    DateGeneration = DateTime.UtcNow
                };
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Accès non autorisé au dashboard Caissier");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du dashboard Caissier");
                
                // Retourner un dashboard vide en cas d'erreur pour éviter l'erreur 500
                return new CaissierDashboardDto
                {
                    StatistiquesJournalieres = new CaissierStatistiquesDto(),
                    PaiementsEnCours = new List<PaiementEnCoursDto>(),
                    PaiementsRecents = new List<PaiementRecentDto>(),
                    RecettesJournalieres = new List<RecetteJournaliereDto>(),
                    AlertesCaissier = new List<AlerteCaissierDto>(),
                    ResumeCaisse = new ResumeCaisseDto(),
                    DateGeneration = DateTime.UtcNow
                };
            }
        }

        public async Task<CaissierStatistiquesDto> GetStatistiquesJournalieresAsync(int societeId, int? idUtilisateur = null)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(societeId);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                if (!clientIds.Any())
                {
                    _logger.LogWarning("Aucun client trouvé pour la société {SocieteId}", societeId);
                    return new CaissierStatistiquesDto();
                }

            // Total des recettes du jour
            var totalRecettes = await _context.Paiements
                .Where(p => !p.IsDeleted && p.DatePaiement.Date == DateTime.Today && 
                       p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            // Nombre de transactions du jour
            var nombreTransactions = await _context.Paiements
                .Where(p => !p.IsDeleted && p.DatePaiement.Date == DateTime.Today && 
                       p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .CountAsync();

            // Plus gros et plus petit montant du jour
            var montants = await _context.Paiements
                .Where(p => !p.IsDeleted && p.DatePaiement.Date == DateTime.Today && 
                       p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .Select(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye))
                .ToListAsync();

            var plusGrosMontant = montants.Any() ? montants.Max() : 0;
            var plusPetitMontant = montants.Any() ? montants.Min() : 0;

            // Total des arriérés
            var totalArrieres = await _context.ClientFactures
                .Where(f => clientIds.Contains(f.IdClient) && f.Statut == true)
                .SumAsync(f => (f.MontantDuDevisePrincipale ?? f.MontantDu ?? 0));

            return new CaissierStatistiquesDto
                {
                    TotalRecettes = totalRecettes,
                    NombreTransactions = nombreTransactions,
                    MoyenneTransaction = nombreTransactions > 0 ? totalRecettes / nombreTransactions : 0,
                    PlusGrosMontant = plusGrosMontant,
                    PlusPetitMontant = plusPetitMontant,
                    NombreClients = clients.Count,
                    TotalArrieres = totalArrieres
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques journalières pour la société {SocieteId}", societeId);
                return new CaissierStatistiquesDto();
            }
        }

        public async Task<List<PaiementEnCoursDto>> GetPaiementsEnCoursAsync(int societeId, int? idUtilisateur = null)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(societeId);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                if (!clientIds.Any())
                {
                    return new List<PaiementEnCoursDto>();
                }

            var paiements = await _context.Paiements
                .Include(p => p.Client)
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && 
                       clientIds.Contains(p.IdClient.Value) && 
                       (p.Statut == "En attente" || p.Statut == "Partiel") &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .OrderByDescending(p => p.DatePaiement)
                .Take(20)
                .ToListAsync();

            var result = new List<PaiementEnCoursDto>();
            
            foreach (var paiement in paiements)
            {
                result.Add(new PaiementEnCoursDto
                {
                    IdPaiement = paiement.IdPaiement,
                    Reference = $"PAY-{paiement.IdPaiement:D6}",
                    NomClient = paiement.Client?.NomClient ?? "Client inconnu",
                    MontantAPaye = paiement.MontantAPaye ?? 0,
                    MontantVerse = paiement.MontantPaye,
                    ResteAPayer = paiement.ResteAPaye ?? 0,
                    DatePaiement = paiement.DatePaiement,
                    MethodePaiement = paiement.MethodePaiement ?? "Non spécifié",
                    Statut = paiement.Statut
                });
            }

            return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements en cours pour la société {SocieteId}", societeId);
                return new List<PaiementEnCoursDto>();
            }
        }

        public async Task<List<PaiementRecentDto>> GetPaiementsRecentsAsync(int societeId, int? idUtilisateur = null)
        {
            var clients = await GetSocieteClientsAsync(societeId);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            var paiements = await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => !p.IsDeleted && p.DatePaiement.Date >= DateTime.Today.AddDays(-7) && 
                       p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .OrderByDescending(p => p.DatePaiement)
                .Take(20)
                .ToListAsync();

            var result = new List<PaiementRecentDto>();
            
            foreach (var paiement in paiements)
            {
                result.Add(new PaiementRecentDto
                {
                    IdPaiement = paiement.IdPaiement,
                    Reference = $"PAY-{paiement.IdPaiement:D6}",
                    NomClient = paiement.Client?.NomClient ?? "Client inconnu",
                    MontantPaye = paiement.MontantPaye,
                    DatePaiement = paiement.DatePaiement,
                    MethodePaiement = paiement.MethodePaiement ?? "Non spécifié",
                    Statut = paiement.Statut,
                    UtilisateurEnregistrement = paiement.Utilisateur?.NomComplet ?? "Système"
                });
            }

            return result;
        }

        public async Task<List<RecetteJournaliereDto>> GetRecettesJournalieresAsync(int societeId, int? idUtilisateur = null)
        {
            var clients = await GetSocieteClientsAsync(societeId);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            var recettes = new List<RecetteJournaliereDto>();
            
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Today.AddDays(-i);
                
                var paiementsJour = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.DatePaiement.Date == date && 
                           p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                           (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                    .ToListAsync();

                var totalMontant = paiementsJour.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                var nombreTransactions = paiementsJour.Count;

                var recetteEspece = paiementsJour
                    .Where(p => p.MethodePaiement?.ToLower() == "espèces")
                    .Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                
                var recetteMobileMoney = paiementsJour
                    .Where(p => p.MethodePaiement?.ToLower().Contains("mobile") == true)
                    .Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                
                var recetteVirement = paiementsJour
                    .Where(p => p.MethodePaiement?.ToLower() == "virement")
                    .Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                
                var recetteCarte = paiementsJour
                    .Where(p => p.MethodePaiement?.ToLower() == "carte")
                    .Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                recettes.Add(new RecetteJournaliereDto
                {
                    Date = date,
                    MontantTotal = totalMontant,
                    NombreTransactions = nombreTransactions,
                    RecetteEspece = recetteEspece,
                    RecetteMobileMoney = recetteMobileMoney,
                    RecetteVirement = recetteVirement,
                    RecetteCarte = recetteCarte
                });
            }

            return recettes;
        }

        public async Task<List<AlerteCaissierDto>> GetAlertesCaissierAsync(int societeId, int? idUtilisateur = null)
        {
            var clients = await GetSocieteClientsAsync(societeId);
            var clientIds = clients.Select(c => c.IdClient).ToList();
            var alertes = new List<AlerteCaissierDto>();

            // Alerte pour paiements en attente depuis plus de 24h
            var paiementsEnAttente = await _context.Paiements
                .Include(p => p.Client)
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && 
                       clientIds.Contains(p.IdClient.Value) && 
                       p.Statut == "En attente" && 
                       p.DatePaiement < DateTime.Now.AddHours(-24) &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .ToListAsync();

            foreach (var paiement in paiementsEnAttente)
            {
                alertes.Add(new AlerteCaissierDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Paiement en attente",
                    Description = $"Paiement en attente depuis plus de 24h pour {paiement.Client?.NomClient}",
                    NiveauCriticite = "Moyenne",
                    DateAlerte = DateTime.Now,
                    IdClient = paiement.IdClient ?? 0,
                    NomClient = paiement.Client?.NomClient ?? "Client inconnu",
                    MontantConcerne = paiement.MontantAPaye ?? 0,
                    EstLue = false
                });
            }

            // Alerte pour gros montants non validés
            var grosMontantsEnAttente = await _context.Paiements
                .Include(p => p.Client)
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && 
                       clientIds.Contains(p.IdClient.Value) && 
                       p.Statut == "En attente" && 
                       p.MontantAPaye > 1000000 &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .ToListAsync();

            foreach (var paiement in grosMontantsEnAttente)
            {
                alertes.Add(new AlerteCaissierDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Gros montant en attente",
                    Description = $"Gros montant en attente de validation: {paiement.MontantAPaye:N0} FC pour {paiement.Client?.NomClient}",
                    NiveauCriticite = "Élevée",
                    DateAlerte = DateTime.Now,
                    IdClient = paiement.IdClient ?? 0,
                    NomClient = paiement.Client?.NomClient ?? "Client inconnu",
                    MontantConcerne = paiement.MontantAPaye ?? 0,
                    EstLue = false
                });
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        public async Task<ResumeCaisseDto> GetResumeCaisseAsync(int societeId, int? idUtilisateur = null)
        {
            var clients = await GetSocieteClientsAsync(societeId);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            // Total des entrées du jour
            var totalEntrees = await _context.Paiements
                .Where(p => !p.IsDeleted && p.DatePaiement.Date == DateTime.Today && 
                       p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) && 
                       p.Statut == "Validé" &&
                       (!idUtilisateur.HasValue || p.IdUtilisateur == idUtilisateur.Value))  // NOUVEAU FILTRE
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            // Solde initial (simulé - à adapter selon votre logique métier)
            var soldeInitial = 0m;

            // Total des sorties (simulé - à adapter selon votre logique métier)
            var totalSorties = 0m;

            var soldeFinal = soldeInitial + totalEntrees - totalSorties;
            var ecart = 0m; // À calculer selon votre logique de caisse

            return new ResumeCaisseDto
            {
                SoldeInitial = soldeInitial,
                TotalEntrees = totalEntrees,
                TotalSorties = totalSorties,
                SoldeFinal = soldeFinal,
                Ecart = ecart,
                DateCloture = DateTime.Now,
                StatutCaisse = "Ouverte"
            };
        }

        #region Méthodes utilitaires

        private async Task<List<Client>> GetSocieteClientsAsync(int societeId)
        {
            try
            {
                // Récupérer les IDs des catégories de clients pour cette société
                var categorieIds = await _context.CategorieClients
                    .Where(cc => cc.IdSociete == societeId)
                    .Select(cc => cc.IdCategorie)
                    .ToListAsync();

                if (!categorieIds.Any())
                {
                    _logger.LogWarning("Aucune catégorie de client trouvée pour la société {SocieteId}", societeId);
                    return new List<Client>();
                }

                // Récupérer les IDs des usages pour ces catégories
                var usageIds = await _context.Usages
                    .Where(u => categorieIds.Contains(u.IdCategorieClient))
                    .Select(u => u.IdUsage)
                    .ToListAsync();

                if (!usageIds.Any())
                {
                    _logger.LogWarning("Aucun usage trouvé pour les catégories de la société {SocieteId}", societeId);
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
                _logger.LogError(ex, "Erreur lors de la récupération des clients de la société {SocieteId}", societeId);
                return new List<Client>();
            }
        }

        #endregion
    }
}
