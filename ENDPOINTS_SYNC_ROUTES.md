# 🛣️ **Endpoints de Synchronisation - Routes Complètes**

## 📋 **Vue d'ensemble**

**5 nouveaux endpoints** pour la synchronisation offline  
**Tous sous la route `/api/sync`** pour cohérence  
**1 endpoint modifié** pour optimisation existante

---

## 🚀 **Nouveaux Endpoints (5 au total)**

### **1. Bootstrap - Informations de synchronisation**
```http
GET /api/sync/bootstrap
```

**Purpose:** Fournir les informations initiales pour démarrer la synchronisation  
**Authentification:** JWT (tenant automatique)  
**Response:**
```json
{
  "serverTimeUtc": "2026-03-20T10:15:30Z",
  "snapshot": "2026-03-20T10:15:30Z",
  "recommendedPageSize": 1000,
  "maxPageSize": 5000,
  "supportsDelta": true,
  "datasets": {
    "clients": { "estimatedCount": 15000 },
    "arrears": { "estimatedLines": 42000 }
  }
}
```

---

### **2. Sync Clients - Download paginé**
```http
GET /api/sync/clients?cursor=...&pageSize=...&snapshot=...&since=...
```

**Purpose:** Télécharger les clients avec cursor pagination et delta sync  
**Authentification:** JWT (filtre automatique par société)  
**Paramètres:**
- `cursor` (optionnel): Token de pagination base64
- `pageSize` (optionnel): Taille de page (défaut: 1000, max: 5000)
- `snapshot` (optionnel): Token de snapshot serveur
- `since` (optionnel): Date/heure pour delta sync (UTC)

**Response:**
```json
{
  "snapshot": "2026-03-20T10:15:30Z",
  "items": [
    {
      "idClient": 123,
      "nomClient": "Jean Dupont",
      "adresseClient": "123 Rue A",
      "telephone": "+243123456789",
      "emailClient": "jean@email.com",
      "codeCons": "CONS001",
      "idCategorieClient": 4,
      "idAxe": 12,
      "idCabine": 7,
      "isActif": true,
      "statut": true,
      "updatedAt": "2026-03-19T08:21:00Z"
    }
  ],
  "nextCursor": "base64(eyJ1cGRhdGVkQXQiOiIyMDI2LTAzLTE5VDA4OjIxOjAwWiIsImlkQ2xpZW50IjoxMjR9)",
  "hasMore": true
}
```

---

### **3. Sync Arriérés - Download paginé**
```http
GET /api/sync/arrears?cursor=...&pageSize=...&snapshot=...&since=...&onlyOutstanding=true
```

**Purpose:** Télécharger les arriérés/impayés avec filtres optimisés  
**Authentification:** JWT (filtre automatique par société)  
**Paramètres:**
- `cursor` (optionnel): Token de pagination base64
- `pageSize` (optionnel): Taille de page (défaut: 1000, max: 5000)
- `snapshot` (optionnel): Token de snapshot serveur
- `since` (optionnel): Date/heure pour delta sync (UTC)
- `onlyOutstanding` (optionnel): Filtre montants dus > 0 (défaut: true)

**Response:**
```json
{
  "snapshot": "2026-03-20T10:15:30Z",
  "items": [
    {
      "idClientFacture": 9991,
      "idFacture": 8812,
      "idClient": 123,
      "numeroFacture": "FAC-2026-0001",
      "dateEmission": "2026-03-01T00:00:00Z",
      "mois": "3",
      "annees": 2026,
      "montantTotal": 100.0,
      "montantPaye": 20.0,
      "montantDu": 80.0,
      "libelleUsage": "Résidentiel",
      "estArrierePreExistant": false,
      "updatedAt": "2026-03-19T08:21:00Z"
    }
  ],
  "nextCursor": "base64(eyJ1cGRhdGVkQXQiOiIyMDI2LTAzLTE5VDA4OjIxOjAwWiIsImlkQ2xpZW50RmFjdHVyZSI6OTk5MX0=",
  "hasMore": true
}
```

---

### **4. Sync Deletions - Suppressions depuis dernière sync**
```http
GET /api/sync/deletions?since=...&snapshot=...
```

