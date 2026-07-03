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
    public class AxeController : ControllerBase
    {
        private readonly IAxeRepository _axeRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public AxeController(
            IAxeRepository axeRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _axeRepository = axeRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/Axe
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Axe>>> GetAxes()
        {
            var axes = await _axeRepository.GetAllAsync();
            return Ok(axes);
        }

        // GET: api/Axe/cabine/{idCabine}
        [HttpGet("cabine/{idCabine}")]
        public async Task<ActionResult<IEnumerable<Axe>>> GetAxesByCabine(int idCabine)
        {
            var axes = await _axeRepository.GetByCabineAsync(idCabine);
            return Ok(axes);
        }

        // GET: api/Axe/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<Axe>>> GetAxesBySociete(int idSociete)
        {
            var axes = await _axeRepository.GetBySocieteAsync(idSociete);
            return Ok(axes);
        }

        // GET: api/Axe/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Axe>> GetAxe(int id)
        {
            var axe = await _axeRepository.GetByIdAsync(id);
            if (axe == null)
            {
                return NotFound(new { message = "Axe non trouvé" });
            }
            return Ok(axe);
        }

        // POST: api/Axe
        [HttpPost]
        public async Task<ActionResult<Axe>> CreateAxe(Axe axe)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier si un axe avec le même nom existe déjà pour cette cabine
            if (!string.IsNullOrWhiteSpace(axe.NomAxe))
            {
                if (await _axeRepository.ExistsByNomAsync(axe.NomAxe, axe.IdCabine))
                {
                    return Conflict(new { message = "Un axe avec ce nom existe déjà pour cette cabine." });
                }
            }

            var created = await _axeRepository.CreateAsync(axe);

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création axe");

            return CreatedAtAction(nameof(GetAxe), new { id = created.IdAxe }, created);
        }

        // PUT: api/Axe/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Axe>> UpdateAxe(int id, Axe axe)
        {
            if (id != axe.IdAxe)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _axeRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Axe non trouvé" });
            }

            // Vérifier si un autre axe avec le même nom existe déjà pour cette cabine
            if (!string.IsNullOrWhiteSpace(axe.NomAxe) && axe.NomAxe != existing.NomAxe)
            {
                if (await _axeRepository.ExistsByNomAsync(axe.NomAxe, axe.IdCabine))
                {
                    return Conflict(new { message = "Un axe avec ce nom existe déjà pour cette cabine." });
                }
            }

            // Snapshot avant modification
            var oldAxe = new Axe
            {
                IdAxe = existing.IdAxe,
                NomAxe = existing.NomAxe,
                Description = existing.Description,
                IdCabine = existing.IdCabine
            };

            var updated = await _axeRepository.UpdateAsync(axe);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldAxe, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification axe");

            return Ok(updated);
        }

        // DELETE: api/Axe/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteAxe(int id)
        {
            var axe = await _axeRepository.GetByIdAsync(id);
            if (axe == null)
            {
                return NotFound(new { message = "Axe non trouvé" });
            }

            var deleted = await _axeRepository.DeleteAsync(id);
            if (!deleted)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression de l'axe." });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(axe, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Désactivation axe (soft delete)");

            return Ok(new 
            { 
                message = "Axe désactivé avec succès (soft delete)",
                idAxe = id,
                note = "L'axe a été désactivé. Les données sont conservées pour l'historique."
            });
        }
    }
}
