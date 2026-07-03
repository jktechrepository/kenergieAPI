# 🔍 **Analyse Complète des Modèles et Relations**

## 📋 **Vue d'ensemble des Entités**

J'ai analysé tous les modèles pour comprendre les relations directes et indirectes avant l'implémentation de la synchronisation.

---

## 🏗️ **Architecture des Relations**

### **🏢 Société (Entité Racine)**
```csharp
public class Societe
{
    public int IdSociete { get; set; }        // PK
    public string? Nom { get; set; }           // Nom société
    public bool? Statut { get; set; }          // Active/inactive
    
    // Relations directes (1-N):
    public ICollection<Utilisateur>? Utilisateurs { get; set; }
    public ICollection<Agent>? Agents { get; set; }
    public ICollection<Notification>? Notifications { get; set; }
    public ICollection<CategorieClient>? CategorieClients { get; set; }
}
```

### **👥 Utilisateur (Multi-rôles)**
```csharp
public class Utilisateur
{
    public int IdUtilisateur { get; set; }      // PK
    public int? IdSociete { get; set; }       // FK → Societe
    public int? IdRole { get; set; }          // FK → Role (rétrocompatibilité)
    public int? IdAgent { get; set; }          // FK → Agent (nullable)
    public int? IdClient { get; set; }         // FK → Client (nullable)
    
    // Relations N-N avec Role (multi-rôles):
    public ICollection<UserRole>? UserRoles { get; set; }
    
    // Propriétés calculées:
    public IEnumerable<Role> Roles => UserRoles.Where(ur => ur.Statut == true).Select(ur => ur.Role);
    public Role? PrimaryRole => UserRoles.Where(ur => ur.Statut == true && ur.IsPrimary).Select(ur => ur.Role).FirstOrDefault();
}
```

### **🎭 Role (RBAC)**
```csharp
public class Role
{
    public int IdRole { get; set; }           // PK
    public string Nom { get; set; }           // "Super-Admin", "Gerant", etc.
    public int? Niveau { get; set; }          // Hiérarchie (1=Super-Admin, 10=bas)
    
    // Relations:
    public ICollection<UserRole>? UserRoles { get; set; }        // N-N avec Utilisateur
    public ICollection<RolePermission>? RolePermissions { get; set; } // N-N avec Permission
}
```

### **🔗 UserRole (Table de Liaison N-N)**
```csharp
public class UserRole
{
    public int IdUserRole { get; set; }       // PK
    public int IdUtilisateur { get; set; }     // FK → Utilisateur
    public int IdRole { get; set; }           // FK → Role
    public bool IsPrimary { get; set; }        // Rôle principal?
    public bool? Statut { get; set; }         // Actif/inactif
    
    // Relations:
    public Utilisateur Utilisateur { get; set; } // N-1
    public Role Role { get; set; }           // N-1
}
```

### **👤 Agent (Terrain)**
```csharp
public class Agent
{
    public int IdAgent { get; set; }          // PK
    public int? IdSociete { get; set; }       // FK → Societe
    public string? Matricule { get; set; }      // Unique
    public string? Zone { get; set; }          // Zone géographique
    
    // Relations:
    public Societe? Societe { get; set; }     // N-1
    public ICollection<Utilisateur>? Utilisateurs { get; set; } // 1-N
}
```

### **👥 Client (Entité Principale pour Sync)**
```csharp
public class Client
{
    public int IdClient { get; set; }         // PK
    public string? CodeCons { get; set; }       // Unique
    public int? IdAxe { get; set; }          // FK → Axe
    public bool Statut { get; set; }          // Soft delete
    public bool IsActif { get; set; }         // Actif métier
    
    // Relations directes:
    public Axe? Axe { get; set; }            // N-1
    public ICollection<ClientUsage>? ClientsUsages { get; set; }     // N-N
    public ICollection<Utilisateur>? Utilisateurs { get; set; }    // 1-N
    public ICollection<ClientFacture>? ClientFactures { get; set; } // 1-N
}
```

### **🏢 Axe (Géographique)**
```csharp
public class Axe
{
    public int IdAxe { get; set; }           // PK
    public int IdCabine { get; set; }         // FK → Cabine
    public string? CodeAxe { get; set; }       // Pour génération CodeCons
    
    // Relations:
    public Cabine? Cabine { get; set; }     // N-1
    public ICollection<Client>? Clients { get; set; } // 1-N
}
```

