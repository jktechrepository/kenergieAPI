-- ============================================================================
-- SCRIPT 1/3 — PREVIEW AVANT régularisation (LECTURE SEULE)
-- ============================================================================
-- Fichier : 01_regularisation_preview_avant_2026_02_03_04.sql
-- Objectif : afficher l'état des ClientFactures AVANT modification.
-- Période  : Février, Mars, Avril 2026 — 481 CodeCons Excel.
-- Action   : aucune modification de données.
--
-- Workflow :
--   1) Exécuter CE script et archiver les résultats
--   2) Vérifier SECTION 3c verdict = OK
--   3) Passer au script 02 uniquement après validation métier + backup
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
-- SECTION 1 — Résolution CodeCons → Client
-- ----------------------------------------------------------------------------

SELECT 'SECTION 1a — Clients résolus (CodeCons → IdClient)' AS Etape;

SELECT
    t.CodeCons,
    c.IdClient,
    c.NomClient,
    c.IsActif,
    c.Statut AS client_statut,
    t.MontantCibleFevrier,
    t.MontantCibleMars,
    t.MontantCibleAvril
FROM tmp_codecons_regularisation t
INNER JOIN Clients c ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
ORDER BY t.CodeCons;


SELECT 'SECTION 1b — CodeCons Excel ABSENTS en base' AS Etape;

SELECT
    t.CodeCons,
    t.EstFormatSuspect,
    CASE
        WHEN t.EstFormatSuspect = 1 THEN 'Format suspect — vérifier variante (ex. G/G2/0142)'
        ELSE 'Absent'
    END AS note
FROM tmp_codecons_regularisation t
LEFT JOIN Clients c ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE c.IdClient IS NULL
ORDER BY t.CodeCons;


SELECT 'SECTION 1c — CodeCons format suspect (alerte)' AS Etape;

SELECT
    t.CodeCons,
    t.EstFormatSuspect
FROM tmp_codecons_regularisation t
WHERE t.EstFormatSuspect = 1
ORDER BY t.CodeCons;


SELECT COUNT(*) INTO @nb_codecons_excel FROM tmp_codecons_regularisation;

SELECT COUNT(DISTINCT LOWER(TRIM(t.CodeCons))) INTO @nb_codecons_resolus
FROM tmp_codecons_regularisation t
INNER JOIN Clients c ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons));

SELECT COUNT(*) INTO @nb_codecons_absents
FROM tmp_codecons_regularisation t
LEFT JOIN Clients c ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE c.IdClient IS NULL;

SELECT SUM(EstFormatSuspect) INTO @nb_format_suspect FROM tmp_codecons_regularisation;

SELECT 'SECTION 1d — Synthèse résolution' AS Etape;

SELECT
    @nb_codecons_excel AS nb_codecons_excel,
    @nb_codecons_resolus AS nb_codecons_resolus,
    @nb_codecons_absents AS nb_codecons_absents,
    @nb_format_suspect AS nb_format_suspect;


-- ----------------------------------------------------------------------------
-- SECTION 2 — Preview lignes éligibles AVANT
-- ----------------------------------------------------------------------------

SELECT 'SECTION 2a — Synthèse lignes éligibles (MontantPaye=0, sans Paiement)' AS Etape;

SELECT
    COUNT(*) AS nb_cf_eligibles,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_distincts,
    SUM(COALESCE(cf.Montant, 0)) AS somme_montant_avant,
    SUM(COALESCE(cf.MontantDu, 0)) AS somme_montant_du_avant,
    SUM(CASE WHEN cf.Mois IN (@mois_fev_pad, @mois_fev_sp) THEN 1 ELSE 0 END) AS nb_fevrier,
    SUM(CASE WHEN cf.Mois IN (@mois_mar_pad, @mois_mar_sp) THEN 1 ELSE 0 END) AS nb_mars,
    SUM(CASE WHEN cf.Mois IN (@mois_avr_pad, @mois_avr_sp) THEN 1 ELSE 0 END) AS nb_avril

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


SELECT 'SECTION 2b — Lignes BLOQUÉES (paiement ou MontantPaye > 0)' AS Etape;

SELECT
    COUNT(*) AS nb_cf_bloquees,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_bloques,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye

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


SELECT 'SECTION 2b détail — TOP 50 lignes bloquées' AS Etape;

SELECT
    cf.IdClientFacture,
    c.CodeCons,
    c.NomClient,
    cf.Mois,
    cf.Annees,
    cf.Montant,
    cf.MontantPaye,
    cf.MontantDu,
    CASE
        WHEN COALESCE(cf.MontantPaye, 0) > 0 THEN 'MontantPaye > 0'
        WHEN EXISTS (SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture)
            THEN 'Paiement lié'
        ELSE 'Autre'
    END AS raison_blocage

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
  )
ORDER BY c.CodeCons, cf.Mois, cf.IdClientFacture
LIMIT 50;


SELECT 'SECTION 2c — Clients trouvés SANS ClientFacture Fév/Mar/Avr 2026' AS Etape;

SELECT
    c.IdClient,
    t.CodeCons,
    c.NomClient
FROM tmp_codecons_regularisation t
INNER JOIN Clients c ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE NOT EXISTS (
    SELECT 1
    FROM ClientFactures cf
    WHERE cf.IdClient = c.IdClient
      AND cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
      AND cf.Statut = 1
)
ORDER BY t.CodeCons;


SELECT 'SECTION 2d — Détail lignes éligibles AVANT (toutes)' AS Etape;

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
    cf.Statut AS cf_statut

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


-- ----------------------------------------------------------------------------
-- SECTION 3 — Verdict avant modification
-- ----------------------------------------------------------------------------

SELECT 'SECTION 3a — Exclues : MontantPaye > 0' AS Etape;

SELECT
    COUNT(*) AS nb_exclues_montant_paye_positif,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) > 0;


SELECT 'SECTION 3b — Exclues : au moins un Paiement lié' AS Etape;

SELECT
    COUNT(DISTINCT cf.IdClientFacture) AS nb_exclues_avec_paiement

FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
  AND cf.Statut = 1
  AND EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT COUNT(*) INTO @v_nb_absents
FROM tmp_codecons_regularisation t
LEFT JOIN Clients c ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
WHERE c.IdClient IS NULL;

SELECT COUNT(*) INTO @v_nb_doublons
FROM (
    SELECT LOWER(TRIM(c.CodeCons))
    FROM Clients c
    INNER JOIN tmp_codecons_regularisation t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
    GROUP BY LOWER(TRIM(c.CodeCons))
    HAVING COUNT(*) > 1
) d;

SELECT COUNT(*) INTO @v_nb_eligibles

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

SELECT COUNT(*) INTO @v_nb_a_modifier

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

SELECT 'SECTION 3c — Verdict AVANT' AS Etape;

SELECT
    @v_nb_absents AS nb_codecons_absents,
    @v_nb_doublons AS nb_codecons_en_doublon_base,
    @v_nb_eligibles AS nb_cf_eligibles,
    @v_nb_a_modifier AS nb_cf_a_modifier,
    CASE
        WHEN @v_nb_doublons > 0
        THEN 'ARRET — doublon CodeCons en base : trancher avant script 02'
        WHEN @v_nb_eligibles = 0
        THEN 'ARRET — aucune ligne éligible à régulariser'
        ELSE 'OK — script 02 autorisé après backup et validation métier'
    END AS verdict;

SELECT 'Fin script 01 — archiver les résultats avant de lancer le script 02' AS Etape;
