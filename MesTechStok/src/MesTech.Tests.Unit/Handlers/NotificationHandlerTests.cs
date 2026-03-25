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
    // ═══════ GetNotificationsHandler ═══════

    [Fact]
    public async Task GetNotifications_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<INotificationLogRepository>();
        var sut = new GetNotificationsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }
}
