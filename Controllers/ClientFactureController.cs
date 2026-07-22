using Kenergie.Models;
using Kenergie.Models.DTOs.ClientFacture;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Kenergie.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Kenergie.Data;
using System.Threading;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientFactureController : ControllerBase
    {
        private readonly IClientFactureRepository _clientFactureRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly KenergieDbContext _context;
        private readonly ExcelClientFactureService _excelClientFactureService;
        private readonly ILogger<ClientFactureController> _logger;

        public ClientFactureController(
            IClientFactureRepository clientFactureRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            KenergieDbContext context,
            ExcelClientFactureService excelClientFactureService,
            ILogger<ClientFactureController> logger)
        {
            _clientFactureRepository = clientFactureRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _context = context;
            _excelClientFactureService = excelClientFactureService;
            _logger = logger;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ⚠️ IMPORTANT : Les routes les plus spécifiques doivent être AVANT les routes générales
        // ═══════════════════════════════════════════════════════════════════

        // GET: api/ClientFacture/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ClientFactureDto>> GetClientFacture(int id)
        {
            var clientFacture = await _clientFactureRepository.GetByIdAsync(id);
            if (clientFacture == null)
            {
                return NotFound(new { message = "ClientFacture non trouvée" });
            }

            var dto = await ConvertToDtoAsync(clientFacture);
            return Ok(dto);
        }

        // GET: api/ClientFacture/client/{idClient}/consolidee/mois/{mois}/annee/{annee}
        /// <summary>
        /// ✨ NOUVEAU : Récupère la facture consolidée d'un client pour une période spécifique (mois/année)
        /// </summary>
        /// <param name="idClient">Identifiant du client</param>
        /// <param name="mois">Mois (format: "01", "02", ..., "12")</param>
        /// <param name="annee">Année (ex: 2024)</param>
        /// <returns>Facture consolidée pour la période spécifiée</returns>
        [HttpGet("client/{idClient}/consolidee/mois/{mois}/annee/{annee}")]
        [Authorize]
        public async Task<ActionResult<ClientFactureConsolideeDto>> GetClientFactureConsolideeByPeriode(
            int idClient,
            string mois,
            int annee)
        {
            if (string.IsNullOrWhiteSpace(mois))
            {
                return BadRequest(new { message = "Le paramètre 'mois' ne peut pas être vide." });
            }

            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(idClient);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var result = await _clientFactureRepository.GetClientFactureConsolideeByPeriodeAsync(idClient, mois.Trim(), annee);
            if (result == null)
            {
                return NotFound(new { message = $"Aucune facture trouvée pour le client {idClient} pour la période {mois}/{annee}" });
            }

            return Ok(result);
        }

        // GET: api/ClientFacture/client/{idClient}/arrieres-consolides
        /// <summary>
        /// ✨ NOUVEAU : Récupère les arriérés d'un client groupés par période (mois/année) avec totaux consolidés
        /// Seules les factures avec MontantDu > 0 sont incluses
        /// Format similaire à /consolidee/mois/{mois}/annee/{annee} mais pour tous les arriérés
        /// </summary>
        /// <param name="idClient">Identifiant du client</param>
        /// <returns>Arriérés consolidés par période avec détails</returns>
        [HttpGet("client/{idClient}/arrieres-consolides")]
        [Authorize]
        public async Task<ActionResult<ArrieresConsolidesResponseDto>> GetArrieresConsolidesByClient(int idClient)
        {
            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(idClient);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var result = await _clientFactureRepository.GetArrieresConsolidesByClientAsync(idClient);
            return Ok(result);
        }

        // GET: api/ClientFacture/client/{idClient}/consolidees
        /// <summary>
        /// ✨ NOUVEAU : Récupère toutes les factures d'un client groupées par période (mois/année) avec totaux consolidés
        /// Permet d'afficher un total consolidé pour toutes les factures d'un client, regroupées par période
        /// </summary>
        /// <param name="idClient">Identifiant du client</param>
        /// <returns>Factures consolidées par période avec totaux globaux</returns>
        [HttpGet("client/{idClient}/consolidees")]
        [Authorize]
        public async Task<ActionResult<ClientFacturesConsolideesResponseDto>> GetClientFacturesConsolidees(int idClient)
        {
            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(idClient);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var result = await _clientFactureRepository.GetClientFacturesConsolideesAsync(idClient);
            return Ok(result);
        }

        // GET: api/ClientFacture/arrieres-consolides
        /// <summary>
        /// ✨ NOUVEAU : Récupère un rapport global des arriérés consolidés pour tous les clients
        /// Retourne les totaux globaux et la liste des arriérés par client groupés par période
        /// Seules les factures avec MontantDu > 0 sont incluses
        /// </summary>
        /// <param name="moisFacturePrecedentSeulement">Si true, filtre uniquement les clients facturés le mois précédent (défaut: true)</param>
        /// <param name="idAxe">Optionnel: filtre par axe spécifique</param>
        /// <param name="idTypeDeCourant">Optionnel: filtre par type de courant (ClientUsage actif)</param>
        /// <param name="mois">Optionnel: mois de la période de relance (ex. "04" ou "4"). Ignoré si moisFacturePrecedentSeulement=false.</param>
        /// <param name="annee">Optionnel: année de la période de relance. Ignoré si moisFacturePrecedentSeulement=false.</param>
        /// <returns>Rapport global des arriérés consolidés avec totaux et détails par client</returns>
        [HttpGet("arrieres-consolides")]
        [Authorize(Roles = "Super-Admin,Admin,Technicien, Financier, Responsable Commercial")]
        public async Task<ActionResult<ArrieresConsolidesGlobauxResponseDto>> GetArrieresConsolidesGlobaux(
            [FromQuery] bool moisFacturePrecedentSeulement = true,
            [FromQuery] int? idAxe = null,
            [FromQuery] int? idTypeDeCourant = null,
            [FromQuery] string? mois = null,
            [FromQuery] int? annee = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Timeout manuel de 10 minutes pour .NET 6
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                
                _logger.LogInformation(
                    "Début du traitement des arriérés consolidés avec moisFacturePrecedentSeulement={MoisFacture}, mois={Mois}, annee={Annee}",
                    moisFacturePrecedentSeulement, mois, annee);
                
                var result = await _clientFactureRepository.GetArrieresConsolidesGlobauxAsync(
                    moisFacturePrecedentSeulement, idAxe, idTypeDeCourant, mois, annee);
                    
                _logger.LogInformation("Traitement terminé - {Count} clients traités", result.ArrieresParClient?.Count ?? 0);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Requête annulée par le client");
                return StatusCode(408, new { message = "Requête annulée par le client" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement des arriérés consolidés");
                return StatusCode(500, new { message = "Le traitement a pris trop de temps ou a rencontré une erreur. Veuillez réessayer avec des filtres plus restrictifs." });
            }
        }

        // GET: api/ClientFacture/arrieres
        /// <summary>
        /// Récupère tous les arriérés (tous les clients) où MontantDu > 0
        /// Même structure de réponse que GET /api/ClientFacture/client/{idClient}/arrieres
        /// </summary>
        /// <returns>Liste de tous les arriérés (ClientFacture avec MontantDu > 0)</returns>
        [HttpGet("arrieres")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetAllArrieres()
        {
            var clientFactures = await _clientFactureRepository.GetAllArrieresAsync();
            var dtos = new List<ClientFactureDto>();

            foreach (var cf in clientFactures)
            {
                dtos.Add(await ConvertToDtoAsync(cf));
            }

            return Ok(dtos);
        }

        // GET: api/ClientFacture/client/{idClient}/arrieres
        [HttpGet("client/{idClient}/arrieres")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetArrieresByClient(int idClient)
        {
            var clientFactures = await _clientFactureRepository.GetByClientWithArrieresAsync(idClient);
            var dtos = new List<ClientFactureDto>();

            foreach (var cf in clientFactures)
            {
                dtos.Add(await ConvertToDtoAsync(cf));
            }

            return Ok(dtos);
        }

        // GET: api/ClientFacture/client/{idClient}/pre-existants
        [HttpGet("client/{idClient}/pre-existants")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetPreExistantsByClient(int idClient)
        {
            var clientFactures = await _clientFactureRepository.GetPreExistantsByClientAsync(idClient);
            var dtos = new List<ClientFactureDto>();

            foreach (var cf in clientFactures)
            {
                dtos.Add(await ConvertToDtoAsync(cf));
            }

            return Ok(dtos);
        }

        // GET: api/ClientFacture/client/{idClient}
        [HttpGet("client/{idClient}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetClientFacturesByClient(int idClient)
        {
            var clientFactures = await _clientFactureRepository.GetByClientAsync(idClient);
            var dtos = new List<ClientFactureDto>();

            foreach (var cf in clientFactures)
            {
                dtos.Add(await ConvertToDtoAsync(cf));
            }

            return Ok(dtos);
        }

        // GET: api/ClientFacture/facture/{idFacture}
        [HttpGet("facture/{idFacture}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetClientFacturesByFacture(int idFacture)
        {
            var clientFactures = await _clientFactureRepository.GetByFactureAsync(idFacture);
            var dtos = new List<ClientFactureDto>();

            foreach (var cf in clientFactures)
            {
                dtos.Add(await ConvertToDtoAsync(cf));
            }

            return Ok(dtos);
        }

        // GET: api/ClientFacture/societe/{idSociete}/annees/{annees}/mois/{mois}
        /// <summary>
        /// Récupère les ClientFacture d'une société pour une année et un mois donnés
        /// où le montant dû est supérieur au montant payé (arriérés)
        /// </summary>
        /// <param name="idSociete">Identifiant de la société</param>
        /// <param name="annees">Année (ex: 2024)</param>
        /// <param name="mois">Mois (format: "01", "02", ..., "12" ou "Janvier", "Février", etc.)</param>
        /// <returns>Liste des ClientFacture avec MontantDu > MontantPaye</returns>
        [HttpGet("societe/{idSociete}/annees/{annees}/mois/{mois}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetClientFacturesBySocieteAnneeMois(
            int idSociete, 
            int annees, 
            string mois)
        {
            if (string.IsNullOrWhiteSpace(mois))
            {
                return BadRequest(new { message = "Le paramètre 'mois' ne peut pas être vide." });
            }

            var clientFactures = await _clientFactureRepository.GetBySocieteAnneeMoisWithArrieresAsync(
                idSociete, 
                annees, 
                mois.Trim());

            var dtos = new List<ClientFactureDto>();

            foreach (var cf in clientFactures)
            {
                dtos.Add(await ConvertToDtoAsync(cf));
            }

            return Ok(dtos);
        }

        // GET: api/ClientFacture/societe/{idSociete}/annees/{annees}/mois/{mois}/consolide
        /// <summary>
        /// Récupère les ClientFacture d'une société pour une année et un mois donnés avec statistiques consolidées
        /// où le montant dû est supérieur au montant payé (arriérés)
        /// </summary>
        /// <param name="idSociete">Identifiant de la société</param>
        /// <param name="annees">Année (ex: 2024)</param>
        /// <param name="mois">Mois (format: "01", "02", ..., "12" ou "Janvier", "Février", etc.)</param>
        /// <returns>ClientFactureConsolideDto avec statistiques et liste détaillée</returns>
        [HttpGet("societe/{idSociete}/annees/{annees}/mois/{mois}/consolide")]
        [Authorize]
        public async Task<ActionResult<ClientFactureConsolideDto>> GetClientFacturesBySocieteAnneeMoisConsolide(
            int idSociete, 
            int annees, 
            string mois)
        {
            if (string.IsNullOrWhiteSpace(mois))
            {
                return BadRequest(new { message = "Le paramètre 'mois' ne peut pas être vide." });
            }

            try
            {
                var result = await _clientFactureRepository.GetBySocieteAnneeMoisWithStatsAsync(
                    idSociete, 
                    annees, 
                    mois.Trim());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération des factures consolidées pour la société {SocieteId}, année {Annee}, mois {Mois}", 
                    idSociete, annees, mois);
                return StatusCode(500, new { message = "Erreur lors de la récupération des données consolidées" });
            }
        }

        // GET: api/ClientFacture/template-excel
        /// <summary>
        /// Génère et retourne un template Excel pour l'import en masse d'arriérés pré-existants
        /// </summary>
        /// <returns>Fichier Excel template</returns>
        [HttpGet("template-excel")]
        [Authorize(Roles = "Super-Admin,Admin, Technicien,Financier, Responsable Commercial")]
        public IActionResult GetTemplateExcel()
        {
            try
            {
                var template = _excelClientFactureService.GenerateTemplate();
                var fileName = $"Template_Arrieres_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(template, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la génération du template Excel");
                return StatusCode(500, new { message = $"Erreur lors de la génération du template : {ex.Message}" });
            }
        }

        // POST: api/ClientFacture/bulk-excel
        /// <summary>
        /// ✨ NOUVEAU : Import en masse d'arriérés pré-existants depuis un fichier Excel
        /// Le fichier doit contenir : CodeCons, Montant, Mois, Annees
        /// Le CodeCons est utilisé pour récupérer l'IdClient
        /// </summary>
        /// <param name="file">Fichier Excel (.xlsx)</param>
        /// <returns>Résultat de l'import avec statistiques</returns>
        [HttpPost("bulk-excel")]
        [Authorize(Roles = "Super-Admin,Admin,Financier, Technicien")]
        public async Task<ActionResult<BulkClientFactureResult>> BulkInsertFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Le fichier Excel est requis" });
            }

            try
            {
                var result = await _excelClientFactureService.ProcessExcelFileAsync(file);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'import Excel");
                return StatusCode(500, new { message = $"Erreur lors de l'import : {ex.Message}" });
            }
        }

        // POST: api/ClientFacture
        [HttpPost]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<ClientFactureDto>> CreateClientFacture([FromBody] CreateClientFactureDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier que la facture existe
            var facture = await _context.Factures
                .Include(f => f.Usage)
                .FirstOrDefaultAsync(f => f.IdFacture == dto.IdFacture);
            if (facture == null)
            {
                return NotFound(new { message = "Facture non trouvée" });
            }

            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(dto.IdClient);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // Vérifier si une ClientFacture existe déjà pour ce client et cette facture
            var existing = await _clientFactureRepository.GetByClientAndFactureAsync(dto.IdClient, dto.IdFacture);
            if (existing != null)
            {
                return Conflict(new { message = "Une ClientFacture existe déjà pour ce client et cette facture" });
            }

            // Calculer le montant si non fourni
            decimal montant = dto.Montant ?? 0;
            if (montant == 0)
            {
                // Trouver le ClientUsage pour obtenir nombreBatiment
                var clientUsage = await _context.ClientUsages
                    .FirstOrDefaultAsync(cu => cu.IdClient == dto.IdClient && 
                                               cu.IdUsage == facture.IdUsage && 
                                               cu.Statut == true);
                var nombreBatiment = clientUsage?.nombreBatiment ?? (dto.nombreBatiment ?? 1);
                montant = (facture.Montant ?? 0) * nombreBatiment;
            }

            var clientFacture = new ClientFacture
            {
                IdFacture = dto.IdFacture,
                IdClient = dto.IdClient,
                Montant = montant,
                nombreBatiment = dto.nombreBatiment,
                MontantPaye = 0,
                MontantDu = montant,
                Mois = dto.Mois ?? facture.MoisEmission.ToString("D2"),
                Annees = dto.Annees ?? facture.AnneesEmission,
                DateEmission = dto.DateEmission ?? facture.DateEmission ?? DateTime.Now,
                EstArrierePreExistant = false,
                Statut = true,
                DateCreation = DateTime.Now
            };

            var created = await _clientFactureRepository.CreateAsync(clientFacture);

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création ClientFacture");

            var createdDto = await ConvertToDtoAsync(created);
            return CreatedAtAction(nameof(GetClientFacture), new { id = created.IdClientFacture }, createdDto);
        }

        // POST: api/ClientFacture/pre-existant
        [HttpPost("pre-existant")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<ClientFactureDto>> CreateArrierePreExistant([FromBody] CreateArrierePreExistantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier que le client existe
            var client = await _context.Clients.FindAsync(dto.IdClient);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var clientFacture = await _clientFactureRepository.CreatePreExistantAsync(
                dto.IdClient,
                dto.Montant,
                dto.Mois,
                dto.Annees,
                dto.Description,
                dto.DateEmission,
                dto.CodeDevisePrix
            );

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(clientFacture, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création arriéré pré-existant");

            var dtoResult = await ConvertToDtoAsync(clientFacture);
            return CreatedAtAction(nameof(GetClientFacture), new { id = clientFacture.IdClientFacture }, dtoResult);
        }

        // PUT: api/ClientFacture/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<ClientFactureDto>> UpdateClientFacture(int id, [FromBody] UpdateClientFactureDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _clientFactureRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "ClientFacture non trouvée" });
            }

            // Snapshot avant modification
            var oldClientFacture = new ClientFacture
            {
                IdClientFacture = existing.IdClientFacture,
                Montant = existing.Montant,
                MontantPaye = existing.MontantPaye,
                MontantDu = existing.MontantDu,
                Statut = existing.Statut
            };

            // Mettre à jour uniquement les champs fournis
            if (dto.Montant.HasValue)
                existing.Montant = dto.Montant.Value;
            if (dto.MontantPaye.HasValue)
                existing.MontantPaye = dto.MontantPaye.Value;
            if (!string.IsNullOrWhiteSpace(dto.Mois))
                existing.Mois = dto.Mois;
            if (dto.Annees.HasValue)
                existing.Annees = dto.Annees.Value;
            if (dto.DateEmission.HasValue)
                existing.DateEmission = dto.DateEmission.Value;
            if (!string.IsNullOrWhiteSpace(dto.Description))
                existing.Description = dto.Description;
            if (dto.Statut.HasValue)
                existing.Statut = dto.Statut.Value;

            // Recalculer MontantDu si Montant ou MontantPaye ont changé
            if (existing.Montant.HasValue && existing.MontantPaye.HasValue)
            {
                existing.MontantDu = existing.Montant.Value - existing.MontantPaye.Value;
            }

            var updated = await _clientFactureRepository.UpdateAsync(existing);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldClientFacture, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification ClientFacture");

            var updatedDto = await ConvertToDtoAsync(updated);
            return Ok(updatedDto);
        }

        // POST: api/ClientFacture/{idClientFacture}/paiement
        [HttpPost("{idClientFacture}/paiement")]
        [Authorize(Roles = "Super-Admin,Admin,Caissier,Financier, Technicien")]
        public async Task<ActionResult<ClientFactureDto>> EnregistrerPaiementArrierePreExistant(
            int idClientFacture,
            [FromBody] CreatePaiementArrierePreExistantDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Vérifier que la ClientFacture existe et est un arriéré pré-existant
            var clientFacture = await _clientFactureRepository.GetByIdAsync(idClientFacture);
            if (clientFacture == null)
            {
                return NotFound(new { message = "ClientFacture non trouvée" });
            }

            if (!clientFacture.EstArrierePreExistant)
            {
                return BadRequest(new { message = "Cette ClientFacture n'est pas un arriéré pré-existant. Utilisez l'endpoint /api/Paiement pour les factures système." });
            }

            // Vérifier que le montant payé ne dépasse pas le montant dû
            var montantDu = clientFacture.MontantDu ?? 0;
            if (dto.MontantPaye > montantDu)
            {
                return BadRequest(new { 
                    message = $"Le montant payé ({dto.MontantPaye}) dépasse le montant dû ({montantDu})",
                    montantDu = montantDu,
                    montantPaye = dto.MontantPaye
                });
            }

            // Mettre à jour le MontantPaye et MontantDu
            var ancienMontantPaye = clientFacture.MontantPaye ?? 0;
            clientFacture.MontantPaye = ancienMontantPaye + dto.MontantPaye;
            
            if (clientFacture.Montant.HasValue)
            {
                clientFacture.MontantDu = clientFacture.Montant.Value - clientFacture.MontantPaye.Value;
            }
            
            clientFacture.DateModification = DateTime.Now;

            var updated = await _clientFactureRepository.UpdateAsync(clientFacture);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(
                clientFacture,
                updated,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                $"Paiement arriéré pré-existant: {dto.MontantPaye} FCFA");

            var updatedDto = await ConvertToDtoAsync(updated);
            return Ok(new
            {
                message = "Paiement enregistré avec succès",
                clientFacture = updatedDto,
                montantPaye = dto.MontantPaye,
                montantDu = updatedDto.MontantDu
            });
        }

        // DELETE: api/ClientFacture/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<object>> DeleteClientFacture(int id)
        {
            var exists = await _clientFactureRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound(new { message = "ClientFacture non trouvée" });
            }

            var entity = await _clientFactureRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound(new { message = "ClientFacture non trouvée" });
            }

            await _clientFactureRepository.DeleteAsync(id);

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(entity, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Désactivation ClientFacture (soft delete)");

            return Ok(new 
            { 
                message = "ClientFacture désactivée avec succès (soft delete)",
                idClientFacture = id,
                note = "La ClientFacture a été désactivée. Les données sont conservées pour l'historique."
            });
        }

        /// <summary>
        /// Convertit une ClientFacture en DTO avec les informations supplémentaires
        /// </summary>
        private async Task<ClientFactureDto> ConvertToDtoAsync(ClientFacture clientFacture)
        {
            var dto = new ClientFactureDto
            {
                IdClientFacture = clientFacture.IdClientFacture,
                IdFacture = clientFacture.IdFacture,
                IdClient = clientFacture.IdClient,
                Montant = clientFacture.Montant,
                nombreBatiment = clientFacture.nombreBatiment,
                MontantPaye = clientFacture.MontantPaye,
                MontantDu = clientFacture.MontantDu,
                Mois = clientFacture.Mois,
                Annees = clientFacture.Annees,
                DateEmission = clientFacture.DateEmission,
                EstArrierePreExistant = clientFacture.EstArrierePreExistant,
                Description = clientFacture.Description,
                Statut = clientFacture.Statut,
                DateCreation = clientFacture.DateCreation,
                DateModification = clientFacture.DateModification
            };

            // Charger les informations supplémentaires
            var client = await _context.Clients.FindAsync(clientFacture.IdClient);
            dto.NomClient = client?.NomClient;

            if (clientFacture.IdFacture.HasValue)
            {
                var facture = await _context.Factures
                    .Include(f => f.Usage)
                    .FirstOrDefaultAsync(f => f.IdFacture == clientFacture.IdFacture.Value);
                dto.NumeroFacture = facture?.NumeroFacture;
                dto.LibelleUsage = facture?.Usage?.Libelle;
            }

            return dto;
        }

        // GET: api/ClientFacture/report
        /// <summary>
        /// Récupère le rapport des client-factures agrégées par mois/année
        /// Correspond à la requête SQL de reporting des factures clients avec jointures multiples
        /// </summary>
        /// <param name="mois">Mois de facturation (optionnel, défaut: mois-1)</param>
        /// <param name="annees">Année de facturation (optionnel, défaut: année courante)</param>
        /// <param name="axe">Filtre optionnel par nom d'axe</param>
        /// <param name="usage">Filtre optionnel par libellé d'usage</param>
        /// <param name="limit">Nombre maximum de résultats (défaut: 200)</param>
        /// <returns>Liste des factures clients agrégées avec informations client, axe et usage</returns>
        [HttpGet("report")]
        [Authorize(Roles = "Admin,Super-Admin,Responsable Commercial,Financier")]
        [ProducesResponseType(typeof(IEnumerable<ClientFactureReportDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<ClientFactureReportDto>>> GetClientFacturesReport(
            [FromQuery] string? mois = null, 
            [FromQuery] int? annees = null, 
            [FromQuery] string? axe = null,
            [FromQuery] string? usage = null,
            [FromQuery] int limit = 200)
        {
            try
            {
                _logger.LogInformation("Demande de rapport ClientFacture - Mois: {Mois}, Années: {Annees}, Axe: {Axe}, Usage: {Usage}, Limit: {Limit}", 
                    mois, annees, axe, usage, limit);

                var rapport = await _clientFactureRepository.GetClientFacturesReportAsync(mois, annees, axe, usage, limit);

                if (!rapport.Any())
                {
                    return Ok(new List<ClientFactureReportDto>());
                }

                return Ok(rapport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rapport ClientFacture");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }

        // GET: api/ClientFacture/report/paged
        /// <summary>
        /// Récupère le rapport des client-factures agrégées par mois/année avec pagination
        /// Correspond à la requête SQL de reporting des factures clients avec jointures multiples
        /// </summary>
        /// <param name="request">Paramètres de pagination</param>
        /// <param name="mois">Mois de facturation (optionnel, défaut: mois-1)</param>
        /// <param name="annees">Année de facturation (optionnel, défaut: année courante)</param>
        /// <param name="axe">Filtre optionnel par nom d'axe</param>
        /// <param name="usage">Filtre optionnel par libellé d'usage</param>
        /// <returns>Résultat paginé des factures clients agrégées avec informations client, axe et usage</returns>
        [HttpGet("report/paged")]
        [ProducesResponseType(typeof(PagedResult<ClientFactureReportDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<PagedResult<ClientFactureReportDto>>> GetClientFacturesReportPaged(
            [FromQuery] PagedRequest request,
            [FromQuery] string? mois = null, 
            [FromQuery] int? annees = null, 
            [FromQuery] string? axe = null,
            [FromQuery] string? usage = null)
        {
            try
            {
                _logger.LogInformation("Demande de rapport ClientFacture paginé - Mois: {Mois}, Années: {Annees}, Axe: {Axe}, Usage: {Usage}, Page: {Page}, Size: {Size}", 
                    mois, annees, axe, usage, request.PageNumber, request.PageSize);

                var rapport = await _clientFactureRepository.GetClientFacturesReportPagedAsync(request, mois, annees, axe, usage);

                return Ok(rapport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rapport ClientFacture paginé");
                return StatusCode(500, new { message = "Erreur interne du serveur", details = ex.Message });
            }
        }
    }
}
