using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Super-Admin,Gerant")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            DashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère les statistiques du dashboard pour une société spécifique
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <returns>Statistiques du dashboard pour la société</returns>
        [HttpGet("{idSociete}")]
        [ProducesResponseType(typeof(DashboardDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DashboardDto>> GetDashboardStats(int idSociete)
        {
            try
            {
                if (idSociete <= 0)
                {
                    return BadRequest("ID de société invalide");
                }

                _logger.LogInformation("Récupération des statistiques du dashboard pour la société {SocieteId}", idSociete);
                
                var dashboard = await _dashboardService.GetDashboardDataAsync(idSociete);
                
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du dashboard pour la société {SocieteId}", idSociete);
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
