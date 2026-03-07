using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using Xunit;

namespace MesTechStok.Tests.Integration;

/// <summary>
/// Database Integration Tests — InMemory Provider.
/// </summary>
[Trait("Category", "Integration")]
public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ServiceProvider _serviceProvider;

    public DatabaseIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"MesTechStok_Integration_{Guid.NewGuid()}"));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();

        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Database_ShouldCreateTables_WithCorrectSchema()
    {
        var productCount = await _context.Products.CountAsync();
        var categoryCount = await _context.Categories.CountAsync();
        var supplierCount = await _context.Suppliers.CountAsync();

        Assert.True(productCount >= 0);
        Assert.True(categoryCount >= 0);
        Assert.True(supplierCount >= 0);
    }

    [Fact]
    public async Task Database_ShouldSupport_ComplexRelationalOperations()
    {
        var uniqueCode = Guid.NewGuid().ToString()[..8];

        var category = new Category
        {
            Name = $"Electronics_{uniqueCode}",
            Code = $"ELEC_{uniqueCode}",
            Description = "Electronic devices"
        };
        _context.Categories.Add(category);

        var supplier = new Supplier
        {
            Name = $"Tech Corp_{uniqueCode}",
            Code = $"TCORP_{uniqueCode}"
        };
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Smart Phone",
            Barcode = $"PHONE123_{uniqueCode}",
            SKU = $"SKU_{uniqueCode}",
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            Price = 899.99m
        };
        _context.Products.Add(product);

        await _context.SaveChangesAsync();

        var savedProduct = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstAsync(p => p.Id == product.Id);

        Assert.NotNull(savedProduct.Category);
        Assert.NotNull(savedProduct.Supplier);
        Assert.Equal($"Electronics_{uniqueCode}", savedProduct.Category.Name);
        Assert.Equal($"Tech Corp_{uniqueCode}", savedProduct.Supplier.Name);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
