#!/usr/bin/env python3
"""
Génère depuis « LISTE DES FACTURES CLIENT A REGULARISER.xlsx » :
  - Scripts/data_codecons_regularisation_2026_02_03_04.sql
  - Scripts/01_regularisation_preview_avant_2026_02_03_04.sql
  - Scripts/02_regularisation_appliquer_2026_02_03_04.sql
  - Scripts/03_regularisation_preview_apres_2026_02_03_04.sql
  - Scripts/production_regulariser_clientfactures_2026_02_03_04.sql (monolithique, archive)

Usage (depuis la racine du repo) :
    python3 Scripts/generate_codecons_regularisation_data.py
"""
from __future__ import annotations

import re
import zipfile
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
EXCEL_PATH = REPO_ROOT / "LISTE DES FACTURES CLIENT A REGULARISER.xlsx"
DATA_OUTPUT_PATH = REPO_ROOT / "Scripts" / "data_codecons_regularisation_2026_02_03_04.sql"
PROD_OUTPUT_PATH = REPO_ROOT / "Scripts" / "production_regulariser_clientfactures_2026_02_03_04.sql"
SCRIPT_01_PATH = REPO_ROOT / "Scripts" / "01_regularisation_preview_avant_2026_02_03_04.sql"
SCRIPT_02_PATH = REPO_ROOT / "Scripts" / "02_regularisation_appliquer_2026_02_03_04.sql"
SCRIPT_03_PATH = REPO_ROOT / "Scripts" / "03_regularisation_preview_apres_2026_02_03_04.sql"
TEMPLATES_DIR = REPO_ROOT / "Scripts" / "templates" / "regularisation"
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}

MONTH_HEADERS = {
    "avril": "MontantCibleAvril",
    "mars": "MontantCibleMars",
    "fevrier": "MontantCibleFevrier",
}


@dataclass
class RegularisationRow:
    code_cons: str
    montant_avril: str
    montant_mars: str
    montant_fevrier: str

    @property
    def est_format_suspect(self) -> bool:
        return self.code_cons.count("/") < 2


def sql_escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace("'", "''")


def col_to_idx(col: str) -> int:
    idx = 0
    for ch in col:
        idx = idx * 26 + (ord(ch) - 64)
    return idx - 1


def read_regularisation_rows(path: Path) -> list[RegularisationRow]:
    with zipfile.ZipFile(path) as zf:
        strings_root = ET.fromstring(zf.read("xl/sharedStrings.xml"))
        strings = [
            "".join((t.text or "") for t in si.findall(".//m:t", NS))
            for si in strings_root.findall(".//m:si", NS)
        ]
        sheet_root = ET.fromstring(zf.read("xl/worksheets/sheet1.xml"))
        parsed_rows: list[list[str]] = []
        max_col = 0

        for row in sheet_root.findall(".//m:row", NS):
            cells: dict[int, str] = {}
            for cell in row.findall("m:c", NS):
                ref = cell.get("r", "")
                match = re.match(r"([A-Z]+)", ref)
                if not match:
                    continue
                ci = col_to_idx(match.group(1))
                max_col = max(max_col, ci)
                value_el = cell.find("m:v", NS)
                if value_el is None or value_el.text is None:
                    continue
                raw = value_el.text
                if cell.get("t") == "s":
                    raw = strings[int(raw)]
                cells[ci] = str(raw).strip()

            if not cells:
                continue

            arr = [""] * (max_col + 1)
            for ci, value in cells.items():
                arr[ci] = value
            parsed_rows.append(arr)

    if not parsed_rows:
        raise SystemExit("Excel vide.")

    header = [h.lower() for h in parsed_rows[0]]
    month_indexes = {}
    for excel_name, sql_name in MONTH_HEADERS.items():
        try:
            month_indexes[sql_name] = header.index(excel_name)
        except ValueError as exc:
            raise SystemExit(f"Colonne mois '{excel_name}' introuvable.") from exc

    rows: list[RegularisationRow] = []
    for raw in parsed_rows[1:]:
        code = raw[0].strip() if raw else ""
        if not code:
            continue
        rows.append(
            RegularisationRow(
                code_cons=code,
                montant_avril=raw[month_indexes["MontantCibleAvril"]]
                if month_indexes["MontantCibleAvril"] < len(raw)
                else "0",
                montant_mars=raw[month_indexes["MontantCibleMars"]]
                if month_indexes["MontantCibleMars"] < len(raw)
                else "0",
                montant_fevrier=raw[month_indexes["MontantCibleFevrier"]]
                if month_indexes["MontantCibleFevrier"] < len(raw)
                else "0",
            )
        )

    deduped: dict[str, RegularisationRow] = {}
    for row in rows:
        deduped.setdefault(row.code_cons, row)
    return sorted(deduped.values(), key=lambda item: item.code_cons.lower())


