namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant une facture payée avec les détails de paiement
    /// </summary>
    public class FacturePayeeDto
    {
        public int IdFacture { get; set; }
        public string? NumeroFacture { get; set; }
        public DateTime? DateEmission { get; set; }
        public int MoisEmission { get; set; }
        public int AnneesEmission { get; set; }
        public decimal? MontantTotal { get; set; }
        public decimal? MontantPaye { get; set; }
        public DateTime? DatePaiementComplet { get; set; }
        public string? NomCategorie { get; set; }
    }
}

