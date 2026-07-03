using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour la réponse des utilisateurs par rôle
    /// </summary>
    public class UtilisateursParRoleDto
    {
        /// <summary>
        /// ID de l'utilisateur
        /// </summary>
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// Nom complet de l'utilisateur
        /// </summary>
        public string NomComplet { get; set; } = string.Empty;

        /// <summary>
        /// Email de l'utilisateur
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Nom d'utilisateur par défaut
        /// </summary>
        public string? DefaultUsername { get; set; }

        /// <summary>
        /// Téléphone de l'utilisateur
        /// </summary>
        public string? Telephone { get; set; }

        /// <summary>
        /// Statut de l'utilisateur (actif/inactif)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Date de création du compte
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Si l'utilisateur est connecté actuellement
        /// </summary>
        public bool IsConnecte { get; set; }

        /// <summary>
        /// Si l'utilisateur doit changer son mot de passe
        /// </summary>
        public bool DoitChangerMotDePasse { get; set; }

        /// <summary>
        /// Nom de la société (si applicable)
        /// </summary>
        public string? NomSociete { get; set; }

        /// <summary>
        /// ID de la société (si applicable)
        /// </summary>
        public int? IdSociete { get; set; }

        /// <summary>
        /// Rôle principal de l'utilisateur
        /// </summary>
        public string? RolePrincipal { get; set; }

        /// <summary>
        /// Liste de tous les rôles de l'utilisateur (système multi-rôles)
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();

        /// <summary>
        /// URL de la photo de profil (si disponible)
        /// </summary>
        public string? PhotoUrl { get; set; }

        /// <summary>
        /// Date de dernière connexion (si disponible)
        /// </summary>
        public DateTime? DerniereConnexion { get; set; }
    }

    /// <summary>
    /// DTO pour la réponse paginée des utilisateurs par rôle
    /// </summary>
    public class UtilisateursParRoleResponseDto
    {
        /// <summary>
        /// Page actuelle
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Taille de la page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Nombre total de pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Nombre total d'utilisateurs
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Nom du rôle recherché
        /// </summary>
        public string NomRole { get; set; } = string.Empty;

        /// <summary>
        /// ID du rôle recherché
        /// </summary>
        public int? RoleId { get; set; }

        /// <summary>
        /// Liste des utilisateurs
        /// </summary>
        public List<UtilisateursParRoleDto> Data { get; set; } = new List<UtilisateursParRoleDto>();
    }
}
