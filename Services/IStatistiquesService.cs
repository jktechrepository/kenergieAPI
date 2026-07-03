using Kenergie.Models.DTOs.Statistiques;
using System;
using System.Threading.Tasks;

namespace Kenergie.Services
{
    /// <summary>
    /// Interface du service de statistiques
    /// </summary>
    public interface IStatistiquesService
    {
        /// <summary>
        /// Obtient les statistiques générales pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="filtres">Filtres optionnels à appliquer</param>
        /// <returns>Statistiques générales</returns>
        Task<StatistiquesGeneralesDto> GetStatistiquesGeneralesAsync(int idSociete, StatistiquesFiltresDto filtres = null);

        /// <summary>
        /// Obtient les statistiques financières pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="debut">Date de début (optionnel)</param>
        /// <param name="fin">Date de fin (optionnel)</param>
        /// <param name="filtres">Filtres optionnels à appliquer</param>
        /// <returns>Statistiques financières</returns>
        Task<StatistiquesFinancieresDto> GetStatistiquesFinancieresAsync(int idSociete, DateTime? debut = null, DateTime? fin = null, StatistiquesFiltresDto filtres = null);

        /// <summary>
        /// Obtient les statistiques opérationnelles pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="filtres">Filtres optionnels à appliquer</param>
        /// <returns>Statistiques opérationnelles</returns>
        Task<StatistiquesOperationnellesDto> GetStatistiquesOperationnellesAsync(int idSociete, StatistiquesFiltresDto filtres = null);

        /// <summary>
        /// Obtient les statistiques de performance pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="filtres">Filtres optionnels à appliquer</param>
        /// <returns>Statistiques de performance</returns>
        Task<StatistiquesPerformanceDto> GetStatistiquesPerformanceAsync(int idSociete, StatistiquesFiltresDto filtres = null);

        /// <summary>
        /// Obtient toutes les statistiques consolidées pour une société
        /// </summary>
        /// <param name="idSociete">ID de la société</param>
        /// <param name="debut">Date de début (optionnel)</param>
        /// <param name="fin">Date de fin (optionnel)</param>
        /// <param name="filtres">Filtres optionnels à appliquer</param>
        /// <returns>Statistiques consolidées</returns>
        Task<StatistiquesConsolideesDto> GetStatistiquesConsolideesAsync(int idSociete, DateTime? debut = null, DateTime? fin = null, StatistiquesFiltresDto filtres = null);
    }
}
