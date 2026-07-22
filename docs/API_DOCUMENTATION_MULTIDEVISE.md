# Documentation API — Module Multi-devises

## Objectif

Gestion des devises et taux de change par société, avec snapshots monétaires sur Facture / ClientFacture / Paiement, et agrégats consolidés en devise principale.

Devise principale par défaut (données existantes et nouvelles sociétés) : **CDF**.

> Le champ historique `Societe.Devise` reste un slogan. La devise ISO est `Societe.CodeDevisePrincipale`.

---

## Règles métier

- Une société a **une seule** devise principale (`CodeDevisePrincipale`).
- Une devise est unique par société : `(IdSociete, CodeDevise)`.
- Montants stockés en **devise d’origine** + **devise principale** (taux figé à la saisie).
- **Phase 1** : un paiement doit être dans la **même devise** que la facture / ClientFacture (sinon `400`).
- Les stats / dashboards agrègent les colonnes `*DevisePrincipale` et exposent `codeDevisePrincipale`.

---

## Authentification

Tous les endpoints `api/Devise` requièrent un JWT.

**Lecture** (`GET devises`, `GET devises/{id}`, `GET taux-change`, `GET preview-conversion`) :  
`Super-Admin`, `Admin`, `Gerant`, `Financier`, `Caissier`, `Responsable Commercial`, `Agent Direction Commercial`

**Écriture** (POST/PUT devises, bascule principale, POST taux) :  
`Super-Admin`, `Admin`, `Gerant`

- `Super-Admin` : scope global  
- Autres rôles : limités à leur société

---

## Endpoints Devise

### Lister les devises actives

`GET /api/Devise/devises`

### Créer une devise

`POST /api/Devise/devises`

```json
{
  "idSociete": 1,
  "codeDevise": "USD",
  "libelle": "Dollar américain",
  "symbole": "$",
  "statut": true,
  "estDevisePrincipale": false
}
```

### Consulter / modifier

- `GET /api/Devise/devises/{idDeviseMonetaire}`
- `PUT /api/Devise/devises/{idDeviseMonetaire}` — body : `libelle`, `symbole`, `statut`, `estDevisePrincipale`

### Bascule devise principale

`PUT /api/Devise/societe/{idSociete}/devise-principale/{codeDevise}`

### Taux de change

- `POST /api/Devise/taux-change`
- `GET /api/Devise/taux-change?idSociete=1&source=USD&cible=CDF`

```json
{
  "idSociete": 1,
  "codeDeviseSource": "USD",
  "codeDeviseCible": "CDF",
  "taux": 2850.50,
  "dateEffet": "2026-07-14T10:30:00Z"
}
```

### Preview conversion

`GET /api/Devise/preview-conversion?idSociete=1&codeDeviseSource=USD&montant=25&datePaiement=2026-07-14T10:30:00Z`

---

## Impact Facture / ClientFacture / Paiement

### Facture

Création (simple ou bulk) : champ optionnel `codeDevisePrix` (défaut = devise principale).  
Le backend alimente : `codeDevisePrincipale`, `tauxVersDevisePrincipale`, `montantDevisePrincipale`.

### ClientFacture

Hérite du snapshot facture (système) ou accepte `codeDevisePrix` pour les arriérés pré-existants.

### Paiement

Champ optionnel `codeDevisePaiement` (défaut = devise de la facture).  
Si ≠ devise facture → `400` avec message explicite.  
Snapshot : `codeDevisePrincipale`, `tauxVersDevisePrincipale`, `montantPayeDevisePrincipale`, etc.

Sync offline (`POST` batch paiements) : même champ `codeDevisePaiement` + même règle.

---

## Statistiques

Les montants des endpoints stats / dashboards sont consolidés en devise principale.  
Les DTO exposent `codeDevisePrincipale` (ex. stats générales / financières).

---

## Checklist de test manuel

- [ ] Créer une devise non principale (ex. USD)
- [ ] Créer une devise avec `estDevisePrincipale=true`
- [ ] Vérifier une seule devise principale par société
- [ ] Tenter de désactiver la devise principale actuelle (doit échouer)
- [ ] Créer taux USD→CDF et CDF→USD
- [ ] Vérifier `preview-conversion`
- [ ] Créer facture CDF et USD → snapshots ClientFacture OK
- [ ] Paiement CDF sur facture CDF OK
- [ ] Paiement USD sur facture CDF → 400
- [ ] Stats société en CDF (`*DevisePrincipale`)
- [ ] Sociétés existantes migrées : `CodeDevisePrincipale=CDF`, historiques taux 1

---

## Migration

Migration EF : `AjoutModuleMultiDevise`  
Appliquer avec :

```bash
dotnet ef database update --project Kenergie.csproj
```
