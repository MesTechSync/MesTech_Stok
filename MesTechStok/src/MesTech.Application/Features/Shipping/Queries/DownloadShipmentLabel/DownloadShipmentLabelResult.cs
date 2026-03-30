namespace MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;

/// <summary>
/// Kargo etiketi indirme sonucu.
/// </summary>
public sealed class DownloadShipmentLabelResult
{
    public byte[] LabelData { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}
