-- =============================================================================
-- Script SQL production : Module FlexPay (paiement électronique)
-- Équivalent EF : 20260714063200_AjoutModuleFlexPay
-- SGBD : MySQL / MariaDB
-- IMPORTANT : backup avant exécution
-- =============================================================================

START TRANSACTION;

CREATE TABLE IF NOT EXISTS `InfosPaiementSociete` (
    `IdInfoPaiementSociete` INT NOT NULL AUTO_INCREMENT,
    `IdSociete` INT NOT NULL,
    `CodeMarchand` VARCHAR(100) NOT NULL,
    `ApiToken` VARCHAR(500) NOT NULL,
    `ActifMobileMoney` TINYINT(1) NOT NULL DEFAULT 1,
    `ActifCarteBancaire` TINYINT(1) NOT NULL DEFAULT 0,
    `Statut` TINYINT(1) NOT NULL DEFAULT 1,
    `DateCreation` DATETIME(6) NOT NULL,
    `DateModification` DATETIME(6) NULL,
    PRIMARY KEY (`IdInfoPaiementSociete`),
    KEY `IX_InfosPaiementSociete_IdSociete` (`IdSociete`),
    CONSTRAINT `FK_InfosPaiementSociete_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `PaiementsElectroniquesEnAttente` (
    `IdPaiementElectroniqueEnAttente` INT NOT NULL AUTO_INCREMENT,
    `IdSociete` INT NOT NULL,
    `IdClient` INT NOT NULL,
    `IdClientFacture` INT NULL,
    `IdFacture` INT NULL,
    `IdUtilisateur` INT NULL,
    `Montant` DECIMAL(18,2) NOT NULL,
    `CodeDevisePaiement` VARCHAR(3) NOT NULL DEFAULT 'CDF',
    `Methode` VARCHAR(30) NOT NULL,
    `Telephone` VARCHAR(20) NULL,
    `Reference` VARCHAR(100) NOT NULL,
    `OrderNumber` VARCHAR(100) NULL,
    `PaymentUrl` VARCHAR(500) NULL,
    `Statut` VARCHAR(20) NOT NULL DEFAULT 'EnAttente',
    `IdPaiementFinalise` INT NULL,
    `HoldExpireAt` DATETIME(6) NOT NULL,
    `DateCreation` DATETIME(6) NOT NULL,
    `DateFinalisation` DATETIME(6) NULL,
    `MessageErreur` VARCHAR(500) NULL,
    PRIMARY KEY (`IdPaiementElectroniqueEnAttente`),
    UNIQUE KEY `UX_PaiementElectronique_Reference` (`Reference`),
    KEY `IX_PaiementElectronique_OrderNumber` (`OrderNumber`),
    KEY `IX_PaiementElectronique_Societe_Statut` (`IdSociete`, `Statut`),
    CONSTRAINT `FK_PaiementElectronique_Societes` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PaiementElectronique_Clients` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PaiementElectronique_ClientFactures` FOREIGN KEY (`IdClientFacture`) REFERENCES `ClientFactures` (`IdClientFacture`) ON DELETE SET NULL,
    CONSTRAINT `FK_PaiementElectronique_Factures` FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) ON DELETE SET NULL,
    CONSTRAINT `FK_PaiementElectronique_Paiements` FOREIGN KEY (`IdPaiementFinalise`) REFERENCES `Paiements` (`IdPaiement`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `TransactionsFlexPay` (
    `IdTransactionFlexPay` INT NOT NULL AUTO_INCREMENT,
    `IdPaiementElectroniqueEnAttente` INT NOT NULL,
    `IdSociete` INT NOT NULL,
    `Reference` VARCHAR(100) NOT NULL,
    `OrderNumber` VARCHAR(100) NULL,
    `TypeFlexPay` VARCHAR(5) NOT NULL DEFAULT '1',
    `Montant` DECIMAL(18,2) NOT NULL,
    `CodeDevise` VARCHAR(3) NOT NULL DEFAULT 'CDF',
    `NombreCallbacks` INT NOT NULL DEFAULT 0,
    `DateCreation` DATETIME(6) NOT NULL,
    PRIMARY KEY (`IdTransactionFlexPay`),
    KEY `IX_TransactionFlexPay_OrderNumber` (`OrderNumber`),
    CONSTRAINT `FK_TransactionFlexPay_Pending`
        FOREIGN KEY (`IdPaiementElectroniqueEnAttente`)
        REFERENCES `PaiementsElectroniquesEnAttente` (`IdPaiementElectroniqueEnAttente`)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `CallbacksFlexPay` (
    `IdCallbackFlexPay` INT NOT NULL AUTO_INCREMENT,
    `OrderNumber` VARCHAR(100) NULL,
    `Reference` VARCHAR(100) NULL,
    `Code` VARCHAR(20) NULL,
    `PayloadJson` LONGTEXT NULL,
    `HeadersJson` VARCHAR(1000) NULL,
    `IpAddress` VARCHAR(50) NULL,
    `TraiteAvecSucces` TINYINT(1) NOT NULL DEFAULT 0,
    `MessageTraitement` VARCHAR(500) NULL,
    `DateReception` DATETIME(6) NOT NULL,
    PRIMARY KEY (`IdCallbackFlexPay`),
    KEY `IX_CallbackFlexPay_OrderNumber` (`OrderNumber`),
    KEY `IX_CallbackFlexPay_DateReception` (`DateReception`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `PaiementHolds` (
    `IdPaiementHold` INT NOT NULL AUTO_INCREMENT,
    `IdSociete` INT NOT NULL,
    `CleRessource` VARCHAR(100) NOT NULL,
    `Telephone` VARCHAR(20) NULL,
    `IdPaiementElectroniqueEnAttente` INT NULL,
    `ExpireAt` DATETIME(6) NOT NULL,
    `DateCreation` DATETIME(6) NOT NULL,
    `EstLibere` TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (`IdPaiementHold`),
    KEY `IX_PaiementHold_Societe_Cle` (`IdSociete`, `CleRessource`, `EstLibere`),
    CONSTRAINT `FK_PaiementHold_Pending`
        FOREIGN KEY (`IdPaiementElectroniqueEnAttente`)
        REFERENCES `PaiementsElectroniquesEnAttente` (`IdPaiementElectroniqueEnAttente`)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260714063200_AjoutModuleFlexPay', '6.0.25'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20260714063200_AjoutModuleFlexPay'
);

COMMIT;
