using Kenergie.Data;
using Kenergie.Models;
using Kenergie.Middleware;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Kenergie.Services.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using KenergieAPI.Services.Repositories;
using KenergieAPI.Services;
using Serilog;
using AspNetCoreRateLimit;
using System.Reflection;
using Amazon.S3;
using Amazon;

// ═══════════════════════════════════════════════════════════════════════════════════
// Assembly pour JWT (compatibilité entre JwtBearer 6.0.25 et JWT 8.3.1)
// ═══════════════════════════════════════════════════════════════════════════════════
AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    string assemblyName = new AssemblyName(args.Name).Name;
    if (assemblyName == "System.IdentityModel.Tokens.Jwt")
    {
        // Charger la version 8.3.1 au lieu de 6.10.0.0
        var assembly = Assembly.LoadFrom(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System.IdentityModel.Tokens.Jwt.dll")
        );
        return assembly;
    }
    return null;
};

// ═══════════════════════════════════════════════════════════════════════════════════
//  CONFIGURATION SERILOG (Étape 1 : Charger la configuration avant CreateBuilder)
// ═══════════════════════════════════════════════════════════════════════════════════

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information(" Démarrage de KenergieAPI...");

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Désactiver la validation des scopes au démarrage pour éviter les erreurs StopTheHostException
    // Cette validation peut masquer l'exception réelle
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false; // Désactiver la validation pour le démarrage
        options.ValidateOnBuild = false; // Désactiver la validation à la construction
    });

    //  Configurer Serilog à partir d'appsettings.json
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentName());

    Log.Information(" Serilog configuré avec succès");

// Configuration pour écouter sur toutes les interfaces réseau
// builder.WebHost.UseUrls("https://0.0.0.0:7110");

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // 🎯 ANTI-RÉFÉRENCE CIRCULAIRE: Ignorer les références circulaires
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // 🎯 ANTI-RÉFÉRENCE CIRCULAIRE: Écrire les enums comme strings
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ═══════════════════════════════════════════════════════════════════════════════════
//  PERFORMANCE OPTIMIZATIONS
// ═══════════════════════════════════════════════════════════════════════════════════

//  1. Response Compression (Gzip/Brotli) - Réduit la taille des réponses de 70-90%
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

//  2. In-Memory Cache - Accélère les données statiques/semi-statiques
// Note : MemoryCache est configuré plus bas pour le Rate Limiting (sans SizeLimit)

Log.Information(" Performance optimizations configurées (Compression + Cache)");

// ═══════════════════════════════════════════════════════════════════════════════════
// RATE LIMITING - Protection contre abus et attaques brute-force
// ═══════════════════════════════════════════════════════════════════════════════════

// 1. Configuration du stockage en mémoire pour Rate Limiting
builder.Services.AddMemoryCache();

// 2. Configuration du Rate Limiting par IP
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));

// 3. Configuration des politiques de Rate Limiting
builder.Services.Configure<IpRateLimitPolicies>(options =>
{
    options.IpRules = new List<IpRateLimitPolicy>();
});

// 4. Enregistrement des services requis
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

Log.Information(" Rate Limiting configuré (AspNetCoreRateLimit)");

// Configuration JWT avec authentification Bearer
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false; // Pour le développement
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? "Kenergie-SecretKey-2025-V1-Ultra-Secure-Key-For-JWT-Token-Generation")
            ),
            ValidateIssuer = false, // Pas de validation d'issuer pour simplifier
            ValidateAudience = false, // Pas de validation d'audience pour simplifier
            ValidateLifetime = true, // Valider l'expiration du token
            ClockSkew = TimeSpan.Zero // Pas de tolérance sur l'expiration
        };
        // SignalR WebSockets : le JWT est passé en query ?access_token= (clients Flutter / JS)
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "KenergieAPI",
        Version = "v2",
        Description = "Kenergie - API sécurisée avec JWT"
    });
    
    // ✅ Configuration pour éviter les conflits de schemaId
    // Utilise le nom complet du type (avec namespace) pour éviter les collisions
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
    
    // Configuration de l'authentification JWT dans Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez le token JWT (avec ou sans préfixe Bearer) : {votre_token}"
    });
    
    // Configuration alternative pour accepter les tokens sans "Bearer"
    c.AddSecurityDefinition("TokenOnly", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez uniquement le token JWT (sans Bearer) : {votre_token}"
    });
    
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<KenergieDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("KenergieConnection"),
        new MariaDbServerVersion(new Version(10, 11, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)
        )
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Enregistrement du service JWT
builder.Services.AddScoped<ISimpleJwtService, SimpleJwtService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>(); //  REFRESH TOKEN : Service de gestion des refresh tokens

