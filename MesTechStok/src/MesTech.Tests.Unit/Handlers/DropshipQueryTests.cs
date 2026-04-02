using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;
using MesTech.Application.Features.Dropshipping.Queries.GetSupplierPerformance;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Application.Queries.GetSuppliers;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Dropshipping, Supplier, CategoryMapping, and Counterparty query handler tests.
/// Covers: GetDropshipOrders, GetDropshipProducts, GetSuppliers,
/// GetSupplierPerformance, GetCategoryMappings, GetCounterparties.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "DropshipQueries")]
[Trait("Phase", "Dalga15")]
public class DropshipQueryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken CT = CancellationToken.None;

    // ── GetDropshipOrdersHandler ──

    [Fact]
    public async Task GetDropshipOrdersHandler_NullRequest_Throws()
    {
        var sut = new GetDropshipOrdersHandler(
            new Mock<IDropshipOrderRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetDropshipOrdersHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IDropshipOrderRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Dropshipping.Entities.DropshipOrder>());

        var sut = new GetDropshipOrdersHandler(repo.Object);
        var result = await sut.Handle(new GetDropshipOrdersQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetDropshipProductsHandler ──

    [Fact]
    public async Task GetDropshipProductsHandler_NullRequest_Throws()
    {
        var sut = new GetDropshipProductsHandler(
            new Mock<IDropshipProductRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetDropshipProductsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IDropshipProductRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<bool?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Dropshipping.Entities.DropshipProduct>());

        var sut = new GetDropshipProductsHandler(repo.Object);
        var result = await sut.Handle(new GetDropshipProductsQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetSuppliersHandler ──

    [Fact]
    public async Task GetSuppliersHandler_NullRequest_Throws()
    {
        var sut = new GetSuppliersHandler(
            new Mock<ISupplierRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetSuppliersHandler_ActiveOnly_ReturnsEmptyList()
    {
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Supplier>());

        var sut = new GetSuppliersHandler(repo.Object);
        var result = await sut.Handle(new GetSuppliersQuery(ActiveOnly: true), CT);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSuppliersHandler_AllSuppliers_ReturnsEmptyList()
    {
        var repo = new Mock<ISupplierRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Supplier>());

        var sut = new GetSuppliersHandler(repo.Object);
        var result = await sut.Handle(new GetSuppliersQuery(ActiveOnly: false), CT);

        result.Should().BeEmpty();
    }

    // ── GetSupplierPerformanceHandler ──

    [Fact]
    public async Task GetSupplierPerformanceHandler_NullRequest_Throws()
    {
        var sut = new GetSupplierPerformanceHandler(
            new Mock<IDropshipSupplierRepository>().Object,
            new Mock<IDropshipOrderRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetSupplierPerformanceHandler_NoSuppliers_ReturnsEmptyList()
    {
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        supplierRepo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Dropshipping.Entities.DropshipSupplier>());

        var orderRepo = new Mock<IDropshipOrderRepository>();
        orderRepo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Dropshipping.Entities.DropshipOrder>());

        var sut = new GetSupplierPerformanceHandler(supplierRepo.Object, orderRepo.Object);
        var result = await sut.Handle(new GetSupplierPerformanceQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetCategoryMappingsHandler ──

    [Fact]
    public async Task GetCategoryMappingsHandler_NullRequest_Throws()
    {
        var sut = new GetCategoryMappingsHandler(
            new Mock<ICategoryRepository>().Object,
            new Mock<ICategoryPlatformMappingRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetCategoryMappingsHandler_EmptyCategories_ReturnsEmptyList()
    {
        var catRepo = new Mock<ICategoryRepository>();
        catRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Category>());

        var mappingRepo = new Mock<ICategoryPlatformMappingRepository>();
        mappingRepo.Setup(r => r.GetByTenantAsync(
                It.IsAny<Guid>(), It.IsAny<MesTech.Domain.Enums.PlatformType?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.CategoryPlatformMapping>());

        var sut = new GetCategoryMappingsHandler(catRepo.Object, mappingRepo.Object);
        var result = await sut.Handle(new GetCategoryMappingsQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetCounterpartiesHandler ──

    [Fact]
    public async Task GetCounterpartiesHandler_NullRequest_Throws()
    {
        var sut = new GetCounterpartiesHandler(
            new Mock<ICounterpartyRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetCounterpartiesHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ICounterpartyRepository>();
        repo.Setup(r => r.GetAllAsync(
                It.IsAny<Guid>(),
                It.IsAny<MesTech.Domain.Accounting.Enums.CounterpartyType?>(),
                It.IsAny<bool?>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Accounting.Entities.Counterparty>());

        var sut = new GetCounterpartiesHandler(repo.Object);
        var result = await sut.Handle(new GetCounterpartiesQuery(TenantId), CT);

        result.Should().BeEmpty();
    }
}
