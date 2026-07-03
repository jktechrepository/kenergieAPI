# Analyse du Contenu des Notifications de Factures

## 📋 Vue d'ensemble

Ce document analyse le contenu exact de chaque type de notification envoyée lors de la diffusion d'une facture aux clients.

**Source** : `Services/FactureNotificationService.cs` - Méthode `PrepareFactureMessages()`

---

## 🔔 1. NOTIFICATION PUSH (Firebase Cloud Messaging)

### Structure
```csharp
Title: "Nouvelle facture disponible"
Body: "Facture {numeroFacture} - {montant} FC"
Type: "FACTURE"
Data: {
    "type": "FACTURE",
    "idFacture": "123",
    "numeroFacture": "FACT-2025-001",
    "montant": "15,000.00",
    "lien": "https://k-energie.kansaconsulting.com/factures/123"
}
IsEnabled: true (si préférences push activées)
```

### Exemple Concret
```
Titre: Nouvelle facture - K-Energie
Corps: Facture FACT-2025-001 (12/2025) - 15,000.00 FC

Données JSON:
{
    "type": "FACTURE",
    "idFacture": "123",
    "numeroFacture": "FACT-2025-001",
    "montant": "15,000.00",
    "moisAnnee": "12/2025",
    "dateEmission": "15/12/2025",
    "nomSociete": "K-Energie",
    "lien": "https://k-energie.kansaconsulting.com/factures/123"
}
```

### Caractéristiques
- ✅ **Court et concis** : Titre + Corps en une ligne
- ✅ **Nom de société** : Identifie immédiatement la source dans le titre
- ✅ **Période incluse** : Mois/année dans le corps pour plus de contexte
- ✅ **Données structurées** : JSON avec métadonnées complètes (enrichi avec période, date et nom de société)
- ✅ **Action directe** : Lien vers la facture dans les données
- ✅ **Type identifié** : "FACTURE" pour filtrage côté client
- ⚠️ **Limitation** : Titre et corps limités par les contraintes FCM (200 chars pour titre, 1000 pour corps)

### Variables Utilisées
- `nomSociete` : Nom de la société (ex: "K-Energie") - dans le titre et les métadonnées
- `numeroFacture` : Numéro de la facture (ex: "FACT-2025-001")
- `montant` : Montant formaté avec 2 décimales (ex: "15,000.00")
- `idFacture` : ID numérique de la facture
- `lien` : URL complète vers la facture

---

## 📱 2. NOTIFICATION SMS (Twilio)

### Structure
```csharp
Body: "{nomSociete}: Votre facture de ({moisAnnee}) est de {montant} FC. N° {numeroFacture}."
IsEnabled: true (si préférences SMS activées ET téléphone présent)
```

### Exemple Concret
```
K-Energie: Votre facture de (12/2025) est de 15,000.00 FC. N FACT-2025-001.
```

### Caractéristiques
- ✅ **Format convivial** : Utilise "Votre facture" pour un ton personnel
- ✅ **Informations essentielles** : Société, période, montant, numéro de facture
- ✅ **Période incluse** : Mois/année dans le format "(12/2025)"
- ✅ **Numéro de facture** : Référence claire avec "N° FACT-2025-001"
- ✅ **Format court** : Sans URL pour économiser des caractères
- ✅ **Très confortable** : 76 caractères, bien sous la limite de 160
- ⚠️ **Pas de lien direct** : Le client accède à la facture via l'application (push, email, in-app)
- ⚠️ **Pas de formatage** : Texte brut uniquement
- ⚠️ **Coût** : ~0.01-0.05 USD par SMS selon le pays

### Longueur du Message
**Exemple** : `K-Energie: Votre facture de (12/2025) est de 15,000.00 FC. N FACT-2025-001.`
- **Longueur** : 75 caractères ✅ (très confortable, 85 caractères de marge)
- **Format** : Ton professionnel et convivial, informations essentielles sans URL
- **Optimisation** : Utilise "N" au lieu de "N°" pour éviter l'encodage Unicode (1 segment au lieu de 2)

