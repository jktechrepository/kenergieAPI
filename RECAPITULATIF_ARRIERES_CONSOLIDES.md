# ✅ Récapitulatif : Implémentation Arriérés Consolidés

## 📋 Résumé

Implémentation de l'endpoint `/api/ClientFacture/client/{idClient}/arrieres-consolides` pour retourner les arriérés d'un client groupés par période (mois/année) avec totaux consolidés.

**Date :** 2025-01-05  
**Statut :** ✅ **Implémentation terminée**

---

## ✅ Fichiers Créés

### 1. DTOs
- ✅ `Models/DTOs/ClientFacture/ArrieresConsolidesResponseDto.cs`
- ✅ `Models/DTOs/ClientFacture/ArriereParPeriodeDto.cs`

---

## 📝 Fichiers Modifiés

### 1. Interface Repository
**Fichier :** `Services/Repositories/IClientFactureRepository.cs`

**Ajout :**
```csharp
/// <summary>
/// ✨ NOUVEAU : Récupère les arriérés d'un client groupés par période (mois/année) avec totaux consolidés
/// Seules les factures avec MontantDu > 0 sont incluses
/// </summary>
Task<ArrieresConsolidesResponseDto> GetArrieresConsolidesByClientAsync(int idClient);
```

---

### 2. Service
**Fichier :** `Services/ClientFactureService.cs`

**Méthode ajoutée :** `GetArrieresConsolidesByClientAsync(int idClient)`

**Logique :**
1. Récupère toutes les `ClientFacture` du client avec `MontantDu > 0`
2. Groupe par période (Mois/Annees)
3. Pour chaque groupe :
   - Calcule `MontantTotal`, `MontantPayeTotal`, `MontantDuTotal`
   - Compte `NombreFactures` et `NombreUsages`
   - Récupère `DateEmission` (la plus récente)
   - Convertit chaque `ClientFacture` en `ClientFactureDto`
4. Crée `ArrieresConsolidesResponseDto` avec les informations du client
5. Retourne le résultat

---

### 3. Controller
**Fichier :** `Controllers/ClientFactureController.cs`

**Endpoint ajouté :**
```csharp
// GET: api/ClientFacture/client/{idClient}/arrieres-consolides
[HttpGet("client/{idClient}/arrieres-consolides")]
[Authorize]
public async Task<ActionResult<ArrieresConsolidesResponseDto>> GetArrieresConsolidesByClient(int idClient)
```

**Fonctionnalités :**
- Vérifie l'existence du client
- Appelle la méthode du repository
- Retourne la réponse consolidée

---

## 📊 Structure de Réponse

### Format JSON

```json
{
  "idClient": 1,
  "nomClient": "Kalambayi Jonathan",
  "codeCons": "B/b1/0001",
  "arrieresParPeriode": [
    {
      "mois": "01",
      "annees": 2026,
      "nombreUsages": 3,
      "nombreFactures": 3,
      "dateEmission": "2026-01-15",
      "montantTotal": 45000,
      "montantPayeTotal": 0,
      "montantDuTotal": 45000,
      "detailFactures": [
        {
          "idClientFacture": 1,
          "idFacture": 1,
          "idClient": 1,
          "montant": 5000,
          "nombreBatiment": 1,
          "montantPaye": 0,
          "montantDu": 5000,
          "mois": "01",
          "annees": 2026,
          "dateEmission": "2026-01-15T00:00:00",
          "estArrierePreExistant": false,
          "description": null,
          "statut": true,
          "dateCreation": "2026-01-15T07:56:35.642782",
          "dateModification": null,
          "nomClient": "Kalambayi Jonathan",
          "numeroFacture": "FAC-DOM-0126-0001",
          "libelleUsage": "DOMESTIQUE"
        }
      ]
    }
  ]
}
```

---

## 🔍 Différences avec l'Ancien Endpoint

### Ancien Endpoint : `/api/ClientFacture/client/{idClient}/arrieres`

**Format :** Tableau simple de `ClientFactureDto[]`
```json
[
  {
    "idClientFacture": 1,
    "montantDu": 5000,
    ...
  },
  {
    "idClientFacture": 2,
    "montantDu": 10000,
    ...
  }
]
```

