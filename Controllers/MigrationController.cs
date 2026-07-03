using Kenergie.Services;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Super-Admin,Admin")]
    public class MigrationController : ControllerBase
    {
        private readonly ClientFactureMigrationService _migrationService;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;

        public MigrationController(
            ClientFactureMigrationService migrationService,
            IAuditService auditService,
            ICurrentUserService currentUserService)
        {
            _migrationService = migrationService;
            _auditService = auditService;
            _currentUserService = currentUserService;
        }

        // POST: api/Migration/migrate-factures
        [HttpPost("migrate-factures")]
        [Authorize(Roles = "Super-Admin")]
        public async Task<ActionResult<object>> MigrateFactures()
        {
            try
            {
                var result = await _migrationService.MigrateExistingFacturesAsync();

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    result,
                    ctx.UserId,
                    ctx.UserName,
                    ctx.UserRole,
                    ctx.IdSociete,
                    ctx.IpAddress,
                    ctx.UserAgent,
                    $"Migration des factures vers ClientFactures: {result.ClientFacturesCreated} créée(s)");

                return Ok(new
                {
                    success = result.Success,
                    message = result.Success
                        ? $"Migration réussie: {result.ClientFacturesCreated} ClientFacture(s) créée(s)"
                        : $"Migration terminée avec {result.Errors} erreur(s)",
                    result = new
                    {
                        totalFactures = result.TotalFactures,
                        clientFacturesCreated = result.ClientFacturesCreated,
                        skipped = result.Skipped,
                        errors = result.Errors,
                        duration = result.Duration?.ToString(@"hh\:mm\:ss"),
                        errorMessages = result.ErrorMessages
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Erreur lors de la migration: {ex.Message}"
                });
            }
        }

        // GET: api/Migration/validate
        [HttpGet("validate")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<object>> ValidateMigration()
        {
            try
            {
                var result = await _migrationService.ValidateMigrationAsync();

                return Ok(new
                {
                    isValid = result.IsValid,
                    message = result.IsValid
                        ? "✅ Migration validée avec succès"
                        : $"⚠️  {result.IncoherencesMontantPaye + result.IncoherencesMontantDu} incohérence(s) détectée(s)",
                    result = new
                    {
                        totalClientFactures = result.TotalClientFactures,
                        totalFactures = result.TotalFactures,
                        facturesWithClientFacture = result.FacturesWithClientFacture,
                        incoherencesMontantPaye = result.IncoherencesMontantPaye,
                        incoherencesMontantDu = result.IncoherencesMontantDu,
                        duration = result.Duration?.ToString(@"hh\:mm\:ss"),
                        errorMessage = result.ErrorMessage
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    isValid = false,
                    message = $"Erreur lors de la validation: {ex.Message}"
                });
            }
        }
    }
}
