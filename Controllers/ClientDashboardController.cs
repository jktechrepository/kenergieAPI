using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Client,Super-Admin")]
    public class ClientDashboardController : ControllerBase
    {
        private readonly ClientDashboardService _clientDashboardService;

        public ClientDashboardController(ClientDashboardService clientDashboardService)
        {
            _clientDashboardService = clientDashboardService;
        }

        [HttpGet]
        public async Task<ActionResult<ClientDashboardDto>> GetClientDashboard()
        {
            try
            {
                var dashboard = await _clientDashboardService.GetDashboardDataAsync();
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        [HttpGet("statistiques")]
        public async Task<ActionResult<ClientStatistiquesDto>> GetStatistiques()
        {
            try
            {
                var statistiques = await _clientDashboardService.GetClientStatistiquesAsync();
                return Ok(statistiques);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        [HttpGet("factures-recentes")]
        public async Task<ActionResult<List<FactureRecenteDto>>> GetFacturesRecentes()
        {
            try
            {
                var factures = await _clientDashboardService.GetFacturesRecentesAsync();
                return Ok(factures);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        [HttpGet("paiements-recents")]
        public async Task<ActionResult<List<PaiementClientRecentDto>>> GetPaiementsRecents()
        {
            try
            {
                var paiements = await _clientDashboardService.GetPaiementsRecentsAsync();
                return Ok(paiements);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        [HttpGet("consommations")]
        public async Task<ActionResult<List<ConsommationDto>>> GetConsommations()
        {
            try
            {
                var consommations = await _clientDashboardService.GetConsommationsAsync();
                return Ok(consommations);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        [HttpGet("alertes-client")]
        public async Task<ActionResult<List<AlerteClientDto>>> GetAlertesClient()
        {
            try
            {
                var alertes = await _clientDashboardService.GetAlertesClientAsync();
                return Ok(alertes);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        [HttpGet("resume-client")]
        public async Task<ActionResult<ResumeClientDto>> GetResumeClient()
        {
            try
            {
                var resume = await _clientDashboardService.GetResumeClientAsync();
                return Ok(resume);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
