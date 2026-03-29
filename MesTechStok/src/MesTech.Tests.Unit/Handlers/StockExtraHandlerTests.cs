using FluentAssertions;
using MesTech.Application.Commands.TransferStock;
using MesTech.Application.Features.Dashboard.Queries.GetStockAlerts;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Application.Features.Product.Commands.ExportProducts;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetStockMovements;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class StockExtraHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── TransferStockHandler ───────────────────────────────────

    [Fact]
    public async Task TransferStock_NullRequest_ThrowsArgumentNullException()
    {
        var productRepo = new Mock<IProductRepository>();
        var movementRepo = new Mock<IStockMovementRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new TransferStockHandler(productRepo.Object, movementRepo.Object, warehouseRepo.Object, uow.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task TransferStock_ZeroQuantity_ReturnsFailure()
    {
        var productRepo = new Mock<IProductRepository>();
        var movementRepo = new Mock<IStockMovementRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new TransferStockHandler(productRepo.Object, movementRepo.Object, warehouseRepo.Object, uow.Object);

        var command = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0);
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("pozitif");
    }

    [Fact]
    public async Task TransferStock_SameWarehouse_ReturnsFailure()
    {
        var productRepo = new Mock<IProductRepository>();
        var movementRepo = new Mock<IStockMovementRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new TransferStockHandler(productRepo.Object, movementRepo.Object, warehouseRepo.Object, uow.Object);

        var whId = Guid.NewGuid();
        var command = new TransferStockCommand(Guid.NewGuid(), whId, whId, 10);
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ayn");
    }

    [Fact]
    public async Task TransferStock_ProductNotFound_ReturnsFailure()
    {
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);
        var movementRepo = new Mock<IStockMovementRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new TransferStockHandler(productRepo.Object, movementRepo.Object, warehouseRepo.Object, uow.Object);

        var command = new TransferStockCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5);
        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public void TransferStock_NullProductRepo_ThrowsArgumentNullException()
    {
        var act = () => new TransferStockHandler(
            null!,
            new Mock<IStockMovementRepository>().Object,
            new Mock<IWarehouseRepository>().Object,
            new Mock<IUnitOfWork>().Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TransferStock_NullUow_ThrowsArgumentNullException()
    {
        var act = () => new TransferStockHandler(
            new Mock<IProductRepository>().Object,
            new Mock<IStockMovementRepository>().Object,
            new Mock<IWarehouseRepository>().Object,
            null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── GetStockMovementsHandler ───────────────────────────────

    [Fact]
    public async Task GetStockMovements_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IStockMovementRepository>();
        var sut = new GetStockMovementsHandler(repo.Object, Mock.Of<ITenantProvider>());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetStockMovements_NoFilters_ReturnsEmptyList()
    {
        var repo = new Mock<IStockMovementRepository>();
        var sut = new GetStockMovementsHandler(repo.Object, Mock.Of<ITenantProvider>());

        var query = new GetStockMovementsQuery();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStockMovements_WithProductId_QueriesByProduct()
    {
        var productId = Guid.NewGuid();
        var repo = new Mock<IStockMovementRepository>();
        repo.Setup(r => r.GetByProductIdAsync(productId))
            .ReturnsAsync(new List<StockMovement>().AsReadOnly());

        var sut = new GetStockMovementsHandler(repo.Object, Mock.Of<ITenantProvider>());
        var query = new GetStockMovementsQuery(productId);
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.GetByProductIdAsync(productId), Times.Once());
    }

    // ── GetStockAlertsHandler ──────────────────────────────────

    [Fact]
    public async Task GetStockAlerts_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IProductRepository>();
        var sut = new GetStockAlertsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetStockAlerts_NoLowStock_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync()).ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new GetStockAlertsHandler(repo.Object);
        var result = await sut.Handle(new GetStockAlertsQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // ── ExportProductsHandler ──────────────────────────────────

    [Fact]
    public async Task ExportProducts_NullRequest_ThrowsArgumentNullException()
    {
        var service = new Mock<IBulkProductImportService>();
        var sut = new ExportProductsHandler(service.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExportProducts_ValidRequest_DelegatesToService()
    {
        var expectedBytes = new byte[] { 1, 2, 3, 4 };
        var service = new Mock<IBulkProductImportService>();
        service.Setup(s => s.ExportProductsAsync(It.IsAny<BulkExportOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBytes);

        var sut = new ExportProductsHandler(service.Object);
        var command = new ExportProductsCommand(Format: "xlsx");
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().BeEquivalentTo(expectedBytes);
        service.Verify(s => s.ExportProductsAsync(
            It.Is<BulkExportOptions>(o => o.Format == "xlsx"), It.IsAny<CancellationToken>()), Times.Once());
    }

    // ── BulkUpdateProductsHandler ──────────────────────────────

    [Fact]
    public async Task BulkUpdateProducts_NullProductIds_ReturnsZero()
    {
        var repo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = NullLogger<BulkUpdateProductsHandler>.Instance;
        var sut = new BulkUpdateProductsHandler(repo.Object, uow.Object, logger);

        var command = new BulkUpdateProductsCommand(null!, BulkUpdateAction.StatusActivate);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkUpdateProducts_EmptyList_ReturnsZero()
    {
        var repo = new Mock<IProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = NullLogger<BulkUpdateProductsHandler>.Instance;
        var sut = new BulkUpdateProductsHandler(repo.Object, uow.Object, logger);

        var command = new BulkUpdateProductsCommand(new List<Guid>(), BulkUpdateAction.StatusActivate);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().Be(0);
    }

    // ── GetTopProductsHandler ──────────────────────────────────

    [Fact]
    public async Task GetTopProducts_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IOrderRepository>();
        var sut = new GetTopProductsHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetTopProducts_NoOrders_ReturnsEmptyList()
    {
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByDateRangeWithItemsAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var sut = new GetTopProductsHandler(repo.Object);
        var result = await sut.Handle(new GetTopProductsQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
