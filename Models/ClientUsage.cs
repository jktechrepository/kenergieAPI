using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Kenergie.Models
{
    /// <summary>
    /// Table de jointure pour la relation many-to-many entre Client et Usage
    /// Permet à un client d'avoir plusieurs usages avec un nombre de bâtiments spécifique
    /// </summary>
    public class ClientUsage
    {
        /// <summary>
        /// Identifiant unique de la relation Client-Usage
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdClientUsage { get; set; }

        /// <summary>
        /// Identifiant du client
        /// </summary>
        [Required]
        public int IdClient { get; set; }

        /// <summary>
        /// Identifiant de l'usage
        /// </summary>
        [Required]
        public int IdUsage { get; set; }

        /// <summary>
        /// Nombre de bâtiments pour cet usage (utilisé pour le calcul des arriérés)
        /// Le montant des factures sera multiplié par ce nombre
        /// </summary>
        [Required]
        public int nombreBatiment { get; set; } = 1;

        /// <summary>
        /// Date d'attribution de l'usage au client
        /// </summary>
        public DateTime DateAttribution { get; set; } = DateTime.Now;

        /// <summary>
        /// Statut de la relation Client-Usage (true = actif, false = inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Type de courant pour cette ligne client–usage (tarification par branche)
        /// </summary>
        public int? IdTypeDeCourant { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ForeignKey("IdClient")]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ForeignKey("IdUsage")]
        public Usage? Usage { get; set; }

        [JsonIgnore]
        [ForeignKey("IdTypeDeCourant")]
        public TypeDeCourant? TypeDeCourant { get; set; }
    }
}
