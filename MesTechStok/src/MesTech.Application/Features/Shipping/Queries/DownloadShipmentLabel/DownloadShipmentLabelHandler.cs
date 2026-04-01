using MediatR;

namespace MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;

/// <summary>
/// Kargo etiketi indirme handler'i.
/// Stub: Gercek PDF/ZPL uretimi Infrastructure katmaninda (ILabelGeneratorService) yapilacak.
/// </summary>
public sealed class DownloadShipmentLabelHandler : IRequestHandler<DownloadShipmentLabelQuery, DownloadShipmentLabelResult>
{
    public Task<DownloadShipmentLabelResult> Handle(
        DownloadShipmentLabelQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contentType = request.Format.ToUpperInvariant() switch
        {
            "ZPL" => "application/x-zpl",
            "PNG" => "image/png",
            _ => "application/pdf"
        };

        var extension = request.Format.ToUpperInvariant() switch
        {
            "ZPL" => "zpl",
            "PNG" => "png",
            _ => "pdf"
        };

        var trackingPart = request.TrackingNumber ?? request.ShipmentId.ToString("N")[..8];
        var fileName = $"etiket_{trackingPart}_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}";

        var result = new DownloadShipmentLabelResult
        {
            LabelData = ReadOnlyMemory<byte>.Empty, // Infrastructure concern — ILabelGeneratorService
            FileName = fileName,
            ContentType = contentType
        };

        return Task.FromResult(result);
    }
}
