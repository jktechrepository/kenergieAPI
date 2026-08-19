# 📋 Documentation - Diffusion en Masse des Factures

## 🎯 Vue d'ensemble

Nouvel endpoint pour diffuser les factures en attente d'une société **pour une période donnée** (année / mois d'émission).

**Endpoint :** `POST /api/Facture/societe/{idSociete}/diffusion/bulk`  
**Autorisation :** Super-Admin, Admin, Gerant (permission `Facture.Update`)

---

## Avantages

### Simplicité
- Un paramètre path : `idSociete`
- Query optionnels : `annee`, `mois` (sinon **mois précédent** par défaut)
- Pas besoin de spécifier chaque facture individuellement

### Efficacité
- Traitement en batch dans une seule requête
- Mise en queue asynchrone (ne bloque pas la réponse API)
- Gestion des erreurs : continue même si une facture échoue

### Sécurité
- Vérification de société
- Filtrage : `estDiffusee = false` + période `MoisEmission` / `AnneesEmission`

---

## Utilisation

### Requête (défaut = mois calendaire précédent)

```http
POST /api/Facture/societe/1/diffusion/bulk
Authorization: Bearer {token}
```

Exemple le 15/08/2026 → diffuse la période **07/2026**.

### Requête avec période explicite

```http
POST /api/Facture/societe/1/diffusion/bulk?annee=2026&mois=5
Authorization: Bearer {token}
```

**Paramètres :**
- `idSociete` (path, obligatoire) : société cible
- `annee` (query, optionnel) : à fournir **avec** `mois`
- `mois` (query, optionnel) : 1–12, à fournir **avec** `annee`

Si un seul de `annee` / `mois` est fourni → **400**.

**Aucun body requis.**

### Réponse Succès (200 OK)

```json
{
  "success": true,
  "societeId": 1,
  "annee": 2026,
  "mois": 7,
  "totalFactures": 3,
  "facturesEnQueue": 3,
  "facturesEchecs": 0,
  "facturesDiffusees": [
    {
      "factureId": 1,
      "numeroFacture": "FAC-RES-0125-0001",
      "usageId": 1,
      "nomUsage": "Résidentiel",
      "totalClients": 5
    },
    {
      "factureId": 2,
      "numeroFacture": "FAC-COM-0125-0001",
      "usageId": 2,
      "nomUsage": "Commercial",
      "totalClients": 3
    },
    {
      "factureId": 3,
      "numeroFacture": "FAC-IND-0125-0001",
      "usageId": 3,
      "nomUsage": "Industriel",
      "totalClients": 2
    }
  ],
  "erreurs": [],
  "duree": "0.15s",
  "message": "Toutes les 3 facture(s) de 07/2026 ont été mises en queue pour diffusion"
}
```

### Réponse Partielle (207 Multi-Status)

Si certaines factures ont échoué :

```json
{
  "success": true,
  "societeId": 1,
  "totalFactures": 3,
  "facturesEnQueue": 2,
  "facturesEchecs": 1,
  "facturesDiffusees": [
    {
      "factureId": 1,
      "numeroFacture": "FAC-RES-0125-0001",
      "usageId": 1,
      "nomUsage": "Résidentiel",
      "totalClients": 5
    },
    {
      "factureId": 2,
      "numeroFacture": "FAC-COM-0125-0001",
      "usageId": 2,
      "nomUsage": "Commercial",
      "totalClients": 3
    }
  ],
  "erreurs": [
    {
      "factureId": 3,
      "numeroFacture": "FAC-IND-0125-0001",
      "message": "La facture n'a pas d'usage associé"
    }
  ],
  "duree": "0.12s",
  "message": "2 facture(s) mise(s) en queue, 1 échec(s)"
}
```

### Réponse Aucune Facture (200 OK)

Si aucune facture en attente :

```json
{
  "success": true,
  "societeId": 1,
  "totalFactures": 0,
  "facturesEnQueue": 0,
  "facturesEchecs": 0,
  "facturesDiffusees": [],
  "erreurs": [],
  "duree": "0.02s",
  "message": "Aucune facture en attente de diffusion pour la société 1 sur la période 07/2026"
}
```

---

## 🔄 Flux de Traitement

