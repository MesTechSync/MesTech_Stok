using MediatR;

namespace MesTech.Application.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid ProductId,
    int Quantity,
    string? Reason = null,
    string? PerformedBy = null
) : IRequest<AdjustStockResult>;

public class AdjustStockResult
{
    public bool IsSuccess { get; set; }
    public int NewStockLevel { get; set; }
    public Guid MovementId { get; set; }
    public string? ErrorMessage { get; set; }
}
