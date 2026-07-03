using Kenergie.Data;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Kenergie.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Kenergie.Attributes
{
    /// <summary>
    /// Attribut de double validation : Permission + Rôle
    /// Fournit une couche de sécurité supplémentaire pour les actions sensibles
    /// </summary>
    public class RequirePermissionAndRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly string[] _allowedRoles;

        public RequirePermissionAndRoleAttribute(string permission, params string[] allowedRoles)
        {
            _permission = permission;
            _allowedRoles = allowedRoles ?? Array.Empty<string>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequirePermissionAndRoleAttribute>>();

            try
            {
                // 1. Vérifier que l'utilisateur est authentifié
                if (!currentUserService.IsAuthenticated)
                {
                    logger.LogWarning("🚪 Tentative d'accès non authentifiée à la permission '{Permission}'", _permission);
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var userId = currentUserService.GetUserId();
                var userRole = currentUserService.UserRole;

                // 2. Vérifier le rôle
                if (!_allowedRoles.Contains(userRole))
                {
                    logger.LogWarning("🚫 Rôle '{UserRole}' non autorisé pour la permission '{Permission}'. Rôles attendus: {AllowedRoles}", 
                        userRole, _permission, string.Join(", ", _allowedRoles));
                    context.Result = new ForbidResult($"Rôle '{userRole}' non autorisé pour cette action");
                    return;
                }

                // 3. Vérifier la permission spécifique
                var hasPermission = await permissionService.UserHasPermissionAsync(userId, _permission);
                
                if (!hasPermission)
                {
                    logger.LogWarning("🔒 Permission '{Permission}' refusée pour l'utilisateur {UserId} ({UserRole})", 
                        _permission, userId, userRole);
                    context.Result = new ForbidResult($"Permission '{_permission}' requise");
                    return;
                }

                // 4. Validation supplémentaire pour les techniciens
                await ValidateTechnicianRestrictions(context, currentUserService, logger, userId, userRole);

                logger.LogInformation("✅ Double validation réussie: Permission '{Permission}' + Rôle '{UserRole}' pour utilisateur {UserId}", 
                    _permission, userRole, userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erreur lors de la double validation pour la permission '{Permission}'", _permission);
                context.Result = new StatusCodeResult(500);
            }
        }

        private async Task ValidateTechnicianRestrictions(AuthorizationFilterContext context, 
            ICurrentUserService currentUserService, ILogger logger, int userId, string userRole)
        {
            // Restrictions supplémentaires pour les techniciens
            if (userRole == "Technicien")
            {
                var controllerName = context.HttpContext.Request.RouteValues["controller"]?.ToString();
                var actionName = context.HttpContext.Request.RouteValues["action"]?.ToString();

                // Les techniciens ont des restrictions supplémentaires selon le contexte
                switch (controllerName)
                {
                    case "Facture":
                        // Les techniciens peuvent seulement lire les factures
                        if (actionName != "GetFacture" && actionName != "GetFactures")
                        {
                            logger.LogWarning("🚡 Technicien {UserId} a tenté une action non autorisée sur Facture: {Action}", 
                                userId, actionName);
                            context.Result = new ForbidResult("Les techniciens peuvent seulement consulter les factures");
                            return;
                        }
                        break;

                    case "Client":
                        // Les techniciens peuvent seulement lire les clients
                        if (actionName != "GetClient" && actionName != "GetClients")
                        {
                            logger.LogWarning("🚡 Technicien {UserId} a tenté une action non autorisée sur Client: {Action}", 
                                userId, actionName);
                            context.Result = new ForbidResult("Les techniciens peuvent seulement consulter les clients");
                            return;
                        }
                        break;

                    case "Agent":
                        // Les techniciens peuvent seulement lire les agents
                        if (actionName != "GetAgent" && actionName != "GetAgents")
                        {
                            logger.LogWarning("🚡 Technicien {UserId} a tenté une action non autorisée sur Agent: {Action}", 
                                userId, actionName);
                            context.Result = new ForbidResult("Les techniciens peuvent seulement consulter les agents");
                            return;
                        }
                        break;
                }

                // Validation de la société pour les techniciens
                var societeId = currentUserService.SocieteId;
                if (societeId == 0)
                {
                    logger.LogWarning("🚡 Technicien {UserId} sans société associée", userId);
                    context.Result = new BadRequestObjectResult("Technicien sans société associée");
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Attribut simplifié pour les actions nécessitant uniquement une validation de rôle
    /// </summary>
    public class RequireRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _allowedRoles;

        public RequireRoleAttribute(params string[] allowedRoles)
        {
            _allowedRoles = allowedRoles ?? Array.Empty<string>();
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<RequireRoleAttribute>>();

            try
            {
                if (!currentUserService.IsAuthenticated)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                var userRole = currentUserService.UserRole;

                if (!_allowedRoles.Contains(userRole))
                {
                    logger.LogWarning("🚫 Rôle '{UserRole}' non autorisé. Rôles attendus: {AllowedRoles}", 
                        userRole, string.Join(", ", _allowedRoles));
                    context.Result = new ForbidResult($"Rôle '{userRole}' non autorisé");
                    return;
                }

                logger.LogInformation("✅ Validation rôle réussie: '{UserRole}' pour utilisateur {UserId}", 
                    userRole, currentUserService.GetUserId());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erreur lors de la validation de rôle");
                context.Result = new StatusCodeResult(500);
            }
        }
    }
}
