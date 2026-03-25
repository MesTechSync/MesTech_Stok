using MediatR;

namespace MesTech.Application.Queries.GetInventoryValue;

public record GetInventoryValueQuery() : IRequest<InventoryValueResult>;

public sealed class InventoryValueResult
{
    public decimal TotalValue { get; set; }
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}
