using Kenergie.Data;
using Kenergie.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    /// <summary>
    /// Service pour migrer les factures existantes vers ClientFactures
    /// </summary>
    public class ClientFactureMigrationService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<ClientFactureMigrationService> _logger;

        public ClientFactureMigrationService(
            KenergieDbContext context,
            ILogger<ClientFactureMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Migre toutes les factures existantes vers ClientFactures
        /// </summary>
        public async Task<MigrationResult> MigrateExistingFacturesAsync()
        {
            var result = new MigrationResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("🚀 Début de la migration des factures existantes vers ClientFactures");

                // Récupérer toutes les factures actives
                var factures = await _context.Factures
                    .Include(f => f.Usage)
                    .Where(f => f.Statut == true)
                    .ToListAsync();

                result.TotalFactures = factures.Count;
                _logger.LogInformation("📊 {Count} facture(s) active(s) trouvée(s)", factures.Count);

                var clientFacturesCreated = 0;
                var errors = new List<string>();

                foreach (var facture in factures)
                {
                    try
                    {
                        // Récupérer tous les clients ayant cet usage
                        var clientUsages = await _context.ClientUsages
                            .Include(cu => cu.Client)
                            .Where(cu => cu.IdUsage == facture.IdUsage &&
                                        cu.Statut == true &&
                                        cu.Client != null &&
                                        cu.Client.Statut == true)
                            .ToListAsync();

                        foreach (var clientUsage in clientUsages)
                        {
                            // Vérifier si la ClientFacture existe déjà
                            var exists = await _context.ClientFactures
                                .AnyAsync(cf => cf.IdFacture == facture.IdFacture &&
                                               cf.IdClient == clientUsage.IdClient &&
                                               cf.Statut == true);

                            if (exists)
                            {
                                result.Skipped++;
                                continue;
                            }

                            // Calculer le montant total pour ce client
                            var nombreBatiment = clientUsage.nombreBatiment > 0 ? clientUsage.nombreBatiment : 1;
                            var montantTotal = (facture.Montant ?? 0) * nombreBatiment;

                            // Calculer le montant payé depuis les paiements
                            var montantPaye = await _context.Paiements
                                .Where(p => p.IdFacture == facture.IdFacture &&
                                           p.IdClient == clientUsage.IdClient &&
                                           p.Statut != null &&
                                           (p.Statut == "Validé" || p.Statut.ToLower() == "true"))
                                .SumAsync(p => p.MontantPaye);

                            // Calculer le montant dû
                            var montantDu = montantTotal - montantPaye;

                            // Créer la ClientFacture
                            var clientFacture = new ClientFacture
                            {
                                IdFacture = facture.IdFacture,
                                IdClient = clientUsage.IdClient,
                                Montant = montantTotal,
                                nombreBatiment = nombreBatiment,
                                MontantPaye = montantPaye,
                                MontantDu = montantDu,
                                Mois = facture.MoisEmission.ToString("D2"),
                                Annees = facture.AnneesEmission,
                                DateEmission = facture.DateEmission ?? DateTime.Now,
                                EstArrierePreExistant = false,
                                Statut = true,
                                DateCreation = DateTime.Now
                            };

                            _context.ClientFactures.Add(clientFacture);
                            clientFacturesCreated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Erreur lors de la migration de la facture {facture.IdFacture}: {ex.Message}";
                        errors.Add(errorMsg);
                        _logger.LogError(ex, "❌ {Error}", errorMsg);
                        result.Errors++;
                    }
                }

                // Sauvegarder toutes les ClientFactures créées
                await _context.SaveChangesAsync();

                result.ClientFacturesCreated = clientFacturesCreated;
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = errors.Count == 0;

                _logger.LogInformation(
                    "✅ Migration terminée: {Created} ClientFacture(s) créée(s), {Skipped} ignorée(s), {Errors} erreur(s) en {Duration}",
                    clientFacturesCreated, result.Skipped, result.Errors, result.Duration);

                if (errors.Any())
                {
                    result.ErrorMessages = errors;
                }

                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.Success = false;
                result.ErrorMessages = new List<string> { ex.Message };
                _logger.LogError(ex, "❌ Erreur fatale lors de la migration");
                return result;
            }
        }

        /// <summary>
        /// Valide la cohérence des données après migration
        /// </summary>
        public async Task<ValidationResult> ValidateMigrationAsync()
        {
            var result = new ValidationResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogInformation("🔍 Début de la validation de la migration");

                // Compter les ClientFactures
                result.TotalClientFactures = await _context.ClientFactures
                    .Where(cf => cf.Statut == true)
                    .CountAsync();

                // Compter les factures actives
                result.TotalFactures = await _context.Factures
                    .Where(f => f.Statut == true)
                    .CountAsync();

                // Compter les factures avec ClientFacture
                result.FacturesWithClientFacture = await _context.Factures
                    .Where(f => f.Statut == true &&
                               _context.ClientFactures.Any(cf => cf.IdFacture == f.IdFacture && cf.Statut == true))
                    .CountAsync();

                // Vérifier la cohérence MontantPaye
                var incoherences = await _context.ClientFactures
                    .Where(cf => cf.IdFacture != null && cf.Statut == true)
                    .Select(cf => new
                    {
                        cf.IdClientFacture,
                        cf.IdFacture,
                        cf.IdClient,
                        cf.MontantPaye,
                        MontantPayeCalcule = _context.Paiements
                            .Where(p => p.IdFacture == cf.IdFacture &&
                                       p.IdClient == cf.IdClient &&
                                       p.Statut != null &&
                                       (p.Statut == "Validé" || p.Statut.ToLower() == "true"))
                            .Sum(p => (decimal?)p.MontantPaye) ?? 0m
                    })
                    .ToListAsync();

                result.IncoherencesMontantPaye = incoherences
                    .Where(x => Math.Abs((x.MontantPaye ?? 0m) - x.MontantPayeCalcule) > 0.01m)
                    .Count();

                // Vérifier la cohérence MontantDu
                var incoherencesMontantDu = await _context.ClientFactures
                    .Where(cf => cf.Montant != null &&
                                cf.MontantPaye != null &&
                                cf.MontantDu != null &&
                                cf.Statut == true)
                    .ToListAsync();

                result.IncoherencesMontantDu = incoherencesMontantDu
                    .Where(cf => Math.Abs(cf.MontantDu.Value - (cf.Montant.Value - cf.MontantPaye.Value)) > 0.01m)
                    .Count();

                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.IsValid = result.IncoherencesMontantPaye == 0 && result.IncoherencesMontantDu == 0;

                _logger.LogInformation(
                    "✅ Validation terminée: {Total} ClientFacture(s), {IncoherencesPaye} incohérence(s) MontantPaye, {IncoherencesDu} incohérence(s) MontantDu",
                    result.TotalClientFactures, result.IncoherencesMontantPaye, result.IncoherencesMontantDu);

                return result;
            }
            catch (Exception ex)
            {
                result.EndTime = DateTime.Now;
                result.Duration = result.EndTime - result.StartTime;
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "❌ Erreur lors de la validation");
                return result;
            }
        }
    }

    /// <summary>
    /// Résultat de la migration
    /// </summary>
    public class MigrationResult
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool Success { get; set; }
        public int TotalFactures { get; set; }
        public int ClientFacturesCreated { get; set; }
        public int Skipped { get; set; }
        public int Errors { get; set; }
        public List<string>? ErrorMessages { get; set; }
    }

    /// <summary>
    /// Résultat de la validation
    /// </summary>
    public class ValidationResult
    {
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public bool IsValid { get; set; }
        public int TotalClientFactures { get; set; }
        public int TotalFactures { get; set; }
        public int FacturesWithClientFacture { get; set; }
        public int IncoherencesMontantPaye { get; set; }
        public int IncoherencesMontantDu { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
