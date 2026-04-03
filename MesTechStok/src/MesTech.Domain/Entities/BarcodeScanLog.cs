using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public sealed class BarcodeScanLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public int RawLength { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }

    public static BarcodeScanLog Create(
        Guid tenantId, string barcode, string format, string source,
        bool isValid, string? validationMessage = null,
        string? deviceId = null, string? correlationId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new BarcodeScanLog
        {
            TenantId = tenantId,
            Barcode = barcode.Trim(),
            Format = format ?? string.Empty,
            Source = source.Trim(),
            IsValid = isValid,
            ValidationMessage = validationMessage,
            DeviceId = deviceId,
            CorrelationId = correlationId,
            RawLength = barcode.Length,
            TimestampUtc = DateTime.UtcNow
        };
    }
}
