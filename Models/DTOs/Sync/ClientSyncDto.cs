using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour la synchronisation des clients (projection optimisée)
    /// Contient uniquement les champs nécessaires pour le mode offline
    /// </summary>
    public class ClientSyncDto
    {
        /// <summary>
        /// Identifiant unique du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Adresse complète du client
        /// </summary>
        public string AdresseClient { get; set; } = string.Empty;

        /// <summary>
        /// Numéro de téléphone du client
        /// </summary>
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client
        /// </summary>
        public string? EmailClient { get; set; }

        /// <summary>
        /// Code consommateur unique du client
        /// </summary>
        public string? CodeCons { get; set; }

        /// <summary>
        /// Genre du client (M, F, Autre)
        /// </summary>
        public string? GenreClient { get; set; }

        /// <summary>
        /// Identifiant de l'axe auquel appartient ce client
        /// </summary>
        public int? IdAxe { get; set; }

        /// <summary>
        /// Identifiant de la cabine (via Axe)
        /// </summary>
        public int? IdCabine { get; set; }

        /// <summary>
        /// Identifiant de la société (via relation indirecte Axe->Cabine->Societe)
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>
        /// Catégorie principale du client (via premier usage)
        /// </summary>
        public int? IdCategorieClient { get; set; }

        /// <summary>
        /// Type de courant indicatif (première ligne ClientUsage active, tri par IdClientUsage).
        /// Préférer <see cref="ClientUsages"/> pour le détail par branche.
        /// </summary>
        public int? IdTypeDeCourant { get; set; }

        /// <summary>
        /// Lignes client–usage (toutes les relations, actives ou non) pour le mode offline.
        /// </summary>
        public List<ClientUsageSyncItemDto> ClientUsages { get; set; } = new();

        /// <summary>
        /// Indique si le client est actif (champ métier)
        /// </summary>
        public bool IsActif { get; set; }

        /// <summary>
        /// Statut du client (true = actif, false = supprimé)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Indique si le client est supprimé (soft delete pour sync)
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Date de dernière modification (pour delta sync)
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Ligne client–usage exposée dans la synchronisation
    /// </summary>
    public class ClientUsageSyncItemDto
    {
        public int IdClientUsage { get; set; }

        public int IdClient { get; set; }

        public int IdUsage { get; set; }

        /// <summary>
        /// Libellé de l'usage (snapshot pour affichage offline)
        /// </summary>
        public string LibelleUsage { get; set; } = string.Empty;

        public int? IdCategorieClient { get; set; }

        public int nombreBatiment { get; set; }

        public bool Statut { get; set; }

        public int? IdTypeDeCourant { get; set; }
    }
}