// AUDIT TRAIL: Service d'audit pour tracer toutes les modifications
builder.Services.AddScoped<IAuditService, AuditService>();

// CACHE SERVICE: Service de cache in-memory pour données statiques
builder.Services.AddScoped<ICacheService, CacheService>();

// Enregistrement des repositories
builder.Services.AddScoped<ISocieteRepository, SocieteService>();
builder.Services.AddScoped<IUtilisateurRepository, UtilisateurService>();
builder.Services.AddScoped<IAgentRepository, AgentService>();
builder.Services.AddScoped<IRoleRepository, RoleService>();
builder.Services.AddScoped<ICategorieClientRepository, CategorieClientService>();
builder.Services.AddScoped<IUsageRepository, UsageService>();
builder.Services.AddScoped<ITypeDeCourantRepository, TypeDeCourantService>();
builder.Services.AddScoped<IClientUsageRepository, ClientUsageService>();
builder.Services.AddScoped<ICabineRepository, CabineService>();
builder.Services.AddScoped<IAxeRepository, AxeService>();
builder.Services.AddScoped<IClientRepository, ClientService>();
builder.Services.AddScoped<IFactureRepository, FactureService>();
builder.Services.AddScoped<IPaiementRepository, PaiementService>();
builder.Services.AddScoped<IClientFactureRepository, ClientFactureService>();
builder.Services.AddScoped<IDeviseConversionService, DeviseConversionService>();
builder.Services.AddScoped<IRapportFinancierUsdEnrichmentService, RapportFinancierUsdEnrichmentService>();
builder.Services.AddScoped<IDeviseRepository, DeviseService>();
builder.Services.Configure<Kenergie.Models.Configuration.FlexPayOptions>(
    builder.Configuration.GetSection(Kenergie.Models.Configuration.FlexPayOptions.SectionName));
builder.Services.AddHttpClient("FlexPay", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<Kenergie.Services.FlexPay.IFlexPayHttpService, Kenergie.Services.FlexPay.FlexPayHttpService>();
builder.Services.AddScoped<Kenergie.Services.FlexPay.IInfoPaiementSocieteService, Kenergie.Services.FlexPay.InfoPaiementSocieteService>();
builder.Services.AddScoped<Kenergie.Services.FlexPay.IPaiementElectroniqueService, Kenergie.Services.FlexPay.PaiementElectroniqueService>();
builder.Services.AddScoped<Kenergie.Services.FlexPay.IPaiementFlexPayPostFinalizationService, Kenergie.Services.FlexPay.PaiementFlexPayPostFinalizationService>();
builder.Services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceService>();
builder.Services.AddScoped<FactureNotificationService>(); // Service de diffusion multi-canal des factures
builder.Services.AddScoped<ArrieresService>(); // Service de suivi des arriérés
builder.Services.AddScoped<ISocieteClientScopeService, SocieteClientScopeService>();
builder.Services.AddScoped<DashboardService>(); // Service de statistiques du dashboard
builder.Services.AddScoped<IStatistiquesService, StatistiquesService>(); // Service de statistiques centralisées
builder.Services.AddScoped<ClientFactureMigrationService>(); // Service de migration des factures vers ClientFactures
builder.Services.AddScoped<ExcelClientService>(); // Service d'import Excel pour les clients
builder.Services.AddScoped<ExcelClientFactureService>(); // Service d'import Excel pour les arriérés pré-existants
builder.Services.AddScoped<ClientExportService>(); // Service d'export Excel pour les clients
builder.Services.AddScoped<MetricsService>(); // Service de métriques système
builder.Services.AddScoped<TypeDeCourantDataService>(); // Service d'initialisation des types de courant
builder.Services.AddScoped<ISmsNotificationService, TwilioSmsService>(); // Service SMS Twilio
// Services de communication
builder.Services.AddScoped<IClientFilterService, ClientFilterService>();
builder.Services.AddScoped<ICommunicationCampaignRepository, CommunicationCampaignService>();
builder.Services.AddScoped<ICommunicationDispatchService, CommunicationDispatchService>();
// Services de plaintes clients
builder.Services.AddScoped<IPlainteClientRepository, PlainteClientService>();
builder.Services.AddScoped<IPlainteClientNotificationService, PlainteClientNotificationService>();
builder.Services.AddScoped<IDepenseRepository, DepenseService>();
builder.Services.AddScoped<ICategorieDepenseRepository, CategorieDepenseService>();



// Configuration AWS S3
var awsAccessKeyId = builder.Configuration["AWS:S3:AccessKeyId"];
var awsSecretAccessKey = builder.Configuration["AWS:S3:SecretAccessKey"];
var awsRegion = builder.Configuration["AWS:S3:Region"] ?? "us-east-1";

if (!string.IsNullOrEmpty(awsAccessKeyId) && !string.IsNullOrEmpty(awsSecretAccessKey))
{
    // Configuration du client S3 avec credentials explicites
    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion)
    };
    
    builder.Services.AddSingleton<IAmazonS3>(sp =>
    {
        return new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, s3Config);
    });
    
    // Utiliser le service S3
    builder.Services.AddScoped<IFileStorageService, S3FileStorageService>();
    Log.Information("✅ Stockage AWS S3 configuré et activé");
}
else
{
    // Fallback vers le stockage local si les credentials AWS ne sont pas configurés
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    Log.Warning("⚠️  Credentials AWS S3 non configurés. Utilisation du stockage local.");
}

