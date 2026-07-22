-- =============================================================================
-- Script SQL production : Module multi-devises
-- Équivalent EF : 20260714044826_AjoutModuleMultiDevise
-- SGBD : MySQL / MariaDB (Pomelo)
--
-- ObjectIF : devises par société, taux de change, snapshots monétaires sur
-- Factures / ClientFactures / Paiements, seed CDF pour l'existant.
--
-- IMPORTANT :
-- 1. Faire une sauvegarde avant exécution
-- 2. Exécuter une seule fois (script idempotent autant que possible)
-- 3. Déployer ensuite l'API qui contient le code multi-devises
--
-- Usage :
--   mysqldump -u USER -p DBNAME > backup_avant_multidevise.sql
--   mysql -u USER -p DBNAME < Scripts/production_add_module_multidevise.sql
-- =============================================================================

START TRANSACTION;

-- -----------------------------------------------------------------------------
-- Helpers : procédures pour ajouts conditionnels (MySQL/MariaDB)
-- -----------------------------------------------------------------------------

-- Societes.CodeDevisePrincipale
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Societes'
      AND COLUMN_NAME = 'CodeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Societes` ADD COLUMN `CodeDevisePrincipale` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Factures
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Factures' AND COLUMN_NAME = 'CodeDevisePrix'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Factures` ADD COLUMN `CodeDevisePrix` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Factures' AND COLUMN_NAME = 'CodeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Factures` ADD COLUMN `CodeDevisePrincipale` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Factures' AND COLUMN_NAME = 'MontantDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Factures` ADD COLUMN `MontantDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Factures' AND COLUMN_NAME = 'TauxVersDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Factures` ADD COLUMN `TauxVersDevisePrincipale` DECIMAL(18,6) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ClientFactures
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientFactures' AND COLUMN_NAME = 'CodeDevisePrix'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ClientFactures` ADD COLUMN `CodeDevisePrix` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientFactures' AND COLUMN_NAME = 'CodeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ClientFactures` ADD COLUMN `CodeDevisePrincipale` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientFactures' AND COLUMN_NAME = 'MontantDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ClientFactures` ADD COLUMN `MontantDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientFactures' AND COLUMN_NAME = 'MontantPayeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ClientFactures` ADD COLUMN `MontantPayeDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientFactures' AND COLUMN_NAME = 'MontantDuDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ClientFactures` ADD COLUMN `MontantDuDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ClientFactures' AND COLUMN_NAME = 'TauxVersDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ClientFactures` ADD COLUMN `TauxVersDevisePrincipale` DECIMAL(18,6) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Paiements
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'CodeDevisePaiement'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `CodeDevisePaiement` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'CodeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `CodeDevisePrincipale` VARCHAR(3) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'MontantPayeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `MontantPayeDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'MontantAPayeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `MontantAPayeDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'ResteAPayeDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `ResteAPayeDevisePrincipale` DECIMAL(18,2) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'TauxVersDevisePrincipale'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `TauxVersDevisePrincipale` DECIMAL(18,6) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- Tables DevisesMonetaires / TauxChanges
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS `DevisesMonetaires` (
    `IdDeviseMonetaire` INT NOT NULL AUTO_INCREMENT,
    `IdSociete` INT NOT NULL,
    `CodeDevise` VARCHAR(3) NOT NULL,
    `Libelle` VARCHAR(100) NOT NULL,
    `Symbole` VARCHAR(10) NULL,
    `Statut` TINYINT(1) NOT NULL DEFAULT 1,
    `DateCreation` DATETIME(6) NOT NULL,
    `DateModification` DATETIME(6) NULL,
    PRIMARY KEY (`IdDeviseMonetaire`),
    UNIQUE KEY `UX_DevisesMonetaires_Societe_Code` (`IdSociete`, `CodeDevise`),
    CONSTRAINT `FK_DevisesMonetaires_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`)
        REFERENCES `Societes` (`IdSociete`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `TauxChanges` (
    `IdTauxChange` INT NOT NULL AUTO_INCREMENT,
    `IdSociete` INT NOT NULL,
    `CodeDeviseSource` VARCHAR(3) NOT NULL,
    `CodeDeviseCible` VARCHAR(3) NOT NULL,
    `Taux` DECIMAL(18,6) NOT NULL,
    `DateEffet` DATETIME(6) NOT NULL,
    `DateCreation` DATETIME(6) NOT NULL,
    PRIMARY KEY (`IdTauxChange`),
    KEY `IX_TauxChanges_Societe_Paired_DateEffet` (`IdSociete`, `CodeDeviseSource`, `CodeDeviseCible`, `DateEffet`),
    CONSTRAINT `FK_TauxChanges_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`)
        REFERENCES `Societes` (`IdSociete`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Index unique DevisesMonetaires si table existait sans l'index
SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'DevisesMonetaires'
      AND INDEX_NAME = 'UX_DevisesMonetaires_Societe_Code'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE UNIQUE INDEX `UX_DevisesMonetaires_Societe_Code` ON `DevisesMonetaires` (`IdSociete`, `CodeDevise`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'TauxChanges'
      AND INDEX_NAME = 'IX_TauxChanges_Societe_Paired_DateEffet'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_TauxChanges_Societe_Paired_DateEffet` ON `TauxChanges` (`IdSociete`, `CodeDeviseSource`, `CodeDeviseCible`, `DateEffet`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- Seed CDF + backfill historique
-- -----------------------------------------------------------------------------

UPDATE `Societes`
SET `CodeDevisePrincipale` = 'CDF'
WHERE `CodeDevisePrincipale` IS NULL OR `CodeDevisePrincipale` = '';

INSERT INTO `DevisesMonetaires` (`IdSociete`, `CodeDevise`, `Libelle`, `Symbole`, `Statut`, `DateCreation`, `DateModification`)
SELECT s.`IdSociete`, 'CDF', 'Franc congolais', 'FC', 1, UTC_TIMESTAMP(6), NULL
FROM `Societes` s
WHERE NOT EXISTS (
    SELECT 1 FROM `DevisesMonetaires` d
    WHERE d.`IdSociete` = s.`IdSociete` AND d.`CodeDevise` = 'CDF'
);

UPDATE `Factures`
SET `CodeDevisePrix` = 'CDF',
    `CodeDevisePrincipale` = 'CDF',
    `TauxVersDevisePrincipale` = 1,
    `MontantDevisePrincipale` = `Montant`
WHERE `CodeDevisePrix` IS NULL;

UPDATE `ClientFactures`
SET `CodeDevisePrix` = 'CDF',
    `CodeDevisePrincipale` = 'CDF',
    `TauxVersDevisePrincipale` = 1,
    `MontantDevisePrincipale` = `Montant`,
    `MontantPayeDevisePrincipale` = IFNULL(`MontantPaye`, 0),
    `MontantDuDevisePrincipale` = `MontantDu`
WHERE `CodeDevisePrix` IS NULL;

UPDATE `Paiements`
SET `CodeDevisePaiement` = 'CDF',
    `CodeDevisePrincipale` = 'CDF',
    `TauxVersDevisePrincipale` = 1,
    `MontantPayeDevisePrincipale` = `MontantPaye`,
    `MontantAPayeDevisePrincipale` = `MontantAPaye`,
    `ResteAPayeDevisePrincipale` = `ResteAPaye`
WHERE `CodeDevisePaiement` IS NULL;

-- -----------------------------------------------------------------------------
-- Historique EF (évite un double-apply via dotnet ef database update)
-- -----------------------------------------------------------------------------

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260714044826_AjoutModuleMultiDevise', '6.0.25'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20260714044826_AjoutModuleMultiDevise'
);

COMMIT;

-- =============================================================================
-- CHECKS de validation (à exécuter manuellement après coup)
-- =============================================================================
/*
SELECT COUNT(*) AS SocietesSansDevisePrincipale
FROM Societes
WHERE CodeDevisePrincipale IS NULL OR CodeDevisePrincipale = '';

SELECT COUNT(*) AS FacturesSansDevise
FROM Factures
WHERE CodeDevisePrix IS NULL;

SELECT COUNT(*) AS ClientFacturesSansDevise
FROM ClientFactures
WHERE CodeDevisePrix IS NULL;

SELECT COUNT(*) AS PaiementsSansDevise
FROM Paiements
WHERE CodeDevisePaiement IS NULL;

SELECT s.IdSociete, s.Nom, s.CodeDevisePrincipale, d.CodeDevise, d.Libelle
FROM Societes s
LEFT JOIN DevisesMonetaires d ON d.IdSociete = s.IdSociete AND d.CodeDevise = 'CDF'
ORDER BY s.IdSociete;

SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
WHERE MigrationId = '20260714044826_AjoutModuleMultiDevise';
*/

-- =============================================================================
-- DOWN : rollback planifié uniquement (ne pas exécuter en prod sans besoin)
-- =============================================================================
/*
START TRANSACTION;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260714044826_AjoutModuleMultiDevise';

DROP TABLE IF EXISTS `TauxChanges`;
DROP TABLE IF EXISTS `DevisesMonetaires`;

ALTER TABLE `Paiements`
    DROP COLUMN IF EXISTS `CodeDevisePaiement`,
    DROP COLUMN IF EXISTS `CodeDevisePrincipale`,
    DROP COLUMN IF EXISTS `MontantPayeDevisePrincipale`,
    DROP COLUMN IF EXISTS `MontantAPayeDevisePrincipale`,
    DROP COLUMN IF EXISTS `ResteAPayeDevisePrincipale`,
    DROP COLUMN IF EXISTS `TauxVersDevisePrincipale`;

ALTER TABLE `ClientFactures`
    DROP COLUMN IF EXISTS `CodeDevisePrix`,
    DROP COLUMN IF EXISTS `CodeDevisePrincipale`,
    DROP COLUMN IF EXISTS `MontantDevisePrincipale`,
    DROP COLUMN IF EXISTS `MontantPayeDevisePrincipale`,
    DROP COLUMN IF EXISTS `MontantDuDevisePrincipale`,
    DROP COLUMN IF EXISTS `TauxVersDevisePrincipale`;

ALTER TABLE `Factures`
    DROP COLUMN IF EXISTS `CodeDevisePrix`,
    DROP COLUMN IF EXISTS `CodeDevisePrincipale`,
    DROP COLUMN IF EXISTS `MontantDevisePrincipale`,
    DROP COLUMN IF EXISTS `TauxVersDevisePrincipale`;

ALTER TABLE `Societes`
    DROP COLUMN IF EXISTS `CodeDevisePrincipale`;

COMMIT;
*/
