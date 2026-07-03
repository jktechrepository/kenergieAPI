using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.ArriereeCrashed;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArriereeCrashedController : ControllerBase
    {
        private readonly KenergieDbContext _context;
        private readonly IClientFactureRepository _clientFactureRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ArriereeCrashedController> _logger;

        public ArriereeCrashedController(
            KenergieDbContext context,
            IClientFactureRepository clientFactureRepository,
            IClientRepository clientRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<ArriereeCrashedController> logger)
        {
            _context = context;
            _clientFactureRepository = clientFactureRepository;
            _clientRepository = clientRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les arriérées échouées
        /// GET: api/ArriereeCrashed
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArriereeCrashedResponseDto>>> GetAll()
        {
            var arriereesCrashed = await _context.ArriereesCrashed
                .OrderByDescending(ac => ac.DateCreation)
                .ToListAsync();

            var response = arriereesCrashed.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Récupère une arriérée échouée par ID
        /// GET: api/ArriereeCrashed/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ArriereeCrashedResponseDto>> GetById(int id)
        {
            var arriereeCrashed = await _context.ArriereesCrashed
                .FirstOrDefaultAsync(ac => ac.IdArriereeCrashed == id);

            if (arriereeCrashed == null)
            {
                return NotFound(new { message = "Arriérée échouée non trouvée" });
            }

            return Ok(MapToResponseDto(arriereeCrashed));
        }

        /// <summary>
        /// Récupère les arriérées échouées par statut
        /// GET: api/ArriereeCrashed/statut/{statut}
        /// </summary>
        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<ArriereeCrashedResponseDto>>> GetByStatut(string statut)
        {
            var arriereesCrashed = await _context.ArriereesCrashed
                .Where(ac => ac.Statut == statut)
                .OrderByDescending(ac => ac.DateCreation)
                .ToListAsync();

            var response = arriereesCrashed.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Met à jour une arriérée échouée
        /// PUT: api/ArriereeCrashed/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin,Admin,Financier")]
        public async Task<ActionResult<ArriereeCrashedResponseDto>> Update(int id, UpdateArriereeCrashedDto dto)
        {
            var arriereeCrashed = await _context.ArriereesCrashed
                .FirstOrDefaultAsync(ac => ac.IdArriereeCrashed == id);

            if (arriereeCrashed == null)
            {
                return NotFound(new { message = "Arriérée échouée non trouvée" });
            }

            // Snapshot avant modification pour l'audit
            var oldArriereeCrashed = new ArriereeCrashed
            {
                IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed,
                NumeroLigne = arriereeCrashed.NumeroLigne,
                CodeCons = arriereeCrashed.CodeCons,
                Montant = arriereeCrashed.Montant,
                Mois = arriereeCrashed.Mois,
                Annees = arriereeCrashed.Annees,
                IdClient = arriereeCrashed.IdClient,
                Statut = arriereeCrashed.Statut
            };

            // Mettre à jour les champs
            if (dto.CodeCons != null)
            {
                arriereeCrashed.CodeCons = dto.CodeCons;
                // Essayer de récupérer l'IdClient si CodeCons est fourni
                if (!string.IsNullOrWhiteSpace(dto.CodeCons))
                {
                    var client = await _clientRepository.GetByCodeConsAsync(dto.CodeCons);
                    if (client != null)
                    {
                        arriereeCrashed.IdClient = client.IdClient;
                    }
                    else
                    {
                        arriereeCrashed.IdClient = null;
                    }
                }
            }
            if (dto.Montant != null)
                arriereeCrashed.Montant = dto.Montant;
            if (dto.Mois != null)
                arriereeCrashed.Mois = dto.Mois;
            if (dto.Annees != null)
                arriereeCrashed.Annees = dto.Annees;
            if (dto.IdClient.HasValue)
                arriereeCrashed.IdClient = dto.IdClient;
            if (dto.Statut != null)
                arriereeCrashed.Statut = dto.Statut;

            arriereeCrashed.DateModification = DateTime.Now;

            // Si le statut passe à CORRIGE, mettre à jour DateCorrection
            if (dto.Statut == "CORRIGE" && arriereeCrashed.DateCorrection == null)
            {
                arriereeCrashed.DateCorrection = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(
                oldArriereeCrashed,
                arriereeCrashed,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                "Mise à jour arriérée échouée"
            );

            return Ok(MapToResponseDto(arriereeCrashed));
        }

        /// <summary>
        /// Réessaye la création d'une arriérée à partir d'une ligne échouée
        /// POST: api/ArriereeCrashed/{id}/retry
        /// </summary>
        [HttpPost("{id}/retry")]
        [Authorize(Roles = "Super-Admin,Admin,Financier")]
        public async Task<ActionResult<RetryArriereeCrashedResponseDto>> Retry(int id)
        {
            var arriereeCrashed = await _context.ArriereesCrashed
                .FirstOrDefaultAsync(ac => ac.IdArriereeCrashed == id);

            if (arriereeCrashed == null)
            {
                return NotFound(new { message = "Arriérée échouée non trouvée" });
            }

            try
            {
                // Vérifier que l'IdClient est disponible
                int? idClient = arriereeCrashed.IdClient;

                // Si IdClient n'est pas disponible, essayer de le récupérer via CodeCons
                if (!idClient.HasValue && !string.IsNullOrWhiteSpace(arriereeCrashed.CodeCons))
                {
                    var client = await _clientRepository.GetByCodeConsAsync(arriereeCrashed.CodeCons);
                    if (client != null)
                    {
                        idClient = client.IdClient;
                        arriereeCrashed.IdClient = idClient;
                    }
                }

                if (!idClient.HasValue)
                {
                    return BadRequest(new RetryArriereeCrashedResponseDto
                    {
                        Success = false,
                        Message = "Impossible de trouver le client. Veuillez corriger le CodeCons ou fournir l'IdClient.",
                        IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                    });
                }

                // Valider et convertir les données
                if (string.IsNullOrWhiteSpace(arriereeCrashed.Montant) || !decimal.TryParse(arriereeCrashed.Montant, out var montant) || montant <= 0)
                {
                    return BadRequest(new RetryArriereeCrashedResponseDto
                    {
                        Success = false,
                        Message = "Le montant est invalide ou manquant.",
                        IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                    });
                }

                if (string.IsNullOrWhiteSpace(arriereeCrashed.Mois))
                {
                    return BadRequest(new RetryArriereeCrashedResponseDto
                    {
                        Success = false,
                        Message = "Le mois est manquant.",
                        IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                    });
                }

                // Normaliser le mois en format "01"-"12"
                string moisNormalise = arriereeCrashed.Mois;
                if (int.TryParse(arriereeCrashed.Mois, out var moisInt) && moisInt >= 1 && moisInt <= 12)
                {
                    moisNormalise = moisInt.ToString("D2");
                }

                if (string.IsNullOrWhiteSpace(arriereeCrashed.Annees) || !int.TryParse(arriereeCrashed.Annees, out var annees))
                {
                    return BadRequest(new RetryArriereeCrashedResponseDto
                    {
                        Success = false,
                        Message = "L'année est invalide ou manquante.",
                        IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                    });
                }

                // Vérifier si une ClientFacture existe déjà (doublon)
                var existing = await _context.ClientFactures
                    .FirstOrDefaultAsync(cf =>
                        cf.IdClient == idClient.Value &&
                        cf.Mois == moisNormalise &&
                        cf.Annees == annees &&
                        cf.EstArrierePreExistant == true &&
                        cf.Statut == true);

                if (existing != null)
                {
                    // Mettre à jour le statut
                    arriereeCrashed.Statut = "CORRIGE";
                    arriereeCrashed.IdClientFactureCree = existing.IdClientFacture;
                    arriereeCrashed.DateCorrection = DateTime.Now;
                    arriereeCrashed.DateModification = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return Ok(new RetryArriereeCrashedResponseDto
                    {
                        Success = true,
                        Message = $"Un arriéré pré-existant existe déjà pour ce client (IdClientFacture: {existing.IdClientFacture})",
                        IdClientFactureCree = existing.IdClientFacture,
                        IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                    });
                }

                // Créer l'arriéré pré-existant
                var clientFacture = await _clientFactureRepository.CreatePreExistantAsync(
                    idClient.Value,
                    montant,
                    moisNormalise,
                    annees,
                    null, // Description
                    null  // DateEmission (utilisera DateTime.Now)
                );

                // Mettre à jour l'ArriereeCrashed
                arriereeCrashed.Statut = "CORRIGE";
                arriereeCrashed.IdClientFactureCree = clientFacture.IdClientFacture;
                arriereeCrashed.DateCorrection = DateTime.Now;
                arriereeCrashed.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    clientFacture,
                    ctx.UserId,
                    ctx.UserName,
                    ctx.UserRole,
                    ctx.IdSociete,
                    ctx.IpAddress,
                    ctx.UserAgent,
                    $"Arriérée créée depuis ligne échouée (IdArriereeCrashed: {id})"
                );

                _logger.LogInformation("✅ Arriérée créée avec succès depuis ArriereeCrashed {IdArriereeCrashed}, IdClientFacture: {IdClientFacture}",
                    id, clientFacture.IdClientFacture);

                return Ok(new RetryArriereeCrashedResponseDto
                {
                    Success = true,
                    Message = "Arriérée créée avec succès",
                    IdClientFactureCree = clientFacture.IdClientFacture,
                    IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la réessai de création pour ArriereeCrashed {IdArriereeCrashed}", id);

                // Mettre à jour le message d'erreur
                arriereeCrashed.MessageErreur = ex.Message;
                arriereeCrashed.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();

                return StatusCode(500, new RetryArriereeCrashedResponseDto
                {
                    Success = false,
                    Message = $"Erreur lors de la création de l'arriérée : {ex.Message}",
                    IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed
                });
            }
        }

        /// <summary>
        /// Supprime/ignore une arriérée échouée
        /// DELETE: api/ArriereeCrashed/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super-Admin,Admin,Financier")]
        public async Task<ActionResult> Delete(int id)
        {
            var arriereeCrashed = await _context.ArriereesCrashed
                .FirstOrDefaultAsync(ac => ac.IdArriereeCrashed == id);

            if (arriereeCrashed == null)
            {
                return NotFound(new { message = "Arriérée échouée non trouvée" });
            }

            // Marquer comme IGNORE au lieu de supprimer (soft delete)
            arriereeCrashed.Statut = "IGNORE";
            arriereeCrashed.DateModification = DateTime.Now;
            await _context.SaveChangesAsync();

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(
                arriereeCrashed,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                "Arriérée échouée ignorée"
            );

            return Ok(new { message = "Arriérée échouée ignorée avec succès" });
        }

        /// <summary>
        /// Supprime définitivement une arriérée échouée (hard delete)
        /// DELETE: api/ArriereeCrashed/{id}/permanent
        /// </summary>
        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "Super-Admin")]
        public async Task<ActionResult> DeletePermanent(int id)
        {
            var arriereeCrashed = await _context.ArriereesCrashed
                .FirstOrDefaultAsync(ac => ac.IdArriereeCrashed == id);

            if (arriereeCrashed == null)
            {
                return NotFound(new { message = "Arriérée échouée non trouvée" });
            }

            // Audit avant suppression
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(
                arriereeCrashed,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                "Suppression définitive arriérée échouée"
            );

            _context.ArriereesCrashed.Remove(arriereeCrashed);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Arriérée échouée supprimée définitivement" });
        }

        /// <summary>
        /// Mappe ArriereeCrashed vers ArriereeCrashedResponseDto
        /// </summary>
        private ArriereeCrashedResponseDto MapToResponseDto(ArriereeCrashed arriereeCrashed)
        {
            return new ArriereeCrashedResponseDto
            {
                IdArriereeCrashed = arriereeCrashed.IdArriereeCrashed,
                NumeroLigne = arriereeCrashed.NumeroLigne,
                CodeCons = arriereeCrashed.CodeCons,
                Montant = arriereeCrashed.Montant,
                Mois = arriereeCrashed.Mois,
                Annees = arriereeCrashed.Annees,
                IdClient = arriereeCrashed.IdClient,
                DonneesBrutesJson = arriereeCrashed.DonneesBrutesJson,
                MessageErreur = arriereeCrashed.MessageErreur,
                TypeErreur = arriereeCrashed.TypeErreur,
                ErreursJson = arriereeCrashed.ErreursJson,
                Statut = arriereeCrashed.Statut,
                IdClientFactureCree = arriereeCrashed.IdClientFactureCree,
                DateCreation = arriereeCrashed.DateCreation,
                DateCorrection = arriereeCrashed.DateCorrection,
                DateModification = arriereeCrashed.DateModification
            };
        }
    }
}
