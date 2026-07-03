# 📋 Plan de Travail : Consolidation des Arriérés par Période

## 🎯 Objectif

Améliorer l'endpoint `/api/ClientFacture/client/{idClient}/arrieres` pour retourner une réponse consolidée par période (mois/année), similaire au format de `/api/ClientFacture/client/{idClient}/consolidee/mois/{mois}/annee/{annee}`.

**Date de début :** 2025-01-05  
**Durée estimée :** 3-4 jours  
**Priorité :** Moyenne

---

## 📊 Structure de Réponse Souhaitée

```json
{
  "idClient": 1,
  "nomClient": "Kalambayi Jonathan",
  "codeCons": "B/b1/0001",
  "arrieresParPeriode": [
    {
      "mois": "01",
      "annees": 2026,
      "nombreUsages": 3,
      "nombreFactures": 3,
      "dateEmission": "2026-01-15",
      "montantTotal": 45000,
      "montantPayeTotal": 0,
      "montantDuTotal": 45000,
      "detailFactures": [
        {
          "idClientFacture": 1,
          "idFacture": 1,
          "idClient": 1,
          "montant": 5000,
          "nombreBatiment": 1,
          "montantPaye": 0,
          "montantDu": 5000,
          "mois": "01",
          "annees": 2026,
          "dateEmission": "2026-01-15T00:00:00",
          "estArrierePreExistant": false,
          "description": null,
          "statut": true,
          "dateCreation": "2026-01-15T07:56:35.642782",
          "dateModification": null,
          "nomClient": "Kalambayi Jonathan",
          "numeroFacture": "FAC-DOM-0126-0001",
          "libelleUsage": "DOMESTIQUE"
        }
      ]
    }
  ]
}
```

---

## 📝 Phases du Plan de Travail

### Phase 1 : Création des DTOs (0.5 jour)

#### 1.1. Créer `ArrieresConsolidesResponseDto`
**Fichier :** `Models/DTOs/ClientFacture/ArrieresConsolidesResponseDto.cs`

**Structure :**
```csharp
public class ArrieresConsolidesResponseDto
{
    public int IdClient { get; set; }
    public string? NomClient { get; set; }
    public string? CodeCons { get; set; }
    public List<ArriereParPeriodeDto> ArrieresParPeriode { get; set; } = new List<ArriereParPeriodeDto>();
}
```

**Checklist :**
- [ ] Créer le fichier `ArrieresConsolidesResponseDto.cs`
- [ ] Définir les propriétés
- [ ] Ajouter les commentaires XML

---

#### 1.2. Créer `ArriereParPeriodeDto`
**Fichier :** `Models/DTOs/ClientFacture/ArriereParPeriodeDto.cs`

**Structure :**
```csharp
public class ArriereParPeriodeDto
{
    public string Mois { get; set; } = string.Empty;
    public int Annees { get; set; }
    public int NombreUsages { get; set; }
    public int NombreFactures { get; set; }
    public DateTime? DateEmission { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantPayeTotal { get; set; }
    public decimal MontantDuTotal { get; set; }
    public List<ClientFactureDto> DetailFactures { get; set; } = new List<ClientFactureDto>();
}
```

**Checklist :**
- [ ] Créer le fichier `ArriereParPeriodeDto.cs`
- [ ] Définir les propriétés
- [ ] Ajouter les commentaires XML

---

### Phase 2 : Implémentation dans le Repository/Service (1 jour)

#### 2.1. Ajouter la méthode dans `IClientFactureRepository`
**Fichier :** `Services/Repositories/IClientFactureRepository.cs`

**Signature :**
```csharp
/// <summary>
/// Récupère les arriérés d'un client groupés par période (mois/année) avec totaux consolidés
/// </summary>
Task<ArrieresConsolidesResponseDto> GetArrieresConsolidesByClientAsync(int idClient);
```

**Checklist :**
- [ ] Ajouter la signature dans l'interface
- [ ] Ajouter les commentaires XML

---

