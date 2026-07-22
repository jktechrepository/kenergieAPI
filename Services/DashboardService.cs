using Kenergie.Models.DTOs;
using Kenergie.Models;
using Kenergie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour calculer les statistiques du dashboard
    /// </summary>
    public class DashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<DashboardService> _logger;
        private readonly ISocieteClientScopeService _clientScope;

        public DashboardService(
            KenergieDbContext context,
            ILogger<DashboardService> logger,
            ISocieteClientScopeService clientScope)
        {
            _context = context;
            _logger = logger;
            _clientScope = clientScope;
        }

        /// <summary>
        /// Récupère toutes les statistiques du dashboard pour une société
        /// </summary>
        public async Task<DashboardDto> GetDashboardDataAsync(int societeId)
        {
            try
            {
                var dashboard = new DashboardDto();  

                // Clients actifs (effectif) vs tous les clients rattachés (KPI financiers)
                var activeClientIds = await _clientScope.GetActiveClientIdsAsync(societeId);
                var financialClientIds = await _clientScope.GetFinancialClientIdsAsync(societeId);

                // 1. Total agents actifs
                dashboard.TotalAgents = await _context.Agents
                    .Where(a => a.IdSociete == societeId && a.Statut == true)
                    .CountAsync();

                // 2. Total clients actifs
                dashboard.TotalClientsActifs = activeClientIds.Count;

                // 3. Calculs paiements mensuels (tous clients rattachés, hors supprimés)
                dashboard.CollecteMois = await GetCollecteMoisAsync(societeId, financialClientIds);

                // 4. Total général des arriérés (périmètre société)
                dashboard.TotalGeneralArriere = await GetTotalGeneralArrieresAsync(societeId, financialClientIds);

                // 5. Facturation mensuelle
                dashboard.FactureMois = await GetFactureMoisAsync(societeId, financialClientIds);

                // 6. Paiements du mois (pour compatibilité)
                dashboard.PaiementsDuMois = dashboard.CollecteMois.Montant;

                // 7. Répartition clients par catégorie
                dashboard.RepartitionClientsParCategorie = await GetRepartitionClientsParCategorieAsync(societeId);

                // 8. Top 10 agents collecteurs
                dashboard.Top10AgentsCollecteurs = await GetTop10AgentsCollecteursAsync(societeId, financialClientIds);

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du dashboard pour la société {SocieteId}", societeId);
                return new DashboardDto();
            }
        }

        /// <summary>
        /// Calcule la collecte mensuelle avec variations
        /// </summary>
        private async Task<CollecteMoisDto> GetCollecteMoisAsync(int societeId, List<int> clientIds)
        {
            try
            {
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
                var debutMoisPrecedent = debutMois.AddMonths(-1);
                var finMoisPrecedent = debutMois.AddDays(-1).AddHours(23).AddMinutes(59);

                // Paiements du mois en cours
                var paiementsMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMois && 
                           p.DatePaiement <= finMois &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .ToListAsync();

                var montantMois = paiementsMois.Sum(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));
                var nombrePaiements = paiementsMois.Count;
                var ticketMoyen = nombrePaiements > 0 ? montantMois / nombrePaiements : 0;

                // Paiements du mois précédent
                var montantMoisPrecedent = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMoisPrecedent && 
                           p.DatePaiement <= finMoisPrecedent &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                var nombrePaiementsPrecedent = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMoisPrecedent && 
                           p.DatePaiement <= finMoisPrecedent &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .CountAsync();

                var ticketMoyenPrecedent = nombrePaiementsPrecedent > 0 ? montantMoisPrecedent / nombrePaiementsPrecedent : 0;

                // Calcul des variations
                var variationPourcentage = montantMoisPrecedent == 0
                    ? (montantMois > 0 ? 100 : 0)
                    : Math.Round(((montantMois - montantMoisPrecedent) / montantMoisPrecedent) * 100, 2);

                var variationTicketMoyen = ticketMoyenPrecedent == 0
                    ? 0
                    : Math.Round(((ticketMoyen - ticketMoyenPrecedent) / ticketMoyenPrecedent) * 100, 2);

                return new CollecteMoisDto
                {
                    MoisLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)} {DateTime.Now.Year}",
                    Montant = montantMois,
                    MontantMoisPrecedent = montantMoisPrecedent,
                    VariationPourcentage = variationPourcentage,
                    NombrePaiements = nombrePaiements,
                    TicketMoyen = ticketMoyen,
                    VariationTicketMoyen = variationTicketMoyen
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de la collecte mensuelle pour la société {SocieteId}", societeId);
                return new CollecteMoisDto();
            }
        }

        /// <summary>
        /// Calcule la facturation mensuelle avec variations
        /// </summary>
        private async Task<FactureMoisDto> GetFactureMoisAsync(int societeId, List<int> clientIds)
        {
            try
            {
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);
                var debutMoisPrecedent = debutMois.AddMonths(-1);
                var finMoisPrecedent = debutMois.AddDays(-1).AddHours(23).AddMinutes(59);

                // Factures du mois en cours - gère les deux formats de mois: "01" et "1"
                var moisActuelNormalise = NormaliserMois(DateTime.Now.Month.ToString());
                
                var facturesMois = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && 
                               f.Statut == true &&
                               f.Mois == moisActuelNormalise &&
                               f.Annees == DateTime.Now.Year)
                    .ToListAsync();

                var montantTotalFactures = facturesMois.Sum(f => (f.MontantDevisePrincipale ?? f.Montant ?? 0));
                var nombreFactures = facturesMois.Count;
                var factureMoyenne = nombreFactures > 0 ? montantTotalFactures / nombreFactures : 0;

                // Factures du mois précédent - gère les deux formats de mois: "01" et "1"
                var moisPrecedentNormalise = NormaliserMois(debutMoisPrecedent.Month.ToString());
                
                var facturesMoisPrecedent = await _context.ClientFactures
                    .Where(f => clientIds.Contains(f.IdClient) && 
                               f.Statut == true &&
                               f.Mois == moisPrecedentNormalise &&
                               f.Annees == debutMoisPrecedent.Year)
                    .ToListAsync();

                var montantMoisPrecedent = facturesMoisPrecedent.Sum(f => (f.MontantDevisePrincipale ?? f.Montant ?? 0));
                var nombreFacturesMoisPrecedent = facturesMoisPrecedent.Count;
                var factureMoyenneMoisPrecedent = nombreFacturesMoisPrecedent > 0 
                    ? montantMoisPrecedent / nombreFacturesMoisPrecedent 
                    : 0;

                // Calcul de la variation
                var variationPourcentage = montantMoisPrecedent == 0
                    ? (montantTotalFactures > 0 ? 100 : 0)
                    : Math.Round(((montantTotalFactures - montantMoisPrecedent) / montantMoisPrecedent) * 100, 2);

                // Calcul de la variation de la facture moyenne
                var variationFactureMoyenne = factureMoyenneMoisPrecedent == 0
                    ? (factureMoyenne > 0 ? 100 : 0)
                    : Math.Round(((factureMoyenne - factureMoyenneMoisPrecedent) / factureMoyenneMoisPrecedent) * 100, 2);

                // Taux de recouvrement estimé
                // NOTE: La facturation est post-consommation, donc les paiements du mois M correspondent aux factures du mois M-1
                var paiementsDuMois = await _context.Paiements
                    .Where(p => !p.IsDeleted && 
                           p.DatePaiement >= debutMois && 
                           p.DatePaiement <= finMois &&
                           p.IdClient.HasValue && 
                           clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                var tauxRecouvrementEstime = montantMoisPrecedent > 0
                    ? Math.Round((paiementsDuMois / montantMoisPrecedent) * 100, 2)
                    : 0;

                return new FactureMoisDto
                {
                    MoisLabel = $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month)} {DateTime.Now.Year}",
                    MontantTotalFactures = montantTotalFactures,
                    MontantTotalFacturesMoisPrecedent = montantMoisPrecedent,
                    VariationPourcentage = variationPourcentage,
                    NombreFactures = nombreFactures,
                    NombreFacturesMoisPrecedent = nombreFacturesMoisPrecedent,
                    FactureMoyenne = factureMoyenne,
                    FactureMoyenneMoisPrecedent = factureMoyenneMoisPrecedent,
                    VariationFactureMoyenne = variationFactureMoyenne,
                    TauxRecouvrementEstime = tauxRecouvrementEstime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de la facturation mensuelle pour la société {SocieteId}", societeId);
                return new FactureMoisDto();
            }
        }

        /// <summary>
        /// Calcule la répartition des clients par catégorie
        /// </summary>
        private async Task<List<RepartitionClientParCategorieDto>> GetRepartitionClientsParCategorieAsync(int societeId)
        {
            try
            {
                var categories = await _context.CategorieClients
                    .Where(cc => cc.IdSociete == societeId && cc.Statut != false)
                    .ToListAsync();

                var result = new List<RepartitionClientParCategorieDto>();

                foreach (var categorie in categories)
                {
                    var usageIds = await _context.Usages
                        .Where(u => u.IdCategorieClient == categorie.IdCategorie && u.Statut == true)
                        .Select(u => u.IdUsage)
                        .ToListAsync();

                    var nombreClients = await (
                        from cu in _context.ClientUsages
                        join c in _context.Clients on cu.IdClient equals c.IdClient
                        where usageIds.Contains(cu.IdUsage)
                              && cu.Statut == true
                              && c.IsActif == true
                              && c.Statut == true
                              && (!c.IsDeleted.HasValue || !c.IsDeleted.Value)
                        select cu.IdClient
                    ).Distinct().CountAsync();

                    result.Add(new RepartitionClientParCategorieDto
                    {
                        IdCategorie = categorie.IdCategorie,
                        NomCategorie = categorie.NomCategorie ?? "Inconnue",
                        NombreClients = nombreClients,
                        Pourcentage = 0 // Sera calculé après
                    });
                }

                // Calcul des pourcentages
                var totalClients = result.Sum(r => r.NombreClients);
                if (totalClients > 0)
                {
                    foreach (var item in result)
                    {
                        item.Pourcentage = Math.Round((decimal)item.NombreClients / totalClients * 100, 2);
                    }
                }

                return result.OrderByDescending(r => r.NombreClients).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul de la répartition des clients pour la société {SocieteId}", societeId);
                return new List<RepartitionClientParCategorieDto>();
            }
        }

        /// <summary>
        /// Calcule le total général des arriérés pour les clients rattachés à la société.
        /// </summary>
        private async Task<decimal> GetTotalGeneralArrieresAsync(int societeId, List<int> clientIds)
        {
            try
            {
                if (!clientIds.Any())
                {
                    return 0;
                }

                var montantTotalFactures = await _context.ClientFactures
                    .Where(f => f.Statut == true && clientIds.Contains(f.IdClient))
                    .SumAsync(f => (f.MontantDevisePrincipale ?? f.Montant ?? 0));

                var montantTotalPaiements = await _context.Paiements
                    .Where(p => !p.IsDeleted
                        && p.IdClient.HasValue
                        && clientIds.Contains(p.IdClient.Value))
                    .SumAsync(p => (p.MontantPayeDevisePrincipale ?? p.MontantPaye));

                return montantTotalFactures - montantTotalPaiements;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul du total général des arriérés pour la société {SocieteId}", societeId);
                return 0;
            }
        }

        /// <summary>
        /// Récupère le top 10 des agents collecteurs pour une société
        /// </summary>
        private async Task<List<TopAgentCollecteurDto>> GetTop10AgentsCollecteursAsync(int societeId, List<int> clientIds)
        {
            try
            {
                // Période pour le calcul (mois en cours uniquement)
                var debutMois = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var finMois = debutMois.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59);

                // Étape 1: Récupérer les paiements avec les IDs utilisateur et agent (mois en cours uniquement)
                var paiementsParAgent = await _context.Paiements
                    .AsNoTracking()
                    .Where(p => !p.IsDeleted && 
                               p.IdUtilisateur.HasValue &&
                               p.IdClient.HasValue &&
                               clientIds.Contains(p.IdClient.Value) &&
                               p.MontantPaye > 0 &&
                               p.DatePaiement >= debutMois && 
                               p.DatePaiement <= finMois)
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
                               u.Agent.IdSociete == societeId &&
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
                _logger.LogError(ex, "Erreur lors de la récupération du top 10 des agents collecteurs pour la société {SocieteId}", societeId);
                return new List<TopAgentCollecteurDto>();
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
    }
}
