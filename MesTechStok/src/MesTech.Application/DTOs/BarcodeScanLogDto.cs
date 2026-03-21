namespace MesTech.Application.DTOs;

/// <summary>
/// Barcode Scan Log data transfer object.
/// </summary>
public class BarcodeScanLogDto
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public int RawLength { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string? CorrelationId { get; set; }
}
