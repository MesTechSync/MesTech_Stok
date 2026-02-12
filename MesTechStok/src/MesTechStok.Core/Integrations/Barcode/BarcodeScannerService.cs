using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MesTechStok.Core.Integrations.Barcode.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MesTechStok.Core.Integrations.Barcode
{
    public class BarcodeScannerService : IBarcodeScannerService, IDisposable
    {
        private readonly Dictionary<string, BarcodeDeviceInfo> _connectedDevices;
        private readonly Dictionary<string, SerialPort> _serialPorts;
        private readonly object _lockObject = new object();
        private bool _isScanning = false;
        private bool _disposed = false;
        private BarcodeScannerSettings? _currentSettings;
        private readonly HidBarcodeListener _hidListener;

        // Interface Events
        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
        public event EventHandler<BarcodeScanErrorEventArgs>? ScanError;
        public event EventHandler<DeviceConnectionEventArgs>? DeviceConnectionChanged;

        // Legacy Events (for backward compatibility)
        public event EventHandler<BarcodeErrorEventArgs>? LegacyScanError;
        public event EventHandler<DeviceConnectionEventArgs>? DeviceConnected;
        public event EventHandler<DeviceConnectionEventArgs>? DeviceDisconnected;

        // Interface Properties
        public bool IsConnected => _connectedDevices.Any(d => d.Value.IsConnected);

        public string? ConnectedDeviceInfo
        {
            get
            {
                var connectedDevice = _connectedDevices.Values.FirstOrDefault(d => d.IsConnected);
                return connectedDevice?.DeviceName;
            }
        }

        public BarcodeScannerService()
        {
            _connectedDevices = new Dictionary<string, BarcodeDeviceInfo>();
            _serialPorts = new Dictionary<string, SerialPort>();
            _currentSettings = new BarcodeScannerSettings();

            // HID listener'ı başlat
            _hidListener = new HidBarcodeListener();
            _hidListener.BarcodeReceived += OnHidBarcodeReceived;
            _hidListener.Error += OnHidError;
        }

        // Interface Methods
        public async Task<bool> StartScanningAsync()
        {
            _isScanning = true;

            // HID listener'ı başlat
            await _hidListener.StartListeningAsync();

            // USB HID dinleyicisini başlat (simüle edilmiş - backward compatibility için)
            _ = Task.Run(() => SimulateUsbHidScanning());

            return true;
        }

        public async Task<bool> StopScanningAsync()
        {
            _isScanning = false;

            // HID listener'ı durdur
            await _hidListener.StopListeningAsync();

            return true;
        }

        public async Task<bool> ReconnectAsync()
        {
            try
            {
                // Tüm bağlantıları kes ve yeniden bağlan
                var deviceIds = _connectedDevices.Keys.ToList();

                foreach (var deviceId in deviceIds)
                {
                    await DisconnectDeviceAsync(deviceId);
                    await Task.Delay(100);
                    await ConnectDeviceAsync(deviceId);
                }

                return true;
            }
            catch (Exception ex)
            {
                OnScanError(new BarcodeScanErrorEventArgs
                {
                    ErrorMessage = $"Reconnection failed: {ex.Message}",
                    Exception = ex,
                    ErrorType = BarcodeErrorType.DeviceNotConnected
                });
                return false;
            }
        }

        public async Task<IEnumerable<BarcodeDevice>> GetAvailableDevicesAsync()
        {
            var devices = new List<BarcodeDevice>();

            await Task.Run(() =>
            {
                try
                {
                    var ports = SerialPort.GetPortNames();
                    foreach (var portName in ports)
                    {
                        var device = new BarcodeDevice
                        {
                            Id = $"SERIAL_{portName}",
                            Name = $"Serial Barcode Scanner ({portName})",
                            Manufacturer = "Generic",
                            Model = "Serial Scanner",
                            ConnectionType = "Serial",
                            IsConnected = _connectedDevices.ContainsKey($"SERIAL_{portName}"),
                            LastSeen = DateTime.Now
                        };
                        devices.Add(device);
                    }

                    // Varsayılan USB HID cihazı ekle
                    var usbDevice = new BarcodeDevice
                    {
                        Id = "USB_HID_DEFAULT",
                        Name = "USB HID Barcode Scanner",
                        Manufacturer = "Generic",
                        Model = "USB HID Scanner",
                        ConnectionType = "USB",
                        IsConnected = _connectedDevices.ContainsKey("USB_HID_DEFAULT"),
                        LastSeen = DateTime.Now
                    };
                    devices.Add(usbDevice);
                }
                catch (Exception ex)
                {
                    OnScanError(new BarcodeScanErrorEventArgs
                    {
                        ErrorMessage = $"Device scan error: {ex.Message}",
                        Exception = ex,
                        ErrorType = BarcodeErrorType.HardwareError
                    });
                }
            });

            return devices;
        }

        public async Task<bool> ConnectToDeviceAsync(string deviceId)
        {
            return await ConnectDeviceAsync(deviceId);
        }

        public async Task<bool> UpdateScannerSettingsAsync(BarcodeScannerSettings settings)
        {
            try
            {
                _currentSettings = settings;

                // Gerçek implementasyonda buraya cihaza özel konfigürasyon komutları gelecek

                return true;
            }
            catch (Exception ex)
            {
                OnScanError(new BarcodeScanErrorEventArgs
                {
                    ErrorMessage = $"Settings update failed: {ex.Message}",
                    Exception = ex,
                    ErrorType = BarcodeErrorType.SoftwareError
                });
                return false;
            }
        }

        public async Task<BarcodeScannerSettings?> GetScannerSettingsAsync()
        {
            return _currentSettings;
        }

        public async Task<bool> SendTestBarcodeAsync(string barcode)
        {
            try
            {
                var eventArgs = new BarcodeScannedEventArgs
                {
                    Barcode = barcode,
                    RawData = barcode,
                    BarcodeType = DetectBarcodeTypeFromData(barcode),
                    ScannedAt = DateTime.UtcNow,
                    DeviceId = "TEST_DEVICE",
                    Quality = 1.0
                };

                OnBarcodeScanned(eventArgs);
                return true;
            }
            catch (Exception ex)
            {
                OnScanError(new BarcodeScanErrorEventArgs
                {
                    ErrorMessage = $"Test barcode failed: {ex.Message}",
                    Exception = ex,
                    ErrorType = BarcodeErrorType.SoftwareError
                });
                return false;
            }
        }

        // Legacy Methods (for backward compatibility)
        public async Task<IEnumerable<BarcodeDeviceInfo>> ScanForDevicesAsync()
        {
            var devices = new List<BarcodeDeviceInfo>();

            await Task.Run(() =>
            {
                try
                {
                    var ports = SerialPort.GetPortNames();
                    foreach (var portName in ports)
                    {
                        var device = new BarcodeDeviceInfo
                        {
                            DeviceId = $"SERIAL_{portName}",
                            DeviceName = $"Serial Barcode Scanner ({portName})",
                            DeviceType = BarcodeDeviceType.SerialPort.ToString(),
                            ConnectionType = BarcodeConnectionType.Serial,
                            PortName = portName,
                            IsConnected = false,
                            SupportedFormats = new List<BarcodeFormat>
                            {
                                BarcodeFormat.Code128,
                                BarcodeFormat.Code39,
                                BarcodeFormat.EAN13,
                                BarcodeFormat.EAN8,
                                BarcodeFormat.UPCA,
                                BarcodeFormat.QRCode
                            }
                        };
                        devices.Add(device);
                    }

                    var usbDevice = new BarcodeDeviceInfo
                    {
                        DeviceId = "USB_HID_DEFAULT",
                        DeviceName = "USB HID Barcode Scanner",
                        DeviceType = BarcodeDeviceType.UsbHid.ToString(),
                        ConnectionType = BarcodeConnectionType.USB,
                        IsConnected = false,
                        SupportedFormats = new List<BarcodeFormat>
                        {
                            BarcodeFormat.Code128,
                            BarcodeFormat.Code39,
                            BarcodeFormat.EAN13,
                            BarcodeFormat.EAN8,
                            BarcodeFormat.UPCA,
                            BarcodeFormat.QRCode
                        }
                    };
                    devices.Add(usbDevice);
                }
                catch (Exception ex)
                {
                    OnScanError(new BarcodeScanErrorEventArgs
                    {
                        ErrorMessage = $"Device scan error: {ex.Message}",
                        Exception = ex,
                        ErrorType = BarcodeErrorType.HardwareError
                    });
                }
            });

            return devices;
        }

        public async Task<bool> ConnectDeviceAsync(string deviceId)
        {
            try
            {
                var devices = await ScanForDevicesAsync();
                var device = devices.FirstOrDefault(d => d.DeviceId == deviceId);

                if (device == null)
                    return false;

                lock (_lockObject)
                {
                    if (_connectedDevices.ContainsKey(deviceId))
                        return true;

                    if (device.DeviceType == BarcodeDeviceType.SerialPort.ToString())
                    {
                        var serialPort = new SerialPort(device.PortName, 9600, Parity.None, 8, StopBits.One);
                        serialPort.DataReceived += (sender, e) => OnSerialDataReceived(sender, e, deviceId);

                        serialPort.Open();
                        _serialPorts[deviceId] = serialPort;
                    }

                    device.IsConnected = true;
                    device.ConnectedAt = DateTime.Now;
                    _connectedDevices[deviceId] = device;

                    OnDeviceConnectionChanged(new DeviceConnectionEventArgs
                    {
                        DeviceId = deviceId,
                        DeviceName = device.DeviceName,
                        IsConnected = true,
                        EventTime = DateTime.UtcNow
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                OnScanError(new BarcodeScanErrorEventArgs
                {
                    ErrorMessage = $"Device connection error: {ex.Message}",
                    Exception = ex,
                    ErrorType = BarcodeErrorType.DeviceNotConnected
                });
                return false;
            }
        }

        public async Task<bool> DisconnectDeviceAsync(string deviceId)
        {
            try
            {
                lock (_lockObject)
                {
                    if (!_connectedDevices.ContainsKey(deviceId))
                        return false;

                    var device = _connectedDevices[deviceId];

                    if (_serialPorts.ContainsKey(deviceId))
                    {
                        var serialPort = _serialPorts[deviceId];
                        if (serialPort.IsOpen)
                            serialPort.Close();
                        serialPort.Dispose();
                        _serialPorts.Remove(deviceId);
                    }

                    device.IsConnected = false;
                    _connectedDevices.Remove(deviceId);

                    OnDeviceConnectionChanged(new DeviceConnectionEventArgs
                    {
                        DeviceId = deviceId,
                        DeviceName = device.DeviceName,
                        IsConnected = false,
                        EventTime = DateTime.UtcNow
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                OnScanError(new BarcodeScanErrorEventArgs
                {
                    ErrorMessage = $"Device disconnection error: {ex.Message}",
                    Exception = ex,
                    ErrorType = BarcodeErrorType.DeviceNotConnected
                });
                return false;
            }
        }

        public IEnumerable<BarcodeDeviceInfo> GetConnectedDevices()
        {
            lock (_lockObject)
            {
                return _connectedDevices.Values.ToList();
            }
        }

        public async Task<bool> TestDeviceAsync(string deviceId)
        {
            try
            {
                if (!_connectedDevices.ContainsKey(deviceId))
                    return false;

                var device = _connectedDevices[deviceId];

                if (device.DeviceType == BarcodeDeviceType.SerialPort.ToString() && _serialPorts.ContainsKey(deviceId))
                {
                    var serialPort = _serialPorts[deviceId];
                    return serialPort.IsOpen;
                }

                return device.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        public BarcodeValidationResult ValidateBarcode(string barcodeData, BarcodeFormat expectedFormat)
        {
            var result = new BarcodeValidationResult
            {
                IsValid = false,
                BarcodeData = barcodeData,
                DetectedFormat = BarcodeFormat.Unknown,
                ValidationErrors = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(barcodeData))
            {
                result.ValidationErrors.Add("Barcode data is empty");
                return result;
            }

            result.DetectedFormat = DetectBarcodeFormat(barcodeData);

            if (expectedFormat != BarcodeFormat.Unknown && result.DetectedFormat != expectedFormat)
            {
                result.ValidationErrors.Add($"Expected format {expectedFormat}, but detected {result.DetectedFormat}");
                return result;
            }

            if (result.DetectedFormat == BarcodeFormat.EAN13 || result.DetectedFormat == BarcodeFormat.UPCA)
            {
                result.IsValid = ValidateEAN(barcodeData);
                if (!result.IsValid)
                    result.ValidationErrors.Add("Invalid EAN checksum");
            }
            else
            {
                result.IsValid = true;
            }

            return result;
        }

        public async Task<BarcodeDeviceInfo?> GetDeviceInfoAsync(string deviceId)
        {
            return _connectedDevices.ContainsKey(deviceId) ? _connectedDevices[deviceId] : null;
        }

        public async Task<bool> SetDeviceConfigurationAsync(string deviceId, BarcodeDeviceConfiguration config)
        {
            try
            {
                if (!_connectedDevices.ContainsKey(deviceId))
                    return false;

                var device = _connectedDevices[deviceId];
                device.Configuration = config;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<BarcodeDeviceConfiguration?> GetDeviceConfigurationAsync(string deviceId)
        {
            var device = _connectedDevices.ContainsKey(deviceId) ? _connectedDevices[deviceId] : null;
            return device?.Configuration ?? new BarcodeDeviceConfiguration();
        }

        public bool IsScanning => _isScanning;

        private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e, string deviceId)
        {
            try
            {
                var serialPort = sender as SerialPort;
                var data = serialPort?.ReadLine()?.Trim();

                if (!string.IsNullOrEmpty(data))
                {
                    var eventArgs = new BarcodeScannedEventArgs
                    {
                        Barcode = data,
                        RawData = data,
                        BarcodeType = DetectBarcodeTypeFromData(data),
                        ScannedAt = DateTime.UtcNow,
                        DeviceId = deviceId,
                        Quality = 1.0
                    };

                    OnBarcodeScanned(eventArgs);
                }
            }
            catch (Exception ex)
            {
                OnScanError(new BarcodeScanErrorEventArgs
                {
                    ErrorMessage = $"Serial data read error: {ex.Message}",
                    Exception = ex,
                    DeviceId = deviceId,
                    ErrorType = BarcodeErrorType.HardwareError
                });
            }
        }

        private async Task SimulateUsbHidScanning()
        {
            while (_isScanning && !_disposed)
            {
                await Task.Delay(100);
                // Gerçek HID dinleyici buraya gelecek
            }
        }

        private BarcodeFormat DetectBarcodeFormat(string barcodeData)
        {
            if (string.IsNullOrEmpty(barcodeData))
                return BarcodeFormat.Unknown;

            if (barcodeData.Length == 13 && barcodeData.All(char.IsDigit))
                return BarcodeFormat.EAN13;

            if (barcodeData.Length == 12 && barcodeData.All(char.IsDigit))
                return BarcodeFormat.UPCA;

            if (barcodeData.Length == 8 && barcodeData.All(char.IsDigit))
                return BarcodeFormat.EAN8;

            if (barcodeData.All(c => char.IsDigit(c) || char.IsUpper(c) || c == '-' || c == '.' || c == ' ' || c == '$' || c == '/' || c == '+' || c == '%'))
                return BarcodeFormat.Code39;

            return BarcodeFormat.Code128;
        }

        private BarcodeType DetectBarcodeTypeFromData(string barcodeData)
        {
            if (string.IsNullOrEmpty(barcodeData))
                return BarcodeType.Unknown;

            if (barcodeData.Length == 13 && barcodeData.All(char.IsDigit))
                return BarcodeType.EAN13;

            if (barcodeData.Length == 12 && barcodeData.All(char.IsDigit))
                return BarcodeType.UPC_A;

            if (barcodeData.Length == 8 && barcodeData.All(char.IsDigit))
                return BarcodeType.EAN8;

            if (barcodeData.All(c => char.IsDigit(c) || char.IsUpper(c) || c == '-' || c == '.' || c == ' ' || c == '$' || c == '/' || c == '+' || c == '%'))
                return BarcodeType.Code39;

            return BarcodeType.Code128;
        }

        private bool ValidateEAN(string ean)
        {
            if (string.IsNullOrEmpty(ean) || !ean.All(char.IsDigit))
                return false;

            if (ean.Length != 13 && ean.Length != 8)
                return false;

            var digits = ean.Select(c => int.Parse(c.ToString())).ToArray();
            var checksum = 0;

            for (int i = 0; i < digits.Length - 1; i++)
            {
                checksum += digits[i] * (i % 2 == 0 ? 1 : 3);
            }

            var calculatedCheck = (10 - (checksum % 10)) % 10;
            return calculatedCheck == digits[digits.Length - 1];
        }

        protected virtual void OnBarcodeScanned(BarcodeScannedEventArgs e)
        {
            BarcodeScanned?.Invoke(this, e);
        }

        protected virtual void OnScanError(BarcodeScanErrorEventArgs e)
        {
            ScanError?.Invoke(this, e);
        }

        protected virtual void OnDeviceConnectionChanged(DeviceConnectionEventArgs e)
        {
            DeviceConnectionChanged?.Invoke(this, e);
        }

        /// <summary>
        /// HID listener'dan gelen barkod event'ini işler
        /// </summary>
        private void OnHidBarcodeReceived(object? sender, string barcode)
        {
            var validation = ValidateBarcode(barcode, BarcodeFormat.Unknown);
            var args = new BarcodeScannedEventArgs
            {
                Barcode = barcode,
                RawData = barcode,
                BarcodeType = DetectBarcodeTypeFromData(barcode),
                ScannedAt = DateTime.UtcNow,
                DeviceId = "USB_HID_DEFAULT",
                Quality = 1.0
            };

            OnBarcodeScanned(args);
            // Persist scan log
            try
            {
                using var scope = MesTechStok.Core.Diagnostics.ServiceLocator.CreateScope();
                var db = scope.ServiceProvider.GetService<MesTechStok.Core.Data.AppDbContext>();
                if (db != null)
                {
                    db.BarcodeScanLogs.Add(new MesTechStok.Core.Data.Models.BarcodeScanLog
                    {
                        Barcode = barcode,
                        Format = DetectBarcodeFormat(barcode).ToString(),
                        Source = "USB_HID",
                        DeviceId = "USB_HID_DEFAULT",
                        IsValid = validation.IsValid,
                        ValidationMessage = string.Join(";", validation.ValidationErrors ?? new List<string>()),
                        RawLength = barcode?.Length ?? 0,
                        TimestampUtc = DateTime.UtcNow,
                        CorrelationId = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId
                    });
                    db.SaveChanges();
                }
            }
            catch { }
        }

        /// <summary>
        /// HID listener error'larını işler
        /// </summary>
        private void OnHidError(object? sender, Exception ex)
        {
            var args = new BarcodeScanErrorEventArgs
            {
                ErrorMessage = $"HID Scanner Error: {ex.Message}",
                Exception = ex,
                ErrorType = BarcodeErrorType.HardwareError,
                DeviceId = "USB_HID_DEFAULT"
            };

            OnScanError(args);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _isScanning = false;

                // HID listener'ı temizle
                _hidListener?.Dispose();

                foreach (var serialPort in _serialPorts.Values)
                {
                    if (serialPort.IsOpen)
                        serialPort.Close();
                    serialPort.Dispose();
                }

                _serialPorts.Clear();
                _connectedDevices.Clear();
                _disposed = true;
            }
        }
    }
}
