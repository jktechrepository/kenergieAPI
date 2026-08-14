using Kenergie.Attributes;
using Kenergie.Helpers;
using Kenergie.Models.DTOs.Depense;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepenseController : ControllerBase
    {
        private readonly IDepenseRepository _depenseRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<DepenseController> _logger;

        public DepenseController(
            IDepenseRepository depenseRepository,
            IAuditService auditService,
            ICurrentUserService currentUser,
            ILogger<DepenseController> logger)
        {
            _depenseRepository = depenseRepository;
            _auditService = auditService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [Permission("Depense.ReadAll")]
        public async Task<ActionResult<PagedResult<DepenseResponseDto>>> GetPaged(
            [FromQuery] PagedRequest request,
            [FromQuery] int? idSociete = null,
            [FromQuery] DateTime? dateDebut = null,
            [FromQuery] DateTime? dateFin = null,
            [FromQuery] int? idCategorieDepense = null,
            [FromQuery] string? statut = null)
        {
            try
            {
                var result = await _depenseRepository.GetPagedAsync(
                    request,
                    idSociete,
                    _currentUser.UserId,
                    _currentUser.PrimaryRole,
                    _currentUser.SocieteId,
                    dateDebut,
                    dateFin,
                    idCategorieDepense,
                    statut);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpGet("mois")]
        [Permission("Depense.ReadAll")]
        public async Task<ActionResult<DepenseMoisResponseDto>> GetByMois(
            [FromQuery] int? mois = null,
            [FromQuery] int? annee = null,
            [FromQuery] int? idSociete = null,
            [FromQuery] string? statut = null)
        {
            try
            {
                var now = DateTime.UtcNow;
                var result = await _depenseRepository.GetByMoisAsync(
                    mois ?? now.Month,
                    annee ?? now.Year,
                    idSociete,
                    _currentUser.UserId,
                    _currentUser.PrimaryRole,
                    _currentUser.SocieteId,
                    statut);
                return Ok(result);
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

        [HttpGet("{id}")]
        [Permission("Depense.Read")]
        public async Task<ActionResult<DepenseResponseDto>> GetById(int id)
        {
            try
            {
                var depense = await _depenseRepository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (depense == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                return Ok(depense);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Permission("Depense.Create")]
        public async Task<ActionResult<DepenseResponseDto>> Create([FromBody] CreateDepenseDto dto)
        {
            try
            {
                var created = await _depenseRepository.CreateAsync(
                    dto, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Création dépense");

                return CreatedAtAction(nameof(GetById), new { id = created.IdDepense }, created);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur création dépense");
                return StatusCode(500, new { message = "Erreur lors de la création de la dépense" });
            }
        }

        [HttpPut("{id}")]
        [Permission("Depense.Update")]
        public async Task<ActionResult<DepenseResponseDto>> Update(int id, [FromBody] UpdateDepenseDto dto)
        {
            try
            {
                var existing = await _depenseRepository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var updated = await _depenseRepository.UpdateAsync(
                    id, dto, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (updated == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(
                    existing, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Modification dépense");

                return Ok(updated);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/valider")]
        [Permission("Depense.Validate")]
        public async Task<ActionResult<DepenseResponseDto>> Valider(int id)
        {
            try
            {
                var existing = await _depenseRepository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var result = await _depenseRepository.ValiderAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (result == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(
                    existing, result, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Validation dépense");

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/refuser")]
        [Permission("Depense.Validate")]
        public async Task<ActionResult<DepenseResponseDto>> Refuser(int id, [FromBody] AnnulerDepenseDto dto)
        {
            try
            {
                var existing = await _depenseRepository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var result = await _depenseRepository.RefuserAsync(
                    id, dto, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (result == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(
                    existing, result, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Refus dépense");

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/annuler")]
        [Permission("Depense.Update")]
        public async Task<ActionResult<DepenseResponseDto>> Annuler(int id, [FromBody] AnnulerDepenseDto dto)
        {
            try
            {
                var existing = await _depenseRepository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var result = await _depenseRepository.AnnulerAsync(
                    id, dto, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (result == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(
                    existing, result, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Annulation dépense");

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Permission("Depense.Delete")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _depenseRepository.GetByIdAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (existing == null)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var deleted = await _depenseRepository.DeleteAsync(
                    id, _currentUser.UserId, _currentUser.PrimaryRole, _currentUser.SocieteId);

                if (!deleted)
                    return NotFound(new { message = $"Dépense {id} introuvable" });

                var ctx = this.GetAuditContext();
                await _auditService.LogDeleteAsync(
                    existing, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete,
                    ctx.IpAddress, ctx.UserAgent, "Suppression dépense");

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }
    }
}
