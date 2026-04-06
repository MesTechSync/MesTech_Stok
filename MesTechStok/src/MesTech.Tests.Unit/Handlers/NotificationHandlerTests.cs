using FluentAssertions;
using MesTech.Application.Features.Notifications.Queries.GetNotifications;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tests for notification handlers.
/// </summary>
[Trait("Category", "Unit")]
public class NotificationHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════ GetNotificationsHandler ═══════

    [Fact]
    public async Task GetNotifications_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<INotificationLogRepository>();
        var sut = new GetNotificationsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetNotifications_EmptyRepo_ReturnsEmptyResult()
    {
        var repo = new Mock<INotificationLogRepository>();
        repo.Setup(r => r.GetPagedAsync(_tenantId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<MesTech.Domain.Entities.NotificationLog>(), 0));

        var sut = new GetNotificationsHandler(repo.Object);
        var result = await sut.Handle(
            new GetNotificationsQuery(_tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_PassesFilterToRepo()
    {
        var repo = new Mock<INotificationLogRepository>();
        repo.Setup(r => r.GetPagedAsync(_tenantId, 1, 20, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<MesTech.Domain.Entities.NotificationLog>(), 0));

        var sut = new GetNotificationsHandler(repo.Object);
        await sut.Handle(
            new GetNotificationsQuery(_tenantId, UnreadOnly: true), CancellationToken.None);

        repo.Verify(r => r.GetPagedAsync(_tenantId, 1, 20, true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
