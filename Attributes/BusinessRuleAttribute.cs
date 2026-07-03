using Kenergie.Data;
using Kenergie.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Kenergie.Services;
using System;
using System.Threading.Tasks;

namespace Kenergie.Attributes
{
    /// <summary>
    /// Attribut de validation des règles métier pour les actions sensibles
    /// Protège contre les suppressions dangereuses et les opérations à risque
    /// </summary>
    public class BusinessRuleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _rule;

        public BusinessRuleAttribute(string rule)
        {
            _rule = rule;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<KenergieDbContext>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<BusinessRuleAttribute>>();

            try
            {
                var userId = currentUserService.GetUserId();
                var userRole = currentUserService.UserRole;
                var societeId = currentUserService.SocieteId;

                logger.LogInformation("🛡️ Vérification règle métier '{Rule}' pour utilisateur {UserId} ({Role})", 
                    _rule, userId, userRole);

                switch (_rule)
                {
                    case "NO_OUTSTANDING_INVOICES":
                        await ValidateNoOutstandingInvoices(context, dbContext, logger, userId);
                        break;
                    
                    case "NO_DEPENDENT_RECORDS":
                        await ValidateNoDependentRecords(context, dbContext, logger, userId);
                        break;
                    
                    case "TECHNICIAN_SAFE_DELETE":
                        await ValidateTechnicianSafeDelete(context, dbContext, logger, userId);
                        break;
                    
                    default:
                        logger.LogWarning("⚠️ Règle métier inconnue: {Rule}", _rule);
                        context.Result = new BadRequestObjectResult($"Règle métier inconnue: {_rule}");
                        return;
                }

                logger.LogInformation("✅ Règle métier '{Rule}' validée pour utilisateur {UserId}", _rule, userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Erreur lors de la validation de la règle métier '{Rule}'", _rule);
                context.Result = new StatusCodeResult(500);
            }
        }

        private async Task ValidateNoOutstandingInvoices(AuthorizationFilterContext context, 
            KenergieDbContext dbContext, ILogger logger, int userId)
        {
            // Récupérer l'ID du client depuis la route
            if (!int.TryParse(context.HttpContext.Request.RouteValues["id"]?.ToString(), out int clientId))
            {
                context.Result = new BadRequestObjectResult("ID client invalide");
                return;
            }

            // Vérifier les factures impayées
            var hasOutstandingInvoices = dbContext.ClientFactures
                .Any(cf => cf.IdClient == clientId && cf.Statut == true);

            if (hasOutstandingInvoices)
            {
                logger.LogWarning("🚡 Tentative de suppression client {ClientId} avec factures impayées par utilisateur {UserId}", 
                    clientId, userId);
                context.Result = new BadRequestObjectResult(
                    "Impossible de supprimer un client avec des factures impayées. Veuillez régler les factures d'abord.");
                return;
            }
        }

        private async Task ValidateNoDependentRecords(AuthorizationFilterContext context, 
            KenergieDbContext dbContext, ILogger logger, int userId)
        {
            // Récupérer l'ID de l'entité depuis la route
            if (!int.TryParse(context.HttpContext.Request.RouteValues["id"]?.ToString(), out int entityId))
            {
                context.Result = new BadRequestObjectResult("ID entité invalide");
                return;
            }

            var controllerName = context.HttpContext.Request.RouteValues["controller"]?.ToString();
            
            switch (controllerName)
            {
                case "Cabine":
                    // Simplifié : vérifier seulement si la cabine existe (pas de dépendances directes)
                    var cabineExists = dbContext.Cabines
                        .Any(c => c.IdCabine == entityId && c.Statut == true);
                    
                    if (!cabineExists)
                    {
                        context.Result = new NotFoundObjectResult("Cabine non trouvée");
                        return;
                    }
                    
                    // TODO: Implémenter une vérification plus complexe si nécessaire
                    // Pour l'instant, on autorise la suppression des cabines
                    break;

                case "Axe":
                    // Simplifié : vérifier seulement si l'axe existe
                    var axeExists = dbContext.Axes
                        .Any(a => a.IdAxe == entityId && a.Statut == true);
                    
                    if (!axeExists)
                    {
                        context.Result = new NotFoundObjectResult("Axe non trouvé");
                        return;
                    }
                    break;

                case "Usage":
                    // Vérifier si des clients utilisent cet usage
                    var hasClientUsagesForUsage = dbContext.ClientUsages
                        .Any(cu => cu.IdUsage == entityId && cu.Statut == true);
                    
                    if (hasClientUsagesForUsage)
                    {
                        logger.LogWarning("🚡 Tentative de suppression usage {UsageId} avec clients actifs par utilisateur {UserId}", 
                            entityId, userId);
                        context.Result = new BadRequestObjectResult(
                            "Impossible de supprimer un usage avec des clients actifs. Transférez les clients d'abord.");
                        return;
                    }
                    break;

                case "CategorieClient":
                    // Vérifier si des clients utilisent cette catégorie via leurs usages
                    var hasClientsInCategory = dbContext.ClientUsages
                        .Any(cu => cu.Usage != null && cu.Usage.IdCategorieClient == entityId && cu.Statut == true);
                    
                    if (hasClientsInCategory)
                    {
                        logger.LogWarning("🚡 Tentative de suppression catégorie {CategoryId} avec clients actifs par utilisateur {UserId}", 
                            entityId, userId);
                        context.Result = new BadRequestObjectResult(
                            "Impossible de supprimer une catégorie avec des clients actifs. Transférez les clients d'abord.");
                        return;
                    }
                    break;
            }
        }

        private async Task ValidateTechnicianSafeDelete(AuthorizationFilterContext context, 
            KenergieDbContext dbContext, ILogger logger, int userId)
        {
            var currentUserService = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();
            
            // Validation supplémentaire pour les techniciens
            if (currentUserService.UserRole == "Technicien")
            {
                var controllerName = context.HttpContext.Request.RouteValues["controller"]?.ToString();
                
                // Les techniciens ne peuvent pas supprimer certaines entités sensibles
                var restrictedControllers = new[] { "Client", "Facture", "Agent", "Societe" };
                
                if (Array.Exists(restrictedControllers, c => c.Equals(controllerName, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.LogWarning("🚡 Tentative de suppression non autorisée par technicien {UserId} sur {Controller}", 
                        userId, controllerName);
                    context.Result = new ForbidResult("Les techniciens ne peuvent pas supprimer cette entité");
                    return;
                }
            }
        }
    }
}
