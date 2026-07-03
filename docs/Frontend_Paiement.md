## Paiement – Guide d'intégration Frontend

### Base
- URL prod : `https://mombongo.asdc-rdc.org`
- URL dev local : `https://localhost:7110`
- Auth : `Authorization: Bearer <token>`
- Rôles autorisés (endpoint paged) : Super-Admin, Admin, Gérant, Financier, Responsable Commercial, Agent Direction Commercial, Caissier, Technicien

### Endpoints clés
- `GET /api/Paiement/societe/{idSociete}` : liste non paginée (paiements d'une société).
- `GET /api/Paiement/societe/{idSociete}/paged` : liste paginée avec tri, recherche et filtres optionnels.

### Paramètres de pagination (`PagedRequest`)
| Paramètre | Défaut | Description |
|-----------|--------|-------------|
| `pageNumber` | `1` | Numéro de page (≥ 1) |
| `pageSize` | `20` | Taille de page (max 100 000) |
| `searchTerm` | — | Recherche texte (voir ci-dessous) |
| `sortBy` | — | Champ de tri (voir ci-dessous). Si absent : tri par `DatePaiement` **décroissant** |
| `sortDescending` | `false` | `true` = ordre décroissant sur le champ `sortBy` |

### Filtres optionnels (`PaiementPagedRequest`)
Tous les filtres ci-dessous sont **optionnels**. Aucun filtre de date n'est appliqué par défaut.

| Paramètre | Type | Description |
|-----------|------|-------------|
| `date` | date (`YYYY-MM-DD`) | Paiements dont `datePaiement` est ce jour précis |
| `dateDebut` | date | Début de période (inclus) sur `datePaiement` |
| `dateFin` | date | Fin de période (inclus) sur `datePaiement` |
| `mois` | int (1–12) | Mois de `datePaiement` |
| `annee` | int | Année de `datePaiement` |
| `idUtilisateur` | int | Collecteur (`IdUtilisateur` du paiement) |
| `idAxe` | int | Axe du client (`Client.IdAxe`) |

**Combinaisons utiles :**
- Historique complet (sans filtre date) : ne pas envoyer `date`, `dateDebut`, `dateFin`, `mois`, `annee`.
- Paiements du jour : `?date=2026-05-16` (date du jour côté client).
- Période : `?dateDebut=2026-05-01&dateFin=2026-05-16`.
- Mois : `?mois=5&annee=2026`.

> **Migration :** avant, l'API filtrait implicitement sur la date du jour. Le front doit désormais passer `date` (ou `dateDebut` / `dateFin`) explicitement pour les écrans « caisse du jour ».

### Filtres toujours appliqués (côté API)
Sans paramètre query, la réponse reste limitée aux paiements qui respectent :
- société = `{idSociete}` (via facture → usage → catégorie client) ;
- facture liée et active (`Facture.Statut == true`) ;
- paiement non supprimé (`IsDeleted == false`).

Les paiements d'arriérés **sans facture** (`IdFacture` null) ne sont pas inclus dans cet endpoint.

### Réponse paginée (`PagedResultPaiement`)
En plus de `data`, `totalCount`, `pageNumber`, `pageSize` :
- `montantTotalPaiement` : somme des `montantPaye` sur **tous** les résultats filtrés (pas seulement la page courante) ;
- `nombreTotalPaiement` : nombre total de paiements filtrés ;
- `nombreTotalCollecteur` : nombre de collecteurs distincts (`idUtilisateur`).

### Modèle Paiement (champs importants)
- `idPaiement`, `idFacture`, `idClient`, `montantPaye`, `montantAPaye`, `resteAPaye`, `datePaiement`, `methodePaiement`, `referenceTransaction`, `commentaire`, `statut`, `idUtilisateurEnregistrement`.

**Alignement avec `GET /api/ClientFacture/client/{idClient}/arrieres` (enrichissement à la lecture) :**

| Champ paiement | Source ClientFacture | Équivalent arriérés |
|----------------|----------------------|---------------------|
| `idClientFacture` | `idClientFacture` | `idClientFacture` |
| `montantAPaye` | `montant` | `montant` |
| `resteAPaye` | `montantDu` | `montantDu` |

Résolution : `idClientFacture` explicite, sinon couple `(idClient, idFacture)` comme `GetByClientAndFactureAsync`. Les valeurs reflètent l’état **actuel** de la ligne `ClientFacture` (après tous les paiements), pas un snapshot historique au moment du paiement.

### Tri / Recherche
- Tri (`sortBy`) : `DatePaiement` (ou `date`), `MontantPaye` (ou `Montant`, `montant`), `Statut` (`statut`), `MethodePaiement` (`methode`).
- Recherche (`searchTerm`) : référence transaction, méthode, commentaire, numéro de facture, nom client.

### Flux UI recommandé
1) Lister (paged) filtré par société ; ajouter `date` ou une période selon l'écran (jour / mois / historique).
2) Afficher montant payé, reste à payer, méthode et statut.
3) Utiliser `montantTotalPaiement` et `nombreTotalCollecteur` pour les bandeaux de synthèse.
4) Lien vers la facture associée (`idFacture`) et vers le client (`idClient`) si besoin.

### Exemples cURL
Historique paginé (sans filtre date) :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Paiement/societe/1/paged?pageNumber=1&pageSize=20&sortBy=DatePaiement&sortDescending=true"
```

Paiements du jour :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Paiement/societe/1/paged?date=2026-05-16&pageNumber=1&pageSize=20"
```

Période + collecteur :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Paiement/societe/1/paged?dateDebut=2026-05-01&dateFin=2026-05-16&idUtilisateur=12"
```

Non paginé :
```
curl -H "Authorization: Bearer $TOKEN" \
  "https://mombongo.asdc-rdc.org/api/Paiement/societe/1"
```

### Bonnes pratiques Front
- Utiliser la pagination en liste principale ; garder la non paginée pour des exports courts.
- Pour l'écran « paiements du jour », passer explicitement `date` (timezone / jour local côté client).
- Surveiller `statut` (ex. : `Validé`) avant d'afficher comme payé.
- Quand `resteAPaye` ou `montantAPaye` sont renseignés, les mettre en avant pour la relance.
