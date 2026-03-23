using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Queries.GetSupplierPerformance;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using Moq;

namespace MesTech.Tests.Unit.Application.Dropshipping;

[Trait("Category", "Unit")]
public class GetSupplierPerformanceHandlerTests
{
    private readonly Mock<IDropshipSupplierRepository> _supplierRepo = new();
    private readonly Mock<IDropshipOrderRepository> _orderRepo = new();

    private GetSupplierPerformanceHandler CreateHandler() =>
        new(_supplierRepo.Object, _orderRepo.Object);

    [Fact]
    public async Task Handle_SuppliersWithOrders_ShouldCalculatePerformance()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplier = DropshipSupplier.Create(
            tenantId, "Test Supplier", "https://supplier.com",
            DropshipMarkupType.Percentage, 15m);

        var order1 = DropshipOrder.Create(tenantId, Guid.NewGuid(), supplier.Id, Guid.NewGuid());
        order1.PlaceWithSupplier("REF-001");
        order1.MarkShipped("TRACK-001");
        order1.MarkDelivered();

        var order2 = DropshipOrder.Create(tenantId, Guid.NewGuid(), supplier.Id, Guid.NewGuid());
        order2.PlaceWithSupplier("REF-002");
        order2.MarkShipped("TRACK-002");
        order2.MarkDelivered();

        _supplierRepo
            .Setup(r => r.GetByTenantAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier> { supplier });

        _orderRepo
            .Setup(r => r.GetByTenantAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipOrder> { order1, order2 });

        var handler = CreateHandler();
        var query = new GetSupplierPerformanceQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        var perf = result[0];
        perf.SupplierName.Should().Be("Test Supplier");
        perf.TotalOrders.Should().Be(2);
        perf.FulfilledOrders.Should().Be(2);
        perf.FailedOrders.Should().Be(0);
        perf.Rating.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_NoSuppliers_ShouldReturnEmptyList()
    {
        // Arrange
        _supplierRepo
            .Setup(r => r.GetByTenantAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier>());

        _orderRepo
            .Setup(r => r.GetByTenantAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipOrder>());

        var handler = CreateHandler();
        var query = new GetSupplierPerformanceQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SupplierWithNoOrders_ShouldReturnZeroRating()
    {
        // Arrange
        var supplier = DropshipSupplier.Create(
            Guid.NewGuid(), "Empty Supplier", null,
            DropshipMarkupType.FixedAmount, 5m);

        _supplierRepo
            .Setup(r => r.GetByTenantAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipSupplier> { supplier });

        _orderRepo
            .Setup(r => r.GetByTenantAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipOrder>());

        var handler = CreateHandler();
        var query = new GetSupplierPerformanceQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
        result[0].TotalOrders.Should().Be(0);
        result[0].Rating.Should().Be(0);
        result[0].ReturnRate.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
