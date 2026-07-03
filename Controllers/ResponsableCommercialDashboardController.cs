using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.Extensions.Logging;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour le Dashboard du Responsable Commercial
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Responsable Commercial,Super-Admin")]
    public class ResponsableCommercialDashboardController : ControllerBase
    {
        private readonly ResponsableCommercialDashboardService _dashboardService;
        private readonly ILogger<ResponsableCommercialDashboardController> _logger;

        public ResponsableCommercialDashboardController(
            ResponsableCommercialDashboardService dashboardService,
            ILogger<ResponsableCommercialDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard complet du Responsable Commercial
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel, utilise la société de l'utilisateur si non fourni)</param>
        /// <returns>Dashboard complet avec statistiques financières et commerciales</returns>
        [HttpGet]
        public async Task<ActionResult<ResponsableCommercialDashboardDto>> GetDashboard([FromQuery] int? idSociete = null)
        {
            try
            {
                _logger.LogInformation("Demande de dashboard Responsable Commercial - Société: {IdSociete}", idSociete);

                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);

                _logger.LogInformation("Dashboard Responsable Commercial généré avec succès");
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Responsable Commercial");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération du dashboard", details = ex.Message });
            }
        }

        /// <summary>
        /// Récupère uniquement les statistiques financières globales
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Statistiques financières globales</returns>
        [HttpGet("financial-stats")]
        public async Task<ActionResult<GlobalFinancierStatistiquesDto>> GetFinancialStats([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.GlobalStatistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques financières");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des statistiques financières" });
            }
        }

        /// <summary>
        /// Récupère uniquement les statistiques commerciales
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Statistiques commerciales</returns>
        [HttpGet("commercial-stats")]
        public async Task<ActionResult<CommercialStatsDto>> GetCommercialStats([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.CommercialStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques commerciales");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des statistiques commerciales" });
            }
        }

        /// <summary>
        /// Récupère la performance des agents Direction Commercial
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Liste des performances des agents</returns>
        [HttpGet("agents-performance")]
        public async Task<ActionResult<List<AgentPerformanceDto>>> GetAgentsPerformance([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.AgentsPerformance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la performance des agents");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération de la performance des agents" });
            }
        }

        /// <summary>
        /// Récupère les acquisitions récentes de nouveaux clients
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Liste des acquisitions récentes</returns>
        [HttpGet("client-acquisitions")]
        public async Task<ActionResult<List<ClientAcquisitionDto>>> GetClientAcquisitions([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.ClientAcquisitions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des acquisitions de clients");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des acquisitions de clients" });
            }
        }

        /// <summary>
        /// Récupère les prospects et opportunités commerciales
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Liste des prospects</returns>
        [HttpGet("prospects")]
        public async Task<ActionResult<List<ProspectDto>>> GetProspects([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.Prospects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des prospects");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des prospects" });
            }
        }

        /// <summary>
        /// Récupère les tendances commerciales sur 12 mois
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Tendances commerciales</returns>
        [HttpGet("trends")]
        public async Task<ActionResult<TendancesCommercialesDto>> GetTrends([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.TendancesCommerciales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tendances commerciales");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des tendances commerciales" });
            }
        }
    }
}
