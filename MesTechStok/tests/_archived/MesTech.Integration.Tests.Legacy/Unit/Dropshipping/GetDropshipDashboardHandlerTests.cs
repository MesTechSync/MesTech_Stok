using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dropshipping;

/// <summary>
/// GetDropshipDashboardHandler unit testleri.
/// Bos DB ve KPI hesaplama senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Dropshipping")]
public class GetDropshipDashboardHandlerTests
{
    private readonly Mock<IDropshipSupplierRepository> _supplierRepoMock = new();
    private readonly Mock<ISupplierFeedRepository> _feedRepoMock = new();
    private readonly Mock<IDropshipProductRepository> _productRepoMock = new();
    private readonly Mock<IDropshipOrderRepository> _orderRepoMock = new();
    private readonly GetDropshipDashboardHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetDropshipDashboardHandlerTests()
    {
        _handler = new GetDropshipDashboardHandler(
            _supplierRepoMock.Object,
            _feedRepoMock.Object,
            _productRepoMock.Object,
            _orderRepoMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Bos DB — tum KPI degerleri sifir
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Dashboard_EmptyDb_ReturnsZeros()
    {
        // Arrange: all repos return empty
        _supplierRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DropshipSupplier>());

        _feedRepoMock
            .Setup(r => r.GetActiveCountAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _productRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DropshipProduct>());

        _orderRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DropshipOrder>());

        // Act
        var result = await _handler.Handle(
            new GetDropshipDashboardQuery(_tenantId), CancellationToken.None);

        // Assert
        result.ActiveSuppliers.Should().Be(0);
        result.ActiveFeeds.Should().Be(0);
        result.TotalDropshipProducts.Should().Be(0);
        result.PendingOrders.Should().Be(0);
        result.MonthlyRevenue.Should().Be(0m);
        result.MonthlyProfit.Should().Be(0m);
        result.AverageMargin.Should().Be(0m);
        result.TopSuppliers.Should().BeEmpty();
    }
}
