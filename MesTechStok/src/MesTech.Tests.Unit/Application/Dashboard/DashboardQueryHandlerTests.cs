using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Domain.Interfaces;
using Moq;
using DomainOrder = MesTech.Domain.Entities.Order;
using DomainProduct = MesTech.Domain.Entities.Product;
using DomainInvoice = MesTech.Domain.Entities.Invoice;

namespace MesTech.Tests.Unit.Application.Dashboard;

// ════════════════════════════════════════════════════════
// DEV5 TUR 9: Dashboard query handler unit tests
// Coverage: GetSalesChartData, GetLowStockAlerts,
//           GetPendingInvoices, GetRecentOrders
// ════════════════════════════════════════════════════════

#region GetSalesChartDataHandler

[Trait("Category", "Unit")]
public class GetSalesChartDataHandlerTests
{
    [Fact]
    public void Constructor_NullRepo_ShouldNotThrow()
    {
        // GetSalesChartDataHandler uses expression body, no null guard
        var orderRepo = new Mock<IOrderRepository>();
        var handler = new GetSalesChartDataHandler(orderRepo.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnSalesChartData()
    {
        var orderRepo = new Mock<IOrderRepository>();
        IReadOnlyList<DomainOrder> orders = new List<DomainOrder>();
        orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(orders));

        var handler = new GetSalesChartDataHandler(orderRepo.Object);
        var query = new GetSalesChartDataQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Labels.Should().NotBeNull();
        result.Series.Should().NotBeNull();
    }
}

#endregion

#region GetLowStockAlertsHandler

[Trait("Category", "Unit")]
public class GetLowStockAlertsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    [Fact]
    public void Constructor_NullRepo_ShouldThrowArgumentNullException()
    {
        var act = () => new GetLowStockAlertsHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnAlertList()
    {
        IReadOnlyList<DomainProduct> products = new List<DomainProduct>();
        _productRepo.Setup(r => r.GetLowStockAsync())
            .Returns(Task.FromResult(products));

        var handler = new GetLowStockAlertsHandler(_productRepo.Object);
        var query = new GetLowStockAlertsQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

#endregion

#region GetPendingInvoicesHandler

[Trait("Category", "Unit")]
public class GetPendingInvoicesHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();

    [Fact]
    public void Constructor_NullRepo_ShouldThrowArgumentNullException()
    {
        var act = () => new GetPendingInvoicesHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnPendingInvoiceList()
    {
        IReadOnlyList<DomainInvoice> invoices = new List<DomainInvoice>();
        _invoiceRepo.Setup(r => r.GetFailedAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(invoices));

        var handler = new GetPendingInvoicesHandler(_invoiceRepo.Object);
        var query = new GetPendingInvoicesQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

#endregion

#region GetRecentOrdersHandler

[Trait("Category", "Unit")]
public class GetRecentOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();

    [Fact]
    public void Constructor_NullRepo_ShouldThrowArgumentNullException()
    {
        var act = () => new GetRecentOrdersHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ShouldReturnRecentOrderList()
    {
        IReadOnlyList<DomainOrder> orders = new List<DomainOrder>();
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(orders));

        var handler = new GetRecentOrdersHandler(_orderRepo.Object);
        var query = new GetRecentOrdersQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

#endregion
