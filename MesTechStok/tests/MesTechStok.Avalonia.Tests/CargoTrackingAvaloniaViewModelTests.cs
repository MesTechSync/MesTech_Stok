using FluentAssertions;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;
using CargoQueryDto = MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList.CargoTrackingItemDto;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CargoTrackingAvaloniaViewModelTests
{
    private readonly CargoTrackingAvaloniaViewModel _sut;

    public CargoTrackingAvaloniaViewModelTests()
    {
        var mediatorMock = new Mock<IMediator>();
        // 10 shipments: 4 Yurtiçi, 3 Aras, 3 Sürat
        var items = new List<CargoQueryDto>();
        for (int i = 1; i <= 4; i++)
            items.Add(new CargoQueryDto { OrderId = Guid.NewGuid(), OrderNumber = $"ORD-YK-{i:000}", TrackingNumber = $"TRK-YK-{i:000}", CargoProvider = "Yurtiçi Kargo", ShippedAt = DateTime.Now.AddDays(-i), Status = "Shipped" });
        for (int i = 1; i <= 3; i++)
            items.Add(new CargoQueryDto { OrderId = Guid.NewGuid(), OrderNumber = $"ORD-AR-{i:000}", TrackingNumber = $"TRK-AR-{i:000}", CargoProvider = "Aras Kargo", ShippedAt = DateTime.Now.AddDays(-i), Status = "Delivered" });
        for (int i = 1; i <= 3; i++)
            items.Add(new CargoQueryDto { OrderId = Guid.NewGuid(), OrderNumber = $"ORD-SK-{i:000}", TrackingNumber = $"TRK-SK-{i:000}", CargoProvider = "Sürat Kargo", ShippedAt = DateTime.Now.AddDays(-i), Status = "Shipped" });

        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCargoTrackingListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<CargoQueryDto>)items);
        _sut = new CargoTrackingAvaloniaViewModel(mediatorMock.Object, Mock.Of<ITenantProvider>());
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateShipments()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Shipments.Should().HaveCount(10);
        _sut.TotalCount.Should().Be(10);
        _sut.IsEmpty.Should().BeFalse();
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
            if (e.PropertyName == nameof(CargoTrackingAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SelectedFirm_ShouldFilterByFirma()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedFirm = "Aras Kargo";

        // Assert
        _sut.Shipments.Should().OnlyContain(s => s.Firma == "Aras Kargo");
        _sut.TotalCount.Should().BeGreaterThan(0);
        _sut.TotalCount.Should().BeLessThan(10);
    }

    [Fact]
    public async Task SelectedFirm_ResetToTumu_ShouldShowAll()
    {
        // Arrange
        await _sut.LoadAsync();
        _sut.SelectedFirm = "Sürat Kargo";
        var filteredCount = _sut.TotalCount;

        // Act
        _sut.SelectedFirm = "Tümü";

        // Assert
        filteredCount.Should().BeLessThan(10);
        _sut.TotalCount.Should().Be(10);
        _sut.Shipments.Should().HaveCount(10);
    }

    [Fact]
    public void Firms_ShouldContainExpectedOptions()
    {
        // Assert
        _sut.Firms.Should().Contain("Tümü");
        _sut.Firms.Should().Contain("Yurtiçi Kargo");
        _sut.Firms.Should().Contain("Aras Kargo");
        _sut.Firms.Should().Contain("Sürat Kargo");
        _sut.Firms.Should().HaveCount(4);
    }
}
