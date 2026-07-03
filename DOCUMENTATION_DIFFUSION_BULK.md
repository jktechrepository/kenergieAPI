# 📋 Documentation - Diffusion en Masse des Factures

## 🎯 Vue d'ensemble

Nouvel endpoint simplifié pour diffuser toutes les factures en attente de diffusion d'une société en une seule opération.

**Endpoint :** `POST /api/Facture/societe/{idSociete}/diffusion/bulk`  
**Autorisation :** Super-Admin, Admin, Gerant

---

## ✨ Avantages

### Simplicité
- ✅ **Un seul paramètre** : `idSociete`
- ✅ **Pas besoin de spécifier chaque facture** individuellement
- ✅ **Traitement automatique** de toutes les factures en attente

### Efficacité
- ✅ **Traitement en batch** : Toutes les factures sont traitées en une seule requête
- ✅ **Mise en queue asynchrone** : Ne bloque pas la réponse API
- ✅ **Gestion des erreurs** : Continue même si une facture échoue

### Sécurité
- ✅ **Vérification de société** : Seules les factures de la société sont diffusées
- ✅ **Filtrage automatique** : Seules les factures avec `estDiffusee = false` sont traitées

---

## 📡 Utilisation

### Requête

```http
POST /api/Facture/societe/1/diffusion/bulk
Authorization: Bearer {token}
```

**Paramètres :**
- `idSociete` (path) : Identifiant de la société

**Aucun body requis** - L'endpoint récupère automatiquement toutes les factures en attente.

### Réponse Succès (200 OK)

```json
{
  "success": true,
  "societeId": 1,
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
  "message": "Toutes les 3 facture(s) ont été mises en queue pour diffusion"
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
  "message": "Aucune facture en attente de diffusion pour cette société"
}
```

---

## 🔄 Flux de Traitement

```
1. POST /api/Facture/societe/{idSociete}/diffusion/bulk
   ↓
2. Vérifier que la société existe
   ↓
3. Récupérer toutes les factures avec :
   - Statut = true
   - EstDiffusee = false
   - Usage.CategorieClient.IdSociete = idSociete
   ↓
4. Pour chaque facture :
   a. Vérifier que l'usage existe
   b. Compter les clients (GetTotalClientsByUsageAsync)
   c. Marquer EstDiffusee = true, DateDiffusion = DateTime.Now
   d. Mettre à jour la facture
   e. Enqueue dans la queue asynchrone
   f. Ajouter à la liste des succès
   ↓
5. Si erreur sur une facture :
   - Ajouter à la liste des erreurs
   - Continuer avec les autres factures
   ↓
6. Retourner le résumé (succès/échecs)
```

---

## 📊 Critères de Sélection

Les factures sélectionnées doivent respecter **tous** ces critères :

1. ✅ `Statut = true` (facture active)
2. ✅ `EstDiffusee = false` (pas encore diffusée)
3. ✅ `Usage.CategorieClient.IdSociete = idSociete` (appartient à la société)

**Tri :** Les factures sont triées par `DateEmission` puis `DateCreation` (plus anciennes en premier).

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

### Exemple 1 : Diffuser toutes les factures en attente

```bash
curl -X POST "https://api.example.com/api/Facture/societe/1/diffusion/bulk" \
  -H "Authorization: Bearer {token}"
```

### Exemple 2 : Vérifier les factures en attente avant diffusion

```bash
# 1. Lister les factures en attente
GET /api/Facture/societe/1?estDiffusee=false

# 2. Diffuser toutes les factures en attente
POST /api/Facture/societe/1/diffusion/bulk
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
