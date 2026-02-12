using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Integrations.Barcode.Models;

namespace MesTechStok.Core.Integrations.Barcode
{
    public interface ICameraBarcodeService : IDisposable
    {
        /// <summary>
        /// Mevcut kamera cihazlarını listeler
        /// </summary>
        Task<IEnumerable<CameraDevice>> GetAvailableCamerasAsync();

        /// <summary>
        /// Belirtilen kamerayı başlatır
        /// </summary>
        Task<bool> StartCameraAsync(string deviceId);

        /// <summary>
        /// Kamerayı durdurur
        /// </summary>
        Task<bool> StopCameraAsync();

        /// <summary>
        /// Kameradan sürekli barkod tarama başlatır
        /// </summary>
        Task<bool> StartScanningAsync();

        /// <summary>
        /// Barkod taramayı durdurur
        /// </summary>
        Task<bool> StopScanningAsync();

        /// <summary>
        /// Kamera çözünürlüğünü ayarlar
        /// </summary>
        Task<bool> SetResolutionAsync(string deviceId, CameraResolution resolution);

        /// <summary>
        /// Kamera frame rate'ini ayarlar
        /// </summary>
        Task<bool> SetFrameRateAsync(string deviceId, int frameRate);

        /// <summary>
        /// Mevcut kamera durumunu getirir
        /// </summary>
        Task<CameraDevice?> GetCameraStatusAsync(string deviceId);

        /// <summary>
        /// Bağlı kameraları listeler
        /// </summary>
        IEnumerable<CameraDevice> GetConnectedCameras();

        /// <summary>
        /// Barkod algılandığında tetiklenir
        /// </summary>
        event EventHandler<BarcodeDetectedEventArgs> BarcodeDetected;

        /// <summary>
        /// Kamera hatası oluştuğunda tetiklenir
        /// </summary>
        event EventHandler<CameraErrorEventArgs> CameraError;

        /// <summary>
        /// Kamera bağlantı durumu değiştiğinde tetiklenir
        /// </summary>
        event EventHandler<DeviceConnectionEventArgs> CameraConnectionChanged;

        /// <summary>
        /// Tarama durumunu döner
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// Aktif kamera sayısını döner
        /// </summary>
        int ActiveCameraCount { get; }
    }
}
