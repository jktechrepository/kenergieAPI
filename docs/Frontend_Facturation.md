## Facturation – Guide d’intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>`

### Endpoints clés
- `GET /api/Facture/societe/{idSociete}` : liste non paginée.
- `GET /api/Facture/societe/{idSociete}/paged?pageNumber=1&pageSize=20&searchTerm=&sortBy=Montant&sortDescending=false` : liste paginée avec tri/recherche.
- `GET /api/Facture/numero/{numeroFacture}` : recherche d'une facture par **numéro**, **CodeCons** ou **NomClient** (égalité exacte, insensible à la casse pour CodeCons/NomClient). Le paramètre de route reste `numeroFacture` pour compatibilité. Pour un CodeCons avec slashs (`A/a1/0236`), encoder l'URL côté client : `encodeURIComponent('A/a1/0236')` → `A%2Fa1%2F0236`.
- `POST /api/Facture/{idFacture}/societe/{idSociete}/diffusion?forcer=false` : met en file la diffusion multi-canal (push, in-app, email, SMS) vers tous les clients de la catégorie de la facture.
- `GET /api/Facture/{idFacture}/diffusion/statistiques` : stats de la dernière diffusion.

### Permissions (rôles)
- Diffusion : `Super-Admin, Admin, Gerant`
- Lecture paginée : généralement authentifié ; vérifier les guards côté app.

### Modèle Facture (champs importants)
- `idFacture`, `numeroFacture`, `montant`, `dateEmission`, `moisEmission`, `anneesEmission`, `idCategorie`, `estDiffusee`, `dateDiffusion`, `statut` (bool).

### Flux UI recommandé
1) Lister (paged) avec recherche par numéro/montant/date.
2) Bouton “Diffuser” sur une facture non diffusée (`estDiffusee=false`), avec option “forcer” si besoin.
3) Afficher le retour immédiat du queueing et un lien vers les stats.
4) Rafraîchir périodiquement les stats (`/diffusion/statistiques`).

### Exemples cURL
Lister paginé :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/societe/1/paged?pageNumber=1&pageSize=20"
```
Diffuser :
```
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/1/societe/1/diffusion?forcer=false"
```

### États et messages
- `estDiffusee=true` + `dateDiffusion` indiquent une diffusion déjà lancée.
- Conflit si déjà diffusée et `forcer=false`.
- Le message de réponse précise le nombre de clients ciblés et le temps estimé.

### Bonnes pratiques Front
- Toujours paginer pour les listes.
- Gérer le conflit 409 (déjà diffusée) en proposant “forcer”.
- Afficher les canaux activés selon les préférences utilisateur (retour en back déjà filtré).
- Vérifier `statut` (true) pour afficher uniquement les factures actives.

### Règle du 15 (enregistrement client)

Lors de la création d'une `ClientFacture` (automatique ou manuelle), un client **enregistré le 15 ou après** dans le mois M **ne reçoit pas** la facture de ce mois M (même année). Les mois suivants ne sont pas concernés.

- **Génération automatique** (`POST /api/Facture`, bulk) : la facture est créée ; les clients exclus n'ont pas de ligne `ClientFacture` (filtrage silencieux côté serveur, log applicatif).
- **Création manuelle** (`POST /api/ClientFacture`, `POST /api/ClientFacture/pre-existant`, import Excel) : réponse **400** avec un message du type :
  `"Ce client a été enregistré le 15/05/2026 (à partir du 15 du mois). Il ne peut pas recevoir la facture de 05/2026."`
- **Date de référence** : `Client.dateCreation` au moment de l'enregistrement.

