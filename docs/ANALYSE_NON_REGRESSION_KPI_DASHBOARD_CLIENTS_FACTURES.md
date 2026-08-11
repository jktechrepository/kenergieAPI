# Analyse de non-régression — KPI Dashboard clients / factures

**Objectif** : documenter le contrat actuel de `GET /api/Dashboard/{idSociete}` **avant** tout alignement de `totalClientsActifs` vs `nombreFacturesMoisPrecedent`.  
**Hors scope** : aucun changement de calcul KPI dans cette note.

**Contexte observé (prod société 1, août 2026)** : `totalClientsActifs = 6703` et `factureMois.nombreFacturesMoisPrecedent = 7028`. L’écart n’est pas une anomalie si on respecte les définitions ci-dessous.

---

## 1. Contrat actuel (référence)

Sources : [`Services/DashboardService.cs`](../Services/DashboardService.cs), [`Services/SocieteClientScopeService.cs`](../Services/SocieteClientScopeService.cs).

| Champ JSON | Calcul | Unité | Périmètre clients |
|------------|--------|-------|-------------------|
| `totalClientsActifs` | `GetActiveClientIdsAsync` : `IsActif` + `Statut` + `ClientUsage` actif, non soft-deleted | **Clients distincts** (stock **aujourd’hui**) | Actif |
| `factureMois.nombreFactures` | `ClientFactures.Count` mois/année courants + `Statut=true` | **Lignes** ClientFacture | Financier |
| `factureMois.nombreFacturesMoisPrecedent` | Idem pour mois précédent | **Lignes** ClientFacture | Financier |
| `factureMois.montantTotalFactures*` | `Sum(MontantDevisePrincipale ?? Montant)` | Montant | Financier |
| `collecteMois.*` / `paiementsDuMois` | Paiements non deleted, période calendaire | Montant / nb paiements | Financier |
| `totalGeneralArriere` | Arriérés consolidés | Montant | Financier |

Périmètres ([`ISocieteClientScopeService`](../Services/ISocieteClientScopeService.cs)) :

- **Actif** : sous-ensemble opérationnel (headcount).
- **Financier** : clients liés à la société (catégories → usages → `ClientUsage`, lien actif **ou non**), hors soft-delete ; **sans** exiger `IsActif` / `Statut` client.

```text
[Clients actifs]  ⊂  [Clients financiers]
       │                      │
       ▼                      ▼
totalClientsActifs     nombreFactures* (lignes)
                       collecte / arriérés / montants
```

**Conséquences métier** :

1. Un client peut avoir **plusieurs** `ClientFacture` le même mois → `nombreFactures` ≥ clients uniques facturés.
2. Des factures du mois M-1 peuvent concerner des clients **inactifs aujourd’hui** → comptées dans `nombreFacturesMoisPrecedent`, exclus de `totalClientsActifs`.
3. Comparer `totalClientsActifs` et `nombreFacturesMoisPrecedent` côte à côte est **trompeur** (unités et temps différents).

---

## 2. Matrice impact consommateur × champ

| Consommateur | `totalClientsActifs` | `nombreFactures*` | Montants collecte / factures / arriérés |
|--------------|----------------------|-------------------|----------------------------------------|
| `GET /api/Dashboard/{id}` ([`DashboardController`](../Controllers/DashboardController.cs)) | Affiché | Affiché | Affichés |
| SignalR [`DashboardHub`](../Hubs/DashboardHub.cs) | Même scope actif | KPIs via scope financier | Scope financier |
| Front Vue / Flutter | Libellé « clients actifs » | Souvent libellé « factures » | Cartes financières |
| [`FinancierDashboardService`](../Services/FinancierDashboardService.cs) | N/A (autre DTO) | Même logique `.Count` lignes | Cohérence à garder si on change le dashboard Admin |
| Tests [`DashboardServiceFinancialStatsTests`](../Tests/DashboardServiceFinancialStatsTests.cs) | Doit rester **strict** (inactifs exclus) | Doit inclure inactifs | Doit inclure inactifs |

---

## 3. Scénarios de correctif futurs — risques de régression

| # | Changement envisagé | Ce qui bouge | Ce qui doit rester stable | Risque |
|---|---------------------|--------------|---------------------------|--------|
| 1 | Remplacer `nombreFactures*` par `COUNT(DISTINCT IdClient)` **sur le même champ** | Compteurs ↓ ; `factureMoyenne` change de sens | Montants, `totalClientsActifs` | Front : libellé « factures » faux ; moyenne trompeuse |
| 2 | Filtrer factures/collecte avec `activeClientIds` | Collecte / factures / arriérés ↓ | — | **Régression forte** vs test inactifs + métier recouvrement |
| 3 | Élargir `totalClientsActifs` au financier | Headcount ↑ | — | « Actifs » ne veut plus dire actifs |
| **R** | **Recommandé** : ajouter `nombreClientsFacturesMois` / `…MoisPrecedent` (DISTINCT), **garder** `nombreFactures*` = lignes | Nouveau champ seulement | Tous les champs existants | Faible ; doc + libellés front |

---

## 4. Requêtes baseline (à exécuter **avant** correctif)

Adapter `@idSociete` et le mois précédent (ex. juillet 2026 → `Mois='07'`, `Annees=2026`). Format mois API : `"01"`…`"12"` (via `NormaliserMois`).

### 4.1 Clients actifs (équivalent `GetActiveClientIdsAsync`)

