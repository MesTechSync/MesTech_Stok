using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Concrete;
using Xunit;

namespace MesTechStok.Tests.Services;

/// <summary>
/// ProductService Integration Tests — InMemory Provider.
/// </summary>
[Trait("Category", "Integration")]
public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProductService _productService;
    private readonly ServiceProvider _serviceProvider;

    public ProductServiceTests()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"MesTechStok_ProductService_{Guid.NewGuid()}"));

        services.AddLogging(builder => builder.AddConsole());
        services.AddTransient<ProductService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();
        _productService = _serviceProvider.GetRequiredService<ProductService>();

        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateProduct_ShouldSave_WithValidData()
    {
        var product = new Product
        {
            Name = "Test Product",
            Barcode = "TEST123456",
            SKU = "TST-001",
            Price = 99.99m,
            IsActive = true
        };

        var result = await _productService.CreateProductAsync(product);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);

        var savedProduct = await _context.Products.FindAsync(result.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("Test Product", savedProduct.Name);
        Assert.Equal("TEST123456", savedProduct.Barcode);
    }

    [Fact]
    public async Task GetProductByBarcode_ShouldReturn()
    {
        var product = new Product
        {
            Name = "Barcode Test Product",
            Barcode = "BARCODE123",
            SKU = "BAR-001",
            Price = 49.99m
        };

        await _productService.CreateProductAsync(product);

        var result = await _productService.GetProductByBarcodeAsync("BARCODE123");

        Assert.NotNull(result);
        Assert.Equal("Barcode Test Product", result.Name);
        Assert.Equal("BARCODE123", result.Barcode);
    }

    [Fact]
    public async Task UpdateProduct_ShouldPersist()
    {
        var product = new Product
        {
            Name = "Update Test",
            Barcode = "UPDATE123",
            Price = 29.99m
        };

        var created = await _productService.CreateProductAsync(product);

        created.Name = "Updated Product Name";
        created.Price = 39.99m;
        var updated = await _productService.UpdateProductAsync(created);

        Assert.Equal("Updated Product Name", updated.Name);
        Assert.Equal(39.99m, updated.Price);

        var dbProduct = await _context.Products.FindAsync(created.Id);
        Assert.Equal("Updated Product Name", dbProduct!.Name);
        Assert.Equal(39.99m, dbProduct.Price);
    }

    [Fact]
    public async Task DeactivateProduct_ShouldMarkInactive()
    {
        var product = new Product
        {
            Name = "Deactivate Test Product",
            Barcode = "DEACT123",
            IsActive = true
        };

        var created = await _productService.CreateProductAsync(product);

        await _productService.DeactivateProductAsync(created.Id);

        var deactivatedProduct = await _context.Products.FindAsync(created.Id);
        Assert.NotNull(deactivatedProduct);
        Assert.False(deactivatedProduct.IsActive);
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturn()
    {
        var uniqueId = Guid.NewGuid().ToString()[..8];

        var product1 = new Product { Name = $"TestProduct1_{uniqueId}", Barcode = $"P001_{uniqueId}" };
        var product2 = new Product { Name = $"TestProduct2_{uniqueId}", Barcode = $"P002_{uniqueId}" };

        await _productService.CreateProductAsync(product1);
        await _productService.CreateProductAsync(product2);

        var products = await _productService.GetAllProductsAsync();

        Assert.Contains(products, p => p.Name == $"TestProduct1_{uniqueId}");
        Assert.Contains(products, p => p.Name == $"TestProduct2_{uniqueId}");
        Assert.True(products.Any(), "Should return at least some products");
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
