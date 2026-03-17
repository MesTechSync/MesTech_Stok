using MesTech.Domain.Accounting.Enums;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Sabit kiymet olusturuldugunda tetiklenir.
/// Amortisman takvimi baslatmak ve muhasebe kaydini otomatik olusturmak icin kullanilir.
/// </summary>
public record FixedAssetCreatedEvent : AccountingDomainEvent
{
    public Guid FixedAssetId { get; init; }
    public string AssetName { get; init; } = string.Empty;
    public string AssetCode { get; init; } = string.Empty;
    public decimal AcquisitionCost { get; init; }
    public DepreciationMethod Method { get; init; }
}
