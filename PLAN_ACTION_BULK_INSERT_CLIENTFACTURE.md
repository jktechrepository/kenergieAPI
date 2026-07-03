# 📋 Plan d'Action : Bulk Insert ClientFacture depuis Excel

## 🎯 Objectif

Créer un endpoint pour permettre l'import en masse de `ClientFacture` (arriérés pré-existants) depuis un fichier Excel, en utilisant le `CodeCons` pour récupérer l'`IdClient`.

**Date :** 2025-01-05  
**Priorité :** Moyenne

---

## 📊 Analyse de l'Existant

### ✅ Points Positifs

1. **Service Excel existant** : `ExcelClientService` fournit un excellent modèle
2. **Méthode de création** : `CreatePreExistantAsync` existe déjà
3. **Récupération par CodeCons** : `GetByCodeConsAsync` est disponible
4. **Gestion des erreurs** : Table `clientsCrashed` pour stocker les erreurs

### 📝 Structure Excel Proposée

**Colonnes requises :**
- `CodeCons` : Code consommateur (obligatoire) - utilisé pour récupérer `IdClient`
- `Montant` : Montant de l'arriéré (obligatoire)
- `Mois` : Mois d'émission (obligatoire) - format "01", "02", ..., "12"
- `Annees` : Année d'émission (obligatoire) - ex: 2025
- `DateEmission` : Date d'émission (optionnel) - format date
- `Description` : Description/libellé (optionnel)

**Exemple de données :**
```
CodeCons    | Montant  | Mois | Annees | DateEmission | Description
B/b1/0001   | 100000   | 9    | 2025   | 2025-09-15  | Arriéré septembre
A/a1/0002   | 50000    | 8    | 2025   | 2025-08-10  | Arriéré août
```

---

## 📋 Plan d'Action en 6 Phases

### Phase 1 : Création des DTOs (0.5 jour)

#### 1.1. DTO pour les données brutes Excel
**Fichier :** `Models/DTOs/ClientFacture/ClientFactureExcelRaw.cs`

```csharp
public class ClientFactureExcelRaw
{
    public string? CodeCons { get; set; }
    public string? Montant { get; set; }
    public string? Mois { get; set; }
    public string? Annees { get; set; }
    public string? DateEmission { get; set; }
    public string? Description { get; set; }
}
```

#### 1.2. DTO enrichi avec validation
**Fichier :** `Models/DTOs/ClientFacture/ClientFactureExcelDto.cs`

```csharp
public class ClientFactureExcelDto
{
    public int NumeroLigne { get; set; }
    public string? CodeCons { get; set; }
    public int? IdClient { get; set; } // Récupéré depuis CodeCons
    public decimal? Montant { get; set; }
    public string? Mois { get; set; }
    public int? Annees { get; set; }
    public DateTime? DateEmission { get; set; }
    public string? Description { get; set; }
    public List<string> Erreurs { get; set; } = new List<string>();
}
```

#### 1.3. DTO de résultat
**Fichier :** `Models/DTOs/ClientFacture/BulkClientFactureResult.cs`

```csharp
public class BulkClientFactureResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalLignes { get; set; }
    public int LignesReussies { get; set; }
    public int LignesEchouees { get; set; }
    public List<ClientFactureCree> ClientFacturesCrees { get; set; } = new List<ClientFactureCree>();
    public List<LigneErreurClientFacture> LignesAvecErreurs { get; set; } = new List<LigneErreurClientFacture>();
}

public class ClientFactureCree
{
    public bool Success { get; set; }
    public int? IdClientFacture { get; set; }
    public string? CodeCons { get; set; }
    public string? Message { get; set; }
}

public class LigneErreurClientFacture
{
    public int NumeroLigne { get; set; }
    public string? CodeCons { get; set; }
    public List<string> Erreurs { get; set; } = new List<string>();
}
```

**Checklist :**
- [ ] Créer `ClientFactureExcelRaw.cs`
- [ ] Créer `ClientFactureExcelDto.cs`
- [ ] Créer `BulkClientFactureResult.cs`
- [ ] Créer `ClientFactureCree.cs`
- [ ] Créer `LigneErreurClientFacture.cs`

---

### Phase 2 : Création du Service Excel (2 jours)

#### 2.1. Créer `ExcelClientFactureService`
**Fichier :** `Services/ExcelClientFactureService.cs`

**Méthodes principales :**
1. `ProcessExcelFileAsync(IFormFile file)` : Point d'entrée principal
2. `ValidateFile(IFormFile file)` : Validation du fichier
3. `ReadExcelFileAsync(IFormFile file)` : Lecture du fichier Excel
4. `ConvertToClientFactureExcelDtoAsync(List<ClientFactureExcelRaw> rawData)` : Conversion et enrichissement
5. `ValidateClientFactures(List<ClientFactureExcelDto> data)` : Validation des données
6. `ProcessBatchesAsync(List<ClientFactureExcelDto> lignesValides, BulkClientFactureResult result)` : Traitement par lots
7. `GenerateTemplate()` : Génération d'un template Excel

