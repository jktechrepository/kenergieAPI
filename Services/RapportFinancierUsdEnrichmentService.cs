using Kenergie.Data;
using Kenergie.Models.DTOs.Devise;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class RapportFinancierUsdEnrichmentService : IRapportFinancierUsdEnrichmentService
    {
        public const string CodeDeviseUsd = "USD";

        private readonly KenergieDbContext _context;
        private readonly IDeviseConversionService _deviseConversion;
        private readonly ILogger<RapportFinancierUsdEnrichmentService> _logger;

        public RapportFinancierUsdEnrichmentService(
            KenergieDbContext context,
            IDeviseConversionService deviseConversion,
            ILogger<RapportFinancierUsdEnrichmentService> logger)
        {
            _context = context;
            _deviseConversion = deviseConversion;
            _logger = logger;
        }

        public async Task<EquivalentUsdDto> BuildEquivalentUsdAsync(int idSociete, decimal montant, DateTime? date = null)
        {
            var refDate = date ?? DateTime.UtcNow;

            if (montant == 0)
            {
                return new EquivalentUsdDto
                {
                    MontantEquivalentUsd = 0,
                    TauxVersUsd = 1,
                    DateTaux = refDate,
                    ConversionUsdDisponible = true
                };
            }

            try
            {
                var codePrincipale = await ResolveCodeDevisePrincipaleAsync(idSociete);

                if (codePrincipale == CodeDeviseUsd)
                {
                    return new EquivalentUsdDto
                    {
                        MontantEquivalentUsd = Math.Round(montant, 2, MidpointRounding.AwayFromZero),
                        TauxVersUsd = 1,
                        DateTaux = refDate,
                        ConversionUsdDisponible = true
                    };
                }

                var usdActif = await _context.DevisesMonetaires
                    .AnyAsync(d => d.IdSociete == idSociete
                        && d.CodeDevise == CodeDeviseUsd
                        && d.Statut);

                if (!usdActif)
                {
                    return Unavailable();
                }

                var taux = await _deviseConversion.GetDernierTauxAsync(
                    idSociete, codePrincipale, CodeDeviseUsd, refDate);

                if (!taux.HasValue)
                {
                    return Unavailable();
                }

                return new EquivalentUsdDto
                {
                    MontantEquivalentUsd = Math.Round(montant * taux.Value, 2, MidpointRounding.AwayFromZero),
                    TauxVersUsd = taux.Value,
                    DateTaux = refDate,
                    ConversionUsdDisponible = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Conversion USD indisponible pour société {SocieteId}, montant {Montant}",
                    idSociete, montant);
                return Unavailable();
            }
        }

        public async Task<EquivalentUsdDto> SumEquivalentUsdAsync(
            IReadOnlyList<(int IdSociete, decimal Montant)> items,
            DateTime? date = null)
        {
            if (items == null || items.Count == 0)
            {
                return Unavailable();
            }

            var refDate = date ?? DateTime.UtcNow;
            decimal sum = 0;
            decimal? singleTaux = null;
            var societeCount = 0;

            foreach (var (idSociete, montant) in items)
            {
                if (montant == 0)
                    continue;

                var converted = await BuildEquivalentUsdAsync(idSociete, montant, refDate);
                if (!converted.ConversionUsdDisponible)
                {
                    return Unavailable();
                }

                sum += converted.MontantEquivalentUsd ?? 0;
                singleTaux = converted.TauxVersUsd;
                societeCount++;
            }

            if (societeCount == 0)
            {
                return new EquivalentUsdDto
                {
                    MontantEquivalentUsd = 0,
                    TauxVersUsd = 1,
                    DateTaux = refDate,
                    ConversionUsdDisponible = true
                };
            }

            return new EquivalentUsdDto
            {
                MontantEquivalentUsd = Math.Round(sum, 2, MidpointRounding.AwayFromZero),
                TauxVersUsd = societeCount == 1 ? singleTaux : null,
                DateTaux = refDate,
                ConversionUsdDisponible = true
            };
        }

        public async Task<StatistiquesGeneralesSyntheseUsdDto> BuildStatistiquesGeneralesSyntheseUsdAsync(
            int idSociete,
            decimal totalArrieres,
            decimal totalPaiements,
            DateTime? date = null)
        {
            return new StatistiquesGeneralesSyntheseUsdDto
            {
                TotalArrieres = await BuildEquivalentUsdAsync(idSociete, totalArrieres, date),
                TotalPaiements = await BuildEquivalentUsdAsync(idSociete, totalPaiements, date)
            };
        }

        public async Task<StatistiquesFinancieresSyntheseUsdDto> BuildStatistiquesFinancieresSyntheseUsdAsync(
            int idSociete,
            decimal chiffreAffaires,
            decimal montantArrieres,
            decimal montantPaye,
            decimal montantDu,
            DateTime? date = null)
        {
            return new StatistiquesFinancieresSyntheseUsdDto
            {
                ChiffreAffaires = await BuildEquivalentUsdAsync(idSociete, chiffreAffaires, date),
                MontantArrieres = await BuildEquivalentUsdAsync(idSociete, montantArrieres, date),
                MontantPaye = await BuildEquivalentUsdAsync(idSociete, montantPaye, date),
                MontantDu = await BuildEquivalentUsdAsync(idSociete, montantDu, date)
            };
        }

        public async Task<GlobalFinancierSyntheseUsdDto> BuildGlobalFinancierSyntheseUsdAsync(
            IReadOnlyList<(int IdSociete, decimal ChiffreAffaires, decimal MontantEncaisse, decimal MontantArrieres, decimal TotalGeneralArriere, decimal ChiffreAffairesJournalier)> items,
            IReadOnlyList<(int IdSociete, decimal Montant)>? depensesMoisItems = null,
            IReadOnlyList<(int IdSociete, decimal Montant)>? facturesMoisPrecedentItems = null,
            DateTime? date = null)
        {
            return new GlobalFinancierSyntheseUsdDto
            {
                ChiffreAffairesTotal = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.ChiffreAffaires)).ToList(), date),
                MontantTotalEncaisse = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.MontantEncaisse)).ToList(), date),
                MontantTotalArrieres = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.MontantArrieres)).ToList(), date),
                TotalGeneralArriere = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.TotalGeneralArriere)).ToList(), date),
                ChiffreAffairesJournalier = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.ChiffreAffairesJournalier)).ToList(), date),
                MontantTotalDepensesMois = depensesMoisItems != null
                    ? await SumEquivalentUsdAsync(depensesMoisItems, date)
                    : null,
                MontantTotalFacturesMoisPrecedent = facturesMoisPrecedentItems != null
                    ? await SumEquivalentUsdAsync(facturesMoisPrecedentItems, date)
                    : null
            };
        }

        public async Task<GlobalStatistiquesSyntheseUsdDto> BuildGlobalStatistiquesSyntheseUsdAsync(
            IReadOnlyList<(int IdSociete, decimal ChiffreAffaires, decimal MontantArrieres, decimal MontantPaiements)> items,
            DateTime? date = null)
        {
            return new GlobalStatistiquesSyntheseUsdDto
            {
                ChiffreAffairesGlobal = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.ChiffreAffaires)).ToList(), date),
                MontantTotalArrieresGlobal = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.MontantArrieres)).ToList(), date),
                MontantTotalPaiementsGlobal = await SumEquivalentUsdAsync(
                    items.Select(i => (i.IdSociete, i.MontantPaiements)).ToList(), date)
            };
        }

        public async Task<SocieteStatistiquesSyntheseUsdDto> BuildSocieteStatistiquesSyntheseUsdAsync(
            int idSociete,
            decimal chiffreAffairesMois,
            decimal montantTotalArrieres,
            decimal montantDepensesMois,
            decimal montantTotalFacturesMoisPrecedent,
            DateTime? date = null)
        {
            return new SocieteStatistiquesSyntheseUsdDto
            {
                ChiffreAffairesMois = await BuildEquivalentUsdAsync(idSociete, chiffreAffairesMois, date),
                MontantTotalArrieres = await BuildEquivalentUsdAsync(idSociete, montantTotalArrieres, date),
                MontantDepensesMois = await BuildEquivalentUsdAsync(idSociete, montantDepensesMois, date),
                MontantTotalFacturesMoisPrecedent = await BuildEquivalentUsdAsync(
                    idSociete, montantTotalFacturesMoisPrecedent, date)
            };
        }

        public async Task<DashboardSyntheseUsdDto> BuildDashboardSyntheseUsdAsync(
            int idSociete,
            decimal paiementsDuMois,
            decimal totalGeneralArriere,
            decimal montantTotalFacturesMoisPrecedent,
            decimal montantTotalDepensesMois,
            DateTime? date = null)
        {
            return new DashboardSyntheseUsdDto
            {
                PaiementsDuMois = await BuildEquivalentUsdAsync(idSociete, paiementsDuMois, date),
                TotalGeneralArriere = await BuildEquivalentUsdAsync(idSociete, totalGeneralArriere, date),
                MontantTotalFacturesMoisPrecedent = await BuildEquivalentUsdAsync(
                    idSociete, montantTotalFacturesMoisPrecedent, date),
                MontantTotalDepensesMois = await BuildEquivalentUsdAsync(
                    idSociete, montantTotalDepensesMois, date)
            };
        }

        private async Task<string> ResolveCodeDevisePrincipaleAsync(int idSociete)
        {
            var code = await _context.Societes
                .Where(s => s.IdSociete == idSociete)
                .Select(s => s.CodeDevisePrincipale)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(code))
            {
                return "CDF";
            }

            return DeviseConversionService.NormalizeCode(code);
        }

        private static EquivalentUsdDto Unavailable()
        {
            return new EquivalentUsdDto { ConversionUsdDisponible = false };
        }
    }
}
