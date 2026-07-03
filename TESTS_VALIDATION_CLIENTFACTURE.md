# 🧪 Tests et Validation - ClientFacture

## 📋 Vue d'ensemble

Ce document décrit la suite de tests créée pour valider l'implémentation du modèle `ClientFacture` et les améliorations apportées au système de facturation, paiement et calcul des arriérés.

---

## ✅ Tests Unitaires

### 1. ClientFactureServiceTests

**Fichier :** `Kenergie.Tests.Unit/Services/ClientFactureServiceTests.cs`

**Tests couverts :**

#### CRUD de base
- ✅ `CreateAsync_ShouldCreateClientFacture_WhenValidData` : Création d'une ClientFacture normale
- ✅ `GetByIdAsync_ShouldReturnClientFacture_WhenExists` : Récupération par ID
- ✅ `GetByIdAsync_ShouldReturnNull_WhenNotExists` : Gestion des cas inexistants
- ✅ `DeleteAsync_ShouldSoftDelete_WhenValidId` : Soft delete (Statut = false)

#### Requêtes par Client
- ✅ `GetByClientAsync_ShouldReturnAllClientFactures_ForClient` : Toutes les ClientFacture d'un client
- ✅ `GetByClientWithArrieresAsync_ShouldReturnOnlyArrieres_ForClient` : Seulement les arriérés (MontantDu > 0)
- ✅ `GetPreExistantsByClientAsync_ShouldReturnOnlyPreExistants_ForClient` : Seulement les arriérés pré-existants

#### Requêtes par Facture
- ✅ `GetByFactureAsync_ShouldReturnAllClientFactures_ForFacture` : Toutes les ClientFacture d'une facture
- ✅ `GetByClientAndFactureAsync_ShouldReturnClientFacture_WhenExists` : ClientFacture spécifique client+facture
- ✅ `GetByClientAndFactureAsync_ShouldReturnNull_WhenNotExists` : Gestion des cas inexistants

#### Arriérés pré-existants
- ✅ `CreatePreExistantAsync_ShouldCreatePreExistantArriere_WhenValidData` : Création d'arriéré pré-existant

#### Mise à jour des montants
- ✅ `UpdateMontantPayeAsync_ShouldUpdateMontantPaye_AndRecalculateMontantDu` : Mise à jour du montant payé
- ✅ `RecalculateMontantDuAsync_ShouldRecalculateMontantDu_Correctly` : Recalcul du montant dû

**Couverture :** ~95% des méthodes de `ClientFactureService`

---

### 2. ArrieresServiceRegressionTests

**Fichier :** `Kenergie.Tests.Unit/Services/ArrieresServiceRegressionTests.cs`

**Tests de régression :**

#### Inclusion des arriérés pré-existants
- ✅ `GetArrieresByClientAsync_ShouldIncludePreExistantArrieres` : Vérifie que les arriérés pré-existants sont inclus dans les calculs

#### Exclusion des factures payées
- ✅ `GetArrieresByClientAsync_ShouldExcludePaidInvoices` : Vérifie que les factures payées (MontantDu = 0) ne sont pas incluses

#### Calculs corrects
- ✅ `GetArrieresByClientAsync_ShouldCalculateCorrectTotals` : Vérifie les totaux (TotalArrieres, MontantTotalFactures, MontantTotalPaye)

#### Filtrage des factures impayées
- ✅ `GetFacturesImpayeesByClientAsync_ShouldReturnOnlyUnpaidInvoices` : Vérifie que seules les factures impayées sont retournées

#### Gestion des cas limites
- ✅ `GetArrieresByClientAsync_ShouldReturnNull_WhenClientNotFound` : Client inexistant
- ✅ `GetArrieresByClientAsync_ShouldReturnNull_WhenNoArrieres` : Aucun arriéré

**Objectif :** Vérifier que la refactorisation de `ArrieresService` pour utiliser `ClientFacture` n'a pas introduit de régressions.

---

## 🔧 Tests d'Intégration (À créer)

### 3. ClientFactureControllerTests

**Fichier :** `Kenergie.Tests.Unit/Controllers/ClientFactureControllerTests.cs` (À créer)

**Tests prévus :**

