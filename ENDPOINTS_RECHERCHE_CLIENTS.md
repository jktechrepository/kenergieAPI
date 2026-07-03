# 🔍 Endpoint de Recherche des Clients

## 📋 Description

Un **seul endpoint** de recherche optimisé pour trouver des clients par société avec recherche multi-champs incluant `CodeCons`.

## 🚀 Endpoints Disponibles

### 1️⃣ **Recherche Multi-Champs (Complète)**
```
GET /api/Client/societe/{idSociete}/recherche?searchTerm={searchTerm}&includeInactive={includeInactive}
```

### 2️⃣ **Recherche Multi-Champs (Paginée)** - **NOUVEAU**
```
GET /api/Client/societe/{idSociete}/paged?searchTerm={searchTerm}&includeInactive={includeInactive}&page=1&pageSize=20
```

## 📝 Paramètres

### Paramètres Communs
- **`idSociete`** (int, route, obligatoire) : ID de la société pour filtrer les clients
- **`searchTerm`** (string, query, optionnel) : Terme de recherche multi-champs
- **`includeInactive`** (bool, query, optionnel) : Inclure les clients inactifs (défaut = false) - **NOUVEAU**

### Paramètres Pagination (endpoint /paged uniquement)
- **`page`** (int, query, optionnel) : Numéro de page (défaut = 1)
- **`pageSize`** (int, query, optionnel) : Taille de page (défaut = 20, max = 100)
- **`sortBy`** (string, query, optionnel) : Champ de tri (NomClient, DateCreation, IdClient)
- **`sortDescending`** (bool, query, optionnel) : Tri descendant (défaut = true)

## 🎯 **Fonctionnalités**

### 🔍 **Recherche Multi-Champs** (`/recherche?searchTerm=...`)

**Champs de recherche :**
- ✅ **NomClient** : Recherche partielle (contains)
- ✅ **CodeCons** : Recherche partielle (contains) - **NOUVEAU**
- ✅ **AdresseClient** : Recherche partielle (contains) - **NOUVEAU**
- ✅ **Téléphone** : Recherche partielle (contains) - **NOUVEAU**
- ✅ **EmailClient** : Recherche partielle (contains) - **NOUVEAU**

**Filtres automatiques :**
- ✅ `Statut == true` (clients non supprimés)
- ✅ `IsActif == true` (clients actifs uniquement, sauf si `includeInactive=true`)
- ✅ `IdSociete` correspondant via les usages
- ✅ Relations complètes incluses (Usages, Catégories, Axe, Cabine)

## 📊 **Performances**

### 🚀 **Optimisations**
- **Requête unique** avec tous les includes nécessaires
- **Indexation** implicite sur les champs de recherche
- **Filtrage côté serveur** pour limiter le volume
- **Tri optimisé** par `DateCreation` descendant

### 📈 **Cas d'usage**
```bash
# Recherche par nom (clients actifs uniquement)
GET /api/Client/societe/1/recherche?searchTerm=jean

# Recherche par CodeCons (AVEC SLASH) - NOUVEAU
GET /api/Client/societe/1/recherche?searchTerm=ABC/12345

# Recherche TOUS les clients (actifs + inactifs) - NOUVEAU
GET /api/Client/societe/1/recherche?searchTerm=dupont&includeInactive=true

# Recherche paginée par CodeCons (AVEC SLASH) - NOUVEAU
GET /api/Client/societe/1/paged?searchTerm=ABC/12345&page=1&pageSize=20

# Recherche paginée par nom avec tri et clients inactifs - NOUVEAU
GET /api/Client/societe/1/paged?searchTerm=dupont&page=2&pageSize=50&sortBy=NomClient&sortDescending=false&includeInactive=true

# Recherche par adresse
GET /api/Client/societe/1/recherche?searchTerm=15 rue

# Recherche par téléphone
GET /api/Client/societe/1/recherche?searchTerm=0612345678
```

## 🎯 **Exemples d'Utilisation**

