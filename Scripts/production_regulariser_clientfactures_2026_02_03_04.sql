-- ============================================================================
-- Script PRODUCTION : régularisation ClientFactures — Fév/Mar/Avr 2026
-- ============================================================================
-- Objectif :
--   Remettre Montant = 0 et MontantDu = 0 sur les ClientFactures
--   pour Annees = 2026, Mois Février (02/2), Mars (03/3), Avril (04/4),
--   pour les clients dont le CodeCons figure dans
--   « LISTE DES FACTURES CLIENT A REGULARISER.xlsx » (481 codes uniques).
--
-- Relation :
--   ClientFactures.IdClient → Clients.IdClient
--   Filtre métier           → Clients.CodeCons (matching TRIM + LOWER)
--
-- Politique paiement (alignée scripts suppression) :
--   - UPDATE uniquement si COALESCE(MontantPaye, 0) = 0
--   - UPDATE uniquement si aucun Paiement lié (Paiements.IdClientFacture)
--   - MontantPaye > 0 ou paiement lié → BLOQUÉ (section 2b), pas de mise à 0 forcée
--
-- NE MODIFIE PAS : Factures, Clients, Paiements
-- NE SUPPRIME PAS : lignes ClientFactures
--
-- Codes format suspect dans l'Excel : F/F10020, G2/0142
--
-- ============================================================================
-- PROCÉDURE D'EXÉCUTION (OBLIGATOIRE)
-- ============================================================================
--
-- 1) Backup :
--      mysqldump -u ... -p ... ClientFactures > backup_cf_regularisation_YYYYMMDD.sql
--
-- 2) (Re)générer les données si l'Excel change :
--      python3 Scripts/generate_codecons_regularisation_data.py
--
-- 3) Exécuter SECTIONS 0 à 3 (lecture seule) — valider :
--      - nb CodeCons résolus (section 1a)
--      - CodeCons absents (section 1b) et format suspect (section 1c)
--      - totaux section 2 = lignes réellement régularisables
--      - section 2b : lignes bloquées (paiement) acceptées par le métier
--      - section 2c : clients sans CF Fév/Mar/Avr 2026
--
-- 4) DRY-RUN : SECTION 4 + ROLLBACK;
-- 5) PRODUCTION : SECTION 4 + COMMIT;
--
-- Alternative procédure stockée :
--      CALL sp_regulariser_clientfactures_2026_02_03_04(1);  -- dry-run
--      CALL sp_regulariser_clientfactures_2026_02_03_04(0);  -- réel
--
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SECTION 0 — Paramètres et table staging
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

