using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentOrders;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class FulfillmentInboundViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly FulfillmentInboundViewModel _sut;

    public FulfillmentInboundViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        SetupDefaultMediatorResponses();
        _sut = new FulfillmentInboundViewModel(_mediatorMock.Object);
    }

    private void SetupDefaultMediatorResponses()
    {
        // AmazonFBA orders
        var fbaOrders = new List<FulfillmentOrderResult>
        {
            new("SHP-FBA-001", "Tamamlandi",
                new List<FulfillmentOrderItem> { new("SKU-001", 10, 10), new("SKU-002", 5, 5) },
                DateTime.UtcNow.AddDays(-5), "TRK-FBA-001", "UPS"),
            new("SHP-FBA-002", "Gonderildi",
                new List<FulfillmentOrderItem> { new("SKU-003", 20, 0) },
                DateTime.UtcNow.AddDays(-1), "TRK-FBA-002", "FedEx")
        };

        // Hepsilojistik orders
        var hlOrders = new List<FulfillmentOrderResult>
        {
            new("SHP-HL-001", "Isleniyor",
                new List<FulfillmentOrderItem> { new("SKU-004", 15, 0) },
                DateTime.UtcNow.AddDays(-2), "TRK-HL-001", "Aras")
        };

        // TrendyolFulfillment orders
        var tyOrders = new List<FulfillmentOrderResult>
        {
            new("SHP-TY-001", "Hazirlaniyor",
                new List<FulfillmentOrderItem> { new("SKU-005", 8, 0) }),
            new("SHP-TY-002", "Tamamlandi",
                new List<FulfillmentOrderItem> { new("SKU-006", 3, 3), new("SKU-007", 7, 7) },
                DateTime.UtcNow.AddDays(-10), "TRK-TY-002", "Surat")
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

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetFulfillmentOrdersQuery>(q => q.Center == FulfillmentCenter.TrendyolFulfillment),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<FulfillmentOrderResult>)tyOrders);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        // Assert — Providers: 5 items (Tumu + 4 providers), Statuses: 7 items
        _sut.Providers.Should().HaveCount(5);
        _sut.Providers.Should().Contain("Tumu");
        _sut.Providers.Should().Contain("Amazon FBA");
        _sut.Providers.Should().Contain("Hepsilojistik");

        _sut.Statuses.Should().HaveCount(7);
        _sut.Statuses.Should().Contain("Tumu");
        _sut.Statuses.Should().Contain("Tamamlandi");
        _sut.Statuses.Should().Contain("Iptal");

        _sut.SelectedProvider.Should().Be("Tumu");
        _sut.SelectedStatus.Should().Be("Tumu");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateShipments()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 2 FBA + 1 HL + 2 TY = 5 shipments from 3 centers
        _sut.Shipments.Should().HaveCount(5);
        _sut.TotalCount.Should().Be(5);
        _sut.Shipments.Select(s => s.ShipmentId).Should()
            .Contain("SHP-FBA-001")
            .And.Contain("SHP-HL-001")
            .And.Contain("SHP-TY-001");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        // Arrange — track IsLoading transitions
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FulfillmentInboundViewModel.IsLoading))
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
    public async Task LoadAsync_Error_ShouldSetErrorState()
    {
        // Arrange — mediator throws
        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetFulfillmentOrdersQuery>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        var sut = new FulfillmentInboundViewModel(_mediatorMock.Object);

        // Act
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().Contain("yuklenirken hata");
        sut.ErrorMessage.Should().Contain("Service unavailable");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyFilters_ProviderFilter_ShouldFilterByProvider()
    {
        // Arrange
        await _sut.LoadAsync();
        _sut.Shipments.Should().HaveCount(5);

        // Act — filter to Amazon FBA only
        _sut.SelectedProvider = "Amazon FBA";

        // Assert — Provider field contains "AmazonFBA" (enum ToString)
        _sut.Shipments.Should().AllSatisfy(s =>
            s.Provider.Should().Contain("Amazon"));
        _sut.Shipments.Should().HaveCount(2);
    }

    [Fact]
    public async Task ApplyFilters_StatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — filter to "Tamamlandi" status
        _sut.SelectedStatus = "Tamamlandi";

        // Assert — only shipments with "Tamamlandi" status
        _sut.Shipments.Should().AllSatisfy(s =>
            s.Status.Should().Contain("Tamamlandi"));
        _sut.Shipments.Should().HaveCount(2); // SHP-FBA-001 + SHP-TY-002
    }

    [Fact]
    public async Task ApplyFilters_SearchText_ShouldFilterByShipmentId()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search by shipment ID prefix
        _sut.SearchText = "SHP-HL";

        // Assert
        _sut.Shipments.Should().HaveCount(1);
        _sut.Shipments[0].ShipmentId.Should().Be("SHP-HL-001");
    }

    [Fact]
    public async Task ApplyFilters_NoMatch_ShouldSetIsEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — search for non-existent shipment
        _sut.SearchText = "NONEXISTENT-999";

        // Assert
        _sut.Shipments.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCount.Should().Be(0);
    }
}
