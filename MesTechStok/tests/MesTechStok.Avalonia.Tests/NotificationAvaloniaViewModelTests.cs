using FluentAssertions;
using MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class NotificationAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly NotificationAvaloniaViewModel _sut;

    public NotificationAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserNotificationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserNotificationListResult(
                Items: Array.Empty<UserNotificationDto>(),
                TotalCount: 0,
                Page: 1,
                PageSize: 20));
        _sut = new NotificationAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Notifications.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.IsEmpty.Should().BeTrue();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NotificationAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAllReadCommand_ShouldNotThrowWhenEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.MarkAllReadCommand.Execute(null);

        // Assert
        _sut.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.Notifications.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
    }
}
