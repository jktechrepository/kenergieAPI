namespace Kenergie.Models.DTOs.ArriereeCrashed
{
    /// <summary>
    /// DTO de réponse pour ArriereeCrashed
    /// </summary>
    public class ArriereeCrashedResponseDto
    {
        public int IdArriereeCrashed { get; set; }
        public int NumeroLigne { get; set; }
        public string? CodeCons { get; set; }
        public string? Montant { get; set; }
        public string? Mois { get; set; }
        public string? Annees { get; set; }
        public int? IdClient { get; set; }
        public string? DonneesBrutesJson { get; set; }
        public string MessageErreur { get; set; } = string.Empty;
        public string? TypeErreur { get; set; }
        public string? ErreursJson { get; set; }
        public string Statut { get; set; } = "EN_ATTENTE";
        public int? IdClientFactureCree { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateCorrection { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
