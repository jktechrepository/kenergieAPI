using System.Text.Json;
using System.Text;
using Kenergie.Models.DTOs.FlexPay;
using Kenergie.Services.FlexPay;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlexPayController : ControllerBase
    {
        private readonly IPaiementElectroniqueService _paiementElectroniqueService;
        private readonly ICurrentUserService _currentUserService;

        public FlexPayController(
            IPaiementElectroniqueService paiementElectroniqueService,
            ICurrentUserService currentUserService)
        {
            _paiementElectroniqueService = paiementElectroniqueService;
            _currentUserService = currentUserService;
        }

        /// <summary>Webhook FlexPay — public, sans JWT.</summary>
        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<ActionResult<FlexPayCallbackResponseDto>> Callback()
        {
            Request.EnableBuffering();
            string payloadJson;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                payloadJson = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            if (string.IsNullOrWhiteSpace(payloadJson))
                payloadJson = "{}";

            FlexPayCallbackDto dto;
            try
            {
                dto = JsonSerializer.Deserialize<FlexPayCallbackDto>(payloadJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new FlexPayCallbackDto();
            }
            catch
            {
                dto = new FlexPayCallbackDto();
            }

            dto.NormalizeFromRawJson(payloadJson);

            var headers = string.Join("; ", Request.Headers.Select(h => $"{h.Key}={h.Value}"));
            if (headers.Length > 1000) headers = headers[..1000];
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _paiementElectroniqueService.ProcessCallbackAsync(dto, payloadJson, headers, ip);
            return Ok(result);
        }

        [HttpGet("verifier/{orderNumber}")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Financier,Caissier,Responsable Commercial,Agent Direction Commercial,Client")]
        public async Task<ActionResult<FlexPayCallbackResponseDto>> Verifier(string orderNumber)
        {
            try
            {
                return Ok(await _paiementElectroniqueService.VerifierAsync(
                    orderNumber,
                    _currentUserService.UserId > 0 ? _currentUserService.UserId : null));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("approve")]
        [AllowAnonymous]
        public IActionResult Approve() =>
            Ok(new { message = "Paiement carte approuvé (informatif). La finalisation se fait via callback." });

        [HttpGet("cancel")]
        [AllowAnonymous]
        public IActionResult Cancel() => Ok(new { message = "Paiement carte annulé (informatif)." });

        [HttpGet("decline")]
        [AllowAnonymous]
        public IActionResult Decline() => Ok(new { message = "Paiement carte refusé (informatif)." });
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Super-Admin,Admin,Financier")]
    public class InfoPaiementSocieteController : ControllerBase
    {
        private readonly IInfoPaiementSocieteService _service;
        private readonly ICurrentUserService _currentUser;

        public InfoPaiementSocieteController(
            IInfoPaiementSocieteService service,
            ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InfoPaiementSocieteDto>>> GetAll()
        {
            int? filter = _currentUser.IsSuperAdmin ? null : _currentUser.SocieteId;
            return Ok(await _service.GetAllAsync(filter));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<InfoPaiementSocieteDto>> Get(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new { message = "Introuvable." });
            if (!_currentUser.IsSuperAdmin && item.IdSociete != _currentUser.SocieteId)
                return Forbid();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<InfoPaiementSocieteDto>> Create([FromBody] CreateInfoPaiementSocieteDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!_currentUser.IsSuperAdmin && dto.IdSociete != _currentUser.SocieteId)
                return Forbid();
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(Get), new { id = created.IdInfoPaiementSociete }, created);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<InfoPaiementSocieteDto>> Update(int id, [FromBody] UpdateInfoPaiementSocieteDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Introuvable." });
            if (!_currentUser.IsSuperAdmin && existing.IdSociete != _currentUser.SocieteId)
                return Forbid();
            try
            {
                return Ok(await _service.UpdateAsync(id, dto));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _service.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "Introuvable." });
            if (!_currentUser.IsSuperAdmin && existing.IdSociete != _currentUser.SocieteId)
                return Forbid();
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
