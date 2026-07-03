using Kenergie.Models.DTOs.Sync;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour la synchronisation offline
    /// Gère la synchronisation des clients, arriérés, suppressions et paiements
    /// </summary>
    [ApiController]
    [Route("api/sync")]
    [Authorize] // JWT avec tenant automatique
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(
            ISyncService syncService,
            ICurrentUserService currentUserService,
            ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Fournit les informations initiales pour démarrer la synchronisation
        /// </summary>
        [HttpGet("bootstrap")]
        [ProducesResponseType(typeof(SyncBootstrapDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SyncBootstrapDto>> GetBootstrap()
        {
            try
            {
               var societeId = _currentUserService.SocieteId;
               // int societeId = idSociete;
                _logger.LogInformation("Bootstrap de synchronisation - Société: {SocieteId}", societeId);

                var result = await _syncService.GetBootstrapAsync(societeId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du bootstrap de synchronisation");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupère les clients avec pagination cursor et delta sync
        /// </summary>
        [HttpGet("clients")]
        [ProducesResponseType(typeof(SyncPageDto<ClientSyncDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SyncPageDto<ClientSyncDto>>> GetClients([FromQuery] SyncRequestDto request)
        {
            try
            {
                var societeId = _currentUserService.SocieteId;
                _logger.LogInformation("Synchronisation des clients - Société: {SocieteId}, PageSize: {PageSize}", 
                    societeId, request.PageSize);

                var result = await _syncService.GetClientsAsync(societeId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation des clients");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupère les arriérés avec pagination cursor et delta sync
        /// </summary>
        [HttpGet("arrears")]
        [ProducesResponseType(typeof(SyncPageDto<ArrearSyncDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SyncPageDto<ArrearSyncDto>>> GetArrears([FromQuery] SyncArrearsRequestDto request)
        {
            try
            {
                var societeId = _currentUserService.SocieteId;
                _logger.LogInformation("Synchronisation des arriérés - Société: {SocieteId}, OnlyOutstanding: {OnlyOutstanding}", 
                    societeId, request.OnlyOutstanding);

                var result = await _syncService.GetArrearsAsync(societeId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation des arriérés");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Récupère les suppressions depuis la dernière synchronisation
        /// </summary>
        [HttpGet("deletions")]
        [ProducesResponseType(typeof(SyncDeletionsDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<SyncDeletionsDto>> GetDeletions([FromQuery] SyncDeletionsRequestDto request)
        {
            try
            {
                var societeId = _currentUserService.SocieteId;
                _logger.LogInformation("Récupération des suppressions - Société: {SocieteId}", societeId);

                var result = await _syncService.GetDeletionsAsync(societeId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des suppressions");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        /// <summary>
        /// Traite un batch de paiements offline avec idempotence
        /// </summary>
        [HttpPost("payments/batch")]
        [ProducesResponseType(typeof(PaymentBatchResultDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PaymentBatchResultDto>> ProcessPaymentsBatch([FromBody] PaymentBatchRequestDto request)
        {
            try
            {
                var societeId = _currentUserService.SocieteId;
                var userId = _currentUserService.UserId;  // Récupérer l'ID utilisateur connecté
                
                _logger.LogInformation("Traitement batch de paiements - Société: {SocieteId}, Utilisateur: {UserId}, Count: {Count}", 
                    societeId, userId, request.Items?.Count ?? 0);

                var result = await _syncService.ProcessPaymentsBatchAsync(societeId, userId, request);  // Passer l'ID utilisateur
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement batch de paiements");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}
