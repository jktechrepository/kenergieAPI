using Kenergie.Models;

namespace Kenergie.Services.Repositories
{
    public interface IAxeRepository
    {
        Task<IEnumerable<Axe>> GetAllAsync();
        Task<IEnumerable<Axe>> GetByCabineAsync(int idCabine);
        Task<IEnumerable<Axe>> GetBySocieteAsync(int idSociete);
        Task<Axe> GetByIdAsync(int id);
        Task<Axe> GetByNomAsync(string nomAxe, int idCabine);
        Task<Axe> CreateAsync(Axe axe);
        Task<Axe> UpdateAsync(Axe axe);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNomAsync(string nomAxe, int idCabine);
    }
}
