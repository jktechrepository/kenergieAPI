# Documentation API — FlexPay (paiement électronique)

## Objectif

Initier un paiement Mobile Money ou carte via FlexPay, confirmer par callback HTTPS, puis créer un `Paiement` validé et mettre à jour la `ClientFacture`.

Le flux CASH (`POST /api/Paiement`, sync offline) reste inchangé. Les méthodes FlexPay y sont **refusées**.

## Prérequis

1. Appliquer le schéma : `Scripts/production_add_module_flexpay.sql`
2. Configurer `appsettings` section `FlexPay` (`Enabled=true`, `CallbackBaseUrl` HTTPS public)
3. Créer un marchand : `POST /api/InfoPaiementSociete`

## Flux

```
POST /api/Paiement/electronique  → pending + hold + appel FlexPay
FlexPay → push MM ou paymentUrl carte
FlexPay → POST /api/FlexPay/callback (code=0)
API → crée Paiement Validé + maj soldes
Secours : GET /api/FlexPay/verifier/{orderNumber}
```

## Endpoints

| Méthode | Route | Auth |
|---------|-------|------|
| POST | `/api/Paiement/electronique` | JWT |
| GET | `/api/Paiement/electronique/{idPending}` | JWT |
| POST | `/api/FlexPay/callback` | Public |
| GET | `/api/FlexPay/verifier/{orderNumber}` | JWT |
| GET | `/api/FlexPay/approve\|cancel\|decline` | Public (info carte) |
| CRUD | `/api/InfoPaiementSociete` | Admin/Financier |

### Initiation — body

```json
{
  "idClientFacture": 123,
  "methode": "MOBILE_MONEY",
  "telephone": "243900000000",
  "codeDevisePaiement": "CDF",
  "montant": 15000
}
```

- `methode` : `MOBILE_MONEY` ou `CARTE_BANCAIRE`
- Devises : **CDF** ou **USD** uniquement (aligné facture)
- `montant` optionnel (défaut = `MontantDu`)
- Montant recalculé / validé **serveur**

### Réponse initiation

```json
{
  "idPending": 1,
  "orderNumberFlexPay": "FP...",
  "referenceFlexPay": "KE-...",
  "montantFlexPay": 15000,
  "codeDevisePaiement": "CDF",
  "statut": "EnAttente",
  "holdExpireAt": "...",
  "paymentUrl": null,
  "flexPayAccepted": true
}
```

Pour la carte, ouvrir `paymentUrl` dans le navigateur.

## Règles

- Création métier **uniquement** après callback `code=="0"`
- Double callback → `alreadyProcessed`
- Hold TTL (défaut 15 min) anti-doublon
- Token marchand jamais exposé en clair (`hasApiToken` seulement)
- `ApiToken` = JWT FlexPay **sans** préfixe `Bearer ` (l’API le normalise au save et à l’envoi)
- Callback : validation montant sur `amount` (marchand) avant `amountCustomer` ; tolérance `FlexPay:MontantTolerance` (défaut 0,05)

## Checklist recette

- [ ] Marchand actif MM (+ carte si besoin)
- [ ] Init MM → callback 0 → 1 seul Paiement
- [ ] Double callback → alreadyProcessed
- [ ] Callback code!=0 → pending Echec
- [ ] Écart montant → pas de finalisation
- [ ] POST /api/Paiement avec Mobile Money → 400
