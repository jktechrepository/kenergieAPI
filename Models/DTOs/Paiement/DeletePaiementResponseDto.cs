namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour la réponse de suppression d'un paiement
    /// Inclut les informations de ClientFacture pour afficher les montants mis à jour après suppression
    /// </summary>
    public class DeletePaiementResponseDto
    {
        /// <summary>
        /// Le paiement supprimé (informations avant suppression)
        /// </summary>
        public Models.Paiement? PaiementSupprime { get; set; }

        /// <summary>
        /// La facture associée
        /// </summary>
        public Models.Facture? Facture { get; set; }

        /// <summary>
        /// Informations de ClientFacture (montants mis à jour après la suppression du paiement)
        /// </summary>
        public ClientFactureInfoDto? ClientFacture { get; set; }

        /// <summary>
        /// Message de confirmation
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
