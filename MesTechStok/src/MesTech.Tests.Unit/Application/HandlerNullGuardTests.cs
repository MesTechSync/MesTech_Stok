using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Application.Queries.GetCategories;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Application.Queries.GetLowStockProducts;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Application.Queries.GetSuppliers;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// Tests null guard paths in Application handlers and edge-case field mappings.
/// Contributes to Application layer coverage (D-13).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "HandlerNullGuards")]
[Trait("Phase", "Dalga5")]
public class HandlerNullGuardTests
{
    // ── PlaceOrderHandler Constructor Guards (4 explicit guards) ──

    [Fact]
    public void PlaceOrderHandler_NullOrderRepo_Throws()
    {
        var act = () => new PlaceOrderHandler(
            null!,
            new Mock<IProductRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            new StockCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("orderRepository");
    }

    [Fact]
    public void PlaceOrderHandler_NullProductRepo_Throws()
    {
        var act = () => new PlaceOrderHandler(
            new Mock<IOrderRepository>().Object,
            null!,
            new Mock<IUnitOfWork>().Object,
            new StockCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("productRepository");
    }

    [Fact]
    public void PlaceOrderHandler_NullUnitOfWork_Throws()
    {
        var act = () => new PlaceOrderHandler(
            new Mock<IOrderRepository>().Object,
            new Mock<IProductRepository>().Object,
            null!,
            new StockCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public void PlaceOrderHandler_NullStockCalculation_Throws()
    {
        var act = () => new PlaceOrderHandler(
            new Mock<IOrderRepository>().Object,
            new Mock<IProductRepository>().Object,
            new Mock<IUnitOfWork>().Object,
            null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stockCalculation");
    }

    // ── GetInventoryValueHandler Guards ──

    [Fact]
    public void GetInventoryValueHandler_NullRepo_Throws()
    {
        var act = () => new GetInventoryValueHandler(null!, new StockCalculationService());
        act.Should().Throw<ArgumentNullException>().WithParameterName("productRepository");
    }

    [Fact]
    public void GetInventoryValueHandler_NullStockCalc_Throws()
    {
        var act = () => new GetInventoryValueHandler(new Mock<IProductRepository>().Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stockCalc");
    }

    [Fact]
    public async Task GetInventoryValueHandler_NullRequest_Throws()
    {
        var handler = new GetInventoryValueHandler(
            new Mock<IProductRepository>().Object,
            new StockCalculationService());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetLowStockProductsHandler Guards ──

    [Fact]
    public void GetLowStockProductsHandler_NullRepo_Throws()
    {
        var act = () => new GetLowStockProductsHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("productRepository");
    }

    [Fact]
    public async Task GetLowStockProductsHandler_NullRequest_Throws()
    {
        var handler = new GetLowStockProductsHandler(new Mock<IProductRepository>().Object);
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetCategoriesHandler — full field mapping verification ──

    [Fact]
    public async Task GetCategoriesHandler_MapsAllFields()
    {
        var repo = new Mock<ICategoryRepository>();
        var parentId = Guid.NewGuid();
        var cat = new Category
        {
            Name = "Electronics",
            Code = "ELEC",
            Description = "Elektronik ürünler",
            ParentCategoryId = parentId,
            SortOrder = 3,
            IsActive = true,
            Color = "#FF5733",
            Icon = "laptop"
        };
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category> { cat }.AsReadOnly());

        var handler = new GetCategoriesHandler(repo.Object);
        var result = await handler.Handle(new GetCategoriesQuery(ActiveOnly: false), CancellationToken.None);

        var dto = result.Single();
        dto.Description.Should().Be("Elektronik ürünler");
        dto.ParentCategoryId.Should().Be(parentId);
        dto.SortOrder.Should().Be(3);
        dto.Color.Should().Be("#FF5733");
        dto.Icon.Should().Be("laptop");
    }

    // ── GetWarehousesHandler — full field mapping verification ──

    [Fact]
    public async Task GetWarehousesHandler_MapsAllFields()
    {
        var repo = new Mock<IWarehouseRepository>();
        var wh = new Warehouse
        {
            Name = "Ana Depo",
            Code = "WH-MAIN",
            Description = "Ana depo açıklaması",
            Type = "MAIN",
            City = "İstanbul",
            Address = "Atatürk Mah. No:1",
            IsActive = true,
            HasClimateControl = false
        };
        wh.SetAsDefault();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Warehouse> { wh }.AsReadOnly());

        var handler = new GetWarehousesHandler(repo.Object);
        var result = await handler.Handle(new GetWarehousesQuery(ActiveOnly: false), CancellationToken.None);

        var dto = result.Single();
        dto.Description.Should().Be("Ana depo açıklaması");
        dto.Type.Should().Be("MAIN");
        dto.City.Should().Be("İstanbul");
        dto.Address.Should().Be("Atatürk Mah. No:1");
    }

    // ── GetStoresByTenantHandler — ProductMappings null safety ──

    [Fact]
    public async Task GetStoresByTenantHandler_NullProductMappings_ReturnsZeroCount()
    {
        var repo = new Mock<IStoreRepository>();
        var tenantId = Guid.NewGuid();
        var store = new Store
        {
            TenantId = tenantId,
            PlatformType = PlatformType.Trendyol,
            StoreName = "Test Store",
            IsActive = true,
            // ProductMappings = null (not initialized)
        };
        repo.Setup(r => r.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Store> { store }.AsReadOnly());

        var handler = new GetStoresByTenantHandler(repo.Object);
        var result = await handler.Handle(new GetStoresByTenantQuery(tenantId), CancellationToken.None);

        result.Single().ProductMappingCount.Should().Be(0);
    }

    // ── GetSuppliersHandler — default ActiveOnly = true ──

    [Fact]
    public async Task GetSuppliersHandler_DefaultQuery_CallsGetActive()
    {
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetActiveAsync())
            .ReturnsAsync(new List<Supplier>().AsReadOnly());

        var handler = new GetSuppliersHandler(repo.Object);
        await handler.Handle(new GetSuppliersQuery(), CancellationToken.None);

        repo.Verify(r => r.GetActiveAsync(), Times.Once);
        repo.Verify(r => r.GetAllAsync(), Times.Never);
    }
}
