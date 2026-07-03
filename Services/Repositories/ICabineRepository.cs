using Kenergie.Models;

namespace Kenergie.Services.Repositories
{
    public interface ICabineRepository
    {
        Task<IEnumerable<Cabine>> GetAllAsync();
        Task<IEnumerable<Cabine>> GetBySocieteAsync(int idSociete);
        Task<Cabine> GetByIdAsync(int id);
        Task<Cabine> GetByNomAsync(string nom, int idSociete);
        Task<Cabine> CreateAsync(Cabine cabine);
        Task<Cabine> UpdateAsync(Cabine cabine);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNomAsync(string nom, int idSociete);
    }
}
