-- ============================================================================
-- Ajustements permissions RBAC par rôle — Kenergie API
-- Date : 2026-08-11
-- Description : Révoque et accorde les permissions selon la nouvelle matrice
-- IMPORTANT : Idempotent — peut être exécuté plusieurs fois sans erreur
-- NOTE : Les utilisateurs concernés doivent obtenir un nouveau JWT après exécution
-- ============================================================================

-- USE `KenergieDB`;  -- Décommenter et adapter selon l'environnement

SET FOREIGN_KEY_CHECKS = 0;
START TRANSACTION;

-- ============================================================================
-- 1. RÉVOCATIONS
-- ============================================================================

-- Agent Direction Commercial : Client.Create + toutes Facture/Paiement
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Agent Direction Commercial'
  AND (
      p.`Nom` = 'Client.Create'
      OR p.`Categorie` IN ('Facture', 'Paiement')
  );

-- Financier : modification paiement + suppression usages
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Financier'
  AND p.`Nom` IN ('Paiement.Update', 'Usage.Delete');

-- Responsable Commercial : suppression plaintes
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Responsable Commercial'
  AND p.`Nom` = 'PlainteClient.Delete';

-- Caissier : campagnes de communication (si assignées manuellement)
DELETE rp FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Caissier'
  AND p.`Categorie` = 'CommunicationCampaign';

-- ============================================================================
-- 2. NOUVELLES ASSIGNATIONS
-- ============================================================================

-- Admin : créer / lire les axes + paiements
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Admin'
  AND p.`Statut` = 1
  AND (
      (p.`Categorie` = 'Axe' AND p.`Action` IN ('Create', 'Read', 'ReadAll'))
      OR p.`Categorie` = 'Paiement'
  );

-- Client : paiement électronique (Paiement.Create)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Client'
  AND p.`Statut` = 1
  AND p.`Nom` = 'Paiement.Create';

-- Responsable Commercial : plaintes (sans Delete — ré-assigner Read/ReadAll/Create/Update si manquants)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Responsable Commercial'
  AND p.`Statut` = 1
  AND p.`Categorie` = 'PlainteClient'
  AND p.`Action` != 'Delete';

-- Caissier : paiements (collecte)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Caissier'
  AND p.`Statut` = 1
  AND p.`Categorie` = 'Paiement'
  AND p.`Action` IN ('Create', 'Read', 'ReadAll', 'Update');

-- Financier : s'assurer Create/Read sans Update (ré-assigner si manquants)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Financier'
  AND p.`Statut` = 1
  AND p.`Categorie` = 'Paiement'
  AND p.`Action` IN ('Create', 'Read', 'ReadAll');

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Financier'
  AND p.`Statut` = 1
  AND p.`Categorie` = 'Usage'
  AND p.`Action` != 'Delete';

-- Agent Direction Commercial : lecture/mise à jour clients uniquement
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Agent Direction Commercial'
  AND p.`Statut` = 1
  AND p.`Categorie` = 'Client'
  AND p.`Action` IN ('Read', 'ReadAll', 'Update');

COMMIT;
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- 3. VÉRIFICATIONS
-- ============================================================================

SELECT 'Admin — Axe + Paiement' AS Section;
SELECT p.`Nom`, p.`Action`
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Admin'
  AND (p.`Categorie` IN ('Axe', 'Paiement'))
ORDER BY p.`Categorie`, p.`Action`;

SELECT 'Agent Direction Commercial' AS Section;
SELECT p.`Nom`, p.`Categorie`, p.`Action`
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Agent Direction Commercial'
ORDER BY p.`Categorie`, p.`Action`;

SELECT 'Financier — Paiement + Usage' AS Section;
SELECT p.`Nom`, p.`Action`
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Financier'
  AND p.`Categorie` IN ('Paiement', 'Usage')
ORDER BY p.`Categorie`, p.`Action`;

SELECT 'Responsable Commercial — PlainteClient' AS Section;
SELECT p.`Nom`, p.`Action`
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Responsable Commercial'
  AND p.`Categorie` = 'PlainteClient'
ORDER BY p.`Action`;

SELECT 'Caissier — CommunicationCampaign (doit être vide)' AS Section;
SELECT p.`Nom`
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Caissier'
  AND p.`Categorie` = 'CommunicationCampaign';

SELECT 'Client — Paiement' AS Section;
SELECT p.`Nom`
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON rp.`IdRole` = r.`IdRole`
INNER JOIN `Permissions` p ON rp.`IdPermission` = p.`IdPermission`
WHERE r.`Nom` = 'Client'
  AND p.`Categorie` = 'Paiement';

SELECT '✅ Script adjust_role_permissions_2026 terminé' AS Resultat;
