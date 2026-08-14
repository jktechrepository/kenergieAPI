using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Depense;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class DepenseService : IDepenseRepository
    {
        private static readonly HashSet<string> RolesCreation = new(StringComparer.OrdinalIgnoreCase)
        {
            "Financier"
        };

        private static readonly HashSet<string> RolesValidation = new(StringComparer.OrdinalIgnoreCase)
        {
            "Admin", "Gerant"
        };

        private readonly KenergieDbContext _context;
        private readonly IDeviseConversionService _deviseConversion;
        private readonly ILogger<DepenseService> _logger;

        public DepenseService(
            KenergieDbContext context,
            IDeviseConversionService deviseConversion,
            ILogger<DepenseService> logger)
        {
            _context = context;
            _deviseConversion = deviseConversion;
            _logger = logger;
        }

        public async Task<PagedResult<DepenseResponseDto>> GetPagedAsync(
            PagedRequest request,
            int? idSociete,
            int callerUserId,
            string callerRole,
            int callerSocieteId,
            DateTime? dateDebut = null,
            DateTime? dateFin = null,
            int? idCategorieDepense = null,
            string? statut = null)
        {
            EnsureCanRead(callerRole);
            request ??= new PagedRequest();

            var societeScope = ResolveSocieteScope(idSociete, callerRole, callerSocieteId);

            var query = QueryBase().Where(d => !d.IsDeleted);

            if (societeScope.HasValue)
                query = query.Where(d => d.IdSociete == societeScope.Value);

            if (dateDebut.HasValue)
                query = query.Where(d => d.DateDepense >= dateDebut.Value);

            if (dateFin.HasValue)
                query = query.Where(d => d.DateDepense <= dateFin.Value);

            if (idCategorieDepense.HasValue)
                query = query.Where(d => d.IdCategorieDepense == idCategorieDepense.Value);

            if (!string.IsNullOrWhiteSpace(statut))
                query = query.Where(d => d.Statut == statut);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(d =>
                    d.Libelle.ToLower().Contains(term) ||
                    (d.Beneficiaire != null && d.Beneficiaire.ToLower().Contains(term)) ||
                    (d.ReferencePiece != null && d.ReferencePiece.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();

            query = request.SortDescending
                ? query.OrderByDescending(d => d.DateDepense)
                : query.OrderBy(d => d.DateDepense);

            var depenses = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var items = depenses.Select(MapToDto).ToList();

            return new PagedResult<DepenseResponseDto>(items, total, request.PageNumber, request.PageSize);
        }

        public async Task<DepenseResponseDto?> GetByIdAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanRead(callerRole);

            var depense = await QueryBase()
                .FirstOrDefaultAsync(d => d.IdDepense == id && !d.IsDeleted);

            if (depense == null)
                return null;

            EnsureSocieteAccess(depense.IdSociete, callerRole, callerSocieteId);
            return MapToDto(depense);
        }

        public async Task<DepenseMoisResponseDto> GetByMoisAsync(
            int mois,
            int annee,
            int? idSociete,
            int callerUserId,
            string callerRole,
            int callerSocieteId,
            string? statut = null)
        {
            EnsureCanRead(callerRole);

            if (mois < 1 || mois > 12)
                throw new ArgumentException("Le mois doit être compris entre 1 et 12.");

            if (annee < 2000 || annee > 2100)
                throw new ArgumentException("Année invalide.");

            var statutFilter = NormalizeStatutFilter(statut);

            var dateDebut = new DateTime(annee, mois, 1, 0, 0, 0, DateTimeKind.Utc);
            var dateFinExclusive = dateDebut.AddMonths(1);
            var dateFin = dateFinExclusive.AddTicks(-1);

            var societeScope = ResolveSocieteScope(idSociete, callerRole, callerSocieteId);

            var query = QueryBase()
                .Where(d => !d.IsDeleted
                    && d.DateDepense >= dateDebut
                    && d.DateDepense < dateFinExclusive);

            if (societeScope.HasValue)
                query = query.Where(d => d.IdSociete == societeScope.Value);

            if (statutFilter != null)
                query = query.Where(d => d.Statut == statutFilter);

            var depenses = await query
                .OrderByDescending(d => d.DateDepense)
                .ToListAsync();

            var items = depenses.Select(MapToDto).ToList();

            return new DepenseMoisResponseDto
            {
                Mois = mois,
                Annee = annee,
                DateDebut = dateDebut,
                DateFin = dateFin,
                Depenses = items,
                SyntheseDepense = new SyntheseDepenseDto
                {
                    MontantTotal = depenses.Sum(d =>
                        d.Statut == DepenseStatuts.Validee
                            ? (d.MontantDevisePrincipale ?? d.Montant)
                            : d.Montant),
                    NombreDepenses = items.Count,
                    NombreValidees = depenses.Count(d => d.Statut == DepenseStatuts.Validee),
                    NombreEnAttente = depenses.Count(d => d.Statut == DepenseStatuts.EnAttente)
                }
            };
        }

        public async Task<DepenseResponseDto> CreateAsync(
            CreateDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanCreate(callerRole);
            EnsureSocieteAccess(dto.IdSociete, callerRole, callerSocieteId);
            await ValidateCategorieAsync(dto.IdCategorieDepense, dto.IdSociete);

            var now = DateTime.UtcNow;
            var codeMontant = DeviseConversionService.NormalizeCode(
                !string.IsNullOrWhiteSpace(dto.CodeDeviseMontant)
                    ? dto.CodeDeviseMontant!
                    : await _deviseConversion.GetCodeDevisePrincipaleAsync(dto.IdSociete));

            var depense = new Depense
            {
                IdSociete = dto.IdSociete,
                IdCategorieDepense = dto.IdCategorieDepense,
                Libelle = dto.Libelle.Trim(),
                Description = dto.Description?.Trim(),
                Beneficiaire = dto.Beneficiaire?.Trim(),
                ReferencePiece = dto.ReferencePiece?.Trim(),
                Montant = dto.Montant,
                CodeDeviseMontant = codeMontant,
                ModePaiement = dto.ModePaiement?.Trim(),
                DateDepense = dto.DateDepense ?? now,
                Statut = DepenseStatuts.EnAttente,
                IdUtilisateurCreateur = callerUserId,
                IdUtilisateurValidateur = null,
                DateValidation = null,
                IdCabine = dto.IdCabine,
                IdAxe = dto.IdAxe,
                DateCreation = now
            };

            _context.Depenses.Add(depense);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dépense {IdDepense} créée en attente par utilisateur {UserId}", depense.IdDepense, callerUserId);

            return (await GetByIdAsync(depense.IdDepense, callerUserId, callerRole, callerSocieteId))!;
        }

        public async Task<DepenseResponseDto?> UpdateAsync(
            int id,
            UpdateDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanCreate(callerRole);

            var depense = await _context.Depenses
                .FirstOrDefaultAsync(d => d.IdDepense == id && !d.IsDeleted);

            if (depense == null)
                return null;

            EnsureSocieteAccess(depense.IdSociete, callerRole, callerSocieteId);

            if (depense.Statut != DepenseStatuts.EnAttente)
                throw new InvalidOperationException("Seule une dépense en attente peut être modifiée.");

            if (depense.IdUtilisateurCreateur != callerUserId)
                throw new UnauthorizedAccessException("Vous ne pouvez modifier que vos propres dépenses en attente.");

            if (dto.IdCategorieDepense.HasValue)
                await ValidateCategorieAsync(dto.IdCategorieDepense, depense.IdSociete);

            if (dto.IdCategorieDepense.HasValue || dto.IdCategorieDepense == null)
                depense.IdCategorieDepense = dto.IdCategorieDepense;

            if (!string.IsNullOrWhiteSpace(dto.Libelle))
                depense.Libelle = dto.Libelle.Trim();

            if (dto.Description != null)
                depense.Description = dto.Description.Trim();

            if (dto.Beneficiaire != null)
                depense.Beneficiaire = dto.Beneficiaire.Trim();

            if (dto.ReferencePiece != null)
                depense.ReferencePiece = dto.ReferencePiece.Trim();

            if (dto.ModePaiement != null)
                depense.ModePaiement = dto.ModePaiement.Trim();

            if (dto.DateDepense.HasValue)
                depense.DateDepense = dto.DateDepense.Value;

            if (dto.IdCabine.HasValue)
                depense.IdCabine = dto.IdCabine;

            if (dto.IdAxe.HasValue)
                depense.IdAxe = dto.IdAxe;

            depense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id, callerUserId, callerRole, callerSocieteId);
        }

        public async Task<DepenseResponseDto?> AnnulerAsync(
            int id,
            AnnulerDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            var depense = await _context.Depenses
                .FirstOrDefaultAsync(d => d.IdDepense == id && !d.IsDeleted);

            if (depense == null)
                return null;

            EnsureSocieteAccess(depense.IdSociete, callerRole, callerSocieteId);

            if (depense.Statut == DepenseStatuts.Annulee)
                return await GetByIdAsync(id, callerUserId, callerRole, callerSocieteId);

            if (depense.Statut == DepenseStatuts.EnAttente)
            {
                if (!RolesCreation.Contains(callerRole))
                    throw new UnauthorizedAccessException("Seul le Financier peut retirer une dépense en attente. Admin et Gérant doivent utiliser le refus.");

                if (depense.IdUtilisateurCreateur != callerUserId)
                    throw new UnauthorizedAccessException("Vous ne pouvez retirer que vos propres dépenses en attente.");
            }
            else if (depense.Statut == DepenseStatuts.Validee)
            {
                if (!IsAdmin(callerRole))
                    throw new UnauthorizedAccessException("Seul l'Admin peut annuler une dépense déjà validée.");
            }
            else
            {
                throw new InvalidOperationException($"Impossible d'annuler une dépense au statut {depense.Statut}.");
            }

            depense.Statut = DepenseStatuts.Annulee;
            depense.MotifAnnulation = dto.MotifAnnulation?.Trim();
            depense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetByIdAsync(id, callerUserId, callerRole, callerSocieteId);
        }

        public async Task<DepenseResponseDto?> ValiderAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanValidate(callerRole);

            var depense = await _context.Depenses
                .FirstOrDefaultAsync(d => d.IdDepense == id && !d.IsDeleted);

            if (depense == null)
                return null;

            EnsureSocieteAccess(depense.IdSociete, callerRole, callerSocieteId);

            if (depense.Statut != DepenseStatuts.EnAttente)
                throw new InvalidOperationException("Seule une dépense en attente peut être validée.");

            var now = DateTime.UtcNow;
            depense.Statut = DepenseStatuts.Validee;
            depense.IdUtilisateurValidateur = callerUserId;
            depense.DateValidation = now;
            depense.UpdatedAt = now;

            await ApplyDepenseDeviseSnapshotAsync(depense);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dépense {IdDepense} validée par utilisateur {UserId}", id, callerUserId);

            return await GetByIdAsync(id, callerUserId, callerRole, callerSocieteId);
        }

        public async Task<DepenseResponseDto?> RefuserAsync(
            int id,
            AnnulerDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanValidate(callerRole);

            var depense = await _context.Depenses
                .FirstOrDefaultAsync(d => d.IdDepense == id && !d.IsDeleted);

            if (depense == null)
                return null;

            EnsureSocieteAccess(depense.IdSociete, callerRole, callerSocieteId);

            if (depense.Statut != DepenseStatuts.EnAttente)
                throw new InvalidOperationException("Seule une dépense en attente peut être refusée.");

            depense.Statut = DepenseStatuts.Annulee;
            depense.MotifAnnulation = dto.MotifAnnulation?.Trim();
            depense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Dépense {IdDepense} refusée par utilisateur {UserId}", id, callerUserId);

            return await GetByIdAsync(id, callerUserId, callerRole, callerSocieteId);
        }

        public async Task<bool> DeleteAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            if (!IsAdminRole(callerRole))
                throw new UnauthorizedAccessException("Seuls Admin et Super-Admin peuvent supprimer une dépense.");

            var depense = await _context.Depenses
                .FirstOrDefaultAsync(d => d.IdDepense == id && !d.IsDeleted);

            if (depense == null)
                return false;

            EnsureSocieteAccess(depense.IdSociete, callerRole, callerSocieteId);

            depense.IsDeleted = true;
            depense.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task ApplyDepenseDeviseSnapshotAsync(Depense depense)
        {
            var principale = await _deviseConversion.GetCodeDevisePrincipaleAsync(depense.IdSociete);
            var codeMontant = DeviseConversionService.NormalizeCode(
                !string.IsNullOrWhiteSpace(depense.CodeDeviseMontant)
                    ? depense.CodeDeviseMontant!
                    : principale);

            var conversion = await _deviseConversion.ConvertirVersPrincipaleAsync(
                depense.IdSociete, codeMontant, depense.Montant, depense.DateDepense);

            depense.CodeDeviseMontant = codeMontant;
            depense.CodeDevisePrincipale = principale;
            depense.TauxVersDevisePrincipale = conversion.Taux;
            depense.MontantDevisePrincipale = conversion.MontantConverti;
        }

        private async Task ValidateCategorieAsync(int? idCategorieDepense, int idSociete)
        {
            if (!idCategorieDepense.HasValue)
                return;

            var exists = await _context.CategorieDepenses
                .AnyAsync(c => c.IdCategorieDepense == idCategorieDepense.Value
                    && c.IdSociete == idSociete
                    && c.Statut);

            if (!exists)
                throw new ArgumentException("Catégorie de dépense introuvable ou inactive pour cette société.");
        }

        private IQueryable<Depense> QueryBase()
        {
            return _context.Depenses
                .AsNoTracking()
                .Include(d => d.CategorieDepense)
                .Include(d => d.UtilisateurCreateur)
                .Include(d => d.UtilisateurValidateur);
        }

        private static DepenseResponseDto MapToDto(Depense d)
        {
            return new DepenseResponseDto
            {
                IdDepense = d.IdDepense,
                IdSociete = d.IdSociete,
                IdCategorieDepense = d.IdCategorieDepense,
                NomCategorie = d.CategorieDepense?.NomCategorie,
                Libelle = d.Libelle,
                Description = d.Description,
                Beneficiaire = d.Beneficiaire,
                ReferencePiece = d.ReferencePiece,
                Montant = d.Montant,
                CodeDeviseMontant = d.CodeDeviseMontant,
                CodeDevisePrincipale = d.CodeDevisePrincipale,
                TauxVersDevisePrincipale = d.TauxVersDevisePrincipale,
                MontantDevisePrincipale = d.MontantDevisePrincipale,
                ModePaiement = d.ModePaiement,
                DateDepense = d.DateDepense,
                Statut = d.Statut,
                IdUtilisateurCreateur = d.IdUtilisateurCreateur,
                NomCreateur = d.UtilisateurCreateur?.NomComplet,
                IdUtilisateurValidateur = d.IdUtilisateurValidateur,
                NomValidateur = d.UtilisateurValidateur?.NomComplet,
                DateValidation = d.DateValidation,
                IdCabine = d.IdCabine,
                IdAxe = d.IdAxe,
                MotifAnnulation = d.MotifAnnulation,
                DateCreation = d.DateCreation
            };
        }

        private static void EnsureCanRead(string callerRole)
        {
            if (IsCaissier(callerRole))
                throw new UnauthorizedAccessException("Le rôle Caissier n'a pas accès au module Dépenses.");
        }

        private static void EnsureCanCreate(string callerRole)
        {
            if (!RolesCreation.Contains(callerRole))
                throw new UnauthorizedAccessException("Seul le Financier peut créer ou modifier une dépense.");
        }

        private static void EnsureCanValidate(string callerRole)
        {
            if (!RolesValidation.Contains(callerRole))
                throw new UnauthorizedAccessException("Seuls Admin et Gérant peuvent valider ou refuser une dépense.");
        }

        private static void EnsureSocieteAccess(int idSociete, string callerRole, int callerSocieteId)
        {
            if (IsSuperAdmin(callerRole))
                return;

            if (callerSocieteId <= 0 || idSociete != callerSocieteId)
                throw new UnauthorizedAccessException("Accès refusé à la société demandée.");
        }

        private static int? ResolveSocieteScope(int? idSociete, string callerRole, int callerSocieteId)
        {
            if (IsSuperAdmin(callerRole))
                return idSociete;

            if (callerSocieteId <= 0)
                throw new UnauthorizedAccessException("Société de l'utilisateur introuvable.");

            if (idSociete.HasValue && idSociete.Value != callerSocieteId)
                throw new UnauthorizedAccessException("Accès refusé à la société demandée.");

            return callerSocieteId;
        }

        /// <summary>
        /// null = pas de filtre (Tous). Sinon le statut normalisé (défaut Validee).
        /// </summary>
        private static string? NormalizeStatutFilter(string? statut)
        {
            if (string.IsNullOrWhiteSpace(statut))
                return DepenseStatuts.Validee;

            var normalized = statut.Trim();
            if (string.Equals(normalized, "Tous", StringComparison.OrdinalIgnoreCase))
                return null;

            if (string.Equals(normalized, DepenseStatuts.Validee, StringComparison.OrdinalIgnoreCase))
                return DepenseStatuts.Validee;
            if (string.Equals(normalized, DepenseStatuts.EnAttente, StringComparison.OrdinalIgnoreCase))
                return DepenseStatuts.EnAttente;
            if (string.Equals(normalized, DepenseStatuts.Annulee, StringComparison.OrdinalIgnoreCase))
                return DepenseStatuts.Annulee;

            throw new ArgumentException("Statut invalide. Valeurs autorisées : Validee, EnAttente, Annulee, Tous.");
        }

        private static bool IsCaissier(string role) =>
            string.Equals(role, "Caissier", StringComparison.OrdinalIgnoreCase);

        private static bool IsSuperAdmin(string role) =>
            string.Equals(role, "Super-Admin", StringComparison.OrdinalIgnoreCase);

        private static bool IsAdmin(string role) =>
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        private static bool IsAdminRole(string role) =>
            IsSuperAdmin(role) || IsAdmin(role);
    }
}
