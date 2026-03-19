using MediatR;

namespace MesTech.Application.Commands.TransferStock;

public record TransferStockCommand(
    Guid ProductId,
    Guid SourceWarehouseId,
    Guid TargetWarehouseId,
    int Quantity,
    string? Notes = null
) : IRequest<TransferStockResult>;

public class TransferStockResult
{
    public bool IsSuccess { get; set; }
    public int SourceRemainingStock { get; set; }
    public int TargetNewStock { get; set; }
    public Guid MovementId { get; set; }
    public string? ErrorMessage { get; set; }
}
