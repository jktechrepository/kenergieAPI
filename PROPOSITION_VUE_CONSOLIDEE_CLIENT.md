# 💡 Proposition : Vue Consolidée des Factures par Client

## 📋 Résumé

**Objectif :** Conserver la logique actuelle (facture par usage) mais permettre d'afficher un **total consolidé** pour le client, regroupé par période (mois/année).

**Avantage :** Pas de changement d'architecture, juste ajout d'une vue agrégée.

**Date :** 2025-01-05

---

## 🎯 Approche Proposée

### Principe
- ✅ **Conserver** : Les `Facture` restent liées à un `Usage`
- ✅ **Conserver** : Les `ClientFacture` restent une par facture/usage
- ✅ **Ajouter** : Des DTOs avec totaux consolidés par période
- ✅ **Ajouter** : Des endpoints qui retournent les factures groupées par période avec totaux

---

## 📊 Structure Proposée

### 1. Nouveau DTO : `ClientFactureConsolideeDto`

```csharp
namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant une facture consolidée pour un client (regroupement par période)
    /// </summary>
    public class ClientFactureConsolideeDto
    {
        // Informations de la période
        public string Mois { get; set; }  // "01", "02", ..., "12"
        public int Annees { get; set; }
        public DateTime? DateEmission { get; set; }
        
        // Totaux consolidés
        public decimal MontantTotal { get; set; }      // Somme de tous les Montant
        public decimal MontantPayeTotal { get; set; }  // Somme de tous les MontantPaye
        public decimal MontantDuTotal { get; set; }    // Somme de tous les MontantDu
        
        // Détail par usage (liste des factures individuelles)
        public List<ClientFactureDto> DetailFactures { get; set; } = new List<ClientFactureDto>();
        
        // Informations client
        public int IdClient { get; set; }
        public string? NomClient { get; set; }
        public string? CodeCons { get; set; }
        
        // Statistiques
        public int NombreFactures { get; set; }  // Nombre de factures dans cette période
        public int NombreUsages { get; set; }   // Nombre d'usages différents
    }
}
```

### 2. Nouveau DTO : `ClientFacturesConsolideesResponseDto`

```csharp
namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO de réponse avec toutes les factures consolidées d'un client
    /// </summary>
    public class ClientFacturesConsolideesResponseDto
    {
        // Informations client
        public int IdClient { get; set; }
        public string? NomClient { get; set; }
        public string? CodeCons { get; set; }
        
        // Liste des factures consolidées par période
        public List<ClientFactureConsolideeDto> FacturesConsolidees { get; set; } = new List<ClientFactureConsolideeDto>();
        
        // Totaux globaux (toutes périodes confondues)
        public decimal MontantTotalGlobal { get; set; }
        public decimal MontantPayeTotalGlobal { get; set; }
        public decimal MontantDuTotalGlobal { get; set; }
        public int NombreTotalFactures { get; set; }
        public int NombreTotalPeriodes { get; set; }
    }
}
```

---

## 🔧 Modifications Proposées

### 1. Nouveau Méthode dans `IClientFactureRepository`

```csharp
/// <summary>
/// Récupère les factures d'un client groupées par période (mois/année) avec totaux consolidés
/// </summary>
Task<ClientFacturesConsolideesResponseDto> GetClientFacturesConsolideesAsync(int idClient);

/// <summary>
/// Récupère les factures d'un client pour une période spécifique avec totaux consolidés
/// </summary>
Task<ClientFactureConsolideeDto?> GetClientFactureConsolideeByPeriodeAsync(int idClient, string mois, int annee);
```

### 2. Implémentation dans `ClientFactureService`

