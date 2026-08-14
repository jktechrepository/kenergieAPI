# Guide d'intégration Frontend — Module Dépenses

Documentation **orientée front** pour brancher les sorties d'argent (charges, fournisseurs, caisse) dans :

- **Web** : Vue.js 3 (Composition API)
- **Mobile** : Flutter

Référence API : [`API_DOCUMENTATION_DEPENSE.md`](./API_DOCUMENTATION_DEPENSE.md)

**Base URL** : `{API_BASE}`  
**Auth** : `Authorization: Bearer {jwt}` sur toutes les routes.  
JSON en **camelCase**.

---

## 1. Principes

| Règle | Impact UI |
|---|---|
| Une dépense = une **sortie** (jamais mélangée au CA / paiements) | Cartes et totaux séparés |
| Seul le **Financier** crée | Masquer le bouton « Nouvelle dépense » pour Admin, Gérant, Super-Admin |
| Création → statut `EnAttente` | Badge « En attente » ; **non comptée** dans les totaux |
| Admin / Gérant valident ou refusent | File d’attente + boutons Valider / Refuser |
| Snapshot devises figé **à la validation** | Avant validation : afficher `montant` + `codeDeviseMontant`. Après : aussi `montantDevisePrincipale` |
| Caissier **sans accès API** Depense | Ne pas appeler `/api/Depense`. Afficher seulement `resumeCaisse.totalSorties` |

Statuts : `EnAttente` | `Validee` | `Annulee`.

```
Financier crée → EnAttente
  → Admin / Gérant valide → Validee (comptabilisée)
  → Admin / Gérant refuse → Annulee
  → Financier retire sa soumission → Annulee
Validee → Admin annule → Annulee
```

---

## 2. Matrice UI par rôle

| Écran / action | Financier | Admin | Gérant | Super-Admin | RC | Caissier |
|---|---|---|---|---|---|---|
| Liste / détail / rapport mois | oui | oui | oui | oui | oui | **non** |
| Créer / modifier (si EnAttente, sa ligne) | **oui** | non | non | non | non | non |
| Valider / refuser | non | **oui** | **oui** | non | non | non |
| Annuler EnAttente (retrait) | **oui** (ses saisies) | non (utiliser Refuser) | non | non | non | non |
| Annuler Validee | non | **oui** | non | non | non | non |
| Soft delete | non | oui | non | oui | non | non |
| Catégories lecture | oui | oui | oui | oui | oui | non |
| Catégories écriture | oui | oui | non | oui | non | non |

Permissions JWT : `Depense.Create`, `Read`, `ReadAll`, `Update`, `Validate`, `Delete` + `CategorieDepense.*`.

---

## 3. Écrans à brancher

### 3.1 Liste paginée

`GET /api/Depense`

Query : `pageNumber`, `pageSize`, `searchTerm`, `sortDescending`, `idSociete`, `dateDebut`, `dateFin`, `idCategorieDepense`, `statut`.

Réponse `PagedResult` :

