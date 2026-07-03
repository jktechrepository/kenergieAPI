using System.ComponentModel.DataAnnotations;

namespace Kenergie.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour la synchronisation des arriérés (projection optimisée)
    /// Contient uniquement les champs nécessaires pour le mode offline
    /// </summary>
    public class ArrearSyncDto
    {
        /// <summary>
        /// Identifiant unique de la facture client
        /// </summary>
        public int IdClientFacture { get; set; }

        /// <summary>
        /// Identifiant de la facture (NULL pour arriérés pré-existants)
        /// </summary>
        public int? IdFacture { get; set; }

        /// <summary>
        /// Identifiant du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Numéro de la facture
        /// </summary>
        public string? NumeroFacture { get; set; }

        /// <summary>
        /// Date d'émission de la facture
        /// </summary>
        public DateTime DateEmission { get; set; }

        /// <summary>
        /// Mois d'émission (format: "01", "02", ..., "12")
        /// </summary>
        public string? Mois { get; set; }

        /// <summary>
        /// Année d'émission
        /// </summary>
        public int? Annees { get; set; }

        /// <summary>
        /// Montant total de la facture pour ce client
        /// </summary>
        public decimal MontantTotal { get; set; }

        /// <summary>
        /// Montant déjà payé par ce client pour cette facture
        /// </summary>
        public decimal MontantPaye { get; set; }

        /// <summary>
        /// Montant restant dû par ce client pour cette facture
        /// </summary>
        public decimal MontantDu { get; set; }

        /// <summary>
        /// Libellé de l'usage (ex: "Résidentiel", "Commercial")
        /// </summary>
        public string? LibelleUsage { get; set; }

        /// <summary>
        /// Indique si c'est un arriéré pré-existant
        /// </summary>
        public bool EstArrierePreExistant { get; set; }

        /// <summary>
        /// Date de dernière modification (pour delta sync)
        /// </summary>
        public DateTime DateModification { get; set; }
    }
}
