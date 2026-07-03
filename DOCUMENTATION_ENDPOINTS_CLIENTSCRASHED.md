# 📚 Documentation : Endpoints ClientCrashed

## 📋 Résumé

Documentation complète des endpoints pour gérer les lignes échouées (`clientsCrashed`) lors de l'import Excel.

**Base URL :** `/api/ClientCrashed`  
**Authentification :** Tous les endpoints nécessitent un token JWT valide  
**Date :** 2025-01-05

---

## 🔐 Autorisations

| Endpoint | Méthode | Rôles Requis |
|----------|---------|--------------|
| GET `/api/ClientCrashed` | GET | Tous les utilisateurs authentifiés |
| GET `/api/ClientCrashed/{id}` | GET | Tous les utilisateurs authentifiés |
| GET `/api/ClientCrashed/societe/{idSociete}` | GET | Tous les utilisateurs authentifiés |
| GET `/api/ClientCrashed/statut/{statut}` | GET | Tous les utilisateurs authentifiés |
| GET `/api/ClientCrashed/societe/{idSociete}/statut/{statut}` | GET | Tous les utilisateurs authentifiés |
| PUT `/api/ClientCrashed/{id}` | PUT | Super-Admin, Admin |
| POST `/api/ClientCrashed/{id}/retry` | POST | Super-Admin, Admin |
| DELETE `/api/ClientCrashed/{id}` | DELETE | Super-Admin, Admin |
| DELETE `/api/ClientCrashed/{id}/permanent` | DELETE | Super-Admin uniquement |

---

## 📊 Endpoints GET

### 1. Liste toutes les lignes échouées

**Endpoint :** `GET /api/ClientCrashed`

**Description :** Récupère toutes les lignes échouées, triées par date de création (plus récentes en premier).

**Réponse :** `200 OK`

```json
[
  {
    "idClientCrashed": 1,
    "idSociete": 1,
    "numeroLigne": 3,
    "nomClient": "MULONDA SAFARI",
    "adresseClient": "KIKINDI",
    "telephone": "+243900000001",
    "emailClient": "mulonda@email.com",
    "genreClient": "M",
    "codeCons": "A/a1/0002",
    "libelleUsage": "CINEMENT",
    "donneesBrutesJson": "{...}",
    "messageErreur": "L'usage 'CINEMENT' n'existe pas pour cette société",
    "typeErreur": "VALIDATION",
    "erreursJson": "[\"L'usage 'CINEMENT' n'existe pas\"]",
    "statut": "EN_ATTENTE",
    "idClientCree": null,
    "dateCreation": "2025-01-05T10:30:00",
    "dateCorrection": null,
    "dateModification": null
  }
]
```

---

### 2. Récupère une ligne échouée par ID

**Endpoint :** `GET /api/ClientCrashed/{id}`

**Paramètres :**
- `id` (int, path) : ID de la ligne échouée

**Réponse :** `200 OK` ou `404 Not Found`

```json
{
  "idClientCrashed": 1,
  "idSociete": 1,
  "numeroLigne": 3,
  "nomClient": "MULONDA SAFARI",
  "adresseClient": "KIKINDI",
  "telephone": "+243900000001",
  "emailClient": "mulonda@email.com",
  "genreClient": "M",
  "codeCons": "A/a1/0002",
  "libelleUsage": "CINEMENT",
  "messageErreur": "L'usage 'CINEMENT' n'existe pas pour cette société",
  "typeErreur": "VALIDATION",
  "statut": "EN_ATTENTE",
  "idClientCree": null
}
```

**Erreur 404 :**
```json
{
  "message": "Ligne échouée non trouvée"
}
```

---

### 3. Récupère les lignes échouées d'une société

**Endpoint :** `GET /api/ClientCrashed/societe/{idSociete}`

**Paramètres :**
- `idSociete` (int, path) : ID de la société

**Réponse :** `200 OK`

```json
[
  {
    "idClientCrashed": 1,
    "idSociete": 1,
    "numeroLigne": 3,
    "nomClient": "MULONDA SAFARI",
    "statut": "EN_ATTENTE",
    ...
  }
]
```

---

### 4. Récupère les lignes échouées par statut

**Endpoint :** `GET /api/ClientCrashed/statut/{statut}`

**Paramètres :**
- `statut` (string, path) : Statut de la ligne (`EN_ATTENTE`, `CORRIGE`, `IGNORE`)

