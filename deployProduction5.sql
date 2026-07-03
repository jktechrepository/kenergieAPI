CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `AuditLogs` (
    `IdAudit` bigint NOT NULL AUTO_INCREMENT,
    `TableName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `RecordId` int NOT NULL,
    `Action` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `UserId` int NOT NULL,
    `UserName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `UserRole` varchar(50) CHARACTER SET utf8mb4 NULL,
    `IdSociete` int NULL,
    `DateAction` datetime(6) NOT NULL,
    `OldValues` TEXT CHARACTER SET utf8mb4 NULL,
    `NewValues` TEXT CHARACTER SET utf8mb4 NULL,
    `ChangedFields` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IpAddress` varchar(50) CHARACTER SET utf8mb4 NULL,
    `UserAgent` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Commentaire` TEXT CHARACTER SET utf8mb4 NULL,
    `HttpMethod` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Endpoint` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DurationMs` int NULL,
    `Success` tinyint(1) NOT NULL,
    `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AuditLogs` PRIMARY KEY (`IdAudit`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Permissions` (
    `IdPermission` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Categorie` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Action` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Permissions` PRIMARY KEY (`IdPermission`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Roles` (
    `IdRole` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Niveau` int NULL,
    `Statut` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Roles` PRIMARY KEY (`IdRole`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Societes` (
    `IdSociete` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Slogan` longtext CHARACTER SET utf8mb4 NULL,
    `Longitute` longtext CHARACTER SET utf8mb4 NULL,
    `Latitude` longtext CHARACTER SET utf8mb4 NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Logo` longtext CHARACTER SET utf8mb4 NULL,
    `Telephone` longtext CHARACTER SET utf8mb4 NULL,
    `EmailContact` longtext CHARACTER SET utf8mb4 NULL,
    `SiteWeb` longtext CHARACTER SET utf8mb4 NULL,
    `ProvinceEducationnel` longtext CHARACTER SET utf8mb4 NULL,
    `NomCompletResponsable` longtext CHARACTER SET utf8mb4 NULL,
    `GenreResponsable` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `AcceptNotification` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    `Province` longtext CHARACTER SET utf8mb4 NULL,
    `Ville` longtext CHARACTER SET utf8mb4 NULL,
    `Commune` longtext CHARACTER SET utf8mb4 NULL,
    `Quartier` longtext CHARACTER SET utf8mb4 NULL,
    `Avenue` longtext CHARACTER SET utf8mb4 NULL,
    `Numero` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Societes` PRIMARY KEY (`IdSociete`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `RolePermissions` (
    `IdRolePermission` int NOT NULL AUTO_INCREMENT,
    `IdRole` int NOT NULL,
    `IdPermission` int NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    `IdUtilisateurAttribution` int NULL,
    `PermissionIdPermission` int NULL,
    `RoleIdRole` int NULL,
    CONSTRAINT `PK_RolePermissions` PRIMARY KEY (`IdRolePermission`),
    CONSTRAINT `FK_RolePermissions_Permissions_IdPermission` FOREIGN KEY (`IdPermission`) REFERENCES `Permissions` (`IdPermission`) ON DELETE CASCADE,
    CONSTRAINT `FK_RolePermissions_Permissions_PermissionIdPermission` FOREIGN KEY (`PermissionIdPermission`) REFERENCES `Permissions` (`IdPermission`),
    CONSTRAINT `FK_RolePermissions_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE CASCADE,
    CONSTRAINT `FK_RolePermissions_Roles_RoleIdRole` FOREIGN KEY (`RoleIdRole`) REFERENCES `Roles` (`IdRole`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Agents` (
    `IdAgent` int NOT NULL AUTO_INCREMENT,
    `Matricule` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Nom` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Postnom` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Prenom` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Genre` varchar(10) CHARACTER SET utf8mb4 NULL,
    `DateNaissance` datetime(6) NOT NULL,
    `TelephoneAgent` longtext CHARACTER SET utf8mb4 NULL,
    `EmailAgent` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `EtatCivil` varchar(20) CHARACTER SET utf8mb4 NULL,
    `SerialNumber` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Fonction` longtext CHARACTER SET utf8mb4 NULL,
    `RoleAgent` longtext CHARACTER SET utf8mb4 NULL,
    `PhotoUrl` longtext CHARACTER SET utf8mb4 NULL,
    `IdSociete` int NULL,
    `AdresseResidence` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `Province` longtext CHARACTER SET utf8mb4 NULL,
    `Ville` longtext CHARACTER SET utf8mb4 NULL,
    `Commune` longtext CHARACTER SET utf8mb4 NULL,
    `Quartier` longtext CHARACTER SET utf8mb4 NULL,
    `Avenue` longtext CHARACTER SET utf8mb4 NULL,
    `Numero` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Agents` PRIMARY KEY (`IdAgent`),
    CONSTRAINT `FK_Agents_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `CategorieClients` (
    `IdCategorie` int NOT NULL AUTO_INCREMENT,
    `NomCategorie` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Actif` tinyint(1) NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `SocieteIdSociete1` int NULL,
    CONSTRAINT `PK_CategorieClients` PRIMARY KEY (`IdCategorie`),
    CONSTRAINT `FK_CategorieClients_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_CategorieClients_Societes_SocieteIdSociete1` FOREIGN KEY (`SocieteIdSociete1`) REFERENCES `Societes` (`IdSociete`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Utilisateurs` (
    `IdUtilisateur` int NOT NULL AUTO_INCREMENT,
    `ReferenceUtilisateur` char(36) COLLATE ascii_general_ci NULL,
    `NomUtilisateur` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `PostNomUtilisateur` longtext CHARACTER SET utf8mb4 NULL,
    `PrenomUtilisateur` longtext CHARACTER SET utf8mb4 NULL,
    `Email` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Telephone` longtext CHARACTER SET utf8mb4 NULL,
    `PhotoUrl` longtext CHARACTER SET utf8mb4 NULL,
    `LieuNaissance` longtext CHARACTER SET utf8mb4 NULL,
    `DateNaissance` datetime(6) NULL,
    `Genre` longtext CHARACTER SET utf8mb4 NULL,
    `MotDePasseHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DefaultUsername` longtext CHARACTER SET utf8mb4 NULL,
    `DoitChangerMotDePasse` tinyint(1) NOT NULL,
    `Statut` tinyint(1) NULL,
    `IdRole` int NULL,
    `IdSociete` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    `IsConnecte` tinyint(1) NOT NULL,
    `IdAgent` int NULL,
    `Province` longtext CHARACTER SET utf8mb4 NULL,
    `Ville` longtext CHARACTER SET utf8mb4 NULL,
    `Commune` longtext CHARACTER SET utf8mb4 NULL,
    `Quartier` longtext CHARACTER SET utf8mb4 NULL,
    `Avenue` longtext CHARACTER SET utf8mb4 NULL,
    `Numero` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Utilisateurs` PRIMARY KEY (`IdUtilisateur`),
    CONSTRAINT `FK_Utilisateurs_Agents_IdAgent` FOREIGN KEY (`IdAgent`) REFERENCES `Agents` (`IdAgent`),
    CONSTRAINT `FK_Utilisateurs_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE CASCADE,
    CONSTRAINT `FK_Utilisateurs_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Clients` (
    `IdClient` int NOT NULL AUTO_INCREMENT,
    `NomClient` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Commune` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Quartier` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Telephone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `numero_compteur` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `IdCategorieClient` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Clients` PRIMARY KEY (`IdClient`),
    CONSTRAINT `FK_Clients_CategorieClients_IdCategorieClient` FOREIGN KEY (`IdCategorieClient`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `Factures` (
    `IdFacture` int NOT NULL AUTO_INCREMENT,
    `numero_facture` varchar(100) CHARACTER SET utf8mb4 NULL,
    `MontantPaye` decimal(18,2) NULL,
    `DateEmission` datetime(6) NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `MoisEmission` int NOT NULL,
    `AnneesEmission` int NOT NULL,
    `IdCategorie` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Factures` PRIMARY KEY (`IdFacture`),
    CONSTRAINT `FK_Factures_CategorieClients_IdCategorie` FOREIGN KEY (`IdCategorie`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `Notifications` (
    `IdNotification` int NOT NULL AUTO_INCREMENT,
    `Titre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NULL,
    `Contenu` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `TypeNotification` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `EstLue` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateLecture` datetime(6) NULL,
    `LienAction` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Icone` varchar(50) CHARACTER SET utf8mb4 NULL,
    `EstActive` tinyint(1) NOT NULL,
    `IdExpediteur` int NULL,
    `IdDestinataire` int NULL,
    `IdSociete` int NULL,
    `IdAgent` int NULL,
    `CanalUtilise` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Priorite` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `PayloadJson` longtext CHARACTER SET utf8mb4 NULL,
    `StatutEnvoi` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `TrackingId` varchar(100) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Notifications` PRIMARY KEY (`IdNotification`),
    CONSTRAINT `FK_Notifications_Agents_IdAgent` FOREIGN KEY (`IdAgent`) REFERENCES `Agents` (`IdAgent`),
    CONSTRAINT `FK_Notifications_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`),
    CONSTRAINT `FK_Notifications_Utilisateurs_IdDestinataire` FOREIGN KEY (`IdDestinataire`) REFERENCES `Utilisateurs` (`IdUtilisateur`),
    CONSTRAINT `FK_Notifications_Utilisateurs_IdExpediteur` FOREIGN KEY (`IdExpediteur`) REFERENCES `Utilisateurs` (`IdUtilisateur`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `PasswordResetTokens` (
    `IdPasswordResetToken` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `Token` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NOT NULL,
    `DateUtilisation` datetime(6) NULL,
    CONSTRAINT `PK_PasswordResetTokens` PRIMARY KEY (`IdPasswordResetToken`),
    CONSTRAINT `FK_PasswordResetTokens_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `RefreshTokens` (
    `IdRefreshToken` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `TokenHash` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NOT NULL,
    `DateRevocation` datetime(6) NULL,
    `DeviceInfo` varchar(200) CHARACTER SET utf8mb4 NULL,
    `IpAddress` varchar(50) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`IdRefreshToken`),
    CONSTRAINT `FK_RefreshTokens_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `SmsLogs` (
    `IdSmsLog` int NOT NULL AUTO_INCREMENT,
    `NumeroDestinataire` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdUtilisateur` int NULL,
    `Message` varchar(1600) CHARACTER SET utf8mb4 NOT NULL,
    `TypeNotification` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `MessageSid` varchar(100) CHARACTER SET utf8mb4 NULL,
    `MessageErreur` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CodeErreur` int NULL,
    `CoutUsd` double NOT NULL,
    `CoutFc` double NOT NULL,
    `DateEnvoi` datetime(6) NOT NULL,
    `DateLivraison` datetime(6) NULL,
    `DateEchec` datetime(6) NULL,
    `NombreSegments` int NOT NULL,
    `Direction` varchar(10) CHARACTER SET utf8mb4 NULL,
    `NumeroExpediteur` varchar(20) CHARACTER SET utf8mb4 NULL,
    `UtilisateurIdUtilisateur` int NULL,
    CONSTRAINT `PK_SmsLogs` PRIMARY KEY (`IdSmsLog`),
    CONSTRAINT `FK_SmsLogs_Utilisateurs_UtilisateurIdUtilisateur` FOREIGN KEY (`UtilisateurIdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserDevices` (
    `IdUserDevice` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `FcmToken` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `DeviceType` varchar(100) CHARACTER SET utf8mb4 NULL,
    `DeviceModel` varchar(100) CHARACTER SET utf8mb4 NULL,
    `OsVersion` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DefaultDevice` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `DateEnregistrement` datetime(6) NOT NULL,
    `DateDerniereUtilisation` datetime(6) NULL,
    CONSTRAINT `PK_UserDevices` PRIMARY KEY (`IdUserDevice`),
    CONSTRAINT `FK_UserDevices_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserPermissions` (
    `IdUserPermission` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `IdPermission` int NOT NULL,
    `IsGranted` tinyint(1) NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NULL,
    `Commentaire` varchar(500) CHARACTER SET utf8mb4 NULL,
    `AttribueParIdUtilisateur` int NULL,
    CONSTRAINT `PK_UserPermissions` PRIMARY KEY (`IdUserPermission`),
    CONSTRAINT `FK_UserPermissions_Permissions_IdPermission` FOREIGN KEY (`IdPermission`) REFERENCES `Permissions` (`IdPermission`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserPermissions_Utilisateurs_AttribueParIdUtilisateur` FOREIGN KEY (`AttribueParIdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`),
    CONSTRAINT `FK_UserPermissions_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserRoles` (
    `IdUserRole` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `IdRole` int NOT NULL,
    `IsPrimary` tinyint(1) NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    `IdUtilisateurAttribution` int NULL,
    `Statut` tinyint(1) NULL,
    CONSTRAINT `PK_UserRoles` PRIMARY KEY (`IdUserRole`),
    CONSTRAINT `FK_UserRoles_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE RESTRICT,
    CONSTRAINT `FK_UserRoles_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Agents_Email_Unique` ON `Agents` (`EmailAgent`);

CREATE INDEX `IX_Agents_IdSociete` ON `Agents` (`IdSociete`);

CREATE UNIQUE INDEX `IX_Agents_Matricule_Unique` ON `Agents` (`Matricule`);

CREATE UNIQUE INDEX `IX_Agents_SerialNumber_Unique` ON `Agents` (`SerialNumber`);

CREATE INDEX `IX_AuditLog_Action` ON `AuditLogs` (`Action`);

CREATE INDEX `IX_AuditLog_DateAction` ON `AuditLogs` (`DateAction`);

CREATE INDEX `IX_AuditLog_IdSociete` ON `AuditLogs` (`IdSociete`);

CREATE INDEX `IX_AuditLog_Table_Record` ON `AuditLogs` (`TableName`, `RecordId`);

CREATE INDEX `IX_AuditLog_UserId` ON `AuditLogs` (`UserId`);

CREATE INDEX `IX_CategorieClient_NomCategorie_IdSociete` ON `CategorieClients` (`NomCategorie`, `IdSociete`);

CREATE INDEX `IX_CategorieClients_IdSociete` ON `CategorieClients` (`IdSociete`);

CREATE INDEX `IX_CategorieClients_SocieteIdSociete1` ON `CategorieClients` (`SocieteIdSociete1`);

CREATE UNIQUE INDEX `IX_Client_NumeroCompteur_Unique` ON `Clients` (`numero_compteur`);

CREATE INDEX `IX_Clients_IdCategorieClient` ON `Clients` (`IdCategorieClient`);

CREATE INDEX `IX_Facture_Mois_Annee_Categorie` ON `Factures` (`MoisEmission`, `AnneesEmission`, `IdCategorie`);

CREATE UNIQUE INDEX `IX_Facture_NumeroFacture_Unique` ON `Factures` (`numero_facture`);

CREATE INDEX `IX_Factures_IdCategorie` ON `Factures` (`IdCategorie`);

CREATE INDEX `IX_Notifications_IdAgent` ON `Notifications` (`IdAgent`);

CREATE INDEX `IX_Notifications_IdDestinataire` ON `Notifications` (`IdDestinataire`);

CREATE INDEX `IX_Notifications_IdExpediteur` ON `Notifications` (`IdExpediteur`);

CREATE INDEX `IX_Notifications_IdSociete` ON `Notifications` (`IdSociete`);

CREATE INDEX `IX_PasswordResetTokens_IdUtilisateur` ON `PasswordResetTokens` (`IdUtilisateur`);

CREATE UNIQUE INDEX `IX_PasswordResetTokens_Token` ON `PasswordResetTokens` (`Token`);

CREATE INDEX `IX_RefreshTokens_IdUtilisateur` ON `RefreshTokens` (`IdUtilisateur`);

CREATE INDEX `IX_RolePermissions_IdPermission` ON `RolePermissions` (`IdPermission`);

CREATE INDEX `IX_RolePermissions_IdRole` ON `RolePermissions` (`IdRole`);

CREATE INDEX `IX_RolePermissions_PermissionIdPermission` ON `RolePermissions` (`PermissionIdPermission`);

CREATE INDEX `IX_RolePermissions_RoleIdRole` ON `RolePermissions` (`RoleIdRole`);

CREATE UNIQUE INDEX `IX_Roles_Nom` ON `Roles` (`Nom`);

CREATE INDEX `IX_SmsLogs_UtilisateurIdUtilisateur` ON `SmsLogs` (`UtilisateurIdUtilisateur`);

CREATE INDEX `IX_UserDevices_IdUtilisateur` ON `UserDevices` (`IdUtilisateur`);

CREATE INDEX `IX_UserPermissions_AttribueParIdUtilisateur` ON `UserPermissions` (`AttribueParIdUtilisateur`);

CREATE INDEX `IX_UserPermissions_IdPermission` ON `UserPermissions` (`IdPermission`);

CREATE INDEX `IX_UserPermissions_IdUtilisateur` ON `UserPermissions` (`IdUtilisateur`);

CREATE INDEX `IX_UserRole_IdRole` ON `UserRoles` (`IdRole`);

CREATE INDEX `IX_UserRole_IdUtilisateur` ON `UserRoles` (`IdUtilisateur`);

CREATE UNIQUE INDEX `IX_UserRole_Utilisateur_Role_Unique` ON `UserRoles` (`IdUtilisateur`, `IdRole`);

CREATE INDEX `IX_UserRole_Utilisateur_Statut` ON `UserRoles` (`IdUtilisateur`, `Statut`);

CREATE UNIQUE INDEX `IX_Utilisateurs_Email_Unique` ON `Utilisateurs` (`Email`);

CREATE INDEX `IX_Utilisateurs_IdAgent` ON `Utilisateurs` (`IdAgent`);

CREATE INDEX `IX_Utilisateurs_IdRole` ON `Utilisateurs` (`IdRole`);

CREATE INDEX `IX_Utilisateurs_IdSociete` ON `Utilisateurs` (`IdSociete`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251205111949_InitialCreate', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Societes` DROP COLUMN `Latitude`;

ALTER TABLE `Societes` DROP COLUMN `Longitute`;

ALTER TABLE `Societes` DROP COLUMN `ProvinceEducationnel`;

ALTER TABLE `Societes` RENAME COLUMN `Slogan` TO `Devise`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251205122833_UpdateSocieteModel', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Societes` DROP COLUMN `AcceptNotification`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251205125325_RemoveAcceptNotificationFromSociete', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Utilisateurs` ADD `NomComplet` varchar(200) CHARACTER SET utf8mb4 NULL;


                UPDATE Utilisateurs 
                SET NomComplet = TRIM(CONCAT_WS(' ', 
                    COALESCE(PrenomUtilisateur, ''), 
                    COALESCE(NomUtilisateur, ''), 
                    COALESCE(PostNomUtilisateur, '')
                ))
                WHERE NomComplet IS NULL;
            

ALTER TABLE `Utilisateurs` MODIFY COLUMN `NomComplet` varchar(200) CHARACTER SET utf8mb4 NOT NULL DEFAULT '';

ALTER TABLE `Agents` ADD `NomComplet` varchar(200) CHARACTER SET utf8mb4 NULL;


                UPDATE Agents 
                SET NomComplet = TRIM(CONCAT_WS(' ', 
                    COALESCE(Prenom, ''), 
                    COALESCE(Nom, ''), 
                    COALESCE(Postnom, '')
                ))
                WHERE NomComplet IS NULL;
            

ALTER TABLE `Utilisateurs` DROP COLUMN `NomUtilisateur`;

ALTER TABLE `Utilisateurs` DROP COLUMN `PostNomUtilisateur`;

ALTER TABLE `Utilisateurs` DROP COLUMN `PrenomUtilisateur`;

ALTER TABLE `Agents` DROP COLUMN `Nom`;

ALTER TABLE `Agents` DROP COLUMN `Postnom`;

ALTER TABLE `Agents` DROP COLUMN `Prenom`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251205131535_ReplaceNomPostnomPrenomWithNomComplet', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `RolePermissions` DROP FOREIGN KEY `FK_RolePermissions_Permissions_PermissionIdPermission`;

ALTER TABLE `RolePermissions` DROP FOREIGN KEY `FK_RolePermissions_Roles_RoleIdRole`;

ALTER TABLE `RolePermissions` DROP INDEX `IX_RolePermissions_PermissionIdPermission`;

ALTER TABLE `RolePermissions` DROP INDEX `IX_RolePermissions_RoleIdRole`;

ALTER TABLE `RolePermissions` DROP COLUMN `PermissionIdPermission`;

ALTER TABLE `RolePermissions` DROP COLUMN `RoleIdRole`;

ALTER TABLE `Agents` DROP COLUMN `Avenue`;

ALTER TABLE `Agents` DROP COLUMN `Commune`;

ALTER TABLE `Agents` DROP COLUMN `Numero`;

ALTER TABLE `Agents` DROP COLUMN `Province`;

ALTER TABLE `Agents` DROP COLUMN `Quartier`;

ALTER TABLE `Agents` DROP COLUMN `Ville`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207152331_RemoveAgentAddressFields', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Utilisateurs` DROP COLUMN `Avenue`;

ALTER TABLE `Utilisateurs` DROP COLUMN `Commune`;

ALTER TABLE `Utilisateurs` DROP COLUMN `Numero`;

ALTER TABLE `Utilisateurs` DROP COLUMN `Province`;

ALTER TABLE `Utilisateurs` DROP COLUMN `Quartier`;

ALTER TABLE `Utilisateurs` DROP COLUMN `Ville`;

ALTER TABLE `Utilisateurs` ADD `AdresseResidence` varchar(500) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207153222_RemoveUtilisateurAddressFields', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Societes` DROP COLUMN `Avenue`;

ALTER TABLE `Societes` DROP COLUMN `Commune`;

ALTER TABLE `Societes` DROP COLUMN `Numero`;

ALTER TABLE `Societes` DROP COLUMN `Province`;

ALTER TABLE `Societes` DROP COLUMN `Quartier`;

ALTER TABLE `Societes` DROP COLUMN `Ville`;

ALTER TABLE `Societes` ADD `AdresseResidence` varchar(500) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207154002_RemoveSocieteAddressFields', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` DROP COLUMN `Commune`;

ALTER TABLE `Clients` DROP COLUMN `Quartier`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207172631_RemoveCommuneQuartierFromClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` ADD `EmailClient` varchar(256) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Clients` ADD `GenreClient` varchar(10) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207173049_AddEmailClientAndGenreClientToClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Utilisateurs` ADD `IdClient` int NULL;

CREATE INDEX `IX_Utilisateurs_IdClient` ON `Utilisateurs` (`IdClient`);

ALTER TABLE `Utilisateurs` ADD CONSTRAINT `FK_Utilisateurs_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207173759_AddIdClientToUtilisateur', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `Paiements` (
    `IdPaiement` int NOT NULL AUTO_INCREMENT,
    `IdFacture` int NOT NULL,
    `IdClient` int NULL,
    `Montant` decimal(18,2) NOT NULL,
    `DatePaiement` datetime(6) NOT NULL,
    `MethodePaiement` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ReferenceTransaction` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Commentaire` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdUtilisateurEnregistrement` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    `FactureIdFacture` int NULL,
    CONSTRAINT `PK_Paiements` PRIMARY KEY (`IdPaiement`),
    CONSTRAINT `FK_Paiements_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE SET NULL,
    CONSTRAINT `FK_Paiements_Factures_FactureIdFacture` FOREIGN KEY (`FactureIdFacture`) REFERENCES `Factures` (`IdFacture`),
    CONSTRAINT `FK_Paiements_Factures_IdFacture` FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement` FOREIGN KEY (`IdUtilisateurEnregistrement`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Paiements_DatePaiement` ON `Paiements` (`DatePaiement`);

CREATE INDEX `IX_Paiements_FactureIdFacture` ON `Paiements` (`FactureIdFacture`);

CREATE INDEX `IX_Paiements_IdClient` ON `Paiements` (`IdClient`);

CREATE INDEX `IX_Paiements_IdFacture` ON `Paiements` (`IdFacture`);

CREATE INDEX `IX_Paiements_IdUtilisateurEnregistrement` ON `Paiements` (`IdUtilisateurEnregistrement`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207184928_AddPaiementTable', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Factures` ADD `MontantTotal` decimal(18,2) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251207192116_AddMontantTotalToFacture', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `CategorieClients` DROP FOREIGN KEY `FK_CategorieClients_Societes_SocieteIdSociete1`;

ALTER TABLE `CategorieClients` DROP INDEX `IX_CategorieClients_SocieteIdSociete1`;

ALTER TABLE `CategorieClients` DROP COLUMN `SocieteIdSociete1`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251208111454_FixCategorieClientSocieteRelationship', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Factures` DROP COLUMN `MontantTotal`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251208161947_RemoveMontantTotalFromFacture', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Factures` RENAME COLUMN `MontantPaye` TO `Montant`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251208162747_RenameMontantPayeToMontantInFacture', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Paiements` RENAME COLUMN `Montant` TO `MontantPaye`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251208170309_RenameMontantToMontantPayeInPaiement', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Paiements` ADD `MontantAPaye` decimal(18,2) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251208170516_AddMontantAPayeToPaiement', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Paiements` ADD `ResteAPaye` decimal(18,2) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251208170749_AddResteAPayeToPaiement', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Factures` ADD `DateDiffusion` datetime(6) NULL;

ALTER TABLE `Factures` ADD `EstDiffusee` tinyint(1) NOT NULL DEFAULT FALSE;

CREATE TABLE `DiffusionStatistiques` (
    `IdDiffusionStatistique` int NOT NULL AUTO_INCREMENT,
    `IdFacture` int NOT NULL,
    `IdCategorie` int NOT NULL,
    `TotalClients` int NOT NULL,
    `ClientsNotifies` int NOT NULL,
    `ClientsEchecs` int NOT NULL,
    `StatistiquesCanaux` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `DateDebut` datetime(6) NOT NULL,
    `DateFin` datetime(6) NULL,
    `DureeSecondes` double NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdUtilisateurLanceur` int NULL,
    CONSTRAINT `PK_DiffusionStatistiques` PRIMARY KEY (`IdDiffusionStatistique`),
    CONSTRAINT `FK_DiffusionStatistiques_CategorieClients_IdCategorie` FOREIGN KEY (`IdCategorie`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE CASCADE,
    CONSTRAINT `FK_DiffusionStatistiques_Factures_IdFacture` FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `NotificationPreferences` (
    `IdNotificationPreference` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `AllowPush` tinyint(1) NOT NULL,
    `AllowInApp` tinyint(1) NOT NULL,
    `AllowSms` tinyint(1) NOT NULL,
    `AllowEmail` tinyint(1) NOT NULL,
    `OptOutGlobal` tinyint(1) NOT NULL,
    `OptOutFactures` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NOT NULL,
    CONSTRAINT `PK_NotificationPreferences` PRIMARY KEY (`IdNotificationPreference`),
    CONSTRAINT `FK_NotificationPreferences_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_DiffusionStatistiques_IdCategorie` ON `DiffusionStatistiques` (`IdCategorie`);

CREATE INDEX `IX_DiffusionStatistiques_IdFacture` ON `DiffusionStatistiques` (`IdFacture`);

CREATE INDEX `IX_NotificationPreferences_IdUtilisateur` ON `NotificationPreferences` (`IdUtilisateur`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251209040333_AddEstDiffuseeToFacture', '6.0.25');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251209040344_AddNotificationPreferencesAndDiffusionStats', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `SmsLogs` MODIFY COLUMN `NumeroExpediteur` varchar(50) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251209062640_IncreaseNumeroExpediteurLength', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Paiements` DROP FOREIGN KEY `FK_Paiements_Factures_FactureIdFacture`;

ALTER TABLE `Paiements` DROP INDEX `IX_Paiements_FactureIdFacture`;

ALTER TABLE `Paiements` DROP COLUMN `FactureIdFacture`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251209094905_FixPaiementFactureFk', '6.0.25');

COMMIT;

START TRANSACTION;


                SET @exists := (SELECT COUNT(*) 
                                FROM INFORMATION_SCHEMA.COLUMNS 
                                WHERE TABLE_SCHEMA = DATABASE() 
                                  AND TABLE_NAME = 'Clients' 
                                  AND COLUMN_NAME = 'IsActif');
                SET @sql := IF(@exists = 0, 
                               'ALTER TABLE `Clients` ADD COLUMN `IsActif` TINYINT(1) NOT NULL DEFAULT 1;', 
                               'SELECT 0;');
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251211132731_AddIsActifToClient', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `PleinteClients` (
    `IdPliente` int NOT NULL AUTO_INCREMENT,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    CONSTRAINT `PK_PleinteClients` PRIMARY KEY (`IdPliente`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251211135354_AddPleinteClient', '6.0.25');

COMMIT;

START TRANSACTION;


                SET @foreign_key_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Paiements'
                    AND CONSTRAINT_NAME = 'FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement'
                );
                
                SET @sql = IF(@foreign_key_exists > 0,
                    'ALTER TABLE `Paiements` DROP FOREIGN KEY `FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement`',
                    'SELECT ''Foreign key does not exist, skipping drop'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            


                SET @column_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Paiements'
                    AND COLUMN_NAME = 'IdUtilisateurEnregistrement'
                );
                
                SET @sql = IF(@column_exists > 0,
                    'ALTER TABLE `Paiements` CHANGE COLUMN `IdUtilisateurEnregistrement` `IdUtilisateur` INT NULL',
                    'SELECT ''Column does not exist or already renamed, skipping rename'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            


                SET @index_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.STATISTICS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Paiements'
                    AND INDEX_NAME = 'IX_Paiements_IdUtilisateurEnregistrement'
                );
                
                SET @sql = IF(@index_exists > 0,
                    'ALTER TABLE `Paiements` RENAME INDEX `IX_Paiements_IdUtilisateurEnregistrement` TO `IX_Paiements_IdUtilisateur`',
                    'SELECT ''Index does not exist or already renamed, skipping rename'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            


                SET @column_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'PleinteClients'
                    AND COLUMN_NAME = 'NiveauImportance'
                );
                
                SET @sql = IF(@column_exists = 0,
                    'ALTER TABLE `PleinteClients` ADD COLUMN `NiveauImportance` VARCHAR(50) NULL',
                    'SELECT ''Column NiveauImportance already exists, skipping add'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            


                SET @column_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'PleinteClients'
                    AND COLUMN_NAME = 'RisquesPrincipaux'
                );
                
                SET @sql = IF(@column_exists = 0,
                    'ALTER TABLE `PleinteClients` ADD COLUMN `RisquesPrincipaux` VARCHAR(500) NULL',
                    'SELECT ''Column RisquesPrincipaux already exists, skipping add'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            


                SET @column_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'PleinteClients'
                    AND COLUMN_NAME = 'TypePanne'
                );
                
                SET @sql = IF(@column_exists = 0,
                    'ALTER TABLE `PleinteClients` ADD COLUMN `TypePanne` VARCHAR(200) NULL',
                    'SELECT ''Column TypePanne already exists, skipping add'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            


                SET @foreign_key_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Paiements'
                    AND CONSTRAINT_NAME = 'FK_Paiements_Utilisateurs_IdUtilisateur'
                );
                
                SET @sql = IF(@foreign_key_exists = 0,
                    'ALTER TABLE `Paiements` ADD CONSTRAINT `FK_Paiements_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL',
                    'SELECT ''Foreign key already exists, skipping add'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251211152633_AddFieldsToPleinteClient', '6.0.25');

COMMIT;

START TRANSACTION;


                SET @column_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Clients'
                    AND COLUMN_NAME = 'Usage'
                );
                
                SET @sql = IF(@column_exists = 0,
                    'ALTER TABLE `Clients` ADD COLUMN `Usage` VARCHAR(200) NULL',
                    'SELECT ''Column Usage already exists, skipping add'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251211153957_AddUsageToClient', '6.0.25');

COMMIT;

START TRANSACTION;


                SET @column_exists = (
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'Agents'
                    AND COLUMN_NAME = 'Zone'
                );
                
                SET @sql = IF(@column_exists = 0,
                    'ALTER TABLE `Agents` ADD COLUMN `Zone` VARCHAR(200) NULL',
                    'SELECT ''Column Zone already exists, skipping add'' AS message'
                );
                
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251214102714_AddZoneToAgent', '6.0.25');

COMMIT;

START TRANSACTION;

RENAME TABLE `PleinteClients` TO `PanneSignalements`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251214104747_RenamePleinteClientToPanneSignalement', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `PanneSignalements` CHANGE COLUMN `IdPliente` `IdPanneSignalement` INT NOT NULL AUTO_INCREMENT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251214105329_RenameIdPlienteToIdPanneSignalement', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `CommunicationCampaigns` (
    `IdCampagne` int NOT NULL AUTO_INCREMENT,
    `Titre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Contenu` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `TypeCampagne` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IdSociete` int NULL,
    `IdUtilisateurCreateur` int NOT NULL,
    `CriteresCiblage` TEXT CHARACTER SET utf8mb4 NULL,
    `ListeIdClients` TEXT CHARACTER SET utf8mb4 NULL,
    `ActiverPush` tinyint(1) NOT NULL,
    `ActiverSms` tinyint(1) NOT NULL,
    `ActiverEmail` tinyint(1) NOT NULL,
    `ActiverInApp` tinyint(1) NOT NULL,
    `DateEnvoi` datetime(6) NULL,
    `EstProgrammee` tinyint(1) NOT NULL,
    `EstEnCours` tinyint(1) NOT NULL,
    `EstTerminee` tinyint(1) NOT NULL,
    `NombreDestinataires` int NOT NULL,
    `NombreEnvoyes` int NOT NULL,
    `NombreSucces` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateDerniereModification` datetime(6) NOT NULL,
    `DateEnvoiEffectif` datetime(6) NULL,
    CONSTRAINT `PK_CommunicationCampaigns` PRIMARY KEY (`IdCampagne`),
    CONSTRAINT `FK_CommunicationCampaigns_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE SET NULL,
    CONSTRAINT `FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur` FOREIGN KEY (`IdUtilisateurCreateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_CommunicationCampaigns_IdSociete` ON `CommunicationCampaigns` (`IdSociete`);

CREATE INDEX `IX_CommunicationCampaigns_IdUtilisateurCreateur` ON `CommunicationCampaigns` (`IdUtilisateurCreateur`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251214114757_AddCommunicationCampaign', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `PlainteClients` (
    `IdPlainte` int NOT NULL AUTO_INCREMENT,
    `IdClient` int NOT NULL,
    `IdPanneSignalement` int NULL,
    `Titre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `TypePanne` varchar(200) CHARACTER SET utf8mb4 NULL,
    `NiveauImportance` varchar(50) CHARACTER SET utf8mb4 NULL,
    `RisquesPrincipaux` varchar(500) CHARACTER SET utf8mb4 NULL,
    `StatutPlainte` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Priorite` varchar(50) CHARACTER SET utf8mb4 NULL,
    `IdAgentAssigné` int NULL,
    `IdUtilisateurCreateur` int NULL,
    `CommentaireResolution` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `DateResolution` datetime(6) NULL,
    `EstUrgente` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateDerniereModification` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlainteClients` PRIMARY KEY (`IdPlainte`),
    CONSTRAINT `FK_PlainteClients_Agents_IdAgentAssigné` FOREIGN KEY (`IdAgentAssigné`) REFERENCES `Agents` (`IdAgent`) ON DELETE SET NULL,
    CONSTRAINT `FK_PlainteClients_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlainteClients_PanneSignalements_IdPanneSignalement` FOREIGN KEY (`IdPanneSignalement`) REFERENCES `PanneSignalements` (`IdPanneSignalement`) ON DELETE SET NULL,
    CONSTRAINT `FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur` FOREIGN KEY (`IdUtilisateurCreateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_PlainteClients_IdAgentAssigné` ON `PlainteClients` (`IdAgentAssigné`);

CREATE INDEX `IX_PlainteClients_IdClient` ON `PlainteClients` (`IdClient`);

CREATE INDEX `IX_PlainteClients_IdPanneSignalement` ON `PlainteClients` (`IdPanneSignalement`);

CREATE INDEX `IX_PlainteClients_IdUtilisateurCreateur` ON `PlainteClients` (`IdUtilisateurCreateur`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251214133925_AddPlainteClient', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `ClientCategorieClients` (
    `IdClient` int NOT NULL,
    `IdCategorie` int NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClientCategorieClients` PRIMARY KEY (`IdClient`, `IdCategorie`),
    CONSTRAINT `FK_ClientCategorieClients_CategorieClients_IdCategorie` FOREIGN KEY (`IdCategorie`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE CASCADE,
    CONSTRAINT `FK_ClientCategorieClients_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ClientCategorieClient_IdCategorie` ON `ClientCategorieClients` (`IdCategorie`);

CREATE INDEX `IX_ClientCategorieClient_IdClient` ON `ClientCategorieClients` (`IdClient`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251220123707_AddClientCategorieClientManyToMany', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` DROP COLUMN `Usage`;

ALTER TABLE `CategorieClients` ADD `Usage` varchar(200) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251220132709_MoveUsageFromClientToCategorieClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` ADD `CodeCons` varchar(100) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251220141623_AddCodeConsToClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` DROP FOREIGN KEY `FK_Clients_CategorieClients_IdCategorieClient`;

ALTER TABLE `Factures` DROP FOREIGN KEY `FK_Factures_CategorieClients_IdCategorie`;

DROP TABLE `ClientCategorieClients`;

ALTER TABLE `Factures` DROP INDEX `IX_Facture_Mois_Annee_Categorie`;

ALTER TABLE `Factures` DROP INDEX `IX_Factures_IdCategorie`;

ALTER TABLE `Clients` DROP INDEX `IX_Clients_IdCategorieClient`;

ALTER TABLE `Factures` DROP COLUMN `IdCategorie`;

ALTER TABLE `Clients` DROP COLUMN `IdCategorieClient`;

ALTER TABLE `CategorieClients` DROP COLUMN `Usage`;

ALTER TABLE `Factures` ADD `IdUsage` int NOT NULL DEFAULT 0;

CREATE TABLE `Usages` (
    `IdUsage` int NOT NULL AUTO_INCREMENT,
    `Libelle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IdCategorieClient` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Usages` PRIMARY KEY (`IdUsage`),
    CONSTRAINT `FK_Usages_CategorieClients_IdCategorieClient` FOREIGN KEY (`IdCategorieClient`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClientUsages` (
    `IdClientUsage` int NOT NULL AUTO_INCREMENT,
    `IdClient` int NOT NULL,
    `IdUsage` int NOT NULL,
    `nombreBatiment` int NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    CONSTRAINT `PK_ClientUsages` PRIMARY KEY (`IdClientUsage`),
    CONSTRAINT `FK_ClientUsages_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE CASCADE,
    CONSTRAINT `FK_ClientUsages_Usages_IdUsage` FOREIGN KEY (`IdUsage`) REFERENCES `Usages` (`IdUsage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Facture_Mois_Annee_Usage` ON `Factures` (`MoisEmission`, `AnneesEmission`, `IdUsage`);

CREATE INDEX `IX_Factures_IdUsage` ON `Factures` (`IdUsage`);

CREATE UNIQUE INDEX `IX_ClientUsage_Client_Usage_Unique` ON `ClientUsages` (`IdClient`, `IdUsage`);

CREATE INDEX `IX_ClientUsage_IdClient` ON `ClientUsages` (`IdClient`);

CREATE INDEX `IX_ClientUsage_IdUsage` ON `ClientUsages` (`IdUsage`);

CREATE INDEX `IX_Usage_Libelle_IdCategorieClient` ON `Usages` (`Libelle`, `IdCategorieClient`);

CREATE INDEX `IX_Usages_IdCategorieClient` ON `Usages` (`IdCategorieClient`);

ALTER TABLE `Factures` ADD CONSTRAINT `FK_Factures_Usages_IdUsage` FOREIGN KEY (`IdUsage`) REFERENCES `Usages` (`IdUsage`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251222054322_RefactorToUsageModel', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `Cabines` (
    `IdCabine` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Adresse` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Cabines` PRIMARY KEY (`IdCabine`),
    CONSTRAINT `FK_Cabines_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Cabine_IdSociete` ON `Cabines` (`IdSociete`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251226073705_AddCabineModel', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `Axes` (
    `IdAxe` int NOT NULL AUTO_INCREMENT,
    `NomAxe` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IdCabine` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Axes` PRIMARY KEY (`IdAxe`),
    CONSTRAINT `FK_Axes_Cabines_IdCabine` FOREIGN KEY (`IdCabine`) REFERENCES `Cabines` (`IdCabine`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Axe_IdCabine` ON `Axes` (`IdCabine`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20251226074022_AddAxeModel', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` ADD `IdAxe` int NULL;

CREATE INDEX `IX_Client_IdAxe` ON `Clients` (`IdAxe`);

ALTER TABLE `Clients` ADD CONSTRAINT `FK_Clients_Axes_IdAxe` FOREIGN KEY (`IdAxe`) REFERENCES `Axes` (`IdAxe`) ON DELETE SET NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260103152533_AddIdAxeToClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` DROP COLUMN `Zone`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260103153808_RemoveZoneFromClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Cabines` ADD `CodeCabine` varchar(50) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Axes` ADD `CodeAxe` varchar(50) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260103163705_AddCodeCabineCodeAxe', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `CategorieClients` RENAME COLUMN `Actif` TO `Statut`;

ALTER TABLE `Cabines` MODIFY COLUMN `DateCreation` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6);

ALTER TABLE `Axes` MODIFY COLUMN `DateCreation` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260103173953_RenameActifToStatutInCategorieClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Usages` ADD `Statut` tinyint(1) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260103202244_AddStatutToUsage', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Clients` DROP INDEX `IX_Client_NumeroCompteur_Unique`;

ALTER TABLE `Clients` DROP COLUMN `numero_compteur`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260103214502_RemoveNumeroCompteurFromClient', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ClientUsages` ADD `Statut` tinyint(1) NOT NULL DEFAULT TRUE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260104105332_AddStatutToClientUsage', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Cabines` ADD `Description` varchar(1000) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260104122724_AddDescriptionToCabine', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `ClientFactures` (
    `IdClientFacture` int NOT NULL AUTO_INCREMENT,
    `IdFacture` int NULL,
    `IdClient` int NOT NULL,
    `Montant` decimal(18,2) NULL,
    `nombreBatiment` int NULL,
    `MontantPaye` decimal(18,2) NULL DEFAULT 0.0,
    `MontantDu` decimal(18,2) NULL,
    `Mois` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Annees` int NULL,
    `DateEmission` datetime(6) NULL,
    `EstArrierePreExistant` tinyint(1) NOT NULL DEFAULT FALSE,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_ClientFactures` PRIMARY KEY (`IdClientFacture`),
    CONSTRAINT `FK_ClientFactures_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ClientFactures_Factures_IdFacture` FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_ClientFacture_Client_Mois_Annees` ON `ClientFactures` (`IdClient`, `Mois`, `Annees`);

CREATE INDEX `IX_ClientFacture_DateEmission` ON `ClientFactures` (`DateEmission`);

CREATE INDEX `IX_ClientFacture_IdClient` ON `ClientFactures` (`IdClient`);

CREATE INDEX `IX_ClientFacture_IdFacture` ON `ClientFactures` (`IdFacture`);

CREATE INDEX `IX_ClientFacture_MontantDu` ON `ClientFactures` (`MontantDu`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260104145233_AddClientFacture', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `PlainteClients` ADD `Statut` tinyint(1) NOT NULL DEFAULT TRUE;

ALTER TABLE `Paiements` MODIFY COLUMN `IdFacture` int NULL;

ALTER TABLE `Paiements` ADD `EstPaiementArriere` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Paiements` ADD `IdClientFacture` int NULL;

ALTER TABLE `Paiements` ADD `IsDeleted` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `CommunicationCampaigns` ADD `Statut` tinyint(1) NOT NULL DEFAULT FALSE;

ALTER TABLE `Cabines` ADD `Statut` tinyint(1) NOT NULL DEFAULT TRUE;

ALTER TABLE `Axes` ADD `Statut` tinyint(1) NOT NULL DEFAULT TRUE;

CREATE TABLE `ArriereesCrashed` (
    `IdArriereeCrashed` int NOT NULL AUTO_INCREMENT,
    `NumeroLigne` int NOT NULL,
    `CodeCons` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Montant` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Mois` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Annees` varchar(10) CHARACTER SET utf8mb4 NULL,
    `IdClient` int NULL,
    `DonneesBrutesJson` TEXT CHARACTER SET utf8mb4 NULL,
    `MessageErreur` TEXT CHARACTER SET utf8mb4 NOT NULL,
    `TypeErreur` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ErreursJson` TEXT CHARACTER SET utf8mb4 NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdClientFactureCree` int NULL,
    `DateCreation` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `DateCorrection` datetime(6) NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_ArriereesCrashed` PRIMARY KEY (`IdArriereeCrashed`),
    CONSTRAINT `FK_ArriereesCrashed_ClientFactures_IdClientFactureCree` FOREIGN KEY (`IdClientFactureCree`) REFERENCES `ClientFactures` (`IdClientFacture`) ON DELETE SET NULL,
    CONSTRAINT `FK_ArriereesCrashed_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `ClientsCrashed` (
    `IdClientCrashed` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `NumeroLigne` int NOT NULL,
    `NomClient` varchar(200) CHARACTER SET utf8mb4 NULL,
    `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Telephone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `EmailClient` varchar(256) CHARACTER SET utf8mb4 NULL,
    `GenreClient` varchar(10) CHARACTER SET utf8mb4 NULL,
    `CodeCons` varchar(100) CHARACTER SET utf8mb4 NULL,
    `LibelleUsage` TEXT CHARACTER SET utf8mb4 NULL,
    `DonneesBrutesJson` TEXT CHARACTER SET utf8mb4 NULL,
    `MessageErreur` TEXT CHARACTER SET utf8mb4 NOT NULL,
    `TypeErreur` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ErreursJson` TEXT CHARACTER SET utf8mb4 NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdClientCree` int NULL,
    `DateCreation` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `DateCorrection` datetime(6) NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_ClientsCrashed` PRIMARY KEY (`IdClientCrashed`),
    CONSTRAINT `FK_ClientsCrashed_Clients_IdClientCree` FOREIGN KEY (`IdClientCree`) REFERENCES `Clients` (`IdClient`) ON DELETE SET NULL,
    CONSTRAINT `FK_ClientsCrashed_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Paiements_IdClientFacture` ON `Paiements` (`IdClientFacture`);

CREATE UNIQUE INDEX `IX_Client_CodeCons_Unique` ON `Clients` (`CodeCons`);

CREATE INDEX `IX_ArriereeCrashed_CodeCons` ON `ArriereesCrashed` (`CodeCons`);

CREATE INDEX `IX_ArriereeCrashed_DateCreation` ON `ArriereesCrashed` (`DateCreation`);

CREATE INDEX `IX_ArriereeCrashed_Statut` ON `ArriereesCrashed` (`Statut`);

CREATE INDEX `IX_ArriereesCrashed_IdClient` ON `ArriereesCrashed` (`IdClient`);

CREATE INDEX `IX_ArriereesCrashed_IdClientFactureCree` ON `ArriereesCrashed` (`IdClientFactureCree`);

CREATE INDEX `IX_ClientCrashed_DateCreation` ON `ClientsCrashed` (`DateCreation`);

CREATE INDEX `IX_ClientCrashed_IdSociete` ON `ClientsCrashed` (`IdSociete`);

CREATE INDEX `IX_ClientCrashed_Statut` ON `ClientsCrashed` (`Statut`);

CREATE INDEX `IX_ClientsCrashed_IdClientCree` ON `ClientsCrashed` (`IdClientCree`);

ALTER TABLE `Paiements` ADD CONSTRAINT `FK_Paiements_ClientFactures_IdClientFacture` FOREIGN KEY (`IdClientFacture`) REFERENCES `ClientFactures` (`IdClientFacture`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260213080207_AddPaiementArriereSupport', '6.0.25');

COMMIT;

START TRANSACTION;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260321165615_AddSyncFieldsFinal', '6.0.25');

COMMIT;

