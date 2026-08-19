## Facturation – Guide d’intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>`

### Endpoints clés
- `GET /api/Facture/societe/{idSociete}` : liste non paginée.
- `GET /api/Facture/societe/{idSociete}/paged?pageNumber=1&pageSize=20&searchTerm=&sortBy=Montant&sortDescending=false` : liste paginée avec tri/recherche.
- `GET /api/Facture/numero/{numeroFacture}` : recherche d'une facture par **numéro**, **CodeCons** ou **NomClient** (égalité exacte, insensible à la casse pour CodeCons/NomClient). Le paramètre de route reste `numeroFacture` pour compatibilité. Pour un CodeCons avec slashs (`A/a1/0236`), encoder l'URL côté client : `encodeURIComponent('A/a1/0236')` → `A%2Fa1%2F0236`.
- `POST /api/Facture/{idFacture}/societe/{idSociete}/diffusion?forcer=false` : met en file la diffusion multi-canal (push, in-app, email, SMS) pour une facture.
- `POST /api/Facture/societe/{idSociete}/diffusion/bulk?annee=&mois=` : diffuse en masse les factures **non diffusées** de la société pour une période (`MoisEmission` / `AnneesEmission`). Sans query : **mois calendaire précédent**. `annee` et `mois` doivent être fournis ensemble.
- `GET /api/Facture/{idFacture}/diffusion/statistiques` : stats de la dernière diffusion.

### Permissions (rôles)
- Diffusion : `Super-Admin, Admin, Gerant`
- Lecture paginée : généralement authentifié ; vérifier les guards côté app.

### Modèle Facture (champs importants)
- `idFacture`, `numeroFacture`, `montant`, `dateEmission`, `moisEmission`, `anneesEmission`, `idCategorie`, `estDiffusee`, `dateDiffusion`, `statut` (bool).

### Flux UI recommandé
1) Lister (paged) avec recherche par numéro/montant/date.
2) Bouton “Diffuser” sur une facture non diffusée (`estDiffusee=false`), avec option “forcer” si besoin.
3) Bouton “Diffuser le mois” (bulk) : par défaut mois précédent, ou sélecteur année/mois → `POST .../diffusion/bulk`.
4) Afficher le retour immédiat du queueing (`annee` / `mois` / `facturesEnQueue`) et un lien vers les stats.
5) Rafraîchir périodiquement les stats (`/diffusion/statistiques`).

### Exemples cURL
Lister paginé :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/societe/1/paged?pageNumber=1&pageSize=20"
```
Diffuser une facture :
```
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/1/societe/1/diffusion?forcer=false"
```
Diffuser en masse (mois précédent) :
```
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/societe/1/diffusion/bulk"
```
Diffuser en masse (période explicite) :
```
curl -X POST -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Facture/societe/1/diffusion/bulk?annee=2026&mois=5"
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

### Règle d'éligibilité (enregistrement / réactivation)

Lors de la création d'une `ClientFacture` (automatique ou manuelle), la période facturée est contrôlée par rapport à la **date effective de démarrage** :

```text
dateEffective = dateDerniereReactivation ?? dateCreation
```

1. **Période antérieure** au mois de `dateEffective` : refusée.
2. **Règle du 15** : si `dateEffective` tombe le **15 ou après** dans le mois M → pas de facture de M.
3. **Mois suivants** : autorisés.

Exemples :
- Client créé le 20/05/2026 (jamais réactivé) : pas avril 2026, pas mai 2026, oui à partir de juin 2026.
- Client créé le 10/01/2026, réactivé le 20/06/2026 : pas de facture avant juillet 2026 (juin bloqué par le cutoff du 15).

`dateDerniereReactivation` est renseignée automatiquement lors d'une transition **inactif → actif** (`PATCH` toggle IsActif ou mise à jour client). Elle n'est pas vidée à la désactivation.

- **Génération automatique** (`POST /api/Facture`, bulk) : la facture est créée ; les clients exclus n'ont pas de ligne `ClientFacture` (filtrage silencieux côté serveur, log applicatif). Les clients `isActif=false` restent exclus du périmètre.
- **Création manuelle** (`POST /api/ClientFacture`, `POST /api/ClientFacture/pre-existant`, import Excel) : réponse **400** avec un message du type :
  - période antérieure (création) : `"Ce client a été enregistré le 20/05/2026. Il ne peut pas recevoir une facture pour une période antérieure à son enregistrement (04/2026)."`
  - période antérieure (réactivation) : `"Ce client a été réactivé le 20/06/2026. Il ne peut pas recevoir une facture pour une période antérieure à sa réactivation (05/2026)."`
  - règle du 15 : `"Ce client a été enregistré le 15/05/2026 (à partir du 15 du mois). Il ne peut pas recevoir la facture de 05/2026."`
- **Champ API client** : `dateDerniereReactivation` (nullable) exposé en lecture sur les réponses client.

