using FluentAssertions;
using MesTech.Application.Features.Stock.Commands.CreateStockLot;
using MesTech.Application.Features.Stock.Commands.ExportStock;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using MesTech.Application.Features.Stock.Queries.GetStockLots;
using MesTech.Application.Features.Stock.Queries.GetStockPlacements;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5: Stock handler batch tests — 6 handlers
// Pattern: mock repo → verify call or NotThrow
// ════════════════════════════════════════════════════════

#region CreateStockLot

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class CreateStockLotHandlerTests2
{
    [Fact]
    public async Task Handle_ShouldCallAddAndSave()
    {
        var lotRepo = new Mock<IStockLotRepository>();
        var uow = new Mock<IUnitOfWork>();
        var logger = new Mock<ILogger<CreateStockLotHandler>>();

        lotRepo.Setup(r => r.AddAsync(It.IsAny<StockLot>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var sut = new CreateStockLotHandler(lotRepo.Object, uow.Object, logger.Object);
        var cmd = new CreateStockLotCommand(Guid.NewGuid(), Guid.NewGuid(), "LOT-001", 100, 5.50m);

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lotRepo.Verify(r => r.AddAsync(It.IsAny<StockLot>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ExportStock

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class ExportStockHandlerBatchTests
{
    [Fact]
    public async Task Handle_ShouldReturnResultWithFileName()
    {
        var sut = new ExportStockHandler();
        var cmd = new ExportStockCommand(Guid.NewGuid(), "csv");

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Contain(".csv");
        result.ExportedCount.Should().Be(0);
    }
}

#endregion

#region StartStockCount

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class StartStockCountHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnSessionWithItems()
    {
        var productRepo = new Mock<IProductRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();

        productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new StartStockCountHandler(productRepo.Object, warehouseRepo.Object);
        var cmd = new StartStockCountCommand(Guid.NewGuid());

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.SessionId.Should().NotBeEmpty();
        productRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetStockLots

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class GetStockLotsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepoGetByTenant()
    {
        var repo = new Mock<IStockLotRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockLot>());

        var sut = new GetStockLotsHandler(repo.Object);
        var query = new GetStockLotsQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        repo.Verify(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetStockPlacements

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class GetStockPlacementsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallRepoGetByTenant()
    {
        var repo = new Mock<IStockPlacementRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockPlacement>());

        var sut = new GetStockPlacementsHandler(repo.Object);
        var query = new GetStockPlacementsQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        repo.Verify(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetStockSummary

[Trait("Category", "Unit")]
[Trait("Layer", "Stock")]
public class GetStockSummaryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCallProductRepoGetAll()
    {
        var productRepo = new Mock<IProductRepository>();
        productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = new GetStockSummaryHandler(productRepo.Object);
        var query = new GetStockSummaryQuery(Guid.NewGuid());

        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalProducts.Should().Be(0);
        productRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