### **🏢 Cabine (Infrastructure)**
```csharp
public class Cabine
{
    public int IdCabine { get; set; }         // PK
    public int IdSociete { get; set; }       // FK → Societe
    public string? CodeCabine { get; set; }    // Pour génération CodeCons
    
    // Relations:
    public Societe? Societe { get; set; }     // N-1
    public ICollection<Axe>? Axes { get; set; } // 1-N
}
```

### **📂 CategorieClient (Tarification)**
```csharp
public class CategorieClient
{
    public int IdCategorie { get; set; }       // PK
    public int IdSociete { get; set; }       // FK → Societe
    public string? NomCategorie { get; set; }  // "Résidentiel", "Commercial"
    
    // Relations:
    public Societe? Societe { get; set; }     // N-1
    public ICollection<Usage>? Usages { get; set; } // 1-N
}
```

### **⚡ Usage (Type de Service)**
```csharp
public class Usage
{
    public int IdUsage { get; set; }          // PK
    public int IdCategorieClient { get; set; } // FK → CategorieClient
    public string Libelle { get; set; }         // "Résidentiel", "Commercial"
    
    // Relations:
    public CategorieClient? CategorieClient { get; set; } // N-1
    public ICollection<ClientUsage>? ClientsUsages { get; set; } // N-N
    public ICollection<Facture>? Factures { get; set; } // 1-N
}
```

### **🔗 ClientUsage (N-N Client↔Usage)**
```csharp
public class ClientUsage
{
    public int IdClientUsage { get; set; }     // PK
    public int IdClient { get; set; }         // FK → Client
    public int IdUsage { get; set; }         // FK → Usage
    public int nombreBatiment { get; set; }    // Multiplicateur pour factures
    
    // Relations:
    public Client? Client { get; set; }       // N-1
    public Usage? Usage { get; set; }         // N-1
}
```

### **🧾 Facture (Émission)**
```csharp
public class Facture
{
    public int IdFacture { get; set; }        // PK
    public int IdUsage { get; set; }         // FK → Usage
    public string? NumeroFacture { get; set; } // Unique
    public decimal? Montant { get; set; }     // Montant total
    
    // Relations:
    public Usage? Usage { get; set; }         // N-1
    public ICollection<Paiement>? Paiements { get; set; } // 1-N
    public ICollection<ClientFacture>? ClientFactures { get; set; } // 1-N
}
```

### **🧾 ClientFacture (Arriérés - CLÉ pour Sync)**
```csharp
public class ClientFacture
{
    public int IdClientFacture { get; set; }  // PK
    public int? IdFacture { get; set; }      // FK → Facture (nullable)
    public int IdClient { get; set; }         // FK → Client
    public decimal? Montant { get; set; }      // Montant total
    public decimal? MontantPaye { get; set; }  // Déjà payé
    public decimal? MontantDu { get; set; }     // Restant dû
    public DateTime? DateModification { get; set; } // ✅ POUR DELTA SYNC
    public bool Statut { get; set; }          // Soft delete
    
    // Relations:
    public Client? Client { get; set; }       // N-1
    public Facture? Facture { get; set; }     // N-1
    public ICollection<Paiement>? Paiements { get; set; } // 1-N
}
```

### **💰 Paiement (Transactions)**
```csharp
public class Paiement
{
    public int IdPaiement { get; set; }       // PK
    public int? IdFacture { get; set; }      // FK → Facture (nullable)
    public int? IdClient { get; set; }       // FK → Client (nullable)
    public int? IdClientFacture { get; set; } // FK → ClientFacture (nullable)
    public decimal MontantPaye { get; set; }  // Montant payé
    public bool IsDeleted { get; set; }        // Soft delete
    // ❌ MANQUE: UpdatedAt pour sync
    // ❌ MANQUE: ClientRequestId pour idempotence
    
    // Relations:
    public Client? Client { get; set; }       // N-1
    public Facture? Facture { get; set; }     // N-1
    public ClientFacture? ClientFacture { get; set; } // N-1
}
```

---

## 🎯 **Analyse des Relations pour la Sync**

### **📊 Hiérarchie des Données:**
```
Societe (Racine)
├── Agent (Terrain)
│   └── Utilisateur (Multi-rôles)
├── Cabine (Infrastructure)
│   └── Axe (Géographique)
│       └── Client (Principal pour sync)
│           └── ClientUsage (N-N)
│               └── Usage (Service)
│                   └── CategorieClient (Tarification)
├── CategorieClient (Tarification)
│   └── Usage (Service)
├── Facture (Émission)
│   └── ClientFacture (Arriérés) ⭐ CLÉ SYNC
└── Paiement (Transactions) ⭐ CLÉ SYNC
```