#### 2.2. Implémenter la méthode dans `ClientFactureService`
**Fichier :** `Services/ClientFactureService.cs`

**Logique :**
1. Récupérer toutes les `ClientFacture` avec `MontantDu > 0` pour le client
2. Grouper par période (Mois/Annees)
3. Pour chaque groupe :
   - Calculer `MontantTotal`, `MontantPayeTotal`, `MontantDuTotal`
   - Compter `NombreFactures` et `NombreUsages`
   - Récupérer `DateEmission` (la plus récente)
   - Convertir chaque `ClientFacture` en `ClientFactureDto`
4. Créer `ArrieresConsolidesResponseDto` avec les informations du client
5. Retourner le résultat

**Checklist :**
- [ ] Implémenter `GetArrieresConsolidesByClientAsync`
- [ ] Utiliser `GetByClientWithArrieresAsync` pour récupérer les arriérés
- [ ] Grouper par `Mois` et `Annees`
- [ ] Calculer les totaux pour chaque période
- [ ] Compter `NombreFactures` et `NombreUsages`
- [ ] Convertir en DTOs
- [ ] Gérer le cas où il n'y a pas d'arriérés

---

### Phase 3 : Création du Nouvel Endpoint (0.5 jour)

#### 3.1. Créer le nouvel endpoint (Option Recommandée : Nouvel Endpoint)
**Fichier :** `Controllers/ClientFactureController.cs`

**Endpoint :** `GET /api/ClientFacture/client/{idClient}/arrieres-consolides`

**Implémentation :**
```csharp
[HttpGet("client/{idClient}/arrieres-consolides")]
[Authorize]
public async Task<ActionResult<ArrieresConsolidesResponseDto>> GetArrieresConsolidesByClient(int idClient)
{
    // Vérifier que le client existe
    var client = await _context.Clients.FindAsync(idClient);
    if (client == null)
    {
        return NotFound(new { message = "Client non trouvé" });
    }

    var result = await _clientFactureRepository.GetArrieresConsolidesByClientAsync(idClient);
    return Ok(result);
}
```

**Checklist :**
- [ ] Ajouter l'endpoint dans `ClientFactureController`
- [ ] Vérifier l'existence du client
- [ ] Appeler la méthode du repository
- [ ] Retourner la réponse
- [ ] Ajouter les commentaires XML

---

### Phase 4 : Tests (1 jour)

#### 4.1. Tests Unitaires
**Fichier :** `Tests/ClientFactureServiceTests.cs` (à créer si nécessaire)

**Scénarios à tester :**
- [ ] Client avec arriérés sur plusieurs périodes
- [ ] Client avec arriérés sur une seule période
- [ ] Client sans arriérés
- [ ] Client inexistant
- [ ] Vérification des totaux consolidés
- [ ] Vérification du nombre de factures et usages
- [ ] Vérification du groupement par période

**Checklist :**
- [ ] Créer les tests unitaires
- [ ] Tester tous les scénarios
- [ ] Vérifier les calculs de totaux

---

#### 4.2. Tests d'Intégration
**Fichier :** `Tests/ClientFactureControllerTests.cs` (à créer si nécessaire)

**Scénarios à tester :**
- [ ] Appel de l'endpoint avec un client valide
- [ ] Appel de l'endpoint avec un client inexistant
- [ ] Vérification de la structure de réponse
- [ ] Vérification des codes HTTP

**Checklist :**
- [ ] Créer les tests d'intégration
- [ ] Tester tous les scénarios
- [ ] Vérifier la structure JSON

---

### Phase 5 : Documentation (0.5 jour)

#### 5.1. Documentation API
**Fichier :** `DOCUMENTATION_ENDPOINT_ARRIERES_CONSOLIDES.md`

**Contenu :**
- Description de l'endpoint
- Structure de la réponse
- Exemples de requêtes/réponses
- Codes d'erreur
- Comparaison avec l'ancien endpoint

**Checklist :**
- [ ] Créer la documentation
- [ ] Ajouter des exemples
- [ ] Documenter les codes d'erreur

