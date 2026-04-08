using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetAuditLogsHandlerTests
{
    private readonly Mock<IAccessLogRepository> _repository = new();

    private GetAuditLogsHandler CreateHandler() => new(_repository.Object);

    [Fact]
    public async Task Handle_WithResults_ShouldReturnAuditLogs()
    {
        var tenantId = Guid.NewGuid();
        var logs = new List<AccessLog>
        {
            new() { TenantId = tenantId, UserId = Guid.NewGuid(), Action = "Login", Resource = "/dashboard" },
            new() { TenantId = tenantId, UserId = Guid.NewGuid(), Action = "Export", Resource = "/stock" }
        }.AsReadOnly();

        _repository.Setup(r => r.GetPagedAsync(
            tenantId, null, null, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        var handler = CreateHandler();
        var query = new GetAuditLogsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Action.Should().Be("Login");
        result[1].Action.Should().Be("Export");
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyList()
    {
        var tenantId = Guid.NewGuid();
        _repository.Setup(r => r.GetPagedAsync(
            tenantId, null, null, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessLog>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetAuditLogsQuery(tenantId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithFilters_ShouldPassCorrectParameters()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        _repository.Setup(r => r.GetPagedAsync(
            tenantId, from, to, userId, "Login", 2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessLog>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetAuditLogsQuery(tenantId, from, to, userId.ToString(), "Login", 2, 25);

        await handler.Handle(query, CancellationToken.None);

        _repository.Verify(r => r.GetPagedAsync(
            tenantId, from, to, userId, "Login", 2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PageSizeExceeds100_ShouldClampTo100()
    {
        var tenantId = Guid.NewGuid();
        _repository.Setup(r => r.GetPagedAsync(
            tenantId, null, null, null, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessLog>().AsReadOnly());

        var handler = CreateHandler();
        var query = new GetAuditLogsQuery(tenantId, PageSize: 999);

        await handler.Handle(query, CancellationToken.None);

        _repository.Verify(r => r.GetPagedAsync(
            tenantId, null, null, null, null, 1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }
}
