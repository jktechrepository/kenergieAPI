using Kenergie.Models;
using Kenergie.Models.DTOs.Statistiques;
using Kenergie.Data;
using Kenergie.Services.Repositories;
using Kenergie.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Kenergie.Services
{
    /// <summary>
    /// Service de gestion des statistiques
    /// </summary>
    public class StatistiquesService : IStatistiquesService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<StatistiquesService> _logger;
        private readonly ISignalRStatistiquesService _signalRStatistiquesService;
        private readonly ISocieteClientScopeService _clientScope;

        public StatistiquesService(
            KenergieDbContext context,
            ILogger<StatistiquesService> logger,
            ISignalRStatistiquesService signalRStatistiquesService,
            ISocieteClientScopeService clientScope)
        {
            _context = context;
            _logger = logger;
            _signalRStatistiquesService = signalRStatistiquesService;
            _clientScope = clientScope;
        }

        /// <summary>
        /// Obtient les statistiques générales pour une société
        /// </summary>
        public async Task<StatistiquesGeneralesDto> GetStatistiquesGeneralesAsync(int idSociete, StatistiquesFiltresDto filtres = null)
        {
            try
            {
                _logger.LogInformation("Calcul des statistiques générales pour la société {SocieteId} avec filtres", idSociete);

                var activeClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: true);
                var financialClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: false);

                var totalClients = activeClientIds.Count;

                var (debutMois, finMois) = PeriodBoundsHelper.GetMoisCourantBounds();
                var debutMoisPrecedent = debutMois.AddMonths(-1);
                var moisPrecedentNormalise = NormaliserMois(debutMoisPrecedent.Month.ToString());

                var paiementsMoisCourantQuery = _context.Paiements
                    .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                               p.DatePaiement >= debutMois &&
                               p.DatePaiement <= finMois &&
                               p.IdClient.HasValue &&
                               financialClientIds.Contains(p.IdClient.Value));

                var totalPaiements = await paiementsMoisCourantQuery.SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                var totalPaiementsCount = await paiementsMoisCourantQuery.CountAsync();
                var collecteMois = totalPaiements;

                var totalArrieres = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                               cf.MontantDu.HasValue &&
                               cf.MontantDu.Value > 0 &&
                               financialClientIds.Contains(cf.IdClient))
                    .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                // Factures du mois précédent
                var facturesMoisPrecedent = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                               cf.Montant.HasValue &&
                               cf.Montant.Value > 0 &&
                               cf.Mois == moisPrecedentNormalise &&
                               cf.Annees == debutMoisPrecedent.Year &&
                               financialClientIds.Contains(cf.IdClient))
                    .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));

                // Calcul du totalFactures (nombre de factures du mois précédent)
                var totalFactures = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                               cf.Mois == moisPrecedentNormalise &&
                               cf.Annees == debutMoisPrecedent.Year &&
                               financialClientIds.Contains(cf.IdClient))
                    .CountAsync();

                // Taux de recouvrement (collecte mois M / factures mois M-1)
                var tauxRecouvrement = facturesMoisPrecedent > 0
                    ? Math.Round((collecteMois / facturesMoisPrecedent) * 100, 2)
                    : (collecteMois > 0 ? 100 : 0);

                var codeDevisePrincipale = await _context.Societes
                    .Where(s => s.IdSociete == idSociete)
                    .Select(s => s.CodeDevisePrincipale)
                    .FirstOrDefaultAsync() ?? "CDF";

                var statistiques = new StatistiquesGeneralesDto
                {
                    TotalClients = totalClients,
                    TotalFactures = totalFactures,
                    TotalArrieres = totalArrieres,
                    TotalPaiements = totalPaiements,
                    TauxRecouvrement = tauxRecouvrement,
                    TotalPaiementsCount = totalPaiementsCount,
                    DateGeneration = DateTime.Now,
                    CodeDevisePrincipale = codeDevisePrincipale
                };

                _logger.LogInformation("✅ Statistiques générales calculées avec succès pour la société {SocieteId}: {Clients} clients, {Factures} factures, {Arrieres:C} arriérés, {Paiements:C} payés", 
                    idSociete, totalClients, totalFactures, totalArrieres, totalPaiements);

                // Notifier les clients connectés de la mise à jour des statistiques générales
                try
                {
                    await _signalRStatistiquesService.NotifyStatistiquesGeneralesUpdatedAsync(idSociete, statistiques);
                    _logger.LogInformation($"📊 Statistiques générales update sent to society {idSociete} via SignalR");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending statistiques générales update to society {idSociete} via SignalR");
                }

                return statistiques;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques générales pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        /// <summary>
        /// Obtient les statistiques financières pour une société
        /// </summary>
        public async Task<StatistiquesFinancieresDto> GetStatistiquesFinancieresAsync(int idSociete, DateTime? debut = null, DateTime? fin = null, StatistiquesFiltresDto filtres = null)
        {
            try
            {
                _logger.LogInformation("💰 Calcul des statistiques financières pour la société {SocieteId}", idSociete);

                var categoriesIds = await GetCategorieIdsAsync(idSociete);
                var financialClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: false);

                var dateDebut = debut ?? new DateTime(DateTime.Now.Year, 1, 1);
                var dateFin = fin ?? DateTime.Now;

                var (debutMois, finMois) = PeriodBoundsHelper.GetMoisCourantBounds();
                var periodePaiementsDebut = debut ?? debutMois;
                var periodePaiementsFin = fin ?? finMois;

                var chiffreAffaires = await SumPaiementsValidesAsync(financialClientIds, debutMois, finMois);

                var montantArrieres = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                               cf.MontantDu.HasValue &&
                               cf.MontantDu.Value > 0 &&
                               financialClientIds.Contains(cf.IdClient))
                    .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                var montantPaye = debut.HasValue || fin.HasValue
                    ? await SumPaiementsValidesAsync(financialClientIds, periodePaiementsDebut, periodePaiementsFin)
                    : chiffreAffaires;

                var montantDu = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                               cf.MontantDu.HasValue &&
                               cf.MontantDu.Value > 0 &&
                               financialClientIds.Contains(cf.IdClient))
                    .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                var evolutionMensuelle = await GetEvolutionMensuelleAsync(idSociete, categoriesIds, financialClientIds, dateDebut, dateFin);

                var repartitionPaiements = await GetRepartitionPaiementsAsync(
                    idSociete, financialClientIds, periodePaiementsDebut, periodePaiementsFin);

                var codeDevisePrincipale = await _context.Societes
                    .Where(s => s.IdSociete == idSociete)
                    .Select(s => s.CodeDevisePrincipale)
                    .FirstOrDefaultAsync() ?? "CDF";

                var statistiques = new StatistiquesFinancieresDto
                {
                    ChiffreAffaires = chiffreAffaires,
                    MontantArrieres = montantArrieres,
                    MontantPaye = montantPaye,
                    MontantDu = montantDu,
                    EvolutionMensuelle = evolutionMensuelle,
                    RepartitionPaiements = repartitionPaiements,
                    DateGeneration = DateTime.Now,
                    CodeDevisePrincipale = codeDevisePrincipale
                };

                _logger.LogInformation("✅ Statistiques financières calculées avec succès pour la société {SocieteId}: CA={CA:C}, Arriérés={Arriérés:C}, Payé={Payé:C}", 
                    idSociete, chiffreAffaires, montantArrieres, montantPaye);

                // Notifier les clients connectés de la mise à jour des statistiques financières
                try
                {
                    await _signalRStatistiquesService.NotifyStatistiquesFinancieresUpdatedAsync(idSociete, statistiques);
                    _logger.LogInformation($"💰 Statistiques financières update sent to society {idSociete} via SignalR");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending statistiques financières update to society {idSociete} via SignalR");
                }

                return statistiques;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques financières pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        /// <summary>
        /// Obtient les statistiques opérationnelles pour une société
        /// </summary>
        public async Task<StatistiquesOperationnellesDto> GetStatistiquesOperationnellesAsync(int idSociete, StatistiquesFiltresDto filtres = null)
        {
            try
            {
                _logger.LogInformation("🏢 Calcul des statistiques opérationnelles pour la société {SocieteId}", idSociete);

                var categoriesIds = await GetCategorieIdsAsync(idSociete);
                var activeClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: true);
                var financialClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: false);

                // 2. Calculer la répartition des clients par catégorie
                var repartitionClientsParCategorie = await GetRepartitionClientsParCategorieAsync(idSociete, categoriesIds, activeClientIds);

                // 3. Calculer la répartition des clients par axe/cabine
                var repartitionClientsParAxe = await GetRepartitionClientsParAxeAsync(idSociete, activeClientIds);

                // 4. Calculer les statistiques des factures par mois
                var statistiquesFacturesMois = await GetStatistiquesFacturesMoisAsync(idSociete, categoriesIds, financialClientIds);

                // 5. Calculer l'activité des clients
                var clientActivite = await GetClientActiviteAsync(financialClientIds);

                var statistiques = new StatistiquesOperationnellesDto
                {
                    RepartitionClientsParCategorie = repartitionClientsParCategorie,
                    RepartitionClientsParAxe = repartitionClientsParAxe,
                    StatistiquesFacturesMois = statistiquesFacturesMois,
                    ClientActivite = clientActivite,
                    DateGeneration = DateTime.Now
                };

                _logger.LogInformation("✅ Statistiques opérationnelles calculées avec succès pour la société {SocieteId}: {Categories} catégories, {Axes} axes, {FacturesMois} mois de factures", 
                    idSociete, repartitionClientsParCategorie.Count, repartitionClientsParAxe.Count, statistiquesFacturesMois.Count);

                // Notifier les clients connectés de la mise à jour des statistiques opérationnelles
                try
                {
                    await _signalRStatistiquesService.NotifyStatistiquesOperationnellesUpdatedAsync(idSociete, statistiques);
                    _logger.LogInformation($"🏢 Statistiques opérationnelles update sent to society {idSociete} via SignalR");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending statistiques opérationnelles update to society {idSociete} via SignalR");
                }

                return statistiques;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques opérationnelles pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        /// <summary>
        /// Obtient les statistiques de performance pour une société
        /// </summary>
        public async Task<StatistiquesPerformanceDto> GetStatistiquesPerformanceAsync(int idSociete, StatistiquesFiltresDto filtres = null)
        {
            try
            {
                _logger.LogInformation("⚡ Calcul des statistiques de performance pour la société {SocieteId}", idSociete);

                var categoriesIds = await GetCategorieIdsAsync(idSociete);
                var financialClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: false);

                // 2. Calculer le taux de recouvrement global
                var tauxRecouvrementGlobal = await GetTauxRecouvrementGlobalAsync(financialClientIds);

                // 3. Calculer le taux de recouvrement par catégorie
                var tauxRecouvrementParCategorie = await GetTauxRecouvrementParCategorieAsync(idSociete, categoriesIds, financialClientIds);

                // 4. Calculer le top des agents par montant collecté
                var topAgents = await GetTopAgentsAsync(idSociete, financialClientIds);

                // 5. Calculer la performance mensuelle
                var performanceMensuelle = await GetPerformanceMensuelleAsync(idSociete, categoriesIds, financialClientIds);

                var statistiques = new StatistiquesPerformanceDto
                {
                    TauxRecouvrementGlobal = tauxRecouvrementGlobal,
                    TauxRecouvrementParCategorie = tauxRecouvrementParCategorie,
                    TopAgents = topAgents,
                    PerformanceMensuelle = performanceMensuelle,
                    DateGeneration = DateTime.Now
                };

                _logger.LogInformation("✅ Statistiques de performance calculées avec succès pour la société {SocieteId}: Taux global={TauxGlobal}%, {TopAgentsCount} agents, {PerfMoisCount} mois de performance", 
                    idSociete, tauxRecouvrementGlobal, topAgents.Count, performanceMensuelle.Count);

                // Notifier les clients connectés de la mise à jour des statistiques de performance
                try
                {
                    await _signalRStatistiquesService.NotifyStatistiquesPerformanceUpdatedAsync(idSociete, statistiques);
                    _logger.LogInformation($"⚡ Statistiques performance update sent to society {idSociete} via SignalR");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending statistiques performance update to society {idSociete} via SignalR");
                }

                return statistiques;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques de performance pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        /// <summary>
        /// Obtient toutes les statistiques consolidées pour une société
        /// </summary>
        public async Task<StatistiquesConsolideesDto> GetStatistiquesConsolideesAsync(int idSociete, DateTime? debut = null, DateTime? fin = null, StatistiquesFiltresDto filtres = null)
        {
            try
            {
                _logger.LogInformation("📊 Calcul des statistiques consolidées pour la société {SocieteId}", idSociete);

                var generales = await GetStatistiquesGeneralesAsync(idSociete, filtres);
                // KPIs paiements (montantPaye, repartition) : mois courant — pas de fenêtre YTD via query params
                var financieres = await GetStatistiquesFinancieresAsync(idSociete, null, null, filtres);

                if (debut.HasValue || fin.HasValue)
                {
                    var categoriesIds = await GetCategorieIdsAsync(idSociete);
                    var financialClientIds = await BuildClientIdsAsync(idSociete, filtres, activeOnly: false);
                    var dateDebutEvolution = debut ?? new DateTime(DateTime.Now.Year, 1, 1);
                    var dateFinEvolution = fin ?? DateTime.Now;
                    financieres.EvolutionMensuelle = await GetEvolutionMensuelleAsync(
                        idSociete, categoriesIds, financialClientIds, dateDebutEvolution, dateFinEvolution);
                }

                var operationnelles = await GetStatistiquesOperationnellesAsync(idSociete, filtres);
                var performance = await GetStatistiquesPerformanceAsync(idSociete, filtres);

                var periode = new PeriodeStatistiquesDto
                {
                    DateDebut = debut,
                    DateFin = fin,
                    LibellePeriode = GetLibellePeriode(debut, fin)
                };

                var statistiques = new StatistiquesConsolideesDto
                {
                    Generales = generales,
                    Financieres = financieres,
                    Operationnelles = operationnelles,
                    Performance = performance,
                    Periode = periode,
                    DateGeneration = DateTime.Now
                };

                // Notifier les clients connectés de la mise à jour des statistiques consolidées
                try
                {
                    await _signalRStatistiquesService.NotifyStatistiquesConsolideesUpdatedAsync(idSociete, statistiques);
                    _logger.LogInformation($"📈 Statistiques consolidées update sent to society {idSociete} via SignalR");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending statistiques consolidées update to society {idSociete} via SignalR");
                }

                return statistiques;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques consolidées pour la société {SocieteId}", idSociete);
                throw;
            }
        }

        #region Méthodes utilitaires

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

            // Valeur par défaut si le format n'est pas reconnu
            return mois;
        }

        /// <summary>
        /// Calcule le taux de recouvrement global
        /// NOTE: La facturation est post-consommation, donc les paiements correspondent aux factures du mois précédent
        /// </summary>
        private async Task<decimal> GetTauxRecouvrementGlobalAsync(List<int> clientsIds)
        {
            try
            {
                // Définir la période: mois en cours pour les paiements, mois précédent pour les factures
                var debutMoisPrecedent = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                var finMoisPrecedent = debutMoisPrecedent.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);

                // Normaliser le mois précédent (gère "01" et "1")
                var moisPrecedentNormalise = NormaliserMois(debutMoisPrecedent.Month.ToString());

                // Calculer le montant total des factures du mois précédent
                var montantFacturesMoisPrecedent = await _context.ClientFactures
                    .Where(cf => cf.Statut == true &&
                               cf.Montant.HasValue &&
                               cf.Montant.Value > 0 &&
                               cf.Mois == moisPrecedentNormalise &&
                               cf.Annees == debutMoisPrecedent.Year &&
                               clientsIds.Contains(cf.IdClient))
                    .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));

                // Calculer le montant total payé ce mois-ci
                var montantPaye = await _context.Paiements
                    .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                               p.DatePaiement >= debutMois &&
                               p.DatePaiement <= finMois &&
                               p.IdClient.HasValue &&
                               clientsIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                // Calculer le taux de recouvrement (paiements du mois M / factures du mois M-1)
                var tauxRecouvrement = montantFacturesMoisPrecedent > 0
                    ? Math.Round(((montantPaye / montantFacturesMoisPrecedent) * 100), 2)
                    : (montantPaye > 0 ? 100 : 0);

                return tauxRecouvrement;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul du taux de recouvrement global");
                return 0;
            }
        }

        /// <summary>
        /// Calcule le taux de recouvrement par catégorie
        /// NOTE: La facturation est post-consommation, donc les paiements correspondent aux factures du mois précédent
        /// </summary>
        private async Task<List<TauxRecouvrementParCategorieDto>> GetTauxRecouvrementParCategorieAsync(int idSociete, List<int> categoriesIds, List<int> clientsIds)
        {
            try
            {
                // Définir la période: mois en cours pour les paiements, mois précédent pour les factures
                var debutMoisPrecedent = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                var finMoisPrecedent = debutMoisPrecedent.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);

                // Normaliser le mois précédent (gère "01" et "1")
                var moisPrecedentNormalise = NormaliserMois(debutMoisPrecedent.Month.ToString());

                // Récupérer les catégories de la société
                var categories = await _context.CategorieClients
                    .Where(cc => cc.IdSociete == idSociete)
                    .Select(cc => new { cc.IdCategorie, cc.NomCategorie })
                    .ToListAsync();

                var tauxParCategorie = new List<TauxRecouvrementParCategorieDto>();

                foreach (var categorie in categories)
                {
                    // Récupérer les clients de cette catégorie
                    var clientsCategorieIds = await _context.ClientUsages
                        .Include(cu => cu.Usage)
                            .ThenInclude(u => u.CategorieClient)
                        .Where(cu => cu.Usage != null &&
                                   cu.Usage.CategorieClient != null &&
                                   cu.Usage.CategorieClient.IdCategorie == categorie.IdCategorie &&
                                   cu.Statut == true &&
                                   clientsIds.Contains(cu.IdClient))
                        .Select(cu => cu.IdClient)
                        .Distinct()
                        .ToListAsync();

                    // Calculer le montant des factures du mois précédent pour cette catégorie
                    var montantFacturesCategorie = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                   cf.Montant.HasValue &&
                                   cf.Montant.Value > 0 &&
                                   cf.Mois == moisPrecedentNormalise &&
                                   cf.Annees == debutMoisPrecedent.Year &&
                                   clientsCategorieIds.Contains(cf.IdClient))
                        .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));

                    // Calculer le montant payé ce mois-ci pour cette catégorie
                    var montantPayeCategorie = await _context.Paiements
                        .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                                   p.DatePaiement >= debutMois &&
                                   p.DatePaiement <= finMois &&
                                   p.IdClient.HasValue &&
                                   clientsCategorieIds.Contains(p.IdClient.Value))
                        .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                    // Calculer le taux de recouvrement pour cette catégorie (paiements du mois M / factures du mois M-1)
                    var tauxRecouvrementCategorie = montantFacturesCategorie > 0
                        ? Math.Round(((montantPayeCategorie / montantFacturesCategorie) * 100), 2)
                        : (montantPayeCategorie > 0 ? 100 : 0);

                    tauxParCategorie.Add(new TauxRecouvrementParCategorieDto
                    {
                        IdCategorie = categorie.IdCategorie,
                        NomCategorie = categorie.NomCategorie ?? "Sans catégorie",
                        TauxRecouvrement = tauxRecouvrementCategorie,
                        MontantDu = montantFacturesCategorie, // Renommé pour plus de clarté
                        MontantPaye = montantPayeCategorie
                    });
                }

                return tauxParCategorie.OrderByDescending(t => t.TauxRecouvrement).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul du taux de recouvrement par catégorie pour la société {SocieteId}", idSociete);
                return new List<TauxRecouvrementParCategorieDto>();
            }
        }

        /// <summary>
        /// Calcule le top des agents caissiers par montant collecté sur le mois en cours.
        /// </summary>
        private async Task<List<TopAgentDto>> GetTopAgentsAsync(int idSociete, List<int> clientsIds)
        {
            try
            {
                var (debutMois, finMois) = PeriodBoundsHelper.GetMoisCourantBounds();

                // 1. Récupérer l'ID du rôle "Caissier"
                var caissierRoleId = await _context.Roles
                    .Where(r => r.Nom == "Caissier" && r.Statut == true)
                    .Select(r => r.IdRole)
                    .FirstOrDefaultAsync();

                if (caissierRoleId == 0)
                {
                    _logger.LogWarning("⚠️ Rôle 'Caissier' non trouvé pour la société {SocieteId}", idSociete);
                    return new List<TopAgentDto>();
                }

                // 2. Récupérer les agents de la société qui ont le rôle "Caissier"
                // IMPORTANT: Un agent peut avoir plusieurs utilisateurs, mais chaque utilisateur est lié à un seul agent
                var caissierAgentsData = await _context.Agents
                    .Where(a => a.Statut == true && a.IdSociete == idSociete)
                    .Join(_context.Utilisateurs.Where(u => u.Statut == true),
                          agent => agent.IdAgent,
                          utilisateur => utilisateur.IdAgent,
                          (agent, utilisateur) => new { agent, utilisateur })
                    .Join(_context.UserRoles.Where(ur => ur.IdRole == caissierRoleId && ur.Statut == true),
                          au => au.utilisateur.IdUtilisateur,
                          ur => ur.IdUtilisateur,
                          (au, ur) => new { au.agent, au.utilisateur })
                    .ToListAsync();

                // Grouper par agent pour éviter les doublons si un agent a plusieurs utilisateurs
                var caissierAgents = caissierAgentsData
                    .GroupBy(x => x.agent.IdAgent)
                    .Select(g => g.First())
                    .ToList();

                _logger.LogInformation("🔍 {NbAgents} agents caissiers trouvés pour la société {SocieteId}", caissierAgents.Count, idSociete);

                // DEBUG: Afficher les relations Agent-Utilisateur
                foreach (var agentData in caissierAgents)
                {
                    _logger.LogInformation("📋 Relation: Agent {AgentId} ({NomAgent}) ↔ Utilisateur {UserId} ({NomUtilisateur})", 
                        agentData.agent.IdAgent, agentData.agent.NomComplet, 
                        agentData.utilisateur.IdUtilisateur, agentData.utilisateur.NomComplet);
                }

                var topAgents = new List<TopAgentDto>();

                foreach (var agentData in caissierAgents)
                {
                    var agent = agentData.agent;
                    var utilisateurAgent = agentData.utilisateur;

                    _logger.LogInformation("🔍 Agent {AgentId} ({NomAgent}) - Utilisateur {UserId}", 
                        agent.IdAgent, agent.NomComplet, utilisateurAgent.IdUtilisateur);

                    // DEBUG: Vérifier si des paiements existent pour cet utilisateur
                    var paiementsAgent = await _context.Paiements
                        .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                               p.DatePaiement >= debutMois &&
                               p.DatePaiement <= finMois &&
                               p.IdUtilisateur == utilisateurAgent.IdUtilisateur &&
                               p.IdClient.HasValue &&
                               clientsIds.Contains(p.IdClient.Value))
                        .ToListAsync();

                    _logger.LogInformation("🔍 Agent {AgentId} ({NomAgent}) - Utilisateur {UserId} - {NbPaiements} paiements trouvés", 
                        agent.IdAgent, agent.NomComplet, utilisateurAgent.IdUtilisateur, paiementsAgent.Count);

                    // 4. Calculer le montant collecté par cet agent via l'utilisateur
                    var montantCollecte = paiementsAgent.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                    // 5. Calculer le nombre de paiements pour cet agent
                    var nombrePaiements = paiementsAgent.Count;

                    // 6. Calculer le taux de conversion (pourcentage de réussite)
                    // On considère que tous les paiements validés sont des réussites
                    var tauxConversion = nombrePaiements > 0
                        ? Math.Round((100.0 / nombrePaiements), 2) // Simplifié: 100% pour les paiements validés
                        : 0;

                    if (montantCollecte <= 0)
                    {
                        continue;
                    }

                    topAgents.Add(new TopAgentDto
                    {
                        IdAgent = agent.IdAgent,
                        NomAgent = agent.NomComplet ?? $"Agent {agent.IdAgent}",
                        MontantCollecte = montantCollecte,
                        NombrePaiements = nombrePaiements,
                        TauxConversion = (decimal)tauxConversion
                    });
                }

                return topAgents.OrderByDescending(t => t.MontantCollecte).Take(10).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul du top des agents caissiers pour la société {SocieteId}", idSociete);
                return new List<TopAgentDto>();
            }
        }

        /// <summary>
        /// Calcule la performance mensuelle
        /// </summary>
        private async Task<List<PerformanceMensuelleDto>> GetPerformanceMensuelleAsync(int idSociete, List<int> categoriesIds, List<int> clientsIds)
        {
            try
            {
                var performance = new List<PerformanceMensuelleDto>();
                
                // Parcourir les 6 derniers mois
                var dateCourante = DateTime.Now.AddMonths(-5);
                var finPeriode = DateTime.Now;
                
                while (dateCourante <= finPeriode)
                {
                    var debutMois = new DateTime(dateCourante.Year, dateCourante.Month, 1);
                    var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                    
                    // Calculer les montants pour ce mois
                    var montantDuMois = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                   cf.MontantDu.HasValue &&
                                   cf.MontantDu.Value > 0 &&
                                   clientsIds.Contains(cf.IdClient) &&
                                   cf.DateCreation >= debutMois &&
                                   cf.DateCreation <= finMois)
                        .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                    var montantCollecteMois = await _context.Paiements
                        .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                                   p.DatePaiement >= debutMois &&
                                   p.DatePaiement <= finMois &&
                                   p.IdClient.HasValue &&
                                   clientsIds.Contains(p.IdClient.Value))
                        .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                    var nombrePaiementsMois = await _context.Paiements
                        .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                                   p.DatePaiement >= debutMois &&
                                   p.DatePaiement <= finMois &&
                                   p.IdClient.HasValue &&
                                   clientsIds.Contains(p.IdClient.Value))
                        .CountAsync();

                    // Calculer le taux de recouvrement mensuel
                    var tauxRecouvrementMois = montantDuMois > 0
                        ? Math.Round(((montantCollecteMois / montantDuMois) * 100), 2)
                        : (montantCollecteMois > 0 ? 100 : 0);

                    // Calculer le ticket moyen
                    var ticketMoyen = nombrePaiementsMois > 0
                        ? Math.Round(montantCollecteMois / nombrePaiementsMois, 2)
                        : 0;

                    performance.Add(new PerformanceMensuelleDto
                    {
                        Mois = $"{CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.GetMonthName(dateCourante.Month)} {dateCourante.Year}",
                        TauxRecouvrement = tauxRecouvrementMois,
                        MontantCollecte = montantCollecteMois,
                        NombrePaiements = nombrePaiementsMois,
                        TicketMoyen = ticketMoyen
                    });

                    dateCourante = dateCourante.AddMonths(1);
                }

                return performance.OrderByDescending(p => p.Mois).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul de la performance mensuelle pour la société {SocieteId}", idSociete);
                return new List<PerformanceMensuelleDto>();
            }
        }
        private async Task<List<RepartitionClientParCategorieDto>> GetRepartitionClientsParCategorieAsync(
            int idSociete,
            List<int> categoriesIds,
            List<int> activeClientIds)
        {
            try
            {
                // Récupérer les catégories de la société
                var categories = await _context.CategorieClients
                    .Where(cc => cc.IdSociete == idSociete)
                    .Select(cc => new { cc.IdCategorie, cc.NomCategorie })
                    .ToListAsync();

                // Compter les clients actifs par catégorie (via leurs usages)
                var clientsParCategorie = await _context.ClientUsages
                    .Include(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                    .Include(cu => cu.Client)
                    .Where(cu => cu.Client != null &&
                               activeClientIds.Contains(cu.IdClient) &&
                               cu.Statut == true &&
                               cu.Usage != null &&
                               cu.Usage.CategorieClient != null &&
                               categoriesIds.Contains(cu.Usage.CategorieClient.IdCategorie))
                    .GroupBy(cu => cu.Usage.CategorieClient.IdCategorie)
                    .Select(g => new
                    {
                        IdCategorie = g.Key,
                        NombreClients = g.Select(cu => cu.IdClient).Distinct().Count(),
                        MontantTotal = g.Sum(cu => cu.Client != null ? 
                            _context.ClientFactures.Where(cf => cf.IdClient == cu.Client.IdClient && cf.Montant.HasValue)
                                .Sum(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value)) : 0)
                    })
                    .ToListAsync();

                var totalClients = clientsParCategorie.Sum(c => c.NombreClients);

                var repartition = clientsParCategorie
                    .Join(categories, 
                        c => c.IdCategorie, 
                        cat => cat.IdCategorie, 
                        (c, cat) => new RepartitionClientParCategorieDto
                        {
                            IdCategorie = c.IdCategorie,
                            NomCategorie = cat.NomCategorie ?? "Sans catégorie",
                            NombreClients = c.NombreClients,
                            Pourcentage = totalClients > 0 ? (decimal)Math.Round((c.NombreClients * 100.0 / totalClients), 2) : 0,
                            MontantTotal = c.MontantTotal
                        })
                    .OrderByDescending(c => c.NombreClients)
                    .ToList();

                return repartition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul de la répartition des clients par catégorie pour la société {SocieteId}", idSociete);
                return new List<RepartitionClientParCategorieDto>();
            }
        }

        /// <summary>
        /// Calcule la répartition des clients par axe/cabine
        /// </summary>
        private async Task<List<RepartitionClientParAxeDto>> GetRepartitionClientsParAxeAsync(int idSociete, List<int> activeClientIds)
        {
            try
            {
                if (!activeClientIds.Any())
                {
                    return new List<RepartitionClientParAxeDto>();
                }

                // Compter les clients actifs par axe/cabine
                var clientsParAxe = await _context.Clients
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                    .Where(c => activeClientIds.Contains(c.IdClient) &&
                               c.Axe != null &&
                               c.Axe.Cabine != null &&
                               c.Axe.Cabine.IdSociete == idSociete)
                    .GroupBy(c => new { c.Axe.IdAxe, c.Axe.NomAxe, c.Axe.Cabine.IdCabine, c.Axe.Cabine.Nom })
                    .Select(g => new
                    {
                        IdAxe = g.Key.IdAxe,
                        NomAxe = g.Key.NomAxe,
                        NomCabine = g.Key.Nom,
                        NombreClients = g.Count()
                    })
                    .ToListAsync();

                var totalClients = clientsParAxe.Sum(c => c.NombreClients);

                var repartition = clientsParAxe
                    .Select(c => new RepartitionClientParAxeDto
                    {
                        IdAxe = c.IdAxe,
                        NomAxe = c.NomAxe ?? "Sans axe",
                        NomCabine = c.NomCabine,
                        NombreClients = c.NombreClients,
                        Pourcentage = totalClients > 0 ? (decimal)Math.Round((c.NombreClients * 100.0 / totalClients), 2) : 0
                    })
                    .OrderByDescending(c => c.NombreClients)
                    .ToList();

                return repartition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul de la répartition des clients par axe pour la société {SocieteId}", idSociete);
                return new List<RepartitionClientParAxeDto>();
            }
        }

        /// <summary>
        /// Calcule les statistiques des factures par mois
        /// </summary>
        private async Task<List<StatistiqueFactureMoisDto>> GetStatistiquesFacturesMoisAsync(int idSociete, List<int> categoriesIds, List<int> clientsIds)
        {
            try
            {
                var statistiques = new List<StatistiqueFactureMoisDto>();
                
                // Parcourir les 12 derniers mois
                var dateCourante = DateTime.Now.AddMonths(-11);
                var finPeriode = DateTime.Now;
                
                while (dateCourante <= finPeriode)
                {
                    var debutMois = new DateTime(dateCourante.Year, dateCourante.Month, 1);
                    var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                    
                    // Calculer les statistiques pour ce mois
                    var facturesMois = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                   clientsIds.Contains(cf.IdClient) &&
                                   cf.DateCreation >= debutMois &&
                                   cf.DateCreation <= finMois)
                        .ToListAsync();

                    var montantTotal = facturesMois.Where(cf => cf.Montant.HasValue).Sum(cf => (cf.MontantDevisePrincipale ?? cf.Montant.Value));
                    var nombreFactures = facturesMois.Count;
                    var montantMoyen = nombreFactures > 0 ? Math.Round(montantTotal / nombreFactures, 2) : 0;

                    statistiques.Add(new StatistiqueFactureMoisDto
                    {
                        Mois = $"{CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.GetMonthName(dateCourante.Month)} {dateCourante.Year}",
                        MontantTotal = montantTotal,
                        NombreFactures = nombreFactures,
                        MontantMoyen = montantMoyen
                    });

                    dateCourante = dateCourante.AddMonths(1);
                }

                return statistiques.OrderByDescending(s => s.Mois).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques des factures par mois pour la société {SocieteId}", idSociete);
                return new List<StatistiqueFactureMoisDto>();
            }
        }

        /// <summary>
        /// Calcule l'activité des clients
        /// </summary>
        private async Task<ClientActiviteDto> GetClientActiviteAsync(List<int> financialClientIds)
        {
            try
            {
                if (!financialClientIds.Any())
                {
                    return new ClientActiviteDto();
                }

                var nombreClientsActifs = await _context.Clients
                    .Where(c => financialClientIds.Contains(c.IdClient) && c.IsActif == true)
                    .CountAsync();

                var nombreClientsInactifs = await _context.Clients
                    .Where(c => financialClientIds.Contains(c.IdClient) && c.IsActif == false)
                    .CountAsync();

                var totalClients = nombreClientsActifs + nombreClientsInactifs;
                var pourcentageActifs = totalClients > 0 ? (decimal)Math.Round((nombreClientsActifs * 100.0 / totalClients), 2) : 0;
                var pourcentageInactifs = totalClients > 0 ? (decimal)Math.Round((nombreClientsInactifs * 100.0 / totalClients), 2) : 0;

                return new ClientActiviteDto
                {
                    NombreClientsActifs = nombreClientsActifs,
                    NombreClientsInactifs = nombreClientsInactifs,
                    TotalClients = totalClients,
                    PourcentageActifs = pourcentageActifs,
                    PourcentageInactifs = pourcentageInactifs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul de l'activité des clients");
                return new ClientActiviteDto();
            }
        }
        private async Task<List<EvolutionMensuelleDto>> GetEvolutionMensuelleAsync(
            int idSociete, 
            List<int> categoriesIds, 
            List<int> clientsIds, 
            DateTime dateDebut, 
            DateTime dateFin)
        {
            try
            {
                var evolution = new List<EvolutionMensuelleDto>();
                
                // Parcourir chaque mois dans la période
                var dateCourante = new DateTime(dateDebut.Year, dateDebut.Month, 1);
                
                while (dateCourante <= dateFin)
                {
                    var debutMois = dateCourante;
                    var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59);
                    
                    // Calculer les montants pour ce mois
                    var montantFactures = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                   cf.MontantDu.HasValue &&
                                   cf.MontantDu.Value > 0 &&
                                   clientsIds.Contains(cf.IdClient) &&
                                   cf.DateCreation >= debutMois &&
                                   cf.DateCreation <= finMois)
                        .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                    var montantPaiements = await _context.Paiements
                        .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                                   p.DatePaiement >= debutMois &&
                                   p.DatePaiement <= finMois &&
                                   p.IdClient.HasValue &&
                                   clientsIds.Contains(p.IdClient.Value))
                        .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                    var montantArrieres = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                   cf.MontantDu.HasValue &&
                                   cf.MontantDu.Value > 0 &&
                                   clientsIds.Contains(cf.IdClient) &&
                                   cf.DateCreation <= finMois)
                        .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu.Value));

                    var nombreFactures = await _context.ClientFactures
                        .Where(cf => cf.Statut == true &&
                                   clientsIds.Contains(cf.IdClient) &&
                                   cf.DateCreation >= debutMois &&
                                   cf.DateCreation <= finMois)
                        .CountAsync();

                    var nombrePaiements = await _context.Paiements
                        .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                                   p.DatePaiement >= debutMois &&
                                   p.DatePaiement <= finMois &&
                                   p.IdClient.HasValue &&
                                   clientsIds.Contains(p.IdClient.Value))
                        .CountAsync();

                    evolution.Add(new EvolutionMensuelleDto
                    {
                        Mois = $"{CultureInfo.GetCultureInfo("fr-FR").DateTimeFormat.GetMonthName(dateCourante.Month)} {dateCourante.Year}",
                        MontantFactures = montantFactures,
                        MontantPaiements = montantPaiements,
                        MontantArrieres = montantArrieres,
                        NombreFactures = nombreFactures,
                        NombrePaiements = nombrePaiements
                    });

                    dateCourante = dateCourante.AddMonths(1);
                }

                return evolution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul de l'évolution mensuelle pour la société {SocieteId}", idSociete);
                return new List<EvolutionMensuelleDto>();
            }
        }

        private async Task<decimal> SumPaiementsValidesAsync(
            List<int> financialClientIds,
            DateTime dateDebut,
            DateTime dateFin)
        {
            return await _context.Paiements
                .Where(p => !p.IsDeleted &&
                           (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                           p.DatePaiement >= dateDebut &&
                           p.DatePaiement <= dateFin &&
                           p.IdClient.HasValue &&
                           financialClientIds.Contains(p.IdClient.Value))
                .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
        }

        /// <summary>
        /// Calcule la répartition des paiements par méthode
        /// </summary>
        private async Task<List<RepartitionPaiementDto>> GetRepartitionPaiementsAsync(
            int idSociete, 
            List<int> clientsIds, 
            DateTime dateDebut, 
            DateTime dateFin)
        {
            try
            {
                var paiementsParMethode = await _context.Paiements
                    .Where(p => !p.IsDeleted &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                               p.DatePaiement >= dateDebut &&
                               p.DatePaiement <= dateFin &&
                               p.IdClient.HasValue &&
                               clientsIds.Contains(p.IdClient.Value) &&
                               !string.IsNullOrWhiteSpace(p.MethodePaiement))
                    .GroupBy(p => p.MethodePaiement)
                    .Select(g => new
                    {
                        MethodePaiement = g.Key,
                        MontantTotal = g.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye)),
                        NombrePaiements = g.Count()
                    })
                    .ToListAsync();

                var montantTotal = paiementsParMethode.Sum(p => p.MontantTotal);

                var repartition = paiementsParMethode
                    .Select(p => new RepartitionPaiementDto
                    {
                        MethodePaiement = p.MethodePaiement,
                        MontantTotal = p.MontantTotal,
                        NombrePaiements = p.NombrePaiements,
                        Pourcentage = montantTotal > 0 ? Math.Round((p.MontantTotal / montantTotal) * 100, 2) : 0
                    })
                    .OrderByDescending(p => p.MontantTotal)
                    .ToList();

                return repartition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul de la répartition des paiements pour la société {SocieteId}", idSociete);
                return new List<RepartitionPaiementDto>();
            }
        }

        /// <summary>
        /// Génère le libellé de la période
        /// </summary>
        private string GetLibellePeriode(DateTime? debut, DateTime? fin)
        {
            if (!debut.HasValue && !fin.HasValue)
            {
                return "Toutes périodes";
            }

            if (debut.HasValue && fin.HasValue)
            {
                return $"Du {debut.Value:dd/MM/yyyy} au {fin.Value:dd/MM/yyyy}";
            }

            if (debut.HasValue)
            {
                return $"Depuis le {debut.Value:dd/MM/yyyy}";
            }

            return $"Jusqu'au {fin.Value:dd/MM/yyyy}";
        }

        private async Task<List<int>> GetCategorieIdsAsync(int idSociete)
        {
            return await _context.CategorieClients
                .Where(cc => cc.IdSociete == idSociete && cc.Statut != false)
                .Select(cc => cc.IdCategorie)
                .ToListAsync();
        }

        /// <summary>
        /// Construit la liste d'IDs clients (actifs ou financiers) avec filtres optionnels.
        /// </summary>
        private async Task<List<int>> BuildClientIdsAsync(int idSociete, StatistiquesFiltresDto filtres, bool activeOnly)
        {
            var baseIds = activeOnly
                ? await _clientScope.GetActiveClientIdsAsync(idSociete)
                : await _clientScope.GetFinancialClientIdsAsync(idSociete);

            if (!baseIds.Any())
            {
                return baseIds;
            }

            if (filtres == null || !filtres.HasAnyFilter())
            {
                return baseIds;
            }

            var query = _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                .Where(c => baseIds.Contains(c.IdClient));

            query = query.AppliquerFiltresStatistiques(filtres);

            return await query.Select(c => c.IdClient).ToListAsync();
        }

        #endregion
    }
}
