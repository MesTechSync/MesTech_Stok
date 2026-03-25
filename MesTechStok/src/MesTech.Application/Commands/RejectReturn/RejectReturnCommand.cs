using MediatR;

namespace MesTech.Application.Commands.RejectReturn;

/// <summary>
/// Bekleyen iade talebini reddeder.
/// </summary>
public record RejectReturnCommand(
    Guid ReturnRequestId,
    string? RejectionReason = null
) : IRequest<RejectReturnResult>;

public sealed class RejectReturnResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
