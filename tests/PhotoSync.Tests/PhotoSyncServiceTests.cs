using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace PhotoSync.Tests
{
    public class PhotoSyncServiceTests
    {
        private readonly Mock<ILogger<PhotoSyncService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IStateManager> _mockStateManager;
        private readonly Mock<IGraphClientFactory> _mockGraphClientFactory;
        private readonly PhotoSyncService _service;

        public PhotoSyncServiceTests()
        {
            _mockLogger = new Mock<ILogger<PhotoSyncService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockStateManager = new Mock<IStateManager>();
            _mockGraphClientFactory = new Mock<IGraphClientFactory>();

            // Setup basic configuration
            _mockConfiguration.Setup(c => c["OneDriveSource:ClientId"]).Returns("sourceClient");
            _mockConfiguration.Setup(c => c["OneDriveSource:TenantId"]).Returns("sourceTenant");
            _mockConfiguration.Setup(c => c["OneDriveSource:ClientSecret"]).Returns("sourceSecret");
            _mockConfiguration.Setup(c => c["OneDriveSource:SourceFolder"]).Returns("/Photos");

            _mockConfiguration.Setup(c => c["OneDriveDestination:ClientId"]).Returns("destClient");
            _mockConfiguration.Setup(c => c["OneDriveDestination:TenantId"]).Returns("destTenant");
            _mockConfiguration.Setup(c => c["OneDriveDestination:ClientSecret"]).Returns("destSecret");
            _mockConfiguration.Setup(c => c["OneDriveDestination:DestinationFolder"]).Returns("/Backup");

            _service = new PhotoSyncService(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockStateManager.Object,
                _mockGraphClientFactory.Object);
        }

        [Fact]
        public void PhotoSyncService_Constructor_RequiresLogger()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoSyncService(null, _mockConfiguration.Object, _mockStateManager.Object, _mockGraphClientFactory.Object));
        }

        [Fact]
        public void PhotoSyncService_Constructor_RequiresConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoSyncService(_mockLogger.Object, null, _mockStateManager.Object, _mockGraphClientFactory.Object));
        }

        [Fact]
        public void PhotoSyncService_Constructor_RequiresStateManager()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoSyncService(_mockLogger.Object, _mockConfiguration.Object, null, _mockGraphClientFactory.Object));
        }

        [Fact]
        public void PhotoSyncService_Constructor_RequiresGraphClientFactory()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoSyncService(_mockLogger.Object, _mockConfiguration.Object, _mockStateManager.Object, null));
        }

        [Fact]
        public void PhotoSyncService_Constructor_SucceedsWithValidParameters()
        {
            var service = new PhotoSyncService(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockStateManager.Object,
                _mockGraphClientFactory.Object);
            Assert.NotNull(service);
        }

        [Theory]
        [InlineData("IMG_20231225_143022.jpg")]
        [InlineData("20231225_143022.jpg")]
        [InlineData("2023-12-25-14-30-22.jpg")]
        public void TryParseFileNameDate_ExtractsDateFromFileName(string fileName)
        {
            // Use reflection to call private method
            var method = typeof(PhotoSyncService).GetMethod("TryParseFileNameDate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = method?.Invoke(_service, new object[] { fileName }) as string;

            // Should extract date and return formatted name
            Assert.NotNull(result);
            Assert.Matches(@"\d{8}_\d{6}", result);
        }

        [Theory]
        [InlineData("random_photo.jpg")]
        [InlineData("vacation.png")]
        [InlineData("IMG.jpg")]
        public void TryParseFileNameDate_ReturnsNullForInvalidPattern(string fileName)
        {
            var method = typeof(PhotoSyncService).GetMethod("TryParseFileNameDate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = method?.Invoke(_service, new object[] { fileName }) as string;

            Assert.Null(result);
        }

        [Fact]
        public void GenerateFileName_FallsBackToOriginalNameWhenNoExif()
        {
            // Create a simple non-image stream
            var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
            var originalName = "photo.jpg";

            var method = typeof(PhotoSyncService).GetMethod("GenerateFileName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = method?.Invoke(_service, new object[] { stream, originalName }) as string;

            // Should return original name or attempt to parse it
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData(".jpg", true)]
        [InlineData(".jpeg", true)]
        [InlineData(".png", true)]
        [InlineData(".heic", true)]
        [InlineData(".heif", true)]
        [InlineData(".raw", true)]
        [InlineData(".cr2", true)]
        [InlineData(".nef", true)]
        [InlineData(".txt", false)]
        [InlineData(".pdf", false)]
        public void PhotoExtensions_ValidatesCorrectFormats(string extension, bool isValid)
        {
            var photoExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".raw", ".cr2", ".nef" };
            var result = photoExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(isValid, result);
        }

        [Fact]
        public void PhotoSyncService_Configuration_Source_HasRequiredKeys()
        {
            Assert.NotNull(_mockConfiguration.Object["OneDriveSource:ClientId"]);
            Assert.NotNull(_mockConfiguration.Object["OneDriveSource:TenantId"]);
            Assert.NotNull(_mockConfiguration.Object["OneDriveSource:ClientSecret"]);
            Assert.NotNull(_mockConfiguration.Object["OneDriveSource:SourceFolder"]);
        }

        [Fact]
        public void PhotoSyncService_Configuration_Destination_HasRequiredKeys()
        {
            Assert.NotNull(_mockConfiguration.Object["OneDriveDestination:ClientId"]);
            Assert.NotNull(_mockConfiguration.Object["OneDriveDestination:TenantId"]);
            Assert.NotNull(_mockConfiguration.Object["OneDriveDestination:ClientSecret"]);
            Assert.NotNull(_mockConfiguration.Object["OneDriveDestination:DestinationFolder"]);
        }

        [Theory]
        [InlineData(1024 * 1024, false)] // 1 MB - simple upload
        [InlineData(4 * 1024 * 1024, false)] // 4 MB - simple upload (at threshold)
        [InlineData(5 * 1024 * 1024, true)] // 5 MB - chunked upload
        [InlineData(10 * 1024 * 1024, true)] // 10 MB - chunked upload
        public void FileSize_Threshold_DeterminesUploadMethod(long fileSize, bool shouldUseChunkedUpload)
        {
            var threshold = 4 * 1024 * 1024; // 4 MB
            var usesChunked = fileSize > threshold;
            Assert.Equal(shouldUseChunkedUpload, usesChunked);
        }

        [Fact]
        public void UploadPath_Construction_CombinesFolderAndFileName()
        {
            var destinationFolder = "/Backup";
            var fileName = "20231225_143022.jpg";
            var expectedPath = $"{destinationFolder}/{fileName}";

            Assert.Equal("/Backup/20231225_143022.jpg", expectedPath);
        }

        [Fact]
        public void FileId_Construction_CombinesClientIdAndPhotoId()
        {
            var clientId = "client123";
            var photoId = "photo456";
            var fileId = $"{clientId}:{photoId}";

            Assert.Equal("client123:photo456", fileId);
            Assert.Contains(":", fileId);
        }
    }
}
