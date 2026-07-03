namespace Kenergie.Models.DTOs
{
    /// <summary>
    /// DTO pour les statistiques de diffusion
    /// </summary>
    public class DiffusionStatistiqueDto
    {
        public int IdDiffusionStatistique { get; set; }
        public int IdFacture { get; set; }
        public string? NumeroFacture { get; set; }
        public int IdCategorie { get; set; }
        public string? NomCategorie { get; set; }
        public int TotalClients { get; set; }
        public int ClientsNotifies { get; set; }
        public int ClientsEchecs { get; set; }
        public double? TauxReussite { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public double? DureeSecondes { get; set; }
        public string Statut { get; set; } = string.Empty;
        public Dictionary<string, CanalStatistiqueDto>? Canaux { get; set; }
    }
}

