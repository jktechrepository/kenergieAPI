namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant une facture impayée avec les détails d'arriérés
    /// </summary>
    public class FactureImpayeeDto
    {
        public int IdFacture { get; set; }

        /// <summary>
        /// Identifiant de la ligne ClientFacture (présent pour les listes par client ; null pour les agrégats société).
        /// </summary>
        public int? IdClientFacture { get; set; }

        public string? NumeroFacture { get; set; }
        public DateTime? DateEmission { get; set; }
        public int MoisEmission { get; set; }
        public int AnneesEmission { get; set; }
        public decimal? MontantTotal { get; set; }
        public decimal? MontantPaye { get; set; }
        public decimal MontantDu { get; set; }
        public int? JoursRetard { get; set; }
        public string? NomCategorie { get; set; }
        
        /// <summary>
        /// ✨ NOUVEAU : Nombre de clients avec arriérés pour cette facture (consolidé)
        /// </summary>
        public int? NombreClientsAvecArrieres { get; set; }
    }
}

