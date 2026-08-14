# Guide d'intégration Frontend — Équivalents USD (rapport financier)

Documentation **orientée front** pour afficher les équivalents USD indicatifs sur stats et dashboards, dans :

- **Web** : Vue.js 3 (Composition API)
- **Mobile** : Flutter

Références API :
- [`API_DOCUMENTATION_MULTIDEVISE.md`](./API_DOCUMENTATION_MULTIDEVISE.md)
- [`API_DOCUMENTATION_STATISTIQUES.md`](./API_DOCUMENTATION_STATISTIQUES.md)

**Base URL** : `{API_BASE}`  
**Auth** : `Authorization: Bearer {jwt}`  
JSON en **camelCase**.

Aucun nouvel endpoint : le bloc `syntheseUsd` est déjà présent sur les réponses existantes.

---

## 1. Principes

| Règle | Impact UI |
|---|---|
| Les KPI restent en **devise principale** (`codeDevisePrincipale`) | Afficher d’abord le montant API inchangé |
| `syntheseUsd` est **indicatif** (taux du jour, pas un snapshot de facture) | Libellé du type « ≈ … USD » |
| Le front **ne convertit pas** | Ne pas appeler `GET /api/Devise/preview-conversion` pour les totaux |
| Afficher l’USD seulement si `conversionUsdDisponible === true` | Sinon masquer ou « — » |
| Caissier, dépenses, évolutions, répartitions | **Pas** de `syntheseUsd` |

Les montants d’origine (factures, paiements) gardent leur snapshot `montantDevisePrincipale` : ne pas les remplacer par cet équivalent USD.

---

## 2. Contrat `EquivalentUsdDto`

Disponible :

```json
{
  "montantEquivalentUsd": 400.00,
  "tauxVersUsd": 0.0004,
  "dateTaux": "2026-08-14T16:00:00Z",
  "conversionUsdDisponible": true
}
```

Indisponible (USD inactif, taux manquant, ou échec sur **une** société d’un agrégat multi-sociétés) :

```json
{
  "montantEquivalentUsd": null,
  "tauxVersUsd": null,
  "dateTaux": null,
  "conversionUsdDisponible": false
}
```

| Champ | Usage UI |
|---|---|
| `conversionUsdDisponible` | Gate unique avant d’afficher l’USD |
| `montantEquivalentUsd` | Valeur à formater (2 décimales côté API) |
| `tauxVersUsd` | Optionnel (tooltip). `null` si agrégat **plusieurs** sociétés (somme USD uniquement) |
| `dateTaux` | Optionnel (tooltip « taux du … ») |

Devise principale = `USD` : `tauxVersUsd = 1`, montant arrondi 2 décimales.  
Montant KPI = 0 : `conversionUsdDisponible = true`, `montantEquivalentUsd = 0`.

---

## 3. UX

Format recommandé :

```
1 250 000 CDF  (≈ 500 USD)
```

Si `conversionUsdDisponible` est `false` : n’afficher que `1 250 000 CDF` (ou « — » à la place de l’USD).

Ne pas recalculer `montant * tauxVersUsd` côté client.

---

## 4. Carte des endpoints

Sur le dashboard **complet**, `syntheseUsd` est imbriqué. Sur les sous-routes qui renvoient uniquement le DTO stats, il est **à la racine**.

