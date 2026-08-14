using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour la génération du dashboard spécifique aux Gérants
    /// </summary>
    public class GerantDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<GerantDashboardService> _logger;
        private readonly IRapportFinancierUsdEnrichmentService _usdEnrichment;

        public GerantDashboardService(
            KenergieDbContext context,
            ILogger<GerantDashboardService> logger,
            IRapportFinancierUsdEnrichmentService usdEnrichment)
        {
            _context = context;
            _logger = logger;
            _usdEnrichment = usdEnrichment;
        }

        /// <summary>
        /// Récupère toutes les données du dashboard pour un gérant
        /// </summary>
        public async Task<GerantDashboardDto> GetDashboardDataAsync(int idSociete)
        {
            try
            {
                // Exécuter les requêtes séquentiellement pour éviter les problèmes de concurrence DbContext
                var societeStatistiques = await GetSocieteStatistiquesAsync(idSociete);
                var clientsStatistiques = await GetClientsStatistiquesAsync(idSociete);
                var top5ClientsCA = await GetTop5ClientsCAAsync(idSociete);
                var top5ClientsArrieres = await GetTop5ClientsArrieresAsync(idSociete);
                var alertesSociete = await GetAlertesSocieteAsync(idSociete);
                var tendances = await GetTendancesMensuellesAsync(idSociete);
                var paiementsStatistiques = await GetPaiementsStatistiquesAsync(idSociete);

                return new GerantDashboardDto
                {
                    SocieteStatistiques = societeStatistiques,
                    ClientsStatistiques = clientsStatistiques,
                    Top5ClientsCA = top5ClientsCA,
                    Top5ClientsArrieres = top5ClientsArrieres,
                    AlertesSociete = alertesSociete,
                    Tendances = tendances,
                    PaiementsStatistiques = paiementsStatistiques
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du dashboard Gérant pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        #region Méthodes principales

        /// <summary>
        /// Calcule les statistiques générales de la société
        /// </summary>
        public async Task<SocieteStatistiquesDto> GetSocieteStatistiquesAsync(int idSociete)
        {
            var societe = await _context.Societes
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete);

            if (societe == null)
                return new SocieteStatistiquesDto();

            var clientsActifs = await GetSocieteClientsActifsAsync(idSociete);
            var tousLesClients = await GetSocieteTousClientsAsync(idSociete);
            var clientIds = clientsActifs.Select(c => c.IdClient).ToList();

            // Chiffre d'affaires du mois
            var caMois = await _context.Paiements
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       p.DatePaiement.Month == DateTime.Now.Month && p.DatePaiement.Year == DateTime.Now.Year)
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            // Factures du mois
            var currentMonthNum = DateTime.Now.Month.ToString();
            var facturesMois = await _context.ClientFactures
                .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                .ToListAsync();
            
            facturesMois = facturesMois
                .Where(f => f.Mois.Contains(currentMonthNum))
                .ToList();

            // Montant total des arriérés
            var montantArrieres = facturesMois
                .Where(f => !f.Statut)
                .Sum(f => (f.MontantDuDevisePrincipale ?? f.MontantDu ?? 0));

            // Taux de recouvrement
            // NOTE: Utilise la même logique que les autres services - collecte du mois / factures du mois précédent
            var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
            var debutMoisPrecedent = debutMois.AddMonths(-1);
            var finMoisPrecedent = debutMoisPrecedent.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
            
            // Normaliser le mois précédent (gère "01" et "1")
            var moisPrecedentNormalise = debutMoisPrecedent.Month.ToString().PadLeft(2, '0');

            // Collecte du mois en cours
            var collecteMois = await _context.Paiements
                .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMois && 
                           p.DatePaiement <= finMois &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            // Factures du mois précédent
            var facturesMoisPrecedent = await _context.ClientFactures
                .Where(f => clientIds.Contains(f.IdClient) && 
                           f.Statut == true &&
                           f.Mois == moisPrecedentNormalise &&
                           f.Annees == debutMoisPrecedent.Year)
                .SumAsync(f => (f.MontantDevisePrincipale ?? f.Montant ?? 0));

            // Taux de recouvrement (collecte mois M / factures mois M-1)
            var tauxRecouvrement = facturesMoisPrecedent > 0
                ? Math.Round((collecteMois / facturesMoisPrecedent) * 100, 2)
                : (collecteMois > 0 ? 100 : 0);

            // Variation CA par rapport au mois précédent
            var variationCA = await CalculateVariationCAMoisPrecedent(idSociete, caMois);

            var montantDepensesMois = await _context.Depenses
                .Where(d => !d.IsDeleted
                    && d.IdSociete == idSociete
                    && d.Statut == DepenseStatuts.Validee
                    && d.DateDepense >= debutMois
                    && d.DateDepense <= finMois)
                .SumAsync(d => d.MontantDevisePrincipale ?? d.Montant);

            var nombreDepensesAValider = await _context.Depenses
                .CountAsync(d => !d.IsDeleted
                    && d.IdSociete == idSociete
                    && d.Statut == DepenseStatuts.EnAttente);

            var montantDepensesEnAttente = await _context.Depenses
                .Where(d => !d.IsDeleted
                    && d.IdSociete == idSociete
                    && d.Statut == DepenseStatuts.EnAttente)
                .SumAsync(d => d.Montant);

            return new SocieteStatistiquesDto
            {
                NomSociete = societe.Nom,
                TotalClients = tousLesClients.Count,
                ClientsActifs = clientsActifs.Count,
                ChiffreAffairesMois = caMois,
                MontantTotalArrieres = montantArrieres,
                MontantDepensesMois = montantDepensesMois,
                NombreDepensesAValider = nombreDepensesAValider,
                MontantDepensesEnAttente = montantDepensesEnAttente,
                TauxRecouvrement = tauxRecouvrement,
                VariationCAMoisPrecedent = variationCA,
                TotalFacturesMois = facturesMois.Count,
                FacturesPayeesMois = facturesMois.Count(f => f.Statut),
                SyntheseUsd = await _usdEnrichment.BuildSocieteStatistiquesSyntheseUsdAsync(
                    idSociete, caMois, montantArrieres)
            };
        }

        /// <summary>
        /// Calcule les statistiques des clients
        /// </summary>
        public async Task<ClientsStatistiquesDto> GetClientsStatistiquesAsync(int idSociete)
        {
            var clients = await GetSocieteClientsActifsAsync(idSociete);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            // Clients actifs métier ayant une facture ce mois
            var currentMonthNum = DateTime.Now.Month.ToString();
            var facturesMois = await _context.ClientFactures
                .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                .ToListAsync();
            
            facturesMois = facturesMois
                .Where(f => f.Mois.Contains(currentMonthNum))
                .ToList();

            var clientsActifsIds = facturesMois.Select(f => f.IdClient).Distinct().ToList();
            var clientsActifs = clients.Where(c => clientsActifsIds.Contains(c.IdClient)).ToList();

            // Nouveaux clients ce mois
            var nouveauxClientsMois = clients.Count(c => c.DateCreation.Month == DateTime.Now.Month && 
                                                          c.DateCreation.Year == DateTime.Now.Year);

            // Clients avec des arriérés
            var clientsAvecArrieres = facturesMois
                .Where(f => !f.Statut)
                .Select(f => f.IdClient)
                .Distinct()
                .Count();

            var pourcentageArrieres = clients.Any() ? (decimal)clientsAvecArrieres / clients.Count * 100 : 0;

            // Répartition par catégorie
            var repartitionParCategorie = await GetRepartitionClientsParCategorieAsync(idSociete);

            return new ClientsStatistiquesDto
            {
                TotalClients = clients.Count,
                ClientsActifs = clientsActifs.Count,
                NouveauxClientsMois = nouveauxClientsMois,
                ClientsAvecArrieres = clientsAvecArrieres,
                PourcentageClientsAvecArrieres = pourcentageArrieres,
                RepartitionParCategorie = repartitionParCategorie
            };
        }

        /// <summary>
        /// Calcule le top 5 des clients par chiffre d'affaires
        /// </summary>
        public async Task<List<TopClientDto>> GetTop5ClientsCAAsync(int idSociete)
        {
            var clients = await GetSocieteClientsActifsAsync(idSociete);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            var caMoisDernier = await _context.Paiements
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       p.DatePaiement.Month == DateTime.Now.Month && p.DatePaiement.Year == DateTime.Now.Year)
                .ToListAsync();

            var caByClient = caMoisDernier
                .GroupBy(p => p.IdClient!.Value)
                .Select(g => new
                {
                    IdClient = g.Key,
                    ChiffreAffaires = g.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye))
                })
                .OrderByDescending(g => g.ChiffreAffaires)
                .Take(5)
                .ToList();

            var topClients = new List<TopClientDto>();
            for (int i = 0; i < caByClient.Count; i++)
            {
                var item = caByClient[i];
                var client = clients.FirstOrDefault(c => c.IdClient == item.IdClient);
                
                topClients.Add(new TopClientDto
                {
                    Rang = i + 1,
                    IdClient = item.IdClient,
                    NomClient = client?.NomClient ?? $"Client {item.IdClient}",
                    Valeur = item.ChiffreAffaires,
                    VariationMoisPrecedent = 0 // Simplifié pour l'instant
                });
            }

            return topClients;
        }

        /// <summary>
        /// Calcule le top 5 des clients avec le plus d'arriérés
        /// </summary>
        public async Task<List<TopClientDto>> GetTop5ClientsArrieresAsync(int idSociete)
        {
            var clients = await GetSocieteClientsActifsAsync(idSociete);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            var currentMonthNum = DateTime.Now.Month.ToString();
            var facturesMois = await _context.ClientFactures
                .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                .ToListAsync();
            
            facturesMois = facturesMois
                .Where(f => f.Mois.Contains(currentMonthNum))
                .ToList();

            var arrieresByClient = facturesMois
                .Where(f => !f.Statut && f.MontantDu.HasValue)
                .GroupBy(f => f.IdClient)
                .Select(g => new
                {
                    IdClient = g.Key,
                    MontantArrieres = g.Sum(f => (f.MontantDuDevisePrincipale ?? f.MontantDu!.Value))
                })
                .OrderByDescending(g => g.MontantArrieres)
                .Take(5)
                .ToList();

            var topClientsArrieres = new List<TopClientDto>();
            for (int i = 0; i < arrieresByClient.Count; i++)
            {
                var item = arrieresByClient[i];
                var client = clients.FirstOrDefault(c => c.IdClient == item.IdClient);
                
                topClientsArrieres.Add(new TopClientDto
                {
                    Rang = i + 1,
                    IdClient = item.IdClient,
                    NomClient = client?.NomClient ?? $"Client {item.IdClient}",
                    Valeur = item.MontantArrieres,
                    VariationMoisPrecedent = 0 // Simplifié pour l'instant
                });
            }

            return topClientsArrieres;
        }

        /// <summary>
        /// Récupère les alertes importantes pour la société
        /// </summary>
        public async Task<List<AlerteSocieteDto>> GetAlertesSocieteAsync(int idSociete)
        {
            var alertes = new List<AlerteSocieteDto>();
            var clients = await GetSocieteClientsActifsAsync(idSociete);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            // Alertes de recouvrement faible
            var currentMonthNum = DateTime.Now.Month.ToString();
            var facturesMois = await _context.ClientFactures
                .Where(f => clientIds.Contains(f.IdClient) && f.Annees == DateTime.Now.Year)
                .ToListAsync();
            
            facturesMois = facturesMois
                .Where(f => f.Mois.Contains(currentMonthNum))
                .ToList();

            var tauxRecouvrementGlobal = facturesMois.Any() 
                ? (decimal)facturesMois.Count(f => f.Statut) / facturesMois.Count * 100 
                : 0;

            if (tauxRecouvrementGlobal < 70)
            {
                alertes.Add(new AlerteSocieteDto
                {
                    TypeAlerte = "Recouvrement",
                    Description = $"Taux de recouvrement faible: {tauxRecouvrementGlobal:F1}%",
                    NiveauCriticite = tauxRecouvrementGlobal < 50 ? "Élevée" : "Moyenne",
                    DateAlerte = DateTime.Now,
                    Statut = "Non lue"
                });
            }

            // Alertes pour clients avec gros arriérés
            var clientsGrosArrieres = facturesMois
                .Where(f => !f.Statut && (f.MontantDu ?? 0) > 100000) // Plus de 100,000 FC d'arriérés
                .Select(f => f.IdClient)
                .Distinct()
                .Take(10)
                .ToList();

            foreach (var clientId in clientsGrosArrieres)
            {
                var client = clients.FirstOrDefault(c => c.IdClient == clientId);
                if (client != null)
                {
                    alertes.Add(new AlerteSocieteDto
                    {
                        TypeAlerte = "Arriérés",
                        Description = $"Client avec arriérés importants: {client.NomClient}",
                        NiveauCriticite = "Élevée",
                        DateAlerte = DateTime.Now,
                        IdClient = clientId,
                        NomClient = client.NomClient,
                        Statut = "Non lue"
                    });
                }
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        /// <summary>
        /// Calcule les tendances sur les 12 derniers mois
        /// </summary>
        public async Task<TendancesDto> GetTendancesMensuellesAsync(int idSociete)
        {
            var caTendances = await GetTendanceMensuelleAsync(async (mois, annee) =>
            {
                var clients = await GetSocieteClientsActifsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                return await _context.Paiements
                    .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                           p.DatePaiement.Month == mois && p.DatePaiement.Year == annee)
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
            });

            var recouvrementTendances = await GetTendanceMensuelleAsync(async (mois, annee) =>
            {
                var clients = await GetSocieteClientsActifsAsync(idSociete);
                var clientIds = clients.Select(c => c.IdClient).ToList();

                var factures = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && f.Annees == annee)
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
        /// Calcule les statistiques des paiements
        /// </summary>
        public async Task<PaiementsStatistiquesDto> GetPaiementsStatistiquesAsync(int idSociete)
        {
            var clients = await GetSocieteClientsActifsAsync(idSociete);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            var now = DateTime.Now;
            var startOfDay = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0);

            var paiements = await _context.Paiements
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value))
                .ToListAsync();

            var paiementsJour = paiements
                .Where(p => p.DatePaiement >= startOfDay)
                .ToList();

            var paiementsSemaine = paiements
                .Where(p => p.DatePaiement >= startOfWeek)
                .ToList();

            var paiementsMois = paiements
                .Where(p => p.DatePaiement >= startOfMonth)
                .ToList();

            var joursEcoulesMois = now.Day;
            var moyenneJournaliere = joursEcoulesMois > 0 ? paiementsMois.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye)) / joursEcoulesMois : 0;

            return new PaiementsStatistiquesDto
            {
                PaiementsJour = paiementsJour.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye)),
                PaiementsSemaine = paiementsSemaine.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye)),
                PaiementsMois = paiementsMois.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye)),
                NombrePaiementsJour = paiementsJour.Count,
                NombrePaiementsSemaine = paiementsSemaine.Count,
                NombrePaiementsMois = paiementsMois.Count,
                MoyennePaiementsJournaliers = moyenneJournaliere
            };
        }

        #endregion

        #region Méthodes utilitaires

        /// <summary>
        /// Clients actifs d'une société : Statut, IsActif, non supprimé, liaison/catégorie/usage actifs.
        /// </summary>
        private async Task<List<Client>> GetSocieteClientsActifsAsync(int idSociete)
        {
            var clientIds = await GetSocieteClientIdsAsync(idSociete, activeOnly: true);
            if (!clientIds.Any())
            {
                return new List<Client>();
            }

            return await _context.Clients
                .Where(c => clientIds.Contains(c.IdClient)
                    && c.IsActif == true
                    && c.Statut == true
                    && (!c.IsDeleted.HasValue || !c.IsDeleted.Value))
                .ToListAsync();
        }

        /// <summary>
        /// Tous les clients rattachés à la société (hors soft delete), actifs ou non.
        /// </summary>
        private async Task<List<Client>> GetSocieteTousClientsAsync(int idSociete)
        {
            var clientIds = await GetSocieteClientIdsAsync(idSociete, activeOnly: false);
            if (!clientIds.Any())
            {
                return new List<Client>();
            }

            return await _context.Clients
                .Where(c => clientIds.Contains(c.IdClient)
                    && (!c.IsDeleted.HasValue || !c.IsDeleted.Value))
                .ToListAsync();
        }

        /// <summary>
        /// IDs clients liés à la société via Catégorie → Usage → ClientUsage.
        /// </summary>
        private async Task<List<int>> GetSocieteClientIdsAsync(int idSociete, bool activeOnly)
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

            var query = _context.ClientUsages
                .Where(cu => usageIds.Contains(cu.IdUsage));

            if (activeOnly)
            {
                query = query.Where(cu => cu.Statut == true);
            }

            return await query
                .Select(cu => cu.IdClient)
                .Distinct()
                .ToListAsync();
        }

        /// <summary>
        /// Calcule la variation du CA par rapport au mois précédent
        /// </summary>
        private async Task<decimal> CalculateVariationCAMoisPrecedent(int idSociete, decimal caActuel)
        {
            var clients = await GetSocieteClientsActifsAsync(idSociete);
            var clientIds = clients.Select(c => c.IdClient).ToList();

            var caMoisPrecedent = await _context.Paiements
                .Where(p => !p.IsDeleted && p.IdClient.HasValue && clientIds.Contains(p.IdClient.Value) &&
                       p.DatePaiement.Month == DateTime.Now.AddMonths(-1).Month && 
                       p.DatePaiement.Year == DateTime.Now.AddMonths(-1).Year)
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

            return caMoisPrecedent > 0 ? ((caActuel - caMoisPrecedent) / caMoisPrecedent) * 100 : 0;
        }

        /// <summary>
        /// Calcule la répartition des clients par catégorie
        /// </summary>
        private async Task<List<ClientsParCategorieDto>> GetRepartitionClientsParCategorieAsync(int idSociete)
        {
            var societe = await _context.Societes
                .Include(s => s.CategorieClients)
                .ThenInclude(cc => cc.Usages)
                .ThenInclude(u => u.ClientsUsages)
                .ThenInclude(cu => cu.Client)
                .FirstOrDefaultAsync(s => s.IdSociete == idSociete);

            if (societe?.CategorieClients == null)
                return new List<ClientsParCategorieDto>();

            var repartition = new List<ClientsParCategorieDto>();
            var totalClients = 0;

            foreach (var categorie in societe.CategorieClients.Where(cc => cc.Statut != false))
            {
                var clientsCount = categorie.Usages?
                    .Where(u => u.Statut == true)
                    .SelectMany(u => u.ClientsUsages?
                        .Where(cu => cu.Statut == true)
                        .Select(cu => cu.Client) ?? Enumerable.Empty<Client>())
                    .Count(c => c != null
                        && c.IsActif == true
                        && c.Statut == true
                        && (!c.IsDeleted.HasValue || !c.IsDeleted.Value)) ?? 0;

                totalClients += clientsCount;

                repartition.Add(new ClientsParCategorieDto
                {
                    Categorie = categorie.NomCategorie ?? "Non définie",
                    NombreClients = clientsCount
                });
            }

            // Calculer les pourcentages
            foreach (var item in repartition)
            {
                item.Pourcentage = totalClients > 0 ? (decimal)item.NombreClients / totalClients * 100 : 0;
            }

            return repartition.OrderByDescending(r => r.NombreClients).ToList();
        }

        /// <summary>
        /// Calcule les tendances mensuelles pour une métrique donnée
        /// </summary>
        private async Task<List<TendanceMensuelleDto>> GetTendanceMensuelleAsync(Func<int, int, Task<decimal>> calculateur)
        {
            var tendances = new List<TendanceMensuelleDto>();
            var valeurPrecedente = 0m;

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
