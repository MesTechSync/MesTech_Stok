using MediatR;

namespace MesTech.Application.Commands.CreateOrder;

/// <summary>
/// Manual order creation — different from PlaceOrder which handles stock deduction.
/// This is for importing or manually entering orders without stock impact.
/// </summary>
public record CreateOrderCommand(
    Guid CustomerId,
    string CustomerName,
    string? CustomerEmail,
    string OrderType,
    string? Notes,
    DateTime? RequiredDate = null
) : IRequest<CreateOrderResult>;

public sealed class CreateOrderResult
{
    public bool IsSuccess { get; set; }
    public Guid OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? ErrorMessage { get; set; }
}
