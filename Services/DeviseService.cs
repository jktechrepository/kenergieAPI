using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Devise;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class DeviseService : IDeviseRepository
    {
        private readonly KenergieDbContext _context;
        private readonly IDeviseConversionService _conversionService;

        public DeviseService(KenergieDbContext context, IDeviseConversionService conversionService)
        {
            _context = context;
            _conversionService = conversionService;
        }

        public async Task EnsureDevisePrincipaleCdfAsync(int idSociete)
        {
            var societe = await _context.Societes.FindAsync(idSociete);
            if (societe == null) return;

            if (string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale))
            {
                societe.CodeDevisePrincipale = "CDF";
            }

            var exists = await _context.DevisesMonetaires
                .AnyAsync(d => d.IdSociete == idSociete && d.CodeDevise == societe.CodeDevisePrincipale);

            if (!exists)
            {
                var code = DeviseConversionService.NormalizeCode(societe.CodeDevisePrincipale);
                _context.DevisesMonetaires.Add(new DeviseMonetaire
                {
                    IdSociete = idSociete,
                    CodeDevise = code,
                    Libelle = code == "CDF" ? "Franc congolais" : code,
                    Symbole = code == "CDF" ? "FC" : code,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<DeviseDto>> GetDevisesActivesAsync(int? idSocieteFilter)
        {
            var query = _context.DevisesMonetaires
                .AsNoTracking()
                .Where(d => d.Statut);

            if (idSocieteFilter.HasValue)
                query = query.Where(d => d.IdSociete == idSocieteFilter.Value);

            var devises = await query.OrderBy(d => d.CodeDevise).ToListAsync();
            var societesPrincipales = await _context.Societes
                .AsNoTracking()
                .Where(s => !idSocieteFilter.HasValue || s.IdSociete == idSocieteFilter.Value)
                .Select(s => new { s.IdSociete, s.CodeDevisePrincipale })
                .ToDictionaryAsync(s => s.IdSociete, s => DeviseConversionService.NormalizeCode(s.CodeDevisePrincipale ?? ""));

            return devises.Select(d => MapDevise(d, societesPrincipales.GetValueOrDefault(d.IdSociete)));
        }

        public async Task<DeviseDto?> GetDeviseByIdAsync(int idDeviseMonetaire)
        {
            var devise = await _context.DevisesMonetaires.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IdDeviseMonetaire == idDeviseMonetaire);
            if (devise == null) return null;

            var principale = await _context.Societes
                .Where(s => s.IdSociete == devise.IdSociete)
                .Select(s => s.CodeDevisePrincipale)
                .FirstOrDefaultAsync();

            return MapDevise(devise, DeviseConversionService.NormalizeCode(principale ?? ""));
        }

        public async Task<DeviseDto> CreateDeviseAsync(CreateDeviseDto dto)
        {
            var code = DeviseConversionService.NormalizeCode(dto.CodeDevise);
            if (code.Length != 3)
                throw new ArgumentException("Le code devise doit contenir exactement 3 caractères.");

            if (dto.EstDevisePrincipale && !dto.Statut)
                throw new ArgumentException("Une devise principale doit être active.");

            var societe = await _context.Societes.FindAsync(dto.IdSociete)
                ?? throw new KeyNotFoundException($"Société {dto.IdSociete} introuvable.");

            var exists = await _context.DevisesMonetaires
                .AnyAsync(d => d.IdSociete == dto.IdSociete && d.CodeDevise == code);
            if (exists)
                throw new InvalidOperationException($"La devise {code} existe déjà pour cette société.");

            var devise = new DeviseMonetaire
            {
                IdSociete = dto.IdSociete,
                CodeDevise = code,
                Libelle = dto.Libelle.Trim(),
                Symbole = dto.Symbole?.Trim(),
                Statut = dto.Statut,
                DateCreation = DateTime.UtcNow
            };

            _context.DevisesMonetaires.Add(devise);

            if (dto.EstDevisePrincipale)
            {
                societe.CodeDevisePrincipale = code;
            }
            else if (string.IsNullOrWhiteSpace(societe.CodeDevisePrincipale))
            {
                // Première devise sans flag principal : devenir principale si active
                if (dto.Statut)
                    societe.CodeDevisePrincipale = code;
            }

            await _context.SaveChangesAsync();
            return MapDevise(devise, DeviseConversionService.NormalizeCode(societe.CodeDevisePrincipale ?? ""));
        }

        public async Task<DeviseDto> UpdateDeviseAsync(int idDeviseMonetaire, UpdateDeviseDto dto)
        {
            var devise = await _context.DevisesMonetaires.FindAsync(idDeviseMonetaire)
                ?? throw new KeyNotFoundException("Devise introuvable.");

            var societe = await _context.Societes.FindAsync(devise.IdSociete)
                ?? throw new KeyNotFoundException("Société introuvable.");

            var principale = DeviseConversionService.NormalizeCode(societe.CodeDevisePrincipale ?? "");
            var isCurrentPrincipale = string.Equals(devise.CodeDevise, principale, StringComparison.OrdinalIgnoreCase);

            if (isCurrentPrincipale && !dto.Statut)
                throw new ArgumentException("Impossible de désactiver la devise principale actuelle. Basculez d'abord vers une autre devise.");

            if (dto.EstDevisePrincipale && !dto.Statut)
                throw new ArgumentException("Une devise principale doit être active.");

            devise.Libelle = dto.Libelle.Trim();
            devise.Symbole = dto.Symbole?.Trim();
            devise.Statut = dto.Statut;
            devise.DateModification = DateTime.UtcNow;

            if (dto.EstDevisePrincipale)
            {
                societe.CodeDevisePrincipale = devise.CodeDevise;
            }

            await _context.SaveChangesAsync();
            return MapDevise(devise, DeviseConversionService.NormalizeCode(societe.CodeDevisePrincipale ?? ""));
        }

        public async Task SetDevisePrincipaleAsync(int idSociete, string codeDevise)
        {
            var code = DeviseConversionService.NormalizeCode(codeDevise);
            var societe = await _context.Societes.FindAsync(idSociete)
                ?? throw new KeyNotFoundException("Société introuvable.");

            var devise = await _context.DevisesMonetaires
                .FirstOrDefaultAsync(d => d.IdSociete == idSociete && d.CodeDevise == code)
                ?? throw new KeyNotFoundException($"Devise {code} introuvable pour la société.");

            if (!devise.Statut)
                throw new ArgumentException("La devise principale doit être active.");

            societe.CodeDevisePrincipale = code;
            await _context.SaveChangesAsync();
        }

        public async Task<TauxChangeDto> CreateTauxChangeAsync(CreateTauxChangeDto dto)
        {
            var source = DeviseConversionService.NormalizeCode(dto.CodeDeviseSource);
            var cible = DeviseConversionService.NormalizeCode(dto.CodeDeviseCible);

            if (source == cible)
                throw new ArgumentException("La devise source et la devise cible doivent être différentes.");

            var societeExists = await _context.Societes.AnyAsync(s => s.IdSociete == dto.IdSociete);
            if (!societeExists)
                throw new KeyNotFoundException("Société introuvable.");

            await EnsureDeviseActiveAsync(dto.IdSociete, source);
            await EnsureDeviseActiveAsync(dto.IdSociete, cible);

            var taux = new TauxChange
            {
                IdSociete = dto.IdSociete,
                CodeDeviseSource = source,
                CodeDeviseCible = cible,
                Taux = dto.Taux,
                DateEffet = dto.DateEffet ?? DateTime.UtcNow,
                DateCreation = DateTime.UtcNow
            };

            _context.TauxChanges.Add(taux);
            await _context.SaveChangesAsync();

            return MapTaux(taux);
        }

        public async Task<IEnumerable<TauxChangeDto>> GetTauxChangesAsync(int? idSociete, string? source, string? cible)
        {
            var query = _context.TauxChanges.AsNoTracking();

            if (idSociete.HasValue)
                query = query.Where(t => t.IdSociete == idSociete.Value);

            if (!string.IsNullOrWhiteSpace(source))
            {
                var s = DeviseConversionService.NormalizeCode(source);
                query = query.Where(t => t.CodeDeviseSource == s);
            }

            if (!string.IsNullOrWhiteSpace(cible))
            {
                var c = DeviseConversionService.NormalizeCode(cible);
                query = query.Where(t => t.CodeDeviseCible == c);
            }

            var taux = await query
                .OrderByDescending(t => t.DateEffet)
                .ThenByDescending(t => t.DateCreation)
                .ToListAsync();

            return taux.Select(MapTaux).ToList();
        }

        public async Task<PreviewConversionDto> PreviewConversionAsync(int idSociete, string codeDeviseSource, decimal montant, DateTime datePaiement)
        {
            var conversion = await _conversionService.ConvertirVersPrincipaleAsync(idSociete, codeDeviseSource, montant, datePaiement);
            return new PreviewConversionDto
            {
                IdSociete = idSociete,
                CodeDeviseSource = conversion.CodeDeviseSource,
                CodeDevisePrincipale = conversion.CodeDeviseCible,
                DatePaiement = datePaiement,
                Taux = conversion.Taux,
                MontantSource = conversion.MontantSource,
                MontantConverti = conversion.MontantConverti
            };
        }

        private async Task EnsureDeviseActiveAsync(int idSociete, string code)
        {
            var ok = await _context.DevisesMonetaires
                .AnyAsync(d => d.IdSociete == idSociete && d.CodeDevise == code && d.Statut);
            if (!ok)
                throw new ArgumentException($"La devise {code} est inactive ou inexistante pour cette société.");
        }

        private static DeviseDto MapDevise(DeviseMonetaire d, string principale)
        {
            return new DeviseDto
            {
                IdDeviseMonetaire = d.IdDeviseMonetaire,
                IdSociete = d.IdSociete,
                CodeDevise = d.CodeDevise,
                Libelle = d.Libelle,
                Symbole = d.Symbole,
                Statut = d.Statut,
                EstDevisePrincipale = string.Equals(d.CodeDevise, principale, StringComparison.OrdinalIgnoreCase),
                DateCreation = d.DateCreation,
                DateModification = d.DateModification
            };
        }

        private static TauxChangeDto MapTaux(TauxChange t) => new()
        {
            IdTauxChange = t.IdTauxChange,
            IdSociete = t.IdSociete,
            CodeDeviseSource = t.CodeDeviseSource,
            CodeDeviseCible = t.CodeDeviseCible,
            Taux = t.Taux,
            DateEffet = t.DateEffet,
            DateCreation = t.DateCreation
        };
    }
}
