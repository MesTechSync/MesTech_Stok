using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// PlaceOrderHandler: sipariş→stok düşüm→event zincirini test eder.
/// Z1 zinciri: OrderConfirmedEvent → StockDeduction
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "OrderChain")]
public class PlaceOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly StockCalculationService _stockCalc = new();

    public PlaceOrderHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        // Default: GetByIdsAsync returns empty (override per test)
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());
    }

    private PlaceOrderHandler CreateHandler() =>
        new(_orderRepo.Object, _productRepo.Object, _uow.Object, _stockCalc);

    private Product CreateProduct(int stock = 100, decimal salePrice = 200m)
    {
        var product = new Product
        {
            Name = "Test Ürün",
            SKU = "TST-001",
            Barcode = "8680001234567",
            PurchasePrice = 100m,
            SalePrice = salePrice,
            CategoryId = Guid.NewGuid()
        };
        product.AdjustStock(stock, StockMovementType.StockIn, "Initial stock");
        return product;
    }

    [Fact]
    public async Task Handle_ValidOrder_ReturnsSuccess_And_DecrementsStock()
    {
        // Arrange
        var product = CreateProduct(stock: 50);
        var productId = product.Id;
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var cmd = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Test Müşteri",
            CustomerEmail: "test@test.com",
            Notes: null,
            Items: new List<PlaceOrderItem>
            {
                new(productId, Quantity: 5, UnitPrice: 200m)
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.OrderId.Should().NotBeEmpty();
        result.OrderNumber.Should().StartWith("ORD-");
        product.Stock.Should().Be(45); // 50 - 5
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        // GetByIdsAsync boş döner — product bulunamaz
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var cmd = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Müşteri",
            CustomerEmail: null,
            Notes: null,
            Items: new List<PlaceOrderItem>
            {
                new(missingId, Quantity: 1, UnitPrice: 100m)
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MultipleItems_DecrementsAllProducts()
    {
        // Arrange
        var product1 = CreateProduct(stock: 30);
        var product2 = CreateProduct(stock: 20);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        var cmd = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Toptan Müşteri",
            CustomerEmail: null,
            Notes: null,
            Items: new List<PlaceOrderItem>
            {
                new(product1.Id, Quantity: 10, UnitPrice: 100m),
                new(product2.Id, Quantity: 5, UnitPrice: 200m)
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product1.Stock.Should().Be(20); // 30 - 10
        product2.Stock.Should().Be(15); // 20 - 5
    }

    [Fact]
    public async Task Handle_ZeroStockAfterOrder_ProductIsOutOfStock()
    {
        // Arrange — tam stok kadar sipariş
        var product = CreateProduct(stock: 10);
        _productRepo.Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var cmd = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Son Stok Müşteri",
            CustomerEmail: null,
            Notes: null,
            Items: new List<PlaceOrderItem>
            {
                new(product.Id, Quantity: 10, UnitPrice: 100m)
            });

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        product.Stock.Should().Be(0);
        product.IsOutOfStock().Should().BeTrue();
    }
}
