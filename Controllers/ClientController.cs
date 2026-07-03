using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Client;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Kenergie.Attributes;
using Kenergie.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace Kenergie.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ClientController : ControllerBase
    {
        private readonly IClientRepository _clientRepository;
        private readonly IAuditService _auditService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ArrieresService _arrieresService;
        private readonly ExcelClientService _excelClientService;
        private readonly ClientExportService _clientExportService;
        private readonly Kenergie.Data.KenergieDbContext _context;
        private readonly ILogger<ClientController> _logger;

        public ClientController(
            IClientRepository clientRepository,
            IAuditService auditService,
            ICurrentUserService currentUserService,
            ArrieresService arrieresService,
            ExcelClientService excelClientService,
            ClientExportService clientExportService,
            Kenergie.Data.KenergieDbContext context,
            ILogger<ClientController> logger)
        {
            _clientRepository = clientRepository;
            _auditService = auditService;
            _currentUserService = currentUserService;
            _arrieresService = arrieresService;
            _excelClientService = excelClientService;
            _clientExportService = clientExportService;
            _context = context;
            _logger = logger;
        }

        // GET: api/Client
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClients()
        {
            var clients = await _clientRepository.GetAllAsync();
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }

        // GET: api/Client/paged
        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ClientResponseDto>>> GetClientsPaged([FromQuery] PagedRequest request)
        {
            var result = await _clientRepository.GetPagedAsync(request);
            var mappedData = MapToClientResponseDtoList(result.Data);
            var mappedResult = new PagedResult<ClientResponseDto>(
                mappedData.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
            return Ok(mappedResult);
        }

        // GET: api/Client/categorie/{idCategorie}
        [HttpGet("categorie/{idCategorie}")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsByCategorie(int idCategorie)
        {
            var clients = await _clientRepository.GetByCategorieAsync(idCategorie);
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }

        // GET: api/Client/societe/{idSociete}
        [HttpGet("societe/{idSociete}")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsBySociete(int idSociete)
        {
            var clients = await _clientRepository.GetBySocieteAsync(idSociete);
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }

        // GET: api/Client/TypeDeCourant/{idTypeDeCourant}
        [HttpGet("TypeDeCourant/{idTypeDeCourant}")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsByTypeDeCourant(int idTypeDeCourant)
        {
            var clients = await _clientRepository.GetByTypeDeCourantAsync(idTypeDeCourant);
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }

        // GET: api/Client/societe/{idSociete}/paged?searchTerm={searchTerm}&includeInactive={includeInactive}&page=1&pageSize=20
        [HttpGet("societe/{idSociete}/paged")]
        public async Task<ActionResult<PagedResult<ClientResponseDto>>> GetClientsBySocietePaged(int idSociete, [FromQuery] ClientPagedSearchRequestDto request)
        {
            var result = await _clientRepository.GetBySocietePagedAsync(idSociete, request);
            var mappedData = MapToClientResponseDtoList(result.Data);
            var mappedResult = new PagedResult<ClientResponseDto>(
                mappedData.ToList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize
            );
            return Ok(mappedResult);
        }

        // GET: api/Client/societe/{idSociete}/recherche?searchTerm={searchTerm}&includeInactive={includeInactive}
        [HttpGet("societe/{idSociete}/recherche")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsBySocieteAndSearch(int idSociete, [FromQuery] ClientSearchRequestDto request)
        {
            var clients = await _clientRepository.GetBySocieteAndSearchAsync(idSociete, request.SearchTerm, request.IncludeInactive);
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }

        // GET: api/Client/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientResponseDto>> GetClient(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }
            var response = MapToClientResponseDto(client);
            return Ok(response);
        }

        // GET: api/Client/nom/{nom}
        [HttpGet("nom/{nom}")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsByNom(string nom)
        {
            var clients = await _clientRepository.GetByNomAsync(nom);
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }

        // GET: api/Client/nom/{nom}
        [HttpGet("isActif/{isActif}")]
        public async Task<ActionResult<IEnumerable<ClientResponseDto>>> GetClientsByIsActif(bool isActif)
        {
            var clients = await _clientRepository.GetByIsActifAsync(isActif);
            var response = MapToClientResponseDtoList(clients);
            return Ok(response);
        }
        
        // GET: api/Client/codecons?codeCons={codeCons}
        [HttpGet("codecons")]
        public async Task<ActionResult<ClientResponseDto>> GetClientByCodeCons([FromQuery] string codeCons)
        {
            if (string.IsNullOrWhiteSpace(codeCons))
            {
                return BadRequest(new { message = "Le CodeCons ne peut pas être vide." });
            }

            // Décoder le paramètre URL si nécessaire
            var decodedCodeCons = Uri.UnescapeDataString(codeCons);

            var client = await _clientRepository.GetByCodeConsAsync(decodedCodeCons);
            if (client == null)
            {
                return NotFound(new { message = "Aucun client trouvé avec ce CodeCons." });
            }
            var response = MapToClientResponseDto(client);
            return Ok(response);
        }

        // POST: api/Client
        [HttpPost]
        public async Task<ActionResult<Client>> CreateClient([FromBody] CreateClientWithUsagesDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Valider que la liste des usages n'est pas vide
            if (dto.Usages == null || dto.Usages.Count == 0)
            {
                return BadRequest(new { message = "Au moins un usage doit être fourni dans la propriété 'usages'." });
            }

            // Créer l'objet Client à partir du DTO
            var client = new Client
            {
                NomClient = dto.NomClient,
                AdresseClient = dto.AdresseClient,
                Telephone = dto.Telephone,
                EmailClient = dto.EmailClient,
                GenreClient = dto.GenreClient,
                CodeCons = dto.CodeCons,
                Statut = dto.Statut,
                IsActif = dto.IsActif,
                IdAxe = dto.IdAxe
            };

            // Préparer la liste des usages
            var usages = dto.Usages.Select(u => (u.LibelleUsage, u.nombreBatiment, u.IdTypeDeCourant)).ToList();

            // Créer le client avec ses usages dans une transaction
            Client created;
            try
            {
                created = await _clientRepository.CreateWithUsagesAsync(client, usages);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par un autre client." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la création du client avec usages: {ex.Message}" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création client avec usages");

            return CreatedAtAction(nameof(GetClient), new { id = created.IdClient }, created);
        }

        // POST: api/Client/simple
        [HttpPost("simple")]
        public async Task<ActionResult<Client>> CreateClientSimple(Client client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Client created;
            try
            {
                created = await _clientRepository.CreateAsync(client);
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par un autre client." });
            }
            
            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogCreateAsync(created, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Création client");

            return CreatedAtAction(nameof(GetClient), new { id = created.IdClient }, created);
        }

        // PUT: api/Client/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Client>> UpdateClient(int id, [FromBody] UpdateClientWithUsagesDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _clientRepository.GetByIdAsync(id);
            if (existing == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // Snapshot avant modification
            var oldClient = new Client
            {
                IdClient = existing.IdClient,
                NomClient = existing.NomClient,
                AdresseClient = existing.AdresseClient,
                Statut = existing.Statut
            };

            // Créer l'objet Client à partir du DTO (seulement les champs fournis)
            var client = new Client
            {
                IdClient = id,
                NomClient = dto.NomClient ?? existing.NomClient,
                AdresseClient = dto.AdresseClient ?? existing.AdresseClient,
                Telephone = dto.Telephone ?? existing.Telephone,
                EmailClient = dto.EmailClient ?? existing.EmailClient,
                GenreClient = dto.GenreClient ?? existing.GenreClient,
                CodeCons = dto.CodeCons ?? existing.CodeCons,
                Statut = dto.Statut.HasValue ? dto.Statut.Value : existing.Statut,
                IsActif = dto.IsActif.HasValue ? dto.IsActif.Value : existing.IsActif,
                IdAxe = dto.IdAxe ?? existing.IdAxe
            };

            // Préparer la liste des usages si fournie
            List<(string LibelleUsage, int nombreBatiment, bool Statut, int? IdTypeDeCourant)>? usages = null;
            if (dto.Usages != null && dto.Usages.Count > 0)
            {
                usages = dto.Usages.Select(u => (u.LibelleUsage, u.nombreBatiment, u.Statut, u.IdTypeDeCourant)).ToList();
            }

            Client updated;
            try
            {
                // Utiliser UpdateWithUsagesAsync si des usages sont fournis, sinon UpdateAsync
                if (usages != null && usages.Count > 0)
                {
                    updated = await _clientRepository.UpdateWithUsagesAsync(id, client, usages);
                }
                else
                {
                    updated = await _clientRepository.UpdateAsync(client);
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new { message = "Cet email est déjà utilisé par un autre client." });
            }
            
            if (updated == null)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour" });
            }

            // Audit
            var ctx = this.GetAuditContext();
            await _auditService.LogUpdateAsync(oldClient, updated, ctx.UserId, ctx.UserName, ctx.UserRole, ctx.IdSociete, ctx.IpAddress, ctx.UserAgent, "Modification client avec usages");

            return Ok(updated);
        }

        // DELETE: api/Client/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteClient(int id)
        {
            var exists = await _clientRepository.ExistsAsync(id);
            if (!exists)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var entity = await _clientRepository.GetByIdAsync(id);
            if (entity == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            // ✨ NOUVEAU : Vérifier les dépendances avant suppression
            var hasClientFactures = await _context.ClientFactures
                .AnyAsync(cf => cf.IdClient == id && cf.Statut == true);
            
            var hasPaiements = await _context.Paiements
                .AnyAsync(p => p.IdClient == id);

            var hasFactures = await _context.Factures
                .Include(f => f.Usage)
                    .ThenInclude(u => u.ClientsUsages)
                .AnyAsync(f => f.Usage != null && f.Usage.ClientsUsages.Any(cu => cu.IdClient == id));

            if (hasClientFactures || hasPaiements || hasFactures)
            {
                return BadRequest(new 
                { 
                    message = "Impossible de supprimer ce client car des données sont liées.",
                    details = new
                    {
                        hasClientFactures,
                        hasPaiements,
                        hasFactures
                    },
                    note = "Le client sera désactivé (soft delete) au lieu d'être supprimé."
                });
            }
            else
            { 
                return Ok(new 
                { 
                    message = "Client désactivé avec succès (soft delete)",
                    clientId = id,
                    note = "Le client et ses ClientUsage ont été désactivés. Les données sont conservées pour l'historique."
                });
            }
        }

        // PUT: api/Client/toggle-statut/{id}
        [HttpPut("toggle-statut/{id}")]
        public async Task<ActionResult<object>> ToggleStatut(int id)
        {
            try
            {
                var success = await _clientRepository.ToggleStatutAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Client non trouvé" });
                }

                var client = await _clientRepository.GetByIdAsync(id);
                var nouveauStatut = client?.Statut ?? false;

                return Ok(new
                {
                    message = "Statut modifié avec succès",
                    statut = nouveauStatut,
                    client = client
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la modification du statut: {ex.Message}" });
            }
        }

        // PUT: api/Client/toggle-isActif/{id}
        [HttpPut("toggle-isActif/{id}")]
        [Authorize]
        public async Task<ActionResult<object>> ToggleIsActif(int id)
        {
            try
            {
                var success = await _clientRepository.ToggleIsActifAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Client non trouvé" });
                }

                var client = await _clientRepository.GetByIdAsync(id);
                var nouveauIsActif = client?.IsActif ?? false;

                return Ok(new
                {
                    message = "IsActif modifié avec succès",
                    statut = nouveauIsActif,
                    client = client
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la modification du statut: {ex.Message}" });
            }
        }
        
        // PUT: api/Client/set-statut/{id}
        [HttpPut("set-statut/{id}")]
        public async Task<ActionResult<object>> SetStatut(int id, [FromQuery] bool statut)
        {
            try
            {
                var success = await _clientRepository.SetStatutAsync(id, statut);
                if (!success)
                {
                    return NotFound(new { message = "Client non trouvé" });
                }

                var client = await _clientRepository.GetByIdAsync(id);

                return Ok(new
                {
                    message = $"Statut défini à {statut}",
                    statut = statut,
                    client = client
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la définition du statut: {ex.Message}" });
            }
        }

        // GET: api/Client/{id}/arrieres
        [HttpGet("{id}/arrieres")]
        [Authorize]
        public async Task<ActionResult<ArrieresClientDto>> GetArrieresClient(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var arrieres = await _arrieresService.GetArrieresByClientAsync(id);
            if (arrieres == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            return Ok(arrieres);
        }

        // GET: api/Client/{id}/factures-impayees
        [HttpGet("{id}/factures-impayees")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FactureImpayeeDto>>> GetFacturesImpayeesClient(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var facturesImpayees = await _arrieresService.GetFacturesImpayeesByClientAsync(id);
            return Ok(facturesImpayees);
        }

        // GET: api/Client/{id}/factures-impayees/paged
        [HttpGet("{id}/factures-impayees/paged")]
        [Authorize]
        public async Task<ActionResult<PagedResult<FactureImpayeeDto>>> GetFacturesImpayeesClientPaged(int id, [FromQuery] PagedRequest request)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var result = await _arrieresService.GetFacturesImpayeesByClientPagedAsync(id, request);
            return Ok(result);
        }

        // GET: api/Client/{id}/factures-payees/paged
        [HttpGet("{id}/factures-payees/paged")]
        [Authorize]
        public async Task<ActionResult<PagedResult<FacturePayeeDto>>> GetFacturesPayeesClientPaged(int id, [FromQuery] PagedRequest request)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var result = await _arrieresService.GetFacturesPayeesByClientPagedAsync(id, request);
            return Ok(result);
        }

        // GET: api/Client/arrieres/global/{idSociete}
        [HttpGet("arrieres/global/{idSociete}")]
        public async Task<ActionResult<ArrieresGlobalDto>> GetArrieresGlobal(int idSociete)
        {
            var arrieres = await _arrieresService.GetArrieresGlobalAsync(idSociete);
            return Ok(arrieres);
        }

        // GET: api/Client/arrieres/clients/{idSociete}
        [HttpGet("arrieres/clients/{idSociete}")]
        public async Task<ActionResult<IEnumerable<ArrieresClientDto>>> GetClientsAvecArrieres(int idSociete)
        {
            var clients = await _arrieresService.GetClientsAvecArrieresAsync(idSociete);
            return Ok(clients);
        }

        // GET: api/Client/{id}/usages
        [HttpGet("{id}/usages")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Usage>>> GetClientUsages(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var usages = await _clientRepository.GetClientUsagesAsync(id);
            return Ok(usages);
        }

        // GET: api/Client/{id}/usages/details
        [HttpGet("{id}/usages/details")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ClientUsage>>> GetClientUsagesWithDetails(int id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var clientUsages = await _clientRepository.GetClientUsagesWithDetailsAsync(id);
            return Ok(clientUsages);
        }

        // POST: api/Client/{id}/usages/{idUsage}
        [HttpPost("{id}/usages/{idUsage}")]
        public async Task<ActionResult> AddUsageToClient(int id, int idUsage, [FromQuery] int nombreBatiment = 1, [FromQuery] int? idTypeDeCourant = null)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var success = await _clientRepository.AddUsageToClientAsync(id, idUsage, nombreBatiment, idTypeDeCourant);
            if (!success)
            {
                return BadRequest(new { message = "Impossible d'ajouter l'usage au client. Vérifiez que l'usage existe." });
            }

            return Ok(new { message = $"Usage ajouté au client avec succès (nombreBatiment: {nombreBatiment})" });
        }

        // DELETE: api/Client/{id}/usages/{idUsage}
        [HttpDelete("{id}/usages/{idUsage}")]
        public async Task<ActionResult> RemoveUsageFromClient(int id, int idUsage)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            var success = await _clientRepository.RemoveUsageFromClientAsync(id, idUsage);
            if (!success)
            {
                return NotFound(new { message = "Cet usage n'est pas assigné à ce client" });
            }

            return Ok(new { message = "Usage retiré du client avec succès" });
        }

        // GET: api/Client/template-excel
        [HttpGet("template-excel")]
        public IActionResult GetTemplateExcel()
        {
            try
            {
                var template = _excelClientService.GenerateTemplate();
                var fileName = $"Template_Clients_{DateTime.Now:yyyyMMdd}.xlsx";
                return File(template, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de la génération du template : {ex.Message}" });
            }
        }

        // POST: api/Client/bulk-excel
        [HttpPost("bulk-excel")]
        public async Task<ActionResult<BulkClientResult>> BulkInsertFromExcel(
            IFormFile file,
            [FromQuery] int idSociete)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Aucun fichier fourni" });
            }

            try
            {
                var result = await _excelClientService.ProcessExcelFileAsync(file, idSociete);
                
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
                return StatusCode(500, new { message = $"Erreur lors du traitement du fichier : {ex.Message}" });
            }
        }

        /// <summary>
        /// Convertit un Client en ClientResponseDto avec ses informations d'usage
        /// </summary>
        private ClientResponseDto MapToClientResponseDto(Client client)
        {
            if (client == null)
                return null!;

            var dto = new ClientResponseDto
            {
                IdClient = client.IdClient,
                NomClient = client.NomClient,
                AdresseClient = client.AdresseClient,
                Telephone = client.Telephone,
                EmailClient = client.EmailClient,
                GenreClient = client.GenreClient,
                CodeCons = client.CodeCons,
                Statut = client.Statut,
                IsActif = client.IsActif,
                IdAxe = client.IdAxe,
                DateCreation = client.DateCreation,
                Usages = new List<ClientUsageInfoDto>()
            };

            // Récupérer les informations de l'axe si disponible
            if (client.Axe != null)
            {
                dto.NomAxe = client.Axe.NomAxe;
                dto.CodeAxe = client.Axe.CodeAxe;
                dto.IdCabine = client.Axe.IdCabine;

                if (client.Axe.Cabine != null)
                {
                    dto.NomCabine = client.Axe.Cabine.Nom;
                    dto.CodeCabine = client.Axe.Cabine.CodeCabine;
                    dto.IdSociete = client.Axe.Cabine.IdSociete;
                }
            }

            // Mapper les usages
            if (client.ClientsUsages != null && client.ClientsUsages.Any())
            {
                dto.Usages = client.ClientsUsages
                    .Where(cu => cu.Statut == true) // Filtrer seulement les usages actifs
                    .Select(cu => new ClientUsageInfoDto
                    {
                        IdClientUsage = cu.IdClientUsage,
                        IdUsage = cu.IdUsage,
                        LibelleUsage = cu.Usage?.Libelle ?? string.Empty,
                        DescriptionUsage = cu.Usage?.Description,
                        NombreBatiment = cu.nombreBatiment,
                        DateAttribution = cu.DateAttribution,
                        Statut = cu.Statut,
                        IdCategorieClient = cu.Usage?.IdCategorieClient ?? 0,
                        NomCategorie = cu.Usage?.CategorieClient?.NomCategorie,
                        IdSociete = cu.Usage?.CategorieClient?.IdSociete,
                        NomSociete = cu.Usage?.CategorieClient?.Societe?.Nom,
                        IdTypeDeCourant = cu.IdTypeDeCourant
                    })
                    .ToList();
            }

            return dto;
        }

        /// <summary>
        /// Convertit une collection de Clients en ClientResponseDto
        /// </summary>
        private IEnumerable<ClientResponseDto> MapToClientResponseDtoList(IEnumerable<Client> clients)
        {
            return clients.Select(MapToClientResponseDto);
        }

        // GET: api/Client/societe/{idSociete}/export
        [HttpGet("societe/{idSociete}/export")]
        [Authorize]
        public async Task<IActionResult> ExportClientsBySociete(int idSociete, [FromQuery] ClientExportRequest request)
        {
            try
            {
                _logger.LogInformation("Début de l'export des clients pour la société {SocieteId}", idSociete);

                // Validation du type de fichier
                if (string.IsNullOrWhiteSpace(request.FileType) || 
                    (request.FileType.ToLower() != "excel" && request.FileType.ToLower() != "pdf"))
                {
                    return BadRequest("Type de fichier non supporté. Utilisez 'excel' ou 'pdf'.");
                }

                // Pour l'instant, seul Excel est implémenté
                if (request.FileType.ToLower() != "excel")
                {
                    return BadRequest("Seul l'export Excel est actuellement disponible.");
                }

                // Génération du fichier
                var fileBytes = await _clientExportService.ExportToExcelAsync(idSociete, request);

                // Audit trail
                await _auditService.LogCreateAsync(
                    new { ExportType = "Excel", Request = request }, 
                    _currentUserService.UserId, 
                    _currentUserService.UserName ?? "Unknown",
                    _currentUserService.UserRole,
                    idSociete: idSociete,
                    commentaire: $"Export Excel - Société: {idSociete}, Axe: {request.IdAxe}, Recherche: {request.SearchTerm}");

                var fileName = $"clients_societe_{idSociete}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                _logger.LogInformation("Export terminé avec succès - Fichier: {FileName}", fileName);

                return File(fileBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export des clients pour la société {SocieteId}", idSociete);
                return StatusCode(500, "Une erreur est survenue lors de la génération du fichier d'export.");
            }
        }

        private static bool IsDuplicateEmail(DbUpdateException ex)
        {
            var mySqlEx = ex.InnerException as MySqlException
                          ?? ex.InnerException?.InnerException as MySqlException;

            if (mySqlEx != null)
            {
                if (mySqlEx.Number == 1062 || mySqlEx.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
                    return true;
            }

            var message = ex.InnerException?.Message ?? ex.Message;
            return !string.IsNullOrEmpty(message)
                   && message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                   && message.Contains("email", StringComparison.OrdinalIgnoreCase);
        }
    }
}

