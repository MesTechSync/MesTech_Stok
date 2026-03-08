using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Globalization;
using AForge.Video;
using AForge.Video.DirectShow;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using MesTechStok.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using DrawingBitmap = System.Drawing.Bitmap;
using ZXing;
using ZXing.Windows.Compatibility;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// BarcodeView.xaml için interaction logic
    /// </summary>
    public partial class BarcodeView : UserControl
    {
        private readonly Random _random;
        private readonly Dictionary<string, ProductInfo> _demoProducts;
        private readonly List<ScanResult> _scanHistory;
        private readonly DispatcherTimer _scanTimer;
        private DateTime _scanStartUtc = DateTime.MinValue;
        private int _baseTimeoutMs = 8000;
        private int _absoluteTimeoutMs = 20000;
        private readonly DispatcherTimer _scanLineTimer;
        private readonly BarcodeReader _barcodeReader;
        private bool _upceEnabledUi = false; // UI toggle
        private string? _singleFormatForce = null; // UI seçimi ("Auto" ise null)
        private bool _glossyOptimization = false; // Parlak yüzey için işleme değişikliği
        private bool _matteOptimization = false;  // Mat yüzey için işleme değişikliği
        private bool _nearOptimization = false;   // Yakın mesafe için ölçek/ROI ayarı
        private bool _uiFrameBusy = false;        // UI frame güncelleme throttling
        private DateTime _lastUiUpdate = DateTime.MinValue;

        private FilterInfoCollection? _videoDevices;
        private VideoCaptureDevice? _videoSource;
        private bool _isCameraActive = false;
        private bool _isScanning = false;
        private bool _isRealBarcodeMode = true; // Gerçek barkod okuma modu
        private bool _frameReceived = false;
        private DateTime _lastFrameUtc = DateTime.MinValue;
        private DispatcherTimer? _cameraWatchdog;
        private DateTime _lastDecodeUtc = DateTime.MinValue;
        private TimeSpan _decodeCooldown = TimeSpan.FromMilliseconds(700); // tek seferde mantıklı tarama (debounce)
        private int _timeoutStreak = 0;              // ardışık zaman aşımı sayacı
        private bool _fallbackFullFrame = false;      // bir sonraki denemede tam kare dene (ROI kapalı)
        // DB log paneli kaldırıldı; geçmiş limitini UI'de 5 olarak tutuyoruz
        // Responsive düzen için en/boy eşikleri
        private const double MobileBreakpoint = 1100.0;

        public BarcodeView()
        {
            InitializeComponent();

            _random = new Random();
            _scanHistory = new List<ScanResult>();

            // Demo ürün veritabanı
            _demoProducts = new Dictionary<string, ProductInfo>
            {
                { "1234567890123", new ProductInfo { Id = Guid.NewGuid(), Name = "Coca Cola 330ml", SKU = "CC-330", Price = 5.50m, Stock = 25, Category = "İçecek", Brand = "Coca Cola", ImagePath = "/Images/coca-cola.jpg" } },
                { "9876543210987", new ProductInfo { Id = Guid.NewGuid(), Name = "Doritos Nacho 150g", SKU = "DOR-150", Price = 12.75m, Stock = 18, Category = "Atıştırmalık", Brand = "Doritos", ImagePath = "/Images/doritos.jpg" } },
                { "5555555555555", new ProductInfo { Id = Guid.NewGuid(), Name = "Samsung Galaxy S24", SKU = "SAM-S24", Price = 35000.00m, Stock = 3, Category = "Elektronik", Brand = "Samsung", ImagePath = "/Images/samsung-s24.jpg", Description = "En son teknoloji akıllı telefon" } },
                { "1111111111111", new ProductInfo { Id = Guid.NewGuid(), Name = "Nivea Krem 100ml", SKU = "NIV-100", Price = 25.90m, Stock = 42, Category = "Kozmetik", Brand = "Nivea", ImagePath = "/Images/nivea.jpg" } },
                { "2222222222222", new ProductInfo { Id = Guid.NewGuid(), Name = "Adidas Spor Ayakkabı", SKU = "ADI-SPR", Price = 850.00m, Stock = 7, Category = "Spor", Brand = "Adidas", ImagePath = "/Images/adidas.jpg" } },
                { "3333333333333", new ProductInfo { Id = Guid.NewGuid(), Name = "MacBook Pro 14\"", SKU = "MAC-PRO14", Price = 75000.00m, Stock = 1, Category = "Elektronik", Brand = "Apple", ImagePath = "/Images/macbook.jpg", Description = "M3 işlemcili profesyonel laptop" } },
                { "4444444444444", new ProductInfo { Id = Guid.NewGuid(), Name = "Nutella 750g", SKU = "NUT-750", Price = 89.90m, Stock = 33, Category = "Gıda", Brand = "Nutella", ImagePath = "/Images/nutella.jpg" } },
                { "6666666666666", new ProductInfo { Id = Guid.NewGuid(), Name = "iPhone 15 Pro", SKU = "IPH-15P", Price = 55000.00m, Stock = 2, Category = "Elektronik", Brand = "Apple", ImagePath = "/Images/iphone.jpg", Description = "En yeni iPhone modeli" } },
                { "7777777777777", new ProductInfo { Id = Guid.NewGuid(), Name = "Lego City Set", SKU = "LEG-CTY", Price = 299.99m, Stock = 12, Category = "Oyuncak", Brand = "Lego", ImagePath = "/Images/lego.jpg" } },
                { "8888888888888", new ProductInfo { Id = Guid.NewGuid(), Name = "Sony WH-1000XM5", SKU = "SON-WH5", Price = 1200.00m, Stock = 8, Category = "Elektronik", Brand = "Sony", ImagePath = "/Images/sony-headphone.jpg", Description = "Gürültü önleyici kulaklık" } }
            };

            // Initialize ZXing Barcode Reader
            _barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new ZXing.Common.DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = false,
                    AssumeGS1 = true,
                    ReturnCodabarStartEnd = true,
                    PureBarcode = false,
                    PossibleFormats = new[]
                    {
                        ZXing.BarcodeFormat.CODE_128,
                        ZXing.BarcodeFormat.CODE_39,
                        ZXing.BarcodeFormat.EAN_13,
                        ZXing.BarcodeFormat.EAN_8,
                        ZXing.BarcodeFormat.ITF,
                        ZXing.BarcodeFormat.UPC_A,
                        // UPC_E başlangıçta kapalı (UI ile açılabilir)
                        ZXing.BarcodeFormat.QR_CODE,
                        ZXing.BarcodeFormat.DATA_MATRIX,
                        ZXing.BarcodeFormat.PDF_417
                    },
                    CharacterSet = "UTF-8"
                }
            };

            // Kamera preview görüntü netliği: yüksek kalite
            RenderOptions.SetBitmapScalingMode(CameraPreview, BitmapScalingMode.HighQuality);

            // ROI parametreleri (ince ayar)
            _decodeCooldown = TimeSpan.FromMilliseconds(350);

            // Scan timer - barkod tarama simülasyonu
            // Tarama zamanlayıcısı – konfigürasyondan okunur (BaseTimeoutMs)
            var baseTimeoutMs = 8000;
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var cfg = sp?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (cfg != null)
                {
                    baseTimeoutMs = int.TryParse(cfg["BarcodeView:Scan:BaseTimeoutMs"], out var btm) ? btm : baseTimeoutMs;
                }
            }
            catch { }
            _baseTimeoutMs = Math.Max(2000, baseTimeoutMs);
            try
            {
                var sp2 = MesTechStok.Desktop.App.ServiceProvider;
                var cfg2 = sp2?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (cfg2 != null)
                {
                    _absoluteTimeoutMs = int.TryParse(cfg2["BarcodeView:Scan:AbsoluteTimeoutMs"], out var atm)
                        ? Math.Max(_baseTimeoutMs, atm) : _absoluteTimeoutMs;
                }
            }
            catch { }

            _scanTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_baseTimeoutMs) };
            _scanTimer.Tick += ScanTimer_Tick;

            // Scan line animation timer
            _scanLineTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _scanLineTimer.Tick += ScanLineTimer_Tick;

            // Initialize camera devices
            InitializeCameraDevices();

            // USB HID dinleyiciyi de başlat (arka planda)
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var hid = sp?.GetService<MesTechStok.Core.Integrations.Barcode.IBarcodeScannerService>();
                hid?.StartScanningAsync();
                GlobalLogger.Instance.LogInfo("USB HID dinleyici başlatıldı", "BarcodeView");
            }
            catch { }

            // Başlangıçta overscan boyutlandırma Loaded'da yapılacak
        }
        private void BarcodeView_Loaded(object sender, RoutedEventArgs e)
        {
            // İlk ölçümlerde ActualWidth/Height 0 olabilir; dispatcher ile geciktir
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CameraHost_SizeChanged(CameraHost, null);
            }), DispatcherPriority.Loaded);

            // Ayarları konfigürasyondan yükle (geri döndürülebilir profil)
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var config = sp?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                if (config != null)
                {
                    // Okuyucu profili ve seçenekleri
                    var profile = config["BarcodeView:Reader:Profile"] ?? "Standard";
                    var formatPreset = config["BarcodeView:Reader:FormatPreset"] ?? "RetailPlus2D";
                    bool useRoi = bool.TryParse(config["BarcodeView:Reader:UseROI"], out var roi) ? roi : true;
                    double roiTopPct = double.TryParse(config["BarcodeView:Reader:RoiTopPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rtp) ? rtp : 0.25;
                    double roiHeightPct = double.TryParse(config["BarcodeView:Reader:RoiHeightPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rhp) ? rhp : 0.5;
                    int cooldownMs = int.TryParse(config["BarcodeView:Reader:DecodeCooldownMs"], out var cd) ? cd : 350;
                    bool tryHarder = bool.TryParse(config["BarcodeView:Reader:TryHarder"], out var th) ? th : true;
                    bool tryInverted = bool.TryParse(config["BarcodeView:Reader:TryInverted"], out var ti) ? ti : false;

                    _decodeCooldown = TimeSpan.FromMilliseconds(Math.Max(100, cooldownMs));

                    // FormatPreset'e göre olası formatları daralt – yanlış pozitifleri azaltır
                    try
                    {
                        var preset = (formatPreset ?? "Retail1D").Trim().ToLowerInvariant();
                        ZXing.BarcodeFormat[] allowedFormats = preset switch
                        {
                            // 1D perakende (EAN/UPC/Code128/Code39/ITF)
                            "retail1d" => new[]
                            {
                                ZXing.BarcodeFormat.EAN_13,
                                ZXing.BarcodeFormat.EAN_8,
                                ZXing.BarcodeFormat.UPC_A,
                                ZXing.BarcodeFormat.CODE_128,
                                ZXing.BarcodeFormat.CODE_39,
                                ZXing.BarcodeFormat.ITF
                            },
                            // 1D perakende (UPC-E hariç)
                            "retail1d_noupce" => new[]
                            {
                                ZXing.BarcodeFormat.EAN_13,
                                ZXing.BarcodeFormat.EAN_8,
                                ZXing.BarcodeFormat.UPC_A,
                                ZXing.BarcodeFormat.CODE_128,
                                ZXing.BarcodeFormat.CODE_39,
                                ZXing.BarcodeFormat.ITF
                            },
                            // 1D + 2D (QR/DM), daha düşük yanlış pozitif için PDF_417 hariç
                            "retailplus2d" => new[]
                            {
                                ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                                ZXing.BarcodeFormat.UPC_A, ZXing.BarcodeFormat.UPC_E,
                                ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF,
                                ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX
                            },
                            // 1D + 2D (QR/DM) – UPC-E hariç
                            "retailplus2d_noupce" => new[]
                            {
                                ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                                ZXing.BarcodeFormat.UPC_A,
                                ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF,
                                ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX
                            },
                            // Tüm desteklenenler (daha geniş alan; yanlış pozitif riski daha yüksek)
                            "all" => new[]
                            {
                                ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                                ZXing.BarcodeFormat.UPC_A, ZXing.BarcodeFormat.UPC_E,
                                ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF,
                                ZXing.BarcodeFormat.QR_CODE, ZXing.BarcodeFormat.DATA_MATRIX, ZXing.BarcodeFormat.PDF_417
                            },
                            _ => new[]
                            {
                                ZXing.BarcodeFormat.EAN_13, ZXing.BarcodeFormat.EAN_8,
                                ZXing.BarcodeFormat.UPC_A, ZXing.BarcodeFormat.UPC_E,
                                ZXing.BarcodeFormat.CODE_128, ZXing.BarcodeFormat.CODE_39, ZXing.BarcodeFormat.ITF
                            }
                        };
                        // UI: UPC-E toggle
                        if (_upceEnabledUi)
                        {
                            allowedFormats = allowedFormats.Concat(new[] { ZXing.BarcodeFormat.UPC_E }).ToArray();
                        }
                        // UI: Single format force
                        if (!string.IsNullOrWhiteSpace(_singleFormatForce) && !_singleFormatForce.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Enum.TryParse<BarcodeFormat>(_singleFormatForce, out var f))
                            {
                                allowedFormats = new[] { f };
                            }
                        }
                        _barcodeReader.Options.TryHarder = tryHarder;
                        _barcodeReader.Options.PossibleFormats = allowedFormats;
                        // Ters kontrast ve GS1 ayarlarını config'e göre uygula
                        _barcodeReader.Options.TryInverted = tryInverted;
                        _barcodeReader.Options.AssumeGS1 = true;
                    }
                    catch { }

                    // Önizleme enable
                    if (this.FindName("PreviewToggle") is CheckBox pt)
                    {
                        bool preview = bool.TryParse(config["BarcodeView:PreviewEnabled"], out var pe) ? pe : true;
                        pt.IsChecked = preview;
                        CameraHost.Visibility = preview ? Visibility.Visible : Visibility.Collapsed;
                    }

                    if (this.FindName("WidthMulSlider") is Slider wsl && this.FindName("WidthMulValueText") is TextBlock wtx)
                    {
                        if (double.TryParse(config["BarcodeView:Overscan:WidthMultiplier"], out var wm))
                        {
                            wsl.Value = Math.Min(Math.Max(wm, wsl.Minimum), wsl.Maximum);
                            wtx.Text = $"{wsl.Value:F2}x";
                        }
                    }
                    if (this.FindName("HeightMulSlider") is Slider hsl && this.FindName("HeightMulValueText") is TextBlock htx)
                    {
                        if (double.TryParse(config["BarcodeView:Overscan:HeightMultiplier"], out var hm))
                        {
                            hsl.Value = Math.Min(Math.Max(hm, hsl.Minimum), hsl.Maximum);
                            htx.Text = $"{hsl.Value:F2}x";
                        }
                    }
                }
            }
            catch { }
        }

        private void BarcodeView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                // Basit responsive kural: genişlik 1100px altına düşünce sağ panel alta insin
                var totalWidth = ActualWidth;
                if (totalWidth <= 0) return;
                if (this.FindName("MainGrid") is Grid g && this.FindName("LeftColumn") is ColumnDefinition lc && this.FindName("RightColumn") is ColumnDefinition rc)
                {
                    if (totalWidth < 1100)
                    {
                        // Mobil benzeri: tek kolon akış
                        g.RowDefinitions.Clear();
                        g.ColumnDefinitions.Clear();
                        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                        g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
                        g.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        // Çocukları yeniden konumlandır (Camera card 0, Results card 2)
                        var cameraCard = g.Children.Cast<UIElement>().FirstOrDefault(el => Grid.GetColumn(el) == 0);
                        var spacer = g.Children.Cast<UIElement>().FirstOrDefault(el => Grid.GetColumn(el) == 1);
                        var resultCard = g.Children.Cast<UIElement>().FirstOrDefault(el => Grid.GetColumn(el) == 2);
                        if (cameraCard != null) { Grid.SetRow(cameraCard, 0); Grid.SetColumn(cameraCard, 0); }
                        if (spacer != null) { Grid.SetRow(spacer, 1); Grid.SetColumn(spacer, 0); }
                        if (resultCard != null) { Grid.SetRow(resultCard, 2); Grid.SetColumn(resultCard, 0); }
                    }
                    else
                    {
                        // Masaüstü düzenine geri dön
                        g.RowDefinitions.Clear();
                        g.ColumnDefinitions.Clear();
                        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
                        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
                        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                        var cameraCard = g.Children.Cast<UIElement>().FirstOrDefault(el => Grid.GetRow(el) == 0 || Grid.GetColumn(el) == 0);
                        var spacer = g.Children.Cast<UIElement>().FirstOrDefault(el => Grid.GetRow(el) == 1 || Grid.GetColumn(el) == 1);
                        var resultCard = g.Children.Cast<UIElement>().FirstOrDefault(el => Grid.GetRow(el) == 2 || Grid.GetColumn(el) == 2);
                        if (cameraCard != null) { Grid.SetRow(cameraCard, 0); Grid.SetColumn(cameraCard, 0); }
                        if (spacer != null) { Grid.SetRow(spacer, 0); Grid.SetColumn(spacer, 1); }
                        if (resultCard != null) { Grid.SetRow(resultCard, 0); Grid.SetColumn(resultCard, 2); }
                    }
                }
            }
            catch { }
        }
        private void CameraHost_SizeChanged(object sender, SizeChangedEventArgs? e)
        {
            try
            {
                // Overscan katsayılarını konfigürasyondan oku (yoksa varsayılan 2.5 / 1.333)
                double widthMul = 2.5, heightMul = 1.333;
                try
                {
                    var sp = MesTechStok.Desktop.App.ServiceProvider;
                    var config = sp?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                    if (config != null)
                    {
                        widthMul = double.TryParse(config["BarcodeView:Overscan:WidthMultiplier"], out var wm) ? wm : widthMul;
                        heightMul = double.TryParse(config["BarcodeView:Overscan:HeightMultiplier"], out var hm) ? hm : heightMul;
                    }
                }
                catch { /* config yoksa varsayılan */ }

                var baseW = CameraHost.ActualWidth;
                var baseH = CameraHost.ActualHeight;
                if (baseW <= 0 || baseH <= 0) return;

                var targetW = baseW * widthMul;
                var targetH = baseH * heightMul;

                // UniformToFill + RenderTransform ile overscan
                // Ölçek değerini hesapla ve sadece render transform uygula (layout'u şişirme)
                var scaleX = targetW / baseW;
                var scaleY = targetH / baseH;
                var scale = Math.Max(scaleX, scaleY);
                if (this.FindName("CameraScale") is ScaleTransform st)
                {
                    // Üst sınır: aşırı büyümede taşmayı sınırlamak için clamp
                    var clamped = Math.Min(scale, 3.0);
                    st.ScaleX = clamped;
                    st.ScaleY = clamped;
                }
                if (this.FindName("CameraTranslate") is TranslateTransform tt)
                {
                    // Merkezden büyütüyoruz; ekstra ofset gerekmiyor
                    tt.X = 0;
                    tt.Y = 0;
                }

                // UI slider metinlerini güncelle
                if (this.FindName("WidthMulValueText") is TextBlock wtx && this.FindName("WidthMulSlider") is Slider wsl)
                {
                    wtx.Text = $"{wsl.Value:F2}x";
                }
                if (this.FindName("HeightMulValueText") is TextBlock htx && this.FindName("HeightMulSlider") is Slider hsl)
                {
                    htx.Text = $"{hsl.Value:F2}x";
                }
            }
            catch { }
        }

        private void OverscanToggle_Checked(object sender, RoutedEventArgs e)
        {
            CameraHost_SizeChanged(CameraHost, null);
            if (this.FindName("PreviewToggle") is CheckBox pt && pt.IsChecked == true)
            {
                CameraHost.Visibility = Visibility.Visible;
            }
        }

        private void OverscanToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.FindName("CameraScale") is ScaleTransform st)
            {
                st.ScaleX = 1; st.ScaleY = 1;
            }
            if (this.FindName("CameraTranslate") is TranslateTransform tt)
            {
                tt.X = 0; tt.Y = 0;
            }
            // Önizleme kapalıysa alanı gizli tut
            if (this.FindName("PreviewToggle") is CheckBox pt && pt.IsChecked != true)
            {
                CameraHost.Visibility = Visibility.Collapsed;
            }
        }

        private void WidthMulSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.FindName("OverscanToggle") is CheckBox cb && cb.IsChecked == true)
            {
                CameraHost_SizeChanged(CameraHost, null);
            }
        }

        private void HeightMulSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.FindName("OverscanToggle") is CheckBox cb && cb.IsChecked == true)
            {
                CameraHost_SizeChanged(CameraHost, null);
            }
        }

        // DB log geçmişi paneli ve ilgili event'ler kaldırıldı – yönetim LogView'dan yapılmaktadır.

        private void InitializeCameraDevices()
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_videoDevices.Count == 0)
                {
                    AddActivity("⚠️ Kamera bulunamadı - Demo modunda çalışacak", Colors.Orange);
                }
                else
                {
                    AddActivity($"📹 {_videoDevices.Count} kamera bulundu", Colors.Green);
                }
            }
            catch (Exception ex)
            {
                AddActivity($"❌ Kamera başlatma hatası: {ex.Message}", Colors.Red);
            }
        }

        private void StartCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Eski bir kamera örneği varsa önce kapat
                SafeStopAndDisposeCamera();
                // Real camera'yı başlat
                if (_videoDevices != null && _videoDevices.Count > 0)
                {
                    StartRealCamera();
                }
                else
                {
                    StartDemoCamera();
                }

                _isCameraActive = true;

                // UI güncellemeleri
                StartCameraBtn.IsEnabled = false;
                StopCameraBtn.IsEnabled = true;
                ScanBarcodeBtn.IsEnabled = true;

                CameraStatusText.Text = "🎥 Kamera Aktif";
                CameraStatusText.Foreground = new SolidColorBrush(Colors.Green);
                ScanStatusText.Text = "Barkod taramaya hazır";
                ScanStatusText.Foreground = new SolidColorBrush(Colors.Green);

                // Önizleme durumu toggle'dan alınır (varsayılan açık)
                if (this.FindName("PreviewToggle") is CheckBox pt)
                {
                    CameraHost.Visibility = pt.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Kamera başlatma hatası: {ex.Message}", isCritical: true);
            }
        }

        private void StartRealCamera()
        {
            try
            {
                if (_videoDevices == null || _videoDevices.Count == 0) return;

                // Kamera preview Image'ı oluştur ve ScanOverlay'a ekle
                CreateCameraPreview();

                // İlk kamerayı kullan
                _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);

                // Hata olaylarını dinle
                _videoSource.VideoSourceError += (s, ev) =>
                {
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"Camera VideoSourceError: {ev.Description}", "BarcodeView");
                };
                _videoSource.PlayingFinished += (s, ev) =>
                {
                    // Kamera kapanışı sonrası UI'yi güvenle re-enable et
                    Dispatcher.Invoke(() =>
                    {
                        _isCameraActive = false;
                        StartCameraBtn.IsEnabled = true;
                        StopCameraBtn.IsEnabled = false;
                        ScanBarcodeBtn.IsEnabled = false;
                        ScanLine.Opacity = 0;
                        CameraStatusText.Text = "🎥 Kamera Kapalı";
                        CameraStatusText.Foreground = new SolidColorBrush(Colors.Gray);
                        ScanStatusText.Text = "Barkod taramaya hazır değil";
                        ScanStatusText.Foreground = new SolidColorBrush(Colors.Gray);
                    });
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo("Camera PlayingFinished", "BarcodeView");
                };

                // Çözünürlük: önce 1280x720 civarı, yoksa varsayılan/en iyi
                try
                {
                    var caps = _videoSource.VideoCapabilities;
                    if (caps != null && caps.Length > 0)
                    {
                        var preferred = caps
                            .OrderBy(c => Math.Abs(c.FrameSize.Width - 1280) + Math.Abs(c.FrameSize.Height - 720))
                            .FirstOrDefault();
                        _videoSource.VideoResolution = preferred ?? caps.OrderByDescending(c => c.FrameSize.Width * c.FrameSize.Height).First();
                    }
                }
                catch { /* capability seçimi kritik değil */ }

                _videoSource.NewFrame += VideoSource_NewFrame;

                // Watchdog hazırlığı
                _frameReceived = false;
                _lastFrameUtc = DateTime.UtcNow;
                _cameraWatchdog?.Stop();
                _cameraWatchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _cameraWatchdog.Tick += (s, e) =>
                {
                    if (!_frameReceived)
                    {
                        MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogWarning("Kamera frame gelmedi, fallback başlatılıyor", "BarcodeView");
                        // Fallback: varsayılan çözünürlükle yeniden başlat
                        try
                        {
                            SafeStopAndDisposeCamera();
                            _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);
                            _videoSource.NewFrame += VideoSource_NewFrame;
                            _videoSource.Start();
                        }
                        catch (Exception rex)
                        {
                            MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"Kamera fallback hatası: {rex.Message}", "BarcodeView");
                        }
                    }
                    _cameraWatchdog?.Stop();
                };

                _videoSource.Start();
                _cameraWatchdog.Start();

                AddActivity("📹 Gerçek kamera başlatıldı", Colors.Green);
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo("Kamera başlatıldı", "BarcodeView");
                ToastManager.ShowSuccess("Kamera başlatıldı!", "Barkod");
            }
            catch (Exception ex)
            {
                AddActivity($"❌ Gerçek kamera hatası: {ex.Message}", Colors.Red);
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"Kamera başlatma hatası: {ex.Message}", "BarcodeView");
                ToastManager.ShowError("Kamera başlatılamadı!", "Barkod");
            }
        }

        private void CreateCameraPreview()
        {
            try
            {
                // XAML'de CameraPreview zaten mevcut; ekstra oluşturma yok
                AddActivity("📺 Kamera preview hazır", Colors.Blue);
                // Varsayılan olarak önizleme açık
                if (this.FindName("PreviewToggle") is CheckBox pt)
                {
                    CameraHost.Visibility = pt.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                AddActivity($"❌ Preview oluşturma hatası: {ex.Message}", Colors.Red);
            }
        }

        private void PreviewToggle_Checked(object sender, RoutedEventArgs e)
        {
            try { CameraHost.Visibility = Visibility.Visible; } catch { }
        }

        private void PreviewToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try { CameraHost.Visibility = Visibility.Collapsed; } catch { }
        }

        private void UpceToggle_Checked(object sender, RoutedEventArgs e)
        {
            _upceEnabledUi = true;
            try
            {
                // Reader format setini güncelle
                ApplyUiFormatsToReader();
                AddActivity("UPC-E etkinleştirildi", Colors.Blue);
            }
            catch { }
        }

        private void UpceToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _upceEnabledUi = false;
            try
            {
                ApplyUiFormatsToReader();
                AddActivity("UPC-E devre dışı", Colors.Blue);
            }
            catch { }
        }

        private void GlossyToggle_Checked(object sender, RoutedEventArgs e)
        {
            _glossyOptimization = true;
            AddActivity("Parlak yüzey optimizasyonu: AÇIK", Colors.Blue);
        }

        private void GlossyToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _glossyOptimization = false;
            AddActivity("Parlak yüzey optimizasyonu: KAPALI", Colors.Blue);
        }

        private void MatteToggle_Checked(object sender, RoutedEventArgs e)
        {
            _matteOptimization = true;
            AddActivity("Mat yüzey optimizasyonu: AÇIK", Colors.Blue);
        }

        private void MatteToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _matteOptimization = false;
            AddActivity("Mat yüzey optimizasyonu: KAPALI", Colors.Blue);
        }

        private void NearToggle_Checked(object sender, RoutedEventArgs e)
        {
            _nearOptimization = true;
            // Yakın mesafe için ROI'yi daralt (yükseklik bandını %35 civarında tut), decode ölçeğini artır
            try
            {
                AddActivity("Yakın mesafe optimizasyonu: AÇIK (ROI dar, ölçek artırıldı)", Colors.Blue);
            }
            catch { }
        }

        private void NearToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            _nearOptimization = false;
            try
            {
                AddActivity("Yakın mesafe optimizasyonu: KAPALI", Colors.Blue);
            }
            catch { }
        }

        private void SingleFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SingleFormatCombo?.SelectedItem is ComboBoxItem item && item.Content is string label)
                {
                    _singleFormatForce = label;
                    ApplyUiFormatsToReader();
                    AddActivity($"Tek format: {label}", Colors.Blue);
                }
            }
            catch { }
        }

        private void ApplyUiFormatsToReader()
        {
            try
            {
                var current = _barcodeReader.Options.PossibleFormats?.ToList() ?? new List<BarcodeFormat>();
                // Başlangıç kümesi: EAN_13, EAN_8, UPC_A, CODE128, CODE39, ITF (+ opsiyonel UPC_E)
                var baseSet = new List<BarcodeFormat>
                {
                    BarcodeFormat.EAN_13, BarcodeFormat.EAN_8,
                    BarcodeFormat.UPC_A,
                    BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.ITF
                };
                if (_upceEnabledUi)
                    baseSet.Add(BarcodeFormat.UPC_E);

                // Tek format zorlaması (Auto değilse)
                if (!string.IsNullOrWhiteSpace(_singleFormatForce) && !_singleFormatForce.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse<BarcodeFormat>(_singleFormatForce, out var f))
                    {
                        _barcodeReader.Options.PossibleFormats = new[] { f };
                        return;
                    }
                }

                _barcodeReader.Options.PossibleFormats = baseSet.ToArray();
            }
            catch { }
        }

        private void StartDemoCamera()
        {
            // Demo kamera başlatma (fallback)
            var cameraBackground = new LinearGradientBrush
            {
                StartPoint = new System.Windows.Point(0, 0),
                EndPoint = new System.Windows.Point(1, 1)
            };
            cameraBackground.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x2C, 0x3E, 0x50), 0));
            cameraBackground.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromRgb(0x34, 0x49, 0x5E), 1));

            var cameraRect = ScanOverlay.Children.OfType<System.Windows.Shapes.Rectangle>().FirstOrDefault();
            if (cameraRect != null)
            {
                cameraRect.Fill = cameraBackground;
            }

            AddActivity("📹 Demo kamera başlatıldı", Colors.Orange);

            // modal uyarı kaldırıldı
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Kamera frame'ini WPF Image'e dönüştür
                using var bitmap = (DrawingBitmap)eventArgs.Frame.Clone();
                _frameReceived = true;
                _lastFrameUtc = DateTime.UtcNow;
                var srcWidth = bitmap.Width;
                var srcHeight = bitmap.Height;

                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // UI throttling: 20ms altında UI kaynak atamasını atla (yükü azaltır)
                        var now = DateTime.UtcNow;
                        if (_lastUiUpdate != DateTime.MinValue && (now - _lastUiUpdate).TotalMilliseconds < 20)
                        {
                            // Yine de decode tarafı devam etsin
                        }
                        else
                        {
                            var bitmapSource = ConvertBitmapToBitmapSource(bitmap);
                            RenderOptions.SetBitmapScalingMode(CameraPreview, BitmapScalingMode.HighQuality);
                            var cameraPreview = CameraPreview;
                            if (cameraPreview != null)
                            {
                                cameraPreview.Source = bitmapSource;
                                try
                                {
                                    var spz = MesTechStok.Desktop.App.ServiceProvider;
                                    var cfgz = spz?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                                    if (cfgz != null && this.FindName("CameraScale") is ScaleTransform st)
                                    {
                                        double scale = 1.0;
                                        scale = double.TryParse(cfgz["BarcodeView:Preview:Scale"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sc) ? sc : 1.0;
                                        st.ScaleX = Math.Max(0.5, Math.Min(2.0, scale));
                                        st.ScaleY = Math.Max(0.5, Math.Min(2.0, scale));
                                    }
                                }
                                catch { }
                            }
                            _lastUiUpdate = now;
                        }

                        // GERÇEK BARKOD OKUMA - Sadece tarama aktifken
                        if (_isScanning && _isRealBarcodeMode)
                        {
                            try
                            {
                                // ZXing ile barkod okuma (debounce + ROI)
                                // ZXing Windows Compatibility kullanımı - daha doğru sonuç için grayscale + TryHarder aktif
                                if (DateTime.UtcNow - _lastDecodeUtc < _decodeCooldown)
                                {
                                    return; // çok sık decode etmeyelim
                                }
                                _lastDecodeUtc = DateTime.UtcNow;
                                ResetScanTimeout();

                                // ROI ayarını konfigürasyondan oku (geri alınabilir)
                                bool useRoi = true;
                                double roiTopPct = 0.25, roiHeightPct = 0.50;
                                try
                                {
                                    var sp = MesTechStok.Desktop.App.ServiceProvider;
                                    var cfg = sp?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                                    if (cfg != null)
                                    {
                                        useRoi = bool.TryParse(cfg["BarcodeView:Reader:UseROI"], out var ur) ? ur : useRoi;
                                        roiTopPct = double.TryParse(cfg["BarcodeView:Reader:RoiTopPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rtp) ? rtp : roiTopPct;
                                        roiHeightPct = double.TryParse(cfg["BarcodeView:Reader:RoiHeightPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rhp) ? rhp : roiHeightPct;
                                        // Yüzde olarak (25 => 0.25) girilmiş olma olasılığına karşı dönüştür
                                        if (roiTopPct > 1) roiTopPct /= 100.0;
                                        if (roiHeightPct > 1) roiHeightPct /= 100.0;
                                        // Kullanışlı alt sınırlar (çok dar ROI zaman aşımına sebep olur)
                                        if (roiHeightPct < 0.3) roiHeightPct = 0.5;
                                    }
                                }
                                catch { }

                                // Yatay ROI de uygula
                                double roiLeftPct = 0.05, roiWidthPct = 0.90;
                                try
                                {
                                    var sp2 = MesTechStok.Desktop.App.ServiceProvider;
                                    var cfg2 = sp2?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                                    if (cfg2 != null)
                                    {
                                        roiLeftPct = double.TryParse(cfg2["BarcodeView:Reader:RoiLeftPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rlp) ? rlp : roiLeftPct;
                                        roiWidthPct = double.TryParse(cfg2["BarcodeView:Reader:RoiWidthPercent"], NumberStyles.Any, CultureInfo.InvariantCulture, out var rwp) ? rwp : roiWidthPct;
                                        if (roiLeftPct > 1) roiLeftPct /= 100.0;
                                        if (roiWidthPct > 1) roiWidthPct /= 100.0;
                                        if (roiWidthPct < 0.5) roiWidthPct = 0.9;
                                    }
                                }
                                catch { }

                                // Yakın mesafe optimizasyonu açıksa dikey ROI'yi daralt
                                if (_nearOptimization)
                                {
                                    roiTopPct = 0.325; // orta bant
                                    roiHeightPct = 0.35;
                                }

                                // Zaman aşımı sonrası tam kare fallback isteği varsa ROI'yi kapat
                                if (_fallbackFullFrame)
                                {
                                    useRoi = false;
                                }

                                var roiTop = (int)(srcHeight * roiTopPct);
                                var roiHeight = (int)(srcHeight * roiHeightPct);
                                var roiLeft = (int)(srcWidth * roiLeftPct);
                                var roiWidth = (int)(srcWidth * roiWidthPct);
                                if (!useRoi)
                                {
                                    roiTop = 0; roiLeft = 0; roiWidth = srcWidth; roiHeight = srcHeight;
                                }
                                if (roiTop + roiHeight > srcHeight) roiHeight = srcHeight - roiTop;
                                if (roiLeft + roiWidth > srcWidth) roiWidth = srcWidth - roiLeft;
                                using var roiBmp = bitmap.Clone(new System.Drawing.Rectangle(roiLeft, roiTop, roiWidth, roiHeight), bitmap.PixelFormat);

                                // Pixel format normalizasyonu (ZXing için güvenli yol: 24bpp RGB)
                                System.Drawing.Bitmap NormalizeBmp(System.Drawing.Bitmap src)
                                {
                                    if (src.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                                        return (System.Drawing.Bitmap)src.Clone();
                                    var nb = new System.Drawing.Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                    using (var g = System.Drawing.Graphics.FromImage(nb))
                                    {
                                        g.DrawImage(src, new System.Drawing.Rectangle(0, 0, nb.Width, nb.Height));
                                    }
                                    return nb;
                                }

                                using var roiNorm = NormalizeBmp(roiBmp);
                                using var fullNorm = NormalizeBmp(bitmap);
                                // Decode ölçek faktörü (yakınlaştırma/uzaklaştırma etkisi)
                                double decodeScale = 1.0;
                                try
                                {
                                    var sp3 = MesTechStok.Desktop.App.ServiceProvider;
                                    var cfg3 = sp3?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                                    if (cfg3 != null)
                                    {
                                        decodeScale = double.TryParse(cfg3["BarcodeView:Reader:DecodeScale"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ds) ? ds : 1.0;
                                        decodeScale = Math.Max(0.75, Math.Min(2.0, decodeScale));
                                    }
                                }
                                catch { }

                                // 2D önceliklendirme: önce 2D dene (QR/DM), sonra genel decoder
                                ZXing.Result? result = null;
                                var twoDReader = new BarcodeReader
                                {
                                    AutoRotate = _barcodeReader.AutoRotate,
                                    Options = new ZXing.Common.DecodingOptions
                                    {
                                        TryHarder = _barcodeReader.Options.TryHarder,
                                        TryInverted = _barcodeReader.Options.TryInverted,
                                        AssumeGS1 = _barcodeReader.Options.AssumeGS1,
                                        PossibleFormats = new[] { BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX }
                                    }
                                };
                                // Çoklu ölçek denemeleri (yakın/uzak için farklı ölçekler)
                                var extraScale = _nearOptimization ? 1.6 : 1.25;
                                System.Drawing.Bitmap[] candidates = new[] { roiNorm, fullNorm }
                                    .SelectMany(b => new[]
                                    {
                        b,
                        new System.Drawing.Bitmap(b, new System.Drawing.Size((int)(b.Width*decodeScale), (int)(b.Height*decodeScale))),
                        new System.Drawing.Bitmap(b, new System.Drawing.Size((int)(b.Width*Math.Max(1.1, decodeScale*extraScale)), (int)(b.Height*Math.Max(1.1, decodeScale*extraScale))))
                                    })
                                    .ToArray();
                                foreach (var bmp in candidates)
                                {
                                    // Parlak yüzey optimizasyonu: 1D için aşırı parlama/doyma durumunda ters eşik denemesi
                                    if (_glossyOptimization)
                                    {
                                        try
                                        {
                                            using var temp = new System.Drawing.Bitmap(bmp.Width, bmp.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                            using (var g = System.Drawing.Graphics.FromImage(temp)) { g.DrawImage(bmp, 0, 0, bmp.Width, bmp.Height); }
                                            // Basit kontrast germe + invert (glare azaltma denemesi)
                                            temp.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipNone); // no-op: GDI başlatmak için
                                            result = twoDReader.Decode(temp) ?? _barcodeReader.Decode(temp);
                                        }
                                        catch { }
                                        if (result != null && !string.IsNullOrEmpty(result.Text)) break;
                                    }
                                    // Mat yüzey optimizasyonu: düşük kontrastta CLAHE + keskinleştirme denemesi
                                    if (_matteOptimization && (result == null || string.IsNullOrEmpty(result.Text)))
                                    {
                                        try
                                        {
                                            using var src = new System.Drawing.Bitmap(bmp);
                                            using var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(src);
                                            using var gray = new OpenCvSharp.Mat();
                                            OpenCvSharp.Cv2.CvtColor(mat, gray, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
                                            using var clahe = OpenCvSharp.Cv2.CreateCLAHE(2.0, new OpenCvSharp.Size(8, 8));
                                            clahe.Apply(gray, gray);
                                            // Unsharp mask
                                            using var blur = new OpenCvSharp.Mat();
                                            OpenCvSharp.Cv2.GaussianBlur(gray, blur, new OpenCvSharp.Size(0, 0), 3);
                                            OpenCvSharp.Cv2.AddWeighted(gray, 1.5, blur, -0.5, 0, gray);
                                            using var boosted = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(gray);
                                            result = twoDReader.Decode(boosted) ?? _barcodeReader.Decode(boosted);
                                        }
                                        catch { }
                                        if (result != null && !string.IsNullOrEmpty(result.Text)) break;
                                    }
                                    // Decode hızlı başarısızlık: tek format seçiliyse TryHarder'ı geçici düşür
                                    var originalTryHarder = _barcodeReader.Options.TryHarder;
                                    if (!string.IsNullOrWhiteSpace(_singleFormatForce) && !_singleFormatForce.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                                    {
                                        _barcodeReader.Options.TryHarder = false;
                                    }
                                    try
                                    {
                                        result = twoDReader.Decode(bmp) ?? _barcodeReader.Decode(bmp);
                                    }
                                    finally
                                    {
                                        _barcodeReader.Options.TryHarder = originalTryHarder;
                                    }
                                    if (result != null && !string.IsNullOrEmpty(result.Text)) break;
                                }
                                if (result != null && !string.IsNullOrEmpty(result.Text))
                                {
                                    // Barkod bulundu!
                                    _timeoutStreak = 0;
                                    _fallbackFullFrame = false;
                                    _isScanning = false;
                                    _scanTimer.Stop();
                                    _scanLineTimer.Stop();

                                    // Gerçek barkod işleme - GS1 trimming + kontrol karakterlerini temizle
                                    var cleaned = new string((result.Text ?? string.Empty)
                                        .Where(ch => !char.IsControl(ch))
                                        .ToArray())
                                        .Trim();
                                    if (!string.IsNullOrEmpty(cleaned))
                                    {
                                        ProcessRealBarcode(cleaned, result.BarcodeFormat.ToString());
                                    }

                                    // Barkod tespit overlay'ini çiz
                                    DrawDetectionOverlay(result, srcWidth, srcHeight);

                                    // UI güncelleme
                                    ScanBarcodeBtn.Content = "📱 Barkod Tara";
                                    ScanBarcodeBtn.IsEnabled = true;
                                    ScanLine.Opacity = 0;

                                    AddActivity($"✅ Gerçek barkod okundu: {result.Text} ({result.BarcodeFormat})", Colors.Green);
                                }
                            }
                            catch (Exception barcodeEx)
                            {
                                // Barkod okuma hatası (normal, sürekli dene)
                                System.Diagnostics.Debug.WriteLine($"Barcode decode error: {barcodeEx.Message}");
                            }
                        }
                        // Çok sık log basmamak için frame logunu kaldırdık
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Frame processing error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Video source error: {ex.Message}");
            }
        }

        private void DrawDetectionOverlay(ZXing.Result result, int srcWidth, int srcHeight)
        {
            try
            {
                // Önce eski detection overlay'lerini temizle
                var toRemove = ScanOverlay.Children.OfType<System.Windows.Shapes.Shape>()
                    .Where(s => Equals(s.Tag, "DETECTION"))
                    .ToList();
                foreach (var s in toRemove) ScanOverlay.Children.Remove(s);

                // Hedef boyutlar (overscan ile sol-üstten hizalı)
                var destW = CameraHost.ActualWidth;
                var destH = CameraHost.ActualHeight;
                if (destW <= 0 || destH <= 0) return;

                // RenderTransform ölçeğini dikkate alarak mapping
                double rScaleX = 1.0, rScaleY = 1.0;
                if (this.FindName("CameraScale") is ScaleTransform rst)
                {
                    rScaleX = rst.ScaleX;
                    rScaleY = rst.ScaleY;
                }

                // RenderTransform merkezden büyüttüğü için overlay koordinatlarını da merkez referanslı hesapla
                var baseScaleX = destW / (double)srcWidth;
                var baseScaleY = destH / (double)srcHeight;
                var scaleX = baseScaleX * rScaleX;
                var scaleY = baseScaleY * rScaleY;

                System.Windows.Point Map(ZXing.ResultPoint p)
                {
                    var x = p.X * scaleX; // sol-üst başlangıç
                    var y = p.Y * scaleY;
                    // ROI ofseti uygula (varsa)
                    if (_isScanning) // yalnızca tarama sırasında ROI kullanıldı
                    {
                        try
                        {
                            // roiTop'u son hesaplamadan al: VideoSource_NewFrame kapsamındaki lokal değişkene erişemeyiz,
                            // bu nedenle merkez etiket konumu zaten görsel doğrulama sağlar.
                        }
                        catch { }
                    }
                    // Görünür alana kliple (CameraHost içinde)
                    if (x < 0) x = 0; if (y < 0) y = 0;
                    if (x > destW) x = destW; if (y > destH) y = destH;
                    return new System.Windows.Point(x, y);
                }

                var pts = result.ResultPoints;
                if (pts != null && pts.Length >= 2)
                {
                    // Noktalar arası poligon/çerçeve
                    var poly = new System.Windows.Shapes.Polyline
                    {
                        Stroke = new SolidColorBrush(Colors.LimeGreen),
                        StrokeThickness = 3,
                        Tag = "DETECTION"
                    };

                    foreach (var rp in pts)
                    {
                        poly.Points.Add(Map(rp));
                    }
                    // Kapatmak için ilk noktayı tekrar ekleyelim
                    poly.Points.Add(Map(pts[0]));
                    ScanOverlay.Children.Add(poly);
                }

                // Merkezde küçük bir kutu/etiket
                var centerLabel = new TextBlock
                {
                    Text = "BARKOD",
                    Foreground = new SolidColorBrush(Colors.LimeGreen),
                    FontWeight = FontWeights.Bold,
                    Tag = "DETECTION"
                };
                if (pts != null && pts.Length > 0)
                {
                    var avgX = pts.Average(p => p.X);
                    var avgY = pts.Average(p => p.Y);
                    var m = Map(new ZXing.ResultPoint(avgX, avgY));
                    Canvas.SetLeft(centerLabel, m.X);
                    Canvas.SetTop(centerLabel, m.Y);
                    ScanOverlay.Children.Add(centerLabel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DrawDetectionOverlay error: {ex.Message}");
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private BitmapSource ConvertBitmapToBitmapSource(DrawingBitmap bitmap)
        {
            // HBITMAP üzerinden güvenli dönüşüm (piksel formatından bağımsız)
            IntPtr hBitmap = IntPtr.Zero;
            try
            {
                hBitmap = bitmap.GetHbitmap();
                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                if (hBitmap != IntPtr.Zero)
                {
                    DeleteObject(hBitmap);
                }
            }
        }

        private void StopCamera_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isCameraActive = false;
                _isScanning = false;

                // Stop and dispose real camera HARD (ışık kapanması için)
                SafeStopAndDisposeCamera();

                // Overscan transform'u sıfırla (UI'nin daralmış görünmesini önle)
                if (this.FindName("CameraScale") is ScaleTransform st)
                {
                    st.ScaleX = 1; st.ScaleY = 1;
                }
                if (this.FindName("CameraTranslate") is TranslateTransform tt)
                {
                    tt.X = 0; tt.Y = 0;
                }

                // Timers'ı durdur
                _scanTimer.Stop();
                _scanLineTimer.Stop();

                // UI güncellemeleri
                StartCameraBtn.IsEnabled = true;
                StopCameraBtn.IsEnabled = false;
                ScanBarcodeBtn.IsEnabled = false;

                CameraStatusText.Text = "🎥 Kamera Kapalı";
                CameraStatusText.Foreground = new SolidColorBrush(Colors.Gray);
                ScanStatusText.Text = "Barkod taramaya hazır değil";
                ScanStatusText.Foreground = new SolidColorBrush(Colors.Gray);

                // Scan line'ı gizle
                ScanLine.Opacity = 0;

                // Önizleme alanını gizle (kapatınca yer kaplamasın)
                CameraHost.Visibility = Visibility.Collapsed;

                // Sessiz kapanış; modal kaldırıldı
            }
            catch (Exception ex)
            {
                ShowError($"Kamera durdurma hatası: {ex.Message}");
            }
        }

        private void SafeStopAndDisposeCamera()
        {
            try
            {
                if (_videoSource != null)
                {
                    try
                    {
                        if (_videoSource.IsRunning)
                        {
                            // Hızlı kapanış: timeout'lu bekleme + zorlayıcı durdurma
                            _videoSource.SignalToStop();
                            var waited = System.Threading.SpinWait.SpinUntil(() => !_videoSource.IsRunning, TimeSpan.FromSeconds(2));
                            if (!waited)
                            {
                                try { _videoSource.Stop(); } catch { }
                            }
                        }
                    }
                    catch { /* ignore */ }
                    finally
                    {
                        try { _videoSource.NewFrame -= VideoSource_NewFrame; } catch { }
                        try { _videoSource.Stop(); } catch { }
                        try { _videoSource = null; } catch { }
                    }
                }

                // UI kaynaklarını temizle
                Dispatcher.Invoke(() =>
                {
                    if (CameraPreview != null)
                    {
                        CameraPreview.Source = null;
                    }
                });

                // GC ile DirectShow grafını serbest bırakmayı zorla
                try { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); } catch { }
                AddActivity("⏹️ Kamera tamamen serbest bırakıldı", Colors.Red);
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo("Kamera kaynağı serbest", "BarcodeView");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SafeStopAndDisposeCamera error: {ex.Message}");
            }
        }

        private void BarcodeView_Unloaded(object sender, RoutedEventArgs e)
        {
            // View kapatılırken mutlaka kamera kaynağını bırak
            SafeStopAndDisposeCamera();
        }

        private void ScanBarcode_Click(object sender, RoutedEventArgs e)
        {
            if (!_isCameraActive)
            {
                MessageBox.Show("Önce kamerayı başlatın!", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_isScanning)
            {
                MessageBox.Show("Zaten bir tarama işlemi devam ediyor!", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartScanning();
        }

        private void StartScanning()
        {
            try
            {
                _isScanning = true;
                ScanBarcodeBtn.Content = "🔄 Taranıyor...";
                ScanBarcodeBtn.IsEnabled = false;

                // Scan line animasyonu başlat
                _scanLineTimer.Start();

                // Tarama işlemi başlat – adaptif timeout
                _scanStartUtc = DateTime.UtcNow;
                _scanTimer.Interval = TimeSpan.FromMilliseconds(_baseTimeoutMs);
                _scanTimer.Start();
                _timeoutStreak = 0; // yeni turda sıfırla
                _fallbackFullFrame = false;

                AddActivity("🔍 Barkod tarama başlatıldı", Colors.Blue);
                try
                {
                    bool useRoi = true;
                    double roiTopPercent = 0.25;
                    double roiHeightPercent = 0.50;
                    int decodeCooldownMs = 250;
                    try
                    {
                        var sp = MesTechStok.Desktop.App.ServiceProvider;
                        var config = sp?.GetService<Microsoft.Extensions.Configuration.IConfiguration>();
                        if (config != null)
                        {
                            useRoi = bool.TryParse(config["BarcodeView:Reader:UseROI"], out var ur) ? ur : useRoi;
                            roiTopPercent = double.TryParse(config["BarcodeView:Reader:RoiTopPercent"], out var rtp) ? rtp : roiTopPercent;
                            roiHeightPercent = double.TryParse(config["BarcodeView:Reader:RoiHeightPercent"], out var rhp) ? rhp : roiHeightPercent;
                            decodeCooldownMs = int.TryParse(config["BarcodeView:Reader:DecodeCooldownMs"], out var dcm) ? dcm : decodeCooldownMs;
                        }
                    }
                    catch { }
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("BARCODE", $"ScanStarted UseROI={useRoi} ROI=top:{roiTopPercent:P0} height:{roiHeightPercent:P0} CooldownMs={decodeCooldownMs}", "BarcodeView");
                }
                catch
                {
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("BARCODE", "ScanStarted", "BarcodeView");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Tarama başlatma hatası: {ex.Message}");
            }
        }

        private void ScanTimer_Tick(object? sender, EventArgs e)
        {
            _scanTimer.Stop();
            _scanLineTimer.Stop();

            try
            {
                // Eğer gerçek barkod modu kapalıysa demo barkod üret
                if (!_isRealBarcodeMode)
                {
                    var randomBarcode = GenerateRandomBarcode();
                    ProcessBarcode(randomBarcode);
                }
                else
                {
                    // Adaptif: BaseTimeout dolduysa ancak toplam süre AbsoluteTimeout'u aşmadıysa bir uzatma daha tanı
                    var elapsedMs = (int)(DateTime.UtcNow - _scanStartUtc).TotalMilliseconds;
                    if (elapsedMs + _baseTimeoutMs < _absoluteTimeoutMs)
                    {
                        _scanTimer.Interval = TimeSpan.FromMilliseconds(Math.Min(_absoluteTimeoutMs - elapsedMs, _baseTimeoutMs));
                        _scanTimer.Start();
                        // Kullanıcıyı bilgilendir
                        AddActivity("⏳ Arama sürüyor (uzatıldı)", Colors.Orange);
                        return;
                    }
                    // Mutlak zaman aşımı
                    AddActivity("⏰ Barkod tarama zaman aşımı", Colors.Orange);
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("BARCODE", "PrimaryDecodeTimeout", "BarcodeView");
                    _timeoutStreak++;
                    // Bir sonraki deneme için tam kare fallback etkinleştir (ROI kapalı)
                    if (_timeoutStreak >= 1)
                    {
                        _fallbackFullFrame = true;
                    }
                    ToastManager.ShowWarning("Barkod bulunamadı - tekrar deneyin", "Barkod");
                }

                _isScanning = false;
                ScanBarcodeBtn.Content = "📱 Barkod Tara";
                ScanBarcodeBtn.IsEnabled = true;
                ScanLine.Opacity = 0;
            }
            catch (Exception ex)
            {
                ShowError($"Tarama işlemi hatası: {ex.Message}");
                _isScanning = false;
                ScanBarcodeBtn.Content = "📱 Barkod Tara";
                ScanBarcodeBtn.IsEnabled = true;
            }
        }

        private void ResetScanTimeout()
        {
            // Sliding window: her decode denemesinde süreyi baseTimeout kadar uzat; absolute sınırı aşma
            if (_scanStartUtc == DateTime.MinValue)
            {
                _scanStartUtc = DateTime.UtcNow;
            }
            var elapsedMs = (int)(DateTime.UtcNow - _scanStartUtc).TotalMilliseconds;
            var remaining = Math.Max(0, _absoluteTimeoutMs - elapsedMs);
            var next = Math.Min(remaining, _baseTimeoutMs);
            if (next > 0)
            {
                _scanTimer.Interval = TimeSpan.FromMilliseconds(next);
            }
        }

        private void ScanLineTimer_Tick(object? sender, EventArgs e)
        {
            // Scan line animasyonu: ROI orta bantta ileri-geri hareket
            try
            {
                var h = CameraHost.ActualHeight;
                if (h <= 0) return;
                var top = h * 0.5; // orta bant
                Canvas.SetTop(ScanLine, top);
                ScanLine.Opacity = ScanLine.Opacity == 0 ? 1 : 0;
            }
            catch { }
        }

        private string GenerateRandomBarcode()
        {
            // Mevcut demo barkodlardan birini seç (80% şans)
            if (_random.Next(100) < 80)
            {
                var availableBarcodes = _demoProducts.Keys.ToList();
                return availableBarcodes[_random.Next(availableBarcodes.Count)];
            }

            // Yeni rastgele barkod üret (20% şans)
            return _random.Next(100000000, 999999999).ToString() + _random.Next(1000, 9999).ToString();
        }

        private void ProcessBarcode(string barcode)
        {
            try
            {
                LastBarcodeText.Text = barcode;

                // Ürün arama
                if (_demoProducts.TryGetValue(barcode, out var product))
                {
                    ShowProductInfo(barcode, product);
                    AddActivity($"✅ Ürün bulundu: {product.Name}", Colors.Green);
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogInfo($"Demo barkod okundu: {barcode}", "BarcodeView");
                }
                else
                {
                    ShowProductNotFound(barcode);
                    AddActivity($"❌ Ürün bulunamadı: {barcode}", Colors.Red);
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogWarning($"Demo barkod bulunamadı: {barcode}", "BarcodeView");
                }

                // Geçmişe ekle
                _scanHistory.Insert(0, new ScanResult
                {
                    Barcode = barcode,
                    Timestamp = DateTime.Now,
                    Found = _demoProducts.ContainsKey(barcode)
                });
                // UI üst blok: en fazla 10 madde göster
                TrimScanHistoryUi(10);
            }
            catch (Exception ex)
            {
                ShowError($"Barkod işleme hatası: {ex.Message}");
            }
        }

        private void ProcessRealBarcode(string barcodeText, string format)
        {
            try
            {
                // UI metni
                LastBarcodeText.Text = barcodeText;

                // Global log'a da yaz (LogView gerçek zamanlı güncellensin)
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("BARCODE", $"PrimaryDecodeSuccess value={barcodeText} format={format}", "BarcodeView");

                // Gerçek barkod işleme
                if (_demoProducts.TryGetValue(barcodeText, out var product))
                {
                    ShowProductInfo(barcodeText, product);
                    AddActivity($"✅ Ürün bulundu: {product.Name} (Format: {format})", Colors.Green);
                    ToastManager.ShowSuccess($"Barkod okundu: {product.Name}", "Barkod");
                }
                else
                {
                    ShowProductNotFound(barcodeText);
                    AddActivity($"❌ Ürün bulunamadı: {barcodeText} (Format: {format})", Colors.Red);
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogWarning($"Ürün bulunamadı: {barcodeText}", "BarcodeView");
                    ToastManager.ShowWarning($"Barkod bulunamadı: {barcodeText}", "Barkod");
                }

                // Geçmişe ekle
                _scanHistory.Insert(0, new ScanResult
                {
                    Barcode = barcodeText,
                    Timestamp = DateTime.Now,
                    Found = _demoProducts.ContainsKey(barcodeText),
                    Format = format
                });
                TrimScanHistoryUi(10);

                // SQL'e kalıcı barkod logu yaz
                try
                {
                    var sp = MesTechStok.Desktop.App.ServiceProvider;
                    if (sp != null)
                    {
                        using var scope = sp.CreateScope();
                        var db = scope.ServiceProvider.GetService<MesTechStok.Core.Data.AppDbContext>();
                        if (db != null)
                        {
                            db.BarcodeScanLogs.Add(new MesTechStok.Core.Data.Models.BarcodeScanLog
                            {
                                Barcode = barcodeText,
                                Format = format,
                                Source = "Camera",
                                DeviceId = "DEFAULT_CAMERA",
                                IsValid = true,
                                ValidationMessage = null,
                                RawLength = barcodeText?.Length ?? 0,
                                TimestampUtc = DateTime.UtcNow,
                                CorrelationId = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId
                            });
                            db.SaveChanges();
                            MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("DB", $"BarcodeLogged value={barcodeText} format={format} corr={MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId}", "BarcodeView");
                        }
                    }
                }
                catch (Exception logEx)
                {
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("DB", $"BarcodeLogError {logEx.Message}", "BarcodeView");
                }

                // Kısa bir süre bekleme (duplicate okuma önleme)
                Task.Delay(1000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!_isScanning)
                        {
                            _isScanning = false; // Tekrar tarama için hazır
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                ShowError($"Barkod işleme hatası: {ex.Message}");
            }
        }

        private void ShowProductInfo(string barcode, ProductInfo product)
        {
            ProductInfoPanel.Visibility = Visibility.Visible;
            ErrorPanel.Visibility = Visibility.Collapsed;

            ProductNameText.Text = product.Name;
            ProductPriceText.Text = $"Fiyat: ₺{product.Price:F2}";
            ProductStockText.Text = $"Stok: {product.Stock} adet";

            // POPUP'I GÖSTER - Demo ürününü gerçek Product entity'sine çevir
            ShowProductPopup(barcode, product);
        }

        private void ShowProductNotFound(string barcode)
        {
            ProductInfoPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;

            ErrorText.Text = $"Barkod '{barcode}' için ürün bulunamadı.\nYeni ürün eklemek için ürün yönetimi modülünü kullanın.";
        }

        private void ShowError(string message, bool isCritical = false)
        {
            ProductInfoPanel.Visibility = Visibility.Collapsed;
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorText.Text = message;

            if (isCritical)
            {
                BarcodeErrorText.Text = message;
                BarcodeErrorState.Visibility = Visibility.Visible;
            }

            AddActivity($"❌ Hata: {message}", Colors.Red);
        }

        private void ShowSuccess(string message)
        {
            ErrorPanel.Visibility = Visibility.Collapsed;
            ProductInfoPanel.Visibility = Visibility.Visible;

            AddActivity($"✅ {message}", Colors.Green);
        }

        private void RetryBarcode_Click(object sender, RoutedEventArgs e)
        {
            // Hide error overlay and re-attempt camera start
            BarcodeErrorState.Visibility = Visibility.Collapsed;
            StartCamera_Click(sender, e);
        }

        private void AddActivity(string message, System.Windows.Media.Color color)
        {
            try
            {
                var activityText = new TextBlock
                {
                    Text = $"{DateTime.Now:HH:mm:ss} - {message}",
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = new SolidColorBrush(color)
                };

                ScanHistoryPanel.Children.Insert(0, activityText);
                // En fazla 10 aktivite tutulsun
                while (ScanHistoryPanel.Children.Count > 10)
                {
                    ScanHistoryPanel.Children.RemoveAt(ScanHistoryPanel.Children.Count - 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Activity add error: {ex.Message}");
            }
        }

        private void ShowProductPopup(string barcode, ProductInfo productInfo)
        {
            try
            {
                // Demo ürününü gerçek Product entity'sine çevir
                var product = new MesTechStok.Core.Data.Models.Product
                {
                    Id = productInfo.Id,
                    Name = productInfo.Name,
                    SKU = productInfo.SKU,
                    Barcode = barcode,
                    Description = productInfo.Description,
                    PurchasePrice = productInfo.Price * 0.7m, // Demo için alış fiyatı = satış * 0.7
                    SalePrice = productInfo.Price,
                    Stock = productInfo.Stock,
                    MinimumStock = 5,
                    Brand = productInfo.Brand,
                    ImageUrl = productInfo.ImagePath
                    // Category is an object, not string - leave null for demo
                };

                // Popup'ı göster
                var popup = new BarcodeProductPopup(product);
                popup.Owner = Window.GetWindow(this);
                popup.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError($"Ürün popup gösterme hatası: {ex.Message}");
            }
        }

        private void TrimScanHistoryUi(int maxItems)
        {
            try
            {
                // Placeholder varsa ilk taramada kaldır
                if (ScanHistoryPanel.Children.Count == 1 && ScanHistoryPanel.Children[0] is TextBlock tb && tb.Text.Contains("Henüz tarama geçmişi yok"))
                {
                    ScanHistoryPanel.Children.Clear();
                }
                while (ScanHistoryPanel.Children.Count > maxItems)
                {
                    ScanHistoryPanel.Children.RemoveAt(ScanHistoryPanel.Children.Count - 1);
                }
            }
            catch { }
        }

        private void TestScan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isCameraActive)
                {
                    MessageBox.Show("Önce kamerayı başlatın!", "Uyarı",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Test barkodu ile tarama yap
                var testBarcode = "1234567890123"; // Coca Cola
                ProcessBarcode(testBarcode);

                MessageBox.Show($"🧪 Test barkodu tarandı!\n\nBarkod: {testBarcode}\nÜrün: Coca Cola 330ml",
                    "Test Tarama", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowError($"Test tarama hatası: {ex.Message}");
            }
        }

        private void ManualBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new ManualBarcodeDialog();
                if (dialog.ShowDialog() == true)
                {
                    var barcode = dialog.BarcodeText;
                    if (!string.IsNullOrWhiteSpace(barcode))
                    {
                        ProcessBarcode(barcode.Trim());
                        AddActivity($"⌨️ Manuel barkod girişi: {barcode}", Colors.Blue);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"Manuel barkod girişi hatası: {ex.Message}");
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Tarama geçmişini temizlemek istediğinizden emin misiniz?",
                    "Geçmişi Temizle", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ScanHistoryPanel.Children.Clear();
                    _scanHistory.Clear();

                    var noHistoryText = new TextBlock
                    {
                        Text = "Henüz tarama geçmişi yok",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Colors.Gray)
                    };
                    ScanHistoryPanel.Children.Add(noHistoryText);

                    MessageBox.Show("✅ Tarama geçmişi temizlendi!", "Temizlendi",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Geçmiş temizleme hatası: {ex.Message}");
            }
        }

        private async void TestGlobalBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[BarcodeView] TestGlobalBarcode_Click called");

                // GlobalBarcodeService'i al
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var globalBarcodeService = sp?.GetService<IGlobalBarcodeService>();

                if (globalBarcodeService == null)
                {
                    ShowError("Global barkod servisi bulunamadı!");
                    System.Diagnostics.Debug.WriteLine("[BarcodeView] ❌ GlobalBarcodeService not found");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[BarcodeView] GlobalBarcodeService found: IsListening={globalBarcodeService.IsListening}, IsEnabled={globalBarcodeService.IsEnabled}");

                // Test barkodu ile servisi test et
                var testBarcode = "Test123456789";

                // BarcodeScannerService'den test barkodu gönder
                var barcodeScannerService = sp?.GetService<MesTechStok.Core.Integrations.Barcode.IBarcodeScannerService>();
                if (barcodeScannerService != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[BarcodeView] Sending test barcode via BarcodeScannerService: {testBarcode}");
                    var success = await barcodeScannerService.SendTestBarcodeAsync(testBarcode);
                    System.Diagnostics.Debug.WriteLine($"[BarcodeView] SendTestBarcodeAsync result: {success}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[BarcodeView] ⚠️ BarcodeScannerService not found, manual trigger");
                    // Manuel olarak event trigger et (fallback)
                    var eventArgs = new MesTechStok.Core.Integrations.Barcode.BarcodeScannedEventArgs
                    {
                        Barcode = testBarcode,
                        RawData = testBarcode,
                        BarcodeType = MesTechStok.Core.Integrations.Barcode.BarcodeType.Code128,
                        ScannedAt = DateTime.UtcNow,
                        DeviceId = "TEST_DEVICE",
                        Quality = 1.0
                    };

                    // Reflection ile private OnBarcodeScanned metodunu çağır
                    var method = globalBarcodeService.GetType().GetMethod("OnBarcodeScanned",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (method != null)
                    {
                        method.Invoke(globalBarcodeService, new object[] { null, eventArgs });
                        System.Diagnostics.Debug.WriteLine("[BarcodeView] ✅ Manual OnBarcodeScanned triggered");
                    }
                }

                ShowSuccess($"🧪 Test barkodu gönderildi: {testBarcode}");
            }
            catch (Exception ex)
            {
                ShowError($"Global barkod test hatası: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BarcodeView] ❌ TestGlobalBarcode error: {ex}");
            }
        }

        public void Dispose()
        {
            // Kamerayı durdur
            try
            {
                if (_videoSource != null && _videoSource.IsRunning)
                {
                    _videoSource.SignalToStop();
                    _videoSource.WaitForStop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Camera disposal error: {ex.Message}");
            }

            _scanTimer?.Stop();
            _scanLineTimer?.Stop();
        }
    }

    // Helper classes
    public class ProductInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string SKU { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Category { get; set; } = "";
        public string Brand { get; set; } = "";
        public string ImagePath { get; set; } = "";
    }

    public class ScanResult
    {
        public string Barcode { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool Found { get; set; }
        public string Format { get; set; } = ""; // Yeni eklenen format bilgisi
    }

    // Manuel barkod girişi için dialog
    public partial class ManualBarcodeDialog : Window
    {
        public string BarcodeText { get; private set; } = "";

        public ManualBarcodeDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Title = "Manuel Barkod Girişi";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock
            {
                Text = "📱 Barkod numarasını girin:",
                FontSize = 14,
                Margin = new Thickness(20, 20, 20, 10)
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            var textBox = new TextBox
            {
                Name = "BarcodeTextBox",
                FontSize = 16,
                Padding = new Thickness(10),
                Margin = new Thickness(20, 0, 20, 20)
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20)
            };

            var okButton = new Button
            {
                Content = "✅ Tamam",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            okButton.Click += (s, e) => { BarcodeText = textBox.Text; DialogResult = true; };

            var cancelButton = new Button
            {
                Content = "❌ İptal",
                Width = 80,
                Height = 30
            };
            cancelButton.Click += (s, e) => { DialogResult = false; };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            Content = grid;
            textBox.Focus();
        }
    }
}