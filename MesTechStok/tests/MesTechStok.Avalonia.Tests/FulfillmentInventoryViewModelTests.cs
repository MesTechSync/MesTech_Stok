using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentInventory;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FulfillmentInventoryViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly FulfillmentInventoryViewModel _sut;

    public FulfillmentInventoryViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        SetupDefaultMediatorResponses();
        _sut = new FulfillmentInventoryViewModel(_mediatorMock.Object);
    }

    private void SetupDefaultMediatorResponses()
    {
        var fbaInventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            new List<FulfillmentStock>
            {
                new("SKU-001", 50, 5, 10),
                new("SKU-002", 3, 0, 0)   // Kritik — total < 5
            },
            DateTime.UtcNow);

        var hepsiInventory = new FulfillmentInventory(
            FulfillmentCenter.Hepsilojistik,
            new List<FulfillmentStock>
            {
                new("SKU-001", 20, 2, 5), // same SKU as FBA — will merge
                new("SKU-003", 0, 0, 0)   // Stok Yok — total = 0
            },
            DateTime.UtcNow);

        var localInventory = new FulfillmentInventory(
            FulfillmentCenter.OwnWarehouse,
            new List<FulfillmentStock>
            {
                new("SKU-001", 10, 1, 0), // same SKU — 3-way merge
                new("SKU-004", 100, 10, 20)
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
            .ReturnsAsync(hepsiInventory);

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFulfillmentInventoryQuery>(q => q.Center == FulfillmentCenter.OwnWarehouse),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(localInventory);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SelectedTab.Should().Be("Tumu");
        _sut.SearchText.Should().BeEmpty();
        _sut.InventoryItems.Should().BeEmpty();
        _sut.TabAllWeight.Should().Be("Bold");
        _sut.TabFbaWeight.Should().Be("Normal");
    }

    [Fact]
    public async Task LoadAsync_ShouldMergeBySku()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — SKU-001 appears in all 3 centers, merged into 1 row
        // Unique SKUs: SKU-001, SKU-002, SKU-003, SKU-004 = 4 items
        _sut.InventoryItems.Should().HaveCount(4);

        var sku001 = _sut.InventoryItems.First(i => i.Sku == "SKU-001");
        sku001.FbaQty.Should().Be(50);
        sku001.HepsiQty.Should().Be(20);
        sku001.LocalQty.Should().Be(10);
        sku001.TotalQty.Should().Be(80); // 50 + 20 + 10
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateStatus()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        // SKU-001: total 80 → "Yeterli"
        _sut.InventoryItems.First(i => i.Sku == "SKU-001").Status.Should().Be("Yeterli");

        // SKU-002: FBA=3, others=0 → total 3 < 5 → "Kritik"
        _sut.InventoryItems.First(i => i.Sku == "SKU-002").Status.Should().Be("Kritik");

        // SKU-003: Hepsi=0, others=0 → total 0 → "Stok Yok"
        _sut.InventoryItems.First(i => i.Sku == "SKU-003").Status.Should().Be("Stok Yok");

        // SKU-004: Local=100 → total 100 → "Yeterli"
        _sut.InventoryItems.First(i => i.Sku == "SKU-004").Status.Should().Be("Yeterli");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange — track IsLoading transitions
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FulfillmentInventoryViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert — first true (start), then false (finally)
        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task SelectTab_AmazonFBA_ShouldFilterToFbaOnly()
    {
        // Arrange
        await _sut.LoadAsync();
        _sut.InventoryItems.Should().HaveCount(4);

        // Act — select AmazonFBA tab
        _sut.SelectTabCommand.Execute("AmazonFBA");

        // Assert — only items with FbaQty > 0
        _sut.InventoryItems.Should().HaveCount(2); // SKU-001 (50), SKU-002 (3)
        _sut.InventoryItems.Should().AllSatisfy(i => i.FbaQty.Should().BeGreaterThan(0));
        _sut.TabFbaWeight.Should().Be("Bold");
        _sut.TabAllWeight.Should().Be("Normal");
    }

    [Fact]
    public async Task SelectTab_Tumu_ShouldShowAll()
    {
        // Arrange — load and filter to FBA first
        await _sut.LoadAsync();
        _sut.SelectTabCommand.Execute("AmazonFBA");
        _sut.InventoryItems.Should().HaveCount(2);

        // Act — go back to Tumu tab
        _sut.SelectTabCommand.Execute("Tumu");

        // Assert — all 4 items shown again
        _sut.InventoryItems.Should().HaveCount(4);
        _sut.TabAllWeight.Should().Be("Bold");
        _sut.TabFbaWeight.Should().Be("Normal");
    }

    [Fact]
    public async Task SearchText_ShouldFilterBySku()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for SKU-004
        _sut.SearchText = "SKU-004";

        // Assert
        _sut.InventoryItems.Should().HaveCount(1);
        _sut.InventoryItems[0].Sku.Should().Be("SKU-004");
        _sut.InventoryItems[0].LocalQty.Should().Be(100);
    }

    [Fact]
    public async Task LoadAsync_Error_ShouldSetErrorState()
    {
        // Arrange — mediator throws
        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetFulfillmentInventoryQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database timeout"));

        var sut = new FulfillmentInventoryViewModel(_mediatorMock.Object);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("Envanter verileri yuklenemedi");
        sut.ErrorMessage.Should().Contain("Database timeout");
        sut.IsLoading.Should().BeFalse();
    }
}
