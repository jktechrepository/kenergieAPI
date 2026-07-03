using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kenergie.Data;
using Kenergie.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services.Notifications
{
    /// <summary>
    /// Worker en arrière-plan pour traiter les diffusions batch de factures
    /// Traite les jobs par lots pour optimiser les performances
    /// </summary>
    public class FactureDiffusionWorker : BackgroundService
    {
        private readonly IFactureDiffusionQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FactureDiffusionWorker> _logger;
        private const int BATCH_SIZE = 50; // Traiter 50 clients à la fois
        private const int DELAY_BETWEEN_BATCHES_MS = 1000; // 1 seconde entre les lots

        public FactureDiffusionWorker(
            IFactureDiffusionQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<FactureDiffusionWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 FactureDiffusionWorker démarré");

            while (!stoppingToken.IsCancellationRequested)
            {
                FactureDiffusionJob job;
                try
                {
                    job = await _queue.DequeueDiffusionAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors de la récupération d'un job de diffusion");
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<KenergieDbContext>();
                    var factureNotificationService = scope.ServiceProvider.GetRequiredService<FactureNotificationService>();

                    // Récupérer la facture
                    var facture = await context.Factures
                        .Include(f => f.Usage)
                        .FirstOrDefaultAsync(f => f.IdFacture == job.FactureId, stoppingToken);

                    if (facture == null)
                    {
                        _logger.LogWarning("⚠️ Facture {FactureId} non trouvée pour la diffusion", job.FactureId);
                        continue;
                    }

                    // Récupérer tous les clients ayant cet usage (via ClientUsage)
                    var clientUsages = await context.ClientUsages
                        .Include(cu => cu.Client)
                        .Where(cu => cu.IdUsage == job.IdUsage && 
                                    cu.Client != null && 
                                    cu.Client.Statut == true)
                        .ToListAsync(stoppingToken);

                    var clients = clientUsages.Select(cu => cu.Client!).ToList();

                    var utilisateurs = await context.Utilisateurs
                        .Include(u => u.Societe)
                        .Where(u => u.IdClient.HasValue && clients.Select(c => c.IdClient).Contains(u.IdClient.Value))
                        .ToListAsync(stoppingToken);

                    // Charger les préférences de notification pour éviter N+1
                    var utilisateurIds = utilisateurs.Select(u => u.IdUtilisateur).ToList();
                    var preferences = await context.NotificationPreferences
                        .Where(p => utilisateurIds.Contains(p.IdUtilisateur))
                        .ToListAsync(stoppingToken);

                    int successCount = 0;
                    int errorCount = 0;
                    int totalClients = clients.Count;

                    _logger.LogInformation("📤 Début de la diffusion batch pour facture {FactureId} à {Total} clients", 
                        job.FactureId, totalClients);

                    // Traiter par lots pour éviter la surcharge
                    for (int i = 0; i < clients.Count; i += BATCH_SIZE)
                    {
                        var batch = clients.Skip(i).Take(BATCH_SIZE);
                        
                        foreach (var client in batch)
                        {
                            try
                            {
                                var utilisateur = utilisateurs.FirstOrDefault(u => u.IdClient == client.IdClient);
                                if (utilisateur != null)
                                {
                                    var success = await factureNotificationService.DiffuserFactureAClientAsync(
                                        facture, client, utilisateur);
                                    if (success) 
                                    {
                                        successCount++;
                                    }
                                    else
                                    {
                                        errorCount++;
                                        _logger.LogWarning("⚠️ Échec de diffusion pour client {ClientId} (facture {FactureId})", 
                                            client.IdClient, job.FactureId);
                                    }
                                }
                                else
                                {
                                    errorCount++;
                                    _logger.LogWarning("⚠️ Aucun utilisateur trouvé pour le client {ClientId} (facture {FactureId})", 
                                        client.IdClient, job.FactureId);
                                }
                            }
                            catch (Exception clientEx)
                            {
                                errorCount++;
                                _logger.LogError(clientEx, "❌ Erreur lors de la diffusion au client {ClientId} (facture {FactureId})", 
                                    client.IdClient, job.FactureId);
                                // Continuer avec les autres clients même en cas d'erreur
                            }
                        }

                        // Délai entre les lots pour éviter la surcharge
                        if (i + BATCH_SIZE < clients.Count)
                        {
                            await Task.Delay(DELAY_BETWEEN_BATCHES_MS, stoppingToken);
                        }
                    }

                    var duree = DateTime.Now - job.EnqueuedAt;
                    _logger.LogInformation(
                        "✅ Diffusion batch terminée pour facture {FactureId}: {Success}/{Total} clients notifiés, {Errors} erreurs en {Duree}",
                        job.FactureId, successCount, totalClients, errorCount, duree);

                    // Mettre à jour la date de diffusion de la facture (déjà marquée comme diffusée dans le contrôleur)
                    // Utiliser un try-catch séparé pour éviter que l'échec de sauvegarde ne masque les résultats
                    try
                    {
                        facture.DateDiffusion = DateTime.Now;
                        await context.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "⚠️ Erreur lors de la mise à jour de DateDiffusion pour facture {FactureId}, mais diffusion terminée: {Success}/{Total}", 
                            job.FactureId, successCount, totalClients);
                        // Ne pas faire échouer le job entier si la sauvegarde échoue
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Erreur lors du traitement du job de diffusion {FactureId}", job.FactureId);
                }
            }

            _logger.LogInformation("⏹️ FactureDiffusionWorker arrêté");
        }
    }
}