builder.Services.AddScoped<IAntivirusService, AntivirusService>();
// NOTIFICATIONS AVANCÉES
builder.Services.AddScoped<Kenergie.Services.Repositories.INotificationService, Kenergie.Services.NotificationService>();
builder.Services.AddScoped<KenergieAPI.Services.Repositories.INotificationRepository, Kenergie.Services.NotificationService>();
builder.Services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
builder.Services.AddSingleton<INotificationJobQueue, NotificationJobQueue>();
builder.Services.AddScoped<INotificationSender, NotificationSender>();
builder.Services.AddScoped<PaiementNotificationService>();
builder.Services.AddHostedService<NotificationJobWorker>();
builder.Services.AddSingleton<IFactureDiffusionQueue, FactureDiffusionQueue>();
builder.Services.AddHostedService<FactureDiffusionWorker>();
builder.Services.AddScoped<IUserDeviceRepository, UserDeviceService>();
builder.Services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();

// Au démarrage de l'API
// Service de nettoyage des logs pour éviter une croissance infinie des fichiers de log (optionnel mais recommandé en production)
builder.Services.AddHostedService<LogCleanupService>();

//  ACTIVATION FIREBASE: Initialiser Firebase Admin SDK au démarrage
Console.WriteLine("\n === INITIALISATION FIREBASE ===");
var firebaseCredentialsPath = builder.Configuration["Firebase:CredentialsPath"] ?? "firebase-credentials.json";
Console.WriteLine($"📋 Chemin configuré: {firebaseCredentialsPath}");

var fullPath = Path.Combine(Directory.GetCurrentDirectory(), firebaseCredentialsPath);
Console.WriteLine($"📂 Chemin complet: {fullPath}");
Console.WriteLine($"📁 Répertoire actuel: {Directory.GetCurrentDirectory()}");

if (File.Exists(fullPath))
{
    Console.WriteLine($" Fichier trouvé ! Taille: {new FileInfo(fullPath).Length} octets");
    try
    {
        Console.WriteLine(" Initialisation Firebase en cours...");
        FirebaseNotificationService.InitializeFirebase(fullPath);
        Console.WriteLine(" Firebase Admin SDK initialisé avec succès");
        Console.WriteLine($" Credentials: {fullPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Erreur lors de l'initialisation de Firebase: {ex.Message}");
        Console.WriteLine($"📚 Stack trace: {ex.StackTrace}");
        Console.WriteLine($"⚠️  Les notifications push ne fonctionneront pas.");
    }
}
else
{
    Console.WriteLine($"❌ FICHIER INTROUVABLE: {fullPath}");
    Console.WriteLine($"⚠️  Les notifications push ne fonctionneront PAS.");
    Console.WriteLine($"💡 Solution: Placer le fichier {firebaseCredentialsPath} à la racine du projet.");
}
Console.WriteLine("🔥 === FIN INITIALISATION FIREBASE ===\n");

