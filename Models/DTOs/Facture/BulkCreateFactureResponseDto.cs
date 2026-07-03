namespace Kenergie.Models.DTOs.Facture
{
    /// <summary>
    /// DTO pour la réponse de création en masse de factures
    /// </summary>
    public class BulkCreateFactureResponseDto
    {
        /// <summary>
        /// Nombre total de factures dans la requête
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Nombre de factures créées avec succès
        /// </summary>
        public int Succes { get; set; }

        /// <summary>
        /// Nombre de factures en échec
        /// </summary>
        public int Echecs { get; set; }

        /// <summary>
        /// Liste des factures créées avec succès
        /// </summary>
        public List<FactureSuccesDto> FacturesCreees { get; set; } = new List<FactureSuccesDto>();

        /// <summary>
        /// Liste des erreurs pour les factures en échec
        /// </summary>
        public List<FactureErreurDto> Erreurs { get; set; } = new List<FactureErreurDto>();

        /// <summary>
        /// Indique si toutes les factures ont été créées avec succès
        /// </summary>
        public bool TousSucces => Echecs == 0;

        /// <summary>
        /// Message de résumé
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour une facture créée avec succès
    /// </summary>
    public class FactureSuccesDto
    {
        /// <summary>
        /// Index dans la liste originale (0-based)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Identifiant de la facture créée
        /// </summary>
        public int IdFacture { get; set; }

        /// <summary>
        /// Numéro de la facture
        /// </summary>
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Identifiant de l'usage
        /// </summary>
        public int IdUsage { get; set; }

        /// <summary>
        /// Nombre de ClientFacture créées automatiquement
        /// </summary>
        public int NombreClientFacturesCreees { get; set; }
    }

    /// <summary>
    /// DTO pour une erreur lors de la création d'une facture
    /// </summary>
    public class FactureErreurDto
    {
        /// <summary>
        /// Index dans la liste originale (0-based)
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Message d'erreur
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Code d'erreur (optionnel)
        /// </summary>
        public string? CodeErreur { get; set; }

        /// <summary>
        /// Données de la facture qui a échoué (pour référence)
        /// </summary>
        public CreateFactureItemDto? Facture { get; set; }
    }
}
