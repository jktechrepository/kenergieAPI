using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Kenergie.Data;
using Kenergie.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Kenergie.Controllers
{
    /// <summary>
    /// Controller pour corriger les permissions des rôles commerciaux
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Super-Admin,Admin")]
    public class PermissionFixController : ControllerBase
    {
        private readonly KenergieDbContext _context;
        private readonly ILogger<PermissionFixController> _logger;

        public PermissionFixController(KenergieDbContext context, ILogger<PermissionFixController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Force l'assignation des permissions aux rôles commerciaux existants
        /// </summary>
        [HttpPost("fix-commercial-roles")]
        public async Task<IActionResult> FixCommercialRolesPermissions()
        {
            try
            {
                _logger.LogInformation("Début de la correction des permissions des rôles commerciaux...");

                // 1. Vérifier si les rôles existent
                var responsableCommercialRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom == "Responsable Commercial");
                
                var agentDirectionCommercialRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom == "Agent Direction Commercial");

                if (responsableCommercialRole == null)
                {
                    return BadRequest("Le rôle 'Responsable Commercial' n'existe pas");
                }

                if (agentDirectionCommercialRole == null)
                {
                    return BadRequest("Le rôle 'Agent Direction Commercial' n'existe pas");
                }

                _logger.LogInformation("Rôles trouvés: Responsable Commercial (ID: {RCId}), Agent Direction Commercial (ADCId: {ADCId})", 
                    responsableCommercialRole.IdRole, agentDirectionCommercialRole.IdRole);

                // 2. Récupérer toutes les permissions
                var allPermissions = await _context.Permissions.ToListAsync();
                _logger.LogInformation("{PermissionCount} permissions trouvées dans la base", allPermissions.Count);

                // 3. Assigner les permissions au Responsable Commercial
                var responsableCommercialPermissions = allPermissions.Where(p =>
                    // Dashboard commercial
                    (p.Categorie == "Dashboard" && p.Action == "Read") ||
                    // Agents : Gestion des Agent Direction Commercial uniquement
                    (p.Categorie == "Agent" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Manage")) ||
                    // Clients : Gestion complète
                    p.Categorie == "Client" ||
                    // Paiements : Création et lecture
                    (p.Categorie == "Paiement" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Statistiques commerciales
                    (p.Categorie == "Commercial" && p.Action == "Read") ||
                    // Catégorie Clients : Lecture
                    (p.Categorie == "CategorieClient" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Utilisateurs : Lecture et création (pour les agents)
                    (p.Categorie == "Utilisateur" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Create")) ||
                    // Gestion organisationnelle complète
                    p.Categorie == "Axe" ||
                    p.Categorie == "Cabine" ||
                    p.Categorie == "Usage" ||
                    p.Categorie == "TypeDeCourant" ||
                    // Plaintes clients : Gestion complète
                    p.Categorie == "PlainteClient" ||
                    // Campagnes de communication : Gestion complète
                    p.Categorie == "CommunicationCampaign"
                ).ToList();

                // 4. Assigner les permissions à l'Agent Direction Commercial
                var agentDirectionCommercialPermissions = allPermissions.Where(p =>
                    // Dashboard commercial personnel
                    (p.Categorie == "Dashboard" && p.Action == "Read") ||
                    // Clients : Création, lecture et mise à jour (pas de suppression)
                    (p.Categorie == "Client" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Update")) ||
                    // Statistiques commerciales personnelles
                    (p.Categorie == "Commercial" && p.Action == "Read") ||
                    // Catégorie Clients : Lecture
                    (p.Categorie == "CategorieClient" && (p.Action == "Read" || p.Action == "ReadAll"))
                ).ToList();

                // 5. Vérifier et ajouter les permissions manquantes pour Responsable Commercial
                var existingRCPermissions = await _context.RolePermissions
                    .Where(rp => rp.IdRole == responsableCommercialRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();

                var rcPermissionsToAdd = responsableCommercialPermissions
                    .Where(p => !existingRCPermissions.Contains(p.IdPermission))
                    .Select(p => new RolePermission
                    {
                        IdRole = responsableCommercialRole.IdRole,
                        IdPermission = p.IdPermission,
                        DateAttribution = DateTime.Now
                    })
                    .ToList();

                if (rcPermissionsToAdd.Any())
                {
                    await _context.RolePermissions.AddRangeAsync(rcPermissionsToAdd);
                    _logger.LogInformation("{Count} permissions ajoutées au Responsable Commercial", rcPermissionsToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("Toutes les permissions existent déjà pour le Responsable Commercial");
                }

                // 6. Vérifier et ajouter les permissions manquantes pour Agent Direction Commercial
                var existingADCPermissions = await _context.RolePermissions
                    .Where(rp => rp.IdRole == agentDirectionCommercialRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();

                var adcPermissionsToAdd = agentDirectionCommercialPermissions
                    .Where(p => !existingADCPermissions.Contains(p.IdPermission))
                    .Select(p => new RolePermission
                    {
                        IdRole = agentDirectionCommercialRole.IdRole,
                        IdPermission = p.IdPermission,
                        DateAttribution = DateTime.Now
                    })
                    .ToList();

                if (adcPermissionsToAdd.Any())
                {
                    await _context.RolePermissions.AddRangeAsync(adcPermissionsToAdd);
                    _logger.LogInformation("{Count} permissions ajoutées à l'Agent Direction Commercial", adcPermissionsToAdd.Count);
                }
                else
                {
                    _logger.LogInformation("Toutes les permissions existent déjà pour l'Agent Direction Commercial");
                }

                // 7. Sauvegarder les changements
                await _context.SaveChangesAsync();
                _logger.LogInformation("Permissions sauvegardées avec succès");

                // 8. Retourner un résumé
                return Ok(new
                {
                    Message = "Permissions corrigées avec succès",
                    ResponsableCommercial = new
                    {
                        RoleId = responsableCommercialRole.IdRole,
                        TotalPermissions = responsableCommercialPermissions.Count,
                        AddedPermissions = rcPermissionsToAdd.Count,
                        ExistingPermissions = existingRCPermissions.Count
                    },
                    AgentDirectionCommercial = new
                    {
                        RoleId = agentDirectionCommercialRole.IdRole,
                        TotalPermissions = agentDirectionCommercialPermissions.Count,
                        AddedPermissions = adcPermissionsToAdd.Count,
                        ExistingPermissions = existingADCPermissions.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la correction des permissions: {Message}", ex.Message);
                return StatusCode(500, new { Message = "Erreur interne", Error = ex.Message });
            }
        }

        /// <summary>
        /// Vérifie l'état actuel des permissions pour les rôles commerciaux
        /// </summary>
        [HttpGet("check-commercial-roles")]
        public async Task<IActionResult> CheckCommercialRolesPermissions()
        {
            try
            {
                var responsableCommercialRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom == "Responsable Commercial");
                
                var agentDirectionCommercialRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom == "Agent Direction Commercial");

                var result = new
                {
                    ResponsableCommercial = responsableCommercialRole == null ? null : new
                    {
                        RoleId = responsableCommercialRole.IdRole,
                        RoleName = responsableCommercialRole.Nom,
                        PermissionCount = await _context.RolePermissions
                            .CountAsync(rp => rp.IdRole == responsableCommercialRole.IdRole),
                        Permissions = await _context.RolePermissions
                            .Where(rp => rp.IdRole == responsableCommercialRole.IdRole)
                            .Include(rp => rp.Permission)
                            .Select(rp => new { rp.Permission.Nom, rp.Permission.Categorie, rp.Permission.Action })
                            .ToListAsync()
                    },
                    AgentDirectionCommercial = agentDirectionCommercialRole == null ? null : new
                    {
                        RoleId = agentDirectionCommercialRole.IdRole,
                        RoleName = agentDirectionCommercialRole.Nom,
                        PermissionCount = await _context.RolePermissions
                            .CountAsync(rp => rp.IdRole == agentDirectionCommercialRole.IdRole),
                        Permissions = await _context.RolePermissions
                            .Where(rp => rp.IdRole == agentDirectionCommercialRole.IdRole)
                            .Include(rp => rp.Permission)
                            .Select(rp => new { rp.Permission.Nom, rp.Permission.Categorie, rp.Permission.Action })
                            .ToListAsync()
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des permissions: {Message}", ex.Message);
                return StatusCode(500, new { Message = "Erreur interne", Error = ex.Message });
            }
        }
    }
}
