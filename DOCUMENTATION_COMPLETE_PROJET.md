# 📚 Documentation Complète - KenergieAPI

**Version :** 1.0  
**Date :** 15 janvier 2026  
**Auteur :** Équipe Kenergie  
**Statut :** ✅ Production

---

## 📋 Table des Matières

1. [Vue d'Ensemble](#vue-densemble)
2. [Architecture](#architecture)
3. [Modules et Fonctionnalités](#modules-et-fonctionnalités)
4. [API Endpoints](#api-endpoints)
5. [Modèles de Données](#modèles-de-données)
6. [Services](#services)
7. [Authentification et Sécurité](#authentification-et-sécurité)
8. [Configuration](#configuration)
9. [Déploiement](#déploiement)
10. [Guide de Développement](#guide-de-développement)

---

## 🎯 Vue d'Ensemble

### Description

**KenergieAPI** est une API REST complète développée avec **ASP.NET Core 8.0** et **Entity Framework Core** pour la gestion d'un système de facturation et de gestion client pour le secteur de l'énergie. L'API utilise **MariaDB 10.11 (LTS)** comme base de données et implémente des fonctionnalités avancées de facturation, paiement, communication et notification.

### Technologies Principales

- **Framework :** ASP.NET Core 8.0
- **ORM :** Entity Framework Core 8.0
- **Base de données :** MariaDB 10.11 (LTS)
- **Authentification :** JWT (JSON Web Tokens)
- **Documentation API :** Swagger/OpenAPI
- **Logging :** Serilog
- **Notifications :** Firebase Cloud Messaging (FCM), Twilio SMS, Email SMTP
- **Stockage de fichiers :** Amazon S3
- **Rate Limiting :** AspNetCoreRateLimit
- **Compression :** Gzip/Brotli

### Fonctionnalités Principales

- ✅ Gestion complète des clients et factures
- ✅ Système de paiement et suivi des arriérés
- ✅ Campagnes de communication ciblées
- ✅ Notifications multi-canaux (Push, SMS, Email, In-App)
- ✅ Gestion des plaintes clients
- ✅ Dashboard et statistiques
- ✅ Import/Export Excel
- ✅ Audit et traçabilité
- ✅ Gestion des rôles et permissions

---

## 🏗️ Architecture

### Structure du Projet

```
KenergieAPI/
├── Controllers/          # Contrôleurs API REST (26 fichiers)
├── Services/             # Services métier et repositories (58 fichiers)
│   ├── Repositories/     # Interfaces des repositories
│   └── Notifications/    # Services de notification
├── Models/               # Modèles de données (111 fichiers)
│   └── DTOs/            # Data Transfer Objects
├── Data/                 # Contexte Entity Framework
├── Middleware/           # Middleware personnalisés
├── Attributes/           # Attributs personnalisés
├── Helpers/             # Classes utilitaires
├── Hubs/                # SignalR Hubs
├── Migrations/          # Migrations Entity Framework (93 fichiers)
├── Scripts/             # Scripts SQL et shell
└── docs/                # Documentation (30 fichiers)
```

### Pattern Architectural

Le projet suit une **architecture en couches** avec séparation des responsabilités :

1. **Controllers** : Gestion des requêtes HTTP et validation
2. **Services** : Logique métier et orchestration
3. **Repositories** : Accès aux données (pattern Repository)
4. **Models** : Entités et DTOs
5. **Data** : Contexte Entity Framework

### Principes de Conception

- ✅ **Dependency Injection** : Tous les services sont injectés
- ✅ **Repository Pattern** : Abstraction de l'accès aux données
- ✅ **DTO Pattern** : Séparation entre modèles de domaine et DTOs
- ✅ **Soft Delete** : Désactivation plutôt que suppression
- ✅ **Audit Trail** : Traçabilité de toutes les actions
- ✅ **Async/Await** : Opérations asynchrones pour la performance

---

## 📦 Modules et Fonctionnalités

### 1. 🔐 Module Authentification

**Contrôleur :** `UtilisateurController`  
**Service :** `UtilisateurService`, `SimpleJwtService`, `RefreshTokenService`

**Fonctionnalités :**
- Authentification par email/mot de passe
- Génération de tokens JWT
- Refresh tokens pour renouvellement automatique
- Gestion des rôles et permissions
- Récupération de mot de passe

**Endpoints principaux :**
- `POST /api/Utilisateur/login` - Connexion
- `POST /api/Utilisateur/refresh-token` - Renouveler le token
- `POST /api/Utilisateur/forgot-password` - Récupération mot de passe
- `GET /api/Utilisateur/me` - Informations utilisateur actuel

---

### 2. 👥 Module Client

**Contrôleur :** `ClientController`  
**Service :** `ClientService`, `ExcelClientService`

**Fonctionnalités :**
- CRUD complet des clients
- Génération automatique de `CodeCons` (format: `{codeCabine}/{codeAxe}/{4 digits}`)
- Gestion des usages clients (ClientUsage)
- Import/Export Excel
- Recherche et filtrage avancés
- Synchronisation avec utilisateurs

**Endpoints principaux :**
- `GET /api/Client` - Liste des clients
- `POST /api/Client` - Créer un client
- `GET /api/Client/{id}` - Détails d'un client
- `PUT /api/Client/{id}` - Modifier un client
- `DELETE /api/Client/{id}` - Supprimer un client (soft delete)
- `GET /api/Client/codecons/{codeCons}` - Rechercher par CodeCons
- `GET /api/Client/template-excel` - Template Excel
- `POST /api/Client/bulk-excel` - Import Excel en masse

---

### 3. 💰 Module Facturation

**Contrôleur :** `FactureController`  
**Service :** `FactureService`, `FactureNotificationService`

**Fonctionnalités :**
- Création de factures par usage
- Diffusion automatique aux clients
- Consolidation des factures par client
- Gestion des arriérés
- Statistiques de diffusion

**Endpoints principaux :**
- `GET /api/Facture` - Liste des factures
- `POST /api/Facture` - Créer une facture
- `GET /api/Facture/{id}` - Détails d'une facture
- `POST /api/Facture/{idFacture}/societe/{idSociete}/diffusion` - Diffuser une facture
- `POST /api/Facture/societe/{idSociete}/diffusion/bulk` - Diffusion en masse
- `GET /api/Facture/{idFacture}/diffusion/statistiques` - Statistiques de diffusion

---

### 4. 📊 Module ClientFacture

**Contrôleur :** `ClientFactureController`  
**Service :** `ClientFactureService`, `ExcelClientFactureService`

**Fonctionnalités :**
- Gestion de la relation Client-Facture
- Pré-calcul de `MontantPaye` et `MontantDu` pour performance
- Gestion des arriérés pré-existants
- Consolidation par période (mois/année)
- Import Excel en masse
- Rapports d'arriérés consolidés

**Endpoints principaux :**
- `GET /api/ClientFacture` - Liste des ClientFacture
- `GET /api/ClientFacture/client/{idClient}/consolidees` - Factures consolidées par client
- `GET /api/ClientFacture/client/{idClient}/arrieres` - Arriérés d'un client
- `GET /api/ClientFacture/client/{idClient}/arrieres-consolides` - Arriérés consolidés
- `GET /api/ClientFacture/arrieres` - Tous les arriérés
- `GET /api/ClientFacture/arrieres-consolides` - Arriérés consolidés globaux
- `GET /api/ClientFacture/template-excel` - Template Excel
- `POST /api/ClientFacture/bulk-excel` - Import Excel en masse

---

### 5. 💳 Module Paiement

**Contrôleur :** `PaiementController`  
**Service :** `PaiementService`, `PaiementNotificationService`

**Fonctionnalités :**
- Enregistrement des paiements
- Mise à jour automatique de `ClientFacture` après paiement
- Calcul des totaux consolidés
- Identification des factures impayées
- Notifications de paiement

**Endpoints principaux :**
- `GET /api/Paiement` - Liste des paiements
- `POST /api/Paiement` - Créer un paiement
- `GET /api/Paiement/facture/{idFacture}` - Paiements d'une facture
- `GET /api/Paiement/client/{idClient}` - Paiements d'un client
- `GET /api/Paiement/societe/{idSociete}/factureImpayee` - Factures impayées
- `GET /api/Paiement/facture/{idFacture}/total` - Total des paiements

---

### 6. 📢 Module Communication

**Contrôleur :** `CommunicationCampaignController`  
**Service :** `CommunicationCampaignService`, `CommunicationDispatchService`, `ClientFilterService`

**Fonctionnalités :**
- Création de campagnes de communication
- Ciblage avancé des clients :
  - Par catégorie
  - Par société
  - Par usage
  - Par nombre de factures en arriérés ✨ NOUVEAU
- Envoi multi-canaux (Push, SMS, Email, In-App)
- Envoi immédiat ou programmé
- Statistiques d'envoi

**Endpoints principaux :**
- `POST /api/CommunicationCampaign` - Créer une campagne
- `GET /api/CommunicationCampaign` - Liste des campagnes
- `GET /api/CommunicationCampaign/{id}` - Détails d'une campagne
- `POST /api/CommunicationCampaign/{id}/execute` - Exécuter une campagne
- `GET /api/CommunicationCampaign/{id}/preview` - Prévisualiser les destinataires

**Critères de ciblage :**
```json
{
  "idCategorieClients": [1, 2],
  "idSociete": 1,
  "usage": ["DOMESTIQUE", "COMMERCIAL"],
  "nombreFacturesArrieresMin": 3,  // ✨ NOUVEAU
  "nombreFacturesArrieresMax": 5   // ✨ NOUVEAU
}
```

---

### 7. 🔔 Module Notifications

**Contrôleur :** `NotificationController`, `NotificationPushController`  
**Service :** `NotificationService`, `FirebaseNotificationService`, `SignalRNotificationService`, `TwilioSmsService`, `EmailService`

**Fonctionnalités :**
- Notifications Push (Firebase Cloud Messaging)
- Notifications SMS (Twilio)
- Notifications Email (SMTP)
- Notifications In-App (SignalR)
- Gestion des préférences de notification
- Historique des notifications

**Endpoints principaux :**
- `GET /api/Notification` - Liste des notifications
- `GET /api/Notification/destinataire/{idDestinataire}` - Notifications d'un destinataire
- `PUT /api/Notification/{id}/marquer-lue` - Marquer comme lue
- `GET /api/NotificationPreference` - Préférences de notification

---

### 8. 📋 Module Plainte Client

**Contrôleur :** `PlainteClientController`  
**Service :** `PlainteClientService`, `PlainteClientNotificationService`

**Fonctionnalités :**
- Création de plaintes par les clients
- Gestion des plaintes par l'équipe
- Assignation d'agents
- Suivi des statuts (En attente, En cours, Résolu, Fermé)
- Gestion des priorités
- Notifications automatiques

**Endpoints principaux :**
- `POST /api/PlainteClient` - Créer une plainte (Client)
- `GET /api/PlainteClient/mes-plaintes` - Mes plaintes (Client)
- `GET /api/PlainteClient` - Lister toutes les plaintes (Équipe)
- `PATCH /api/PlainteClient/{id}/assigner` - Assigner un agent
- `PATCH /api/PlainteClient/{id}/resoudre` - Résoudre une plainte

---

### 9. 📊 Module Dashboard

**Contrôleur :** `DashboardController`  
**Service :** `DashboardService`

**Fonctionnalités :**
- Statistiques globales par société
- Nombre de clients actifs
- Total des factures
- Total des paiements
- Total des arriérés ✨ NOUVEAU
- Paiements du mois

**Endpoints principaux :**
- `GET /api/Dashboard/{idSociete}` - Statistiques d'une société

**Réponse :**
```json
{
  "nombreClientsActifs": 150,
  "totalFactures": 500,
  "totalPaiements": 7500000,
  "paiementsDuMois": 500000,
  "totalGeneralArriere": 2000000  // ✨ NOUVEAU
}
```

---

### 10. 🏢 Module Organisation

**Contrôleurs :** `SocieteController`, `AxeController`, `CabineController`, `CategorieClientController`, `UsageController`  
**Services :** `SocieteService`, `AxeService`, `CabineService`, `CategorieClientService`, `UsageService`

**Fonctionnalités :**
- Gestion des sociétés
- Gestion des axes
- Gestion des cabines
- Gestion des catégories clients
- Gestion des usages

---

### 11. 👨‍💼 Module Agent

**Contrôleur :** `AgentController`  
**Service :** `AgentService`

**Fonctionnalités :**
- Gestion des agents
- Association aux sociétés
- Synchronisation avec utilisateurs

---

### 12. 🔍 Module Audit

**Contrôleur :** `AuditController`  
**Service :** `AuditService`

**Fonctionnalités :**
- Traçabilité de toutes les actions
- Historique des modifications
- Logs d'audit par utilisateur, date, action

**Endpoints principaux :**
- `GET /api/Audit` - Liste des audits
- `GET /api/Audit/utilisateur/{idUtilisateur}` - Audits d'un utilisateur
- `GET /api/Audit/entite/{nomEntite}` - Audits d'une entité

---

### 13. 🔐 Module Rôles et Permissions

**Contrôleurs :** `RoleController`, `PermissionController`  
**Services :** `RoleService`, `PermissionService`, `AuthorizationService`

**Fonctionnalités :**
- Gestion des rôles
- Gestion des permissions
- Vérification des autorisations
- Rôles prédéfinis : Super-Admin, Admin, Gerant, Financier, Caissier, Technicien, Client

---

## 🔌 API Endpoints

### Base URL

```
https://votre-api.com/api
```

### Authentification

Tous les endpoints (sauf `/api/Utilisateur/login`) nécessitent un token JWT dans le header :

```
Authorization: Bearer {token}
```

### Format des Réponses

- **Content-Type :** `application/json`
- **Format de date :** ISO 8601 (`YYYY-MM-DDTHH:mm:ss`)
- **Format des montants :** `decimal` (2 décimales)
- **Naming convention :** camelCase

### Codes de Statut HTTP

- `200 OK` - Succès
- `201 Created` - Ressource créée
- `204 No Content` - Succès sans contenu
- `400 Bad Request` - Requête invalide
- `401 Unauthorized` - Non authentifié
- `403 Forbidden` - Non autorisé
- `404 Not Found` - Ressource non trouvée
- `500 Internal Server Error` - Erreur serveur

### Documentation Swagger

La documentation interactive de l'API est disponible à :

```
https://votre-api.com/swagger
```

---

## 📊 Modèles de Données

### Modèles Principaux

#### Client
```csharp
public class Client
{
    public int IdClient { get; set; }
    public string CodeCons { get; set; }        // Format: {codeCabine}/{codeAxe}/{4 digits}
    public string NomClient { get; set; }
    public string? Telephone { get; set; }
    public string? EmailClient { get; set; }
    public bool Statut { get; set; }             // Soft delete
    public bool IsActif { get; set; }
    // ...
}
```

#### Facture
```csharp
public class Facture
{
    public int IdFacture { get; set; }
    public string NumeroFacture { get; set; }
    public decimal? Montant { get; set; }
    public int IdUsage { get; set; }
    public DateTime? DateEmission { get; set; }
    public bool EstDiffusee { get; set; }
    public bool Statut { get; set; }
    // ...
}
```

#### ClientFacture
```csharp
public class ClientFacture
{
    public int IdClientFacture { get; set; }
    public int? IdFacture { get; set; }          // NULL pour arriérés pré-existants
    public int IdClient { get; set; }
    public decimal? Montant { get; set; }        // Pré-calculé (facture.Montant × nombreBatiment)
    public decimal? MontantPaye { get; set; }    // Pré-calculé depuis Paiements
    public decimal? MontantDu { get; set; }      // Pré-calculé (Montant - MontantPaye)
    public int? nombreBatiment { get; set; }     // Snapshot
    public string? Mois { get; set; }
    public int? Annees { get; set; }
    public DateTime? DateEmission { get; set; }
    public bool EstArrierePreExistant { get; set; }
    public bool Statut { get; set; }
    // ...
}
```

#### Paiement
```csharp
public class Paiement
{
    public int IdPaiement { get; set; }
    public int IdFacture { get; set; }
    public int? IdClient { get; set; }
    public decimal MontantPaye { get; set; }
    public DateTime DatePaiement { get; set; }
    public string? MethodePaiement { get; set; }
    public string Statut { get; set; }           // "Validé", "En attente", etc.
    public bool IsDeleted { get; set; }          // Soft delete
    // ...
}
```

#### CommunicationCampaign
```csharp
public class CommunicationCampaign
{
    public int IdCampagne { get; set; }
    public string Titre { get; set; }
    public string Contenu { get; set; }
    public string TypeCampagne { get; set; }     // "INFO", "ALERTE", "PROMOTION", etc.
    public int? IdSociete { get; set; }
    public string? CriteresCiblage { get; set; } // JSON
    public bool ActiverPush { get; set; }
    public bool ActiverSms { get; set; }
    public bool ActiverEmail { get; set; }
    public bool ActiverInApp { get; set; }
    public DateTime? DateEnvoi { get; set; }
    // ...
}
```

### Relations Principales

- **Client** ↔ **ClientUsage** ↔ **Usage** ↔ **CategorieClient** ↔ **Societe**
- **Facture** → **Usage**
- **ClientFacture** → **Client** + **Facture**
- **Paiement** → **Facture** + **Client**
- **CommunicationCampaign** → **Societe**

---

## ⚙️ Services

### Services Métier

| Service | Description |
|---------|-------------|
| `ClientService` | Gestion des clients, génération CodeCons |
| `FactureService` | Gestion des factures, création automatique ClientFacture |
| `ClientFactureService` | Consolidation, arriérés, rapports |
| `PaiementService` | Gestion des paiements, mise à jour ClientFacture |
| `CommunicationCampaignService` | Gestion des campagnes |
| `CommunicationDispatchService` | Exécution des campagnes |
| `ClientFilterService` | Filtrage des clients selon critères |
| `DashboardService` | Calcul des statistiques |
| `AuditService` | Traçabilité des actions |

### Services de Notification

| Service | Description |
|---------|-------------|
| `FirebaseNotificationService` | Notifications Push (FCM) |
| `TwilioSmsService` | Notifications SMS |
| `EmailService` | Notifications Email |
| `SignalRNotificationService` | Notifications In-App (temps réel) |
| `NotificationSender` | Orchestrateur multi-canaux |

### Services Utilitaires

| Service | Description |
|---------|-------------|
| `ExcelClientService` | Import/Export Excel clients |
| `ExcelClientFactureService` | Import/Export Excel ClientFacture |
| `FileStorageService` | Stockage de fichiers (S3) |
| `CacheService` | Cache en mémoire |
| `CurrentUserService` | Utilisateur actuel (depuis JWT) |

---

## 🔐 Authentification et Sécurité

### JWT (JSON Web Tokens)

**Configuration :**
- **Secret Key :** Configuré dans `appsettings.json`
- **Expiration :** 120 minutes (configurable)
- **Refresh Token :** 30 jours (configurable)
- **Algorithme :** HS256

**Flux d'authentification :**
1. `POST /api/Utilisateur/login` → Retourne `accessToken` et `refreshToken`
2. Utiliser `accessToken` dans le header `Authorization: Bearer {token}`
3. Quand le token expire, utiliser `POST /api/Utilisateur/refresh-token` avec `refreshToken`

### Rôles et Permissions

**Rôles prédéfinis :**
- `Super-Admin` : Accès complet
- `Admin` : Gestion complète (sauf certaines actions Super-Admin)
- `Gerant` : Gestion de la société
- `Financier` : Gestion financière
- `Caissier` : Enregistrement des paiements
- `Technicien` : Gestion technique
- `Client` : Accès limité à ses propres données

**Attributs d'autorisation :**
```csharp
[Authorize]                                    // Authentification requise
[Authorize(Roles = "Admin,Super-Admin")]      // Rôles spécifiques
```

### Rate Limiting

Protection contre les abus avec **AspNetCoreRateLimit** :
- Limite par IP
- Configurable dans `appsettings.json`

### Soft Delete

Toutes les entités importantes utilisent le soft delete :
- `Client.Statut`
- `Facture.Statut`
- `ClientFacture.Statut`
- `Paiement.IsDeleted`

---

## ⚙️ Configuration

### Fichier `appsettings.json`

```json
{
  "ConnectionStrings": {
    "KelasiConnection": "Server=...;Database=...;User=...;Password=...;"
  },
  "Jwt": {
    "SecretKey": "...",
    "ExpirationMinutes": 120,
    "RefreshTokenExpirationDays": 30
  },
  "Firebase": {
    "ProjectId": "...",
    "CredentialsPath": "..."
  },
  "Twilio": {
    "AccountSid": "...",
    "AuthToken": "...",
    "PhoneNumber": "..."
  },
  "EmailSettings": {
    "SmtpServer": "...",
    "Port": 587,
    "SenderEmail": "..."
  },
  "S3Settings": {
    "AccessKey": "...",
    "SecretKey": "...",
    "BucketName": "..."
  }
}
```

### Variables d'Environnement

Pour la production, utilisez des variables d'environnement au lieu de valeurs en dur.

---

## 🚀 Déploiement

### Prérequis

- .NET 8.0 SDK
- MariaDB 10.11 (LTS)
- Serveur web (IIS, Nginx, Apache)

### Étapes de Déploiement

1. **Cloner le repository**
   ```bash
   git clone https://github.com/votre-repo/KenergieAPI.git
   cd KenergieAPI
   ```

2. **Configurer la base de données**
   ```bash
   # Créer la base de données
   mysql -u root -p < Scripts/create_database_production.sql
   ```

3. **Appliquer les migrations**
   ```bash
   dotnet ef database update
   ```

4. **Configurer `appsettings.Production.json`**
   - Mettre à jour la chaîne de connexion
   - Configurer les clés JWT
   - Configurer Firebase, Twilio, etc.

5. **Publier l'application**
   ```bash
   dotnet publish -c Release -o ./publish
   ```

6. **Déployer**
   - Copier le dossier `publish` sur le serveur
   - Configurer le serveur web (IIS, Nginx, etc.)
   - Démarrer l'application

### Scripts de Déploiement

Des scripts sont disponibles dans le dossier `Scripts/` :
- `deploy.sh` - Script de déploiement Linux
- `deploy.ps1` - Script de déploiement Windows

---

## 👨‍💻 Guide de Développement

### Structure des Contrôleurs

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonController : ControllerBase
{
    private readonly IMonService _service;
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonModel>>> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }
    
    [HttpPost]
    public async Task<ActionResult<MonModel>> Create([FromBody] CreateMonDto dto)
    {
        // Validation, création, retour
    }
}
```

### Structure des Services

```csharp
public class MonService : IMonRepository
{
    private readonly KenergieDbContext _context;
    
    public async Task<MonModel> CreateAsync(MonModel model)
    {
        model.DateCreation = DateTime.Now;
        _context.MonModels.Add(model);
        await _context.SaveChangesAsync();
        return model;
    }
}
```

### Bonnes Pratiques

1. **Toujours utiliser `async/await`** pour les opérations I/O
2. **Valider les entrées** avec `ModelState.IsValid`
3. **Gérer les erreurs** avec try-catch et logging
4. **Utiliser les DTOs** pour les entrées/sorties
5. **Implémenter le soft delete** pour les entités importantes
6. **Logger les actions importantes** avec `ILogger`
7. **Utiliser `Include` et `ThenInclude`** pour éviter N+1 queries

### Tests

Les tests unitaires sont dans `Kenergie.Tests.Unit/`.

Pour exécuter les tests :
```bash
dotnet test
```

---

## 📚 Documentation Supplémentaire

### Documents Disponibles

- `docs/API_DOCUMENTATION_COMMUNICATION.md` - Documentation Communication
- `docs/API_DOCUMENTATION_PLAINTE_CLIENT.md` - Documentation Plaintes
- `PLAN_TRAVAIL_FILTRAGE_ARRIERES_COMMUNICATION.md` - Filtrage par arriérés
- `PLAN_ACTION_ADAPTATION_PAIEMENT_CLIENTFACTURE.md` - Adaptation Paiement
- `RECAPITULATIF_FINAL_CLIENTFACTURE.md` - Récapitulatif ClientFacture

### Collections Postman

- `Kenergie_API_Collection.postman_collection.json` - Collection complète

---

## 🆘 Support et Contact

Pour toute question ou problème :
- **Email :** support@kenergie.com
- **Documentation :** https://docs.kenergie.com
- **Issues :** https://github.com/votre-repo/KenergieAPI/issues

---

## 📝 Changelog

### Version 1.0 (15 janvier 2026)

**Nouveautés :**
- ✅ Filtrage par nombre de factures en arriérés dans les communications
- ✅ Adaptation des endpoints paiement avec ClientFacture
- ✅ Enrichissement des endpoints de diffusion
- ✅ Dashboard avec TotalGeneralArriere
- ✅ Arriérés consolidés globaux

**Améliorations :**
- ✅ Performance optimisée avec pré-calculs
- ✅ Consolidation des factures par période
- ✅ Import/Export Excel améliorés

**Corrections :**
- ✅ Gestion des nullable types
- ✅ Correction des erreurs de compilation

---

**Documentation générée le :** 15 janvier 2026  
**Dernière mise à jour :** 15 janvier 2026  
**Version de l'API :** 1.0
