using FluentAssertions;
using MesTech.Application.Features.Platform.Queries.GetSyncHistory;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetSyncHistoryHandlerTests
{
    private readonly Mock<ISyncLogRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetSyncHistoryHandler CreateSut() => new(_repo.Object);

    [Fact]
    public async Task Handle_EmptyLogs_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetRecentAsync(_tenantId, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog>().AsReadOnly());

        var result = await CreateSut().Handle(
            new GetSyncHistoryQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithLogs_MapsPlatformCodeAndDirection()
    {
        var log = new SyncLog { TenantId = _tenantId, PlatformCode = "Trendyol", Direction = SyncDirection.Pull, EntityType = "Product", IsSuccess = true };
        _repo.Setup(r => r.GetRecentAsync(_tenantId, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog> { log }.AsReadOnly());

        var result = await CreateSut().Handle(
            new GetSyncHistoryQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].PlatformCode.Should().Be("Trendyol");
        result[0].Direction.Should().Be("Pull");
        result[0].EntityType.Should().Be("Product");
    }

    [Fact]
    public async Task Handle_CompletedLog_MapsDurationFormatted()
    {
        var log = new SyncLog { TenantId = _tenantId, PlatformCode = "N11", Direction = SyncDirection.Push, EntityType = "Order", IsSuccess = true };
        log.MarkAsCompleted(50, 2); // 50 processed, 2 failed
        _repo.Setup(r => r.GetRecentAsync(_tenantId, 10, "N11", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog> { log }.AsReadOnly());

        var result = await CreateSut().Handle(
            new GetSyncHistoryQuery(_tenantId, PlatformFilter: "N11", Count: 10), CancellationToken.None);

        result.Should().HaveCount(1);
        // IsSuccess = (failed == 0) → 2 failed means false
        result[0].IsSuccess.Should().BeFalse();
        result[0].ItemsProcessed.Should().Be(50);
        result[0].ItemsFailed.Should().Be(2);
        result[0].CompletedAt.Should().NotBeNull();
        result[0].Duration.Should().NotBeNull(); // mm:ss format
    }

    [Fact]
    public async Task Handle_FailedLog_MapsErrorMessage()
    {
        var log = new SyncLog { TenantId = _tenantId, PlatformCode = "Amazon", Direction = SyncDirection.Pull, EntityType = "Stock" };
        log.MarkAsFailed("API rate limit exceeded");
        _repo.Setup(r => r.GetRecentAsync(_tenantId, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SyncLog> { log }.AsReadOnly());

        var result = await CreateSut().Handle(
            new GetSyncHistoryQuery(_tenantId), CancellationToken.None);

        result[0].IsSuccess.Should().BeFalse();
        result[0].ErrorMessage.Should().Contain("rate limit");
    }
}
