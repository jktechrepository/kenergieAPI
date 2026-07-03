using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Kenergie.Services;
using Kenergie.Hubs;

namespace Kenergie.Tests
{
    /// <summary>
    /// Tests de non-régression pour les contrôleurs existants
    /// </summary>
    public class ControllersNonRegressionTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ControllersNonRegressionTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        }
}
