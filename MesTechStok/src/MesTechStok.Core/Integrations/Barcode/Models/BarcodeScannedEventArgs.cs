using System;

namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class BarcodeScannedEventArgs : EventArgs
    {
        public string Barcode { get; set; } = string.Empty;
        public BarcodeFormat Format { get; set; }
        public DateTime ScanTime { get; set; } = DateTime.Now;
        public string DeviceId { get; set; } = string.Empty;
        public bool IsValid { get; set; } = true;

        public BarcodeScannedEventArgs(string barcode, BarcodeFormat format, string deviceId = "")
        {
            Barcode = barcode;
            Format = format;
            DeviceId = deviceId;
        }
    }
}
