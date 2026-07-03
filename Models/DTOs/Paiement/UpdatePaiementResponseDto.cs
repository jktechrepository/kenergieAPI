namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour la réponse de mise à jour d'un paiement
    /// Inclut les informations de ClientFacture pour afficher les montants mis à jour
    /// </summary>
    public class UpdatePaiementResponseDto
    {
        /// <summary>
        /// Le paiement mis à jour
        /// </summary>
        public Models.Paiement Paiement { get; set; } = null!;

        /// <summary>
        /// La facture associée
        /// </summary>
        public Models.Facture? Facture { get; set; }

        /// <summary>
        /// Informations de ClientFacture (montants mis à jour après la modification du paiement)
        /// </summary>
        public ClientFactureInfoDto? ClientFacture { get; set; }

        /// <summary>
        /// Message de confirmation
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
