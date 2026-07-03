using Kenergie.Models;
using Kenergie.Models.DTOs;
using KenergieAPI.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KenergieAPI.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion des appareils utilisateurs (FCM tokens)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Appareils utilisateurs - Token JWT requis
    public class UserDeviceController : ControllerBase
    {
        private readonly IUserDeviceRepository _userDeviceRepository;
        private readonly ILogger<UserDeviceController> _logger;

        public UserDeviceController(
            IUserDeviceRepository userDeviceRepository,
            ILogger<UserDeviceController> logger)
        {
            _userDeviceRepository = userDeviceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Récupère tous les appareils
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDevice>>> GetAll()
        {
            try
            {
                var devices = await _userDeviceRepository.GetAllAsync();
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des appareils");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère un appareil par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDevice>> GetById(int id)
        {
            try
            {
                var device = await _userDeviceRepository.GetByIdAsync(id);
                if (device == null)
                {
                    return NotFound(new { message = "Appareil non trouvé" });
                }
                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de l'appareil {id}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les appareils d'un utilisateur
        /// </summary>
        [HttpGet("utilisateur/{idUtilisateur}")]
        public async Task<ActionResult<IEnumerable<UserDevice>>> GetByUtilisateur(int idUtilisateur)
        {
            try
            {
                var devices = await _userDeviceRepository.GetByUtilisateurIdAsync(idUtilisateur);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération des appareils de l'utilisateur {idUtilisateur}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère un appareil par son token FCM
        /// </summary>
        [HttpGet("token/{fcmToken}")]
        public async Task<ActionResult<UserDevice>> GetByFcmToken(string fcmToken)
        {
            try
            {
                var device = await _userDeviceRepository.GetByFcmTokenAsync(fcmToken);
                if (device == null)
                {
                    return NotFound(new { message = "Appareil non trouvé" });
                }
                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de l'appareil avec le token {fcmToken}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Crée un nouvel appareil
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDevice>> Create([FromBody] UserDevice userDevice)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdDevice = await _userDeviceRepository.CreateAsync(userDevice);
                return CreatedAtAction(nameof(GetById), new { id = createdDevice.IdUserDevice }, createdDevice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'appareil");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Enregistre ou met à jour un appareil (endpoint spécial pour l'enregistrement)
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<UserDevice>> RegisterDevice([FromBody] RegisterDeviceRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var device = await _userDeviceRepository.CreateOrUpdateAsync(
                    request.IdUtilisateur, 
                    request.FcmToken, 
                    request.DeviceType, 
                    request.DeviceModel, 
                    request.OsVersion);

                return Ok(new { message = "Appareil enregistré avec succès", device = device });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement de l'appareil");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Met à jour un appareil
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDevice>> Update(int id, [FromBody] UpdateUserDeviceDto dto)
        {
            try
            {
                if (id != dto.IdUserDevice)
                {
                    return BadRequest(new { message = "L'ID de l'appareil ne correspond pas" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existing = await _userDeviceRepository.GetByIdAsync(id);
                if (existing == null)
                {
                    return NotFound(new { message = "Appareil non trouvé" });
                }

                existing.FcmToken = dto.FcmToken;
                existing.DeviceType = dto.DeviceType;
                existing.DeviceModel = dto.DeviceModel;
                existing.OsVersion = dto.OsVersion;
                existing.Statut = dto.Statut;

                var updatedDevice = await _userDeviceRepository.UpdateAsync(existing);
                if (updatedDevice == null)
                {
                    return NotFound(new { message = "Appareil non trouvé" });
                }

                return Ok(updatedDevice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la mise à jour de l'appareil {id}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Supprime un appareil
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _userDeviceRepository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Appareil non trouvé" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de l'appareil {id}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Supprime un appareil par son token FCM
        /// </summary>
        [HttpDelete("token/{fcmToken}")]
        public async Task<IActionResult> DeleteByFcmToken(string fcmToken)
        {
            try
            {
                var success = await _userDeviceRepository.DeleteByFcmTokenAsync(fcmToken);
                if (!success)
                {
                    return NotFound(new { message = "Appareil non trouvé" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la suppression de l'appareil avec le token {fcmToken}");
                return StatusCode(500, new { message = "Erreur interne du serveur", error = ex.Message });
            }
        }
    }
}
