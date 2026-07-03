#!/usr/bin/env python3
"""
Génère depuis « Factures non conforme MAI 2.xlsx » :
  - Scripts/data_codecons_mai2026_non_conformes.sql
  - Scripts/production_delete_clientfactures_2026_05_par_codecons_excel.sql

Usage (depuis la racine du repo) :
    python3 Scripts/generate_codecons_mai2026_data.py
"""
from __future__ import annotations

import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
EXCEL_PATH = REPO_ROOT / "Factures non conforme MAI 2.xlsx"
DATA_OUTPUT_PATH = REPO_ROOT / "Scripts" / "data_codecons_mai2026_non_conformes.sql"
PROD_OUTPUT_PATH = (
    REPO_ROOT / "Scripts" / "production_delete_clientfactures_2026_05_par_codecons_excel.sql"
)
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}


def sql_escape(value: str) -> str:
    return value.replace("\\", "\\\\").replace("'", "''")


def read_excel_rows(path: Path) -> list[list[str]]:
    with zipfile.ZipFile(path) as zf:
        strings_root = ET.fromstring(zf.read("xl/sharedStrings.xml"))
        strings = [
            "".join((t.text or "") for t in si.findall(".//m:t", NS))
            for si in strings_root.findall(".//m:si", NS)
        ]
        sheet_root = ET.fromstring(zf.read("xl/worksheets/sheet1.xml"))
        rows: list[list[str]] = []
        for row in sheet_root.findall(".//m:row", NS):
            cells: list[str] = []
            for cell in row.findall("m:c", NS):
                value_el = cell.find("m:v", NS)
                if value_el is None or value_el.text is None:
                    continue
                raw = value_el.text
                if cell.get("t") == "s":
                    raw = strings[int(raw)]
                cells.append(raw.strip())
            if cells:
                rows.append(cells)
    return rows


def dedupe_by_codecons(rows: list[tuple[str, str]]) -> list[tuple[str, str]]:
    seen: dict[str, str] = {}
    for nom, code in rows:
        key = code.strip()
        if not key:
            continue
        if key not in seen:
            seen[key] = nom.strip()
    return sorted(seen.items(), key=lambda item: item[0].lower())


def build_insert_sql(entries: list[tuple[str, str]]) -> str:
    lines = [
        "-- ============================================================================",
        "-- Données générées — liste CodeCons (mai 2026, factures non conformes)",
        f"-- Source : {EXCEL_PATH.name}",
        f"-- Lignes uniques : {len(entries)}",
        "-- ============================================================================",
        "-- Prérequis : tmp_codecons_cibles doit exister (SECTION 0 du script prod).",
        "--",
        "INSERT INTO tmp_codecons_cibles (CodeCons, NomClientExcel) VALUES",
    ]
    value_lines = []
    for code, nom in entries:
        value_lines.append(f"    ('{sql_escape(code)}', '{sql_escape(nom)}')")
    lines.append(",\n".join(value_lines) + ";")
    lines.append("")
    return "\n".join(lines)


def build_values_clause(entries: list[tuple[str, str]]) -> str:
    value_lines = [
        f"    ('{sql_escape(code)}', '{sql_escape(nom)}')" for code, nom in entries
    ]
    return ",\n".join(value_lines)


