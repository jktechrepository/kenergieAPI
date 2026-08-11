using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Models.DTOs.Paiement;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    public class PaiementService : IPaiementRepository
    {
        private readonly KenergieDbContext _context;
        private readonly IClientFactureRepository _clientFactureRepository;
        private readonly IDeviseConversionService _deviseConversionService;

        public PaiementService(
            KenergieDbContext context,
            IClientFactureRepository clientFactureRepository,
            IDeviseConversionService deviseConversionService)
        {
            _context = context;
            _clientFactureRepository = clientFactureRepository;
            _deviseConversionService = deviseConversionService;
        }

        public async Task<IEnumerable<Paiement>> GetAllAsync()
        {
            return await _context.Paiements
                .Include(p => p.Facture)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.IsDeleted == false)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<Paiement?> GetByIdAsync(int id)
        {
            var paiement = await _context.Paiements
                .Include(p => p.Facture)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.IsDeleted == false)
                .FirstOrDefaultAsync(p => p.IdPaiement == id);

            if (paiement != null)
            {
                await EnrichPaiementsWithClientFactureAsync(new List<Paiement> { paiement });
            }

            return paiement;
        }

        public async Task<IEnumerable<Paiement>> GetByFactureAsync(int idFacture)
        {
            var paiements = await _context.Paiements
                .Include(p => p.Facture)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.IdFacture == idFacture && p.IsDeleted == false)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();

            await EnrichPaiementsWithClientFactureAsync(paiements);
            return paiements;
        }

        public async Task<IEnumerable<Paiement>> GetByClientAsync(int idClient)
        {
            var paiements = await _context.Paiements
                .Include(p => p.Facture)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.IdClient == idClient && p.IsDeleted == false)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();

            await EnrichPaiementsWithClientFactureAsync(paiements);
            return paiements;
        }

        public async Task<IEnumerable<Paiement>> GetBySocieteAsync(int idSociete)
        {
            var paiements = await _context.Paiements
                .Include(p => p.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.Facture != null && 
                           p.Facture.Statut == true &&
                           p.Facture.Usage != null &&
                           p.Facture.Usage.CategorieClient != null && 
                           p.Facture.Usage.CategorieClient.IdSociete == idSociete &&
                           p.IsDeleted == false)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();

            await EnrichPaiementsWithClientFactureAsync(paiements);
            return paiements;
        }

        public async Task<PagedResult<Paiement>> GetBySocietePagedAsync(int idSociete, PagedRequest request)
        {
            request ??= new PagedRequest();

            var query = _context.Paiements
                .Include(p => p.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.Facture != null && 
                           p.Facture.Statut == true &&
                           p.Facture.Usage != null &&
                           p.Facture.Usage.CategorieClient != null && 
                           p.Facture.Usage.CategorieClient.IdSociete == idSociete &&
                           p.IsDeleted == false);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    (p.ReferenceTransaction ?? string.Empty).ToLower().Contains(term) ||
                    (p.MethodePaiement ?? string.Empty).ToLower().Contains(term) ||
                    (p.Commentaire ?? string.Empty).ToLower().Contains(term) ||
                    (p.Facture != null && p.Facture.NumeroFacture != null && 
                     p.Facture.NumeroFacture.ToLower().Contains(term)) ||
                    (p.Client != null && p.Client.NomClient != null && 
                     p.Client.NomClient.ToLower().Contains(term)));
            }

            query = request.SortBy switch
            {
                "DatePaiement" or "date" => request.SortDescending 
                    ? query.OrderByDescending(p => p.DatePaiement) 
                    : query.OrderBy(p => p.DatePaiement),
                "MontantPaye" or "Montant" or "montant" => request.SortDescending 
                    ? query.OrderByDescending(p => p.MontantPaye) 
                    : query.OrderBy(p => p.MontantPaye),
                "Statut" or "statut" => request.SortDescending 
                    ? query.OrderByDescending(p => p.Statut) 
                    : query.OrderBy(p => p.Statut),
                "MethodePaiement" or "methode" => request.SortDescending 
                    ? query.OrderByDescending(p => p.MethodePaiement) 
                    : query.OrderBy(p => p.MethodePaiement),
                _ => query.OrderByDescending(p => p.DatePaiement) // Tri par défaut : DatePaiement desc
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            await EnrichPaiementsWithClientFactureAsync(data);

            return new PagedResult<Paiement>(data, total, request.PageNumber, request.PageSize);
        }

        public async Task<Paiement> CreateAsync(Paiement paiement)
        {
            paiement.DateCreation = DateTime.Now;
            if (paiement.DatePaiement == default(DateTime))
            {
                paiement.DatePaiement = DateTime.Now;
            }
            // Normaliser le statut : accepter "true", "True", "TRUE" et les convertir en "Validé"
            if (string.IsNullOrWhiteSpace(paiement.Statut) || 
                paiement.Statut.ToLower() == "true")
            {
                paiement.Statut = "Validé";
            }

            await EnsureMontantNeDepassePasDuAsync(paiement);
            await ApplyPaiementDeviseSnapshotAsync(paiement);

            _context.Paiements.Add(paiement);
            await _context.SaveChangesAsync();

            // ✨ NOUVEAU : Mettre à jour les ClientFacture après création du paiement
            if (paiement.EstPaiementArriere && paiement.IdClientFacture.HasValue)
            {
                // Pour les arriérés pré-existants, mettre à jour directement la ClientFacture
                await UpdateClientFactureArriereAfterPaiementAsync(paiement.IdClientFacture.Value, paiement.MontantPaye);
            }
            else if (paiement.IdFacture.HasValue && paiement.IdFacture.Value > 0 && paiement.IdClient.HasValue)
            {
                // Pour les factures système, utiliser la logique existante
                await UpdateClientFactureAfterPaiementAsync(paiement.IdFacture.Value, paiement.IdClient.Value);
            }

            await EnrichAndPersistPaiementClientFactureFieldsAsync(paiement);

            return paiement;
        }

        /// <summary>
        /// Refuse un paiement dont le montant dépasse le reste à payer (ClientFacture.MontantDu).
        /// Couvre arriérés (IdClientFacture) et factures système (IdFacture + IdClient).
        /// </summary>
        private async Task EnsureMontantNeDepassePasDuAsync(Paiement paiement)
        {
            var clientFacture = await ResolveClientFactureForPaiementAsync(paiement);
            if (clientFacture == null)
                return;

            var montantDu = clientFacture.MontantDu ?? 0;
            if (paiement.MontantPaye > montantDu)
            {
                throw new ArgumentException(
                    $"Le montant payé ({paiement.MontantPaye}) dépasse le montant dû ({montantDu})");
            }
        }

        /// <summary>
        /// Phase 1 : le paiement doit être dans la même devise que la ClientFacture / Facture.
        /// </summary>
        private async Task ApplyPaiementDeviseSnapshotAsync(Paiement paiement)
        {
            var clientFacture = await ResolveClientFactureForPaiementAsync(paiement);
            string? codeFacture = clientFacture?.CodeDevisePrix;

            if (string.IsNullOrWhiteSpace(codeFacture) && paiement.IdFacture.HasValue)
            {
                codeFacture = await _context.Factures
                    .Where(f => f.IdFacture == paiement.IdFacture.Value)
                    .Select(f => f.CodeDevisePrix)
                    .FirstOrDefaultAsync();
            }

            var idSociete = await ResolveIdSocieteForPaiementAsync(paiement, clientFacture);
            if (!idSociete.HasValue)
                throw new InvalidOperationException("Impossible de résoudre la société pour le paiement.");

            var principale = await _deviseConversionService.GetCodeDevisePrincipaleAsync(idSociete.Value);
            codeFacture = DeviseConversionService.NormalizeCode(
                !string.IsNullOrWhiteSpace(codeFacture) ? codeFacture! : principale);

            var codePaiement = DeviseConversionService.NormalizeCode(
                !string.IsNullOrWhiteSpace(paiement.CodeDevisePaiement)
                    ? paiement.CodeDevisePaiement!
                    : codeFacture);

            if (!string.Equals(codePaiement, codeFacture, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"Le paiement doit être dans la même devise que la facture ({codeFacture}). Devise reçue: {codePaiement}.");
            }

            var conversion = await _deviseConversionService.ConvertirVersPrincipaleAsync(
                idSociete.Value, codePaiement, paiement.MontantPaye, paiement.DatePaiement);

            paiement.CodeDevisePaiement = codePaiement;
            paiement.CodeDevisePrincipale = principale;
            paiement.TauxVersDevisePrincipale = conversion.Taux;
            paiement.MontantPayeDevisePrincipale = conversion.MontantConverti;
        }

        private async Task<int?> ResolveIdSocieteForPaiementAsync(Paiement paiement, ClientFacture? clientFacture)
        {
            if (paiement.IdFacture.HasValue)
            {
                var fromFacture = await _context.Factures
                    .Where(f => f.IdFacture == paiement.IdFacture.Value)
                    .Select(f => (int?)f.Usage!.CategorieClient!.IdSociete)
                    .FirstOrDefaultAsync();
                if (fromFacture.HasValue) return fromFacture;
            }

            if (clientFacture != null)
            {
                if (clientFacture.IdFacture.HasValue)
                {
                    var fromCfFacture = await _context.Factures
                        .Where(f => f.IdFacture == clientFacture.IdFacture.Value)
                        .Select(f => (int?)f.Usage!.CategorieClient!.IdSociete)
                        .FirstOrDefaultAsync();
                    if (fromCfFacture.HasValue) return fromCfFacture;
                }

                return await _context.ClientUsages
                    .Where(cu => cu.IdClient == clientFacture.IdClient && cu.Statut == true)
                    .Select(cu => (int?)cu.Usage!.CategorieClient!.IdSociete)
                    .FirstOrDefaultAsync();
            }

            if (paiement.IdClient.HasValue)
            {
                return await _context.ClientUsages
                    .Where(cu => cu.IdClient == paiement.IdClient.Value && cu.Statut == true)
                    .Select(cu => (int?)cu.Usage!.CategorieClient!.IdSociete)
                    .FirstOrDefaultAsync();
            }

            return null;
        }

        public async Task<Paiement?> UpdateAsync(Paiement paiement)
        {
            var existing = await _context.Paiements.FindAsync(paiement.IdPaiement);
            if (existing == null)
                return null;

            // Sauvegarder les anciennes valeurs pour la mise à jour des ClientFacture
            var oldIdFacture = existing.IdFacture;
            var oldIdClient = existing.IdClient;

            _context.Entry(existing).CurrentValues.SetValues(paiement);
            await _context.SaveChangesAsync();

            // ✨ NOUVEAU : Mettre à jour les ClientFacture après modification du paiement
            // Mettre à jour l'ancienne ClientFacture (si client/facture changés)
            if (oldIdFacture.HasValue && oldIdFacture.Value > 0 && oldIdClient.HasValue && 
                (oldIdFacture != paiement.IdFacture || oldIdClient != paiement.IdClient))
            {
                await UpdateClientFactureAfterPaiementAsync(oldIdFacture.Value, oldIdClient.Value);
            }

            // Mettre à jour la nouvelle ClientFacture
            if (paiement.IdFacture.HasValue && paiement.IdFacture.Value > 0 && paiement.IdClient.HasValue)
            {
                await UpdateClientFactureAfterPaiementAsync(paiement.IdFacture.Value, paiement.IdClient.Value);
            }

            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var paiement = await _context.Paiements.FindAsync(id);
            if (paiement == null)
                return false;

            // Sauvegarder les valeurs avant suppression pour la mise à jour des ClientFacture
            var idFacture = paiement.IdFacture;
            var idClient = paiement.IdClient;

            // ✨ Soft delete : mettre IsDeleted à true au lieu de supprimer
            paiement.IsDeleted = true;
            await _context.SaveChangesAsync();

            // ✨ NOUVEAU : Mettre à jour les ClientFacture après suppression du paiement
            if (idFacture.HasValue && idFacture.Value > 0 && idClient.HasValue)
            {
                await UpdateClientFactureAfterPaiementAsync(idFacture.Value, idClient.Value);
            }

            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Paiements.AnyAsync(p => p.IdPaiement == id);
        }

        public async Task<PagedResult<Paiement>> GetPagedAsync(PagedRequest request)
        {
            request ??= new PagedRequest();

            var query = _context.Paiements
                .Include(p => p.Facture)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.IsDeleted == false)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    (p.ReferenceTransaction ?? string.Empty).ToLower().Contains(term) ||
                    (p.MethodePaiement ?? string.Empty).ToLower().Contains(term) ||
                    (p.Commentaire ?? string.Empty).ToLower().Contains(term));
            }

            query = request.SortBy switch
            {
                "DatePaiement" => request.SortDescending ? query.OrderByDescending(p => p.DatePaiement) : query.OrderBy(p => p.DatePaiement),
                "MontantPaye" or "Montant" => request.SortDescending ? query.OrderByDescending(p => p.MontantPaye) : query.OrderBy(p => p.MontantPaye),
                "Statut" => request.SortDescending ? query.OrderByDescending(p => p.Statut) : query.OrderBy(p => p.Statut),
                _ => query.OrderByDescending(p => p.DatePaiement) // Tri par défaut : DatePaiement desc
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Paiement>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// ✨ AMÉLIORÉ : Récupère les factures impayées d'une société en utilisant ClientFacture comme source de vérité
        /// Tient compte de nombreBatiment et des montants pré-calculés (MontantPaye, MontantDu)
        /// </summary>
        public async Task<IEnumerable<FactureImpayeeDto>> GetFacturesImpayeesBySocieteAsync(int idSociete)
        {
            // ✨ NOUVEAU : Récupérer les ClientFacture avec arriérés (MontantDu > 0) pour cette société
            var clientFactures = await _context.ClientFactures
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Where(cf => cf.Statut == true &&
                             cf.MontantDu.HasValue &&
                             cf.MontantDu.Value > 0 &&
                             cf.Facture != null &&
                             cf.Facture.Statut == true &&
                             cf.Facture.Usage != null &&
                             cf.Facture.Usage.CategorieClient != null &&
                             cf.Facture.Usage.CategorieClient.IdSociete == idSociete)
                .ToListAsync();

            // Agréger par facture pour obtenir les totaux consolidés
            // Filtrer les ClientFacture avec IdFacture null (arriérés pré-existants sans facture associée)
            var facturesImpayees = clientFactures
                .Where(cf => cf.IdFacture.HasValue)
                .GroupBy(cf => cf.IdFacture!.Value)
                .Select(g => new
                {
                    IdFacture = g.Key,
                    Facture = g.First().Facture,
                    MontantTotalConsolide = g.Where(cf => cf.Montant.HasValue).Sum(cf => cf.Montant.Value),
                    MontantPayeConsolide = g.Where(cf => cf.MontantPaye.HasValue).Sum(cf => cf.MontantPaye.Value),
                    MontantDuConsolide = g.Where(cf => cf.MontantDu.HasValue).Sum(cf => cf.MontantDu.Value),
                    NombreClients = g.Count()
                })
                .Select(x => new FactureImpayeeDto
                {
                    IdFacture = x.IdFacture,
                    NumeroFacture = x.Facture?.NumeroFacture,
                    DateEmission = x.Facture?.DateEmission,
                    MoisEmission = x.Facture?.MoisEmission ?? 0,
                    AnneesEmission = x.Facture?.AnneesEmission ?? 0,
                    MontantTotal = x.MontantTotalConsolide,
                    MontantPaye = x.MontantPayeConsolide,
                    MontantDu = x.MontantDuConsolide,
                    JoursRetard = x.Facture?.DateEmission.HasValue == true
                        ? (DateTime.Now - x.Facture.DateEmission.Value).Days
                        : (int?)null,
                    NomCategorie = x.Facture?.Usage?.Libelle,
                    NombreClientsAvecArrieres = x.NombreClients
                })
                .OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                .ToList();

            return facturesImpayees;
        }

        /// <summary>
        /// ✨ AMÉLIORÉ : Récupère les factures impayées d'une société avec pagination, tri et recherche
        /// Utilise ClientFacture comme source de vérité pour garantir la cohérence des montants
        /// </summary>
        public async Task<PagedResult<FactureImpayeeDto>> GetFacturesImpayeesBySocietePagedAsync(int idSociete, PagedRequest request)
        {
            request ??= new PagedRequest();

            // ✨ NOUVEAU : Récupérer les ClientFacture avec arriérés (MontantDu > 0) pour cette société
            var clientFactures = await _context.ClientFactures
                .Include(cf => cf.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Where(cf => cf.Statut == true &&
                             cf.MontantDu.HasValue &&
                             cf.MontantDu.Value > 0 &&
                             cf.Facture != null &&
                             cf.Facture.Statut == true &&
                             cf.Facture.Usage != null &&
                             cf.Facture.Usage.CategorieClient != null &&
                             cf.Facture.Usage.CategorieClient.IdSociete == idSociete)
                .ToListAsync();

            // Agréger par facture pour obtenir les totaux consolidés
            // Filtrer les ClientFacture avec IdFacture null (arriérés pré-existants sans facture associée)
            var facturesImpayees = clientFactures
                .Where(cf => cf.IdFacture.HasValue)
                .GroupBy(cf => cf.IdFacture!.Value)
                .Select(g => new
                {
                    IdFacture = g.Key,
                    Facture = g.First().Facture,
                    MontantTotalConsolide = g.Where(cf => cf.Montant.HasValue).Sum(cf => cf.Montant.Value),
                    MontantPayeConsolide = g.Where(cf => cf.MontantPaye.HasValue).Sum(cf => cf.MontantPaye.Value),
                    MontantDuConsolide = g.Where(cf => cf.MontantDu.HasValue).Sum(cf => cf.MontantDu.Value),
                    NombreClients = g.Count()
                })
                .Select(x => new FactureImpayeeDto
                {
                    IdFacture = x.IdFacture,
                    NumeroFacture = x.Facture?.NumeroFacture,
                    DateEmission = x.Facture?.DateEmission,
                    MoisEmission = x.Facture?.MoisEmission ?? 0,
                    AnneesEmission = x.Facture?.AnneesEmission ?? 0,
                    MontantTotal = x.MontantTotalConsolide,
                    MontantPaye = x.MontantPayeConsolide,
                    MontantDu = x.MontantDuConsolide,
                    JoursRetard = x.Facture?.DateEmission.HasValue == true
                        ? (DateTime.Now - x.Facture.DateEmission.Value).Days
                        : (int?)null,
                    NomCategorie = x.Facture?.Usage?.Libelle,
                    NombreClientsAvecArrieres = x.NombreClients
                })
                .AsQueryable();

            // Recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                facturesImpayees = facturesImpayees.Where(f =>
                    (f.NumeroFacture ?? string.Empty).ToLower().Contains(term) ||
                    (f.NomCategorie ?? string.Empty).ToLower().Contains(term));
            }

            // Tri
            facturesImpayees = request.SortBy switch
            {
                "MontantDu" or "montantDu" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.MontantDu)
                    : facturesImpayees.OrderBy(f => f.MontantDu),
                "MontantTotal" or "montantTotal" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.MontantTotal)
                    : facturesImpayees.OrderBy(f => f.MontantTotal),
                "DateEmission" or "date" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                    : facturesImpayees.OrderBy(f => f.DateEmission ?? DateTime.MinValue),
                "NumeroFacture" or "numero" => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.NumeroFacture ?? string.Empty)
                    : facturesImpayees.OrderBy(f => f.NumeroFacture ?? string.Empty),
                _ => request.SortDescending
                    ? facturesImpayees.OrderByDescending(f => f.DateEmission ?? DateTime.MinValue)
                    : facturesImpayees.OrderBy(f => f.DateEmission ?? DateTime.MinValue)
            };

            // Compter le total avant pagination
            var total = facturesImpayees.Count();

            // Pagination en mémoire
            var data = facturesImpayees
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new PagedResult<FactureImpayeeDto>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Calcule le montant total payé pour une facture (somme des paiements validés)
        /// </summary>
        public async Task<decimal> GetTotalPaiementsByFactureAsync(int idFacture)
        {
            return await _context.Paiements
                .Where(p => p.IdFacture == idFacture && p.Statut == "Validé" && p.IsDeleted == false)
                .SumAsync(p => p.MontantPaye);
        }

        /// <summary>
        /// Met à jour la ClientFacture après un paiement (création, modification ou suppression)
        /// Recalcule MontantPaye et MontantDu depuis la table Paiements
        /// </summary>
        private async Task UpdateClientFactureAfterPaiementAsync(int idFacture, int idClient)
        {
            try
            {
                // Récupérer ou créer la ClientFacture
                var clientFacture = await _clientFactureRepository.GetByClientAndFactureAsync(idClient, idFacture);

                if (clientFacture == null)
                {
                    // Si la ClientFacture n'existe pas, la créer automatiquement
                    // Cela peut arriver si la facture a été créée avant l'implémentation de ClientFacture
                    var facture = await _context.Factures
                        .Include(f => f.Usage)
                        .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

                    if (facture == null)
                        return;

                    // Trouver le ClientUsage pour obtenir nombreBatiment
                    var clientUsage = await _context.ClientUsages
                        .FirstOrDefaultAsync(cu => cu.IdClient == idClient && 
                                                   cu.IdUsage == facture.IdUsage && 
                                                   cu.Statut == true);

                    var nombreBatiment = clientUsage?.nombreBatiment ?? 1;
                    var montantTotal = (facture.Montant ?? 0) * nombreBatiment;
                    var mois = facture.MoisEmission.ToString("D2");

                    clientFacture = new ClientFacture
                    {
                        IdFacture = idFacture,
                        IdClient = idClient,
                        Montant = montantTotal,
                        nombreBatiment = nombreBatiment,
                        Mois = mois,
                        Annees = facture.AnneesEmission,
                        DateEmission = facture.DateEmission ?? DateTime.Now,
                        EstArrierePreExistant = false,
                        Statut = true,
                        DateCreation = DateTime.Now
                    };

                    await _clientFactureRepository.CreateAsync(clientFacture);
                }

                // Recalculer le montant payé depuis la table Paiements
                var montantPaye = await _context.Paiements
                    .Where(p => p.IdFacture == idFacture && 
                               p.IdClient == idClient && 
                               p.Statut != null &&
                               (p.Statut == "Validé" || p.Statut.ToLower() == "true") &&
                               p.IsDeleted == false)
                    .SumAsync(p => p.MontantPaye);

                // Mettre à jour la ClientFacture
                clientFacture.MontantPaye = montantPaye;
                if (clientFacture.Montant.HasValue)
                {
                    clientFacture.MontantDu = clientFacture.Montant.Value - montantPaye;
                }
                PaiementClientFactureEnrichment.RecalculateDevisePrincipaleBalances(clientFacture);
                clientFacture.DateModification = DateTime.Now;

                await _clientFactureRepository.UpdateAsync(clientFacture);
            }
            catch (Exception ex)
            {
                // Logger l'erreur mais ne pas faire échouer l'opération de paiement
                // L'erreur sera loggée par le système de logging
                // On peut aussi utiliser un logger injecté si nécessaire
                throw new InvalidOperationException($"Erreur lors de la mise à jour de ClientFacture pour facture {idFacture} et client {idClient}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Met à jour une ClientFacture d'arriéré pré-existant après un paiement
        /// </summary>
        private async Task UpdateClientFactureArriereAfterPaiementAsync(int idClientFacture, decimal montantPaye)
        {
            try
            {
                var clientFacture = await _clientFactureRepository.GetByIdAsync(idClientFacture);
                if (clientFacture == null)
                {
                    return; // Ne pas faire échouer si la ClientFacture n'existe pas
                }

                // Mettre à jour les montants
                var ancienMontantPaye = clientFacture.MontantPaye ?? 0;
                clientFacture.MontantPaye = ancienMontantPaye + montantPaye;
                
                if (clientFacture.Montant.HasValue)
                {
                    clientFacture.MontantDu = clientFacture.Montant.Value - clientFacture.MontantPaye.Value;
                }
                PaiementClientFactureEnrichment.RecalculateDevisePrincipaleBalances(clientFacture);
                
                clientFacture.DateModification = DateTime.Now;

                await _clientFactureRepository.UpdateAsync(clientFacture);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erreur lors de la mise à jour de ClientFacture d'arriéré {idClientFacture}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Récupère les paiements par société avec filtres étendus et totaux
        /// </summary>
        public async Task<PagedResultPaiement> GetBySocietePagedWithFiltersAsync(int idSociete, PaiementPagedRequest request)
        {
            request ??= new PaiementPagedRequest();

            // Validation des filtres
            if (!request.IsValid())
            {
                throw new ArgumentException("Les filtres de date ne sont pas valides");
            }

            // Requête de base avec les includes nécessaires
            var query = _context.Paiements
                .Include(p => p.Facture)
                    .ThenInclude(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(p => p.Client)
                .Include(p => p.Utilisateur)
                .Where(p => p.Facture != null && 
                           p.Facture.Statut == true &&
                           p.Facture.Usage != null &&
                           p.Facture.Usage.CategorieClient != null && 
                           p.Facture.Usage.CategorieClient.IdSociete == idSociete &&
                           p.IsDeleted == false);

            // Appliquer les filtres
            query = ApplyFilters(query, request);

            // Appliquer la recherche
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    (p.ReferenceTransaction ?? string.Empty).ToLower().Contains(term) ||
                    (p.MethodePaiement ?? string.Empty).ToLower().Contains(term) ||
                    (p.Commentaire ?? string.Empty).ToLower().Contains(term) ||
                    (p.Facture != null && p.Facture.NumeroFacture != null && 
                     p.Facture.NumeroFacture.ToLower().Contains(term)) ||
                    (p.Client != null && p.Client.NomClient != null && 
                     p.Client.NomClient.ToLower().Contains(term)));
            }

            // Appliquer le tri
            query = request.SortBy switch
            {
                "DatePaiement" or "date" => request.SortDescending 
                    ? query.OrderByDescending(p => p.DatePaiement) 
                    : query.OrderBy(p => p.DatePaiement),
                "MontantPaye" or "Montant" or "montant" => request.SortDescending 
                    ? query.OrderByDescending(p => p.MontantPaye) 
                    : query.OrderBy(p => p.MontantPaye),
                "Statut" or "statut" => request.SortDescending 
                    ? query.OrderByDescending(p => p.Statut) 
                    : query.OrderBy(p => p.Statut),
                "MethodePaiement" or "methode" => request.SortDescending 
                    ? query.OrderByDescending(p => p.MethodePaiement) 
                    : query.OrderBy(p => p.MethodePaiement),
                _ => query.OrderByDescending(p => p.DatePaiement) // Tri par défaut : DatePaiement desc
            };

            // Calculer les totaux avant pagination
            var total = await query.CountAsync();
            var montantTotal = await query.SumAsync(p => p.MontantPaye);
            var nombreTotalCollecteur = await query
                .Where(p => p.IdUtilisateur.HasValue)
                .Select(p => p.IdUtilisateur.Value)
                .Distinct()
                .CountAsync();

            // Appliquer la pagination
            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            await EnrichPaiementsWithClientFactureAsync(data);

            return new PagedResultPaiement(data, total, request.PageNumber, request.PageSize, montantTotal, nombreTotalCollecteur);
        }

        /// <summary>
        /// Enrichit les paiements avec IdClientFacture, MontantAPaye (= ClientFacture.Montant) et ResteAPaye (= ClientFacture.MontantDu).
        /// Aligné sur GET /api/ClientFacture/client/{idClient}/arrieres (mêmes champs Montant / MontantDu).
        /// </summary>
        private async Task EnrichPaiementsWithClientFactureAsync(IList<Paiement> paiements)
        {
            if (paiements.Count == 0)
            {
                return;
            }

            var explicitIds = paiements
                .Where(p => p.IdClientFacture.HasValue)
                .Select(p => p.IdClientFacture!.Value)
                .Distinct()
                .ToList();

            var clientIds = paiements
                .Where(p => p.IdClient.HasValue)
                .Select(p => p.IdClient!.Value)
                .Distinct()
                .ToList();

            if (explicitIds.Count == 0 && clientIds.Count == 0)
            {
                return;
            }

            // Même source que GET /api/ClientFacture/client/{id}/arrieres (ClientFacture active, sans filtre MontantDu > 0)
            var clientFactures = await _context.ClientFactures
                .AsNoTracking()
                .Where(cf => cf.Statut == true &&
                    (explicitIds.Contains(cf.IdClientFacture) ||
                     (clientIds.Contains(cf.IdClient) && cf.IdFacture.HasValue)))
                .ToListAsync();

            var byId = clientFactures.ToDictionary(cf => cf.IdClientFacture);
            var byClientAndFacture = PaiementClientFactureEnrichment.IndexByClientAndFacture(clientFactures);

            foreach (var paiement in paiements)
            {
                var clientFacture = PaiementClientFactureEnrichment.Resolve(paiement, byId, byClientAndFacture);
                if (clientFacture == null)
                {
                    clientFacture = await ResolveClientFactureForPaiementAsync(paiement);
                }

                if (clientFacture != null)
                {
                    PaiementClientFactureEnrichment.Apply(paiement, clientFacture);
                }
            }
        }

        private async Task EnrichAndPersistPaiementClientFactureFieldsAsync(Paiement paiement)
        {
            var clientFacture = await ResolveClientFactureForPaiementAsync(paiement);
            if (clientFacture == null)
            {
                return;
            }

            PaiementClientFactureEnrichment.Apply(paiement, clientFacture);
            paiement.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        private async Task<ClientFacture?> ResolveClientFactureForPaiementAsync(Paiement paiement)
        {
            if (paiement.IdClientFacture.HasValue)
            {
                return await _context.ClientFactures
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cf => cf.IdClientFacture == paiement.IdClientFacture.Value && cf.Statut == true);
            }

            if (paiement.IdFacture.HasValue && paiement.IdClient.HasValue)
            {
                return await _clientFactureRepository.GetByClientAndFactureAsync(
                    paiement.IdClient.Value,
                    paiement.IdFacture.Value);
            }

            return null;
        }

        /// <summary>
        /// Applique les filtres sur la requête de paiements
        /// </summary>
        private IQueryable<Paiement> ApplyFilters(IQueryable<Paiement> query, PaiementPagedRequest request)
        {
            // Filtre par date spécifique
            if (request.Date.HasValue)
            {
                var targetDate = request.Date.Value.Date;
                query = query.Where(p => p.DatePaiement.Date == targetDate);
            }

            // Filtre par collecteur
            if (request.IdUtilisateur.HasValue)
            {
                query = query.Where(p => p.IdUtilisateur == request.IdUtilisateur.Value);
            }

            // Filtre par période
            if (request.DateDebut.HasValue && request.DateFin.HasValue)
            {
                query = query.Where(p => p.DatePaiement.Date >= request.DateDebut.Value.Date && 
                                       p.DatePaiement.Date <= request.DateFin.Value.Date);
            }
            else if (request.DateDebut.HasValue)
            {
                query = query.Where(p => p.DatePaiement.Date >= request.DateDebut.Value.Date);
            }
            else if (request.DateFin.HasValue)
            {
                query = query.Where(p => p.DatePaiement.Date <= request.DateFin.Value.Date);
            }

            // Filtre par mois/année
            if (request.Mois.HasValue)
            {
                query = query.Where(p => p.DatePaiement.Month == request.Mois.Value);
            }

            if (request.Annee.HasValue)
            {
                query = query.Where(p => p.DatePaiement.Year == request.Annee.Value);
            }

            // Filtre par axe (via Client)
            if (request.IdAxe.HasValue)
            {
                query = query.Where(p => p.Client != null && p.Client.IdAxe == request.IdAxe.Value);
            }

            return query;
        }
    }
}

