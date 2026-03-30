using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;
using MesTech.Application.Queries.ListOrders;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Application.Queries.GetWarehouseById;
using MesTech.Application.Queries.GetWarehouseStock;
using MesTech.Application.Queries.GetWarehouseSummary;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentShipments;
using MesTech.Application.Features.Fulfillment.Queries.GetFulfillmentDashboard;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Orders")]
[Trait("Group", "Handler")]
public class OrderWarehouseHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Mock<IUnitOfWork> _uow = new();
    public OrderWarehouseHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ ORDERS ═══

    [Fact] public async Task CreateOrder_Null_Throws() { var r = new Mock<IOrderRepository>(); var h = new CreateOrderHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ListOrders_Null_Throws() { var r = new Mock<IOrderRepository>(); var h = new ListOrdersHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task CreateAutoOrder_Null_Throws() { var pr = new Mock<IProductRepository>(); var ds = new Mock<IDropshipSupplierRepository>(); var do2 = new Mock<IDropshipOrderRepository>(); var h = new CreateAutoOrderHandler(pr.Object, ds.Object, do2.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task BatchShipOrders_Null_Throws() { var m = new Mock<IMediator>(); var h = new BatchShipOrdersHandler(m.Object, Mock.Of<ILogger<BatchShipOrdersHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetStaleOrders_Null_Throws() { var r = new Mock<IOrderRepository>(); var h = new GetStaleOrdersQueryHandler(r.Object, Mock.Of<ILogger<GetStaleOrdersQueryHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ WAREHOUSE ═══

    [Fact] public async Task GetWarehouses_Null_Throws() { var r = new Mock<IWarehouseRepository>(); var h = new GetWarehousesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    [Fact]
    public async Task GetWarehouses_EmptyRepo_ReturnsEmptyList()
    {
        var r = new Mock<IWarehouseRepository>();
        r.Setup(x => x.GetAllAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Warehouse>());
        var h = new GetWarehousesHandler(r.Object);
        var result = await h.Handle(new GetWarehousesQuery(_tenantId), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact] public async Task GetWarehouseById_Null_Throws() { var r = new Mock<IWarehouseRepository>(); var h = new GetWarehouseByIdHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetWarehouseStock_Null_Throws() { var pr = new Mock<IProductRepository>(); var wr = new Mock<IWarehouseRepository>(); var h = new GetWarehouseStockHandler(pr.Object, wr.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetWarehouseSummary_Null_Throws() { var h = new GetWarehouseSummaryHandler(); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ FULFILLMENT ═══

    [Fact] public async Task GetFulfillmentShipments_Null_Throws() { var h = new GetFulfillmentShipmentsHandler(Mock.Of<ILogger<GetFulfillmentShipmentsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetFulfillmentDashboard_Null_Throws() { var pr = new Mock<IProductRepository>(); var h = new GetFulfillmentDashboardHandler(pr.Object, Mock.Of<ILogger<GetFulfillmentDashboardHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
