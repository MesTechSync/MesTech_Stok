namespace MesTechStok.Core.Integrations.Barcode;

/// <summary>
/// Barkod tarayıcı entegrasyonu için servis arayüzü
/// Farklı marka/model barkod tarayıcılarla uyumluluk sağlar
/// </summary>
public interface IBarcodeScannerService
{
    /// <summary>
    /// Barkod tarayıcı bağlantı durumu
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Bağlı olan tarayıcı bilgileri
    /// </summary>
    string? ConnectedDeviceInfo { get; }

    /// <summary>
    /// Barkod tarayıcıyı başlatır ve dinlemeye başlar
    /// </summary>
    Task<bool> StartScanningAsync();

    /// <summary>
    /// Barkod tarama işlemini durdurur
    /// </summary>
    Task<bool> StopScanningAsync();

    /// <summary>
    /// Barkod tarayıcı bağlantısını yeniden başlatır
    /// </summary>
    Task<bool> ReconnectAsync();

    /// <summary>
    /// Mevcut barkod tarayıcılarını listeler
    /// </summary>
    Task<IEnumerable<BarcodeDevice>> GetAvailableDevicesAsync();

    /// <summary>
    /// Belirli bir cihaza bağlanır
    /// </summary>
    Task<bool> ConnectToDeviceAsync(string deviceId);

    /// <summary>
    /// Barkod okunduğunda tetiklenen event
    /// </summary>
    event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;

    /// <summary>
    /// Cihaz bağlantı durumu değiştiğinde tetiklenen event
    /// </summary>
    event EventHandler<DeviceConnectionEventArgs>? DeviceConnectionChanged;

    /// <summary>
    /// Hata oluştuğunda tetiklenen event
    /// </summary>
    event EventHandler<BarcodeScanErrorEventArgs>? ScanError;

    /// <summary>
    /// Tarayıcı ayarlarını günceller
    /// </summary>
    Task<bool> UpdateScannerSettingsAsync(BarcodeScannerSettings settings);

    /// <summary>
    /// Mevcut tarayıcı ayarlarını getirir
    /// </summary>
    Task<BarcodeScannerSettings?> GetScannerSettingsAsync();

    /// <summary>
    /// Test barkodu gönderir (simülasyon için)
    /// </summary>
    Task<bool> SendTestBarcodeAsync(string barcode);
}

/// <summary>
/// Barkod cihaz bilgileri
/// </summary>
public class BarcodeDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = string.Empty; // USB, Bluetooth, WiFi
    public bool IsConnected { get; set; }
    public DateTime LastSeen { get; set; }
}

/// <summary>
/// Barkod tarandığında oluşan event args
/// </summary>
public class BarcodeScannedEventArgs : EventArgs
{
    public string Barcode { get; set; } = string.Empty;
    public string? RawData { get; set; }
    public BarcodeType BarcodeType { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string DeviceId { get; set; } = string.Empty;
    public double Quality { get; set; } = 1.0; // Okuma kalitesi (0-1 arası)
}

/// <summary>
/// Cihaz bağlantısı değiştiğinde oluşan event args
/// </summary>
public class DeviceConnectionEventArgs : EventArgs
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Barkod tarama hatası event args
/// </summary>
public class BarcodeScanErrorEventArgs : EventArgs
{
    public string ErrorMessage { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public string? DeviceId { get; set; }
    public DateTime ErrorTime { get; set; } = DateTime.UtcNow;
    public BarcodeErrorType ErrorType { get; set; }
}

/// <summary>
/// Barkod tarayıcı ayarları
/// </summary>
public class BarcodeScannerSettings
{
    public bool EnableContinuousScanning { get; set; } = true;
    public int ScanTimeout { get; set; } = 5000; // milliseconds
    public bool EnableBeep { get; set; } = true;
    public bool EnableVibration { get; set; } = false;
    public BarcodeType[] SupportedBarcodeTypes { get; set; } = Array.Empty<BarcodeType>();
    public bool AutoEnterAfterScan { get; set; } = true;
    public string? PrefixChars { get; set; }
    public string? SuffixChars { get; set; }
    public int MinBarcodeLength { get; set; } = 1;
    public int MaxBarcodeLength { get; set; } = 100;
}

/// <summary>
/// Desteklenen barkod türleri
/// </summary>
public enum BarcodeType
{
    Unknown = 0,
    EAN8 = 1,
    EAN13 = 2,
    UPC_A = 3,
    UPC_E = 4,
    Code128 = 5,
    Code39 = 6,
    Code93 = 7,
    QRCode = 8,
    DataMatrix = 9,
    PDF417 = 10,
    Aztec = 11,
    ITF = 12,
    Codabar = 13
}

/// <summary>
/// Barkod tarama hata türleri
/// </summary>
public enum BarcodeErrorType
{
    Unknown = 0,
    DeviceNotFound = 1,
    DeviceNotConnected = 2,
    ScanTimeout = 3,
    InvalidBarcode = 4,
    DeviceBusy = 5,
    PermissionDenied = 6,
    HardwareError = 7,
    SoftwareError = 8
}
