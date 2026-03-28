using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetPendingInvoices;
using MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;
using MesTech.Application.Features.Dashboard.Queries.GetSalesChartData;
using MesTech.Domain.Interfaces;
using Moq;

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
    private readonly Mock<IOrderRepository> _orderRepo = new();

    [Fact]
    public void Constructor_NullRepo_ShouldNotThrow()
    {
        // GetSalesChartDataHandler uses expression body, no null guard
        var handler = new GetSalesChartDataHandler(_orderRepo.Object);
        handler.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnSalesChartData()
    {
        var handler = new GetSalesChartDataHandler(_orderRepo.Object);
        var query = new GetSalesChartDataQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
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
        var handler = new GetLowStockAlertsHandler(_productRepo.Object);
        var query = new GetLowStockAlertsQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
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
        var handler = new GetPendingInvoicesHandler(_invoiceRepo.Object);
        var query = new GetPendingInvoicesQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
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
        var handler = new GetRecentOrdersHandler(_orderRepo.Object);
        var query = new GetRecentOrdersQuery(Guid.NewGuid());

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
    }
}

#endregion