### Variables Utilisées
- `nomSociete` : Nom de la société (ex: "K-Energie")
- `numeroFacture` : Numéro de la facture
- `montant` : Montant formaté
- `lienFacture` : URL complète

---

## 💬 3. NOTIFICATION IN-APP (SignalR)

### Structure
```csharp
Title: "Nouvelle facture disponible"
Content: "Facture {numeroFacture} d'un montant de {montant} FC pour la période {moisAnnee}"
Type: "FACTURE"
Icon: "receipt"
ActionLink: "/factures/{idFacture}"
Metadata: {
    "idFacture": "123",
    "numeroFacture": "FACT-2025-001",
    "montant": "15,000.00",
    "dateEmission": "15/12/2025"
}
IsEnabled: true (si préférences in-app activées)
```

### Exemple Concret
```
Titre: Nouvelle facture disponible
Contenu: Facture FACT-2025-001 d'un montant de 15,000.00 FC pour la période 12/2025
Icône: receipt
Lien d'action: /factures/123

Métadonnées JSON:
{
    "idFacture": "123",
    "numeroFacture": "FACT-2025-001",
    "montant": "15,000.00",
    "dateEmission": "15/12/2025"
}
```

### Caractéristiques
- ✅ **Informations complètes** : Inclut la période (mois/année)
- ✅ **Icône visuelle** : "receipt" pour identification rapide
- ✅ **Action directe** : Lien relatif vers la facture dans l'app
- ✅ **Métadonnées riches** : JSON avec toutes les infos nécessaires
- ✅ **Persistance** : Stockée en base de données pour historique

### Variables Utilisées
- `numeroFacture` : Numéro de la facture
- `montant` : Montant formaté
- `moisAnnee` : Période formatée (ex: "12/2025")
- `idFacture` : ID numérique
- `dateEmission` : Date formatée (dd/MM/yyyy)

---

## 📧 4. NOTIFICATION EMAIL

### Structure
```csharp
Subject: "Nouvelle facture disponible - {numeroFacture}"
HtmlBody: Template HTML complet (voir ci-dessous)
PlainTextBody: Version texte brut (voir ci-dessous)
IsEnabled: true (si préférences email activées ET email présent)
```

### Exemple Concret

#### Sujet
```
Nouvelle facture disponible - FACT-2025-001
```

#### Version HTML (Corps)
```html
<!DOCTYPE html>
<html lang='fr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Nouvelle facture disponible</title>
    <style>
        /* Styles CSS complets pour un email responsive */
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: #f5f5f5;
            margin: 0;
            padding: 0;
            line-height: 1.6;
        }
        .email-wrapper {
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
        }
        .header {
            background-color: #232f3e;
            padding: 30px 40px;
            text-align: center;
        }
        .header-logo {
            color: #ffffff;
            font-size: 28px;
            font-weight: 600;
            letter-spacing: 1px;
            margin: 0;
        }
        .content {
            padding: 40px;
            color: #232f3e;
        }
        .title {
            font-size: 24px;
            font-weight: 600;
            color: #232f3e;
            margin: 0 0 20px 0;
        }
        .facture-info {
            background-color: #f8f9fa;
            border-left: 4px solid #ff9900;
            padding: 20px;
            margin: 20px 0;
        }
        .facture-detail {
            margin: 10px 0;
            font-size: 16px;
        }
        .facture-detail strong {
            color: #232f3e;
        }
        .montant {
            font-size: 28px;
            font-weight: 600;
            color: #ff9900;
            margin: 20px 0;
        }
        .button {
            display: inline-block;
            padding: 12px 30px;
            background-color: #ff9900;
            color: #ffffff;
            text-decoration: none;
            border-radius: 5px;
            font-weight: 600;
            margin: 20px 0;
        }
        .footer {
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            font-size: 14px;
            color: #666;
        }
    </style>
</head>
<body>
    <div class='email-wrapper'>
        <div class='header'>
            <h1 class='header-logo'>K-Energie</h1>
        </div>
        <div class='content'>
            <h2 class='title'>Nouvelle facture disponible</h2>
            <p>Bonjour Jean Dupont,</p>
            <p>Une nouvelle facture est disponible dans votre espace client.</p>
            
            <div class='facture-info'>
                <div class='facture-detail'><strong>Numéro de facture :</strong> FACT-2025-001</div>
                <div class='facture-detail'><strong>Période :</strong> 12/2025</div>
                <div class='facture-detail'><strong>Date d'émission :</strong> 15/12/2025</div>
                <div class='montant'>Montant : 15,000.00 FC</div>
            </div>
            
            <p>Vous pouvez consulter et télécharger votre facture en cliquant sur le bouton ci-dessous :</p>
            
            <a href='https://k-energie.kansaconsulting.com/factures/123' class='button'>Voir ma facture</a>
            
            <p>Si vous avez des questions concernant cette facture, n'hésitez pas à nous contacter.</p>
            
            <p>Cordialement,<br>L'équipe K-Energie</p>
        </div>
        <div class='footer'>
            <p>Cet email a été envoyé automatiquement. Merci de ne pas y répondre.</p>
            <p>&copy; 2025 K-Energie. Tous droits réservés.</p>
        </div>
    </div>
</body>
</html>
```

