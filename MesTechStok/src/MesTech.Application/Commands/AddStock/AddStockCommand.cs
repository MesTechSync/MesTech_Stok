using MediatR;

namespace MesTech.Application.Commands.AddStock;

public record AddStockCommand(
    int ProductId,
    int Quantity,
    decimal UnitCost,
    string? BatchNumber = null,
    DateTime? ExpiryDate = null,
    string? DocumentNumber = null,
    string? Reason = null,
    bool SyncToPlatforms = true
) : IRequest<AddStockResult>;

public class AddStockResult
{
    public bool IsSuccess { get; set; }
    public int NewStockLevel { get; set; }
    public int MovementId { get; set; }
    public string? ErrorMessage { get; set; }
}
