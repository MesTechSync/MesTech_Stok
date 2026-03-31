using MediatR;

namespace MesTech.Application.Commands.CancelOrder;

public record CancelOrderCommand(
    Guid OrderId,
    string? Reason = null
) : IRequest<CancelOrderResult>;

public sealed class CancelOrderResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
