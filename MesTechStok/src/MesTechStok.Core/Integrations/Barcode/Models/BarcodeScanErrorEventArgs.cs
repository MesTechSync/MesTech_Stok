using System;

namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class BarcodeScanErrorEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DateTime ErrorTime { get; set; } = DateTime.Now;

        public BarcodeScanErrorEventArgs(string message, Exception? exception = null, string deviceId = "")
        {
            Message = message;
            Exception = exception;
            DeviceId = deviceId;
        }
    }
}
