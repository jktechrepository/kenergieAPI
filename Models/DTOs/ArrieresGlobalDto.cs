namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO représentant un rapport global des arriérés
    /// </summary>
    public class ArrieresGlobalDto
    {
        public int NombreClientsAvecArrieres { get; set; }
        public int NombreTotalFacturesImpayees { get; set; }
        public decimal TotalArrieres { get; set; }
        public decimal? MontantTotalFactures { get; set; }
        public decimal? MontantTotalPaye { get; set; }
        public List<ArrieresClientDto> ClientsAvecArrieres { get; set; } = new();
    }
}

