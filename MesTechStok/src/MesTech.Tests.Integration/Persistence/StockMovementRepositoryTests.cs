using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence.Repositories;
using MesTech.Tests.Integration._Shared;

namespace MesTech.Tests.Integration.Persistence;

/// <summary>
/// StockMovementRepository integration tests — InMemory DB.
/// Covers: CRUD, GetByProductId ordering, GetByDateRange, GetCount.
/// </summary>
[Trait("Category", "Integration")]
public class StockMovementRepositoryTests : IntegrationTestBase
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestCategoryId = Guid.NewGuid();
    private readonly StockMovementRepository _repo;
    private readonly Guid _productId;

    public StockMovementRepositoryTests()
    {
        _repo = new StockMovementRepository(Context);

        // Seed a product for FK references
        var product = new Product
        {
            Name = "SM Test Product",
            SKU = "SM-PROD-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        };
        Context.Products.Add(product);
        Context.SaveChanges();
        _productId = product.Id;
    }

    [Fact]
    public async Task AddAsync_ShouldPersistMovement()
    {
        var movement = new StockMovement
        {
            ProductId = _productId,
            Quantity = 50,
            PreviousStock = 0,
            NewStock = 50,
            MovementType = StockMovementType.Purchase.ToString(),
            Reason = "Ilk alis",
            TenantId = TestTenantId,
            Date = DateTime.UtcNow
        };

        await _repo.AddAsync(movement);
        await Context.SaveChangesAsync();

        var saved = await _repo.GetByIdAsync(movement.Id);
        saved.Should().NotBeNull();
        saved!.Quantity.Should().Be(50);
        saved.Reason.Should().Be("Ilk alis");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ShouldReturnNull()
    {
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByProductIdAsync_ShouldReturnOrderedByDateDescending()
    {
        var baseDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Context.StockMovements.AddRange(
            new StockMovement
            {
                ProductId = _productId, Quantity = 10, TenantId = TestTenantId,
                Date = baseDate, Reason = "Oldest"
            },
            new StockMovement
            {
                ProductId = _productId, Quantity = 20, TenantId = TestTenantId,
                Date = baseDate.AddDays(5), Reason = "Middle"
            },
            new StockMovement
            {
                ProductId = _productId, Quantity = 30, TenantId = TestTenantId,
                Date = baseDate.AddDays(10), Reason = "Newest"
            }
        );
        await Context.SaveChangesAsync();

        var results = await _repo.GetByProductIdAsync(_productId);

        results.Should().HaveCount(3);
        results[0].Reason.Should().Be("Newest");
        results[1].Reason.Should().Be("Middle");
        results[2].Reason.Should().Be("Oldest");
    }

    [Fact]
    public async Task GetByProductIdAsync_DifferentProduct_ShouldNotCrossPollute()
    {
        var otherProduct = new Product
        {
            Name = "Other Product",
            SKU = "SM-OTHER-001",
            CategoryId = TestCategoryId,
            TenantId = TestTenantId
        };
        Context.Products.Add(otherProduct);
        await Context.SaveChangesAsync();

        Context.StockMovements.AddRange(
            new StockMovement { ProductId = _productId, Quantity = 10, TenantId = TestTenantId, Reason = "Mine" },
            new StockMovement { ProductId = otherProduct.Id, Quantity = 99, TenantId = TestTenantId, Reason = "Other" }
        );
        await Context.SaveChangesAsync();

        var results = await _repo.GetByProductIdAsync(_productId);

        results.Should().ContainSingle();
        results.First().Reason.Should().Be("Mine");
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldFilterAndOrderDescending()
    {
        var jan = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var feb = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc);
        var mar = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc);

        Context.StockMovements.AddRange(
            new StockMovement { ProductId = _productId, Quantity = 1, TenantId = TestTenantId, Date = jan, Reason = "Jan" },
            new StockMovement { ProductId = _productId, Quantity = 2, TenantId = TestTenantId, Date = feb, Reason = "Feb" },
            new StockMovement { ProductId = _productId, Quantity = 3, TenantId = TestTenantId, Date = mar, Reason = "Mar" }
        );
        await Context.SaveChangesAsync();

        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        var results = await _repo.GetByDateRangeAsync(from, to);

        results.Should().HaveCount(2);
        results[0].Reason.Should().Be("Mar");
        results[1].Reason.Should().Be("Feb");
    }

    [Fact]
    public async Task GetByDateRangeAsync_BoundaryInclusive_ShouldIncludeExactDates()
    {
        var exactDate = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        Context.StockMovements.Add(new StockMovement
        {
            ProductId = _productId, Quantity = 1, TenantId = TestTenantId,
            Date = exactDate, Reason = "Boundary"
        });
        await Context.SaveChangesAsync();

        var results = await _repo.GetByDateRangeAsync(exactDate, exactDate);

        results.Should().ContainSingle();
        results.First().Reason.Should().Be("Boundary");
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnTotalMovements()
    {
        Context.StockMovements.AddRange(
            new StockMovement { ProductId = _productId, Quantity = 10, TenantId = TestTenantId },
            new StockMovement { ProductId = _productId, Quantity = 20, TenantId = TestTenantId },
            new StockMovement { ProductId = _productId, Quantity = -5, TenantId = TestTenantId }
        );
        await Context.SaveChangesAsync();

        var count = await _repo.GetCountAsync();

        count.Should().Be(3);
    }

    [Fact]
    public async Task GetCountAsync_WhenEmpty_ShouldReturnZero()
    {
        var count = await _repo.GetCountAsync();

        count.Should().Be(0);
    }
}
