using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Facture;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kenergie.Services
{
    public class FactureService : IFactureRepository
    {
        private readonly KenergieDbContext _context;
        private readonly IClientFactureRepository _clientFactureRepository;
        private readonly IDeviseConversionService _deviseConversionService;
        private readonly ILogger<FactureService> _logger;

        public FactureService(
            KenergieDbContext context,
            IClientFactureRepository clientFactureRepository,
            IDeviseConversionService deviseConversionService,
            ILogger<FactureService> logger)
        {
            _context = context;
            _clientFactureRepository = clientFactureRepository;
            _deviseConversionService = deviseConversionService;
            _logger = logger;
        }

        public async Task<IEnumerable<Facture>> GetAllAsync()
        {
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.Statut == true)
                .OrderByDescending(f => f.DateEmission)
                .ThenByDescending(f => f.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Facture>> GetByCategorieAsync(int idCategorie)
        {
            // Récupérer les factures via les usages de cette catégorie
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.Usage != null && 
                           f.Usage.IdCategorieClient == idCategorie && 
                           f.Statut == true)
                .OrderByDescending(f => f.DateEmission)
                .ThenByDescending(f => f.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Facture>> GetBySocieteAsync(int idSociete)
        {
            // Récupérer les factures via les usages -> catégories de la société
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(f => f.Usage != null && 
                           f.Usage.CategorieClient != null && 
                           f.Usage.CategorieClient.IdSociete == idSociete && 
                           f.Statut == true)
                .OrderByDescending(f => f.DateEmission)
                .ThenByDescending(f => f.DateCreation)
                .ToListAsync();
        }

        public async Task<PagedResult<Facture>> GetBySocietePagedAsync(int idSociete, PagedRequest request)
        {
            request ??= new PagedRequest();

            var query = _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(f => f.Usage != null && 
                           f.Usage.CategorieClient != null && 
                           f.Usage.CategorieClient.IdSociete == idSociete && 
                           f.Statut == true);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(f =>
                    (f.NumeroFacture ?? string.Empty).ToLower().Contains(term) ||
                    (f.Usage != null && f.Usage.Libelle != null && 
                     f.Usage.Libelle.ToLower().Contains(term)));
            }

            query = request.SortBy switch
            {
                "NumeroFacture" or "numero" => request.SortDescending 
                    ? query.OrderByDescending(f => f.NumeroFacture) 
                    : query.OrderBy(f => f.NumeroFacture),
                "DateEmission" or "date" => request.SortDescending 
                    ? query.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue) 
                    : query.OrderBy(f => f.DateEmission ?? DateTime.MinValue),
                "Montant" or "montant" => request.SortDescending 
                    ? query.OrderByDescending(f => f.Montant ?? 0) 
                    : query.OrderBy(f => f.Montant ?? 0),
                "MoisEmission" or "mois" => request.SortDescending 
                    ? query.OrderByDescending(f => f.MoisEmission) 
                    : query.OrderBy(f => f.MoisEmission),
                "AnneesEmission" or "annee" => request.SortDescending 
                    ? query.OrderByDescending(f => f.AnneesEmission) 
                    : query.OrderBy(f => f.AnneesEmission),
                _ => request.SortDescending 
                    ? query.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                        .ThenByDescending(f => f.DateCreation)
                    : query.OrderBy(f => f.DateEmission ?? DateTime.MinValue)
                        .ThenBy(f => f.DateCreation)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Facture>(data, total, request.PageNumber, request.PageSize);
        }

        public async Task<IEnumerable<Facture>> GetByMoisAnneeAsync(int mois, int annee)
        {
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.MoisEmission == mois && f.AnneesEmission == annee && f.Statut == true)
                .OrderByDescending(f => f.DateEmission)
                .ThenByDescending(f => f.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Facture>> GetByCategorieMoisAnneeAsync(int idCategorie, int mois, int annee)
        {
            // Récupérer les factures via les usages de cette catégorie
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.Usage != null && 
                           f.Usage.IdCategorieClient == idCategorie && 
                           f.MoisEmission == mois && 
                           f.AnneesEmission == annee && 
                           f.Statut == true)
                .OrderByDescending(f => f.DateEmission)
                .ThenByDescending(f => f.DateCreation)
                .ToListAsync();
        }

        public async Task<Facture> GetByIdAsync(int id)
        {
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.Statut == true)
                .FirstOrDefaultAsync(f => f.IdFacture == id);
        }

        public async Task<Facture> GetByNumeroFactureAsync(string numeroFacture)
        {
            return await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.Statut == true)
                .FirstOrDefaultAsync(f => f.NumeroFacture == numeroFacture);
        }

        public async Task<Facture?> ResolveFactureBySearchTermAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return null;
            }

            var term = searchTerm.Trim();

            var byNumero = await GetByNumeroFactureAsync(term);
            if (byNumero != null)
            {
                return byNumero;
            }

            var normalizedTerm = term.ToLowerInvariant();

            var byCodeCons = await GetLatestFactureByCodeConsAsync(normalizedTerm);
            if (byCodeCons != null)
            {
                return byCodeCons;
            }

            return await GetLatestFactureByNomClientAsync(normalizedTerm);
        }

        private async Task<Facture?> GetLatestFactureByCodeConsAsync(string normalizedCodeCons)
        {
            var factureId = await (
                from cf in _context.ClientFactures
                join c in _context.Clients on cf.IdClient equals c.IdClient
                join f in _context.Factures on cf.IdFacture equals f.IdFacture
                where cf.Statut == true
                   && cf.IdFacture != null
                   && c.Statut == true
                   && f.Statut == true
                   && c.CodeCons != null
                   && c.CodeCons.Trim().ToLower() == normalizedCodeCons
                orderby f.AnneesEmission descending, f.MoisEmission descending, f.DateEmission descending, f.IdFacture descending
                select f.IdFacture)
                .FirstOrDefaultAsync();

            return factureId == 0 ? null : await GetByIdAsync(factureId);
        }

        private async Task<Facture?> GetLatestFactureByNomClientAsync(string normalizedNomClient)
        {
            var factureId = await (
                from cf in _context.ClientFactures
                join c in _context.Clients on cf.IdClient equals c.IdClient
                join f in _context.Factures on cf.IdFacture equals f.IdFacture
                where cf.Statut == true
                   && cf.IdFacture != null
                   && c.Statut == true
                   && f.Statut == true
                   && c.NomClient.Trim().ToLower() == normalizedNomClient
                orderby f.AnneesEmission descending, f.MoisEmission descending, f.DateEmission descending, f.IdFacture descending
                select f.IdFacture)
                .FirstOrDefaultAsync();

            return factureId == 0 ? null : await GetByIdAsync(factureId);
        }

        public async Task<string> GenerateNumeroFactureAsync(int? idUsage, DateTime? dateEmission)
        {
            // Appel à la nouvelle méthode avec TypeDeCourant null pour rétrocompatibilité
            return await GenerateNumeroFactureAsync(idUsage, dateEmission, null);
        }

        /// <summary>
        /// Génère un numéro de facture unique en prenant en compte le TypeDeCourant
        /// Format : FAC-{INITIALES_USAGE}-{TYPE_COURANT}-{MMYY}-{####}
        /// Exemple : FAC-ACT-CP-0326-0001 (Usage: Activité Commercial, Type: Permanent, Mars 2026)
        /// </summary>
        public async Task<string> GenerateNumeroFactureAsync(int? idUsage, DateTime? dateEmission, int? idTypeDeCourant)
        {
            var dateRef = dateEmission ?? DateTime.Now;

            // Initiales usage (3 caractères)
            string initiales = "GEN";
            if (idUsage.HasValue)
            {
                var usage = await _context.Usages
                    .Include(u => u.CategorieClient)
                    .FirstOrDefaultAsync(u => u.IdUsage == idUsage.Value);
                if (usage != null && !string.IsNullOrWhiteSpace(usage.Libelle))
                {
                    var clean = new string(usage.Libelle
                        .Where(char.IsLetterOrDigit)
                        .Take(3)
                        .ToArray())
                        .ToUpperInvariant();
                    if (!string.IsNullOrWhiteSpace(clean))
                        initiales = clean.PadRight(3, 'X');
                }
            }

            // Code du TypeDeCourant (CP ou CD)
            string typeCourantCode = "XX";
            if (idTypeDeCourant.HasValue)
            {
                var typeDeCourant = await _context.TypeDeCourants
                    .FirstOrDefaultAsync(t => t.IdTypeDeCourant == idTypeDeCourant.Value);
                
                if (typeDeCourant != null && !string.IsNullOrWhiteSpace(typeDeCourant.Libelle))
                {
                    // Extraire le code : "CP" pour "Permanent", "CD" pour "Non Permanent"
                    if (typeDeCourant.Libelle.Equals("Permanent", StringComparison.OrdinalIgnoreCase))
                    {
                        typeCourantCode = "CP";
                    }
                    else if (typeDeCourant.Libelle.Equals("Non Permanent", StringComparison.OrdinalIgnoreCase))
                    {
                        typeCourantCode = "CD";
                    }
                    else
                    {
                        // Pour tout autre type, prendre les 2 premières lettres en majuscules
                        typeCourantCode = new string(typeDeCourant.Libelle
                            .Where(char.IsLetter)
                            .Take(2)
                            .ToArray())
                            .ToUpperInvariant()
                            .PadRight(2, 'X');
                    }
                }
            }

            var prefix = $"FAC-{initiales}-{typeCourantCode}-{dateRef:MMyy}";

            // Trouver le prochain numéro de séquence pour ce prefix (mois/catégorie/type)
            var maxSeq = await _context.Factures
                .Where(f => f.NumeroFacture != null && f.NumeroFacture.StartsWith(prefix))
                .Select(f => f.NumeroFacture!)
                .ToListAsync();

            int nextSeq = 1;
            if (maxSeq.Count > 0)
            {
                // format attendu : FAC-XXX-YY-MMYY-#### -> extraire la dernière partie
                var seqValues = maxSeq
                    .Select(n =>
                    {
                        var parts = n.Split('-', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[^1], out var seq))
                            return seq;
                        return 0;
                    })
                    .ToList();
                nextSeq = seqValues.Max() + 1;
            }

            var numero = $"{prefix}-{nextSeq:D4}";

            // Sécurité : s'assurer que ce numéro n'existe pas (collision improbable mais check async)
            while (await ExistsByNumeroFactureAsync(numero))
            {
                nextSeq++;
                numero = $"{prefix}-{nextSeq:D4}";
            }

            return numero;
        }

        public async Task<Facture> CreateAsync(Facture facture)
        {
            facture.DateCreation = DateTime.Now;
            if (!facture.Statut)
            {
                facture.Statut = true;
            }

            // ✨ Forcer EstDiffusee à false lors de la création
            facture.EstDiffusee = false;
            facture.DateDiffusion = null;

            await ApplyFactureDeviseSnapshotAsync(facture);

            _context.Factures.Add(facture);
            await _context.SaveChangesAsync();

            // ✨ NOUVEAU : Créer automatiquement les ClientFacture pour tous les clients ayant cet usage
            if (facture.IdUsage > 0 && facture.Montant.HasValue)
            {
                await CreateClientFacturesForFactureAsync(facture);
            }

            return facture;
        }

        /// <summary>
        /// Résout la société et applique le snapshot devise sur une facture.
        /// </summary>
        private async Task ApplyFactureDeviseSnapshotAsync(Facture facture, string? codeDevisePrixOverride = null)
        {
            var idSociete = await _context.Usages
                .Where(u => u.IdUsage == facture.IdUsage)
                .Select(u => (int?)u.CategorieClient!.IdSociete)
                .FirstOrDefaultAsync();

            if (!idSociete.HasValue)
                throw new InvalidOperationException($"Impossible de résoudre la société pour l'usage {facture.IdUsage}.");

            var principale = await _deviseConversionService.GetCodeDevisePrincipaleAsync(idSociete.Value);
            var codePrix = DeviseConversionService.NormalizeCode(
                !string.IsNullOrWhiteSpace(codeDevisePrixOverride)
                    ? codeDevisePrixOverride!
                    : (!string.IsNullOrWhiteSpace(facture.CodeDevisePrix) ? facture.CodeDevisePrix! : principale));

            var dateRef = facture.DateEmission ?? DateTime.UtcNow;
            var conversion = await _deviseConversionService.ConvertirVersPrincipaleAsync(
                idSociete.Value, codePrix, facture.Montant ?? 0m, dateRef);

            facture.CodeDevisePrix = codePrix;
            facture.CodeDevisePrincipale = principale;
            facture.TauxVersDevisePrincipale = conversion.Taux;
            facture.MontantDevisePrincipale = conversion.MontantConverti;
        }

        /// <summary>
        /// Crée automatiquement les ClientFacture pour tous les clients ayant l'usage de la facture
        /// </summary>
        private async Task CreateClientFacturesForFactureAsync(Facture facture)
        {
            try
            {
                // 🎯 NOUVEAU: Filtrage par IdUsage + IdTypeDeCourant
                IQueryable<ClientUsage> query = _context.ClientUsages
                    .Include(cu => cu.Client)
                    .Where(cu => cu.IdUsage == facture.IdUsage && 
                                cu.Client != null && 
                                cu.Client.Statut == true &&
                                cu.Client.IsActif == true &&
                                cu.Statut == true);

                // Ajouter le filtre sur TypeDeCourant si spécifié
                if (facture.IdTypeDeCourant.HasValue)
                {
                    query = query.Where(cu => cu.IdTypeDeCourant == facture.IdTypeDeCourant.Value);
                    
                    _logger.LogInformation("🎯 Filtre TypeDeCourant appliqué: IdType={Type} pour IdUsage={Usage}", 
                        facture.IdTypeDeCourant.Value, facture.IdUsage);
                }
                else
                {
                    _logger.LogInformation("🔄 Mode legacy: Filtre sur IdUsage seul pour IdUsage={Usage}", 
                        facture.IdUsage);
                }

                var clientUsages = await query.ToListAsync();

                // 🪵 Validation: Aucun client trouvé
                if (clientUsages.Count == 0)
                {
                    _logger.LogWarning("⚠️ Aucun client compatible trouvé pour Usage={Usage} Type={Type}", 
                        facture.IdUsage, facture.IdTypeDeCourant ?? 0);
                    
                    // Ne pas créer de ClientFacture si aucun client compatible
                    return;
                }

                var dejaFacturesClientIds = await FactureBillingDuplicateHelper.GetClientIdsAlreadyBilledAsync(
                    _context,
                    facture.IdUsage,
                    facture.MoisEmission,
                    facture.AnneesEmission,
                    facture.IdTypeDeCourant);

                var clientFacturesToCreate = new List<ClientFacture>();
                var skippedDuplicates = 0;
                var skippedLateRegistration = 0;

                foreach (var clientUsage in clientUsages)
                {
                    if (dejaFacturesClientIds.Contains(clientUsage.IdClient))
                    {
                        skippedDuplicates++;
                        continue;
                    }

                    if (clientUsage.Client != null &&
                        !FactureBillingEligibilityHelper.IsClientEligibleForBillingPeriod(
                            clientUsage.Client.DateCreation,
                            facture.MoisEmission,
                            facture.AnneesEmission))
                    {
                        skippedLateRegistration++;
                        continue;
                    }

                    // Calculer le montant total pour ce client (facture.Montant × nombreBatiment)
                    var nombreBatiment = clientUsage.nombreBatiment > 0 ? clientUsage.nombreBatiment : 1;
                    var montantTotal = facture.Montant.Value * nombreBatiment;

                    // Préparer le Mois (format string depuis MoisEmission)
                    var mois = facture.MoisEmission.ToString("D2"); // "01", "02", ..., "12"

                    var clientFacture = new ClientFacture
                    {
                        IdFacture = facture.IdFacture,
                        IdClient = clientUsage.IdClient,
                        Montant = montantTotal,
                        nombreBatiment = nombreBatiment, // Snapshot
                        MontantPaye = 0, // Aucun paiement initial
                        MontantDu = montantTotal, // Tout le montant est dû initialement
                        CodeDevisePrix = facture.CodeDevisePrix,
                        CodeDevisePrincipale = facture.CodeDevisePrincipale,
                        TauxVersDevisePrincipale = facture.TauxVersDevisePrincipale,
                        MontantDevisePrincipale = Math.Round(
                            montantTotal * (facture.TauxVersDevisePrincipale ?? 1m), 2, MidpointRounding.AwayFromZero),
                        MontantPayeDevisePrincipale = 0,
                        MontantDuDevisePrincipale = Math.Round(
                            montantTotal * (facture.TauxVersDevisePrincipale ?? 1m), 2, MidpointRounding.AwayFromZero),
                        Mois = mois,
                        Annees = facture.AnneesEmission,
                        DateEmission = facture.DateEmission ?? DateTime.Now,
                        EstArrierePreExistant = false, // Facture système
                        Statut = true,
                        DateCreation = DateTime.Now
                    };

                    clientFacturesToCreate.Add(clientFacture);
                }

                // Créer toutes les ClientFacture en batch
                if (clientFacturesToCreate.Count > 0)
                {
                    _context.ClientFactures.AddRange(clientFacturesToCreate);
                    await _context.SaveChangesAsync();
                }

                if (skippedDuplicates > 0 || skippedLateRegistration > 0)
                {
                    _logger.LogInformation(
                        "Facture {FactureId}: {Created} ClientFacture créée(s), {SkippedDuplicates} client(s) ignoré(s) (déjà facturé(s)), {SkippedLateRegistration} client(s) ignoré(s) (enregistrés à partir du {CutoffDay}, période {Mois}/{Annee}, usage {Usage}, type {Type})",
                        facture.IdFacture,
                        clientFacturesToCreate.Count,
                        skippedDuplicates,
                        skippedLateRegistration,
                        FactureBillingEligibilityHelper.RegistrationBillingCutoffDay,
                        facture.MoisEmission,
                        facture.AnneesEmission,
                        facture.IdUsage,
                        facture.IdTypeDeCourant);
                }
            }
            catch (Exception ex)
            {
                // Logger l'erreur mais ne pas faire échouer la création de la facture
                // L'erreur sera loggée par le système de logging
                throw new InvalidOperationException($"Erreur lors de la création des ClientFacture pour la facture {facture.IdFacture}: {ex.Message}", ex);
            }
        }

        public async Task<Facture> UpdateAsync(Facture facture)
        {
            var existing = await _context.Factures.FindAsync(facture.IdFacture);
            if (existing == null)
                return null;

            _context.Entry(existing).CurrentValues.SetValues(facture);
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var facture = await _context.Factures.FindAsync(id);
            if (facture == null)
                return false;

            _context.Factures.Remove(facture);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Factures.AnyAsync(f => f.IdFacture == id);
        }

        public async Task<bool> ExistsByNumeroFactureAsync(string numeroFacture)
        {
            return await _context.Factures
                .AnyAsync(f => f.NumeroFacture == numeroFacture);
        }

        public async Task<bool> ToggleStatutAsync(int id)
        {
            var facture = await _context.Factures.FindAsync(id);
            if (facture == null)
                return false;

            facture.Statut = !facture.Statut;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            var facture = await _context.Factures.FindAsync(id);
            if (facture == null)
                return false;

            facture.Statut = statut;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<Facture>> GetPagedAsync(PagedRequest request)
        {
            request ??= new PagedRequest();

            var query = _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Where(f => f.Statut == true);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(f =>
                    (f.NumeroFacture ?? string.Empty).ToLower().Contains(term));
            }

            query = request.SortBy switch
            {
                "DateEmission" => request.SortDescending ? query.OrderByDescending(f => f.DateEmission) : query.OrderBy(f => f.DateEmission),
                "MoisEmission" => request.SortDescending ? query.OrderByDescending(f => f.MoisEmission) : query.OrderBy(f => f.MoisEmission),
                "AnneesEmission" => request.SortDescending ? query.OrderByDescending(f => f.AnneesEmission) : query.OrderBy(f => f.AnneesEmission),
                "Montant" => request.SortDescending ? query.OrderByDescending(f => f.Montant) : query.OrderBy(f => f.Montant),
                _ => request.SortDescending ? query.OrderByDescending(f => f.IdFacture) : query.OrderBy(f => f.IdFacture)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Facture>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Enregistre un paiement pour une facture et met à jour le montant payé total
        /// </summary>
        public async Task<Paiement> EnregistrerPaiementAsync(int idFacture, Paiement paiement)
        {
            // Vérifier que la facture existe
            var facture = await _context.Factures.FindAsync(idFacture);
            if (facture == null)
            {
                throw new InvalidOperationException($"La facture avec l'ID {idFacture} n'existe pas.");
            }

            // Assigner l'ID de la facture
            paiement.IdFacture = idFacture;
            paiement.DateCreation = DateTime.Now;
            
            if (paiement.DatePaiement == default(DateTime))
            {
                paiement.DatePaiement = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(paiement.Statut))
            {
                paiement.Statut = "Validé";
            }

            // Enregistrer le paiement
            _context.Paiements.Add(paiement);
            await _context.SaveChangesAsync();

            // Note: Facture.Montant représente le montant initial de la facture et ne doit pas être modifié
            // Le montant payé est calculé dynamiquement depuis la table Paiement

            return paiement;
        }

        /// <summary>
        /// Récupère tous les paiements d'une facture
        /// </summary>
        public async Task<IEnumerable<Paiement>> GetPaiementsByFactureAsync(int idFacture)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.IdFacture == idFacture)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        /// <summary>
        /// Crée plusieurs factures en une seule transaction
        /// Utilise CreateExecutionStrategy() pour être compatible avec MySqlRetryingExecutionStrategy
        /// </summary>
        public async Task<BulkCreateFactureResponseDto> CreateBulkAsync(List<CreateFactureItemDto> facturesDto)
        {
            var response = new BulkCreateFactureResponseDto
            {
                Total = facturesDto.Count
            };

            if (facturesDto == null || facturesDto.Count == 0)
            {
                response.Message = "Aucune facture à créer";
                return response;
            }

            // Utiliser la stratégie d'exécution pour gérer les transactions avec MySqlRetryingExecutionStrategy
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // Créer la transaction à l'intérieur de la stratégie d'exécution
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var facturesCreees = new List<Facture>();
                    var facturesToCreate = new List<Facture>();

                    // Étape 1 : Valider et préparer toutes les factures
                    for (int i = 0; i < facturesDto.Count; i++)
                    {
                        var dto = facturesDto[i];
                        try
                        {
                            // Vérifier que l'usage existe
                            var usage = await _context.Usages.FindAsync(dto.IdUsage);
                            if (usage == null)
                            {
                                response.Erreurs.Add(new FactureErreurDto
                                {
                                    Index = i,
                                    Message = $"Usage avec l'ID {dto.IdUsage} non trouvé",
                                    CodeErreur = "USAGE_NOT_FOUND",
                                    Facture = dto
                                });
                                continue;
                            }

                            // 🎯 NOUVEAU: Vérifier le TypeDeCourant si spécifié
                            if (dto.IdTypeDeCourant.HasValue)
                            {
                                var typeDeCourant = await _context.TypeDeCourants.FindAsync(dto.IdTypeDeCourant.Value);
                                if (typeDeCourant == null)
                                {
                                    response.Erreurs.Add(new FactureErreurDto
                                    {
                                        Index = i,
                                        Message = $"Type de courant avec l'ID {dto.IdTypeDeCourant.Value} non trouvé",
                                        CodeErreur = "TYPE_COURANT_NOT_FOUND",
                                        Facture = dto
                                    });
                                    continue;
                                }
                            }

                            // Vérifier l'unicité du numéro de facture si fourni
                            string? numeroFacture = dto.NumeroFacture;
                            if (!string.IsNullOrWhiteSpace(numeroFacture) && numeroFacture.Trim().ToLower() != "string")
                            {
                                if (await ExistsByNumeroFactureAsync(numeroFacture))
                                {
                                    response.Erreurs.Add(new FactureErreurDto
                                    {
                                        Index = i,
                                        Message = $"Une facture avec le numéro '{numeroFacture}' existe déjà",
                                        CodeErreur = "NUMERO_FACTURE_EXISTS",
                                        Facture = dto
                                    });
                                    continue;
                                }
                            }
                            else
                            {
                                // Générer automatiquement un numéro avec TypeDeCourant
                                numeroFacture = await GenerateNumeroFactureAsync(dto.IdUsage, dto.DateEmission, dto.IdTypeDeCourant);
                            }

                            // Créer l'objet Facture
                            var facture = new Facture
                            {
                                NumeroFacture = numeroFacture,
                                Montant = dto.Montant,
                                DateEmission = dto.DateEmission ?? DateTime.Now,
                                MoisEmission = dto.MoisEmission,
                                AnneesEmission = dto.AnneesEmission,
                                IdUsage = dto.IdUsage,
                                IdTypeDeCourant = dto.IdTypeDeCourant, // 🎯 NOUVEAU
                                CodeDevisePrix = dto.CodeDevisePrix,
                                Statut = dto.Statut ?? true,
                                EstDiffusee = false, // ✨ Toujours false lors de la création
                                DateDiffusion = null, // ✨ Toujours null lors de la création
                                DateCreation = DateTime.Now
                            };

                            await ApplyFactureDeviseSnapshotAsync(facture, dto.CodeDevisePrix);

                            facturesToCreate.Add(facture);
                        }
                        catch (Exception ex)
                        {
                            response.Erreurs.Add(new FactureErreurDto
                            {
                                Index = i,
                                Message = $"Erreur lors de la préparation de la facture: {ex.Message}",
                                CodeErreur = "PREPARATION_ERROR",
                                Facture = dto
                            });
                        }
                    }

                    // Si toutes les factures ont des erreurs, annuler
                    if (facturesToCreate.Count == 0)
                    {
                        await transaction.RollbackAsync();
                        response.Echecs = response.Total;
                        response.Message = "Aucune facture valide à créer";
                        return response;
                    }

                    // Étape 2 : Créer toutes les factures en batch
                    _context.Factures.AddRange(facturesToCreate);
                    await _context.SaveChangesAsync();

                    // Étape 3 : Créer les ClientFacture pour chaque facture créée
                    foreach (var facture in facturesToCreate)
                    {
                        try
                        {
                            if (facture.IdUsage > 0 && facture.Montant.HasValue)
                            {
                                await CreateClientFacturesForFactureAsync(facture);
                            }

                            facturesCreees.Add(facture);
                        }
                        catch (Exception ex)
                        {
                            // Si la création de ClientFacture échoue, on continue quand même
                            // La facture est créée, mais les ClientFacture ne le sont pas
                            // On peut décider de rollback ou continuer selon le besoin
                            // Ici, on continue car la facture principale est créée
                        }
                    }

                    // Valider la transaction
                    await transaction.CommitAsync();

                    // Construire la réponse
                    for (int i = 0; i < facturesDto.Count; i++)
                    {
                        var dto = facturesDto[i];
                        var factureCreee = facturesCreees.FirstOrDefault(f => 
                            f.NumeroFacture == dto.NumeroFacture || 
                            (string.IsNullOrWhiteSpace(dto.NumeroFacture) && f.IdUsage == dto.IdUsage && f.Montant == dto.Montant));

                        if (factureCreee != null)
                        {
                            // Compter les ClientFacture créées
                            var nombreClientFactures = await _context.ClientFactures
                                .CountAsync(cf => cf.IdFacture == factureCreee.IdFacture && cf.Statut == true);

                            response.FacturesCreees.Add(new FactureSuccesDto
                            {
                                Index = i,
                                IdFacture = factureCreee.IdFacture,
                                NumeroFacture = factureCreee.NumeroFacture,
                                IdUsage = factureCreee.IdUsage,
                                NombreClientFacturesCreees = nombreClientFactures
                            });
                            response.Succes++;
                        }
                    }

                    response.Echecs = response.Total - response.Succes;
                    response.Message = response.TousSucces
                        ? $"Toutes les {response.Succes} facture(s) ont été créées avec succès"
                        : $"{response.Succes} facture(s) créée(s) avec succès, {response.Echecs} échec(s)";

                    return response;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    response.Echecs = response.Total;
                    response.Message = $"Erreur lors de la création en masse: {ex.Message}";
                    
                    // Ajouter une erreur générale si aucune erreur spécifique n'existe
                    if (response.Erreurs.Count == 0)
                    {
                        response.Erreurs.Add(new FactureErreurDto
                        {
                            Index = -1,
                            Message = ex.Message,
                            CodeErreur = "BULK_CREATE_ERROR"
                        });
                    }

                    return response;
                }
            });
        }
    }
}

