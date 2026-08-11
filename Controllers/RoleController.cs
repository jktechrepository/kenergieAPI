using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // 🔒 Gestion des rôles - Token JWT requis
    public class RoleController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICurrentUserService _currentUserService;

        public RoleController(IRoleRepository roleRepository, ICurrentUserService currentUserService)
        {
            _roleRepository = roleRepository;
            _currentUserService = currentUserService;
        }

        // GET: api/Role
        /// <summary>
        /// Récupère les rôles actifs visibles pour l'appelant
        /// (exclut Client ; pas de rôles de niveau supérieur au sien).
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetAllRoles()
        {
            var callerNiveau = await ResolveCallerNiveauAsync();
            var roles = await _roleRepository.GetVisibleForCallerAsync(callerNiveau);
            return Ok(roles);
        }

        // GET: api/Role/nomRole/{nomRole}
        /// <summary>
        /// Même filtre que GET /api/Role : basé sur le rôle JWT de l'appelant
        /// (le paramètre d'URL n'élargit pas les droits).
        /// </summary>
        [HttpGet("nomRole/{nomRole}")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles(string nomRole)
        {
            var callerNiveau = await ResolveCallerNiveauAsync();
            var roles = await _roleRepository.GetVisibleForCallerAsync(callerNiveau);
            return Ok(roles);
        }

        // GET: api/Role/5
        [HttpGet("{id}")] 
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }

        // GET: api/Role/nom/Admin
        [HttpGet("nom/{nom}")]
        public async Task<ActionResult<Role>> GetRoleByNom(string nom)
        {
            var role = await _roleRepository.GetByNomAsync(nom);
            if (role == null)
            {
                return NotFound();
            }
            return Ok(role);
        }

        // GET: api/Role/exists/5
        [HttpGet("exists/{id}")]
        public async Task<ActionResult<bool>> RoleExists(int id)
        {
            var exists = await _roleRepository.ExistsAsync(id);
            return Ok(exists);
        }

        // POST: api/Role
        [HttpPost]
        public async Task<ActionResult<Role>> CreateRole(Role role)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdRole = await _roleRepository.CreateAsync(role);
            return CreatedAtAction(nameof(GetRole), new { id = createdRole.IdRole }, createdRole);
        }

        // PUT: api/Role/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin")]
        public async Task<ActionResult<Role>> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
        {
            if (id != dto.IdRole)
            {
                return BadRequest(new { message = "L'ID ne correspond pas" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _roleRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Rôle non trouvé" });
            }

            existing.Nom = dto.Nom;
            existing.Description = dto.Description;
            existing.Niveau = dto.Niveau;

            var updated = await _roleRepository.UpdateAsync(existing);
            return Ok(updated);
        }

        // DELETE: api/Role/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var success = await _roleRepository.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        // PUT: api/Role/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _roleRepository.ToggleStatutAsync(id);
                if (!success)
                    return NotFound(new { message = "Rôle non trouvé" });

                var role = await _roleRepository.GetByIdAsync(id);
                return Ok(new { 
                    message = "Statut modifié avec succès",
                    nouveauStatut = role != null,
                    role = role
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur", error = ex.Message });
            }
        }

        /// <summary>
        /// Niveau hiérarchique du rôle primaire JWT. Défaut 999 si inconnu/null (vue restrictive).
        /// </summary>
        private async Task<int> ResolveCallerNiveauAsync()
        {
            var roleName = _currentUserService.PrimaryRole;
            if (string.IsNullOrWhiteSpace(roleName))
                roleName = _currentUserService.UserRole;

            if (string.IsNullOrWhiteSpace(roleName))
                return 999;

            // Préférer idRole si présent pour éviter les ambiguïtés de nom
            var idRoleClaim = HttpContext.User.FindFirst("idRole")?.Value
                ?? HttpContext.User.FindFirst("IdRole")?.Value;
            if (int.TryParse(idRoleClaim, out var idRole) && idRole > 0)
            {
                var byId = await _roleRepository.GetByIdAsync(idRole);
                if (byId != null)
                    return byId.Niveau ?? 999;
            }

            var role = await _roleRepository.GetByNomAsync(roleName);
            return role?.Niveau ?? 999;
        }
    }
}
