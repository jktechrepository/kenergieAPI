namespace Kenergie.Models.DTOs.ClientCrashed
{
    /// <summary>
    /// DTO de réponse pour ClientCrashed
    /// </summary>
    public class ClientCrashedResponseDto
    {
        public int IdClientCrashed { get; set; }
        public int IdSociete { get; set; }
        public int NumeroLigne { get; set; }
        public string? NomClient { get; set; }
        public string? AdresseClient { get; set; }
        public string? Telephone { get; set; }
        public string? EmailClient { get; set; }
        public string? GenreClient { get; set; }
        public string? CodeCons { get; set; }
        public string? LibelleUsage { get; set; }
        public string? DonneesBrutesJson { get; set; }
        public string MessageErreur { get; set; } = string.Empty;
        public string? TypeErreur { get; set; }
        public string? ErreursJson { get; set; }
        public string Statut { get; set; } = "EN_ATTENTE";
        public int? IdClientCree { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateCorrection { get; set; }
        public DateTime? DateModification { get; set; }
    }
}
