namespace Kenergie.Models.DTOs.ClientFacture
{
    /// <summary>
    /// Résultat de l'import en masse de ClientFacture depuis Excel
    /// </summary>
    public class BulkClientFactureResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalLignes { get; set; }
        public int LignesReussies { get; set; }
        public int LignesEchouees { get; set; }
        public int DoublonsDetectes { get; set; }
        public List<LigneErreurClientFacture> LignesAvecErreurs { get; set; } = new List<LigneErreurClientFacture>();
        public List<ClientFactureCree> ClientFacturesCrees { get; set; } = new List<ClientFactureCree>();
        public DateTime DateTraitement { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Représente une ligne avec des erreurs
    /// </summary>
    public class LigneErreurClientFacture
    {
        public int NumeroLigne { get; set; }
        public string? CodeCons { get; set; }
        public List<string> Erreurs { get; set; } = new List<string>();
    }

    /// <summary>
    /// Représente une ClientFacture créée avec succès
    /// </summary>
    public class ClientFactureCree
    {
        public bool Success { get; set; }
        public int? IdClientFacture { get; set; }
        public string? CodeCons { get; set; }
        public string? Message { get; set; }
    }
}
