using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Client
{
    /// <summary>
    /// DTO pour créer un client avec ses usages en une seule requête
    /// </summary>
    public class CreateClientWithUsagesDto
    {
        // Informations du client
        [Required]
        [MaxLength(200)]
        public string NomClient { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string AdresseClient { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [EmailAddress]
        [MaxLength(256)]
        public string? EmailClient { get; set; }

        [MaxLength(10)]
        public string? GenreClient { get; set; }

        [MaxLength(100)]
        public string? CodeCons { get; set; }

        public bool Statut { get; set; } = true;

        public bool IsActif { get; set; } = true;

        public int? IdAxe { get; set; }

        // Liste des usages à associer au client
        public List<UsageInfoDto> Usages { get; set; } = new List<UsageInfoDto>();

        /// <summary>
        /// Information sur un usage à associer au client
        /// </summary>
        public class UsageInfoDto
        {
            /// <summary>
            /// Libellé de l'usage (ex: "Résidentiel", "Commercial")
            /// </summary>
            [Required]
            public string LibelleUsage { get; set; } = string.Empty;

            /// <summary>
            /// Nombre de bâtiments pour cet usage (défaut: 1)
            /// </summary>
            public int nombreBatiment { get; set; } = 1;

            /// <summary>
            /// Type de courant pour cette ligne client–usage (optionnel)
            /// </summary>
            public int? IdTypeDeCourant { get; set; }
        }
    }
}