**Logique de conversion :**
- Utiliser `GetByCodeConsAsync(codeCons)` pour récupérer `IdClient`
- Valider que le client existe
- Convertir `Mois` en format "01"-"12"
- Convertir `Annees` en int
- Convertir `Montant` en decimal
- Parser `DateEmission` si fourni

**Checklist :**
- [ ] Créer la classe `ExcelClientFactureService`
- [ ] Implémenter `ProcessExcelFileAsync`
- [ ] Implémenter `ValidateFile`
- [ ] Implémenter `ReadExcelFileAsync`
- [ ] Implémenter `ConvertToClientFactureExcelDtoAsync` avec récupération de `IdClient` via `CodeCons`
- [ ] Implémenter `ValidateClientFactures`
- [ ] Implémenter `ProcessBatchesAsync` avec création via `CreatePreExistantAsync`
- [ ] Implémenter `GenerateTemplate`
- [ ] Gestion des erreurs et stockage dans `clientsCrashed` (si applicable)

---

### Phase 3 : Création de l'Endpoint (0.5 jour)

#### 3.1. Endpoint Bulk Insert
**Fichier :** `Controllers/ClientFactureController.cs`

**Endpoint :** `POST /api/ClientFacture/bulk-excel`

**Paramètres :**
- `file` : Fichier Excel (IFormFile)
- `idSociete` : ID de la société (optionnel, pour validation)

**Réponse :** `BulkClientFactureResult`

**Implémentation :**
```csharp
[HttpPost("bulk-excel")]
[Authorize(Roles = "Super-Admin,Admin")]
public async Task<ActionResult<BulkClientFactureResult>> BulkInsertFromExcel(
    IFormFile file,
    [FromQuery] int? idSociete = null)
{
    if (file == null || file.Length == 0)
    {
        return BadRequest(new { message = "Le fichier Excel est requis" });
    }

    var result = await _excelClientFactureService.ProcessExcelFileAsync(file, idSociete);
    
    if (result.Success)
    {
        return Ok(result);
    }
    else
    {
        return BadRequest(result);
    }
}
```

#### 3.2. Endpoint Template Excel
**Endpoint :** `GET /api/ClientFacture/template-excel`

**Réponse :** Fichier Excel (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)

**Checklist :**
- [ ] Ajouter l'endpoint `POST /api/ClientFacture/bulk-excel`
- [ ] Ajouter l'endpoint `GET /api/ClientFacture/template-excel`
- [ ] Injecter `ExcelClientFactureService` dans le controller
- [ ] Gérer les erreurs
- [ ] Ajouter les commentaires XML

---

### Phase 4 : Gestion des Erreurs (0.5 jour)

#### 4.1. Stockage des Erreurs
**Option A :** Utiliser la table `clientsCrashed` existante
- Adapter pour stocker aussi les erreurs de `ClientFacture`

**Option B :** Créer une table `clientFacturesCrashed`
- Structure similaire à `clientsCrashed`
- Stocker les données brutes et les erreurs

**Recommandation :** Option A (réutiliser `clientsCrashed`)

#### 4.2. Types d'Erreurs à Gérer
- CodeCons manquant ou invalide
- Client non trouvé pour le CodeCons
- Montant invalide (négatif, format incorrect)
- Mois invalide (doit être "01"-"12")
- Annees invalide (hors plage raisonnable)
- DateEmission invalide (format incorrect)
- Doublons (même CodeCons + Mois + Annees)

**Checklist :**
- [ ] Adapter `SaveCrashedClientAsync` pour gérer aussi les erreurs de ClientFacture
- [ ] Ou créer `SaveCrashedClientFactureAsync`
- [ ] Gérer tous les types d'erreurs
- [ ] Tester avec des données invalides

---

### Phase 5 : Tests (1 jour)

#### 5.1. Tests Unitaires
- [ ] Test de validation du fichier
- [ ] Test de lecture Excel
- [ ] Test de conversion avec CodeCons valide
- [ ] Test de conversion avec CodeCons invalide
- [ ] Test de validation des données
- [ ] Test de création par lots
- [ ] Test avec fichier vide
- [ ] Test avec fichier invalide

#### 5.2. Tests d'Intégration
- [ ] Test de l'endpoint avec fichier valide
- [ ] Test de l'endpoint avec fichier invalide
- [ ] Test de l'endpoint template
- [ ] Test avec données réelles

**Checklist :**
- [ ] Créer les tests unitaires
- [ ] Créer les tests d'intégration
- [ ] Tester tous les scénarios
- [ ] Valider les performances avec gros fichiers

---

### Phase 6 : Documentation (0.5 jour)

#### 6.1. Documentation API
- [ ] Documenter l'endpoint `POST /api/ClientFacture/bulk-excel`
- [ ] Documenter l'endpoint `GET /api/ClientFacture/template-excel`
- [ ] Exemples de requêtes/réponses
- [ ] Codes d'erreur

