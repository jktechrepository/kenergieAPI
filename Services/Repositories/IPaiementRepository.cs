using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Models.DTOs.Paiement;

namespace Kenergie.Services.Repositories
{
    public interface IPaiementRepository
    {
        Task<IEnumerable<Paiement>> GetAllAsync();
        Task<Paiement?> GetByIdAsync(int id);
        Task<IEnumerable<Paiement>> GetByFactureAsync(int idFacture);
        Task<IEnumerable<Paiement>> GetByClientAsync(int idClient);
        Task<IEnumerable<Paiement>> GetBySocieteAsync(int idSociete);
        Task<PagedResult<Paiement>> GetBySocietePagedAsync(int idSociete, PagedRequest request);
        Task<PagedResultPaiement> GetBySocietePagedWithFiltersAsync(int idSociete, PaiementPagedRequest request);
        Task<IEnumerable<FactureImpayeeDto>> GetFacturesImpayeesBySocieteAsync(int idSociete);
        Task<PagedResult<FactureImpayeeDto>> GetFacturesImpayeesBySocietePagedAsync(int idSociete, PagedRequest request);
        Task<Paiement> CreateAsync(Paiement paiement);
        Task<Paiement?> UpdateAsync(Paiement paiement);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<PagedResult<Paiement>> GetPagedAsync(PagedRequest request);
        Task<decimal> GetTotalPaiementsByFactureAsync(int idFacture);
    }
}

