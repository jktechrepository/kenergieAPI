using Kenergie.Models.DTOs.Devise;

namespace Kenergie.Services.Repositories
{
    public interface IDeviseRepository
    {
        Task<IEnumerable<DeviseDto>> GetDevisesActivesAsync(int? idSocieteFilter);
        Task<DeviseDto?> GetDeviseByIdAsync(int idDeviseMonetaire);
        Task<DeviseDto> CreateDeviseAsync(CreateDeviseDto dto);
        Task<DeviseDto> UpdateDeviseAsync(int idDeviseMonetaire, UpdateDeviseDto dto);
        Task SetDevisePrincipaleAsync(int idSociete, string codeDevise);
        Task<TauxChangeDto> CreateTauxChangeAsync(CreateTauxChangeDto dto);
        Task<IEnumerable<TauxChangeDto>> GetTauxChangesAsync(int? idSociete, string? source, string? cible);
        Task<PreviewConversionDto> PreviewConversionAsync(int idSociete, string codeDeviseSource, decimal montant, DateTime datePaiement);
        Task EnsureDevisePrincipaleCdfAsync(int idSociete);
    }
}
