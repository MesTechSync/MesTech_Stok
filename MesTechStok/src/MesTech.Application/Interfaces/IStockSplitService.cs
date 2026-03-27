using MesTech.Application.DTOs.Fulfillment;
using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Coklu depo/fulfillment merkezi arasinda stok dagitim servisi.
/// Urun bazli stok sorgulama ve fulfillment merkezi stok guncelleme islemlerini yonetir.
/// </summary>
public interface IStockSplitService
{
    /// <summary>
    /// Belirtilen urune ait tum depo/fulfillment center stok kayitlarini getirir.
    /// </summary>
    Task<IReadOnlyList<ProductWarehouseStock>> GetStockByProductAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Belirtilen urunun tum depolardaki toplam kullanilabilir stok miktarini hesaplar.
    /// </summary>
    Task<int> GetTotalAvailableAsync(Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Belirtilen fulfillment merkezindeki urun stogunu gunceller.
    /// </summary>
    Task UpdateFulfillmentStockAsync(Guid productId, FulfillmentCenter center, int quantity, CancellationToken ct = default);

    /// <summary>
    /// Birden fazla urunun toplam kullanilabilir stok miktarini tek sorguda getirir (N+1 onleme).
    /// </summary>
    Task<Dictionary<Guid, int>> GetTotalAvailableBulkAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);
}