### 📋 **Recherche Multi-Champs (Complète)**
```bash
# Recherche par nom
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=dupont" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par CodeCons (AVEC SLASH) - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=ABC%2F12345" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche TOUS les clients (actifs + inactifs) - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=dupont&includeInactive=true" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par adresse (NOUVEAU)
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=15 rue" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par téléphone (NOUVEAU)
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=0612345678" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par email (NOUVEAU)
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=jean@email.com" \
  -H "Authorization: VOTRE_TOKEN_JWT"
```

### 📄 **Recherche Multi-Champs (Paginée)**
```bash
# Recherche paginée par CodeCons (AVEC SLASH) - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/paged?searchTerm=ABC%2F12345&page=1&pageSize=20" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche paginée par nom avec tri et clients inactifs - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/paged?searchTerm=dupont&page=2&pageSize=50&sortBy=NomClient&sortDescending=false&includeInactive=true" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche paginée simple
curl -X GET "https://localhost:7110/api/Client/societe/1/paged?page=1&pageSize=10" \
  -H "Authorization: VOTRE_TOKEN_JWT"
```

## 📄 **Format de Réponse**

### ✅ **Succès (200 OK)**
```json
[
  {
    "idClient": 1,
    "nomClient": "Jean Dupont",
    "adresseClient": "15 Rue de la Paix",
    "telephone": "0612345678",
    "emailClient": "jean.dupont@email.com",
    "codeCons": "ABC123456",
    "statut": true,
    "isActif": true,
    "dateCreation": "2024-01-15T10:30:00Z",
    "axe": {
      "idAxe": 1,
      "codeAxe": "AXE001",
      "nomAxe": "Axe Nord"
    },
    "clientsUsages": [...]
  }
]
```

### ❌ **Erreurs**
```json
// Société non trouvée
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Not Found",
  "status": 404,
  "detail": "Société avec ID 999 non trouvée"
}

// Non autorisé
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Token JWT invalide ou expiré"
}
```

## 🔐 **Sécurité**

### **Authentification Requise**
- **Bearer Token JWT** obligatoire dans l'en-tête `Authorization`
- **Rôles autorisés** : Tous les rôles authentifiés
- **Auto Bearer** : Le middleware ajoute automatiquement "Bearer" si oublié

### **Audit Trail**
- ✅ Toutes les recherches sont tracées dans `AuditLog`
- Informations enregistrées : utilisateur, société, terme recherché, timestamp

## 🚨 **Points d'Attention**

### **Performance**
- **Recherche multi-champs** : Plus gourmande mais plus flexible
- **Recherche CodeCons exact** : Optimisée et rapide
- **Recherche partielle** : Utilise `LIKE` en base de données

### **Sécurité**
- **Validation** des paramètres d'entrée
- **Protection** contre les injections SQL (via Entity Framework)
- **Limitation** aux sociétés autorisées

## 🔄 **Évolutions Prévues**

### **Phase 2 (Court terme)**
- **Recherche floue** (fuzzy search)
- **Suggestion automatique** de corrections orthographiques
- **Recherche par plage** (dates, montants)

### **Phase 3 (Moyen terme)**
- **Indexation全文** (full-text search)
- **Recherche avancée** avec filtres combinés
- **Cache de recherche** pour améliorer la performance

## 🧪 **Tests Recommandés**

### **Cas de test obligatoires**
1. **Recherche vide** : `/recherche/` → Retourne liste complète
2. **Recherche infructueuse** : `/recherche/inexistant` → Liste vide
3. **CodeCons exact** : `/codecons/EXISTANT` → Client trouvé
4. **CodeCons inexistant** : `/codecons/INEXISTANT` → Liste vide
5. **Société invalide** : ID 999 → 404 Not Found
6. **Non autorisé** : Sans token → 401 Unauthorized

### **Tests de performance**
1. **Recherche avec 1000+ clients** → Vérifier temps de réponse < 2s
2. **Recherche multi-champs simultanée** → Vérifier optimisation SQL
3. **Recherche CodeCons** → Vérifier utilisation d'index

---

**🎯 Les endpoints de recherche sont maintenant optimisés et prêts à l'emploi !**
