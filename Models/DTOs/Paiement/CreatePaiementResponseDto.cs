namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour la réponse de création d'un paiement
    /// Inclut les informations de ClientFacture pour afficher les montants mis à jour
    /// </summary>
    public class CreatePaiementResponseDto
    {
        /// <summary>
        /// Le paiement créé
        /// </summary>
        public Models.Paiement Paiement { get; set; } = null!;

        /// <summary>
        /// La facture associée
        /// </summary>
        public Models.Facture? Facture { get; set; }

        /// <summary>
        /// Informations de ClientFacture (montants mis à jour après le paiement)
        /// </summary>
        public ClientFactureInfoDto? ClientFacture { get; set; }

        /// <summary>
        /// Message de confirmation
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour les informations de ClientFacture dans la réponse
    /// </summary>
    public class ClientFactureInfoDto
    {
        /// <summary>
        /// Identifiant de la ClientFacture
        /// </summary>
        public int IdClientFacture { get; set; }

        /// <summary>
        /// Montant total de la facture pour ce client (déjà multiplié par nombreBatiment)
        /// </summary>
        public decimal? Montant { get; set; }

        /// <summary>
        /// Montant déjà payé (mis à jour après le paiement)
        /// </summary>
        public decimal? MontantPaye { get; set; }

        /// <summary>
        /// Montant restant dû (mis à jour après le paiement)
        /// </summary>
        public decimal? MontantDu { get; set; }

        /// <summary>
        /// Nombre de bâtiments (snapshot)
        /// </summary>
        public int? NombreBatiment { get; set; }

        /// <summary>
        /// Indique si c'est un arriéré pré-existant
        /// </summary>
        public bool EstArrierePreExistant { get; set; }
    }
}