| Endpoint | Bloc | Champs `syntheseUsd` |
|---|---|---|
| `GET /api/Statistiques/generales/{idSociete}` | `syntheseUsd` | `totalArrieres`, `totalPaiements` |
| `GET /api/Statistiques/financieres/{idSociete}` | `syntheseUsd` | `chiffreAffaires`, `montantArrieres`, `montantPaye`, `montantDu` |
| `GET /api/Dashboard/{idSociete}` | `syntheseUsd` | `paiementsDuMois`, `totalGeneralArriere` |
| `GET /api/FinancierDashboard` | `globalStatistiques.syntheseUsd` | `chiffreAffairesTotal`, `montantTotalEncaisse`, `montantTotalArrieres`, `totalGeneralArriere`, `chiffreAffairesJournalier` |
| `GET /api/FinancierDashboard/statistiques-globales` | `syntheseUsd` | mêmes champs Financier |
| `GET /api/GerantDashboard` | `societeStatistiques.syntheseUsd` | `chiffreAffairesMois`, `montantTotalArrieres` |
| `GET /api/GerantDashboard/statistiques` | `syntheseUsd` | mêmes champs Gérant |
| `GET /api/SuperAdminDashboard` | `globalStatistiques.syntheseUsd` | `chiffreAffairesGlobal`, `montantTotalArrieresGlobal`, `montantTotalPaiementsGlobal` |
| `GET /api/SuperAdminDashboard/global-statistiques` | `syntheseUsd` | mêmes champs Super-Admin |
| `GET /api/ResponsableCommercialDashboard` | `globalStatistiques.syntheseUsd` | mêmes champs que Financier |

**Sans `syntheseUsd` :** dashboard Caissier, KPI dépenses, `evolutionMensuelle`, `repartitionPaiements`, listes par société (`societesFinancieres`, etc.).

---

## 5. Payloads

### Stats financières

`GET /api/Statistiques/financieres/{idSociete}`

```json
{
  "chiffreAffaires": 2500000,
  "montantArrieres": 800000,
  "montantPaye": 2500000,
  "montantDu": 800000,
  "codeDevisePrincipale": "CDF",
  "syntheseUsd": {
    "chiffreAffaires": {
      "montantEquivalentUsd": 1000.00,
      "tauxVersUsd": 0.0004,
      "dateTaux": "2026-08-14T16:00:00Z",
      "conversionUsdDisponible": true
    },
    "montantArrieres": {
      "montantEquivalentUsd": 320.00,
      "tauxVersUsd": 0.0004,
      "dateTaux": "2026-08-14T16:00:00Z",
      "conversionUsdDisponible": true
    },
    "montantPaye": {
      "montantEquivalentUsd": 1000.00,
      "tauxVersUsd": 0.0004,
      "dateTaux": "2026-08-14T16:00:00Z",
      "conversionUsdDisponible": true
    },
    "montantDu": {
      "montantEquivalentUsd": 320.00,
      "tauxVersUsd": 0.0004,
      "dateTaux": "2026-08-14T16:00:00Z",
      "conversionUsdDisponible": true
    }
  },
  "evolutionMensuelle": [],
  "repartitionPaiements": []
}
```

Affichage CA : `chiffreAffaires` + `codeDevisePrincipale`, puis `syntheseUsd.chiffreAffaires` si disponible.

### Dashboard Financier

`GET /api/FinancierDashboard` (extrait)

```json
{
  "codeDevisePrincipale": "CDF",
  "globalStatistiques": {
    "chiffreAffairesTotal": 5000000,
    "montantTotalEncaisse": 4200000,
    "montantTotalArrieres": 800000,
    "totalGeneralArriere": 800000,
    "chiffreAffairesJournalier": 150000,
    "syntheseUsd": {
      "chiffreAffairesTotal": {
        "montantEquivalentUsd": 2000.00,
        "tauxVersUsd": null,
        "dateTaux": "2026-08-14T16:00:00Z",
        "conversionUsdDisponible": true
      },
      "montantTotalEncaisse": { "montantEquivalentUsd": 1680.00, "tauxVersUsd": null, "dateTaux": "2026-08-14T16:00:00Z", "conversionUsdDisponible": true },
      "montantTotalArrieres": { "montantEquivalentUsd": 320.00, "tauxVersUsd": null, "dateTaux": "2026-08-14T16:00:00Z", "conversionUsdDisponible": true },
      "totalGeneralArriere": { "montantEquivalentUsd": 320.00, "tauxVersUsd": null, "dateTaux": "2026-08-14T16:00:00Z", "conversionUsdDisponible": true },
      "chiffreAffairesJournalier": { "montantEquivalentUsd": 60.00, "tauxVersUsd": null, "dateTaux": "2026-08-14T16:00:00Z", "conversionUsdDisponible": true }
    }
  }
}
```

