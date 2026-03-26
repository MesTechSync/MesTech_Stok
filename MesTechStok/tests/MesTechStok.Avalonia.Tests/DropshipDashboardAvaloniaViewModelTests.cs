using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DropshipDashboardAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DropshipDashboardAvaloniaViewModel _sut;

    public DropshipDashboardAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new DropshipDashboardAvaloniaViewModel();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.TotalOrders.Should().Be(347);
        _sut.TotalRevenue.Should().Be(284_500.00m);
        _sut.TotalProfit.Should().Be(42_675.00m);
        _sut.AverageMargin.Should().Be(15.0m);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateEnhancedKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.SupplierCount.Should().Be(12);
        _sut.ActiveSupplierText.Should().Contain("aktif");
        _sut.ActiveProductCount.Should().Be(1_284);
        _sut.ProductGrowthText.Should().Contain("+42");
        _sut.AverageDeliveryDays.Should().BeApproximately(2.8, 0.01);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateSuppliersAndProducts()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Suppliers.Should().HaveCount(4);
        _sut.Suppliers[0].SupplierName.Should().Be("ABC Elektronik");
        _sut.Suppliers[0].FulfillRate.Should().BeApproximately(97.2, 0.1);

        _sut.TopProfitableProducts.Should().HaveCount(10);
        _sut.TopProfitableProducts[0].ProductName.Should().Contain("Samsung Galaxy S24");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetAutoOrderDefaults()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsAutoOrderEnabled.Should().BeTrue();
        _sut.AutoOrderThreshold.Should().Be(5);
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DropshipDashboardAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }
}
