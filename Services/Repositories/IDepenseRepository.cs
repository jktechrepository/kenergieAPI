using Kenergie.Models;
using Kenergie.Models.DTOs.Depense;
using Kenergie.Models.DTOs.Pagination;

namespace Kenergie.Services.Repositories
{
    public interface IDepenseRepository
    {
        Task<PagedResult<DepenseResponseDto>> GetPagedAsync(
            PagedRequest request,
            int? idSociete,
            int callerUserId,
            string callerRole,
            int callerSocieteId,
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            int? idCategorieDepense = null,
            string? statut = null);

        Task<DepenseResponseDto?> GetByIdAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<DepenseMoisResponseDto> GetByMoisAsync(
            int mois,
            int annee,
            int? idSociete,
            int callerUserId,
            string callerRole,
            int callerSocieteId,
            string? statut = null);

        Task<DepenseResponseDto> CreateAsync(
            CreateDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<DepenseResponseDto?> UpdateAsync(
            int id,
            UpdateDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<DepenseResponseDto?> AnnulerAsync(
            int id,
            AnnulerDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<DepenseResponseDto?> ValiderAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<DepenseResponseDto?> RefuserAsync(
            int id,
            AnnulerDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<bool> DeleteAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId);
    }
}
