using Kenergie.Models;
using Kenergie.Services;
using Xunit;

namespace Kenergie.Tests
{
    public class PaiementClientFactureEnrichmentTests
    {
        [Fact]
        public void Apply_MapMontantEtMontantDu_CommeEndpointArrieres()
        {
            var paiement = new Paiement { IdPaiement = 271, IdFacture = 133, IdClient = 34254 };
            var clientFacture = new ClientFacture
            {
                IdClientFacture = 27225,
                IdFacture = 133,
                IdClient = 34254,
                Montant = 22500,
                MontantDu = 22000
            };

            PaiementClientFactureEnrichment.Apply(paiement, clientFacture);

            Assert.Equal(27225, paiement.IdClientFacture);
            Assert.Equal(22500, paiement.MontantAPaye);
            Assert.Equal(22000, paiement.ResteAPaye);
        }

        [Fact]
        public void Resolve_TrouveClientFacture_ParIdClientEtIdFacture()
        {
            var paiement = new Paiement { IdFacture = 133, IdClient = 34254 };
            var cf = new ClientFacture
            {
                IdClientFacture = 27225,
                IdFacture = 133,
                IdClient = 34254,
                Montant = 22500,
                MontantDu = 22000
            };
            var byId = new Dictionary<int, ClientFacture>();
            var byPair = PaiementClientFactureEnrichment.IndexByClientAndFacture(new[] { cf });

            var resolved = PaiementClientFactureEnrichment.Resolve(paiement, byId, byPair);

            Assert.NotNull(resolved);
            Assert.Equal(27225, resolved!.IdClientFacture);
        }

        [Fact]
        public void Resolve_PrioriseIdClientFacture_QuandPresent()
        {
            var paiement = new Paiement { IdClientFacture = 99, IdFacture = 133, IdClient = 34254 };
            var fromId = new ClientFacture { IdClientFacture = 99, Montant = 100, MontantDu = 50 };
            var other = new ClientFacture
            {
                IdClientFacture = 27225,
                IdFacture = 133,
                IdClient = 34254,
                Montant = 22500,
                MontantDu = 22000
            };
            var byId = new Dictionary<int, ClientFacture> { [99] = fromId };
            var byPair = PaiementClientFactureEnrichment.IndexByClientAndFacture(new[] { other });

            var resolved = PaiementClientFactureEnrichment.Resolve(paiement, byId, byPair);

            Assert.Equal(99, resolved!.IdClientFacture);
            Assert.Equal(100, resolved.Montant);
        }
    }
}