def build_insert_sql(entries: list[RegularisationRow]) -> str:
    suspect = [e.code_cons for e in entries if e.est_format_suspect]
    lines = [
        "-- ============================================================================",
        "-- Données générées — régularisation ClientFactures Fév/Mar/Avr 2026",
        f"-- Source : {EXCEL_PATH.name}",
        f"-- Lignes uniques : {len(entries)}",
        "-- ============================================================================",
        "-- Prérequis : tmp_codecons_regularisation doit exister (SECTION 0 du script prod).",
        "--",
    ]
    if suspect:
        lines.append(
            f"-- ALERTE format suspect ({len(suspect)}) : {', '.join(suspect)}"
        )
        lines.append(
            "--   Ex. G2/0142 → vérifier si le code attendu est G/G2/0142 en base."
        )
        lines.append("--")

    lines.append(
        "INSERT INTO tmp_codecons_regularisation "
        "(CodeCons, MontantCibleAvril, MontantCibleMars, MontantCibleFevrier, EstFormatSuspect) VALUES"
    )
    value_lines = []
    for entry in entries:
        suspect_flag = 1 if entry.est_format_suspect else 0
        value_lines.append(
            "    ("
            f"'{sql_escape(entry.code_cons)}', "
            f"{entry.montant_avril or '0'}, "
            f"{entry.montant_mars or '0'}, "
            f"{entry.montant_fevrier or '0'}, "
            f"{suspect_flag}"
            ")"
        )
    lines.append(",\n".join(value_lines) + ";")
    lines.append("")
    return "\n".join(lines)


def build_values_clause(entries: list[RegularisationRow]) -> str:
    value_lines = []
    for entry in entries:
        suspect_flag = 1 if entry.est_format_suspect else 0
        value_lines.append(
            "    ("
            f"'{sql_escape(entry.code_cons)}', "
            f"{entry.montant_avril or '0'}, "
            f"{entry.montant_mars or '0'}, "
            f"{entry.montant_fevrier or '0'}, "
            f"{suspect_flag}"
            ")"
        )
    return ",\n".join(value_lines)


def build_production_sql(entries: list[RegularisationRow], values_clause: str) -> str:
    nb = len(entries)
    suspect_codes = [e.code_cons for e in entries if e.est_format_suspect]
    suspect_note = (
        f"-- Codes format suspect dans l'Excel : {', '.join(suspect_codes)}"
        if suspect_codes
        else "-- Aucun code format suspect détecté."
    )

    join_codecons = "LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))"
    cf_period = (
        "cf.Annees = @annee_cible\n"
        "  AND cf.Mois IN (@mois_fev_pad, @mois_fev_sp, @mois_mar_pad, @mois_mar_sp, @mois_avr_pad, @mois_avr_sp)"
    )
    cf_eligible = (
        f"{cf_period}\n"
        "  AND cf.Statut = 1\n"
        "  AND COALESCE(cf.MontantPaye, 0) = 0\n"
        "  AND NOT EXISTS (\n"
        "      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture\n"
        "  )"
    )
    cf_blocked = (
        f"{cf_period}\n"
        "  AND cf.Statut = 1\n"
        "  AND (\n"
        "      COALESCE(cf.MontantPaye, 0) > 0\n"
        "      OR EXISTS (\n"
        "          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture\n"
        "      )\n"
        "  )"
    )
    cf_join = f"""
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON {join_codecons}"""
    cf_update_join = f"""UPDATE ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_regularisation t ON {join_codecons}"""

    return f"""-- ============================================================================
-- Script PRODUCTION : régularisation ClientFactures — Fév/Mar/Avr 2026
-- ============================================================================
-- Objectif :
--   Remettre Montant = 0 et MontantDu = 0 sur les ClientFactures
--   pour Annees = 2026, Mois Février (02/2), Mars (03/3), Avril (04/4),
--   pour les clients dont le CodeCons figure dans
--   « LISTE DES FACTURES CLIENT A REGULARISER.xlsx » ({nb} codes uniques).
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
{suspect_note}
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

-- Charger les {nb} CodeCons (généré depuis l'Excel) :
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
INNER JOIN Clients c ON {join_codecons}
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
LEFT JOIN Clients c ON {join_codecons}
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
INNER JOIN Clients c ON {join_codecons};

SELECT COUNT(*) INTO @nb_codecons_absents
FROM tmp_codecons_regularisation t
LEFT JOIN Clients c ON {join_codecons}
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
{cf_join}
WHERE {cf_eligible};


SELECT 'SECTION 2b — Lignes BLOQUÉES (paiement ou MontantPaye > 0)' AS Etape;

SELECT
    COUNT(*) AS nb_cf_bloquees,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_bloques,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye
{cf_join}
WHERE {cf_blocked};


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
{cf_join}
WHERE {cf_blocked}
ORDER BY c.CodeCons, cf.Mois, cf.IdClientFacture
LIMIT 50;


SELECT 'SECTION 2c — Clients trouvés SANS ClientFacture Fév/Mar/Avr 2026' AS Etape;

SELECT
    c.IdClient,
    t.CodeCons,
    c.NomClient
FROM tmp_codecons_regularisation t
INNER JOIN Clients c ON {join_codecons}
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
{cf_join}
WHERE {cf_eligible}
ORDER BY c.CodeCons, cf.Mois, cf.IdClientFacture
LIMIT 50;


-- ----------------------------------------------------------------------------
-- SECTION 3 — Vérification finale (LECTURE SEULE)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 3a — Exclues : MontantPaye > 0' AS Etape;

SELECT
    COUNT(*) AS nb_exclues_montant_paye_positif,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye
{cf_join}
WHERE {cf_period}
  AND cf.Statut = 1
  AND COALESCE(cf.MontantPaye, 0) > 0;


SELECT 'SECTION 3b — Exclues : au moins un Paiement lié' AS Etape;

SELECT
    COUNT(DISTINCT cf.IdClientFacture) AS nb_exclues_avec_paiement
{cf_join}
WHERE {cf_period}
  AND cf.Statut = 1
  AND EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT COUNT(*) INTO @v_nb_absents
FROM tmp_codecons_regularisation t
LEFT JOIN Clients c ON {join_codecons}
WHERE c.IdClient IS NULL;

SELECT COUNT(*) INTO @v_nb_doublons
FROM (
    SELECT LOWER(TRIM(c.CodeCons))
    FROM Clients c
    INNER JOIN tmp_codecons_regularisation t ON {join_codecons}
    GROUP BY LOWER(TRIM(c.CodeCons))
    HAVING COUNT(*) > 1
) d;

SELECT COUNT(*) INTO @v_nb_eligibles
{cf_join}
WHERE {cf_eligible};

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
{cf_join}
WHERE {cf_eligible}
  AND (COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0);

SELECT @nb_eligibles_tx AS nb_lignes_a_modifier_dans_transaction;

{cf_update_join}
SET
    cf.Montant = 0,
    cf.MontantDu = 0,
    cf.DateModification = NOW()
WHERE {cf_eligible}
  AND (COALESCE(cf.Montant, 0) <> 0 OR COALESCE(cf.MontantDu, 0) <> 0);

SELECT ROW_COUNT() AS lignes_modifiees;

SELECT COUNT(*) AS nb_restantes_non_zero_eligibles
{cf_join}
WHERE {cf_eligible}
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
{values_clause};

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
"""


