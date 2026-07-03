using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Technicien,Super-Admin")]
    public class TechnicienDashboardController : ControllerBase
    {
        private readonly TechnicienDashboardService _technicienDashboardService;
        private readonly ILogger<TechnicienDashboardController> _logger;

        public TechnicienDashboardController(
            TechnicienDashboardService technicienDashboardService,
            ILogger<TechnicienDashboardController> logger)
        {
            _technicienDashboardService = technicienDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard complet pour le technicien
        /// </summary>
        /// <returns>Dashboard du technicien avec toutes les statistiques d'intervention</returns>
        [HttpGet]
        [ProducesResponseType(typeof(TechnicienDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TechnicienDashboardDto>> GetTechnicienDashboard()
        {
            try
            {
                _logger.LogInformation("Génération du dashboard Technicien");
                
                var dashboard = await _technicienDashboardService.GetDashboardDataAsync();
                
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Accès non autorisé au dashboard Technicien");
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Technicien");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les statistiques du technicien
        /// </summary>
        /// <returns>Statistiques d'intervention du technicien</returns>
        [HttpGet("statistiques")]
        [ProducesResponseType(typeof(TechnicienStatistiquesDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TechnicienStatistiquesDto>> GetStatistiques()
        {
            try
            {
                var userId = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var idUser))
                {
                    return BadRequest("ID d'utilisateur non trouvé");
                }

                var statistiques = await _technicienDashboardService.GetTechnicienStatistiquesAsync(idUser);
                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du technicien");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les interventions en cours
        /// </summary>
        /// <returns>Liste des interventions en cours</returns>
        [HttpGet("interventions-en-cours")]
        [ProducesResponseType(typeof(List<InterventionEnCoursDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<InterventionEnCoursDto>>> GetInterventionsEnCours()
        {
            try
            {
                var userId = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var idUser))
                {
                    return BadRequest("ID d'utilisateur non trouvé");
                }

                var interventions = await _technicienDashboardService.GetInterventionsEnCoursAsync(idUser);
                return Ok(interventions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des interventions en cours");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les interventions récentes
        /// </summary>
        /// <returns>Liste des interventions récentes</returns>
        [HttpGet("interventions-recentes")]
        [ProducesResponseType(typeof(List<InterventionRecenteDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<InterventionRecenteDto>>> GetInterventionsRecentes()
        {
            try
            {
                var userId = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var idUser))
                {
                    return BadRequest("ID d'utilisateur non trouvé");
                }

                var interventions = await _technicienDashboardService.GetInterventionsRecentesAsync(idUser);
                return Ok(interventions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des interventions récentes");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les pannes signalées
        /// </summary>
        /// <returns>Liste des pannes signalées</returns>
        [HttpGet("pannes-signalees")]
        [ProducesResponseType(typeof(List<PanneSignaleeDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<PanneSignaleeDto>>> GetPannesSignalees()
        {
            try
            {
                var userId = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var idUser))
                {
                    return BadRequest("ID d'utilisateur non trouvé");
                }

                var pannes = await _technicienDashboardService.GetPannesSignaleesAsync(idUser);
                return Ok(pannes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des pannes signalées");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les alertes technicien
        /// </summary>
        /// <returns>Liste des alertes technicien</returns>
        [HttpGet("alertes-technicien")]
        [ProducesResponseType(typeof(List<AlerteTechnicienDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlerteTechnicienDto>>> GetAlertesTechnicien()
        {
            try
            {
                var userId = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var idUser))
                {
                    return BadRequest("ID d'utilisateur non trouvé");
                }

                var alertes = await _technicienDashboardService.GetAlertesTechnicienAsync(idUser);
                return Ok(alertes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des alertes technicien");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère la performance du technicien
        /// </summary>
        /// <returns>Performance du technicien</returns>
        [HttpGet("performance")]
        [ProducesResponseType(typeof(PerformanceTechnicienDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PerformanceTechnicienDto>> GetPerformance()
        {
            try
            {
                var userId = User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var idUser))
                {
                    return BadRequest("ID d'utilisateur non trouvé");
                }

                var performance = await _technicienDashboardService.GetPerformanceTechnicienAsync(idUser);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la performance du technicien");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
