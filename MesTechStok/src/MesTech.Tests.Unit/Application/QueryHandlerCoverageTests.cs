using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Application.Queries.GetIncomes;
using MesTech.Application.Queries.GetWarehouseStock;
using MesTech.Application.Queries.GetWarehouseSummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Handler test kapsam borcu kapatma (Query handlers)
// Testsiz query handler'lar için happy path + null/empty path
// ═══════════════════════════════════════════════════════════════

#region GetWarehouseStockHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Warehouse")]
public class GetWarehouseStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepoMock = new();
    private readonly GetWarehouseStockHandler _sut;

    public GetWarehouseStockHandlerTests()
    {
        _sut = new GetWarehouseStockHandler(_productRepoMock.Object, _warehouseRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NonExistentWarehouse_ReturnsEmpty()
    {
        _warehouseRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Warehouse?)null);

        var result = await _sut.Handle(
            new GetWarehouseStockQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ExistingWarehouse_ReturnsFilteredProducts()
    {
        var warehouseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var warehouse = new Warehouse { Name = "Ana Depo", Code = "AD" };

        var product1 = new Product
        {
            Name = "Ürün 1", SKU = "U1", Stock = 10, SalePrice = 100m,
            WarehouseId = warehouseId, TenantId = tenantId, IsActive = true
        };
        var product2 = new Product
        {
            Name = "Ürün 2", SKU = "U2", Stock = 5, SalePrice = 50m,
            WarehouseId = warehouseId, TenantId = Guid.NewGuid(), IsActive = true
        };
        var products = new List<Product> { product1, product2 };

        _warehouseRepoMock.Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>())).ReturnsAsync(warehouse);
        _productRepoMock.Setup(r => r.GetByWarehouseAsync(warehouseId, It.IsAny<CancellationToken>())).ReturnsAsync(products.AsReadOnly());

        var result = await _sut.Handle(
            new GetWarehouseStockQuery(warehouseId, tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Ürün 1");
    }

    [Fact]
    public void Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetWarehouseSummaryHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Warehouse")]
public class GetWarehouseSummaryHandlerTests
{
    private readonly Mock<IWarehouseRepository> _warehouseRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock2 = new();
    private readonly GetWarehouseSummaryHandler _sut;

    public GetWarehouseSummaryHandlerTests()
    {
        _sut = new GetWarehouseSummaryHandler(_warehouseRepoMock.Object, _productRepoMock2.Object);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList()
    {
        _warehouseRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Warehouse>().AsReadOnly());

        var result = await _sut.Handle(
            new GetWarehouseSummaryQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region GetExpensesHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetExpensesHandlerTests
{
    private readonly Mock<IExpenseRepository> _repoMock = new();
    private readonly GetExpensesHandler _sut;

    public GetExpensesHandlerTests()
    {
        _sut = new GetExpensesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ByType_ReturnsFiltered()
    {
        var tenantId = Guid.NewGuid();
        var expenses = new List<Expense>
        {
            new() { TenantId = tenantId, Description = "Kira", ExpenseType = ExpenseType.Kira, Date = DateTime.UtcNow }
        };
        expenses[0].SetAmount(5000m);
        _repoMock.Setup(r => r.GetByTypeAsync(ExpenseType.Kira, tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(expenses.AsReadOnly());

        var result = await _sut.Handle(
            new GetExpensesQuery(Type: ExpenseType.Kira, TenantId: tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ByDateRange_ReturnsFiltered()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        _repoMock.Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>().AsReadOnly());

        var result = await _sut.Handle(new GetExpensesQuery(From: from, To: to), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAll()
    {
        var tenantId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAllAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Expense>().AsReadOnly());

        var result = await _sut.Handle(new GetExpensesQuery(TenantId: tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetAllAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetIncomesHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Accounting")]
public class GetIncomesHandlerTests
{
    private readonly Mock<IIncomeRepository> _repoMock = new();
    private readonly GetIncomesHandler _sut;

    public GetIncomesHandlerTests()
    {
        _sut = new GetIncomesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ByType_ReturnsFiltered()
    {
        var tenantId = Guid.NewGuid();
        var incomes = new List<Income>
        {
            new() { TenantId = tenantId, Description = "Satış", IncomeType = IncomeType.Satis, Date = DateTime.UtcNow }
        };
        incomes[0].SetAmount(10000m);
        _repoMock.Setup(r => r.GetByTypeAsync(IncomeType.Satis, tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(incomes.AsReadOnly());

        var result = await _sut.Handle(
            new GetIncomesQuery(Type: IncomeType.Satis, TenantId: tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ByDateRange_ReturnsFiltered()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        _repoMock.Setup(r => r.GetByDateRangeAsync(from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Income>().AsReadOnly());

        var result = await _sut.Handle(new GetIncomesQuery(From: from, To: to), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsAll()
    {
        var tenantId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetAllAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(new List<Income>().AsReadOnly());

        var result = await _sut.Handle(new GetIncomesQuery(TenantId: tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetAllAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