**Purpose:** Obtenir la liste des suppressions depuis la dernière sync  
**Authentification:** JWT (filtre automatique par société)  
**Paramètres:**
- `since` (requis): Date/heure de dernière sync (UTC)
- `snapshot` (optionnel): Token de snapshot serveur

**Response:**
```json
{
  "snapshot": "2026-03-20T10:15:30Z",
  "deletedClientIds": [1, 2, 3],
  "clearedClientFactureIds": [9991, 9992],
  "deletedPaymentIds": [555, 556]
}
```

---

### **5. Paiements Offline - Upload batch idempotent**
```http
POST /api/sync/payments/batch
```

**Purpose:** Uploader les paiements effectués offline avec idempotence  
**Authentification:** JWT (filtre automatique par société)  
**Request Body:**
```json
{
  "items": [
    {
      "clientRequestId": "6d5b7f0e-6a38-4e9b-8d1b-0f23b4d5b2a1",
      "idClient": 123,
      "idClientFacture": 9991,
      "idFacture": 8812,
      "montantPaye": 80.0,
      "datePaiementUtc": "2026-03-20T10:10:00Z",
      "methodePaiement": "Espèces",
      "referenceTransaction": "OFF-6d5b7f0e-6a38-4e9b-8d1b-0f23b4d5b2a1",
      "commentaire": "Paiement offline (mobile)",
      "deviceId": "device-123",
      "agentId": 42
    }
  ]
}
```

**Response:**
```json
{
  "results": [
    {
      "clientRequestId": "6d5b7f0e-6a38-4e9b-8d1b-0f23b4d5b2a1",
      "status": "created",
      "idPaiement": 5551,
      "newMontantDu": 0.0,
      "message": "Paiement créé avec succès"
    }
  ],
  "summary": {
    "total": 1,
    "created": 1,
    "duplicates": 0,
    "errors": 0
  }
}
```

---

## 🔄 **Endpoint Modifié (1 optimisation)**

### **6. Arriérés Client - Optimisé pour sync**
```http
# EXISTANT (optimisé)
GET /api/ClientFacture/client/{idClient}/arrieres-consolides
```

**Modification:** Ajout de paramètres de synchronisation  
**Nouveaux paramètres:**
- `since` (optionnel): Delta sync depuis date/heure
- `pageSize` (optionnel): Pagination pour gros volumes
- `includePaid` (optionnel): Inclure paiements soldés (défaut: false)

**Route inchangée** mais **optimisations internes** ajoutées.

---

## 📊 **Résumé des Routes**

| Endpoint | Route | Méthode | Purpose | Auth |
|----------|-------|---------|---------|------|
| **Bootstrap** | `/api/sync/bootstrap` | GET | Infos sync | JWT |
| **Clients** | `/api/sync/clients` | GET | Download clients | JWT |
| **Arriérés** | `/api/sync/arrears` | GET | Download arriérés | JWT |
| **Deletions** | `/api/sync/deletions` | GET | Suppressions | JWT |
| **Paiements** | `/api/sync/payments/batch` | POST | Upload paiements | JWT |
| **Arriérés Client** | `/api/ClientFacture/client/{idClient}/arrieres-consolides` | GET | Optimisé | JWT |

**Total: 5 nouveaux endpoints + 1 optimisé**

---

## 🏗️ **Structure du Controller**

```csharp
[ApiController]
[Route("api/sync")]
[Authorize] // JWT avec tenant automatique
public class SyncController : ControllerBase
{
    // GET /api/sync/bootstrap
    [HttpGet("bootstrap")]
    public async Task<ActionResult<SyncBootstrapDto>> GetBootstrap()
    
    // GET /api/sync/clients
    [HttpGet("clients")]
    public async Task<ActionResult<SyncPageDto<ClientSyncDto>>> GetClients([FromQuery] SyncRequest request)
    
    // GET /api/sync/arrears
    [HttpGet("arrears")]
    public async Task<ActionResult<SyncPageDto<ArrearSyncDto>>> GetArrears([FromQuery] SyncRequest request)
    
    // GET /api/sync/deletions
    [HttpGet("deletions")]
    public async Task<ActionResult<SyncDeletionsDto>> GetDeletions([FromQuery] SyncDeletionsRequest request)
    
    // POST /api/sync/payments/batch
    [HttpPost("payments/batch")]
    public async Task<ActionResult<PaymentBatchResultDto>> ProcessPaymentsBatch([FromBody] PaymentBatchRequest request)
}
```

