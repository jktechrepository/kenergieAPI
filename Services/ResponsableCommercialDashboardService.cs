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
    /// Service pour le Dashboard du Responsable Commercial
    /// Combine les statistiques financières du FinancierDashboard avec des métriques commerciales
    /// </summary>
    public class ResponsableCommercialDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<ResponsableCommercialDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRapportFinancierUsdEnrichmentService _usdEnrichment;

        public ResponsableCommercialDashboardService(
            KenergieDbContext context,
            ILogger<ResponsableCommercialDashboardService> logger,
            ICurrentUserService currentUserService,
            IRapportFinancierUsdEnrichmentService usdEnrichment)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
            _usdEnrichment = usdEnrichment;
        }

        /// <summary>
        /// Récupère le dashboard complet du Responsable Commercial
        /// </summary>
        public async Task<ResponsableCommercialDashboardDto> GetDashboardAsync(int idSociete)
        {
            try
            {
                var currentSocieteId = _currentUserService.SocieteId != 0 ? _currentUserService.SocieteId : idSociete;
                
                var dashboard = new ResponsableCommercialDashboardDto
                {
                    // Statistiques financières (héritées du FinancierDashboard)
                    GlobalStatistiques = await GetGlobalStatistiquesAsync(currentSocieteId),
                    
                    // Statistiques commerciales spécifiques
                    CommercialStats = await GetCommercialStatsAsync(currentSocieteId),
                    
                    // Performance des agents
                    AgentsPerformance = await GetAgentsPerformanceAsync(currentSocieteId),
                    
                    // Acquisition de clients
                    ClientAcquisitions = await GetClientAcquisitionsAsync(currentSocieteId),
                    
                    // Prospects et opportunités
                    Prospects = await GetProspectsAsync(currentSocieteId),
                    
                    // Tendances commerciales
                    TendancesCommerciales = await GetTendancesCommercialesAsync(currentSocieteId),
                    
                    // Top 10 des agents collecteurs du jour
                    Top10AgentsCollecteurs = await GetTop10AgentsCollecteursAsync()
                };

                _logger.LogInformation("Dashboard Responsable Commercial généré pour la société {IdSociete}", currentSocieteId);
                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du dashboard Responsable Commercial pour la société {IdSociete}", idSociete);
                throw;
            }
        }

        /// <summary>
        /// Récupère les statistiques financières globales (réutilisation du FinancierDashboard)
        /// </summary>
        private async Task<GlobalFinancierStatistiquesDto> GetGlobalStatistiquesAsync(int idSociete)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                if (!clientIds.Any())
                {
                    return new GlobalFinancierStatistiquesDto();
                }

                // Périodes pour le calcul (même logique que le dashboard standard)
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
                var debutMoisPrecedent = debutMois.AddMonths(-1);
                var finMoisPrecedent = debutMois.AddDays(-1).AddHours(23).AddMinutes(59);

                // Normalisation du mois (gère les formats "1" et "01")
                var moisActuelNormalise = NormaliserMois(DateTime.Now.Month.ToString());
                var moisPrecedentNormalise = NormaliserMois(debutMoisPrecedent.Month.ToString());

                // Paiements du mois en cours (même logique que DashboardService)
                var paiementsDuMois = await _context.Paiements
                    .Include(p => p.Client)
                    .Include(p => p.Facture)
                    .Where(p => !p.IsDeleted && 
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= finMois &&
                               p.IdClient.HasValue && 
                               clientIds.Contains(p.IdClient.Value))
                    .ToListAsync();

                var chiffreAffairesTotal = paiementsDuMois.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                var nombreTotalTransactions = paiementsDuMois.Count;
                var moyenneTransaction = nombreTotalTransactions > 0 ? chiffreAffairesTotal / nombreTotalTransactions : 0;

                // Factures du mois précédent (même logique que DashboardService)
                var facturesMoisPrecedent = await _context.ClientFactures
                    .Include(cf => cf.Client)
                    .Where(cf => clientIds.Contains(cf.IdClient) && 
                                  cf.Statut == true &&
                                  cf.Mois == moisPrecedentNormalise &&
                                  cf.Annees == debutMoisPrecedent.Year)
                    .ToListAsync();

                var montantMoisPrecedent = facturesMoisPrecedent.Sum(cf => (cf.MontantDevisePrincipale ?? cf.Montant ?? 0));

                // Calcul des arriérés (uniquement les montants dus > 0)
                var facturesImpayees = await _context.ClientFactures
                    .Include(cf => cf.Client)
                    .Where(cf => clientIds.Contains(cf.IdClient) && 
                                  cf.Statut == true &&
                                  cf.MontantDu.HasValue && cf.MontantDu.Value > 0)
                    .ToListAsync();

                var montantTotalArrieres = facturesImpayees.Sum(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                var montantTotalFacturesSociete = await _context.ClientFactures
                    .Where(f => f.Statut == true && clientIds.Contains(f.IdClient))
                    .SumAsync(f => (f.MontantDevisePrincipale ?? f.Montant ?? 0));

                var montantTotalPaiementsSociete = await _context.Paiements
                    .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                var totalGeneralArriere = montantTotalFacturesSociete - montantTotalPaiementsSociete;

                var debutJour = DateTime.Today;
                var finJour = debutJour.AddDays(1).AddTicks(-1);
                var chiffreAffairesJournalier = await _context.Paiements
                    .Where(p => !p.IsDeleted &&
                                p.DatePaiement >= debutJour &&
                                p.DatePaiement <= finJour &&
                                p.IdClient.HasValue &&
                                clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                // Taux de recouvrement global (même formule que tauxRecouvrementEstime du DashboardService)
                var tauxRecouvrementGlobal = montantMoisPrecedent > 0
                    ? Math.Round((chiffreAffairesTotal / montantMoisPrecedent) * 100, 2)
                    : 0;

                return new GlobalFinancierStatistiquesDto
                {
                    ChiffreAffairesTotal = chiffreAffairesTotal,
                    MontantTotalEncaisse = chiffreAffairesTotal,
                    MontantTotalArrieres = montantTotalArrieres,
                    TotalGeneralArriere = totalGeneralArriere,
                    MontantMoisPrecedent = montantMoisPrecedent,
                    TauxRecouvrementGlobal = tauxRecouvrementGlobal,
                    NombreTotalTransactions = nombreTotalTransactions,
                    MoyenneTransaction = Math.Round(moyenneTransaction, 2),
                    ChiffreAffairesJournalier = chiffreAffairesJournalier,
                    SyntheseUsd = await _usdEnrichment.BuildGlobalFinancierSyntheseUsdAsync(new[]
                    {
                        (idSociete, chiffreAffairesTotal, chiffreAffairesTotal, montantTotalArrieres, totalGeneralArriere, chiffreAffairesJournalier)
                    })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des statistiques financières pour la société {Idociete}", idSociete);
                return new GlobalFinancierStatistiquesDto();
            }
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
        /// Récupère les statistiques commerciales spécifiques
        /// </summary>
        private async Task<CommercialStatsDto> GetCommercialStatsAsync(int idSociete)
        {
            try
            {
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);

                // Total clients actifs
                var totalClientsActifs = clients.Count(c => c.IsActif);

                // Nouveaux clients ce mois
                var nouveauxClientsMois = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= finMois);

                // Total agents Direction Commercial
                var totalAgentsDirection = await _context.Agents
                    .Where(a => a.Societe != null && 
                               a.Societe.IdSociete == idSociete && 
                               a.RoleAgent == "Agent Direction Commercial" && 
                               a.Statut == true)
                    .CountAsync();

                // Chiffre d'affaires commercial (basé sur les nouveaux clients)
                var chiffreAffairesCommercial = await _context.ClientFactures
                    .Include(cf => cf.Client)
                    .Where(cf => clientIds.Contains(cf.IdClient) && 
                                  cf.Client.DateCreation >= debutMois && 
                                  cf.Client.DateCreation <= finMois)
                    .Where(cf => cf.Montant.HasValue)
                    .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));

                // Taux de conversion (simplifié - basé sur les prospects convertis)
                var tauxConversion = totalClientsActifs > 0 ? 
                    (decimal)nouveauxClientsMois / totalClientsActifs * 100 : 0;

                // Prospects en cours (simulation - à adapter avec une vraie table prospects)
                var prospectsEnCours = 0; // À implémenter avec la table prospects

                var valeurMoyenneContrat = nouveauxClientsMois > 0 ? 
                    chiffreAffairesCommercial / nouveauxClientsMois : 0;

                return new CommercialStatsDto
                {
                    TotalClientsActifs = totalClientsActifs,
                    NouveauxClientsMois = nouveauxClientsMois,
                    TauxConversion = Math.Round(tauxConversion, 2),
                    TotalAgentsDirection = totalAgentsDirection,
                    ChiffreAffairesCommercial = Math.Round(chiffreAffairesCommercial, 2),
                    ProspectsEnCours = prospectsEnCours,
                    ValeurMoyenneContrat = Math.Round(valeurMoyenneContrat, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des statistiques commerciales pour la société {SocieteId}", idSociete);
                return new CommercialStatsDto();
            }
        }

        /// <summary>
        /// Récupère la performance des caissiers sous la responsabilité
        /// </summary>
        private async Task<List<AgentPerformanceDto>> GetAgentsPerformanceAsync(int idSociete)
        {
            try
            {
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1);

                var agents = await _context.Agents
                    .Include(a => a.Societe)
                    .Where(a => a.Societe != null && 
                               a.Societe.IdSociete == idSociete && 
                               a.RoleAgent == "Caissier" && 
                               a.Statut == true)
                    .ToListAsync();

                var performanceList = new List<AgentPerformanceDto>();
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                foreach (var agent in agents)
                {
                    // Clients gérés par le caissier (via les clients créés)
                    var clientsGeres = clients.Count;

                    // Recouvrement du mois
                    var recouvrementMois = await _context.Paiements
                        .Include(p => p.Client)
                        .Where(p => p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                                   p.DatePaiement >= debutMois && 
                                   p.DatePaiement <= finMois)
                        .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                    // Nouveaux clients ce mois pour ce caissier
                    var nouveauxClientsMois = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= finMois);

                    // Taux d'atteinte d'objectif (simulation)
                    var objectif = 1000000m; // À configurer par caissier
                    var tauxAtteinteObjectif = objectif > 0 ? 
                        (recouvrementMois / objectif) * 100 : 0;

                    var tauxConversion = clientsGeres > 0 ? 
                        (decimal)nouveauxClientsMois / clientsGeres * 100 : 0;

                    // Statut de performance
                    var statutPerformance = tauxAtteinteObjectif >= 100 ? "Excellent" :
                                          tauxAtteinteObjectif >= 80 ? "Bon" :
                                          tauxAtteinteObjectif >= 60 ? "Moyen" : "Faible";

                    performanceList.Add(new AgentPerformanceDto
                    {
                        IdAgent = agent.IdAgent,
                        NomAgent = agent.NomComplet ?? string.Empty,
                        Matricule = agent.Matricule ?? string.Empty,
                        ClientsGeres = clientsGeres,
                        RecouvrementMois = Math.Round(recouvrementMois, 2),
                        NouveauxClientsMois = nouveauxClientsMois,
                        TauxAtteinteObjectif = Math.Round(tauxAtteinteObjectif, 2),
                        TauxConversion = Math.Round(tauxConversion, 2),
                        StatutPerformance = statutPerformance,
                        DerniereActivite = agent.DateCreation
                    });
                }

                return performanceList.OrderByDescending(p => p.RecouvrementMois).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la performance des caissiers pour la société {SocieteId}", idSociete);
                return new List<AgentPerformanceDto>();
            }
        }

        /// <summary>
        /// Récupère les acquisitions récentes de nouveaux clients
        /// </summary>
        private async Task<List<ClientAcquisitionDto>> GetClientAcquisitionsAsync(int idSociete)
        {
            try
            {
                var debutMois = DateTime.Now.AddDays(-30); // 30 derniers jours
                var clients = await GetSocieteClientsAsync(idSociete);

                var acquisitions = clients
                    .Where(c => c.DateCreation >= debutMois)
                    .OrderByDescending(c => c.DateCreation)
                    .Take(20)
                    .Select(c => new ClientAcquisitionDto
                    {
                        IdClient = c.IdClient,
                        NomClient = c.NomClient ?? string.Empty,
                        Telephone = c.Telephone ?? string.Empty,
                        EmailClient = c.EmailClient ?? string.Empty,
                        AgentResponsable = "Non assigné", // Pas de liaison directe dans le modèle
                        DateCreation = c.DateCreation,
                        MontantPremierContrat = 0, // À calculer depuis ClientFacture
                        TypeDeCourant = c.ClientsUsages?
                            .Where(cu => cu.Statut && !string.IsNullOrEmpty(cu.TypeDeCourant?.Libelle))
                            .OrderBy(cu => cu.IdClientUsage)
                            .Select(cu => cu.TypeDeCourant!.Libelle)
                            .FirstOrDefault() ?? "Non défini",
                        Societe = "Société" // À récupérer via la méthode utilitaire
                    })
                    .ToList();

                return acquisitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des acquisitions de clients pour la société {SocieteId}", idSociete);
                return new List<ClientAcquisitionDto>();
            }
        }

        /// <summary>
        /// Récupère les prospects et opportunités commerciales
        /// </summary>
        private async Task<List<ProspectDto>> GetProspectsAsync(int idSociete)
        {
            try
            {
                // Simulation - à adapter avec une vraie table prospects
                var prospects = new List<ProspectDto>
                {
                    new ProspectDto
                    {
                        IdProspect = 1,
                        NomProspect = "Entreprise ABC",
                        Telephone = "0123456789",
                        Email = "contact@entreprise-abc.com",
                        AgentAssigné = "Agent Commercial 1",
                        Statut = "En négociation",
                        PotentielEstime = 50000,
                        DateDernierContact = DateTime.Now.AddDays(-2),
                        DateCreation = DateTime.Now.AddDays(-10),
                        Priorite = "Haute"
                    }
                    // Ajouter d'autres prospects simulés si nécessaire
                };

                return prospects.Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prospects pour la société {SocieteId}", idSociete);
                return new List<ProspectDto>();
            }
        }

        /// <summary>
        /// Récupère les tendances commerciales sur 12 mois
        /// </summary>
        private async Task<TendancesCommercialesDto> GetTendancesCommercialesAsync(int idSociete)
        {
            try
            {
                var tendances = new TendancesCommercialesDto();
                var dateActuelle = DateTime.Now;
                var clients = await GetSocieteClientsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                for (int i = 11; i >= 0; i--)
                {
                    var dateMois = dateActuelle.AddMonths(-i);
                    var debutMois = new DateTime(dateMois.Year, dateMois.Month, 1);
                    var finMois = debutMois.AddMonths(1).AddDays(-1);

                    var nomMois = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateMois.Month);
                    var nomMoisComplet = $"{nomMois} {dateMois.Year}";

                    // Nouveaux clients par mois
                    var nouveauxClientsMois = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= finMois);

                    tendances.NouveauxClientsParMois.Add(new MoisStatistiqueDto
                    {
                        Mois = nomMoisComplet,
                        Annee = dateMois.Year,
                        MoisNumero = dateMois.Month,
                        Nombre = nouveauxClientsMois,
                        Valeur = nouveauxClientsMois,
                        VariationPourcentage = i > 0 ? CalculateVariation(nouveauxClientsMois, tendances.NouveauxClientsParMois.Count > 0 ? tendances.NouveauxClientsParMois[tendances.NouveauxClientsParMois.Count - 1].Nombre : 0) : 0
                    });

                    // Chiffre d'affaires par mois
                    var chiffreAffairesMois = await _context.ClientFactures
                        .Include(cf => cf.Client)
                        .Where(cf => clientIds.Contains(cf.IdClient) && 
                                      cf.Client.DateCreation >= debutMois && 
                                      cf.Client.DateCreation <= finMois)
                        .Where(cf => cf.Montant.HasValue)
                        .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));

                    tendances.ChiffreAffairesParMois.Add(new MoisStatistiqueDto
                    {
                        Mois = nomMoisComplet,
                        Annee = dateMois.Year,
                        MoisNumero = dateMois.Month,
                        Valeur = Math.Round(chiffreAffairesMois, 2),
                        VariationPourcentage = i > 0 ? CalculateVariation(chiffreAffairesMois, tendances.ChiffreAffairesParMois.Count > 0 ? tendances.ChiffreAffairesParMois[tendances.ChiffreAffairesParMois.Count - 1].Valeur : 0) : 0
                    });

                    // Taux de conversion par mois
                    var totalClientsMois = clients.Count(c => c.DateCreation >= debutMois && c.DateCreation <= finMois);
                    var totalClientsTotal = clients.Count(c => c.DateCreation < debutMois);

                    var tauxConversionMois = totalClientsTotal > 0 ? 
                        (decimal)totalClientsMois / (totalClientsMois + totalClientsTotal) * 100 : 0;

                    tendances.TauxConversionParMois.Add(new MoisStatistiqueDto
                    {
                        Mois = nomMoisComplet,
                        Annee = dateMois.Year,
                        MoisNumero = dateMois.Month,
                        Valeur = Math.Round(tauxConversionMois, 2),
                        VariationPourcentage = i > 0 ? CalculateVariation(tauxConversionMois, tendances.TauxConversionParMois.Count > 0 ? tendances.TauxConversionParMois[tendances.TauxConversionParMois.Count - 1].Valeur : 0) : 0
                    });
                }

                return tendances;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tendances commerciales pour la société {SocieteId}", idSociete);
                return new TendancesCommercialesDto();
            }
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
                        p.MontantPayeDevisePrincipale,
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
                        MontantCollecte = g.Sum(x => (x.Paiement.MontantPayeDevisePrincipale ?? x.Paiement.MontantPaye)),
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

        #region Méthodes utilitaires (réutilisation du FinancierDashboardService)

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

        /// <summary>
        /// Calcule la variation en pourcentage entre deux valeurs
        /// </summary>
        private decimal CalculateVariation(decimal valeurActuelle, decimal valeurPrecedente)
        {
            if (valeurPrecedente == 0) return 0;
            return Math.Round(((valeurActuelle - valeurPrecedente) / valeurPrecedente) * 100, 2);
        }

        #endregion
    }
}