### **🔄 Relations Importantes pour la Sync:**

#### **1. Client → Societe (Tenant Isolation)**
```csharp
// ✅ FILTRE AUTOMATIQUE PAR JWT
var societeId = _currentUserService.SocieteId;
var clients = await _context.Clients
    .Where(c => c.IdSociete == societeId) // Isolation garantie
    .ToListAsync();
```

#### **2. ClientFacture → Client (Arriérés)**
```csharp
// ✅ RELATION PRINCIPALE POUR SYNC
var clientFactures = await _context.ClientFactures
    .Include(cf => cf.Client) // Jointure pour infos client
    .Where(cf => cf.Client.IdSociete == societeId) // Double isolation
    .Where(cf => cf.MontantDu > 0) // Uniquement impayés
    .ToListAsync();
```

#### **3. Paiement → ClientFacture (Idempotence)**
```csharp
// ✅ CONTRAINTE D'IDEMPOTENCE
public class Paiement
{
    // ❌ MANQUE: ClientRequestId (UUID)
    // ❌ MANQUE: UpdatedAt (delta sync)
    
    public string? ReferenceTransaction { get; set; } // Existant mais pas unique
}
```

---

## 🚨 **Champs Manquants pour la Sync**

### **🔴 Critiques (à ajouter):**

#### **1. Client - UpdatedAt**
```csharp
public class Client
{
    // ❌ MANQUE: UpdatedAt pour delta sync
    public DateTime? UpdatedAt { get; set; } // À AJOUTER
}
```

#### **2. Paiement - UpdatedAt + ClientRequestId**
```csharp
public class Paiement
{
    // ❌ MANQUE: UpdatedAt pour delta sync
    public DateTime? UpdatedAt { get; set; } // À AJOUTER
    
    // ❌ MANQUE: ClientRequestId pour idempotence
    public string? ClientRequestId { get; set; } // À AJOUTER (UUID)
}
```

#### **3. Client - IsDeleted**
```csharp
public class Client
{
    // ❌ MANQUE: IsDeleted pour suppression logique
    public bool IsDeleted { get; set; } = false; // À AJOUTER
}
```

---

## 🎯 **Champs Existants Utilisables pour la Sync**

### **✅ ClientFacture - Parfaitement Équipé**
```csharp
public class ClientFacture
{
    public DateTime? DateModification { get; set; } // ✅ POUR DELTA SYNC
    public bool Statut { get; set; }              // ✅ SOFT DELETE
    public decimal? MontantDu { get; set; }        // ✅ FILTRE IMPAYÉS
    public bool EstArrierePreExistant { get; set; } // ✅ DISTINCTION TYPE
}
```

### **✅ Client - Bonne Base**
```csharp
public class Client
{
    public string? CodeCons { get; set; }          // ✅ UNIQUE
    public bool Statut { get; set; }              // ✅ SOFT DELETE
    public bool IsActif { get; set; }             // ✅ STATUT MÉTIER
    public int? IdAxe { get; set; }              // ✅ FILTRE GÉOGRAPHIQUE
}
```

---

## 🏗️ **Index Existant vs Nécessaires**

### **✅ Index Existant (DbContext.cs):**
```sql
-- Clients
IX_Client_IdAxe                    -- Sur IdAxe
IX_Client_CodeCons_Unique           -- ✅ Unique sur CodeCons

-- ClientFacture
IX_ClientFacture_IdClient          -- ✅ Sur IdClient
IX_ClientFacture_IdFacture         -- Sur IdFacture
IX_ClientFacture_Client_Mois_Annees -- ✅ Composite (IdClient, Mois, Annees)

-- Paiement
IX_Paiements_IdFacture           -- Sur IdFacture
IX_Paiements_IdClient            -- ✅ Sur IdClient
IX_Paiements_DatePaiement        -- Sur DatePaiement
```

### **❌ Index Manquants (CRITIQUES pour Sync):**
```sql
-- Pour cursor pagination clients
CREATE INDEX IX_Clients_Sync ON Clients (IdSociete, UpdatedAt, IdClient);

-- Pour cursor pagination clientFactures
CREATE INDEX IX_ClientFactures_Sync ON ClientFactures (IdSociete, DateModification, IdClientFacture);

-- Pour idempotence paiements
CREATE UNIQUE INDEX UX_Paiements_Idempotent ON Paiements (IdSociete, ClientRequestId);
```

