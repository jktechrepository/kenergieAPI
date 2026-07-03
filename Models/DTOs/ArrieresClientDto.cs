namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant les arriérés d'un client
    /// </summary>
    public class ArrieresClientDto
    {
        public int IdClient { get; set; }
        public string NomClient { get; set; } = string.Empty;
        public string? Telephone { get; set; }
        public string? EmailClient { get; set; }
        public int NombreFacturesImpayees { get; set; }
        public decimal TotalArrieres { get; set; }
        public decimal? MontantTotalFactures { get; set; }
        public decimal? MontantTotalPaye { get; set; }
        public List<FactureImpayeeDto> FacturesImpayees { get; set; } = new();
    }
}

