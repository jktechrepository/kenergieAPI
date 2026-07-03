-- ============================================================================
-- Script PRODUCTION : suppression ClientFactures — mai 2026, par CodeAxe
-- ============================================================================
-- Objectif :
--   Supprimer les ClientFactures pour Annees = 2026, Mois = '05' ou '5',
--   pour les clients rattachés aux axes dont Axes.CodeAxe est dans la liste :
--   A5, B1, B3, E1, E3, F1, F3, F4, G1, H1
--
-- Relation :
--   ClientFactures.IdClient → Clients.IdClient
--   Clients.IdAxe           → Axes.IdAxe
--   Filtre métier           → Axes.CodeAxe (PAS Clients.IdAxe avec des strings)
--
-- Périmètre suppression :
--   - Tous les clients des axes listés (IsActif true ou false)
--   - Uniquement lignes avec COALESCE(MontantPaye, 0) = 0
--   - Uniquement lignes SANS Paiement lié (Paiements.IdClientFacture)
--
-- NE SUPPRIME PAS : Factures, Clients, Axes, Paiements
--
-- ============================================================================
-- PROCÉDURE D'EXÉCUTION (OBLIGATOIRE)
-- ============================================================================
--
-- 1) Backup :
--      mysqldump -u ... -p ... ClientFactures Paiements > backup_cf_axes_YYYYMMDD.sql
--
-- 2) Exécuter SECTIONS 0 à 3 (lecture seule) — valider :
--      - 10 CodeAxe résolus (section 1b)
--      - aucun doublon CodeAxe (section 1c)
--      - totaux section 2 = lignes réellement supprimables
--
-- 3) DRY-RUN : SECTION 4 + ROLLBACK;
-- 4) PRODUCTION : SECTION 4 + COMMIT;
--
-- Alternative :
--      CALL sp_delete_clientfactures_2026_05_par_codeaxe(1);  -- dry-run
--      CALL sp_delete_clientfactures_2026_05_par_codeaxe(0);  -- réel
--
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SECTION 0 — Paramètres et table des CodeAxe cibles
-- ----------------------------------------------------------------------------
SET @annee_cible   := 2026;
SET @mois_pad      := '05';
SET @mois_sans_pad := '5';

DROP TEMPORARY TABLE IF EXISTS tmp_codes_axe_cibles;
CREATE TEMPORARY TABLE tmp_codes_axe_cibles (
    CodeAxe VARCHAR(50) NOT NULL PRIMARY KEY
);

INSERT INTO tmp_codes_axe_cibles (CodeAxe) VALUES
    ('A5'), ('B1'), ('B3'), ('E1'), ('E3'),
    ('F1'), ('F3'), ('F4'), ('G1'), ('H1');

SELECT
    'SECTION 0 — Paramètres' AS Etape,
    @annee_cible   AS annee_cible,
    @mois_pad      AS mois_pad,
    @mois_sans_pad AS mois_sans_pad,
    (SELECT COUNT(*) FROM tmp_codes_axe_cibles) AS nb_codes_axe_attendus,
    DATABASE()     AS base_courante,
    NOW()          AS execute_le;


-- ----------------------------------------------------------------------------
-- SECTION 1 — Résolution CodeAxe → IdAxe (LECTURE SEULE)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 1a — Axes résolus (CodeAxe → IdAxe)' AS Etape;

SELECT
    a.IdAxe,
    a.CodeAxe,
    a.NomAxe,
    a.Statut AS axe_statut,
    COUNT(c.IdClient) AS nb_clients_rattaches
FROM Axes a
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
LEFT JOIN Clients c ON c.IdAxe = a.IdAxe
GROUP BY a.IdAxe, a.CodeAxe, a.NomAxe, a.Statut
ORDER BY a.CodeAxe;


SELECT 'SECTION 1b — CodeAxe demandés mais ABSENTS en base' AS Etape;
-- Doit retourner 0 ligne. Si des codes apparaissent : corriger la liste ou la base.

SELECT t.CodeAxe AS code_axe_manquant
FROM tmp_codes_axe_cibles t
LEFT JOIN Axes a ON a.CodeAxe = t.CodeAxe
WHERE a.IdAxe IS NULL;


SELECT 'SECTION 1c — Doublons CodeAxe (alerte si > 0)' AS Etape;

SELECT
    a.CodeAxe,
    COUNT(*) AS nb_axes_meme_code,
    GROUP_CONCAT(a.IdAxe ORDER BY a.IdAxe) AS id_axes_concernes
FROM Axes a
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
GROUP BY a.CodeAxe
HAVING COUNT(*) > 1;


