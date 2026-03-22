using MediatR;

namespace MesTech.Application.Commands.ApproveReturn;

/// <summary>
/// Bekleyen iade talebini onaylar.
/// AutoRestoreStock aktifse stok otomatik olarak arttirilir.
/// </summary>
public record ApproveReturnCommand(
    Guid ReturnRequestId,
    bool AutoRestoreStock = true
) : IRequest<ApproveReturnResult>;

public class ApproveReturnResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public bool StockRestored { get; set; }
}
