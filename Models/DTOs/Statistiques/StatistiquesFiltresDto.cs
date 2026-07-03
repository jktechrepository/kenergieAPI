using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Kenergie.Models.DTOs.Statistiques
{
    /// <summary>
    /// DTO pour les filtres optionnels sur les statistiques
    /// Permet de filtrer les statistiques par catégorie, cabine, axe, type de courant et usage
    /// </summary>
    public class StatistiquesFiltresDto
    {
        /// <summary>
        /// Filtre par catégorie de client (optionnel)
        /// </summary>
        public int? IdCategorieClient { get; set; }

        /// <summary>
        /// Filtre par cabine (optionnel)
        /// </summary>
        public int? IdCabine { get; set; }

        /// <summary>
        /// Filtre par axe (optionnel)
        /// </summary>
        public int? IdAxe { get; set; }

        /// <summary>
        /// Filtre par type de courant (optionnel)
        /// </summary>
        public int? IdTypeDeCourant { get; set; }

        /// <summary>
        /// Filtre par usage (optionnel)
        /// </summary>
        public int? IdUsage { get; set; }

        /// <summary>
        /// Vérifie si au moins un filtre est appliqué
        /// </summary>
        public bool HasAnyFilter()
        {
            return IdCategorieClient.HasValue || 
                   IdCabine.HasValue || 
                   IdAxe.HasValue || 
                   IdTypeDeCourant.HasValue || 
                   IdUsage.HasValue;
        }
    }
}
