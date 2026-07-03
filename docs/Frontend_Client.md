## Client – Guide d’intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>`

### Endpoints clés
- `GET /api/Client/societe/{idSociete}` : liste non paginée par société.
- `GET /api/Client/societe/{idSociete}/paged?pageNumber=1&pageSize=20&searchTerm=&sortBy=NomClient&sortDescending=false` : liste paginée avec tri/recherche.
- `GET /api/Client/societe/{idSociete}/recherche/{searchTerm}` : recherche par `NomClient` ou `NumeroCompteur` (LIKE `%%`).
- `GET /api/Client/{id}/factures-payees/paged?pageNumber=1&pageSize=20` : factures payées d’un client (paginé).

### Modèle Client (champs utiles Front)
- `idClient`, `nomClient`, `telephone`, `emailClient`, `adresseClient`, `numeroCompteur`, `idCategorieClient`, `statut`, `dateCreation`.

### Création de client
- La création déclenche l’envoi d’un SMS de bienvenue (avec URL front configurable) et la création/mise à jour d’un compte utilisateur lié au client.

### Tri / Recherche (paged)
- Tri : `NomClient`, `NumeroCompteur`, `DateCreation`, `Statut` (par défaut : `idClient`).
- Recherche (`searchTerm`) : appliquée sur nom, numéro compteur, email, téléphone.

### Exemples cURL
Paged :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Client/societe/1/paged?pageNumber=1&pageSize=20&searchTerm=jo"
```
Recherche rapide :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Client/societe/1/recherche/897"
```
Factures payées (client) :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Client/4/factures-payees/paged?pageNumber=1&pageSize=10"
```

### Bonnes pratiques Front
- Toujours paginer en liste principale.
- Afficher `statut` (actif/inactif) et `dateCreation`.
- Pour la création, valider le numéro de téléphone (format international) et l’email avant envoi.
- Après création, informer l’utilisateur que le SMS de bienvenue a été envoyé automatiquement.

