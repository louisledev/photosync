using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoSync
{
    /// <summary>
    /// Azure Function that syncs photos from a source OneDrive account to a destination OneDrive account
    /// Each Function App deployment handles one source account
    /// </summary>
    public class PhotoSyncFunction
    {
        private readonly ILogger _logger;
        private readonly IPhotoSyncService _photoSyncService;

        public PhotoSyncFunction(
            ILoggerFactory loggerFactory,
            IPhotoSyncService photoSyncService)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (photoSyncService == null) throw new ArgumentNullException(nameof(photoSyncService));

            _logger = loggerFactory.CreateLogger<PhotoSyncFunction>();
            _photoSyncService = photoSyncService;
        }

        [Function("PhotoSyncTimer")]
        public async Task Run([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"Photo sync function started at: {DateTime.UtcNow}");

            try
            {
                await _photoSyncService.SyncPhotosAsync();
                _logger.LogInformation("Photo sync completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during photo sync");
                throw;
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }

    public class PhotoSyncService : IPhotoSyncService
    {
        private readonly ILogger<PhotoSyncService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IStateManager _stateManager;
        private readonly IGraphClientFactory _graphClientFactory;

        public PhotoSyncService(
            ILogger<PhotoSyncService> logger,
            IConfiguration configuration,
            IStateManager stateManager,
            IGraphClientFactory graphClientFactory)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (stateManager == null) throw new ArgumentNullException(nameof(stateManager));
            if (graphClientFactory == null) throw new ArgumentNullException(nameof(graphClientFactory));

            _logger = logger;
            _configuration = configuration;
            _stateManager = stateManager;
            _graphClientFactory = graphClientFactory;
        }

        public async Task SyncPhotosAsync()
        {
            var processedFiles = await _stateManager.LoadProcessedFilesAsync();

            // Get configuration for source and destination accounts
            var sourceConfig = new
            {
                ClientId = _configuration["OneDriveSource:ClientId"],
                RefreshTokenSecretName = _configuration["OneDriveSource:RefreshTokenSecretName"],
                ClientSecretName = _configuration["OneDriveSource:ClientSecretName"],
                SourceFolder = _configuration["OneDriveSource:SourceFolder"],
                DeleteAfterSync = bool.TryParse(_configuration["OneDriveSource:DeleteAfterSync"], out var delete) && delete,
                MaxFilesPerRun = int.TryParse(_configuration["OneDriveSource:MaxFilesPerRun"], out var maxFiles) && maxFiles > 0 ? maxFiles : int.MaxValue
            };

            var destinationConfig = new
            {
                ClientId = _configuration["OneDriveDestination:ClientId"],
                RefreshTokenSecretName = _configuration["OneDriveDestination:RefreshTokenSecretName"],
                ClientSecretName = _configuration["OneDriveDestination:ClientSecretName"],
                DestinationFolder = _configuration["OneDriveDestination:DestinationFolder"]
            };

            _logger.LogInformation($"Processing source account with client ID: {sourceConfig.ClientId}");
            _logger.LogInformation($"Max files per run: {(sourceConfig.MaxFilesPerRun == int.MaxValue ? "unlimited" : sourceConfig.MaxFilesPerRun.ToString())}");

            // Validate refresh tokens before starting sync
            _logger.LogInformation("Validating refresh tokens...");

            var sourceValidation = await _graphClientFactory.ValidateRefreshTokenAsync(
                sourceConfig.ClientId,
                sourceConfig.RefreshTokenSecretName,
                sourceConfig.ClientSecretName);

            if (!sourceValidation.IsValid)
            {
                _logger.LogWarning($"Source account refresh token is invalid: {sourceValidation.ErrorMessage}");
                throw new InvalidOperationException($"Source account refresh token validation failed: {sourceValidation.ErrorMessage}");
            }

            var destinationValidation = await _graphClientFactory.ValidateRefreshTokenAsync(
                destinationConfig.ClientId,
                destinationConfig.RefreshTokenSecretName,
                destinationConfig.ClientSecretName);

            if (!destinationValidation.IsValid)
            {
                _logger.LogWarning($"Destination account refresh token is invalid: {destinationValidation.ErrorMessage}");
                throw new InvalidOperationException($"Destination account refresh token validation failed: {destinationValidation.ErrorMessage}");
            }

            _logger.LogInformation("All refresh tokens validated successfully");

            var sourceClient = CreateGraphClient(
                sourceConfig.ClientId,
                sourceConfig.RefreshTokenSecretName,
                sourceConfig.ClientSecretName);

            var destinationClient = CreateGraphClient(
                destinationConfig.ClientId,
                destinationConfig.RefreshTokenSecretName,
                destinationConfig.ClientSecretName);

            var newFiles = new List<string>();
            var processedCount = 0;

            // Get new photos from source
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var photos = await GetPhotosFromFolderAsync(sourceClient, sourceConfig.SourceFolder);
            stopwatch.Stop();

            _logger.LogInformation($"GetPhotosFromFolderAsync completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            _logger.LogInformation($"Found {photos.Count} total files, processing up to {sourceConfig.MaxFilesPerRun} new files");

            foreach (var photo in photos)
            {
                var fileId = $"{sourceConfig.ClientId}:{photo.Id}";

                if (processedFiles.Contains(fileId))
                {
                    _logger.LogDebug($"Skipping already processed file: {photo.Name}");
                    continue;
                }

                // Check if we've reached the max files per run limit
                if (processedCount >= sourceConfig.MaxFilesPerRun)
                {
                    _logger.LogInformation($"Reached max files per run limit ({sourceConfig.MaxFilesPerRun}). Remaining files will be processed in the next run.");
                    break;
                }

                try
                {
                    // Validate required properties
                    if (string.IsNullOrEmpty(photo.Id) || string.IsNullOrEmpty(photo.Name))
                    {
                        _logger.LogWarning($"Skipping file with missing ID or Name");
                        continue;
                    }

                    // Download photo
                    var photoStream = await DownloadPhotoAsync(sourceClient, photo.Id);

                    // Extract date and generate new filename
                    var dateTaken = ExtractDateFromPhoto(photoStream, photo.Name);

                    // Reset stream position after metadata extraction
                    photoStream.Position = 0;

                    // Generate filename based on date
                    var newFileName = GenerateFileName(photoStream, photo.Name);

                    // Reset stream position again before upload
                    photoStream.Position = 0;

                    // Upload to destination with date-based folder structure
                    await UploadPhotoAsync(
                        destinationClient,
                        destinationConfig.DestinationFolder,
                        newFileName,
                        photoStream,
                        dateTaken);

                    newFiles.Add(fileId);
                    processedCount++;

                    // Log with folder path if date is available
                    var logPath = dateTaken.HasValue
                        ? $"{dateTaken.Value:yyyy}/{dateTaken.Value:yyyy-MM}/{newFileName}"
                        : newFileName;
                    _logger.LogInformation($"Successfully synced ({processedCount}/{sourceConfig.MaxFilesPerRun}): {photo.Name} -> {logPath}");

                    // Delete source file if configured to do so
                    if (sourceConfig.DeleteAfterSync)
                    {
                        await DeletePhotoAsync(sourceClient, photo.Id);
                        _logger.LogInformation($"Deleted source file: {photo.Name}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing file: {photo.Name}");
                }
            }

            // Update state
            if (newFiles.Any())
            {
                await _stateManager.SaveProcessedFilesAsync(processedFiles.Concat(newFiles).ToList());
                _logger.LogInformation($"Sync completed. Processed {processedCount} files in this run.");
            }
            else
            {
                _logger.LogInformation("No new files to process.");
            }
        }

        private GraphServiceClient CreateGraphClient(string clientId, string refreshTokenSecretName, string clientSecretName)
        {
            return _graphClientFactory.CreateClient(clientId, refreshTokenSecretName, clientSecretName);
        }

        private async Task<List<DriveItem>> GetPhotosFromFolderAsync(GraphServiceClient client, string folderPath)
        {
            var photos = new List<DriveItem>();
            var photoExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".raw", ".cr2", ".nef" };
            // var videoExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".m4v" };
            // var allExtensions = photoExtensions.Concat(videoExtensions).ToArray();
            var allExtensions = photoExtensions;
            await GetPhotosFromFolderRecursiveAsync(client, folderPath, allExtensions, photos);

            return photos;
        }

        private async Task GetPhotosFromFolderRecursiveAsync(
            GraphServiceClient client,
            string folderPath,
            string[] extensions,
            List<DriveItem> photos)
        {
            try
            {
                // Get items from the specified folder (Graph v5 syntax)
                var driveItems = await client.Drives["me"]
                    .Root
                    .ItemWithPath(folderPath)
                    .Children
                    .GetAsync();

                if (driveItems?.Value != null)
                {
                    foreach (var item in driveItems.Value)
                    {
                        // Skip items with no name
                        if (string.IsNullOrEmpty(item.Name))
                        {
                            continue;
                        }

                        // If it's a file with matching extension, add it
                        if (item.File != null &&
                            extensions.Any(ext => item.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        {
                            photos.Add(item);
                        }
                        // If it's a folder, recurse into it
                        else if (item.Folder != null)
                        {
                            var subFolderPath = $"{folderPath}/{item.Name}";
                            _logger.LogDebug($"Scanning subfolder: {subFolderPath}");
                            await GetPhotosFromFolderRecursiveAsync(client, subFolderPath, extensions, photos);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving photos from folder: {folderPath}");
            }
        }

        private async Task<Stream> DownloadPhotoAsync(GraphServiceClient client, string itemId)
        {
            // Graph v5 syntax - use Drives["me"] instead of Me.Drive
            var stream = await client.Drives["me"].Items[itemId].Content.GetAsync();
            var memoryStream = new MemoryStream();
            if (stream != null)
            {
                await stream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;
            return memoryStream;
        }

        private DateTime? ExtractDateFromPhoto(Stream photoStream, string originalFileName)
        {
            // Try to extract EXIF date
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(photoStream);
                var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (exifSubIfdDirectory != null &&
                    exifSubIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
                {
                    return dateTaken;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"Could not extract EXIF date from {originalFileName}");
            }

            // Fallback: try to parse date from filename
            return TryParseDateFromFileName(originalFileName);
        }

        private DateTime? TryParseDateFromFileName(string fileName)
        {
            // Try common patterns like 20231225_143022 or 2023-12-25_14-30-22
            var patterns = new[]
            {
                @"(\d{4})(\d{2})(\d{2})[_-](\d{2})(\d{2})(\d{2})",  // 20231225_143022
                @"(\d{4})-(\d{2})-(\d{2})[_-](\d{2})-(\d{2})-(\d{2})" // 2023-12-25-14-30-22
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    try
                    {
                        var year = int.Parse(match.Groups[1].Value);
                        var month = int.Parse(match.Groups[2].Value);
                        var day = int.Parse(match.Groups[3].Value);
                        var hour = int.Parse(match.Groups[4].Value);
                        var minute = int.Parse(match.Groups[5].Value);
                        var second = int.Parse(match.Groups[6].Value);

                        return new DateTime(year, month, day, hour, minute, second);
                    }
                    catch
                    {
                        // Invalid date components
                    }
                }
            }

            return null;
        }

        private string GenerateFileName(Stream photoStream, string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

            // Video extensions - keep original filename
            var videoExtensions = new[] { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".m4v" };
            if (videoExtensions.Contains(extension))
            {
                return originalFileName;
            }

            // For pictures, try to extract EXIF and rename with IMG_ prefix
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(photoStream);
                var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (exifSubIfdDirectory != null &&
                    exifSubIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTaken))
                {
                    return $"{dateTaken:yyyyMMdd_HHmmss}{extension}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not extract EXIF data from {originalFileName}");
            }

            // Fallback: try to parse filename or use original
            return TryParseFileNameDate(originalFileName) ?? originalFileName;
        }

        private string? TryParseFileNameDate(string fileName)
        {
            // Try common patterns like IMG_20231225_143022.jpg or 2023-12-25_14-30-22.jpg
            var patterns = new[]
            {
                @"(\d{4})(\d{2})(\d{2})[_-](\d{2})(\d{2})(\d{2})",  // 20231225_143022
                @"(\d{4})-(\d{2})-(\d{2})[_-](\d{2})-(\d{2})-(\d{2})" // 2023-12-25-14-30-22
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    var year = match.Groups[1].Value;
                    var month = match.Groups[2].Value;
                    var day = match.Groups[3].Value;
                    var hour = match.Groups[4].Value;
                    var minute = match.Groups[5].Value;
                    var second = match.Groups[6].Value;

                    var extension = Path.GetExtension(fileName);
                    return $"{year}{month}{day}_{hour}{minute}{second}{extension}";
                }
            }

            return null;
        }

        private async Task UploadPhotoAsync(
            GraphServiceClient client,
            string destinationFolder,
            string fileName,
            Stream photoStream,
            DateTime? dateTaken = null)
        {
            // Build path with year/month folder structure if date is available
            string uploadPath;
            if (dateTaken.HasValue)
            {
                var yearFolder = dateTaken.Value.ToString("yyyy");
                var monthFolder = dateTaken.Value.ToString("yyyy-MM");
                uploadPath = $"{destinationFolder}/{yearFolder}/{monthFolder}/{fileName}";
            }
            else
            {
                uploadPath = $"{destinationFolder}/{fileName}";
            }

            // Use upload session for all files to support conflict behavior (Graph v5 syntax)
            var uploadSessionRequest = new CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "rename" }
                    }
                }
            };

            var uploadSession = await client.Drives["me"]
                .Root
                .ItemWithPath(uploadPath)
                .CreateUploadSession
                .PostAsync(uploadSessionRequest);

            // Use LargeFileUploadTask for all file sizes
            if (uploadSession is null || string.IsNullOrEmpty(uploadSession.UploadUrl))
            {
                throw new InvalidOperationException("Failed to create upload session");
            }

            var maxChunkSize = 320 * 1024; // 320 KB
            // Note: This constructor is marked obsolete but the recommended IUploadSession approach
            // is not yet well-documented. This approach works correctly for large file uploads.
            #pragma warning disable CS0618
            var fileUploadTask = new Microsoft.Graph.LargeFileUploadTask<DriveItem>(
                uploadSession,
                photoStream,
                maxChunkSize,
                client.RequestAdapter);
            #pragma warning restore CS0618

            await fileUploadTask.UploadAsync();
        }

        private async Task DeletePhotoAsync(GraphServiceClient client, string itemId)
        {
            // Delete the file from the source OneDrive account
            await client.Drives["me"].Items[itemId].DeleteAsync();
        }
    }
}
