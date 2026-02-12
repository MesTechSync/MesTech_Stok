using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Diagnostics;
using MesTechStok.Desktop.Utils;
using MesTechStok.Desktop.Models;
using System.Collections.Generic;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// CHARLIE TİMİ - Gerçek Barcode Hardware Service Implementation
    /// Modern ZXing.Net + OpenCvSharp4 kullanarak gerçek barkod tarama
    /// </summary>
    public class BarcodeHardwareService : IBarcodeService
    {
        private readonly ILogger<BarcodeHardwareService> _logger;
        private readonly BarcodeReader _barcodeReader;
        private VideoCapture? _videoCapture;
        private bool _isConnected = false;
        private bool _isScanning = false;
        private int _cameraIndex = 0;

        // Reader tuning
        private TimeSpan _decodeCooldown = TimeSpan.FromMilliseconds(350);
        private DateTime _lastDecodeUtc = DateTime.MinValue;
        private bool _useRoi = true;

        /// <summary>
        /// 🔄 NAMESPACE ÇAKIŞMA ÇÖZÜCÜ: ZXing format'ını ServiceBarcodeFormat'a çevir
        /// </summary>
        private ServiceBarcodeFormat ConvertZXingToLocalFormat(ZXing.BarcodeFormat zxingFormat)
        {
            return zxingFormat switch
            {
                ZXing.BarcodeFormat.CODE_128 => ServiceBarcodeFormat.Code128,
                ZXing.BarcodeFormat.CODE_39 => ServiceBarcodeFormat.Code39,
                ZXing.BarcodeFormat.EAN_13 => ServiceBarcodeFormat.EAN13,
                ZXing.BarcodeFormat.EAN_8 => ServiceBarcodeFormat.EAN8,
                ZXing.BarcodeFormat.UPC_A => ServiceBarcodeFormat.UPCA,
                ZXing.BarcodeFormat.UPC_E => ServiceBarcodeFormat.UPCE,
                ZXing.BarcodeFormat.QR_CODE => ServiceBarcodeFormat.QRCode,
                ZXing.BarcodeFormat.DATA_MATRIX => ServiceBarcodeFormat.DataMatrix,
                ZXing.BarcodeFormat.PDF_417 => ServiceBarcodeFormat.PDF417,
                ZXing.BarcodeFormat.AZTEC => ServiceBarcodeFormat.Aztec,
                ZXing.BarcodeFormat.ITF => ServiceBarcodeFormat.ITF,
                _ => ServiceBarcodeFormat.Code128 // Default fallback
            };
        }
        private double _roiTopPercent = 0.25;
        private double _roiHeightPercent = 0.50;
        private double _roiLeftPercent = 0.0;
        private double _roiWidthPercent = 1.0;
        private bool _tryInverted = false;
        private bool _assumeGs1 = true;
        private string _formatPreset = "Retail1D"; // Retail1D | RetailPlus2D | All
        private bool _priority2D = true;
        private bool _useClahe = true;
        private string _thresholding = "Adaptive"; // None | Adaptive | Otsu
        private bool _qrFallbackOpenCV = true;

        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
        public event EventHandler<string>? DeviceStatusChanged;

        public bool IsConnected => _isConnected;

        public BarcodeHardwareService(ILogger<BarcodeHardwareService> logger)
        {
            _logger = logger;

            // CHARLIE TEAM: Modern ZXing.Net Configuration
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[]
                    {
                        ZXing.BarcodeFormat.CODE_128,
                        ZXing.BarcodeFormat.CODE_39,
                        ZXing.BarcodeFormat.EAN_13,
                        ZXing.BarcodeFormat.EAN_8,
                        ZXing.BarcodeFormat.UPC_A,
                        ZXing.BarcodeFormat.UPC_E,
                        ZXing.BarcodeFormat.QR_CODE,
                        ZXing.BarcodeFormat.DATA_MATRIX,
                        ZXing.BarcodeFormat.PDF_417,
                        ZXing.BarcodeFormat.AZTEC
                    }
                }
            };

            _logger.LogInformation("[CHARLIE] BarcodeHardwareService initialized with modern ZXing.Net + OpenCvSharp4");
            // Apply config-driven options
            ApplyReaderOptionsFromConfig();
            try { GlobalLogger.Instance.LogInfo("BarcodeHardwareService hazır", "BarcodeHW"); } catch { }
        }

        private void ApplyReaderOptionsFromConfig()
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var config = sp?.GetService<IConfiguration>();
                if (config != null)
                {
                    _formatPreset = config["BarcodeView:Reader:FormatPreset"] ?? _formatPreset;
                    _useRoi = bool.TryParse(config["BarcodeView:Reader:UseROI"], out var ur) ? ur : _useRoi;
                    _roiTopPercent = double.TryParse(config["BarcodeView:Reader:RoiTopPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rtp) ? rtp : _roiTopPercent;
                    _roiHeightPercent = double.TryParse(config["BarcodeView:Reader:RoiHeightPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rhp) ? rhp : _roiHeightPercent;
                    _roiLeftPercent = double.TryParse(config["BarcodeView:Reader:RoiLeftPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rlp) ? rlp : _roiLeftPercent;
                    _roiWidthPercent = double.TryParse(config["BarcodeView:Reader:RoiWidthPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rwp) ? rwp : _roiWidthPercent;
                    // Clamp 0..1
                    _roiTopPercent = Math.Max(0, Math.Min(0.95, _roiTopPercent));
                    _roiHeightPercent = Math.Max(0.05, Math.Min(1.0, _roiHeightPercent));
                    _roiLeftPercent = Math.Max(0, Math.Min(0.95, _roiLeftPercent));
                    _roiWidthPercent = Math.Max(0.1, Math.Min(1.0, _roiWidthPercent));
                    var cooldownMs = int.TryParse(config["BarcodeView:Reader:DecodeCooldownMs"], NumberStyles.Any, CultureInfo.InvariantCulture, out var dcm) ? dcm : 350;
                    _decodeCooldown = TimeSpan.FromMilliseconds(Math.Max(150, cooldownMs));

                    var tryHarder = bool.TryParse(config["BarcodeView:Reader:TryHarder"], out var th) ? th : true;
                    _tryInverted = bool.TryParse(config["BarcodeView:Reader:TryInverted"], out var ti) ? ti : false; // prod default: false
                    _assumeGs1 = true;
                    _priority2D = bool.TryParse(config["BarcodeView:Reader:Priority2D"], out var p2d) ? p2d : _priority2D;
                    _useClahe = bool.TryParse(config["BarcodeView:Reader:UseClahe"], out var uc) ? uc : _useClahe;
                    _thresholding = config["BarcodeView:Reader:Thresholding"] ?? _thresholding;
                    _qrFallbackOpenCV = bool.TryParse(config["BarcodeView:Reader:QrFallbackOpenCV"], out var qf) ? qf : _qrFallbackOpenCV;

                    ZXing.BarcodeFormat[] allowed = _formatPreset?.Trim().ToLowerInvariant() switch
                    {
                        "retailplus2d_noupce" => new[]
                        {
                            ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                            ZXing.BarcodeFormat.UPC_A, /* UPC_E kapalı */
                            ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF,
                            ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX
                        },
                        "retailplus2d" => new[]
                        {
                            ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                            ZXing.BarcodeFormat.UPC_A, ZXing.BarcodeFormat.UPC_E,
                            ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF,
                            ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX
                        },
                        "all" => new[]
                        {
                            ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                            ZXing.BarcodeFormat.UPC_A, ZXing.BarcodeFormat.UPC_E,
                            ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF,
                            ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX, ZXing.BarcodeFormat.PDF_417, ZXing.BarcodeFormat.AZTEC
                        },
                        "retail1d_noupce" => new[]
                        {
                            ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                            ZXing.BarcodeFormat.UPC_A, /* UPC_E kapalı */
                            ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF
                        },
                        _ => new[]
                        {
                            ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                            ZXing.BarcodeFormat.UPC_A, ZXing.BarcodeFormat.UPC_E,
                            ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF
                        }
                    };

                    _barcodeReader.Options.TryHarder = tryHarder;
                    _barcodeReader.Options.TryInverted = _tryInverted;
                    _barcodeReader.Options.AssumeGS1 = _assumeGs1;
                    _barcodeReader.Options.PossibleFormats = allowed;
                }
            }
            catch { }
        }

        public Task<bool> ConnectAsync()
        {
            try
            {
                _logger.LogInformation("[CHARLIE] Connecting to camera for barcode scanning...");

                // CHARLIE TEAM: OpenCvSharp4 Camera Initialization
                _videoCapture = new VideoCapture(_cameraIndex);

                if (!_videoCapture.IsOpened())
                {
                    _logger.LogWarning("[CHARLIE] Failed to open camera {CameraIndex}", _cameraIndex);

                    // Try different camera indices
                    for (int i = 0; i < 5; i++)
                    {
                        _videoCapture?.Release();
                        _videoCapture = new VideoCapture(i);

                        if (_videoCapture.IsOpened())
                        {
                            _cameraIndex = i;
                            _logger.LogInformation("[CHARLIE] Camera connected successfully on index {CameraIndex}", i);
                            break;
                        }
                    }
                }

                if (_videoCapture?.IsOpened() == true)
                {
                    // Configure camera settings
                    // Kamera parametreleri konfigürasyondan
                    try
                    {
                        var sp = MesTechStok.Desktop.App.ServiceProvider;
                        var cfg = sp?.GetService<IConfiguration>();
                        int camW = int.TryParse(cfg?["BarcodeView:Camera:FrameWidth"], out var cw) ? cw : 640;
                        int camH = int.TryParse(cfg?["BarcodeView:Camera:FrameHeight"], out var ch) ? ch : 480;
                        int camFps = int.TryParse(cfg?["BarcodeView:Camera:Fps"], out var cf) ? cf : 30;
                        _videoCapture.Set(VideoCaptureProperties.FrameWidth, camW);
                        _videoCapture.Set(VideoCaptureProperties.FrameHeight, camH);
                        _videoCapture.Set(VideoCaptureProperties.Fps, camFps);
                    }
                    catch
                    {
                        _videoCapture.Set(VideoCaptureProperties.FrameWidth, 640);
                        _videoCapture.Set(VideoCaptureProperties.FrameHeight, 480);
                        _videoCapture.Set(VideoCaptureProperties.Fps, 30);
                    }

                    _isConnected = true;
                    DeviceStatusChanged?.Invoke(this, "Connected to camera");

                    _logger.LogInformation("[CHARLIE] Barcode hardware service connected successfully");
                    try { GlobalLogger.Instance.LogEvent("BARCODE", $"Camera connected (index={_cameraIndex})", "BarcodeHW"); } catch { }
                    return Task.FromResult(true);
                }

                _logger.LogError("[CHARLIE] No available camera found for barcode scanning");
                DeviceStatusChanged?.Invoke(this, "No camera available");
                try { GlobalLogger.Instance.LogWarning("Kamera bulunamadı", "BarcodeHW"); } catch { }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CHARLIE] Error connecting to barcode hardware");
                DeviceStatusChanged?.Invoke(this, $"Connection error: {ex.Message}");
                try { GlobalLogger.Instance.LogError($"Kamera bağlantı hatası: {ex.Message}", "BarcodeHW"); } catch { }
                return Task.FromResult(false);
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            try
            {
                _logger.LogInformation("[CHARLIE] Disconnecting barcode hardware...");

                await StopScanningAsync();

                _videoCapture?.Release();
                _videoCapture?.Dispose();
                _videoCapture = null;

                _isConnected = false;
                DeviceStatusChanged?.Invoke(this, "Disconnected");

                _logger.LogInformation("[CHARLIE] Barcode hardware disconnected successfully");
                try { GlobalLogger.Instance.LogEvent("BARCODE", "Camera disconnected", "BarcodeHW"); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CHARLIE] Error disconnecting barcode hardware");
                try { GlobalLogger.Instance.LogError($"Kamera bağlantı kesme hatası: {ex.Message}", "BarcodeHW"); } catch { }
                return false;
            }
        }

        public async Task<bool> StartScanningAsync()
        {
            try
            {
                await Task.Yield();
                if (!_isConnected || _videoCapture == null)
                {
                    _logger.LogWarning("[CHARLIE] Cannot start scanning - camera not connected");
                    try { GlobalLogger.Instance.LogWarning("Tarama başlatılamadı: kamera bağlı değil", "BarcodeHW"); } catch { }
                    return false;
                }

                if (_isScanning)
                {
                    _logger.LogInformation("[CHARLIE] Scanning already in progress");
                    try { GlobalLogger.Instance.LogInfo("Tarama zaten aktif", "BarcodeHW"); } catch { }
                    return true;
                }

                // Refresh tuning before scanning
                ApplyReaderOptionsFromConfig();

                _isScanning = true;
                DeviceStatusChanged?.Invoke(this, "Scanning started");

                _logger.LogInformation("[CHARLIE] Barcode scanning started");
                try { GlobalLogger.Instance.LogEvent("BARCODE", "ScanStarted", "BarcodeHW"); } catch { }

                // CHARLIE TEAM: Start continuous scanning loop
                _ = Task.Run(async () => await ScanContinuouslyAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CHARLIE] Error starting barcode scanning");
                try { GlobalLogger.Instance.LogError($"Tarama başlatma hatası: {ex.Message}", "BarcodeHW"); } catch { }
                return false;
            }
        }

        public async Task<bool> StopScanningAsync()
        {
            try
            {
                await Task.Yield();
                _isScanning = false;
                DeviceStatusChanged?.Invoke(this, "Scanning stopped");

                _logger.LogInformation("[CHARLIE] Barcode scanning stopped");
                try { GlobalLogger.Instance.LogEvent("BARCODE", "ScanStopped", "BarcodeHW"); } catch { }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CHARLIE] Error stopping barcode scanning");
                try { GlobalLogger.Instance.LogError($"Tarama durdurma hatası: {ex.Message}", "BarcodeHW"); } catch { }
                return false;
            }
        }

        private async Task ScanContinuouslyAsync()
        {
            _logger.LogInformation("[CHARLIE] Starting continuous barcode scanning loop...");

            while (_isScanning && _videoCapture != null && _videoCapture.IsOpened())
            {
                try
                {
                    using var frame = new Mat();

                    if (_videoCapture.Read(frame) && !frame.Empty())
                    {
                        // Cooldown (mükerrer/yanlış pozitifleri azalt)
                        if (DateTime.UtcNow - _lastDecodeUtc < _decodeCooldown)
                        {
                            await Task.Delay(20);
                            continue;
                        }

                        Mat decodeMat = frame;
                        // ROI uygula (yalnızca etkinse)
                        if (_useRoi)
                        {
                            try
                            {
                                int h = frame.Height, w = frame.Width;
                                int roiTop = Math.Max(0, Math.Min(h - 1, (int)(h * _roiTopPercent)));
                                int roiHeight = Math.Max(1, Math.Min(h - roiTop, (int)(h * _roiHeightPercent)));
                                int roiLeft = Math.Max(0, Math.Min(w - 1, (int)(w * _roiLeftPercent)));
                                int roiWidth = Math.Max(1, Math.Min(w - roiLeft, (int)(w * _roiWidthPercent)));
                                var roi = new Rect(roiLeft, roiTop, roiWidth, roiHeight);
                                decodeMat = new Mat(frame, roi);
                            }
                            catch { decodeMat = frame; }
                        }

                        // 2D okuma iyileştirmesi: preset 2D içeriyorsa gri + eşitleme uygula
                        bool presetHas2D = _barcodeReader.Options.PossibleFormats != null &&
                                            _barcodeReader.Options.PossibleFormats.Any(f => f == ZXing.BarcodeFormat.QR_CODE || f == ZXing.BarcodeFormat.DATA_MATRIX);

                        using var procMat = new Mat();
                        if (presetHas2D)
                        {
                            try
                            {
                                // 2D için kontrast/parlama dayanıklı işleme
                                Cv2.CvtColor(decodeMat, procMat, ColorConversionCodes.BGR2GRAY);
                                if (_useClahe)
                                {
                                    using var clahe = Cv2.CreateCLAHE(clipLimit: 2.0, tileGridSize: new OpenCvSharp.Size(8, 8));
                                    clahe.Apply(procMat, procMat);
                                }
                                else
                                {
                                    Cv2.EqualizeHist(procMat, procMat);
                                }
                                // Parlama azaltma için bilateral filtre
                                Cv2.BilateralFilter(procMat, procMat, 5, 50, 50);
                                // Eşikleme
                                var th = (_thresholding ?? "None").Trim().ToLowerInvariant();
                                if (th == "adaptive")
                                {
                                    Cv2.AdaptiveThreshold(procMat, procMat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 11, 2);
                                }
                                else if (th == "otsu")
                                {
                                    Cv2.Threshold(procMat, procMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                                }
                            }
                            catch { decodeMat.CopyTo(procMat); }
                        }
                        else
                        {
                            // 1D için gürültü azaltma + bar kalınlaştırma
                            try
                            {
                                Cv2.CvtColor(decodeMat, procMat, ColorConversionCodes.BGR2GRAY);
                                Cv2.GaussianBlur(procMat, procMat, new OpenCvSharp.Size(3, 3), 0);
                                using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(3, 1));
                                Cv2.MorphologyEx(procMat, procMat, MorphTypes.Close, kernel);
                                Cv2.Threshold(procMat, procMat, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
                            }
                            catch { decodeMat.CopyTo(procMat); }
                        }

                        using var bitmap = MatToBitmap(procMat);
                        _lastDecodeUtc = DateTime.UtcNow;

                        ZXing.Result? result = null;
                        // 2D önceliklendirme: önce QR/DM dene (yanlış sınıflamayı azaltır)
                        if (presetHas2D && _priority2D)
                        {
                            var twoDReader = new BarcodeReader
                            {
                                AutoRotate = _barcodeReader.AutoRotate,
                                Options = new DecodingOptions
                                {
                                    TryHarder = _barcodeReader.Options.TryHarder,
                                    TryInverted = _barcodeReader.Options.TryInverted,
                                    AssumeGS1 = _barcodeReader.Options.AssumeGS1,
                                    PossibleFormats = new[] { ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX }
                                }
                            };
                            result = twoDReader.Decode(bitmap);
                        }

                        // 2D bulunamadıysa genel okuyucu ile dene
                        result ??= _barcodeReader.Decode(bitmap);

                        // ZXing başarısızsa ve QR açık ise OpenCV QR fallback
                        if ((result == null || string.IsNullOrEmpty(result.Text)) && presetHas2D && _qrFallbackOpenCV)
                        {
                            try
                            {
                                using var qr = new QRCodeDetector();
                                // OpenCvSharp signature: DetectAndDecode(InputArray img, out Point2f[] points, OutputArray? straight_qrcode = null)
                                var decoded = qr.DetectAndDecode(procMat, out _);
                                if (!string.IsNullOrWhiteSpace(decoded))
                                {
                                    result = new ZXing.Result(decoded, null, null, ZXing.BarcodeFormat.QR_CODE);
                                }
                            }
                            catch { }
                        }

                        if (result != null && !string.IsNullOrEmpty(result.Text))
                        {
                            _logger.LogInformation("[CHARLIE] Barcode detected: {Barcode} (Format: {Format})",
                                result.Text, result.BarcodeFormat);

                            // Fire barcode scanned event
                            BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(result.Text));
                            try { GlobalLogger.Instance.LogEvent("BARCODE", $"Detected value={result.Text} format={result.BarcodeFormat}", "BarcodeHW"); } catch { }

                            // Persist to SQL (service-level persistence; UI bağımsız)
                            try
                            {
                                var sp = MesTechStok.Desktop.App.ServiceProvider;
                                if (sp != null)
                                {
                                    using var scope = sp.CreateScope();
                                    var db = scope.ServiceProvider.GetService<AppDbContext>();
                                    if (db != null)
                                    {
                                        db.BarcodeScanLogs.Add(new BarcodeScanLog
                                        {
                                            Barcode = result.Text,
                                            Format = result.BarcodeFormat.ToString(),
                                            Source = "Camera",
                                            DeviceId = $"CAM_{_cameraIndex}",
                                            IsValid = true,
                                            ValidationMessage = null,
                                            RawLength = result.Text.Length,
                                            TimestampUtc = DateTime.UtcNow,
                                            CorrelationId = CorrelationContext.CurrentId
                                        });
                                        db.SaveChanges();
                                        try { GlobalLogger.Instance.LogEvent("DB", $"BarcodeLogged value={result.Text} format={result.BarcodeFormat} corr={CorrelationContext.CurrentId}", "BarcodeHW"); } catch { }
                                    }
                                }
                            }
                            catch (Exception exLog)
                            {
                                try { GlobalLogger.Instance.LogEvent("DB", $"BarcodeLogError {exLog.Message}", "BarcodeHW"); } catch { }
                            }

                            // Brief pause after successful scan to prevent duplicate detections
                            await Task.Delay(300);
                        }
                    }

                    // Small delay to prevent excessive CPU usage
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[CHARLIE] Error during continuous scanning");
                    try { GlobalLogger.Instance.LogError($"Sürekli tarama hatası: {ex.Message}", "BarcodeHW"); } catch { }
                    await Task.Delay(1000); // Longer delay on error
                }
            }

            _logger.LogInformation("[CHARLIE] Continuous barcode scanning loop ended");
            try { GlobalLogger.Instance.LogInfo("Sürekli tarama döngüsü sonlandı", "BarcodeHW"); } catch { }
        }

        private Bitmap MatToBitmap(Mat mat)
        {
            // CHARLIE TEAM: Proper Mat to Bitmap conversion using OpenCvSharp4.Extensions
            try
            {
                if (mat.Empty())
                {
                    throw new ArgumentException("Mat is empty");
                }

                // Convert using OpenCvSharp Extensions
                return BitmapConverter.ToBitmap(mat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CHARLIE] Error converting Mat to Bitmap");

                // Fallback: Create a simple bitmap if conversion fails
                return new Bitmap(640, 480, PixelFormat.Format24bppRgb);
            }
        }

        public void Dispose()
        {
            try
            {
                _logger.LogInformation("[CHARLIE] Disposing BarcodeHardwareService...");

                _ = DisconnectAsync();
                // _barcodeReader?.Dispose(); // ZXing BarcodeReader doesn't implement IDisposable
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CHARLIE] Error disposing BarcodeHardwareService");
                try { GlobalLogger.Instance.LogError($"BarcodeHW dispose hatası: {ex.Message}", "BarcodeHW"); } catch { }
            }
        }

        // ========== YENİ ENHANCED BARCODE METHODLARİ ==========
        public async Task<string?> ScanBarcodeAsync(int timeoutMs = 10000)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Barkod tarayıcı bağlı değil");
            }

            var tcs = new TaskCompletionSource<string>();
            string? scannedCode = null;

            EventHandler<BarcodeScannedEventArgs>? handler = null;
            handler = (sender, args) =>
            {
                scannedCode = args.Barcode;
                BarcodeScanned -= handler;
                tcs.SetResult(args.Barcode);
            };

            BarcodeScanned += handler;

            try
            {
                await StartScanningAsync();
                await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));

                if (tcs.Task.IsCompleted)
                {
                    return await tcs.Task;
                }
                else
                {
                    BarcodeScanned -= handler;
                    throw new TimeoutException($"Barkod tarama {timeoutMs}ms içinde tamamlanamadı");
                }
            }
            finally
            {
                await StopScanningAsync();
            }
        }

        public async Task<BarcodeValidationResult> ValidateBarcodeAsync(string barcode)
        {
            await Task.Delay(50); // Simülasyon gecikmesi

            if (string.IsNullOrWhiteSpace(barcode))
            {
                return new BarcodeValidationResult
                {
                    IsValid = false,
                    Message = "Barkod boş olamaz",
                    Format = ServiceBarcodeFormat.Code128,
                    ConfidenceLevel = 0.0
                };
            }

            // Basit format tespiti
            ServiceBarcodeFormat detectedFormat;
            if (barcode.Length == 13 && barcode.All(char.IsDigit))
            {
                detectedFormat = ServiceBarcodeFormat.EAN13;
            }
            else if (barcode.Length == 12 && barcode.All(char.IsDigit))
            {
                detectedFormat = ServiceBarcodeFormat.UPCA;
            }
            else if (barcode.Length == 8 && barcode.All(char.IsDigit))
            {
                detectedFormat = ServiceBarcodeFormat.EAN8;
            }
            else
            {
                detectedFormat = ServiceBarcodeFormat.Code128;
            }

            return new BarcodeValidationResult
            {
                IsValid = true,
                Format = detectedFormat,
                Message = $"Geçerli {detectedFormat} formatında barkod",
                ConfidenceLevel = 0.95,
                Metadata = new Dictionary<string, object>
                {
                    ["DetectedFormat"] = detectedFormat.ToString(),
                    ["Length"] = barcode.Length,
                    ["ValidationTimestamp"] = DateTime.UtcNow
                }
            };
        }

        /// <summary>
        /// 🚀 YENİLİKÇİ GELİŞTİRME: AI-powered product suggestion from barcode
        /// </summary>
        public async Task<ProductSuggestion?> GetProductSuggestionFromBarcodeAsync(string barcode)
        {
            await Task.Delay(500); // AI işlem simülasyonu (gerçekte harici API çağrısı olabilir)

            // Basit mock AI önerileri - gerçek projede harici ML servis kullanılabilir
            var mockSuggestions = new Dictionary<string, ProductSuggestion>
            {
                ["1234567890123"] = new ProductSuggestion
                {
                    ProductName = "Samsung Galaxy S24 Ultra",
                    Category = "Akıllı Telefon",
                    SuggestedPrice = 45999,
                    ConfidenceScore = 0.96,
                    Description = "512GB Titanium Black, S Pen dahil",
                    AlternativeNames = new[] { "S24 Ultra", "Galaxy S24U" }
                },
                ["2345678901234"] = new ProductSuggestion
                {
                    ProductName = "iPhone 15 Pro Max",
                    Category = "Akıllı Telefon",
                    SuggestedPrice = 49999,
                    ConfidenceScore = 0.94,
                    Description = "256GB Natural Titanium",
                    AlternativeNames = new[] { "iPhone 15 Pro Max", "15 Pro Max" }
                }
            };

            // Eğer bilinen barkod varsa önerisini döndür
            if (mockSuggestions.ContainsKey(barcode))
            {
                return mockSuggestions[barcode];
            }

            // Bilinmeyen barkod için genel öneri
            return new ProductSuggestion
            {
                ProductName = "Bilinmeyen Ürün",
                Category = "Genel",
                SuggestedPrice = 0,
                ConfidenceScore = 0.1,
                Description = "Bu barkod için ürün bilgisi bulunamadı",
                AlternativeNames = new string[0]
            };
        }
    }
}
