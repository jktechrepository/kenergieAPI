# 📚 Documentation Complète - Dashboards Kenergie API

## 🎯 Vue d'ensemble

Cette documentation complète couvre tous les dashboards et statistiques de l'API Kenergie avec des exemples d'intégration pour Flutter (mobile) et Vue.js (web).

---

## 📁 Structure des Fichiers

```
📂 Documentation Dashboards/
├── 📄 README_DASHBOARDS.md          # Ce fichier - Vue d'ensemble
├── 📄 DOCUMENTATION_DASHBOARDS.md   # Documentation complète des dashboards
├── 📄 INTEGRATION_FLUTTER.md       # Exemples d'intégration Flutter
├── 📄 INTEGRATION_VUEJS.md         # Exemples d'intégration Vue.js
└── 📄 STATISTIQUES_API.md          # Documentation des endpoints de statistiques
```

---

## 🚀 Quick Start

### 1. Authentification
```bash
# Obtenir un token JWT
curl -X POST "http://localhost:5000/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{
    "emailOuTelephone": "admin@kenergie.cd",
    "motDePasse": "Admin",
    "fcmToken": "string",
    "deviceType": "web",
    "deviceModel": "Test App",
    "osVersion": "1.0.0"
  }'
```

### 2. Dashboard Super-Admin
```bash
# Récupérer le dashboard Super-Admin
curl -X GET "http://localhost:5000/api/SuperAdmin/dashboard" \
  -H "Authorization: Bearer VOTRE_TOKEN"
```

### 3. Statistiques
```bash
# Récupérer les statistiques générales
curl -X GET "http://localhost:5000/api/Statistiques/generales/1" \
  -H "Authorization: Bearer VOTRE_TOKEN"
```

---

## 🏢 Types de Dashboards

### 1. **Super-Admin Dashboard**
- **Endpoint**: `GET /api/SuperAdmin/dashboard`
- **Rôles**: Super-Admin
- **Fonctionnalités**: Vue globale de toutes les sociétés, tendances, alertes critiques

### 2. **Gérant Dashboard**
- **Endpoint**: `GET /api/Gerant/dashboard/{idSociete}`
- **Rôles**: Gérant, Super-Admin
- **Fonctionnalités**: Focus sur une société, clients, performances

### 3. **Technicien Dashboard**
- **Endpoint**: `GET /api/Technicien/dashboard`
- **Rôles**: Technicien, Super-Admin
- **Fonctionnalités**: Interventions, pannes, performance personnelle

### 4. **Client Dashboard**
- **Endpoint**: `GET /api/ClientDashboard`
- **Rôles**: Client, Super-Admin
- **Fonctionnalités**: Factures, paiements, alertes personnelles

---

## 📈 Types de Statistiques

### 1. **Statistiques Générales**
- **Endpoint**: `GET /api/Statistiques/generales/{idSociete}`
- **Données**: Clients, factures, arriérés, taux de recouvrement

### 2. **Statistiques Financières**
- **Endpoint**: `GET /api/Statistiques/financieres/{idSociete}`
- **Données**: CA, évolution mensuelle, répartition paiements

### 3. **Statistiques Opérationnelles**
- **Endpoint**: `GET /api/Statistiques/operationnelles/{idSociete}`
- **Données**: Répartition clients, activité, statistiques factures

### 4. **Statistiques de Performance**
- **Endpoint**: `GET /api/Statistiques/performance/{idSociete}`
- **Données**: Taux recouvrement, top agents, performance mensuelle

### 5. **Statistiques Consolidées**
- **Endpoint**: `GET /api/Statistiques/consolidees/{idSociete}`
- **Données**: Toutes les statistiques en un seul appel

---

## 🎨 Composants UI Disponibles

### Flutter
- ✅ Widgets Dashboard complets
- ✅ Graphiques avec `fl_chart`
- ✅ Gestion des erreurs
- ✅ Material Design
- ✅ Responsive Design

### Vue.js
- ✅ Composants Dashboard complets
- ✅ Graphiques avec `Chart.js`
- ✅ Pinia Store pour la gestion d'état
- ✅ Composition API
- ✅ Responsive Design

---

## 🔐 Gestion de la Sécurité

### JWT Authentication
- **Durée de vie**: 2 heures (7200 secondes)
- **Refresh**: Non implémenté (reconnexion requise)
- **Stockage**: LocalStorage (web) / SecureStorage (mobile)

### Rôles et Permissions
- **Super-Admin**: Accès à tous les dashboards
- **Gérant**: Dashboard de sa société uniquement
- **Client**: Dashboard personnel uniquement
- **Technicien**: Dashboard interventions uniquement
- **Financier**: Accès aux statistiques uniquement

---

## 📊 Exemples de Données

### Dashboard Super-Admin
```json
{
  "globalStatistiques": {
    "totalSocietes": 1,
    "totalClients": 1072,
    "totalAgents": 17,
    "caMoisEnCours": 26000.00,
    "tauxRecouvrementMoisEnCours": 1.22
  },
  "top5SocietesCA": [
    {
      "idSociete": 1,
      "nomSociete": "Kenergie",
      "chiffreAffaires": 26000.00,
      "nombreClients": 1072
    }
  ]
}
```

