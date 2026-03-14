namespace MesTech.Application.Interfaces;

/// <summary>
/// CRM Deal kazanildiginda otomatik Order olusturma koprüsü.
/// Deal.MarkAsWon() sonrasi cagirilir.
/// </summary>
public interface ICrmOrderBridgeService
{
    /// <summary>Deal'den yeni Order olusturur ve Deal.OrderId'yi günceller.</summary>
    Task<Guid> CreateOrderFromDealAsync(Guid dealId, CancellationToken ct = default);

    /// <summary>Pazaryeri siparisi geldiginde musteri yoksa Lead olusturur.</summary>
    Task<Guid?> CreateLeadFromOrderAsync(Guid orderId, CancellationToken ct = default);
}
