namespace MesTech.Application.DTOs.Fulfillment;

/// <summary>
/// Fulfillment merkezi turleri.
/// </summary>
public enum FulfillmentCenter
{
    /// <summary>Kendi deposu (varsayilan).</summary>
    OwnWarehouse = 0,

    /// <summary>Amazon FBA (Fulfillment by Amazon).</summary>
    AmazonFBA = 1,

    /// <summary>Hepsilojistik (Hepsiburada fulfillment).</summary>
    Hepsilojistik = 2,

    /// <summary>Trendyol Fulfillment center.</summary>
    TrendyolFulfillment = 3
}
