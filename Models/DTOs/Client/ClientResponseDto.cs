namespace Kenergie.Models.DTOs.Client
{
    /// <summary>
    /// DTO de réponse pour un client avec ses informations d'usage
    /// </summary>
    public class ClientResponseDto
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
        /// Genre du client (M, F, Autre)
        /// </summary>
        public string? GenreClient { get; set; }

        /// <summary>
        /// Code consommateur du client
        /// </summary>
        public string? CodeCons { get; set; }

        /// <summary>
        /// Statut du client (actif/inactif)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Indique si le client est actif
        /// </summary>
        public bool IsActif { get; set; }

        /// <summary>
        /// Date de dernière réactivation (null si jamais réactivé)
        /// </summary>
        public DateTime? DateDerniereReactivation { get; set; }

        /// <summary>
        /// Identifiant de l'axe auquel appartient ce client
        /// </summary>
        public int? IdAxe { get; set; }

        /// <summary>
        /// Nom de l'axe (si disponible)
        /// </summary>
        public string? NomAxe { get; set; }

        /// <summary>
        /// Code de l'axe (si disponible)
        /// </summary>
        public string? CodeAxe { get; set; }

        /// <summary>
        /// Identifiant de la cabine (si disponible)
        /// </summary>
        public int? IdCabine { get; set; }

        /// <summary>
        /// Nom de la cabine (si disponible)
        /// </summary>
        public string? NomCabine { get; set; }

        /// <summary>
        /// Code de la cabine (si disponible)
        /// </summary>
        public string? CodeCabine { get; set; }

        /// <summary>
        /// Identifiant de la société (si disponible)
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Date de création du client
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Liste des usages associés au client
        /// </summary>
        public List<ClientUsageInfoDto> Usages { get; set; } = new List<ClientUsageInfoDto>();
    }

    /// <summary>
    /// DTO représentant les informations d'un usage associé à un client
    /// </summary>
    public class ClientUsageInfoDto
    {
        /// <summary>
        /// Identifiant unique de la relation Client-Usage
        /// </summary>
        public int IdClientUsage { get; set; }

        /// <summary>
        /// Identifiant de l'usage
        /// </summary>
        public int IdUsage { get; set; }

        /// <summary>
        /// Libellé de l'usage (ex: "Résidentiel", "Commercial")
        /// </summary>
        public string LibelleUsage { get; set; } = string.Empty;

        /// <summary>
        /// Description de l'usage
        /// </summary>
        public string? DescriptionUsage { get; set; }

        /// <summary>
        /// Nombre de bâtiments pour cet usage
        /// </summary>
        public int NombreBatiment { get; set; }

        /// <summary>
        /// Date d'attribution de l'usage au client
        /// </summary>
        public DateTime DateAttribution { get; set; }

        /// <summary>
        /// Statut de la relation Client-Usage (true = actif, false = inactif)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Identifiant de la catégorie de client
        /// </summary>
        public int IdCategorieClient { get; set; }

        /// <summary>
        /// Nom de la catégorie de client
        /// </summary>
        public string? NomCategorie { get; set; }

        /// <summary>
        /// Identifiant de la société (via la catégorie)
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Nom de la société (via la catégorie)
        /// </summary>
        public string? NomSociete { get; set; }

        /// <summary>
        /// Type de courant pour cette ligne client–usage
        /// </summary>
        public int? IdTypeDeCourant { get; set; }
    }
}
