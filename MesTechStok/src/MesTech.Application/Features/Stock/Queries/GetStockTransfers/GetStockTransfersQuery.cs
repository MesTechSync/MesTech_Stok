using MediatR;

namespace MesTech.Application.Features.Stock.Queries.GetStockTransfers;

public record GetStockTransfersQuery(Guid TenantId, int Count = 100) : IRequest<IReadOnlyList<StockTransferItemDto>>;

public sealed class StockTransferItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime MovementDate { get; set; }
}
