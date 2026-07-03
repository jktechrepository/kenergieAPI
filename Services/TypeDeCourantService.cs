using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour la gestion des TypeDeCourant
    /// </summary>
    public class TypeDeCourantService : ITypeDeCourantRepository
    {
        private readonly KenergieDbContext _context;

        public TypeDeCourantService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TypeDeCourant>> GetAllAsync()
        {
            return await _context.TypeDeCourants
                .OrderBy(tc => tc.Libelle)
                .ToListAsync();
        }

        public async Task<TypeDeCourant?> GetByIdAsync(int idTypeDeCourant)
        {
            return await _context.TypeDeCourants
                .FirstOrDefaultAsync(tc => tc.IdTypeDeCourant == idTypeDeCourant);
        }

        public async Task<TypeDeCourant> CreateAsync(TypeDeCourant typeDeCourant)
        {
            typeDeCourant.DateCreation = DateTime.Now;
            typeDeCourant.Statut = true;

            _context.TypeDeCourants.Add(typeDeCourant);
            await _context.SaveChangesAsync();
            return typeDeCourant;
        }

        public async Task<TypeDeCourant?> UpdateAsync(TypeDeCourant typeDeCourant)
        {
            var existing = await _context.TypeDeCourants
                .FirstOrDefaultAsync(tc => tc.IdTypeDeCourant == typeDeCourant.IdTypeDeCourant);

            if (existing == null)
                return null;

            var oldType = new TypeDeCourant
            {
                IdTypeDeCourant = existing.IdTypeDeCourant,
                Libelle = existing.Libelle,
                Description = existing.Description,
                Statut = existing.Statut
            };

            existing.Libelle = typeDeCourant.Libelle;
            existing.Description = typeDeCourant.Description;
            existing.Statut = typeDeCourant.Statut;
            existing.DateModification = DateTime.Now;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int idTypeDeCourant)
        {
            var typeDeCourant = await _context.TypeDeCourants
                .FirstOrDefaultAsync(tc => tc.IdTypeDeCourant == idTypeDeCourant);

            if (typeDeCourant == null)
                return false;

            // Vérifier si le type est utilisé par des lignes ClientUsage ou des factures
            var hasClientUsages = await _context.ClientUsages
                .AnyAsync(cu => cu.IdTypeDeCourant == idTypeDeCourant);
            
            var hasFactures = await _context.Factures
                .AnyAsync(f => f.IdTypeDeCourant == idTypeDeCourant);

            if (hasClientUsages || hasFactures)
                return false; // Ne peut pas supprimer si utilisé

            _context.TypeDeCourants.Remove(typeDeCourant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int idTypeDeCourant)
        {
            return await _context.TypeDeCourants
                .AnyAsync(tc => tc.IdTypeDeCourant == idTypeDeCourant);
        }

        public async Task<IEnumerable<TypeDeCourant>> GetActifsAsync()
        {
            return await _context.TypeDeCourants
                .Where(tc => tc.Statut == true)
                .OrderBy(tc => tc.Libelle)
                .ToListAsync();
        }

        public async Task<TypeDeCourant?> GetByLibelleAsync(string libelle)
        {
            return await _context.TypeDeCourants
                .FirstOrDefaultAsync(tc => tc.Libelle == libelle);
        }
    }
}
