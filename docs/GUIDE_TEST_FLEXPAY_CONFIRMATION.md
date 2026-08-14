# Guide test — confirmation paiement électronique FlexPay

## Objectif

Vérifier qu'aucun `Paiement` métier n'est créé **avant** la confirmation réelle FlexPay (callback opérateur ou vérification statut `SUCCESS`).

## Symptômes corrigés

### 1. Finalisation trop tôt (avant PIN)

Après `POST /api/Paiement/electronique`, un row `Paiements` apparaissait immédiatement.

**Cause** : finalisation sur callback `code=0` sans confirmation opérateur.

**Correction** : Mobile Money finalise seulement si `providerReference` / `provider_reference` présent (ou check FlexPay confirmé).

### 2. Confirmation PIN → statut reste `EnAttente` (annulation OK)

Après validation Mobile Money, le pending restait `EnAttente`, alors que l’annulation passait bien en `Echec`.

**Cause** : FlexPay envoie `provider_reference` (snake_case). Le DTO ne lisait que `providerReference` → référence absente → callback succès ignoré (`CallbackIgnoredNotConfirmed:ProviderReference absent`).

**Correction** :
- Binding `provider_reference` + normalisation depuis le body brut
- Fallback `GET check` FlexPay si `code=0` sans référence
- Rôle `Client` autorisé sur `GET /api/FlexPay/verifier/{orderNumber}` (ses propres paiements)

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
SELECT IdCallbackFlexPay, Code, OrderNumber, MessageTraitement, PayloadJson, DateReception
FROM CallbacksFlexPay
ORDER BY IdCallbackFlexPay DESC
LIMIT 5;
```

Diagnostic « confirmé mais EnAttente » :

```sql
SELECT IdCallbackFlexPay, Code, OrderNumber, MessageTraitement, PayloadJson
FROM CallbacksFlexPay
WHERE MessageTraitement LIKE 'CallbackIgnoredNotConfirmed%'
ORDER BY IdCallbackFlexPay DESC
LIMIT 10;
```

Si `PayloadJson` contient `provider_reference` mais `MessageTraitement` parle d’absence → bug de binding (corrigé).

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
- [ ] Callback prématuré `code=0` sans `provider_reference` → ignoré, pending reste `EnAttente`
- [ ] Après PIN : callback FlexPay avec `provider_reference` → 1 `Paiement`, pending `Finalise`
- [ ] Annulation → pending `Echec`
- [ ] `GET /api/Paiement/electronique/{idPending}` → `estConfirme: true` après succès
- [ ] `GET /api/FlexPay/verifier/{orderNumber}` (y compris rôle Client) avant confirmation → message « en attente »
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
- `FlexPay callback reçu pending=... providerRef=... deltaSec=...`
- `FlexPay callback ignoré (non confirmé) ... reason=ProviderReference absent`
- `FlexPay enrich providerReference via check ...`
