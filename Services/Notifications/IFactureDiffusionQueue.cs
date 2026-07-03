using System.Threading;
using System.Threading.Tasks;

namespace Kenergie.Services.Notifications
{
    /// <summary>
    /// Interface pour la queue de diffusion batch de factures
    /// </summary>
    public interface IFactureDiffusionQueue
    {
        /// <summary>
        /// Ajoute une tâche de diffusion de facture à la queue
        /// </summary>
        ValueTask EnqueueDiffusionAsync(int factureId, int idUsage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère une tâche de diffusion de la queue
        /// </summary>
        ValueTask<FactureDiffusionJob> DequeueDiffusionAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Représente un job de diffusion de facture
    /// </summary>
    public class FactureDiffusionJob
    {
        public int FactureId { get; set; }
        public int IdUsage { get; set; }
        public DateTime EnqueuedAt { get; set; } = DateTime.Now;
    }
}

