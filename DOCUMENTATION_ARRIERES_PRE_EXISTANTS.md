# 📋 Documentation : Gestion des Arriérés Pré-Existants

## 🎯 Vue d'ensemble

Les arriérés pré-existants sont des montants dûs par les clients qui existaient **avant l'arrivée du système informatisé**. Ces arriérés ne sont pas liés à une facture système (pas de `IdFacture`).

---

## 🔑 Caractéristiques

### Modèle `ClientFacture`

Pour les arriérés pré-existants :
- ✅ `IdFacture = NULL` (pas de facture système)
- ✅ `EstArrierePreExistant = true`
- ✅ `Description` : Description/libellé de l'arriéré
- ✅ `Montant` : Montant total de l'arriéré
- ✅ `MontantPaye` : Montant déjà payé (initialisé à 0)
- ✅ `MontantDu` : Montant restant dû (initialisé à Montant)

---

## 📡 Endpoints API

### 1. Créer un arriéré pré-existant

**Endpoint :** `POST /api/ClientFacture/pre-existant`

**Autorisation :** Super-Admin, Admin

**Body :**
```json
{
  "IdClient": 123,
  "Montant": 50000.00,
  "Mois": "01",
  "Annees": 2023,
  "DateEmission": "2023-01-15T00:00:00",
  "Description": "Arriérés avant informatisation - Période janvier 2023"
}
```

**Réponse :**
```json
{
  "idClientFacture": 456,
  "idFacture": null,
  "idClient": 123,
  "montant": 50000.00,
  "montantPaye": 0,
  "montantDu": 50000.00,
  "mois": "01",
  "annees": 2023,
  "dateEmission": "2023-01-15T00:00:00",
  "estArrierePreExistant": true,
  "description": "Arriérés avant informatisation - Période janvier 2023",
  "statut": true
}
```

---

### 2. Enregistrer un paiement sur un arriéré pré-existant

**Endpoint :** `POST /api/ClientFacture/{idClientFacture}/paiement`

**Autorisation :** Super-Admin, Admin, Caissier, Financier

**Body :**
```json
{
  "MontantPaye": 20000.00,
  "DatePaiement": "2025-01-05T10:30:00",
  "MethodePaiement": "Mobile Money",
  "ReferenceTransaction": "MM-20250105-001",
  "Commentaire": "Paiement partiel arriéré pré-existant"
}
```

**Réponse :**
```json
{
  "message": "Paiement enregistré avec succès",
  "clientFacture": {
    "idClientFacture": 456,
    "montant": 50000.00,
    "montantPaye": 20000.00,
    "montantDu": 30000.00,
    ...
  },
  "montantPaye": 20000.00,
  "montantDu": 30000.00
}
```

**Validation :**
- ✅ Vérifie que le montant payé ne dépasse pas le montant dû
- ✅ Met à jour automatiquement `MontantPaye` et `MontantDu`
- ✅ Enregistre l'audit trail

---

### 3. Récupérer les arriérés pré-existants d'un client

**Endpoint :** `GET /api/ClientFacture/client/{idClient}/pre-existants`

**Autorisation :** Authentifié

**Réponse :**
```json
[
  {
    "idClientFacture": 456,
    "idFacture": null,
    "idClient": 123,
    "montant": 50000.00,
    "montantPaye": 20000.00,
    "montantDu": 30000.00,
    "mois": "01",
    "annees": 2023,
    "dateEmission": "2023-01-15T00:00:00",
    "estArrierePreExistant": true,
    "description": "Arriérés avant informatisation - Période janvier 2023",
    "statut": true
  }
]
```

---

### 4. Récupérer tous les arriérés d'un client (inclut les pré-existants)

**Endpoint :** `GET /api/Client/{id}/arrieres`

**Autorisation :** Authentifié

**Réponse :**
```json
{
  "idClient": 123,
  "nomClient": "Jean Dupont",
  "telephone": "+243900000000",
  "emailClient": "jean.dupont@example.com",
  "nombreFacturesImpayees": 3,
  "totalArrieres": 80000.00,
  "montantTotalFactures": 100000.00,
  "montantTotalPaye": 20000.00,
  "facturesImpayees": [
    {
      "idFacture": 0,
      "numeroFacture": "ARRIERE-PRE-EXISTANT",
      "dateEmission": "2023-01-15T00:00:00",
      "moisEmission": 1,
      "anneesEmission": 2023,
      "montantTotal": 50000.00,
      "montantPaye": 20000.00,
      "montantDu": 30000.00,
      "joursRetard": 720,
      "nomCategorie": "Arriéré pré-existant"
    },
    {
      "idFacture": 789,
      "numeroFacture": "FAC-RES-0124-0001",
      "dateEmission": "2024-01-15T00:00:00",
      "moisEmission": 1,
      "anneesEmission": 2024,
      "montantTotal": 30000.00,
      "montantPaye": 0,
      "montantDu": 30000.00,
      "joursRetard": 355,
      "nomCategorie": "Résidentiel"
    },
    {
      "idFacture": 790,
      "numeroFacture": "FAC-RES-0224-0001",
      "dateEmission": "2024-02-15T00:00:00",
      "moisEmission": 2,
      "anneesEmission": 2024,
      "montantTotal": 20000.00,
      "montantPaye": 0,
      "montantDu": 20000.00,
      "joursRetard": 325,
      "nomCategorie": "Résidentiel"
    }
  ]
}
```

**Note :** Les arriérés pré-existants sont automatiquement inclus dans les calculs d'arriérés.

---

## 🔄 Flux de données

### Création d'un arriéré pré-existant

