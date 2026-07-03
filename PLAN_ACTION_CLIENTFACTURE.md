# 📋 Plan d'Action : Implémentation du Modèle `ClientFacture`

## 🎯 Objectif

Créer un modèle `ClientFacture` pour :
1. ✅ **Gérer les arriérés pré-existants** (avant l'arrivée du système informatisé)
2. ✅ **Améliorer les performances** (éviter les recalculs dynamiques)
3. ✅ **Snapshot du nombreBatiment** (valeur figée au moment de la facture)
4. ✅ **Liaison explicite Facture-Client** (actuellement implicite via Usage)

---

## 📊 Analyse de la Proposition

### ✅ Avantages de la proposition

1. **Résout le problème des arriérés pré-existants**
   - `IdFacture = NULL` pour les arriérés manuels/pré-existants
   - Permet de saisir des montants dûs sans facture système

2. **Améliore les performances**
   - Montant pré-calculé et stocké (évite N+1 queries)
   - Pas besoin de recalculer à chaque requête

3. **Snapshot du nombreBatiment**
   - Stocke le `nombreBatiment` au moment de la facture
   - Évite les problèmes de recalcul si le client change de nombreBatiment

4. **Liaison explicite**
   - Lie directement une facture à un client
   - Permet des factures personnalisées par client (futur)

### 🔍 Suggestions d'amélioration

Le modèle proposé est excellent, mais je suggère d'ajouter quelques champs :

```csharp
public class ClientFacture
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int IdClientFacture { get; set; }
    
    // Champs proposés par l'utilisateur
    public int? IdFacture { get; set; }  // NULL pour arriérés pré-existants
    public int? IdClient { get; set; }   // Obligatoire
    public decimal? Montant { get; set; } // Montant total (déjà multiplié par nombreBatiment)
    public string? Mois { get; set; }    // Mois (format: "01", "02", ..., "12" ou "Janvier", "Février", etc.)
    public int? Annees { get; set; }     // Année
    
    // Champs supplémentaires suggérés
    public int? nombreBatiment { get; set; }  // Snapshot du nombreBatiment au moment de la facture
    public decimal? MontantPaye { get; set; } // Montant déjà payé (pré-calculé)
    public decimal? MontantDu { get; set; }  // Montant restant dû (pré-calculé)
    public DateTime? DateEmission { get; set; } // Date d'émission (pour tri et filtrage)
    public bool EstArrierePreExistant { get; set; } // Flag pour identifier les arriérés pré-existants
    public string? Description { get; set; } // Description/libellé pour les arriérés pré-existants
    public bool Statut { get; set; } = true; // Actif/Inactif
    
    // Attributs techniques
    public DateTime DateCreation { get; set; } = DateTime.Now;
    public DateTime? DateModification { get; set; }
    
    // Navigation properties
    [JsonIgnore]
    public Client? Client { get; set; }
    
    [JsonIgnore]
    public Facture? Facture { get; set; }
}
```

**Justification des champs supplémentaires :**
- `nombreBatiment` : Snapshot pour éviter les recalculs
- `MontantPaye` / `MontantDu` : Pré-calculés pour performance
- `DateEmission` : Utile pour tri et filtrage (plus fiable que Mois/Annees)
- `EstArrierePreExistant` : Flag explicite pour distinguer les types
- `Description` : Permet de documenter les arriérés pré-existants
- `Statut` : Permet de désactiver une facture sans la supprimer

---

## 📝 Plan d'Action Détaillé

### Phase 1 : Création du Modèle et Migration (1-2 jours)

#### 1.1. Créer le modèle `ClientFacture`
- [ ] Créer `/Models/ClientFacture.cs`
- [ ] Définir les propriétés avec annotations
- [ ] Ajouter les navigation properties

#### 1.2. Configurer le DbContext
- [ ] Ajouter `DbSet<ClientFacture> ClientFactures` dans `KenergieDbContext`
- [ ] Configurer les relations (FK vers `Client` et `Facture`)
- [ ] Configurer les index (sur `IdClient`, `IdFacture`, `Mois`, `Annees`)
- [ ] Configurer les contraintes (ex: `IdClient` obligatoire)

#### 1.3. Créer la migration EF Core
- [ ] Exécuter `dotnet ef migrations add AddClientFacture`
- [ ] Vérifier la migration générée
- [ ] Créer le script SQL de production

#### 1.4. Script SQL de production
- [ ] Créer `/Scripts/production_add_clientfacture.sql`
- [ ] Inclure la création de la table
- [ ] Inclure les index et contraintes
- [ ] Inclure les commentaires SQL

---

### Phase 2 : Adaptation des Services (2-3 jours)

#### 2.1. Créer `ClientFactureService`
- [ ] Créer `/Services/ClientFactureService.cs`
- [ ] Méthode `CreateAsync` : Créer une `ClientFacture`
- [ ] Méthode `CreatePreExistantAsync` : Créer un arriéré pré-existant
- [ ] Méthode `UpdateAsync` : Mettre à jour une `ClientFacture`
- [ ] Méthode `DeleteAsync` : Supprimer (soft delete avec `Statut = false`)
- [ ] Méthode `GetByClientAsync` : Récupérer toutes les `ClientFacture` d'un client
- [ ] Méthode `GetByFactureAsync` : Récupérer toutes les `ClientFacture` d'une facture
- [ ] Méthode `GetPreExistantsByClientAsync` : Récupérer les arriérés pré-existants

#### 2.2. Adapter `FactureService`
- [ ] Modifier `CreateAsync` pour créer automatiquement les `ClientFacture`
  - Lors de la création d'une facture, créer une `ClientFacture` pour chaque client ayant l'usage
  - Calculer `Montant = facture.Montant × nombreBatiment`
  - Initialiser `MontantPaye = 0`, `MontantDu = Montant`
- [ ] Modifier `DiffuserFactureAUsageAsync` pour créer les `ClientFacture` si nécessaire

#### 2.3. Adapter `PaiementService`
- [ ] Modifier `CreateAsync` pour mettre à jour les `ClientFacture`
  - Après création d'un paiement, mettre à jour `MontantPaye` et `MontantDu` de la `ClientFacture` correspondante
  - Utiliser une transaction pour garantir la cohérence
- [ ] Modifier `UpdateAsync` et `DeleteAsync` pour mettre à jour les `ClientFacture`

#### 2.4. Refactoriser `ArrieresService`
- [ ] **Option A (Recommandée)** : Utiliser `ClientFacture` comme source principale
  - Remplacer les calculs dynamiques par des requêtes sur `ClientFacture`
  - `GetArrieresByClientAsync` : Requête directe sur `ClientFacture` où `MontantDu > 0`
  - Performance : **1 requête au lieu de N+1**
- [ ] **Option B** : Garder l'ancien système et utiliser `ClientFacture` en complément
  - Calculer les arriérés depuis `ClientFacture` + factures sans `ClientFacture`
  - Migration progressive

#### 2.5. Créer le Repository (optionnel mais recommandé)
- [ ] Créer `/Services/Repositories/IClientFactureRepository.cs`
- [ ] Créer `/Services/Repositories/ClientFactureRepository.cs`
- [ ] Implémenter les méthodes CRUD

---

### Phase 3 : Endpoints API (1-2 jours)

#### 3.1. Créer `ClientFactureController`
- [ ] Créer `/Controllers/ClientFactureController.cs`
- [ ] `GET /api/ClientFacture/client/{idClient}` : Liste des factures d'un client
- [ ] `GET /api/ClientFacture/facture/{idFacture}` : Liste des clients d'une facture
- [ ] `GET /api/ClientFacture/client/{idClient}/pre-existants` : Arriérés pré-existants
- [ ] `POST /api/ClientFacture` : Créer une `ClientFacture` (pour factures système)
- [ ] `POST /api/ClientFacture/pre-existant` : Créer un arriéré pré-existant
- [ ] `PUT /api/ClientFacture/{id}` : Mettre à jour
- [ ] `DELETE /api/ClientFacture/{id}` : Supprimer (soft delete)

#### 3.2. Adapter les endpoints existants
- [ ] `GET /api/Client/{id}/arrieres` : Utiliser `ClientFacture` au lieu de calcul dynamique
- [ ] `GET /api/Client/{id}/factures-impayees` : Utiliser `ClientFacture`
- [ ] `GET /api/Client/{id}/factures-payees` : Utiliser `ClientFacture`

#### 3.3. DTOs
- [ ] Créer `/Models/DTOs/ClientFacture/CreateClientFactureDto.cs`
- [ ] Créer `/Models/DTOs/ClientFacture/CreateArrierePreExistantDto.cs`
- [ ] Créer `/Models/DTOs/ClientFacture/ClientFactureDto.cs`
- [ ] Créer `/Models/DTOs/ClientFacture/UpdateClientFactureDto.cs`

---

### Phase 4 : Migration des Données Existantes (1 jour)

#### 4.1. Script de migration des données
- [ ] Créer `/Scripts/migrate_existing_factures_to_clientfacture.sql`
- [ ] Pour chaque facture existante :
  - Trouver tous les clients ayant l'usage de la facture
  - Créer une `ClientFacture` pour chaque client
  - Calculer `Montant = facture.Montant × nombreBatiment`
  - Calculer `MontantPaye` depuis la table `Paiements`
  - Calculer `MontantDu = Montant - MontantPaye`
  - Remplir `Mois`, `Annees`, `DateEmission` depuis la facture

#### 4.2. Service de migration (optionnel)
- [ ] Créer `/Services/MigrationService.cs`
- [ ] Méthode `MigrateExistingFacturesToClientFactureAsync`
- [ ] Endpoint `POST /api/Migration/migrate-factures` (protégé, admin uniquement)

---

### Phase 5 : Gestion des Arriérés Pré-Existants (1 jour)

#### 5.1. Interface de saisie
- [ ] Endpoint `POST /api/ClientFacture/pre-existant` avec validation
- [ ] DTO `CreateArrierePreExistantDto` :
  ```csharp
  {
      "IdClient": 123,
      "Montant": 50000.00,
      "Mois": "01",
      "Annees": 2023,
      "Description": "Arriérés avant informatisation",
      "DateEmission": "2023-01-15"
  }
  ```

#### 5.2. Validation
- [ ] Vérifier que le client existe
- [ ] Vérifier que `Montant > 0`
- [ ] Vérifier que `Mois` est valide (1-12 ou format string)
- [ ] Vérifier que `Annees` est valide (2000-2100)

#### 5.3. Intégration dans les calculs
- [ ] Les arriérés pré-existants doivent apparaître dans `GetArrieresByClientAsync`
- [ ] Filtrer par `EstArrierePreExistant = true` ou `IdFacture IS NULL`

---

### Phase 6 : Tests et Validation (1-2 jours)

#### 6.1. Tests unitaires (optionnel)
- [ ] Tests pour `ClientFactureService`
- [ ] Tests pour la création automatique lors de création de facture
- [ ] Tests pour la mise à jour lors de paiement

#### 6.2. Tests d'intégration
- [ ] Tester la création d'une facture → vérifier les `ClientFacture` créées
- [ ] Tester un paiement → vérifier la mise à jour de `ClientFacture`
- [ ] Tester la création d'un arriéré pré-existant
- [ ] Tester le calcul des arriérés (doit être plus rapide)

#### 6.3. Tests de performance
- [ ] Comparer les temps de réponse avant/après
- [ ] Vérifier la réduction des requêtes SQL (N+1 → 1)

---

## 🔄 Flux de Données

### Flux actuel (avant ClientFacture)
```
1. Création Facture → Facture créée
2. Diffusion → Trouve clients via Usage
3. Calcul Arriérés → Pour chaque facture :
   - Trouve nombreBatiment (requête)
   - Calcule MontantTotal (facture.Montant × nombreBatiment)
   - Calcule MontantPaye (SUM des paiements) ← N+1 queries
   - Calcule MontantDu = MontantTotal - MontantPaye
```

### Flux nouveau (avec ClientFacture)
```
1. Création Facture → Facture créée
2. Diffusion → Crée ClientFacture pour chaque client :
   - Montant = facture.Montant × nombreBatiment (snapshot)
   - MontantPaye = 0
   - MontantDu = Montant
3. Paiement → Met à jour ClientFacture :
   - MontantPaye += paiement.MontantPaye
   - MontantDu = Montant - MontantPaye
4. Calcul Arriérés → Requête directe :
   SELECT * FROM ClientFactures 
   WHERE IdClient = @idClient AND MontantDu > 0
   ← 1 seule requête !
```

---

## 📊 Structure de la Table `ClientFactures`

```sql
CREATE TABLE ClientFactures (
    IdClientFacture INT AUTO_INCREMENT PRIMARY KEY,
    IdFacture INT NULL,  -- NULL pour arriérés pré-existants
    IdClient INT NOT NULL,
    Montant DECIMAL(18,2) NULL,
    nombreBatiment INT NULL,  -- Snapshot
    MontantPaye DECIMAL(18,2) DEFAULT 0,
    MontantDu DECIMAL(18,2) NULL,  -- Calculé: Montant - MontantPaye
    Mois VARCHAR(20) NULL,  -- "01", "02", "Janvier", etc.
    Annees INT NULL,
    DateEmission DATETIME NULL,
    EstArrierePreExistant BOOLEAN DEFAULT FALSE,
    Description VARCHAR(500) NULL,
    Statut BOOLEAN DEFAULT TRUE,
    DateCreation DATETIME(6) DEFAULT CURRENT_TIMESTAMP(6),
    DateModification DATETIME(6) NULL,
    
    FOREIGN KEY (IdClient) REFERENCES Clients(IdClient) ON DELETE RESTRICT,
    FOREIGN KEY (IdFacture) REFERENCES Factures(IdFacture) ON DELETE SET NULL,
    
    INDEX idx_client (IdClient),
    INDEX idx_facture (IdFacture),
    INDEX idx_client_mois_annees (IdClient, Mois, Annees),
    INDEX idx_montant_du (MontantDu)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## ⚠️ Points d'Attention

### 1. Cohérence des données
- **Problème** : Si un paiement est créé/modifié/supprimé, il faut mettre à jour `ClientFacture`
- **Solution** : Utiliser des transactions et mettre à jour `ClientFacture` dans `PaiementService`

### 2. Migration des données existantes
- **Problème** : Les factures existantes n'ont pas de `ClientFacture`
- **Solution** : Script de migration qui crée les `ClientFacture` rétroactivement

### 3. Double source de vérité
- **Problème** : `MontantPaye` dans `ClientFacture` vs SUM des `Paiements`
- **Solution** : 
  - **Option A** : `ClientFacture` est la source de vérité, `Paiements` est l'historique
  - **Option B** : Calculer `MontantPaye` depuis `Paiements` et mettre à jour `ClientFacture` (recommandé)

### 4. Performance de la migration
- **Problème** : Migration peut être longue avec beaucoup de factures/clients
- **Solution** : Migration par batch, avec barre de progression

### 5. Arriérés pré-existants sans facture
- **Problème** : Comment gérer les paiements sur des arriérés pré-existants ?
- **Solution** : 
  - Créer un `Paiement` avec `IdFacture = NULL` et `IdClientFacture` (nouveau champ ?)
  - Ou créer un `Paiement` avec `IdClient` et un champ `IdClientFacture` optionnel

---

## 🎯 Ordre d'Implémentation Recommandé

### Sprint 1 (Semaine 1)
1. ✅ Créer le modèle `ClientFacture`
2. ✅ Configurer le DbContext
3. ✅ Créer la migration
4. ✅ Créer le script SQL de production

### Sprint 2 (Semaine 2)
5. ✅ Créer `ClientFactureService`
6. ✅ Adapter `FactureService` pour créer automatiquement les `ClientFacture`
7. ✅ Adapter `PaiementService` pour mettre à jour les `ClientFacture`
8. ✅ Créer les endpoints de base

### Sprint 3 (Semaine 3)
9. ✅ Refactoriser `ArrieresService` pour utiliser `ClientFacture`
10. ✅ Créer le script de migration des données existantes
11. ✅ Implémenter la gestion des arriérés pré-existants
12. ✅ Tests et validation

---

## 📈 Métriques de Succès

- ✅ **Performance** : Réduction de 90%+ du temps de réponse pour `GetArrieresByClientAsync`
- ✅ **Requêtes SQL** : Passage de N+1 queries à 1 seule requête
- ✅ **Fonctionnalité** : Possibilité de saisir des arriérés pré-existants
- ✅ **Cohérence** : Les montants dans `ClientFacture` correspondent aux paiements
- ✅ **Migration** : 100% des factures existantes ont une `ClientFacture` correspondante

---

## 🔄 Rétrocompatibilité

### Option A : Migration complète (recommandée)
- Toutes les factures existantes sont migrées vers `ClientFacture`
- Les anciens endpoints utilisent `ClientFacture` directement
- **Avantage** : Performance immédiate, code simplifié
- **Inconvénient** : Migration nécessaire avant déploiement

### Option B : Migration progressive
- Les nouvelles factures créent des `ClientFacture`
- Les anciennes factures continuent d'utiliser le calcul dynamique
- Migration progressive des anciennes factures
- **Avantage** : Déploiement progressif, pas de blocage
- **Inconvénient** : Code plus complexe (deux chemins)

---

## ❓ Questions à Valider

1. **Format du champ `Mois`** : String libre ("01", "Janvier", "Jan") ou enum strict ?
2. **Gestion des paiements sur arriérés pré-existants** : Nouveau champ `IdClientFacture` dans `Paiement` ?
3. **Migration** : Automatique au démarrage ou manuelle via endpoint ?
4. **Suppression** : Soft delete (`Statut = false`) ou hard delete ?
5. **Validation** : Faut-il empêcher la modification de `Montant` après création ?

---

## 📝 Prochaines Étapes

1. **Valider le plan** avec l'équipe
2. **Décider sur les questions** ci-dessus
3. **Commencer par Phase 1** : Création du modèle
4. **Tester progressivement** chaque phase
5. **Documenter** les changements pour l'équipe

---

**Prêt à commencer l'implémentation ?** 🚀
