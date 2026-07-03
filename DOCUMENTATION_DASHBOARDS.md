# 📊 Documentation Complète des Dashboards et Statistiques - Kenergie API

## 🎯 Vue d'ensemble

Cette documentation couvre tous les endpoints de dashboards et statistiques de l'API Kenergie avec des exemples d'intégration pour Flutter (mobile) et Vue.js (web).

---

## 🔐 Authentification

Tous les endpoints nécessitent un token JWT d'authentification.

### Endpoint d'authentification
```
POST /api/Utilisateur/authentifier
```

### Corps de la requête
```json
{
  "emailOuTelephone": "admin@kenergie.cd",
  "motDePasse": "Admin",
  "fcmToken": "string",
  "deviceType": "string",
  "deviceModel": "string",
  "osVersion": "string"
}
```

### Réponse
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "Bearer",
  "expiresIn": 7200,
  "utilisateur": {
    "idUtilisateur": 2,
    "nomComplet": "Administrateur Kenergie",
    "email": "admin@kenergie.cd",
    "idSociete": 1,
    "roles": ["Admin", "Financier", "Caissier"]
  }
}
```

---

## 🏢 Dashboard Super-Admin

### Endpoint
```
GET /api/SuperAdmin/dashboard
```

### Rôles autorisés
- Super-Admin

### Réponse complète
```json
{
  "globalStatistiques": {
    "totalSocietes": 1,
    "totalClients": 1072,
    "totalAgents": 17,
    "totalUtilisateurs": 2,
    "caMoisEnCours": 26000.00,
    "caMoisDernier": 0,
    "tauxRecouvrementMoisEnCours": 1.22,
    "tauxRecouvrementMoisDernier": 0,
    "croissanceCA": 0,
    "croissanceRecouvrement": 0
  },
  "societes": [
    {
      "idSociete": 1,
      "nomSociete": "Kenergie",
      "totalClients": 1072,
      "caMoisEnCours": 26000.00,
      "caMoisDernier": 0,
      "tauxRecouvrement": 1.22,
      "croissance": 0,
      "statut": "Actif"
    }
  ],
  "top5SocietesCA": [
    {
      "idSociete": 1,
      "nomSociete": "Kenergie",
      "chiffreAffaires": 26000.00,
      "nombreClients": 1072,
      "croissance": 0
    }
  ],
  "top5SocietesRecouvrement": [
    {
      "idSociete": 1,
      "nomSociete": "Kenergie",
      "tauxRecouvrement": 1.22,
      "montantCollecte": 26000.00,
      "objectif": 100000.00,
      "progression": 26.00
    }
  ],
  "alertesCritiques": [],
  "tendances": {
    "chiffreAffaires": [
      {"mois": "octobre 2025", "valeur": 0},
      {"mois": "novembre 2025", "valeur": 0},
      {"mois": "décembre 2025", "valeur": 0},
      {"mois": "janvier 2026", "valeur": 0},
      {"mois": "février 2026", "valeur": 26000.00}
    ],
    "tauxRecouvrement": [
      {"mois": "octobre 2025", "valeur": 0},
      {"mois": "novembre 2025", "valeur": 0},
      {"mois": "décembre 2025", "valeur": 0},
      {"mois": "janvier 2026", "valeur": 0},
      {"mois": "février 2026", "valeur": 1.22}
    ]
  },
  "utilisateursStatistiques": {
    "totalUtilisateurs": 2,
    "utilisateursActifs": 2,
    "nouveauxUtilisateursMois": 0,
    "utilisateursParRole": [
      {"role": "Admin", "nombre": 1},
      {"role": "Financier", "nombre": 1}
    ]
  },
  "dateGeneration": "2026-02-15T22:45:15.291346+02:00"
}
```

---

## 🏢 Dashboard Gérant

### Endpoint
```
GET /api/Gerant/dashboard/{idSociete}
```

### Rôles autorisés
- Gérant
- Super-Admin

### Réponse complète
```json
{
  "societeStatistiques": {
    "idSociete": 1,
    "nomSociete": "Kenergie",
    "totalClients": 1072,
    "clientsActifs": 1072,
    "caMoisEnCours": 26000.00,
    "caMoisDernier": 0,
    "tauxRecouvrement": 1.22,
    "montantArrieres": 2611000.00,
    "croissanceCA": 0,
    "croissanceRecouvrement": 0
  },
  "clientsStatistiques": {
    "totalClients": 1072,
    "clientsActifs": 1072,
    "nouveauxClientsMois": 0,
    "clientsParCategorie": [
      {
        "idCategorie": 5,
        "nomCategorie": "DOMESTIQUE",
        "nombreClients": 934,
        "pourcentage": 86.96
      },
      {
        "idCategorie": 3,
        "nomCategorie": "COMMERCIAL",
        "nombreClients": 124,
        "pourcentage": 11.55
      }
    ]
  },
  "top5ClientsCA": [
    {
      "idClient": 1,
      "nomClient": "Client Example",
      "montantConsommation": 50000.00,
      "montantPaye": 13000.00,
      "montantDu": 37000.00,
      "tauxRecouvrement": 26.00
    }
  ],
  "top5ClientsArrieres": [
    {
      "idClient": 1,
      "nomClient": "Client Example",
      "montantArrieres": 37000.00,
      "nombreMoisRetard": 3,
      "dernierPaiement": "2026-01-15T10:30:00"
    }
  ],
  "alertesSociete": [],
  "tendances": {
    "chiffreAffaires": [
      {"mois": "janvier 2026", "valeur": 0},
      {"mois": "février 2026", "valeur": 26000.00}
    ],
    "tauxRecouvrement": [
      {"mois": "janvier 2026", "valeur": 0},
      {"mois": "février 2026", "valeur": 1.22}
    ]
  },
  "paiementsStatistiques": {
    "totalPaiements": 2,
    "montantTotal": 26000.00,
    "paiementsParMethode": [
      {
        "methode": "Espace",
        "nombre": 2,
        "montant": 26000.00,
        "pourcentage": 100.00
      }
    ],
    "moyennePaiement": 13000.00
  }
}
```

---

## 👨‍💼 Dashboard Technicien

### Endpoint
```
GET /api/Technicien/dashboard
```

### Rôles autorisés
- Technicien
- Super-Admin

### Réponse complète
```json
{
  "statistiquesPersonnelles": {
    "totalInterventions": 0,
    "interventionsMois": 0,
    "interventionsTerminees": 0,
    "interventionsEnCours": 0,
    "tempsMoyenIntervention": 0,
    "tauxResolution": 0
  },
  "interventionsRecentes": [],
  "pannesSignalees": [],
  "alertesTechnicien": [],
  "performanceTechnicien": {
    "interventionsJour": 0,
    "interventionsSemaine": 0,
    "interventionsMois": 0,
    "tempsMoyenResolution": 0,
    "satisfactionClient": 0
  },
  "dateGeneration": "2026-02-15T22:45:15.291346+02:00"
}
```

---

## 👤 Dashboard Client

### Endpoint
```
GET /api/ClientDashboard
```

### Rôles autorisés
- Client
- Super-Admin

### Réponse complète
```json
{
  "statistiques": {
    "montantTotalFactures": 2637000.00,
    "montantTotalPaye": 26000.00,
    "montantTotalDu": 2611000.00,
    "nombreFactures": 47,
    "nombreFacturesPayees": 2,
    "nombreFacturesEnRetard": 45,
    "tauxRecouvrement": 0.99,
    "consommationTotale": 2637000.00,
    "consommationMoyenneMensuelle": 219750.00
  },
  "facturesRecentes": [
    {
      "idFacture": 1,
      "reference": "FAC-000001",
      "moisAnnee": "01/2024",
      "montantTotal": 20000.00,
      "montantPaye": 15000.00,
      "montantDu": 5000.00,
      "dateEmission": "2024-01-15T00:00:00",
      "dateEcheance": "2024-02-15T00:00:00",
      "statut": "Payée",
      "joursRetard": 0
    }
  ],
  "paiementsRecents": [
    {
      "idPaiement": 1,
      "reference": "PAY-000001",
      "montantPaye": 13000.00,
      "datePaiement": "2026-02-15T10:30:00",
      "methodePaiement": "Espace",
      "statut": "Validé"
    }
  ],
  "consommations": [
    {
      "mois": "janvier 2026",
      "consommation": 476000.00,
      "cout": 476000.00,
      "unite": "kWh"
    }
  ],
  "alertesClient": [
    {
      "idAlerte": 1,
      "typeAlerte": "Paiement en retard",
      "message": "Votre facture FAC-000001 est en retard",
      "dateAlerte": "2026-02-15T10:00:00",
      "niveauUrgence": "Élevée",
      "estLue": false
    }
  ],
  "resumeClient": {
    "nomClient": "John Doe",
    "referenceClient": "CLI-000001",
    "adresseClient": "123 Rue Example",
    "telephoneClient": "+243123456789",
    "emailClient": "client@example.com",
    "categorieClient": "DOMESTIQUE",
    "statutCompte": "Actif",
    "dateCreation": "2024-01-01T00:00:00"
  },
  "dateGeneration": "2026-02-15T22:45:15.291346+02:00"
}
```

---

## 📈 Sous-endpoints Dashboard Client

### Statistiques Client
```
GET /api/ClientDashboard/statistiques
```

### Factures Récentes
```
GET /api/ClientDashboard/factures-recentes
```

### Paiements Récents
```
GET /api/ClientDashboard/paiements-recents
```

### Consommations
```
GET /api/ClientDashboard/consommations
```

### Alertes Client
```
GET /api/ClientDashboard/alertes-client
```

### Résumé Client
```
GET /api/ClientDashboard/resume-client
```