#### Version Texte Brut (Plain Text)
```
Nouvelle facture disponible

Bonjour Jean Dupont,

Une nouvelle facture est disponible dans votre espace client.

Numéro de facture : FACT-2025-001
Période : 12/2025
Date d'émission : 15/12/2025
Montant : 15,000.00 FC

Vous pouvez consulter et télécharger votre facture en visitant :
https://k-energie.kansaconsulting.com/factures/123

Si vous avez des questions concernant cette facture, n'hésitez pas à nous contacter.

Cordialement,
L'équipe K-Energie

---
Cet email a été envoyé automatiquement. Merci de ne pas y répondre.
© 2025 K-Energie. Tous droits réservés.
```

### Caractéristiques
- ✅ **Design professionnel** : Email HTML responsive avec styles CSS
- ✅ **Informations complètes** : Tous les détails de la facture
- ✅ **Call-to-action clair** : Bouton "Voir ma facture"
- ✅ **Version texte** : Fallback pour clients email ne supportant pas HTML
- ✅ **Branding** : En-tête avec logo/nom de la société
- ✅ **Responsive** : Adapté mobile et desktop
- ✅ **Accessible** : Structure HTML sémantique

### Variables Utilisées
- `nomClient` : Nom du client (ex: "Jean Dupont")
- `numeroFacture` : Numéro de la facture
- `moisAnnee` : Période (ex: "12/2025")
- `dateEmission` : Date formatée
- `montant` : Montant formaté
- `lienFacture` : URL complète
- `nomSociete` : Nom de la société

---

## 📊 COMPARAISON DES CANAUX

| Canal | Longueur | Format | Informations | Coût | Persistance |
|-------|----------|--------|--------------|------|-------------|
| **Push** | Court | Titre + Corps | Essentielles + JSON | Gratuit | Non (temporaire) |
| **SMS** | Très court (~120 chars) | Texte brut | Minimales | ~0.01-0.05 USD | Non |
| **In-App** | Moyen | Titre + Contenu | Complètes + Métadonnées | Gratuit | Oui (base de données) |
| **Email** | Long | HTML + Texte | Très complètes | Faible | Oui (boîte email) |

---

## 🔍 ANALYSE DÉTAILLÉE

### Points Forts ✅

1. **Cohérence** : Tous les canaux utilisent les mêmes variables de base
2. **Personnalisation** : Nom du client, nom de la société, montant formaté
3. **Action claire** : Lien vers la facture dans tous les canaux
4. **Respect des préférences** : Chaque canal peut être désactivé individuellement
5. **Formatage approprié** : Chaque canal utilise le format adapté à sa contrainte

### Points d'Amélioration ⚠️

1. ~~**SMS - URL longue**~~ ✅ **RÉSOLU** :
   - **Solution implémentée** : URL raccourcie `/f/{id}` au lieu de `/factures/{id}`
   - **Exemple** : `K-Energie: Facture FACT-2025-001 (12/2025) - 15,000.00 FC. https://k-energie.kansaconsulting.com/f/123`

