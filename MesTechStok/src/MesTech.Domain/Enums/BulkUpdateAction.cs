namespace MesTech.Domain.Enums;

/// <summary>
/// Toplu ürün güncelleme aksiyonları.
/// </summary>
public enum BulkUpdateAction
{
    PriceIncreasePercent,
    PriceDecreasePercent,
    PriceSetFixed,
    StockSet,
    StockAdd,
    StatusActivate,
    StatusDeactivate,
    CategoryAssign,
    PlatformPublish,
    PlatformUnpublish,
    BrandAssign,
    TagAdd,
    TagRemove,
    DescriptionSet,
    StockReset
}
