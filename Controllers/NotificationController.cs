using Kenergie.Models;
using KenergieAPI.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KenergieAPI.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des notifications
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Notifications - Token JWT requis
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationRepository notificationRepository,
            ILogger<NotificationController> logger)
        {
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les notifications
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Notification>>> GetAll()
        {
            try
            {
                var notifications = await _notificationRepository.GetAllAsync();
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des notifications");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère une notification par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Notification>> GetById(int id)
        {
            try
            {
                var notification = await _notificationRepository.GetByIdAsync(id);
                if (notification == null)
                {
                    return NotFound(new { message = "Notification non trouvée" });
                }
                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de la notification {id}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les notifications d'un destinataire
        /// </summary>
        [HttpGet("destinataire/{idDestinataire}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetByDestinataire(int idDestinataire)
        {
            try
            {
                var notifications = await _notificationRepository.GetByDestinataireAsync(idDestinataire);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des notifications du destinataire {idDestinataire}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les notifications d'un expéditeur
        /// </summary>
        [HttpGet("expediteur/{idExpediteur}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetByExpediteur(int idExpediteur)
        {
            try
            {
                var notifications = await _notificationRepository.GetByExpediteurAsync(idExpediteur);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des notifications de l'expéditeur {idExpediteur}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les notifications d'une école
        /// </summary>
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetBySociete(int idSociete)
        {
            try
            {
                var notifications = await _notificationRepository.GetBySocieteAsync(idSociete);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des notifications de l'école {idSociete}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les notifications d'une classe
        /// </summary>
        [HttpGet("classe/{idClasse}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetByClasse(int idClasse)
        {
            try
            {
                var notifications = await _notificationRepository.GetByClasseAsync(idClasse);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des notifications de la classe {idClasse}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les notifications par type
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetByType(string type)
        {
            try
            {
                var notifications = await _notificationRepository.GetByTypeAsync(type);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des notifications de type {type}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les notifications non lues d'un destinataire
        /// </summary>
        [HttpGet("destinataire/{idDestinataire}/non-lues")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNonLues(int idDestinataire)
        {
            try
            {
                var notifications = await _notificationRepository.GetNonLuesAsync(idDestinataire);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des notifications non lues du destinataire {idDestinataire}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Crée une nouvelle notification
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Notification>> Create([FromBody] Notification notification)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdNotification = await _notificationRepository.CreateAsync(notification);
                return CreatedAtAction(nameof(GetById), new { id = createdNotification.IdNotification }, createdNotification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la notification");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Marque une notification comme lue
        /// </summary>
        [HttpPut("{id}/marquer-lue")]
        public async Task<IActionResult> MarquerCommeLue(int id)
        {
            try
            {
                var success = await _notificationRepository.MarquerCommeLueAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Notification non trouvée" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du marquage de la notification {id} comme lue");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Marque toutes les notifications d'un destinataire comme lues
        /// </summary>
        [HttpPut("destinataire/{idDestinataire}/marquer-toutes-lues")]
        public async Task<IActionResult> MarquerToutesCommeLues(int idDestinataire)
        {
            try
            {
                var success = await _notificationRepository.MarquerToutesCommeLuesAsync(idDestinataire);
                if (!success)
                {
                    return NotFound(new { message = "Aucune notification trouvée" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du marquage de toutes les notifications du destinataire {idDestinataire} comme lues");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Supprime une notification (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(int id)
        {
            try
            {
                var success = await _notificationRepository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Notification non trouvée" });
                }
                return Ok(new 
                { 
                    message = "Notification désactivée avec succès (soft delete)",
                    idNotification = id,
                    note = "La notification a été désactivée. Les données sont conservées pour l'historique."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de la notification {id}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }
    }
}