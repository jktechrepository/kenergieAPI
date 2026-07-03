using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Kenergie.Services;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TypeDeCourantController : ControllerBase
    {
        private readonly ITypeDeCourantRepository _typeDeCourantRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<TypeDeCourantController> _logger;

        public TypeDeCourantController(
            ITypeDeCourantRepository typeDeCourantRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<TypeDeCourantController> logger)
        {
            _typeDeCourantRepository = typeDeCourantRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        // GET: api/TypeDeCourant
        /// <summary>
        /// Récupère tous les types de courant
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TypeDeCourant>>> GetAll()
        {
            try
            {
                var types = await _typeDeCourantRepository.GetAllAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de courant");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        // GET: api/TypeDeCourant/actifs
        /// <summary>
        /// Récupère les types de courant actifs uniquement
        /// </summary>
        [HttpGet("actifs")]
        public async Task<ActionResult<IEnumerable<TypeDeCourant>>> GetActifs()
        {
            try
            {
                var types = await _typeDeCourantRepository.GetActifsAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des types de courant actifs");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        // GET: api/TypeDeCourant/5
        /// <summary>
        /// Récupère un type de courant par son ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TypeDeCourant>> GetById(int id)
        {
            try
            {
                var type = await _typeDeCourantRepository.GetByIdAsync(id);
                if (type == null)
                    return NotFound(new { message = "Type de courant non trouvé" });

                return Ok(type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du type de courant {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        // POST: api/TypeDeCourant
        /// <summary>
        /// Crée un nouveau type de courant
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Super-Admin,Admin,Financier, Gerant")]
        public async Task<ActionResult<TypeDeCourant>> Create([FromBody] TypeDeCourant typeDeCourant)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Vérifier si le libelle existe déjà
                var existingByLibelle = await _typeDeCourantRepository.GetByLibelleAsync(typeDeCourant.Libelle);
                if (existingByLibelle != null)
                    return BadRequest(new { message = "Ce libellé existe déjà" });

                var newType = await _typeDeCourantRepository.CreateAsync(typeDeCourant);

                // Audit
                await _auditService.LogCreateAsync(
                    newType,
                    _currentUserService.UserId,
                    _currentUserService.UserName ?? "Unknown",
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Création du type de courant: {newType.Libelle}");

                return CreatedAtAction(nameof(GetById), new { id = newType.IdTypeDeCourant }, newType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du type de courant");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        // PUT: api/TypeDeCourant/5
        /// <summary>
        /// Met à jour un type de courant
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin,Admin,Financier, Gerant")]
        public async Task<ActionResult<TypeDeCourant>> Update(int id, [FromBody] TypeDeCourant typeDeCourant)
        {
            try
            {
                if (id != typeDeCourant.IdTypeDeCourant)
                    return BadRequest(new { message = "ID mismatch" });

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var existing = await _typeDeCourantRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Type de courant non trouvé" });

                var oldType = new TypeDeCourant
                {
                    IdTypeDeCourant = existing.IdTypeDeCourant,
                    Libelle = existing.Libelle,
                    Description = existing.Description,
                    Statut = existing.Statut
                };

                var updatedType = await _typeDeCourantRepository.UpdateAsync(typeDeCourant);
                if (updatedType == null)
                    return BadRequest(new { message = "Erreur lors de la mise à jour" });

                // Audit
                await _auditService.LogUpdateAsync<TypeDeCourant>(
                    oldType,
                    updatedType,
                    _currentUserService.UserId,
                    _currentUserService.UserName ?? "Unknown",
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Mise à jour du type de courant: {updatedType.Libelle}");

                return Ok(updatedType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du type de courant {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        // DELETE: api/TypeDeCourant/5
        /// <summary>
        /// Supprime un type de courant
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super-Admin,Admin,Financier, Gerant")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var existing = await _typeDeCourantRepository.GetByIdAsync(id);
                if (existing == null)
                    return NotFound(new { message = "Type de courant non trouvé" });

                var deleted = await _typeDeCourantRepository.DeleteAsync(id);
                if (!deleted)
                    return BadRequest(new { message = "Impossible de supprimer ce type de courant (utilisé par des clients ou factures)" });

                // Audit
                await _auditService.LogDeleteAsync(
                    existing,
                    _currentUserService.UserId,
                    _currentUserService.UserName ?? "Unknown",
                    _currentUserService.UserRole,
                    _currentUserService.SocieteId,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString(),
                    $"Suppression du type de courant: {existing.Libelle}");

                return Ok(new { message = "Type de courant supprimé avec succès" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du type de courant {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }
    }
}
