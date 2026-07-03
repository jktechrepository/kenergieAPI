using Kenergie.Models;
using Kenergie.Models.DTOs.Pagination;

namespace Kenergie.Services.Repositories
{
    public interface IFactureRepository
    {
        Task<IEnumerable<Facture>> GetAllAsync();
        Task<IEnumerable<Facture>> GetByCategorieAsync(int idCategorie);
        Task<IEnumerable<Facture>> GetBySocieteAsync(int idSociete);
        Task<PagedResult<Facture>> GetBySocietePagedAsync(int idSociete, PagedRequest request);
        Task<IEnumerable<Facture>> GetByMoisAnneeAsync(int mois, int annee);
        Task<IEnumerable<Facture>> GetByCategorieMoisAnneeAsync(int idCategorie, int mois, int annee);
        Task<Facture> GetByIdAsync(int id);
        Task<Facture> GetByNumeroFactureAsync(string numeroFacture);
        Task<Facture?> ResolveFactureBySearchTermAsync(string searchTerm);
        Task<Facture> CreateAsync(Facture facture);
        Task<Facture> UpdateAsync(Facture facture);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByNumeroFactureAsync(string numeroFacture);
        Task<string> GenerateNumeroFactureAsync(int? idUsage, DateTime? dateEmission);
        Task<string> GenerateNumeroFactureAsync(int? idUsage, DateTime? dateEmission, int? idTypeDeCourant);
        Task<bool> ToggleStatutAsync(int id);
        Task<bool> SetStatutAsync(int id, bool statut);
        Task<PagedResult<Facture>> GetPagedAsync(PagedRequest request);
        Task<Paiement> EnregistrerPaiementAsync(int idFacture, Paiement paiement);
        Task<IEnumerable<Paiement>> GetPaiementsByFactureAsync(int idFacture);
    }
}

