using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Queries.ListOrders;

public record ListOrdersQuery(
    DateTime? From = null,
    DateTime? To = null,
    string? Status = null
) : IRequest<IReadOnlyList<OrderListDto>>;

public sealed class OrderListDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PlatformType? SourcePlatform { get; set; }
    public string? ExternalOrderId { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
}
