-- =====================================================
-- SCRIPT DE MIGRATION: AJOUT DU TYPE DE COURANT
-- =====================================================
-- Base de données: Kenergie
-- Auteur: Cascade AI
-- Date: 2026-04-03
-- Description: Création du modèle TypeDeCourant et ajout des champs IdTypeDeCourant dans Client et Facture

-- =====================================================
-- 1. CRÉATION DE LA TABLE TypeDeCourants
-- =====================================================

CREATE TABLE TypeDeCourants (
    IdTypeDeCourant INT AUTO_INCREMENT PRIMARY KEY,
    Libelle VARCHAR(100) NOT NULL,
    Description VARCHAR(500) NULL,
    Statut TINYINT(1) NOT NULL DEFAULT 1,
    DateCreation DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    DateModification DATETIME(6) NULL,
    -- Index pour optimisation
    INDEX IX_TypeDeCourants_Libelle (Libelle),
    INDEX IX_TypeDeCourants_Statut (Statut)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 2. AJOUT DES CHAMPS IdTypeDeCourant DANS LA TABLE Clients
-- =====================================================

ALTER TABLE Clients 
ADD COLUMN IdTypeDeCourant INT NULL,
ADD INDEX IX_Clients_IdTypeDeCourant (IdTypeDeCourant);

-- =====================================================
-- 3. AJOUT DES CHAMPS IdTypeDeCourant DANS LA TABLE Factures
-- =====================================================

ALTER TABLE Factures 
ADD COLUMN IdTypeDeCourant INT NULL,
ADD INDEX IX_Factures_IdTypeDeCourant (IdTypeDeCourant);

-- =====================================================
-- 4. CRÉATION DES CONTRAINTES DE CLÉ ÉTRANGÈRE (Optionnel)
-- =====================================================

-- Contrainte pour Clients.IdTypeDeCourant -> TypeDeCourants.IdTypeDeCourant
ALTER TABLE Clients 
ADD CONSTRAINT FK_Clients_TypeDeCourants 
FOREIGN KEY (IdTypeDeCourant) REFERENCES TypeDeCourants(IdTypeDeCourant) 
ON DELETE SET NULL ON UPDATE CASCADE;

-- Contrainte pour Factures.IdTypeDeCourant -> TypeDeCourants.IdTypeDeCourant  
ALTER TABLE Factures 
ADD CONSTRAINT FK_Factures_TypeDeCourants 
FOREIGN KEY (IdTypeDeCourant) REFERENCES TypeDeCourants(IdTypeDeCourant) 
ON DELETE SET NULL ON UPDATE CASCADE;

-- =====================================================
-- 5. INSERTION DES TYPES DE COURANT PAR DÉFAUT
-- =====================================================

-- Type "Permanent" (service continu 24/7)
INSERT INTO TypeDeCourants (Libelle, Description, Statut, DateCreation) 
VALUES ('Permanent', 'Courant permanent sans interruption (service continu 24/7)', 1, NOW(6));

-- Type "Non Permanent" (service avec délestage)
INSERT INTO TypeDeCourants (Libelle, Description, Statut, DateCreation) 
VALUES ('Non Permanent', 'Courant non permanent avec délestage (service intermittent)', 1, NOW(6));

-- =====================================================
-- 6. MISE À JOUR DES CLIENTS EXISTANTS (Optionnel)
-- =====================================================
-- Assigner le type "Permanent" par défaut aux clients existants
-- Décommenter si vous voulez mettre à jour les clients existants

-- UPDATE Clients 
-- SET IdTypeDeCourant = (SELECT IdTypeDeCourant FROM TypeDeCourants WHERE Libelle = 'Permanent') 
-- WHERE IdTypeDeCourant IS NULL;

-- =====================================================
-- 7. VALIDATION DE LA MIGRATION
-- =====================================================

-- Vérifier la création de la table
SELECT 'Table TypeDeCourants créée' AS Status FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'TypeDeCourants';

-- Vérifier les nouvelles colonnes
SELECT 'Colonne IdTypeDeCourant ajoutée à Clients' AS Status 
FROM information_schema.columns 
WHERE table_schema = DATABASE() AND table_name = 'Clients' AND column_name = 'IdTypeDeCourant';

SELECT 'Colonne IdTypeDeCourant ajoutée à Factures' AS Status 
FROM information_schema.columns 
WHERE table_schema = DATABASE() AND table_name = 'Factures' AND column_name = 'IdTypeDeCourant';

-- Vérifier les types de courant créés
SELECT * FROM TypeDeCourants ORDER BY IdTypeDeCourant;

-- =====================================================
-- 8. NETTOYAGE (Optionnel - si migration inverse nécessaire)
-- =====================================================

-- Pour annuler la migration (ROLLBACK), exécuter dans l'ordre inverse:

-- -- 8.1 Supprimer les contraintes de clé étrangère
-- ALTER TABLE Clients DROP FOREIGN KEY FK_Clients_TypeDeCourants;
-- ALTER TABLE Factures DROP FOREIGN KEY FK_Factures_TypeDeCourants;

-- -- 8.2 Supprimer les colonnes
-- ALTER TABLE Clients DROP COLUMN IdTypeDeCourant;
-- ALTER TABLE Factures DROP COLUMN IdTypeDeCourant;

-- -- 8.3 Supprimer la table
-- DROP TABLE TypeDeCourants;

-- =====================================================
-- FIN DU SCRIPT DE MIGRATION
-- =====================================================