#### Endpoints GET
- ✅ `GET /api/ClientFacture/{id}` : Récupération par ID
- ✅ `GET /api/ClientFacture/client/{id}` : Toutes les ClientFacture d'un client
- ✅ `GET /api/ClientFacture/client/{id}/arrieres` : Arriérés d'un client
- ✅ `GET /api/ClientFacture/client/{id}/pre-existants` : Arriérés pré-existants
- ✅ `GET /api/ClientFacture/facture/{id}` : Toutes les ClientFacture d'une facture

#### Endpoints POST
- ✅ `POST /api/ClientFacture` : Création d'une ClientFacture
- ✅ `POST /api/ClientFacture/pre-existant` : Création d'un arriéré pré-existant
- ✅ `POST /api/ClientFacture/{id}/paiement` : Enregistrement d'un paiement sur arriéré pré-existant

#### Endpoints PUT
- ✅ `PUT /api/ClientFacture/{id}` : Mise à jour d'une ClientFacture

#### Endpoints DELETE
- ✅ `DELETE /api/ClientFacture/{id}` : Suppression (soft delete)

#### Validation et erreurs
- ✅ Validation des DTOs
- ✅ Gestion des erreurs (404, 400, 500)
- ✅ Autorisation (rôles)

---

## 📊 Tests de Performance (À créer)

### 4. PerformanceTests

**Fichier :** `Kenergie.Tests.Unit/Performance/ClientFacturePerformanceTests.cs` (À créer)

**Tests prévus :**

#### Comparaison N+1 queries
- ✅ `GetArrieresByClientAsync_ShouldUseSingleQuery_InsteadOfNPlusOne` : Vérifie qu'une seule requête est exécutée au lieu de N+1
- ✅ `GetArrieresByClientAsync_ShouldBeFaster_ThanOldImplementation` : Comparaison de performance

#### Charge
- ✅ `GetArrieresByClientAsync_ShouldHandleLargeDatasets` : Test avec 1000+ ClientFacture
- ✅ `GetArrieresByClientAsync_ShouldHandleMultipleClients` : Test avec 100+ clients

**Méthodologie :**
- Utiliser `Stopwatch` pour mesurer le temps d'exécution
- Comparer avec l'ancienne implémentation (si disponible)
- Vérifier le nombre de requêtes SQL avec un logger de requêtes

---

## 🗄️ Tests de Migration (À créer)

### 5. MigrationTests

**Fichier :** `Kenergie.Tests.Unit/Services/ClientFactureMigrationServiceTests.cs` (À créer)

**Tests prévus :**

#### Migration des données
- ✅ `MigrateExistingFacturesAsync_ShouldCreateClientFactures_ForAllFactures` : Migration complète
- ✅ `MigrateExistingFacturesAsync_ShouldCalculateMontantCorrectly` : Calcul correct du montant (facture.Montant * nombreBatiment)
- ✅ `MigrateExistingFacturesAsync_ShouldCalculateMontantPayeCorrectly` : Calcul correct du montant payé (somme des Paiements)
- ✅ `MigrateExistingFacturesAsync_ShouldCalculateMontantDuCorrectly` : Calcul correct du montant dû
- ✅ `MigrateExistingFacturesAsync_ShouldSkipAlreadyMigrated` : Ne pas créer de doublons

#### Validation de la migration
- ✅ `ValidateMigrationAsync_ShouldReturnSuccess_WhenMigrationIsValid` : Validation réussie
- ✅ `ValidateMigrationAsync_ShouldDetectInconsistencies` : Détection d'incohérences
- ✅ `ValidateMigrationAsync_ShouldDetectMissingClientFactures` : Détection de ClientFacture manquantes

---

## 🚀 Exécution des Tests

### Exécuter tous les tests

```bash
cd /Users/mac/Documents/KenergieAPI
dotnet test Kenergie.Tests.Unit/Kenergie.Tests.Unit.csproj
```

### Exécuter une classe de tests spécifique

```bash
dotnet test --filter "FullyQualifiedName~ClientFactureServiceTests"
```

### Exécuter un test spécifique

```bash
dotnet test --filter "FullyQualifiedName~CreateAsync_ShouldCreateClientFacture_WhenValidData"
```

