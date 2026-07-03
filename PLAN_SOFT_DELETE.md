# 📋 Plan d'Implémentation du Soft Delete

## 📊 État Actuel des Entités

| Entité | Champ Statut | DeleteAsync Actuel | Action Requise |
|--------|--------------|-------------------|----------------|
| **Axe** | ❌ Non | Hard delete | ✅ Ajouter Statut + Soft delete |
| **Cabine** | ❌ Non | Hard delete | ✅ Ajouter Statut + Soft delete |
| **ClientFacture** | ✅ Oui | Hard delete | ✅ Modifier pour soft delete |
| **ClientUsage** | ✅ Oui | Hard delete (vérifie factures) | ✅ Modifier pour soft delete |
| **CommunicationCampaign** | ❌ Non | Hard delete | ✅ Ajouter Statut + Soft delete |
| **Notification** | ✅ Oui | Hard delete | ✅ Modifier pour soft delete |
| **Paiement** | ⚠️ Statut (string) | Hard delete | ✅ Modifier pour soft delete (utiliser Statut) |
| **PanneSignalement** | ✅ Oui | Hard delete | ✅ Modifier pour soft delete |
| **PlainteClient** | ⚠️ StatutPlainte (string) | Hard delete | ✅ Ajouter Statut bool + Soft delete |
| **Usage** | ✅ Oui | Hard delete | ✅ Modifier pour soft delete |
| **Utilisateur** | ✅ Oui | Hard delete | ✅ Modifier pour soft delete |

---

## 🔧 Actions à Effectuer

### Phase 1 : Ajouter le champ Statut aux modèles qui n'en ont pas

1. **Axe** : Ajouter `public bool Statut { get; set; } = true;`
2. **Cabine** : Ajouter `public bool Statut { get; set; } = true;`
3. **CommunicationCampaign** : Ajouter `public bool Statut { get; set; } = true;`
4. **PlainteClient** : Ajouter `public bool Statut { get; set; } = true;` (en plus de StatutPlainte)

### Phase 2 : Modifier les services DeleteAsync

Pour chaque service, remplacer :
```csharp
_context.Entities.Remove(entity);
await _context.SaveChangesAsync();
```

Par :
```csharp
entity.Statut = false;
await _context.SaveChangesAsync();
```

### Phase 3 : Modifier les contrôleurs DELETE

- Retourner `Ok()` avec informations au lieu de `NoContent()`
- Ajouter message informatif sur le soft delete

### Phase 4 : S'assurer que les GET filtrent par Statut

Vérifier que tous les `GetAllAsync` et `GetByIdAsync` filtrent par `Statut == true` (sauf si déjà fait).

---

## ⚠️ Cas Spéciaux

### Paiement
- Utilise `Statut` (string) : "Validé", "En attente", etc.
- Pour soft delete, on peut soit :
  - Ajouter un champ `bool IsDeleted` 
  - Ou utiliser `Statut = "Supprimé"`
- **Recommandation** : Ajouter `bool IsDeleted` pour ne pas polluer le champ Statut métier

### PlainteClient
- A déjà `StatutPlainte` (string) : "En attente", "Résolu", etc.
- Ajouter `bool Statut` pour le soft delete

---

**Date de création :** 2025-01-05
