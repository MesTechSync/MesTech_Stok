using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;

public record GetCargoTrackingListQuery(Guid TenantId, int Count = 100) : IRequest<IReadOnlyList<CargoTrackingItemDto>>;

public sealed class CargoTrackingItemDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? CargoProvider { get; set; }
    public string? CargoBarcode { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string Status { get; set; } = string.Empty;
}
