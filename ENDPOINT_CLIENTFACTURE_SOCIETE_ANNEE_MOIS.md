# ✅ Endpoint ClientFacture par Société, Année et Mois

## 📋 Résumé

Nouvel endpoint GET pour récupérer les `ClientFacture` d'une société pour une année et un mois donnés, filtrées où le montant dû est supérieur au montant payé (arriérés).

---

## 🎯 Endpoint Créé

### Route
```
GET /api/ClientFacture/societe/{idSociete}/annees/{annees}/mois/{mois}
```

### Paramètres
- `idSociete` (int) : Identifiant de la société
- `annees` (int) : Année (ex: 2024)
- `mois` (string) : Mois (format: "01", "02", ..., "12" ou "Janvier", "Février", etc.)

### Réponse
- **Type :** `IEnumerable<ClientFactureDto>`
- **Filtre appliqué :** `MontantDu > MontantPaye` (montant dû supérieur au montant payé)
- **Tri :** Par `DateEmission` décroissante, puis par `DateCreation` décroissante

---

## 🔍 Logique de Filtrage

### Critères de Filtrage

1. **Statut actif** : `Statut == true`
2. **Année** : `Annees == {annees}`
3. **Mois** : `Mois == {mois}`
4. **Arriérés** : `MontantDu > MontantPaye` (montant dû supérieur au montant payé)
5. **Société** : Filtrage par société via deux chemins possibles :
   - **Chemin 1 (Factures système)** : `ClientFacture -> Facture -> Usage -> CategorieClient -> Societe`
   - **Chemin 2 (Arriérés pré-existants)** : `ClientFacture -> Client -> Axe -> Cabine -> Societe`

### Gestion des Deux Types de ClientFacture

L'endpoint gère deux types de `ClientFacture` :

1. **ClientFacture avec Facture** (`IdFacture != null`) :
   - Filtrage via `Facture -> Usage -> CategorieClient -> Societe`
   - Pour les factures créées dans le système