```
1. POST /api/Facture/societe/{idSociete}/diffusion/bulk[?annee=&mois=]
   ↓
2. Vérifier que la société existe
   ↓
3. Résoudre la période (défaut = mois calendaire précédent)
   ↓
4. Récupérer les factures avec :
   - Statut = true
   - EstDiffusee = false
   - MoisEmission / AnneesEmission = période
   - Usage.CategorieClient.IdSociete = idSociete
   ↓
5. Pour chaque facture :
   a. Vérifier usage + ClientFacture
   b. Compter les clients
   c. Marquer EstDiffusee = true, DateDiffusion = now
   d. Enqueue async
   ↓
6. Retourner le résumé (succès/échecs + annee/mois)
```

---

## 📊 Critères de Sélection

Les factures sélectionnées doivent respecter **tous** ces critères :

1. `Statut = true` (facture active)
2. `EstDiffusee = false` (pas encore diffusée)
3. `MoisEmission` / `AnneesEmission` = période résolue (query ou mois précédent)
4. `Usage.CategorieClient.IdSociete = idSociete`

**Tri :** `DateEmission` puis `DateCreation` (plus anciennes en premier).

---

## ⚙️ Comportement

### Mise à jour automatique

Lorsqu'une facture est mise en queue pour diffusion :
- ✅ `EstDiffusee` est mis à `true`
- ✅ `DateDiffusion` est mis à `DateTime.Now`
- ✅ La facture est sauvegardée immédiatement

### Traitement asynchrone

- ✅ Les notifications sont envoyées en arrière-plan via la queue
- ✅ La réponse API est immédiate (ne bloque pas)
- ✅ Les statistiques détaillées seront disponibles après traitement

### Gestion des erreurs

- ✅ Si une facture échoue, les autres continuent
- ✅ Les erreurs sont listées dans la réponse
- ✅ Le processus ne s'arrête pas en cas d'erreur

---

## 🔍 Exemples d'Utilisation

### Exemple 1 : Diffuser le mois précédent (défaut)

```bash
curl -X POST "https://api.example.com/api/Facture/societe/1/diffusion/bulk" \
  -H "Authorization: Bearer {token}"
```

### Exemple 2 : Diffuser une période explicite

```bash
curl -X POST "https://api.example.com/api/Facture/societe/1/diffusion/bulk?annee=2026&mois=5" \
  -H "Authorization: Bearer {token}"
```

---

## ⚠️ Points d'attention

### 1. Factures déjà diffusées

Les factures avec `EstDiffusee = true` sont **automatiquement exclues**. Pour forcer une nouvelle diffusion, utilisez l'endpoint individuel avec `forcer=true`.

### 2. Factures sans usage

Les factures sans usage associé seront listées dans les erreurs et ne seront pas diffusées.

### 3. Performance

Pour un grand nombre de factures (100+), le traitement peut prendre quelques secondes. La réponse est toujours immédiate, mais le traitement en arrière-plan peut prendre du temps.

### 4. Transaction

Chaque facture est mise à jour individuellement. Si une facture échoue, les autres continuent. Il n'y a pas de rollback global.

---

## 📝 Comparaison avec l'Endpoint Individuel

| Aspect | Endpoint Individuel | Endpoint Bulk |
|--------|---------------------|---------------|
| **Route** | `/api/Facture/{idFacture}/societe/{idSociete}/diffusion` | `/api/Facture/societe/{idSociete}/diffusion/bulk` |
| **Paramètres** | `idFacture` + `idSociete` + `forcer?` | `idSociete` uniquement |
| **Factures traitées** | 1 facture | Toutes les factures en attente |
| **Cas d'usage** | Diffusion ciblée | Diffusion en masse |
| **Forcer** | Oui (paramètre `forcer`) | Non (seulement les non-diffusées) |

---

## ✅ Checklist de Validation

- [x] Endpoint créé et fonctionnel
- [x] Filtre les factures avec `EstDiffusee = false`
- [x] Met à jour `EstDiffusee = true` et `DateDiffusion`
- [x] Gestion des erreurs par facture
- [x] Audit pour chaque facture et global
- [x] Réponse détaillée avec succès/échecs
- [x] Traitement asynchrone via queue

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
