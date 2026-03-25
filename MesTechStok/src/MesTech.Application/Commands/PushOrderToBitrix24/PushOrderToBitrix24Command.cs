using MediatR;

namespace MesTech.Application.Commands.PushOrderToBitrix24;

public record PushOrderToBitrix24Command(Guid OrderId) : IRequest<PushOrderToBitrix24Result>;

public sealed class PushOrderToBitrix24Result
{
    public bool IsSuccess { get; set; }
    public string? ExternalDealId { get; set; }
    public Guid? Bitrix24DealId { get; set; }
    public string? ErrorMessage { get; set; }
}
