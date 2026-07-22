using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.FlexPay;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Models.DTOs.Paiement;
using Kenergie.Services;
using Kenergie.Services.FlexPay;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Kenergie.Data;
using KenergieAPI.Services.Repositories;
using Microsoft.Extensions.Logging;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaiementController : ControllerBase
    {
        private readonly IPaiementRepository _paiementRepository;
        private readonly IFactureRepository _factureRepository;
        private readonly IClientFactureRepository _clientFactureRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly PaiementNotificationService _paiementNotificationService;
        private readonly KenergieDbContext _context;
        private readonly ISignalRNotificationService _signalRNotificationService;
        private readonly ISignalRStatistiquesService _signalRStatistiquesService;
        private readonly IPaiementElectroniqueService _paiementElectroniqueService;
        private readonly ILogger<PaiementController> _logger;

        public PaiementController(
            IPaiementRepository paiementRepository,
            IFactureRepository factureRepository,
            IClientFactureRepository clientFactureRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            PaiementNotificationService paiementNotificationService,
            KenergieDbContext context,
            ISignalRNotificationService signalRNotificationService,
            ISignalRStatistiquesService signalRStatistiquesService,
            IPaiementElectroniqueService paiementElectroniqueService,
            ILogger<PaiementController> logger)
        {
            _paiementRepository = paiementRepository;
            _factureRepository = factureRepository;
            _clientFactureRepository = clientFactureRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _paiementNotificationService = paiementNotificationService;
            _context = context;
            _signalRNotificationService = signalRNotificationService;
            _signalRStatistiquesService = signalRStatistiquesService;
            _paiementElectroniqueService = paiementElectroniqueService;
            _logger = logger;
        }

        // GET: api/Paiement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Paiement>>> GetPaiements()
        {
            var paiements = await _paiementRepository.GetAllAsync();
            return Ok(paiements);
        }

        // GET: api/Paiement/paged
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<Paiement>>> GetPaiementsPaged([FromQuery] PagedRequest request)
        {
            var result = await _paiementRepository.GetPagedAsync(request);
            return Ok(result);
        }

        /// <summary>Initier un paiement électronique FlexPay (MM / carte).</summary>
        [HttpPost("electronique")]
        [Authorize(Roles = "Super-Admin,Admin,Caissier,Financier,Responsable Commercial,Agent Direction Commercial")]
        public async Task<ActionResult<PaiementElectroniquePendingDto>> InitierElectronique([FromBody] InitierPaiementElectroniqueDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var idSociete = await ResolveSocieteElectroniqueAsync(dto);
                if (idSociete <= 0)
                    return BadRequest(new { message = "Impossible de déterminer la société. Fournissez idSociete." });

                if (!_currentUserService.IsSuperAdmin && idSociete != _currentUserService.SocieteId)
                    return Forbid();

                var result = await _paiementElectroniqueService.InitierAsync(
                    dto,
                    idSociete,
                    _currentUserService.UserId > 0 ? _currentUserService.UserId : null);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Statut d'un paiement électronique en attente.</summary>
        [HttpGet("electronique/{idPending:int}")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Financier,Caissier,Responsable Commercial,Agent Direction Commercial")]
        public async Task<ActionResult<PaiementElectroniquePendingDto>> GetPendingElectronique(int idPending)
        {
            int? filter = _currentUserService.IsSuperAdmin ? null : _currentUserService.SocieteId;
            var pending = await _paiementElectroniqueService.GetPendingAsync(idPending, filter);
            if (pending == null)
                return NotFound(new { message = "Paiement électronique introuvable." });
            return Ok(pending);
        }

        // GET: api/Paiement/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Paiement>> GetPaiement(int id)
        {
            var paiement = await _paiementRepository.GetByIdAsync(id);
            if (paiement == null)
            {
                return NotFound();
            }
            return Ok(paiement);
        }

        // GET: api/Paiement/facture/{idFacture}
        [HttpGet("facture/{idFacture}")]
        public async Task<ActionResult<IEnumerable<Paiement>>> GetPaiementsByFacture(int idFacture)
        {
            // Vérifier que la facture existe
            var facture = await _factureRepository.GetByIdAsync(idFacture);
            if (facture == null)
            {
                return NotFound(new { message = "Facture non trouvée" });
            }

            var paiements = await _paiementRepository.GetByFactureAsync(idFacture);
            return Ok(paiements);
        }

        // GET: api/Paiement/client/{idClient}
        [HttpGet("client/{idClient}")]
        public async Task<ActionResult<IEnumerable<Paiement>>> GetPaiementsByClient(int idClient)
        {
            var paiements = await _paiementRepository.GetByClientAsync(idClient);
            return Ok(paiements);
        }

        // GET: api/Paiement/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Financier,Responsable Commercial,Agent Direction Commercial,Caissier,Technicien")]
        public async Task<ActionResult<IEnumerable<Paiement>>> GetPaiementsBySociete(int idSociete)
        {
            var paiements = await _paiementRepository.GetBySocieteAsync(idSociete);
            return Ok(paiements);
        }

        // GET: api/Paiement/societe/{idSociete}/paged
        [HttpGet("societe/{idSociete}/paged")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Financier,Responsable Commercial,Agent Direction Commercial,Caissier,Technicien")]
        public async Task<ActionResult<PagedResultPaiement>> GetPaiementsBySocietePaged(int idSociete, [FromQuery] PaiementPagedRequest request)
        {
            try
            {
                // Validation des filtres
                if (request != null && !request.IsValid())
                {
                    return BadRequest(new { message = "Les filtres de date ne sont pas valides" });
                }

                var result = await _paiementRepository.GetBySocietePagedWithFiltersAsync(idSociete, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements paginés pour la société {IdSociete}", idSociete);
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }

        // GET: api/Paiement/societe/{idSociete}/factureImpayee
        [HttpGet("societe/{idSociete}/factureImpayee")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Financier,Responsable Commercial,Agent Direction Commercial,Caissier,Technicien")]
        public async Task<ActionResult<IEnumerable<FactureImpayeeDto>>> GetFacturesImpayeesBySociete(int idSociete)
        {
            var result = await _paiementRepository.GetFacturesImpayeesBySocieteAsync(idSociete);
            return Ok(result);
        }

        // GET: api/Paiement/societe/{idSociete}/paged/factureImpayee
        [HttpGet("societe/{idSociete}/paged/factureImpayee")]
        [Authorize(Roles = "Super-Admin,Admin,Gerant,Financier,Responsable Commercial,Agent Direction Commercial,Caissier,Technicien")]
        public async Task<ActionResult<PagedResult<FactureImpayeeDto>>> GetFacturesImpayeesBySocietePaged(int idSociete, [FromQuery] PagedRequest request)
        {
            var result = await _paiementRepository.GetFacturesImpayeesBySocietePagedAsync(idSociete, request);
            return Ok(result);
        }

        // POST: api/Paiement
        [HttpPost]
        [Authorize(Roles = "Super-Admin,Admin,Caissier,Financier,Responsable Commercial,Agent Direction Commercial")]
        public async Task<ActionResult<Paiement>> CreatePaiement([FromBody] CreatePaiementDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (Kenergie.Helpers.MethodePaiementHelper.IsFlexPay(dto.MethodePaiement))
            {
                return BadRequest(new
                {
                    message = "Pour Mobile Money / Carte, utilisez POST /api/Paiement/electronique."
                });
            }

            // Vérifier que IdFacture ou IdClientFacture est fourni
            if (!dto.IdFacture.HasValue && !dto.IdClientFacture.HasValue)
            {
                return BadRequest(new { message = "IdFacture (pour factures système) ou IdClientFacture (pour arriérés pré-existants) est requis." });
            }

            // Validation selon le type de paiement
            if (dto.IdFacture.HasValue)
            {
                // Vérifier que la facture existe
                var facture = await _factureRepository.GetByIdAsync(dto.IdFacture.Value);
                if (facture == null)
                {
                    return NotFound(new { message = "Facture non trouvée" });
                }
            }
            else if (dto.IdClientFacture.HasValue)
            {
                // Vérifier que la ClientFacture existe et est un arriéré pré-existant
                var clientFacture = await _clientFactureRepository.GetByIdAsync(dto.IdClientFacture.Value);
                if (clientFacture == null)
                {
                    return NotFound(new { message = "ClientFacture non trouvée" });
                }

                if (!clientFacture.EstArrierePreExistant)
                {
                    return BadRequest(new { message = "Cette ClientFacture n'est pas un arriéré pré-existant. Utilisez IdFacture pour les factures système." });
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
            }

            try
            {
                // Récupérer l'ID de l'utilisateur actuel pour l'enregistrement
                var idUtilisateur = _currentUserService.UserId > 0 ? _currentUserService.UserId : (int?)null;

                // Normaliser le statut : convertir "true" en "Validé"
                var statutNormalise = dto.Statut;
                if (string.IsNullOrWhiteSpace(statutNormalise) || 
                    statutNormalise.ToLower() == "true")
                {
                    statutNormalise = "Validé";
                }

                // Déterminer le type de paiement et l'ID client
                bool estPaiementArriere = dto.IdClientFacture.HasValue;
                int? idClient = dto.IdClient;

                if (estPaiementArriere)
                {
                    // Pour les arriérés, récupérer l'ID client depuis la ClientFacture
                    var clientFacture = await _clientFactureRepository.GetByIdAsync(dto.IdClientFacture!.Value);
                    idClient = clientFacture?.IdClient;
                }

                // Créer le paiement
                var paiement = new Paiement
                {
                    IdFacture = dto.IdFacture,
                    IdClientFacture = dto.IdClientFacture,
                    IdClient = idClient,
                    MontantPaye = dto.MontantPaye,
                    DatePaiement = dto.DatePaiement ?? DateTime.Now,
                    MethodePaiement = dto.MethodePaiement,
                    ReferenceTransaction = dto.ReferenceTransaction,
                    Commentaire = dto.Commentaire,
                    Statut = statutNormalise,
                    IdUtilisateur = idUtilisateur,
                    EstPaiementArriere = estPaiementArriere,
                    CodeDevisePaiement = dto.CodeDevisePaiement
                };

                // Enregistrer le paiement et mettre à jour la facture
                var paiementCree = await _paiementRepository.CreateAsync(paiement);

                // Audit
                var ctx = this.GetAuditContext();
                var auditMessage = estPaiementArriere 
                    ? $"Paiement arriéré ClientFacture {dto.IdClientFacture.Value}"
                    : $"Paiement facture {dto.IdFacture!.Value}";
                await _auditService.LogCreateAsync(paiementCree, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, auditMessage);

                // Recharger les informations selon le type de paiement
                ClientFactureInfoDto? clientFactureInfo = null;
                
                if (estPaiementArriere)
                {
                    // Pour les arriérés, recharger la ClientFacture mise à jour
                    var clientFacture = await _clientFactureRepository.GetByIdAsync(dto.IdClientFacture!.Value);
                    if (clientFacture != null)
                    {
                        clientFactureInfo = new ClientFactureInfoDto
                        {
                            IdClientFacture = clientFacture.IdClientFacture,
                            Montant = clientFacture.Montant,
                            MontantPaye = clientFacture.MontantPaye,
                            MontantDu = clientFacture.MontantDu,
                            NombreBatiment = clientFacture.nombreBatiment,
                            EstArrierePreExistant = clientFacture.EstArrierePreExistant
                        };
                    }
                }
                else
                {
                    // Pour les factures système, recharger la facture et la ClientFacture
                    var facture = await _factureRepository.GetByIdAsync(dto.IdFacture!.Value);
                    
                    if (paiementCree.IdClient.HasValue)
                    {
                        var clientFacture = await _clientFactureRepository.GetByClientAndFactureAsync(
                            paiementCree.IdClient.Value, 
                            paiementCree.IdFacture!.Value);

                        if (clientFacture != null)
                        {
                            clientFactureInfo = new ClientFactureInfoDto
                            {
                                IdClientFacture = clientFacture.IdClientFacture,
                                Montant = clientFacture.Montant,
                                MontantPaye = clientFacture.MontantPaye,
                                MontantDu = clientFacture.MontantDu,
                                NombreBatiment = clientFacture.nombreBatiment,
                                EstArrierePreExistant = clientFacture.EstArrierePreExistant
                            };
                        }
                    }
                    else
                    {
                        // Si IdClient n'est pas fourni, essayer de récupérer la première ClientFacture de la facture
                        var clientFactures = await _clientFactureRepository.GetByFactureAsync(paiementCree.IdFacture!.Value);
                        var firstClientFacture = clientFactures.FirstOrDefault();
                        
                        if (firstClientFacture != null)
                        {
                            clientFactureInfo = new ClientFactureInfoDto
                            {
                                IdClientFacture = firstClientFacture.IdClientFacture,
                                Montant = firstClientFacture.Montant,
                                MontantPaye = firstClientFacture.MontantPaye,
                                MontantDu = firstClientFacture.MontantDu,
                                NombreBatiment = firstClientFacture.nombreBatiment,
                                EstArrierePreExistant = firstClientFacture.EstArrierePreExistant
                            };
                        }
                    }
                }

                // Notification paiement (attendu dans la requête pour éviter la perte du scope DbContext)
                try
                {
                    await _paiementNotificationService.NotifierPaiementAsync(paiementCree);
                }
                catch
                {
                    // Journalisation gérée dans le service, on ne bloque pas la réponse API
                }

                // Notifier les clients connectés du nouveau paiement via SignalR
                try
                {
                    // Récupérer l'ID de la société depuis le paiement
                    var societeId = await GetSocieteIdFromPaiementAsync(paiementCree);
                    
                    if (societeId.HasValue)
                    {
                        // Notifier le dashboard du nouveau paiement
                        await _signalRNotificationService.NotifyNewPaiementAsync(societeId.Value, new
                        {
                            id = paiementCree.IdPaiement,
                            montant = paiementCree.MontantPaye,
                            date = paiementCree.DatePaiement,
                            mode = paiementCree.MethodePaiement,
                            statut = paiementCree.Statut,
                            estPaiementArriere = paiementCree.EstPaiementArriere,
                            idClient = paiementCree.IdClient,
                            idFacture = paiementCree.IdFacture,
                            idClientFacture = paiementCree.IdClientFacture
                        });

                        // Notifier les statistiques du nouveau paiement
                        await _signalRStatistiquesService.NotifyStatistiquesStatusChangeAsync(societeId.Value, "paiement", paiementCree.IdPaiement, "créé");
                        
                        _logger.LogInformation($"📡 SignalR notifications sent for new paiement {paiementCree.IdPaiement} to society {societeId}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error sending SignalR notifications for paiement {paiementCree.IdPaiement}");
                }

                // Préparer la réponse enrichie avec ClientFacture
                var response = new CreatePaiementResponseDto
                {
                    Paiement = paiementCree,
                    Facture = estPaiementArriere ? null : await _factureRepository.GetByIdAsync(dto.IdFacture!.Value),
                    ClientFacture = clientFactureInfo,
                    Message = estPaiementArriere 
                        ? "Paiement d'arriéré pré-existant enregistré avec succès"
                        : "Paiement de facture système enregistré avec succès"
                };

                return CreatedAtAction(nameof(GetPaiement), new { id = paiementCree.IdPaiement }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la création du paiement: {ex.Message}" });
            }
        }

        /// <summary>
        /// Récupère l'ID de la société à partir d'un paiement
        /// </summary>
        private async Task<int?> GetSocieteIdFromPaiementAsync(Paiement paiement)
        {
            try
            {
                // Essayer de récupérer via ClientFacture d'abord
                if (paiement.IdClientFacture.HasValue)
                {
                    var clientFacture = await _context.ClientFactures
                        .Include(cf => cf.Client)
                            .ThenInclude(c => c.ClientsUsages)
                                .ThenInclude(cu => cu.Usage)
                                    .ThenInclude(u => u.CategorieClient)
                        .FirstOrDefaultAsync(cf => cf.IdClientFacture == paiement.IdClientFacture.Value);

                    if (clientFacture?.Client?.ClientsUsages?.Any() == true)
                    {
                        var firstUsage = clientFacture.Client.ClientsUsages.First();
                        if (firstUsage.Usage?.CategorieClient?.IdSociete != null)
                        {
                            return firstUsage.Usage.CategorieClient.IdSociete;
                        }
                    }
                }

                // Essayer via Facture
                if (paiement.IdFacture.HasValue)
                {
                    // Récupérer la facture et ses ClientFactures associées
                    var facture = await _context.Factures.FindAsync(paiement.IdFacture.Value);
                    
                    if (facture != null)
                    {
                        var clientFactures = await _context.ClientFactures
                            .Include(cf => cf.Client)
                                .ThenInclude(c => c.ClientsUsages)
                                    .ThenInclude(cu => cu.Usage)
                                        .ThenInclude(u => u.CategorieClient)
                            .Where(cf => cf.IdFacture == paiement.IdFacture.Value)
                            .FirstOrDefaultAsync();

                        if (clientFactures?.Client?.ClientsUsages?.Any() == true)
                        {
                            var firstUsage = clientFactures.Client.ClientsUsages.First();
                            if (firstUsage.Usage?.CategorieClient?.IdSociete != null)
                            {
                                return firstUsage.Usage.CategorieClient.IdSociete;
                            }
                        }
                    }
                }

                // Essayer via Client direct
                if (paiement.IdClient.HasValue)
                {
                    var client = await _context.Clients
                        .Include(c => c.ClientsUsages)
                            .ThenInclude(cu => cu.Usage)
                                .ThenInclude(u => u.CategorieClient)
                        .FirstOrDefaultAsync(c => c.IdClient == paiement.IdClient.Value);

                    if (client?.ClientsUsages?.Any() == true)
                    {
                        var firstUsage = client.ClientsUsages.First();
                        if (firstUsage.Usage?.CategorieClient?.IdSociete != null)
                        {
                            return firstUsage.Usage.CategorieClient.IdSociete;
                        }
                    }
                }

                _logger.LogWarning($"🔍 Could not determine society ID for paiement {paiement.IdPaiement}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error getting society ID for paiement {paiement.IdPaiement}");
                return null;
            }
        }

        // PUT: api/Paiement/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin,Admin,Caissier,Financier,Responsable Commercial,Agent Direction Commercial")]
        public async Task<ActionResult<UpdatePaiementResponseDto>> UpdatePaiement(int id, [FromBody] Paiement paiement)
        {
            if (id != paiement.IdPaiement)
            {
                return BadRequest(new { message = "L'ID dans l'URL ne correspond pas à l'ID dans le corps" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _paiementRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            // Snapshot avant modification
            var oldPaiement = new Paiement
            {
                IdPaiement = existing.IdPaiement,
                MontantPaye = existing.MontantPaye,
                Statut = existing.Statut,
                DatePaiement = existing.DatePaiement
            };

            var updated = await _paiementRepository.UpdateAsync(paiement);
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldPaiement, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification paiement");

            // Notifier les clients connectés de la mise à jour du paiement via SignalR
            try
            {
                // Récupérer l'ID de la société depuis le paiement mis à jour
                var societeId = await GetSocieteIdFromPaiementAsync(updated);
                
                if (societeId.HasValue)
                {
                    // Notifier le dashboard du changement
                    await _signalRNotificationService.NotifyDashboardStatusChangeAsync(societeId.Value, "paiement", updated.IdPaiement, "mis_à_jour");
                    
                    // Notifier les statistiques du changement
                    await _signalRStatistiquesService.NotifyStatistiquesStatusChangeAsync(societeId.Value, "paiement", updated.IdPaiement, "mis_à_jour");
                    
                    _logger.LogInformation($"📡 SignalR notifications sent for updated paiement {updated.IdPaiement} to society {societeId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending SignalR notifications for updated paiement {updated.IdPaiement}");
            }

            // ✨ NOUVEAU : Récupérer la facture et la ClientFacture mise à jour
            var facture = await _factureRepository.GetByIdAsync(updated.IdFacture!.Value);
            
            ClientFactureInfoDto? clientFactureInfo = null;
            if (updated.IdClient.HasValue)
            {
                var clientFacture = await _clientFactureRepository.GetByClientAndFactureAsync(
                    updated.IdClient.Value, 
                    updated.IdFacture!.Value);

                if (clientFacture != null)
                {
                    clientFactureInfo = new ClientFactureInfoDto
                    {
                        IdClientFacture = clientFacture.IdClientFacture,
                        Montant = clientFacture.Montant,
                        MontantPaye = clientFacture.MontantPaye,
                        MontantDu = clientFacture.MontantDu,
                        NombreBatiment = clientFacture.nombreBatiment,
                        EstArrierePreExistant = clientFacture.EstArrierePreExistant
                    };
                }
            }
            else
            {
                // Si IdClient n'est pas fourni, récupérer la première ClientFacture de la facture
                var clientFactures = await _clientFactureRepository.GetByFactureAsync(updated.IdFacture!.Value);
                var firstClientFacture = clientFactures.FirstOrDefault();
                
                if (firstClientFacture != null)
                {
                    clientFactureInfo = new ClientFactureInfoDto
                    {
                        IdClientFacture = firstClientFacture.IdClientFacture,
                        Montant = firstClientFacture.Montant,
                        MontantPaye = firstClientFacture.MontantPaye,
                        MontantDu = firstClientFacture.MontantDu,
                        NombreBatiment = firstClientFacture.nombreBatiment,
                        EstArrierePreExistant = firstClientFacture.EstArrierePreExistant
                    };
                }
            }

            // Préparer la réponse enrichie avec ClientFacture
            var response = new UpdatePaiementResponseDto
            {
                Paiement = updated,
                Facture = facture,
                ClientFacture = clientFactureInfo,
                Message = "Paiement mis à jour avec succès"
            };

            return Ok(response);
        }

        // DELETE: api/Paiement/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<DeletePaiementResponseDto>> DeletePaiement(int id)
        {
            var exists = await _paiementRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound();
            }

            var entity = await _paiementRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }

            // ✨ NOUVEAU : Sauvegarder les informations avant suppression
            var idFacture = entity.IdFacture!.Value;
            var idClient = entity.IdClient;
            var facture = await _factureRepository.GetByIdAsync(idFacture);

            // Notifier les clients connectés de la suppression du paiement via SignalR
            try
            {
                // Récupérer l'ID de la société depuis le paiement avant suppression
                var societeId = await GetSocieteIdFromPaiementAsync(entity);
                
                if (societeId.HasValue)
                {
                    // Notifier le dashboard de la suppression
                    await _signalRNotificationService.NotifyDashboardStatusChangeAsync(societeId.Value, "paiement", entity.IdPaiement, "supprimé");
                    
                    // Notifier les statistiques de la suppression
                    await _signalRStatistiquesService.NotifyStatistiquesStatusChangeAsync(societeId.Value, "paiement", entity.IdPaiement, "supprimé");
                    
                    _logger.LogInformation($"📡 SignalR notifications sent for deleted paiement {entity.IdPaiement} to society {societeId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error sending SignalR notifications for deleted paiement {entity.IdPaiement}");
            }

            // Supprimer le paiement (cela mettra automatiquement à jour la ClientFacture)
            await _paiementRepository.DeleteAsync(id);

            // ✨ NOUVEAU : Récupérer la ClientFacture mise à jour après suppression
            ClientFactureInfoDto? clientFactureInfo = null;
            if (idClient.HasValue)
            {
                var clientFacture = await _clientFactureRepository.GetByClientAndFactureAsync(
                    idClient.Value, 
                    idFacture);

                if (clientFacture != null)
                {
                    clientFactureInfo = new ClientFactureInfoDto
                    {
                        IdClientFacture = clientFacture.IdClientFacture,
                        Montant = clientFacture.Montant,
                        MontantPaye = clientFacture.MontantPaye,
                        MontantDu = clientFacture.MontantDu,
                        NombreBatiment = clientFacture.nombreBatiment,
                        EstArrierePreExistant = clientFacture.EstArrierePreExistant
                    };
                }
            }
            else
            {
                // Si IdClient n'est pas fourni, récupérer la première ClientFacture de la facture
                var clientFactures = await _clientFactureRepository.GetByFactureAsync(idFacture);
                var firstClientFacture = clientFactures.FirstOrDefault();
                
                if (firstClientFacture != null)
                {
                    clientFactureInfo = new ClientFactureInfoDto
                    {
                        IdClientFacture = firstClientFacture.IdClientFacture,
                        Montant = firstClientFacture.Montant,
                        MontantPaye = firstClientFacture.MontantPaye,
                        MontantDu = firstClientFacture.MontantDu,
                        NombreBatiment = firstClientFacture.nombreBatiment,
                        EstArrierePreExistant = firstClientFacture.EstArrierePreExistant
                    };
                }
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(entity, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Suppression paiement");

            // Préparer la réponse enrichie avec ClientFacture
            var response = new DeletePaiementResponseDto
            {
                PaiementSupprime = entity,
                Facture = facture,
                ClientFacture = clientFactureInfo,
                Message = "Paiement supprimé avec succès"
            };

            return Ok(response);
        }

        // GET: api/Paiement/facture/{idFacture}/total
        /// <summary>
        /// ✨ AMÉLIORÉ : Récupère le total des paiements pour une facture avec totaux consolidés depuis ClientFacture
        /// </summary>
        [HttpGet("facture/{idFacture}/total")]
        [Authorize]
        public async Task<ActionResult<object>> GetTotalPaiementsFacture(int idFacture)
        {
            // Vérifier que la facture existe
            var facture = await _factureRepository.GetByIdAsync(idFacture);
            if (facture == null)
            {
                return NotFound(new { message = "Facture non trouvée" });
            }

            // Total depuis Paiements (pour compatibilité)
            var totalPaiements = await _paiementRepository.GetTotalPaiementsByFactureAsync(idFacture);

            // ✨ NOUVEAU : Totaux consolidés depuis ClientFacture
            var clientFactures = await _context.ClientFactures
                .Where(cf => cf.IdFacture == idFacture && cf.Statut == true)
                .ToListAsync();

            var montantTotalConsolide = clientFactures
                .Where(cf => cf.Montant.HasValue)
                .Sum(cf => cf.Montant.Value);
                
            var montantPayeConsolide = clientFactures
                .Where(cf => cf.MontantPaye.HasValue)
                .Sum(cf => cf.MontantPaye.Value);
                
            var montantDuConsolide = clientFactures
                .Where(cf => cf.MontantDu.HasValue)
                .Sum(cf => cf.MontantDu.Value);

            return Ok(new
            {
                idFacture = idFacture,
                numeroFacture = facture.NumeroFacture,
                totalPaiements = totalPaiements,              // Depuis Paiements (compatibilité)
                montant = facture.Montant,                     // Montant base facture
                // ✨ NOUVEAU : Totaux consolidés
                montantTotalConsolide = montantTotalConsolide,
                montantPayeConsolide = montantPayeConsolide,
                montantDuConsolide = montantDuConsolide,
                nombreClients = clientFactures.Count
            });
        }

        private async Task<int> ResolveSocieteElectroniqueAsync(InitierPaiementElectroniqueDto dto)
        {
            if (dto.IdSociete.HasValue && dto.IdSociete.Value > 0)
                return dto.IdSociete.Value;

            if (!_currentUserService.IsSuperAdmin && _currentUserService.SocieteId > 0)
                return _currentUserService.SocieteId;

            if (dto.IdClientFacture.HasValue)
            {
                var fromCf = await _context.ClientFactures
                    .Where(cf => cf.IdClientFacture == dto.IdClientFacture.Value)
                    .Select(cf => (int?)cf.Facture!.Usage!.CategorieClient!.IdSociete)
                    .FirstOrDefaultAsync();
                if (fromCf.HasValue) return fromCf.Value;

                fromCf = await _context.ClientFactures
                    .Where(cf => cf.IdClientFacture == dto.IdClientFacture.Value)
                    .SelectMany(cf => cf.Client!.ClientsUsages!)
                    .Where(cu => cu.Statut)
                    .Select(cu => (int?)cu.Usage!.CategorieClient!.IdSociete)
                    .FirstOrDefaultAsync();
                if (fromCf.HasValue) return fromCf.Value;
            }

            if (dto.IdFacture.HasValue)
            {
                var fromF = await _context.Factures
                    .Where(f => f.IdFacture == dto.IdFacture.Value)
                    .Select(f => (int?)f.Usage!.CategorieClient!.IdSociete)
                    .FirstOrDefaultAsync();
                if (fromF.HasValue) return fromF.Value;
            }

            return _currentUserService.SocieteId;
        }
    }
}

