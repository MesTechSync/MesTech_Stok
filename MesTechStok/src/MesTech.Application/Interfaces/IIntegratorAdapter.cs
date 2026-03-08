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
    /// Platform kategori listesini ceker.
    /// Default: bos liste doner — adapter'lar override edebilir.
    /// </summary>
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(Array.Empty<CategoryDto>());
}
