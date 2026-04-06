using FluentAssertions;
using MesTech.Application.DTOs.Dropshipping;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class DropshipOrdersAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DropshipOrdersAvaloniaViewModel _sut;

    /// <summary>
    /// 8 realistic dropship orders with mixed statuses:
    /// 2 Yeni, 2 Tedarikçiye İletildi, 2 Kargoda, 1 Teslim Edildi, 1 İptal
    /// </summary>
    private static IReadOnlyList<DropshipOrderDto> CreateTestOrders()
    {
        var supplierId = Guid.NewGuid();
        return new List<DropshipOrderDto>
        {
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-001", Status = "Tedarikçiye İletildi" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-002", Status = "Yeni" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-003", Status = "Kargoda" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-004", Status = "Teslim Edildi" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-005", Status = "Tedarikçiye İletildi" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-006", Status = "Kargoda" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-007", Status = "Yeni" },
            new() { Id = Guid.NewGuid(), OrderId = Guid.NewGuid(), DropshipSupplierId = supplierId, SupplierOrderRef = "DS-2026-008", Status = "İptal" },
        };
    }

    public DropshipOrdersAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();

        // Setup: GetDropshipOrdersQuery returns 8 test orders
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetDropshipOrdersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestOrders());

        _sut = new DropshipOrdersAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateOrders()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Orders.Should().HaveCount(8);
        _sut.TotalCount.Should().Be(8);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task SelectedStatus_ShouldFilterOrders()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatus = "Yeni";

        // Assert
        _sut.Orders.Should().OnlyContain(o => o.Status == "Yeni");
        _sut.TotalCount.Should().Be(2); // DS-2026-002, DS-2026-007
    }

    [Fact]
    public async Task SelectedStatus_ResetToTumu_ShouldShowAll()
    {
        // Arrange
        await _sut.LoadAsync();
        _sut.SelectedStatus = "Kargoda";
        var filteredCount = _sut.TotalCount;

        // Act
        _sut.SelectedStatus = "Tumu";

        // Assert
        filteredCount.Should().BeLessThan(8);
        _sut.TotalCount.Should().Be(8);
    }

    [Fact]
    public async Task ForwardToSupplierCommand_ShouldChangeStatusForNewOrder()
    {
        // Arrange
        await _sut.LoadAsync();
        var newOrder = _sut.Orders.First(o => o.Status == "Yeni");

        // Act
        await _sut.ForwardToSupplierCommand.ExecuteAsync(newOrder);

        // Assert
        newOrder.Status.Should().Be("Tedarikçiye İletildi");
    }

    [Fact]
    public async Task ForwardToSupplierCommand_ShouldNotChangeNonNewOrder()
    {
        // Arrange
        await _sut.LoadAsync();
        var kargoOrder = _sut.Orders.First(o => o.Status == "Kargoda");
        var originalStatus = kargoOrder.Status;

        // Act
        await _sut.ForwardToSupplierCommand.ExecuteAsync(kargoOrder);

        // Assert
        kargoOrder.Status.Should().Be(originalStatus);
    }
}
