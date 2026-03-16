using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// FIFO (First In, First Out) yontemi ile satilan mal maliyeti (COGS) hesaplama servisi.
/// StockMovement kayitlarindan alis katmanlari olusturur ve satis hareketlerini
/// en eski katmandan tuketir.
/// </summary>
public interface IFifoCostCalculationService
{
    /// <summary>
    /// Belirli bir urun icin FIFO COGS hesaplar.
    /// </summary>
    Task<FifoCostResultDto> CalculateCOGSAsync(Guid tenantId, Guid productId, CancellationToken ct = default);

    /// <summary>
    /// Tenant'a ait tum urunler icin FIFO COGS hesaplar.
    /// </summary>
    Task<IReadOnlyList<FifoCostResultDto>> CalculateAllCOGSAsync(Guid tenantId, CancellationToken ct = default);
}
