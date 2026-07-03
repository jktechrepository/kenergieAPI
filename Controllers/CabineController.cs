using Kenergie.Models;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CabineController : ControllerBase
    {
        private readonly ICabineRepository _cabineRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public CabineController(
            ICabineRepository cabineRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _cabineRepository = cabineRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/Cabine
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cabine>>> GetCabines()
        {
            var cabines = await _cabineRepository.GetAllAsync();
            return Ok(cabines);
        }

        // GET: api/Cabine/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<Cabine>>> GetCabinesBySociete(int idSociete)
        {
            var cabines = await _cabineRepository.GetBySocieteAsync(idSociete);
            return Ok(cabines);
        }

        // GET: api/Cabine/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Cabine>> GetCabine(int id)
        {
            var cabine = await _cabineRepository.GetByIdAsync(id);
            if (cabine == null)
            {
                return NotFound(new { message = "Cabine non trouvée" });
            }
            return Ok(cabine);
        }

        // POST: api/Cabine
        [HttpPost]
        public async Task<ActionResult<Cabine>> CreateCabine(Cabine cabine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier si une cabine avec le même nom existe déjà pour cette société
            if (!string.IsNullOrWhiteSpace(cabine.Nom))
            {
                if (await _cabineRepository.ExistsByNomAsync(cabine.Nom, cabine.IdSociete))
                {
                    return Conflict(new { message = "Une cabine avec ce nom existe déjà pour cette société." });
                }
            }

            var created = await _cabineRepository.CreateAsync(cabine);

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création cabine");

            return CreatedAtAction(nameof(GetCabine), new { id = created.IdCabine }, created);
        }

        // PUT: api/Cabine/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Cabine>> UpdateCabine(int id, Cabine cabine)
        {
            if (id != cabine.IdCabine)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _cabineRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Cabine non trouvée" });
            }

            // Vérifier si une autre cabine avec le même nom existe déjà pour cette société
            if (!string.IsNullOrWhiteSpace(cabine.Nom) && cabine.Nom != existing.Nom)
            {
                if (await _cabineRepository.ExistsByNomAsync(cabine.Nom, cabine.IdSociete))
                {
                    return Conflict(new { message = "Une cabine avec ce nom existe déjà pour cette société." });
                }
            }

            // Snapshot avant modification
            var oldCabine = new Cabine
            {
                IdCabine = existing.IdCabine,
                Nom = existing.Nom,
                Adresse = existing.Adresse,
                IdSociete = existing.IdSociete
            };

            var updated = await _cabineRepository.UpdateAsync(cabine);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldCabine, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification cabine");

            return Ok(updated);
        }

        // DELETE: api/Cabine/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteCabine(int id)
        {
            var cabine = await _cabineRepository.GetByIdAsync(id);
            if (cabine == null)
            {
                return NotFound(new { message = "Cabine non trouvée" });
            }

            var deleted = await _cabineRepository.DeleteAsync(id);
            if (!deleted)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression de la cabine." });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(cabine, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Désactivation cabine (soft delete)");

            return Ok(new 
            { 
                message = "Cabine désactivée avec succès (soft delete)",
                idCabine = id,
                note = "La cabine a été désactivée. Les données sont conservées pour l'historique."
            });
        }
    }
}
