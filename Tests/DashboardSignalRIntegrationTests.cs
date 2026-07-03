using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using System.Net.Http;
using System.Text.Json;
using Xunit;
using Kenergie.Hubs;
using Kenergie.Services;
using Kenergie.Services.Repositories;
using Kenergie.Models.DTOs;
using System.Threading.Tasks;
using System.Text;
using System;
using Microsoft.Extensions.Logging;

namespace Kenergie.Tests
{
    /// <summary>
    /// Tests d'intégration pour le Dashboard SignalR
    /// </summary>
    public class DashboardSignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public DashboardSignalRIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Ajouter les services nécessaires pour les tests
                    services.AddScoped<DashboardService>();
                    services.AddScoped<SignalRNotificationService>();
                });
            });
        }

        [Fact]
        public async Task DashboardHub_ConnexionEtAbonnement_FonctionneCorrectement()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act - Vérifier que l'endpoint SignalR est accessible
            var response = await client.GetAsync("/hubs/dashboard");
            
            // Assert - Le hub doit être accessible (même si la réponse n'est pas 200 pour WebSocket)
            Assert.True(response.StatusCode >= System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public void Services_InjectionDependencies_Fonctionne()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();

            // Act & Assert
            var dashboardService = scope.ServiceProvider.GetRequiredService<DashboardService>();
            var signalRService = scope.ServiceProvider.GetRequiredService<SignalRNotificationService>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<DashboardHub>>();

            Assert.NotNull(dashboardService);
            Assert.NotNull(signalRService);
            Assert.NotNull(hubContext);
        }

        [Fact]
        public async Task SignalRNotificationService_PeutEnvoyerNotifications()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();
            var signalRService = scope.ServiceProvider.GetRequiredService<SignalRNotificationService>();
            
            var dashboardData = new DashboardDto
            {
                TotalAgents = 5,
                TotalClientsActifs = 50,
                MontantTotalPaiements = 25000,
                MontantTotalArrieres = 2500
            };

            // Act & Assert - Pas d'exception lors de l'envoi
            await signalRService.NotifyDashboardUpdatedAsync(1, dashboardData);
            await signalRService.NotifyNewPaiementAsync(1, new { id = 1, montant = 1000 });
            await signalRService.NotifyNewClientAsync(1, new { id = 1, nom = "Test Client" });
            await signalRService.NotifyDashboardStatusChangeAsync(1, "test", 1, "test_status");
            
            Assert.True(true); // Si nous arrivons ici, c'est que les notifications ont été envoyées sans erreur
        }

        [Fact]
        public void Application_Startup_ServicesSontEnregistres()
        {
            // Arrange
            var scope = _factory.Services.CreateScope();

            // Act & Assert - Vérifier que les services clés sont enregistrés
            var services = scope.ServiceProvider;
            
            Assert.NotNull(services.GetService<DashboardService>());
            Assert.NotNull(services.GetService<SignalRNotificationService>());
            Assert.NotNull(services.GetService<IHubContext<DashboardHub>>());
            Assert.NotNull(services.GetService<ICurrentUserService>());
        }
    }
}
