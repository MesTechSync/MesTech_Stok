using System;

namespace MesTechStok.Core.Data.Models
{
    /// <summary>
    /// Kamera/HID/Seri port kaynaklı barkod tarama olaylarının kalıcı log kaydı
    /// </summary>
    public class BarcodeScanLog
    {
        public long Id { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty; // QR_CODE, CODE_128, EAN_13, etc.
        public string Source { get; set; } = string.Empty; // Camera, USB_HID, Serial
        public string DeviceId { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
        public int RawLength { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string? CorrelationId { get; set; }
    }
}


