using FluentAssertions;
using MesTech.Application.Queries.GetSupplierById;
using MesTech.Application.Queries.GetSuppliers;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Application.Queries.GetWarehouseById;
using MesTech.Application.Queries.GetWarehouseSummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Batch 2 — 5 testsiz Get*QueryHandler kapanisi.
/// GetWarehouseById, GetWarehouseSummary, GetSupplierById, GetSuppliers, GetStoresByTenant
/// </summary>
[Trait("Category", "Unit")]
public class QueryHandlerBatch2Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    // GetWarehouseByIdHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetWarehouseById_NotFound_ReturnsNull()
    {
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Warehouse?)null);

        var sut = new GetWarehouseByIdHandler(repo.Object);
        var result = await sut.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWarehouseById_Found_MapsToDto()
    {
        var whId = Guid.NewGuid();
        var wh = new Warehouse
        {
            Id = whId, Name = "Depo A", Code = "WH-A",
            Type = "MAIN", IsActive = true
        };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(whId, It.IsAny<CancellationToken>())).ReturnsAsync(wh);

        var sut = new GetWarehouseByIdHandler(repo.Object);
        var result = await sut.Handle(new GetWarehouseByIdQuery(whId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Depo A");
        result.Code.Should().Be("WH-A");
    }

    // ═══════════════════════════════════════════════════════════
    // GetWarehouseSummaryHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetWarehouseSummary_NoWarehouses_ReturnsEmpty()
    {
        var whRepo = new Mock<IWarehouseRepository>();
        whRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse>());
        var prodRepo = new Mock<IProductRepository>();

        var sut = new GetWarehouseSummaryHandler(whRepo.Object, prodRepo.Object);
        var result = await sut.Handle(
            new GetWarehouseSummaryQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWarehouseSummary_WithWarehouseAndProducts_CalculatesStats()
    {
        var whId = Guid.NewGuid();
        var wh = new Warehouse
        {
            Id = whId, Name = "Ana Depo", TenantId = _tenantId,
            City = "Istanbul", MaxCapacity = 100, IsActive = true
        };
        var product1 = new Product
        {
            Id = Guid.NewGuid(), Name = "P1", WarehouseId = whId,
            MinimumStock = 5
        };
        var product2 = new Product
        {
            Id = Guid.NewGuid(), Name = "P2", WarehouseId = whId,
            MinimumStock = 5
        };
        product2.SyncStock(50);
        var whRepo = new Mock<IWarehouseRepository>();
        whRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { wh });
        var prodRepo = new Mock<IProductRepository>();
        prodRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        var sut = new GetWarehouseSummaryHandler(whRepo.Object, prodRepo.Object);
        var result = await sut.Handle(
            new GetWarehouseSummaryQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ana Depo");
        result[0].ProductCount.Should().Be(2);
        result[0].TotalStock.Should().Be(50);
        result[0].OutOfStockCount.Should().Be(1);
        result[0].HealthStatus.Should().Be("Critical");
    }

    // ═══════════════════════════════════════════════════════════
    // GetSupplierByIdHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSupplierById_NotFound_ReturnsNull()
    {
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var sut = new GetSupplierByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetSupplierByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSupplierById_Found_ReturnsEntity()
    {
        var supplierId = Guid.NewGuid();
        var supplier = new Supplier
        {
            Id = supplierId, Name = "Tedarikci A",
            Code = "SUP01", IsActive = true
        };
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>())).ReturnsAsync(supplier);

        var sut = new GetSupplierByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetSupplierByIdQuery(supplierId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Tedarikci A");
    }

    // ═══════════════════════════════════════════════════════════
    // GetSuppliersHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSuppliers_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Supplier>());

        var sut = new GetSuppliersHandler(repo.Object);
        var result = await sut.Handle(new GetSuppliersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuppliers_WithData_MapsToDto()
    {
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), Name = "Tedarikci B", Code = "SUP02",
            ContactPerson = "Ali", Email = "ali@test.com",
            IsActive = true
        };
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Supplier> { supplier });

        var sut = new GetSuppliersHandler(repo.Object);
        var result = await sut.Handle(new GetSuppliersQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Tedarikci B");
        result[0].Name.Should().Be("Tedarikci B");
    }

    [Fact]
    public async Task GetSuppliers_AllIncludingInactive_UsesGetAllAsync()
    {
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Supplier>());

        var sut = new GetSuppliersHandler(repo.Object);
        await sut.Handle(new GetSuppliersQuery(ActiveOnly: false), CancellationToken.None);

        repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════
    // GetStoresByTenantHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStoresByTenant_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IStoreRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store>());

        var sut = new GetStoresByTenantHandler(repo.Object);
        var result = await sut.Handle(
            new GetStoresByTenantQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStoresByTenant_WithStore_MapsToDto()
    {
        var store = new Store
        {
            Id = Guid.NewGuid(), TenantId = _tenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "MesTech Trendyol",
            ExternalStoreId = "ext-123",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var repo = new Mock<IStoreRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store });

        var sut = new GetStoresByTenantHandler(repo.Object);
        var result = await sut.Handle(
            new GetStoresByTenantQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].StoreName.Should().Be("MesTech Trendyol");
        result[0].PlatformType.Should().Be(PlatformType.Trendyol);
        result[0].IsActive.Should().BeTrue();
    }
}
