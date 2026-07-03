using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class AxeService : IAxeRepository
    {
        private readonly KenergieDbContext _context;

        public AxeService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Axe>> GetAllAsync()
        {
            return await _context.Axes
                .Include(a => a.Cabine)
                    .ThenInclude(c => c.Societe)
                .Where(a => a.Statut == true)
                .OrderBy(a => a.NomAxe)
                .ToListAsync();
        }

        public async Task<IEnumerable<Axe>> GetByCabineAsync(int idCabine)
        {
            return await _context.Axes
                .Include(a => a.Cabine)
                    .ThenInclude(c => c.Societe)
                .Where(a => a.IdCabine == idCabine && a.Statut == true)
                .OrderBy(a => a.NomAxe)
                .ToListAsync();
        }

        public async Task<IEnumerable<Axe>> GetBySocieteAsync(int idSociete)
        {
            return await _context.Axes
                .Include(a => a.Cabine)
                    .ThenInclude(c => c.Societe)
                .Where(a => a.Cabine != null && a.Cabine.IdSociete == idSociete && a.Statut == true)
                .OrderBy(a => a.NomAxe)
                .ToListAsync();
        }

        public async Task<Axe> GetByIdAsync(int id)
        {
            return await _context.Axes
                .Include(a => a.Cabine)
                    .ThenInclude(c => c.Societe)
                .Where(a => a.Statut == true)
                .FirstOrDefaultAsync(a => a.IdAxe == id);
        }

        public async Task<Axe> GetByNomAsync(string nomAxe, int idCabine)
        {
            return await _context.Axes
                .Include(a => a.Cabine)
                    .ThenInclude(c => c.Societe)
                .Where(a => a.IdCabine == idCabine)
                .FirstOrDefaultAsync(a => a.NomAxe == nomAxe);
        }

        public async Task<Axe> CreateAsync(Axe axe)
        {
            axe.DateCreation = DateTime.Now;
            _context.Axes.Add(axe);
            await _context.SaveChangesAsync();
            return axe;
        }

        public async Task<Axe> UpdateAsync(Axe axe)
        {
            var existing = await _context.Axes.FindAsync(axe.IdAxe);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(axe);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var axe = await _context.Axes.FindAsync(id);
            if (axe == null)
                return false;

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            axe.Statut = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Axes.AnyAsync(a => a.IdAxe == id);
        }

        public async Task<bool> ExistsByNomAsync(string nomAxe, int idCabine)
        {
            return await _context.Axes
                .AnyAsync(a => a.NomAxe == nomAxe && a.IdCabine == idCabine && a.Statut == true);
        }
    }
}
