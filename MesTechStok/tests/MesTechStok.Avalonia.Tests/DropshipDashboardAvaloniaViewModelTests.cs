using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
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
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetDropshipDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MesTech.Application.DTOs.Platform.DropshipDashboardDto());
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetDropshipSuppliersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<MesTech.Application.DTOs.Dropshipping.DropshipSupplierDto>)Array.Empty<MesTech.Application.DTOs.Dropshipping.DropshipSupplierDto>());
        _sut = new DropshipDashboardAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.TotalOrders.Should().Be(0);
        _sut.TotalRevenue.Should().Be(0);
        _sut.TotalProfit.Should().Be(0);
        _sut.AverageMargin.Should().Be(0);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldHaveEmptyCollections()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Suppliers.Should().BeEmpty();
        _sut.TopProfitableProducts.Should().BeEmpty();
        _sut.SupplierCount.Should().Be(0);
        _sut.ActiveProductCount.Should().Be(0);
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

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.TotalOrders.Should().Be(0);
        _sut.TotalRevenue.Should().Be(0);
    }
}
