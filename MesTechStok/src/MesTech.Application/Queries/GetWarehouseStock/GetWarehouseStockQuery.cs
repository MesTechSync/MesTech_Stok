using MediatR;

namespace MesTech.Application.Queries.GetWarehouseStock;

public record GetWarehouseStockQuery(
    Guid WarehouseId,
    Guid TenantId
) : IRequest<IReadOnlyList<WarehouseStockDto>>;

public class WarehouseStockDto
{
    public Guid ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TotalValue { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsCriticalStock { get; set; }
}
