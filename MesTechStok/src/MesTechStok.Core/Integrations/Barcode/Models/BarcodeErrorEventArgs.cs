using System;

namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class BarcodeErrorEventArgs : EventArgs
    {
        public string Message { get; set; }
        public Exception? Exception { get; set; }
        public string? DeviceId { get; set; }

        public BarcodeErrorEventArgs(string message, Exception? exception = null, string? deviceId = null)
        {
            Message = message;
            Exception = exception;
            DeviceId = deviceId;
        }
    }
}