```
1. POST /api/ClientFacture/pre-existant
   ↓
2. Validation (client existe, montant > 0, etc.)
   ↓
3. Création de ClientFacture avec :
   - IdFacture = NULL
   - EstArrierePreExistant = true
   - MontantPaye = 0
   - MontantDu = Montant
   ↓
4. Audit trail
   ↓
5. Retour de la ClientFacture créée
```

### Paiement sur un arriéré pré-existant

```
1. POST /api/ClientFacture/{idClientFacture}/paiement
   ↓
2. Validation :
   - ClientFacture existe
   - EstArrierePreExistant = true
   - MontantPaye <= MontantDu
   ↓
3. Mise à jour de ClientFacture :
   - MontantPaye += nouveau paiement
   - MontantDu = Montant - MontantPaye
   ↓
4. Audit trail
   ↓
5. Retour de la ClientFacture mise à jour
```

### Calcul des arriérés (inclut les pré-existants)

```
1. GET /api/Client/{id}/arrieres
   ↓
2. ArrieresService.GetArrieresByClientAsync(idClient)
   ↓
3. ClientFactureService.GetByClientWithArrieresAsync(idClient)
   - Requête SQL : WHERE IdClient = @idClient 
                    AND Statut = true 
                    AND MontantDu > 0
   - Inclut automatiquement les arriérés pré-existants
   ↓
4. Conversion en FactureImpayeeDto
   - Arriérés pré-existants : NumeroFacture = "ARRIERE-PRE-EXISTANT"
   - NomCategorie = Description ou "Arriéré pré-existant"
   ↓
5. Retour des arriérés (factures système + pré-existants)
```

---

## ⚠️ Points d'attention

### 1. Paiements sur arriérés pré-existants

**Problème :** Les paiements normaux (`POST /api/Paiement`) nécessitent un `IdFacture`, ce qui n'existe pas pour les arriérés pré-existants.

**Solution :** Utiliser l'endpoint spécial `POST /api/ClientFacture/{idClientFacture}/paiement` qui met à jour directement la `ClientFacture`.

### 2. Validation des montants

L'endpoint de paiement vérifie que :
- ✅ Le montant payé ne dépasse pas le montant dû
- ✅ La `ClientFacture` est bien un arriéré pré-existant

### 3. Inclusion dans les calculs

Les arriérés pré-existants sont **automatiquement inclus** dans :
- ✅ `GET /api/Client/{id}/arrieres` (tous les arriérés)
- ✅ `GET /api/Client/{id}/factures-impayees` (liste des factures impayées)
- ✅ `GET /api/Client/{id}/factures-impayees/paged` (liste paginée)

Ils apparaissent avec :
- `IdFacture = 0`
- `NumeroFacture = "ARRIERE-PRE-EXISTANT"`
- `NomCategorie = Description` ou `"Arriéré pré-existant"`

---

## 📊 Exemples d'utilisation

### Exemple 1 : Créer un arriéré pré-existant

```bash
curl -X POST "https://api.example.com/api/ClientFacture/pre-existant" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "IdClient": 123,
    "Montant": 50000.00,
    "Mois": "01",
    "Annees": 2023,
    "DateEmission": "2023-01-15T00:00:00",
    "Description": "Arriérés avant informatisation"
  }'
```

### Exemple 2 : Enregistrer un paiement partiel

```bash
curl -X POST "https://api.example.com/api/ClientFacture/456/paiement" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "MontantPaye": 20000.00,
    "MethodePaiement": "Mobile Money",
    "ReferenceTransaction": "MM-20250105-001"
  }'
```

### Exemple 3 : Récupérer tous les arriérés (inclut pré-existants)

```bash
curl -X GET "https://api.example.com/api/Client/123/arrieres" \
  -H "Authorization: Bearer {token}"
```

---

## 🔍 Vérifications

### Vérifier qu'un arriéré pré-existant est inclus dans les calculs

1. Créer un arriéré pré-existant pour un client
2. Appeler `GET /api/Client/{id}/arrieres`
3. Vérifier que l'arriéré apparaît dans `facturesImpayees` avec `numeroFacture = "ARRIERE-PRE-EXISTANT"`

### Vérifier la cohérence après paiement

1. Créer un arriéré pré-existant de 50 000 FCFA
2. Enregistrer un paiement de 20 000 FCFA
3. Vérifier que :
   - `MontantPaye = 20000.00`
   - `MontantDu = 30000.00`
   - `Montant = 50000.00`

---

## 📝 Notes importantes

1. **Les arriérés pré-existants ne sont pas liés à une facture système**
   - `IdFacture = NULL`
   - Pas de `NumeroFacture` système
   - Pas de lien avec un `Usage`

2. **Les paiements sur arriérés pré-existants ne créent pas de `Paiement`**
   - Ils mettent à jour directement la `ClientFacture`
   - Pas d'historique dans la table `Paiements`
   - L'historique est dans l'audit trail

3. **Les arriérés pré-existants sont inclus automatiquement dans les calculs**
   - Aucune action supplémentaire nécessaire
   - Ils apparaissent dans tous les endpoints d'arriérés

4. **Format du champ `Mois`**
   - Format recommandé : "01", "02", ..., "12"
   - Format accepté : "Janvier", "Février", etc.
   - Le format numérique est préféré pour le tri et les filtres

---

## ✅ Checklist de validation

- [ ] Créer un arriéré pré-existant
- [ ] Vérifier qu'il apparaît dans `GET /api/Client/{id}/arrieres`
- [ ] Enregistrer un paiement partiel
- [ ] Vérifier que `MontantPaye` et `MontantDu` sont mis à jour
- [ ] Vérifier que le total des arriérés inclut les pré-existants
- [ ] Tester la validation (montant payé > montant dû)

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
