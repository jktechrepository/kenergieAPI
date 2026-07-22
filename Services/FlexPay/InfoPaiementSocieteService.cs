using Kenergie.Data;
using Kenergie.Helpers;
using Kenergie.Models;
using Kenergie.Models.DTOs.FlexPay;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services.FlexPay
{
    public interface IInfoPaiementSocieteService
    {
        Task<IEnumerable<InfoPaiementSocieteDto>> GetAllAsync(int? idSocieteFilter);
        Task<InfoPaiementSocieteDto?> GetByIdAsync(int id);
        Task<InfoPaiementSociete?> GetActiveEntityForSocieteAsync(int idSociete);
        Task<InfoPaiementSocieteDto> CreateAsync(CreateInfoPaiementSocieteDto dto);
        Task<InfoPaiementSocieteDto> UpdateAsync(int id, UpdateInfoPaiementSocieteDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class InfoPaiementSocieteService : IInfoPaiementSocieteService
    {
        private readonly KenergieDbContext _context;

        public InfoPaiementSocieteService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<InfoPaiementSocieteDto>> GetAllAsync(int? idSocieteFilter)
        {
            var q = _context.InfosPaiementSociete.AsNoTracking().AsQueryable();
            if (idSocieteFilter.HasValue)
                q = q.Where(x => x.IdSociete == idSocieteFilter.Value);
            var list = await q.OrderByDescending(x => x.DateCreation).ToListAsync();
            return list.Select(Map);
        }

        public async Task<InfoPaiementSocieteDto?> GetByIdAsync(int id)
        {
            var entity = await _context.InfosPaiementSociete.AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdInfoPaiementSociete == id);
            return entity == null ? null : Map(entity);
        }

        public async Task<InfoPaiementSociete?> GetActiveEntityForSocieteAsync(int idSociete)
        {
            return await _context.InfosPaiementSociete
                .Where(x => x.IdSociete == idSociete && x.Statut)
                .OrderByDescending(x => x.DateModification ?? x.DateCreation)
                .FirstOrDefaultAsync();
        }

        public async Task<InfoPaiementSocieteDto> CreateAsync(CreateInfoPaiementSocieteDto dto)
        {
            var societeExists = await _context.Societes.AnyAsync(s => s.IdSociete == dto.IdSociete);
            if (!societeExists)
                throw new KeyNotFoundException("Société introuvable.");

            var entity = new InfoPaiementSociete
            {
                IdSociete = dto.IdSociete,
                CodeMarchand = dto.CodeMarchand.Trim(),
                ApiToken = FlexPayTokenHelper.Normalize(dto.ApiToken),
                ActifMobileMoney = dto.ActifMobileMoney,
                ActifCarteBancaire = dto.ActifCarteBancaire,
                Statut = dto.Statut,
                DateCreation = DateTime.UtcNow
            };
            _context.InfosPaiementSociete.Add(entity);
            await _context.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<InfoPaiementSocieteDto> UpdateAsync(int id, UpdateInfoPaiementSocieteDto dto)
        {
            var entity = await _context.InfosPaiementSociete.FindAsync(id)
                ?? throw new KeyNotFoundException("Configuration marchand introuvable.");

            entity.CodeMarchand = dto.CodeMarchand.Trim();
            if (!string.IsNullOrWhiteSpace(dto.ApiToken))
                entity.ApiToken = FlexPayTokenHelper.Normalize(dto.ApiToken);
            entity.ActifMobileMoney = dto.ActifMobileMoney;
            entity.ActifCarteBancaire = dto.ActifCarteBancaire;
            entity.Statut = dto.Statut;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.InfosPaiementSociete.FindAsync(id);
            if (entity == null) return false;
            entity.Statut = false;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private static InfoPaiementSocieteDto Map(InfoPaiementSociete e) => new()
        {
            IdInfoPaiementSociete = e.IdInfoPaiementSociete,
            IdSociete = e.IdSociete,
            CodeMarchand = e.CodeMarchand,
            HasApiToken = !string.IsNullOrWhiteSpace(e.ApiToken),
            ActifMobileMoney = e.ActifMobileMoney,
            ActifCarteBancaire = e.ActifCarteBancaire,
            Statut = e.Statut,
            DateCreation = e.DateCreation,
            DateModification = e.DateModification
        };
    }
}
