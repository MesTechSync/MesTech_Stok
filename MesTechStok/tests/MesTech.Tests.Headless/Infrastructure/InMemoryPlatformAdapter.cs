using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;

namespace MesTech.Tests.Headless.Infrastructure;

/// <summary>
/// Headless test scope — gerçek API çağrısı YAPILMAZ.
/// Sabit ürün/sipariş verisi döndüren generic platform adapter.
/// Her platform için aynı sınıf kullanılır (PlatformCode parametrik).
/// Gerçek veri PostgreSQL seed'den gelir — bu adapter sadece platform API'yi taklit eder.
/// </summary>
public sealed class InMemoryPlatformAdapter : IIntegratorAdapter
{
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
        => Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());

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
            new CategoryDto { PlatformCategoryId = 2, Name = "Telefon", ParentId = 1 },
            new CategoryDto { PlatformCategoryId = 3, Name = "Giyim", ParentId = null },
            new CategoryDto { PlatformCategoryId = 4, Name = "Ev & Yaşam", ParentId = null },
            new CategoryDto { PlatformCategoryId = 5, Name = "Spor", ParentId = null },
        };
        return Task.FromResult(categories);
    }
}
