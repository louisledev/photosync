using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Testcontainers.Azurite;
using Xunit;

namespace PhotoSync.IntegrationTests
{
    [Collection("StateManager Integration Tests")]
    public class StateManagerIntegrationTests : IAsyncLifetime
    {
        private AzuriteContainer _azuriteContainer = null!;
        private string _connectionString = null!;
        private readonly string _tableName = $"ProcessedPhotos{Guid.NewGuid():N}";

        public async Task InitializeAsync()
        {
            // Start Azurite container (shared for all tests in this class)
            _azuriteContainer = new AzuriteBuilder()
                .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
                .Build();

            await _azuriteContainer.StartAsync();
            _connectionString = _azuriteContainer.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            if (_azuriteContainer != null)
            {
                await _azuriteContainer.DisposeAsync();
            }
        }

        private IConfiguration CreateConfiguration()
        {
            var configData = new Dictionary<string, string>
            {
                { "AzureWebJobsStorage", _connectionString }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();
        }

        private ILogger<StateManager> CreateLogger()
        {
            return new Mock<ILogger<StateManager>>().Object;
        }

        [Fact]
        public async Task LoadProcessedFilesAsync_EmptyTable_ReturnsEmptyHashSet()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);

            // Act
            var result = await stateManager.LoadProcessedFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SaveProcessedFilesAsync_SingleFile_PersistsToStorage()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);
            var fileIds = new[] { "client1:photo123" };

            // Act
            await stateManager.SaveProcessedFilesAsync(fileIds);

            // Assert - Load and verify
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Single(loaded);
            Assert.Contains("client1:photo123", loaded);
        }

        [Fact]
        public async Task SaveProcessedFilesAsync_MultipleFiles_PersistsAllToStorage()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);
            var fileIds = new[]
            {
                "client1:photo1",
                "client1:photo2",
                "client2:photo1",
                "client2:photo2"
            };

            // Act
            await stateManager.SaveProcessedFilesAsync(fileIds);

            // Assert
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(4, loaded.Count);
            Assert.Contains("client1:photo1", loaded);
            Assert.Contains("client1:photo2", loaded);
            Assert.Contains("client2:photo1", loaded);
            Assert.Contains("client2:photo2", loaded);
        }

        [Fact]
        public async Task SaveProcessedFilesAsync_LargeBatch_HandlesCorrectly()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);
            var fileIds = Enumerable.Range(1, 150)
                .Select(i => $"client1:photo{i}")
                .ToList();

            // Act
            await stateManager.SaveProcessedFilesAsync(fileIds);

            // Assert
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(150, loaded.Count);
            Assert.Contains("client1:photo1", loaded);
            Assert.Contains("client1:photo150", loaded);
        }

        [Fact]
        public async Task SaveProcessedFilesAsync_CalledMultipleTimes_AccumulatesFiles()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);
            var firstBatch = new[] { "client1:photo1", "client1:photo2" };
            var secondBatch = new[] { "client1:photo3", "client1:photo4" };

            // Act
            await stateManager.SaveProcessedFilesAsync(firstBatch);
            var afterFirst = await stateManager.LoadProcessedFilesAsync();
            await stateManager.SaveProcessedFilesAsync(afterFirst.Concat(secondBatch));

            // Assert
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(4, loaded.Count);
            Assert.Contains("client1:photo1", loaded);
            Assert.Contains("client1:photo4", loaded);
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_RemovesOldRecords_KeepsRecentOnes()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var tableClient = new TableClient(_connectionString, "ProcessedPhotos");
            await tableClient.CreateIfNotExistsAsync();

            // Add old records (400 days ago)
            var oldDate = DateTime.UtcNow.AddDays(-400);
            await tableClient.AddEntityAsync(new TableEntity("Photos", "old1")
            {
                { "ProcessedDate", oldDate }
            });
            await tableClient.AddEntityAsync(new TableEntity("Photos", "old2")
            {
                { "ProcessedDate", oldDate }
            });

            // Add recent records (30 days ago)
            var recentDate = DateTime.UtcNow.AddDays(-30);
            await tableClient.AddEntityAsync(new TableEntity("Photos", "recent1")
            {
                { "ProcessedDate", recentDate }
            });

            var stateManager = new StateManager(logger, config);

            // Verify records exist before cleanup
            var beforeCleanup = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(3, beforeCleanup.Count);

            // Act - cleanup records older than 365 days
            await stateManager.CleanupOldRecordsAsync(365);

            // Assert
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Single(loaded);
            Assert.Contains("recent1", loaded);
            Assert.DoesNotContain("old1", loaded);
            Assert.DoesNotContain("old2", loaded);
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_CustomRetentionPeriod_RespectsParameter()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var tableClient = new TableClient(_connectionString, "ProcessedPhotos");
            await tableClient.CreateIfNotExistsAsync();

            // Add records 100 days old
            var date100DaysAgo = DateTime.UtcNow.AddDays(-100);
            await tableClient.AddEntityAsync(new TableEntity("Photos", "file100")
            {
                { "ProcessedDate", date100DaysAgo }
            });

            // Add records 50 days old
            var date50DaysAgo = DateTime.UtcNow.AddDays(-50);
            await tableClient.AddEntityAsync(new TableEntity("Photos", "file50")
            {
                { "ProcessedDate", date50DaysAgo }
            });

            var stateManager = new StateManager(logger, config);

            // Act - cleanup records older than 75 days
            await stateManager.CleanupOldRecordsAsync(75);

            // Assert
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Single(loaded);
            Assert.Contains("file50", loaded);
            Assert.DoesNotContain("file100", loaded);
        }

        [Fact]
        public async Task StateManager_RealWorkflow_SaveLoadAndCleanup()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);

            // Act & Assert - Initial state is empty
            var initial = await stateManager.LoadProcessedFilesAsync();
            Assert.Empty(initial);

            // Save some files
            var firstBatch = new[] { "client1:photo1", "client1:photo2", "client1:photo3" };
            await stateManager.SaveProcessedFilesAsync(firstBatch);

            // Load and verify
            var afterFirst = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(3, afterFirst.Count);

            // Add more files (simulating incremental sync)
            var secondBatch = new[] { "client1:photo4", "client1:photo5" };
            await stateManager.SaveProcessedFilesAsync(afterFirst.Concat(secondBatch));

            // Load and verify all files
            var afterSecond = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(5, afterSecond.Count);
            Assert.Contains("client1:photo1", afterSecond);
            Assert.Contains("client1:photo5", afterSecond);

            // Cleanup shouldn't affect recent records
            await stateManager.CleanupOldRecordsAsync(365);
            var afterCleanup = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(5, afterCleanup.Count);
        }

        [Fact]
        public async Task SaveProcessedFilesAsync_DuplicateFileIds_HandlesGracefully()
        {
            // Arrange
            var logger = CreateLogger();
            var config = CreateConfiguration();
            var stateManager = new StateManager(logger, config);
            var fileIds = new[]
            {
                "client1:photo1",
                "client1:photo1", // Duplicate
                "client1:photo2"
            };

            // Act
            await stateManager.SaveProcessedFilesAsync(fileIds);

            // Assert - Should only have 2 unique files
            var loaded = await stateManager.LoadProcessedFilesAsync();
            Assert.Equal(2, loaded.Count);
            Assert.Contains("client1:photo1", loaded);
            Assert.Contains("client1:photo2", loaded);
        }
    }

    [CollectionDefinition("StateManager Integration Tests", DisableParallelization = true)]
    public class StateManagerTestCollection
    {
    }
}