def build_suspect_header(suspect_codes: list[str]) -> str:
    if not suspect_codes:
        return "-- Aucun code format suspect détecté dans l'Excel."
    return f"-- Codes format suspect dans l'Excel : {', '.join(suspect_codes)}"


def render_template(template_name: str, nb: int, suspect_header: str) -> str:
    template_path = TEMPLATES_DIR / template_name
    if not template_path.exists():
        raise SystemExit(f"Template introuvable : {template_path}")
    content = template_path.read_text(encoding="utf-8")
    return (
        content.replace("__NB_CODECONS__", str(nb))
        .replace("__SUSPECT_HEADER__", suspect_header)
    )


def main() -> None:
    if not EXCEL_PATH.exists():
        raise SystemExit(f"Fichier Excel introuvable : {EXCEL_PATH}")

    entries = read_regularisation_rows(EXCEL_PATH)
    values_clause = build_values_clause(entries)
    suspect = [e.code_cons for e in entries if e.est_format_suspect]
    suspect_header = build_suspect_header(suspect)
    nb = len(entries)

    DATA_OUTPUT_PATH.write_text(build_insert_sql(entries), encoding="utf-8")
    PROD_OUTPUT_PATH.write_text(
        build_production_sql(entries, values_clause), encoding="utf-8"
    )
    SCRIPT_01_PATH.write_text(
        render_template("01_regularisation_preview_avant_2026_02_03_04.sql", nb, suspect_header),
        encoding="utf-8",
    )
    SCRIPT_02_PATH.write_text(
        render_template("02_regularisation_appliquer_2026_02_03_04.sql", nb, suspect_header),
        encoding="utf-8",
    )
    SCRIPT_03_PATH.write_text(
        render_template("03_regularisation_preview_apres_2026_02_03_04.sql", nb, suspect_header),
        encoding="utf-8",
    )

    print(f"Écrit {nb} CodeCons → {DATA_OUTPUT_PATH}")
    print(f"Écrit script 01 (avant)  → {SCRIPT_01_PATH}")
    print(f"Écrit script 02 (apply)  → {SCRIPT_02_PATH}")
    print(f"Écrit script 03 (après)  → {SCRIPT_03_PATH}")
    print(f"Écrit script monolithique → {PROD_OUTPUT_PATH}")
    if suspect:
        print(f"Alerte format suspect ({len(suspect)}) : {', '.join(suspect)}")


if __name__ == "__main__":
    main()
