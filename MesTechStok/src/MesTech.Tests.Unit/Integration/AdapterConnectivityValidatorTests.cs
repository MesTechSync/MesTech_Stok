using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// AdapterConnectivityValidator tests — concurrent ping, partial failure, timeout.
/// DEV3 TUR7-FULL: G10799 test gap kapatma.
/// </summary>
[Trait("Category", "Unit")]
public class AdapterConnectivityValidatorTests
{
    private readonly Mock<ILogger<AdapterConnectivityValidator>> _loggerMock = new();

    [Fact]
    public async Task ValidateAllAsync_AllReachable_ShouldReturnAllTrue()
    {
        var adapters = new[]
        {
            CreateMockPingable("Trendyol", true),
            CreateMockPingable("Hepsiburada", true),
            CreateMockPingable("N11", true),
        };

        var sut = new AdapterConnectivityValidator(adapters, _loggerMock.Object);
        var report = await sut.ValidateAllAsync();

        report.TotalCount.Should().Be(3);
        report.ReachableCount.Should().Be(3);
        report.UnreachableCount.Should().Be(0);
        report.AllReachable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAllAsync_PartialFailure_ShouldReportUnreachable()
    {
        var adapters = new[]
        {
            CreateMockPingable("Trendyol", true),
            CreateMockPingable("N11", false),
            CreateMockPingable("PttAVM", false),
        };

        var sut = new AdapterConnectivityValidator(adapters, _loggerMock.Object);
        var report = await sut.ValidateAllAsync();

        report.TotalCount.Should().Be(3);
        report.ReachableCount.Should().Be(1);
        report.UnreachableCount.Should().Be(2);
        report.AllReachable.Should().BeFalse();

        report.Results.Single(r => r.PlatformCode == "N11").IsReachable.Should().BeFalse();
        report.Results.Single(r => r.PlatformCode == "PttAVM").IsReachable.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAllAsync_ExceptionThrown_ShouldCatchAndMarkUnreachable()
    {
        var okAdapter = CreateMockPingable("Trendyol", true);
        var failAdapter = new Mock<IPingableAdapter>();
        failAdapter.Setup(a => a.PlatformCode).Returns("Broken");
        failAdapter.Setup(a => a.PingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("DNS resolution failed"));

        var sut = new AdapterConnectivityValidator(
            new[] { okAdapter, failAdapter.Object }, _loggerMock.Object);
        var report = await sut.ValidateAllAsync();

        report.TotalCount.Should().Be(2);
        report.ReachableCount.Should().Be(1);

        var broken = report.Results.Single(r => r.PlatformCode == "Broken");
        broken.IsReachable.Should().BeFalse();
        broken.Error.Should().Contain("DNS resolution failed");
    }

    [Fact]
    public async Task ValidateAllAsync_Empty_ShouldReturnEmptyReport()
    {
        var sut = new AdapterConnectivityValidator(
            Array.Empty<IPingableAdapter>(), _loggerMock.Object);
        var report = await sut.ValidateAllAsync();

        report.TotalCount.Should().Be(0);
        report.AllReachable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAllAsync_ShouldMeasureResponseTime()
    {
        var slowAdapter = new Mock<IPingableAdapter>();
        slowAdapter.Setup(a => a.PlatformCode).Returns("Slow");
        slowAdapter.Setup(a => a.PingAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) =>
            {
                await Task.Delay(100);
                return true;
            });

        var sut = new AdapterConnectivityValidator(
            new[] { slowAdapter.Object }, _loggerMock.Object);
        var report = await sut.ValidateAllAsync();

        report.Results[0].ResponseTime.TotalMilliseconds.Should().BeGreaterThan(50);
    }

    private static IPingableAdapter CreateMockPingable(string platform, bool reachable)
    {
        var mock = new Mock<IPingableAdapter>();
        mock.Setup(a => a.PlatformCode).Returns(platform);
        mock.Setup(a => a.PingAsync(It.IsAny<CancellationToken>())).ReturnsAsync(reachable);
        return mock.Object;
    }
}
