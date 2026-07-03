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
    public class ClientUsageController : ControllerBase
    {
        private readonly IClientUsageRepository _clientUsageRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public ClientUsageController(
            IClientUsageRepository clientUsageRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _clientUsageRepository = clientUsageRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // GET: api/ClientUsage
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientUsage>>> GetClientUsages()
        {
            var clientUsages = await _clientUsageRepository.GetAllAsync();
            return Ok(clientUsages);
        }

        // GET: api/ClientUsage/client/{idClient}
        [HttpGet("client/{idClient}")]
        public async Task<ActionResult<IEnumerable<ClientUsage>>> GetClientUsagesByClient(int idClient)
        {
            var clientUsages = await _clientUsageRepository.GetByClientAsync(idClient);
            return Ok(clientUsages);
        }

        // GET: api/ClientUsage/usage/{idUsage}
        [HttpGet("usage/{idUsage}")]
        public async Task<ActionResult<IEnumerable<ClientUsage>>> GetClientUsagesByUsage(int idUsage)
        {
            var clientUsages = await _clientUsageRepository.GetByUsageAsync(idUsage);
            return Ok(clientUsages);
        }

        // GET: api/ClientUsage/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientUsage>> GetClientUsage(int id)
        {
            var clientUsage = await _clientUsageRepository.GetByIdAsync(id);
            if (clientUsage == null)
            {
                return NotFound(new { message = "Relation Client-Usage non trouvée" });
            }
            return Ok(clientUsage);
        }

        // GET: api/ClientUsage/client/{idClient}/usage/{idUsage}
        [HttpGet("client/{idClient}/usage/{idUsage}")]
        public async Task<ActionResult<ClientUsage>> GetClientUsageByClientAndUsage(int idClient, int idUsage)
        {
            var clientUsage = await _clientUsageRepository.GetByClientAndUsageAsync(idClient, idUsage);
            if (clientUsage == null)
            {
                return NotFound(new { message = "Relation Client-Usage non trouvée" });
            }
            return Ok(clientUsage);
        }

        // POST: api/ClientUsage
        [HttpPost]
        public async Task<ActionResult<ClientUsage>> CreateClientUsage(ClientUsage clientUsage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var created = await _clientUsageRepository.CreateAsync(clientUsage);

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création relation Client-Usage");

                return CreatedAtAction(nameof(GetClientUsage), new { id = created.IdClientUsage }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/ClientUsage/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ClientUsage>> UpdateClientUsage(int id, ClientUsage clientUsage)
        {
            if (id != clientUsage.IdClientUsage)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps de la requête." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _clientUsageRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Relation Client-Usage non trouvée" });
            }

            try
            {
                // Snapshot avant modification
                var oldClientUsage = new ClientUsage
                {
                    IdClientUsage = existing.IdClientUsage,
                    IdClient = existing.IdClient,
                    IdUsage = existing.IdUsage,
                    nombreBatiment = existing.nombreBatiment
                };

                var updated = await _clientUsageRepository.UpdateAsync(clientUsage);
                if (updated == null)
                {
                    return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
                }

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogUpdateAsync(oldClientUsage, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification relation Client-Usage");

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/ClientUsage/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteClientUsage(int id)
        {
            var clientUsage = await _clientUsageRepository.GetByIdAsync(id);
            if (clientUsage == null)
            {
                return NotFound(new { message = "Relation Client-Usage non trouvée" });
            }

            try
            {
                var deleted = await _clientUsageRepository.DeleteAsync(id);
                if (!deleted)
                {
                    return StatusCode(500, new { message = "Erreur lors de la suppression de la relation." });
                }

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogDeleteAsync(clientUsage, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Désactivation relation Client-Usage (soft delete)");

                return Ok(new 
                { 
                    message = "Relation Client-Usage désactivée avec succès (soft delete)",
                    idClientUsage = id,
                    note = "La relation a été désactivée. Les données sont conservées pour l'historique."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