**Réponse :** `200 OK`

**Exemple :**
```
GET /api/ClientCrashed/statut/EN_ATTENTE
```

---

### 5. Récupère les lignes échouées d'une société par statut

**Endpoint :** `GET /api/ClientCrashed/societe/{idSociete}/statut/{statut}`

**Paramètres :**
- `idSociete` (int, path) : ID de la société
- `statut` (string, path) : Statut de la ligne

**Réponse :** `200 OK`

**Exemple :**
```
GET /api/ClientCrashed/societe/1/statut/EN_ATTENTE
```

---

## ✏️ Endpoints PUT

### 6. Met à jour une ligne échouée

**Endpoint :** `PUT /api/ClientCrashed/{id}`

**Paramètres :**
- `id` (int, path) : ID de la ligne échouée

**Body :** `UpdateClientCrashedDto`

```json
{
  "nomClient": "MULONDA SAFARI CORRIGE",
  "adresseClient": "KIKINDI UPDATED",
  "telephone": "+243900000001",
  "emailClient": "mulonda.updated@email.com",
  "genreClient": "M",
  "codeCons": "A/a1/0002",
  "libelleUsage": "Résidentiel",  // Corriger l'usage
  "statut": "CORRIGE"  // Marquer comme corrigé
}
```

**Réponse :** `200 OK`

```json
{
  "idClientCrashed": 1,
  "nomClient": "MULONDA SAFARI CORRIGE",
  "libelleUsage": "Résidentiel",
  "statut": "CORRIGE",
  "dateCorrection": "2025-01-05T11:00:00",
  ...
}
```

**Notes :**
- Tous les champs sont optionnels (seuls les champs fournis seront mis à jour)
- Si `statut` est défini à `"CORRIGE"`, `dateCorrection` est automatiquement mis à jour
- `dateModification` est automatiquement mis à jour

---

## 🔄 Endpoints POST

### 7. Réessaye la création d'un client

**Endpoint :** `POST /api/ClientCrashed/{id}/retry`

**Paramètres :**
- `id` (int, path) : ID de la ligne échouée

**Description :** Tente de créer un client à partir des données de la ligne échouée. Si le client existe déjà (par CodeCons), met à jour le statut sans créer de doublon.

**Réponse :** `200 OK`

**Succès :**
```json
{
  "success": true,
  "message": "Client créé avec succès",
  "idClientCree": 123,
  "idClientCrashed": 1,
  "erreur": null
}
```

**Client existe déjà :**
```json
{
  "success": true,
  "message": "Un client avec ce CodeCons existe déjà (ID: 123)",
  "idClientCree": 123,
  "idClientCrashed": 1,
  "erreur": null
}
```

**Erreur :**
```json
{
  "success": false,
  "message": "Erreur lors de la création du client",
  "idClientCree": null,
  "idClientCrashed": 1,
  "erreur": "L'usage 'CINEMENT' n'existe pas pour cette société"
}
```

**Notes :**
- Si la création réussit, le statut de la ligne est automatiquement mis à `"CORRIGE"`
- `idClientCree` est rempli avec l'ID du client créé
- `dateCorrection` est automatiquement mis à jour
- Si une erreur survient, le `messageErreur` est mis à jour avec le nouveau message

---

## 🗑️ Endpoints DELETE

### 8. Ignore une ligne échouée (soft delete)

**Endpoint :** `DELETE /api/ClientCrashed/{id}`

**Paramètres :**
- `id` (int, path) : ID de la ligne échouée

**Description :** Marque la ligne comme `"IGNORE"` au lieu de la supprimer définitivement (soft delete).

**Réponse :** `200 OK`

```json
{
  "message": "Ligne échouée ignorée avec succès"
}
```

**Erreur 404 :**
```json
{
  "message": "Ligne échouée non trouvée"
}
```

---

### 9. Supprime définitivement une ligne échouée (hard delete)

**Endpoint :** `DELETE /api/ClientCrashed/{id}/permanent`

**Paramètres :**
- `id` (int, path) : ID de la ligne échouée

**Description :** Supprime définitivement la ligne de la base de données. **⚠️ Action irréversible !**

**Autorisation :** Super-Admin uniquement

**Réponse :** `200 OK`

```json
{
  "message": "Ligne échouée supprimée définitivement"
}
```

