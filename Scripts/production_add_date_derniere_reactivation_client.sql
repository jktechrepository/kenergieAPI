-- =============================================================================
-- Script SQL production : DateDerniereReactivation sur Clients
-- Équivalent EF : 20260815095720_AjoutDateDerniereReactivationClient
-- SGBD : MySQL / MariaDB
--
-- Objectif : enregistrer la dernière réactivation pour l'éligibilité facture.
--
-- IMPORTANT :
-- 1. Faire une sauvegarde avant exécution
-- 2. Script idempotent
--
-- Usage :
--   mysql -u USER -p DBNAME < Scripts/production_add_date_derniere_reactivation_client.sql
-- =============================================================================

START TRANSACTION;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Clients'
      AND COLUMN_NAME = 'DateDerniereReactivation'
);

SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `Clients` ADD COLUMN `DateDerniereReactivation` DATETIME(6) NULL AFTER `IsActif`',
    'SELECT 1');
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

COMMIT;

SELECT 'OK: DateDerniereReactivation' AS Resultat;
