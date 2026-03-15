using MesTech.Application.DTOs.Fulfillment;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Fulfillment center entegrasyonu icin provider interface.
/// AmazonFBA, Hepsilojistik, TrendyolFulfillment gibi merkezler bu interface'i implement eder.
/// </summary>
public interface IFulfillmentProvider
{
    /// <summary>
    /// Bu provider'in bagli oldugu fulfillment merkezi.
    /// </summary>
    FulfillmentCenter Center { get; }

    /// <summary>
    /// Fulfillment merkezine inbound sevkiyat olusturur.
    /// </summary>
    Task<InboundResult> CreateInboundShipmentAsync(InboundShipmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Belirtilen SKU'lar icin fulfillment merkezi envanter seviyelerini sorgular.
    /// </summary>
    Task<FulfillmentInventory> GetInventoryLevelsAsync(IReadOnlyList<string> skus, CancellationToken ct = default);

    /// <summary>
    /// Mevcut bir inbound sevkiyatin durumunu sorgular.
    /// </summary>
    Task<InboundStatus> GetInboundStatusAsync(string shipmentId, CancellationToken ct = default);

    /// <summary>
    /// Fulfillment merkezi API'sinin erisilebilir olup olmadigini kontrol eder.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
}
