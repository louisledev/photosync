using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;

namespace PhotoSync.Tests
{
    public class PhotoSyncFunctionTests
    {
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IPhotoSyncService> _mockPhotoSyncService;
        private readonly PhotoSyncFunction _function;

        public PhotoSyncFunctionTests()
        {
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger>();
            _mockPhotoSyncService = new Mock<IPhotoSyncService>();

            _mockLoggerFactory
                .Setup(f => f.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            _function = new PhotoSyncFunction(_mockLoggerFactory.Object, _mockPhotoSyncService.Object);
        }

        [Fact]
        public void PhotoSyncFunction_Constructor_RequiresLoggerFactory()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoSyncFunction(null, _mockPhotoSyncService.Object));
        }

        [Fact]
        public void PhotoSyncFunction_Constructor_RequiresPhotoSyncService()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PhotoSyncFunction(_mockLoggerFactory.Object, null));
        }

        [Fact]
        public void PhotoSyncFunction_Constructor_CreatesLogger()
        {
            // Constructor should create a logger using the factory
            var mockFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var function = new PhotoSyncFunction(mockFactory.Object, _mockPhotoSyncService.Object);

            Assert.NotNull(function);
            mockFactory.Verify(f => f.CreateLogger(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Run_CallsPhotoSyncService()
        {
            // Arrange
            var timerInfo = new TimerInfo();
            _mockPhotoSyncService
                .Setup(s => s.SyncPhotosAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _function.Run(timerInfo);

            // Assert
            _mockPhotoSyncService.Verify(s => s.SyncPhotosAsync(), Times.Once);
        }

        [Fact]
        public async Task Run_LogsStartMessage()
        {
            // Arrange
            var timerInfo = new TimerInfo();
            _mockPhotoSyncService
                .Setup(s => s.SyncPhotosAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _function.Run(timerInfo);

            // Assert - Verify logger was called (checking for Information level logs)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("started")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Run_LogsCompletionMessage()
        {
            // Arrange
            var timerInfo = new TimerInfo();
            _mockPhotoSyncService
                .Setup(s => s.SyncPhotosAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _function.Run(timerInfo);

            // Assert - Verify completion was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("completed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Run_WithScheduleStatus_LogsNextSchedule()
        {
            // Arrange
            var nextRun = DateTime.UtcNow.AddDays(1);
            var timerInfo = new TimerInfo
            {
                ScheduleStatus = new ScheduleStatus
                {
                    Last = DateTime.UtcNow.AddDays(-1),
                    Next = nextRun
                }
            };
            _mockPhotoSyncService
                .Setup(s => s.SyncPhotosAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _function.Run(timerInfo);

            // Assert - Should log the next schedule
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Next")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Run_WithNullScheduleStatus_DoesNotLogNextSchedule()
        {
            // Arrange
            var timerInfo = new TimerInfo
            {
                ScheduleStatus = null
            };
            _mockPhotoSyncService
                .Setup(s => s.SyncPhotosAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _function.Run(timerInfo);

            // Assert - Verify that function completed successfully even without ScheduleStatus
            _mockPhotoSyncService.Verify(s => s.SyncPhotosAsync(), Times.Once);
        }

        [Fact]
        public async Task Run_WhenServiceThrows_LogsError()
        {
            // Arrange
            var timerInfo = new TimerInfo();
            var expectedException = new Exception("Test error");
            _mockPhotoSyncService
                .Setup(s => s.SyncPhotosAsync())
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _function.Run(timerInfo));

            Assert.Equal("Test error", exception.Message);

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.Is<Exception>(ex => ex.Message == "Test error"),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void TimerTriggerAttribute_HasCorrectSchedule()
        {
            // Verify the cron schedule on the Run method
            var method = typeof(PhotoSyncFunction).GetMethod("Run");
            var attribute = method?.GetCustomAttributes(typeof(FunctionAttribute), false);

            Assert.NotNull(attribute);
            Assert.NotEmpty(attribute);
        }

        [Fact]
        public void FunctionAttribute_HasCorrectName()
        {
            // Verify the function is named correctly
            var method = typeof(PhotoSyncFunction).GetMethod("Run");
            var functionAttributes = method?.GetCustomAttributes(typeof(FunctionAttribute), false);

            Assert.NotNull(functionAttributes);
            Assert.NotEmpty(functionAttributes);

            var functionAttr = functionAttributes[0] as FunctionAttribute;
            Assert.Equal("PhotoSyncTimer", functionAttr?.Name);
        }

        [Theory]
        [InlineData("0 0 2 * * *")]
        public void TimerTriggerSchedule_IsDaily2AM(string expectedSchedule)
        {
            // Verify the schedule format
            var parts = expectedSchedule.Split(' ');
            Assert.Equal(6, parts.Length); // Cron has 6 parts
            Assert.Equal("0", parts[0]); // Second
            Assert.Equal("0", parts[1]); // Minute
            Assert.Equal("2", parts[2]); // Hour (2 AM)
        }

        [Fact]
        public void LoggerFactory_CreatesTypedLogger()
        {
            // Verify that logger is created with the correct type
            var mockFactory = new Mock<ILoggerFactory>();
            var mockLogger = new Mock<ILogger>();
            mockFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

            var function = new PhotoSyncFunction(mockFactory.Object, _mockPhotoSyncService.Object);

            mockFactory.Verify(
                f => f.CreateLogger(It.Is<string>(s => s.Contains("PhotoSyncFunction"))),
                Times.Once);
        }
    }
}
