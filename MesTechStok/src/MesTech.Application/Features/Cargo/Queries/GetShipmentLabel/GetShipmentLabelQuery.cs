using MediatR;

namespace MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;

public record GetShipmentLabelQuery(Guid TenantId, string ShipmentId)
    : IRequest<ShipmentLabelResult>;

public sealed class ShipmentLabelResult
{
    public bool IsSuccess { get; init; }
    public ReadOnlyMemory<byte>? LabelData { get; init; }
    public string ContentType { get; init; } = "application/pdf";
    public string? FileName { get; init; }
    public string? ErrorMessage { get; init; }
}
