using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dropshipping;

/// <summary>
/// GetDropshipProfitabilityHandler unit testleri.
/// Urun bazli net kar ve marj hesaplamasi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Dropshipping")]
public class GetDropshipProfitabilityHandlerTests
{
    private readonly Mock<IDropshipOrderRepository> _orderRepoMock = new();
    private readonly Mock<IDropshipProductRepository> _productRepoMock = new();
    private readonly GetDropshipProfitabilityHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetDropshipProfitabilityHandlerTests()
    {
        _handler = new GetDropshipProfitabilityHandler(
            _orderRepoMock.Object,
            _productRepoMock.Object);
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Kar marji dogru hesaplanmali
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task Profitability_CalculatesMargin()
    {
        // Arrange: product with SellingPrice=100, OriginalPrice=60
        var supplierId = Guid.NewGuid();
        var product = DropshipProduct.Create(
            _tenantId, supplierId, "SKU-001", "Test Product", 60m, 100);

        // Apply markup to set SellingPrice to 100
        // OriginalPrice=60, we want SellingPrice=100 → that's ~66.67% markup
        // Using FixedAmount: 60 + 40 = 100
        product.ApplyMarkup(DropshipMarkupType.FixedAmount, 40m);

        var products = new List<DropshipProduct> { product };

        // 2 orders for this product
        var order1 = DropshipOrder.Create(_tenantId, Guid.NewGuid(), supplierId, product.Id);
        var order2 = DropshipOrder.Create(_tenantId, Guid.NewGuid(), supplierId, product.Id);
        var orders = new List<DropshipOrder> { order1, order2 };

        _productRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _orderRepoMock
            .Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        var result = await _handler.Handle(
            new GetDropshipProfitabilityQuery(_tenantId), CancellationToken.None);

        // Assert: customerPrice=100, supplierPrice=60, commission=0
        // NetProfit = (100 - 60 - 0) * 2 = 80
        // ProfitMargin = (100 - 60) / 100 * 100 = 40%
        result.Should().HaveCount(1);
        var profit = result.First();
        profit.ProductName.Should().Be("Test Product");
        profit.QuantitySold.Should().Be(2);
        profit.CustomerPrice.Should().Be(100m);
        profit.SupplierPrice.Should().Be(60m);
        profit.NetProfit.Should().Be(80m);
        profit.ProfitMargin.Should().Be(40m);
    }
}
