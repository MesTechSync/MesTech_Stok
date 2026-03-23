using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FulfillmentDashboardViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly FulfillmentDashboardViewModel _sut;

    public FulfillmentDashboardViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        SetupDefaultMediatorResponses();
        _sut = new FulfillmentDashboardViewModel(_mediatorMock.Object);
    }

    private void SetupDefaultMediatorResponses()
    {
        var fbaInventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            new List<FulfillmentStock>
            {
                new("SKU-001", 50, 5, 10),
                new("SKU-002", 30, 2, 0)
            },
            DateTime.UtcNow);

        var hlInventory = new FulfillmentInventory(
            FulfillmentCenter.Hepsilojistik,
            new List<FulfillmentStock>
            {
                new("SKU-003", 20, 1, 5),
                new("SKU-004", 15, 0, 0)
            },
            DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFulfillmentInventoryQuery>(q => q.Center == FulfillmentCenter.AmazonFBA),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fbaInventory);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFulfillmentInventoryQuery>(q => q.Center == FulfillmentCenter.Hepsilojistik),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(hlInventory);

        var fbaOrders = new List<FulfillmentOrderResult>
        {
            new("ORD-001", "SHIPPED", new List<FulfillmentOrderItem> { new("SKU-001", 2, 2) },
                DateTime.UtcNow.AddDays(-1), "TRK-001", "UPS"),
            new("ORD-002", "IN_TRANSIT", new List<FulfillmentOrderItem> { new("SKU-002", 1, 0) },
                DateTime.UtcNow.AddDays(-2), "TRK-002", "DHL"),
            new("ORD-003", "ERROR", new List<FulfillmentOrderItem> { new("SKU-001", 3, 0) })
        };

        var hlOrders = new List<FulfillmentOrderResult>
        {
            new("ORD-004", "PROCESSING", new List<FulfillmentOrderItem> { new("SKU-003", 1, 1) },
                DateTime.UtcNow, "TRK-004", "Aras"),
            new("ORD-005", "CANCELLED", new List<FulfillmentOrderItem> { new("SKU-004", 5, 0) })
        };

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFulfillmentOrdersQuery>(q => q.Center == FulfillmentCenter.AmazonFBA),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<FulfillmentOrderResult>)fbaOrders);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFulfillmentOrdersQuery>(q => q.Center == FulfillmentCenter.Hepsilojistik),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<FulfillmentOrderResult>)hlOrders);
    }

    [Fact]
    public void Constructor_ShouldSetTitle()
    {
        // Assert
        _sut.Title.Should().Be("Fulfillment Yonetimi");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateInventory()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — FBA (2 items) + HL (2 items) = 4 merged
        _sut.InventoryItems.Should().HaveCount(4);
        _sut.InventoryItems.Select(i => i.SKU).Should()
            .Contain("SKU-001")
            .And.Contain("SKU-003");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateStockTotals()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — FBA: 50 + 30 = 80, HL: 20 + 15 = 35
        _sut.FbaStockTotal.Should().Be(80);
        _sut.HlStockTotal.Should().Be(35);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateRecentOrders()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 3 FBA + 2 HL = 5 combined orders
        _sut.RecentOrders.Should().HaveCount(5);
        _sut.RecentOrders.Select(o => o.OrderId).Should()
            .Contain("ORD-001")
            .And.Contain("ORD-004");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateShipmentCounts()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        // Active: SHIPPED (ORD-001) + PROCESSING (ORD-004) = 2
        _sut.ActiveShipments.Should().Be(2);
        // Transit: IN_TRANSIT (ORD-002) = 1
        _sut.TransitShipments.Should().Be(1);
        // Problem: ERROR (ORD-003) + CANCELLED (ORD-005) = 2
        _sut.ProblemShipments.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_EmptyData_ShouldSetIsEmpty()
    {
        // Arrange — empty responses
        var emptyInventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            Array.Empty<FulfillmentStock>(),
            DateTime.UtcNow);

        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetFulfillmentInventoryQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyInventory);

        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetFulfillmentOrdersQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<FulfillmentOrderResult>)new List<FulfillmentOrderResult>());

        var sut = new FulfillmentDashboardViewModel(_mediatorMock.Object);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.InventoryItems.Should().BeEmpty();
        sut.RecentOrders.Should().BeEmpty();
        sut.FbaStockTotal.Should().Be(0);
        sut.HlStockTotal.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_Error_ShouldSetErrorState()
    {
        // Arrange — mediator throws
        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetFulfillmentInventoryQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        var sut = new FulfillmentDashboardViewModel(_mediatorMock.Object);

        // Act
        await sut.LoadAsync();

        // Assert — SafeExecuteAsync catches and sets error
        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("Fulfillment verileri yuklenirken hata");
        sut.ErrorMessage.Should().Contain("Connection refused");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshCommand_ShouldReloadData()
    {
        // Arrange — load initial data
        await _sut.LoadAsync();
        _sut.InventoryItems.Should().HaveCount(4);

        // Verify mediator was called for inventory queries
        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetFulfillmentInventoryQuery>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        // Act — refresh
        await _sut.RefreshCommand.ExecuteAsync(null);

        // Assert — mediator called again (2 more inventory queries)
        _mediatorMock.Verify(m => m.Send(
            It.IsAny<GetFulfillmentInventoryQuery>(),
            It.IsAny<CancellationToken>()), Times.Exactly(4));
        _sut.InventoryItems.Should().HaveCount(4);
    }
}
