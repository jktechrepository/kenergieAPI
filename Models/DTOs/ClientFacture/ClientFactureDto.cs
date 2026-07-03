using System.Text.Json.Serialization;

namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO pour retourner les informations d'une ClientFacture
    /// </summary>
    public class ClientFactureDto
    {
        public int IdClientFacture { get; set; }
        public int? IdFacture { get; set; }
        public int IdClient { get; set; }
        public decimal? Montant { get; set; }
        public int? nombreBatiment { get; set; }
        public decimal? MontantPaye { get; set; }
        public decimal? MontantDu { get; set; }
        public string? Mois { get; set; }
        public int? Annees { get; set; }
        public DateTime? DateEmission { get; set; }
        public bool EstArrierePreExistant { get; set; }
        public string? Description { get; set; }
        public bool Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        // Informations supplémentaires
        public string? NomClient { get; set; }
        public string? NumeroFacture { get; set; }
        public string? LibelleUsage { get; set; }
        public int? IdTypeDeCourant { get; set; }
        public string? TypeDeCourant { get; set; }
    }
}
