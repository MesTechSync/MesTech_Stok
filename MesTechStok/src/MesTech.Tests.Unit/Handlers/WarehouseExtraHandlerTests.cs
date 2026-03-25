using FluentAssertions;
using MesTech.Application.Commands.UpdateWarehouse;
using MesTech.Application.Queries.GetWarehouseById;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Application.Queries.GetWarehouseStock;
using MesTech.Application.Queries.GetWarehouseSummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class WarehouseExtraHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── UpdateWarehouseHandler ─────────────────────────────────

    [Fact]
    public async Task UpdateWarehouse_WarehouseNotFound_ReturnsFalse()
    {
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Warehouse?)null);
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateWarehouseHandler(repo.Object, uow.Object);

        var command = new UpdateWarehouseCommand(_tenantId, Guid.NewGuid(), "Test", "WH-01", null, "Standard", true);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().BeFalse();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task UpdateWarehouse_WrongTenant_ReturnsFalse()
    {
        var warehouse = new Warehouse { TenantId = Guid.NewGuid(), Name = "Old", Code = "WH-00" };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(warehouse);
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateWarehouseHandler(repo.Object, uow.Object);

        var command = new UpdateWarehouseCommand(_tenantId, Guid.NewGuid(), "New", "WH-01", null, "Standard", true);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().BeFalse();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task UpdateWarehouse_ValidRequest_ReturnsTrue()
    {
        var whId = Guid.NewGuid();
        var warehouse = new Warehouse { TenantId = _tenantId, Name = "Old", Code = "WH-00" };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(whId)).ReturnsAsync(warehouse);
        var uow = new Mock<IUnitOfWork>();
        var sut = new UpdateWarehouseHandler(repo.Object, uow.Object);

        var command = new UpdateWarehouseCommand(_tenantId, whId, "Updated Depo", "WH-99", "Desc", "Cold", true);
        var result = await sut.Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        warehouse.Name.Should().Be("Updated Depo");
        warehouse.Code.Should().Be("WH-99");
        repo.Verify(r => r.UpdateAsync(warehouse), Times.Once());
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    // ── GetWarehousesHandler ───────────────────────────────────

    [Fact]
    public async Task GetWarehouses_NullRequest_ThrowsArgumentNullException()
    {
        var repo = new Mock<IWarehouseRepository>();
        var sut = new GetWarehousesHandler(repo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetWarehouses_ActiveOnly_FiltersInactive()
    {
        var activeWarehouse = new Warehouse { Name = "Active", Code = "WH-A", IsActive = true };
        var inactiveWarehouse = new Warehouse { Name = "Inactive", Code = "WH-I", IsActive = false };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Warehouse> { activeWarehouse, inactiveWarehouse });
        var sut = new GetWarehousesHandler(repo.Object);

        var result = await sut.Handle(new GetWarehousesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetWarehouses_AllIncluded_ReturnsAll()
    {
        var w1 = new Warehouse { Name = "A", Code = "WH-A", IsActive = true };
        var w2 = new Warehouse { Name = "B", Code = "WH-B", IsActive = false };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Warehouse> { w1, w2 });
        var sut = new GetWarehousesHandler(repo.Object);

        var result = await sut.Handle(new GetWarehousesQuery(ActiveOnly: false), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    // ── GetWarehouseByIdHandler ────────────────────────────────

    [Fact]
    public async Task GetWarehouseById_NotFound_ReturnsNull()
    {
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);
        var sut = new GetWarehouseByIdHandler(repo.Object);

        var result = await sut.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWarehouseById_Found_ReturnsMappedDto()
    {
        var wh = new Warehouse { Name = "Ana Depo", Code = "WH-01", IsActive = true, City = "Istanbul" };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(wh);
        var sut = new GetWarehouseByIdHandler(repo.Object);

        var result = await sut.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Ana Depo");
        result.Code.Should().Be("WH-01");
        result.City.Should().Be("Istanbul");
    }

    // ── GetWarehouseStockHandler ───────────────────────────────

    [Fact]
    public async Task GetWarehouseStock_NullRequest_ThrowsArgumentNullException()
    {
        var productRepo = new Mock<IProductRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();
        var sut = new GetWarehouseStockHandler(productRepo.Object, warehouseRepo.Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetWarehouseStock_WarehouseNotFound_ReturnsEmpty()
    {
        var productRepo = new Mock<IProductRepository>();
        var warehouseRepo = new Mock<IWarehouseRepository>();
        warehouseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);
        var sut = new GetWarehouseStockHandler(productRepo.Object, warehouseRepo.Object);

        var result = await sut.Handle(new GetWarehouseStockQuery(Guid.NewGuid(), _tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        productRepo.Verify(r => r.GetAllAsync(), Times.Never());
    }

    [Fact]
    public void GetWarehouseStock_NullProductRepo_ThrowsArgumentNullException()
    {
        var act = () => new GetWarehouseStockHandler(null!, new Mock<IWarehouseRepository>().Object);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── GetWarehouseSummaryHandler ─────────────────────────────

    [Fact]
    public async Task GetWarehouseSummary_NullRequest_ThrowsArgumentNullException()
    {
        var sut = new GetWarehouseSummaryHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetWarehouseSummary_ValidRequest_ReturnsEmptyList()
    {
        var sut = new GetWarehouseSummaryHandler();
        var result = await sut.Handle(new GetWarehouseSummaryQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