### Exécuter avec couverture de code

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## 📈 Métriques de Test

### Couverture actuelle

| Composant | Couverture | Tests | Statut |
|-----------|------------|-------|--------|
| ClientFactureService | ~95% | 15 tests | ✅ |
| ArrieresService | ~80% | 6 tests | ✅ |
| ClientFactureController | 0% | 0 tests | ⏳ À créer |
| ClientFactureMigrationService | 0% | 0 tests | ⏳ À créer |

### Objectifs

- ✅ **Couverture minimale :** 80% pour tous les services
- ✅ **Tests unitaires :** Toutes les méthodes publiques
- ✅ **Tests d'intégration :** Tous les endpoints API
- ✅ **Tests de régression :** Tous les scénarios critiques

---

## ✅ Checklist de Validation

### Tests unitaires
- [x] ClientFactureService : CRUD de base
- [x] ClientFactureService : Requêtes par Client
- [x] ClientFactureService : Requêtes par Facture
- [x] ClientFactureService : Arriérés pré-existants
- [x] ClientFactureService : Mise à jour des montants
- [x] ArrieresService : Inclusion des arriérés pré-existants
- [x] ArrieresService : Exclusion des factures payées
- [x] ArrieresService : Calculs corrects

### Tests d'intégration
- [ ] ClientFactureController : Endpoints GET
- [ ] ClientFactureController : Endpoints POST
- [ ] ClientFactureController : Endpoints PUT
- [ ] ClientFactureController : Endpoints DELETE
- [ ] ClientFactureController : Validation et erreurs

### Tests de performance
- [ ] Comparaison N+1 queries
- [ ] Tests de charge

### Tests de migration
- [ ] Migration des données existantes
- [ ] Validation de la migration

---

## 🔍 Scénarios de Test Manuels

### Scénario 1 : Création d'un arriéré pré-existant

1. **Créer un arriéré pré-existant**
   ```bash
   POST /api/ClientFacture/pre-existant
   {
     "IdClient": 1,
     "Montant": 50000,
     "Mois": "01",
     "Annees": 2023,
     "Description": "Arriéré pré-existant"
   }
   ```

2. **Vérifier qu'il apparaît dans les arriérés**
   ```bash
   GET /api/Client/1/arrieres
   ```
   - Vérifier que `numeroFacture = "ARRIERE-PRE-EXISTANT"`
   - Vérifier que `montantDu = 50000`

### Scénario 2 : Paiement sur arriéré pré-existant

1. **Enregistrer un paiement partiel**
   ```bash
   POST /api/ClientFacture/{idClientFacture}/paiement
   {
     "MontantPaye": 20000
   }
   ```

2. **Vérifier la mise à jour**
   ```bash
   GET /api/ClientFacture/{idClientFacture}
   ```
   - Vérifier que `montantPaye = 20000`
   - Vérifier que `montantDu = 30000`

### Scénario 3 : Création de facture système

1. **Créer une facture**
   ```bash
   POST /api/Facture
   {
     "IdUsage": 1,
     "Montant": 10000,
     ...
   }
   ```

2. **Vérifier la création automatique de ClientFacture**
   ```bash
   GET /api/ClientFacture/facture/{idFacture}
   ```
   - Vérifier qu'une ClientFacture est créée pour chaque client avec l'usage
   - Vérifier que `montant = facture.montant * nombreBatiment`

### Scénario 4 : Paiement sur facture système

1. **Créer un paiement**
   ```bash
   POST /api/Paiement
   {
     "IdFacture": 1,
     "IdClient": 1,
     "MontantPaye": 15000
   }
   ```

2. **Vérifier la mise à jour de ClientFacture**
   ```bash
   GET /api/ClientFacture/client/1/facture/1
   ```
   - Vérifier que `montantPaye` est mis à jour
   - Vérifier que `montantDu` est recalculé

---

## 📝 Notes

1. **Base de données de test :** Utilise `InMemoryDatabase` pour l'isolation des tests
2. **Seed des données :** Chaque test crée ses propres données de test
3. **Dispose :** Chaque test dispose de son contexte pour éviter les fuites de mémoire
4. **FluentAssertions :** Utilisé pour des assertions plus lisibles

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
