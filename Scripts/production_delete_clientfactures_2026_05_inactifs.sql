-- ============================================================================
-- Script PRODUCTION : suppression ClientFactures — mai 2026, clients inactifs
-- ============================================================================
-- Objectif :
--   Supprimer les lignes ClientFactures pour Annees = 2026, Mois = '05' ou '5',
--   uniquement lorsque Clients.IsActif = 0.
--
-- NE SUPPRIME PAS : table Factures (factures globales par usage).
-- NE SUPPRIME PAS : table Paiements (si un paiement pointe vers une ligne cible,
--                   la suppression est BLOQUÉE).
--
-- ============================================================================
-- PROCÉDURE D'EXÉCUTION (OBLIGATOIRE)
-- ============================================================================
--
-- 1) Sauvegarde avant toute modification, par exemple :
--      mysqldump -u ... -p ... ClientFactures Paiements > backup_cf_paiements_YYYYMMDD.sql
--    ou snapshot / backup complet de la base.
--
-- 2) Exécuter les SECTIONS 1 à 3 (lecture seule) et valider les totaux avec le métier.
--
-- 3) DRY-RUN (premier passage sur la section DELETE) :
--      - Exécuter la SECTION 4 jusqu'au DELETE inclus.
--      - Terminer par : ROLLBACK;
--      - Vérifier que lignes_supprimees correspond au preview section 1.
--
-- 4) PASSAGE RÉEL :
--      - Ré-exécuter SECTIONS 1 à 3 (les comptages doivent être identiques au dry-run).
--      - SECTION 4 : même DELETE, puis COMMIT;
--
-- Alternative automatisée (garde-fou Paiements + dry-run) :
--      CALL sp_delete_clientfactures_2026_05_inactifs(1);  -- dry-run → ROLLBACK
--      CALL sp_delete_clientfactures_2026_05_inactifs(0);  -- réel    → COMMIT
--
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SECTION 0 — Paramètres (modifier ici uniquement si besoin)
-- ----------------------------------------------------------------------------
SET @annee_cible   := 2026;
SET @mois_pad      := '05';
SET @mois_sans_pad := '5';

SELECT
    'SECTION 0 — Paramètres' AS Etape,
    @annee_cible   AS annee_cible,
    @mois_pad      AS mois_pad,
    @mois_sans_pad AS mois_sans_pad,
    DATABASE()     AS base_courante,
    NOW()          AS execute_le;


-- ----------------------------------------------------------------------------
-- SECTION 1 — Preview / comptages (LECTURE SEULE)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 1a — Synthèse des lignes ciblées' AS Etape;

SELECT
    COUNT(*) AS nb_clientfactures_cibles,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_distincts,
    SUM(COALESCE(cf.Montant, 0)) AS somme_montant,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye,
    SUM(COALESCE(cf.MontantDu, 0)) AS somme_montant_du,
    SUM(CASE WHEN cf.Mois = @mois_pad THEN 1 ELSE 0 END) AS nb_mois_format_05,
    SUM(CASE WHEN cf.Mois = @mois_sans_pad THEN 1 ELSE 0 END) AS nb_mois_format_5,
    SUM(CASE WHEN cf.MontantPaye > 0 THEN 1 ELSE 0 END) AS nb_avec_montant_paye_positif
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0;


SELECT 'SECTION 1b — Contrôle négatif (clients IsActif = 1, même période)' AS Etape;
-- Doit retourner 0 lignes si le filtre métier est correct.

SELECT COUNT(*) AS nb_lignes_actifs_meme_periode_a_ne_pas_supprimer
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 1;


SELECT 'SECTION 1c — Répartition par IdFacture (top 20)' AS Etape;

SELECT
    cf.IdFacture,
    COUNT(*) AS nb_lignes_client,
    SUM(COALESCE(cf.MontantDu, 0)) AS total_montant_du
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0
GROUP BY cf.IdFacture
ORDER BY nb_lignes_client DESC
LIMIT 20;


-- ----------------------------------------------------------------------------
-- SECTION 2 — Détail échantillon (LECTURE SEULE, TOP 50)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 2 — Échantillon des lignes à supprimer' AS Etape;

SELECT
    cf.IdClientFacture,
    cf.IdClient,
    c.NomClient,
    c.CodeCons,
    c.IsActif,
    c.Statut AS client_statut,
    cf.IdFacture,
    cf.Mois,
    cf.Annees,
    cf.Montant,
    cf.MontantPaye,
    cf.MontantDu,
    cf.Statut AS cf_statut,
    cf.EstArrierePreExistant,
    cf.DateCreation
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0
ORDER BY cf.IdClientFacture
LIMIT 50;


-- ----------------------------------------------------------------------------
-- SECTION 3 — Garde-fou Paiements (LECTURE SEULE, BLOQUANT)
-- ----------------------------------------------------------------------------
-- Si nb_paiements_bloquants > 0 : NE PAS exécuter la SECTION 4.
-- Traiter les paiements manuellement puis relancer ce script.

SELECT 'SECTION 3a — Nombre de paiements bloquants' AS Etape;

SELECT COUNT(*) AS nb_paiements_bloquants
FROM Paiements p
INNER JOIN ClientFactures cf ON cf.IdClientFacture = p.IdClientFacture
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0;


SELECT 'SECTION 3b — Détail des paiements bloquants (si présents)' AS Etape;

