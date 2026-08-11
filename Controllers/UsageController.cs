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
    public class UsageController : ControllerBase
    {
        private readonly IUsageRepository _usageRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public UsageController(
            IUsageRepository usageRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _usageRepository = usageRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/Usage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usage>>> GetUsages()
        {
            var usages = await _usageRepository.GetAllAsync();
            return Ok(usages);
        }

        // GET: api/Usage/categorie/{idCategorieClient}
        [HttpGet("categorie/{idCategorieClient}")]
        public async Task<ActionResult<IEnumerable<Usage>>> GetUsagesByCategorie(int idCategorieClient)
        {
            var usages = await _usageRepository.GetByCategorieClientAsync(idCategorieClient);
            return Ok(usages);
        }

        // GET: api/Usage/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<Usage>>> GetUsagesBySociete(int idSociete)
        {
            var usages = await _usageRepository.GetBySocieteAsync(idSociete);
            return Ok(usages);
        }

        // GET: api/Usage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Usage>> GetUsage(int id)
        {
            var usage = await _usageRepository.GetByIdAsync(id);
            if (usage == null)
            {
                return NotFound(new { message = "Usage non trouvé" });
            }
            return Ok(usage);
        }

        // POST: api/Usage
        [HttpPost]
        public async Task<ActionResult<Usage>> CreateUsage(Usage usage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier si un usage avec le même libellé existe déjà pour cette catégorie
            if (!string.IsNullOrWhiteSpace(usage.Libelle))
            {
                if (await _usageRepository.ExistsByLibelleAsync(usage.Libelle, usage.IdCategorieClient))
                {
                    return Conflict(new { message = "Un usage avec ce libellé existe déjà pour cette catégorie." });
                }
            }

            var created = await _usageRepository.CreateAsync(usage);

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création usage");

            return CreatedAtAction(nameof(GetUsage), new { id = created.IdUsage }, created);
        }

        // PUT: api/Usage/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Usage>> UpdateUsage(int id, Usage usage)
        {
            if (id != usage.IdUsage)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _usageRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Usage non trouvé" });
            }

            // Vérifier si un autre usage avec le même libellé existe déjà pour cette catégorie
            if (!string.IsNullOrWhiteSpace(usage.Libelle) && usage.Libelle != existing.Libelle)
            {
                if (await _usageRepository.ExistsByLibelleAsync(usage.Libelle, usage.IdCategorieClient))
                {
                    return Conflict(new { message = "Un usage avec ce libellé existe déjà pour cette catégorie." });
                }
            }

            // Snapshot avant modification
            var oldUsage = new Usage
            {
                IdUsage = existing.IdUsage,
                Libelle = existing.Libelle,
                Description = existing.Description,
                IdCategorieClient = existing.IdCategorieClient
            };

            var updated = await _usageRepository.UpdateAsync(usage);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldUsage, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification usage");

            return Ok(updated);
        }

        // DELETE: api/Usage/5
        [HttpDelete("{id}")]
        [Permission("Usage.Delete")]
        public async Task<ActionResult<object>> DeleteUsage(int id)
        {
            var usage = await _usageRepository.GetByIdAsync(id);
            if (usage == null)
            {
                return NotFound(new { message = "Usage non trouvé" });
            }

            try
            {
                var deleted = await _usageRepository.DeleteAsync(id);
                if (!deleted)
                {
                    return StatusCode(500, new { message = "Erreur lors de la suppression de l'usage." });
                }

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogDeleteAsync(usage, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Désactivation usage (soft delete)");

                return Ok(new 
                { 
                    message = "Usage désactivé avec succès (soft delete)",
                    idUsage = id,
                    note = "L'usage a été désactivé. Les données sont conservées pour l'historique."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
