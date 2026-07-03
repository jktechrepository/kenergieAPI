namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// DTO représentant les données brutes lues depuis le fichier Excel
    /// </summary>
    public class ClientFactureExcelRaw
    {
        public string? CodeCons { get; set; }
        public string? Montant { get; set; }
        public string? Mois { get; set; }
        public string? Annees { get; set; }
    }
}
