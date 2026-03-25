using FluentAssertions;
using MesTech.Application.Features.Logging.Queries.GetLogs;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for logging query handlers.
/// </summary>
[Trait("Category", "Unit")]
public class LoggingQueryHandlerTests
{
    private readonly Mock<ILogEntryRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ GetLogCountHandler ═══════

    [Fact]
    public async Task GetLogCount_CallsRepository()
    {
        _repo.Setup(r => r.GetCountAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);

        var sut = new GetLogCountHandler(_repo.Object);
        var result = await sut.Handle(new GetLogCountQuery(_tenantId), CancellationToken.None);

        result.Should().Be(42);
    }

    // ═══════ GetLogsHandler ═══════

    [Fact]
    public async Task GetLogs_CallsPagedRepository()
    {
        _repo.Setup(r => r.GetPagedAsync(
                _tenantId, 1, 20, null, null, null, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LogEntry>().AsReadOnly());

        var sut = new GetLogsHandler(_repo.Object);
        var query = new GetLogsQuery(_tenantId, 1, 20);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }
}
