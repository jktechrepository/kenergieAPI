using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Client
{
    /// <summary>
    /// DTO pour la mise à jour d'un client avec ses usages
    /// Permet de mettre à jour les informations du client et ses usages en une seule requête
    /// </summary>
    public class UpdateClientWithUsagesDto
    {
        /// <summary>
        /// Nom du client
        /// </summary>
        [MaxLength(100, ErrorMessage = "Le nom du client ne peut pas dépasser 100 caractères")]
        public string? NomClient { get; set; }

        /// <summary>
        /// Adresse du client
        /// </summary>
        [MaxLength(255, ErrorMessage = "L'adresse ne peut pas dépasser 255 caractères")]
        public string? AdresseClient { get; set; }

        /// <summary>
        /// Téléphone du client
        /// </summary>
        [MaxLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client
        /// </summary>
        [MaxLength(100, ErrorMessage = "L'email ne peut pas dépasser 100 caractères")]
        [EmailAddress(ErrorMessage = "L'email doit être valide")]
        public string? EmailClient { get; set; }

        /// <summary>
        /// Genre du client
        /// </summary>
        [MaxLength(10, ErrorMessage = "Le genre ne peut pas dépasser 10 caractères")]
        public string? GenreClient { get; set; }

        /// <summary>
        /// Code consommateur (généré automatiquement si non fourni)
        /// </summary>
        [MaxLength(50, ErrorMessage = "Le CodeCons ne peut pas dépasser 50 caractères")]
        public string? CodeCons { get; set; }

        /// <summary>
        /// Statut du client (actif/inactif)
        /// </summary>
        public bool? Statut { get; set; }

        /// <summary>
        /// Indique si le client est actif (pour compatibilité)
        /// </summary>
        public bool? IsActif { get; set; }

        /// <summary>
        /// Identifiant de l'axe
        /// </summary>
        public int? IdAxe { get; set; }

        /// <summary>
        /// Liste des usages à mettre à jour
        /// Si fourni, remplace tous les usages existants du client
        /// Si null ou vide, les usages existants ne sont pas modifiés
        /// </summary>
        public List<ClientUsageUpdateDto>? Usages { get; set; }
    }

    /// <summary>
    /// DTO pour la mise à jour d'un usage d'un client
    /// </summary>
    public class ClientUsageUpdateDto
    {
        /// <summary>
        /// Libellé de l'usage (utilisé pour récupérer l'IdUsage)
        /// </summary>
        [Required(ErrorMessage = "Le libellé de l'usage est obligatoire")]
        [MaxLength(100, ErrorMessage = "Le libellé de l'usage ne peut pas dépasser 100 caractères")]
        public string LibelleUsage { get; set; } = string.Empty;

        /// <summary>
        /// Nombre de bâtiments pour cet usage
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de bâtiments doit être supérieur à 0")]
        public int nombreBatiment { get; set; } = 1;

        /// <summary>
        /// Statut de la relation Client-Usage (actif/inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Type de courant pour cette ligne (optionnel ; null = ne pas modifier le type existant en mise à jour)
        /// </summary>
        public int? IdTypeDeCourant { get; set; }
    }
}
