using Kenergie.Models;

namespace Kenergie.Services.Repositories
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<IEnumerable<Role>> GetAllAsync(string nomRole);
        /// <summary>
        /// Rôles visibles pour un appelant de niveau hiérarchique donné :
        /// actifs, hors Client, avec Niveau &gt;= callerNiveau.
        /// </summary>
        Task<IEnumerable<Role>> GetVisibleForCallerAsync(int callerNiveau);
        Task<Role> GetByIdAsync(int id);
        Task<Role> GetByNomAsync(string nom);
        Task<Role> CreateAsync(Role role);
        Task<Role> UpdateAsync(Role role);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNomAsync(string nom);
        Task<IEnumerable<Utilisateur>> GetUtilisateursAsync(int idRole);
        
        // ✅ SOFT DELETE
        Task<bool> ToggleStatutAsync(int id);
    }
}