SELECT
    p.IdPaiement,
    p.IdClientFacture,
    p.IdClient,
    p.IdFacture,
    p.MontantPaye,
    p.DatePaiement,
    p.Statut AS paiement_statut,
    cf.Mois,
    cf.Annees,
    c.NomClient,
    c.IsActif
FROM Paiements p
INNER JOIN ClientFactures cf ON cf.IdClientFacture = p.IdClientFacture
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0
ORDER BY p.IdPaiement
LIMIT 100;


SELECT 'SECTION 3c — Verdict garde-fou' AS Etape;

SELECT
    CASE
        WHEN (
            SELECT COUNT(*)
            FROM Paiements p
            INNER JOIN ClientFactures cf ON cf.IdClientFacture = p.IdClientFacture
            INNER JOIN Clients c ON c.IdClient = cf.IdClient
            WHERE cf.Annees = @annee_cible
              AND cf.Mois IN (@mois_pad, @mois_sans_pad)
              AND c.IsActif = 0
        ) > 0
        THEN 'ARRET — Paiements liés : ne pas exécuter la SECTION 4'
        ELSE 'OK — Aucun paiement lié : SECTION 4 autorisée (dry-run ROLLBACK d''abord)'
    END AS verdict;


-- ----------------------------------------------------------------------------
-- SECTION 4 — Suppression transactionnelle (MANUELLE)
-- ----------------------------------------------------------------------------
-- Prérequis : SECTION 3c = 'OK ...'
--
-- DRY-RUN  : exécuter le bloc ci-dessous puis ROLLBACK;
-- PRODUCTION : exécuter le bloc ci-dessous puis COMMIT;

START TRANSACTION;

-- Revérification immédiate (évite une course entre preview et delete)
SELECT COUNT(*) INTO @nb_paiements_bloquants_tx
FROM Paiements p
INNER JOIN ClientFactures cf ON cf.IdClientFacture = p.IdClientFacture
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0;

SELECT @nb_paiements_bloquants_tx AS nb_paiements_bloquants_dans_transaction;

-- Si > 0, exécuter ROLLBACK; et STOP (ne pas lancer le DELETE ci-dessous).

DELETE cf
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0;

SELECT ROW_COUNT() AS lignes_supprimees;

-- Post-contrôle : doit être 0
SELECT COUNT(*) AS nb_restantes_apres_delete
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
WHERE cf.Annees = @annee_cible
  AND cf.Mois IN (@mois_pad, @mois_sans_pad)
  AND c.IsActif = 0;

-- >>> DRY-RUN (1er passage) :
ROLLBACK;

-- >>> PRODUCTION (2e passage, après validation) :
-- COMMIT;


-- ----------------------------------------------------------------------------
-- SECTION 4b — Procédure stockée (optionnelle, garde-fou automatique)
-- ----------------------------------------------------------------------------
-- p_dry_run = 1 → DELETE puis ROLLBACK
-- p_dry_run = 0 → DELETE puis COMMIT (uniquement si aucun paiement lié)

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_delete_clientfactures_2026_05_inactifs$$

CREATE PROCEDURE sp_delete_clientfactures_2026_05_inactifs(IN p_dry_run TINYINT)
BEGIN
    DECLARE v_nb_paiements INT DEFAULT 0;
    DECLARE v_nb_avant INT DEFAULT 0;
    DECLARE v_lignes_supprimees INT DEFAULT 0;

    SET @annee_cible   := 2026;
    SET @mois_pad      := '05';
    SET @mois_sans_pad := '5';

    SELECT COUNT(*) INTO v_nb_paiements
    FROM Paiements p
    INNER JOIN ClientFactures cf ON cf.IdClientFacture = p.IdClientFacture
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
      AND c.IsActif = 0;

    IF v_nb_paiements > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Suppression annulée : au moins un Paiement est lié à une ClientFacture cible.';
    END IF;

    SELECT COUNT(*) INTO v_nb_avant
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
      AND c.IsActif = 0;

    START TRANSACTION;

    DELETE cf
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
      AND c.IsActif = 0;

    SET v_lignes_supprimees = ROW_COUNT();

    IF p_dry_run = 1 THEN
        ROLLBACK;
        SELECT
            'DRY-RUN' AS mode,
            v_nb_avant AS nb_cibles_avant,
            v_lignes_supprimees AS lignes_supprimees_simulees,
            'ROLLBACK effectué — aucune donnée persistée' AS resultat;
    ELSE
        COMMIT;
        SELECT
            'PRODUCTION' AS mode,
            v_nb_avant AS nb_cibles_avant,
            v_lignes_supprimees AS lignes_supprimees,
            'COMMIT effectué' AS resultat;
    END IF;
END$$

DELIMITER ;

-- Exemples d'appel (après création de la procédure) :
-- CALL sp_delete_clientfactures_2026_05_inactifs(1);
-- CALL sp_delete_clientfactures_2026_05_inactifs(0);


-- ----------------------------------------------------------------------------
-- SECTION 5 — Checklist opérateur (rappel)
-- ----------------------------------------------------------------------------
-- [ ] Backup ClientFactures + Paiements (ou base complète)
-- [ ] Sections 1–3 exécutées et validées avec le métier
-- [ ] Section 3c = OK
-- [ ] Dry-run SECTION 4 avec ROLLBACK
-- [ ] Passage réel avec COMMIT
-- [ ] Contrôle applicatif (arriérés consolidés, statistiques mai 2026)

SELECT 'Fin du script — vérifier la checklist SECTION 5' AS Etape;