---

## 📝 Types de Données

### ClientCrashedResponseDto

```typescript
interface ClientCrashedResponseDto {
  idClientCrashed: number;
  idSociete: number;
  numeroLigne: number;
  nomClient?: string;
  adresseClient?: string;
  telephone?: string;
  emailClient?: string;
  genreClient?: string;
  codeCons?: string;
  libelleUsage?: string;
  donneesBrutesJson?: string;
  messageErreur: string;
  typeErreur?: string;  // "VALIDATION", "DATABASE", "USAGE", "EMAIL"
  erreursJson?: string;
  statut: string;  // "EN_ATTENTE", "CORRIGE", "IGNORE"
  idClientCree?: number;
  dateCreation: string;  // ISO 8601
  dateCorrection?: string;  // ISO 8601
  dateModification?: string;  // ISO 8601
}
```

### UpdateClientCrashedDto

```typescript
interface UpdateClientCrashedDto {
  nomClient?: string;  // Max 200 caractères
  adresseClient?: string;  // Max 500 caractères
  telephone?: string;  // Max 20 caractères
  emailClient?: string;  // Max 256 caractères, format email valide
  genreClient?: string;  // Max 10 caractères
  codeCons?: string;  // Max 100 caractères
  libelleUsage?: string;
  statut?: string;  // "EN_ATTENTE", "CORRIGE", "IGNORE"
}
```

### RetryClientCrashedResponseDto

```typescript
interface RetryClientCrashedResponseDto {
  success: boolean;
  message: string;
  idClientCree?: number;
  idClientCrashed: number;
  erreur?: string;
}
```

---

## 🔄 Flux de Travail Recommandé

### 1. Identifier les lignes échouées

```http
GET /api/ClientCrashed/societe/1/statut/EN_ATTENTE
```

### 2. Examiner les erreurs

```http
GET /api/ClientCrashed/123
```

### 3. Corriger les données

```http
PUT /api/ClientCrashed/123
Content-Type: application/json

{
  "libelleUsage": "Résidentiel",  // Corriger l'usage
  "statut": "CORRIGE"
}
```

### 4. Réessayer la création

```http
POST /api/ClientCrashed/123/retry
```

### 5. Vérifier le résultat

```http
GET /api/ClientCrashed/123
```

Si `idClientCree` est rempli, la création a réussi !

---

## ⚠️ Types d'Erreurs

| TypeErreur | Description | Exemple |
|------------|-------------|---------|
| `VALIDATION` | Erreur de validation des données | Usages inexistants, champs obligatoires manquants |
| `DATABASE` | Erreur de base de données | Contraintes uniques violées, erreurs de transaction |
| `USAGE` | Erreur liée aux usages | Usage non trouvé, usage invalide |
| `EMAIL` | Erreur liée à l'email | Email en conflit (maintenant résolu automatiquement) |

---

## 📊 Statuts

| Statut | Description |
|--------|-------------|
| `EN_ATTENTE` | Ligne en attente de correction (par défaut) |
| `CORRIGE` | Ligne corrigée, prête à être réessayée ou déjà créée |
| `IGNORE` | Ligne ignorée (soft delete) |

---

## 🔍 Exemples d'Utilisation

### Exemple 1 : Corriger un usage invalide

```http
# 1. Récupérer la ligne échouée
GET /api/ClientCrashed/123

# 2. Corriger l'usage
PUT /api/ClientCrashed/123
{
  "libelleUsage": "Résidentiel",
  "statut": "CORRIGE"
}

# 3. Réessayer
POST /api/ClientCrashed/123/retry
```

### Exemple 2 : Ignorer une ligne

```http
DELETE /api/ClientCrashed/123
```

### Exemple 3 : Lister toutes les lignes en attente d'une société

```http
GET /api/ClientCrashed/societe/1/statut/EN_ATTENTE
```

---

## ✅ Codes de Réponse

| Code | Description |
|------|-------------|
| `200 OK` | Succès |
| `201 Created` | Ressource créée (non utilisé pour ClientCrashed) |
| `400 Bad Request` | Données invalides |
| `401 Unauthorized` | Token JWT manquant ou invalide |
| `403 Forbidden` | Permissions insuffisantes |
| `404 Not Found` | Ligne échouée non trouvée |
| `500 Internal Server Error` | Erreur serveur |

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
