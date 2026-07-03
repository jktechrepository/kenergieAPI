using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class ClientUsageService : IClientUsageRepository
    {
        private readonly KenergieDbContext _context;

        public ClientUsageService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClientUsage>> GetAllAsync()
        {
            return await _context.ClientUsages
                .Include(cu => cu.Client)
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(cu => cu.Statut == true)
                .OrderBy(cu => cu.DateAttribution)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientUsage>> GetByClientAsync(int idClient)
        {
            return await _context.ClientUsages
                .Include(cu => cu.Client)
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(cu => cu.IdClient == idClient && cu.Statut == true)
                .OrderBy(cu => cu.DateAttribution)
                .ToListAsync();
        }

        public async Task<IEnumerable<ClientUsage>> GetByUsageAsync(int idUsage)
        {
            return await _context.ClientUsages
                .Include(cu => cu.Client)
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(cu => cu.IdUsage == idUsage && cu.Statut == true)
                .OrderBy(cu => cu.DateAttribution)
                .ToListAsync();
        }

        public async Task<ClientUsage> GetByIdAsync(int id)
        {
            return await _context.ClientUsages
                .Include(cu => cu.Client)
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(cu => cu.Statut == true)
                .FirstOrDefaultAsync(cu => cu.IdClientUsage == id);
        }

        public async Task<ClientUsage> GetByClientAndUsageAsync(int idClient, int idUsage)
        {
            return await _context.ClientUsages
                .Include(cu => cu.Client)
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(cu => cu.Statut == true)
                .FirstOrDefaultAsync(cu => cu.IdClient == idClient && cu.IdUsage == idUsage);
        }

        public async Task<ClientUsage> CreateAsync(ClientUsage clientUsage)
        {
            // Vérifier que la relation n'existe pas déjà
            var exists = await _context.ClientUsages
                .AnyAsync(cu => cu.IdClient == clientUsage.IdClient && cu.IdUsage == clientUsage.IdUsage);

            if (exists)
            {
                throw new InvalidOperationException("Cette relation Client-Usage existe déjà.");
            }

            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(clientUsage.IdClient);
            if (client == null)
            {
                throw new InvalidOperationException("Le client spécifié n'existe pas.");
            }

            // Vérifier que l'usage existe
            var usage = await _context.Usages.FindAsync(clientUsage.IdUsage);
            if (usage == null)
            {
                throw new InvalidOperationException("L'usage spécifié n'existe pas.");
            }

            clientUsage.DateAttribution = DateTime.Now;
            if (clientUsage.nombreBatiment <= 0)
            {
                clientUsage.nombreBatiment = 1; // Valeur par défaut
            }
            if (!clientUsage.Statut)
            {
                clientUsage.Statut = true; // Valeur par défaut
            }

            _context.ClientUsages.Add(clientUsage);
            await _context.SaveChangesAsync();
            return clientUsage;
        }

        public async Task<ClientUsage> UpdateAsync(ClientUsage clientUsage)
        {
            var existing = await _context.ClientUsages.FindAsync(clientUsage.IdClientUsage);
            if (existing == null)
                return null;

            // Vérifier que nombreBatiment est valide
            if (clientUsage.nombreBatiment <= 0)
            {
                throw new InvalidOperationException("Le nombre de bâtiments doit être supérieur à 0.");
            }

            // Mettre à jour uniquement les champs modifiables (nombreBatiment)
            existing.nombreBatiment = clientUsage.nombreBatiment;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var clientUsage = await _context.ClientUsages.FindAsync(id);
            if (clientUsage == null)
                return false;

            // Vérifier si des factures sont liées à cette relation
            // (via l'usage du client)
            var hasFactures = await _context.Factures
                .AnyAsync(f => f.IdUsage == clientUsage.IdUsage);

            if (hasFactures)
            {
                // ✨ Soft delete : mettre Statut à false au lieu de supprimer
                clientUsage.Statut = false;
                await _context.SaveChangesAsync();
                return true;
            }

            // Si pas de factures, hard delete (suppression complète)
            _context.ClientUsages.Remove(clientUsage);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.ClientUsages.AnyAsync(cu => cu.IdClientUsage == id);
        }

        public async Task<bool> ExistsByClientAndUsageAsync(int idClient, int idUsage)
        {
            return await _context.ClientUsages
                .AnyAsync(cu => cu.IdClient == idClient && cu.IdUsage == idUsage);
        }
    }
}