### Nouvel Endpoint : `/api/ClientFacture/client/{idClient}/arrieres-consolides`

**Format :** Objet avec groupement par période
```json
{
  "idClient": 1,
  "nomClient": "...",
  "arrieresParPeriode": [
    {
      "mois": "01",
      "annees": 2026,
      "montantDuTotal": 45000,
      "detailFactures": [...]
    }
  ]
}
```

**Avantages :**
- ✅ Groupement par période
- ✅ Totaux consolidés
- ✅ Informations enrichies (nombreUsages, nombreFactures)
- ✅ Format cohérent avec `/consolidee/mois/{mois}/annee/{annee}`

---

## ✅ Checklist de Validation

- [x] DTOs créés (`ArrieresConsolidesResponseDto`, `ArriereParPeriodeDto`)
- [x] Méthode ajoutée dans `IClientFactureRepository`
- [x] Méthode implémentée dans `ClientFactureService`
- [x] Endpoint créé dans `ClientFactureController`
- [x] Vérification de l'existence du client
- [x] Filtrage des arriérés (MontantDu > 0)
- [x] Groupement par période
- [x] Calcul des totaux consolidés
- [x] Comptage des factures et usages
- [x] Conversion en DTOs
- [x] Gestion du cas sans arriérés
- [x] Pas d'erreurs de compilation (linter)

---

## 🚀 Utilisation

### Exemple de Requête

```http
GET /api/ClientFacture/client/1/arrieres-consolides
Authorization: Bearer {token}
```

### Exemple de Réponse (Client avec arriérés)

```json
{
  "idClient": 1,
  "nomClient": "Kalambayi Jonathan",
  "codeCons": "B/b1/0001",
  "arrieresParPeriode": [
    {
      "mois": "01",
      "annees": 2026,
      "nombreUsages": 3,
      "nombreFactures": 3,
      "dateEmission": "2026-01-15",
      "montantTotal": 45000,
      "montantPayeTotal": 0,
      "montantDuTotal": 45000,
      "detailFactures": [...]
    }
  ]
}
```

### Exemple de Réponse (Client sans arriérés)

```json
{
  "idClient": 1,
  "nomClient": "Kalambayi Jonathan",
  "codeCons": "B/b1/0001",
  "arrieresParPeriode": []
}
```

### Exemple de Réponse (Client inexistant)

```json
{
  "message": "Client non trouvé"
}
```
**Code HTTP :** `404 Not Found`

---

## 🔄 Compatibilité

### Ancien Endpoint
- ✅ **Maintenu** : `/api/ClientFacture/client/{idClient}/arrieres`
- ✅ **Fonctionne toujours** : Retourne le format tableau simple
- ⚠️ **Dépréciation future** : À prévoir après migration du frontend

### Nouvel Endpoint
- ✅ **Disponible** : `/api/ClientFacture/client/{idClient}/arrieres-consolides`
- ✅ **Format consolidé** : Groupement par période
- ✅ **Cohérent** : Format similaire à `/consolidee/mois/{mois}/annee/{annee}`

---

## 📊 Performance

### Requêtes Base de Données
- **1 requête principale** : Récupération des `ClientFacture` avec `Include`
- **N requêtes supplémentaires** : Conversion en DTOs (chargement des `Facture` et `Usage`)
- **Optimisation possible** : Utiliser `Include` pour précharger toutes les relations

### Complexité
- **Temps :** O(n) où n = nombre de factures avec arriérés
- **Espace :** O(n) pour stocker les DTOs

---

## 🎯 Prochaines Étapes

### 1. Tests (Recommandé)
- [ ] Tests unitaires de `GetArrieresConsolidesByClientAsync`
- [ ] Tests d'intégration de l'endpoint
- [ ] Tests avec données réelles

### 2. Migration Frontend
- [ ] Identifier les composants utilisant l'ancien endpoint
- [ ] Adapter les composants pour le nouveau format
- [ ] Tester l'affichage
- [ ] Valider avec les utilisateurs

