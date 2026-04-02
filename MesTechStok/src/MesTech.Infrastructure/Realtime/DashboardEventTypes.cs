using System.Text.Json.Serialization;

namespace MesTech.Infrastructure.Realtime;

public sealed class DashboardEvent
{
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string? Platform { get; set; }

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public static class DashboardEventType
{
    public const string SyncStatus = "sync.status";
    public const string StockLow = "stock.low";
    public const string OrderNew = "order.new";
    public const string InvoiceGenerated = "invoice.generated";
    public const string ReturnCreated = "return.created";
    public const string BuyboxLost = "pricing.buybox_lost";
    public const string PriceAutoUpdated = "pricing.auto_updated";
    public const string PriceCycleDone = "pricing.cycle_done";
}
