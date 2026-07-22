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
            // Exécuter les requêtes séquentiellement pour éviter les problèmes de concurrence DbContext
            var statistiques = await GetClientStatistiquesAsync();
            var facturesRecentes = await GetFacturesRecentesAsync();
            var paiementsRecents = await GetPaiementsRecentsAsync();
            var consommations = await GetConsommationsAsync();
            var alertesClient = await GetAlertesClientAsync();
            var resumeClient = await GetResumeClientAsync();

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
            try
            {
                _logger.LogInformation("📊 Début du calcul des statistiques client");

                // Pour l'instant, nous allons calculer les statistiques globales
                // TODO: Filtrer par client spécifique une fois GetClientId() disponible

                // 1. Montant total des factures
                var montantTotalFactures = await _context.ClientFactures
                    .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant ?? 0));

                // 2. Montant total payé
                var montantTotalPaye = await _context.ClientFactures
                    .SumAsync(cf => (cf.MontantPayeDevisePrincipale ?? cf.MontantPaye ?? 0));

                // 3. Montant total dû
                var montantTotalDu = await _context.ClientFactures
                    .SumAsync(cf => (cf.MontantDuDevisePrincipale ?? cf.MontantDu ?? 0));

                // 4. Nombre total de factures
                var nombreFactures = await _context.ClientFactures
                    .CountAsync();

                // 5. Nombre de factures payées (MontantDu <= 0 ou MontantPaye >= Montant)
                var nombreFacturesPayees = await _context.ClientFactures
                    .Where(cf => cf.MontantDu <= 0 || (cf.MontantPaye >= cf.Montant))
                    .CountAsync();

                // 6. Nombre de factures en retard (DateEmission > 30 jours et MontantDu > 0)
                var dateLimite = DateTime.Now.AddDays(-30);
                var nombreFacturesEnRetard = await _context.ClientFactures
                    .Where(cf => cf.DateEmission.HasValue && 
                           cf.DateEmission.Value < dateLimite && 
                           cf.MontantDu > 0)
                    .CountAsync();

                // 7. Taux de recouvrement
                var tauxRecouvrement = montantTotalFactures > 0 
                    ? Math.Round((montantTotalPaye / montantTotalFactures) * 100, 2)
                    : 0;

                // 8. Consommation totale (basée sur le montant total comme approximation)
                var consommationTotale = montantTotalFactures;

                // 9. Consommation moyenne mensuelle (basée sur les 12 derniers mois)
                var dateDebut = DateTime.Now.AddMonths(-12);
                var consommationDerniers12Mois = await _context.ClientFactures
                    .Where(cf => cf.DateEmission.HasValue && 
                           cf.DateEmission.Value >= dateDebut)
                    .SumAsync(cf => (cf.MontantDevisePrincipale ?? cf.Montant ?? 0));

                var consommationMoyenneMensuelle = consommationDerniers12Mois / 12;

                var statistiques = new ClientStatistiquesDto
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

                _logger.LogInformation($"✅ Statistiques calculées: {nombreFactures} factures, {tauxRecouvrement}% recouvrement");

                return statistiques;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du calcul des statistiques client");
                
                // Retourner des valeurs par défaut en cas d'erreur
                return new ClientStatistiquesDto
                {
                    MontantTotalFactures = 0,
                    MontantTotalPaye = 0,
                    MontantTotalDu = 0,
                    NombreFactures = 0,
                    NombreFacturesPayees = 0,
                    NombreFacturesEnRetard = 0,
                    TauxRecouvrement = 0,
                    ConsommationTotale = 0,
                    ConsommationMoyenneMensuelle = 0
                };
            }
        }

        public async Task<List<FactureRecenteDto>> GetFacturesRecentesAsync()
        {
            var result = new List<FactureRecenteDto>
                {
                    new FactureRecenteDto
                    {
                        IdFacture = 1,
                        Reference = "FAC-000001",
                        MoisAnnee = "01/2024",
                        MontantTotal = 20000,
                        MontantPaye = 15000,
                        MontantDu = 5000,
                        DateEmission = DateTime.Now.AddDays(-5),
                        DateEcheance = DateTime.Now.AddDays(5),
                        Statut = "Payée",
                        JoursRetard = 0
                    },
                    new FactureRecenteDto
                    {
                        IdFacture = 2,
                        Reference = "FAC-000002",
                        MoisAnnee = "02/2024",
                        MontantTotal = 25000,
                        MontantPaye = 20000,
                        MontantDu = 5000,
                        DateEmission = DateTime.Now.AddDays(-10),
                        DateEcheance = DateTime.Now.AddDays(10),
                        Statut = "Payée",
                        JoursRetard = 0
                    }
                };

            return result;
        }

        public async Task<List<PaiementClientRecentDto>> GetPaiementsRecentsAsync()
        {
            var result = new List<PaiementClientRecentDto>
                {
                    new PaiementClientRecentDto
                    {
                        IdPaiement = 1,
                        Reference = "PAY-000001",
                        MontantPaye = 5000,
                        DatePaiement = DateTime.Now.AddDays(-2),
                        MethodePaiement = "Mobile Money",
                        Statut = "Validé",
                        ReferenceFacture = "FAC-000001"
                    },
                    new PaiementClientRecentDto
                    {
                        IdPaiement = 2,
                        Reference = "PAY-000002",
                        MontantPaye = 30000,
                        DatePaiement = DateTime.Now.AddDays(-5),
                        MethodePaiement = "Espèces",
                        Statut = "Validé",
                        ReferenceFacture = "FAC-000002"
                    }
                };

            return result;
        }

        public async Task<List<ConsommationDto>> GetConsommationsAsync()
        {
            var result = new List<ConsommationDto>
                {
                    new ConsommationDto
                    {
                        IdConsommation = 1,
                        Reference = "CONS-000001",
                        Consommation = 150,
                        Unite = "kWh",
                        DateConsommation = DateTime.Now.AddDays(-3),
                        PrixUnitaire = 150,
                        MontantTotal = 15000,
                        TypeConsommation = "Normal"
                    },
                    new ConsommationDto
                    {
                        IdConsommation = 2,
                        Reference = "CONS-000002",
                        Consommation = 200,
                        Unite = "kWh",
                        DateConsommation = DateTime.Now.AddDays(-7),
                        PrixUnitaire = 200,
                        MontantTotal = 20000,
                        TypeConsommation = "Normal"
                    }
                };

            return result;
        }

        public async Task<List<AlerteClientDto>> GetAlertesClientAsync()
        {
            var result = new List<AlerteClientDto>
                {
                    new AlerteClientDto
                    {
                        IdAlerte = 1,
                        TypeAlerte = "Facture en retard",
                        Description = "Facture en retard depuis 15 jours",
                        NiveauCriticite = "Moyenne",
                        DateAlerte = DateTime.Now,
                        IdFacture = 2,
                        ReferenceFacture = "FAC-000002",
                        MontantConcerne = 5000,
                        EstLue = false
                    }
                };

            return result;
        }

        public async Task<ResumeClientDto> GetResumeClientAsync()
        {
            var result = new ResumeClientDto
            {
                SoldeActuel = 20000,
                LimiteCredit = 500000,
                CreditDisponible = 480000,
                DerniereConnexion = DateTime.Now.AddHours(-2),
                StatutCompte = "Débiteur",
                NombreServicesActifs = 3,
                ProchaineFacture = DateTime.Now.AddDays(15)
            };

            return result;
        }
    }
}
