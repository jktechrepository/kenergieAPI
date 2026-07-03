using Kenergie.Models.DTOs.Pagination;
using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Paiement
{
    /// <summary>
    /// Paramètres de pagination pour les paiements avec filtres étendus
    /// </summary>
    public class PaiementPagedRequest : PagedRequest
    {
        /// <summary>
        /// Filtre optionnel par date spécifique (jour de DatePaiement). Si absent, aucun filtre journalier n'est appliqué.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Filtre par collecteur (IdUtilisateur)
        /// </summary>
        public int? IdUtilisateur { get; set; }

        /// <summary>
        /// Filtre par période - Date de début
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateDebut { get; set; }

        /// <summary>
        /// Filtre par période - Date de fin
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? DateFin { get; set; }

        /// <summary>
        /// Filtre par mois (1-12)
        /// </summary>
        [Range(1, 12, ErrorMessage = "Le mois doit être entre 1 et 12")]
        public int? Mois { get; set; }

        /// <summary>
        /// Filtre par année
        /// </summary>
        [Range(2020, 2030, ErrorMessage = "L'année doit être entre 2020 et 2030")]
        public int? Annee { get; set; }

        /// <summary>
        /// Filtre par axe du client
        /// </summary>
        public int? IdAxe { get; set; }

        /// <summary>
        /// Valide la cohérence des filtres de date
        /// </summary>
        public bool IsValid()
        {
            // Validation période
            if (DateDebut.HasValue && DateFin.HasValue && DateDebut.Value > DateFin.Value)
                return false;

            // Validation mois/année
            if (Mois.HasValue && (Mois < 1 || Mois > 12))
                return false;

            if (Annee.HasValue && (Annee < 2020 || Annee > DateTime.Now.Year + 1))
                return false;

            return true;
        }

        /// <summary>
        /// Vérifie si des filtres sont appliqués (au-delà de la pagination de base)
        /// </summary>
        public bool HasFilters()
        {
            return Date.HasValue || 
                   IdUtilisateur.HasValue || 
                   DateDebut.HasValue || 
                   DateFin.HasValue || 
                   Mois.HasValue || 
                   Annee.HasValue || 
                   IdAxe.HasValue ||
                   !string.IsNullOrWhiteSpace(SearchTerm);
        }
    }
}