```sql
-- Remplacer @idSociete
SELECT COUNT(DISTINCT c.IdClient) AS total_clients_actifs
FROM Clients c
INNER JOIN ClientUsages cu ON cu.IdClient = c.IdClient AND cu.Statut = 1
INNER JOIN Usages u ON u.IdUsage = cu.IdUsage AND u.Statut = 1
INNER JOIN CategorieClients cc ON cc.IdCategorie = u.IdCategorieClient
  AND cc.IdSociete = @idSociete AND (cc.Statut IS NULL OR cc.Statut <> 0)
WHERE c.IsActif = 1
  AND c.Statut = 1
  AND (c.IsDeleted IS NULL OR c.IsDeleted = 0);
```

### 4.2 Clients financiers (équivalent `GetFinancialClientIdsAsync`)

```sql
SELECT COUNT(DISTINCT c.IdClient) AS total_clients_financiers
FROM Clients c
INNER JOIN ClientUsages cu ON cu.IdClient = c.IdClient
INNER JOIN Usages u ON u.IdUsage = cu.IdUsage AND u.Statut = 1
INNER JOIN CategorieClients cc ON cc.IdCategorie = u.IdCategorieClient
  AND cc.IdSociete = @idSociete AND (cc.Statut IS NULL OR cc.Statut <> 0)
WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0);
```

### 4.3 Factures mois précédent — lignes vs clients distincts

```sql
-- @moisPrec = '07', @anneePrec = 2026 (exemple)
SELECT
  COUNT(*) AS nombre_lignes_clientfacture,
  COUNT(DISTINCT cf.IdClient) AS nombre_clients_distincts_factures
FROM ClientFactures cf
WHERE cf.Statut = 1
  AND cf.Mois = @moisPrec
  AND cf.Annees = @anneePrec
  AND cf.IdClient IN (
    SELECT DISTINCT c.IdClient
    FROM Clients c
    INNER JOIN ClientUsages cu ON cu.IdClient = c.IdClient
    INNER JOIN Usages u ON u.IdUsage = cu.IdUsage AND u.Statut = 1
    INNER JOIN CategorieClients cc ON cc.IdCategorie = u.IdCategorieClient
      AND cc.IdSociete = @idSociete AND (cc.Statut IS NULL OR cc.Statut <> 0)
    WHERE (c.IsDeleted IS NULL OR c.IsDeleted = 0)
  );
```

### 4.4 Écarts à noter dans le snapshot

| Métrique | Formule |
|----------|---------|
| Multi-lignes / client | `nombre_lignes - nombre_clients_distincts_factures` |
| Inactifs (ou hors actifs) encore facturés | approx. comparaison distincts facturés vs `total_clients_actifs` (attention : stocks vs flux) |

Snapshot API recommandé :

```http
GET /api/Dashboard/1
Authorization: Bearer {jwt}
```

Conserver : `totalClientsActifs`, `factureMois.nombreFactures`, `nombreFacturesMoisPrecedent`, montants collecte / factures / `totalGeneralArriere`.

---

## 5. Checklist QA non-régression

### Avant tout correctif

- [ ] Exécuter les SQL §4 et archiver les résultats (société cible)
- [ ] Archiver le JSON `GET /api/Dashboard/{id}`
- [ ] Lancer `DashboardServiceFinancialStatsTests` (doivent être verts)

```bash
dotnet test Tests/Kenergie.Tests.csproj --filter "FullyQualifiedName~DashboardServiceFinancialStatsTests"
```

### Contrat à ne pas casser (tests existants)

| Test | Garantie |
|------|----------|
| `GetDashboardDataAsync_IncludesInactiveClientInFinancialKpis_ButNotInHeadcount` | Inactif **exclu** de `TotalClientsActifs` ; **inclus** dans collecte + `NombreFactures` + montants factures |
| `GetDashboardDataAsync_IncludesClientStatutFalse_WithInactiveClientUsageLink` | Client `Statut=false` + usage inactif reste dans collecte ; headcount actifs = 1 |
| `GetDashboardDataAsync_TotalGeneralArriere_ScopedToSocieteClients` | Arriéré limité à la société (pas de fuite hors société) |

### Après un éventuel correctif (phase suivante)

- [ ] Montants (`collecteMois`, `montantTotalFactures*`, `totalGeneralArriere`) **identiques** au snapshot
- [ ] `totalClientsActifs` **identique** (sauf si change volontaire documenté)
- [ ] `nombreFactures*` **identique** si on a ajouté un **nouveau** champ DISTINCT
- [ ] Financier dashboard aligné si la même notion y est exposée
- [ ] Front : libellés mis à jour pour tout nouveau champ
- [ ] Retester `DashboardServiceFinancialStatsTests`

---

## 6. Recommandation pour la phase correctif (ultérieure)

1. **Ne pas** réécrire `nombreFactures` / `nombreFacturesMoisPrecedent` (garder = volume de lignes).
2. Ajouter des champs explicites, ex. :
   - `nombreClientsFacturesMois`
   - `nombreClientsFacturesMoisPrecedent`
3. Documenter dans la doc dashboard + guide front.
4. Aligner éventuellement `FinancierDashboardService` si le même besoin métier existe.

---

## 7. Références code

- [`Services/DashboardService.cs`](../Services/DashboardService.cs) — `GetDashboardDataAsync`, `GetFactureMoisAsync`
- [`Services/SocieteClientScopeService.cs`](../Services/SocieteClientScopeService.cs)
- [`Models/DTOs/DashboardDto.cs`](../Models/DTOs/DashboardDto.cs) — `FactureMoisDto`
- [`Tests/DashboardServiceFinancialStatsTests.cs`](../Tests/DashboardServiceFinancialStatsTests.cs)
- [`docs/API_DOCUMENTATION_STATISTIQUES.md`](./API_DOCUMENTATION_STATISTIQUES.md) — scopes actif / financier
