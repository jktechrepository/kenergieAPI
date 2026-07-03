using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs.ClientCrashed;
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
    public class ClientCrashedController : ControllerBase
    {
        private readonly KenergieDbContext _context;
        private readonly IClientRepository _clientRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ClientCrashedController> _logger;

        public ClientCrashedController(
            KenergieDbContext context,
            IClientRepository clientRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ILogger<ClientCrashedController> logger)
        {
            _context = context;
            _clientRepository = clientRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les lignes échouées
        /// GET: api/ClientCrashed
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientCrashedResponseDto>>> GetAll()
        {
            var clientsCrashed = await _context.ClientsCrashed
                .OrderByDescending(cc => cc.DateCreation)
                .ToListAsync();

            var response = clientsCrashed.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Récupère une ligne échouée par ID
        /// GET: api/ClientCrashed/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientCrashedResponseDto>> GetById(int id)
        {
            var clientCrashed = await _context.ClientsCrashed
                .FirstOrDefaultAsync(cc => cc.IdClientCrashed == id);

            if (clientCrashed == null)
            {
                return NotFound(new { message = "Ligne échouée non trouvée" });
            }

            return Ok(MapToResponseDto(clientCrashed));
        }

        /// <summary>
        /// Récupère les lignes échouées d'une société
        /// GET: api/ClientCrashed/societe/{idSociete}
        /// </summary>
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<ClientCrashedResponseDto>>> GetBySociete(int idSociete)
        {
            var clientsCrashed = await _context.ClientsCrashed
                .Where(cc => cc.IdSociete == idSociete)
                .OrderByDescending(cc => cc.DateCreation)
                .ToListAsync();

            var response = clientsCrashed.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Récupère les lignes échouées par statut
        /// GET: api/ClientCrashed/statut/{statut}
        /// </summary>
        [HttpGet("statut/{statut}")]
        public async Task<ActionResult<IEnumerable<ClientCrashedResponseDto>>> GetByStatut(string statut)
        {
            var clientsCrashed = await _context.ClientsCrashed
                .Where(cc => cc.Statut == statut)
                .OrderByDescending(cc => cc.DateCreation)
                .ToListAsync();

            var response = clientsCrashed.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Récupère les lignes échouées d'une société par statut
        /// GET: api/ClientCrashed/societe/{idSociete}/statut/{statut}
        /// </summary>
        [HttpGet("societe/{idSociete}/statut/{statut}")]
        public async Task<ActionResult<IEnumerable<ClientCrashedResponseDto>>> GetBySocieteAndStatut(int idSociete, string statut)
        {
            var clientsCrashed = await _context.ClientsCrashed
                .Where(cc => cc.IdSociete == idSociete && cc.Statut == statut)
                .OrderByDescending(cc => cc.DateCreation)
                .ToListAsync();

            var response = clientsCrashed.Select(MapToResponseDto).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Met à jour une ligne échouée
        /// PUT: api/ClientCrashed/{id}
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<ClientCrashedResponseDto>> Update(int id, UpdateClientCrashedDto dto)
        {
            var clientCrashed = await _context.ClientsCrashed
                .FirstOrDefaultAsync(cc => cc.IdClientCrashed == id);

            if (clientCrashed == null)
            {
                return NotFound(new { message = "Ligne échouée non trouvée" });
            }

            // Snapshot avant modification pour l'audit
            var oldClientCrashed = new ClientCrashed
            {
                IdClientCrashed = clientCrashed.IdClientCrashed,
                IdSociete = clientCrashed.IdSociete,
                NumeroLigne = clientCrashed.NumeroLigne,
                NomClient = clientCrashed.NomClient,
                AdresseClient = clientCrashed.AdresseClient,
                Telephone = clientCrashed.Telephone,
                EmailClient = clientCrashed.EmailClient,
                GenreClient = clientCrashed.GenreClient,
                CodeCons = clientCrashed.CodeCons,
                LibelleUsage = clientCrashed.LibelleUsage,
                Statut = clientCrashed.Statut
            };

            // Mettre à jour les champs
            if (dto.NomClient != null)
                clientCrashed.NomClient = dto.NomClient;
            if (dto.AdresseClient != null)
                clientCrashed.AdresseClient = dto.AdresseClient;
            if (dto.Telephone != null)
                clientCrashed.Telephone = dto.Telephone;
            if (dto.EmailClient != null)
                clientCrashed.EmailClient = dto.EmailClient;
            if (dto.GenreClient != null)
                clientCrashed.GenreClient = dto.GenreClient;
            if (dto.CodeCons != null)
                clientCrashed.CodeCons = dto.CodeCons;
            if (dto.LibelleUsage != null)
                clientCrashed.LibelleUsage = dto.LibelleUsage;
            if (dto.Statut != null)
                clientCrashed.Statut = dto.Statut;

            clientCrashed.DateModification = DateTime.Now;

            // Si le statut passe à CORRIGE, mettre à jour DateCorrection
            if (dto.Statut == "CORRIGE" && clientCrashed.DateCorrection == null)
            {
                clientCrashed.DateCorrection = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(
                oldClientCrashed,
                clientCrashed,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                "Mise à jour ligne échouée"
            );

            return Ok(MapToResponseDto(clientCrashed));
        }

        /// <summary>
        /// Réessaye la création d'un client à partir d'une ligne échouée
        /// POST: api/ClientCrashed/{id}/retry
        /// </summary>
        [HttpPost("{id}/retry")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult<RetryClientCrashedResponseDto>> Retry(int id)
        {
            var clientCrashed = await _context.ClientsCrashed
                .FirstOrDefaultAsync(cc => cc.IdClientCrashed == id);

            if (clientCrashed == null)
            {
                return NotFound(new { message = "Ligne échouée non trouvée" });
            }

            try
            {
                // Vérifier si le client existe déjà (par CodeCons)
                Client? existingClient = null;
                if (!string.IsNullOrWhiteSpace(clientCrashed.CodeCons))
                {
                    existingClient = await _clientRepository.GetByCodeConsAsync(clientCrashed.CodeCons);
                }

                if (existingClient != null)
                {
                    // Le client existe déjà, mettre à jour le statut
                    clientCrashed.Statut = "CORRIGE";
                    clientCrashed.IdClientCree = existingClient.IdClient;
                    clientCrashed.DateCorrection = DateTime.Now;
                    clientCrashed.DateModification = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return Ok(new RetryClientCrashedResponseDto
                    {
                        Success = true,
                        Message = $"Un client avec ce CodeCons existe déjà (ID: {existingClient.IdClient})",
                        IdClientCree = existingClient.IdClient,
                        IdClientCrashed = clientCrashed.IdClientCrashed
                    });
                }

                // Préparer le client
                var client = new Client
                {
                    NomClient = clientCrashed.NomClient ?? "",
                    AdresseClient = clientCrashed.AdresseClient ?? "",
                    Telephone = clientCrashed.Telephone,
                    EmailClient = clientCrashed.EmailClient,
                    GenreClient = clientCrashed.GenreClient,
                    CodeCons = clientCrashed.CodeCons,
                    Statut = true,
                    IsActif = true
                };

                // Parser les usages depuis LibelleUsage
                var usagesList = new List<(string LibelleUsage, int nombreBatiment, int? IdTypeDeCourant)>();
                if (!string.IsNullOrWhiteSpace(clientCrashed.LibelleUsage))
                {
                    // Séparer par virgule ou point-virgule
                    var usages = clientCrashed.LibelleUsage
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(u => u.Trim())
                        .Where(u => !string.IsNullOrWhiteSpace(u))
                        .ToList();

                    foreach (var usageLibelle in usages)
                    {
                        usagesList.Add((usageLibelle, 1, null)); // nombreBatiment par défaut à 1
                    }
                }

                // Créer le client avec ses usages
                Client created;
                if (usagesList.Count > 0)
                {
                    created = await _clientRepository.CreateWithUsagesAsync(client, usagesList);
                }
                else
                {
                    created = await _clientRepository.CreateAsync(client);
                }

                // Mettre à jour le ClientCrashed
                clientCrashed.Statut = "CORRIGE";
                clientCrashed.IdClientCree = created.IdClient;
                clientCrashed.DateCorrection = DateTime.Now;
                clientCrashed.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();

                // Audit
                var ctx = this.GetAuditContext();
                await _auditService.LogCreateAsync(
                    created,
                    ctx.UserId,
                    ctx.UserName,
                    ctx.UserRole,
                    ctx.IdSociete,
                    ctx.IpAddress,
                    ctx.UserAgent,
                    $"Client créé depuis ligne échouée (IdClientCrashed: {id})"
                );

                _logger.LogInformation("✅ Client créé avec succès depuis ClientCrashed {IdClientCrashed}, IdClient: {IdClient}", 
                    id, created.IdClient);

                return Ok(new RetryClientCrashedResponseDto
                {
                    Success = true,
                    Message = "Client créé avec succès",
                    IdClientCree = created.IdClient,
                    IdClientCrashed = clientCrashed.IdClientCrashed
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la réessai de création pour ClientCrashed {IdClientCrashed}", id);

                // Mettre à jour le message d'erreur
                clientCrashed.MessageErreur = ex.Message;
                clientCrashed.DateModification = DateTime.Now;
                await _context.SaveChangesAsync();

                return StatusCode(500, new RetryClientCrashedResponseDto
                {
                    Success = false,
                    Message = "Erreur lors de la création du client",
                    IdClientCrashed = clientCrashed.IdClientCrashed,
                    Erreur = ex.Message
                });
            }
        }

        /// <summary>
        /// Supprime/ignore une ligne échouée
        /// DELETE: api/ClientCrashed/{id}
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Super-Admin,Admin")]
        public async Task<ActionResult> Delete(int id)
        {
            var clientCrashed = await _context.ClientsCrashed
                .FirstOrDefaultAsync(cc => cc.IdClientCrashed == id);

            if (clientCrashed == null)
            {
                return NotFound(new { message = "Ligne échouée non trouvée" });
            }

            // Marquer comme IGNORE au lieu de supprimer (soft delete)
            clientCrashed.Statut = "IGNORE";
            clientCrashed.DateModification = DateTime.Now;
            await _context.SaveChangesAsync();

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(
                clientCrashed,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                "Ligne échouée ignorée"
            );

            return Ok(new { message = "Ligne échouée ignorée avec succès" });
        }

        /// <summary>
        /// Supprime définitivement une ligne échouée (hard delete)
        /// DELETE: api/ClientCrashed/{id}/permanent
        /// </summary>
        [HttpDelete("{id}/permanent")]
        [Authorize(Roles = "Super-Admin")]
        public async Task<ActionResult> DeletePermanent(int id)
        {
            var clientCrashed = await _context.ClientsCrashed
                .FirstOrDefaultAsync(cc => cc.IdClientCrashed == id);

            if (clientCrashed == null)
            {
                return NotFound(new { message = "Ligne échouée non trouvée" });
            }

            // Audit avant suppression
            var ctx = this.GetAuditContext();
            await _auditService.LogDeleteAsync(
                clientCrashed,
                ctx.UserId,
                ctx.UserName,
                ctx.UserRole,
                ctx.IdSociete,
                ctx.IpAddress,
                ctx.UserAgent,
                "Suppression définitive ligne échouée"
            );

            _context.ClientsCrashed.Remove(clientCrashed);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ligne échouée supprimée définitivement" });
        }

        /// <summary>
        /// Mappe ClientCrashed vers ClientCrashedResponseDto
        /// </summary>
        private ClientCrashedResponseDto MapToResponseDto(ClientCrashed clientCrashed)
        {
            return new ClientCrashedResponseDto
            {
                IdClientCrashed = clientCrashed.IdClientCrashed,
                IdSociete = clientCrashed.IdSociete,
                NumeroLigne = clientCrashed.NumeroLigne,
                NomClient = clientCrashed.NomClient,
                AdresseClient = clientCrashed.AdresseClient,
                Telephone = clientCrashed.Telephone,
                EmailClient = clientCrashed.EmailClient,
                GenreClient = clientCrashed.GenreClient,
                CodeCons = clientCrashed.CodeCons,
                LibelleUsage = clientCrashed.LibelleUsage,
                DonneesBrutesJson = clientCrashed.DonneesBrutesJson,
                MessageErreur = clientCrashed.MessageErreur,
                TypeErreur = clientCrashed.TypeErreur,
                ErreursJson = clientCrashed.ErreursJson,
                Statut = clientCrashed.Statut,
                IdClientCree = clientCrashed.IdClientCree,
                DateCreation = clientCrashed.DateCreation,
                DateCorrection = clientCrashed.DateCorrection,
                DateModification = clientCrashed.DateModification
            };
        }
    }
}
