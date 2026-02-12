using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Services;
using System.Windows.Shapes;
using Microsoft.Extensions.Configuration;

namespace MesTechStok.Desktop.Views
{
    public partial class WelcomeWindow : Window
    {
        private readonly MesTechStok.Desktop.MainWindow? _existingMainWindow;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _systemStatsTimer;
        private readonly List<string> _backgroundImages;
        private int _currentImageIndex = 0;
        private readonly Random _random;
        private bool _isGalleryExpanded = false;
        private readonly ISystemResourceService _sysService;
        private string? _targetModule;

        public WelcomeWindow(string? targetModule = null)
        {
            InitializeComponent();
            _targetModule = targetModule;

            _backgroundImages = new List<string>();
            _random = new Random();

            // Initialize timers
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;

            _systemStatsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _systemStatsTimer.Tick += SystemStatsTimer_Tick;

            this.KeyDown += WelcomeWindow_KeyDown;
            Loaded += WelcomeWindow_Loaded;

            // Canlı ayar güncellemesi için event
            try { MesTechStok.Desktop.Utils.EventBus.CompanySettingsChanged += OnCompanySettingsChanged; } catch { }

            // Resolve system resource service from DI if available
            _sysService = App.ServiceProvider?.GetService<ISystemResourceService>() ?? new SystemResourceService(Microsoft.Extensions.Logging.Abstractions.NullLogger<SystemResourceService>.Instance);
            if (_sysService is SystemResourceService srs)
            {
                srs.Start();
            }
        }

        // Overload: When returning from MainWindow, we pass the hidden instance to reshow instead of creating a new one
        public WelcomeWindow(MesTechStok.Desktop.MainWindow existingMainWindow) : this()
        {
            _existingMainWindow = existingMainWindow;
        }

        private void WelcomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadDefaultImages();
                LoadImageGallery();
                SetRandomBackgroundImage();

                _clockTimer.Start();
                _systemStatsTimer.Start();

                UpdateClock();
                UpdateSystemStats();

