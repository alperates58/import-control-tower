using ImportControlTower.Application.Services;
using ImportControlTower.Domain.Common;
using Moq;
using Xunit;

namespace ImportControlTower.Api.UnitTests;

public class SystemServiceTests
{
    [Fact]
    public async Task GetSystemInfoAsync_ReturnsCorrectSystemInfo_WhenDbIsConnected()
    {
        // Arrange
        var mockTimeProvider = new Mock<IDateTimeProvider>();
        var testUtc = new DateTime(2026, 7, 28, 14, 30, 0, DateTimeKind.Utc);
        mockTimeProvider.Setup(t => t.UtcNow).Returns(testUtc);

        var mockDbHealthChecker = new Mock<IDatabaseHealthChecker>();
        mockDbHealthChecker
            .Setup(db => db.CanConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new SystemService(mockTimeProvider.Object, mockDbHealthChecker.Object);

        // Act
        var result = await service.GetSystemInfoAsync();

        // Assert
        Assert.Equal("Import Control Tower API", result.AppName);
        Assert.Equal("0.1.0-foundation", result.Version);
        Assert.Equal(testUtc, result.ServerTimeUtc);
        Assert.Equal("Connected", result.DatabaseStatus);
        Assert.Contains("2026-07-28 17:30:00", result.ServerTimeIstanbul);
    }
}