def build_production_sql(entries: list[tuple[str, str]], values_clause: str) -> str:
    nb = len(entries)
    join_codecons = (
        "LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))"
    )
    cf_period = (
        "cf.Annees = @annee_cible\n"
        "  AND cf.Mois IN (@mois_pad, @mois_sans_pad)"
    )
    cf_eligible = (
        f"{cf_period}\n"
        "  AND COALESCE(cf.MontantPaye, 0) = 0\n"
        "  AND NOT EXISTS (\n"
        "      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture\n"
        "  )"
    )
    cf_blocked = (
        f"{cf_period}\n"
        "  AND (\n"
        "      COALESCE(cf.MontantPaye, 0) > 0\n"
        "      OR EXISTS (\n"
        "          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture\n"
        "      )\n"
        "  )"
    )

    return f"""-- ============================================================================
-- Script PRODUCTION : suppression ClientFactures — mai 2026, liste Excel CodeCons
-- ============================================================================
-- Objectif :
--   Supprimer les ClientFactures pour Annees = 2026, Mois = '05' ou '5',
--   pour les clients dont le CodeCons figure dans le fichier Excel
--   « Factures non conforme MAI 2.xlsx » ({nb} codes uniques après dédoublonnage).
--
-- Relation :
--   ClientFactures.IdClient → Clients.IdClient
--   Filtre métier           → Clients.CodeCons (matching TRIM + LOWER)
--   NomClient Excel         → contrôle qualité uniquement (section 2d)
--
-- Périmètre suppression :
--   - Uniquement mai 2026 (Mois 05/5)
--   - Uniquement lignes avec COALESCE(MontantPaye, 0) = 0
--   - Uniquement lignes SANS Paiement lié (Paiements.IdClientFacture)
--
-- NE SUPPRIME PAS : Factures, Clients, Paiements
--
-- ============================================================================
-- PROCÉDURE D'EXÉCUTION (OBLIGATOIRE)
-- ============================================================================
--
-- 1) Backup :
--      mysqldump -u ... -p ... ClientFactures Paiements > backup_cf_excel_YYYYMMDD.sql
--
-- 2) (Re)générer les données si l'Excel change :
--      python3 Scripts/generate_codecons_mai2026_data.py
--
-- 3) Exécuter SECTIONS 0 à 3 (lecture seule) — valider :
--      - nb CodeCons résolus (section 1a)
--      - CodeCons absents listés (section 1b) — corriger si besoin
--      - totaux section 2 = lignes réellement supprimables
--      - section 2b : lignes bloquées (paiement) acceptées par le métier
--      - section 2c : clients sans CF mai 2026
--      - section 2d : mismatches NomClient (warning)
--
-- 4) DRY-RUN : SECTION 4 + ROLLBACK;
-- 5) PRODUCTION : SECTION 4 + COMMIT;
--
-- Alternative procédure stockée (recrée la liste en interne) :
--      CALL sp_delete_clientfactures_2026_05_par_codecons_excel(1);  -- dry-run
--      CALL sp_delete_clientfactures_2026_05_par_codecons_excel(0);  -- réel
--
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SECTION 0 — Paramètres et table staging CodeCons
-- ----------------------------------------------------------------------------
SET @annee_cible   := 2026;
SET @mois_pad      := '05';
SET @mois_sans_pad := '5';

DROP TEMPORARY TABLE IF EXISTS tmp_codecons_cibles;
CREATE TEMPORARY TABLE tmp_codecons_cibles (
    CodeCons       VARCHAR(100) NOT NULL PRIMARY KEY,
    NomClientExcel VARCHAR(255) NULL
);

-- Charger les {nb} CodeCons (généré depuis l'Excel) :
SOURCE Scripts/data_codecons_mai2026_non_conformes.sql;
-- Si SOURCE indisponible, exécuter manuellement le fichier data ci-dessus.

SELECT
    'SECTION 0 — Paramètres' AS Etape,
    @annee_cible   AS annee_cible,
    @mois_pad      AS mois_pad,
    @mois_sans_pad AS mois_sans_pad,
    (SELECT COUNT(*) FROM tmp_codecons_cibles) AS nb_codecons_charges,
    DATABASE()     AS base_courante,
    NOW()          AS execute_le;


-- ----------------------------------------------------------------------------
-- SECTION 1 — Résolution CodeCons → Client (LECTURE SEULE)
-- ----------------------------------------------------------------------------

SELECT 'SECTION 1a — Clients résolus (CodeCons → IdClient)' AS Etape;

SELECT
    t.CodeCons,
    t.NomClientExcel,
    c.IdClient,
    c.NomClient AS nom_client_base,
    c.IsActif,
    c.Statut AS client_statut
FROM tmp_codecons_cibles t
INNER JOIN Clients c ON {join_codecons}
ORDER BY t.CodeCons;


SELECT 'SECTION 1b — CodeCons Excel ABSENTS en base' AS Etape;

SELECT
    t.CodeCons,
    t.NomClientExcel
FROM tmp_codecons_cibles t
LEFT JOIN Clients c ON {join_codecons}
WHERE c.IdClient IS NULL
ORDER BY t.CodeCons;


SELECT 'SECTION 1c — Doublons CodeCons en base (alerte si > 0)' AS Etape;

SELECT
    LOWER(TRIM(c.CodeCons)) AS codecons_normalise,
    COUNT(*) AS nb_clients_meme_code,
    GROUP_CONCAT(c.IdClient ORDER BY c.IdClient) AS id_clients_concernes
FROM Clients c
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
GROUP BY LOWER(TRIM(c.CodeCons))
HAVING COUNT(*) > 1;


SELECT 'SECTION 1d — Synthèse résolution' AS Etape;

SELECT
    (SELECT COUNT(*) FROM tmp_codecons_cibles) AS nb_codecons_excel,
    (SELECT COUNT(DISTINCT LOWER(TRIM(t.CodeCons)))
     FROM tmp_codecons_cibles t
     INNER JOIN Clients c ON {join_codecons}) AS nb_codecons_resolus,
    (SELECT COUNT(*)
     FROM tmp_codecons_cibles t
     LEFT JOIN Clients c ON {join_codecons}
     WHERE c.IdClient IS NULL) AS nb_codecons_absents;


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
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_eligible};


SELECT 'SECTION 2b — Lignes BLOQUÉES (paiement ou MontantPaye > 0)' AS Etape;

SELECT
    COUNT(*) AS nb_cf_bloquees,
    COUNT(DISTINCT cf.IdClient) AS nb_clients_bloques,
    SUM(COALESCE(cf.MontantPaye, 0)) AS somme_montant_paye
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_blocked};


SELECT 'SECTION 2b détail — TOP 50 lignes bloquées' AS Etape;

SELECT
    cf.IdClientFacture,
    c.CodeCons,
    c.NomClient,
    t.NomClientExcel,
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
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_blocked}
ORDER BY c.CodeCons, cf.IdClientFacture
LIMIT 50;


SELECT 'SECTION 2c — Clients trouvés SANS ClientFacture mai 2026' AS Etape;

SELECT
    c.IdClient,
    t.CodeCons,
    c.NomClient,
    t.NomClientExcel
FROM tmp_codecons_cibles t
INNER JOIN Clients c ON {join_codecons}
WHERE NOT EXISTS (
    SELECT 1
    FROM ClientFactures cf
    WHERE cf.IdClient = c.IdClient
      AND cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
)
ORDER BY t.CodeCons;


SELECT 'SECTION 2d — Mismatch NomClient Excel vs base (warning)' AS Etape;

SELECT
    t.CodeCons,
    t.NomClientExcel,
    c.NomClient AS nom_client_base,
    c.IdClient
FROM tmp_codecons_cibles t
INNER JOIN Clients c ON {join_codecons}
WHERE UPPER(TRIM(COALESCE(c.NomClient, ''))) <> UPPER(TRIM(COALESCE(t.NomClientExcel, '')))
ORDER BY t.CodeCons;


SELECT 'SECTION 2e — Échantillon TOP 50 lignes éligibles' AS Etape;

SELECT
    cf.IdClientFacture,
    cf.IdClient,
    c.NomClient,
    c.CodeCons,
    t.NomClientExcel,
    c.IsActif,
    cf.IdFacture,
    cf.Mois,
    cf.Annees,
    cf.Montant,
    cf.MontantPaye,
    cf.MontantDu,
    cf.Statut AS cf_statut
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_eligible}
ORDER BY c.CodeCons, cf.IdClientFacture
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
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_period}
  AND COALESCE(cf.MontantPaye, 0) > 0;


SELECT 'SECTION 3b — Exclues : au moins un Paiement lié' AS Etape;

SELECT
    COUNT(DISTINCT cf.IdClientFacture) AS nb_exclues_avec_paiement
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_period}
  AND EXISTS (
      SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
  );


SELECT 'SECTION 3c — Verdict' AS Etape;

SELECT
    (SELECT COUNT(*) FROM tmp_codecons_cibles t
     LEFT JOIN Clients c ON {join_codecons}
     WHERE c.IdClient IS NULL) AS nb_codecons_absents,
    (SELECT COUNT(*) FROM (
        SELECT LOWER(TRIM(c.CodeCons))
        FROM Clients c
        INNER JOIN tmp_codecons_cibles t ON {join_codecons}
        GROUP BY LOWER(TRIM(c.CodeCons))
        HAVING COUNT(*) > 1
     ) d) AS nb_codecons_en_doublon_base,
    (SELECT COUNT(*) FROM ClientFactures cf
     INNER JOIN Clients c ON c.IdClient = cf.IdClient
     INNER JOIN tmp_codecons_cibles t ON {join_codecons}
     WHERE {cf_eligible}) AS nb_cf_eligibles,
    CASE
        WHEN (SELECT COUNT(*) FROM (
            SELECT LOWER(TRIM(c.CodeCons))
            FROM Clients c
            INNER JOIN tmp_codecons_cibles t ON {join_codecons}
            GROUP BY LOWER(TRIM(c.CodeCons))
            HAVING COUNT(*) > 1
        ) d) > 0
        THEN 'ARRET — doublon CodeCons en base : trancher avant DELETE'
        WHEN (SELECT COUNT(*) FROM ClientFactures cf
              INNER JOIN Clients c ON c.IdClient = cf.IdClient
              INNER JOIN tmp_codecons_cibles t ON {join_codecons}
              WHERE {cf_eligible}) = 0
        THEN 'ARRET — aucune ligne éligible à supprimer'
        ELSE 'OK — SECTION 4 autorisée après validation métier (dry-run ROLLBACK d''abord)'
    END AS verdict;


-- ----------------------------------------------------------------------------
-- SECTION 4 — Suppression transactionnelle (MANUELLE)
-- ----------------------------------------------------------------------------
-- Prérequis : SECTION 3c verdict = OK, totaux section 2 validés
--
-- DRY-RUN  : exécuter puis ROLLBACK;
-- PRODUCTION : exécuter puis COMMIT;

START TRANSACTION;

SELECT COUNT(*) INTO @nb_eligibles_tx
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_eligible};

SELECT @nb_eligibles_tx AS nb_eligibles_dans_transaction;

DELETE cf
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_eligible};

SELECT ROW_COUNT() AS lignes_supprimees;

SELECT COUNT(*) AS nb_restantes_eligibles_apres_delete
FROM ClientFactures cf
INNER JOIN Clients c ON c.IdClient = cf.IdClient
INNER JOIN tmp_codecons_cibles t ON {join_codecons}
WHERE {cf_eligible};

-- >>> DRY-RUN (1er passage) :
ROLLBACK;

-- >>> PRODUCTION (2e passage, après validation) :
-- COMMIT;


-- ----------------------------------------------------------------------------
-- SECTION 4b — Procédure stockée (optionnelle)
-- ----------------------------------------------------------------------------
-- Recrée tmp_codecons_cibles et charge la liste en interne.
-- p_dry_run=1 → ROLLBACK, 0 → COMMIT.

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_delete_clientfactures_2026_05_par_codecons_excel$$

CREATE PROCEDURE sp_delete_clientfactures_2026_05_par_codecons_excel(IN p_dry_run TINYINT)
BEGIN
    DECLARE v_nb_doublons INT DEFAULT 0;
    DECLARE v_nb_avant INT DEFAULT 0;
    DECLARE v_lignes_supprimees INT DEFAULT 0;

    SET @annee_cible   := 2026;
    SET @mois_pad      := '05';
    SET @mois_sans_pad := '5';

    DROP TEMPORARY TABLE IF EXISTS tmp_codecons_cibles;
    CREATE TEMPORARY TABLE tmp_codecons_cibles (
        CodeCons       VARCHAR(100) NOT NULL PRIMARY KEY,
        NomClientExcel VARCHAR(255) NULL
    );

    INSERT INTO tmp_codecons_cibles (CodeCons, NomClientExcel) VALUES
{values_clause};

    SELECT COUNT(*) INTO v_nb_doublons
    FROM (
        SELECT LOWER(TRIM(c.CodeCons))
        FROM Clients c
        INNER JOIN tmp_codecons_cibles t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
        GROUP BY LOWER(TRIM(c.CodeCons))
        HAVING COUNT(*) > 1
    ) d;

    IF v_nb_doublons > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Suppression annulée : doublon CodeCons en base pour au moins un code cible.';
    END IF;

    SELECT COUNT(*) INTO v_nb_avant
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    INNER JOIN tmp_codecons_cibles t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
    WHERE cf.Annees = @annee_cible
      AND cf.Mois IN (@mois_pad, @mois_sans_pad)
      AND COALESCE(cf.MontantPaye, 0) = 0
      AND NOT EXISTS (
          SELECT 1 FROM Paiements p WHERE p.IdClientFacture = cf.IdClientFacture
      );

    IF v_nb_avant = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Suppression annulée : aucune ClientFacture éligible trouvée.';
    END IF;

    START TRANSACTION;

    DELETE cf
    FROM ClientFactures cf
    INNER JOIN Clients c ON c.IdClient = cf.IdClient
    INNER JOIN tmp_codecons_cibles t ON LOWER(TRIM(c.CodeCons)) = LOWER(TRIM(t.CodeCons))
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

    DROP TEMPORARY TABLE IF EXISTS tmp_codecons_cibles;
END$$

DELIMITER ;

-- CALL sp_delete_clientfactures_2026_05_par_codecons_excel(1);
-- CALL sp_delete_clientfactures_2026_05_par_codecons_excel(0);


-- ----------------------------------------------------------------------------
-- SECTION 5 — Checklist opérateur
-- ----------------------------------------------------------------------------
-- [ ] Backup ClientFactures + Paiements
-- [ ] python3 Scripts/generate_codecons_mai2026_data.py (si Excel mis à jour)
-- [ ] Section 1b : CodeCons absents revus avec le métier
-- [ ] Section 1c : 0 doublon CodeCons en base (ou validé)
-- [ ] Section 2a : totaux éligibles validés
-- [ ] Section 2b : lignes bloquées (payées) acceptées
-- [ ] Section 2c : clients sans CF mai 2026 notés
-- [ ] Section 2d : mismatches NomClient notés
-- [ ] Dry-run SECTION 4 avec ROLLBACK
-- [ ] Passage réel avec COMMIT
-- [ ] Contrôle arriérés consolidés / relances mai 2026

SELECT 'Fin du script — vérifier la checklist SECTION 5' AS Etape;
"""


