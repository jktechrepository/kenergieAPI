using Kenergie.Models;
using KenergieAPI.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KenergieAPI.Controllers
{
    /// <summary>
    /// Contrôleur pour l'envoi de notifications push via Firebase
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Notifications Push - Token JWT requis
    public class NotificationPushController : ControllerBase
    {
        private readonly IFirebaseNotificationService _firebaseService;
        private readonly ILogger<NotificationPushController> _logger;

        public NotificationPushController(
            IFirebaseNotificationService firebaseService,
            ILogger<NotificationPushController> logger)
        {
            _firebaseService = firebaseService;
            _logger = logger;
        }

        /// <summary>
        /// Envoie une notification push à un utilisateur spécifique
        /// </summary>
        [HttpPost("utilisateur/{idUtilisateur}")]
        public async Task<ActionResult> EnvoyerAUtilisateur(
            int idUtilisateur,
            [FromBody] NotificationPushRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Envoi de notification push à l'utilisateur {idUtilisateur}");

                var success = await _firebaseService.EnvoyerNotificationAUtilisateurAsync(
                    idUtilisateur, request.Titre, request.Corps, request.Donnees);

                if (success)
                {
                    return Ok(new { message = "Notification envoyée avec succès", success = true });
                }
                else
                {
                    return BadRequest(new { message = "Échec de l'envoi de la notification", success = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification à l'utilisateur {idUtilisateur}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie une notification push à tous les utilisateurs d'un rôle
        /// </summary>
        [HttpPost("role/{idRole}")]
        public async Task<ActionResult> EnvoyerParRole(
            int idRole,
            [FromBody] NotificationPushRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Envoi de notification push au rôle {idRole}");

                var count = await _firebaseService.EnvoyerNotificationParRoleAsync(
                    idRole, request.Titre, request.Corps, request.Donnees);

                return Ok(new { 
                    message = $"Notification envoyée à {count} utilisateurs", 
                    success = true, 
                    count = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification au rôle {idRole}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie une notification push à tous les utilisateurs d'une école
        /// </summary>
        [HttpPost("societe/{idSociete}")]
        public async Task<ActionResult> EnvoyerParSociete(
            int idSociete,
            [FromBody] NotificationPushRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Envoi de notification push à l'école {idSociete}");

                var count = await _firebaseService.EnvoyerNotificationParSocieteAsync(
                    idSociete, request.Titre, request.Corps, request.Donnees);

                return Ok(new { 
                    message = $"Notification envoyée à {count} utilisateurs de l'école", 
                    success = true, 
                    count = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification à l'école {idSociete}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie une notification push à tous les utilisateurs d'une classe
        /// </summary>
        [HttpPost("classe/{idClasse}")]
        public async Task<ActionResult> EnvoyerParClasse(
            int idClasse,
            [FromBody] NotificationPushRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Envoi de notification push à la classe {idClasse}");

                var count = await _firebaseService.EnvoyerNotificationParClasseAsync(
                    idClasse, request.Titre, request.Corps, request.Donnees);

                return Ok(new { 
                    message = $"Notification envoyée à {count} utilisateurs de la classe", 
                    success = true, 
                    count = count 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification à la classe {idClasse}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Envoie une notification push à un token FCM spécifique
        /// </summary>
        [HttpPost("token")]
        public async Task<ActionResult> EnvoyerAToken(
            [FromBody] NotificationAvanceeRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Envoi de notification push au token {request.FcmToken}");

                var success = await _firebaseService.EnvoyerNotificationAvanceeAsync(
                    request.FcmToken, 
                    request.Titre, 
                    request.Corps, 
                    request.ImageUrl, 
                    request.ClickAction, 
                    request.Donnees, 
                    request.Sound, 
                    request.Badge);

                if (success)
                {
                    return Ok(new { message = "Notification envoyée avec succès", success = true });
                }
                else
                {
                    return BadRequest(new { message = "Échec de l'envoi de la notification", success = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de l'envoi de notification au token {request.FcmToken}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }
    }
}
