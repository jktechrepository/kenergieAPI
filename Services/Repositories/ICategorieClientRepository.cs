using Kenergie.Models;

namespace Kenergie.Services.Repositories
{
    public interface ICategorieClientRepository
    {
        Task<IEnumerable<CategorieClient>> GetAllAsync();
        Task<IEnumerable<CategorieClient>> GetBySocieteAsync(int idSociete);
        Task<CategorieClient> GetByIdAsync(int id);
        Task<CategorieClient> GetByNomAsync(string nom, int idSociete);
        Task<CategorieClient> CreateAsync(CategorieClient categorieClient);
        Task<CategorieClient> UpdateAsync(CategorieClient categorieClient);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNomAsync(string nom, int idSociete);
        Task<bool> ToggleStatutAsync(int id);
        Task<bool> SetStatutAsync(int id, bool statut);
    }
}

