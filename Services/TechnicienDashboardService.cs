using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class TechnicienDashboardService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<TechnicienDashboardService> _logger;
        private readonly ICurrentUserService _currentUserService;

        public TechnicienDashboardService(
            KenergieDbContext context, 
            ILogger<TechnicienDashboardService> logger,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<TechnicienDashboardDto> GetDashboardDataAsync()
        {
            try
            {
                var userId = _currentUserService.GetUserId();
                if (userId == 0)
                {
                    _logger.LogWarning("ID d'utilisateur non trouvé pour le technicien");
                    throw new UnauthorizedAccessException("ID d'utilisateur non trouvé");
                }

                // Exécuter les requêtes séquentiellement pour éviter les problèmes de concurrence DbContext
                var statistiques = await GetTechnicienStatistiquesAsync(userId);
                var interventionsEnCours = await GetInterventionsEnCoursAsync(userId);
                var interventionsRecentes = await GetInterventionsRecentesAsync(userId);
                var pannesSignalees = await GetPannesSignaleesAsync(userId);
                var alertesTechnicien = await GetAlertesTechnicienAsync(userId);
                var performance = await GetPerformanceTechnicienAsync(userId);

                return new TechnicienDashboardDto
                {
                    Statistiques = statistiques,
                    InterventionsEnCours = interventionsEnCours,
                    InterventionsRecentes = interventionsRecentes,
                    PannesSignalees = pannesSignalees,
                    AlertesTechnicien = alertesTechnicien,
                    Performance = performance,
                    DateGeneration = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données du dashboard Technicien");
                throw;
            }
        }

        public async Task<TechnicienStatistiquesDto> GetTechnicienStatistiquesAsync(int userId)
        {
            var toutesInterventions = await _context.PanneSignalements
                .ToListAsync();

            var interventionsAujourdhui = toutesInterventions
                .Where(p => p.Statut == true)
                .Count();

            var interventionsCetteSemaine = toutesInterventions
                .Where(p => p.Statut == true)
                .Count();

            var interventionsCeMois = toutesInterventions
                .Where(p => p.Statut == true)
                .Count();

            var interventionsTerminees = toutesInterventions
                .Where(p => p.Statut == false)
                .Count();

            var pannesActives = toutesInterventions
                .Where(p => p.Statut == true)
                .Count();

            var tauxResolution = toutesInterventions.Any() ? 
                (decimal)interventionsTerminees / toutesInterventions.Count * 100 : 0;

            var moyenneInterventionsJour = toutesInterventions.Any() ? 1 : 0;

            return new TechnicienStatistiquesDto
            {
                TotalInterventions = toutesInterventions.Count,
                InterventionsAujourdhui = interventionsAujourdhui,
                InterventionsCetteSemaine = interventionsCetteSemaine,
                InterventionsCeMois = interventionsCeMois,
                TauxResolution = Math.Round(tauxResolution, 2),
                MoyenneInterventionsJour = moyenneInterventionsJour,
                PannesActives = pannesActives,
                ClientsIntervenus = toutesInterventions.Count
            };
        }

        public async Task<List<InterventionEnCoursDto>> GetInterventionsEnCoursAsync(int userId)
        {
            var interventions = await _context.PanneSignalements
                .Where(p => p.Statut == true)
                .OrderByDescending(p => p.IdPanneSignalement)
                .Take(20)
                .ToListAsync();

            var result = new List<InterventionEnCoursDto>();
            
            foreach (var intervention in interventions)
            {
                result.Add(new InterventionEnCoursDto
                {
                    IdIntervention = intervention.IdPanneSignalement,
                    Reference = $"INT-{intervention.IdPanneSignalement:D6}",
                    NomClient = $"Client #{intervention.IdPanneSignalement}",
                    TypePanne = intervention.TypePanne ?? "Non spécifié",
                    Description = intervention.Description ?? "",
                    Priorite = intervention.NiveauImportance ?? "Moyenne",
                    DateDebut = DateTime.Now,
                    DateFinPrevue = null,
                    Statut = intervention.Statut ? "Actif" : "Clôturé",
                    Societe = "Société",
                    Localisation = "Non spécifiée"
                });
            }

            return result;
        }

        public async Task<List<InterventionRecenteDto>> GetInterventionsRecentesAsync(int userId)
        {
            var interventions = await _context.PanneSignalements
                .OrderByDescending(p => p.IdPanneSignalement)
                .Take(20)
                .ToListAsync();

            var result = new List<InterventionRecenteDto>();
            
            foreach (var intervention in interventions)
            {
                var duree = "En cours";

                result.Add(new InterventionRecenteDto
                {
                    IdIntervention = intervention.IdPanneSignalement,
                    Reference = $"INT-{intervention.IdPanneSignalement:D6}",
                    NomClient = $"Client #{intervention.IdPanneSignalement}",
                    TypePanne = intervention.TypePanne ?? "Non spécifié",
                    DateIntervention = DateTime.Now,
                    DateFin = null,
                    Duree = duree,
                    Statut = intervention.Statut ? "Actif" : "Clôturé",
                    Technicien = "Technicien",
                    Societe = "Société"
                });
            }

            return result;
        }

        public async Task<List<PanneSignaleeDto>> GetPannesSignaleesAsync(int userId)
        {
            var pannes = await _context.PanneSignalements
                .Where(p => p.Statut == true)
                .OrderByDescending(p => p.IdPanneSignalement)
                .Take(20)
                .ToListAsync();

            var result = new List<PanneSignaleeDto>();
            
            foreach (var panne in pannes)
            {
                var tempsAttente = 24;

                result.Add(new PanneSignaleeDto
                {
                    IdPanne = panne.IdPanneSignalement,
                    Reference = $"PAN-{panne.IdPanneSignalement:D6}",
                    NomClient = $"Client #{panne.IdPanneSignalement}",
                    TypePanne = panne.TypePanne ?? "Non spécifié",
                    Description = panne.Description ?? "",
                    Priorite = panne.NiveauImportance ?? "Moyenne",
                    DateSignalement = DateTime.Now,
                    Statut = panne.Statut ? "Actif" : "Clôturé",
                    Societe = "Société",
                    TempsAttente = tempsAttente
                });
            }

            return result;
        }

        public async Task<List<AlerteTechnicienDto>> GetAlertesTechnicienAsync(int userId)
        {
            var alertes = new List<AlerteTechnicienDto>();

            var interventionsActives = await _context.PanneSignalements
                .Where(p => p.Statut == true)
                .ToListAsync();

            foreach (var intervention in interventionsActives)
            {
                alertes.Add(new AlerteTechnicienDto
                {
                    IdAlerte = alertes.Count + 1,
                    TypeAlerte = "Intervention active",
                    Description = $"Intervention en cours: {intervention.TypePanne}",
                    NiveauCriticite = "Moyenne",
                    DateAlerte = DateTime.Now,
                    IdClient = intervention.IdPanneSignalement,
                    NomClient = $"Client #{intervention.IdPanneSignalement}",
                    IdIntervention = intervention.IdPanneSignalement,
                    ReferenceIntervention = $"INT-{intervention.IdPanneSignalement:D6}",
                    EstLue = false
                });
            }

            return alertes.OrderByDescending(a => a.DateAlerte).ToList();
        }

        public async Task<PerformanceTechnicienDto> GetPerformanceTechnicienAsync(int userId)
        {
            var interventions = await _context.PanneSignalements
                .ToListAsync();

            var interventionsTerminees = interventions
                .Where(p => p.Statut == false)
                .Count();

            var interventionsEnRetard = interventions
                .Where(p => p.Statut == true)
                .Count();

            var tauxResolution = interventions.Any() ? 
                (decimal)interventionsTerminees / interventions.Count * 100 : 0;

            var tempsMoyenResolution = 24m;

            var performanceParType = interventions
                .GroupBy(p => p.TypePanne ?? "Autre")
                .Select(g => new PerformanceParTypeDto
                {
                    TypePanne = g.Key,
                    NombreInterventions = g.Count(),
                    TauxResolution = 100,
                    TempsMoyenResolution = tempsMoyenResolution
                })
                .ToList();

            var performanceMensuelle = interventions
                .GroupBy(p => new { Month = DateTime.Now.Month, Year = DateTime.Now.Year })
                .Select(g => new PerformanceMensuelleDto
                {
                    Mois = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                    Annee = g.Key.Year,
                    NombreInterventions = g.Count(),
                    TauxResolution = 100,
                    TempsMoyenResolution = tempsMoyenResolution
                })
                .OrderByDescending(g => new { g.Annee, g.Mois })
                .Take(6)
                .ToList();

            return new PerformanceTechnicienDto
            {
                TauxResolution = Math.Round(tauxResolution, 2),
                InterventionsTerminees = interventionsTerminees,
                InterventionsEnRetard = interventionsEnRetard,
                TempsMoyenIntervention = Math.Round(tempsMoyenResolution, 2),
                PerformanceParType = performanceParType,
                PerformanceMensuelle = performanceMensuelle
            };
        }
    }
}
