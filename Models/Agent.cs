using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    public class Agent
    {
        [Key]
        public int IdAgent { get; set; }
        
        [MaxLength(50)]
        public string? Matricule { get; set; } // ✨ Nullable - Généré automatiquement si non fourni
        
        [MaxLength(200)]
        public string? NomComplet { get; set; }
        [MaxLength(10)]
        public string? Genre { get; set; }
        [Required]
        public DateTime DateNaissance { get; set; }
        public string? TelephoneAgent { get; set; }
        public string? EmailAgent { get; set; }
        public bool? Statut { get; set; } = true; // True ou False
        [MaxLength(20)]
        public string? EtatCivil { get; set; }
        public string? SerialNumber { get; set; }
        public string? Fonction { get; set; }
        public string? RoleAgent { get; set; }

       // [ValidateNever]
       // public IFormFile? Image { get; set; }

 
        public string? PhotoUrl { get; set; }
        public int? IdSociete { get; set; }
        
        /// <summary>
        /// Adresse de résidence de l'agent
        /// </summary>
        [MaxLength(500)]
        public string? AdresseResidence { get; set; }

        /// <summary>
        /// Zone géographique ou secteur de l'agent
        /// </summary>
        [MaxLength(200)]
        public string? Zone { get; set; }
        
        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation
        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }
        
        // Relation avec Utilisateur (nullable)
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Utilisateur>? Utilisateurs { get; set; } = new List<Utilisateur>();
    }
}

