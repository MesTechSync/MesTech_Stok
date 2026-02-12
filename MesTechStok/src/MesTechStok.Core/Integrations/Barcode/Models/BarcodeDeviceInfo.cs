namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public enum BarcodeDeviceType
    {
        USB,
        Serial,
        SerialPort = Serial, // Alias for Serial
        Bluetooth,
        WiFi,
        Camera,
        UsbHid
    }

    public enum BarcodeConnectionType
    {
        USB,
        Serial,
        Bluetooth,
        WiFi,
        Camera
    }

    public class BarcodeDeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public string DeviceType { get; set; } = string.Empty;
        public BarcodeConnectionType ConnectionType { get; set; }
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public List<BarcodeFormat> SupportedFormats { get; set; } = new();
        public DateTime? ConnectedAt { get; set; }
        public BarcodeDeviceConfiguration Configuration { get; set; } = new();
    }
}
