using FluentAssertions;
using MesTech.Application.Features.System.Users;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class UserManagementAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly UserManagementAvaloniaViewModel _sut;

    public UserManagementAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new UserManagementAvaloniaViewModel(_mediatorMock.Object);
    }

    private static IReadOnlyList<UserListItemDto> CreateTestUsers() =>
    [
        new(Guid.NewGuid(), "admin", "Admin Kullanici", "admin@mestech.com", "Admin", true, DateTime.Now.AddHours(-1)),
        new(Guid.NewGuid(), "operator1", "Mehmet Kaya", "mehmet@mestech.com", "Operator", true, DateTime.Now.AddDays(-2)),
        new(Guid.NewGuid(), "viewer1", "Zeynep Arslan", null, "Viewer", false, null)
    ];

    [Fact]
    public async Task LoadAsync_ShouldPopulateUsersFromMediator()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUsers());

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Users.Should().HaveCount(3);
        _sut.TotalCount.Should().Be(3);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldMapUserFieldsCorrectly()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUsers());

        // Act
        await _sut.LoadAsync();

        // Assert
        var admin = _sut.Users.First(u => u.Username == "admin");
        admin.FullName.Should().Be("Admin Kullanici");
        admin.Email.Should().Be("admin@mestech.com");
        admin.Role.Should().Be("Admin");
        admin.Status.Should().Be("Aktif");
        admin.LastLogin.Should().NotBe("-");

        var inactive = _sut.Users.First(u => u.Username == "viewer1");
        inactive.Status.Should().Be("Pasif");
        inactive.Email.Should().BeEmpty();
        inactive.LastLogin.Should().Be("-");
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_ShouldSetErrorState()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Veritabani baglantisi kesildi"));

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("Veritabani baglantisi kesildi");
        _sut.IsLoading.Should().BeFalse();
        _sut.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WhenNoUsers_ShouldSetIsEmpty()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserListItemDto>() as IReadOnlyList<UserListItemDto>);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCount.Should().Be(0);
        _sut.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUsers());

        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(UserManagementAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }
}
