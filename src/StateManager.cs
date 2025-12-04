using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PhotoSync
{
    public class StateManager : IStateManager
    {
        private readonly ILogger<StateManager> _logger;
        private readonly TableClient _tableClient;
        private const string TableName = "ProcessedPhotos";
        private const string PartitionKey = "Photos";

        public StateManager(ILogger<StateManager> logger, IConfiguration configuration)
        {
            _logger = logger;
            var connectionString = configuration["AzureWebJobsStorage"];
            var serviceClient = new TableServiceClient(connectionString);
            _tableClient = serviceClient.GetTableClient(TableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task<HashSet<string>> LoadProcessedFilesAsync()
        {
            var processedFiles = new HashSet<string>();

            try
            {
                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    filter: $"PartitionKey eq '{PartitionKey}'"))
                {
                    processedFiles.Add(entity.RowKey);
                }

                _logger.LogInformation($"Loaded {processedFiles.Count} processed file records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading processed files state");
            }

            return processedFiles;
        }

        public async Task SaveProcessedFilesAsync(IEnumerable<string> allProcessedFiles)
        {
            try
            {
                var existingFiles = await LoadProcessedFilesAsync();
                // Deduplicate input files first, then filter out existing ones
                var newFiles = allProcessedFiles.Distinct().Where(f => !existingFiles.Contains(f)).ToList();

                if (!newFiles.Any())
                {
                    return;
                }

                // Batch insert new files
                var batch = new List<TableTransactionAction>();
                foreach (var fileId in newFiles)
                {
                    var entity = new TableEntity(PartitionKey, fileId)
                    {
                        { "ProcessedDate", DateTime.UtcNow }
                    };
                    batch.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));

                    // Azure Table Storage has a limit of 100 operations per batch
                    if (batch.Count >= 100)
                    {
                        await _tableClient.SubmitTransactionAsync(batch);
                        batch.Clear();
                    }
                }

                if (batch.Any())
                {
                    await _tableClient.SubmitTransactionAsync(batch);
                }

                _logger.LogInformation($"Saved {newFiles.Count} new processed file records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving processed files state");
                throw;
            }
        }

        public async Task CleanupOldRecordsAsync(int daysToKeep = 365)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var entitiesToDelete = new List<TableEntity>();

                await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
                    filter: $"PartitionKey eq '{PartitionKey}'"))
                {
                    if (entity.TryGetValue("ProcessedDate", out var processedDate))
                    {
                        DateTime? dateToCompare = null;

                        if (processedDate is DateTime dt)
                        {
                            dateToCompare = dt;
                        }
                        else if (processedDate is DateTimeOffset dto)
                        {
                            dateToCompare = dto.DateTime;
                        }

                        if (dateToCompare.HasValue && dateToCompare.Value < cutoffDate)
                        {
                            entitiesToDelete.Add(entity);
                        }
                    }
                }

                foreach (var entity in entitiesToDelete)
                {
                    await _tableClient.DeleteEntityAsync(entity.PartitionKey, entity.RowKey);
                }

                _logger.LogInformation($"Cleaned up {entitiesToDelete.Count} old records");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old records");
            }
        }
    }
}
