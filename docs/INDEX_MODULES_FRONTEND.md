# 📚 Index des Documentations API - Modules Frontend

## Vue d'ensemble

Ce document répertorie toutes les documentations API disponibles pour les équipes frontend, organisées par module.

---

## 📖 Documentations disponibles

### 1. 📢 Module Communication
**Fichier :** [`API_DOCUMENTATION_COMMUNICATION.md`](./API_DOCUMENTATION_COMMUNICATION.md)

**Description :** Module permettant aux sociétés d'envoyer des messages ciblés (push, SMS, email, in-app) à leurs clients.

**Fonctionnalités principales :**
- Création de campagnes de communication
- Ciblage par catégorie, zone, statut actif, usage
- Envoi immédiat ou programmé
- Statistiques d'envoi par canal
- Prévisualisation des destinataires

**Endpoints principaux :**
- `POST /api/CommunicationCampaign` - Créer une campagne
- `GET /api/CommunicationCampaign` - Lister les campagnes
- `GET /api/CommunicationCampaign/{id}` - Détails d'une campagne
- `POST /api/CommunicationCampaign/{id}/execute` - Exécuter une campagne
- `GET /api/CommunicationCampaign/{id}/preview` - Prévisualiser les destinataires

---

### 2. 📝 Module Plainte Client
**Fichier :** [`API_DOCUMENTATION_PLAINTE_CLIENT.md`](./API_DOCUMENTATION_PLAINTE_CLIENT.md)

**Description :** Module permettant aux clients de signaler des problèmes ou plaintes à l'équipe d'intervention.

**Fonctionnalités principales :**
- Création de plaintes par les clients
- Lien avec les signalements de panne
- Gestion des plaintes par l'équipe d'intervention
- Assignation d'agents
- Suivi des statuts (En attente, En cours, Résolu, Fermé)
- Gestion des priorités

**Endpoints principaux :**
- `POST /api/PlainteClient` - Créer une plainte (Client)
- `GET /api/PlainteClient/mes-plaintes` - Mes plaintes (Client)
- `GET /api/PlainteClient` - Lister toutes les plaintes (Équipe)
- `GET /api/PlainteClient/paged` - Liste paginée avec filtres
- `PATCH /api/PlainteClient/{id}/assigner` - Assigner un agent
- `PATCH /api/PlainteClient/{id}/resoudre` - Résoudre une plainte

---

### 3. ⚡ Module Panne Signalement
**Fichier :** [`API_DOCUMENTATION_PANNE_SIGNALEMENT.md`](./API_DOCUMENTATION_PANNE_SIGNALEMENT.md)

**Description :** Module permettant aux clients de signaler des pannes ou problèmes techniques sur le réseau électrique.

**Fonctionnalités principales :**
- Création de signalements de panne
- Gestion des niveaux d'importance
- Statut actif/clôturé
- Lien avec les plaintes clients

**Endpoints principaux :**
- `POST /api/PanneSignalement` - Créer un signalement
- `GET /api/PanneSignalement` - Lister les signalements
- `GET /api/PanneSignalement/paged` - Liste paginée avec filtres
- `GET /api/PanneSignalement/{id}` - Détails d'un signalement
- `PUT /api/PanneSignalement/{id}` - Modifier un signalement

---

## 🔐 Authentification commune

**Tous les endpoints nécessitent une authentification JWT.**

### Obtenir un token

**`POST /api/Utilisateur/authentifier`**

```json
{
  "emailOuTelephone": "admin@kenergie.cd",
  "motDePasse": "Admin",
  "fcmToken": "votre_fcm_token",
  "deviceType": "Mobile",
  "deviceModel": "iPhone 12",
  "osVersion": "iOS 15.0"
}
```

**Response :**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "expiresAt": "2025-12-15T18:01:40Z",
  "utilisateur": { ... },
  "permissions": [ ... ]
}
```

### Utiliser le token

Dans tous les appels API, ajoutez le header :
```
Authorization: Bearer {votre_accessToken}
```

---

## 📊 Codes de statut HTTP

| Code | Description | Action recommandée |
|------|-------------|-------------------|
| 200 | Succès | Traiter la réponse |
| 201 | Créé | Ressource créée avec succès |
| 204 | Pas de contenu | Opération réussie, pas de contenu |
| 400 | Requête invalide | Vérifier les données envoyées |
| 401 | Non authentifié | Reconnecter l'utilisateur |
| 403 | Accès refusé | Vérifier les permissions |
| 404 | Non trouvé | Vérifier l'ID de la ressource |
| 500 | Erreur serveur | Contacter l'équipe backend |

---

## 🔗 Relations entre modules

```
┌─────────────────────┐
│  PanneSignalement   │
│   (Signalement)      │
└──────────┬──────────┘
           │
           │ (peut être lié)
           │
           ▼
┌─────────────────────┐
│   PlainteClient     │
│    (Plainte)        │
└──────────┬──────────┘
           │
           │ (peut être assigné)
           │
           ▼
┌─────────────────────┐
│       Agent         │
│   (Intervention)     │
└─────────────────────┘

┌─────────────────────┐
│ CommunicationCampaign│
│   (Campagne)         │
└──────────┬──────────┘
           │
           │ (cible)
           │
           ▼
┌─────────────────────┐
│      Client         │
│   (Destinataire)     │
└─────────────────────┘
```

---

## 📝 Exemples d'intégration

### Exemple 1 : Workflow complet - Signalement → Plainte → Communication

```javascript
// 1. Client signale une panne
const panne = await fetch('/api/PanneSignalement', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    description: "Coupure d'électricité",
    typePanne: "Coupure totale",
    niveauImportance: "Critique"
  })
});

// 2. Client crée une plainte liée à la panne
const plainte = await fetch('/api/PlainteClient', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    idClient: 1,
    idPanneSignalement: panne.idPanneSignalement,
    titre: "Panne non résolue",
    priorite: "Urgente"
  })
});

// 3. Société envoie une communication aux clients affectés
const campagne = await fetch('/api/CommunicationCampaign', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    titre: "Avis de panne",
    contenu: "Une panne a été signalée dans votre zone",
    typeCampagne: "ALERTE",
    criteresCiblage: {
      zones: ["Kinshasa"],
      clientsActifs: true
    },
    activerPush: true,
    activerInApp: true
  })
});
```

---

## 🛠️ Outils de test

### Swagger UI
Accédez à la documentation interactive Swagger :
```
https://localhost:7110/swagger
```

### Collection Postman
Une collection Postman est disponible :
```
Kenergie_API_Collection.postman_collection.json
```

---

## 📞 Support

Pour toute question ou problème :
- **Documentation technique :** Consultez les fichiers Markdown dans `/docs`
- **Support backend :** Contactez l'équipe backend
- **Issues :** Créez un ticket dans le système de gestion des issues

---

## 📅 Versions

| Module | Version | Dernière mise à jour |
|--------|---------|---------------------|
| Communication | 2.0 | 14 décembre 2025 |
| Plainte Client | 2.0 | 14 décembre 2025 |
| Panne Signalement | 2.0 | 14 décembre 2025 |

---

**Bon développement ! 🚀**

