namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour la réponse de diffusion en masse de factures
    /// </summary>
    public class DiffusionFactureBulkResponseDto
    {
        /// <summary>
        /// Indique si la diffusion globale a réussi
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Identifiant de la société
        /// </summary>
        public int SocieteId { get; set; }

        /// <summary>
        /// Nombre total de factures en attente de diffusion trouvées
        /// </summary>
        public int TotalFactures { get; set; }

        /// <summary>
        /// Nombre de factures mises en queue pour diffusion
        /// </summary>
        public int FacturesEnQueue { get; set; }

        /// <summary>
        /// Nombre de factures qui ont échoué
        /// </summary>
        public int FacturesEchecs { get; set; }

        /// <summary>
        /// Liste des factures diffusées avec succès
        /// </summary>
        public List<FactureDiffusionItemDto> FacturesDiffusees { get; set; } = new List<FactureDiffusionItemDto>();

        /// <summary>
        /// Liste des erreurs pour les factures qui ont échoué
        /// </summary>
        public List<FactureDiffusionErreurDto> Erreurs { get; set; } = new List<FactureDiffusionErreurDto>();

        /// <summary>
        /// Durée totale de l'opération
        /// </summary>
        public string? Duree { get; set; }

        /// <summary>
        /// Message de résumé
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour une facture diffusée avec succès
    /// </summary>
    public class FactureDiffusionItemDto
    {
        /// <summary>
        /// Identifiant de la facture
        /// </summary>
        public int FactureId { get; set; }

        /// <summary>
        /// Numéro de la facture
        /// </summary>
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Identifiant de l'usage
        /// </summary>
        public int UsageId { get; set; }

        /// <summary>
        /// Nom/libellé de l'usage
        /// </summary>
        public string? NomUsage { get; set; }

        /// <summary>
        /// Nombre total de clients qui recevront la notification
        /// </summary>
        public int TotalClients { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Nombre de ClientFacture créées pour cette facture
        /// </summary>
        public int NombreClientFactures { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Montant total de toutes les ClientFacture pour cette facture
        /// </summary>
        public decimal MontantTotalClientFactures { get; set; }

        /// <summary>
        /// ✨ NOUVEAU : Montant dû total (somme de tous les MontantDu) pour cette facture
        /// </summary>
        public decimal MontantDuTotal { get; set; }
    }

    /// <summary>
    /// DTO pour une erreur lors de la diffusion d'une facture
    /// </summary>
    public class FactureDiffusionErreurDto
    {
        /// <summary>
        /// Identifiant de la facture
        /// </summary>
        public int FactureId { get; set; }

        /// <summary>
        /// Numéro de la facture
        /// </summary>
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Message d'erreur
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
