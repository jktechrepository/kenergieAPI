using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Models.DTOs;
using Kenergie.Models.DTOs.Pagination;
using Kenergie.Services.Repositories;
using KenergieAPI.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kenergie.Services
{
    public class ClientService : IClientRepository
    {
        private readonly KenergieDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ISmsNotificationService _smsService;
        private readonly IUtilisateurRepository _utilisateurRepository;
        private readonly ILogger<ClientService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public ClientService(
            KenergieDbContext context,
            IEmailService emailService,
            ISmsNotificationService smsService,
            IUtilisateurRepository utilisateurRepository,
            ILogger<ClientService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _smsService = smsService;
            _utilisateurRepository = utilisateurRepository;
            _logger = logger;
            _configuration = configuration;
            
            // Récupérer la configuration du frontend
            _baseUrl = _configuration["FrontendSettings:BaseUrl"] ?? "https://k-energie.kansaconsulting.com";
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetByCategorieAsync(int idCategorie)
        {
            // Récupérer les clients qui ont des usages appartenant à cette catégorie
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && 
                           c.ClientsUsages != null &&
                           c.ClientsUsages.Any(cu => cu.Usage != null && 
                                                     cu.Usage.IdCategorieClient == idCategorie))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetBySocieteAsync(int idSociete)
        {
            // Récupérer les clients qui ont des usages appartenant à des catégories de la société
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && 
                           c.ClientsUsages != null &&
                           c.ClientsUsages.Any(cu => cu.Usage != null && 
                                                     cu.Usage.CategorieClient != null &&
                                                     cu.Usage.CategorieClient.IdSociete == idSociete))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetByTypeDeCourantAsync(int idTypeDeCourant)
        {
            // Récupérer les clients qui ont au moins une ligne ClientUsage avec ce type de courant
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true &&
                           c.ClientsUsages != null &&
                           c.ClientsUsages.Any(cu => cu.Statut && cu.IdTypeDeCourant == idTypeDeCourant))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetBySocieteAndSearchAsync(int idSociete, string searchTerm, bool includeInactive = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetBySocieteAsync(idSociete);
            }

            var term = searchTerm.Trim().ToLower();
            
            // LOG DE DÉBOGAGE : Afficher le terme recherché
            _logger.LogInformation("🔍 Recherche clients - Société: {SocieteId}, Terme: '{SearchTerm}', TermeLower: '{Term}', IncludeInactive: {IncludeInactive}", 
                idSociete, searchTerm, term, includeInactive);

            var clients = await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && 
                           c.ClientsUsages != null &&
                           c.ClientsUsages.Any(cu => cu.Usage != null && 
                                                     cu.Usage.CategorieClient != null &&
                                                     cu.Usage.CategorieClient.IdSociete == idSociete) &&
                           // Filtre IsActif
                           (includeInactive || c.IsActif == true) &&
                           // Recherche multi-champs optimisée
                           (c.NomClient.ToLower().Contains(term) ||
                            (c.CodeCons != null && c.CodeCons.ToLower().Contains(term)) ||
                            (c.AdresseClient != null && c.AdresseClient.ToLower().Contains(term)) ||
                            (c.Telephone != null && c.Telephone.ToLower().Contains(term)) ||
                            (c.EmailClient != null && c.EmailClient.ToLower().Contains(term))))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();

            // LOG DE DÉBOGAGE : Afficher les résultats
            _logger.LogInformation("📊 Résultats recherche - Trouvé: {Count} clients", clients.Count);
            
            // LOG DE DÉBOGAGE : Afficher les CodeCons trouvés
            var codeConsList = clients.Where(c => !string.IsNullOrEmpty(c.CodeCons))
                                        .Select(c => new { c.IdClient, c.NomClient, c.CodeCons, c.IsActif })
                                        .ToList();
            
            _logger.LogInformation("📋 Clients trouvés: {ClientList}", 
                string.Join(", ", codeConsList.Select(cc => $"{cc.IdClient}:{cc.NomClient}({cc.CodeCons})[Actif:{cc.IsActif}]")));

            return clients;
        }

        public async Task<PagedResult<Client>> GetBySocietePagedAsync(int idSociete, ClientPagedSearchRequestDto request)
        {
            request ??= new ClientPagedSearchRequestDto();

            // 🔍 LOG DE DÉBOGAGE : Vérifier les paramètres reçus
            _logger.LogInformation("🔍 GetBySocietePagedAsync - SocieteId: {SocieteId}, IncludeInactive: {IncludeInactive}, IsActif: {IsActif}, IdTypeDeCourant: {IdTypeDeCourant}, SearchTerm: '{SearchTerm}', Page: {Page}, PageSize: {PageSize}", 
                idSociete, request.IncludeInactive, request.IsActif, request.IdTypeDeCourant, request.SearchTerm, request.PageNumber, request.PageSize);

            var query = _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && 
                           c.ClientsUsages != null &&
                           c.ClientsUsages.Any(cu => cu.Usage != null && 
                                                     cu.Usage.CategorieClient != null &&
                                                     cu.Usage.CategorieClient.IdSociete == idSociete));

            // 🆕 Logique de filtrage unifiée pour IsActif
            if (request.HasIsActifFilter)
            {
                // Priorité au filtre IsActif explicite
                query = query.Where(c => c.IsActif == request.ActifFilterValue);
                _logger.LogInformation("🔍 GetBySocietePagedAsync - Filtre IsActif explicite appliqué: {Value}", request.ActifFilterValue);
            }
            else if (request.IncludeInactive)
            {
                // Logique existante pour rétro-compatibilité
                _logger.LogInformation("🔍 GetBySocietePagedAsync - Filtre IncludeInactive appliqué (tous les statuts)");
            }
            else
            {
                // Défaut : actifs seulement
                query = query.Where(c => c.IsActif == true);
                _logger.LogInformation("🔍 GetBySocietePagedAsync - Filtre par défaut appliqué (actifs seulement)");
            }

            // Filtre : client ayant au moins une ligne ClientUsage active avec ce type de courant
            if (request.IdTypeDeCourant.HasValue)
            {
                var idType = request.IdTypeDeCourant.Value;
                query = query.Where(c => c.ClientsUsages != null &&
                    c.ClientsUsages.Any(cu => cu.Statut && cu.IdTypeDeCourant == idType));
                _logger.LogInformation("🔍 GetBySocietePagedAsync - Filtre IdTypeDeCourant appliqué: {Value}", request.IdTypeDeCourant.Value);
            }

            if (string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                // 🔍 LOG DE DÉBOGAGE : Pas de terme de recherche
                _logger.LogInformation("🔍 GetBySocietePagedAsync - Pas de terme de recherche, filtre IncludeInactive appliqué");
                // Ne pas appliquer de filtre de recherche supplémentaire
            }
            else
            {
                // 🔍 LOG DE DÉBOGAGE : Terme de recherche trouvé
                _logger.LogInformation("🔍 GetBySocietePagedAsync - Terme de recherche trouvé: '{Term}'", request.SearchTerm.Trim());
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.NomClient.ToLower().Contains(term) ||
                    (c.CodeCons != null && c.CodeCons.ToLower().Contains(term)) ||
                    (c.AdresseClient != null && c.AdresseClient.ToLower().Contains(term)) ||
                    (c.Telephone != null && c.Telephone.ToLower().Contains(term)) ||
                    (c.EmailClient != null && c.EmailClient.ToLower().Contains(term)));
            }

            query = request.SortBy switch
            {
                "NomClient" => request.SortDescending ? query.OrderByDescending(c => c.NomClient) : query.OrderBy(c => c.NomClient),
                "DateCreation" => request.SortDescending ? query.OrderByDescending(c => c.DateCreation) : query.OrderBy(c => c.DateCreation),
                _ => request.SortDescending ? query.OrderByDescending(c => c.IdClient) : query.OrderBy(c => c.IdClient)
            };

            var total = await query.CountAsync();

            // 🔍 LOG DE DÉBOGAGE : Résultats du filtre
            _logger.LogInformation("📊 GetBySocietePagedAsync - Total clients trouvés: {Total}", total);

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            // 🔍 LOG DE DÉBOGAGE : Statistiques détaillées
            var actifsCount = data.Count(c => c.IsActif == true);
            var inactifsCount = data.Count(c => c.IsActif == false);
            var typeDeCourantGroups = data
                .SelectMany(c => c.ClientsUsages ?? Enumerable.Empty<ClientUsage>())
                .Where(cu => cu.Statut && cu.IdTypeDeCourant.HasValue)
                .GroupBy(cu => cu.IdTypeDeCourant!.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            
            _logger.LogInformation("📈 GetBySocietePagedAsync - Actifs: {Actifs}, Inactifs: {Inactifs}, Page: {Page}, Total: {Total}", 
                actifsCount, inactifsCount, request.PageNumber, total);
            
            if (typeDeCourantGroups.Any())
            {
                var typesStats = string.Join(", ", typeDeCourantGroups.Select(kvp => $"Type{kvp.Key}({kvp.Value})"));
                _logger.LogInformation("📊 GetBySocietePagedAsync - Répartition par TypeDeCourant: {Stats}", typesStats);
            }

            return new PagedResult<Client>(data, total, request.PageNumber, request.PageSize);
        }

        public async Task<Client> GetByIdAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.TypeDeCourant)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true)
                .FirstOrDefaultAsync(c => c.IdClient == id);
        }

        public async Task<IEnumerable<Client>> GetByNomAsync(string nom)
        {
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && c.NomClient.Contains(nom))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Client>> GetByIsActifAsync(bool IsActif)
        {
            return await _context.Clients
                .Include(c => c.ClientsUsages)
                .ThenInclude(cu => cu.Usage)
                .ThenInclude(u => u.CategorieClient)
                .Include(c => c.Axe)
                .ThenInclude(a => a.Cabine)
                .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && c.IsActif == true)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }
        public async Task<Client?> GetByCodeConsAsync(string codeCons)
        {
            if (string.IsNullOrWhiteSpace(codeCons))
            {
                return null;
            }

            var trimmedCodeCons = codeCons.Trim();

            return await _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                            .ThenInclude(cc => cc.Societe)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && c.CodeCons != null && c.CodeCons == trimmedCodeCons)
                .FirstOrDefaultAsync();
        }

        public async Task<Client> CreateAsync(Client client)
        {
            client.DateCreation = DateTime.Now;
            if (!client.Statut)
            {
                client.Statut = true;
            }

            // ✨ Génération automatique du CodeCons si IdAxe est fourni et CodeCons est vide
            if (client.IdAxe.HasValue && string.IsNullOrWhiteSpace(client.CodeCons))
            {
                try
                {
                    client.CodeCons = await GenerateCodeConsAsync(client.IdAxe.Value);
                    _logger.LogInformation("✅ CodeCons généré automatiquement pour le client: {CodeCons}", client.CodeCons);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Impossible de générer le CodeCons automatiquement pour IdAxe {IdAxe}: {ErrorMessage}", 
                        client.IdAxe.Value, ex.Message);
                    // Ne pas faire échouer la création du client si la génération échoue
                }
            }

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Note: Les usages sont maintenant gérés via ClientUsage, pas via IdCategorieClient
            // Les usages doivent être ajoutés séparément après la création du client

            // ✨ NOUVEAU : Créer automatiquement un compte utilisateur pour le client
            try
            {
                _logger.LogInformation("🔍 Début de la création automatique du compte utilisateur pour le client {ClientId} (Email: {Email})", 
                    client.IdClient, client.EmailClient);
                
                var result = await CreateDefaultClientUserAsync(client);
                if (result == null)
                {
                    _logger.LogWarning("⚠️ CreateDefaultClientUserAsync a retourné null pour le client {ClientId}", client.IdClient);
                }
                else
                {
                    _logger.LogInformation("✅ Compte utilisateur créé/mis à jour pour le client {ClientId} (IdUtilisateur: {UserId})", 
                        client.IdClient, result.IdUtilisateur);
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire échouer la création du client
                _logger.LogError(ex, "❌ ERREUR lors de la création automatique du compte utilisateur pour le client {ClientId}: {ErrorMessage}", 
                    client.IdClient, ex.Message);
            }

            return client;
        }

        /// <summary>
        /// Crée un client avec ses usages dans une transaction atomique
        /// Utilise la stratégie d'exécution pour gérer les transactions de manière compatible avec MySqlRetryingExecutionStrategy
        /// </summary>
        /// <param name="client">Informations du client</param>
        /// <param name="usages">Liste des usages (libellé, nombre de bâtiments, type de courant optionnel par ligne)</param>
        /// <returns>Le client créé avec ses usages</returns>
        /// <exception cref="InvalidOperationException">Si un usage n'est pas trouvé ou si une erreur survient</exception>
        public async Task<Client> CreateWithUsagesAsync(Client client, List<(string LibelleUsage, int nombreBatiment, int? IdTypeDeCourant)> usages)
        {
            // Utiliser la stratégie d'exécution pour gérer les opérations de manière compatible
            // EF Core gère automatiquement les transactions pour SaveChanges()
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // 1. Préparer le client
                client.DateCreation = DateTime.Now;
                if (!client.Statut)
                {
                    client.Statut = true;
                }

                // ✨ Génération automatique du CodeCons si IdAxe est fourni et CodeCons est vide
                // Note: Cette opération fait des requêtes à la base, mais elle est en dehors de la transaction
                // car elle doit être exécutée avant d'ajouter le client au contexte
                if (client.IdAxe.HasValue && string.IsNullOrWhiteSpace(client.CodeCons))
                {
                    try
                    {
                        // Générer le CodeCons avant d'ajouter le client au contexte
                        client.CodeCons = await GenerateCodeConsAsync(client.IdAxe.Value);
                        _logger.LogInformation("✅ CodeCons généré automatiquement pour le client: {CodeCons}", client.CodeCons);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Impossible de générer le CodeCons automatiquement pour IdAxe {IdAxe}: {ErrorMessage}", 
                            client.IdAxe.Value, ex.Message);
                        // Ne pas faire échouer la création du client si la génération échoue
                    }
                }

                // 2. Valider et récupérer les usages AVANT d'ajouter le client
                var validatedUsages = new List<(int IdUsage, int nombreBatiment, int? IdTypeDeCourant)>();
                if (usages != null && usages.Count > 0)
                {
                    foreach (var usageInfo in usages)
                    {
                        if (string.IsNullOrWhiteSpace(usageInfo.LibelleUsage))
                        {
                            throw new InvalidOperationException($"Le LibelleUsage ne peut pas être vide.");
                        }

                        if (usageInfo.IdTypeDeCourant.HasValue)
                        {
                            var typeOk = await _context.TypeDeCourants
                                .AnyAsync(t => t.IdTypeDeCourant == usageInfo.IdTypeDeCourant.Value && t.Statut);
                            if (!typeOk)
                                throw new InvalidOperationException(
                                    $"Le type de courant avec l'ID {usageInfo.IdTypeDeCourant.Value} est introuvable ou inactif.");
                        }

                        // Récupérer l'IdUsage via le LibelleUsage
                        var usage = await _context.Usages
                            .FirstOrDefaultAsync(u => u.Libelle != null && u.Libelle.Trim() == usageInfo.LibelleUsage.Trim());

                        if (usage == null)
                        {
                            throw new InvalidOperationException(
                                $"L'usage avec le libellé '{usageInfo.LibelleUsage}' n'existe pas. " +
                                $"Veuillez vérifier le libellé ou créer l'usage d'abord.");
                        }

                        validatedUsages.Add((usage.IdUsage, usageInfo.nombreBatiment > 0 ? usageInfo.nombreBatiment : 1, usageInfo.IdTypeDeCourant));
                    }
                }

                // 3. Ajouter le client au contexte
                _context.Clients.Add(client);
                
                // 4. Sauvegarder pour obtenir l'IdClient (transaction automatique)
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Client créé avec IdClient: {IdClient}", client.IdClient);

                // 5. Créer les relations ClientUsage
                if (validatedUsages.Count > 0)
                {
                    foreach (var (idUsage, nombreBatiment, idTypeDeCourant) in validatedUsages)
                    {
                        // Vérifier que la relation n'existe pas déjà
                        var exists = await _context.ClientUsages
                            .AnyAsync(cu => cu.IdClient == client.IdClient && cu.IdUsage == idUsage);

                        if (exists)
                        {
                            throw new InvalidOperationException(
                                $"La relation entre le client {client.IdClient} et l'usage {idUsage} existe déjà.");
                        }

                        // Créer la relation ClientUsage
                        var clientUsage = new ClientUsage
                        {
                            IdClient = client.IdClient,
                            IdUsage = idUsage,
                            nombreBatiment = nombreBatiment,
                            DateAttribution = DateTime.Now,
                            Statut = true,
                            IdTypeDeCourant = idTypeDeCourant
                        };

                        _context.ClientUsages.Add(clientUsage);
                        _logger.LogInformation(
                            "✅ ClientUsage créé: Client {IdClient}, Usage {IdUsage}, nombreBatiment: {nombreBatiment}, IdTypeDeCourant: {IdType}",
                            client.IdClient, idUsage, nombreBatiment, idTypeDeCourant);
                    }

                    // Sauvegarder les ClientUsages (transaction automatique)
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ {Count} usage(s) associé(s) au client {IdClient}", validatedUsages.Count, client.IdClient);
                }

                // 6. ✨ NOUVEAU : Créer automatiquement un compte utilisateur pour le client
                try
                {
                    _logger.LogInformation("🔍 Début de la création automatique du compte utilisateur pour le client {ClientId} (Email: {Email})", 
                        client.IdClient, client.EmailClient);
                    
                    var result = await CreateDefaultClientUserAsync(client);
                    if (result == null)
                    {
                        _logger.LogWarning("⚠️ CreateDefaultClientUserAsync a retourné null pour le client {ClientId}", client.IdClient);
                    }
                    else
                    {
                        _logger.LogInformation("✅ Compte utilisateur créé/mis à jour pour le client {ClientId} (IdUtilisateur: {UserId})", 
                            client.IdClient, result.IdUtilisateur);
                    }
                }
                catch (Exception ex)
                {
                    // Log l'erreur mais ne pas faire échouer la création du client
                    _logger.LogError(ex, "❌ ERREUR lors de la création automatique du compte utilisateur pour le client {ClientId}: {ErrorMessage}", 
                        client.IdClient, ex.Message);
                }

                // 7. Recharger le client avec ses relations pour le retourner
                var createdClient = await _context.Clients
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.Usage)
                            .ThenInclude(u => u.CategorieClient)
                                .ThenInclude(cc => cc.Societe)
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.TypeDeCourant)
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                            .ThenInclude(cab => cab.Societe)
                    .FirstOrDefaultAsync(c => c.IdClient == client.IdClient) ?? client;

                _logger.LogInformation("✅ Client avec usages créé avec succès: {IdClient}", client.IdClient);
                return createdClient;
            });
        }

        public async Task<Client> UpdateAsync(Client client)
        {
            var existing = await _context.Clients.FindAsync(client.IdClient);
            if (existing == null)
                return null;

            // Sauvegarder les anciennes valeurs pour la synchronisation
            var oldNomClient = existing.NomClient;
            var oldTelephone = existing.Telephone;
            var oldEmailClient = existing.EmailClient;
            var oldGenreClient = existing.GenreClient;
            var oldAdresseClient = existing.AdresseClient;
            var wasInactive = !existing.IsActif;
            var previousDateReactivation = existing.DateDerniereReactivation;

            _context.Entry(existing).CurrentValues.SetValues(client);
            // Ne pas écraser la date de réactivation via SetValues (DTO/controllers ne la fournissent pas)
            existing.DateDerniereReactivation = previousDateReactivation;
            if (wasInactive && client.IsActif)
                existing.DateDerniereReactivation = DateTime.Now;

            await _context.SaveChangesAsync();

            // Note: Les usages sont maintenant gérés via ClientUsage, pas via IdCategorieClient
            // Les usages doivent être gérés séparément via les méthodes AddUsageToClientAsync/RemoveUsageFromClientAsync

            // ✨ SYNCHRONISATION: Mettre à jour les Utilisateurs liés si les champs pertinents ont changé
            var champsModifies = 
                oldNomClient != client.NomClient ||
                oldTelephone != client.Telephone ||
                oldEmailClient != client.EmailClient ||
                oldGenreClient != client.GenreClient ||
                oldAdresseClient != client.AdresseClient;

            if (champsModifies)
            {
                var utilisateursLies = await _context.Utilisateurs
                    .Where(u => u.IdClient == client.IdClient)
                    .ToListAsync();

                foreach (var utilisateur in utilisateursLies)
                {
                    // Synchroniser uniquement les champs qui ont changé
                    if (oldNomClient != client.NomClient && !string.IsNullOrWhiteSpace(client.NomClient))
                    {
                        utilisateur.NomComplet = client.NomClient;
                    }
                    if (oldTelephone != client.Telephone)
                    {
                        // Vérifier l'unicité du téléphone avant de synchroniser
                        if (!string.IsNullOrWhiteSpace(client.Telephone))
                        {
                            var telephoneDejaUtilise = await _context.Utilisateurs
                                .AnyAsync(u => u.Telephone == client.Telephone && u.IdUtilisateur != utilisateur.IdUtilisateur);
                            
                            if (!telephoneDejaUtilise)
                            {
                                utilisateur.Telephone = client.Telephone;
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "⚠️ Téléphone '{Telephone}' non synchronisé pour l'utilisateur {UserId} car déjà utilisé par un autre utilisateur",
                                    client.Telephone, utilisateur.IdUtilisateur);
                            }
                        }
                        else
                        {
                            // Si le téléphone devient null/vide, on peut le synchroniser
                            utilisateur.Telephone = client.Telephone;
                        }
                    }
                    if (oldEmailClient != client.EmailClient)
                    {
                        // Vérifier l'unicité de l'email avant de synchroniser
                        var emailDejaUtilise = await _context.Utilisateurs
                            .AnyAsync(u => u.Email == client.EmailClient && u.IdUtilisateur != utilisateur.IdUtilisateur);
                        
                        if (!emailDejaUtilise && !string.IsNullOrWhiteSpace(client.EmailClient))
                        {
                            utilisateur.Email = client.EmailClient;
                        }
                        else if (emailDejaUtilise)
                        {
                            _logger.LogWarning(
                                "⚠️ Email '{Email}' non synchronisé pour l'utilisateur {UserId} car déjà utilisé par un autre utilisateur",
                                client.EmailClient, utilisateur.IdUtilisateur);
                        }
                    }
                    if (oldGenreClient != client.GenreClient)
                    {
                        utilisateur.Genre = client.GenreClient;
                    }
                    if (oldAdresseClient != client.AdresseClient)
                    {
                        utilisateur.AdresseResidence = client.AdresseClient;
                    }
                }

                if (utilisateursLies.Any())
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation(
                        "✅ Synchronisation Client → Utilisateurs: {Count} utilisateur(s) mis à jour pour le client {ClientId}",
                        utilisateursLies.Count, client.IdClient);
                }
            }

            return existing;
        }

        /// <summary>
        /// Met à jour un client avec ses usages dans une transaction
        /// </summary>
        public async Task<Client> UpdateWithUsagesAsync(int idClient, Client client, List<(string LibelleUsage, int nombreBatiment, bool Statut, int? IdTypeDeCourant)>? usages)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // 1. Récupérer le client existant
                var existing = await _context.Clients
                    .Include(c => c.ClientsUsages)
                    .FirstOrDefaultAsync(c => c.IdClient == idClient);
                
                if (existing == null)
                    return null;

                // 2. Sauvegarder les anciennes valeurs pour la synchronisation
                var oldNomClient = existing.NomClient;
                var oldTelephone = existing.Telephone;
                var oldEmailClient = existing.EmailClient;
                var oldGenreClient = existing.GenreClient;
                var oldAdresseClient = existing.AdresseClient;
                var wasInactive = !existing.IsActif;

                // 3. Mettre à jour les champs du client (seulement ceux fournis)
                if (client.NomClient != null) existing.NomClient = client.NomClient;
                if (client.AdresseClient != null) existing.AdresseClient = client.AdresseClient;
                if (client.Telephone != null) existing.Telephone = client.Telephone;
                if (client.EmailClient != null) existing.EmailClient = client.EmailClient;
                if (client.GenreClient != null) existing.GenreClient = client.GenreClient;
                if (client.CodeCons != null) existing.CodeCons = client.CodeCons;
                if (client.IdAxe.HasValue) existing.IdAxe = client.IdAxe;
                // Statut et IsActif sont des bool, pas bool?, donc on les met à jour directement
                existing.Statut = client.Statut;
                existing.IsActif = client.IsActif;
                if (wasInactive && client.IsActif)
                    existing.DateDerniereReactivation = DateTime.Now;

                // 4. Mettre à jour les usages si fournis
                if (usages != null && usages.Count > 0)
                {
                    // Valider et récupérer les IdUsage
                    var validatedUsages = new List<(int IdUsage, int nombreBatiment, bool Statut, int? IdTypeDeCourant)>();
                    foreach (var usageInfo in usages)
                    {
                        if (string.IsNullOrWhiteSpace(usageInfo.LibelleUsage))
                        {
                            throw new InvalidOperationException($"Le LibelleUsage ne peut pas être vide.");
                        }

                        if (usageInfo.IdTypeDeCourant.HasValue)
                        {
                            var typeOk = await _context.TypeDeCourants
                                .AnyAsync(t => t.IdTypeDeCourant == usageInfo.IdTypeDeCourant.Value && t.Statut);
                            if (!typeOk)
                                throw new InvalidOperationException(
                                    $"Le type de courant avec l'ID {usageInfo.IdTypeDeCourant.Value} est introuvable ou inactif.");
                        }

                        var usage = await _context.Usages
                            .FirstOrDefaultAsync(u => u.Libelle != null && u.Libelle.Trim() == usageInfo.LibelleUsage.Trim());

                        if (usage == null)
                        {
                            throw new InvalidOperationException(
                                $"L'usage avec le libellé '{usageInfo.LibelleUsage}' n'existe pas.");
                        }

                        validatedUsages.Add((usage.IdUsage, usageInfo.nombreBatiment > 0 ? usageInfo.nombreBatiment : 1, usageInfo.Statut, usageInfo.IdTypeDeCourant));
                    }

                    // Récupérer les ClientUsage existants
                    var existingClientUsages = await _context.ClientUsages
                        .Where(cu => cu.IdClient == idClient)
                        .ToListAsync();

                    // Créer un dictionnaire pour faciliter la recherche
                    var existingByUsageId = existingClientUsages.ToDictionary(cu => cu.IdUsage);

                    // Traiter chaque usage fourni
                    foreach (var (idUsage, nombreBatiment, statut, idTypeDeCourant) in validatedUsages)
                    {
                        if (existingByUsageId.TryGetValue(idUsage, out var existingClientUsage))
                        {
                            existingClientUsage.nombreBatiment = nombreBatiment;
                            existingClientUsage.Statut = statut;
                            if (idTypeDeCourant.HasValue)
                                existingClientUsage.IdTypeDeCourant = idTypeDeCourant;
                            _logger.LogInformation(
                                "✅ ClientUsage mis à jour: Client {IdClient}, Usage {IdUsage}, nombreBatiment: {nombreBatiment}, Statut: {Statut}",
                                idClient, idUsage, nombreBatiment, statut);
                        }
                        else
                        {
                            var newClientUsage = new ClientUsage
                            {
                                IdClient = idClient,
                                IdUsage = idUsage,
                                nombreBatiment = nombreBatiment,
                                Statut = statut,
                                DateAttribution = DateTime.Now,
                                IdTypeDeCourant = idTypeDeCourant
                            };
                            _context.ClientUsages.Add(newClientUsage);
                            _logger.LogInformation(
                                "✅ Nouveau ClientUsage créé: Client {IdClient}, Usage {IdUsage}, nombreBatiment: {nombreBatiment}",
                                idClient, idUsage, nombreBatiment);
                        }
                    }

                    // Supprimer les ClientUsage qui ne sont plus dans la liste fournie
                    var providedUsageIds = validatedUsages.Select(u => u.IdUsage).ToHashSet();
                    var toRemove = existingClientUsages
                        .Where(cu => !providedUsageIds.Contains(cu.IdUsage))
                        .ToList();

                    foreach (var clientUsageToRemove in toRemove)
                    {
                        // Vérifier si des factures sont liées à cet usage
                        var hasFactures = await _context.Factures
                            .AnyAsync(f => f.IdUsage == clientUsageToRemove.IdUsage);

                        if (hasFactures)
                        {
                            // Soft delete : mettre Statut à false au lieu de supprimer
                            clientUsageToRemove.Statut = false;
                            _logger.LogWarning(
                                "⚠️ ClientUsage désactivé (soft delete) car des factures sont liées: Client {IdClient}, Usage {IdUsage}",
                                idClient, clientUsageToRemove.IdUsage);
                        }
                        else
                        {
                            // Hard delete : supprimer complètement
                            _context.ClientUsages.Remove(clientUsageToRemove);
                            _logger.LogInformation(
                                "✅ ClientUsage supprimé: Client {IdClient}, Usage {IdUsage}",
                                idClient, clientUsageToRemove.IdUsage);
                        }
                    }
                }

                // 5. Sauvegarder les modifications
                await _context.SaveChangesAsync();

                // 6. Synchroniser avec les Utilisateurs liés si les champs pertinents ont changé
                var champsModifies = 
                    oldNomClient != existing.NomClient ||
                    oldTelephone != existing.Telephone ||
                    oldEmailClient != existing.EmailClient ||
                    oldGenreClient != existing.GenreClient ||
                    oldAdresseClient != existing.AdresseClient;

                if (champsModifies)
                {
                    var utilisateursLies = await _context.Utilisateurs
                        .Where(u => u.IdClient == idClient)
                        .ToListAsync();

                    foreach (var utilisateur in utilisateursLies)
                    {
                        if (oldNomClient != existing.NomClient && !string.IsNullOrWhiteSpace(existing.NomClient))
                            utilisateur.NomComplet = existing.NomClient;
                        if (oldTelephone != existing.Telephone)
                        {
                            if (!string.IsNullOrWhiteSpace(existing.Telephone))
                            {
                                var telephoneDejaUtilise = await _context.Utilisateurs
                                    .AnyAsync(u => u.Telephone == existing.Telephone && u.IdUtilisateur != utilisateur.IdUtilisateur);
                                
                                if (!telephoneDejaUtilise)
                                    utilisateur.Telephone = existing.Telephone;
                            }
                            else
                                utilisateur.Telephone = existing.Telephone;
                        }
                        if (oldEmailClient != existing.EmailClient)
                        {
                            var emailDejaUtilise = await _context.Utilisateurs
                                .AnyAsync(u => u.Email == existing.EmailClient && u.IdUtilisateur != utilisateur.IdUtilisateur);
                            
                            if (!emailDejaUtilise && !string.IsNullOrWhiteSpace(existing.EmailClient))
                                utilisateur.Email = existing.EmailClient;
                        }
                        if (oldGenreClient != existing.GenreClient)
                            utilisateur.Genre = existing.GenreClient;
                        if (oldAdresseClient != existing.AdresseClient)
                            utilisateur.AdresseResidence = existing.AdresseClient;
                    }

                    if (utilisateursLies.Any())
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation(
                            "✅ Synchronisation Client → Utilisateurs: {Count} utilisateur(s) mis à jour pour le client {ClientId}",
                            utilisateursLies.Count, idClient);
                    }
                }

                // 7. Recharger le client avec ses relations
                var updatedClient = await _context.Clients
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.Usage)
                            .ThenInclude(u => u.CategorieClient)
                                .ThenInclude(cc => cc.Societe)
                    .Include(c => c.ClientsUsages)
                        .ThenInclude(cu => cu.TypeDeCourant)
                    .Include(c => c.Axe)
                        .ThenInclude(a => a.Cabine)
                            .ThenInclude(cab => cab.Societe)
                    .FirstOrDefaultAsync(c => c.IdClient == idClient);

                _logger.LogInformation("✅ Client avec usages mis à jour avec succès: {IdClient}", idClient);
                return updatedClient ?? existing;
            });
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            // ✨ NOUVEAU : Soft delete au lieu de hard delete
            client.Statut = false;
            client.IsActif = false;
            client.IsDeleted = true; // ✅ Ajout du soft delete pour sync
            client.UpdatedAt = DateTime.UtcNow; // ✅ Ajout de UpdatedAt pour delta sync
            await _context.SaveChangesAsync();

            // ✨ NOUVEAU : Soft delete des ClientUsage associés
            var clientUsages = await _context.ClientUsages
                .Where(cu => cu.IdClient == id)
                .ToListAsync();

            foreach (var clientUsage in clientUsages)
            {
                clientUsage.Statut = false;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Client {IdClient} et ses {Count} ClientUsage(s) désactivés (soft delete)", id, clientUsages.Count);
            
            return true;
        }
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Clients.AnyAsync(c => c.IdClient == id);
        }
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            client.Statut = !client.Statut;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleIsActifAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            var wasInactive = !client.IsActif;
            client.IsActif = !client.IsActif;
            if (wasInactive && client.IsActif)
                client.DateDerniereReactivation = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null)
                return false;

            client.Statut = statut;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<Client>> GetPagedAsync(PagedRequest request)
        {
            request ??= new PagedRequest();

            var query = _context.Clients
                .Include(c => c.ClientsUsages)
                    .ThenInclude(cu => cu.Usage)
                        .ThenInclude(u => u.CategorieClient)
                .Include(c => c.Axe)
                    .ThenInclude(a => a.Cabine)
                        .ThenInclude(cab => cab.Societe)
                .Where(c => c.Statut == true && (!c.IsDeleted.HasValue || !c.IsDeleted.Value)); // ✅ Filtre soft delete pour sync

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(c =>
                    c.NomClient.ToLower().Contains(term) ||
                    (c.AdresseClient ?? string.Empty).ToLower().Contains(term) ||
                    (c.Telephone ?? string.Empty).ToLower().Contains(term));
            }

            query = request.SortBy switch
            {
                "NomClient" => request.SortDescending ? query.OrderByDescending(c => c.NomClient) : query.OrderBy(c => c.NomClient),
                "DateCreation" => request.SortDescending ? query.OrderByDescending(c => c.DateCreation) : query.OrderBy(c => c.DateCreation),
                _ => request.SortDescending ? query.OrderByDescending(c => c.IdClient) : query.OrderBy(c => c.IdClient)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Client>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// Génère automatiquement un CodeCons au format {codeCabine}/{codeAxe}/{numéro séquentiel}
        /// </summary>
        /// <param name="idAxe">Identifiant de l'axe du client</param>
        /// <returns>CodeCons généré au format {codeCabine}/{codeAxe}/{0001-9999}</returns>
        /// <exception cref="InvalidOperationException">Si l'axe, la cabine ou les codes sont manquants</exception>
        private async Task<string> GenerateCodeConsAsync(int idAxe)
        {
            // Récupérer l'axe avec sa cabine
            var axe = await _context.Axes
                .Include(a => a.Cabine)
                .FirstOrDefaultAsync(a => a.IdAxe == idAxe);

            if (axe == null)
            {
                throw new InvalidOperationException($"Axe avec IdAxe {idAxe} introuvable.");
            }

            if (axe.Cabine == null)
            {
                throw new InvalidOperationException($"Cabine introuvable pour l'axe {idAxe}.");
            }

            // Vérifier que les codes existent
            if (string.IsNullOrWhiteSpace(axe.Cabine.CodeCabine))
            {
                throw new InvalidOperationException(
                    $"Le codeCabine n'est pas défini pour la cabine {axe.Cabine.IdCabine} (Nom: {axe.Cabine.Nom}). " +
                    $"Veuillez définir le CodeCabine avant de créer des clients pour cet axe.");
            }

            if (string.IsNullOrWhiteSpace(axe.CodeAxe))
            {
                throw new InvalidOperationException(
                    $"Le codeAxe n'est pas défini pour l'axe {idAxe} (Nom: {axe.NomAxe}). " +
                    $"Veuillez définir le CodeAxe avant de créer des clients pour cet axe.");
            }

            string codeCabine = axe.Cabine.CodeCabine.Trim();
            string codeAxe = axe.CodeAxe.Trim();

            // Trouver le dernier numéro séquentiel pour cette combinaison codeCabine/codeAxe
            // Format attendu: {codeCabine}/{codeAxe}/{0001-9999}
            string prefix = $"{codeCabine}/{codeAxe}/";
            
            var existingClients = await _context.Clients
                .Where(c => c.CodeCons != null && c.CodeCons.StartsWith(prefix))
                .Select(c => c.CodeCons)
                .ToListAsync();

            int nextNumber = 1;

            if (existingClients.Any())
            {
                // Extraire les numéros existants et trouver le maximum
                var numbers = existingClients
                    .Select(code => 
                    {
                        // Extraire la partie numérique après le dernier "/"
                        var parts = code.Split('/');
                        if (parts.Length >= 3 && int.TryParse(parts[2], out int num))
                        {
                            return num;
                        }
                        return 0;
                    })
                    .Where(n => n > 0)
                    .ToList();

                if (numbers.Any())
                {
                    int maxNumber = numbers.Max();
                    nextNumber = maxNumber + 1;
                }
            }

            // Vérifier que nous n'avons pas dépassé la limite de 9999
            if (nextNumber > 9999)
            {
                throw new InvalidOperationException(
                    $"Limite de 9999 clients atteinte pour la combinaison {codeCabine}/{codeAxe}. " +
                    $"Impossible de générer un nouveau CodeCons.");
            }

            // Générer le CodeCons au format {codeCabine}/{codeAxe}/{0001-9999}
            string codeCons = $"{codeCabine}/{codeAxe}/{nextNumber:D4}";

            _logger.LogInformation(
                "✅ CodeCons généré: {CodeCons} pour IdAxe {IdAxe} (Cabine: {CabineNom}, Axe: {AxeNom})",
                codeCons, idAxe, axe.Cabine.Nom, axe.NomAxe);

            return codeCons;
        }

        /// <summary>
        /// Crée automatiquement un utilisateur Client par défaut lors de la création d'un nouveau client
        /// ✨ RBAC: Attribution automatique du rôle "Client"
        /// </summary>
        private async Task<UtilisateurInfo?> CreateDefaultClientUserAsync(Client client)
        {
            try
            {
                _logger.LogInformation("🔍 CreateDefaultClientUserAsync appelé pour client {ClientId} (Email: {Email}, Nom: {Nom})", 
                    client.IdClient, client.EmailClient, client.NomClient);
                
                // Récupérer le rôle Client
                var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Client");
                if (clientRole == null)
                {
                    _logger.LogError("❌ Rôle 'Client' non trouvé. Les rôles n'ont peut-être pas été initialisés.");
                    throw new InvalidOperationException(
                        $"Le rôle 'Client' n'existe pas. " +
                        $"Assurez-vous que les rôles ont été initialisés via PermissionSeeder."
                    );
                }

                _logger.LogInformation("✅ Rôle Client trouvé: {Role} (ID: {RoleId})", clientRole.Nom, clientRole.IdRole);

                // Récupérer la société par défaut (ou la première disponible)
                var societe = await _context.Societes.FirstOrDefaultAsync();
                if (societe == null)
                {
                    _logger.LogError("❌ Aucune société trouvée. Impossible de créer un utilisateur client.");
                    return null;
                }

                _logger.LogInformation("✅ Société trouvée: {SocieteNom} (ID: {SocieteId})", societe.Nom, societe.IdSociete);

                // ✨ Utiliser l'email du client s'il est fourni, sinon générer un email unique
                // Évite les erreurs de contrainte unique sur email vide
                string email;
                if (string.IsNullOrWhiteSpace(client.EmailClient))
                {
                    // Générer un email unique basé sur CodeCons ou IdClient
                    var codeCons = client.CodeCons?.Replace("/", "_").Replace("\\", "_") ?? "";
                    if (!string.IsNullOrWhiteSpace(codeCons))
                    {
                        email = $"client_{codeCons}@kenergie.local";
                    }
                    else
                    {
                        // Si CodeCons n'est pas encore disponible, utiliser IdClient temporaire
                        // L'email sera mis à jour plus tard si nécessaire
                        email = $"client_temp_{client.IdClient}_{Guid.NewGuid():N}@kenergie.local";
                    }
                    _logger.LogInformation("✅ Email généré automatiquement pour le client {ClientId}: {Email}", client.IdClient, email);
                }
                else
                {
                    email = client.EmailClient.Trim();
                }
                
                string telephone = client.Telephone ?? "";
                
                // ═══════════════════════════════════════════════════════════════════
                // ✅ MULTI-RÔLES : Vérifier si un utilisateur existe déjà par email/téléphone
                // ═══════════════════════════════════════════════════════════════════
                
                Utilisateur? existingUser = null;
                
                // 1. Vérifier si un utilisateur existe déjà pour ce client (par IdClient)
                existingUser = await _context.Utilisateurs
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IdClient == client.IdClient);
                
                // 2. Si pas trouvé, chercher par email ou téléphone (pour le multi-rôles)
                if (existingUser == null && (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(telephone)))
                {
                    existingUser = await _context.Utilisateurs
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => 
                            (!string.IsNullOrWhiteSpace(email) && u.Email == email) ||
                            (!string.IsNullOrWhiteSpace(telephone) && u.Telephone == telephone)
                        );
                }
                
                // 3. Si utilisateur existe, ajouter le rôle Client (multi-rôles)
                if (existingUser != null)
                {
                    _logger.LogInformation("✅ Utilisateur existant trouvé pour le client '{NomClient}' (ID: {UserId}, Email: {Email})", 
                        client.NomClient, existingUser.IdUtilisateur, existingUser.Email);
                    
                    // Recharger les UserRoles
                    await _context.Entry(existingUser)
                        .Collection(u => u.UserRoles)
                        .Query()
                        .Include(ur => ur.Role)
                        .LoadAsync();
                    
                    // Vérifier si l'utilisateur a déjà le rôle Client
                    var hasClientRole = existingUser.UserRoles
                        .Any(ur => ur.Role.Nom == "Client" && ur.Statut == true);
                    
                    if (!hasClientRole)
                    {
                        // Ajouter le rôle Client
                        var newUserRole = new UserRole
                        {
                            IdUtilisateur = existingUser.IdUtilisateur,
                            IdRole = clientRole.IdRole,
                            IsPrimary = false, // Ne pas remplacer le rôle principal existant
                            Statut = true,
                            DateAttribution = DateTime.Now
                        };
                        
                        _context.UserRoles.Add(newUserRole);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("✅ Rôle 'Client' ajouté avec succès à l'utilisateur {UserId}", 
                            existingUser.IdUtilisateur);
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ L'utilisateur {UserId} a déjà le rôle 'Client'", 
                            existingUser.IdUtilisateur);
                    }
                    
                    // Mettre à jour IdClient si nécessaire
                    if (existingUser.IdClient != client.IdClient)
                    {
                        existingUser.IdClient = client.IdClient;
                        await _context.SaveChangesAsync();
                    }
                    
                    // Retourner les infos de l'utilisateur existant
                    var primaryRole = existingUser.UserRoles
                        .Where(ur => ur.Statut == true && ur.IsPrimary)
                        .Select(ur => ur.Role.Nom)
                        .FirstOrDefault()
                        ?? existingUser.UserRoles
                            .Where(ur => ur.Statut == true)
                            .OrderBy(ur => ur.Role.Niveau ?? 999)
                            .Select(ur => ur.Role.Nom)
                            .FirstOrDefault()
                        ?? "Client";
                    
                    return new UtilisateurInfo
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdAgent = existingUser.IdAgent,
                        Email = existingUser.Email ?? email,
                        DefaultUsername = existingUser.DefaultUsername ?? "",
                        Telephone = existingUser.Telephone ?? telephone,
                        MotDePasseParDefaut = "", // Ne jamais révéler le mot de passe d'un compte existant
                        NomComplet = existingUser.NomComplet ?? client.NomClient,
                        Role = primaryRole
                    };
                }
                
                // Construire le nom complet
                string nomComplet = client.NomClient;
                if (string.IsNullOrWhiteSpace(nomComplet))
                {
                    nomComplet = "Client";
                    _logger.LogWarning("⚠️ Le nom du client est NULL, utilisation de la valeur par défaut 'Client'");
                }
                
                // ✨ Utiliser CodeCons comme DefaultUsername (seul champ unique)
                // Recharger le client pour s'assurer d'avoir le CodeCons généré
                var clientWithCodeCons = await _context.Clients
                    .FirstOrDefaultAsync(c => c.IdClient == client.IdClient);
                
                string defaultUsername;
                if (!string.IsNullOrWhiteSpace(clientWithCodeCons?.CodeCons))
                {
                    // Utiliser CodeCons comme DefaultUsername
                    defaultUsername = clientWithCodeCons.CodeCons.Trim();
                    _logger.LogInformation("✅ Utilisation du CodeCons comme DefaultUsername: {CodeCons}", defaultUsername);
                }
                else
                {
                    // Fallback si CodeCons n'est pas disponible (ne devrait pas arriver normalement)
                    _logger.LogWarning("⚠️ CodeCons non disponible pour le client {ClientId}, utilisation d'un username par défaut", client.IdClient);
                    string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
                    if (string.IsNullOrWhiteSpace(baseUsername))
                    {
                        baseUsername = "Client";
                    }
                    if (baseUsername.Length > 20)
                    {
                        baseUsername = baseUsername.Substring(0, 20);
                    }
                    Random random = new Random();
                    int randomNumber = random.Next(1, 1000);
                    defaultUsername = $"{baseUsername}{randomNumber}";
                }
                
                // Mot de passe par défaut
                string motDePasseParDefaut = "123456";
                
                // Créer l'utilisateur Client
                var clientUser = new Utilisateur
                {
                    IdClient = client.IdClient,
                    ReferenceUtilisateur = Guid.NewGuid(),
                    NomComplet = nomComplet,
                    Email = email,
                    DefaultUsername = defaultUsername,
                    Telephone = telephone,
                    Genre = client.GenreClient,
                    AdresseResidence = client.AdresseClient,
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut),
                    Statut = true,
                    DateCreation = DateTime.Now,
                    IsConnecte = false,
                    DoitChangerMotDePasse = true,
                    IdSociete = societe.IdSociete
                };

                _logger.LogInformation("🔍 Création de l'utilisateur avec les valeurs: NomComplet={NomComplet}, Email={Email}, IdSociete={SocieteId}, IdClient={ClientId}", 
                    clientUser.NomComplet, clientUser.Email, clientUser.IdSociete, clientUser.IdClient);

                // ✨ Vérifier l'unicité de l'email avant insertion
                // Si l'email existe déjà, générer un email unique avec suffixe
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var emailExists = await _context.Utilisateurs
                        .AnyAsync(u => u.Email == email && u.Statut == true);
                    
                    if (emailExists)
                    {
                        // Générer un email unique en ajoutant un suffixe
                        var baseEmail = email;
                        int suffix = 1;
                        string uniqueEmail;
                        
                        do
                        {
                            // Extraire le nom d'utilisateur et le domaine
                            var atIndex = baseEmail.LastIndexOf('@');
                            if (atIndex > 0)
                            {
                                var username = baseEmail.Substring(0, atIndex);
                                var domain = baseEmail.Substring(atIndex);
                                uniqueEmail = $"{username}_{suffix}{domain}";
                            }
                            else
                            {
                                uniqueEmail = $"{baseEmail}_{suffix}";
                            }
                            
                            var exists = await _context.Utilisateurs
                                .AnyAsync(u => u.Email == uniqueEmail && u.Statut == true);
                            
                            if (!exists)
                                break;
                            
                            suffix++;
                        } while (suffix < 10000); // Limite de sécurité
                        
                        email = uniqueEmail;
                        clientUser.Email = email;
                        _logger.LogInformation("⚠️ Email en conflit, utilisation de l'email unique: {Email}", email);
                    }
                }

                // Validation avant ajout
                try
                {
                    _context.Utilisateurs.Add(clientUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Utilisateur sauvegardé avec succès. IdUtilisateur={UserId}", clientUser.IdUtilisateur);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "❌ ERREUR lors de la sauvegarde de l'utilisateur: {ErrorMessage}", saveEx.Message);
                    throw;
                }
                
                // Créer le UserRole pour le système multi-rôles
                UserRole userRole;
                try
                {
                    userRole = new UserRole
                    {
                        IdUtilisateur = clientUser.IdUtilisateur,
                        IdRole = clientRole.IdRole,
                        IsPrimary = true, // Premier rôle = principal
                        Statut = true,
                        DateAttribution = DateTime.Now
                    };
                    
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ UserRole sauvegardé avec succès. IdUserRole={UserRoleId}", userRole.IdUserRole);
                }
                catch (Exception roleEx)
                {
                    _logger.LogError(roleEx, "❌ ERREUR lors de la sauvegarde du UserRole: {ErrorMessage}", roleEx.Message);
                    throw;
                }
                
                _logger.LogInformation("✅ Utilisateur Client créé avec UserRole (ID: {UserId}, Role: {RoleName})", 
                    clientUser.IdUtilisateur, clientRole.Nom);
                
                // Envoyer l'email de bienvenue (si email fourni)
                if (!string.IsNullOrWhiteSpace(email))
                {
                    string nomSociete = societe.Nom ?? "Kenergie";
                    
                    // Envoi asynchrone (ne bloque pas si échec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendWelcomeEmailAsync(
                                email,
                                nomComplet,
                                defaultUsername,
                                telephone,
                                motDePasseParDefaut,
                                clientRole.Nom,
                                nomSociete,
                                client.GenreClient ?? "Masculin"
                            );
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "⚠️ Échec de l'envoi de l'email à {Email}: {ErrorMessage}", 
                                email, emailEx.Message);
                        }
                    });
                }
                
                // Envoyer le SMS de bienvenue (si téléphone fourni)
                if (!string.IsNullOrWhiteSpace(telephone))
                {
                    string nomSociete = societe.Nom ?? "K-Energie";
                    
                    // Créer le message SMS de bienvenue
                    string messageSms = CreateWelcomeSmsMessage(
                        nomComplet,
                        defaultUsername,
                        motDePasseParDefaut,
                        nomSociete
                    );
                    
                    // Envoi asynchrone (ne bloque pas si échec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var smsLog = await _smsService.EnvoyerSmsAsync(
                                telephone,
                                messageSms,
                                "BIENVENUE_CLIENT"
                            );
                            
                            if (smsLog != null && (smsLog.Statut == "SENT" || smsLog.Statut == "DELIVERED"))
                            {
                                _logger.LogInformation("✅ SMS de bienvenue envoyé avec succès à {Telephone}", telephone);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Échec de l'envoi du SMS à {Telephone}: {Statut}", 
                                    telephone, smsLog?.Statut ?? "UNKNOWN");
                            }
                        }
                        catch (Exception smsEx)
                        {
                            _logger.LogWarning(smsEx, "⚠️ Échec de l'envoi du SMS à {Telephone}: {ErrorMessage}", 
                                telephone, smsEx.Message);
                        }
                    });
                }
                
                return new UtilisateurInfo
                {
                    IdUtilisateur = clientUser.IdUtilisateur,
                    IdAgent = null,
                    Email = email,
                    DefaultUsername = defaultUsername,
                    Telephone = telephone,
                    MotDePasseParDefaut = motDePasseParDefaut,
                    NomComplet = nomComplet,
                    Role = clientRole.Nom
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERREUR lors de la création de l'utilisateur client: {ErrorMessage}", ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Crée le message SMS de bienvenue pour un nouveau client
        /// Format: {nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {url}. Vos identifiants ont été envoyés sur votre mail.
        /// Système adaptatif si le message dépasse 160 caractères
        /// </summary>
        private string CreateWelcomeSmsMessage(
            string nomComplet,
            string defaultUsername,
            string motDePasseParDefaut,
            string nomSociete)
        {
            // Format demandé: {nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {url}. Vos identifiants ont été envoyés sur votre mail.
            var message = $"{nomSociete}: Bienvenue ! Votre compte a été créé. Connectez-vous sur {_baseUrl}. Vos identifiants ont été envoyés sur votre mail.";
            
            // Si le message dépasse 160 caractères (nom de société trop long), utiliser une version plus courte
            if (message.Length > 160)
            {
                // Version courte sans "Connectez-vous sur"
                message = $"{nomSociete}: Bienvenue ! Votre compte a été créé. {_baseUrl}. Vos identifiants ont été envoyés sur votre mail.";
                
                // Si toujours trop long, version ultra-courte
                if (message.Length > 160)
                {
                    message = $"{nomSociete}: Bienvenue ! Compte créé. Identifiants: email envoyé. {_baseUrl}";
                }
            }
            
            return message;
        }

        /// <summary>
        /// Ajoute un usage à un client avec un nombre de bâtiments (relation many-to-many)
        /// </summary>
        public async Task<bool> AddUsageToClientAsync(int idClient, int idUsage, int nombreBatiment = 1, int? idTypeDeCourant = null)
        {
            try
            {
                // Vérifier que le client existe
                var client = await _context.Clients.FindAsync(idClient);
                if (client == null)
                    return false;

                // Vérifier que l'usage existe
                var usage = await _context.Usages.FindAsync(idUsage);
                if (usage == null)
                    return false;

                if (idTypeDeCourant.HasValue)
                {
                    var typeOk = await _context.TypeDeCourants
                        .AnyAsync(t => t.IdTypeDeCourant == idTypeDeCourant.Value && t.Statut);
                    if (!typeOk)
                        return false;
                }

                // Vérifier si la relation existe déjà
                var exists = await _context.ClientUsages
                    .AnyAsync(cu => cu.IdClient == idClient && cu.IdUsage == idUsage);

                if (exists)
                    return true; // Déjà assigné

                // Créer la relation
                var clientUsage = new ClientUsage
                {
                    IdClient = idClient,
                    IdUsage = idUsage,
                    nombreBatiment = nombreBatiment,
                    DateAttribution = DateTime.Now,
                    IdTypeDeCourant = idTypeDeCourant
                };

                _context.ClientUsages.Add(clientUsage);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de l'ajout de l'usage {IdUsage} au client {IdClient}", idUsage, idClient);
                return false;
            }
        }

        /// <summary>
        /// Retire un usage d'un client (relation many-to-many)
        /// </summary>
        public async Task<bool> RemoveUsageFromClientAsync(int idClient, int idUsage)
        {
            try
            {
                var clientUsage = await _context.ClientUsages
                    .FirstOrDefaultAsync(cu => cu.IdClient == idClient && cu.IdUsage == idUsage);

                if (clientUsage == null)
                    return false; // La relation n'existe pas

                _context.ClientUsages.Remove(clientUsage);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la suppression de l'usage {IdUsage} du client {IdClient}", idUsage, idClient);
                return false;
            }
        }

        /// <summary>
        /// Récupère tous les usages d'un client (via la relation many-to-many ClientUsage)
        /// </summary>
        public async Task<IEnumerable<Usage>> GetClientUsagesAsync(int idClient)
        {
            var usages = await _context.ClientUsages
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                        .ThenInclude(cc => cc.Societe)
                .Where(cu => cu.IdClient == idClient)
                .Select(cu => cu.Usage)
                .Where(u => u != null)
                .ToListAsync();

            return usages;
        }

        /// <summary>
        /// Récupère les relations ClientUsage d'un client (avec nombreBatiment)
        /// </summary>
        public async Task<IEnumerable<ClientUsage>> GetClientUsagesWithDetailsAsync(int idClient)
        {
            return await _context.ClientUsages
                .Include(cu => cu.Usage)
                    .ThenInclude(u => u.CategorieClient)
                .Include(cu => cu.TypeDeCourant)
                .Where(cu => cu.IdClient == idClient)
                .ToListAsync();
        }
    }
}

