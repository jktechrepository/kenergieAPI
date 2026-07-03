using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Kenergie.Services.Notifications
{
    /// <summary>
    /// Implémentation de la queue pour les diffusions batch de factures
    /// Utilise System.Threading.Channels pour un traitement asynchrone efficace
    /// </summary>
    public class FactureDiffusionQueue : IFactureDiffusionQueue
    {
        private readonly Channel<FactureDiffusionJob> _channel;

        public FactureDiffusionQueue()
        {
            _channel = Channel.CreateUnbounded<FactureDiffusionJob>(new UnboundedChannelOptions
            {
                SingleReader = false, // Plusieurs workers peuvent lire
                SingleWriter = false,  // Plusieurs sources peuvent écrire
                AllowSynchronousContinuations = false
            });
        }

        public ValueTask EnqueueDiffusionAsync(int factureId, int idUsage, CancellationToken cancellationToken = default)
        {
            var job = new FactureDiffusionJob
            {
                FactureId = factureId,
                IdUsage = idUsage,
                EnqueuedAt = DateTime.Now
            };

            return _channel.Writer.WriteAsync(job, cancellationToken);
        }

        public async ValueTask<FactureDiffusionJob> DequeueDiffusionAsync(CancellationToken cancellationToken)
        {
            var job = await _channel.Reader.ReadAsync(cancellationToken);
            return job;
        }
    }
}

