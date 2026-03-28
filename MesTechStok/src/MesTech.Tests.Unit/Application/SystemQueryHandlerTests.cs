using FluentAssertions;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Interfaces;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Application.Features.System.Users;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class SystemQueryHandlerTests
{
    [Fact]
    public async Task GetAuditLogs_ReturnsEmptyList()
    {
        var accessLogRepo = new Mock<IAccessLogRepository>();
        accessLogRepo.Setup(r => r.GetPagedAsync(
            It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.AccessLog>().AsReadOnly());
        var handler = new GetAuditLogsHandler(accessLogRepo.Object);
        var result = await handler.Handle(
            new GetAuditLogsQuery(TenantId: Guid.NewGuid(), Page: 1, PageSize: 50),
            CancellationToken.None);
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBackupHistory_ReturnsEmptyList()
    {
        var logger = new Mock<ILogger<GetBackupHistoryHandler>>();
        var handler = new GetBackupHistoryHandler(logger.Object);
        var result = await handler.Handle(
            new GetBackupHistoryQuery(TenantId: Guid.NewGuid(), Limit: 20),
            CancellationToken.None);
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsers_ReturnsListFromRepo()
    {
        var userRepo = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<GetUsersHandler>>();
        userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var handler = new GetUsersHandler(userRepo.Object, logger.Object);
        var result = await handler.Handle(
            new GetUsersQuery(TenantId: null),
            CancellationToken.None);
        result.Should().NotBeNull();
    }
}
