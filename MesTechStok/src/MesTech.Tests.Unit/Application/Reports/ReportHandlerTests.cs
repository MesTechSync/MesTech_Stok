using FluentAssertions;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Features.Reports.CustomerLifetimeValueReport;
using MesTech.Application.Features.Reports.CustomerSegmentReport;
using MesTech.Application.Features.Reports.InventoryValuationReport;
using MesTech.Application.Features.Reports.OrderFulfillmentReport;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Application.Features.Reports.StockTurnoverReport;
using MesTech.Application.Features.Reports.TaxSummaryReport;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Reports;

// ════════════════════════════════════════════════════════
// DEV5 Agent E: Report Handler Tests
// ════════════════════════════════════════════════════════

#region PlatformSalesReportHandler

[Trait("Category", "Unit")]
public class PlatformSalesReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<ICommissionRecordRepository> _commissionRepo = new();
    private readonly Mock<ISettlementBatchRepository> _settlementRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private PlatformSalesReportHandler CreateHandler() =>
        new(_orderRepo.Object, _commissionRepo.Object, _settlementRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());
        _settlementRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>().AsReadOnly());

        var handler = CreateHandler();
        var query = new PlatformSalesReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithOrders_ShouldGroupByPlatform()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var order1 = FakeData.CreateOrder(sourcePlatform: PlatformType.Trendyol);
        order1.SetFinancials(0m, 0m, 1000m);
        var order2 = FakeData.CreateOrder(sourcePlatform: PlatformType.Trendyol);
        order2.SetFinancials(0m, 0m, 500m);

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order1, order2 }.AsReadOnly());
        _settlementRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SettlementBatch>().AsReadOnly());

        var handler = CreateHandler();
        var query = new PlatformSalesReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Platform.Should().Be("Trendyol");
        result[0].TotalOrders.Should().Be(2);
        result[0].TotalRevenue.Should().Be(1500m);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region InventoryValuationReportHandler

[Trait("Category", "Unit")]
public class InventoryValuationReportHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private InventoryValuationReportHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnEmptyList()
    {
        // Arrange
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var handler = CreateHandler();
        var query = new InventoryValuationReportQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProductsWithStock_ShouldCalculateValuation()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "VAL-001", stock: 10, purchasePrice: 50m, salePrice: 80m);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var handler = CreateHandler();
        var query = new InventoryValuationReportQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].TotalCostValue.Should().Be(500m); // 10 * 50
        result[0].TotalSaleValue.Should().Be(800m); // 10 * 80
        result[0].PotentialProfit.Should().Be(300m); // 800 - 500
    }

    [Fact]
    public async Task Handle_ZeroStockProducts_ShouldBeExcluded()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "ZERO-001", stock: 0, purchasePrice: 50m, salePrice: 80m);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var handler = CreateHandler();
        var query = new InventoryValuationReportQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region StockTurnoverReportHandler

[Trait("Category", "Unit")]
public class StockTurnoverReportHandlerTests
{
    private readonly Mock<IStockMovementRepository> _movementRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();

    private StockTurnoverReportHandler CreateHandler() =>
        new(_movementRepo.Object, _productRepo.Object);

