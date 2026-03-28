using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Application.Features.Orders.Commands.ExportOrders;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Dropshipping")]
[Trait("Group", "Handler")]
public class DropshippingExtendedHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public DropshippingExtendedHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    // ═══ DROPSHIPPING ═══
    [Fact] public async Task CreateDropshipSupplier_Null_Throws() { var r = new Mock<IDropshipSupplierRepository>(); var h = new CreateDropshipSupplierHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task PlaceDropshipOrder_Null_Throws() { var r = new Mock<IDropshipOrderRepository>(); var h = new PlaceDropshipOrderHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task LinkDropshipProduct_Null_Throws() { var r = new Mock<IDropshipProductRepository>(); var h = new LinkDropshipProductHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetDropshipDashboard_Null_Throws() { var s = new Mock<IDropshipSupplierRepository>(); var f = new Mock<ISupplierFeedRepository>(); var p = new Mock<IDropshipProductRepository>(); var o = new Mock<IDropshipOrderRepository>(); var h = new GetDropshipDashboardHandler(s.Object, f.Object, p.Object, o.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetDropshipProfitability_Null_Throws() { var o = new Mock<IDropshipOrderRepository>(); var p = new Mock<IDropshipProductRepository>(); var h = new GetDropshipProfitabilityHandler(o.Object, p.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetDropshipSuppliers_Null_Throws() { var r = new Mock<IDropshipSupplierRepository>(); var h = new GetDropshipSuppliersHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ PRODUCT ═══
    [Fact] public async Task BulkUpdateProducts_Null_Throws() { var r = new Mock<IProductRepository>(); var h = new BulkUpdateProductsHandler(r.Object, _uow.Object, Mock.Of<ILogger<BulkUpdateProductsHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetBuyboxStatus_Null_Throws() { var r = new Mock<IProductRepository>(); var s = new Mock<IBuyboxService>(); var h = new GetBuyboxStatusHandler(r.Object, s.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }

    // ═══ ORDERS ═══
    [Fact] public async Task ExportOrders_Null_Throws() { var r = new Mock<IOrderRepository>(); var e = new Mock<IExcelExportService>(); var h = new ExportOrdersHandler(r.Object, e.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
