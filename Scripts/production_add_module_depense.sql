-- =============================================================================
-- Script SQL production : Module Dépenses
-- Équivalent EF (partiel) : 20260814143446_AjoutModuleDepense
-- SGBD : MySQL / MariaDB (Pomelo)
--
-- Objectif : tables CategorieDepenses / Depenses, index, seed catégories par défaut.
--
-- IMPORTANT :
-- 1. Faire une sauvegarde avant exécution
-- 2. Exécuter une seule fois (script idempotent)
-- 3. Prérequis : module multidevise (CodeDevisePrincipale sur Societes)
--
-- Usage :
--   mysqldump -u USER -p DBNAME > backup_avant_depense.sql
--   mysql -u USER -p DBNAME < Scripts/production_add_module_depense.sql
-- =============================================================================

START TRANSACTION;

-- -----------------------------------------------------------------------------
-- Table CategorieDepenses
-- -----------------------------------------------------------------------------
SET @tbl_exists := (
    SELECT COUNT(*) FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CategorieDepenses'
);
SET @sql := IF(@tbl_exists = 0,
    'CREATE TABLE `CategorieDepenses` (
        `IdCategorieDepense` INT NOT NULL AUTO_INCREMENT,
        `IdSociete` INT NOT NULL,
        `NomCategorie` VARCHAR(100) NOT NULL,
        `Description` VARCHAR(500) NULL,
        `Statut` TINYINT(1) NOT NULL DEFAULT 1,
        `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        PRIMARY KEY (`IdCategorieDepense`),
        UNIQUE INDEX `IX_CategorieDepense_Societe_Nom` (`IdSociete`, `NomCategorie`),
        CONSTRAINT `FK_CategorieDepenses_Societes_IdSociete`
            FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- Table Depenses
-- -----------------------------------------------------------------------------
SET @tbl_exists := (
    SELECT COUNT(*) FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Depenses'
);
SET @sql := IF(@tbl_exists = 0,
    'CREATE TABLE `Depenses` (
        `IdDepense` INT NOT NULL AUTO_INCREMENT,
        `IdSociete` INT NOT NULL,
        `IdCategorieDepense` INT NULL,
        `Libelle` VARCHAR(200) NOT NULL,
        `Description` VARCHAR(1000) NULL,
        `Beneficiaire` VARCHAR(200) NULL,
        `ReferencePiece` VARCHAR(100) NULL,
        `Montant` DECIMAL(18,2) NOT NULL,
        `CodeDeviseMontant` VARCHAR(3) NULL,
        `CodeDevisePrincipale` VARCHAR(3) NULL,
        `TauxVersDevisePrincipale` DECIMAL(18,6) NULL,
        `MontantDevisePrincipale` DECIMAL(18,2) NULL,
        `ModePaiement` VARCHAR(50) NULL,
        `DateDepense` DATETIME(6) NOT NULL,
        `Statut` VARCHAR(20) NOT NULL,
        `IdUtilisateurCreateur` INT NOT NULL,
        `IdUtilisateurValidateur` INT NULL,
        `DateValidation` DATETIME(6) NULL,
        `IdCabine` INT NULL,
        `IdAxe` INT NULL,
        `MotifAnnulation` VARCHAR(500) NULL,
        `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
        `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
        `UpdatedAt` DATETIME(6) NULL,
        PRIMARY KEY (`IdDepense`),
        INDEX `IX_Depense_Societe_Date` (`IdSociete`, `DateDepense`),
        INDEX `IX_Depense_Societe_Statut` (`IdSociete`, `Statut`),
        INDEX `IX_Depense_UtilisateurCreateur` (`IdUtilisateurCreateur`),
        INDEX `IX_Depenses_IdCategorieDepense` (`IdCategorieDepense`),
        INDEX `IX_Depenses_IdCabine` (`IdCabine`),
        INDEX `IX_Depenses_IdAxe` (`IdAxe`),
        INDEX `IX_Depenses_IdUtilisateurValidateur` (`IdUtilisateurValidateur`),
        CONSTRAINT `FK_Depenses_Societes_IdSociete`
            FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Depenses_CategorieDepenses_IdCategorieDepense`
            FOREIGN KEY (`IdCategorieDepense`) REFERENCES `CategorieDepenses` (`IdCategorieDepense`) ON DELETE SET NULL,
        CONSTRAINT `FK_Depenses_Utilisateurs_IdUtilisateurCreateur`
            FOREIGN KEY (`IdUtilisateurCreateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Depenses_Utilisateurs_IdUtilisateurValidateur`
            FOREIGN KEY (`IdUtilisateurValidateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL,
        CONSTRAINT `FK_Depenses_Cabines_IdCabine`
            FOREIGN KEY (`IdCabine`) REFERENCES `Cabines` (`IdCabine`) ON DELETE SET NULL,
        CONSTRAINT `FK_Depenses_Axes_IdAxe`
            FOREIGN KEY (`IdAxe`) REFERENCES `Axes` (`IdAxe`) ON DELETE SET NULL
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- Seed catégories par défaut par société
-- -----------------------------------------------------------------------------
INSERT INTO `CategorieDepenses` (`IdSociete`, `NomCategorie`, `Description`, `Statut`, `DateCreation`)
SELECT s.`IdSociete`, v.`NomCategorie`, v.`Description`, 1, NOW(6)
FROM `Societes` s
CROSS JOIN (
    SELECT 'Carburant' AS NomCategorie, 'Dépenses carburant et transport' AS Description
    UNION ALL SELECT 'Maintenance', 'Entretien et réparations'
    UNION ALL SELECT 'Fournitures', 'Fournitures de bureau et consommables'
    UNION ALL SELECT 'Autre', 'Autres dépenses'
) v
WHERE NOT EXISTS (
    SELECT 1 FROM `CategorieDepenses` c
    WHERE c.`IdSociete` = s.`IdSociete` AND c.`NomCategorie` = v.`NomCategorie`
);

COMMIT;

SELECT 'Module Dépenses : tables et seed catégories OK' AS Resultat;
