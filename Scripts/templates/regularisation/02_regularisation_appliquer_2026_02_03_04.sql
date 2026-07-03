-- ============================================================================
-- SCRIPT 2/3 — APPLIQUER la régularisation (MODIFIE les données)
-- ============================================================================
-- Fichier : 02_regularisation_appliquer_2026_02_03_04.sql
-- Objectif : Montant = 0, MontantDu = 0 sur les ClientFactures éligibles.
-- Période  : Février, Mars, Avril 2026 — __NB_CODECONS__ CodeCons Excel.
--
-- PRÉREQUIS OBLIGATOIRES :
--   [ ] Script 01 exécuté — verdict SECTION 3c = OK
--   [ ] Backup : mysqldump ClientFactures > backup_cf_regularisation_YYYYMMDD.sql
--   [ ] Validation métier des lignes bloquées (script 01 section 2b)
--
-- Politique paiement :
--   - UPDATE uniquement si MontantPaye = 0 et sans Paiement lié
--   - MontantPaye > 0 ou paiement lié → NON modifié
--
__SUSPECT_HEADER__
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SETUP — Paramètres et table staging (autonome par script)
-- ----------------------------------------------------------------------------

SET @annee_cible   := 2026;
SET @mois_fev_pad  := '02';
SET @mois_fev_sp   := '2';
SET @mois_mar_pad  := '03';
SET @mois_mar_sp   := '3';
SET @mois_avr_pad  := '04';
SET @mois_avr_sp   := '4';

DROP TEMPORARY TABLE IF EXISTS tmp_codecons_regularisation;
CREATE TEMPORARY TABLE tmp_codecons_regularisation (
    CodeCons            VARCHAR(100) NOT NULL PRIMARY KEY,
    MontantCibleAvril   DECIMAL(18, 2) NOT NULL DEFAULT 0,
    MontantCibleMars    DECIMAL(18, 2) NOT NULL DEFAULT 0,
    MontantCibleFevrier DECIMAL(18, 2) NOT NULL DEFAULT 0,
    EstFormatSuspect    TINYINT NOT NULL DEFAULT 0
);

SOURCE Scripts/data_codecons_regularisation_2026_02_03_04.sql;

SELECT
    'SETUP — Paramètres' AS Etape,
    @annee_cible AS annee_cible,
    COUNT(*) AS nb_codecons_charges,
    SUM(EstFormatSuspect) AS nb_format_suspect,
    DATABASE() AS base_courante,
    NOW() AS execute_le
FROM tmp_codecons_regularisation;


-- ----------------------------------------------------------------------------
-- APPLICATION — transaction
-- ----------------------------------------------------------------------------
-- 1er passage : laisser ROLLBACK (dry-run)
-- 2e passage  : commenter ROLLBACK, décommenter COMMIT

START TRANSACTION;

SELECT COUNT(*) INTO @nb_eligibles_tx

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  )
  AND (COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0);

SELECT @nb_eligibles_tx AS nb_lignes_a_modifier;

UPDATE ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
SET
    cf.Montant = 0,
    cf.MontantDu = 0,
    cf.DateModification = NOW()
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  )
  AND (COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0);

SELECT ROW_COUNT() AS lignes_modifiees;

-- >>> DRY-RUN (1er passage) :
ROLLBACK;

-- >>> PRODUCTION (2e passage, après validation) :
-- COMMIT;

SELECT 'Fin script 02 — si dry-run : aucune donnée persistée (ROLLBACK)' AS Etape;
