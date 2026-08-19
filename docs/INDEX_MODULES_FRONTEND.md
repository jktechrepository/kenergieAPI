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

### 4. 📊 Module Statistiques
**Fichier :** [`API_DOCUMENTATION_STATISTIQUES.md`](./API_DOCUMENTATION_STATISTIQUES.md)

**Description :** Module de KPI et tableaux de bord pour la societe, avec vues generales, financieres, operationnelles, performance et consolidees.

**Fonctionnalités principales :**
- Vue globale des indicateurs de recouvrement
- Analyse financiere avec fenetre de periode
- Repartition operationnelle des clients
- Classement des agents caissiers (Top agents)
- Endpoint consolide pour alimenter un dashboard unique

**Endpoints principaux :**
- `GET /api/Statistiques/generales/{idSociete}` - KPI generaux
- `GET /api/Statistiques/financieres/{idSociete}` - KPI financiers
- `GET /api/Statistiques/operationnelles/{idSociete}` - KPI operationnels
- `GET /api/Statistiques/performance/{idSociete}` - KPI performance
- `GET /api/Statistiques/consolidees/{idSociete}` - KPI consolides

**Note multi-devises :** les montants sont consolidés en `codeDevisePrincipale` (défaut CDF).

---

### 5. Module Multi-devises
**Fichier :** [`API_DOCUMENTATION_MULTIDEVISE.md`](./API_DOCUMENTATION_MULTIDEVISE.md)

**Description :** Gestion des devises et taux de change par société, snapshots monétaires sur facturation/paiements, agrégats consolidés.

**Fonctionnalités principales :**
- CRUD devises par société + devise principale
- Taux de change datés + preview de conversion
- Snapshots devise sur Facture / ClientFacture / Paiement
- Contrainte phase 1 : paiement dans la même devise que la facture

**Endpoints principaux :**
- `GET /api/Devise/devises` - Lister les devises actives (lecture : Caissier, Financier, Admin…, **Client** = société liée au compte)
- `GET /api/Devise/preview-conversion` - Estimation de conversion (idem lecture)
- `POST /api/Devise/devises` - Créer une devise (Admin / Gérant / Super-Admin)
- `PUT /api/Devise/devises/{id}` - Modifier une devise (Admin / Gérant / Super-Admin)
- `PUT /api/Devise/societe/{idSociete}/devise-principale/{codeDevise}` - Bascule principale
- `POST /api/Devise/taux-change` - Créer un taux

**Guide frontend équivalents USD (stats / dashboards) :** [`FRONTEND_INTEGRATION_RAPPORT_USD.md`](./FRONTEND_INTEGRATION_RAPPORT_USD.md)

---

### 6. Module FlexPay (paiement électronique)
**Fichier :** [`API_DOCUMENTATION_FLEXPAY.md`](./API_DOCUMENTATION_FLEXPAY.md)

**Description :** Paiement Mobile Money / carte via FlexPay (initiation async + callback + finalisation Paiement).

**Fonctionnalités principales :**
- Config marchand par société
- Initiation MM / carte séparée du CASH
- Callback public idempotent
- Vérification secours par orderNumber

**Endpoints principaux :**
- `POST /api/Paiement/electronique` - Initier
- `GET /api/Paiement/electronique/{id}` - Statut pending
- `POST /api/FlexPay/callback` - Webhook public
- `GET /api/FlexPay/verifier/{orderNumber}` - Secours

**Guide recette confirmation :** [`GUIDE_TEST_FLEXPAY_CONFIRMATION.md`](./GUIDE_TEST_FLEXPAY_CONFIRMATION.md)
- `CRUD /api/InfoPaiementSociete` - Config marchand

---

### 6bis. Notifications in-app (SignalR mobile)
**Fichier :** [`FRONTEND_INTEGRATION_NOTIFICATIONS_MOBILE.md`](./FRONTEND_INTEGRATION_NOTIFICATIONS_MOBILE.md)

**Description :** Connexion Flutter au hub `/hubs/notifications`, réception `ReceiveNotification`, marquage lu, hydratation REST.

**Fonctionnalités principales :**
- JWT + `accessTokenFactory` / query `access_token`
- Événement canonique `ReceiveNotification`
- `MarkNotificationAsRead` (persisté en base)
- Pattern REST (historique) + SignalR (live) + FCM (background)

**Hub / endpoints :**
- Hub `GET/WS` `{BASE}/hubs/notifications`
- `GET /api/Notification/destinataire/{id}` — historique
- `GET /api/Notification/destinataire/{id}/non-lues` — badge
- `PUT /api/Notification/{id}/marquer-lue` — fallback REST

**Guide SignalR web (Vue/React) :** [`SIGNALR_FRONTEND_GUIDE.md`](./SIGNALR_FRONTEND_GUIDE.md)

---

### 7. Module Dépenses
**Fichier :** [`API_DOCUMENTATION_DEPENSE.md`](./API_DOCUMENTATION_DEPENSE.md)

**Description :** Sorties d'argent par société : saisie par le Financier, validation / refus par Admin et Gérant. Seules les dépenses `Validee` entrent dans les totaux et dashboards.

**Fonctionnalités principales :**
- Workflow `EnAttente` → `Validee` / `Annulee`
- Catégories de dépense par société
- Rapport du mois + `syntheseDepense`
- KPI dashboards (sorties, file d'attente)

**Endpoints principaux :**
- `GET /api/Depense` - Liste paginée
- `GET /api/Depense/mois` - Rapport du mois (`statut` défaut `Validee`)
- `POST /api/Depense` - Créer (Financier → `EnAttente`)
- `POST /api/Depense/{id}/valider` - Valider (Admin, Gérant)
- `POST /api/Depense/{id}/refuser` - Refuser (Admin, Gérant)
- `CRUD /api/CategorieDepense` - Catégories

**Guide frontend (Vue.js + Flutter) :** [`FRONTEND_INTEGRATION_DEPENSE.md`](./FRONTEND_INTEGRATION_DEPENSE.md)

---

### 8. Guide d'intégration Frontend (Vue.js + Flutter)
**Fichier :** [`FRONTEND_INTEGRATION_MULTIDEVISE_FLEXPAY.md`](./FRONTEND_INTEGRATION_MULTIDEVISE_FLEXPAY.md)

**Description :** Guide pratique pour brancher Multi-devises et FlexPay côté web (Vue.js) et mobile (Flutter) : endpoints, modèles, UX, polling, erreurs, checklist QA.

**Contenu principal :**
- Règles métier communes Web/Mobile
- Services / stores Vue (Pinia) + composable polling
- Services Dio Flutter + Timer polling + url_launcher
- Parcours CASH vs électronique
- Checklist QA

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
| Statistiques | 2.0 | 10 juillet 2026 |

---

**Bon développement ! 🚀**

