using Kenergie.Models.DTOs.Depense;

namespace Kenergie.Services.Repositories
{
    public interface ICategorieDepenseRepository
    {
        Task<IEnumerable<CategorieDepenseResponseDto>> GetBySocieteAsync(
            int idSociete,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<CategorieDepenseResponseDto?> GetByIdAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<CategorieDepenseResponseDto> CreateAsync(
            CreateCategorieDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId);

        Task<CategorieDepenseResponseDto?> UpdateAsync(
            int id,
            UpdateCategorieDepenseDto dto,
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