```csharp
public async Task<ClientFacturesConsolideesResponseDto> GetClientFacturesConsolideesAsync(int idClient)
{
    // Récupérer toutes les ClientFacture du client
    var clientFactures = await _context.ClientFactures
        .Include(cf => cf.Client)
        .Include(cf => cf.Facture)
            .ThenInclude(f => f.Usage)
        .Where(cf => cf.IdClient == idClient && cf.Statut == true)
        .OrderByDescending(cf => cf.Annees)
        .ThenByDescending(cf => cf.Mois)
        .ThenByDescending(cf => cf.DateEmission ?? DateTime.MinValue)
        .ToListAsync();

    if (!clientFactures.Any())
        return new ClientFacturesConsolideesResponseDto { IdClient = idClient };

    // Grouper par période (Mois/Annees)
    var groupedByPeriode = clientFactures
        .GroupBy(cf => new { cf.Mois, cf.Annees })
        .ToList();

    var facturesConsolidees = new List<ClientFactureConsolideeDto>();
    var client = clientFactures.First().Client;

    foreach (var groupe in groupedByPeriode)
    {
        var facturesDuGroupe = groupe.ToList();
        
        var consolidee = new ClientFactureConsolideeDto
        {
            Mois = groupe.Key.Mois ?? "",
            Annees = groupe.Key.Annees ?? 0,
            DateEmission = facturesDuGroupe
                .Where(cf => cf.DateEmission.HasValue)
                .OrderByDescending(cf => cf.DateEmission)
                .FirstOrDefault()?.DateEmission,
            
            // Totaux consolidés
            MontantTotal = facturesDuGroupe
                .Where(cf => cf.Montant.HasValue)
                .Sum(cf => cf.Montant.Value),
            MontantPayeTotal = facturesDuGroupe
                .Where(cf => cf.MontantPaye.HasValue)
                .Sum(cf => cf.MontantPaye.Value),
            MontantDuTotal = facturesDuGroupe
                .Where(cf => cf.MontantDu.HasValue)
                .Sum(cf => cf.MontantDu.Value),
            
            // Détail
            DetailFactures = facturesDuGroupe
                .Select(cf => ConvertToDto(cf))
                .ToList(),
            
            // Informations client
            IdClient = idClient,
            NomClient = client?.NomClient,
            CodeCons = client?.CodeCons,
            
            // Statistiques
            NombreFactures = facturesDuGroupe.Count,
            NombreUsages = facturesDuGroupe
                .Where(cf => cf.Facture?.Usage != null)
                .Select(cf => cf.Facture.Usage.IdUsage)
                .Distinct()
                .Count()
        };
        
        facturesConsolidees.Add(consolidee);
    }

    // Construire la réponse avec totaux globaux
    return new ClientFacturesConsolideesResponseDto
    {
        IdClient = idClient,
        NomClient = client?.NomClient,
        CodeCons = client?.CodeCons,
        FacturesConsolidees = facturesConsolidees,
        
        // Totaux globaux
        MontantTotalGlobal = facturesConsolidees.Sum(f => f.MontantTotal),
        MontantPayeTotalGlobal = facturesConsolidees.Sum(f => f.MontantPayeTotal),
        MontantDuTotalGlobal = facturesConsolidees.Sum(f => f.MontantDuTotal),
        NombreTotalFactures = clientFactures.Count,
        NombreTotalPeriodes = facturesConsolidees.Count
    };
}
```

### 3. Nouveaux Endpoints dans `ClientFactureController`

```csharp
// GET: api/ClientFacture/client/{idClient}/consolidees
/// <summary>
/// Récupère toutes les factures d'un client groupées par période avec totaux consolidés
/// </summary>
[HttpGet("client/{idClient}/consolidees")]
[Authorize]
public async Task<ActionResult<ClientFacturesConsolideesResponseDto>> GetClientFacturesConsolidees(int idClient)
{
    var result = await _clientFactureRepository.GetClientFacturesConsolideesAsync(idClient);
    return Ok(result);
}

// GET: api/ClientFacture/client/{idClient}/consolidee/mois/{mois}/annee/{annee}
/// <summary>
/// Récupère la facture consolidée d'un client pour une période spécifique
/// </summary>
[HttpGet("client/{idClient}/consolidee/mois/{mois}/annee/{annee}")]
[Authorize]
public async Task<ActionResult<ClientFactureConsolideeDto>> GetClientFactureConsolideeByPeriode(
    int idClient, 
    string mois, 
    int annee)
{
    var result = await _clientFactureRepository.GetClientFactureConsolideeByPeriodeAsync(idClient, mois, annee);
    if (result == null)
    {
        return NotFound(new { message = "Aucune facture trouvée pour cette période" });
    }
    return Ok(result);
}
```

---

## 📊 Exemple de Réponse

### Endpoint : `GET /api/ClientFacture/client/123/consolidees`

```json
{
  "idClient": 123,
  "nomClient": "KAMITUGA ELIAS WATANGA",
  "codeCons": "A/a1/0465",
  "facturesConsolidees": [
    {
      "mois": "01",
      "annees": 2024,
      "dateEmission": "2024-01-15T00:00:00",
      "montantTotal": 4000.00,
      "montantPayeTotal": 2000.00,
      "montantDuTotal": 2000.00,
      "nombreFactures": 2,
      "nombreUsages": 2,
      "detailFactures": [
        {
          "idClientFacture": 1,
          "idFacture": 10,
          "montant": 2000.00,
          "montantPaye": 1000.00,
          "montantDu": 1000.00,
          "libelleUsage": "Résidentiel",
          "numeroFacture": "FAC-RES-0124-0001",
          "nombreBatiment": 2
        },
        {
          "idClientFacture": 2,
          "idFacture": 11,
          "montant": 2000.00,
          "montantPaye": 1000.00,
          "montantDu": 1000.00,
          "libelleUsage": "Commercial",
          "numeroFacture": "FAC-COM-0124-0001",
          "nombreBatiment": 1
        }
      ]
    },
    {
      "mois": "02",
      "annees": 2024,
      "dateEmission": "2024-02-15T00:00:00",
      "montantTotal": 4000.00,
      "montantPayeTotal": 0.00,
      "montantDuTotal": 4000.00,
      "nombreFactures": 2,
      "nombreUsages": 2,
      "detailFactures": [...]
    }
  ],
  "montantTotalGlobal": 8000.00,
  "montantPayeTotalGlobal": 2000.00,
  "montantDuTotalGlobal": 6000.00,
  "nombreTotalFactures": 4,
  "nombreTotalPeriodes": 2
}
```

