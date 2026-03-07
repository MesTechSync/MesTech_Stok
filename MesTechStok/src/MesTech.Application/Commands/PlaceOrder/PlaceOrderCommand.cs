using MediatR;

namespace MesTech.Application.Commands.PlaceOrder;

public record PlaceOrderCommand(
    int CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    string? Notes,
    IReadOnlyList<PlaceOrderItem> Items
) : IRequest<PlaceOrderResult>;

public record PlaceOrderItem(
    int ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TaxRate = 0.18m);

public class PlaceOrderResult
{
    public bool IsSuccess { get; set; }
    public int OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public string? ErrorMessage { get; set; }
}
