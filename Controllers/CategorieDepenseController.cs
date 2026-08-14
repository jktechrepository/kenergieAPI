using Kenergie.Attributes;
using Kenergie.Helpers;
using Kenergie.Models.DTOs.Depense;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategorieDepenseController : ControllerBase
    {
        private readonly ICategorieDepenseRepository _repository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;

        public CategorieDepenseController(
            ICategorieDepenseRepository repository,
            IAuditService auditService,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _auditService = auditService;
            _currentUser = currentUser;
        }

        [HttpGet("societe/{idSociete}")]
        [Permission("CategorieDepense.ReadAll")]
        public async Task<ActionResult<IEnumerable<CategorieDepenseResponseDto>>> GetBySociete(int idSociete)
        {
            try
            {
                var items = await _repository.GetBySocieteAsync(
                    idSociete, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);
                return Ok(items);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [Permission("CategorieDepense.Read")]
        public async Task<ActionResult<CategorieDepenseResponseDto>> GetById(int id)
        {
            try
            {
                var item = await _repository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (item == null)
                    return NotFound(new { message = $"Catégorie {id} introuvable" });

                return Ok(item);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Permission("CategorieDepense.Create")]
        public async Task<ActionResult<CategorieDepenseResponseDto>> Create([FromBody] CreateCategorieDepenseDto dto)
        {
            try
            {
                var created = await _repository.CreateAsync(
                    dto, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Création catégorie dépense");

                return CreatedAtAction(nameof(GetById), new { id = created.IdCategorieDepense }, created);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Permission("CategorieDepense.Update")]
        public async Task<ActionResult<CategorieDepenseResponseDto>> Update(int id, [FromBody] UpdateCategorieDepenseDto dto)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Catégorie {id} introuvable" });

                var updated = await _repository.UpdateAsync(
                    id, dto, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (updated == null)
                    return NotFound(new { message = $"Catégorie {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(
                    existing, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Modification catégorie dépense");

                return Ok(updated);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Permission("CategorieDepense.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _repository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Catégorie {id} introuvable" });

                var deleted = await _repository.DeleteAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (!deleted)
                    return NotFound(new { message = $"Catégorie {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogDeleteAsync(
                    existing, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Suppression catégorie dépense");

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}
