using MediatR;

namespace MesTech.Application.Commands.AddStockLot;

public record AddStockLotCommand(
    Guid ProductId,
    string LotNumber,
    int Quantity,
    decimal UnitCost,
    Guid? SupplierId = null,
    DateTime? ExpiryDate = null,
    Guid? WarehouseId = null
) : IRequest<AddStockLotResult>;

public sealed class AddStockLotResult
{
    public bool IsSuccess { get; set; }
    public int NewStockLevel { get; set; }
    public Guid LotId { get; set; }
    public Guid MovementId { get; set; }
    public string? ErrorMessage { get; set; }
}
