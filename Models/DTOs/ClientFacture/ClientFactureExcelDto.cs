namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant les données enrichies avec les IDs après validation
    /// </summary>
    public class ClientFactureExcelDto
    {
        public int NumeroLigne { get; set; }
        public string? CodeCons { get; set; }
        public int? IdClient { get; set; } // Récupéré depuis CodeCons
        public decimal? Montant { get; set; }
        public string? Mois { get; set; } // Format "01"-"12"
        public int? Annees { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();
    }
}
