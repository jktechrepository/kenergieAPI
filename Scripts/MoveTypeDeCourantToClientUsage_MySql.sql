-- =============================================================================
-- Migration manuelle : IdTypeDeCourant Clients -> ClientUsages
-- Équivalent EF : 20260514120000_MoveTypeDeCourantToClientUsage
-- SGBD : MySQL / MariaDB (Pomelo)
-- Exécuter une seule fois sur l'environnement cible (sauvegarde recommandée).
--
-- Si DROP FOREIGN KEY échoue (nom de contrainte différent selon l’historique des
-- migrations), lister le nom réel puis remplacer dans la section UP :
--   SELECT CONSTRAINT_NAME
--   FROM information_schema.TABLE_CONSTRAINTS
--   WHERE TABLE_SCHEMA = DATABASE()
--     AND TABLE_NAME = 'Clients'
--     AND CONSTRAINT_TYPE = 'FOREIGN KEY';
-- =============================================================================

-- -----------------------------------------------------------------------------
-- UP : appliquer les changements
-- -----------------------------------------------------------------------------

START TRANSACTION;

-- 1) Nouvelle colonne sur ClientUsages
ALTER TABLE `ClientUsages`
    ADD COLUMN `IdTypeDeCourant` INT NULL;

-- 2) Copier le type de courant du client sur toutes ses lignes ClientUsages
UPDATE `ClientUsages` AS cu
INNER JOIN `Clients` AS c ON c.`IdClient` = cu.`IdClient`
SET cu.`IdTypeDeCourant` = c.`IdTypeDeCourant`
WHERE c.`IdTypeDeCourant` IS NOT NULL;

-- 3) Index (nom aligné sur KenergieDbContext / migration)
CREATE INDEX `IX_ClientUsage_IdTypeDeCourant`
    ON `ClientUsages` (`IdTypeDeCourant`);

-- 4) FK ClientUsages -> TypeDeCourants (RESTRICT = pas de suppression en cascade du type)
ALTER TABLE `ClientUsages`
    ADD CONSTRAINT `FK_ClientUsages_TypeDeCourants_IdTypeDeCourant`
    FOREIGN KEY (`IdTypeDeCourant`)
    REFERENCES `TypeDeCourants` (`IdTypeDeCourant`)
    ON DELETE RESTRICT
    ON UPDATE RESTRICT;

-- 5) Retirer la relation et la colonne côté Clients
ALTER TABLE `Clients`
    DROP FOREIGN KEY `FK_Clients_TypeDeCourants_IdTypeDeCourant`;

DROP INDEX `IX_Clients_IdTypeDeCourant` ON `Clients`;

ALTER TABLE `Clients`
    DROP COLUMN `IdTypeDeCourant`;

-- 6) Historique EF (optionnel : à n’exécuter que si vous gérez __EFMigrationsHistory)
-- INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
-- VALUES ('20260514120000_MoveTypeDeCourantToClientUsage', '6.0.25');

COMMIT;


-- =============================================================================
-- DOWN : annuler (à utiliser seulement en cas de rollback planifié)
-- =============================================================================
/*
START TRANSACTION;

ALTER TABLE `Clients`
    ADD COLUMN `IdTypeDeCourant` INT NULL;

UPDATE `Clients` AS c
INNER JOIN (
    SELECT `IdClient`, MIN(`IdTypeDeCourant`) AS `IdTypeDeCourant`
    FROM `ClientUsages`
    WHERE `IdTypeDeCourant` IS NOT NULL
    GROUP BY `IdClient`
) AS x ON x.`IdClient` = c.`IdClient`
SET c.`IdTypeDeCourant` = x.`IdTypeDeCourant`;

CREATE INDEX `IX_Clients_IdTypeDeCourant`
    ON `Clients` (`IdTypeDeCourant`);

ALTER TABLE `Clients`
    ADD CONSTRAINT `FK_Clients_TypeDeCourants_IdTypeDeCourant`
    FOREIGN KEY (`IdTypeDeCourant`)
    REFERENCES `TypeDeCourants` (`IdTypeDeCourant`);

ALTER TABLE `ClientUsages`
    DROP FOREIGN KEY `FK_ClientUsages_TypeDeCourants_IdTypeDeCourant`;

DROP INDEX `IX_ClientUsage_IdTypeDeCourant` ON `ClientUsages`;

ALTER TABLE `ClientUsages`
    DROP COLUMN `IdTypeDeCourant`;

-- DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260514120000_MoveTypeDeCourantToClientUsage';

COMMIT;
*/