def main() -> None:
    if not EXCEL_PATH.exists():
        raise SystemExit(f"Fichier Excel introuvable : {EXCEL_PATH}")

    raw_rows = read_excel_rows(EXCEL_PATH)
    if not raw_rows:
        raise SystemExit("Excel vide.")

    header = [h.lower() for h in raw_rows[0]]
    try:
        idx_nom = header.index("nomclient")
        idx_code = header.index("codecons")
    except ValueError as exc:
        raise SystemExit("Colonnes Nomclient / codecons introuvables.") from exc

    pairs: list[tuple[str, str]] = []
    for row in raw_rows[1:]:
        if len(row) <= max(idx_nom, idx_code):
            continue
        nom, code = row[idx_nom], row[idx_code]
        if code:
            pairs.append((nom, code))

    entries = dedupe_by_codecons(pairs)
    values_clause = build_values_clause(entries)

    DATA_OUTPUT_PATH.write_text(build_insert_sql(entries), encoding="utf-8")
    PROD_OUTPUT_PATH.write_text(
        build_production_sql(entries, values_clause), encoding="utf-8"
    )
    print(f"Écrit {len(entries)} CodeCons uniques → {DATA_OUTPUT_PATH}")
    print(f"Écrit script production → {PROD_OUTPUT_PATH}")


if __name__ == "__main__":
    main()
