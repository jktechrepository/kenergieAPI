using Xunit;
using Kenergie.Models.DTOs;

namespace Kenergie.Tests
{
    /// <summary>
    /// Tests unitaires pour les DTOs du Dashboard
    /// </summary>
    public class DashboardDtoTests
    {
        [Fact]
        public void DashboardDto_PeutEtreInstancie()
        {
            // Arrange & Act
            var dashboard = new DashboardDto
            {
                TotalAgents = 10,
                TotalClientsActifs = 100,
                MontantTotalPaiements = 50000,
                MontantTotalArrieres = 5000
            };

            // Assert
            Assert.NotNull(dashboard);
            Assert.Equal(10, dashboard.TotalAgents);
            Assert.Equal(100, dashboard.TotalClientsActifs);
            Assert.Equal(50000, dashboard.MontantTotalPaiements);
            Assert.Equal(5000, dashboard.MontantTotalArrieres);
        }

        [Fact]
        public void TopAgentCollecteurDto_PeutEtreInstancie()
        {
            // Arrange & Act
            var agent = new TopAgentCollecteurDto
            {
                IdAgent = 1,
                NomComplet = "Test Agent",
                MontantCollecte = 10000,
                NombrePaiements = 5
            };

            // Assert
            Assert.NotNull(agent);
            Assert.Equal(1, agent.IdAgent);
            Assert.Equal("Test Agent", agent.NomComplet);
            Assert.Equal(10000, agent.MontantCollecte);
            Assert.Equal(5, agent.NombrePaiements);
        }

        [Fact]
        public void DashboardDto_InitialiseAvecValeursParDefaut()
        {
            // Arrange & Act
            var dashboard = new DashboardDto();

            // Assert
            Assert.NotNull(dashboard);
            Assert.Equal(0, dashboard.TotalAgents);
            Assert.Equal(0, dashboard.TotalClientsActifs);
            Assert.Equal(0, dashboard.MontantTotalPaiements);
            Assert.Equal(0, dashboard.MontantTotalArrieres);
            Assert.NotNull(dashboard.Top5AgentsCollecteurs);
            Assert.Empty(dashboard.Top5AgentsCollecteurs);
        }
    }
}
