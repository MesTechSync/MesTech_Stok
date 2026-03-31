using MediatR;

namespace MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;

/// <summary>
/// Kargo etiketi yazdir komutu — LabelPreviewAvaloniaViewModel.PrintAsync().
/// </summary>
public sealed record PrintShipmentLabelCommand(
    Guid TenantId,
    Guid ShipmentId,
    string? PrinterName = null
) : IRequest<PrintShipmentLabelResult>;
