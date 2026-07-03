namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour retourner les ClientFacture consolidées avec statistiques pour une période donnée
    /// </summary>
    public class ClientFactureConsolideDto
    {
        /// <summary>
        /// Total général des MontantDu pour la période (somme des montants dus)
        /// </summary>
        public decimal TotalGeneral { get; set; }

        /// <summary>
        /// Nombre total de clients distincts avec arriérés pour la période
        /// </summary>
        public int NombreTotalClients { get; set; }

        /// <summary>
        /// Nombre total de factures avec arriérés pour la période
        /// </summary>
        public int NombreTotalFactures { get; set; }

        /// <summary>
        /// Liste détaillée des factures avec arriérés
        /// </summary>
        public List<ClientFactureDto> Factures { get; set; } = new();
    }
}
