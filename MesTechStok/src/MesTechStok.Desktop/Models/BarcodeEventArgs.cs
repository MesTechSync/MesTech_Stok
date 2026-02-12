using System;

namespace MesTechStok.Desktop.Models
{
    /// <summary>
    /// Barkod tarama işlemleri için özel EventArgs sınıfı
    /// </summary>
    public class BarcodeEventArgs : EventArgs
    {
        /// <summary>
        /// Okunan barkod değeri
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// Barkod türü (QR, Code128, vb.)
        /// </summary>
        public string BarcodeType { get; set; } = "Unknown";

        /// <summary>
        /// Tarama zamanı
        /// </summary>
        public DateTime ScannedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// Tarama yapan cihaz ID'si
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Tarama kalite durumu
        /// </summary>
        public string Quality { get; set; } = "Good";

        /// <summary>
        /// Varsayılan constructor
        /// </summary>
        public BarcodeEventArgs()
        {
        }

        /// <summary>
        /// Barkod ile constructor
        /// </summary>
        /// <param name="barcode">Okunan barkod değeri</param>
        public BarcodeEventArgs(string barcode)
        {
            Barcode = barcode ?? string.Empty;
        }

        /// <summary>
        /// Tam detay constructor
        /// </summary>
        /// <param name="barcode">Okunan barkod değeri</param>
        /// <param name="barcodeType">Barkod türü</param>
        /// <param name="deviceId">Cihaz ID'si</param>
        public BarcodeEventArgs(string barcode, string barcodeType, string deviceId)
        {
            Barcode = barcode ?? string.Empty;
            BarcodeType = barcodeType ?? "Unknown";
            DeviceId = deviceId ?? string.Empty;
        }
    }
}
