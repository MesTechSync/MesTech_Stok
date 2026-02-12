using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Concrete;
using MesTechStok.Core.Services.Abstract;
using Xunit;
using System.Threading.Tasks;

namespace MesTechStok.Tests.Services;

/// <summary>
/// SQL Server ProductService Integration Tests
/// A++++ Kalite: Complete SQL Server Database Testing
/// </summary>
public class ProductServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ProductService _productService;
    private readonly ILogger<ProductService> _logger;
    private readonly ServiceProvider _serviceProvider;

    public ProductServiceTests()
    {
        // SQL Server Test Database Connection
        var services = new ServiceCollection();
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MesTechStok_Test;Trusted_Connection=true;MultipleActiveResultSets=true"));
        
        services.AddLogging(builder => builder.AddConsole());
        services.AddTransient<ProductService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();
        _logger = _serviceProvider.GetRequiredService<ILogger<ProductService>>();
        _productService = _serviceProvider.GetRequiredService<ProductService>();
        
        // Ensure database is created for tests
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task CreateProduct_ShouldSaveToSqlServer_WithValidData()
    {
        // Arrange
        var product = new Product
        {
            Name = "Test Product SQL",
            Barcode = "TEST123456",
            SKU = "TST-001",
            Price = 99.99m,
            IsActive = true
        };

        // Act
        var result = await _productService.CreateProductAsync(product);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        
        // SQL Server Database Validation
        var savedProduct = await _context.Products.FindAsync(result.Id);
        Assert.NotNull(savedProduct);
        Assert.Equal("Test Product SQL", savedProduct.Name);
        Assert.Equal("TEST123456", savedProduct.Barcode);
    }

    [Fact]
    public async Task GetProductByBarcode_ShouldReturnFromSqlServer()
    {
        // Arrange
        var product = new Product
        {
            Name = "Barcode Test Product",
            Barcode = "BARCODE123",
            SKU = "BAR-001",
            Price = 49.99m
        };
        
        await _productService.CreateProductAsync(product);

        // Act
        var result = await _productService.GetProductByBarcodeAsync("BARCODE123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Barcode Test Product", result.Name);
        Assert.Equal("BARCODE123", result.Barcode);
    }

    [Fact]
    public async Task UpdateProduct_ShouldPersistInSqlServer()
    {
        // Arrange
        var product = new Product
        {
            Name = "Update Test",
            Barcode = "UPDATE123",
            Price = 29.99m
        };
        
        var created = await _productService.CreateProductAsync(product);

        // Act
        created.Name = "Updated Product Name";
        created.Price = 39.99m;
        var updated = await _productService.UpdateProductAsync(created);

        // Assert
        Assert.Equal("Updated Product Name", updated.Name);
        Assert.Equal(39.99m, updated.Price);
        
        // SQL Server Persistence Check
        var dbProduct = await _context.Products.FindAsync(created.Id);
        Assert.Equal("Updated Product Name", dbProduct!.Name);
        Assert.Equal(39.99m, dbProduct.Price);
    }

    [Fact]
    public async Task DeactivateProduct_ShouldMarkInactiveInSqlServer()
    {
        // Arrange
        var product = new Product
        {
            Name = "Deactivate Test Product",
            Barcode = "DEACT123",
            IsActive = true
        };
        
        var created = await _productService.CreateProductAsync(product);

        // Act
        await _productService.DeactivateProductAsync(created.Id);

        // Assert - Check that product is deactivated, not deleted
        var deactivatedProduct = await _context.Products.FindAsync(created.Id);
        Assert.NotNull(deactivatedProduct);
        Assert.False(deactivatedProduct.IsActive); // Should be deactivated
    }

    [Fact]
    public async Task GetAllProducts_ShouldReturnFromSqlServer()
    {
        // Arrange - Use unique identifiers
        var uniqueId = Guid.NewGuid().ToString()[..8];
        
        var product1 = new Product { Name = $"TestProduct1_{uniqueId}", Barcode = $"P001_{uniqueId}" };
        var product2 = new Product { Name = $"TestProduct2_{uniqueId}", Barcode = $"P002_{uniqueId}" };
        
        await _productService.CreateProductAsync(product1);
        await _productService.CreateProductAsync(product2);

        // Act
        var products = await _productService.GetAllProductsAsync();

        // Assert - Check for our specific test products
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