try
{
    Log.Information("📦 Enregistrement des services finaux...");
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<ISignalRNotificationService, SignalRNotificationService>(); // Service de notifications SignalR
    builder.Services.AddScoped<ISignalRStatistiquesService, SignalRStatistiquesService>();
    builder.Services.AddScoped<IUsernameGeneratorService, UsernameGeneratorService>(); // Service de génération de noms d'utilisateur
    builder.Services.AddScoped<SuperAdminDashboardService>(); // Service dashboard Super-Admin
    builder.Services.AddScoped<GerantDashboardService>(); // Service dashboard Gérant
    builder.Services.AddScoped<FinancierDashboardService>(); // Service dashboard Financier
    builder.Services.AddScoped<CaissierDashboardService>(); // Service dashboard Caissier
    builder.Services.AddScoped<TechnicienDashboardService>(); // Service dashboard Technicien
    builder.Services.AddScoped<ClientDashboardService>(); // Service dashboard Client
    builder.Services.AddScoped<ResponsableCommercialDashboardService>(); // Service dashboard Responsable Commercial
    builder.Services.AddScoped<AgentDirectionCommercialDashboardService>(); // Service dashboard Agent Direction Commercial
            builder.Services.AddScoped<IStatistiquesService, StatistiquesService>(); // Service de statistiques centralisées

            // Services RBAC avec permissions
            builder.Services.AddHttpContextAccessor(); // Nécessaire pour ICurrentUserService
            builder.Services.AddScoped<IPermissionService, PermissionService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            Log.Information(" Services finaux enregistrés avec succès");
            Log.Information("✅ Services finaux enregistrés avec succès");

            // ═════════════════════════════════════════════════════════════
            // ✨ SYNCHRONISATION OFFLINE: Services de synchronisation
            // ═════════════════════════════════════════════════════════════════
            builder.Services.AddScoped<IWatermarkService, WatermarkService>(); // Watermark sécurisé
            builder.Services.AddScoped<ICursorService, CursorService>(); // Cursor pagination sécurisé
            builder.Services.AddScoped<ISyncService, SyncService>(); // Service principal de sync
            Log.Information("✅ Services de synchronisation enregistrés avec succès");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Erreur lors de l'enregistrement des services finaux");
    throw;
}

try
{
    Log.Information("📡 Configuration SignalR...");
    // SignalR: Ajouter SignalR avec configuration
    builder.Services.AddSignalR(options =>
    {
        options.EnableDetailedErrors = builder.Environment.IsDevelopment();
        options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Ping toutes les 15 secondes
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Timeout après 30 secondes
        options.HandshakeTimeout = TimeSpan.FromSeconds(15); // Timeout de handshake
    });
    Log.Information("✅ SignalR configuré avec succès");
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Erreur lors de la configuration SignalR");
    throw;
}

// Configuration CORS améliorée
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // En développement, permettre toutes les origines
                policy.SetIsOriginAllowed(origin => true)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            }
            else
            {
                // PRODUCTION : Configuration CORS complète et sécurisée
                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithHeaders(
                              "Content-Type",
                              "Authorization",
                              "Accept",
                              "Origin",
                              "X-Requested-With",
                              "Cache-Control",  //  AJOUTÉ pour le web
                              "Pragma",         //  AJOUTÉ pour le web
                              "Expires"         //  AJOUTÉ pour le web
                          )
                          .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(10)); // Cache des réponses preflight
                }
                else
                {
                    // Fallback : Autoriser toutes les origines (moins sécurisé)
                    Log.Warning(" Aucune origine CORS configurée ! Utilisation du mode permissif.");
                    policy.SetIsOriginAllowed(origin => true)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            }
        });
});

