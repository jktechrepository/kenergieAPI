-- Script pour créer l'index de performance pour la sélection des ClientFactures
-- Améliore les performances du filtrage par IdTypeDeCourant + Statut

-- Index composite sur Client pour optimiser les requêtes de facturation
CREATE INDEX IX_ClientFactures_Selection 
ON Clients(IdTypeDeCourant, Statut) 
WHERE Statut = 1;

-- Index sur ClientUsage pour optimiser les jointures
CREATE INDEX IX_ClientUsages_Selection 
ON ClientUsages(IdUsage, IdClient, Statut) 
WHERE Statut = 1;

-- Afficher les index créés
SHOW INDEX FROM Clients WHERE Key_name = 'IX_ClientFactures_Selection';
SHOW INDEX FROM ClientUsages WHERE Key_name = 'IX_ClientUsages_Selection';
