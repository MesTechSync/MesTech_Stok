using System.Collections.Generic;

namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class BarcodeScannerSettings
    {
        public string DeviceId { get; set; } = string.Empty;
        public bool AutoConnect { get; set; } = true;
        public bool EnableBeep { get; set; } = true;
        public bool EnableLed { get; set; } = true;
        public int ScanTimeout { get; set; } = 5000;
        public List<BarcodeFormat> SupportedFormats { get; set; } = new();
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public bool ValidateChecksum { get; set; } = true;
        public int MinBarcodeLength { get; set; } = 3;
        public int MaxBarcodeLength { get; set; } = 100;
    }
}