### 3. Dépréciation (Après Migration)
- [ ] Marquer l'ancien endpoint comme déprécié
- [ ] Surveiller les logs pour détecter les utilisations
- [ ] Supprimer l'ancien endpoint après confirmation

---

## 📝 Notes

- Le format de réponse est similaire à `ClientFactureConsolideeDto` mais adapté pour les arriérés
- Seules les factures avec `MontantDu > 0` sont incluses
- Le groupement se fait par période (Mois/Annees)
- Les totaux sont calculés pour chaque période
- L'ancien endpoint reste disponible pour compatibilité

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0  
**Statut :** ✅ Implémentation terminée

---

## Endpoint global et `detteAnterieur`

### Route

```
GET /api/ClientFacture/arrieres-consolides?moisFacturePrecedentSeulement=true&mois=&annee=&idAxe=&idTypeDeCourant=
```

**Query params optionnels** :
- `mois` : mois de la période de relance (`"04"` ou `"4"`). Défaut : **M-1 calendaire** (ex. en janvier 2026 → `12` / année **2025**).
- `annee` : année de la période de relance. Défaut : année de M-1 ; si seul `mois` est fourni → **année courante**.
- `idAxe` : filtre les clients par axe
- `idTypeDeCourant` : filtre les clients ayant au moins un `ClientUsage` actif avec ce type (même niveau que `idAxe`)

**Règles de validation** (uniquement si `moisFacturePrecedentSeulement=true`) :
- `annee` sans `mois` → **400 Bad Request**
- `mois` invalide (hors 1–12) ou `annee` hors plage 2000–2100 → **400 Bad Request**
- Si `moisFacturePrecedentSeulement=false`, `mois` et `annee` sont **ignorés**

**Périmètre de `mois` / `annee`** : affectent **uniquement la sélection initiale des clients** (facturés avec `Montant > 0` sur la période). Ils ne modifient ni `arrieresParPeriode`, ni le calcul de `detteAnterieur`.

**Champs additifs dans `detailFactures[]`** (sans régression) :
- `idTypeDeCourant` (int?, depuis `Facture.IdTypeDeCourant`)
- `typeDeCourant` (string?, libellé ex. « Permanent »)
- `null` pour les arriérés pré-existants sans `IdFacture`

### Calcul de `detteAnterieur` (par client, dans `arrieresParClient[]`)

Uniquement lorsque `moisFacturePrecedentSeulement=true` (défaut) :

1. **Sélection des clients** : facturés sur la période de relance (`Montant > 0`) — par défaut **M-1 calendaire**, ou période `mois`/`annee` si fournie.
2. Chargement de toutes leurs `ClientFacture` actives, groupées par `(Mois, Annees)`.
3. Pour chaque période : `montantDuTotal = Σ MontantDu`.
4. **`detteAnterieur`** = somme des `montantDuTotal` de **toutes les périodes sauf la dernière** `(Mois, Annees)` la plus récente **du client** (tri Années desc, Mois desc avec `NormaliserMois`).

Relation : `detteAnterieur ≈ totalGeneral − montantDuTotal(dernière période client)`.

Si une seule période : `detteAnterieur = 0`. Si `moisFacturePrecedentSeulement=false`, `detteAnterieur` vaut **0**.

### Exemple (mai 2026)

| Période | montantDuTotal |
|---------|----------------|
| 02/2026 | 10 000 |
| 04/2026 | 8 000 |
| 05/2026 (dernière du client) | 2 000 |

- `totalGeneral` = 20 000  
- `detteAnterieur` = 18 000 (05/2026 exclu, pas 04/2026)

### Tests

- `Tests/ClientFactureDetteAnterieurTests.cs` : exclusion dernière période, normalisation mois, période unique, mode `moisFacturePrecedentSeulement=false`.
- `Tests/ClientFactureTypeDeCourantTests.cs` : `idTypeDeCourant` / `typeDeCourant` dans `detailFactures`, filtre client par `idTypeDeCourant`.
- `Tests/ClientFacturePeriodeRelanceTests.cs` : période custom, défaut M-1, `mois` seul (année courante), params ignorés en mode tous clients, validation 400.
