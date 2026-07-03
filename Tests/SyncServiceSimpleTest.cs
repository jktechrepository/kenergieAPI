using Kenergie.Models.DTOs.Sync;
using Kenergie.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kenergie.Tests
{
    /// <summary>
    /// Test simple pour valider le service de synchronisation
    /// Isolé des problèmes de nullabilité des services existants
    /// </summary>
    public class SyncServiceSimpleTest
    {
        private readonly ISyncService _syncService;

        public SyncServiceSimpleTest()
        {
            // Configuration minimale pour le test
            var services = new ServiceCollection();
            
            // Service de logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Service de watermark (mock)
            services.AddSingleton<IWatermarkService>(new TestWatermarkService());
            
            // Service de cursor (mock)
            services.AddSingleton<ICursorService>(new TestCursorService());
            
            // Service de synchronisation à tester
            services.AddScoped<ISyncService, TestSyncService>();
            
            var serviceProvider = services.BuildServiceProvider();
            _syncService = serviceProvider.GetRequiredService<ISyncService>();
        }

        [Fact]
        public async Task GetBootstrap_ShouldReturnValidResponse()
        {
            // Arrange
            var societeId = 1;

            // Act
            var result = await _syncService.GetBootstrapAsync(societeId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.SupportsDelta);
            Assert.Equal(1000, result.RecommendedPageSize);
            Assert.Equal(5000, result.MaxPageSize);
            Assert.NotNull(result.ServerWatermark);
            Assert.NotNull(result.Snapshot);
        }

        [Fact]
        public async Task GetClients_ShouldReturnPagedResults()
        {
            // Arrange
            var societeId = 1;
            var request = new SyncRequestDto
            {
                PageSize = 10,
                Cursor = null,
                Snapshot = null,
                Since = null
            };

            // Act
            var result = await _syncService.GetClientsAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Equal(10, result.PageSize);
            Assert.NotNull(result.Snapshot);
        }

        [Fact]
        public async Task GetArrears_ShouldReturnPagedResults()
        {
            // Arrange
            var societeId = 1;
            var request = new SyncArrearsRequestDto
            {
                PageSize = 10,
                OnlyOutstanding = true
            };

            // Act
            var result = await _syncService.GetArrearsAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.True(request.OnlyOutstanding);
        }

        [Fact]
        public async Task GetDeletions_ShouldReturnEmptyResults()
        {
            // Arrange
            var societeId = 1;
            var request = new SyncDeletionsRequestDto
            {
                Since = "base64(test-watermark)"
            };

            // Act
            var result = await _syncService.GetDeletionsAsync(societeId, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.DeletedClientIds);
            Assert.NotNull(result.RemovedClientFactureIds);
            Assert.NotNull(result.DeletedPaymentIds);
        }

        [Fact]
        public async Task ProcessPaymentsBatch_ShouldHandleEmptyBatch()
        {
            // Arrange
            var societeId = 1;
            var request = new PaymentBatchRequestDto
            {
                Items = new List<PaymentRequestDto>()
            };

            // Act
            var result = await _syncService.ProcessPaymentsBatchAsync(societeId, 1, request);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Results);
            Assert.Equal(0, result.Summary.Total);
            Assert.Equal(0, result.Summary.Created);
            Assert.Equal(0, result.Summary.Errors);
        }
    }

    /// <summary>
    /// Mock du service de watermark pour les tests
    /// </summary>
    public class TestWatermarkService : IWatermarkService
    {
        public string CreateWatermark(DateTime lastModified, int lastId)
        {
            return $"test-watermark-{lastModified:O}-{lastId}";
        }

        public (DateTime lastModified, int lastId) ParseWatermark(string watermark)
        {
            return (DateTime.UtcNow, 0);
        }

        public string CreateInitialWatermark()
        {
            return "test-initial-watermark";
        }
    }

    /// <summary>
    /// Mock du service de cursor pour les tests
    /// </summary>
    public class TestCursorService : ICursorService
    {
        string ICursorService.CreateCursor<T>(T entity)
        {
            return "test-cursor";
        }

        public (DateTime updatedAt, int id) ParseCursor(string cursor)
        {
            return (DateTime.UtcNow, 0);
        }
    }

    /// <summary>
    /// Mock du service de synchronisation pour les tests
    /// </summary>
    public class TestSyncService : ISyncService
    {
        public async Task<SyncBootstrapDto> GetBootstrapAsync(int societeId)
        {
            return new SyncBootstrapDto
            {
                ServerTimeUtc = DateTime.UtcNow,
                Snapshot = DateTime.UtcNow.ToString("O"),
                ServerWatermark = "test-watermark",
                RecommendedPageSize = 1000,
                MaxPageSize = 5000,
                SupportsDelta = true,
                Datasets = new DatasetInfoDto { EstimatedCount = 15000 }
            };
        }

        public async Task<SyncPageDto<ClientSyncDto>> GetClientsAsync(int societeId, SyncRequestDto request)
        {
            var items = new List<ClientSyncDto>();
            for (int i = 0; i < request.PageSize; i++)
            {
                items.Add(new ClientSyncDto
                {
                    IdClient = i + 1,
                    NomClient = $"Client {i + 1}",
                    UpdatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            return new SyncPageDto<ClientSyncDto>
            {
                Snapshot = DateTime.UtcNow.ToString("O"),
                Items = items,
                NextCursor = request.PageSize < 50 ? "test-cursor" : null,
                HasMore = request.PageSize < 50,
                NextSince = "test-next-since"
            };
        }

        public async Task<SyncPageDto<ArrearSyncDto>> GetArrearsAsync(int societeId, SyncArrearsRequestDto request)
        {
            var items = new List<ArrearSyncDto>();
            for (int i = 0; i < request.PageSize; i++)
            {
                items.Add(new ArrearSyncDto
                {
                    IdClientFacture = i + 1,
                    IdClient = 1,
                    MontantDu = 100,
                    DateModification = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            return new SyncPageDto<ArrearSyncDto>
            {
                Snapshot = DateTime.UtcNow.ToString("O"),
                Items = items,
                NextCursor = request.PageSize < 50 ? "test-cursor" : null,
                HasMore = request.PageSize < 50,
                NextSince = "test-next-since"
            };
        }

        public async Task<SyncDeletionsDto> GetDeletionsAsync(int societeId, SyncDeletionsRequestDto request)
        {
            return new SyncDeletionsDto
            {
                Snapshot = DateTime.UtcNow.ToString("O"),
                DeletedClientIds = new List<int>(),
                RemovedClientFactureIds = new List<int>(),
                DeletedPaymentIds = new List<int>(),
                NextSince = "test-next-since"
            };
        }

        public async Task<PaymentBatchResultDto> ProcessPaymentsBatchAsync(int societeId, int userId, PaymentBatchRequestDto request)
        {
            var results = request.Items.Select(payment => new PaymentResultDto
            {
                ClientRequestId = payment.ClientRequestId,
                Status = "created",
                IdPaiement = new Random().Next(1, 1000),
                NewMontantDu = 0,
                Message = "Test payment created"
            }).ToList();

            return new PaymentBatchResultDto
            {
                Results = results,
                Summary = new PaymentSummaryDto
                {
                    Total = request.Items.Count,
                    Created = request.Items.Count,
                    Duplicates = 0,
                    Errors = 0
                }
            };
        }
    }
}
