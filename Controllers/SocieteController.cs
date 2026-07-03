using System.Linq;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.Enums;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Gestion des sociétés - Token JWT requis
    public class SocieteController : ControllerBase
    {
        private readonly ISocieteRepository _societeRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public SocieteController(
            ISocieteRepository societeRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _societeRepository = societeRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/Societe
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Societe>>> GetSocietes()
        {
            var societes = await _societeRepository.GetAllAsync();
            return Ok(societes);
        }

        // GET: api/Societe/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Societe>> GetSociete(int id)
        {
            var societe = await _societeRepository.GetByIdAsync(id);
            if (societe == null)
            {
                return NotFound();
            }
            return Ok(societe);
        }

        // GET: api/Societe/nom/{nom}
        [HttpGet("nom/{nom}")]
        public async Task<ActionResult<Societe>> GetSocieteByNom(string nom)
        {
            var societe = await _societeRepository.GetByNomAsync(nom);
            if (societe == null)
            {
                return NotFound();
            }
            return Ok(societe);
        }

        // GET: api/Societe/code/{code}
        [HttpGet("code/{code}")]
        //public async Task<ActionResult<Societe>> GetSocieteByCode(string code)
        //{
        //    var societe = await _societeRepository.GetByCodeAsync(code);
        //    if (societe == null)
        //    {
        //        return NotFound();
        //    }
        //    return Ok(societe);
        //}

        // GET: api/Societe/statut/{statut}
        [HttpGet("statut/{statut}")]
        //public async Task<ActionResult<IEnumerable<Societe>>> GetSocietesByStatut(bool statut)
        //{
        //    var societes = await _societeRepository.GetByStatutAsync(statut);
        //    return Ok(societes);
        //}

        // GET: api/Societe/5/utilisateurs
        [HttpGet("{id}/utilisateurs")]
        public async Task<ActionResult<IEnumerable<Utilisateur>>> GetSocieteUtilisateurs(int id)
        {
            var utilisateurs = await _societeRepository.GetUtilisateursAsync(id);
            return Ok(utilisateurs);
        }

        // GET: api/Societe/5/agents
        [HttpGet("{id}/agents")]
        public async Task<ActionResult<IEnumerable<Agent>>> GetSocieteAgents(int id)
        {
            var agents = await _societeRepository.GetAgentsAsync(id);
            return Ok(agents);
        }

        // GET: api/Societe/5/agents/caissiers
        [HttpGet("{id}/agents/caissiers")]
        public async Task<ActionResult<PagedResult<Agent>>> GetSocieteCaissiers(
            int id,
            [FromQuery] PagedRequest request)
        {
            var caissiers = await _societeRepository.GetAgentsByRoleAsync(id, "Caissier", request);
            return Ok(caissiers);
        }

        // POST: api/Societe
        [HttpPost]
        public async Task<ActionResult<object>> CreateSociete(Societe societe)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Societe createdSociete;
            try
            {
                createdSociete = await _societeRepository.CreateAsync(societe);
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par une autre société." });
            }
            
            // Récupérer les informations de l'utilisateur Admin créé automatiquement
            var adminUser = await _societeRepository.GetUtilisateursAsync(createdSociete.IdSociete);
            var admin = adminUser.FirstOrDefault(u => u.Role?.Nom == "Admin");
            
            var response = new
            {
                societe = createdSociete,
                adminUser = admin != null ? new
                {
                    email = admin.Email, // ✨ Maintenant : email de l'école (emailContact)
                    telephone = admin.Telephone,
                    motDePasse = "Admin", // Mot de passe par défaut
                    nomComplet = admin.NomComplet ?? "Administrateur",
                    message = "Email de bienvenue envoyé automatiquement à l'administrateur"
                } : null
            };
            
            return CreatedAtAction(nameof(GetSociete), new { id = createdSociete.IdSociete }, response);
        }

        // PUT: api/Societe/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Sous-Directeur")]
        [ProducesResponseType(typeof(Societe), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Societe>> UpdateSociete(int id, [FromBody] UpdateSocieteDto dto)
        {
            if (id != dto.IdSociete)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingSociete = await _societeRepository.GetByIdAsync(id);
            if (existingSociete == null)
            {
                return NotFound(new { message = "École non trouvée" });
            }

            if (!_currentUserService.IsSuperAdmin)
            {
                if (_currentUserService.SocieteId == 0)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Impossible de déterminer votre école. Veuillez-vous reconnecter." });
                }

                if (_currentUserService.SocieteId != existingSociete.IdSociete)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous ne pouvez modifier que votre propre école." });
                }

                if (!new[] { UserRoles.ADMIN, UserRoles.GERANT, UserRoles.SOUS_DIRECTEUR }.Contains(_currentUserService.UserRole))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Votre rôle ne permet pas de modifier les informations de l'école." });
                }
            }

            // 📸 AUDIT: Snapshot AVANT modification
            var oldSociete = new Societe
            {
                IdSociete = existingSociete.IdSociete,
                Nom = existingSociete.Nom,
                Description = existingSociete.Description,
                Devise = existingSociete.Devise,
                Type = existingSociete.Type,
                Telephone = existingSociete.Telephone,
                EmailContact = existingSociete.EmailContact
            };

            // Mettre à jour seulement les champs autorisés
            existingSociete.Nom = dto.Nom;
            existingSociete.Description = dto.Description;
            existingSociete.Devise = dto.Devise;
            existingSociete.Type = dto.Type;
            existingSociete.Logo = dto.Logo;
            existingSociete.SiteWeb = dto.SiteWeb;
            existingSociete.Telephone = dto.Telephone;
            existingSociete.EmailContact = dto.EmailContact;
            existingSociete.NomCompletResponsable = dto.NomCompletResponsable;
            existingSociete.GenreResponsable = dto.GenreResponsable;
            existingSociete.AdresseResidence = dto.AdresseResidence;

            Societe updatedSociete;
            try
            {
                updatedSociete = await _societeRepository.UpdateAsync(existingSociete);
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par une autre société." });
            }
            if (updatedSociete == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // 📝 AUDIT: Enregistrer
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldSociete, updatedSociete, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification école");

            return Ok(updatedSociete);
        }

        // DELETE: api/Societe/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSociete(int id)
        {
            var exists = await _societeRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound();
            }

            await _societeRepository.DeleteAsync(id);
            return NoContent();
        }

        // PUT: api/Societe/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _societeRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "École non trouvée" });
                }

                // Récupérer l'école après le toggle pour connaître le nouveau statut
                // Note: GetByIdAsync retourne null si l'école est désactivée à cause du filtre Statut
                var societeApresToggle = await _societeRepository.GetByIdAsync(id);
                var nouveauStatut = societeApresToggle != null;
                
                return Ok(new { 
                    message = "Statut modifié avec succès",
                    nouveauStatut = nouveauStatut,
                    statut = nouveauStatut
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors du changement de statut", error = ex.Message });
            }
        }
        
        // PUT: api/Societe/set-statut/{id}
        [HttpPut("set-statut/{id}")]
        public async Task<ActionResult<object>> SetStatut(int id, [FromQuery] bool statut)
        {
            try
            {
                var success = await _societeRepository.SetStatutAsync(id, statut);
                if (!success)
                {
                    return NotFound(new { message = "École non trouvée" });
                }

                var societe = await _societeRepository.GetByIdAsync(id);
                
                return Ok(new { 
                    message = $"Statut défini à {statut}",
                    statut = statut,
                    societe = societe
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la modification du statut", error = ex.Message });
            }
        }

        private static bool IsDuplicateEmail(DbUpdateException ex)
        {
            var mySqlEx = ex.InnerException as MySqlException
                          ?? ex.InnerException?.InnerException as MySqlException;

            if (mySqlEx != null)
            {
                if (mySqlEx.Number == 1062 || mySqlEx.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                    return true;
            }

            var message = ex.InnerException?.Message ?? ex.Message;
            return !string.IsNullOrEmpty(message)
                   && message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("email", StringComparison.OrdinalIgnoreCase);
        }

    }
}
