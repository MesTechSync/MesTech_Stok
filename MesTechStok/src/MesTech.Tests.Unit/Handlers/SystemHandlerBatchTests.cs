using FluentAssertions;
using MesTech.Application.Features.AI.Commands.GenerateProductDescription;
using MesTech.Application.Features.System.Kvkk.Queries.GetKvkkAuditLogs;
using MesTech.Application.Features.System.LaunchReadiness;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.System.Queries.GetBackupHistory;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;
using MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using MesTech.Application.Features.System.Users;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — System Handler Batch Tests (9 System + 1 AI)
// ═══════════════════════════════════════════════════════════════

#region GetKvkkAuditLogsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetKvkkAuditLogsHandlerBatchTests
{
    private readonly Mock<IKvkkAuditLogRepository> _repoMock = new();
    private readonly GetKvkkAuditLogsHandler _sut;

    public GetKvkkAuditLogsHandlerBatchTests()
    {
        _sut = new GetKvkkAuditLogsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsZeroItems()
    {
        var tenantId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByTenantPagedAsync(tenantId, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<KvkkAuditLog>(), 0));

        var result = await _sut.Handle(new GetKvkkAuditLogsQuery(tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _repoMock.Verify(r => r.GetByTenantPagedAsync(tenantId, 1, 50, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetLaunchReadinessHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetLaunchReadinessHandlerBatchTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly GetLaunchReadinessHandler _sut;

    public GetLaunchReadinessHandlerBatchTests()
    {
        _sut = new GetLaunchReadinessHandler(
            _productRepoMock.Object,
            _orderRepoMock.Object,
            Mock.Of<ILogger<GetLaunchReadinessHandler>>());
    }

    [Fact]
    public async Task Handle_ReturnsReadinessDto_WithCriteria()
    {
        _productRepoMock.Setup(r => r.GetCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(10);

        var result = await _sut.Handle(
            new GetLaunchReadinessQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
        result.Criteria.Should().NotBeEmpty();
        result.PassedCriteria.Should().BeGreaterThan(0);
        _productRepoMock.Verify(r => r.GetCountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetAuditLogsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetAuditLogsHandlerBatchTests
{
    private readonly Mock<IAccessLogRepository> _repoMock = new();
    private readonly GetAuditLogsHandler _sut;

    public GetAuditLogsHandlerBatchTests()
    {
        _sut = new GetAuditLogsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyLogs_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetPagedAsync(
                tenantId, null, null, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccessLog>().AsReadOnly());

        var result = await _sut.Handle(
            new GetAuditLogsQuery(tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetPagedAsync(
            tenantId, null, null, null, null, 1, 50, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetBackupHistoryHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetBackupHistoryHandlerBatchTests
{
    private readonly Mock<IBackupEntryRepository> _repoMock = new();
    private readonly GetBackupHistoryHandler _sut;

    public GetBackupHistoryHandlerBatchTests()
    {
        _sut = new GetBackupHistoryHandler(
            _repoMock.Object,
            Mock.Of<ILogger<GetBackupHistoryHandler>>());
    }

    [Fact]
    public async Task Handle_EmptyHistory_ReturnsEmptyList()
    {
        var tenantId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BackupEntry>());

        var result = await _sut.Handle(
            new GetBackupHistoryQuery(tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetByTenantAsync(tenantId, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region MarkAllUserNotificationsReadHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class MarkAllUserNotificationsReadHandlerBatchTests
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MarkAllUserNotificationsReadHandler _sut;

    public MarkAllUserNotificationsReadHandlerBatchTests()
    {
        _sut = new MarkAllUserNotificationsReadHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsMarkAllAsRead()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.MarkAllAsReadAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var result = await _sut.Handle(
            new MarkAllUserNotificationsReadCommand(tenantId, userId), CancellationToken.None);

        result.Should().Be(5);
        _repoMock.Verify(r => r.MarkAllAsReadAsync(tenantId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region MarkUserNotificationReadHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class MarkUserNotificationReadHandlerBatchTests
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly MarkUserNotificationReadHandler _sut;

    public MarkUserNotificationReadHandlerBatchTests()
    {
        _sut = new MarkUserNotificationReadHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsFalse()
    {
        var tenantId = Guid.NewGuid();
        var notifId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(notifId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserNotification?)null);

        var result = await _sut.Handle(
            new MarkUserNotificationReadCommand(tenantId, notifId), CancellationToken.None);

        result.Should().BeFalse();
        _repoMock.Verify(r => r.GetByIdAsync(notifId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetUnreadNotificationCountHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetUnreadNotificationCountHandlerBatchTests
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly GetUnreadNotificationCountHandler _sut;

    public GetUnreadNotificationCountHandlerBatchTests()
    {
        _sut = new GetUnreadNotificationCountHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsUnreadCount()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetUnreadCountAsync(tenantId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var result = await _sut.Handle(
            new GetUnreadNotificationCountQuery(tenantId, userId), CancellationToken.None);

        result.Should().Be(3);
        _repoMock.Verify(r => r.GetUnreadCountAsync(tenantId, userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetUserNotificationsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetUserNotificationsHandlerBatchTests
{
    private readonly Mock<IUserNotificationRepository> _repoMock = new();
    private readonly GetUserNotificationsHandler _sut;

    public GetUserNotificationsHandlerBatchTests()
    {
        _sut = new GetUserNotificationsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyNotifications_ReturnsEmptyResult()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetPagedAsync(tenantId, userId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserNotification>(), 0));

        var result = await _sut.Handle(
            new GetUserNotificationsQuery(tenantId, userId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _repoMock.Verify(r => r.GetPagedAsync(tenantId, userId, 1, 20, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetUsersHandler

[Trait("Category", "Unit")]
[Trait("Feature", "System")]
public class GetUsersHandlerBatchTests
{
    private readonly Mock<IUserRepository> _repoMock = new();
    private readonly GetUsersHandler _sut;

    public GetUsersHandlerBatchTests()
    {
        _sut = new GetUsersHandler(
            _repoMock.Object,
            Mock.Of<ILogger<GetUsersHandler>>());
    }

    [Fact]
    public async Task Handle_NoUsers_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var result = await _sut.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GenerateProductDescriptionHandler

[Trait("Category", "Unit")]
[Trait("Feature", "AI")]
public class GenerateProductDescriptionHandlerBatchTests
{
    private readonly Mock<IMesaAIService> _mesaAIMock = new();
    private readonly GenerateProductDescriptionHandler _sut;

    public GenerateProductDescriptionHandlerBatchTests()
    {
        _sut = new GenerateProductDescriptionHandler(
            _mesaAIMock.Object,
            Mock.Of<ILogger<GenerateProductDescriptionHandler>>());
    }

    [Fact]
    public async Task Handle_AIFailure_ReturnsErrorResult()
    {
        _mesaAIMock.Setup(s => s.GenerateProductDescriptionAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiContentResult(false, null, "AI unavailable", null));

        var cmd = new GenerateProductDescriptionCommand(
            Guid.NewGuid(), Guid.NewGuid(), "Test Product", "Electronics", "TestBrand", null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("AI unavailable");
        _mesaAIMock.Verify(s => s.GenerateProductDescriptionAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<List<string>?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