2. ~~**SMS - Manque d'informations**~~ ✅ **RÉSOLU** :
   - **Solution implémentée** : Période (mois/année) ajoutée dans le SMS
   - **Exemple** : `K-Energie: Facture FACT-2025-001 (12/2025) - 15,000.00 FC. [lien]`

3. ~~**Push - Corps limité**~~ ✅ **AMÉLIORÉ** :
   - **Solution implémentée** : Période ajoutée dans le corps du push
   - **Exemple** : `Facture FACT-2025-001 (12/2025) - 15,000.00 FC`
   - **Métadonnées enrichies** : `moisAnnee` et `dateEmission` ajoutés

4. ~~**Email - Lien statique**~~ ✅ **RÉSOLU** :
   - **Solution implémentée** : Lien configurable via `appsettings.json` → `FrontendSettings:BaseUrl` et `FrontendSettings:FacturePath`
   - **Avantage** : Facilement modifiable selon l'environnement (dev/prod)

5. **Localisation** :
   - **Problème** : Tous les textes sont en français
   - **Solution** : Ajouter un système de traduction si nécessaire (futur)

---

## 📝 EXEMPLE COMPLET DE NOTIFICATION

### Données d'Entrée
```json
{
    "facture": {
        "idFacture": 123,
        "numeroFacture": "FACT-2025-001",
        "montant": 15000.00,
        "dateEmission": "2025-12-15",
        "moisEmission": 12,
        "anneesEmission": 2025
    },
    "client": {
        "nomClient": "Jean Dupont",
        "idClient": 456
    },
    "societe": {
        "nom": "K-Energie"
    }
}
```

### Notifications Générées

#### 1. Push Notification
```
Titre: Nouvelle facture disponible
Corps: Facture FACT-2025-001 - 15,000.00 FC
```

#### 2. SMS
```
K-Energie: Votre facture de (12/2025) est de 15,000.00 FC. N° FACT-2025-001.
```

#### 3. In-App
```
Titre: Nouvelle facture disponible
Contenu: Facture FACT-2025-001 d'un montant de 15,000.00 FC pour la période 12/2025
```

#### 4. Email
```
Sujet: Nouvelle facture disponible - FACT-2025-001
Corps: [Email HTML complet avec tous les détails]
```

---

## 🎯 RECOMMANDATIONS

### Court Terme
1. ✅ **Optimiser le SMS** : ✅ **FAIT** - URL raccourcie `/f/{id}` et période ajoutée
2. ✅ **Configurer le lien** : ✅ **FAIT** - URL configurable via `FrontendSettings:BaseUrl`
3. ✅ **Ajouter la période au SMS** : ✅ **FAIT** - Période incluse avec fallback intelligent

### Moyen Terme
1. ⚠️ **Système de traduction** : Support multi-langues
2. ⚠️ **Templates personnalisables** : Permettre aux sociétés de personnaliser les messages
3. ⚠️ **A/B Testing** : Tester différents formats pour optimiser l'engagement

### Long Terme
1. ⚠️ **Rich Notifications** : Images dans les notifications push
2. ⚠️ **Deep Linking** : Liens directs vers des sections spécifiques de l'app
3. ⚠️ **Analytics** : Suivre les taux d'ouverture par canal

---

## 📍 LOCALISATION DU CODE

- **Service principal** : `Services/FactureNotificationService.cs`
- **Méthode de préparation** : `PrepareFactureMessages()` (ligne 163)
- **Template Email HTML** : `CreateFactureEmailHtml()` (ligne 240)
- **Template Email Texte** : `CreateFactureEmailPlainText()` (ligne 370)
- **Template SMS** : `CreateFactureSmsText()` (ligne 410)

---

## ✅ CONCLUSION

Le système de notification est **bien structuré** avec :
- ✅ Contenu adapté à chaque canal
- ✅ Informations essentielles présentes
- ✅ Respect des préférences utilisateur
- ✅ Formatage approprié pour chaque type

Les principales améliorations possibles concernent :
- ⚠️ L'optimisation de la longueur du SMS
- ⚠️ La configuration des URLs
- ⚠️ L'ajout de la période dans le SMS