---

#### 5.2. Mise à jour de la Documentation Existante
**Fichiers :**
- `DOCUMENTATION_ARRIERES_PRE_EXISTANTS.md` (si existe)
- README.md (si existe)

**Checklist :**
- [ ] Mettre à jour la documentation existante
- [ ] Ajouter des références au nouvel endpoint

---

### Phase 6 : Migration Frontend (2-3 jours) - **À FAIRE PAR LE FRONTEND**

#### 6.1. Identification des Composants
- [ ] Identifier tous les composants utilisant `/api/ClientFacture/client/{idClient}/arrieres`
- [ ] Lister les modifications nécessaires

#### 6.2. Adaptation des Composants
- [ ] Adapter les composants pour utiliser le nouveau format
- [ ] Tester l'affichage
- [ ] Valider avec les utilisateurs

#### 6.3. Tests Frontend
- [ ] Tests unitaires des composants
- [ ] Tests d'intégration
- [ ] Tests E2E

---

## 🔄 Option Alternative : Versioning

Si on choisit l'**Option 1 : Versioning**, les phases 3 et 6 sont modifiées :

### Phase 3 Modifiée : Créer l'Endpoint Versionné
- [ ] Créer `/api/ClientFacture/client/{idClient}/arrieres/v2`
- [ ] Maintenir `/api/ClientFacture/client/{idClient}/arrieres` (ancien format)
- [ ] Marquer l'ancien endpoint comme déprécié (après migration)

### Phase 6 Modifiée : Migration Progressive
- [ ] Migrer progressivement les composants vers `/v2`
- [ ] Surveiller les logs pour détecter les utilisations de l'ancien endpoint
- [ ] Déprécier l'ancien endpoint après migration complète

---

## 📊 Estimation du Temps

| Phase | Durée | Responsable |
|-------|-------|-------------|
| Phase 1 : DTOs | 0.5 jour | Backend |
| Phase 2 : Repository/Service | 1 jour | Backend |
| Phase 3 : Endpoint | 0.5 jour | Backend |
| Phase 4 : Tests | 1 jour | Backend |
| Phase 5 : Documentation | 0.5 jour | Backend |
| Phase 6 : Migration Frontend | 2-3 jours | Frontend |
| **TOTAL** | **5.5-6.5 jours** | |

---

## ✅ Checklist Globale

### Backend
- [ ] Phase 1 : DTOs créés
- [ ] Phase 2 : Méthode implémentée
- [ ] Phase 3 : Endpoint créé
- [ ] Phase 4 : Tests passent
- [ ] Phase 5 : Documentation créée
- [ ] Code review
- [ ] Déploiement en staging
- [ ] Tests en staging
- [ ] Déploiement en production

### Frontend
- [ ] Phase 6 : Composants identifiés
- [ ] Phase 6 : Composants adaptés
- [ ] Phase 6 : Tests frontend passent
- [ ] Validation utilisateurs
- [ ] Déploiement frontend

---

## 🚨 Points d'Attention

### 1. Performance
- ⚠️ Vérifier que le groupement ne dégrade pas les performances
- ⚠️ Optimiser les requêtes si nécessaire

### 2. Compatibilité
- ⚠️ Maintenir l'ancien endpoint pendant la période de transition
- ⚠️ Communiquer le changement au frontend

### 3. Tests
- ⚠️ Tester avec des données réelles
- ⚠️ Tester avec des clients ayant beaucoup d'arriérés

### 4. Documentation
- ⚠️ Documenter clairement le changement
- ⚠️ Fournir des exemples de migration

---

## 📝 Notes

- Le format de réponse est similaire à `ClientFactureConsolideeDto` mais adapté pour les arriérés
- Seules les factures avec `MontantDu > 0` sont incluses
- Le groupement se fait par période (Mois/Annees)
- Les totaux sont calculés pour chaque période

---

**Date de création :** 2025-01-05  
**Version :** 1.0.0  
**Statut :** 📋 Plan prêt pour implémentation