    [Fact]
    public async Task Handle_NoSales_ShouldReturnEmptyList()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        _movementRepo.Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockMovement>().AsReadOnly());
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var handler = CreateHandler();
        var query = new StockTurnoverReportQuery(Guid.NewGuid(), from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithSales_ShouldCalculateTurnover()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var product = FakeData.CreateProduct(sku: "TURN-001", stock: 20);

        var movements = new List<StockMovement>
        {
            new() { ProductId = product.Id, Quantity = -10, Date = DateTime.UtcNow.AddDays(-15) },
            new() { ProductId = product.Id, Quantity = -5, Date = DateTime.UtcNow.AddDays(-5) },
        }.AsReadOnly();

        _movementRepo.Setup(r => r.GetByDateRangeAsync(from, to, It.IsAny<CancellationToken>())).ReturnsAsync(movements);
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var handler = CreateHandler();
        var query = new StockTurnoverReportQuery(Guid.NewGuid(), from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SoldQuantity.Should().Be(15);
        result[0].TurnoverRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region OrderFulfillmentReportHandler

[Trait("Category", "Unit")]
public class OrderFulfillmentReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private OrderFulfillmentReportHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();
        var query = new OrderFulfillmentReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithShippedOrders_ShouldCalculateFulfillmentMetrics()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var order = new Order
        {
            SourcePlatform = PlatformType.Trendyol,
            OrderDate = DateTime.UtcNow.AddDays(-5),
            Status = OrderStatus.Confirmed,
        };
        order.MarkAsShipped("TRK-001", CargoProvider.YurticiKargo);
        order.MarkAsDelivered();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order }.AsReadOnly());

        var handler = CreateHandler();
        var query = new OrderFulfillmentReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Platform.Should().Be("Trendyol");
        result[0].FulfillmentRate.Should().Be(100.0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region CargoPerformanceReportHandler

[Trait("Category", "Unit")]
public class CargoPerformanceReportHandlerTests
{
    private readonly Mock<ICargoExpenseRepository> _cargoExpenseRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CargoPerformanceReportHandler CreateHandler() =>
        new(_cargoExpenseRepo.Object, _orderRepo.Object);

    [Fact]
    public async Task Handle_NoShippedOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        _cargoExpenseRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CargoExpense>().AsReadOnly());
        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();
        var query = new CargoPerformanceReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithShippedOrders_ShouldCalculatePerformance()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        var order = new Order
        {
            SourcePlatform = PlatformType.Trendyol,
            OrderDate = DateTime.UtcNow.AddDays(-5),
            Status = OrderStatus.Confirmed,
        };
        order.MarkAsShipped("CARGO-001", CargoProvider.YurticiKargo);
        order.MarkAsDelivered();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order }.AsReadOnly());
        _cargoExpenseRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CargoExpense>().AsReadOnly());

        var handler = CreateHandler();
        var query = new CargoPerformanceReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].CargoProvider.Should().Be("YurticiKargo");
        result[0].SuccessRate.Should().Be(100.0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region CustomerLifetimeValueReportHandler

[Trait("Category", "Unit")]
public class CustomerLifetimeValueReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CustomerLifetimeValueReportHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-90);
        var to = DateTime.UtcNow;

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();
        var query = new CustomerLifetimeValueReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CustomerWithMultipleOrders_ShouldCalculateCLV()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-90);
        var to = DateTime.UtcNow;
        var customerId = Guid.NewGuid();

        var order1 = FakeData.CreateOrder(customerId: customerId);
        order1.SetFinancials(0m, 0m, 1000m);
        order1.OrderDate = DateTime.UtcNow.AddDays(-60);
        order1.CustomerName = "Ali Yilmaz";

        var order2 = FakeData.CreateOrder(customerId: customerId);
        order2.SetFinancials(0m, 0m, 500m);
        order2.OrderDate = DateTime.UtcNow.AddDays(-10);

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order1, order2 }.AsReadOnly());

        var handler = CreateHandler();
        var query = new CustomerLifetimeValueReportQuery(_tenantId, from, to, MinOrderCount: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].CustomerName.Should().Be("Ali Yilmaz");
        result[0].TotalOrders.Should().Be(2);
        result[0].TotalSpent.Should().Be(1500m);
        result[0].AverageOrderValue.Should().Be(750m);
        result[0].EstimatedCLV.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region CustomerSegmentReportHandler

[Trait("Category", "Unit")]
public class CustomerSegmentReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CustomerSegmentReportHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-90);
        var to = DateTime.UtcNow;

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();
        var query = new CustomerSegmentReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_VipCustomer_ShouldBeClassifiedAsVip()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-90);
        var to = DateTime.UtcNow;
        var customerId = Guid.NewGuid();

        var orders = Enumerable.Range(0, 6).Select(_ =>
        {
            var o = FakeData.CreateOrder(customerId: customerId);
            o.SetFinancials(0m, 0m, 200m);
            return o;
        }).ToList();

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.AsReadOnly());

        var handler = CreateHandler();
        var query = new CustomerSegmentReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Segment.Should().Be("VIP");
        result[0].CustomerCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NewCustomer_ShouldBeClassifiedAsNew()
    {
        // Arrange
        var from = DateTime.UtcNow.AddDays(-90);
        var to = DateTime.UtcNow;
        var customerId = Guid.NewGuid();

        var order = FakeData.CreateOrder(customerId: customerId);
        order.SetFinancials(0m, 0m, 100m);

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order }.AsReadOnly());

        var handler = CreateHandler();
        var query = new CustomerSegmentReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Segment.Should().Be("New");
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region TaxSummaryReportHandler

[Trait("Category", "Unit")]
public class TaxSummaryReportHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private TaxSummaryReportHandler CreateHandler() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrders_ShouldReturnEmptyList()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var handler = CreateHandler();
        var query = new TaxSummaryReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithOrders_ShouldGroupByMonth()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);

        var janOrder = new Order { OrderDate = new DateTime(2026, 1, 15) };
        janOrder.SetFinancials(820m, 180m, 1000m);

        var febOrder = new Order { OrderDate = new DateTime(2026, 2, 10) };
        febOrder.SetFinancials(410m, 90m, 500m);

        _orderRepo.Setup(r => r.GetByDateRangeAsync(_tenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { janOrder, febOrder }.AsReadOnly());

        var handler = CreateHandler();
        var query = new TaxSummaryReportQuery(_tenantId, from, to);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].TaxPeriod.Should().Be("2026-01");
        result[1].TaxPeriod.Should().Be("2026-02");
        result[0].OutputVat.Should().Be(180m);
        result[1].OutputVat.Should().Be(90m);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion
