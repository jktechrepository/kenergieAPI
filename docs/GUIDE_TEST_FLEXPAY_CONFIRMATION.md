# Guide test — confirmation paiement électronique FlexPay

## Objectif

Vérifier qu'aucun `Paiement` métier n'est créé **avant** la confirmation réelle FlexPay (callback opérateur ou vérification statut `SUCCESS`).

## Symptôme corrigé

Après `POST /api/Paiement/electronique`, un row `Paiements` apparaissait immédiatement alors que l'utilisateur n'avait pas encore validé le push Mobile Money.

**Cause** : finalisation sur callback `code=0` sans `providerReference` (acceptation technique FlexPay, pas confirmation opérateur).

**Correction** :
- Mobile Money : finalisation uniquement si `providerReference` présent (config `FlexPay:RequireProviderReferenceForMobileMoney`, défaut `true`)
- `GET /api/FlexPay/verifier/{orderNumber}` : finalise seulement si statut FlexPay confirmé (`SUCCESS`, `0`, etc.)
- SignalR `PaiementElectroniqueStatusChanged` + `NewPaiement` émis **après** finalisation

## Script SQL de diagnostic

Exécuter **juste après** `POST /api/Paiement/electronique`, **avant** saisie PIN :

```sql
-- Dernier pending
SELECT IdPaiementElectroniqueEnAttente, Statut, Reference, OrderNumber, DateCreation, IdPaiementFinalise
FROM PaiementsElectroniquesEnAttente
ORDER BY IdPaiementElectroniqueEnAttente DESC
LIMIT 1;

-- Derniers Paiements FlexPay (doit être VIDE ou ancien avant PIN)
SELECT IdPaiement, MontantPaye, Statut, Commentaire, DateCreation
FROM Paiements
WHERE Commentaire LIKE 'FlexPay%'
ORDER BY IdPaiement DESC
LIMIT 5;

-- Callbacks reçus (peut contenir CallbackIgnoredNotConfirmed)
SELECT IdCallbackFlexPay, Code, OrderNumber, MessageTraitement, DateReception
FROM CallbacksFlexPay
ORDER BY IdCallbackFlexPay DESC
LIMIT 5;
```

### Attendu AVANT validation Mobile Money

| Table | Attendu |
|-------|---------|
| `PaiementsElectroniquesEnAttente` | 1 ligne `Statut = EnAttente`, `IdPaiementFinalise = NULL` |
| `Paiements` | Aucune nouvelle ligne FlexPay |
| `CallbacksFlexPay` | Callbacks éventuels avec `MessageTraitement LIKE 'CallbackIgnoredNotConfirmed%'` |

### Attendu APRÈS validation Mobile Money

| Table | Attendu |
|-------|---------|
| `PaiementsElectroniquesEnAttente` | `Statut = Finalise`, `IdPaiementFinalise` renseigné |
| `Paiements` | 1 ligne `Statut = Validé`, `Commentaire LIKE 'FlexPay%'` |
| Dashboard SignalR | Events `PaiementElectroniqueStatusChanged` puis `NewPaiement` |

## Checklist recette manuelle

- [ ] Init MM → réponse `estConfirme: false`, `statut: EnAttente`
- [ ] Avant PIN : SQL ci-dessus → pas de nouveau `Paiements`
- [ ] Callback prématuré `code=0` sans `providerReference` → ignoré, pending reste `EnAttente`
- [ ] Après PIN : callback avec `providerReference` → 1 `Paiement`, pending `Finalise`
- [ ] `GET /api/Paiement/electronique/{idPending}` → `estConfirme: true` après succès
- [ ] `GET /api/FlexPay/verifier/{orderNumber}` avant confirmation → message « en attente », pas de `Paiement`
- [ ] UI : ne pas afficher succès sur `flexPayAccepted=true` seul ; poller ou écouter SignalR

## Configuration (`appsettings`)

```json
"FlexPay": {
  "RequireProviderReferenceForMobileMoney": true,
  "MinSecondsBeforeFinalize": 0
}
```

En recette, `MinSecondsBeforeFinalize: 3` peut aider à détecter les callbacks instantanés (optionnel).

## Logs applicatifs

Rechercher :
- `FlexPay callback reçu pending=... deltaSec=...`
- `FlexPay callback ignoré (non confirmé) ... reason=ProviderReference absent`
