using Kenergie.Helpers;
using Kenergie.Models.DTOs.Devise;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviseController : ControllerBase
    {
        private const string RolesLecture =
            "Super-Admin,Admin,Gerant,Financier,Caissier,Responsable Commercial,Agent Direction Commercial";
        private const string RolesAdmin =
            "Super-Admin,Admin,Gerant";

        private readonly IDeviseRepository _deviseRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;

        public DeviseController(
            IDeviseRepository deviseRepository,
            ICurrentUserService currentUserService,
            IAuditService auditService)
        {
            _deviseRepository = deviseRepository;
            _currentUserService = currentUserService;
            _auditService = auditService;
        }

        /// <summary>
        /// Liste les devises actives (scope société hors Super-Admin).
        /// </summary>
        [HttpGet("devises")]
        [Authorize(Roles = RolesLecture)]
        public async Task<ActionResult<IEnumerable<DeviseDto>>> GetDevisesActives()
        {
            int? filter = _currentUserService.IsSuperAdmin ? null : _currentUserService.SocieteId;
            var devises = await _deviseRepository.GetDevisesActivesAsync(filter);
            return Ok(devises);
        }

        [HttpGet("devises/{idDeviseMonetaire:int}")]
        [Authorize(Roles = RolesLecture)]
        public async Task<ActionResult<DeviseDto>> GetDevise(int idDeviseMonetaire)
        {
            var devise = await _deviseRepository.GetDeviseByIdAsync(idDeviseMonetaire);
            if (devise == null)
                return NotFound(new { message = "Devise introuvable." });

            if (!CanAccessSociete(devise.IdSociete))
                return Forbid();

            return Ok(devise);
        }

        [HttpPost("devises")]
        [Authorize(Roles = RolesAdmin)]
        public async Task<ActionResult<DeviseDto>> CreateDevise([FromBody] CreateDeviseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!CanAccessSociete(dto.IdSociete))
                return Forbid();

            try
            {
                var created = await _deviseRepository.CreateDeviseAsync(dto);
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création devise");
                return CreatedAtAction(nameof(GetDevise), new { idDeviseMonetaire = created.IdDeviseMonetaire }, created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("devises/{idDeviseMonetaire:int}")]
        [Authorize(Roles = RolesAdmin)]
        public async Task<ActionResult<DeviseDto>> UpdateDevise(int idDeviseMonetaire, [FromBody] UpdateDeviseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _deviseRepository.GetDeviseByIdAsync(idDeviseMonetaire);
            if (existing == null)
                return NotFound(new { message = "Devise introuvable." });

            if (!CanAccessSociete(existing.IdSociete))
                return Forbid();

            try
            {
                var updated = await _deviseRepository.UpdateDeviseAsync(idDeviseMonetaire, dto);
                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(existing, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification devise");
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("societe/{idSociete:int}/devise-principale/{codeDevise}")]
        [Authorize(Roles = RolesAdmin)]
        public async Task<IActionResult> SetDevisePrincipale(int idSociete, string codeDevise)
        {
            if (!CanAccessSociete(idSociete))
                return Forbid();

            try
            {
                await _deviseRepository.SetDevisePrincipaleAsync(idSociete, codeDevise);
                return Ok(new { idSociete, codeDevisePrincipale = codeDevise.Trim().ToUpperInvariant() });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("taux-change")]
        [Authorize(Roles = RolesAdmin)]
        public async Task<ActionResult<TauxChangeDto>> CreateTauxChange([FromBody] CreateTauxChangeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!CanAccessSociete(dto.IdSociete))
                return Forbid();

            try
            {
                var created = await _deviseRepository.CreateTauxChangeAsync(dto);
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création taux de change");
                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("taux-change")]
        [Authorize(Roles = RolesLecture)]
        public async Task<ActionResult<TauxChangeDto>> GetDernierTauxChange(
            [FromQuery] int idSociete,
            [FromQuery] string source,
            [FromQuery] string cible)
        {
            if (!CanAccessSociete(idSociete))
                return Forbid();

            var taux = await _deviseRepository.GetDernierTauxChangeAsync(idSociete, source, cible);
            if (taux == null)
                return NotFound(new { message = "Aucun taux trouvé pour cette paire." });

            return Ok(taux);
        }

        [HttpGet("preview-conversion")]
        [Authorize(Roles = RolesLecture)]
        public async Task<ActionResult<PreviewConversionDto>> PreviewConversion(
            [FromQuery] int idSociete,
            [FromQuery] string codeDeviseSource,
            [FromQuery] decimal montant,
            [FromQuery] DateTime? datePaiement)
        {
            if (!CanAccessSociete(idSociete))
                return Forbid();

            try
            {
                var preview = await _deviseRepository.PreviewConversionAsync(
                    idSociete,
                    codeDeviseSource,
                    montant,
                    datePaiement ?? DateTime.UtcNow);
                return Ok(preview);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        private bool CanAccessSociete(int idSociete)
        {
            if (_currentUserService.IsSuperAdmin)
                return true;
            return _currentUserService.SocieteId == idSociete;
        }
    }
}
