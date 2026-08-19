using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class ClientDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<ClientDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public ClientDashboardService(
            KenergieDbContext context,
            ILogger<ClientDashboardService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<ClientDashboardDto> GetDashboardDataAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();

            var statistiques = await GetClientStatistiquesAsync(idClient);
            var facturesRecentes = await GetFacturesRecentesAsync(idClient);
            var paiementsRecents = await GetPaiementsRecentsAsync(idClient);
            var consommations = await GetConsommationsAsync(idClient);
            var alertesClient = await GetAlertesClientAsync(idClient);
            var resumeClient = await GetResumeClientAsync(idClient);

            return new ClientDashboardDto
            {
                Statistiques = statistiques,
                FacturesRecentes = facturesRecentes,
                PaiementsRecents = paiementsRecents,
                Consommations = consommations,
                AlertesClient = alertesClient,
                ResumeClient = resumeClient,
                DateGeneration = DateTime.UtcNow
            };
        }

        public async Task<ClientStatistiquesDto> GetClientStatistiquesAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();
            return await GetClientStatistiquesAsync(idClient);
        }

        public async Task<List<FactureRecenteDto>> GetFacturesRecentesAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();
            return await GetFacturesRecentesAsync(idClient);
        }

        public async Task<List<PaiementClientRecentDto>> GetPaiementsRecentsAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();
            return await GetPaiementsRecentsAsync(idClient);
        }

        public async Task<List<ConsommationDto>> GetConsommationsAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();
            return await GetConsommationsAsync(idClient);
        }

        public async Task<List<AlerteClientDto>> GetAlertesClientAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();
            return await GetAlertesClientAsync(idClient);
        }

        public async Task<ResumeClientDto> GetResumeClientAsync()
        {
            var idClient = await ResolveConnectedClientIdAsync();
            return await GetResumeClientAsync(idClient);
        }

        private async Task<int> ResolveConnectedClientIdAsync()
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
                throw new UnauthorizedAccessException("Utilisateur non authentifié.");

            var idClient = await _context.Utilisateurs
                .AsNoTracking()
                .Where(u => u.IdUtilisateur == userId)
                .Select(u => u.IdClient)
                .FirstOrDefaultAsync();

            if (!idClient.HasValue || idClient.Value <= 0)
                throw new UnauthorizedAccessException(
                    "Aucun client associé à cet utilisateur. Le dashboard client est réservé aux comptes liés à un client.");

            return idClient.Value;
        }

        private IQueryable<ClientFacture> ClientFacturesQuery(int idClient)
        {
            return _context.ClientFactures
                .AsNoTracking()
                .Where(cf => cf.IdClient == idClient && cf.Statut == true);
        }

        private async Task<ClientStatistiquesDto> GetClientStatistiquesAsync(int idClient)
        {
            try
            {
                _logger.LogInformation("Calcul des statistiques pour le client {IdClient}", idClient);

                var query = ClientFacturesQuery(idClient);

                var montantTotalFactures = await query
                    .SumAsync(cf => cf.MontantDevisePrincipale ?? cf.Montant ?? 0);

                var montantTotalPaye = await query
                    .SumAsync(cf => cf.MontantPayeDevisePrincipale ?? cf.MontantPaye ?? 0);

                var montantTotalDu = await query
                    .SumAsync(cf => cf.MontantDuDevisePrincipale ?? cf.MontantDu ?? 0);

                var nombreFactures = await query.CountAsync();

                var nombreFacturesPayees = await query
                    .Where(cf => (cf.MontantDu ?? 0) <= 0
                                 || (cf.MontantPaye ?? 0) >= (cf.Montant ?? 0))
                    .CountAsync();

                var dateLimite = DateTime.Now.AddDays(-30);
                var nombreFacturesEnRetard = await query
                    .Where(cf => cf.DateEmission.HasValue
                                 && cf.DateEmission.Value < dateLimite
                                 && (cf.MontantDu ?? 0) > 0)
                    .CountAsync();

                var tauxRecouvrement = montantTotalFactures > 0
                    ? Math.Round((montantTotalPaye / montantTotalFactures) * 100, 2)
                    : 0;

                var consommationTotale = montantTotalFactures;

                var dateDebut = DateTime.Now.AddMonths(-12);
                var consommationDerniers12Mois = await query
                    .Where(cf => cf.DateEmission.HasValue && cf.DateEmission.Value >= dateDebut)
                    .SumAsync(cf => cf.MontantDevisePrincipale ?? cf.Montant ?? 0);

                var consommationMoyenneMensuelle = consommationDerniers12Mois / 12;

                return new ClientStatistiquesDto
                {
                    MontantTotalFactures = montantTotalFactures,
                    MontantTotalPaye = montantTotalPaye,
                    MontantTotalDu = montantTotalDu,
                    NombreFactures = nombreFactures,
                    NombreFacturesPayees = nombreFacturesPayees,
                    NombreFacturesEnRetard = nombreFacturesEnRetard,
                    TauxRecouvrement = tauxRecouvrement,
                    ConsommationTotale = consommationTotale,
                    ConsommationMoyenneMensuelle = Math.Round(consommationMoyenneMensuelle, 2)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du calcul des statistiques client {IdClient}", idClient);
                return new ClientStatistiquesDto();
            }
        }

        private async Task<List<FactureRecenteDto>> GetFacturesRecentesAsync(int idClient)
        {
            var now = DateTime.Now;
            var items = await ClientFacturesQuery(idClient)
                .Include(cf => cf.Facture)
                .OrderByDescending(cf => cf.DateEmission ?? cf.DateCreation)
                .Take(10)
                .ToListAsync();

            return items.Select(cf =>
            {
                var montant = cf.MontantDevisePrincipale ?? cf.Montant ?? 0;
                var paye = cf.MontantPayeDevisePrincipale ?? cf.MontantPaye ?? 0;
                var du = cf.MontantDuDevisePrincipale ?? cf.MontantDu ?? 0;
                var dateEmission = cf.DateEmission ?? cf.DateCreation;
                var dateEcheance = dateEmission.AddDays(30);
                var joursRetard = du > 0 && dateEmission < now.AddDays(-30)
                    ? (int)(now - dateEmission).TotalDays - 30
                    : 0;
                if (joursRetard < 0) joursRetard = 0;

                string statut;
                if (du <= 0)
                    statut = "Payée";
                else if (dateEmission < now.AddDays(-30))
                    statut = "En retard";
                else
                    statut = "En cours";

                var moisAnnee = !string.IsNullOrWhiteSpace(cf.Mois) && cf.Annees.HasValue
                    ? $"{cf.Mois}/{cf.Annees}"
                    : dateEmission.ToString("MM/yyyy");

                return new FactureRecenteDto
                {
                    IdFacture = cf.IdFacture ?? cf.IdClientFacture,
                    Reference = cf.Facture?.NumeroFacture
                                ?? (cf.EstArrierePreExistant ? $"ARR-{cf.IdClientFacture}" : $"CF-{cf.IdClientFacture}"),
                    MoisAnnee = moisAnnee,
                    MontantTotal = montant,
                    MontantPaye = paye,
                    MontantDu = du,
                    DateEmission = dateEmission,
                    DateEcheance = dateEcheance,
                    Statut = statut,
                    JoursRetard = joursRetard
                };
            }).ToList();
        }

        private async Task<List<PaiementClientRecentDto>> GetPaiementsRecentsAsync(int idClient)
        {
            var paiements = await _context.Paiements
                .AsNoTracking()
                .Include(p => p.Facture)
                .Where(p => p.IdClient == idClient
                            && p.IsDeleted == false
                            && p.Statut != null
                            && (p.Statut == "Validé" || p.Statut.ToLower() == "true"))
                .OrderByDescending(p => p.DatePaiement)
                .Take(10)
                .ToListAsync();

            return paiements.Select(p => new PaiementClientRecentDto
            {
                IdPaiement = p.IdPaiement,
                Reference = !string.IsNullOrWhiteSpace(p.ReferenceTransaction)
                    ? p.ReferenceTransaction!
                    : $"PAY-{p.IdPaiement}",
                MontantPaye = p.MontantPayeDevisePrincipale ?? p.MontantPaye,
                DatePaiement = p.DatePaiement,
                MethodePaiement = p.MethodePaiement ?? string.Empty,
                Statut = p.Statut ?? string.Empty,
                ReferenceFacture = p.Facture?.NumeroFacture
                                   ?? (p.IdClientFacture.HasValue ? $"CF-{p.IdClientFacture}" : string.Empty)
            }).ToList();
        }

        /// <summary>
        /// Proxy consommation : montants ClientFacture agrégés par mois (12 derniers mois).
        /// </summary>
        private async Task<List<ConsommationDto>> GetConsommationsAsync(int idClient)
        {
            var dateDebut = DateTime.Now.AddMonths(-12);
            var factures = await ClientFacturesQuery(idClient)
                .Where(cf => cf.DateEmission.HasValue && cf.DateEmission.Value >= dateDebut)
                .Select(cf => new
                {
                    Date = cf.DateEmission!.Value,
                    Montant = cf.MontantDevisePrincipale ?? cf.Montant ?? 0,
                    Mois = cf.Mois,
                    Annees = cf.Annees
                })
                .ToListAsync();

            var grouped = factures
                .GroupBy(f => new
                {
                    Year = f.Annees ?? f.Date.Year,
                    Month = int.TryParse(f.Mois, out var m) ? m : f.Date.Month
                })
                .OrderByDescending(g => g.Key.Year)
                .ThenByDescending(g => g.Key.Month)
                .Select((g, index) => new ConsommationDto
                {
                    IdConsommation = index + 1,
                    Reference = $"{g.Key.Month:D2}/{g.Key.Year}",
                    Consommation = g.Sum(x => x.Montant),
                    Unite = "CDF",
                    DateConsommation = new DateTime(g.Key.Year, Math.Clamp(g.Key.Month, 1, 12), 1),
                    PrixUnitaire = 0,
                    MontantTotal = g.Sum(x => x.Montant),
                    TypeConsommation = "Facturation"
                })
                .ToList();

            return grouped;
        }

        private async Task<List<AlerteClientDto>> GetAlertesClientAsync(int idClient)
        {
            var dateLimite = DateTime.Now.AddDays(-30);
            var enRetard = await ClientFacturesQuery(idClient)
                .Include(cf => cf.Facture)
                .Where(cf => (cf.MontantDu ?? 0) > 0
                             && cf.DateEmission.HasValue
                             && cf.DateEmission.Value < dateLimite)
                .OrderBy(cf => cf.DateEmission)
                .Take(20)
                .ToListAsync();

            return enRetard.Select((cf, index) =>
            {
                var dateEmission = cf.DateEmission ?? cf.DateCreation;
                var jours = (int)(DateTime.Now - dateEmission).TotalDays;
                var du = cf.MontantDuDevisePrincipale ?? cf.MontantDu ?? 0;
                var niveau = jours > 60 ? "Haute" : "Moyenne";

                return new AlerteClientDto
                {
                    IdAlerte = index + 1,
                    TypeAlerte = "Facture en retard",
                    Description = $"Facture en retard depuis {jours} jour(s)",
                    NiveauCriticite = niveau,
                    DateAlerte = DateTime.Now,
                    IdFacture = cf.IdFacture ?? cf.IdClientFacture,
                    ReferenceFacture = cf.Facture?.NumeroFacture
                                       ?? (cf.EstArrierePreExistant ? $"ARR-{cf.IdClientFacture}" : $"CF-{cf.IdClientFacture}"),
                    MontantConcerne = du,
                    EstLue = false
                };
            }).ToList();
        }

        private async Task<ResumeClientDto> GetResumeClientAsync(int idClient)
        {
            var soldeActuel = await ClientFacturesQuery(idClient)
                .SumAsync(cf => cf.MontantDuDevisePrincipale ?? cf.MontantDu ?? 0);

            var nombreServicesActifs = await _context.ClientUsages
                .AsNoTracking()
                .CountAsync(cu => cu.IdClient == idClient && cu.Statut == true);

            var derniereConnexion = await _context.Utilisateurs
                .AsNoTracking()
                .Where(u => u.IdUtilisateur == _currentUserService.UserId)
                .Select(u => (DateTime?)u.DateCreation)
                .FirstOrDefaultAsync();

            var prochaine = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);

            return new ResumeClientDto
            {
                SoldeActuel = soldeActuel,
                LimiteCredit = 0,
                CreditDisponible = 0,
                DerniereConnexion = derniereConnexion ?? DateTime.Now,
                StatutCompte = soldeActuel > 0 ? "Soldeur" : "À jour",
                NombreServicesActifs = nombreServicesActifs,
                ProchaineFacture = prochaine
            };
        }
    }
}