                // Firma adını SQL Server'dan otomatik yükle ve başlık/alt bilgiye yansıt
                _ = LoadCompanySettingsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Karşılama ekranı başlatma hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task LoadCompanySettingsAsync()
        {
            try
            {
                var sp = App.ServiceProvider;
                if (sp == null) return;

                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var settings = await db.Set<MesTechStok.Core.Data.Models.CompanySettings>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (settings != null)
                {
                    // Pencere başlığı ve footer güncelle
                    Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            this.Title = $"{settings.CompanyName} - MesTech Stok Takip Sistemi";
                            if (FooterCompanyText != null)
                            {
                                FooterCompanyText.Text = settings.CompanyName;
                            }
                        }
                        catch { }
                    });
                }
            }
            catch (Exception ex)
            {
                // Sessiz geç, log gerekirse eklenebilir
                System.Diagnostics.Debug.WriteLine($"CompanySettings yükleme hatası: {ex.Message}");
            }
        }

        private void LoadDefaultImages()
        {
            try
            {
                var imagesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                var imageFiles = Directory.GetFiles(imagesPath, "*.jpg")
                    .Concat(Directory.GetFiles(imagesPath, "*.jpeg"))
                    .Concat(Directory.GetFiles(imagesPath, "*.png"))
                    .Concat(Directory.GetFiles(imagesPath, "*.bmp"))
                    .ToArray();

                foreach (var imageFile in imageFiles)
                {
                    _backgroundImages.Add(imageFile);
                }

                if (_backgroundImages.Count == 0)
                {
                    CreateDefaultGradientBackground();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Default images load error: {ex.Message}");
                CreateDefaultGradientBackground();
            }
        }

        private void CreateDefaultGradientBackground()
        {
            try
            {
                var gradientBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1)
                };

                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1A, 0x23, 0x7E), 0));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x21, 0x96, 0xF3), 0.5));
                gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x03, 0xA9, 0xF4), 1));

                BackgroundImage.Source = null;
                this.Background = gradientBrush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gradient background error: {ex.Message}");
                this.Background = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
            }
        }

        private void LoadImageGallery()
        {
            try
            {
                ImageGalleryPanel.Children.Clear();

                foreach (var imagePath in _backgroundImages)
                {
                    var previewBorder = CreateImagePreview(imagePath);
                    ImageGalleryPanel.Children.Add(previewBorder);
                }

                var uploadPlaceholder = CreateUploadPlaceholder();
                ImageGalleryPanel.Children.Add(uploadPlaceholder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery load error: {ex.Message}");
            }
        }

        private Border CreateImagePreview(string imagePath)
        {
            var mainBorder = new Border
            {
                Width = 200,
                Height = 120,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(2),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var grid = new Grid();
            mainBorder.Child = grid;

            try
            {
                // BitmapImage oluştururken file handle'ı hemen kapat
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                bitmap.Freeze();

                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill
                };
                grid.Children.Add(image);

                mainBorder.MouseLeftButtonDown += (s, e) =>
                {
                    SetBackgroundImage(imagePath);
                };
            }
            catch
            {
                grid.Background = new SolidColorBrush(Colors.Gray);
                var errorText = new TextBlock
                {
                    Text = "Resim\nYüklenemedi",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center
                };
                grid.Children.Add(errorText);
            }

            var deleteButton = new Button
            {
                Content = "🗑️",
                Width = 30,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 5, 5, 0),
                Background = new SolidColorBrush(Color.FromArgb(200, 244, 67, 54)),
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0),
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            deleteButton.Click += (s, e) =>
            {
                e.Handled = true;
                DeleteImage(imagePath);
            };

            grid.Children.Add(deleteButton);
            return mainBorder;
        }

        private Border CreateUploadPlaceholder()
        {
            var border = new Border
            {
                Width = 200,
                Height = 120,
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(0, 0, 0, 10),
                BorderBrush = new SolidColorBrush(Colors.Orange),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(100, 255, 152, 0)),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = "📁",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Yeni Resim\nEkle",
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold
            });

            border.Child = stackPanel;
            border.MouseLeftButtonDown += (s, e) => UploadImageButton_Click(s, e);

            return border;
        }

        private void SetBackgroundImage(string imagePath)
        {
            try
            {
                // BitmapImage oluştururken file handle'ı hemen kapat
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Resmi belleğe yükle, file handle'ı kapat
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();
                bitmap.Freeze(); // Performance için freeze et

                BackgroundImage.Source = bitmap;
                this.Background = Brushes.Transparent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Background set error: {ex.Message}");
            }
        }

        private void SetRandomBackgroundImage()
        {
            try
            {
                if (_backgroundImages.Count > 0)
                {
                    var randomIndex = _random.Next(_backgroundImages.Count);
                    SetBackgroundImage(_backgroundImages[randomIndex]);
                    _currentImageIndex = randomIndex;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Random background error: {ex.Message}");
            }
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            try
            {
                var now = DateTime.Now;
                TimeDisplay.Text = now.ToString("HH:mm:ss");
                DateDisplay.Text = now.ToString("dd MMMM yyyy, dddd", new System.Globalization.CultureInfo("tr-TR"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clock update error: {ex.Message}");
            }
        }

        private void SystemStatsTimer_Tick(object? sender, EventArgs e)
        {
            UpdateSystemStats();
        }

        private void UpdateSystemStats()
        {
            try
            {
                var perf = (_sysService as SystemResourceService)?.SystemPerformance;
                if (perf != null)
                {
                    CpuUsage.Text = $"{perf.TotalCpuUsage:F1}%";
                    RamUsage.Text = $"{perf.UsedMemoryGB:F1} GB";
                    var uptime = perf.SystemUptime;
                    SystemUptime.Text = $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"System stats error: {ex.Message}");
            }
        }

        private void SystemStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var perf = (_sysService as SystemResourceService)?.SystemPerformance;
                if (perf != null)
                {
                    OverlayCpu.Text = $"{perf.TotalCpuUsage:F1}%";
                    OverlayMem.Text = $"{perf.UsedMemoryGB:F1} GB / {perf.TotalMemoryGB:F1} GB";
                    OverlayDisk.Text = $"{perf.DiskUsage:F1}%";
                    OverlayTotal.Text = $"{perf.TotalMemoryGB:F1} GB";
                    OverlayAvail.Text = $"{perf.AvailableMemoryGB:F1} GB";
                    OverlayProcCount.Text = perf.ActiveProcessCount.ToString();
                    var up = perf.SystemUptime; OverlayUptime.Text = $"{(int)up.TotalHours}h {up.Minutes}m";
                    OverlayMesCpu.Text = $"{perf.MesTechCpuUsage:F1}%";
                    OverlayMesRam.Text = $"{perf.MesTechMemoryUsageMB:F0} MB";
                }
                SystemOverlay.Visibility = Visibility.Visible;
                // Slide-in animation
                var anim = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 600,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(280),
                    EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                };
                OverlaySlide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);

                // Bar width animation (CPU)
                if (perf != null && double.TryParse(perf.TotalCpuUsage.ToString("F0"), out var cpu))
                {
                    var barAnim = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0,
                        To = Math.Max(20, Math.Min(300, cpu * 3)),
                        Duration = TimeSpan.FromMilliseconds(500),
                        EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut }
                    };
                    BarCpu.BeginAnimation(FrameworkElement.WidthProperty, barAnim);
                }
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Sistem bilgisi gösterilemedi: {ex.Message}", "#F44336");
            }
        }

        private void CloseSystemOverlay_Click(object sender, RoutedEventArgs e)
        {
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 600,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };
            anim.Completed += (s, _) => SystemOverlay.Visibility = Visibility.Collapsed;
            OverlaySlide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, anim);
        }

        private async void PerfModeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowToast("⚡ Performans modu: Diğer uygulamalar sınırlandırılıyor...", "#4CAF50");
                if (_sysService is SystemResourceService srs)
                {
                    await srs.ApplyThrottlingForNonMesTechAsync(70);
                    ShowToast("✅ Uygulandı. MesTech önceliklendirildi (admin gerekebilir).", "#4CAF50");
                }
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Performans modu hatası: {ex.Message}", "#F44336");
            }
        }

        private void EnterSystemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _clockTimer?.Stop();
                _systemStatsTimer?.Stop();

                if (_existingMainWindow != null)
                {
                    // Return to the already running MainWindow (hidden)
                    _existingMainWindow.Show();
                    _existingMainWindow.WindowState = WindowState.Maximized;
                    _existingMainWindow.Activate();
                    _existingMainWindow.Focus();

                    // Navigate to dashboard (or keep last state). Prefer dashboard for a clean return
                    if (_existingMainWindow.DataContext is ViewModels.MainViewModel vm)
                    {
                        // Hedef modül varsa ona git
                        if (!string.IsNullOrEmpty(_targetModule))
                        {
                            switch (_targetModule)
                            {
                                case "barcode":
                                    vm.ShowBarcodeCommand.Execute(null);
                                    break;
                                default:
                                    vm.ShowDashboardCommand.Execute(null);
                                    break;
                            }
                        }
                        else
                        {
                            vm.ShowDashboardCommand.Execute(null);
                        }
                    }
                }
                else
                {
                    // Get MainViewModel from DI container and create a fresh instance
                    var serviceProvider = App.ServiceProvider;
                    if (serviceProvider != null)
                    {
                        var mainViewModel = serviceProvider.GetRequiredService<ViewModels.MainViewModel>();
                        var mainWindow = new MainWindow(mainViewModel);
                        mainWindow.Show();
                        mainWindow.WindowState = WindowState.Maximized;

                        // Hedef modül varsa ona git
                        if (!string.IsNullOrEmpty(_targetModule))
                        {
                            switch (_targetModule)
                            {
                                case "barcode":
                                    mainViewModel.ShowBarcodeCommand.Execute(null);
                                    break;
                                default:
                                    mainViewModel.ShowInventoryCommand.Execute(null);
                                    break;
                            }
                        }
                        else
                        {
                            // Directly navigate to Inventory View
                            mainViewModel.ShowInventoryCommand.Execute(null);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Service provider not available");
                    }
                }

                this.Hide();
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Ana sistem açılırken hata oluştu: {ex.Message}", "#F44336");
            }
        }

        private void UploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Arka Plan Resmi Seçin",
                    Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp|JPG Files|*.jpg|PNG Files|*.png|BMP Files|*.bmp|All Files|*.*",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var imagesPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    var addedCount = 0;

                    foreach (var fileName in openFileDialog.FileNames)
                    {
                        try
                        {
                            var destFileName = System.IO.Path.Combine(imagesPath, System.IO.Path.GetFileName(fileName));

                            if (!File.Exists(destFileName))
                            {
                                File.Copy(fileName, destFileName);
                                _backgroundImages.Add(destFileName);
                                addedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            ShowToast($"❌ Resim kopyalama hatası: {ex.Message}", "#F44336");
                        }
                    }

                    LoadImageGallery();

                    if (addedCount > 0)
                    {
                        ShowToast($"✅ {addedCount} resim başarıyla eklendi!", "#4CAF50");
                    }
                    else
                    {
                        ShowToast($"ℹ️ Seçilen resimler zaten mevcut", "#FF9800");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowToast($"❌ resim yükleme hatası: {ex.Message}", "#F44336");
            }
        }

        private void NextImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backgroundImages.Count > 0)
                {
                    _currentImageIndex = (_currentImageIndex + 1) % _backgroundImages.Count;
                    SetBackgroundImage(_backgroundImages[_currentImageIndex]);
                    ShowToast($"🔄 Sonraki resim: {System.IO.Path.GetFileName(_backgroundImages[_currentImageIndex])}", "#2196F3");
                }
                else
                {
                    ShowToast("ℹ️ Gösterilecek resim bulunamadı", "#FF9800");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Next image error: {ex.Message}");
                ShowToast("❌ Resim değiştirme hatası", "#F44336");
            }
        }

        private void RandomImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backgroundImages.Count > 0)
                {
                    SetRandomBackgroundImage();
                    ShowToast($"🎲 Rastgele resim: {System.IO.Path.GetFileName(_backgroundImages[_currentImageIndex])}", "#9C27B0");
                }
                else
                {
                    ShowToast("ℹ️ Gösterilecek resim bulunamadı", "#FF9800");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Random image error: {ex.Message}");
                ShowToast("❌ Rastgele resim seçme hatası", "#F44336");
            }
        }

        private void FabUploadImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UploadImageButton_Click(sender, e);
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Hızlı yükleme hatası: {ex.Message}", "#F44336");
            }
        }

        private void OpenMarketplacesOverlay()
        {
            if (MarketplacesOverlay.Visibility != Visibility.Visible)
            {
                MarketplacesOverlay.Visibility = Visibility.Visible;
                // Tile butonlara click handler ata (tek sefer)
                AttachMarketplaceHandlers();
                // İçerikleri mevcut dosyaya göre güncelle (logo yoksa fallback vektör)
                try { ReplaceMarketplaceTileContent(); } catch { }
            }
        }

        private void AttachMarketplaceHandlers()
        {
            try
            {
                void AttachToButtons(Panel panel)
                {
                    foreach (var child in panel.Children)
                    {
                        if (child is Button btn)
                        {
                            btn.Click -= MarketplaceTile_Click;
                            btn.Click += MarketplaceTile_Click;
                            // Marka rengi kenarlığı için Tag'e göre hover rengini ayarla (Opacity ~0.25)
                            var tag = btn.Tag?.ToString()?.ToLowerInvariant();
                            var brand = tag switch
                            {
                                "trendyol" => (Color)ColorConverter.ConvertFromString("#FFFE5000"),
                                "hepsiburada" => (Color)ColorConverter.ConvertFromString("#FFFF6A00"),
                                "pazarama" => (Color)ColorConverter.ConvertFromString("#FF00ADEF"),
                                "n11" => (Color)ColorConverter.ConvertFromString("#FFD50000"),
                                "ptt avm" => (Color)ColorConverter.ConvertFromString("#FF0057A6"),
                                "çiçeksepeti" => (Color)ColorConverter.ConvertFromString("#FF1E88E5"),
                                "ozon" => (Color)ColorConverter.ConvertFromString("#FF0066FF"),
                                "amazon" => (Color)ColorConverter.ConvertFromString("#FFFF9900"),
                                _ => (Color)ColorConverter.ConvertFromString("#FFD1E3FF")
                            };
                            btn.MouseEnter -= (_, __) => { };
                            btn.MouseEnter += (_, __) =>
                            {
                                btn.BorderBrush = new SolidColorBrush(Color.FromArgb((byte)(0.25 * 255), brand.R, brand.G, brand.B));
                                btn.BorderThickness = new Thickness(2);
                            };
                            btn.MouseLeave -= (_, __) => { };
                            btn.MouseLeave += (_, __) =>
                            {
                                btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
                                btn.BorderThickness = new Thickness(1);
                            };
                        }
                        else if (child is Panel nested)
                        {
                            AttachToButtons(nested);
                        }
                    }
                }

                if (MarketplacesOverlay.Children.Count > 0 && MarketplacesOverlay.Children[0] is Panel root)
                {
                    AttachToButtons(root);
                }
            }
            catch { }
        }

        private void MarketplaceTile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                var name = btn.Tag?.ToString() ?? (btn.Content as string) ?? "Pazaryeri";
                if (string.Equals(name, "Kapat", StringComparison.OrdinalIgnoreCase))
                {
                    MarketplacesOverlay.Visibility = Visibility.Collapsed;
                    return;
                }

                ShowToast($"🛍️ {name} modülü hazırlanıyor", "#3B82F6");
            }
        }

        private void ReplaceMarketplaceTileContent()
        {
            if (MarketplacesOverlay.Children.Count == 0) return;
            if (MarketplacesOverlay.Children[0] is Panel root)
            {
                foreach (var child in root.Children)
                {
                    if (child is Button btn)
                    {
                        var tag = btn.Tag?.ToString() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(tag) && !string.Equals(tag, "Kapat", StringComparison.OrdinalIgnoreCase))
                        {
                            btn.Content = BuildMarketplaceTile(tag);
                        }
                    }
                }
            }
        }

        private UIElement BuildMarketplaceTile(string brandName)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical };
            var normalized = NormalizeBrandFileName(brandName);
            try
            {
                var packUri = new Uri($"pack://application:,,,/Assets/Logos/{normalized}.png", UriKind.Absolute);
                var image = new Image
                {
                    Source = new BitmapImage(packUri),
                    Height = 40,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stack.Children.Add(image);
            }
            catch
            {
                // Fallback: marka rengiyle basit vektör rozet + başlık
                var color = GetBrandColor(brandName);
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(32, color.R, color.G, color.B)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(96, color.R, color.G, color.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                var icon = new Grid { Width = 40, Height = 24 };
                icon.Children.Add(new Rectangle
                {
                    RadiusX = 4,
                    RadiusY = 4,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(color)
                });
                icon.Children.Add(new Line
                {
                    X1 = 6,
                    Y1 = 12,
                    X2 = 34,
                    Y2 = 12,
                    StrokeThickness = 2,
                    Stroke = new SolidColorBrush(color)
                });
                border.Child = icon;
                stack.Children.Add(border);
            }
            stack.Children.Add(new TextBlock
            {
                Text = brandName,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return stack;
        }

        private static string NormalizeBrandFileName(string brand)
        {
            var s = brand.ToLowerInvariant()
                .Replace("ç", "c").Replace("ş", "s").Replace("ğ", "g").Replace("ı", "i").Replace("ö", "o").Replace("ü", "u");
            s = new string(s.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray()).Trim().Replace(' ', '_');
            if (s == "ptt_avm") s = "ptt"; // klasörde ptt.png kullanıyoruz
            return s;
        }

        private static Color GetBrandColor(string? tag)
        {
            tag = (tag ?? string.Empty).ToLowerInvariant();
            return tag switch
            {
                "trendyol" => (Color)ColorConverter.ConvertFromString("#FE5000"),
                "hepsiburada" => (Color)ColorConverter.ConvertFromString("#FF6A00"),
                "pazarama" => (Color)ColorConverter.ConvertFromString("#00ADEF"),
                "n11" => (Color)ColorConverter.ConvertFromString("#D50000"),
                "ptt avm" => (Color)ColorConverter.ConvertFromString("#0057A6"),
                "çiçeksepeti" => (Color)ColorConverter.ConvertFromString("#1E88E5"),
                "ozon" => (Color)ColorConverter.ConvertFromString("#0066FF"),
                "amazon" => (Color)ColorConverter.ConvertFromString("#FF9900"),
                _ => (Color)ColorConverter.ConvertFromString("#D1E3FF")
            };
        }
        private void GalleryToggleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isGalleryExpanded = !_isGalleryExpanded;

                if (_isGalleryExpanded)
                {
                    GalleryPanel.Visibility = Visibility.Visible;
                    GalleryToggleText.Text = "Kapat";
                    ShowToast("🖼️ Resim galerisi açıldı", "#4CAF50");
                }
                else
                {
                    GalleryPanel.Visibility = Visibility.Collapsed;
                    GalleryToggleText.Text = "Aç";
                    ShowToast("🖼️ Resim galerisi kapatıldı", "#FF9800");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery toggle error: {ex.Message}");
            }
        }

        private void WelcomeWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    // Önce açık overlay'leri kapat
                    if (SystemOverlay != null && SystemOverlay.Visibility == Visibility.Visible)
                    {
                        CloseSystemOverlay_Click(this, new RoutedEventArgs());
                        e.Handled = true;
                        return;
                    }
                    if (MarketplacesOverlay != null && MarketplacesOverlay.Visibility == Visibility.Visible)
                    {
                        MarketplacesOverlay.Visibility = Visibility.Collapsed;
                        e.Handled = true;
                        return;
                    }

                    // Ana pencereye dön (varsa), yoksa minimize et
                    if (_existingMainWindow != null)
                    {
                        try
                        {
                            _existingMainWindow.Show();
                            _existingMainWindow.WindowState = WindowState.Maximized;
                            _existingMainWindow.Activate();
                            _existingMainWindow.Focus();
                            this.Hide();
                            e.Handled = true;
                            return;
                        }
                        catch { }
                    }

                    this.WindowState = WindowState.Minimized;
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeyDown error: {ex.Message}");
            }
        }

        private void MesTechMarketplacesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenMarketplacesOverlay();
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Pazaryerleri açılamadı: {ex.Message}", "#F44336");
            }
        }

        private void MesTechDropShippingButton_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("🚚 MesTech DropShipping - Geliştiriliyor!", "#9C27B0");
        }

        private void OtomasyonButton_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("⚙️ MesTech Otomasyon - Geliştiriliyor!", "#FF5722");
        }

        private void MesTechAIButton_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("🎓 MesTech Eğitim - Geliştiriliyor!", "#607D8B");
        }

        private void MesTechEgitimButton_Click(object sender, RoutedEventArgs e)
        {
            ShowToast("🎓 MesTech Eğitim - Geliştiriliyor!", "#607D8B");
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new SettingsOverlayWindow
                {
                    Owner = this
                };
                var result = dlg.ShowDialog();
                if (result == true)
                {
                    // Firma adını footer'a yansıtma (varsa)
                    try
                    {
                        var sp = App.ServiceProvider;
                        if (sp != null)
                        {
                            using var scope = sp.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            var settings = await db.Set<MesTechStok.Core.Data.Models.CompanySettings>().FirstOrDefaultAsync();
                            if (settings != null)
                            {
                                // Footer sol metnini güncelle
                                // Grid -> Row 2 -> Column 0 -> StackPanel -> first TextBlock
                                if (this.Content is Grid rootGrid)
                                {
                                    var footerGrid = rootGrid.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 2);
                                    if (footerGrid != null)
                                    {
                                        var leftPanel = footerGrid.Children.OfType<StackPanel>().FirstOrDefault(spn => Grid.GetColumn(spn) == 0);
                                        var firstText = leftPanel?.Children.OfType<TextBlock>().FirstOrDefault();
                                        if (firstText != null)
                                        {
                                            firstText.Text = settings.CompanyName;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Ayarlar açılamadı: {ex.Message}", "#F44336");
            }
        }

        private void ShowToast(string message, string backgroundColor = "#4CAF50")
        {
            try
            {
                var toastBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(backgroundColor)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20, 15, 20, 15),
                    Margin = new Thickness(20, 20, 20, 20),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    MaxWidth = 400,
                    Opacity = 0
                };

                toastBorder.Effect = new DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 5,
                    Color = Colors.Black,
                    Opacity = 0.3
                };

                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap
                };

                toastBorder.Child = textBlock;

                var mainGrid = (Grid)this.Content;
                Grid.SetRowSpan(toastBorder, 3);
                mainGrid.Children.Add(toastBorder);

                var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                toastBorder.BeginAnimation(OpacityProperty, fadeInAnimation);

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(4)
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();

                    var fadeOutAnimation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300)
                    };

                    fadeOutAnimation.Completed += (s2, e2) =>
                    {
                        mainGrid.Children.Remove(toastBorder);
                    };

                    toastBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);
                };

                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Toast notification error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Toast: {message}");
            }
        }

        private void DeleteImage(string imagePath)
        {
            try
            {
                ShowConfirmationToast(
                    $"🗑️ Bu resmi silmek istediğinizden emin misiniz?\n{System.IO.Path.GetFileName(imagePath)}",
                    () =>
                    {
                        try
                        {
                            // Önce resmi liste ve UI'dan kaldır
                            _backgroundImages.Remove(imagePath);

                            // Eğer bu resim şu an kullanılıyorsa, farklı bir resme geç
                            if (BackgroundImage?.Source != null &&
                                BackgroundImage.Source is BitmapImage currentBitmap &&
                                currentBitmap.UriSource?.LocalPath == imagePath)
                            {
                                // UI'dan resmi temizle
                                BackgroundImage.Source = null;

                                // Garbage collection'ı zorunlu yap
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                            }

                            // Biraz bekle ki file handle serbest kalsın
                            System.Threading.Thread.Sleep(100);

                            // Dosyayı sil
                            if (File.Exists(imagePath))
                            {
                                File.Delete(imagePath);
                            }

                            // Yeni resim ayarla
                            if (_currentImageIndex >= _backgroundImages.Count && _backgroundImages.Count > 0)
                            {
                                _currentImageIndex = 0;
                                SetBackgroundImage(_backgroundImages[_currentImageIndex]);
                            }
                            else if (_backgroundImages.Count == 0)
                            {
                                CreateDefaultGradientBackground();
                            }

                            LoadImageGallery();
                            ShowToast("✅ Resim başarıyla silindi!", "#4CAF50");
                        }
                        catch (Exception ex)
                        {
                            ShowToast($"❌ Resim silme hatası: {ex.Message}", "#F44336");
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                ShowToast($"❌ Resim silme hatası: {ex.Message}", "#F44336");
                System.Diagnostics.Debug.WriteLine($"Delete image error: {ex.Message}");
            }
        }

        private void ShowConfirmationToast(string message, Action onConfirm)
        {
            try
            {
                var toastBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5722")),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(20, 15, 20, 15),
                    Margin = new Thickness(20, 20, 20, 20),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    MaxWidth = 450,
                    Opacity = 0
                };

                toastBorder.Effect = new DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 5,
                    Color = Colors.Black,
                    Opacity = 0.3
                };

                var stackPanel = new StackPanel();

                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                var confirmButton = new Button
                {
                    Content = "✅ Evet",
                    Background = new SolidColorBrush(Colors.Green),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(15, 5, 15, 5),
                    Margin = new Thickness(0, 0, 10, 0),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                var cancelButton = new Button
                {
                    Content = "❌ Hayır",
                    Background = new SolidColorBrush(Colors.Gray),
                    Foreground = new SolidColorBrush(Colors.White),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(15, 5, 15, 5),
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                confirmButton.Click += (s, e) =>
                {
                    var mainGrid = (Grid)this.Content;
                    mainGrid.Children.Remove(toastBorder);
                    onConfirm?.Invoke();
                };

                cancelButton.Click += (s, e) =>
                {
                    var mainGrid = (Grid)this.Content;
                    mainGrid.Children.Remove(toastBorder);
                    ShowToast("ℹ️ İşlem iptal edildi", "#FF9800");
                };

                buttonPanel.Children.Add(confirmButton);
                buttonPanel.Children.Add(cancelButton);

                stackPanel.Children.Add(textBlock);
                stackPanel.Children.Add(buttonPanel);

                toastBorder.Child = stackPanel;

                var mainGrid = (Grid)this.Content;
                Grid.SetRowSpan(toastBorder, 3);
                mainGrid.Children.Add(toastBorder);

                var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                toastBorder.BeginAnimation(OpacityProperty, fadeInAnimation);

                var timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    if (mainGrid.Children.Contains(toastBorder))
                    {
                        mainGrid.Children.Remove(toastBorder);
                        ShowToast("ℹ️ İşlem zaman aşımına uğradı", "#FF9800");
                    }
                };

                timer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Confirmation toast error: {ex.Message}");
                onConfirm?.Invoke();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _clockTimer?.Stop();
            _systemStatsTimer?.Stop();
            try { MesTechStok.Desktop.Utils.EventBus.CompanySettingsChanged -= OnCompanySettingsChanged; } catch { }
            base.OnClosed(e);
        }

        private void OnCompanySettingsChanged(string? companyName)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(companyName))
                    {
                        this.Title = $"{companyName} - MesTech Stok Takip Sistemi";
                        if (FooterCompanyText != null) FooterCompanyText.Text = companyName;
                    }
                    else
                    {
                        _ = LoadCompanySettingsAsync();
                    }
                });
            }
            catch { }
        }
    }
}