using MediatR;

namespace MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;

/// <summary>
/// Kargo etiketi indirme sorgusu — LabelPreviewAvaloniaViewModel.DownloadAsync().
/// </summary>
public sealed record DownloadShipmentLabelQuery(
    Guid TenantId,
    Guid ShipmentId,
    string? TrackingNumber = null,
    string Format = "PDF"
) : IRequest<DownloadShipmentLabelResult>;
