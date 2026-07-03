namespace Kenergie.Models.DTOs.ClientCrashed
{
    /// <summary>
    /// DTO de réponse pour la réessai de création d'un ClientCrashed
    /// </summary>
    public class RetryClientCrashedResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdClientCree { get; set; }
        public int IdClientCrashed { get; set; }
        public string? Erreur { get; set; }
    }
}
