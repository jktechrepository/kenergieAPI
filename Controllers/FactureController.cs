using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Facture;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services;
using Kenergie.Services.Notifications;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Kenergie.Data;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FactureController : ControllerBase
    {
        private readonly IFactureRepository _factureRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly FactureNotificationService _factureNotificationService;
        private readonly IFactureDiffusionQueue _diffusionQueue;
        private readonly KenergieDbContext _context;

        public FactureController(
            IFactureRepository factureRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            FactureNotificationService factureNotificationService,
            IFactureDiffusionQueue diffusionQueue,
            KenergieDbContext context)
        {
            _factureRepository = factureRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _factureNotificationService = factureNotificationService;
            _diffusionQueue = diffusionQueue;
            _context = context;
        }

        // GET: api/Facture
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Facture>>> GetFactures()
        {
            var factures = await _factureRepository.GetAllAsync();
            return Ok(factures);
        }

        // GET: api/Facture/paged
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<Facture>>> GetFacturesPaged([FromQuery] PagedRequest request)
        {
            var result = await _factureRepository.GetPagedAsync(request);
            return Ok(result);
        }

        // GET: api/Facture/categorie/{idCategorie}
        [HttpGet("categorie/{idCategorie}")]
        public async Task<ActionResult<IEnumerable<Facture>>> GetFacturesByCategorie(int idCategorie)
        {
            var factures = await _factureRepository.GetByCategorieAsync(idCategorie);
            return Ok(factures);
        }

        // GET: api/Facture/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<Facture>>> GetFacturesBySociete(int idSociete)
        {
            var factures = await _factureRepository.GetBySocieteAsync(idSociete);
            return Ok(factures);
        }

        // GET: api/Facture/societe/{idSociete}/paged
        [HttpGet("societe/{idSociete}/paged")]
        public async Task<ActionResult<PagedResult<Facture>>> GetFacturesBySocietePaged(int idSociete, [FromQuery] PagedRequest request)
        {
            var result = await _factureRepository.GetBySocietePagedAsync(idSociete, request);
            return Ok(result);
        }

        // GET: api/Facture/mois/{mois}/annee/{annee}
        [HttpGet("mois/{mois}/annee/{annee}")]
        public async Task<ActionResult<IEnumerable<Facture>>> GetFacturesByMoisAnnee(int mois, int annee)
        {
            var factures = await _factureRepository.GetByMoisAnneeAsync(mois, annee);
            return Ok(factures);
        }

        // GET: api/Facture/categorie/{idCategorie}/mois/{mois}/annee/{annee}
        [HttpGet("categorie/{idCategorie}/mois/{mois}/annee/{annee}")]
        public async Task<ActionResult<IEnumerable<Facture>>> GetFacturesByCategorieMoisAnnee(int idCategorie, int mois, int annee)
        {
            var factures = await _factureRepository.GetByCategorieMoisAnneeAsync(idCategorie, mois, annee);
            return Ok(factures);
        }

        // GET: api/Facture/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Facture>> GetFacture(int id)
        {
            var facture = await _factureRepository.GetByIdAsync(id);
            if (facture == null)
            {
                return NotFound();
            }
            return Ok(facture);
        }

        /// <summary>
        /// Recherche une facture par numéro, CodeCons ou NomClient (égalité exacte, insensible à la casse pour CodeCons/NomClient).
        /// Pour un CodeCons contenant des slashs (ex. A/a1/0236), encoder l'URL : encodeURIComponent('A/a1/0236') → A%2Fa1%2F0236.
        /// </summary>
        // GET: api/Facture/numero/{numeroFacture}
        [HttpGet("numero/{numeroFacture}")]
        [ProducesResponseType(typeof(Facture), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Facture>> GetFactureByNumero(string numeroFacture)
        {
            var facture = await _factureRepository.ResolveFactureBySearchTermAsync(numeroFacture);
            if (facture == null)
            {
                return NotFound();
            }
            return Ok(facture);
        }

        // POST: api/Facture
        [HttpPost]
        [Permission("Facture.Create")]
        public async Task<ActionResult<Facture>> CreateFacture(Facture facture)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier l'unicité du numéro de facture si fourni
            if (!string.IsNullOrWhiteSpace(facture.NumeroFacture) && facture.NumeroFacture.Trim().ToLower() != "string")
            {
                if (await _factureRepository.ExistsByNumeroFactureAsync(facture.NumeroFacture))
                {
                    return Conflict(new { message = "Une facture avec ce numéro existe déjà." });
                }
            }
            else
            {
                // Générer automatiquement un numéro si absent ou égal à "string" (inclut le type de courant si renseigné)
                facture.NumeroFacture = await _factureRepository.GenerateNumeroFactureAsync(facture.IdUsage, facture.DateEmission, facture.IdTypeDeCourant);
            }

            var created = await _factureRepository.CreateAsync(facture);
            
            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création facture");

            // ✨ NOUVEAU : Diffuser la facture aux clients via tous les canaux (Email, SMS, Push, In-App)
            // La facture est maintenant liée à un usage, on diffuse aux clients ayant cet usage
            if (created.IdUsage > 0)
            {
                // Diffusion asynchrone (ne bloque pas la réponse)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var count = await _factureNotificationService.DiffuserFactureAUsageAsync(created, created.IdUsage);
                        // Le log est géré dans le service
                    }
                    catch (Exception ex)
                    {
                        // Log l'erreur mais ne pas faire échouer la création
                        // L'erreur sera loggée par le service
                    }
                });
            }

            return CreatedAtAction(nameof(GetFacture), new { id = created.IdFacture }, created);
        }

        // POST: api/Facture/bulk
        [HttpPost("bulk")]
        [Permission("Facture.Create")]
        public async Task<ActionResult<BulkCreateFactureResponseDto>> CreateFacturesBulk([FromBody] BulkCreateFactureDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.Factures == null || dto.Factures.Count == 0)
            {
                return BadRequest(new { message = "La liste des factures ne peut pas être vide" });
            }

            if (dto.Factures.Count > 100)
            {
                return BadRequest(new { message = "Maximum 100 factures par requête" });
            }

            // Valider chaque facture individuellement
            var validationErrors = new List<object>();
            for (int i = 0; i < dto.Factures.Count; i++)
            {
                var facture = dto.Factures[i];
                if (facture.IdUsage <= 0)
                {
                    validationErrors.Add(new { index = i, field = "IdUsage", message = "L'identifiant de l'usage doit être valide" });
                }
                if (facture.Montant <= 0)
                {
                    validationErrors.Add(new { index = i, field = "Montant", message = "Le montant doit être supérieur à 0" });
                }
                if (facture.MoisEmission < 1 || facture.MoisEmission > 12)
                {
                    validationErrors.Add(new { index = i, field = "MoisEmission", message = "Le mois doit être entre 1 et 12" });
                }
                if (facture.AnneesEmission < 2000 || facture.AnneesEmission > 2100)
                {
                    validationErrors.Add(new { index = i, field = "AnneesEmission", message = "L'année doit être entre 2000 et 2100" });
                }
            }

            if (validationErrors.Count > 0)
            {
                return BadRequest(new { message = "Erreurs de validation", errors = validationErrors });
            }

            // Créer les factures en bulk
            var factureService = _factureRepository as FactureService;
            if (factureService == null)
            {
                return StatusCode(500, new { message = "Service non disponible" });
            }

            var result = await factureService.CreateBulkAsync(dto.Factures);

            // Audit pour chaque facture créée
            var ctx = this.GetAuditContext();
            foreach (var factureSucces in result.FacturesCreees)
            {
                var facture = await _factureRepository.GetByIdAsync(factureSucces.IdFacture);
                if (facture != null)
                {
                    await _auditService.LogCreateAsync(
                        facture,
                        ctx.UserId,
                        ctx.UserName,
                        ctx.UserRole,
                        ctx.IdSociete,
                        ctx.IpAddress,
                        ctx.UserAgent,
                        $"Création facture en masse (bulk) - {factureSucces.NombreClientFacturesCreees} ClientFacture créées");
                }
            }

            // Diffusion asynchrone pour les factures créées (ne bloque pas la réponse)
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (var factureSucces in result.FacturesCreees)
                    {
                        var facture = await _factureRepository.GetByIdAsync(factureSucces.IdFacture);
                        if (facture != null && facture.IdUsage > 0)
                        {
                            await _factureNotificationService.DiffuserFactureAUsageAsync(facture, facture.IdUsage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log l'erreur mais ne pas faire échouer la réponse
                }
            });

            // Retourner le résultat
            if (result.TousSucces)
            {
                return Ok(result);
            }
            else if (result.Succes > 0)
            {
                // Certaines factures ont été créées, d'autres non
                return StatusCode(207, result); // 207 Multi-Status
            }
            else
            {
                // Toutes les factures ont échoué
                return BadRequest(result);
            }
        }

        // PUT: api/Facture/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Facture>> UpdateFacture(int id, Facture facture)
        {
            if (id != facture.IdFacture)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _factureRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Vérifier l'unicité du numéro de facture si modifié
            if (string.IsNullOrWhiteSpace(facture.NumeroFacture) || facture.NumeroFacture.Trim().ToLower() == "string")
            {
                facture.NumeroFacture = await _factureRepository.GenerateNumeroFactureAsync(facture.IdUsage, facture.DateEmission);
            }
            else if (facture.NumeroFacture != existing.NumeroFacture)
            {
                if (await _factureRepository.ExistsByNumeroFactureAsync(facture.NumeroFacture))
                {
                    return Conflict(new { message = "Une facture avec ce numéro existe déjà." });
                }
            }

            // Snapshot avant modification
            var oldFacture = new Facture
            {
                IdFacture = existing.IdFacture,
                NumeroFacture = existing.NumeroFacture,
                Montant = existing.Montant,
                DateEmission = existing.DateEmission,
                Statut = existing.Statut
            };

            var updated = await _factureRepository.UpdateAsync(facture);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldFacture, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification facture");

            return Ok(updated);
        }

        // DELETE: api/Facture/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFacture(int id)
        {
            var exists = await _factureRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound();
            }

            var entity = await _factureRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            
            await _factureRepository.DeleteAsync(id);
            
            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(entity, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Suppression facture");

            return NoContent();
        }

        // PUT: api/Facture/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _factureRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Facture non trouvée" });
                }

                var facture = await _factureRepository.GetByIdAsync(id);
                var nouveauStatut = facture?.Statut ?? false;

                return Ok(new
                {
                    message = "Statut modifié avec succès",
                    statut = nouveauStatut,
                    facture = facture
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la modification du statut: {ex.Message}" });
            }
        }

        // PUT: api/Facture/set-statut/{id}
        [HttpPut("set-statut/{id}")]
        public async Task<ActionResult<object>> SetStatut(int id, [FromQuery] bool statut)
        {
            try
            {
                var success = await _factureRepository.SetStatutAsync(id, statut);
                if (!success)
                {
                    return NotFound(new { message = "Facture non trouvée" });
                }

                var facture = await _factureRepository.GetByIdAsync(id);

                return Ok(new
                {
                    message = $"Statut défini à {statut}",
                    statut = statut,
                    facture = facture
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la définition du statut: {ex.Message}" });
            }
        }

        // POST: api/Facture/{idFacture}/societe/{idSociete}/diffusion
        [HttpPost("{idFacture}/societe/{idSociete}/diffusion")]
        [Permission("Facture.Update")]
        public async Task<ActionResult<DiffusionFactureResponseDto>> DiffuserFacture(
            int idFacture, 
            int idSociete,
            [FromQuery] bool forcer = false)
        {
            var startTime = DateTime.Now;

            try
            {
                // ✨ OPTIMISÉ : Charger la facture avec l'usage et la catégorie en une seule requête
                var facture = await _context.Factures
                    .Include(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                    .FirstOrDefaultAsync(f => f.IdFacture == idFacture);

                if (facture == null)
                {
                    return NotFound(new { message = "Facture non trouvée" });
                }

                // Vérifier que la facture a un usage
                if (facture.IdUsage <= 0 || facture.Usage == null)
                {
                    return BadRequest(new { message = "La facture n'a pas d'usage associé" });
                }

                var usage = facture.Usage;

                // Vérifier que la facture appartient à la société (via usage -> catégorie)
                if (usage.CategorieClient == null || usage.CategorieClient.IdSociete != idSociete)
                {
                    return BadRequest(new { message = "La facture n'appartient pas à cette société" });
                }

                // Vérifier si la facture a déjà été diffusée
                if (facture.EstDiffusee && !forcer)
                {
                    return Conflict(new DiffusionFactureResponseDto
                    {
                        Success = false,
                        FactureId = facture.IdFacture,
                        NumeroFacture = facture.NumeroFacture,
                        UsageId = usage.IdUsage,
                        NomUsage = usage.Libelle,
                        CategorieId = usage.IdCategorieClient, // Pour compatibilité
                        NomCategorie = usage.Libelle, // Pour compatibilité
                        TotalClients = 0,
                        ClientsNotifies = 0,
                        ClientsEchecs = 0,
                        Duree = "0s",
                        Message = $"Cette facture a déjà été diffusée le {facture.DateDiffusion?.ToString("dd/MM/yyyy HH:mm")}. Utilisez le paramètre 'forcer=true' pour forcer une nouvelle diffusion."
                    });
                }

                // ✨ NOUVEAU : Vérifier que les ClientFacture existent pour cette facture
                var clientFacturesCount = await _context.ClientFactures
                    .Where(cf => cf.IdFacture == facture.IdFacture && cf.Statut == true)
                    .CountAsync();

                if (clientFacturesCount == 0)
                {
                    return BadRequest(new DiffusionFactureResponseDto
                    {
                        Success = false,
                        FactureId = facture.IdFacture,
                        NumeroFacture = facture.NumeroFacture,
                        UsageId = usage.IdUsage,
                        NomUsage = usage.Libelle,
                        TotalClients = 0,
                        ClientsNotifies = 0,
                        ClientsEchecs = 0,
                        Duree = $"{(DateTime.Now - startTime).TotalSeconds:F2}s",
                        Message = "Aucune ClientFacture trouvée pour cette facture. Les ClientFacture doivent être créées lors de la création de la facture avant de pouvoir diffuser."
                    });
                }

                // Récupérer le nombre total de clients ayant cet usage via le service
                var totalClients = await _factureNotificationService.GetTotalClientsByUsageAsync(facture.IdUsage);

                // ✨ NOUVEAU : Calculer les statistiques depuis ClientFacture
                var clientFacturesStats = await _context.ClientFactures
                    .Where(cf => cf.IdFacture == facture.IdFacture && cf.Statut == true)
                    .Select(cf => new
                    {
                        Montant = cf.Montant ?? 0,
                        MontantDu = cf.MontantDu ?? 0
                    })
                    .ToListAsync();

                var nombreClientFactures = clientFacturesStats.Count;
                var montantTotalClientFactures = clientFacturesStats.Sum(cf => cf.Montant);
                var montantDuTotal = clientFacturesStats.Sum(cf => cf.MontantDu);

                // Marquer la facture comme en cours de diffusion
                facture.EstDiffusee = true;
                facture.DateDiffusion = DateTime.Now;
                await _factureRepository.UpdateAsync(facture);

                // Ajouter la diffusion à la queue pour traitement asynchrone
                await _diffusionQueue.EnqueueDiffusionAsync(facture.IdFacture, facture.IdUsage);

                var duree = DateTime.Now - startTime;

                // Préparer la réponse (traitement asynchrone, donc on retourne immédiatement)
                var response = new DiffusionFactureResponseDto
                {
                    Success = true,
                    FactureId = facture.IdFacture,
                    NumeroFacture = facture.NumeroFacture,
                    UsageId = usage.IdUsage,
                    NomUsage = usage.Libelle,
                    CategorieId = usage.IdCategorieClient, // Pour compatibilité avec l'existant
                    NomCategorie = usage.Libelle, // Pour compatibilité avec l'existant
                    TotalClients = totalClients,
                    NombreClientFactures = nombreClientFactures,
                    MontantTotalClientFactures = montantTotalClientFactures,
                    MontantDuTotal = montantDuTotal,
                    ClientsNotifies = 0, // Sera mis à jour par le worker
                    ClientsEchecs = 0,   // Sera mis à jour par le worker
                    Duree = $"{duree.TotalSeconds:F2}s",
                    Message = $"Diffusion de la facture mise en queue. {totalClients} client(s) seront notifiés en arrière-plan."
                };

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    facture, 
                    ctx.UserId, 
                    ctx.UserName, 
                    ctx.UserRole, 
                    ctx.IdSociete, 
                    ctx.IpAddress, 
                    ctx.UserAgent, 
                    $"Diffusion facture {facture.NumeroFacture ?? facture.IdFacture.ToString()} mise en queue pour {totalClients} clients (Usage: {usage.Libelle})");

                return Ok(response);
            }
            catch (Exception ex)
            {
                var duree = DateTime.Now - startTime;
                return StatusCode(500, new DiffusionFactureResponseDto
                {
                    Success = false,
                    FactureId = idFacture,
                    TotalClients = 0,
                    ClientsNotifies = 0,
                    ClientsEchecs = 0,
                    Duree = $"{duree.TotalSeconds:F2}s",
                    Message = $"Erreur lors de la diffusion : {ex.Message}"
                });
            }
        }

        // POST: api/Facture/societe/{idSociete}/diffusion/bulk
        [HttpPost("societe/{idSociete}/diffusion/bulk")]
        [Permission("Facture.Update")]
        public async Task<ActionResult<DiffusionFactureBulkResponseDto>> DiffuserToutesFacturesEnAttente(int idSociete)
        {
            var startTime = DateTime.Now;

            try
            {
                // Vérifier que la société existe
                var societe = await _context.Societes.FindAsync(idSociete);
                if (societe == null)
                {
                    return NotFound(new { message = "Société non trouvée" });
                }

                // Récupérer toutes les factures en attente de diffusion pour cette société
                var facturesEnAttente = await _context.Factures
                    .Include(f => f.Usage)
                        .ThenInclude(u => u.CategorieClient)
                    .Where(f => f.Statut == true &&
                                f.EstDiffusee == false &&
                                f.Usage != null &&
                                f.Usage.CategorieClient != null &&
                                f.Usage.CategorieClient.IdSociete == idSociete)
                    .OrderBy(f => f.DateEmission)
                    .ThenBy(f => f.DateCreation)
                    .ToListAsync();

                if (facturesEnAttente.Count == 0)
                {
                    return Ok(new DiffusionFactureBulkResponseDto
                    {
                        Success = true,
                        SocieteId = idSociete,
                        TotalFactures = 0,
                        FacturesEnQueue = 0,
                        FacturesEchecs = 0,
                        Duree = $"{(DateTime.Now - startTime).TotalSeconds:F2}s",
                        Message = "Aucune facture en attente de diffusion pour cette société"
                    });
                }

                var response = new DiffusionFactureBulkResponseDto
                {
                    Success = true,
                    SocieteId = idSociete,
                    TotalFactures = facturesEnAttente.Count
                };

                // Traiter chaque facture
                foreach (var facture in facturesEnAttente)
                {
                    try
                    {
                        // Vérifier que la facture a un usage valide
                        if (facture.IdUsage <= 0 || facture.Usage == null)
                        {
                            response.Erreurs.Add(new FactureDiffusionErreurDto
                            {
                                FactureId = facture.IdFacture,
                                NumeroFacture = facture.NumeroFacture,
                                Message = "La facture n'a pas d'usage associé"
                            });
                            response.FacturesEchecs++;
                            continue;
                        }

                        // ✨ NOUVEAU : Vérifier que les ClientFacture existent pour cette facture
                        var clientFacturesCount = await _context.ClientFactures
                            .Where(cf => cf.IdFacture == facture.IdFacture && cf.Statut == true)
                            .CountAsync();

                        if (clientFacturesCount == 0)
                        {
                            response.Erreurs.Add(new FactureDiffusionErreurDto
                            {
                                FactureId = facture.IdFacture,
                                NumeroFacture = facture.NumeroFacture,
                                Message = "Aucune ClientFacture trouvée. Les ClientFacture doivent être créées lors de la création de la facture."
                            });
                            response.FacturesEchecs++;
                            continue;
                        }

                        // Récupérer le nombre total de clients ayant cet usage
                        var totalClients = await _factureNotificationService.GetTotalClientsByUsageAsync(facture.IdUsage);

                        // ✨ NOUVEAU : Calculer les statistiques depuis ClientFacture
                        var clientFacturesStats = await _context.ClientFactures
                            .Where(cf => cf.IdFacture == facture.IdFacture && cf.Statut == true)
                            .Select(cf => new
                            {
                                Montant = cf.Montant ?? 0,
                                MontantDu = cf.MontantDu ?? 0
                            })
                            .ToListAsync();

                        var nombreClientFactures = clientFacturesStats.Count;
                        var montantTotalClientFactures = clientFacturesStats.Sum(cf => cf.Montant);
                        var montantDuTotal = clientFacturesStats.Sum(cf => cf.MontantDu);

                        // Marquer la facture comme en cours de diffusion
                        facture.EstDiffusee = true;
                        facture.DateDiffusion = DateTime.Now;
                        await _factureRepository.UpdateAsync(facture);

                        // Ajouter la diffusion à la queue pour traitement asynchrone
                        await _diffusionQueue.EnqueueDiffusionAsync(facture.IdFacture, facture.IdUsage);

                        // Ajouter à la liste des factures diffusées
                        response.FacturesDiffusees.Add(new FactureDiffusionItemDto
                        {
                            FactureId = facture.IdFacture,
                            NumeroFacture = facture.NumeroFacture,
                            UsageId = facture.Usage.IdUsage,
                            NomUsage = facture.Usage.Libelle,
                            TotalClients = totalClients,
                            NombreClientFactures = nombreClientFactures,
                            MontantTotalClientFactures = montantTotalClientFactures,
                            MontantDuTotal = montantDuTotal
                        });

                        response.FacturesEnQueue++;

                        // Audit pour chaque facture
                        var ctx = this.GetAuditContext();
                        await _auditService.LogCreateAsync(
                            facture,
                            ctx.UserId,
                            ctx.UserName,
                            ctx.UserRole,
                            ctx.IdSociete,
                            ctx.IpAddress,
                            ctx.UserAgent,
                            $"Diffusion en masse - Facture {facture.NumeroFacture ?? facture.IdFacture.ToString()} mise en queue pour {totalClients} clients (Usage: {facture.Usage.Libelle})");
                    }
                    catch (Exception ex)
                    {
                        response.Erreurs.Add(new FactureDiffusionErreurDto
                        {
                            FactureId = facture.IdFacture,
                            NumeroFacture = facture.NumeroFacture,
                            Message = $"Erreur lors de la mise en queue: {ex.Message}"
                        });
                        response.FacturesEchecs++;
                    }
                }

                var duree = DateTime.Now - startTime;
                response.Duree = $"{duree.TotalSeconds:F2}s";

                if (response.FacturesEnQueue == response.TotalFactures)
                {
                    response.Message = $"Toutes les {response.FacturesEnQueue} facture(s) ont été mises en queue pour diffusion";
                }
                else if (response.FacturesEnQueue > 0)
                {
                    response.Message = $"{response.FacturesEnQueue} facture(s) mise(s) en queue, {response.FacturesEchecs} échec(s)";
                }
                else
                {
                    response.Success = false;
                    response.Message = $"Aucune facture n'a pu être mise en queue. {response.FacturesEchecs} échec(s)";
                }

                // Audit global
                var ctxGlobal = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    new { SocieteId = idSociete, TotalFactures = response.TotalFactures, FacturesEnQueue = response.FacturesEnQueue },
                    ctxGlobal.UserId,
                    ctxGlobal.UserName,
                    ctxGlobal.UserRole,
                    ctxGlobal.IdSociete,
                    ctxGlobal.IpAddress,
                    ctxGlobal.UserAgent,
                    $"Diffusion en masse de {response.FacturesEnQueue} facture(s) pour la société {idSociete}");

                if (response.Success && response.FacturesEnQueue > 0)
                {
                    return Ok(response);
                }
                else if (response.FacturesEnQueue > 0)
                {
                    return StatusCode(207, response); // 207 Multi-Status
                }
                else
                {
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                var duree = DateTime.Now - startTime;
                return StatusCode(500, new DiffusionFactureBulkResponseDto
                {
                    Success = false,
                    SocieteId = idSociete,
                    TotalFactures = 0,
                    FacturesEnQueue = 0,
                    FacturesEchecs = 0,
                    Duree = $"{duree.TotalSeconds:F2}s",
                    Message = $"Erreur lors de la diffusion en masse: {ex.Message}"
                });
            }
        }

        // GET: api/Facture/{idFacture}/diffusion/statistiques
        /// <summary>
        /// ✨ AMÉLIORÉ : Récupère les statistiques de diffusion d'une facture avec informations détaillées depuis ClientFacture
        /// </summary>
        [HttpGet("{idFacture}/diffusion/statistiques")]
        public async Task<ActionResult<object>> GetStatistiquesDiffusion(int idFacture)
        {
            // Vérifier que la facture existe
            var facture = await _factureRepository.GetByIdAsync(idFacture);
            if (facture == null)
            {
                return NotFound(new { message = "Facture non trouvée" });
            }

            // Charger l'usage pour obtenir le libellé
            var usage = await _context.Usages
                .Include(u => u.CategorieClient)
                .FirstOrDefaultAsync(u => u.IdUsage == facture.IdUsage);

            // ✨ NOUVEAU : Récupérer les statistiques depuis ClientFacture
            var clientFactures = await _context.ClientFactures
                .Where(cf => cf.IdFacture == facture.IdFacture && cf.Statut == true)
                .ToListAsync();

            var nombreClientFactures = clientFactures.Count;
            var montantTotal = clientFactures
                .Where(cf => cf.Montant.HasValue)
                .Sum(cf => cf.Montant.Value);
            var montantPayeTotal = clientFactures
                .Where(cf => cf.MontantPaye.HasValue)
                .Sum(cf => cf.MontantPaye.Value);
            var montantDuTotal = clientFactures
                .Where(cf => cf.MontantDu.HasValue)
                .Sum(cf => cf.MontantDu.Value);
            var nombreClientsAvecArrieres = clientFactures
                .Count(cf => cf.MontantDu.HasValue && cf.MontantDu.Value > 0);

            // Récupérer le nombre total de clients ayant cet usage (pour référence)
            var totalClients = await _factureNotificationService.GetTotalClientsByUsageAsync(facture.IdUsage);

            return Ok(new
            {
                factureId = facture.IdFacture,
                numeroFacture = facture.NumeroFacture,
                usageId = facture.IdUsage,
                libelleUsage = usage?.Libelle,
                categorieId = usage?.IdCategorieClient, // Pour compatibilité
                nomCategorie = usage?.Libelle, // Pour compatibilité
                totalClients = totalClients,
                // ✨ NOUVEAU : Statistiques depuis ClientFacture
                nombreClientFactures = nombreClientFactures,
                montantTotal = montantTotal,
                montantPayeTotal = montantPayeTotal,
                montantDuTotal = montantDuTotal,
                nombreClientsAvecArrieres = nombreClientsAvecArrieres,
                estDiffusee = facture.EstDiffusee,
                dateDiffusion = facture.DateDiffusion
            });
        }
    }
}

