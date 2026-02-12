using System;

namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class CameraDevice
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string MonikerString { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public CameraResolution? Resolution { get; set; }
        public int FrameRate { get; set; } = 30;
    }

    public class CameraResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }

    public class BarcodeDetectedEventArgs : EventArgs
    {
        public string BarcodeData { get; set; } = string.Empty;
        public string BarcodeFormat { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    public class CameraErrorEventArgs : EventArgs
    {
        public string Error { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
