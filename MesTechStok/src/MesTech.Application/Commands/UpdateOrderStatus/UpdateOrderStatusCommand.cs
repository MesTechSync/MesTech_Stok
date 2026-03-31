using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Commands.UpdateOrderStatus;

/// <summary>
/// Sipariş durumunu günceller. Domain logic (Place/Ship/Deliver) tetiklenir.
/// </summary>
public record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    string? TrackingNumber = null,
    CargoProvider? CargoProvider = null
) : IRequest<UpdateOrderStatusResult>;

public sealed class UpdateOrderStatusResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