---

## 🔐 **Sécurité & Autorisation**

### **JWT Claims utilisés:**
```csharp
// Filtre automatique par société
var societeId = _currentUserService.SocieteId;

// Filtrage par agent (si applicable)
var agentId = _currentUserService.AgentId;

// Périmètre de données automatique
query = query.Where(x => x.IdSociete == societeId);
```

### **Permissions requises:**
```csharp
// Tous les endpoints nécessitent:
[Authorize] // JWT valide
// ET l'accès aux données de la société du JWT
// PAS besoin de vérifier manuellement idSociete
```

---

## 📱 **Exemple de Flux Mobile Complet**

### **1. Bootstrap (premier lancement)**
```http
GET /api/sync/bootstrap
→ Récupérer snapshot et paramètres
```

### **2. Download Clients (pagination)**
```http
GET /api/sync/clients?pageSize=1000
→ Page 1: nextCursor = "abc123"
GET /api/sync/clients?pageSize=1000&cursor=abc123
→ Page 2: nextCursor = "def456"
→ ...jusqu'à hasMore = false
```

### **3. Download Arriérés (pagination)**
```http
GET /api/sync/arrears?pageSize=2000&onlyOutstanding=true
→ Pages successives jusqu'à hasMore = false
```

### **4. Paiement Offline (upload)**
```http
POST /api/sync/payments/batch
→ Upload des paiements avec clientRequestId unique
```

### **5. Delta Sync (quotidien)**
```http
GET /api/sync/clients?since=2025-03-20T10:00:00Z
→ Uniquement les nouveaux/modifiés
GET /api/sync/arrears?since=2025-03-20T10:00:00Z
→ Uniquement les nouveaux arriérés
GET /api/sync/deletions?since=2025-03-20T10:00:00Z
→ Uniquement les suppressions
```

---

## 🎯 **Avantages de cette Structure**

### **1. Cohérence**
- **Route unique** `/api/sync` pour toutes les opérations
- **Nomenclature uniforme** (bootstrap, clients, arrears, deletions, payments)

### **2. Scalabilité**
- **Cursor pagination** pour volumes massifs
- **Delta sync** pour optimisation continue
- **Compression automatique** (Brotli/Gzip)

### **3. Simplicité mobile**
- **5 endpoints seulement** à implémenter
- **Flux logique** et prédictible
- **Réponses uniformes** avec pagination

### **4. Sécurité**
- **JWT tenant isolation** automatique
- **Pas de paramètres idSociete** exposés
- **Permissions RBAC** respectées

---

## 📋 **Checklist Implémentation**

### **Backend (5 endpoints):**
- [ ] `SyncController.cs` avec 5 actions
- [ ] `ISyncService.cs` interface et implémentation
- [ ] DTOs pour chaque endpoint
- [ ] Cursor pagination helper
- [ ] Idempotence pour paiements

### **Database (modifications):**
- [ ] `UpdatedAt` sur `Clients` et `Paiements`
- [ ] `ClientRequestId` sur `Paiements`
- [ ] Index composites optimisés
- [ ] Trigger `ON UPDATE` pour `UpdatedAt`

### **Tests:**
- [ ] Tests unitaires service
- [ ] Tests intégration controller
- [ ] Tests charge avec volumes réels
- [ ] Tests idempotence paiements

---

## 🚀 **Conclusion**

**5 nouveaux endpoints** sous la route `/api/sync` pour une synchronisation offline complète:

1. **GET /api/sync/bootstrap** - Informations initiales
2. **GET /api/sync/clients** - Download clients (read-only)
3. **GET /api/sync/arrears** - Download arriérés (optimisé)
4. **GET /api/sync/deletions** - Suppressions delta
5. **POST /api/sync/payments/batch** - Upload paiements idempotent

**+ 1 endpoint optimisé** pour maintenir la compatibilité existante.

Cette structure offre **cohérence, performance et simplicité** pour une synchronisation offline robuste! 🎯✨

---

*Routes définies le 21 mars 2026 - Architecture de synchronisation complète*
