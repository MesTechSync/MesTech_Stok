using MediatR;

namespace MesTech.Application.Features.Stock.Commands.CreateStockLot;

/// <summary>
/// Yeni stok lot kaydi olusturma komutu.
/// G415: Avalonia StockLotAvaloniaView SaveLot icin backend handler.
/// </summary>
public record CreateStockLotCommand(
    Guid TenantId,
    Guid ProductId,
    string LotNumber,
    int Quantity,
    decimal UnitCost,
    Guid? WarehouseId = null,
    string? WarehouseName = null,
    Guid? SupplierId = null,
    string? SupplierName = null,
    DateTime? ExpiryDate = null,
    string? Notes = null) : IRequest<CreateStockLotResult>;

public sealed class CreateStockLotResult
{
    public bool IsSuccess { get; init; }
    public Guid LotId { get; init; }
    public string? ErrorMessage { get; init; }

    public static CreateStockLotResult Success(Guid id) => new() { IsSuccess = true, LotId = id };
    public static CreateStockLotResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
