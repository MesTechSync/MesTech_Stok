using MesTech.Application.DTOs;
using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform entegrasyon adapter'ı — her platform (OpenCart, Trendyol vb.) bunu implement eder.
/// </summary>
public interface IIntegratorAdapter
{
    string PlatformCode { get; }
    bool SupportsStockUpdate { get; }
    bool SupportsPriceUpdate { get; }
    bool SupportsShipment { get; }

    Task<bool> PushProductAsync(Product product, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> PullProductsAsync(CancellationToken ct = default);
    Task<bool> PushStockUpdateAsync(Guid productId, int newStock, CancellationToken ct = default);
    Task<bool> PushPriceUpdateAsync(Guid productId, decimal newPrice, CancellationToken ct = default);
    Task<ConnectionTestResultDto> TestConnectionAsync(Dictionary<string, string> credentials, CancellationToken ct = default);

    /// <summary>
    /// Platform kategorilerini ceker. Desteklemeyen adapter'lar bos liste doner.
    /// </summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Delta sync — sadece belirtilen tarihten sonra degisen urunleri ceker.
    /// Desteklemeyen adapter'lar PullProductsAsync'e fallback yapar.
    /// D12-11: dateQueryType=LAST_MODIFIED_DATE (Trendyol), lastModifiedDate (HB), vb.
    /// </summary>
    Task<ProductSyncResult> SyncProductsDeltaAsync(
        DateTime lastSyncTime, int pageSize = 200, CancellationToken ct = default)
        => Task.FromResult(ProductSyncResult.Empty); // Default: desteklemiyor
}

/// <summary>
/// Delta sync sonucu — kac urun cekildi, kac API call yapildi.
/// </summary>
public sealed record ProductSyncResult(
    IReadOnlyList<Product> Products,
    int TotalCount,
    int PagesFetched,
    int ApiCallsMade,
    TimeSpan Duration,
    DateTime SyncTimestamp)
{
    public static ProductSyncResult Empty => new(
        Array.Empty<Product>(), 0, 0, 0, TimeSpan.Zero, DateTime.UtcNow);
}