---

## ✅ Avantages de cette Approche

### 1. Pas de Changement d'Architecture
- ✅ Les `Facture` restent liées à un `Usage`
- ✅ Les `ClientFacture` restent une par facture
- ✅ Pas de migration de données nécessaire
- ✅ Pas de risque de perte de données

### 2. Flexibilité
- ✅ Vue détaillée : Liste des factures individuelles
- ✅ Vue consolidée : Totaux par période
- ✅ Vue globale : Totaux toutes périodes confondues
- ✅ Le frontend choisit la vue à afficher

### 3. Performance
- ✅ Pas de changement dans les requêtes existantes
- ✅ Les nouvelles requêtes sont optimisées (groupement en base)
- ✅ Pas d'impact sur les performances existantes

### 4. Rétrocompatibilité
- ✅ Tous les endpoints existants continuent de fonctionner
- ✅ Les DTOs existants ne changent pas
- ✅ Pas de breaking changes

---

## 🔧 Modifications Nécessaires

### Fichiers à Modifier/Créer

1. **Nouveaux DTOs** :
   - `Models/DTOs/ClientFacture/ClientFactureConsolideeDto.cs`
   - `Models/DTOs/ClientFacture/ClientFacturesConsolideesResponseDto.cs`

2. **Interface Repository** :
   - `Services/Repositories/IClientFactureRepository.cs` (ajouter 2 méthodes)

3. **Service** :
   - `Services/ClientFactureService.cs` (implémenter les 2 méthodes)

4. **Controller** :
   - `Controllers/ClientFactureController.cs` (ajouter 2 endpoints)

**Temps estimé :** 2-3 heures

---

## 📋 Plan d'Implémentation

### Phase 1 : Création des DTOs (30 min)
- [ ] Créer `ClientFactureConsolideeDto`
- [ ] Créer `ClientFacturesConsolideesResponseDto`

### Phase 2 : Ajout des Méthodes Repository (1h)
- [ ] Ajouter méthodes dans `IClientFactureRepository`
- [ ] Implémenter dans `ClientFactureService`
- [ ] Tests unitaires

### Phase 3 : Ajout des Endpoints (1h)
- [ ] Ajouter endpoints dans `ClientFactureController`
- [ ] Tests d'intégration
- [ ] Documentation Swagger

### Phase 4 : Tests et Validation (30 min)
- [ ] Tests avec données réelles
- [ ] Validation des totaux
- [ ] Tests de performance

---

## 🎯 Cas d'Usage

### 1. Affichage dans le Frontend

**Vue Liste :**
```
Client: KAMITUGA ELIAS WATANGA (A/a1/0465)

Janvier 2024
├─ Total: 4000 FC | Payé: 2000 FC | Dû: 2000 FC
├─ Résidentiel (2 bât.) : 2000 FC
└─ Commercial (1 bât.) : 2000 FC

Février 2024
├─ Total: 4000 FC | Payé: 0 FC | Dû: 4000 FC
├─ Résidentiel (2 bât.) : 2000 FC
└─ Commercial (1 bât.) : 2000 FC

TOTAL GLOBAL: 8000 FC | Payé: 2000 FC | Dû: 6000 FC
```

### 2. Export PDF/Excel
- Possibilité d'exporter la vue consolidée
- Possibilité d'exporter le détail par usage

### 3. Rapports
- Rapports avec totaux consolidés
- Possibilité de filtrer par période

---

## ⚠️ Points d'Attention

### 1. Performance
- Le groupement se fait en mémoire (après récupération)
- Pour de très gros volumes, considérer un groupement SQL

### 2. Cohérence des Données
- Les totaux sont calculés à partir des `ClientFacture`
- S'assurer que les `MontantPaye` et `MontantDu` sont à jour

### 3. Périodes sans Factures
- Gérer les cas où un client n'a pas de factures pour une période
- Retourner `null` ou une structure vide

---

## 📊 Comparaison avec l'Option de Consolidation Complète

| Aspect | Consolidation Complète | Vue Consolidée (Proposée) |
|--------|------------------------|---------------------------|
| **Changement Architecture** | ⭐⭐⭐⭐⭐ (Majeur) | ⭐ (Minimal) |
| **Risque** | ⭐⭐⭐⭐ (Élevé) | ⭐ (Très faible) |
| **Migration Données** | ⭐⭐⭐⭐⭐ (Complexe) | ⭐ (Aucune) |
| **Temps Développement** | 5-6 jours | 2-3 heures |
| **Granularité par Usage** | ❌ Perdue | ✅ Conservée |
| **Vue Consolidée** | ✅ Native | ✅ Via DTO |
| **Rétrocompatibilité** | ❌ Breaking changes | ✅ 100% compatible |

---

## ✅ Recommandation

**Cette approche est recommandée car :**
1. ✅ Pas de changement d'architecture
2. ✅ Pas de migration de données
3. ✅ Risque minimal
4. ✅ Temps de développement très court
5. ✅ Conserve la granularité par usage
6. ✅ Permet d'afficher les totaux consolidés
7. ✅ 100% rétrocompatible

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0
