using Kenergie.Models.DTOs.Pagination;
using Kenergie.Models;

namespace Kenergie.Models.DTOs.Paiement
{
    /// <summary>
    /// Résultat paginé des paiements avec totaux additionnels
    /// </summary>
    public class PagedResultPaiement : PagedResult<Kenergie.Models.Paiement>
    {
        /// <summary>
        /// Montant total des paiements (somme des MontantPaye)
        /// </summary>
        public decimal MontantTotalPaiement { get; set; }

        /// <summary>
        /// Nombre total de paiements (identique à TotalCount mais plus explicite)
        /// </summary>
        public int NombreTotalPaiement { get; set; }

        /// <summary>
        /// Nombre total de collecteurs uniques dans les résultats
        /// </summary>
        public int NombreTotalCollecteur { get; set; }

        /// <summary>
        /// Constructeur pour créer le résultat enrichi
        /// </summary>
        public PagedResultPaiement(
            IEnumerable<Kenergie.Models.Paiement> data, 
            int totalCount, 
            int pageNumber, 
            int pageSize, 
            decimal montantTotal, 
            int nombreTotalCollecteur) 
            : base(data, totalCount, pageNumber, pageSize)
        {
            MontantTotalPaiement = montantTotal;
            NombreTotalPaiement = totalCount;
            NombreTotalCollecteur = nombreTotalCollecteur;
        }

        /// <summary>
        /// Constructeur par défaut pour la sérialisation
        /// </summary>
        public PagedResultPaiement() : base(new List<Kenergie.Models.Paiement>(), 0, 1, 20)
        {
            MontantTotalPaiement = 0;
            NombreTotalPaiement = 0;
            NombreTotalCollecteur = 0;
        }

        /// <summary>
        /// Crée une instance à partir d'un PagedResult existant
        /// </summary>
        public static PagedResultPaiement FromPagedResult(
            PagedResult<Kenergie.Models.Paiement> pagedResult, 
            decimal montantTotal, 
            int nombreTotalCollecteur)
        {
            return new PagedResultPaiement(
                pagedResult.Data, 
                pagedResult.TotalCount, 
                pagedResult.PageNumber, 
                pagedResult.PageSize,
                montantTotal,
                nombreTotalCollecteur);
        }
    }
}
