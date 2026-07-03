using Kenergie.Models;
using Kenergie.Services.Repositories;

namespace Kenergie.Services.Repositories
{
    /// <summary>
    /// Interface pour le repository TypeDeCourant
    /// </summary>
    public interface ITypeDeCourantRepository
    {
        // CRUD de base
        Task<IEnumerable<TypeDeCourant>> GetAllAsync();
        Task<TypeDeCourant?> GetByIdAsync(int idTypeDeCourant);
        Task<TypeDeCourant> CreateAsync(TypeDeCourant typeDeCourant);
        Task<TypeDeCourant?> UpdateAsync(TypeDeCourant typeDeCourant);
        Task<bool> DeleteAsync(int idTypeDeCourant);
        Task<bool> ExistsAsync(int idTypeDeCourant);

        // Requêtes spécifiques
        Task<IEnumerable<TypeDeCourant>> GetActifsAsync();
        Task<TypeDeCourant?> GetByLibelleAsync(string libelle);
    }
}
