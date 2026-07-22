using Kenergie.Models.DTOs;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Kenergie.Data;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour le Dashboard Agent Direction Commercial
    /// Vue simplifiée adaptée aux agents de terrain avec leurs métriques personnelles
    /// </summary>
    public class AgentDirectionCommercialDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<AgentDirectionCommercialDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public AgentDirectionCommercialDashboardService(
            KenergieDbContext context,
            ILogger<AgentDirectionCommercialDashboardService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Récupère le dashboard complet de l'Agent Direction Commercial
        /// </summary>
        public async Task<AgentDirectionCommercialDashboardDto> GetDashboardAsync(int idSociete)
        {
            try
            {
                var currentSocieteId = _currentUserService.SocieteId != 0 ? _currentUserService.SocieteId : idSociete;
                var currentUserId = _currentUserService.UserId;

                var dashboard = new AgentDirectionCommercialDashboardDto
                {
                    // Statistiques personnelles
                    AgentStats = await GetAgentStatsAsync(currentSocieteId, currentUserId),
                    
                    // Performance personnelle
                    Performance = await GetAgentPerformanceAsync(currentSocieteId, currentUserId),
                    
                    // Clients gérés
                    ClientsGeres = await GetClientsGeresAsync(currentSocieteId, currentUserId),
                    
                    // Prospects assignés
                    Prospects = await GetProspectsAssignesAsync(currentSocieteId, currentUserId),
                    
                    // Tâches du jour
                    TachesDuJour = await GetTachesDuJourAsync(currentUserId),
                    
                    // Objectifs du mois
                    ObjectifsMois = await GetObjectifsMoisAsync(currentSocieteId, currentUserId),
                    
                    // Activités récentes
                    ActivitesRecentes = await GetActivitesRecentesAsync(currentSocieteId, currentUserId)
                };

                _logger.LogInformation("Dashboard Agent Direction Commercial généré pour l'utilisateur {UserId} - Société {IdSociete}", currentUserId, currentSocieteId);
                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du dashboard Agent Direction Commercial pour l'utilisateur {UserId}", _currentUserService.UserId);
                throw;
            }
        }

        /// <summary>
        /// Récupère les statistiques personnelles de l'agent
        /// </summary>
        private async Task<AgentStatsDto> GetAgentStatsAsync(int idSociete, int userId)
        {
            try
            {
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);

                var agent = await GetAgentByUserIdAsync(userId);
                if (agent == null)
                {
                    return new AgentStatsDto();
                }

                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                // Total des clients gérés (simulation - à adapter avec la vraie logique)
                var totalClientsGeres = clients.Count;

                // Nouveaux clients ce mois
                var nouveauxClientsMois = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= finMois);

                // Recouvrement du mois
                var recouvrementMois = await _context.Paiements
                    .Include(p => p.Client)
                    .Where(p => p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= finMois)
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                // Taux de conversion personnel
                var tauxConversionPersonnel = totalClientsGeres > 0 ? 
                    (decimal)nouveauxClientsMois / totalClientsGeres * 100 : 0;

                // Visites du mois (simulation)
                var visitesMois = 15; // À adapter avec une vraie table de visites

                // Prospects en cours
                var prospectsEnCours = 5; // À adapter avec la vraie table prospects

                // Valeur moyenne des contrats
                var valeurMoyenneContrat = nouveauxClientsMois > 0 ? 
                    recouvrementMois / nouveauxClientsMois : 0;

                // Classement dans l'équipe (simulation)
                var classementEquipe = 3;
                var totalAgentsEquipe = 8;

                return new AgentStatsDto
                {
                    TotalClientsGeres = totalClientsGeres,
                    NouveauxClientsMois = nouveauxClientsMois,
                    RecouvrementMois = Math.Round(recouvrementMois, 2),
                    TauxConversionPersonnel = Math.Round(tauxConversionPersonnel, 2),
                    VisitesMois = visitesMois,
                    ProspectsEnCours = prospectsEnCours,
                    ValeurMoyenneContrat = Math.Round(valeurMoyenneContrat, 2),
                    ClassementEquipe = classementEquipe,
                    TotalAgentsEquipe = totalAgentsEquipe
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des statistiques de l'agent {UserId}", userId);
                return new AgentStatsDto();
            }
        }

        /// <summary>
        /// Récupère la performance personnelle détaillée
        /// </summary>
        private async Task<AgentPerformancePersonnelDto> GetAgentPerformanceAsync(int idSociete, int userId)
        {
            try
            {
                var agent = await GetAgentByUserIdAsync(userId);
                if (agent == null)
                {
                    return new AgentPerformancePersonnelDto();
                }

                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);
                var debutMoisPrecedent = debutMois.AddMonths(-1);
                var finMoisPrecedent = debutMois.AddDays(-1);

                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                // Objectifs (simulation - à configurer par agent)
                var objectifRecouvrement = 2000000m;
                var objectifNouveauxClients = 10;

                // Recouvrement réalisé ce mois
                var recouvrementRealise = await _context.Paiements
                    .Include(p => p.Client)
                    .Where(p => p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= finMois)
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                // Recouvrement mois précédent
                var recouvrementMoisPrecedent = await _context.Paiements
                    .Include(p => p.Client)
                    .Where(p => p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                               p.DatePaiement >= debutMoisPrecedent && 
                               p.DatePaiement <= finMoisPrecedent)
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                // Nouveaux clients obtenus
                var nouveauxClientsObtenus = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= finMois);

                // Taux d'atteinte
                var tauxAtteinteObjectif = objectifRecouvrement > 0 ? 
                    (recouvrementRealise / objectifRecouvrement) * 100 : 0;
                var tauxAtteinteNouveauxClients = objectifNouveauxClients > 0 ? 
                    (decimal)nouveauxClientsObtenus / objectifNouveauxClients * 100 : 0;

                // Note de performance
                var notePerformance = tauxAtteinteObjectif >= 100 ? "Excellent" :
                                    tauxAtteinteObjectif >= 80 ? "Bon" :
                                    tauxAtteinteObjectif >= 60 ? "Moyen" : "À améliorer";

                // Progression par rapport au mois précédent
                var progressionMoisPrecedent = recouvrementMoisPrecedent > 0 ? 
                    ((recouvrementRealise - recouvrementMoisPrecedent) / recouvrementMoisPrecedent) * 100 : 0;

                return new AgentPerformancePersonnelDto
                {
                    IdAgent = agent.IdAgent,
                    NomAgent = agent.NomComplet ?? string.Empty,
                    ObjectifRecouvrement = objectifRecouvrement,
                    RecouvrementRealise = Math.Round(recouvrementRealise, 2),
                    TauxAtteinteObjectif = Math.Round(tauxAtteinteObjectif, 2),
                    ObjectifNouveauxClients = objectifNouveauxClients,
                    NouveauxClientsObtenus = nouveauxClientsObtenus,
                    TauxAtteinteNouveauxClients = Math.Round(tauxAtteinteNouveauxClients, 2),
                    NotePerformance = notePerformance,
                    ProgressionMoisPrecedent = Math.Round(progressionMoisPrecedent, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de la performance de l'agent {UserId}", userId);
                return new AgentPerformancePersonnelDto();
            }
        }

        /// <summary>
        /// Récupère les clients gérés par l'agent
        /// </summary>
        private async Task<List<ClientAgentDto>> GetClientsGeresAsync(int idSociete, int userId)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var clientsData = await _context.Clients
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.TypeDeCourant)
                    .Where(c => clientIds.Contains(c.IdClient))
                    .OrderByDescending(c => c.DateCreation)
                    .Take(50)
                    .ToListAsync();

                var result = new List<ClientAgentDto>();

                foreach (var client in clientsData)
                {
                    // Factures du client
                    var factures = await _context.ClientFactures
                        .Where(cf => cf.IdClient == client.IdClient)
                        .ToListAsync();

                    var montantTotalFactures = factures.Where(cf => cf.Montant.HasValue).Sum(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));
                    var montantPaye = factures.Where(cf => cf.MontantPaye.HasValue).Sum(cf => (cf.MontantPayeDevisePrincipale ?? cf.MontantPaye.Value));
                    var montantDu = factures.Where(cf => cf.MontantDu.HasValue).Sum(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                    // Dernier paiement
                    var dernierPaiement = await _context.Paiements
                        .Where(p => p.IdClient == client.IdClient)
                        .OrderByDescending(p => p.DatePaiement)
                        .FirstOrDefaultAsync();

                    // Priorité de suivi
                    var prioriteSuivi = montantDu > 100000 ? "Haute" :
                                       montantDu > 50000 ? "Moyenne" : "Basse";

                    result.Add(new ClientAgentDto
                    {
                        IdClient = client.IdClient,
                        NomClient = client.NomClient ?? string.Empty,
                        Telephone = client.Telephone ?? string.Empty,
                        Email = client.EmailClient ?? string.Empty,
                        Adresse = client.AdresseClient ?? string.Empty,
                        Statut = client.IsActif ? "Actif" : "Inactif",
                        DerniereVisite = null, // À adapter avec une vraie table de visites
                        MontantTotalFactures = Math.Round(montantTotalFactures, 2),
                        MontantPaye = Math.Round(montantPaye, 2),
                        MontantDu = Math.Round(montantDu, 2),
                        DernierPaiement = dernierPaiement?.DatePaiement,
                        TypeDeCourant = client.ClientsUsages?
                            .Where(cu => cu.Statut && !string.IsNullOrEmpty(cu.TypeDeCourant?.Libelle))
                            .OrderBy(cu => cu.IdClientUsage)
                            .Select(cu => cu.TypeDeCourant!.Libelle)
                            .FirstOrDefault() ?? "Non défini",
                        PrioriteSuivi = prioriteSuivi
                    });
                }

                return result.OrderByDescending(c => c.MontantDu).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients gérés par l'agent {UserId}", userId);
                return new List<ClientAgentDto>();
            }
        }

        /// <summary>
        /// Récupère les prospects assignés à l'agent
        /// </summary>
        private async Task<List<ProspectAgentDto>> GetProspectsAssignesAsync(int idSociete, int userId)
        {
            try
            {
                // Simulation - à adapter avec une vraie table prospects
                var prospects = new List<ProspectAgentDto>
                {
                    new ProspectAgentDto
                    {
                        IdProspect = 1,
                        NomProspect = "Entreprise ABC",
                        Telephone = "0123456789",
                        Email = "contact@entreprise-abc.com",
                        Adresse = "123 Rue de la République, Paris",
                        Statut = "En négociation",
                        PotentielEstime = 75000,
                        DateDernierContact = DateTime.Now.AddDays(-2),
                        ProchaineAction = "Appel de suivi",
                        DateProchaineAction = DateTime.Now.AddDays(1),
                        Priorite = "Haute",
                        Notes = "Intéressé par l'offre premium",
                        DateCreation = DateTime.Now.AddDays(-10)
                    },
                    new ProspectAgentDto
                    {
                        IdProspect = 2,
                        NomProspect = "Société XYZ",
                        Telephone = "0234567890",
                        Email = "info@societe-xyz.fr",
                        Adresse = "456 Avenue des Champs-Élysées, Paris",
                        Statut = "Premier contact",
                        PotentielEstime = 50000,
                        DateDernierContact = DateTime.Now.AddDays(-5),
                        ProchaineAction = "Envoyer documentation",
                        DateProchaineAction = DateTime.Now.AddDays(2),
                        Priorite = "Moyenne",
                        Notes = "Demande de documentation technique",
                        DateCreation = DateTime.Now.AddDays(-15)
                    }
                };

                return prospects.OrderBy(p => p.Priorite == "Haute" ? 0 : p.Priorite == "Moyenne" ? 1 : 2).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prospects de l'agent {UserId}", userId);
                return new List<ProspectAgentDto>();
            }
        }

        /// <summary>
        /// Récupère les tâches du jour
        /// </summary>
        private async Task<List<TacheDto>> GetTachesDuJourAsync(int userId)
        {
            try
            {
                // Simulation - à adapter avec une vraie table de tâches
                var taches = new List<TacheDto>
                {
                    new TacheDto
                    {
                        IdTache = 1,
                        Titre = "Visite client Entreprise ABC",
                        Description = "Suivi mensuel et présentation nouvelle offre",
                        TypeTache = "Visite client",
                        Priorite = "Haute",
                        HeurePrevue = new TimeSpan(10, 0, 0),
                        Statut = "À faire",
                        EntiteConcernee = "Entreprise ABC",
                        DateCreation = DateTime.Now.AddDays(-1),
                        DateEcheance = DateTime.Today
                    },
                    new TacheDto
                    {
                        IdTache = 2,
                        Titre = "Appel prospect Société XYZ",
                        Description = "Suivi après envoi documentation",
                        TypeTache = "Appel téléphonique",
                        Priorite = "Moyenne",
                        HeurePrevue = new TimeSpan(14, 30, 0),
                        Statut = "À faire",
                        EntiteConcernee = "Société XYZ",
                        DateCreation = DateTime.Now.AddDays(-2),
                        DateEcheance = DateTime.Today
                    },
                    new TacheDto
                    {
                        IdTache = 3,
                        Titre = "Rapport hebdomadaire",
                        Description = "Compiler les résultats de la semaine",
                        TypeTache = "Administratif",
                        Priorite = "Basse",
                        HeurePrevue = new TimeSpan(17, 0, 0),
                        Statut = "À faire",
                        EntiteConcernee = "Interne",
                        DateCreation = DateTime.Now.AddDays(-3),
                        DateEcheance = DateTime.Today
                    }
                };

                return taches.OrderBy(t => t.Priorite == "Haute" ? 0 : t.Priorite == "Moyenne" ? 1 : 2)
                            .ThenBy(t => t.HeurePrevue)
                            .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tâches de l'agent {UserId}", userId);
                return new List<TacheDto>();
            }
        }

        /// <summary>
        /// Récupère les objectifs du mois et progression
        /// </summary>
        private async Task<ObjectifsMoisDto> GetObjectifsMoisAsync(int idSociete, int userId)
        {
            try
            {
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);
                var joursRestants = (finMois - DateTime.Now).Days + 1;

                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                // Objectifs (simulation - à configurer par agent)
                var objectifRecouvrement = 2000000m;
                var objectifNouveauxClients = 10;
                var objectifVisites = 20;

                // Réalisations du mois
                var recouvrementActuel = await _context.Paiements
                    .Include(p => p.Client)
                    .Where(p => p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= DateTime.Now)
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                var nouveauxClientsActuels = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= DateTime.Now);
                var visitesRealisees = 15; // Simulation

                // Progressions
                var progressionRecouvrement = objectifRecouvrement > 0 ? 
                    (recouvrementActuel / objectifRecouvrement) * 100 : 0;
                var progressionNouveauxClients = objectifNouveauxClients > 0 ? 
                    (decimal)nouveauxClientsActuels / objectifNouveauxClients * 100 : 0;
                var progressionVisites = objectifVisites > 0 ? 
                    (decimal)visitesRealisees / objectifVisites * 100 : 0;

                var nomMois = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month);

                return new ObjectifsMoisDto
                {
                    Mois = nomMois,
                    Annee = DateTime.Now.Year,
                    ObjectifRecouvrement = objectifRecouvrement,
                    RecouvrementActuel = Math.Round(recouvrementActuel, 2),
                    ProgressionRecouvrement = Math.Round(progressionRecouvrement, 2),
                    ObjectifNouveauxClients = objectifNouveauxClients,
                    NouveauxClientsActuels = nouveauxClientsActuels,
                    ProgressionNouveauxClients = Math.Round(progressionNouveauxClients, 2),
                    ObjectifVisites = objectifVisites,
                    VisitesRealisees = visitesRealisees,
                    ProgressionVisites = Math.Round(progressionVisites, 2),
                    JoursRestants = joursRestants
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des objectifs de l'agent {UserId}", userId);
                return new ObjectifsMoisDto();
            }
        }

        /// <summary>
        /// Récupère les activités récentes de l'agent
        /// </summary>
        private async Task<List<ActiviteRecenteDto>> GetActivitesRecentesAsync(int idSociete, int userId)
        {
            try
            {
                var debutSemaine = DateTime.Now.AddDays(-7);
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var activites = new List<ActiviteRecenteDto>();

                // Paiements récents
                var paiements = await _context.Paiements
                    .Include(p => p.Client)
                    .Where(p => p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                               p.DatePaiement >= debutSemaine)
                    .OrderByDescending(p => p.DatePaiement)
                    .Take(10)
                    .ToListAsync();

                foreach (var paiement in paiements)
                {
                    activites.Add(new ActiviteRecenteDto
                    {
                        IdActivite = activites.Count + 1,
                        TypeActivite = "Paiement",
                        Description = $"Paiement reçu de {paiement.Client?.NomClient}",
                        EntiteConcernee = paiement.Client?.NomClient ?? "Client inconnu",
                        MontantConcerne = paiement.MontantPaye,
                        DateActivite = paiement.DatePaiement,
                        Statut = "Complété",
                        Commentaires = $"Référence: PAY-{paiement.IdPaiement:D6}"
                    });
                }

                // Nouveaux clients récents
                var nouveauxClients = clients
                    .Where(c => c.DateCreation >= debutSemaine)
                    .OrderByDescending(c => c.DateCreation)
                    .Take(5)
                    .ToList();

                foreach (var client in nouveauxClients)
                {
                    activites.Add(new ActiviteRecenteDto
                    {
                        IdActivite = activites.Count + 1,
                        TypeActivite = "Nouveau client",
                        Description = "Ajout d'un nouveau client",
                        EntiteConcernee = client.NomClient ?? "Client inconnu",
                        DateActivite = client.DateCreation,
                        Statut = "Complété",
                        Commentaires = $"Type: {client.ClientsUsages?.Where(cu => cu.Statut && !string.IsNullOrEmpty(cu.TypeDeCourant?.Libelle)).OrderBy(cu => cu.IdClientUsage).Select(cu => cu.TypeDeCourant!.Libelle).FirstOrDefault() ?? "Non défini"}"
                    });
                }

                return activites.OrderByDescending(a => a.DateActivite).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des activités récentes de l'agent {UserId}", userId);
                return new List<ActiviteRecenteDto>();
            }
        }

        #region Méthodes utilitaires

        private async Task<Agent?> GetAgentByUserIdAsync(int userId)
        {
            try
            {
                return await _context.Agents
                    .Include(a => a.Societe)
                    .FirstOrDefaultAsync(a => a.Utilisateurs.Any(u => u.IdUtilisateur == userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'agent {UserId}", userId);
                return null;
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
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.TypeDeCourant)
                    .ToListAsync();

                return clients.Where(c => c != null).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients de la société {SocieteId}", idSociete);
                return new List<Client>();
            }
        }

        #endregion
    }
}
