using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class UsageService : IUsageRepository
    {
        private readonly KenergieDbContext _context;

        public UsageService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Usage>> GetAllAsync()
        {
            return await _context.Usages
                .Include(u => u.CategorieClient)
                    .ThenInclude(cc => cc.Societe)
                .Where(u => u.Statut == true)
                .OrderBy(u => u.Libelle)
                .ToListAsync();
        }

        public async Task<IEnumerable<Usage>> GetByCategorieClientAsync(int idCategorieClient)
        {
            return await _context.Usages
                .Include(u => u.CategorieClient)
                    .ThenInclude(cc => cc.Societe)
                .Where(u => u.IdCategorieClient == idCategorieClient && u.Statut == true)
                .OrderBy(u => u.Libelle)
                .ToListAsync();
        }

        public async Task<IEnumerable<Usage>> GetBySocieteAsync(int idSociete)
        {
            return await _context.Usages
                .Include(u => u.CategorieClient)
                    .ThenInclude(cc => cc.Societe)
                .Where(u => u.CategorieClient != null && u.CategorieClient.IdSociete == idSociete && u.Statut == true)
                .OrderBy(u => u.Libelle)
                .ToListAsync();
        }

        public async Task<Usage> GetByIdAsync(int id)
        {
            return await _context.Usages
                .Include(u => u.CategorieClient)
                    .ThenInclude(cc => cc.Societe)
                .Where(u => u.Statut == true)
                .FirstOrDefaultAsync(u => u.IdUsage == id);
        }

        public async Task<Usage> GetByLibelleAsync(string libelle, int idCategorieClient)
        {
            return await _context.Usages
                .Include(u => u.CategorieClient)
                    .ThenInclude(cc => cc.Societe)
                .Where(u => u.IdCategorieClient == idCategorieClient)
                .FirstOrDefaultAsync(u => u.Libelle == libelle);
        }

        public async Task<Usage> CreateAsync(Usage usage)
        {
            usage.DateCreation = DateTime.Now;
            if (usage.Statut == null)
            {
                usage.Statut = true;
            }
            _context.Usages.Add(usage);
            await _context.SaveChangesAsync();
            return usage;
        }

        public async Task<Usage> UpdateAsync(Usage usage)
        {
            var existing = await _context.Usages.FindAsync(usage.IdUsage);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(usage);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usage = await _context.Usages.FindAsync(id);
            if (usage == null)
                return false;

            // Vérifier si l'usage est utilisé par des clients ou des factures
            var hasClients = await _context.ClientUsages.AnyAsync(cu => cu.IdUsage == id && cu.Statut == true);
            var hasFactures = await _context.Factures.AnyAsync(f => f.IdUsage == id && f.Statut == true);

            if (hasClients || hasFactures)
            {
                // ✨ Soft delete : mettre Statut à false au lieu de supprimer
                usage.Statut = false;
                await _context.SaveChangesAsync();
                return true;
            }

            // Si pas utilisé, hard delete (suppression complète)
            _context.Usages.Remove(usage);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Usages.AnyAsync(u => u.IdUsage == id);
        }

        public async Task<bool> ExistsByLibelleAsync(string libelle, int idCategorieClient)
        {
            return await _context.Usages
                .AnyAsync(u => u.Libelle == libelle && u.IdCategorieClient == idCategorieClient);
        }
    }
}
