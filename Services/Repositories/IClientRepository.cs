using Kenergie.Models;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Models.DTOs;

namespace Kenergie.Services.Repositories
{
    public interface IClientRepository
    {
        Task<IEnumerable<Client>> GetAllAsync();
        Task<IEnumerable<Client>> GetByCategorieAsync(int idCategorie);
        Task<IEnumerable<Client>> GetBySocieteAsync(int idSociete);
        Task<IEnumerable<Client>> GetByTypeDeCourantAsync(int idTypeDeCourant);
        Task<IEnumerable<Client>> GetBySocieteAndSearchAsync(int idSociete, string searchTerm, bool includeInactive = false);
        Task<PagedResult<Client>> GetBySocietePagedAsync(int idSociete, ClientPagedSearchRequestDto request);
        Task<Client> GetByIdAsync(int id);
        Task<IEnumerable<Client>> GetByNomAsync(string nom);
        Task<IEnumerable<Client>> GetByIsActifAsync(bool IsActif);
        Task<Client?> GetByCodeConsAsync(string codeCons);
        Task<Client> CreateAsync(Client client);
        Task<Client> CreateWithUsagesAsync(Client client, List<(string LibelleUsage, int nombreBatiment, int? IdTypeDeCourant)> usages);
        Task<Client> UpdateAsync(Client client);
        Task<Client> UpdateWithUsagesAsync(int idClient, Client client, List<(string LibelleUsage, int nombreBatiment, bool Statut, int? IdTypeDeCourant)>? usages);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ToggleStatutAsync(int id);
        
        Task<bool> ToggleIsActifAsync(int id);
        Task<bool> SetStatutAsync(int id, bool statut);
        Task<PagedResult<Client>> GetPagedAsync(PagedRequest request);
        
        // Méthodes pour gérer les usages (remplace les méthodes de catégories)
        Task<bool> AddUsageToClientAsync(int idClient, int idUsage, int nombreBatiment = 1, int? idTypeDeCourant = null);
        Task<bool> RemoveUsageFromClientAsync(int idClient, int idUsage);
        Task<IEnumerable<Usage>> GetClientUsagesAsync(int idClient);
        Task<IEnumerable<ClientUsage>> GetClientUsagesWithDetailsAsync(int idClient);
    }
}