### Dashboard Client
```json
{
  "statistiques": {
    "montantTotalFactures": 2637000.00,
    "montantTotalPaye": 26000.00,
    "montantTotalDu": 2611000.00,
    "tauxRecouvrement": 0.99
  },
  "facturesRecentes": [...],
  "paiementsRecents": [...]
}
```

---

## 🛠️ Installation et Configuration

### Flutter
```bash
# Ajouter les dépendances
flutter pub add http dio flutter_secure_storage json_annotation
flutter pub add fl_chart intl provider

# Générer les modèles JSON
flutter pub run build_runner build
```

### Vue.js
```bash
# Installer les dépendances
npm install axios vue-router pinia chart.js vue-chartjs
npm install @vue/composition-api @vueuse/core

# Démarrer le serveur de développement
npm run serve
```

---

## 🔄 Mises à Jour en Temps Réel

### SignalR Integration
- **Hub**: `DashboardHub`
- **Notifications**: Mises à jour automatiques des dashboards
- **Événements**: 
  - `DashboardRefreshed`
  - `StatistiquesUpdated`
  - `AlertesUpdated`

### Exemple Flutter
```dart
// Écouter les mises à jour en temps réel
hubConnection.on('DashboardRefreshed', (data) {
  // Rafraîchir le dashboard
  setState(() {
    _dashboardFuture = _loadDashboard();
  });
});
```

### Exemple Vue.js
```javascript
// Écouter les mises à jour en temps réel
hubConnection.on('DashboardRefreshed', (data) => {
  // Rafraîchir le dashboard
  this.loadDashboard();
});
```

---

## 📱 Support Multi-Plateforme

### Mobile (Flutter)
- ✅ Android 5.0+ (API 21+)
- ✅ iOS 11.0+
- ✅ Responsive Design
- ✅ Navigation adaptative

### Web (Vue.js)
- ✅ Chrome 70+
- ✅ Firefox 65+
- ✅ Safari 12+
- ✅ Edge 79+
- ✅ Responsive Design

---

## 🐛 Gestion des Erreurs

### Codes d'Erreur Communs
- **401**: Non authentifié / Token expiré
- **403**: Permissions insuffisantes
- **404**: Ressource non trouvée
- **500**: Erreur serveur interne

### Stratégies de Gestion
1. **Token expiré**: Redirection vers la page de login
2. **Erreur réseau**: Message d'erreur avec bouton retry
3. **Données invalides**: Affichage des erreurs de validation
4. **Service indisponible**: Mode dégradé avec cache

---

## 📈 Performance et Optimisation

### Côté Client
- **Lazy Loading**: Chargement des composants à la demande
- **Pagination**: Pour les listes importantes
- **Cache**: Stockage local des données fréquemment utilisées
- **Debouncing**: Pour les recherches en temps réel

### Côté Serveur
- **Compression**: Gzip pour les réponses
- **Caching**: Redis pour les données statiques
- **Pagination**: Limitation des résultats par requête
- **Indexation**: Optimisation des requêtes SQL

---

## 🔍 Monitoring et Analytics

### Métriques Disponibles
- **Temps de réponse**: Par endpoint
- **Taux d'erreur**: Par type d'erreur
- **Utilisation**: Par utilisateur et rôle
- **Performance**: Temps de chargement des dashboards

### Logs Structurés
```json
{
  "timestamp": "2026-02-15T22:45:15.291346+02:00",
  "level": "INFO",
  "message": "Dashboard Super-Admin chargé",
  "userId": 2,
  "userRole": "Super-Admin",
  "societeId": 1,
  "duration": 1250
}
```

---

## 🚀 Déploiement

### Production
- **API**: Docker + Kubernetes
- **Frontend Web**: Nginx + CDN
- **Mobile**: App Store / Google Play
- **Base de données**: MySQL Cluster

### Environnements
- **Développement**: Local + Hot Reload
- **Staging**: Pré-production avec données de test
- **Production**: Cluster haute disponibilité

---

## 📞 Support et Maintenance

### Contact Support
- **Email**: support@kenergie.cd
- **Documentation**: https://docs.kenergie.cd
- **Issues**: GitHub Issues

### Mises à Jour
- **Version actuelle**: v1.0.0
- **Fréquence**: Mensuelle
- **Rétrocompatibilité**: 2 versions supportées

---

## 📝 Notes de Version

### v1.0.0 (2026-02-15)
- ✅ Dashboards Super-Admin, Gérant, Technicien, Client
- ✅ API Statistiques complètes
- ✅ Authentification JWT
- ✅ SignalR pour temps réel
- ✅ Documentation Flutter et Vue.js

### Roadmap v1.1.0
- 🔄 Refresh Token automatique
- 🔄 Export PDF/Excel
- 🔄 Notifications push mobile
- 🔄 Thème sombre
- 🔄 Multi-langues

---

## 🎯 Conclusion

Cette documentation complète fournit tout le nécessaire pour intégrer les dashboards Kenergie dans vos applications Flutter et Vue.js. Les exemples sont prêts à l'emploi et couvrent tous les cas d'usage courants.

Pour toute question ou problème, n'hésitez pas à consulter les fichiers détaillés ou contacter le support technique.

**Bon développement! 🚀**