---

## 🎯 **Mapping pour les DTOs de Sync**

### **ClientSyncDto (Projection Optimisée):**
```csharp
public class ClientSyncDto
{
    public int IdClient { get; set; }
    public string NomClient { get; set; }
    public string AdresseClient { get; set; }
    public string Telephone { get; set; }
    public string EmailClient { get; set; }
    public string CodeCons { get; set; }
    public int IdCategorieClient { get; set; } // Via ClientUsage
    public int IdAxe { get; set; }
    public int IdCabine { get; set; }         // Via Axe
    public bool IsActif { get; set; }
    public bool Statut { get; set; }
    public DateTime UpdatedAt { get; set; }    // ✅ À AJOUTER
}
```

### **ArrearSyncDto (Projection Optimisée):**
```csharp
public class ArrearSyncDto
{
    public int IdClientFacture { get; set; }
    public int IdFacture { get; set; }
    public int IdClient { get; set; }
    public string NumeroFacture { get; set; }
    public DateTime DateEmission { get; set; }
    public string Mois { get; set; }
    public int Annees { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public decimal MontantDu { get; set; }
    public string LibelleUsage { get; set; }    // Via Usage
    public bool EstArrierePreExistant { get; set; }
    public DateTime DateModification { get; set; }  // ✅ EXISTANT
}
```

---

## 📋 **Checklist Pré-Implémentation**

### **✅ Ce qui est déjà correct:**
- [x] **ClientFacture** parfaitement équipé pour sync
- [x] **Soft delete** implémenté sur ClientFacture
- [x] **Relations N-N** Client↔Usage via ClientUsage
- [x] **Isolation par société** via IdSociete
- [x] **Index base** existants

### **❌ Ce qui doit être ajouté:**
- [ ] **Client.UpdatedAt** pour delta sync
- [ ] **Client.IsDeleted** pour suppression logique
- [ ] **Paiement.UpdatedAt** pour delta sync
- [ ] **Paiement.ClientRequestId** pour idempotence
- [ ] **Index composites** pour cursor pagination
- [ ] **Index unique** pour idempotence paiements

---

## 🎯 **Recommandations pour l'Implémentation**

### **1. Priorité 1 - Database (Obligatoire):**
```sql
-- Ajouter les champs manquants
ALTER TABLE Clients ADD COLUMN UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;
ALTER TABLE Clients ADD COLUMN IsDeleted BIT DEFAULT 0;
ALTER TABLE Paiements ADD COLUMN UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;
ALTER TABLE Paiements ADD COLUMN ClientRequestId VARCHAR(36);

-- Ajouter les index de performance
CREATE INDEX IX_Clients_Sync ON Clients (IdSociete, UpdatedAt, IdClient);
CREATE INDEX IX_ClientFactures_Sync ON ClientFactures (IdSociete, DateModification, IdClientFacture);
CREATE UNIQUE INDEX UX_Paiements_Idempotent ON Paiements (IdSociete, ClientRequestId);
```

### **2. Priorité 2 - Models (Obligatoire):**
```csharp
// Ajouter les propriétés manquantes
public class Client
{
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

public class Paiement
{
    public DateTime? UpdatedAt { get; set; }
    public string? ClientRequestId { get; set; }
}
```

### **3. Priorité 3 - DTOs (Optimisation):**
- Créer les DTOs de sync optimisés
- Utiliser les projections pour éviter les surchargements
- Inclure uniquement les champs nécessaires pour le mobile

---

## 🎉 **Conclusion**

### **✅ Points forts de l'architecture existante:**
- **Structure relationnelle** bien conçue
- **Multi-rôles** bien implémenté
- **Soft delete** déjà présent sur ClientFacture
- **Isolation par société** via IdSociete

### **⚠️ Points à corriger pour la sync:**
- **Ajouter UpdatedAt** sur Client et Paiement
- **Ajouter ClientRequestId** sur Paiement
- **Créer les index** de performance
- **Optimiser les DTOs** pour le mobile

### **🎯 Feuille de route claire:**
1. **Database** (champs + index)
2. **Models** (propriétés manquantes)
3. **Services** (logique de sync)
4. **Controllers** (endpoints)
5. **Tests** (validation)

L'architecture existante est **solide** et nécessite seulement des **ajouts ciblés** pour supporter la synchronisation offline!

---

*Analyse des modèles réalisée le 21 mars 2026 - Prêt pour implémentation*
