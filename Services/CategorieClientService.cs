using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class CategorieClientService : ICategorieClientRepository
    {
        private readonly KenergieDbContext _context;

        public CategorieClientService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<CategorieClient>> GetAllAsync()
        {
            // Retourner toutes les catégories sans filtre sur Statut
            // (la colonne Statut peut ne pas exister encore en base de données)
            return await _context.CategorieClients
                .Include(c => c.Societe)
                .OrderBy(c => c.NomCategorie)
                .ToListAsync();
        }

        public async Task<IEnumerable<CategorieClient>> GetBySocieteAsync(int idSociete)
        {
            return await _context.CategorieClients
                .Include(c => c.Societe)
                .Where(c => c.IdSociete == idSociete && (c.Statut == true || c.Statut == null))
                .OrderBy(c => c.NomCategorie)
                .ToListAsync();
        }

        public async Task<CategorieClient> GetByIdAsync(int id)
        {
            return await _context.CategorieClients
                .Include(c => c.Societe)
                .FirstOrDefaultAsync(c => c.IdCategorie == id);
        }

        public async Task<CategorieClient> GetByNomAsync(string nom, int idSociete)
        {
            return await _context.CategorieClients
                .Include(c => c.Societe)
                .Where(c => c.IdSociete == idSociete && (c.Statut == true || c.Statut == null))
                .FirstOrDefaultAsync(c => c.NomCategorie == nom);
        }

        public async Task<CategorieClient> CreateAsync(CategorieClient categorieClient)
        {
            categorieClient.DateCreation = DateTime.Now;
            if (categorieClient.Statut == null)
            {
                categorieClient.Statut = true;
            }

            _context.CategorieClients.Add(categorieClient);
            await _context.SaveChangesAsync();
            return categorieClient;
        }

        public async Task<CategorieClient> UpdateAsync(CategorieClient categorieClient)
        {
            var existing = await _context.CategorieClients.FindAsync(categorieClient.IdCategorie);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(categorieClient);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var categorieClient = await _context.CategorieClients.FindAsync(id);
            if (categorieClient == null)
                return false;

            _context.CategorieClients.Remove(categorieClient);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.CategorieClients.AnyAsync(c => c.IdCategorie == id);
        }

        public async Task<bool> ExistsByNomAsync(string nom, int idSociete)
        {
            return await _context.CategorieClients
                .AnyAsync(c => c.NomCategorie == nom && c.IdSociete == idSociete);
        }

        public async Task<bool> ToggleStatutAsync(int id)
        {
            var categorieClient = await _context.CategorieClients.FindAsync(id);
            if (categorieClient == null)
                return false;

            categorieClient.Statut = categorieClient.Statut != true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            var categorieClient = await _context.CategorieClients.FindAsync(id);
            if (categorieClient == null)
                return false;

            categorieClient.Statut = statut;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

