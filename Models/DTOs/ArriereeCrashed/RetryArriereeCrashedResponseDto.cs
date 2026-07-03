namespace Kenergie.Models.DTOs.ArriereeCrashed
{
    /// <summary>
    /// DTO de réponse pour la réessai de création d'une ArriereeCrashed
    /// </summary>
    public class RetryArriereeCrashedResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdClientFactureCree { get; set; }
        public int IdArriereeCrashed { get; set; }
    }
}
