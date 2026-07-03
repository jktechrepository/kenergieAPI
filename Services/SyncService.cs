using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Kenergie.Services
{
    /// <summary>
    /// Service de synchronisation offline
    /// Implémente la logique de synchronisation avec cursor pagination et delta sync
    /// </summary>
    public class SyncService : ISyncService
    {
        private readonly KenergieDbContext _context;
        private readonly IWatermarkService _watermarkService;
        private readonly ICursorService _cursorService;
        private readonly ILogger<SyncService> _logger;

        public SyncService(
            KenergieDbContext context,
            IWatermarkService watermarkService,
            ICursorService cursorService,
            ILogger<SyncService> logger)
        {
            _context = context;
            _watermarkService = watermarkService;
            _cursorService = cursorService;
            _logger = logger;
        }

        /// <summary>
        /// Fournit les informations initiales pour démarrer la synchronisation
        /// </summary>
        public async Task<SyncBootstrapDto> GetBootstrapAsync(int societeId)
        {
            try
            {
                _logger.LogInformation("Début du bootstrap pour la société {SocieteId}", societeId);

                var serverTimeUtc = DateTime.UtcNow;
                var snapshot = serverTimeUtc.ToString("O");

                // Récupérer les clients via la relation indirecte: Client → Axe → Cabine → Societe
                // Diagnostic: Compter tous les clients pour cette société
                var totalClients = await _context.Clients
                    .AsNoTracking()
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                    .Where(c => c.IdAxe.HasValue && 
                               c.Axe != null && 
                               c.Axe.Cabine != null &&
                               c.Axe.Cabine.IdSociete == societeId)
                    .CountAsync();

                _logger.LogInformation("Diagnostic: Total clients trouvés pour société {SocieteId}: {Count}", societeId, totalClients);

                var clientEntities = await _context.Clients
                    .AsNoTracking()
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.Usage)
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.TypeDeCourant)
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                    .Where(c => c.IdAxe.HasValue && // Vérifier que IdAxe n'est pas null
                               c.Axe != null && 
                               c.Axe.Cabine != null &&
                               c.Axe.Cabine.IdSociete == societeId && 
                               (!c.IsDeleted.HasValue || !c.IsDeleted.Value))
                    .OrderBy(c => c.IdClient)
                    .Take(20000) // Limite pour le bootstrap
                    .ToListAsync();

                var clients = clientEntities.Select(ToClientSyncDto).ToList();

                // Récupérer les arriérés via la même relation indirecte
                var arrears = await _context.ClientFactures
                    .AsNoTracking()
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.Axe)
                            .ThenInclude(a => a.Cabine)
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.ClientsUsages)
                            .ThenInclude(cu => cu.Usage)
                    .Where(cf => cf.Client.Axe.Cabine.IdSociete == societeId && 
                               cf.Statut == true && 
                               cf.MontantDu > 0)
                    .Select(cf => new ArrearSyncDto
                    {
                        IdClientFacture = cf.IdClientFacture,
                        IdFacture = cf.IdFacture,
                        IdClient = cf.IdClient,
                        NumeroFacture = cf.Facture != null ? cf.Facture.NumeroFacture : "Arriéré",
                        MontantTotal = cf.Montant ?? 0,
                        MontantPaye = cf.MontantPaye ?? 0,
                        MontantDu = cf.MontantDu ?? 0,
                        Mois = cf.Mois,
                        Annees = cf.Annees ?? 0,
                        DateEmission = cf.DateEmission ?? DateTime.Now,
                        EstArrierePreExistant = cf.EstArrierePreExistant,
                        LibelleUsage = cf.Client.ClientsUsages.Any() ? cf.Client.ClientsUsages.First().Usage.Libelle : "Inconnu",
                        DateModification = cf.DateModification ?? cf.DateCreation
                    })
                    .Take(20000) // Limite pour le bootstrap
                    .ToListAsync();

                // Créer le watermark initial
                var watermark = _watermarkService.CreateInitialWatermark();

                _logger.LogInformation("Bootstrap réussi - Clients: {ClientCount}, Arrears: {ArrearCount}", 
                    clients.Count, arrears.Count);

                // Diagnostic détaillé
                _logger.LogInformation("Diagnostic détaillé - Premier client: {FirstClient}, Premier IdSociete: {FirstSociete}", 
                    clients.Any() ? clients.First().IdClient.ToString() : "Aucun",
                    clients.Any() ? clients.First().IdSociete.ToString() : "Aucun");

                return new SyncBootstrapDto
                {
                    Watermark = watermark,
                    Clients = clients,
                    Arrears = arrears
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du bootstrap pour la société {SocieteId}", societeId);
                throw;
            }
        }

        /// <summary>
        /// Récupère les clients avec pagination cursor et delta sync
        /// </summary>
        public async Task<SyncPageDto<ClientSyncDto>> GetClientsAsync(int societeId, SyncRequestDto request)
        {
            try
            {
                _logger.LogInformation("Synchronisation des clients - Société: {SocieteId}, PageSize: {PageSize}, Since: {Since}", 
                    societeId, request.PageSize, request.Since);

                var query = _context.Clients
                    .AsNoTracking()
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.Usage)
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.TypeDeCourant)
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                    .Where(c => c.IdAxe.HasValue && // Vérifier que IdAxe n'est pas null
                               c.Axe != null && 
                               c.Axe.Cabine != null &&
                               c.Axe.Cabine.IdSociete == societeId && 
                               (!c.IsDeleted.HasValue || !c.IsDeleted.Value));

                // Appliquer le filtre delta si 'since' est fourni
                if (!string.IsNullOrEmpty(request.Since))
                {
                    var (sinceUpdatedAt, sinceId) = _watermarkService.ParseWatermark(request.Since);
                    query = query.Where(c => c.UpdatedAt > sinceUpdatedAt || 
                                               (c.UpdatedAt == sinceUpdatedAt && c.IdClient > sinceId));
                }

                // Appliquer le filtre de snapshot si fourni
                if (!string.IsNullOrEmpty(request.Snapshot))
                {
                    var snapshotUtc = DateTime.Parse(request.Snapshot, CultureInfo.InvariantCulture);
                    query = query.Where(c => c.UpdatedAt <= snapshotUtc);
                }

                // Appliquer la pagination cursor si 'cursor' est fourni
                if (!string.IsNullOrEmpty(request.Cursor))
                {
                    var (cursorUpdatedAt, cursorId) = _cursorService.ParseCursor(request.Cursor);
                    query = query.Where(c => c.UpdatedAt > cursorUpdatedAt || 
                                               (c.UpdatedAt == cursorUpdatedAt && c.IdClient > cursorId));
                }

                var clientEntities = await query
                    .OrderBy(c => c.UpdatedAt)
                    .ThenBy(c => c.IdClient)
                    .Take(request.PageSize)
                    .ToListAsync();

                var items = clientEntities.Select(ToClientSyncDto).ToList();

                // Déterminer s'il y a une page suivante
                var hasMore = items.Count >= request.PageSize;
                string? nextCursor = null;
                string? nextSince = null;

                if (hasMore && items.Any())
                {
                    var lastItem = items.Last();
                    nextCursor = _cursorService.CreateCursor(new { UpdatedAt = lastItem.UpdatedAt, Id = lastItem.IdClient });
                    nextSince = _watermarkService.CreateWatermark(lastItem.UpdatedAt, lastItem.IdClient);
                }

                return new SyncPageDto<ClientSyncDto>
                {
                    Snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O"),
                    Items = items,
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                    NextSince = nextSince
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation des clients");
                throw;
            }
        }

        /// <summary>
        /// Récupère les arriérés avec pagination cursor et delta sync
        /// </summary>
        public async Task<SyncPageDto<ArrearSyncDto>> GetArrearsAsync(int societeId, SyncArrearsRequestDto request)
        {
            try
            {
                _logger.LogInformation("Synchronisation des arriérés - Société: {SocieteId}, OnlyOutstanding: {OnlyOutstanding}", 
                    societeId, request.OnlyOutstanding);

                var query = _context.ClientFactures
                    .AsNoTracking()
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.Axe)
                            .ThenInclude(a => a.Cabine)
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.ClientsUsages)
                            .ThenInclude(cu => cu.Usage)
                    .Where(cf => cf.Client.IdAxe.HasValue && // Vérifier que IdAxe n'est pas null
                               cf.Client.Axe != null && 
                               cf.Client.Axe.Cabine != null &&
                               cf.Client.Axe.Cabine.IdSociete == societeId && 
                               cf.Statut == true);

                // Appliquer le filtre onlyOutstanding
                if (request.OnlyOutstanding)
                {
                    query = query.Where(cf => cf.MontantDu > 0);
                }

                // Appliquer le filtre delta si 'since' est fourni
                if (!string.IsNullOrEmpty(request.Since))
                {
                    var (sinceDateModification, sinceId) = _watermarkService.ParseWatermark(request.Since);
                    query = query.Where(cf => cf.DateModification > sinceDateModification || 
                                                    (cf.DateModification == sinceDateModification && cf.IdClientFacture > sinceId));
                }

                // Appliquer le filtre de snapshot si fourni
                if (!string.IsNullOrEmpty(request.Snapshot))
                {
                    var snapshotUtc = DateTime.Parse(request.Snapshot, CultureInfo.InvariantCulture);
                    query = query.Where(cf => cf.DateModification <= snapshotUtc);
                }

                // Appliquer la pagination cursor si 'cursor' est fourni
                if (!string.IsNullOrEmpty(request.Cursor))
                {
                    var (cursorDateModification, cursorId) = _cursorService.ParseCursor(request.Cursor);
                    query = query.Where(cf => cf.DateModification > cursorDateModification || 
                                                    (cf.DateModification == cursorDateModification && cf.IdClientFacture > cursorId));
                }

                // Tri stable et pagination
                var items = await query
                    .OrderBy(cf => cf.DateModification)
                    .ThenBy(cf => cf.IdClientFacture)
                    .Take(request.PageSize)
                    .Select(cf => new ArrearSyncDto
                    {
                        IdClientFacture = cf.IdClientFacture,
                        IdFacture = cf.IdFacture,
                        IdClient = cf.IdClient,
                        NumeroFacture = cf.Facture != null ? cf.Facture.NumeroFacture : "Arriéré",
                        MontantTotal = cf.Montant ?? 0,
                        MontantPaye = cf.MontantPaye ?? 0,
                        MontantDu = cf.MontantDu ?? 0,
                        Mois = cf.Mois,
                        Annees = cf.Annees ?? 0,
                        DateEmission = cf.DateEmission ?? DateTime.Now,
                        EstArrierePreExistant = cf.EstArrierePreExistant,
                        LibelleUsage = cf.Client.ClientsUsages.Any() ? cf.Client.ClientsUsages.First().Usage.Libelle : "Inconnu",
                        DateModification = cf.DateModification ?? cf.DateCreation
                    })
                    .ToListAsync();

                // Déterminer s'il y a une page suivante
                var hasMore = items.Count >= request.PageSize;
                string? nextCursor = null;
                string? nextSince = null;

                if (hasMore && items.Any())
                {
                    var lastItem = items.Last();
                    nextCursor = _cursorService.CreateCursor(new { DateModification = lastItem.DateModification, Id = lastItem.IdClientFacture });
                    nextSince = _watermarkService.CreateWatermark(lastItem.DateModification, lastItem.IdClientFacture);
                }

                return new SyncPageDto<ArrearSyncDto>
                {
                    Snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O"),
                    Items = items,
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                    NextSince = nextSince
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la synchronisation des arriérés");
                throw;
            }
        }

        /// <summary>
        /// Récupère les suppressions depuis la dernière synchronisation
        /// </summary>
        public async Task<SyncDeletionsDto> GetDeletionsAsync(int societeId, SyncDeletionsRequestDto request)
        {
            try
            {
                _logger.LogInformation("Récupération des suppressions - Société: {SocieteId}, Since: {Since}", 
                    societeId, request.Since);

                var (sinceDateModification, sinceId) = _watermarkService.ParseWatermark(request.Since);

                // Clients supprimés (soft delete)
                var deletedClients = await _context.Clients
                    .AsNoTracking()
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                    .Where(c => c.IdAxe.HasValue && // Vérifier que IdAxe n'est pas null
                               c.Axe != null && 
                               c.Axe.Cabine != null &&
                               c.Axe.Cabine.IdSociete == societeId && 
                               c.IsDeleted.HasValue && c.IsDeleted.Value &&
                               (c.UpdatedAt > sinceDateModification || 
                                (c.UpdatedAt == sinceDateModification && c.IdClient > sinceId)))
                    .Select(c => c.IdClient)
                    .ToListAsync();

                // ClientFactures sorties du filtre (soldées, annulées, etc.)
                var removedClientFactures = await _context.ClientFactures
                    .AsNoTracking()
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.Axe)
                            .ThenInclude(a => a.Cabine)
                    .Where(cf => cf.Client.IdAxe.HasValue && // Vérifier que IdAxe n'est pas null
                               cf.Client.Axe != null && 
                               cf.Client.Axe.Cabine != null &&
                               cf.Client.Axe.Cabine.IdSociete == societeId && 
                               cf.Statut == false &&
                               (cf.DateModification > sinceDateModification || 
                                (cf.DateModification == sinceDateModification && cf.IdClientFacture > sinceId)))
                    .Select(cf => cf.IdClientFacture)
                    .ToListAsync();
                // Paiements supprimés
                var deletedPayments = await _context.Paiements
                    .AsNoTracking()
                    .Where(p => p.IsDeleted &&
                               (p.UpdatedAt > sinceDateModification || 
                                (p.UpdatedAt == sinceDateModification && p.IdPaiement > sinceId)))
                    .Select(p => p.IdPaiement)
                    .ToListAsync();

                var nextSince = _watermarkService.CreateWatermark(DateTime.UtcNow, 0);

                return new SyncDeletionsDto
                {
                    Snapshot = request.Snapshot ?? DateTime.UtcNow.ToString("O"),
                    DeletedClientIds = deletedClients,
                    RemovedClientFactureIds = removedClientFactures,
                    DeletedPaymentIds = deletedPayments,
                    NextSince = nextSince
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des suppressions");
                throw;
            }
        }

        /// <summary>
        /// Traite un batch de paiements offline avec idempotence
        /// </summary>
        public async Task<PaymentBatchResultDto> ProcessPaymentsBatchAsync(int societeId, int userId, PaymentBatchRequestDto request)
        {
            var results = new List<PaymentResultDto>();
            var created = 0;
            var duplicates = 0;
            var rejected = 0;
            var errors = 0;

            _logger.LogInformation("Traitement batch de paiements - Société: {SocieteId}, Count: {Count}", 
                societeId, request.Items.Count);

            foreach (var paymentRequest in request.Items)
            {
                try
                {
                    // Vérifier l'idempotence
                    var existingPayment = await _context.Paiements
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.ClientRequestId == paymentRequest.ClientRequestId);

                    if (existingPayment != null)
                    {
                        results.Add(new PaymentResultDto
                        {
                            ClientRequestId = paymentRequest.ClientRequestId,
                            Status = "duplicate",
                            IdPaiement = existingPayment.IdPaiement,
                            Message = "Paiement déjà enregistré",
                            ErrorCode = null
                        });
                        duplicates++;
                        continue;
                    }

                    // Validation métier
                    var validationResult = await ValidatePaymentAsync(societeId, paymentRequest);
                    if (!validationResult.IsValid)
                    {
                        results.Add(new PaymentResultDto
                        {
                            ClientRequestId = paymentRequest.ClientRequestId,
                            Status = "rejected",
                            IdPaiement = null,
                            Message = validationResult.Message,
                            ErrorCode = validationResult.ErrorCode
                        });
                        rejected++;
                        continue;
                    }

                    // Créer le paiement
                    var paiement = new Paiement
                    {
                        ClientRequestId = paymentRequest.ClientRequestId,
                        IdClient = paymentRequest.IdClient,
                        IdClientFacture = paymentRequest.IdClientFacture,
                        IdFacture = paymentRequest.IdFacture,
                        MontantPaye = paymentRequest.MontantPaye,
                        DatePaiement = paymentRequest.DatePaiementUtc,
                        MethodePaiement = paymentRequest.MethodePaiement,
                        ReferenceTransaction = paymentRequest.ReferenceTransaction,
                        Commentaire = paymentRequest.Commentaire,
                        IdUtilisateur = userId,  // Correction critique : lier le paiement à l'utilisateur
                        Statut = "Validé",
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _context.Paiements.AddAsync(paiement);
                    await _context.SaveChangesAsync();
                    if (paymentRequest.IdClientFacture.HasValue)
                    {
                        await UpdateClientFactureAsync(paymentRequest.IdClientFacture.Value, paymentRequest.MontantPaye);
                    }

                    results.Add(new PaymentResultDto
                    {
                        ClientRequestId = paymentRequest.ClientRequestId,
                        Status = "created",
                        IdPaiement = paiement.IdPaiement,
                        NewMontantDu = await CalculateNewMontantDuAsync(paymentRequest.IdClientFacture.Value, paymentRequest.MontantPaye),
                        Message = "Paiement créé avec succès",
                        ErrorCode = null
                    });
                    created++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors du traitement du paiement {ClientRequestId}", paymentRequest.ClientRequestId);
                    results.Add(new PaymentResultDto
                    {
                        ClientRequestId = paymentRequest.ClientRequestId,
                        Status = "error",
                        IdPaiement = null,
                        Message = "Erreur interne serveur",
                        ErrorCode = "INTERNAL_ERROR"
                    });
                    errors++;
                }
            }

            return new PaymentBatchResultDto
            {
                Results = results,
                Summary = new PaymentSummaryDto
                {
                    Total = request.Items.Count,
                    Created = created,
                    Duplicates = duplicates,
                    Rejected = rejected,
                    Errors = errors
                }
            };
        }

        /// <summary>
        /// Valide un paiement selon les règles métier
        /// </summary>
        private async Task<(bool IsValid, string Message, string? ErrorCode)> ValidatePaymentAsync(int societeId, PaymentRequestDto paymentRequest)
        {
            // Vérifier que le client appartient à la société (via relation indirecte)
            var client = await _context.Clients
                .AsNoTracking()
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                .FirstOrDefaultAsync(c => c.IdClient == paymentRequest.IdClient && 
                                       c.IdAxe.HasValue && 
                                       c.Axe != null && 
                                       c.Axe.Cabine != null &&
                                       c.Axe.Cabine.IdSociete == societeId && 
                                       (!c.IsDeleted.HasValue || !c.IsDeleted.Value));

            if (client == null)
                return (false, "Client introuvable", "INVALID_CLIENT");

            // Vérifier le ClientFacture si fourni
            if (paymentRequest.IdClientFacture.HasValue)
            {
                var clientFacture = await _context.ClientFactures
                    .AsNoTracking()
                    .Include(cf => cf.Client)
                        .ThenInclude(c => c.Axe)
                            .ThenInclude(a => a.Cabine)
                    .FirstOrDefaultAsync(cf => cf.IdClientFacture == paymentRequest.IdClientFacture.Value && 
                                                 cf.Client.IdAxe.HasValue && 
                                                 cf.Client.Axe != null && 
                                                 cf.Client.Axe.Cabine != null &&
                                                 cf.Client.Axe.Cabine.IdSociete == societeId);

                if (clientFacture == null)
                    return (false, "Facture client introuvable", "INVALID_FACTURE");

                if (paymentRequest.MontantPaye > (clientFacture.MontantDu ?? 0))
                    return (false, "Montant supérieur au montant dû", "AMOUNT_EXCEEDS_DUE");
            }

            return (true, "Validation réussie", null);
        }

        /// <summary>
        /// Met à jour le ClientFacture après un paiement
        /// </summary>
        private async Task UpdateClientFactureAsync(int idClientFacture, decimal montantPaye)
        {
            var clientFacture = await _context.ClientFactures
                .FirstOrDefaultAsync(cf => cf.IdClientFacture == idClientFacture);

            if (clientFacture != null)
            {
                clientFacture.MontantPaye = (clientFacture.MontantPaye ?? 0) + montantPaye;
                clientFacture.MontantDu = (clientFacture.Montant ?? 0) - clientFacture.MontantPaye;
                clientFacture.DateModification = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mappe une entité Client (déjà chargée avec ClientUsages, Usage, TypeDeCourant, Axe, Cabine) vers ClientSyncDto.
        /// </summary>
        private static ClientSyncDto ToClientSyncDto(Client c)
        {
            var orderedUsages = (c.ClientsUsages ?? Enumerable.Empty<ClientUsage>())
                .OrderBy(cu => cu.IdClientUsage)
                .ToList();

            var usageDtos = orderedUsages.Select(cu => new ClientUsageSyncItemDto
            {
                IdClientUsage = cu.IdClientUsage,
                IdClient = cu.IdClient,
                IdUsage = cu.IdUsage,
                LibelleUsage = cu.Usage?.Libelle ?? string.Empty,
                IdCategorieClient = cu.Usage?.IdCategorieClient,
                nombreBatiment = cu.nombreBatiment,
                Statut = cu.Statut,
                IdTypeDeCourant = cu.IdTypeDeCourant
            }).ToList();

            var firstActive = orderedUsages.FirstOrDefault(cu => cu.Statut);

            return new ClientSyncDto
            {
                IdClient = c.IdClient,
                NomClient = c.NomClient,
                AdresseClient = c.AdresseClient,
                Telephone = c.Telephone,
                EmailClient = c.EmailClient,
                CodeCons = c.CodeCons,
                GenreClient = c.GenreClient,
                IdAxe = c.IdAxe,
                IdCabine = c.Axe != null ? c.Axe.IdCabine : null,
                IdSociete = c.Axe?.Cabine?.IdSociete ?? 0,
                IdCategorieClient = firstActive?.Usage?.IdCategorieClient
                    ?? orderedUsages.FirstOrDefault(cu => cu.Usage != null)?.Usage?.IdCategorieClient,
                IdTypeDeCourant = firstActive?.IdTypeDeCourant,
                ClientUsages = usageDtos,
                IsActif = c.IsActif,
                Statut = c.Statut,
                IsDeleted = c.IsDeleted ?? false,
                UpdatedAt = c.UpdatedAt ?? c.DateCreation
            };
        }

        /// <summary>
        /// Calcule le nouveau montant dû après paiement
        /// </summary>
        private async Task<decimal> CalculateNewMontantDuAsync(int idClientFacture, decimal montantPaye)
        {
            var clientFacture = await _context.ClientFactures
                .AsNoTracking()
                .FirstOrDefaultAsync(cf => cf.IdClientFacture == idClientFacture);

            if (clientFacture != null)
            {
                var nouveauMontantPaye = (clientFacture.MontantPaye ?? 0) + montantPaye;
                return Math.Max(0, (clientFacture.Montant ?? 0) - nouveauMontantPaye);
            }

            return 0;
        }
    }
}
