using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PanneSignalementController : ControllerBase
    {
        private readonly KenergieDbContext _context;

        public PanneSignalementController(KenergieDbContext context)
        {
            _context = context;
        }

        // GET: api/PanneSignalement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PanneSignalement>>> GetAll()
        {
            var items = await _context.PanneSignalements
                .AsNoTracking()
                .Where(p => p.Statut == true)
                .ToListAsync();
            return Ok(items);
        }

        // GET: api/PanneSignalement/paged
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<PanneSignalement>>> GetPaged([FromQuery] PagedRequest request, [FromQuery] bool? statut = null)
        {
            request ??= new PagedRequest();
            var query = _context.PanneSignalements.AsNoTracking();

            // Par défaut, ne retourner que les signalements actifs (Statut = true)
            if (!statut.HasValue || statut.Value)
            {
                query = query.Where(p => p.Statut == true);
            }
            else
            {
                query = query.Where(p => p.Statut == false);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p => p.Description.ToLower().Contains(term));
            }

            query = request.SortBy?.ToLower() switch
            {
                "statut" => request.SortDescending ? query.OrderByDescending(p => p.Statut) : query.OrderBy(p => p.Statut),
                "description" => request.SortDescending ? query.OrderByDescending(p => p.Description) : query.OrderBy(p => p.Description),
                _ => request.SortDescending ? query.OrderByDescending(p => p.IdPanneSignalement) : query.OrderBy(p => p.IdPanneSignalement)
            };

            var total = await query.CountAsync();
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return Ok(new PagedResult<PanneSignalement>(data, total, request.PageNumber, request.PageSize));
        }

        // GET: api/PanneSignalement/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PanneSignalement>> GetById(int id)
        {
            var item = await _context.PanneSignalements
                .Where(p => p.Statut == true)
                .FirstOrDefaultAsync(p => p.IdPanneSignalement == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // POST: api/PanneSignalement
        [HttpPost]
        public async Task<ActionResult<PanneSignalement>> Create([FromBody] PanneSignalement model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new PanneSignalement
            {
                Description = model.Description,
                Statut = model.Statut,
                TypePanne = model.TypePanne,
                NiveauImportance = model.NiveauImportance,
                RisquesPrincipaux = model.RisquesPrincipaux
            };

            _context.PanneSignalements.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.IdPanneSignalement }, entity);
        }

        // PUT: api/PanneSignalement/5
        [HttpPut("{id}")]
        public async Task<ActionResult<PanneSignalement>> Update(int id, [FromBody] PanneSignalement model)
        {
            if (id != model.IdPanneSignalement)
                return BadRequest(new { message = "L'ID URL ne correspond pas à l'ID du corps." });

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = await _context.PanneSignalements.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Description = model.Description;
            entity.Statut = model.Statut;
            entity.TypePanne = model.TypePanne;
            entity.NiveauImportance = model.NiveauImportance;
            entity.RisquesPrincipaux = model.RisquesPrincipaux;

            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        // PATCH: api/PanneSignalement/5/statut
        [HttpPatch("{id}/statut")]
        public async Task<ActionResult<PanneSignalement>> UpdateStatut(int id, [FromBody] bool statut)
        {
            var entity = await _context.PanneSignalements.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Statut = statut;
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        // PATCH: api/PanneSignalement/5/toggle-statut
        [HttpPatch("{id}/toggle-statut")]
        public async Task<ActionResult<PanneSignalement>> ToggleStatut(int id)
        {
            var entity = await _context.PanneSignalements.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Statut = !entity.Statut;
            await _context.SaveChangesAsync();
            return Ok(entity);
        }

        // DELETE: api/PanneSignalement/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<object>> Delete(int id)
        {
            var entity = await _context.PanneSignalements.FindAsync(id);
            if (entity == null) return NotFound(new { message = "PanneSignalement non trouvé" });

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            entity.Statut = false;
            await _context.SaveChangesAsync();
            
            return Ok(new 
            { 
                message = "PanneSignalement désactivé avec succès (soft delete)",
                idPanneSignalement = id,
                note = "Le signalement a été désactivé. Les données sont conservées pour l'historique."
            });
        }
    }
}

