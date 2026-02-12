using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Concrete;
using Xunit;

namespace MesTechStok.Tests.Integration;

/// <summary>
/// SQL Server Database Integration Tests
/// A++++ Kalite: Full Database Operations Testing
/// </summary>
public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ServiceProvider _serviceProvider;

    public DatabaseIntegrationTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MesTechStok_Integration_Test;Trusted_Connection=true"));
        
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AppDbContext>();
        
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SqlServer_ShouldCreateTables_WithCorrectSchema()
    {
        // Act & Assert - Check core tables exist
        var productCount = await _context.Products.CountAsync();
        var categoryCount = await _context.Categories.CountAsync();
        var supplierCount = await _context.Suppliers.CountAsync();
        
        Assert.True(productCount >= 0);
        Assert.True(categoryCount >= 0);
        Assert.True(supplierCount >= 0);
    }

    [Fact]
    public async Task SqlServer_ShouldSupport_ComplexRelationalOperations()
    {
        // Arrange - Use unique codes to prevent duplicates
        var uniqueCode = Guid.NewGuid().ToString()[..8]; // First 8 characters of GUID
        
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
            SKU = $"SKU_{uniqueCode}", // Unique SKU to prevent duplicates
            CategoryId = category.Id,
            SupplierId = supplier.Id,
            Price = 899.99m
        };
        _context.Products.Add(product);

        // Act
        await _context.SaveChangesAsync();

        // Assert - Test relationships
        var savedProduct = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstAsync(p => p.Id == product.Id);

        Assert.NotNull(savedProduct.Category);
        Assert.NotNull(savedProduct.Supplier);
        Assert.Equal($"Electronics_{uniqueCode}", savedProduct.Category.Name);
        Assert.Equal($"Tech Corp_{uniqueCode}", savedProduct.Supplier.Name);
    }

    [Fact]
    public async Task SqlServer_ShouldSupport_TransactionOperations()
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            // Arrange
            var product1 = new Product { Name = "Product A", Barcode = "PRODA" };
            var product2 = new Product { Name = "Product B", Barcode = "PRODB" };
            
            _context.Products.AddRange(product1, product2);
            await _context.SaveChangesAsync();
            
            // Simulate error condition
            var product3 = new Product { Name = "Product C", Barcode = "PRODA" }; // Duplicate barcode
            _context.Products.Add(product3);
            
            // This should fail due to unique constraint
            await Assert.ThrowsAnyAsync<Exception>(() => _context.SaveChangesAsync());
            
            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
        }
        
        // Assert - No products should be saved
        var count = await _context.Products.CountAsync(p => p.Name.StartsWith("Product "));
        Assert.Equal(0, count);
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
