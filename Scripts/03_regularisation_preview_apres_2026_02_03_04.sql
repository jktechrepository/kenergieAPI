-- ============================================================================
-- SCRIPT 3/3 — PREVIEW APRÈS régularisation (LECTURE SEULE)
-- ============================================================================
-- Fichier : 03_regularisation_preview_apres_2026_02_03_04.sql
-- Objectif : contrôler l'état des ClientFactures APRÈS le script 02.
-- Période  : Février, Mars, Avril 2026 — 481 CodeCons Excel.
-- Action   : aucune modification de données.
--
-- Critère de succès :
--   - nb_restantes_non_zero_eligibles = 0
--   - somme_montant_apres = 0 et somme_montant_du_apres = 0 (lignes éligibles)
--
-- Codes format suspect dans l'Excel : F/F10020, G2/0142
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
-- SECTION 1 — Synthèse APRÈS régularisation
-- ----------------------------------------------------------------------------

SELECT 'SECTION 1a — Synthèse lignes éligibles APRÈS' AS Etape;

SELECT
    COUNT(*) AS nb_cf_eligibles,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_distincts,
    SUM(COALESCE(cf.Montant, 0)) AS somme_montant_apres,
    SUM(COALESCE(cf.MontantDu, 0)) AS somme_montant_du_apres,
    SUM(CASE WHEN COALESCE(cf.Montant, 0) = 0 AND COALESCE(cf.MontantDu, 0) = 0 THEN 1 ELSE 0 END) AS nb_a_zero,
    SUM(CASE WHEN COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0 THEN 1 ELSE 0 END) AS nb_restantes_non_zero

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT 'SECTION 1b — Détail lignes éligibles APRÈS (toutes)' AS Etape;

SELECT
    cf.IdClientFacture,
    cf.IdClient,
    c.CodeCons,
    c.NomClient,
    cf.Mois,
    cf.Annees,
    cf.Montant,
    cf.MontantPaye,
    cf.MontantDu,
    cf.DateModification,
    CASE
        WHEN COALESCE(cf.Montant, 0) = 0 AND COALESCE(cf.MontantDu, 0) = 0 THEN 'OK — à zéro'
        ELSE 'ANOMALIE — montants non nuls'
    END AS statut_regularisation

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
ORDER BY c.CodeCons, cf.Mois, cf.IdClientFacture;


SELECT 'SECTION 1c — Anomalies : éligibles encore non nulles' AS Etape;

SELECT
    cf.IdClientFacture,
    c.CodeCons,
    c.NomClient,
    cf.Mois,
    cf.Montant,
    cf.MontantDu

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
  AND (COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0)
ORDER BY c.CodeCons, cf.Mois;


SELECT 'SECTION 1d — Lignes bloquées (inchangées, contrôle)' AS Etape;

SELECT
    COUNT(*) AS nb_cf_bloquees,
    SUM(COALESCE(cf.Montant, 0)) AS somme_montant_bloquees

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND (
      COALESCE(cf.MontantPaye, 0) > 0
      OR EXISTS (
          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
      )
  );


-- ----------------------------------------------------------------------------
-- SECTION 2 — Verdict APRÈS
-- ----------------------------------------------------------------------------

SELECT COUNT(*) INTO @v_nb_eligibles_apres

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );

SELECT COUNT(*) INTO @v_nb_non_zero_apres

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

SELECT SUM(COALESCE(cf.Montant, 0)) INTO @v_somme_montant_apres

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );

SELECT SUM(COALESCE(cf.MontantDu, 0)) INTO @v_somme_montant_du_apres

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );

SELECT 'SECTION 2 — Verdict APRÈS' AS Etape;

SELECT
    @v_nb_eligibles_apres AS nb_cf_eligibles,
    @v_nb_non_zero_apres AS nb_restantes_non_zero_eligibles,
    @v_somme_montant_apres AS somme_montant_apres,
    @v_somme_montant_du_apres AS somme_montant_du_apres,
    CASE
        WHEN @v_nb_non_zero_apres > 0
        THEN 'ECHEC — des lignes éligibles ont encore Montant ou MontantDu <> 0'
        WHEN @v_somme_montant_apres <> 0 OR @v_somme_montant_du_apres <> 0
        THEN 'ECHEC — sommes non nulles sur lignes éligibles'
        ELSE 'OK — régularisation conforme (montants à zéro)'
    END AS verdict;

SELECT 'Fin script 03 — archiver les résultats de contrôle' AS Etape;
