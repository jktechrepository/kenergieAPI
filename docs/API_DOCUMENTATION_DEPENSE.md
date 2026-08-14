# API — Module Dépenses (phase 2)

Guide frontend (Vue.js + Flutter) : [`FRONTEND_INTEGRATION_DEPENSE.md`](./FRONTEND_INTEGRATION_DEPENSE.md)

## Vue d'ensemble

Le module Dépenses enregistre les **sorties d'argent** par société. Le **Financier** saisit ; **Admin** et **Gérant** valident ou refusent.

| Rôle | Accès |
|---|---|
| **Financier** | Créer, modifier (si EnAttente), retirer sa soumission, lecture |
| **Admin** | Valider, refuser, annuler une dépense déjà validée, supprimer, lecture |
| **Gérant** | Valider, refuser, lecture — **ne peut pas créer** |
| **Super-Admin** | Lecture et suppression — **ne peut pas créer ni valider** |
| **Responsable Commercial** | Lecture seule |
| **Caissier** | **Aucun accès API** — voit uniquement `TotalSorties` sur le résumé caisse |

## Workflow

```
Financier crée → EnAttente
  → Admin / Gérant valide → Validee (comptabilisée)
  → Admin / Gérant refuse → Annulee
  → Financier retire sa soumission → Annulee
Validee → Admin annule → Annulee
```

| Statut | Comptabilisé dans les KPI |
|---|---|
| `EnAttente` | Non |
| `Validee` | Oui |
| `Annulee` | Non |

À la création : `idUtilisateurValidateur` et `dateValidation` sont nuls.  
À la validation : le validateur est renseigné et le **snapshot multidevise** (taux du jour) est figé.

---

## Endpoints — `api/Depense`

| Méthode | Route | Permission | Rôle métier |
|---|---|---|---|
| GET | `/api/Depense` | `Depense.ReadAll` | Liste paginée |
| GET | `/api/Depense/mois` | `Depense.ReadAll` | Dépenses du mois + `syntheseDepense` |
| GET | `/api/Depense/{id}` | `Depense.Read` | Détail |
| POST | `/api/Depense` | `Depense.Create` | Financier → `EnAttente` |
| PUT | `/api/Depense/{id}` | `Depense.Update` | Financier, si `EnAttente` |
| POST | `/api/Depense/{id}/valider` | `Depense.Validate` | Admin, Gérant |
| POST | `/api/Depense/{id}/refuser` | `Depense.Validate` | Admin, Gérant |
| POST | `/api/Depense/{id}/annuler` | `Depense.Update` | Financier (EnAttente) ou Admin (Validee) |
| DELETE | `/api/Depense/{id}` | `Depense.Delete` | Admin, Super-Admin |

### Filtres GET paginé

- `idSociete` (Super-Admin)
- `dateDebut`, `dateFin`
- `idCategorieDepense`
- `statut` (`EnAttente` / `Validee` / `Annulee`)
- `PageNumber`, `PageSize`, `SearchTerm`, `SortDescending`

### Dépenses du mois

`GET /api/Depense/mois`

Query optionnels : `mois` (1–12), `annee`, `idSociete` (Super-Admin), `statut`.  
Sans paramètres : mois UTC en cours, **`statut=Validee`**.

Valeurs de `statut` : `Validee` (défaut), `EnAttente`, `Annulee`, `Tous`.

Liste non paginée filtrée. `syntheseDepense` porte sur cette liste (`montantTotal` = somme des lignes affichées).

```json
{
  "mois": 8,
  "annee": 2026,
  "dateDebut": "2026-08-01T00:00:00Z",
  "dateFin": "2026-08-31T23:59:59.9999999Z",
  "depenses": [],
  "syntheseDepense": {
    "montantTotal": 150000,
    "nombreDepenses": 12,
    "nombreValidees": 8,
    "nombreEnAttente": 3
  }
}
```

### Exemple création

```json
POST /api/Depense
{
  "idSociete": 1,
  "idCategorieDepense": 2,
  "libelle": "Carburant générateur",
  "montant": 150000,
  "codeDeviseMontant": "CDF",
  "modePaiement": "Espèces",
  "beneficiaire": "Station Total",
  "dateDepense": "2026-08-14T10:00:00Z"
}
```

Réponse : `statut: "EnAttente"`, `idUtilisateurValidateur: null`.

### Exemple validation

```http
POST /api/Depense/12/valider
```

Réponse : `statut: "Validee"`, `idUtilisateurValidateur` et `dateValidation` renseignés, snapshot `montantDevisePrincipale`.

### Exemple refus

```json
POST /api/Depense/12/refuser
{
  "motifAnnulation": "Justificatif manquant"
}
```

Réponse : `statut: "Annulee"`.

---

## Endpoints — `api/CategorieDepense`

| Méthode | Route | Permission |
|---|---|---|
| GET | `/api/CategorieDepense/societe/{idSociete}` | `CategorieDepense.ReadAll` |
| GET | `/api/CategorieDepense/{id}` | `CategorieDepense.Read` |
| POST | `/api/CategorieDepense` | `CategorieDepense.Create` |
| PUT | `/api/CategorieDepense/{id}` | `CategorieDepense.Update` |
| DELETE | `/api/CategorieDepense/{id}` | `CategorieDepense.Delete` |

Catégories par défaut (seed prod) : Carburant, Maintenance, Fournitures, Autre.

---

## Dashboards

### Caissier — `ResumeCaisse.TotalSorties`

Somme des dépenses **validées du jour** pour la société (agrégat, sans filtre utilisateur caissier). Les `EnAttente` sont exclues.

### Financier — `GlobalStatistiques`

- `montantTotalDepensesMois` (validées)
- `montantTotalDepensesJournalier` (validées)
- `resultatNetMois` (= encaissements − dépenses validées du mois)
- `nombreDepensesEnAttente`

### Gérant — `SocieteStatistiques`

- `montantDepensesMois` (validées)
- `nombreDepensesAValider`
- `montantDepensesEnAttente` (indicatif, non comptabilisé)

---

## Déploiement production

1. Appliquer `Scripts/production_add_module_multidevise.sql` si pas déjà fait
2. Appliquer `Scripts/production_add_module_depense.sql`
3. Appliquer `Scripts/add_permissions_depense.sql` (retire Create à Admin/Super-Admin, ajoute `Depense.Validate`)
4. Déployer l'API
