using Kenergie.Models;

namespace Kenergie.Services.Repositories
{
    public interface IClientUsageRepository
    {
        Task<IEnumerable<ClientUsage>> GetAllAsync();
        Task<IEnumerable<ClientUsage>> GetByClientAsync(int idClient);
        Task<IEnumerable<ClientUsage>> GetByUsageAsync(int idUsage);
        Task<ClientUsage> GetByIdAsync(int id);
        Task<ClientUsage> GetByClientAndUsageAsync(int idClient, int idUsage);
        Task<ClientUsage> CreateAsync(ClientUsage clientUsage);
        Task<ClientUsage> UpdateAsync(ClientUsage clientUsage);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByClientAndUsageAsync(int idClient, int idUsage);
    }
}
