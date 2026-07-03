using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class CabineService : ICabineRepository
    {
        private readonly KenergieDbContext _context;

        public CabineService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cabine>> GetAllAsync()
        {
            return await _context.Cabines
                .Include(c => c.Societe)
                .Where(c => c.Statut == true)
                .OrderBy(c => c.Nom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Cabine>> GetBySocieteAsync(int idSociete)
        {
            return await _context.Cabines
                .Include(c => c.Societe)
                .Where(c => c.IdSociete == idSociete && c.Statut == true)
                .OrderBy(c => c.Nom)
                .ToListAsync();
        }

        public async Task<Cabine> GetByIdAsync(int id)
        {
            return await _context.Cabines
                .Include(c => c.Societe)
                .Where(c => c.Statut == true)
                .FirstOrDefaultAsync(c => c.IdCabine == id);
        }

        public async Task<Cabine> GetByNomAsync(string nom, int idSociete)
        {
            return await _context.Cabines
                .Include(c => c.Societe)
                .Where(c => c.IdSociete == idSociete)
                .FirstOrDefaultAsync(c => c.Nom == nom);
        }

        public async Task<Cabine> CreateAsync(Cabine cabine)
        {
            cabine.DateCreation = DateTime.Now;
            _context.Cabines.Add(cabine);
            await _context.SaveChangesAsync();
            return cabine;
        }

        public async Task<Cabine> UpdateAsync(Cabine cabine)
        {
            var existing = await _context.Cabines.FindAsync(cabine.IdCabine);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(cabine);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cabine = await _context.Cabines.FindAsync(id);
            if (cabine == null)
                return false;

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            cabine.Statut = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Cabines.AnyAsync(c => c.IdCabine == id);
        }

        public async Task<bool> ExistsByNomAsync(string nom, int idSociete)
        {
            return await _context.Cabines
                .AnyAsync(c => c.Nom == nom && c.IdSociete == idSociete && c.Statut == true);
        }
    }
}