-- ----------------------------------------------------------------------------
-- SECTION 2 — Preview lignes ÉLIGIBLES à la suppression (LECTURE SEULE)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 2a — Synthèse lignes éligibles (MontantPaye=0, sans Paiement)' AS Etape;

SELECT
    COUNT(*) AS nb_cf_eligibles,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_distincts,
    SUM(COALESCE(cf.Montant, 0)) AS somme_montant,
    SUM(COALESCE(cf.MontantDu, 0)) AS somme_montant_du,
    SUM(CASE WHEN cf.Mois = @mois_pad THEN 1 ELSE 0 END) AS nb_mois_format_05,
    SUM(CASE WHEN cf.Mois = @mois_sans_pad THEN 1 ELSE 0 END) AS nb_mois_format_5
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT 'SECTION 2b — Répartition par CodeAxe' AS Etape;

SELECT
    a.CodeAxe,
    a.IdAxe,
    COUNT(*) AS nb_cf_eligibles,
    SUM(COALESCE(cf.MontantDu, 0)) AS total_montant_du
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  )
GROUP BY a.CodeAxe, a.IdAxe
ORDER BY a.CodeAxe;


SELECT 'SECTION 2c — Contrôle négatif (mai 2026 HORS axes listés, même critères)' AS Etape;

SELECT COUNT(*) AS nb_cf_hors_axes_non_concernees
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
LEFT JOIN Axes a ON a.IdAxe = c.IdAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND (a.CodeAxe IS NULL OR a.CodeAxe NOT IN (SELECT CodeAxe FROM tmp_codes_axe_cibles))
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT 'SECTION 2d — Échantillon TOP 50 lignes éligibles' AS Etape;

SELECT
    cf.IdClientFacture,
    cf.IdClient,
    c.NomClient,
    c.CodeCons,
    c.IsActif,
    a.CodeAxe,
    a.IdAxe,
    cf.IdFacture,
    cf.Mois,
    cf.Annees,
    cf.Montant,
    cf.MontantPaye,
    cf.MontantDu,
    cf.Statut AS cf_statut
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  )
ORDER BY a.CodeAxe, cf.IdClientFacture
LIMIT 50;


-- ----------------------------------------------------------------------------
-- SECTION 3 — Lignes EXCLUES de la suppression (LECTURE SEULE)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 3a — Exclues : MontantPaye > 0' AS Etape;

SELECT
    COUNT(*) AS nb_exclues_montant_paye_positif,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) > 0;


SELECT 'SECTION 3b — Exclues : au moins un Paiement lié' AS Etape;

SELECT
    COUNT(DISTINCT cf.IdClientFacture) AS nb_exclues_avec_paiement
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT 'SECTION 3c — Détail lignes exclues (TOP 50)' AS Etape;

SELECT
    cf.IdClientFacture,
    c.CodeCons,
    a.CodeAxe,
    cf.MontantPaye,
    cf.MontantDu,
    CASE
        WHEN COALESCE(cf.MontantPaye, 0) > 0 THEN 'MontantPaye > 0'
        WHEN EXISTS (SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture)
            THEN 'Paiement lié'
        ELSE 'Autre'
    END AS raison_exclusion
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND (
      COALESCE(cf.MontantPaye, 0) > 0
      OR EXISTS (SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture)
  )
ORDER BY a.CodeAxe, cf.IdClientFacture
LIMIT 50;


SELECT 'SECTION 3d — Verdict' AS Etape;

SELECT
    (SELECT COUNT(*) FROM tmp_codes_axe_cibles t
     LEFT JOIN Axes a ON a.CodeAxe = t.CodeAxe WHERE a.IdAxe IS NULL) AS nb_codes_manquants,
    (SELECT COUNT(*) FROM (
        SELECT a.CodeAxe FROM Axes a
        INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
        GROUP BY a.CodeAxe HAVING COUNT(*) > 1
     ) d) AS nb_codes_en_doublon,
    CASE
        WHEN (SELECT COUNT(*) FROM tmp_codes_axe_cibles t
              LEFT JOIN Axes a ON a.CodeAxe = t.CodeAxe WHERE a.IdAxe IS NULL) > 0
        THEN 'ARRET — CodeAxe manquant(s) : corriger avant DELETE'
        ELSE 'OK — SECTION 4 autorisée après validation métier (dry-run ROLLBACK d''abord)'
    END AS verdict;


-- ----------------------------------------------------------------------------
-- SECTION 4 — Suppression transactionnelle (MANUELLE)
-- ----------------------------------------------------------------------------
-- Prérequis : SECTION 3d verdict = OK, totaux section 2 validés
--
-- DRY-RUN  : exécuter puis ROLLBACK;
-- PRODUCTION : exécuter puis COMMIT;

