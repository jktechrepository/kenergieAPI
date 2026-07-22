using Kenergie.Data;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class DeviseConversionService : IDeviseConversionService
    {
        private readonly KenergieDbContext _context;

        public DeviseConversionService(KenergieDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetCodeDevisePrincipaleAsync(int idSociete)
        {
            var code = await _context.Societes
                .Where(s => s.IdSociete == idSociete)
                .Select(s => s.CodeDevisePrincipale)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException($"Aucune devise principale définie pour la société {idSociete}.");

            return NormalizeCode(code);
        }

        public async Task<decimal?> GetDernierTauxAsync(int idSociete, string codeDeviseSource, string codeDeviseCible, DateTime date)
        {
            var source = NormalizeCode(codeDeviseSource);
            var cible = NormalizeCode(codeDeviseCible);

            if (source == cible)
                return 1m;

            return await _context.TauxChanges
                .Where(t => t.IdSociete == idSociete
                    && t.CodeDeviseSource == source
                    && t.CodeDeviseCible == cible
                    && t.DateEffet <= date)
                .OrderByDescending(t => t.DateEffet)
                .Select(t => (decimal?)t.Taux)
                .FirstOrDefaultAsync();
        }

        public async Task<ConversionResult> ConvertirAsync(int idSociete, string codeDeviseSource, string codeDeviseCible, decimal montant, DateTime date)
        {
            var source = NormalizeCode(codeDeviseSource);
            var cible = NormalizeCode(codeDeviseCible);

            var taux = await GetDernierTauxAsync(idSociete, source, cible, date);
            if (!taux.HasValue)
            {
                throw new InvalidOperationException(
                    $"Aucun taux de change trouvé pour {source} → {cible} (société {idSociete}) à la date {date:O}.");
            }

            var montantConverti = Math.Round(montant * taux.Value, 2, MidpointRounding.AwayFromZero);

            return new ConversionResult
            {
                CodeDeviseSource = source,
                CodeDeviseCible = cible,
                Taux = taux.Value,
                MontantSource = montant,
                MontantConverti = montantConverti,
                DateReference = date
            };
        }

        public async Task<ConversionResult> ConvertirVersPrincipaleAsync(int idSociete, string codeDeviseSource, decimal montant, DateTime date)
        {
            var principale = await GetCodeDevisePrincipaleAsync(idSociete);
            return await ConvertirAsync(idSociete, codeDeviseSource, principale, montant, date);
        }

        public static string NormalizeCode(string code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }
    }
}
