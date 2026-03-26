using MediatR;

namespace MesTech.Application.Features.Stock.Commands.StartStockCount;

/// <summary>
/// Barkod sayım oturumu başlat — Ekran 10 Stok Sayım modu.
/// Barkod taranınca sayılan +1 artar. Bitirince BulkUpdateStockCommand ile kaydet.
/// </summary>
public record StartStockCountCommand(
    Guid TenantId,
    Guid? WarehouseId = null,
    string? Description = null
) : IRequest<StockCountSessionDto>;

public sealed class StockCountSessionDto
{
    public Guid SessionId { get; set; }
    public DateTime StartedAt { get; set; }
    public string? WarehouseName { get; set; }
    public List<StockCountItemDto> Items { get; set; } = new();
}

public sealed class StockCountItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int ExpectedStock { get; set; }
    public int CountedStock { get; set; }
    public int Difference => CountedStock - ExpectedStock;
}