START TRANSACTION;

SELECT COUNT(*) INTO @nb_eligibles_tx
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );

SELECT @nb_eligibles_tx AS nb_eligibles_dans_transaction;

DELETE cf
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );

SELECT ROW_COUNT() AS lignes_supprimees;

SELECT COUNT(*) AS nb_restantes_eligibles_apres_delete
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN Axes a ON a.IdAxe = c.IdAxe
INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND COALESCE(cf.MontantPaye, 0) = 0
  AND NOT EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );

-- >>> DRY-RUN (1er passage) :
ROLLBACK;

-- >>> PRODUCTION (2e passage, après validation) :
-- COMMIT;


-- ----------------------------------------------------------------------------
-- SECTION 4b — Procédure stockée (optionnelle)
-- ----------------------------------------------------------------------------
-- Recrée tmp_codes_axe_cibles en interne. p_dry_run=1 → ROLLBACK, 0 → COMMIT.

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_delete_clientfactures_2026_05_par_codeaxe$$

CREATE PROCEDURE sp_delete_clientfactures_2026_05_par_codeaxe(IN p_dry_run TINYINT)
BEGIN
    DECLARE v_nb_manquants INT DEFAULT 0;
    DECLARE v_nb_avant INT DEFAULT 0;
    DECLARE v_lignes_supprimees INT DEFAULT 0;

    SET @annee_cible   := 2026;
    SET @mois_pad      := '05';
    SET @mois_sans_pad := '5';

    DROP TEMPORARY TABLE IF EXISTS tmp_codes_axe_cibles;
    CREATE TEMPORARY TABLE tmp_codes_axe_cibles (
        CodeAxe VARCHAR(50) NOT NULL PRIMARY KEY
    );

    INSERT INTO tmp_codes_axe_cibles (CodeAxe) VALUES
        ('A5'), ('B1'), ('B3'), ('E1'), ('E3'),
        ('F1'), ('F3'), ('F4'), ('G1'), ('H1');

    SELECT COUNT(*) INTO v_nb_manquants
    FROM tmp_codes_axe_cibles t
    LEFT JOIN Axes a ON a.CodeAxe = t.CodeAxe
    WHERE a.IdAxe IS NULL;

    IF v_nb_manquants > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Suppression annulée : au moins un CodeAxe cible est absent de la table Axes.';
    END IF;

    SELECT COUNT(*) INTO v_nb_avant
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    INNER JOIN Axes a ON a.IdAxe = c.IdAxe
    INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
      AND COALESCE(cf.MontantPaye, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
      );

    START TRANSACTION;

    DELETE cf
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    INNER JOIN Axes a ON a.IdAxe = c.IdAxe
    INNER JOIN tmp_codes_axe_cibles t ON t.CodeAxe = a.CodeAxe
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
      AND COALESCE(cf.MontantPaye, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
      );

    SET v_lignes_supprimees = ROW_COUNT();

    IF p_dry_run = 1 THEN
        ROLLBACK;
        SELECT
            'DRY-RUN' AS mode,
            v_nb_avant AS nb_eligibles_avant,
            v_lignes_supprimees AS lignes_supprimees_simulees,
            'ROLLBACK effectué — aucune donnée persistée' AS resultat;
    ELSE
        COMMIT;
        SELECT
            'PRODUCTION' AS mode,
            v_nb_avant AS nb_eligibles_avant,
            v_lignes_supprimees AS lignes_supprimees,
            'COMMIT effectué' AS resultat;
    END IF;

    DROP TEMPORARY TABLE IF EXISTS tmp_codes_axe_cibles;
END$$

DELIMITER ;

-- CALL sp_delete_clientfactures_2026_05_par_codeaxe(1);
-- CALL sp_delete_clientfactures_2026_05_par_codeaxe(0);


-- ----------------------------------------------------------------------------
-- SECTION 5 — Checklist opérateur
-- ----------------------------------------------------------------------------
-- [ ] Backup ClientFactures + Paiements
-- [ ] Section 1b : 0 CodeAxe manquant
-- [ ] Section 1c : 0 doublon CodeAxe (ou validé avec le métier)
-- [ ] Section 2 : totaux validés avec le métier
-- [ ] Section 3 : lignes exclues (payées) acceptées
-- [ ] Dry-run SECTION 4 avec ROLLBACK
-- [ ] Passage réel avec COMMIT
-- [ ] Contrôle arriérés consolidés / relances mai 2026

SELECT 'Fin du script — vérifier la checklist SECTION 5' AS Etape;
