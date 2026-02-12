namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class BarcodeDeviceConfiguration
    {
        public string DeviceId { get; set; } = string.Empty;
        public string PortName { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public bool AutoConnect { get; set; } = true;
        public int ReadTimeout { get; set; } = 5000;
        public int WriteTimeout { get; set; } = 5000;
        public bool EnableBeep { get; set; } = true;
        public bool EnableLed { get; set; } = true;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
    }
}
