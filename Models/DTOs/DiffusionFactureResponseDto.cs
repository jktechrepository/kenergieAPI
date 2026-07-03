namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant le résultat d'une diffusion de facture
    /// </summary>
    public class DiffusionFactureResponseDto
    {
        /// <summary>
        /// Indique si la diffusion a réussi globalement
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Identifiant de la facture diffusée
        /// </summary>
        public int FactureId { get; set; }

        /// <summary>
        /// Numéro de la facture
        /// </summary>
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Identifiant de l'usage concerné
        /// </summary>
        public int? UsageId { get; set; }

        /// <summary>
        /// Nom/libellé de l'usage
        /// </summary>
        public string? NomUsage { get; set; }

        /// <summary>
        /// Identifiant de la catégorie de clients concernée (pour compatibilité)
        /// </summary>
        [Obsolete("Utilisez UsageId à la place. Conservé pour compatibilité.")]
        public int? CategorieId { get; set; }

        /// <summary>
        /// Nom de la catégorie (pour compatibilité)
        /// </summary>
        [Obsolete("Utilisez NomUsage à la place. Conservé pour compatibilité.")]
        public string? NomCategorie { get; set; }

        /// <summary>
        /// Nombre total de clients dans la catégorie
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

        /// <summary>
        /// Nombre de clients ayant reçu au moins une notification
        /// </summary>
        public int ClientsNotifies { get; set; }

        /// <summary>
        /// Nombre de clients pour lesquels la diffusion a échoué
        /// </summary>
        public int ClientsEchecs { get; set; }

        /// <summary>
        /// Statistiques par canal de notification
        /// </summary>
        public Dictionary<string, CanalStatistiqueDto>? Canaux { get; set; }

        /// <summary>
        /// Durée de la diffusion
        /// </summary>
        public string? Duree { get; set; }

        /// <summary>
        /// Message de résultat
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO représentant les statistiques d'un canal de notification
    /// </summary>
    public class CanalStatistiqueDto
    {
        /// <summary>
        /// Nombre de notifications envoyées avec succès
        /// </summary>
        public int Envoyes { get; set; }

        /// <summary>
        /// Nombre de notifications en échec
        /// </summary>
        public int Echecs { get; set; }

        /// <summary>
        /// Nombre de notifications ignorées (opt-out, pas de device, etc.)
        /// </summary>
        public int Ignores { get; set; }
    }
}