```json
{
  "data": [ /* DepenseResponseDto */ ],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### 3.2 Rapport du mois (écran principal totaux)

`GET /api/Depense/mois`

| Query | Défaut |
|---|---|
| `mois` | mois UTC courant (1–12) |
| `annee` | année UTC courante |
| `idSociete` | société JWT (Super-Admin peut filtrer) |
| `statut` | **`Validee`** |

Autres valeurs `statut` : `EnAttente`, `Annulee`, `Tous`.

Afficher `syntheseDepense.montantTotal` en en-tête (somme des **lignes affichées**).

File d’attente Admin / Gérant :

```
GET /api/Depense/mois?statut=EnAttente
```

### 3.3 Formulaire création (Financier)

1. Charger les catégories : `GET /api/CategorieDepense/societe/{idSociete}`
2. `POST /api/Depense` → réponse `statut: "EnAttente"`, `idUtilisateurValidateur: null`
3. Message UX : « Envoyée pour validation »

Le **montant n’est pas modifiable** après création (`UpdateDepenseDto` ne le contient pas). Pour corriger un montant : retirer (`POST .../annuler`) puis recréer.

### 3.4 Actions

| Action | Route | Body |
|---|---|---|
| Modifier EnAttente | `PUT /api/Depense/{id}` | champs optionnels (pas de montant) |
| Valider | `POST /api/Depense/{id}/valider` | aucun |
| Refuser | `POST /api/Depense/{id}/refuser` | `{ "motifAnnulation": "..." }` |
| Retirer / annuler | `POST /api/Depense/{id}/annuler` | `{ "motifAnnulation": "..." }` |
| Supprimer | `DELETE /api/Depense/{id}` | — → `204` |

---

## 4. Payloads

### Création

```json
POST /api/Depense
{
  "idSociete": 1,
  "idCategorieDepense": 2,
  "libelle": "Carburant générateur",
  "description": null,
  "beneficiaire": "Station Total",
  "referencePiece": "FAC-2026-014",
  "montant": 150000,
  "codeDeviseMontant": "CDF",
  "modePaiement": "Espèces",
  "dateDepense": "2026-08-14T10:00:00Z",
  "idCabine": null,
  "idAxe": null
}
```

### Réponse (extrait)

```json
{
  "idDepense": 12,
  "idSociete": 1,
  "nomCategorie": "Carburant",
  "libelle": "Carburant générateur",
  "montant": 150000,
  "codeDeviseMontant": "CDF",
  "codeDevisePrincipale": null,
  "montantDevisePrincipale": null,
  "statut": "EnAttente",
  "idUtilisateurCreateur": 10,
  "nomCreateur": "Jean Financier",
  "idUtilisateurValidateur": null,
  "nomValidateur": null,
  "dateValidation": null
}
```

Après validation : `statut: "Validee"`, `nomValidateur` renseigné, `montantDevisePrincipale` et `tauxVersDevisePrincipale` figés.

### Rapport mois

```json
{
  "mois": 8,
  "annee": 2026,
  "dateDebut": "2026-08-01T00:00:00Z",
  "dateFin": "2026-08-31T23:59:59.9999999Z",
  "depenses": [],
  "syntheseDepense": {
    "montantTotal": 150000,
    "nombreDepenses": 8,
    "nombreValidees": 8,
    "nombreEnAttente": 0
  }
}
```

### Catégorie

```json
POST /api/CategorieDepense
{ "idSociete": 1, "nomCategorie": "Loyer", "description": "Loyer sites" }
```

Seed prod : Carburant, Maintenance, Fournitures, Autre.

---

## 5. Vue.js 3

```ts
// services/depenseApi.ts
import { api } from '@/plugins/axios'

export const depenseApi = {
  list: (params?: Record<string, unknown>) => api.get('/api/Depense', { params }),
  get: (id: number) => api.get(`/api/Depense/${id}`),
  mois: (params?: { mois?: number; annee?: number; idSociete?: number; statut?: string }) =>
    api.get('/api/Depense/mois', { params }),
  create: (body: Record<string, unknown>) => api.post('/api/Depense', body),
  update: (id: number, body: Record<string, unknown>) => api.put(`/api/Depense/${id}`, body),
  valider: (id: number) => api.post(`/api/Depense/${id}/valider`),
  refuser: (id: number, motifAnnulation?: string) =>
    api.post(`/api/Depense/${id}/refuser`, { motifAnnulation }),
  annuler: (id: number, motifAnnulation?: string) =>
    api.post(`/api/Depense/${id}/annuler`, { motifAnnulation }),
  remove: (id: number) => api.delete(`/api/Depense/${id}`),
}