`tauxVersUsd: null` : agrégat **plusieurs** sociétés (somme des USD). Une seule société → le taux est renseigné.

---

## 6. Vue.js 3

```ts
export type EquivalentUsd = {
  montantEquivalentUsd: number | null
  tauxVersUsd: number | null
  dateTaux: string | null
  conversionUsdDisponible: boolean
}

export function formatKpiWithUsd(
  montant: number,
  codeDevise: string | null | undefined,
  eq?: EquivalentUsd | null,
): string {
  const principal = `${montant.toLocaleString('fr-FR')} ${codeDevise ?? ''}`.trim()
  if (!eq?.conversionUsdDisponible || eq.montantEquivalentUsd == null) {
    return principal
  }
  return `${principal} (≈ ${eq.montantEquivalentUsd.toLocaleString('fr-FR')} USD)`
}
```

Exemple stats financières :

```ts
formatKpiWithUsd(stats.chiffreAffaires, stats.codeDevisePrincipale, stats.syntheseUsd?.chiffreAffaires)
```

Financier :

```ts
formatKpiWithUsd(
  dashboard.globalStatistiques.chiffreAffairesTotal,
  dashboard.codeDevisePrincipale,
  dashboard.globalStatistiques.syntheseUsd?.chiffreAffairesTotal,
)
```

---

## 7. Flutter

```dart
class EquivalentUsd {
  EquivalentUsd({
    required this.conversionUsdDisponible,
    this.montantEquivalentUsd,
    this.tauxVersUsd,
    this.dateTaux,
  });

  final bool conversionUsdDisponible;
  final double? montantEquivalentUsd;
  final double? tauxVersUsd;
  final DateTime? dateTaux;

  factory EquivalentUsd.fromJson(Map<String, dynamic>? json) {
    if (json == null) {
      return EquivalentUsd(conversionUsdDisponible: false);
    }
    return EquivalentUsd(
      conversionUsdDisponible: json['conversionUsdDisponible'] == true,
      montantEquivalentUsd: (json['montantEquivalentUsd'] as num?)?.toDouble(),
      tauxVersUsd: (json['tauxVersUsd'] as num?)?.toDouble(),
      dateTaux: json['dateTaux'] != null ? DateTime.parse(json['dateTaux'] as String) : null,
    );
  }
}

String formatKpiWithUsd(num montant, String? codeDevise, EquivalentUsd? eq) {
  final principal = '${montant.toStringAsFixed(0)} ${codeDevise ?? ''}'.trim();
  if (eq == null || !eq.conversionUsdDisponible || eq.montantEquivalentUsd == null) {
    return principal;
  }
  return '$principal (≈ ${eq.montantEquivalentUsd!.toStringAsFixed(2)} USD)';
}
```

---

## 8. Prérequis métier

Pour `conversionUsdDisponible: true` (société dont la principale n’est pas USD) :

1. Devise `USD` **active** pour la société (`POST /api/Devise/devises`)
2. Taux **principale → USD** (`POST /api/Devise/taux-change`, ex. `CDF` → `USD`)

Si la principale est déjà `USD`, aucun taux n’est requis (`tauxVersUsd = 1`).

Si une société d’un agrégat Super-Admin / Financier n’a pas de taux : **tout** le bloc agrégé passe à `conversionUsdDisponible: false`.

---

## 9. Checklist QA

- [ ] Devise USD + taux CDF→USD : stats financières affichent `≈ … USD` à côté du CA / arriérés / payé / dû
- [ ] Sans taux (ou USD inactif) : `conversionUsdDisponible: false`, l’UI n’affiche pas d’USD
- [ ] Principale = USD : équivalent = montant arrondi, `tauxVersUsd = 1`
- [ ] Super-Admin / Financier multi-sociétés : somme USD, `tauxVersUsd` null si plus d’une société
- [ ] Une société sans taux dans l’agrégat → USD masqué sur les totaux globaux
- [ ] `evolutionMensuelle` / `repartitionPaiements` / Caissier / dépenses : pas d’équivalent USD
- [ ] Le front n’appelle pas `preview-conversion` pour ces KPI
