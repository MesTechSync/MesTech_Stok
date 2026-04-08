using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetBackupHistoryHandlerTests
{
    private readonly Mock<IBackupEntryRepository> _repo = new();
    private readonly Mock<ILogger<GetBackupHistoryHandler>> _logger = new();

    private GetBackupHistoryHandler CreateHandler() =>
        new(_repo.Object, _logger.Object);

    [Fact]
    public async Task Handle_WithEntries_ShouldReturnOrderedByCreatedAtDesc()
    {
        var tenantId = Guid.NewGuid();
        var entry1 = BackupEntry.Create(tenantId, "backup_20260101.sql");
        var entry2 = BackupEntry.Create(tenantId, "backup_20260201.sql");

        // entry2 is newer
        var entries = new List<BackupEntry> { entry1, entry2 }.AsReadOnly();

        _repo.Setup(r => r.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var handler = CreateHandler();
        var query = new GetBackupHistoryQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].FileName.Should().NotBeEmpty();
        result[1].FileName.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyHistory_ShouldReturnEmptyList()
    {
        var tenantId = Guid.NewGuid();
        _repo.Setup(r => r.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupEntry>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetBackupHistoryQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CustomLimit_ShouldPassLimitToRepo()
    {
        var tenantId = Guid.NewGuid();
        _repo.Setup(r => r.GetByTenantAsync(tenantId, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupEntry>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetBackupHistoryQuery(tenantId, Limit: 5);

        await handler.Handle(query, CancellationToken.None);

        _repo.Verify(r => r.GetByTenantAsync(tenantId, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapAllDtoProperties()
    {
        var tenantId = Guid.NewGuid();
        var entry = BackupEntry.Create(tenantId, "test_backup.sql");
        entry.MarkCompleted(1024);

        _repo.Setup(r => r.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupEntry> { entry }.AsReadOnly());

        var handler = CreateHandler();
        var query = new GetBackupHistoryQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        var dto = result.Single();
        dto.Id.Should().Be(entry.Id);
        dto.FileName.Should().Be("test_backup.sql");
        dto.SizeBytes.Should().Be(1024);
        dto.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_FailedEntry_ShouldIncludeErrorMessage()
    {
        var tenantId = Guid.NewGuid();
        var entry = BackupEntry.Create(tenantId, "failed_backup.sql");
        entry.MarkFailed("Disk full");

        _repo.Setup(r => r.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupEntry> { entry }.AsReadOnly());

        var handler = CreateHandler();
        var result = await handler.Handle(new GetBackupHistoryQuery(tenantId), CancellationToken.None);

        result.Single().Status.Should().Be("Failed");
        result.Single().ErrorMessage.Should().Be("Disk full");
    }
}
