using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.Depense;
using Kenergie.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class CategorieDepenseService : ICategorieDepenseRepository
    {
        private static readonly HashSet<string> RolesEcriture = new(StringComparer.OrdinalIgnoreCase)
        {
            "Super-Admin", "Admin", "Financier"
        };

        private static readonly HashSet<string> RolesLecture = new(StringComparer.OrdinalIgnoreCase)
        {
            "Super-Admin", "Admin", "Financier", "Gerant", "Responsable Commercial"
        };

        private readonly KenergieDbContext _context;
        private readonly ILogger<CategorieDepenseService> _logger;

        public CategorieDepenseService(KenergieDbContext context, ILogger<CategorieDepenseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CategorieDepenseResponseDto>> GetBySocieteAsync(
            int idSociete,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanRead(callerRole);
            EnsureSocieteAccess(idSociete, callerRole, callerSocieteId);

            return await _context.CategorieDepenses
                .AsNoTracking()
                .Where(c => c.IdSociete == idSociete && c.Statut)
                .OrderBy(c => c.NomCategorie)
                .Select(c => new CategorieDepenseResponseDto
                {
                    IdCategorieDepense = c.IdCategorieDepense,
                    IdSociete = c.IdSociete,
                    NomCategorie = c.NomCategorie,
                    Description = c.Description,
                    Statut = c.Statut,
                    DateCreation = c.DateCreation
                })
                .ToListAsync();
        }

        public async Task<CategorieDepenseResponseDto?> GetByIdAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanRead(callerRole);

            var categorie = await _context.CategorieDepenses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.IdCategorieDepense == id && c.Statut);

            if (categorie == null)
                return null;

            EnsureSocieteAccess(categorie.IdSociete, callerRole, callerSocieteId);
            return MapToDto(categorie);
        }

        public async Task<CategorieDepenseResponseDto> CreateAsync(
            CreateCategorieDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanWrite(callerRole);
            EnsureSocieteAccess(dto.IdSociete, callerRole, callerSocieteId);

            var nom = dto.NomCategorie.Trim();
            var exists = await _context.CategorieDepenses
                .AnyAsync(c => c.IdSociete == dto.IdSociete && c.NomCategorie == nom && c.Statut);

            if (exists)
                throw new ArgumentException($"La catégorie '{nom}' existe déjà pour cette société.");

            var categorie = new CategorieDepense
            {
                IdSociete = dto.IdSociete,
                NomCategorie = nom,
                Description = dto.Description?.Trim(),
                Statut = true,
                DateCreation = DateTime.UtcNow
            };

            _context.CategorieDepenses.Add(categorie);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Catégorie dépense {Id} créée pour société {Societe}", categorie.IdCategorieDepense, dto.IdSociete);
            return MapToDto(categorie);
        }

        public async Task<CategorieDepenseResponseDto?> UpdateAsync(
            int id,
            UpdateCategorieDepenseDto dto,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            EnsureCanWrite(callerRole);

            var categorie = await _context.CategorieDepenses
                .FirstOrDefaultAsync(c => c.IdCategorieDepense == id);

            if (categorie == null)
                return null;

            EnsureSocieteAccess(categorie.IdSociete, callerRole, callerSocieteId);

            if (!string.IsNullOrWhiteSpace(dto.NomCategorie))
                categorie.NomCategorie = dto.NomCategorie.Trim();

            if (dto.Description != null)
                categorie.Description = dto.Description.Trim();

            if (dto.Statut.HasValue)
                categorie.Statut = dto.Statut.Value;

            await _context.SaveChangesAsync();
            return MapToDto(categorie);
        }

        public async Task<bool> DeleteAsync(
            int id,
            int callerUserId,
            string callerRole,
            int callerSocieteId)
        {
            if (!IsAdminRole(callerRole))
                throw new UnauthorizedAccessException("Seuls Admin et Super-Admin peuvent supprimer une catégorie.");

            var categorie = await _context.CategorieDepenses
                .FirstOrDefaultAsync(c => c.IdCategorieDepense == id);

            if (categorie == null)
                return false;

            EnsureSocieteAccess(categorie.IdSociete, callerRole, callerSocieteId);
            categorie.Statut = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private static CategorieDepenseResponseDto MapToDto(CategorieDepense c) => new()
        {
            IdCategorieDepense = c.IdCategorieDepense,
            IdSociete = c.IdSociete,
            NomCategorie = c.NomCategorie,
            Description = c.Description,
            Statut = c.Statut,
            DateCreation = c.DateCreation
        };

        private static void EnsureCanRead(string callerRole)
        {
            if (!RolesLecture.Contains(callerRole))
                throw new UnauthorizedAccessException("Accès refusé aux catégories de dépenses.");
        }

        private static void EnsureCanWrite(string callerRole)
        {
            if (!RolesEcriture.Contains(callerRole))
                throw new UnauthorizedAccessException("Seuls Financier, Admin et Super-Admin peuvent gérer les catégories.");
        }

        private static void EnsureSocieteAccess(int idSociete, string callerRole, int callerSocieteId)
        {
            if (string.Equals(callerRole, "Super-Admin", StringComparison.OrdinalIgnoreCase))
                return;

            if (callerSocieteId <= 0 || idSociete != callerSocieteId)
                throw new UnauthorizedAccessException("Accès refusé à la société demandée.");
        }

        private static bool IsAdminRole(string role) =>
            string.Equals(role, "Super-Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}
