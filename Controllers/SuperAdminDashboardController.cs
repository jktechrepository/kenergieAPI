using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kenergie.Models.DTOs;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using KenergieAPI.Services.Repositories;
using Microsoft.Extensions.Logging;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour le dashboard Super-Admin
    /// Accès réservé au rôle Super-Admin uniquement
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Super-Admin")]
    public class SuperAdminDashboardController : ControllerBase
    {
        private readonly SuperAdminDashboardService _superAdminDashboardService;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ISignalRStatistiquesService _signalRStatistiquesService;
        private readonly ILogger<SuperAdminDashboardController> _logger;

        public SuperAdminDashboardController(
            SuperAdminDashboardService superAdminDashboardService,
            ISignalRNotificationService signalRNotificationService,
            ISignalRStatistiquesService signalRStatistiquesService,
            ILogger<SuperAdminDashboardController> logger)
        {
            _superAdminDashboardService = superAdminDashboardService;
            _signalRNotificationService = signalRNotificationService;
            _signalRStatistiquesService = signalRStatistiquesService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard Super-Admin complet
        /// Vue globale multi-sociétés avec statistiques, tendances et alertes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<SuperAdminDashboardDto>> GetSuperAdminDashboard()
        {
            try
            {
                _logger.LogInformation("📊 Demande du dashboard Super-Admin");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();

                _logger.LogInformation("✅ Dashboard Super-Admin récupéré avec succès");
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération du dashboard Super-Admin");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les statistiques globales uniquement
        /// </summary>
        [HttpGet("global-statistiques")]
        public async Task<ActionResult<GlobalStatistiquesDto>> GetGlobalStatistiques()
        {
            try
            {
                _logger.LogInformation("📊 Demande des statistiques globales");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();
                
                _logger.LogInformation("✅ Statistiques globales récupérées avec succès");
                return Ok(dashboard.GlobalStatistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des statistiques globales");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère la liste des sociétés avec leurs statistiques
        /// </summary>
        [HttpGet("societes")]
        public async Task<ActionResult<List<SocieteSummaryDto>>> GetSocietesSummaries()
        {
            try
            {
                _logger.LogInformation("📊 Demande des résumés des sociétés");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();
                
                _logger.LogInformation("✅ Résumés des sociétés récupérés avec succès");
                return Ok(dashboard.Societes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des résumés des sociétés");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les alertes critiques uniquement
        /// </summary>
        [HttpGet("alertes-critiques")]
        public async Task<ActionResult<List<AlerteCritiqueDto>>> GetAlertesCritiques()
        {
            try
            {
                _logger.LogInformation("📊 Demande des alertes critiques");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();
                
                _logger.LogInformation($"✅ {dashboard.AlertesCritiques.Count} alertes critiques récupérées");
                return Ok(dashboard.AlertesCritiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des alertes critiques");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les tendances sur les 12 derniers mois
        /// </summary>
        [HttpGet("tendances")]
        public async Task<ActionResult<TendancesDto>> GetTendances()
        {
            try
            {
                _logger.LogInformation("📊 Demande des tendances");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();
                
                _logger.LogInformation("✅ Tendances récupérées avec succès");
                return Ok(dashboard.Tendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des tendances");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les statistiques des utilisateurs par rôle
        /// </summary>
        [HttpGet("utilisateurs-statistiques")]
        public async Task<ActionResult<UtilisateursStatistiquesDto>> GetUtilisateursStatistiques()
        {
            try
            {
                _logger.LogInformation("📊 Demande des statistiques des utilisateurs");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();
                
                _logger.LogInformation("✅ Statistiques des utilisateurs récupérées avec succès");
                return Ok(dashboard.UtilisateursStatistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des statistiques des utilisateurs");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Force le rafraîchissement du dashboard et notifie les clients Super-Admin
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult> RefreshDashboard()
        {
            try
            {
                _logger.LogInformation("🔄 Rafraîchissement forcé du dashboard Super-Admin");

                // Récupérer les données fraîches
                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();

                // Notifier tous les Super-Admin connectés via SignalR
                await _signalRNotificationService.NotifySuperAdminDashboardUpdatedAsync(dashboard);

                _logger.LogInformation("✅ Dashboard Super-Admin rafraîchi et notifié avec succès");
                return Ok(new { message = "Dashboard rafraîchi avec succès", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors du rafraîchissement du dashboard");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les détails d'une société spécifique
        /// </summary>
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<SocieteSummaryDto>> GetSocieteDetails(int idSociete)
        {
            try
            {
                _logger.LogInformation($"📊 Demande des détails de la société {idSociete}");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();
                var societe = dashboard.Societes.FirstOrDefault(s => s.IdSociete == idSociete);

                if (societe == null)
                {
                    _logger.LogWarning($"⚠️ Société {idSociete} non trouvée");
                    return NotFound(new { message = "Société non trouvée" });
                }

                _logger.LogInformation($"✅ Détails de la société {idSociete} récupérés avec succès");
                return Ok(societe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors de la récupération des détails de la société {idSociete}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Marque une alerte comme lue
        /// </summary>
        [HttpPut("alertes/{idAlerte}/marquer-lue")]
        public async Task<ActionResult> MarquerAlerteLue(int idAlerte)
        {
            try
            {
                _logger.LogInformation($"📊 Marquage de l'alerte {idAlerte} comme lue");

                // TODO: Implémenter la persistance des alertes en base de données
                // Pour l'instant, on simule le succès

                _logger.LogInformation($"✅ Alerte {idAlerte} marquée comme lue");
                return Ok(new { message = "Alerte marquée comme lue", idAlerte = idAlerte });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur lors du marquage de l'alerte {idAlerte} comme lue");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Exporte les données du dashboard en format JSON
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult> ExportDashboard()
        {
            try
            {
                _logger.LogInformation("📊 Export du dashboard Super-Admin");

                var dashboard = await _superAdminDashboardService.GetDashboardDataAsync();

                var fileName = $"super-admin-dashboard-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                var contentType = "application/json";

                _logger.LogInformation($"✅ Dashboard exporté avec succès: {fileName}");
                return File(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(dashboard), contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'export du dashboard");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }
    }
}
