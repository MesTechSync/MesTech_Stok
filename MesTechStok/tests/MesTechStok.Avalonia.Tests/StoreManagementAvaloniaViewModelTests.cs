using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StoreManagementAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly StoreManagementAvaloniaViewModel _sut;

    public StoreManagementAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new StoreManagementAvaloniaViewModel(_mediatorMock.Object);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateStores()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Stores.Should().HaveCount(4);
        _sut.TotalCount.Should().Be(4);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedPlatforms()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Stores.Should().Contain(s => s.Platform == "Trendyol");
        _sut.Stores.Should().Contain(s => s.Platform == "Hepsiburada");
        _sut.Stores.Should().Contain(s => s.Platform == "N11");
        _sut.Stores.Should().Contain(s => s.Platform == "Ciceksepeti");
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StoreManagementAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_StoresShouldHaveApiStatusAndProductCount()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        var trendyol = _sut.Stores.First(s => s.Platform == "Trendyol");
        trendyol.ApiStatus.Should().Be("Bagli");
        trendyol.ProductCount.Should().Be(1250);
        trendyol.LastSync.Should().NotBeNullOrEmpty();

        var disconnected = _sut.Stores.First(s => s.Platform == "Ciceksepeti");
        disconnected.ApiStatus.Should().Be("Baglanti Kesildi");
    }

    [Fact]
    public void Constructor_ShouldInitializeDefaultState()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.Stores.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
    }
}
