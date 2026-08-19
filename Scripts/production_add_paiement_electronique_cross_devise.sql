-- =============================================================================
-- Script SQL production : PaiementElectronique cross-devise
-- Équivalent EF : 20260819134325_PaiementElectroniqueCrossDevise
-- SGBD : MySQL / MariaDB
--
-- Objectif :
-- - stocker le snapshot de conversion figé pour FlexPay
-- - séparer montant facture vs montant réellement débité
-- - permettre la finalisation comptable d'un paiement électronique cross-devise
--
-- IMPORTANT :
-- 1. Faire une sauvegarde avant exécution
-- 2. Script idempotent
--
-- Usage :
--   mysql -u USER -p DBNAME < Scripts/production_add_paiement_electronique_cross_devise.sql
-- =============================================================================

START TRANSACTION;

-- ---------------------------------------------------------------------------
-- PaiementsElectroniquesEnAttente
-- ---------------------------------------------------------------------------

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'CodeDeviseFacture'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `PaiementsElectroniquesEnAttente` ADD COLUMN `CodeDeviseFacture` VARCHAR(3) NULL AFTER `CodeDevisePaiement`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'CodeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `PaiementsElectroniquesEnAttente` ADD COLUMN `CodeDevisePrincipale` VARCHAR(3) NULL AFTER `CodeDeviseFacture`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'MontantFacture'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `PaiementsElectroniquesEnAttente` ADD COLUMN `MontantFacture` DECIMAL(18,2) NULL AFTER `Montant`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'MontantFactureDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `PaiementsElectroniquesEnAttente` ADD COLUMN `MontantFactureDevisePrincipale` DECIMAL(18,2) NULL AFTER `MontantFacture`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'TauxFactureVersDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `PaiementsElectroniquesEnAttente` ADD COLUMN `TauxFactureVersDevisePrincipale` DECIMAL(18,6) NULL AFTER `MontantFactureDevisePrincipale`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'TauxFactureVersPaiement'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `PaiementsElectroniquesEnAttente` ADD COLUMN `TauxFactureVersPaiement` DECIMAL(18,6) NULL AFTER `TauxFactureVersDevisePrincipale`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Backfill sécurité pour les pending déjà existants
UPDATE `PaiementsElectroniquesEnAttente`
SET `MontantFacture` = COALESCE(`MontantFacture`, `Montant`),
    `CodeDeviseFacture` = COALESCE(NULLIF(`CodeDeviseFacture`, ''), `CodeDevisePaiement`),
    `TauxFactureVersPaiement` = COALESCE(`TauxFactureVersPaiement`, 1)
WHERE COALESCE(`CodeDevisePaiement`, '') <> '';

-- Rendre les colonnes cœur non nulles après backfill
SET @is_nullable := (
    SELECT IS_NULLABLE FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'CodeDeviseFacture'
);
SET @sql := IF(@is_nullable = 'YES',
    'ALTER TABLE `PaiementsElectroniquesEnAttente` MODIFY COLUMN `CodeDeviseFacture` VARCHAR(3) NOT NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @is_nullable := (
    SELECT IS_NULLABLE FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PaiementsElectroniquesEnAttente'
      AND COLUMN_NAME = 'MontantFacture'
);
SET @sql := IF(@is_nullable = 'YES',
    'ALTER TABLE `PaiementsElectroniquesEnAttente` MODIFY COLUMN `MontantFacture` DECIMAL(18,2) NOT NULL',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- Paiements
-- ---------------------------------------------------------------------------

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Paiements'
      AND COLUMN_NAME = 'CodeDeviseFacture'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `CodeDeviseFacture` VARCHAR(3) NULL AFTER `CodeDevisePaiement`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Paiements'
      AND COLUMN_NAME = 'MontantPayeDevisePaiement'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `MontantPayeDevisePaiement` DECIMAL(18,2) NULL AFTER `MontantPaye`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Paiements'
      AND COLUMN_NAME = 'TauxFactureVersDevisePaiement'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `TauxFactureVersDevisePaiement` DECIMAL(18,6) NULL AFTER `CodeDevisePrincipale`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Historique EF
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260819134325_PaiementElectroniqueCrossDevise', '6.0.25'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1
    FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20260819134325_PaiementElectroniqueCrossDevise'
);

COMMIT;

SELECT 'OK: PaiementElectronique cross-devise' AS Resultat;
