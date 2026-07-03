using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour le dashboard spécifique aux Gérants
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Gerant,Super-Admin")]
    public class GerantDashboardController : ControllerBase
    {
        private readonly GerantDashboardService _gerantDashboardService;
        private readonly ILogger<GerantDashboardController> _logger;

        public GerantDashboardController(
            GerantDashboardService gerantDashboardService,
            ILogger<GerantDashboardController> logger)
        {
            _gerantDashboardService = gerantDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard complet pour le gérant
        /// </summary>
        /// <returns>Dashboard du gérant avec toutes les statistiques</returns>
        [HttpGet]
        [ProducesResponseType(typeof(GerantDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GerantDashboardDto>> GetGerantDashboard()
        {
            try
            {
                // Récupérer l'ID de la société depuis les claims de l'utilisateur
                var idSocieteClaim = User.FindFirst("idSociete")?.Value;
                
                if (string.IsNullOrEmpty(idSocieteClaim) || !int.TryParse(idSocieteClaim, out var idSociete))
                {
                    // Pour les tests, utiliser la société 1 par défaut
                    idSociete = 1;
                    _logger.LogWarning("ID de société non trouvé dans les claims, utilisation de la société 1 par défaut");
                }

                _logger.LogInformation("Génération du dashboard Gérant pour la société {SocieteId}", idSociete);
                
                var dashboard = await _gerantDashboardService.GetDashboardDataAsync(idSociete);
                
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Gérant");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère le dashboard pour une société spécifique (pour les admins)
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <returns>Dashboard de la société spécifiée</returns>
        [HttpGet("societe/{idSociete}")]
        [Authorize(Roles = "Admin,Super-Admin")]
        [ProducesResponseType(typeof(GerantDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GerantDashboardDto>> GetSocieteDashboard(int idSociete)
        {
            try
            {
                _logger.LogInformation("Génération du dashboard Gérant pour la société {SocieteId}", idSociete);
                
                var dashboard = await _gerantDashboardService.GetDashboardDataAsync(idSociete);
                
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Gérant pour la société {SocieteId}", idSociete);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les statistiques de la société uniquement
        /// </summary>
        /// <returns>Statistiques de la société</returns>
        [HttpGet("statistiques")]
        [ProducesResponseType(typeof(SocieteStatistiquesDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SocieteStatistiquesDto>> GetSocieteStatistiques()
        {
            try
            {
                var idSocieteClaim = User.FindFirst("idSociete")?.Value;
                
                if (string.IsNullOrEmpty(idSocieteClaim) || !int.TryParse(idSocieteClaim, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                var statistiques = await _gerantDashboardService.GetSocieteStatistiquesAsync(idSociete);
                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de la société");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les alertes de la société uniquement
        /// </summary>
        /// <returns>Alertes de la société</returns>
        [HttpGet("alertes")]
        [ProducesResponseType(typeof(List<AlerteSocieteDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlerteSocieteDto>>> GetAlertesSociete()
        {
            try
            {
                var idSocieteClaim = User.FindFirst("idSociete")?.Value;
                
                if (string.IsNullOrEmpty(idSocieteClaim) || !int.TryParse(idSocieteClaim, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                var alertes = await _gerantDashboardService.GetAlertesSocieteAsync(idSociete);
                return Ok(alertes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des alertes de la société");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
