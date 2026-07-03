using Kenergie.Models;
using Kenergie.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Data
{
    /// <summary>
    /// Initialise les permissions par défaut du système RBAC
    /// </summary>
    public static class PermissionSeeder
    {
        /// <summary>
        /// Crée toutes les permissions par défaut et les assigne aux rôles appropriés
        /// </summary>
        public static async Task SeedPermissionsAsync(KenergieDbContext context)
        {
            // Vérifier si des permissions existent déjà
            var permissionsExist = await context.Permissions.AnyAsync();
            
            Console.WriteLine("🔨 Initialisation des permissions par défaut...");

            // 1. Créer tous les rôles nécessaires d'abord
            await CreateMissingRolesAsync(context);

            // 2. Ajouter les permissions (nouvelles permissions seront ajoutées même si certaines existent déjà)
            var defaultPermissions = GetDefaultPermissions();
            var existingPermissionNames = await context.Permissions.Select(p => p.Nom).ToListAsync();
            
            var newPermissions = defaultPermissions
                .Where(p => !existingPermissionNames.Contains(p.Nom))
                .ToList();
            
            if (newPermissions.Any())
            {
                await context.Permissions.AddRangeAsync(newPermissions);
                await context.SaveChangesAsync();
                Console.WriteLine($" {newPermissions.Count} nouvelles permissions créées");
            }
            else
            {
                Console.WriteLine(" Toutes les permissions existent déjà");
            }

            // 3. TOUJOURS assigner les permissions aux rôles
            // AssignPermissionsToRolesAsync vérifie déjà les assignations existantes
            // et n'ajoute que les permissions manquantes
            await AssignPermissionsToRolesAsync(context);
            Console.WriteLine(" Vérification et assignation des permissions aux rôles terminée");
        }

        /// <summary>
        /// Crée tous les rôles manquants dans le système
        /// </summary>
        private static async Task CreateMissingRolesAsync(KenergieDbContext context)
        {
            var roleNames = new[] { "Super-Admin", "Admin", "Gerant", "Financier", "Caissier", "Technicien", "Client", "Responsable Commercial", "Agent Direction Commercial" };
            var existingRoles = await context.Roles.Select(r => r.Nom).ToListAsync();

            foreach (var roleName in roleNames)
            {
                if (!existingRoles.Contains(roleName))
                {
                    context.Roles.Add(new Role
                    {
                        Nom = roleName,
                        DateCreation = DateTime.UtcNow,
                        Statut = true
                    });
                    Console.WriteLine($" Rôle '{roleName}' créé");
                }
                else
                {
                    Console.WriteLine($" Rôle '{roleName}' existe déjà");
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Retourne la liste de toutes les permissions par défaut (80+ permissions)
        /// </summary>
        private static List<Permission> GetDefaultPermissions()
        {
            return new List<Permission>
            {
                // ═══════════════════════════════════════════════════════════════════
                // Societe - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Societe.Create",  Categorie = "Societe", Action = "Create",  Description = "Créer une Societe", Statut = true },
                new Permission { Nom = "Societe.Read",    Categorie = "Societe", Action = "Read",    Description = "Voir les informations d'une Societe", Statut = true },
                new Permission { Nom = "Societe.ReadAll", Categorie = "Societe", Action = "ReadAll", Description = "Voir toutes les Societes", Statut = true },
                new Permission { Nom = "Societe.Update",  Categorie = "Societe", Action = "Update",  Description = "Modifier une Societe", Statut = true },
                new Permission { Nom = "Societe.Delete",  Categorie = "Societe", Action = "Delete",  Description = "Supprimer une Societe", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // UTILISATEUR - 6 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Utilisateur.Create",         Categorie = "Utilisateur", Action = "Create",         Description = "Créer un utilisateur",                     Statut = true },
                new Permission { Nom = "Utilisateur.Read",           Categorie = "Utilisateur", Action = "Read",           Description = "Voir un utilisateur",                      Statut = true },
                new Permission { Nom = "Utilisateur.ReadAll",        Categorie = "Utilisateur", Action = "ReadAll",        Description = "Voir tous les utilisateurs",               Statut = true },
                new Permission { Nom = "Utilisateur.Update",         Categorie = "Utilisateur", Action = "Update",         Description = "Modifier un utilisateur",                  Statut = true },
                new Permission { Nom = "Utilisateur.Delete",         Categorie = "Utilisateur", Action = "Delete",         Description = "Supprimer un utilisateur",                 Statut = true },
                new Permission { Nom = "Utilisateur.ChangePassword", Categorie = "Utilisateur", Action = "ChangePassword", Description = "Changer le mot de passe d'un utilisateur", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // AGENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Agent.Create",  Categorie = "Agent", Action = "Create",  Description = "Créer un agent",       Statut = true },
                new Permission { Nom = "Agent.Read",    Categorie = "Agent", Action = "Read",    Description = "Voir un agent",        Statut = true },
                new Permission { Nom = "Agent.ReadAll", Categorie = "Agent", Action = "ReadAll", Description = "Voir tous les agents", Statut = true },
                new Permission { Nom = "Agent.Update",  Categorie = "Agent", Action = "Update",  Description = "Modifier un agent",    Statut = true },
                new Permission { Nom = "Agent.Delete",  Categorie = "Agent", Action = "Delete",  Description = "Supprimer un agent",   Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // COMMERCIAL - 4 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Commercial.Dashboard.Read", Categorie = "Dashboard", Action = "Read", Description = "Voir dashboard commercial", Statut = true },
                new Permission { Nom = "Commercial.Stats.Read", Categorie = "Commercial", Action = "Read", Description = "Voir statistiques commerciales", Statut = true },
                new Permission { Nom = "Agent.DirectionCommercial.Manage", Categorie = "Agent", Action = "Manage", Description = "Gérer agents direction commerciale", Statut = true },
                new Permission { Nom = "Agent.DirectionCommercial.Read", Categorie = "Agent", Action = "Read", Description = "Voir agents direction commerciale", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // CLIENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Client.Create",  Categorie = "Client", Action = "Create",  Description = "Créer un client",       Statut = true },
                new Permission { Nom = "Client.Read",    Categorie = "Client", Action = "Read",    Description = "Voir un client",        Statut = true },
                new Permission { Nom = "Client.ReadAll", Categorie = "Client", Action = "ReadAll", Description = "Voir tous les clients", Statut = true },
                new Permission { Nom = "Client.Update",  Categorie = "Client", Action = "Update",  Description = "Modifier un client",    Statut = true },
                new Permission { Nom = "Client.Delete",  Categorie = "Client", Action = "Delete",  Description = "Supprimer un client",   Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // CATEGORIE CLIENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "CategorieClient.Create",  Categorie = "CategorieClient", Action = "Create",  Description = "Créer une catégorie de client",         Statut = true },
                new Permission { Nom = "CategorieClient.Read",    Categorie = "CategorieClient", Action = "Read",    Description = "Voir une catégorie de client",          Statut = true },
                new Permission { Nom = "CategorieClient.ReadAll", Categorie = "CategorieClient", Action = "ReadAll", Description = "Voir toutes les catégories de clients", Statut = true },
                new Permission { Nom = "CategorieClient.Update",  Categorie = "CategorieClient", Action = "Update",  Description = "Modifier une catégorie de client",      Statut = true },
                new Permission { Nom = "CategorieClient.Delete",  Categorie = "CategorieClient", Action = "Delete",  Description = "Supprimer une catégorie de client",     Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // FACTURE - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Facture.Create",    Categorie = "Facture", Action = "Create", Description = "Créer une facture", Statut = true },
                new Permission { Nom = "Facture.Read",      Categorie = "Facture", Action = "Read", Description = "Voir une facture", Statut = true },
                new Permission { Nom = "Facture.ReadAll",   Categorie = "Facture", Action = "ReadAll", Description = "Voir toutes les factures", Statut = true },
                new Permission { Nom = "Facture.Update",    Categorie = "Facture", Action = "Update", Description = "Modifier une facture", Statut = true },
                new Permission { Nom = "Facture.Delete",    Categorie = "Facture", Action = "Delete", Description = "Supprimer une facture", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // RÔLE - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Role.Create", Categorie = "Role", Action = "Create", Description = "Créer un rôle", Statut = true },
                new Permission { Nom = "Role.Read", Categorie = "Role", Action = "Read", Description = "Voir un rôle", Statut = true },
                new Permission { Nom = "Role.ReadAll", Categorie = "Role", Action = "ReadAll", Description = "Voir tous les rôles", Statut = true },
                new Permission { Nom = "Role.Update", Categorie = "Role", Action = "Update", Description = "Modifier un rôle", Statut = true },
                new Permission { Nom = "Role.Delete", Categorie = "Role", Action = "Delete", Description = "Supprimer un rôle", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // PERMISSION - 7 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Permission.Create", Categorie = "Permission", Action = "Create", Description = "Créer une permission", Statut = true },
                new Permission { Nom = "Permission.Read", Categorie = "Permission", Action = "Read", Description = "Voir une permission", Statut = true },
                new Permission { Nom = "Permission.ReadAll", Categorie = "Permission", Action = "ReadAll", Description = "Voir toutes les permissions", Statut = true },
                new Permission { Nom = "Permission.Update", Categorie = "Permission", Action = "Update", Description = "Modifier une permission", Statut = true },
                new Permission { Nom = "Permission.Delete", Categorie = "Permission", Action = "Delete", Description = "Supprimer une permission", Statut = true },
                new Permission { Nom = "Permission.Assign", Categorie = "Permission", Action = "Assign", Description = "Assigner une permission à un rôle", Statut = true },
                new Permission { Nom = "Permission.Revoke", Categorie = "Permission", Action = "Revoke", Description = "Retirer une permission d'un rôle", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // PLAINTE CLIENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "PlainteClient.Create", Categorie = "PlainteClient", Action = "Create", Description = "Créer une plainte client", Statut = true },
                new Permission { Nom = "PlainteClient.Read", Categorie = "PlainteClient", Action = "Read", Description = "Voir une plainte client", Statut = true },
                new Permission { Nom = "PlainteClient.ReadAll", Categorie = "PlainteClient", Action = "ReadAll", Description = "Voir toutes les plaintes clients", Statut = true },
                new Permission { Nom = "PlainteClient.Update", Categorie = "PlainteClient", Action = "Update", Description = "Modifier une plainte client", Statut = true },
                new Permission { Nom = "PlainteClient.Delete", Categorie = "PlainteClient", Action = "Delete", Description = "Supprimer une plainte client", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // COMMUNICATION CAMPAIGN - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "CommunicationCampaign.Create", Categorie = "CommunicationCampaign", Action = "Create", Description = "Créer une campagne de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.Read", Categorie = "CommunicationCampaign", Action = "Read", Description = "Voir une campagne de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.ReadAll", Categorie = "CommunicationCampaign", Action = "ReadAll", Description = "Voir toutes les campagnes de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.Update", Categorie = "CommunicationCampaign", Action = "Update", Description = "Modifier une campagne de communication", Statut = true },
                new Permission { Nom = "CommunicationCampaign.Delete", Categorie = "CommunicationCampaign", Action = "Delete", Description = "Supprimer une campagne de communication", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // PANNE SIGNALEMENT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "PanneSignalement.Create", Categorie = "PanneSignalement", Action = "Create", Description = "Créer un signalement de panne", Statut = true },
                new Permission { Nom = "PanneSignalement.Read", Categorie = "PanneSignalement", Action = "Read", Description = "Voir un signalement de panne", Statut = true },
                new Permission { Nom = "PanneSignalement.ReadAll", Categorie = "PanneSignalement", Action = "ReadAll", Description = "Voir tous les signalements de panne", Statut = true },
                new Permission { Nom = "PanneSignalement.Update", Categorie = "PanneSignalement", Action = "Update", Description = "Modifier un signalement de panne", Statut = true },
                new Permission { Nom = "PanneSignalement.Delete", Categorie = "PanneSignalement", Action = "Delete", Description = "Supprimer un signalement de panne", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // AXE - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Axe.Create", Categorie = "Axe", Action = "Create", Description = "Créer un axe", Statut = true },
                new Permission { Nom = "Axe.Read", Categorie = "Axe", Action = "Read", Description = "Voir un axe", Statut = true },
                new Permission { Nom = "Axe.ReadAll", Categorie = "Axe", Action = "ReadAll", Description = "Voir tous les axes", Statut = true },
                new Permission { Nom = "Axe.Update", Categorie = "Axe", Action = "Update", Description = "Modifier un axe", Statut = true },
                new Permission { Nom = "Axe.Delete", Categorie = "Axe", Action = "Delete", Description = "Supprimer un axe", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // CABINE - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Cabine.Create", Categorie = "Cabine", Action = "Create", Description = "Créer une cabine", Statut = true },
                new Permission { Nom = "Cabine.Read", Categorie = "Cabine", Action = "Read", Description = "Voir une cabine", Statut = true },
                new Permission { Nom = "Cabine.ReadAll", Categorie = "Cabine", Action = "ReadAll", Description = "Voir toutes les cabines", Statut = true },
                new Permission { Nom = "Cabine.Update", Categorie = "Cabine", Action = "Update", Description = "Modifier une cabine", Statut = true },
                new Permission { Nom = "Cabine.Delete", Categorie = "Cabine", Action = "Delete", Description = "Supprimer une cabine", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // USAGE - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "Usage.Create", Categorie = "Usage", Action = "Create", Description = "Créer un usage", Statut = true },
                new Permission { Nom = "Usage.Read", Categorie = "Usage", Action = "Read", Description = "Voir un usage", Statut = true },
                new Permission { Nom = "Usage.ReadAll", Categorie = "Usage", Action = "ReadAll", Description = "Voir tous les usages", Statut = true },
                new Permission { Nom = "Usage.Update", Categorie = "Usage", Action = "Update", Description = "Modifier un usage", Statut = true },
                new Permission { Nom = "Usage.Delete", Categorie = "Usage", Action = "Delete", Description = "Supprimer un usage", Statut = true },

                // ═══════════════════════════════════════════════════════════════════
                // TYPE DE COURANT - 5 permissions
                // ═══════════════════════════════════════════════════════════════════
                new Permission { Nom = "TypeDeCourant.Create", Categorie = "TypeDeCourant", Action = "Create", Description = "Créer un type de courant", Statut = true },
                new Permission { Nom = "TypeDeCourant.Read", Categorie = "TypeDeCourant", Action = "Read", Description = "Voir un type de courant", Statut = true },
                new Permission { Nom = "TypeDeCourant.ReadAll", Categorie = "TypeDeCourant", Action = "ReadAll", Description = "Voir tous les types de courant", Statut = true },
                new Permission { Nom = "TypeDeCourant.Update", Categorie = "TypeDeCourant", Action = "Update", Description = "Modifier un type de courant", Statut = true },
                new Permission { Nom = "TypeDeCourant.Delete", Categorie = "TypeDeCourant", Action = "Delete", Description = "Supprimer un type de courant", Statut = true },

                // =================================================================
                // PAIEMENT - 5 permissions
                // =================================================================
                new Permission { Nom = "Paiement.Create", Categorie = "Paiement", Action = "Create", Description = "Créer un paiement", Statut = true },
                new Permission { Nom = "Paiement.Read", Categorie = "Paiement", Action = "Read", Description = "Voir un paiement", Statut = true },
                new Permission { Nom = "Paiement.ReadAll", Categorie = "Paiement", Action = "ReadAll", Description = "Voir tous les paiements", Statut = true },
                new Permission { Nom = "Paiement.Update", Categorie = "Paiement", Action = "Update", Description = "Modifier un paiement", Statut = true },
                new Permission { Nom = "Paiement.Delete", Categorie = "Paiement", Action = "Delete", Description = "Supprimer un paiement", Statut = true },

            };
        }

        /// <summary>
        /// Assigne les permissions aux rôles appropriés
        /// </summary>
        private static async Task AssignPermissionsToRolesAsync(KenergieDbContext context)
        {
            // Récupérer les rôles existants
            var superAdminRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Super-Admin");
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");
            var gerantRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Gerant");
            var financierRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Financier"); // ✨ Changé de Comptable à Financier
            var caissierRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Caissier");
            var technicienRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Technicien");
            var clientRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Client");
            // nouveaux rôles
            var responsableCommercialRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Responsable Commercial");
            var agentDirectionCommercialRole = await context.Roles.FirstOrDefaultAsync(r => r.Nom == "Agent Direction Commercial");
         
            if (superAdminRole == null)
            {
                Console.WriteLine(" Rôles non trouvés. Les permissions seront créées mais non assignées.");
                Console.WriteLine(" Vous devrez assigner manuellement les permissions aux rôles.");
                return;
            }

            // Récupérer toutes les permissions
            var allPermissions = await context.Permissions.ToListAsync();

            // ═══════════════════════════════════════════════════════════════════
            //  SUPER-ADMIN : TOUTES LES PERMISSIONS (Root User - Aucune restriction)
            // ═══════════════════════════════════════════════════════════════════
            if (superAdminRole != null)
            {
                // Vérifier les permissions déjà assignées
                var existingSuperAdminPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == superAdminRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var permissionsToAdd = allPermissions
                    .Where(p => !existingSuperAdminPermissions.Contains(p.IdPermission))
                    .ToList();
                
                foreach (var permission in permissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = superAdminRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (permissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {permissionsToAdd.Count} permissions assignées à Super-Admin (Root - Aucune restriction)");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Super-Admin ({existingSuperAdminPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  ADMIN : Gestion complète de sa societe (sauf création/suppression de la societe)
            // ═══════════════════════════════════════════════════════════════════
            if (adminRole != null)
            {
                var adminPermissions = allPermissions.Where(p =>
                    // Écoles : Lecture et modification uniquement (pas création/suppression)
                    (p.Categorie == "Societe" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Update")) ||
                    // Gestion complète de son école
                    p.Categorie == "Utilisateur" ||
                    p.Categorie == "Agent" ||
                    p.Categorie == "Client" ||
                    p.Categorie == "CategorieClient" ||
                    p.Categorie == "Facture" ||
                    p.Categorie == "PlainteClient" ||
                    p.Categorie == "CommunicationCampaign" ||
                    p.Categorie == "PanneSignalement"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingAdminPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == adminRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var adminPermissionsToAdd = adminPermissions
                    .Where(p => !existingAdminPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in adminPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = adminRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (adminPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {adminPermissionsToAdd.Count} nouvelles permissions assignées à Admin");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Admin ({existingAdminPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  GERANT : Mêmes permissions que Admin, sauf modification/suppression de paiements
            // Peut créer des utilisateurs sauf Admin et Super-Admin (vérifié au niveau métier)
            // ═══════════════════════════════════════════════════════════════════
            if (gerantRole != null)
            {
                // Prendre toutes les permissions de Admin
                var gerantPermissions = allPermissions.Where(p =>
                    // Societe : Lecture et modification uniquement (pas création/suppression)
                    (p.Categorie == "Societe" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Update")) ||
                    // Gestion complète de son societe
                    p.Categorie == "Utilisateur" ||
                    p.Categorie == "Agent" ||
                    p.Categorie == "Client" ||
                    p.Categorie == "CategorieClient" ||
                    // Factures : Création et lecture uniquement (PAS modification ni suppression)
                    (p.Categorie == "Facture" && p.Action != "Update" && p.Action != "Delete") ||
                    p.Categorie == "PlainteClient" ||
                    p.Categorie == "CommunicationCampaign" ||
                    p.Categorie == "PanneSignalement"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingGerantPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == gerantRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var gerantPermissionsToAdd = gerantPermissions
                    .Where(p => !existingGerantPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in gerantPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = gerantRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (gerantPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {gerantPermissionsToAdd.Count} nouvelles permissions assignées à Gerant");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Gerant ({existingGerantPermissions.Count} permissions)");
                }
            }

            // =================================================================
            //  RESPONSABLE COMMERCIAL : Gestion commerciale et d'équipe
            //  Peut gérer les agents Agent Direction Commercial et les clients
            // =================================================================
            if (responsableCommercialRole != null)
            {
                var responsableCommercialPermissions = allPermissions.Where(p =>
                    // Dashboard commercial
                    (p.Categorie == "Dashboard" && p.Action == "Read") ||
                    // Agents : Gestion des Agent Direction Commercial uniquement
                    (p.Categorie == "Agent" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Manage")) ||
                    // Clients : Gestion complète
                    p.Categorie == "Client" ||
                    // 💰 FACTURES : Gestion complète sauf modification (sécurité financière)
                    (p.Categorie == "Facture" && p.Action != "Update") ||
                    // Paiements : Création et lecture
                    (p.Categorie == "Paiement" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Statistiques commerciales
                    (p.Categorie == "Commercial" && p.Action == "Read") ||
                    // Catégorie Clients : Lecture
                    (p.Categorie == "CategorieClient" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Utilisateurs : Lecture et création (pour les agents)
                    (p.Categorie == "Utilisateur" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Create")) ||
                    // NOUVEAUX: Gestion organisationnelle complète
                    p.Categorie == "Axe" ||
                    p.Categorie == "Cabine" ||
                    p.Categorie == "Usage" ||
                    p.Categorie == "TypeDeCourant" ||
                    // Plaintes clients : Gestion complète
                    p.Categorie == "PlainteClient" ||
                    // Campagnes de communication : Gestion complète
                    p.Categorie == "CommunicationCampaign"
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingResponsableCommercialPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == responsableCommercialRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var responsableCommercialPermissionsToAdd = responsableCommercialPermissions
                    .Where(p => !existingResponsableCommercialPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in responsableCommercialPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = responsableCommercialRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (responsableCommercialPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {responsableCommercialPermissionsToAdd.Count} nouvelles permissions assignées à Responsable Commercial");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Responsable Commercial ({existingResponsableCommercialPermissions.Count} permissions)");
                }
            }

            // =================================================================
            //  AGENT DIRECTION COMMERCIAL : Vue agent de terrain
            //  Accès limité à la gestion clients uniquement (pas de paiements)
            // =================================================================
            if (agentDirectionCommercialRole != null)
            {
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

                // Vérifier les permissions déjà assignées
                var existingAgentDirectionCommercialPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == agentDirectionCommercialRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var agentDirectionCommercialPermissionsToAdd = agentDirectionCommercialPermissions
                    .Where(p => !existingAgentDirectionCommercialPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in agentDirectionCommercialPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = agentDirectionCommercialRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (agentDirectionCommercialPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {agentDirectionCommercialPermissionsToAdd.Count} nouvelles permissions assignées à Agent Direction Commercial");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Agent Direction Commercial ({existingAgentDirectionCommercialPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  CAISSIER : Gestion des paiements et transactions
            // ═══════════════════════════════════════════════════════════════════
            if (caissierRole != null)
            {
                var caissierPermissions = allPermissions.Where(p =>
                    // Factures : Création et lecture uniquement (PAS modification ni suppression)
                    (p.Categorie == "Facture" && p.Action != "Update" && p.Action != "Delete") ||
                    // Clients : Lecture seule (pour vérifier les factures)
                    (p.Categorie == "Client" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Catégorie Clients : Lecture seule
                    (p.Categorie == "CategorieClient" && (p.Action == "Read" || p.Action == "ReadAll"))
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingCaissierPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == caissierRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var caissierPermissionsToAdd = caissierPermissions
                    .Where(p => !existingCaissierPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in caissierPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = caissierRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (caissierPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {caissierPermissionsToAdd.Count} nouvelles permissions assignées à Caissier");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Caissier ({existingCaissierPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            //  FINANCIER : Gestion financière (Paiements et Frais)
            // Peut créer et lire les paiements, mais PAS modifier ni supprimer
            // ═══════════════════════════════════════════════════════════════════
            if (financierRole != null)
            {
                var financierPermissions = allPermissions.Where(p =>
                    // 💰 GESTION FINANCIÈRE : Factures et paiements
                    (p.Categorie == "Facture" && p.Action != "Update" && p.Action != "Delete") ||
                    (p.Categorie == "Paiement") ||
                    
                    // 👥 GESTION CLIENTS : CRUD complet pour la gestion financière
                    (p.Categorie == "Client") ||
                    (p.Categorie == "CategorieClient") ||
                    
                    // ⚡ GESTION TECHNIQUE : Usages et types de courant pour la tarification
                    (p.Categorie == "Usage") ||
                    (p.Categorie == "TypeDeCourant")
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingFinancierPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == financierRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var financierPermissionsToAdd = financierPermissions
                    .Where(p => !existingFinancierPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in financierPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = financierRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (financierPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {financierPermissionsToAdd.Count} nouvelles permissions assignées à Financier");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Financier ({existingFinancierPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // 🟣 TECHNICIEN : Maintenance et support technique (Palier 1 - Sécurisé)
            // ═══════════════════════════════════════════════════════════════════
            // Permissions techniques uniquement pour éviter les régressions critiques
            if (technicienRole != null)
            {
                var technicienPermissions = allPermissions.Where(p =>
                    // 🛡️ GÉRÉ PAR LE TECHNICIEN : Équipements et infrastructures techniques
                    p.Categorie == "Cabine" ||                    // Gestion complète des cabines
                    p.Categorie == "Axe" ||                       // Gestion complète des axes
                    p.Categorie == "Usage" ||                     // Gestion complète des usages
                    p.Categorie == "TypeDeCourant" ||             // Gestion complète des types de courant
                    p.Categorie == "PanneSignalement" ||         // Déjà existant - gestion complète
                    p.Categorie == "PlainteClient" ||             // Déjà existant - gestion complète
                    
                    // 👁️ LECTURE SEULE : Pour comprendre le contexte technique
                    (p.Categorie == "Agent" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "Societe" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    (p.Categorie == "Utilisateur" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    
                    // 📋 GESTION COMMERCIALE : Contexte pour les interventions techniques
                    (p.Categorie == "Client") ||
                    (p.Categorie == "CategorieClient") ||
                    (p.Categorie == "Facture" && (p.Action == "Read" || p.Action == "ReadAll" || p.Action == "Create"))
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingTechnicienPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == technicienRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var technicienPermissionsToAdd = technicienPermissions
                    .Where(p => !existingTechnicienPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in technicienPermissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = technicienRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (technicienPermissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {technicienPermissionsToAdd.Count} nouvelles permissions assignées à Technicien");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Technicien ({existingTechnicienPermissions.Count} permissions)");
                }
            }

            // ═══════════════════════════════════════════════════════════════════
            // 🔵 CLIENT : Accès en lecture à ses propres données et factures
            // ═══════════════════════════════════════════════════════════════════
            if (clientRole != null)
            {
                var clientPermissions = allPermissions.Where(p =>
                    // Factures : Lecture seule (pour voir ses propres factures)
                    (p.Categorie == "Facture" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Clients : Lecture seule (pour voir ses propres informations)
                    (p.Categorie == "Client" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Catégorie Clients : Lecture seule
                    (p.Categorie == "CategorieClient" && (p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Plaintes : Création et lecture de ses propres plaintes
                    (p.Categorie == "PlainteClient" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll")) ||
                    // Signalements de panne : Création et lecture de ses propres signalements
                    (p.Categorie == "PanneSignalement" && (p.Action == "Create" || p.Action == "Read" || p.Action == "ReadAll"))
                ).ToList();

                // Vérifier les permissions déjà assignées
                var existingClientPermissions = await context.RolePermissions
                    .Where(rp => rp.IdRole == clientRole.IdRole)
                    .Select(rp => rp.IdPermission)
                    .ToListAsync();
                
                var permissionsToAdd = clientPermissions
                    .Where(p => !existingClientPermissions.Contains(p.IdPermission))
                    .ToList();

                foreach (var permission in permissionsToAdd)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        IdRole = clientRole.IdRole,
                        IdPermission = permission.IdPermission,
                        DateAttribution = DateTime.UtcNow
                    });
                }
                
                if (permissionsToAdd.Count > 0)
                {
                    await context.SaveChangesAsync();
                    Console.WriteLine($" {permissionsToAdd.Count} permissions assignées à Client");
                }
                else
                {
                    Console.WriteLine($" Toutes les permissions sont déjà assignées à Client ({existingClientPermissions.Count} permissions)");
                }
            }

            await context.SaveChangesAsync();
        }
    }
}

