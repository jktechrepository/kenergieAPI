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
    public class CategorieClientController : ControllerBase
    {
        private readonly ICategorieClientRepository _categorieClientRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public CategorieClientController(
            ICategorieClientRepository categorieClientRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _categorieClientRepository = categorieClientRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/CategorieClient
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategorieClient>>> GetCategorieClients()
        {
            var categories = await _categorieClientRepository.GetAllAsync();
            return Ok(categories);
        }

        // GET: api/CategorieClient/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<CategorieClient>>> GetCategorieClientsBySociete(int idSociete)
        {
            var categories = await _categorieClientRepository.GetBySocieteAsync(idSociete);
            return Ok(categories);
        }

        // GET: api/CategorieClient/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CategorieClient>> GetCategorieClient(int id)
        {
            var categorie = await _categorieClientRepository.GetByIdAsync(id);
            if (categorie == null)
            {
                return NotFound();
            }
            return Ok(categorie);
        }

        // GET: api/CategorieClient/nom/{nom}/societe/{idSociete}
        [HttpGet("nom/{nom}/societe/{idSociete}")]
        public async Task<ActionResult<CategorieClient>> GetCategorieClientByNom(string nom, int idSociete)
        {
            var categorie = await _categorieClientRepository.GetByNomAsync(nom, idSociete);
            if (categorie == null)
            {
                return NotFound();
            }
            return Ok(categorie);
        }

        // POST: api/CategorieClient
        [HttpPost]
        public async Task<ActionResult<CategorieClient>> CreateCategorieClient(CategorieClient categorieClient)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier l'unicité du nom dans la société
            if (await _categorieClientRepository.ExistsByNomAsync(categorieClient.NomCategorie ?? "", categorieClient.IdSociete))
            {
                return Conflict(new { message = "Une catégorie avec ce nom existe déjà pour cette société." });
            }

            var created = await _categorieClientRepository.CreateAsync(categorieClient);
            
            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création catégorie client");

            return CreatedAtAction(nameof(GetCategorieClient), new { id = created.IdCategorie }, created);
        }

        // PUT: api/CategorieClient/5
        [HttpPut("{id}")]
        public async Task<ActionResult<CategorieClient>> UpdateCategorieClient(int id, CategorieClient categorieClient)
        {
            if (id != categorieClient.IdCategorie)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _categorieClientRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Snapshot avant modification
            var oldCategorie = new CategorieClient
            {
                IdCategorie = existing.IdCategorie,
                NomCategorie = existing.NomCategorie,
                Description = existing.Description,
                Statut = existing.Statut
            };

            var updated = await _categorieClientRepository.UpdateAsync(categorieClient);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldCategorie, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification catégorie client");

            return Ok(updated);
        }

        // DELETE: api/CategorieClient/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategorieClient(int id)
        {
            var exists = await _categorieClientRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound();
            }

            var entity = await _categorieClientRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            
            await _categorieClientRepository.DeleteAsync(id);
            
            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(entity, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Suppression catégorie client");

            return NoContent();
        }

        // PUT: api/CategorieClient/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _categorieClientRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Catégorie non trouvée" });
                }

                var categorie = await _categorieClientRepository.GetByIdAsync(id);
                var nouveauStatut = categorie?.Statut ?? false;

                return Ok(new
                {
                    message = "Statut modifié avec succès",
                    statut = nouveauStatut,
                    categorie = categorie
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la modification du statut: {ex.Message}" });
            }
        }

        // PUT: api/CategorieClient/set-statut/{id}
        [HttpPut("set-statut/{id}")]
        public async Task<ActionResult<object>> SetStatut(int id, [FromQuery] bool statut)
        {
            try
            {
                var success = await _categorieClientRepository.SetStatutAsync(id, statut);
                if (!success)
                {
                    return NotFound(new { message = "Catégorie non trouvée" });
                }

                var categorie = await _categorieClientRepository.GetByIdAsync(id);

                return Ok(new
                {
                    message = $"Statut défini à {statut}",
                    statut = statut,
                    categorie = categorie
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la définition du statut: {ex.Message}" });
            }
        }
    }
}

