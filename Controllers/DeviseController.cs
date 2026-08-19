using Kenergie.Data;
using Kenergie.Helpers;
using Kenergie.Models.DTOs.Devise;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeviseController : ControllerBase
    {
        private const string RolesLecture =
            "Super-Admin,Admin,Gerant,Financier,Caissier,Responsable Commercial,Agent Direction Commercial,Client";
        private const string RolesAdmin =
            "Super-Admin,Admin,Gerant";

        private readonly IDeviseRepository _deviseRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAuditService _auditService;
        private readonly KenergieDbContext _context;

        public DeviseController(
            IDeviseRepository deviseRepository,
            ICurrentUserService currentUserService,
            IAuditService auditService,
            KenergieDbContext context)
        {
            _deviseRepository = deviseRepository;
            _currentUserService = currentUserService;
            _auditService = auditService;
            _context = context;
        }

        /// <summary>
        /// Liste les devises actives (scope société hors Super-Admin).
        /// Client : scope sur la société liée à son IdClient.
        /// </summary>
        [HttpGet("devises")]
        [Authorize(Roles = RolesLecture)]
        public async Task<ActionResult<IEnumerable<DeviseDto>>> GetDevisesActives()
        {
            try
            {
                var filter = await ResolveSocieteFilterAsync();
                if (IsClientRole() && (!filter.HasValue || filter.Value <= 0))
                    return BadRequest(new { message = "Société introuvable pour ce client." });

                var devises = await _deviseRepository.GetDevisesActivesAsync(filter);
                return Ok(devises);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
        }

        [HttpGet("devises/{idDeviseMonetaire:int}")]
        [Authorize(Roles = RolesLecture)]
        public async Task<ActionResult<DeviseDto>> GetDevise(int idDeviseMonetaire)
        {
            var devise = await _deviseRepository.GetDeviseByIdAsync(idDeviseMonetaire);
            if (devise == null)
                return NotFound(new { message = "Devise introuvable." });

            if (!await CanAccessSocieteAsync(devise.IdSociete))
                return Forbid();

            return Ok(devise);
        }

        [HttpPost("devises")]
        [Authorize(Roles = RolesAdmin)]
        public async Task<ActionResult<DeviseDto>> CreateDevise([FromBody] CreateDeviseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await CanAccessSocieteAsync(dto.IdSociete))
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

            if (!await CanAccessSocieteAsync(existing.IdSociete))
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
            if (!await CanAccessSocieteAsync(idSociete))
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

            if (!await CanAccessSocieteAsync(dto.IdSociete))
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
        public async Task<ActionResult<IEnumerable<TauxChangeDto>>> GetTauxChanges(
            [FromQuery] int? idSociete = null,
            [FromQuery] string? source = null,
            [FromQuery] string? cible = null)
        {
            if (idSociete.HasValue && !await CanAccessSocieteAsync(idSociete.Value))
                return Forbid();

            int? societeFilter;
            if (_currentUserService.IsSuperAdmin)
            {
                societeFilter = idSociete;
            }
            else
            {
                societeFilter = await ResolveSocieteFilterAsync();
                if (IsClientRole() && (!societeFilter.HasValue || societeFilter.Value <= 0))
                    return BadRequest(new { message = "Société introuvable pour ce client." });
            }

            var taux = await _deviseRepository.GetTauxChangesAsync(societeFilter, source, cible);
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
            if (!await CanAccessSocieteAsync(idSociete))
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

        private bool IsClientRole()
        {
            return string.Equals(_currentUserService.UserRole, "Client", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(_currentUserService.PrimaryRole, "Client", StringComparison.OrdinalIgnoreCase)
                   || string.Equals(_currentUserService.GetUserRole(), "Client", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Super-Admin : null (toutes). Staff : claim SocieteId. Client : claim ou société dérivée de IdClient.
        /// </summary>
        private async Task<int?> ResolveSocieteFilterAsync()
        {
            if (_currentUserService.IsSuperAdmin)
                return null;

            if (_currentUserService.SocieteId > 0)
                return _currentUserService.SocieteId;

            if (IsClientRole())
                return await ResolveSocieteFromClientAsync();

            return null;
        }

        private async Task<int?> ResolveSocieteFromClientAsync()
        {
            var userId = _currentUserService.UserId;
            if (userId <= 0)
                return null;

            var idClient = await _context.Utilisateurs
                .AsNoTracking()
                .Where(u => u.IdUtilisateur == userId)
                .Select(u => u.IdClient)
                .FirstOrDefaultAsync();

            if (!idClient.HasValue || idClient.Value <= 0)
                return null;

            var fromUsage = await _context.ClientUsages
                .AsNoTracking()
                .Where(cu => cu.IdClient == idClient.Value && cu.Statut)
                .Select(cu => (int?)cu.Usage!.CategorieClient!.IdSociete)
                .FirstOrDefaultAsync();

            if (fromUsage.HasValue && fromUsage.Value > 0)
                return fromUsage.Value;

            return await _context.ClientFactures
                .AsNoTracking()
                .Where(cf => cf.IdClient == idClient.Value && cf.Statut && cf.IdFacture != null)
                .Select(cf => (int?)cf.Facture!.Usage!.CategorieClient!.IdSociete)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> CanAccessSocieteAsync(int idSociete)
        {
            if (_currentUserService.IsSuperAdmin)
                return true;

            if (_currentUserService.SocieteId > 0)
                return _currentUserService.SocieteId == idSociete;

            if (IsClientRole())
            {
                var resolved = await ResolveSocieteFromClientAsync();
                return resolved.HasValue && resolved.Value == idSociete;
            }

            return false;
        }
    }
}