export const categorieDepenseApi = {
  bySociete: (idSociete: number) => api.get(`/api/CategorieDepense/societe/${idSociete}`),
  create: (body: Record<string, unknown>) => api.post('/api/CategorieDepense', body),
  update: (id: number, body: Record<string, unknown>) => api.put(`/api/CategorieDepense/${id}`, body),
  remove: (id: number) => api.delete(`/api/CategorieDepense/${id}`),
}
```

Affichage montant :

```ts
function labelMontant(d: { montant: number; codeDeviseMontant?: string; montantDevisePrincipale?: number | null; codeDevisePrincipale?: string | null; statut: string }) {
  const orig = `${d.montant} ${d.codeDeviseMontant ?? ''}`.trim()
  if (d.statut === 'Validee' && d.montantDevisePrincipale != null && d.codeDevisePrincipale && d.codeDevisePrincipale !== d.codeDeviseMontant) {
    return `${orig} (${d.montantDevisePrincipale} ${d.codeDevisePrincipale})`
  }
  return orig
}
```

---

## 6. Flutter

```dart
class DepenseApi {
  DepenseApi(this._dio);
  final Dio _dio;

  Future<Map<String, dynamic>> list({Map<String, dynamic>? params}) async {
    final r = await _dio.get('/api/Depense', queryParameters: params);
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> mois({int? mois, int? annee, int? idSociete, String? statut}) async {
    final r = await _dio.get('/api/Depense/mois', queryParameters: {
      if (mois != null) 'mois': mois,
      if (annee != null) 'annee': annee,
      if (idSociete != null) 'idSociete': idSociete,
      if (statut != null) 'statut': statut,
    });
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> create(Map<String, dynamic> body) async {
    final r = await _dio.post('/api/Depense', data: body);
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> valider(int id) async {
    final r = await _dio.post('/api/Depense/$id/valider');
    return r.data as Map<String, dynamic>;
  }

  Future<Map<String, dynamic>> refuser(int id, {String? motif}) async {
    final r = await _dio.post('/api/Depense/$id/refuser', data: {'motifAnnulation': motif});
    return r.data as Map<String, dynamic>;
  }
}
```

---

## 7. Dashboards (champs déjà exposés)

Ne pas recalculer côté front : utiliser les KPI API.

| Dashboard | Champ | Contenu |
|---|---|---|
| Caissier `resumeCaisse` | `totalSorties` | Dépenses **Validee** du **jour** (société) |
| Financier `globalStatistiques` | `montantTotalDepensesMois` | Validee du mois |
| | `montantTotalDepensesJournalier` | Validee du jour |
| | `resultatNetMois` | Encaissements − dépenses validées |
| | `nombreDepensesEnAttente` | File à faire valider (informer, pas d’action Financier) |
| Gérant `societeStatistiques` | `montantDepensesMois` | Validee du mois |
| | `nombreDepensesAValider` | Badge file d’attente |
| | `montantDepensesEnAttente` | Indicatif, **non comptabilisé** |

---

## 8. Erreurs fréquentes

| HTTP | `message` typique | UI |
|---|---|---|
| 403 | « Seul le Financier peut créer… » | Masquer l’action selon le rôle |
| 403 | « Le rôle Caissier n'a pas accès… » | Ne pas appeler le module |
| 400 | « Seule une dépense en attente peut être validée / modifiée » | Rafraîchir la ligne |
| 400 | « Statut invalide. Valeurs autorisées : Validee, EnAttente, Annulee, Tous. » | Corriger le filtre |
| 400 | « Catégorie de dépense introuvable ou inactive… » | Recharger les catégories |
| 404 | Dépense introuvable | Retour liste |

Corps d’erreur : `{ "message": "..." }`.

---

## 9. Checklist QA

- [ ] Financier : `POST /api/Depense` → `EnAttente`, absent de `GET /mois` (défaut Validee)
- [ ] Admin / Gérant : `GET /mois?statut=EnAttente` → Valider → apparaît dans `GET /mois` et `syntheseDepense.montantTotal` augmente
- [ ] Refus → `Annulee`, hors totaux
- [ ] Admin / Super-Admin / Gérant : `POST /api/Depense` → 403
- [ ] Caissier : `/api/Depense` → 403 ; dashboard caisse affiche `totalSorties`
- [ ] Modifier une `Validee` → 400
- [ ] Devise saisie ≠ principale : après validation, `montantDevisePrincipale` renseigné
