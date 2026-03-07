using MediatR;

namespace MesTech.Application.Commands.RemoveStock;

public record RemoveStockCommand(
    int ProductId,
    int Quantity,
    string? Reason = null,
    string? DocumentNumber = null,
    bool SyncToPlatforms = true
) : IRequest<RemoveStockResult>;

public class RemoveStockResult
{
    public bool IsSuccess { get; set; }
    public int NewStockLevel { get; set; }
    public int MovementId { get; set; }
    public string? ErrorMessage { get; set; }
}
