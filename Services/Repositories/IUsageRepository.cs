using Kenergie.Models;

namespace Kenergie.Services.Repositories
{
    public interface IUsageRepository
    {
        Task<IEnumerable<Usage>> GetAllAsync();
        Task<IEnumerable<Usage>> GetByCategorieClientAsync(int idCategorieClient);
        Task<IEnumerable<Usage>> GetBySocieteAsync(int idSociete);
        Task<Usage> GetByIdAsync(int id);
        Task<Usage> GetByLibelleAsync(string libelle, int idCategorieClient);
        Task<Usage> CreateAsync(Usage usage);
        Task<Usage> UpdateAsync(Usage usage);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByLibelleAsync(string libelle, int idCategorieClient);
    }
}
