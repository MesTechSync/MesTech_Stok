using FluentAssertions;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetNotificationSettingsHandlerTests
{
    private readonly Mock<INotificationSettingRepository> _repoMock = new();
    private readonly GetNotificationSettingsHandler _sut;

    public GetNotificationSettingsHandlerTests()
    {
        _sut = new GetNotificationSettingsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSettings()
    {
        var userId = Guid.NewGuid();
        var settings = new List<NotificationSetting>
        {
            new NotificationSetting { Id = Guid.NewGuid(), UserId = userId, Channel = NotificationChannel.Email, IsEnabled = true },
            new NotificationSetting { Id = Guid.NewGuid(), UserId = userId, Channel = NotificationChannel.Push, IsEnabled = false }
        };
        _repoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings.AsReadOnly());

        var query = new GetNotificationSettingsQuery(Guid.NewGuid(), userId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