-- Charger les 481 CodeCons (généré depuis l'Excel) :
SOURCE Scripts/data_codecons_regularisation_2026_02_03_04.sql;

-- MySQL : une table TEMP ne peut pas être référencée 2× dans la même requête (erreur 1137).
SELECT
    'SECTION 0 — Paramètres' AS Etape,
    @annee_cible AS annee_cible,
    COUNT(*) AS nb_codecons_charges,
    SUM(EstFormatSuspect) AS nb_format_suspect,
    DATABASE() AS base_courante,
    NOW() AS execute_le
FROM tmp_codecons_regularisation;


-- ----------------------------------------------------------------------------
-- SECTION 1 — Résolution CodeCons → Client (LECTURE SEULE)
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
-- SECTION 2 — Preview lignes ÉLIGIBLES à la régularisation (LECTURE SEULE)
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


SELECT 'SECTION 2d — Échantillon TOP 50 lignes éligibles (avant UPDATE)' AS Etape;

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
ORDER BY c.CodeCons, cf.Mois, cf.IdClientFacture
LIMIT 50;


-- ----------------------------------------------------------------------------
-- SECTION 3 — Vérification finale (LECTURE SEULE)
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

SELECT 'SECTION 3c — Verdict' AS Etape;

SELECT
    @v_nb_absents AS nb_codecons_absents,
    @v_nb_doublons AS nb_codecons_en_doublon_base,
    @v_nb_eligibles AS nb_cf_eligibles,
    CASE
        WHEN @v_nb_doublons > 0
        THEN 'ARRET — doublon CodeCons en base : trancher avant UPDATE'
        WHEN @v_nb_eligibles = 0
        THEN 'ARRET — aucune ligne éligible à régulariser'
        ELSE 'OK — SECTION 4 autorisée après validation métier (dry-run ROLLBACK d''abord)'
    END AS verdict;


-- ----------------------------------------------------------------------------
-- SECTION 4 — Régularisation transactionnelle (MANUELLE)
-- ----------------------------------------------------------------------------
-- Prérequis : SECTION 3c verdict = OK
-- Met Montant = 0, MontantDu = 0 (MontantPaye inchangé — déjà 0 sur lignes éligibles)

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

SELECT @nb_eligibles_tx AS nb_lignes_a_modifier_dans_transaction;

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

SELECT COUNT(*) AS nb_restantes_non_zero_eligibles

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

-- >>> DRY-RUN (1er passage) :
ROLLBACK;

-- >>> PRODUCTION (2e passage, après validation) :
-- COMMIT;


-- ----------------------------------------------------------------------------
-- SECTION 4b — Procédure stockée (optionnelle)
-- ----------------------------------------------------------------------------

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_regulariser_clientfactures_2026_02_03_04$$

CREATE PROCEDURE sp_regulariser_clientfactures_2026_02_03_04(IN p_dry_run TINYINT)
BEGIN
    DECLARE v_nb_doublons INT DEFAULT 0;
    DECLARE v_nb_avant INT DEFAULT 0;
    DECLARE v_lignes_modifiees INT DEFAULT 0;

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

    INSERT INTO tmp_codecons_regularisation
        (CodeCons, MontantCibleAvril, MontantCibleMars, MontantCibleFevrier, EstFormatSuspect) VALUES
    ('A/A1/0214', 0, 0, 0, 0),
    ('A/A2/0130', 0, 0, 0, 0),
    ('A/A3/0047', 0, 0, 0, 0),
    ('A/A3/0053', 0, 0, 0, 0),
    ('A/A3/0116', 0, 0, 0, 0),
    ('A/A3/0132', 0, 0, 0, 0),
    ('A/A3/0157', 0, 0, 0, 0),
    ('A/A4/0242', 0, 0, 0, 0),
    ('A/A4/0421', 0, 0, 0, 0),
    ('A/A5/0001', 0, 0, 0, 0),
    ('A/A5/0055', 0, 0, 0, 0),
    ('A/A5/0123', 0, 0, 0, 0),
    ('A/A5/0128', 0, 0, 0, 0),
    ('A/A5/0162', 0, 0, 0, 0),
    ('A/A5/0190', 0, 0, 0, 0),
    ('A/A5/0210', 0, 0, 0, 0),
    ('A/A5/0213', 0, 0, 0, 0),
    ('A/A5/0234', 0, 0, 0, 0),
    ('A/A5/0239', 0, 0, 0, 0),
    ('A/A5/0253', 0, 0, 0, 0),
    ('A/A5/0353', 0, 0, 0, 0),
    ('A/A5/0360', 0, 0, 0, 0),
    ('A/A5/0458', 0, 0, 0, 0),
    ('A/A5/0459', 0, 0, 0, 0),
    ('A/A6/0005', 0, 0, 0, 0),
    ('A/A6/0013', 0, 0, 0, 0),
    ('A/A6/0024', 0, 0, 0, 0),
    ('A/A6/0061', 0, 0, 0, 0),
    ('A/A6/0069', 0, 0, 0, 0),
    ('A/A6/0098', 0, 0, 0, 0),
    ('A/A6/0105', 0, 0, 0, 0),
    ('A/A6/0223', 0, 0, 0, 0),
    ('A/A6/0275', 0, 0, 0, 0),
    ('A/A6/0276', 0, 0, 0, 0),
    ('A/A6/0314', 0, 0, 0, 0),
    ('A/A6/0315', 0, 0, 0, 0),
    ('A/A6/0358', 0, 0, 0, 0),
    ('A/A6/0430', 0, 0, 0, 0),
    ('A/A6/0543', 0, 0, 0, 0),
    ('A/A6/0565', 0, 0, 0, 0),
    ('A/A7/0028', 0, 0, 0, 0),
    ('A/A7/0080', 0, 0, 0, 0),
    ('A/A7/0122', 0, 0, 0, 0),
    ('A/A7/0150', 0, 0, 0, 0),
    ('A/A7/0155', 0, 0, 0, 0),
    ('A/A7/0164', 0, 0, 0, 0),
    ('A/A7/0166', 0, 0, 0, 0),
    ('A/A7/0241', 0, 0, 0, 0),
    ('A/A8/0005', 0, 0, 0, 0),
    ('A/A8/0025', 0, 0, 0, 0),
    ('A/A8/0047', 0, 0, 0, 0),
    ('A/A8/0061', 0, 0, 0, 0),
    ('A/A8/0065', 0, 0, 0, 0),
    ('A/A8/0066', 0, 0, 0, 0),
    ('A/A8/0128', 0, 0, 0, 0),
    ('A/A8/0290', 0, 0, 0, 0),
    ('A/A8/0317', 0, 0, 0, 0),
    ('A/A8/0357', 0, 0, 0, 0),
    ('A/A8/0367', 0, 0, 0, 0),
    ('A/Q5/0353', 0, 0, 0, 0),
    ('B/B1/0016', 0, 0, 0, 0),
    ('B/B1/0116', 0, 0, 0, 0),
    ('B/B1/0130', 0, 0, 0, 0),
    ('B/B1/0155', 0, 0, 0, 0),
    ('B/B1/0160', 0, 0, 0, 0),
    ('B/B1/0195', 0, 0, 0, 0),
    ('B/B1/0209', 0, 0, 0, 0),
    ('B/B1/0230', 0, 0, 0, 0),
    ('B/B1/0292', 0, 0, 0, 0),
    ('B/B1/0299', 0, 0, 0, 0),
    ('B/B1/0311', 0, 0, 0, 0),
    ('B/B1/0330', 0, 0, 0, 0),
    ('B/B1/0365', 0, 0, 0, 0),
    ('B/B1/0417', 0, 0, 0, 0),
    ('B/B1/0493', 0, 0, 0, 0),
    ('B/B1/0524', 0, 0, 0, 0),
    ('B/B1/0552', 0, 0, 0, 0),
    ('B/B1/0584', 0, 0, 0, 0),
    ('B/B2/0052', 0, 0, 0, 0),
    ('B/B2/0082', 0, 0, 0, 0),
    ('B/B2/0095', 0, 0, 0, 0),
    ('B/B2/0142', 0, 0, 0, 0),
    ('B/B2/0153', 0, 0, 0, 0),
    ('B/B2/0175', 0, 0, 0, 0),
    ('B/B2/0278', 0, 0, 0, 0),
    ('B/B3/0017', 0, 0, 0, 0),
    ('B/B3/0050', 0, 0, 0, 0),
    ('B/B3/0127', 0, 0, 0, 0),
    ('B/B3/0205', 0, 0, 0, 0),
    ('B/B3/0220', 0, 0, 0, 0),
    ('B/B3/0294', 0, 0, 0, 0),
    ('B/B3/0352', 0, 0, 0, 0),
    ('B/B4/0030', 0, 0, 0, 0),
    ('B/B4/0052', 0, 0, 0, 0),
    ('B/B4/0094', 0, 0, 0, 0),
    ('B/B4/0127', 0, 0, 0, 0),
    ('B/B4/0132', 0, 0, 0, 0),
    ('B/B4/0201', 0, 0, 0, 0),
    ('B/B4/0258', 0, 0, 0, 0),
    ('B/B4/0330', 0, 0, 0, 0),
    ('B/B4/0340', 0, 0, 0, 0),
    ('C/C1/0018', 0, 0, 0, 0),
    ('C/C1/0050', 0, 0, 0, 0),
    ('C/C1/0071', 0, 0, 0, 0),
    ('C/C1/0093', 0, 0, 0, 0),
    ('C/C1/0155', 0, 0, 0, 0),
    ('C/C1/0157', 0, 0, 0, 0),
    ('C/C1/0161', 0, 0, 0, 0),
    ('C/C1/0179', 0, 0, 0, 0),
    ('C/C1/0230', 0, 0, 0, 0),
    ('C/C2/0002', 0, 0, 0, 0),
    ('C/C2/0011', 0, 0, 0, 0),
    ('C/C2/0023', 0, 0, 0, 0),
    ('C/C2/0046', 0, 0, 0, 0),
    ('C/C2/0056', 0, 0, 0, 0),
    ('C/C2/0082', 0, 0, 0, 0),
    ('C/C2/0107', 0, 0, 0, 0),
    ('C/C2/0134', 0, 0, 0, 0),
    ('C/C2/0145', 0, 0, 0, 0),
    ('C/C2/0152', 0, 0, 0, 0),
    ('C/C2/0197', 0, 0, 0, 0),
    ('C/C2/0211', 0, 0, 0, 0),
    ('C/C2/0300', 0, 0, 0, 0),
    ('C/C2/0369', 0, 0, 0, 0),
    ('C/C2/0384', 0, 0, 0, 0),
    ('C/C2/0387', 0, 0, 0, 0),
    ('C/C2/0428', 0, 0, 0, 0),
    ('C/C3/0001', 0, 0, 0, 0),
    ('C/C3/0020', 0, 0, 0, 0),
    ('C/C3/0028', 0, 0, 0, 0),
    ('C/C3/0050', 0, 0, 0, 0),
    ('C/C3/0075', 0, 0, 0, 0),
    ('C/C3/0155', 0, 0, 0, 0),
    ('C/C3/0169', 0, 0, 0, 0),
    ('C/C3/0172', 0, 0, 0, 0),
    ('C/C3/0173', 0, 0, 0, 0),
    ('C/C3/0189', 0, 0, 0, 0),
    ('C/C3/0199', 0, 0, 0, 0),
    ('C/C3/0221', 0, 0, 0, 0),
    ('C/C3/0375', 0, 0, 0, 0),
    ('C/C3/0420', 0, 0, 0, 0),
    ('C/C3/0464', 0, 0, 0, 0),
    ('D/D1/0005', 0, 0, 0, 0),
    ('D/D1/0058', 0, 0, 0, 0),
    ('D/D1/0061', 0, 0, 0, 0),
    ('D/D1/0066', 0, 0, 0, 0),
    ('D/D1/0080', 0, 0, 0, 0),
    ('D/D1/0082', 0, 0, 0, 0),
    ('D/D1/0102', 0, 0, 0, 0),
    ('D/D1/0105', 0, 0, 0, 0),
    ('D/D1/0110', 0, 0, 0, 0),
    ('D/D1/0127', 0, 0, 0, 0),
    ('D/D1/0137', 0, 0, 0, 0),
    ('D/D1/0142', 0, 0, 0, 0),
    ('D/D1/0155', 0, 0, 0, 0),
    ('D/D1/0169', 0, 0, 0, 0),
    ('D/D1/0175', 0, 0, 0, 0),
    ('D/D1/0232', 0, 0, 0, 0),
    ('D/D1/0236', 0, 0, 0, 0),
    ('D/D1/0238', 0, 0, 0, 0),
    ('D/D1/0244', 0, 0, 0, 0),
    ('D/D1/0255', 0, 0, 0, 0),
    ('D/D1/0260', 0, 0, 0, 0),
    ('D/D1/0268', 0, 0, 0, 0),
    ('D/D1/0280', 0, 0, 0, 0),
    ('D/D1/0286', 0, 0, 0, 0),
    ('D/D1/0298', 0, 0, 0, 0),
    ('D/D1/0308', 0, 0, 0, 0),
    ('D/D1/0341', 0, 0, 0, 0),
    ('D/D1/0373', 0, 0, 0, 0),
    ('D/D1/0407', 0, 0, 0, 0),
    ('D/D1/0408', 0, 0, 0, 0),
    ('D/D1/0429', 0, 0, 0, 0),
    ('D/D1/0442', 0, 0, 0, 0),
    ('D/D1/0446', 0, 0, 0, 0),
    ('D/D1/0447', 0, 0, 0, 0),
    ('D/D1/0454', 0, 0, 0, 0),
    ('D/d1/0461', 0, 0, 0, 0),
    ('D/D1/0467', 0, 0, 0, 0),
    ('D/D1/0468', 0, 0, 0, 0),
    ('D/D2/0022', 0, 0, 0, 0),
    ('D/D2/0024', 0, 0, 0, 0),
    ('D/D2/0040', 0, 0, 0, 0),
    ('D/D2/0058', 0, 0, 0, 0),
    ('D/D2/0074', 0, 0, 0, 0),
    ('D/D2/0118', 0, 0, 0, 0),
    ('D/D2/0130', 0, 0, 0, 0),
    ('D/D2/0197', 0, 0, 0, 0),
    ('D/D2/0204', 0, 0, 0, 0),
    ('D/D2/0231', 0, 0, 0, 0),
    ('D/D2/0250', 0, 0, 0, 0),
    ('D/D2/0299', 0, 0, 0, 0),
    ('D/D2/0311', 0, 0, 0, 0),
    ('D/D3/0007', 0, 0, 0, 0),
    ('D/D3/0009', 0, 0, 0, 0),
    ('D/D3/0026', 0, 0, 0, 0),
    ('D/D3/0031', 0, 0, 0, 0),
    ('D/D3/0048', 0, 0, 0, 0),
    ('D/D3/0058', 0, 0, 0, 0),
    ('D/D3/0088', 0, 0, 0, 0),
    ('D/D3/0095', 0, 0, 0, 0),
    ('D/D3/0099', 0, 0, 0, 0),
    ('D/D3/0146', 0, 0, 0, 0),
    ('D/D3/0161', 0, 0, 0, 0),
    ('D/D3/0173', 0, 0, 0, 0),
    ('D/D3/0217', 0, 0, 0, 0),
    ('D/D3/0263', 0, 0, 0, 0),
    ('D/D3/0296', 0, 0, 0, 0),
    ('D/D3/0334', 0, 0, 0, 0),
    ('D/D3/0372', 0, 0, 0, 0),
    ('D/D3/0378', 0, 0, 0, 0),
    ('D/D3/0400', 0, 0, 0, 0),
    ('D/D3/0411', 0, 0, 0, 0),
    ('D/D3/0429', 0, 0, 0, 0),
    ('D/D3/0436', 0, 0, 0, 0),
    ('D/D3/0481', 0, 0, 0, 0),
    ('D/D3/0484', 0, 0, 0, 0),
    ('D/D4/0016', 0, 0, 0, 0),
    ('D/D4/0037', 0, 0, 0, 0),
    ('D/D4/0048', 0, 0, 0, 0),
    ('D/D4/0103', 0, 0, 0, 0),
    ('D/D4/0122', 0, 0, 0, 0),
    ('D/D4/0163', 0, 0, 0, 0),
    ('D/D4/0164', 0, 0, 0, 0),
    ('D/D4/0200', 0, 0, 0, 0),
    ('D/D4/0233', 0, 0, 0, 0),
    ('D/D4/0255', 0, 0, 0, 0),
    ('D/D4/0257', 0, 0, 0, 0),
    ('D/D5/0079', 0, 0, 0, 0),
    ('D/D6/0001', 0, 0, 0, 0),
    ('D/D6/0006', 0, 0, 0, 0),
    ('D/D6/0021', 0, 0, 0, 0),
    ('D/D6/0043', 0, 0, 0, 0),
    ('D/D6/0052', 0, 0, 0, 0),
    ('D/D6/0059', 0, 0, 0, 0),
    ('D/D6/0064', 0, 0, 0, 0),
    ('D/D6/0071', 0, 0, 0, 0),
    ('D/D6/0074', 0, 0, 0, 0),
    ('D/D6/0079', 0, 0, 0, 0),
    ('D/D6/0088', 0, 0, 0, 0),
    ('D/D6/0119', 0, 0, 0, 0),
    ('D/D6/0142', 0, 0, 0, 0),
    ('D/D6/0154', 0, 0, 0, 0),
    ('D/D6/0155', 0, 0, 0, 0),
    ('D/D6/0157', 0, 0, 0, 0),
    ('D/D6/0201', 0, 0, 0, 0),
    ('D/D6/0202', 0, 0, 0, 0),
    ('D/D6/0256', 0, 0, 0, 0),
    ('D/D6/0278', 0, 0, 0, 0),
    ('D/D6/0281', 0, 0, 0, 0),
    ('D/D7/0062', 0, 0, 0, 0),
    ('D/D7/0132', 0, 0, 0, 0),
    ('D/D7/0167', 0, 0, 0, 0),
    ('D/D7/0240', 0, 0, 0, 0),
    ('D/D7/0352', 0, 0, 0, 0),
    ('E/E1/0020 A', 0, 0, 0, 0),
    ('E/E1/0023', 0, 0, 0, 0),
    ('E/E1/0040', 0, 0, 0, 0),
    ('E/E1/0195', 0, 0, 0, 0),
    ('E/E1/0213', 0, 0, 0, 0),
    ('E/E1/0252', 0, 0, 0, 0),
    ('E/E1/0283', 0, 0, 0, 0),
    ('E/E1/0291', 0, 0, 0, 0),
    ('E/E1/0367', 0, 0, 0, 0),
    ('E/E1/0411', 0, 0, 0, 0),
    ('E/E1/0415', 0, 0, 0, 0),
    ('E/E2/0027', 0, 0, 0, 0),
    ('E/E2/0053', 0, 0, 0, 0),
    ('E/E2/0077', 0, 0, 0, 0),
    ('E/E2/0093', 0, 0, 0, 0),
    ('E/E3/0035', 0, 0, 0, 0),
    ('E/E3/0064', 0, 0, 0, 0),
    ('E/E3/0076', 0, 0, 0, 0),
    ('E/E3/0081', 0, 0, 0, 0),
    ('E/E3/0121', 0, 0, 0, 0),
    ('E/E3/0314', 0, 0, 0, 0),
    ('E/E3/0328', 0, 0, 0, 0),
    ('E/E4/0018', 0, 0, 0, 0),
    ('E/E4/0036', 0, 0, 0, 0),
    ('E/E4/0065', 0, 0, 0, 0),
    ('E/E4/0094', 0, 0, 0, 0),
    ('E/E4/0099', 0, 0, 0, 0),
    ('E/E4/0204', 0, 0, 0, 0),
    ('E/E4/0224', 0, 0, 0, 0),
    ('E/E4/0290', 0, 0, 0, 0),
    ('E/E5/0005', 0, 0, 0, 0),
    ('E/E5/0079', 0, 0, 0, 0),
    ('E/E5/0081', 0, 0, 0, 0),
    ('E/E5/0113', 0, 0, 0, 0),
    ('E/E5/0118', 0, 0, 0, 0),
    ('E/E5/0125', 0, 0, 0, 0),
    ('E/E5/0163', 0, 0, 0, 0),
    ('E/E5/0172', 0, 0, 0, 0),
    ('F/F1/0021', 0, 0, 0, 0),
    ('F/F1/0036', 0, 0, 0, 0),
    ('F/F1/0052', 0, 0, 0, 0),
    ('F/F1/0062', 0, 0, 0, 0),
    ('F/F1/0074', 0, 0, 0, 0),
    ('F/F1/0080', 0, 0, 0, 0),
    ('F/F1/0082', 0, 0, 0, 0),
    ('F/F1/0087', 0, 0, 0, 0),
    ('F/F1/0092', 0, 0, 0, 0),
    ('F/F1/0142', 0, 0, 0, 0),
    ('F/F1/0149', 0, 0, 0, 0),
    ('F/F1/0212', 0, 0, 0, 0),
    ('F/F1/0310', 0, 0, 0, 0),
    ('F/F10020', 0, 0, 0, 1),
    ('F/F2/0016', 0, 0, 0, 0),
    ('F/F2/0021', 0, 0, 0, 0),
    ('F/F2/0035', 0, 0, 0, 0),
    ('F/F2/0041', 0, 0, 0, 0),
    ('F/F2/0077', 0, 0, 0, 0),
    ('F/F2/0079', 0, 0, 0, 0),
    ('F/F2/0083', 0, 0, 0, 0),
    ('F/F2/0085', 0, 0, 0, 0),
    ('F/F2/0089', 0, 0, 0, 0),
    ('F/F2/0090', 0, 0, 0, 0),
    ('F/F2/0131', 0, 0, 0, 0),
    ('F/F2/0134', 0, 0, 0, 0),
    ('F/F2/0135', 0, 0, 0, 0),
    ('F/F2/0146', 0, 0, 0, 0),
    ('F/F2/0152', 0, 0, 0, 0),
    ('F/F2/0247', 0, 0, 0, 0),
    ('F/F2/0320', 0, 0, 0, 0),
    ('F/F2/0347', 0, 0, 0, 0),
    ('F/F2/0348', 0, 0, 0, 0),
    ('F/F2/0351', 0, 0, 0, 0),
    ('F/F2/0371', 0, 0, 0, 0),
    ('F/F2/0373', 0, 0, 0, 0),
    ('F/F3/0042', 0, 0, 0, 0),
    ('F/F3/0106', 0, 0, 0, 0),
    ('F/F3/0147', 0, 0, 0, 0),
    ('F/F3/0159', 0, 0, 0, 0),
    ('F/F3/0167', 0, 0, 0, 0),
    ('F/F3/0183', 0, 0, 0, 0),
    ('F/F3/0197', 0, 0, 0, 0),
    ('F/F3/0225', 0, 0, 0, 0),
    ('F/F3/0268', 0, 0, 0, 0),
    ('F/F3/0285', 0, 0, 0, 0),
    ('F/F3/0296', 0, 0, 0, 0),
    ('F/F3/0297', 0, 0, 0, 0),
    ('F/F3/0309', 0, 0, 0, 0),
    ('F/F3/0318', 0, 0, 0, 0),
    ('F/F4/0018', 0, 0, 0, 0),
    ('F/F4/0081', 0, 0, 0, 0),
    ('F/F4/0090', 0, 0, 0, 0),
    ('F/F4/0143', 0, 0, 0, 0),
    ('F/F4/0169', 0, 0, 0, 0),
    ('F/F5/0005', 0, 0, 0, 0),
    ('F/F5/0021', 0, 0, 0, 0),
    ('F/F5/0026', 0, 0, 0, 0),
    ('F/F5/0049', 0, 0, 0, 0),
    ('F/F5/0061', 0, 0, 0, 0),
    ('F/F5/0065', 0, 0, 0, 0),
    ('F/F5/0090', 0, 0, 0, 0),
    ('F/F5/0098', 0, 0, 0, 0),
    ('F/F5/0135', 0, 0, 0, 0),
    ('G/G1/0035', 0, 0, 0, 0),
    ('G/G1/0058', 0, 0, 0, 0),
    ('G/G1/0064', 0, 0, 0, 0),
    ('G/G1/0066', 0, 0, 0, 0),
    ('G/G1/0112', 0, 0, 0, 0),
    ('G/G1/0122', 0, 0, 0, 0),
    ('G/G1/0123', 0, 0, 0, 0),
    ('G/G1/0129', 0, 0, 0, 0),
    ('G/G1/0136', 0, 0, 0, 0),
    ('G/G1/0146', 0, 0, 0, 0),
    ('G/G1/0210', 0, 0, 0, 0),
    ('G/G1/0346', 0, 0, 0, 0),
    ('G/G1/0366', 0, 0, 0, 0),
    ('G/G1/0371A', 0, 0, 0, 0),
    ('G/G1/0382', 0, 0, 0, 0),
    ('G/G1/0390', 0, 0, 0, 0),
    ('G/G2/0038', 0, 0, 0, 0),
    ('G/G2/0052', 0, 0, 0, 0),
    ('G/G2/0069', 0, 0, 0, 0),
    ('G/G2/0081', 0, 0, 0, 0),
    ('G/G2/0085', 0, 0, 0, 0),
    ('G/G2/0127', 0, 0, 0, 0),
    ('G/G2/0134', 0, 0, 0, 0),
    ('G/G2/0146', 0, 0, 0, 0),
    ('G/G2/0165', 0, 0, 0, 0),
    ('G/G2/0186', 0, 0, 0, 0),
    ('G/G2/0212', 0, 0, 0, 0),
    ('G/G2/0213', 0, 0, 0, 0),
    ('G/G2/0227', 0, 0, 0, 0),
    ('G/G3/0022', 0, 0, 0, 0),
    ('G/G3/0028', 0, 0, 0, 0),
    ('G/G3/0034', 0, 0, 0, 0),
    ('G/G3/0060', 0, 0, 0, 0),
    ('G/G3/0085', 0, 0, 0, 0),
    ('G/G3/0098', 0, 0, 0, 0),
    ('G/G3/0122', 0, 0, 0, 0),
    ('G/G3/0178', 0, 0, 0, 0),
    ('G/G4/0018', 0, 0, 0, 0),
    ('G/G4/0053', 0, 0, 0, 0),
    ('G/G4/0107', 0, 0, 0, 0),
    ('G/G4/0119', 0, 0, 0, 0),
    ('G/G4/0126', 0, 0, 0, 0),
    ('G/G4/0127', 0, 0, 0, 0),
    ('G/G4/0140', 0, 0, 0, 0),
    ('G/G4/0158', 0, 0, 0, 0),
    ('G/G4/0172', 0, 0, 0, 0),
    ('G/G4/0189', 0, 0, 0, 0),
    ('G/G4/0202', 0, 0, 0, 0),
    ('G/G4/0203', 0, 0, 0, 0),
    ('G2/0142', 0, 0, 0, 1),
    ('H/H1/0085', 0, 0, 0, 0),
    ('H/H1/0096', 0, 0, 0, 0),
    ('H/H1/0111', 0, 0, 0, 0),
    ('H/H1/0122', 0, 0, 0, 0),
    ('H/H1/0138', 0, 0, 0, 0),
    ('H/H1/0159', 0, 0, 0, 0),
    ('H/H1/0172', 0, 0, 0, 0),
    ('H/H1/0182', 0, 0, 0, 0),
    ('H/H1/0248', 0, 0, 0, 0),
    ('H/H1/0259', 0, 0, 0, 0),
    ('H/H1/0283', 0, 0, 0, 0),
    ('H/H1/0335', 0, 0, 0, 0),
    ('H/H1/0361', 0, 0, 0, 0),
    ('H/H1/0382', 0, 0, 0, 0),
    ('H/H1/0388', 0, 0, 0, 0),
    ('H/H1/0461', 0, 0, 0, 0),
    ('H/H1/0481', 0, 0, 0, 0),
    ('H/H2/0009', 0, 0, 0, 0),
    ('H/H2/0016', 0, 0, 0, 0),
    ('H/H2/0028', 0, 0, 0, 0),
    ('H/H2/0060', 0, 0, 0, 0),
    ('H/H2/0117', 0, 0, 0, 0),
    ('H/H2/0118', 0, 0, 0, 0),
    ('H/H2/0141', 0, 0, 0, 0),
    ('H/H2/0166', 0, 0, 0, 0),
    ('H/h2/0189', 0, 0, 0, 0),
    ('H/H2/0250', 0, 0, 0, 0),
    ('H/H2/0252', 0, 0, 0, 0),
    ('H/H2/0260', 0, 0, 0, 0),
    ('H/H3/0030', 0, 0, 0, 0),
    ('H/H3/0041', 0, 0, 0, 0),
    ('H/H3/0064', 0, 0, 0, 0),
    ('H/H3/0119', 0, 0, 0, 0),
    ('H/H3/0163', 0, 0, 0, 0),
    ('H/H3/0186', 0, 0, 0, 0),
    ('H/H3/0205', 0, 0, 0, 0),
    ('H/H3/0227', 0, 0, 0, 0),
    ('H/H3/0253', 0, 0, 0, 0),
    ('H/H3/0277', 0, 0, 0, 0),
    ('H/H4/0011', 0, 0, 0, 0),
    ('H/H4/0022', 0, 0, 0, 0),
    ('H/H4/0083', 0, 0, 0, 0),
    ('H/H4/0086', 0, 0, 0, 0),
    ('H/H4/0098', 0, 0, 0, 0),
    ('H/H4/0110', 0, 0, 0, 0),
    ('H/H4/0137', 0, 0, 0, 0),
    ('H/H4/0150', 0, 0, 0, 0),
    ('H/H4/0154', 0, 0, 0, 0),
    ('H/H4/0175', 0, 0, 0, 0),
    ('H/H4/0176', 0, 0, 0, 0),
    ('H/H4/0179', 0, 0, 0, 0),
    ('H/H4/0186', 0, 0, 0, 0),
    ('H/H4/0193', 0, 0, 0, 0),
    ('H/H4/0252', 0, 0, 0, 0),
    ('H/H4/0273', 0, 0, 0, 0),
    ('H/H4/0291', 0, 0, 0, 0),
    ('H/H5/0001', 0, 0, 0, 0),
    ('H/H5/0006', 0, 0, 0, 0),
    ('H/H5/0021', 0, 0, 0, 0),
    ('H/H5/0050', 0, 0, 0, 0),
    ('H/H5/0064', 0, 0, 0, 0),
    ('H/H5/0091', 0, 0, 0, 0),
    ('H/H5/0115', 0, 0, 0, 0),
    ('H/H5/0128', 0, 0, 0, 0),
    ('H/H5/0135', 0, 0, 0, 0),
    ('H/H5/0166', 0, 0, 0, 0),
    ('H/H5/0223', 0, 0, 0, 0),
    ('H/H5/0225', 0, 0, 0, 0),
    ('H/H5/0272', 0, 0, 0, 0),
    ('H/H5/0314', 0, 0, 0, 0),
    ('H/H5/0326', 0, 0, 0, 0),
    ('H/H5/0384', 0, 0, 0, 0),
    ('H/H5/0400', 0, 0, 0, 0),
    ('H/H5/0403', 0, 0, 0, 0);

    SELECT COUNT(*) INTO v_nb_doublons
    FROM (
        SELECT LOWER(TRIM(c.CodeCons))
        FROM Clients c
        INNER JOIN tmp_codecons_regularisation t
            ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
        GROUP BY LOWER(TRIM(c.CodeCons))
        HAVING COUNT(*) > 1
    ) d;

    IF v_nb_doublons > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Régularisation annulée : doublon CodeCons en base pour au moins un code cible.';
    END IF;

    SELECT COUNT(*) INTO v_nb_avant
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    INNER JOIN tmp_codecons_regularisation t
        ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)
      AND cf.Statut = 1
      AND COALESCE(cf.MontantPaye, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
      )
      AND (COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0);

    IF v_nb_avant = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Régularisation annulée : aucune ClientFacture éligible à modifier.';
    END IF;

    START TRANSACTION;

    UPDATE ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    INNER JOIN tmp_codecons_regularisation t
        ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
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

    SET v_lignes_modifiees = ROW_COUNT();

    IF p_dry_run = 1 THEN
        ROLLBACK;
        SELECT
            'DRY-RUN' AS mode,
            v_nb_avant AS nb_eligibles_avant,
            v_lignes_modifiees AS lignes_modifiees_simulees,
            'ROLLBACK effectué — aucune donnée persistée' AS resultat;
    ELSE
        COMMIT;
        SELECT
            'PRODUCTION' AS mode,
            v_nb_avant AS nb_eligibles_avant,
            v_lignes_modifiees AS lignes_modifiees,
            'COMMIT effectué' AS resultat;
    END IF;

    DROP TEMPORARY TABLE IF EXISTS tmp_codecons_regularisation;
END$$

DELIMITER ;

-- CALL sp_regulariser_clientfactures_2026_02_03_04(1);
-- CALL sp_regulariser_clientfactures_2026_02_03_04(0);


-- ----------------------------------------------------------------------------
-- SECTION 5 — Checklist opérateur
-- ----------------------------------------------------------------------------
-- [ ] Backup ClientFactures
-- [ ] python3 Scripts/generate_codecons_regularisation_data.py (si Excel mis à jour)
-- [ ] Section 1b/1c : absents et G2/0142 revus avec le métier
-- [ ] Section 1d : nb résolus validé
-- [ ] Section 2a : totaux éligibles validés
-- [ ] Section 2b : lignes bloquées (payées) acceptées
-- [ ] Section 2c : clients sans CF notés
-- [ ] Dry-run SECTION 4 avec ROLLBACK
-- [ ] Passage réel avec COMMIT
-- [ ] Contrôle arriérés consolidés / rapports Fév-Mar-Avr 2026

SELECT 'Fin du script — vérifier la checklist SECTION 5' AS Etape;