#### 6.2. Guide d'Utilisation
- [ ] Format du fichier Excel
- [ ] Exemples de données
- [ ] Gestion des erreurs
- [ ] Bonnes pratiques

**Checklist :**
- [ ] Créer la documentation API
- [ ] Créer un guide d'utilisation
- [ ] Ajouter des exemples

---

## 🔍 Détails Techniques

### Structure du Fichier Excel

**En-têtes (ligne 1 ou 2) :**
- CodeCons
- Montant
- Mois
- Annees
- DateEmission (optionnel)
- Description (optionnel)

**Format des données :**
- `CodeCons` : Texte (ex: "B/b1/0001")
- `Montant` : Nombre (ex: 100000)
- `Mois` : Texte ou nombre (ex: "9" ou "09" → converti en "09")
- `Annees` : Nombre (ex: 2025)
- `DateEmission` : Date (ex: "2025-09-15" ou format Excel)
- `Description` : Texte (optionnel)

---

### Logique de Validation

#### 1. Validation du CodeCons
- Vérifier que le CodeCons n'est pas vide
- Récupérer le client via `GetByCodeConsAsync`
- Si client non trouvé → erreur

#### 2. Validation du Montant
- Vérifier que le montant est présent
- Vérifier que le montant est > 0
- Vérifier le format (decimal)

#### 3. Validation du Mois
- Vérifier que le mois est présent
- Convertir en format "01"-"12"
- Accepter : "1", "01", "9", "09", "12"

#### 4. Validation de l'Année
- Vérifier que l'année est présente
- Vérifier que l'année est entre 2000 et 2100

#### 5. Validation de la DateEmission
- Si fournie, vérifier le format
- Si non fournie, utiliser la date système

#### 6. Détection des Doublons
- Vérifier si une `ClientFacture` existe déjà pour :
  - Même `IdClient`
  - Même `Mois`
  - Même `Annees`
  - `EstArrierePreExistant = true`

---

### Traitement par Lots

**Stratégie :**
- Traiter par lots de 50 lignes
- Utiliser des transactions pour garantir la cohérence
- En cas d'erreur dans un lot, continuer avec le lot suivant
- Stocker toutes les erreurs

**Performance :**
- Charger tous les clients en mémoire au début (par CodeCons)
- Créer un dictionnaire pour lookup rapide
- Éviter les requêtes N+1

---

## ⚠️ Points d'Attention

### 1. Performance
- ⚠️ Charger tous les clients en mémoire au début pour éviter N+1 queries
- ⚠️ Utiliser des transactions pour les lots
- ⚠️ Limiter la taille du fichier (10 MB max)

### 2. Gestion des Erreurs
- ⚠️ Ne pas faire échouer tout l'import si une ligne échoue
- ⚠️ Stocker toutes les erreurs pour correction ultérieure
- ⚠️ Retourner un rapport détaillé

### 3. Doublons
- ⚠️ Détecter les doublons dans le fichier
- ⚠️ Détecter les doublons avec les données existantes
- ⚠️ Permettre de choisir : ignorer, mettre à jour, ou créer quand même

### 4. Validation
- ⚠️ Valider toutes les données avant insertion
- ⚠️ Fournir des messages d'erreur clairs
- ⚠️ Indiquer le numéro de ligne pour chaque erreur

---

## 📊 Estimation du Temps

| Phase | Durée | Responsable |
|-------|-------|-------------|
| Phase 1 : DTOs | 0.5 jour | Backend |
| Phase 2 : Service Excel | 2 jours | Backend |
| Phase 3 : Endpoint | 0.5 jour | Backend |
| Phase 4 : Gestion Erreurs | 0.5 jour | Backend |
| Phase 5 : Tests | 1 jour | Backend |
| Phase 6 : Documentation | 0.5 jour | Backend |
| **TOTAL** | **5 jours** | |

---

## ✅ Checklist Globale

### Backend
- [ ] Phase 1 : DTOs créés
- [ ] Phase 2 : Service Excel implémenté
- [ ] Phase 3 : Endpoints créés
- [ ] Phase 4 : Gestion des erreurs implémentée
- [ ] Phase 5 : Tests passent
- [ ] Phase 6 : Documentation créée
- [ ] Code review
- [ ] Déploiement en staging
- [ ] Tests en staging
- [ ] Déploiement en production

---

## 🎯 Résultat Attendu

### Endpoint Disponible
- `POST /api/ClientFacture/bulk-excel` : Import en masse depuis Excel
- `GET /api/ClientFacture/template-excel` : Téléchargement du template

### Format de Réponse
```json
{
  "success": true,
  "message": "Traitement terminé : 100 ClientFacture(s) créé(s) sur 105 ligne(s), 5 échouée(s)",
  "totalLignes": 105,
  "lignesReussies": 100,
  "lignesEchouees": 5,
  "clientFacturesCrees": [...],
  "lignesAvecErreurs": [...]
}
```

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0  
**Statut :** 📋 Plan prêt pour implémentation
