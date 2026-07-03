using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Financier,Super-Admin")]
    public class FinancierDashboardController : ControllerBase
    {
        private readonly FinancierDashboardService _financierDashboardService;
        private readonly ILogger<FinancierDashboardController> _logger;

        public FinancierDashboardController(
            FinancierDashboardService financierDashboardService,
            ILogger<FinancierDashboardController> logger)
        {
            _financierDashboardService = financierDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard complet pour le financier
        /// </summary>
        /// <returns>Dashboard du financier avec toutes les statistiques financières</returns>
        [HttpGet]
        [ProducesResponseType(typeof(FinancierDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<FinancierDashboardDto>> GetFinancierDashboard()
        {
            try
            {
                _logger.LogInformation("Génération du dashboard Financier");
                
                var dashboard = await _financierDashboardService.GetDashboardDataAsync();
                
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Financier");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les statistiques financières globales
        /// </summary>
        /// <returns>Statistiques financières globales</returns>
        [HttpGet("statistiques-globales")]
        [ProducesResponseType(typeof(GlobalFinancierStatistiquesDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GlobalFinancierStatistiquesDto>> GetGlobalStatistiques()
        {
            try
            {
                var statistiques = await _financierDashboardService.GetGlobalFinancierStatistiquesAsync();
                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques globales");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les résumés financiers des sociétés
        /// </summary>
        /// <returns>Liste des résumés financiers par société</returns>
        [HttpGet("societes-financieres")]
        [ProducesResponseType(typeof(List<SocieteFinancierSummaryDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<SocieteFinancierSummaryDto>>> GetSocietesFinancieres()
        {
            try
            {
                var societes = await _financierDashboardService.GetSocietesFinancieresAsync();
                return Ok(societes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des sociétés financières");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les transactions récentes
        /// </summary>
        /// <returns>Liste des transactions récentes</returns>
        [HttpGet("transactions-recentes")]
        [ProducesResponseType(typeof(List<TransactionRecenteDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<TransactionRecenteDto>>> GetTransactionsRecentes()
        {
            try
            {
                var transactions = await _financierDashboardService.GetTransactionsRecentesAsync();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des transactions récentes");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les alertes financières
        /// </summary>
        /// <returns>Liste des alertes financières</returns>
        [HttpGet("alertes-financieres")]
        [ProducesResponseType(typeof(List<AlerteFinanciereDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlerteFinanciereDto>>> GetAlertesFinancieres()
        {
            try
            {
                var alertes = await _financierDashboardService.GetAlertesFinancieresAsync();
                return Ok(alertes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des alertes financières");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les tendances financières
        /// </summary>
        /// <returns>Tendances financières sur 12 mois</returns>
        [HttpGet("tendances-financieres")]
        [ProducesResponseType(typeof(TendancesFinancieresDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<TendancesFinancieresDto>> GetTendancesFinancieres()
        {
            try
            {
                var tendances = await _financierDashboardService.GetTendancesFinancieresAsync();
                return Ok(tendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tendances financières");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }
    }
}
