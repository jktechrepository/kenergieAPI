using Kenergie.Data;
using Kenergie.Models;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Services
{
    /// <summary>
    /// Service d'initialisation des données de base pour TypeDeCourant
    /// </summary>
    public class TypeDeCourantDataService
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<TypeDeCourantDataService> _logger;

        public TypeDeCourantDataService(KenergieDbContext context, ILogger<TypeDeCourantDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Initialise les types de courant par défaut si la table est vide
        /// </summary>
        public async Task InitializeDefaultTypesAsync()
        {
            try
            {
                var existingTypes = await _context.TypeDeCourants.ToListAsync();
                
                if (existingTypes.Any())
                {
                    _logger.LogInformation("Les types de courant sont déjà initialisés ({Count} types)", existingTypes.Count);
                    return;
                }

                var defaultTypes = new List<TypeDeCourant>
                {
                    new TypeDeCourant
                    {
                        Libelle = "Permanent",
                        Description = "Courant permanent sans interruption (service continu 24/7)",
                        Statut = true,
                        DateCreation = DateTime.Now
                    },
                    new TypeDeCourant
                    {
                        Libelle = "Non Permanent",
                        Description = "Courant non permanent avec délestage (service intermittent)",
                        Statut = true,
                        DateCreation = DateTime.Now
                    }
                };

                await _context.TypeDeCourants.AddRangeAsync(defaultTypes);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Initialisation des types de courant par défaut terminée ({Count} types créés)", defaultTypes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des types de courant par défaut");
                throw;
            }
        }

        /// <summary>
        /// Vérifie l'intégrité des données et corrige si nécessaire
        /// </summary>
        public async Task ValidateAndRepairDataAsync()
        {
            try
            {
                var types = await _context.TypeDeCourants.ToListAsync();
                
                // Vérifier que les libellés sont uniques
                var duplicateLibelles = types
                    .GroupBy(t => t.Libelle)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateLibelles.Any())
                {
                    _logger.LogWarning("Libellés de courant en double détectés: {Libelles}", string.Join(", ", duplicateLibelles));
                }

                // S'assurer qu'au moins un type est actif
                var activeTypes = types.Where(t => t.Statut == true).ToList();
                if (!activeTypes.Any())
                {
                    _logger.LogWarning("Aucun type de courant actif trouvé, activation du premier type disponible");
                    var firstType = types.FirstOrDefault();
                    if (firstType != null)
                    {
                        firstType.Statut = true;
                        firstType.DateModification = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }

                _logger.LogInformation("Validation des types de courant terminée");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation des types de courant");
                throw;
            }
        }
    }
}
