namespace Kenergie.Services.Repositories
{
    /// <summary>
    /// Résultat d'une conversion monétaire.
    /// </summary>
    public class ConversionResult
    {
        public string CodeDeviseSource { get; set; } = string.Empty;
        public string CodeDeviseCible { get; set; } = string.Empty;
        public decimal Taux { get; set; }
        public decimal MontantSource { get; set; }
        public decimal MontantConverti { get; set; }
        public DateTime DateReference { get; set; }
    }

    public interface IDeviseConversionService
    {
        /// <summary>
        /// Dernier taux actif pour une paire à une date donnée (DateEffet &lt;= date).
        /// Retourne 1 si source == cible.
        /// </summary>
        Task<decimal?> GetDernierTauxAsync(int idSociete, string codeDeviseSource, string codeDeviseCible, DateTime date);

        /// <summary>
        /// Convertit un montant vers la devise cible. Lance InvalidOperationException si taux introuvable.
        /// </summary>
        Task<ConversionResult> ConvertirAsync(int idSociete, string codeDeviseSource, string codeDeviseCible, decimal montant, DateTime date);

        /// <summary>
        /// Convertit vers la devise principale de la société.
        /// </summary>
        Task<ConversionResult> ConvertirVersPrincipaleAsync(int idSociete, string codeDeviseSource, decimal montant, DateTime date);

        Task<string> GetCodeDevisePrincipaleAsync(int idSociete);
    }
}
