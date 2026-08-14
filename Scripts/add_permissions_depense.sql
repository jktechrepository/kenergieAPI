-- ============================================================================
-- Script d'ajout des permissions Module Dépenses (phase 2 — approbation)
-- Description : Depense.* et CategorieDepense.*
-- IMPORTANT : Script IDEMPOTENT
-- ============================================================================

-- Permissions Depense
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Depense.Create', 'Depense', 'Create', 'Créer une dépense', 1, NOW()),
    ('Depense.Read', 'Depense', 'Read', 'Voir une dépense', 1, NOW()),
    ('Depense.ReadAll', 'Depense', 'ReadAll', 'Voir toutes les dépenses', 1, NOW()),
    ('Depense.Update', 'Depense', 'Update', 'Modifier une dépense', 1, NOW()),
    ('Depense.Validate', 'Depense', 'Validate', 'Valider ou refuser une dépense', 1, NOW()),
    ('Depense.Delete', 'Depense', 'Delete', 'Supprimer une dépense', 1, NOW());

-- Permissions CategorieDepense
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('CategorieDepense.Create', 'CategorieDepense', 'Create', 'Créer une catégorie de dépense', 1, NOW()),
    ('CategorieDepense.Read', 'CategorieDepense', 'Read', 'Voir une catégorie de dépense', 1, NOW()),
    ('CategorieDepense.ReadAll', 'CategorieDepense', 'ReadAll', 'Voir toutes les catégories de dépense', 1, NOW()),
    ('CategorieDepense.Update', 'CategorieDepense', 'Update', 'Modifier une catégorie de dépense', 1, NOW()),
    ('CategorieDepense.Delete', 'CategorieDepense', 'Delete', 'Supprimer une catégorie de dépense', 1, NOW());

-- Rôles
SET @super_admin_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Super-Admin' LIMIT 1);
SET @admin_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Admin' LIMIT 1);
SET @gerant_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Gerant' LIMIT 1);
SET @financier_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Financier' LIMIT 1);
SET @rc_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Responsable Commercial' LIMIT 1);

-- Retirer Create / Validate précédemment assignés à Super-Admin (phase 2)
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Permissions` p ON p.IdPermission = rp.IdPermission
WHERE rp.IdRole = @super_admin_role_id
  AND p.Categorie = 'Depense'
  AND p.Action IN ('Create', 'Validate');

-- Retirer Create précédemment assigné à Admin
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Permissions` p ON p.IdPermission = rp.IdPermission
WHERE rp.IdRole = @admin_role_id
  AND p.Categorie = 'Depense'
  AND p.Action = 'Create';

-- Super-Admin : lecture + suppression (pas de création ni validation)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @super_admin_role_id, IdPermission, NOW()
FROM Permissions
WHERE (Categorie = 'Depense' AND Action IN ('Read', 'ReadAll', 'Update', 'Delete'))
   OR Categorie = 'CategorieDepense';

-- Admin : validation + lecture + suppression (pas de création)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @admin_role_id, IdPermission, NOW()
FROM Permissions
WHERE (Categorie = 'Depense' AND Action != 'Create')
   OR Categorie = 'CategorieDepense';

-- Financier : saisie uniquement (pas de validation ni suppression)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @financier_role_id, IdPermission, NOW()
FROM Permissions
WHERE (Categorie = 'Depense' AND Action IN ('Create', 'Read', 'ReadAll', 'Update'))
   OR (Categorie = 'CategorieDepense' AND Action != 'Delete');

-- Gerant : lecture + validation
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @gerant_role_id, IdPermission, NOW()
FROM Permissions
WHERE (Categorie = 'Depense' AND Action IN ('Read', 'ReadAll', 'Validate'))
   OR (Categorie = 'CategorieDepense' AND Action IN ('Read', 'ReadAll'));

-- Responsable Commercial : lecture dépenses, lecture catégories
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @rc_role_id, IdPermission, NOW()
FROM Permissions
WHERE (Categorie = 'Depense' AND Action IN ('Read', 'ReadAll'))
   OR (Categorie = 'CategorieDepense' AND Action = 'Read');

SELECT 'Permissions module Dépenses (phase 2) assignées' AS Resultat;
