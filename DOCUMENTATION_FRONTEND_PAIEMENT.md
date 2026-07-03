# 📚 Documentation Frontend - API Paiement

## 📋 Table des matières

1. [Introduction](#introduction)
2. [Authentification](#authentification)
3. [Endpoints GET](#endpoints-get)
4. [Endpoints POST](#endpoints-post)
5. [Endpoints PUT](#endpoints-put)
6. [Endpoints DELETE](#endpoints-delete)
7. [Structures de données](#structures-de-données)
8. [Codes d'erreur](#codes-derreur)
9. [Exemples d'utilisation](#exemples-dutilisation)
10. [Bonnes pratiques](#bonnes-pratiques)

---

## 🎯 Introduction

Cette documentation décrit l'utilisation de l'API de gestion des paiements. L'API permet de créer, lire, modifier et supprimer des paiements, avec intégration du modèle `ClientFacture` pour afficher les montants mis à jour.

### Base URL

```
https://votre-api.com/api/Paiement
```

### Format des données

- **Content-Type** : `application/json`
- **Format de date** : ISO 8601 (`YYYY-MM-DDTHH:mm:ss`)
- **Format des montants** : `decimal` (2 décimales)

---

## 🔐 Authentification

Tous les endpoints nécessitent une authentification via un token JWT dans le header :

```
Authorization: Bearer {token}
```

### Rôles requis par endpoint

| Endpoint | Rôles autorisés |
|----------|----------------|
| GET (liste) | Tous les utilisateurs authentifiés |
| GET (détails) | Tous les utilisateurs authentifiés |
| POST | `Super-Admin`, `Admin`, `Caissier`, `Financier` |
| PUT | `Super-Admin`, `Admin`, `Caissier`, `Financier` |
| DELETE | `Super-Admin`, `Admin` |
| GET (société) | `Super-Admin`, `Admin`, `Gerant`, `Financier`, `Caissier`, `Technicien` |

---

## 📖 Endpoints GET

### 1. Liste tous les paiements

**Endpoint :** `GET /api/Paiement`

**Description :** Récupère la liste de tous les paiements.

**Réponse :** `200 OK`

```json
[
  {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 15000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "methodePaiement": "Mobile Money",
    "referenceTransaction": "MM-20250105-001",
    "commentaire": "Paiement partiel",
    "statut": "Validé",
    "idUtilisateur": 5,
    "dateCreation": "2025-01-05T10:30:00"
  },
  {
    "idPaiement": 2,
    "idFacture": 11,
    "idClient": 124,
    "montantPaye": 25000.00,
    "datePaiement": "2025-01-05T11:00:00",
    "methodePaiement": "Espèces",
    "referenceTransaction": null,
    "commentaire": null,
    "statut": "Validé",
    "idUtilisateur": 5,
    "dateCreation": "2025-01-05T11:00:00"
  }
]
```

---

### 2. Liste paginée des paiements

**Endpoint :** `GET /api/Paiement/paged`

**Description :** Récupère une liste paginée des paiements.

**Paramètres de requête :**

| Paramètre | Type | Requis | Description |
|-----------|------|--------|-------------|
| `pageNumber` | `int` | Non | Numéro de page (défaut: 1) |
| `pageSize` | `int` | Non | Taille de page (défaut: 20, max: 100) |
| `sortBy` | `string` | Non | Champ de tri |
| `sortDescending` | `bool` | Non | Tri décroissant si `true` (défaut: `false`) |
| `searchTerm` | `string` | Non | Terme de recherche optionnel |

**Exemple de requête :**

```
GET /api/Paiement/paged?pageNumber=1&pageSize=20&sortBy=datePaiement&sortDescending=true
```

**Réponse :** `200 OK`

```json
{
  "data": [
    {
      "idPaiement": 1,
      "idFacture": 10,
      "idClient": 123,
      "montantPaye": 15000.00,
      "datePaiement": "2025-01-05T10:30:00",
      "statut": "Validé"
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 8,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

### 3. Détails d'un paiement

**Endpoint :** `GET /api/Paiement/{id}`

**Description :** Récupère les détails d'un paiement spécifique.

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `int` | ID du paiement |

**Réponse :** `200 OK`

```json
{
  "idPaiement": 1,
  "idFacture": 10,
  "idClient": 123,
  "montantPaye": 15000.00,
  "datePaiement": "2025-01-05T10:30:00",
  "methodePaiement": "Mobile Money",
  "referenceTransaction": "MM-20250105-001",
  "commentaire": "Paiement partiel",
  "statut": "Validé",
  "idUtilisateur": 5,
  "dateCreation": "2025-01-05T10:30:00"
}
```

**Erreur :** `404 Not Found` si le paiement n'existe pas

---

### 4. Paiements d'une facture

**Endpoint :** `GET /api/Paiement/facture/{idFacture}`

**Description :** Récupère tous les paiements associés à une facture.

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `idFacture` | `int` | ID de la facture |

**Réponse :** `200 OK`

```json
[
  {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 15000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "statut": "Validé"
  },
  {
    "idPaiement": 2,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 5000.00,
    "datePaiement": "2025-01-06T14:00:00",
    "statut": "Validé"
  }
]
```

**Erreur :** `404 Not Found` si la facture n'existe pas

---

### 5. Paiements d'un client

**Endpoint :** `GET /api/Paiement/client/{idClient}`

**Description :** Récupère tous les paiements d'un client.

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `idClient` | `int` | ID du client |

**Réponse :** `200 OK`

```json
[
  {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 15000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "statut": "Validé"
  }
]
```

---

### 6. Paiements d'une société

**Endpoint :** `GET /api/Paiement/societe/{idSociete}`

**Description :** Récupère tous les paiements d'une société.

**Rôles requis :** `Super-Admin`, `Admin`, `Gerant`, `Financier`, `Caissier`, `Technicien`

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `idSociete` | `int` | ID de la société |

**Réponse :** `200 OK`

```json
[
  {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 15000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "statut": "Validé"
  }
]
```

---

### 7. Paiements d'une société (paginé)

**Endpoint :** `GET /api/Paiement/societe/{idSociete}/paged`

**Description :** Récupère une liste paginée des paiements d'une société.

**Rôles requis :** `Super-Admin`, `Admin`, `Gerant`, `Financier`, `Caissier`, `Technicien`

**Paramètres de requête :** Identiques à `/paged`

**Réponse :** `200 OK` (format identique à `/paged`)

---

### 8. Total des paiements d'une facture

**Endpoint :** `GET /api/Paiement/facture/{idFacture}/total`

**Description :** Récupère le total des paiements validés pour une facture.

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `idFacture` | `int` | ID de la facture |

**Réponse :** `200 OK`

```json
{
  "idFacture": 10,
  "numeroFacture": "FAC-RES-0125-0001",
  "totalPaiements": 20000.00,
  "montant": 25000.00
}
```

**Erreur :** `404 Not Found` si la facture n'existe pas

---

## ➕ Endpoints POST

### Créer un paiement

**Endpoint :** `POST /api/Paiement`

**Description :** Crée un nouveau paiement et met automatiquement à jour la `ClientFacture` associée.

**Rôles requis :** `Super-Admin`, `Admin`, `Caissier`, `Financier`

**Corps de la requête :**

```json
{
  "idFacture": 10,
  "idClient": 123,
  "montantPaye": 15000.00,
  "datePaiement": "2025-01-05T10:30:00",
  "methodePaiement": "Mobile Money",
  "referenceTransaction": "MM-20250105-001",
  "commentaire": "Paiement partiel",
  "statut": "Validé"
}
```

**Champs :**

| Champ | Type | Requis | Description |
|-------|------|--------|-------------|
| `idFacture` | `int` | ✅ Oui | ID de la facture |
| `idClient` | `int?` | ⚠️ Optionnel | ID du client (recommandé) |
| `montantPaye` | `decimal` | ✅ Oui | Montant payé (doit être > 0) |
| `datePaiement` | `DateTime?` | ❌ Non | Date du paiement (défaut: maintenant) |
| `methodePaiement` | `string?` | ❌ Non | Méthode de paiement (max 50 caractères) |
| `referenceTransaction` | `string?` | ❌ Non | Référence de transaction (max 100 caractères) |
| `commentaire` | `string?` | ❌ Non | Commentaire (max 500 caractères) |
| `statut` | `string?` | ❌ Non | Statut (défaut: "Validé") |

**Réponse :** `201 Created`

```json
{
  "paiement": {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 15000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "methodePaiement": "Mobile Money",
    "referenceTransaction": "MM-20250105-001",
    "commentaire": "Paiement partiel",
    "statut": "Validé",
    "idUtilisateur": 5,
    "dateCreation": "2025-01-05T10:30:00"
  },
  "facture": {
    "idFacture": 10,
    "numeroFacture": "FAC-RES-0125-0001",
    "montant": 10000.00,
    "moisEmission": 1,
    "anneesEmission": 2025
  },
  "clientFacture": {
    "idClientFacture": 456,
    "montant": 20000.00,
    "montantPaye": 15000.00,
    "montantDu": 5000.00,
    "nombreBatiment": 2,
    "estArrierePreExistant": false
  },
  "message": "Paiement enregistré avec succès"
}
```

**⚠️ Important :** La réponse inclut les informations de `ClientFacture` avec les montants mis à jour après le paiement :
- `montant` : Montant total (facture.Montant × nombreBatiment)
- `montantPaye` : Montant déjà payé (mis à jour)
- `montantDu` : Montant restant dû (mis à jour)

**Erreurs possibles :**

- `400 Bad Request` : Données invalides
  ```json
  {
    "message": "IdFacture est requis et doit être inclus dans le DTO."
  }
  ```

- `404 Not Found` : Facture non trouvée
  ```json
  {
    "message": "Facture non trouvée"
  }
  ```

- `500 Internal Server Error` : Erreur serveur
  ```json
  {
    "message": "Erreur lors de la création du paiement: {détails}"
  }
  ```

---

## ✏️ Endpoints PUT

### Modifier un paiement

**Endpoint :** `PUT /api/Paiement/{id}`

**Description :** Modifie un paiement existant et met automatiquement à jour la `ClientFacture` associée.

**Rôles requis :** `Super-Admin`, `Admin`, `Caissier`, `Financier`

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `int` | ID du paiement |

**Corps de la requête :**

```json
{
  "idPaiement": 1,
  "idFacture": 10,
  "idClient": 123,
  "montantPaye": 20000.00,
  "datePaiement": "2025-01-05T10:30:00",
  "methodePaiement": "Virement",
  "referenceTransaction": "VIR-20250105-001",
  "commentaire": "Paiement complet",
  "statut": "Validé"
}
```

**⚠️ Important :** L'`idPaiement` dans le corps doit correspondre à l'`id` dans l'URL.

**Réponse :** `200 OK`

```json
{
  "paiement": {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 20000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "methodePaiement": "Virement",
    "referenceTransaction": "VIR-20250105-001",
    "commentaire": "Paiement complet",
    "statut": "Validé",
    "idUtilisateur": 5,
    "dateCreation": "2025-01-05T10:30:00"
  },
  "facture": {
    "idFacture": 10,
    "numeroFacture": "FAC-RES-0125-0001",
    "montant": 10000.00
  },
  "clientFacture": {
    "idClientFacture": 456,
    "montant": 20000.00,
    "montantPaye": 20000.00,
    "montantDu": 0.00,
    "nombreBatiment": 2,
    "estArrierePreExistant": false
  },
  "message": "Paiement mis à jour avec succès"
}
```

**Erreurs possibles :**

- `400 Bad Request` : ID ne correspond pas ou données invalides
  ```json
  {
    "message": "L'ID dans l'URL ne correspond pas à l'ID dans le corps"
  }
  ```

- `404 Not Found` : Paiement non trouvé

- `500 Internal Server Error` : Erreur serveur
  ```json
  {
    "message": "Erreur lors de la mise à jour"
  }
  ```

---

## 🗑️ Endpoints DELETE

### Supprimer un paiement

**Endpoint :** `DELETE /api/Paiement/{id}`

**Description :** Supprime un paiement et met automatiquement à jour la `ClientFacture` associée.

**Rôles requis :** `Super-Admin`, `Admin`

**Paramètres :**

| Paramètre | Type | Description |
|-----------|------|-------------|
| `id` | `int` | ID du paiement |

**Réponse :** `200 OK`

```json
{
  "paiementSupprime": {
    "idPaiement": 1,
    "idFacture": 10,
    "idClient": 123,
    "montantPaye": 15000.00,
    "datePaiement": "2025-01-05T10:30:00",
    "statut": "Validé"
  },
  "facture": {
    "idFacture": 10,
    "numeroFacture": "FAC-RES-0125-0001",
    "montant": 10000.00
  },
  "clientFacture": {
    "idClientFacture": 456,
    "montant": 20000.00,
    "montantPaye": 0.00,
    "montantDu": 20000.00,
    "nombreBatiment": 2,
    "estArrierePreExistant": false
  },
  "message": "Paiement supprimé avec succès"
}
```

**⚠️ Important :** 
- L'endpoint retourne maintenant `200 OK` avec les informations au lieu de `204 No Content`
- La `ClientFacture` est mise à jour après suppression (montants recalculés)

**Erreurs possibles :**

- `404 Not Found` : Paiement non trouvé

---

## 📊 Structures de données

### Paiement

```typescript
interface Paiement {
  idPaiement: number;
  idFacture: number;
  idClient?: number | null;
  montantPaye: number;
  datePaiement: string; // ISO 8601
  methodePaiement?: string | null;
  referenceTransaction?: string | null;
  commentaire?: string | null;
  statut: string;
  idUtilisateur?: number | null;
  dateCreation: string; // ISO 8601
}
```

### CreatePaiementDto

```typescript
interface CreatePaiementDto {
  idFacture: number; // Requis
  idClient?: number | null; // Optionnel mais recommandé
  montantPaye: number; // Requis, > 0
  datePaiement?: string | null; // ISO 8601, optionnel
  methodePaiement?: string | null; // Max 50 caractères
  referenceTransaction?: string | null; // Max 100 caractères
  commentaire?: string | null; // Max 500 caractères
  statut?: string | null; // Défaut: "Validé"
}
```

### CreatePaiementResponseDto

```typescript
interface CreatePaiementResponseDto {
  paiement: Paiement;
  facture?: Facture | null;
  clientFacture?: ClientFactureInfoDto | null;
  message: string;
}
```

### UpdatePaiementResponseDto

```typescript
interface UpdatePaiementResponseDto {
  paiement: Paiement;
  facture?: Facture | null;
  clientFacture?: ClientFactureInfoDto | null;
  message: string;
}
```

### DeletePaiementResponseDto

```typescript
interface DeletePaiementResponseDto {
  paiementSupprime?: Paiement | null;
  facture?: Facture | null;
  clientFacture?: ClientFactureInfoDto | null;
  message: string;
}
```

### ClientFactureInfoDto

```typescript
interface ClientFactureInfoDto {
  idClientFacture: number;
  montant?: number | null; // Montant total (facture.Montant × nombreBatiment)
  montantPaye?: number | null; // Montant déjà payé (mis à jour)
  montantDu?: number | null; // Montant restant dû (mis à jour)
  nombreBatiment?: number | null; // Snapshot du nombre de bâtiments
  estArrierePreExistant: boolean; // Indique si c'est un arriéré pré-existant
}
```

### PagedResult<T>

```typescript
interface PagedResult<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
```

### PagedRequest

```typescript
interface PagedRequest {
  pageNumber?: number; // Défaut: 1
  pageSize?: number; // Défaut: 20, Max: 100
  sortBy?: string | null;
  sortDescending?: boolean; // Défaut: false
  searchTerm?: string | null;
}
```

---

## ❌ Codes d'erreur

| Code | Description | Exemple |
|------|-------------|---------|
| `200 OK` | Succès | GET, PUT, DELETE (avec réponse) |
| `201 Created` | Ressource créée | POST |
| `400 Bad Request` | Données invalides | Validation échouée |
| `401 Unauthorized` | Non authentifié | Token manquant ou invalide |
| `403 Forbidden` | Non autorisé | Rôle insuffisant |
| `404 Not Found` | Ressource non trouvée | Paiement/Facture inexistant |
| `500 Internal Server Error` | Erreur serveur | Erreur inattendue |

---

## 💡 Exemples d'utilisation

### Exemple 1 : Créer un paiement

```javascript
// JavaScript/TypeScript
const createPaiement = async (paiementData) => {
  try {
    const response = await fetch('https://api.example.com/api/Paiement', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        idFacture: 10,
        idClient: 123,
        montantPaye: 15000.00,
        datePaiement: new Date().toISOString(),
        methodePaiement: 'Mobile Money',
        referenceTransaction: 'MM-20250105-001',
        commentaire: 'Paiement partiel',
        statut: 'Validé'
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }

    const result = await response.json();
    
    console.log('Paiement créé:', result.paiement);
    console.log('Montant total:', result.clientFacture.montant);
    console.log('Montant payé:', result.clientFacture.montantPaye);
    console.log('Montant dû:', result.clientFacture.montantDu);
    
    return result;
  } catch (error) {
    console.error('Erreur:', error.message);
    throw error;
  }
};
```

### Exemple 2 : Modifier un paiement

```javascript
const updatePaiement = async (id, paiementData) => {
  try {
    const response = await fetch(`https://api.example.com/api/Paiement/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        idPaiement: id,
        ...paiementData
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message);
    }

    const result = await response.json();
    console.log('Paiement mis à jour:', result.paiement);
    console.log('Nouveau montant dû:', result.clientFacture.montantDu);
    
    return result;
  } catch (error) {
    console.error('Erreur:', error.message);
    throw error;
  }
};
```

### Exemple 3 : Supprimer un paiement

```javascript
const deletePaiement = async (id) => {
  try {
    const response = await fetch(`https://api.example.com/api/Paiement/${id}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error('Erreur lors de la suppression');
    }

    const result = await response.json();
    console.log('Paiement supprimé:', result.paiementSupprime);
    console.log('Montant dû après suppression:', result.clientFacture.montantDu);
    
    return result;
  } catch (error) {
    console.error('Erreur:', error.message);
    throw error;
  }
};
```

### Exemple 4 : Récupérer les paiements d'une facture avec montants

```javascript
const getPaiementsWithMontants = async (idFacture) => {
  try {
    // Récupérer les paiements
    const paiementsResponse = await fetch(
      `https://api.example.com/api/Paiement/facture/${idFacture}`,
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );
    
    const paiements = await paiementsResponse.json();
    
    // Récupérer le total
    const totalResponse = await fetch(
      `https://api.example.com/api/Paiement/facture/${idFacture}/total`,
      {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      }
    );
    
    const total = await totalResponse.json();
    
    return {
      paiements,
      total: total.totalPaiements,
      montantFacture: total.montant,
      resteAPayer: total.montant - total.totalPaiements
    };
  } catch (error) {
    console.error('Erreur:', error.message);
    throw error;
  }
};
```

### Exemple 5 : Utilisation avec React

```tsx
import React, { useState } from 'react';

interface PaiementFormProps {
  idFacture: number;
  idClient: number;
  onPaiementCreated: (result: CreatePaiementResponseDto) => void;
}

const PaiementForm: React.FC<PaiementFormProps> = ({ 
  idFacture, 
  idClient, 
  onPaiementCreated 
}) => {
  const [montantPaye, setMontantPaye] = useState<number>(0);
  const [methodePaiement, setMethodePaiement] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await fetch('/api/Paiement', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          idFacture,
          idClient,
          montantPaye,
          methodePaiement,
          statut: 'Validé'
        })
      });

      if (!response.ok) {
        throw new Error('Erreur lors de la création du paiement');
      }

      const result = await response.json();
      onPaiementCreated(result);
      
      // Afficher les informations de ClientFacture
      alert(
        `Paiement créé !\n` +
        `Montant total: ${result.clientFacture.montant} FCFA\n` +
        `Montant payé: ${result.clientFacture.montantPaye} FCFA\n` +
        `Montant dû: ${result.clientFacture.montantDu} FCFA`
      );
    } catch (error) {
      console.error('Erreur:', error);
      alert('Erreur lors de la création du paiement');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="number"
        value={montantPaye}
        onChange={(e) => setMontantPaye(parseFloat(e.target.value))}
        placeholder="Montant payé"
        required
        min="0.01"
        step="0.01"
      />
      <select
        value={methodePaiement}
        onChange={(e) => setMethodePaiement(e.target.value)}
        required
      >
        <option value="">Sélectionner une méthode</option>
        <option value="Espèces">Espèces</option>
        <option value="Mobile Money">Mobile Money</option>
        <option value="Virement">Virement</option>
        <option value="Carte">Carte</option>
      </select>
      <button type="submit" disabled={loading}>
        {loading ? 'Enregistrement...' : 'Enregistrer le paiement'}
      </button>
    </form>
  );
};
```

---

## ✅ Bonnes pratiques

### 1. Gestion des erreurs

```javascript
const handleApiError = (response) => {
  if (!response.ok) {
    if (response.status === 401) {
      // Rediriger vers la page de connexion
      window.location.href = '/login';
    } else if (response.status === 403) {
      // Afficher un message d'autorisation insuffisante
      alert('Vous n\'avez pas les permissions nécessaires');
    } else if (response.status === 404) {
      // Ressource non trouvée
      alert('Ressource non trouvée');
    } else {
      // Autre erreur
      response.json().then(data => {
        alert(`Erreur: ${data.message}`);
      });
    }
    throw new Error(`HTTP ${response.status}`);
  }
};
```

### 2. Utilisation de ClientFacture

**⚠️ Important :** Toujours utiliser les montants de `ClientFacture` plutôt que de recalculer :

```javascript
// ✅ BON : Utiliser les montants de ClientFacture
const montantTotal = result.clientFacture.montant;
const montantPaye = result.clientFacture.montantPaye;
const montantDu = result.clientFacture.montantDu;

// ❌ MAUVAIS : Recalculer depuis la facture
const montantTotal = result.facture.montant * nombreBatiment; // Incorrect !
```

### 3. Fournir IdClient

**Recommandation :** Toujours fournir `IdClient` lors de la création d'un paiement pour une meilleure précision :

```javascript
// ✅ BON : Fournir IdClient
{
  idFacture: 10,
  idClient: 123, // Fourni explicitement
  montantPaye: 15000.00
}

// ⚠️ ACCEPTABLE : IdClient optionnel (première ClientFacture utilisée)
{
  idFacture: 10,
  montantPaye: 15000.00
}
```

### 4. Gestion des dates

```javascript
// ✅ BON : Utiliser ISO 8601
const datePaiement = new Date().toISOString();

// ❌ MAUVAIS : Format personnalisé
const datePaiement = '05/01/2025'; // Ne pas utiliser
```

### 5. Validation côté client

```javascript
const validatePaiement = (paiement) => {
  const errors = [];
  
  if (!paiement.idFacture || paiement.idFacture <= 0) {
    errors.push('ID de facture requis');
  }
  
  if (!paiement.montantPaye || paiement.montantPaye <= 0) {
    errors.push('Montant payé doit être supérieur à 0');
  }
  
  if (paiement.methodePaiement && paiement.methodePaiement.length > 50) {
    errors.push('Méthode de paiement trop longue (max 50 caractères)');
  }
  
  return errors;
};
```

### 6. Mise à jour de l'UI après paiement

```javascript
const handlePaiementCreated = (result) => {
  // Mettre à jour l'affichage avec les nouvelles valeurs
  updateMontantPaye(result.clientFacture.montantPaye);
  updateMontantDu(result.clientFacture.montantDu);
  
  // Afficher un message de succès
  showNotification(
    `Paiement enregistré ! Reste à payer: ${result.clientFacture.montantDu} FCFA`
  );
  
  // Rafraîchir la liste des paiements
  refreshPaiementsList();
};
```

---

## 📝 Notes importantes

### ClientFacture

- Les montants dans `ClientFacture` sont **pré-calculés** et **mis à jour automatiquement** après chaque opération (POST, PUT, DELETE)
- Le `montant` inclut déjà la multiplication par `nombreBatiment`
- Utilisez toujours les valeurs de `ClientFacture` pour l'affichage, ne recalculez pas

### Statut du paiement

- Par défaut, le statut est `"Validé"` si non fourni
- Seuls les paiements avec `statut === "Validé"` sont pris en compte dans les calculs
- Les valeurs acceptées : `"Validé"`, `"En attente"`, `"Rejeté"`, etc.

### IdClient optionnel

- Si `IdClient` n'est pas fourni, la première `ClientFacture` de la facture est utilisée
- Pour une meilleure précision, **toujours fournir `IdClient`**

### DELETE retourne maintenant une réponse

- L'endpoint DELETE retourne `200 OK` avec les informations au lieu de `204 No Content`
- Cela permet d'afficher les montants mis à jour après suppression

---

## 🔗 Ressources supplémentaires

- Documentation API complète : `/docs`
- Swagger UI : `/swagger`
- Support : support@example.com

---

**Version :** 1.2.0  
**Dernière mise à jour :** 2025-01-05  
**Auteur :** Équipe Kenergie API
