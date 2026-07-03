using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.Extensions.Logging;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour le Dashboard Agent Direction Commercial
    /// Vue simplifiée adaptée aux agents de terrain
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Agent Direction Commercial,Responsable Commercial,Super-Admin")]
    public class AgentDirectionCommercialDashboardController : ControllerBase
    {
        private readonly AgentDirectionCommercialDashboardService _dashboardService;
        private readonly ILogger<AgentDirectionCommercialDashboardController> _logger;

        public AgentDirectionCommercialDashboardController(
            AgentDirectionCommercialDashboardService dashboardService,
            ILogger<AgentDirectionCommercialDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard complet de l'Agent Direction Commercial
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel, utilise la société de l'utilisateur si non fourni)</param>
        /// <returns>Dashboard complet avec métriques personnelles</returns>
        [HttpGet]
        public async Task<ActionResult<AgentDirectionCommercialDashboardDto>> GetDashboard([FromQuery] int? idSociete = null)
        {
            try
            {
                _logger.LogInformation("Demande de dashboard Agent Direction Commercial - Société: {IdSociete}", idSociete);

                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);

                _logger.LogInformation("Dashboard Agent Direction Commercial généré avec succès");
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Agent Direction Commercial");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération du dashboard", details = ex.Message });
            }
        }

        /// <summary>
        /// Récupère uniquement les statistiques personnelles de l'agent
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Statistiques personnelles</returns>
        [HttpGet("agent-stats")]
        public async Task<ActionResult<AgentStatsDto>> GetAgentStats([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.AgentStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques de l'agent");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des statistiques de l'agent" });
            }
        }

        /// <summary>
        /// Récupère uniquement la performance personnelle
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Performance personnelle détaillée</returns>
        [HttpGet("performance")]
        public async Task<ActionResult<AgentPerformancePersonnelDto>> GetPerformance([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.Performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la performance de l'agent");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération de la performance de l'agent" });
            }
        }

        /// <summary>
        /// Récupère les clients gérés par l'agent
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Liste des clients gérés</returns>
        [HttpGet("clients")]
        public async Task<ActionResult<List<ClientAgentDto>>> GetClients([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.ClientsGeres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients gérés");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des clients gérés" });
            }
        }

        /// <summary>
        /// Récupère les prospects assignés à l'agent
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Liste des prospects</returns>
        [HttpGet("prospects")]
        public async Task<ActionResult<List<ProspectAgentDto>>> GetProspects([FromQuery] int? idSociete = null)
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
        /// Récupère les tâches du jour
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Liste des tâches du jour</returns>
        [HttpGet("tasks")]
        public async Task<ActionResult<List<TacheDto>>> GetTasks([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.TachesDuJour);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des tâches");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des tâches" });
            }
        }

        /// <summary>
        /// Récupère les objectifs du mois et progression
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Objectifs du mois</returns>
        [HttpGet("objectives")]
        public async Task<ActionResult<ObjectifsMoisDto>> GetObjectives([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.ObjectifsMois);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des objectifs");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des objectifs" });
            }
        }

        /// <summary>
        /// Récupère les activités récentes de l'agent
        /// </summary>
        /// <param name="idSociete">Identifiant de la société (optionnel)</param>
        /// <returns>Activités récentes</returns>
        [HttpGet("activities")]
        public async Task<ActionResult<List<ActiviteRecenteDto>>> GetActivities([FromQuery] int? idSociete = null)
        {
            try
            {
                var dashboard = await _dashboardService.GetDashboardAsync(idSociete ?? 0);
                return Ok(dashboard.ActivitesRecentes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des activités récentes");
                return StatusCode(500, new { message = "Une erreur est survenue lors de la récupération des activités récentes" });
            }
        }

        /// <summary>
        /// Marque une tâche comme complétée
        /// </summary>
        /// <param name="idTache">Identifiant de la tâche</param>
        /// <returns>Résultat de l'opération</returns>
        [HttpPost("tasks/{idTache}/complete")]
        public async Task<ActionResult> CompleteTask(int idTache)
        {
            try
            {
                // Simulation - à implémenter avec une vraie table de tâches
                _logger.LogInformation("Tâche {IdTache} marquée comme complétée", idTache);
                
                return Ok(new { message = "Tâche marquée comme complétée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la complétion de la tâche {IdTache}", idTache);
                return StatusCode(500, new { message = "Une erreur est survenue lors de la complétion de la tâche" });
            }
        }

        /// <summary>
        /// Met à jour le statut d'un prospect
        /// </summary>
        /// <param name="idProspect">Identifiant du prospect</param>
        /// <param name="statut">Nouveau statut du prospect</param>
        /// <returns>Résultat de l'opération</returns>
        [HttpPut("prospects/{idProspect}/status")]
        public async Task<ActionResult> UpdateProspectStatus(int idProspect, [FromBody] UpdateProspectStatusRequest request)
        {
            try
            {
                // Simulation - à implémenter avec une vraie table de prospects
                _logger.LogInformation("Mise à jour du statut du prospect {IdProspect}: {Statut}", idProspect, request.Statut);
                
                return Ok(new { message = "Statut du prospect mis à jour avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut du prospect {IdProspect}", idProspect);
                return StatusCode(500, new { message = "Une erreur est survenue lors de la mise à jour du statut du prospect" });
            }
        }

        /// <summary>
        /// Enregistre une activité pour l'agent
        /// </summary>
        /// <param name="request">Détails de l'activité</param>
        /// <returns>Résultat de l'opération</returns>
        [HttpPost("activities")]
        public async Task<ActionResult> LogActivity([FromBody] LogActivityRequest request)
        {
            try
            {
                // Simulation - à implémenter avec une vraie table d'activités
                _logger.LogInformation("Activité enregistrée: {TypeActivite} - {Description}", request.TypeActivite, request.Description);
                
                return Ok(new { message = "Activité enregistrée avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement de l'activité");
                return StatusCode(500, new { message = "Une erreur est survenue lors de l'enregistrement de l'activité" });
            }
        }
    }

    /// <summary>
    /// DTO pour la mise à jour du statut d'un prospect
    /// </summary>
    public class UpdateProspectStatusRequest
    {
        /// <summary>
        /// Nouveau statut du prospect
        /// </summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>
        /// Commentaires sur le changement de statut
        /// </summary>
        public string? Commentaires { get; set; }
    }

    /// <summary>
    /// DTO pour l'enregistrement d'une activité
    /// </summary>
    public class LogActivityRequest
    {
        /// <summary>
        /// Type d'activité
        /// </summary>
        public string TypeActivite { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'activité
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Entité concernée (client/prospect)
        /// </summary>
        public string? EntiteConcernee { get; set; }

        /// <summary>
        /// Montant concerné (si applicable)
        /// </summary>
        public decimal? MontantConcerne { get; set; }

        /// <summary>
        /// Commentaires supplémentaires
        /// </summary>
        public string? Commentaires { get; set; }
    }
}
