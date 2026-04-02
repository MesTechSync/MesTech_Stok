using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Jobs;

/// <summary>
/// SettlementSyncJob multi-platform tests.
/// G492: parallel platform settlement sync, timeout, fail-safe.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Jobs")]
[Trait("Group", "Settlement")]
public class SettlementSyncJobTests
{
    private readonly Mock<IAdapterFactory> _factoryMock;
    private readonly Mock<ILogger<SettlementSyncJob>> _loggerMock;
    private readonly SettlementSyncJob _job;

    public SettlementSyncJobTests()
    {
        _factoryMock = new Mock<IAdapterFactory>();
        _loggerMock = new Mock<ILogger<SettlementSyncJob>>();
        _job = new SettlementSyncJob(_factoryMock.Object, _loggerMock.Object);
    }

    private Mock<T> CreateAdapter<T>(string platformCode) where T : class, ISettlementCapableAdapter, IIntegratorAdapter
    {
        var mock = new Mock<T>();
        mock.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns(platformCode);
        return mock;
    }

    // ─── Basic Properties ───

    [Fact]
    public void JobId_ReturnsSettlementSync()
    {
        _job.JobId.Should().Be("settlement-sync");
    }

    [Fact]
    public void CronExpression_RunsDailyAt3AM()
    {
        _job.CronExpression.Should().Be("0 3 * * *");
    }

    // ─── No Adapters ───

    [Fact]
    public async Task ExecuteAsync_NoAdapters_LogsWarningAndReturns()
    {
        // Arrange
        _factoryMock.Setup(f => f.GetAll())
            .Returns(new List<IIntegratorAdapter>());

        // Act
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert — should log warning, not throw
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("bulunamadi")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ─── Multi-Platform Success ───

    [Fact]
    public async Task ExecuteAsync_TwoPlatforms_BothSucceed()
    {
        // Arrange
        var trendyol = new Mock<ISettlementCapableAdapter>();
        var hepsiburada = new Mock<ISettlementCapableAdapter>();

        var trendyolAdapter = trendyol.As<IIntegratorAdapter>();
        trendyolAdapter.Setup(a => a.PlatformCode).Returns("trendyol");
        var hbAdapter = hepsiburada.As<IIntegratorAdapter>();
        hbAdapter.Setup(a => a.PlatformCode).Returns("hepsiburada");

        trendyol.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettlementDto { PlatformCode = "trendyol", TotalSales = 10000m });

        hepsiburada.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettlementDto { PlatformCode = "hepsiburada", TotalSales = 5000m });

        // Both implement ISettlementCapableAdapter and IIntegratorAdapter
        var adapters = new List<IIntegratorAdapter>
        {
            (IIntegratorAdapter)trendyol.Object,
            (IIntegratorAdapter)hepsiburada.Object
        };
        _factoryMock.Setup(f => f.GetAll()).Returns(adapters);

        // Act
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert — both should be called
        trendyol.Verify(a => a.GetSettlementAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        hepsiburada.Verify(a => a.GetSettlementAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Platform Timeout ───

    [Fact]
    public async Task ExecuteAsync_PlatformTimeout_ReturnsFalseForThat()
    {
        // Arrange — one adapter that times out
        var slowAdapter = new Mock<ISettlementCapableAdapter>();
        slowAdapter.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns("slow_platform");
        slowAdapter.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns<DateTime, DateTime, CancellationToken>(async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromMinutes(5), ct); // Will be cancelled by 60s timeout
                return null;
            });

        var adapters = new List<IIntegratorAdapter> { (IIntegratorAdapter)slowAdapter.Object };
        _factoryMock.Setup(f => f.GetAll()).Returns(adapters);

        // Act — should not throw, job should complete
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert — timeout logged as warning
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("TIMEOUT")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ─── Platform Exception ───

    [Fact]
    public async Task ExecuteAsync_PlatformThrows_OthersContinue()
    {
        // Arrange — failing + succeeding adapters
        var failAdapter = new Mock<ISettlementCapableAdapter>();
        failAdapter.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns("fail_platform");
        failAdapter.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        var goodAdapter = new Mock<ISettlementCapableAdapter>();
        goodAdapter.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns("good_platform");
        goodAdapter.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SettlementDto { PlatformCode = "good_platform", TotalSales = 1000m });

        var adapters = new List<IIntegratorAdapter>
        {
            (IIntegratorAdapter)failAdapter.Object,
            (IIntegratorAdapter)goodAdapter.Object
        };
        _factoryMock.Setup(f => f.GetAll()).Returns(adapters);

        // Act — should NOT throw
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert — both called; error logged for fail, success for good
        failAdapter.Verify(a => a.GetSettlementAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        goodAdapter.Verify(a => a.GetSettlementAsync(
            It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("HATA")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ─── Null Settlement Result ───

    [Fact]
    public async Task ExecuteAsync_NullSettlement_StillReturnsTrue()
    {
        // Arrange — adapter returns null (no settlement data)
        var adapter = new Mock<ISettlementCapableAdapter>();
        adapter.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns("empty_platform");
        adapter.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementDto?)null);

        var adapters = new List<IIntegratorAdapter> { (IIntegratorAdapter)adapter.Object };
        _factoryMock.Setup(f => f.GetAll()).Returns(adapters);

        // Act
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert — still counts as success (no error logged)
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ─── Global Cancellation ───

    [Fact]
    public async Task ExecuteAsync_GlobalCancellation_Propagates()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel(); // pre-cancelled

        var adapter = new Mock<ISettlementCapableAdapter>();
        adapter.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns("any");
        adapter.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns<DateTime, DateTime, CancellationToken>((_, _, ct) => Task.FromCanceled<SettlementDto?>(ct));

        var adapters = new List<IIntegratorAdapter> { (IIntegratorAdapter)adapter.Object };
        _factoryMock.Setup(f => f.GetAll()).Returns(adapters);

        // Act & Assert — global cancellation should propagate
        // When the global token is cancelled, the exception is NOT caught by the timeout handler
        // (because ct.IsCancellationRequested is true), so it bubbles up
        // The Task.WhenAll wraps it, but the job should log error
        await _job.ExecuteAsync(cts.Token);

        // Should log error (not timeout warning)
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("HATA")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ─── Date Range Verification ───

    [Fact]
    public async Task ExecuteAsync_PassesYesterdayToday()
    {
        // Arrange
        DateTime? capturedStart = null;
        DateTime? capturedEnd = null;

        var adapter = new Mock<ISettlementCapableAdapter>();
        adapter.As<IIntegratorAdapter>().Setup(a => a.PlatformCode).Returns("trendyol");
        adapter.Setup(a => a.GetSettlementAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, CancellationToken>((s, e, _) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync(new SettlementDto());

        var adapters = new List<IIntegratorAdapter> { (IIntegratorAdapter)adapter.Object };
        _factoryMock.Setup(f => f.GetAll()).Returns(adapters);

        // Act
        await _job.ExecuteAsync(CancellationToken.None);

        // Assert
        capturedStart.Should().Be(DateTime.UtcNow.Date.AddDays(-1));
        capturedEnd.Should().Be(DateTime.UtcNow.Date);
    }
}
