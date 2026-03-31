using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Headless test scope — gerçek API çağrısı YAPILMAZ.
/// Katman 1.5: 12 platform view'ın veri göstermesi için dummy ürün/kategori döner.
/// Her platform için aynı sınıf kullanılır (PlatformCode parametrik).
/// </summary>
public sealed class InMemoryPlatformAdapter : IIntegratorAdapter
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid TestCategoryId = Guid.Parse("00000000-0000-0000-0000-000000000010");

    public string PlatformCode { get; }
    public bool SupportsStockUpdate => true;
    public bool SupportsPriceUpdate => true;
    public bool SupportsShipment => true;

    public InMemoryPlatformAdapter(string platformCode)
    {
        PlatformCode = platformCode;
    }

    public Task<bool> PushProductAsync(Product product, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Product> products = new[]
        {
            CreateProduct($"TST-{PlatformCode}-001", $"{PlatformCode} Kablosuz Kulaklık", 99.90m, 50),
            CreateProduct($"TST-{PlatformCode}-002", $"{PlatformCode} USB-C Şarj Kablosu", 29.90m, 200),
            CreateProduct($"TST-{PlatformCode}-003", $"{PlatformCode} Laptop Standı", 149.90m, 35),
            CreateProduct($"TST-{PlatformCode}-004", $"{PlatformCode} Mouse Pad XL", 49.90m, 120),
            CreateProduct($"TST-{PlatformCode}-005", $"{PlatformCode} Webcam HD 1080p", 199.90m, 25),
        };
        return Task.FromResult(products);
    }

    public Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<ConnectionTestResultDto> TestConnectionAsync(
        Dictionary<string, string> credentials, CancellationToken ct = default)
        => Task.FromResult(new ConnectionTestResultDto
        {
            PlatformCode = PlatformCode,
            IsSuccess = true,
            ResponseTime = TimeSpan.FromMilliseconds(1),
            StoreName = $"InMemory-{PlatformCode}"
        });

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<CategoryDto> categories = new[]
        {
            new CategoryDto { PlatformCategoryId = 1, Name = "Elektronik", ParentId = null },
            new CategoryDto { PlatformCategoryId = 2, Name = "Telefon & Aksesuar", ParentId = 1 },
            new CategoryDto { PlatformCategoryId = 3, Name = "Bilgisayar & Tablet", ParentId = 1 },
        };
        return Task.FromResult(categories);
    }

    private static Product CreateProduct(string sku, string name, decimal price, int stock)
    {
        return new Product
        {
            TenantId = TestTenantId,
            Name = name,
            SKU = sku,
            SalePrice = price,
            PurchasePrice = price * 0.6m,
            Stock = stock,
            CategoryId = TestCategoryId,
            TaxRate = 0.20m,
            MinimumStock = 5
        };
    }
}
