namespace MesTechStok.Core.Integrations.Barcode.Models;

/// <summary>
/// Lightweight DTO for scan-log persistence — decouples BarcodeScannerService from AppDbContext.
/// The Desktop layer maps this to CreateBarcodeScanLogCommand (MediatR).
/// </summary>
public class BarcodeScanLogData
{
    public string Barcode { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public int RawLength { get; set; }
    public string? CorrelationId { get; set; }
}
