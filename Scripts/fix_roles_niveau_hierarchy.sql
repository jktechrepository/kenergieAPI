-- ============================================================================
-- Correction des niveaux hiérarchiques des rôles
-- Date : 2026-08-11
-- Description : Normalise Role.Niveau pour que le filtre GET /api/Role
--               exclue correctement les rôles supérieurs (ex. Super-Admin pour Admin)
-- IMPORTANT : Idempotent — peut être exécuté plusieurs fois
-- ============================================================================

-- USE `KenergieDB`;  -- Adapter selon l'environnement

START TRANSACTION;

UPDATE `Roles` SET `Niveau` = 1 WHERE `Nom` = 'Super-Admin' AND (`Niveau` IS NULL OR `Niveau` <> 1);
UPDATE `Roles` SET `Niveau` = 2 WHERE `Nom` = 'Admin' AND (`Niveau` IS NULL OR `Niveau` <> 2);
UPDATE `Roles` SET `Niveau` = 3 WHERE `Nom` = 'Gerant' AND (`Niveau` IS NULL OR `Niveau` <> 3);
UPDATE `Roles` SET `Niveau` = 4 WHERE `Nom` = 'Financier' AND (`Niveau` IS NULL OR `Niveau` <> 4);
UPDATE `Roles` SET `Niveau` = 5 WHERE `Nom` = 'Caissier' AND (`Niveau` IS NULL OR `Niveau` <> 5);
UPDATE `Roles` SET `Niveau` = 6 WHERE `Nom` = 'Technicien' AND (`Niveau` IS NULL OR `Niveau` <> 6);
UPDATE `Roles` SET `Niveau` = 7 WHERE `Nom` = 'Client' AND (`Niveau` IS NULL OR `Niveau` <> 7);

-- Rôles commerciaux (si présents)
UPDATE `Roles` SET `Niveau` = 3 WHERE `Nom` = 'Responsable Commercial' AND (`Niveau` IS NULL OR `Niveau` <> 3);
UPDATE `Roles` SET `Niveau` = 4 WHERE `Nom` = 'Agent Direction Commercial' AND (`Niveau` IS NULL OR `Niveau` <> 4);

COMMIT;

SELECT `IdRole`, `Nom`, `Niveau`, `Statut`
FROM `Roles`
ORDER BY `Niveau`, `Nom`;

SELECT '✅ Script fix_roles_niveau_hierarchy terminé' AS Resultat;
