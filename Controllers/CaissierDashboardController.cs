using Kenergie.Models.DTOs;
using Kenergie.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Caissier,Super-Admin")]
    public class CaissierDashboardController : ControllerBase
    {
        private readonly CaissierDashboardService _caissierDashboardService;
        private readonly ILogger<CaissierDashboardController> _logger;

        public CaissierDashboardController(
            CaissierDashboardService caissierDashboardService,
            ILogger<CaissierDashboardController> logger)
        {
            _caissierDashboardService = caissierDashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le dashboard complet pour le caissier
        /// </summary>
        /// <param name="idUtilisateur">ID utilisateur optionnel (par défaut: utilisateur connecté)</param>
        /// <returns>Dashboard du caissier avec toutes les statistiques de caisse</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CaissierDashboardDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CaissierDashboardDto>> GetCaissierDashboard(int? idUtilisateur = null)
        {
            try
            {
                // Récupérer l'utilisateur connecté
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                // Utiliser l'utilisateur spécifié ou l'utilisateur connecté par défaut
                var targetUserId = idUtilisateur ?? currentUserId;

                _logger.LogInformation("Génération du dashboard Caissier pour l'utilisateur {TargetUserId}", targetUserId);
                
                var dashboard = await _caissierDashboardService.GetDashboardDataAsync(targetUserId);
                
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Accès non autorisé au dashboard Caissier");
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard Caissier");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les statistiques journalières du caissier
        /// </summary>
        /// <returns>Statistiques journalières de caisse</returns>
        [HttpGet("statistiques-journalieres")]
        [ProducesResponseType(typeof(CaissierStatistiquesDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<CaissierStatistiquesDto>> GetStatistiquesJournalieres()
        {
            try
            {
                var societeId = User.FindFirst("idSociete")?.Value;
                if (string.IsNullOrEmpty(societeId) || !int.TryParse(societeId, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                var statistiques = await _caissierDashboardService.GetStatistiquesJournalieresAsync(idSociete);
                return Ok(statistiques);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques journalières");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les paiements en cours
        /// </summary>
        /// <param name="idUtilisateur">ID utilisateur optionnel (par défaut: utilisateur connecté)</param>
        /// <returns>Liste des paiements en cours</returns>
        [HttpGet("paiements-en-cours")]
        [ProducesResponseType(typeof(List<PaiementEnCoursDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<PaiementEnCoursDto>>> GetPaiementsEnCours(int? idUtilisateur = null)
        {
            try
            {
                // Récupérer l'utilisateur connecté et la société
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var societeId = User.FindFirst("idSociete")?.Value;
                if (string.IsNullOrEmpty(societeId) || !int.TryParse(societeId, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                // Utiliser l'utilisateur spécifié ou l'utilisateur connecté par défaut
                var targetUserId = idUtilisateur ?? currentUserId;

                var paiements = await _caissierDashboardService.GetPaiementsEnCoursAsync(idSociete, targetUserId);
                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements en cours");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les paiements récents
        /// </summary>
        /// <param name="idUtilisateur">ID utilisateur optionnel (par défaut: utilisateur connecté)</param>
        /// <returns>Liste des paiements récents</returns>
        [HttpGet("paiements-recents")]
        [ProducesResponseType(typeof(List<PaiementRecentDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<PaiementRecentDto>>> GetPaiementsRecents(int? idUtilisateur = null)
        {
            try
            {
                // Récupérer l'utilisateur connecté et la société
                var currentUserId = GetCurrentUserId();
                if (currentUserId == 0)
                {
                    return Unauthorized("Utilisateur non authentifié");
                }

                var societeId = User.FindFirst("idSociete")?.Value;
                if (string.IsNullOrEmpty(societeId) || !int.TryParse(societeId, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                // Utiliser l'utilisateur spécifié ou l'utilisateur connecté par défaut
                var targetUserId = idUtilisateur ?? currentUserId;

                var paiements = await _caissierDashboardService.GetPaiementsRecentsAsync(idSociete, targetUserId);
                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements récents");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les recettes journalières
        /// </summary>
        /// <returns>Liste des recettes journalières</returns>
        [HttpGet("recettes-journalieres")]
        [ProducesResponseType(typeof(List<RecetteJournaliereDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<RecetteJournaliereDto>>> GetRecettesJournalieres()
        {
            try
            {
                var societeId = User.FindFirst("idSociete")?.Value;
                if (string.IsNullOrEmpty(societeId) || !int.TryParse(societeId, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                var recettes = await _caissierDashboardService.GetRecettesJournalieresAsync(idSociete);
                return Ok(recettes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des recettes journalières");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère les alertes caissier
        /// </summary>
        /// <returns>Liste des alertes caissier</returns>
        [HttpGet("alertes-caissier")]
        [ProducesResponseType(typeof(List<AlerteCaissierDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<List<AlerteCaissierDto>>> GetAlertesCaissier()
        {
            try
            {
                var societeId = User.FindFirst("idSociete")?.Value;
                if (string.IsNullOrEmpty(societeId) || !int.TryParse(societeId, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                var alertes = await _caissierDashboardService.GetAlertesCaissierAsync(idSociete);
                return Ok(alertes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des alertes caissier");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère le résumé de caisse
        /// </summary>
        /// <returns>Résumé de caisse</returns>
        [HttpGet("resume-caisse")]
        [ProducesResponseType(typeof(ResumeCaisseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ResumeCaisseDto>> GetResumeCaisse()
        {
            try
            {
                var societeId = User.FindFirst("idSociete")?.Value;
                if (string.IsNullOrEmpty(societeId) || !int.TryParse(societeId, out var idSociete))
                {
                    return BadRequest("ID de société non trouvé");
                }

                var resume = await _caissierDashboardService.GetResumeCaisseAsync(idSociete);
                return Ok(resume);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du résumé de caisse");
                return StatusCode(500, "Erreur interne du serveur");
            }
        }

        /// <summary>
        /// Récupère l'ID de l'utilisateur connecté depuis les claims JWT
        /// </summary>
        /// <returns>ID de l'utilisateur connecté ou 0 si non trouvé</returns>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return 0;
            }
            return userId;
        }
    }
}
