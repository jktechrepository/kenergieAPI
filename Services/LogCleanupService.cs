using Kenergie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kenergie.Services
{
    /// <summary>
    /// Service d'arrière-plan pour le nettoyage automatique des logs
    /// Stratégie hybride basée sur les niveaux de log :
    /// - Information : 1 jours
    /// - Warning : 3 jours
    /// - Error : 7 jours
    /// - Fatal : Jamais supprimé (conservation illimitée)
    /// </summary>
    public class LogCleanupService : BackgroundService
    {
        private readonly ILogger<LogCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(6); // Toutes les 6 heures

        // Périodes de rétention par niveau
        private readonly TimeSpan _informationRetention = TimeSpan.FromDays(1);   // 1 jours
        private readonly TimeSpan _warningRetention = TimeSpan.FromDays(3);      // 3 jours
        private readonly TimeSpan _errorRetention = TimeSpan.FromDays(7);       // 7 jours
        // Fatal : jamais supprimé

        public LogCleanupService(
            ILogger<LogCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("=== DÉMARRAGE DU SERVICE DE NETTOYAGE DES LOGS ===");
            _logger.LogInformation("Stratégie hybride basée sur les niveaux :");
            _logger.LogInformation("  - Information : {Days} jours", _informationRetention.Days);
            _logger.LogInformation("  - Warning : {Days} jours", _warningRetention.Days);
            _logger.LogInformation("  - Error : {Days} jours", _errorRetention.Days);
            _logger.LogInformation("  - Fatal : Jamais supprimé");
            _logger.LogInformation("Fréquence de nettoyage : {Hours} heures", _cleanupInterval.TotalHours);
            _logger.LogInformation("================================================");

            // Attendre un peu après le démarrage pour ne pas interférer
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("=== DÉBUT DU NETTOYAGE DES LOGS ===");
                    
                    var cleanupResult = await CleanupLogsByLevelAsync();
                    
                    _logger.LogInformation("=== NETTOYAGE TERMINÉ ===");
                    _logger.LogInformation("Total supprimé : {TotalCount} logs", cleanupResult.TotalDeleted);
                    _logger.LogInformation("Espace libéré estimé : {SpaceMB} MB", cleanupResult.EstimatedSpaceMB);
                    
                    // Détail par niveau
                    if (cleanupResult.InformationDeleted > 0)
                        _logger.LogInformation("  - Information : {Count} logs", cleanupResult.InformationDeleted);
                    if (cleanupResult.WarningDeleted > 0)
                        _logger.LogInformation("  - Warning : {Count} logs", cleanupResult.WarningDeleted);
                    if (cleanupResult.ErrorDeleted > 0)
                        _logger.LogInformation("  - Error : {Count} logs", cleanupResult.ErrorDeleted);
                    
                    _logger.LogInformation("Prochain nettoyage dans : {Hours} heures", _cleanupInterval.TotalHours);
                    _logger.LogInformation("=====================================");
                    
                    // Attendre le prochain nettoyage
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("=== ARRÊT DU SERVICE DE NETTOYAGE DES LOGS ===");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "=== ERREUR LORS DU NETTOYAGE DES LOGS ===");
                    
                    // Attendre 1 heure avant de réessayer en cas d'erreur
                    _logger.LogInformation("Nouvelle tentative dans 1 heure...");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task<CleanupResult> CleanupLogsByLevelAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<KenergieDbContext>();
            
            var result = new CleanupResult();
            
            // Dates limites pour chaque niveau
            var informationCutoff = DateTime.UtcNow.Subtract(_informationRetention);
            var warningCutoff = DateTime.UtcNow.Subtract(_warningRetention);
            var errorCutoff = DateTime.UtcNow.Subtract(_errorRetention);

            _logger.LogDebug("Périodes de rétention :");
            _logger.LogDebug("  - Information avant {Date}", informationCutoff.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.LogDebug("  - Warning avant {Date}", warningCutoff.ToString("yyyy-MM-dd HH:mm:ss"));
            _logger.LogDebug("  - Error avant {Date}", errorCutoff.ToString("yyyy-MM-dd HH:mm:ss"));

            try
            {
                // 1. Supprimer les logs Information après 1 jours
                _logger.LogDebug("Suppression des logs Information...");
                result.InformationDeleted = await context.Database
                    .ExecuteSqlRawAsync(
                        "DELETE FROM Logs WHERE Level = 'Information' AND Timestamp < {0}", 
                        informationCutoff);
                
                if (result.InformationDeleted > 0)
                    _logger.LogDebug("  {Count} logs Information supprimés", result.InformationDeleted);

                // 2. Supprimer les logs Warning après 3 jours
                _logger.LogDebug("Suppression des logs Warning...");
                result.WarningDeleted = await context.Database
                    .ExecuteSqlRawAsync(
                        "DELETE FROM Logs WHERE Level = 'Warning' AND Timestamp < {0}", 
                        warningCutoff);
                
                if (result.WarningDeleted > 0)
                    _logger.LogDebug("  {Count} logs Warning supprimés", result.WarningDeleted);

                // 3. Supprimer les logs Error après 7 jours
                _logger.LogDebug("Suppression des logs Error...");
                result.ErrorDeleted = await context.Database
                    .ExecuteSqlRawAsync(
                        "DELETE FROM Logs WHERE Level = 'Error' AND Timestamp < {0}", 
                        errorCutoff);
                
                if (result.ErrorDeleted > 0)
                    _logger.LogDebug("  {Count} logs Error supprimés", result.ErrorDeleted);

                // 4. NE JAMAIS supprimer les logs Fatal (conservation illimitée)
                _logger.LogDebug("Les logs Fatal sont conservés indéfiniment (non supprimés)");

                // Optimiser la table si beaucoup de suppressions
                var totalDeleted = result.TotalDeleted;
                if (totalDeleted > 1000)
                {
                    _logger.LogDebug("Optimisation de la table Logs...");
                    await context.Database.ExecuteSqlRawAsync("OPTIMIZE TABLE Logs");
                    _logger.LogDebug("Table optimisée après suppression de {Count} logs", totalDeleted);
                }

                // Estimer l'espace libéré (approximation : 400 octets par log)
                result.EstimatedSpaceMB = Math.Round((totalDeleted * 400L) / 1024.0 / 1024.0, 2);

                // Log de statistiques
                await LogCleanupStatisticsAsync(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du nettoyage des logs par niveau");
                throw;
            }

            return result;
        }

        private async Task LogCleanupStatisticsAsync(KenergieDbContext context)
        {
            try
            {
                // Statistiques après nettoyage
                var stats = await context.Database
                    .ExecuteSqlRawAsync(@"
                        SELECT 
                            Level,
                            COUNT(*) as Count,
                            MIN(Timestamp) as Oldest,
                            MAX(Timestamp) as Newest
                        FROM Logs 
                        GROUP BY Level
                        ORDER BY Level");

                _logger.LogDebug("Statistiques des logs après nettoyage :");
                // Note : En pratique, vous voudrez utiliser FromSqlRaw pour lire les résultats
                // mais pour simplifier, on log juste que les stats ont été collectées
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de collecter les statistiques après nettoyage");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("=== ARRÊT DU SERVICE DE NETTOYAGE DES LOGS ===");
            await base.StopAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Résultat du nettoyage des logs
    /// </summary>
    public class CleanupResult
    {
        public int InformationDeleted { get; set; }
        public int WarningDeleted { get; set; }
        public int ErrorDeleted { get; set; }
        public int TotalDeleted => InformationDeleted + WarningDeleted + ErrorDeleted;
        public double EstimatedSpaceMB { get; set; }
    }
}