Log.Information("🔧 Construction de l'application...");
WebApplication app;
try
{
    // Tenter de construire l'application
    app = builder.Build();
    Log.Information("✅ Application construite avec succès");
}
catch (Exception buildEx)
{
    // Log détaillé de l'exception
    Log.Fatal(buildEx, "❌ ERREUR CRITIQUE lors de la construction de l'application");
    Log.Fatal("📚 Type d'exception: {ExceptionType}", buildEx.GetType().FullName);
    Log.Fatal("📚 Message: {Message}", buildEx.Message);
    Log.Fatal("📚 Stack trace: {StackTrace}", buildEx.StackTrace);
    
    // Extraire toutes les exceptions internes
    var innerEx = buildEx.InnerException;
    int depth = 0;
    while (innerEx != null && depth < 10)
    {
        Log.Fatal("📚 Exception interne #{Depth}: {Type} - {Message}", depth + 1, innerEx.GetType().FullName, innerEx.Message);
        Log.Fatal("📚 Stack trace interne #{Depth}: {StackTrace}", depth + 1, innerEx.StackTrace);
        innerEx = innerEx.InnerException;
        depth++;
    }
    
    // Si c'est une StopTheHostException, essayer d'extraire plus d'informations
    if (buildEx.GetType().Name.Contains("StopTheHostException"))
    {
        Log.Fatal("⚠️ StopTheHostException détectée - cette exception masque souvent l'exception réelle");
        Log.Fatal("💡 Vérifiez les logs précédents pour trouver l'exception réelle qui a causé l'arrêt");
    }
    
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

//  ACTIVATION DE LA COMPRESSION (à mettre TRÈS TÔT dans le pipeline)
app.UseResponseCompression();
Log.Information(" Response Compression activée (Brotli/Gzip)");

//  ACTIVATION DU RATE LIMITING (AVANT l'authentification)
app.UseIpRateLimiting();
Log.Information(" Rate Limiting activé - Protection contre brute-force et abus");

// Swagger disponible dans tous les environnements
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kenergie v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "Kenergie - Documentation";
});

// Redirection HTTPS seulement en production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

app.UseRouting();

// Activation de l'authentification et de l'autorisation JWT
// Ajout du middleware pour gérer automatiquement le préfixe "Bearer"
app.UseAutoBearer();

// Ajout du middleware pour tracker les métriques
app.UseMetricsTracking();

app.UseAuthentication(); // DOIT être avant UseAuthorization
app.UseAuthorization();

app.MapControllers();

// Configuration des hubs SignalR
app.MapHub<KenergieAPI.Hubs.NotificationHub>("/hubs/notifications");
app.MapHub<Kenergie.Hubs.DashboardHub>("/hubs/dashboard");

// Apply migrations and initialize default data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<KenergieDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        // 1. Appliquer les migrations d'abord
        //  TEMPORAIREMENT DÉSACTIVÉ : Les migrations doivent être appliquées manuellement
        // logger.LogInformation("Application des migrations à la base de données...");
        // context.Database.Migrate();
        // logger.LogInformation("Migrations appliquées avec succès.");
        
        // 2. Initialisation des données par défaut (pas de vues nécessaires pour les modèles conservés)

        // 3. Initialiser les données par défaut (Super-Admin, Ekelasi School, etc.)
        logger.LogInformation("Initialisation des données par défaut...");
        await context.InitializeDefaultDataAsync();
        logger.LogInformation("Initialisation des données par défaut terminée avec succès.");
        
        // 4.  NOUVEAU : Initialiser les permissions RBAC
        logger.LogInformation("Initialisation des permissions RBAC...");
        await PermissionSeeder.SeedPermissionsAsync(context);
        logger.LogInformation("Permissions RBAC initialisées avec succès.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur s'est produite lors de l'initialisation de la base de données.");
    }
}

Log.Information("✅ KenergieAPI démarré et prêt à recevoir des requêtes");
Log.Information("📊 Environnement : {Environment}", app.Environment.EnvironmentName);
Log.Information("🔗 Swagger UI : https://localhost:7110/swagger");

// Initialiser les données de l'application
try
{
    var serviceProvider = app.Services;
    using var scope = serviceProvider.CreateScope();
    var typeDeCourantDataService = scope.ServiceProvider.GetRequiredService<TypeDeCourantDataService>();
    
    await typeDeCourantDataService.InitializeDefaultTypesAsync();
    await typeDeCourantDataService.ValidateAndRepairDataAsync();
    
    Log.Information("✅ Données de l'application initialisées avec succès");
}
catch (Exception ex)
{
    Log.Error(ex, "❌ Erreur lors de l'initialisation des données de l'application");
}

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ L'application s'est arrêtée de manière inattendue");
}
finally
{
    Log.Information("🛑 Arrêt de KenergieAPI");
    Log.CloseAndFlush();
}

// Exposer Program pour les tests d'intégration
public partial class Program { }
