using FluentAssertions;
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
        _sut = new NotificationAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateNotifications()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Notifications.Should().HaveCount(5);
        _sut.TotalCount.Should().Be(5);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedNotificationTypes()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Notifications.Should().Contain(n => n.Title == "Stok Uyarisi");
        _sut.Notifications.Should().Contain(n => n.Title == "Yeni Siparis");
        _sut.Notifications.Should().Contain(n => n.Title == "Kargo Teslim");
        _sut.Notifications.Should().Contain(n => n.Title == "Fiyat Guncelleme");
        _sut.Notifications.Should().Contain(n => n.Title == "Sistem Bakimi");
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
    public async Task MarkAllReadCommand_ShouldSetAllColorsToMuted()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.MarkAllReadCommand.Execute(null);

        // Assert
        _sut.Notifications.Should().OnlyContain(n => n.StatusColor == "#94A3B8");
    }

    [Fact]
    public async Task LoadAsync_NotificationsHaveDistinctStatusColors()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — before MarkAllRead, at least some have non-muted colors
        _sut.Notifications.Should().Contain(n => n.StatusColor == "#EF4444", "critical alert should be red");
        _sut.Notifications.Should().Contain(n => n.StatusColor == "#059669", "success should be green");
        _sut.Notifications.Should().Contain(n => n.StatusColor == "#F59E0B", "warning should be amber");
    }
}