2. **ClientFacture sans Facture** (`IdFacture == null`) :
   - Filtrage via `Client -> Axe -> Cabine -> Societe`
   - Pour les arriérés pré-existants (avant l'informatisation)

---

## 📊 Exemple de Requête

### Requête
```
GET /api/ClientFacture/societe/1/annees/2024/mois/01
```

### Réponse JSON
```json
[
  {
    "idClientFacture": 10,
    "idFacture": 5,
    "idClient": 3,
    "montant": 50000.00,
    "nombreBatiment": 2,
    "montantPaye": 20000.00,
    "montantDu": 30000.00,
    "mois": "01",
    "annees": 2024,
    "dateEmission": "2024-01-15T00:00:00Z",
    "estArrierePreExistant": false,
    "description": null,
    "statut": true,
    "dateCreation": "2024-01-15T10:30:00Z",
    "dateModification": null,
    "nomClient": "Jean Dupont",
    "numeroFacture": "FAC-2024-001",
    "libelleUsage": "Résidentiel"
  },
  {
    "idClientFacture": 15,
    "idFacture": null,
    "idClient": 7,
    "montant": 75000.00,
    "nombreBatiment": 1,
    "montantPaye": 10000.00,
    "montantDu": 65000.00,
    "mois": "01",
    "annees": 2024,
    "dateEmission": "2024-01-10T00:00:00Z",
    "estArrierePreExistant": true,
    "description": "Arriéré de décembre 2023",
    "statut": true,
    "dateCreation": "2024-01-10T08:00:00Z",
    "dateModification": null,
    "nomClient": "Marie Martin",
    "numeroFacture": null,
    "libelleUsage": null
  }
]
```

---

## ✅ Modifications Apportées

### 1. Interface IClientFactureRepository

**Fichier :** `Services/Repositories/IClientFactureRepository.cs`

**Méthode ajoutée :**
```csharp
Task<IEnumerable<ClientFacture>> GetBySocieteAnneeMoisWithArrieresAsync(int idSociete, int annees, string mois);
```

### 2. Service ClientFactureService

**Fichier :** `Services/ClientFactureService.cs`

**Méthode implémentée :**
```csharp
public async Task<IEnumerable<ClientFacture>> GetBySocieteAnneeMoisWithArrieresAsync(int idSociete, int annees, string mois)
{
    // Filtre par société, année, mois et MontantDu > MontantPaye
    // Gère les deux chemins : via Facture (factures système) et via Client (arriérés pré-existants)
}
```

### 3. Contrôleur ClientFactureController

**Fichier :** `Controllers/ClientFactureController.cs`

**Endpoint ajouté :**
```csharp
[HttpGet("societe/{idSociete}/annees/{annees}/mois/{mois}")]
public async Task<ActionResult<IEnumerable<ClientFactureDto>>> GetClientFacturesBySocieteAnneeMois(
    int idSociete, 
    int annees, 
    string mois)
```

---

## 🔍 Détails Techniques

### Filtrage par Société

Le filtrage par société utilise une condition OR pour gérer les deux cas :

```csharp
(
    // Chemin 1 : Via Facture (factures système)
    (cf.IdFacture != null && 
     cf.Facture != null && 
     cf.Facture.Usage != null && 
     cf.Facture.Usage.CategorieClient != null && 
     cf.Facture.Usage.CategorieClient.IdSociete == idSociete) ||
    // Chemin 2 : Via Client (arriérés pré-existants)
    (cf.IdFacture == null && 
     cf.Client != null && 
     cf.Client.Axe != null && 
     cf.Client.Axe.Cabine != null && 
     cf.Client.Axe.Cabine.IdSociete == idSociete)
)
```

### Filtrage MontantDu > MontantPaye

```csharp
cf.MontantDu.HasValue &&
cf.MontantPaye.HasValue &&
cf.MontantDu.Value > cf.MontantPaye.Value
```

**Note :** Cette condition filtre les `ClientFacture` où il reste plus d'argent à payer que ce qui a déjà été payé, ce qui indique des arriérés.

---

## ⚠️ Notes Importantes

1. **Format du mois** : Le paramètre `mois` doit correspondre exactement au format stocké en base de données (ex: "01", "02", "Janvier", etc.)

2. **Valeurs nulles** : La requête vérifie que `MontantDu` et `MontantPaye` ne sont pas null avant de comparer

3. **Performance** : Les `Include()` sont utilisés pour charger les relations nécessaires en une seule requête

4. **Soft Delete** : Seules les `ClientFacture` avec `Statut == true` sont retournées

---

## 📝 Exemples d'Utilisation

### Exemple 1 : Récupérer les arriérés de janvier 2024 pour la société 1
```
GET /api/ClientFacture/societe/1/annees/2024/mois/01
```

### Exemple 2 : Récupérer les arriérés de décembre 2023 pour la société 2
```
GET /api/ClientFacture/societe/2/annees/2023/mois/12
```

### Exemple 3 : Récupérer les arriérés de février 2024 pour la société 1
```
GET /api/ClientFacture/societe/1/annees/2024/mois/02
```

---

## ✅ Checklist de Validation

- [x] Interface `IClientFactureRepository` mise à jour
- [x] Méthode `GetBySocieteAnneeMoisWithArrieresAsync` implémentée dans le service
- [x] Endpoint GET créé dans le contrôleur
- [x] Filtrage par société (via deux chemins)
- [x] Filtrage par année et mois
- [x] Filtrage `MontantDu > MontantPaye`
- [x] Inclusion des relations nécessaires (`Include`)
- [x] Tri par `DateEmission` décroissante
- [x] Conversion en DTO pour la réponse
- [x] Code compile sans erreurs

---

**Date d'implémentation :** 2025-01-05  
**Version :** 1.0.0
