using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MesTechStok.Desktop.ViewModels;
using MesTechStok.Desktop.Views;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Services;
using MesTechStok.Core.Services.Abstract;
using System.Windows.Documents;
using MesTechStok.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace MesTechStok.Desktop
{
    /// <summary>
    /// MainWindow.xaml için etkileşim mantığı
    /// EMERGENCY FIX: Simplified constructor to prevent crash
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private bool _isWelcomeMode = false;
        private DispatcherTimer? _welcomeClockTimer;
        private readonly List<string> _backgroundImages = new();
        private int _currentImageIndex = 0;
        private readonly Random _random = new();

        // Gallery Panel Management
        private bool _isGalleryOpen = false;

        // Toast ve Ekran Koruyucu
        private readonly ObservableCollection<ToastItem> _toastItems = null!;
        private DispatcherTimer? _idleTimer;
        private DispatcherTimer? _screensaverImageTimer;
        private DateTime _lastActivityTime;
        private bool _isScreensaverActive = false;
        private bool _screensaverEnabled = false;
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(3);
        private Point _lastMousePosition;

        // Fields for fullscreen state
        private WindowState _previousWindowState = WindowState.Maximized;
        private WindowStyle _previousWindowStyle = WindowStyle.SingleBorderWindow;

        // Password authentication (configurable)
        private const string MASTER_PASSWORD = "Admin123!";
        private bool _isAuthenticated = false;

        public MainWindow(MainViewModel viewModel, bool startInWelcomeMode = false)
        {
            try
            {
                InitializeComponent();
                DataContext = viewModel;
                _viewModel = viewModel;
                _toastItems = new ObservableCollection<ToastItem>();
                _isWelcomeMode = startInWelcomeMode;

                // PROGRESSIVE RESTORE: Basic window setup
                this.MinWidth = 1200;
                this.MinHeight = 800;
                this.Title = "MesTech Stok Takip Sistemi";
                this.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

                // PROGRESSIVE RESTORE: Show main system content
                this.Loaded += MainWindow_ProgressiveLoad;

                // VISIBILITY FIX: Force show window
                this.WindowState = WindowState.Normal;
                this.Show();
                this.Activate();
                this.Focus();

                // Header bilgilerini doldur (CompanySettings + DB)
                try
                {
                    var sp = App.ServiceProvider;
                    var cfg = sp?.GetService<IConfiguration>();
                    var dbInfo = "MesTech_stok – SQL Server (SQLEXPRESS)";
                    var dbText = this.FindName("HeaderDbInfo") as TextBlock;
                    if (dbText != null) dbText.Text = dbInfo;

                    // Firma adı için SettingsView ile aynı kaynaktan (CompanySettings) okunacaksa burada basit placeholder bırakıyoruz.
                    var companyText = this.FindName("HeaderCompanyName") as TextBlock;
                    if (companyText != null && string.IsNullOrWhiteSpace(companyText.Text))
                        companyText.Text = "MesChain Tekstil";

                    // Canlı güncelleme için EventBus dinleyicisi
                    try { MesTechStok.Desktop.Utils.EventBus.CompanySettingsChanged += OnCompanySettingsChanged; } catch { }
                }
                catch { }

                // APPLICATION READY: Trigger application ready event for monitoring
                TriggerApplicationReady();

                // Setup global exception handling early
                try { SetupGlobalExceptionHandling(); } catch { }

                // Authentication: Skip overlay if configured
                try
                {
                    var config = App.ServiceProvider?.GetService<IConfiguration>();
                    // Screensaver config (default: disabled)
                    _screensaverEnabled = config?.GetSection("Screensaver")?.GetValue<bool>("Enabled") ?? false;
                    var skipLogin = config?.GetSection("Authentication")?.GetValue<bool>("SkipLogin") ?? false;
                    if (skipLogin)
                    {
                        _isAuthenticated = true;
                        HidePasswordOverlay();
                    }
                }
                catch { /* ignore */ }

                // CHOOSE STARTUP MODE: Welcome vs Main System
                if (_isWelcomeMode)
                {
                    InitializeWelcomeModeDirect();
                }
                else
                {
                    InitializeMainSystemDirect();
                }

            }
            catch (Exception ex)
            {
                // LOG ONLY - Don't show MessageBox that blocks UI
                System.Diagnostics.Debug.WriteLine($"❌ MainWindow Constructor Error: {ex.Message}");
                GlobalLogger.Instance?.LogError($"MainWindow constructor error: {ex.Message}", "MainWindow");
            }
        }

        private void OnCompanySettingsChanged(string? companyName)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    var companyText = this.FindName("HeaderCompanyName") as TextBlock;
                    if (companyText != null && !string.IsNullOrWhiteSpace(companyName))
                    {
                        companyText.Text = companyName.Trim();
                    }
                });
            }
            catch { }
        }

        private void MainWindow_ProgressiveLoad(object? sender, RoutedEventArgs e)
        {
            try
            {
                // ULTRA SAFE: Just show UI elements, NO COMMAND EXECUTION
                var mainSystemContent = this.FindName("MainSystemContent") as FrameworkElement;
                if (mainSystemContent != null)
                {
                    mainSystemContent.Visibility = Visibility.Visible;
                }

                // Hide welcome content (if exists)
                var welcomeContent = this.FindName("WelcomeContent") as FrameworkElement;
                if (welcomeContent != null)
                {
                    welcomeContent.Visibility = Visibility.Collapsed;
                }

                // VISIBILITY FIX: Ensure window is properly shown
                this.Topmost = true;
                this.Topmost = false; // Flash to front
                this.WindowState = WindowState.Normal;

                // Collapse/expand sidebar binding
                var toggle = this.FindName("SidebarToggle") as System.Windows.Controls.Primitives.ToggleButton;
                var leftCol = this.FindName("LeftNavColumn") as System.Windows.Controls.ColumnDefinition;
                var logoText = this.FindName("SidebarLogoText") as TextBlock;
                var logoImage = this.FindName("SidebarLogoImage") as Image;
                var navPanel = this.FindName("NavMenuPanel") as StackPanel;
                if (toggle != null && leftCol != null)
                {
                    toggle.Checked += (_, __) =>
                    {
                        leftCol.Width = new GridLength(72);
                        if (logoText != null) logoText.Visibility = Visibility.Collapsed;
                        if (logoImage != null) logoImage.Visibility = Visibility.Visible;
                        // shrink nav buttons vertically to avoid clipping
                        if (navPanel != null) navPanel.Margin = new Thickness(0, 8, 0, 8);
                    };
                    toggle.Unchecked += (_, __) =>
                    {
                        leftCol.Width = new GridLength(260);
                        if (logoText != null) logoText.Visibility = Visibility.Visible;
                        if (logoImage != null) logoImage.Visibility = Visibility.Collapsed;
                        if (navPanel != null) navPanel.Margin = new Thickness(0, 15, 0, 15);
                    };
                }

                // LOG SUCCESS - No blocking MessageBox
                GlobalLogger.Instance?.LogInfo("✅ MesTech Stok UI loaded successfully", "MainWindow");
                System.Diagnostics.Debug.WriteLine("✅ MesTech Stok UI loaded successfully");
                // Badge sayaçlarını ilk yüklemede güncelle
                _ = UpdateNavBadgesAsync();
                // RBAC: Sol menü görünürlüğünü kullanıcı izinlerine göre uygula
                _ = ApplyMenuVisibilityAsync();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Progressive load error: {ex.Message}");
                GlobalLogger.Instance?.LogError($"Progressive load error: {ex.Message}", "MainWindow");
            }
        }

        private async System.Threading.Tasks.Task ApplyMenuVisibilityAsync()
        {
            // TODO: Basit güvenlik kontrolü (gelecekte SimpleSecurityService ile entegre edilecek)
            // Şu anda tüm menüler görünür
            try
            {
                // Tüm menüleri görünür yap
                if (NavProducts != null) NavProducts.Visibility = Visibility.Visible;
                if (NavStock != null) NavStock.Visibility = Visibility.Visible;
                if (NavOrders != null) NavOrders.Visibility = Visibility.Visible;
                if (NavReports != null) NavReports.Visibility = Visibility.Visible;
                if (NavExports != null) NavExports.Visibility = Visibility.Visible;
                if (NavOpenCart != null) NavOpenCart.Visibility = Visibility.Visible;
                if (NavSystemResources != null) NavSystemResources.Visibility = Visibility.Visible;
                if (NavLogs != null) NavLogs.Visibility = Visibility.Visible;
                if (NavSettings != null) NavSettings.Visibility = Visibility.Visible;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyMenuVisibility error: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task UpdateNavBadgesAsync()
        {
            try
            {
                var sp = App.ServiceProvider;
                if (sp == null) return;
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Bekleyen sipariş sayısı
                var pendingOrders = await db.Orders.AsNoTracking()
                    .CountAsync(o => o.Status == MesTechStok.Core.Data.Models.OrderStatus.Pending);
                // Kritik stok sayısı
                var lowStock = await db.Products.AsNoTracking()
                    .CountAsync(p => p.IsActive && p.Stock <= p.MinimumStock);

                // NavOrders badge
                if (NavOrders != null)
                {
                    NavProperties.SetHasBadge(NavOrders, pendingOrders > 0);
                    NavProperties.SetBadgeText(NavOrders, pendingOrders > 0 ? pendingOrders.ToString() : string.Empty);
                }
                // NavReports badge (kritik stok)
                if (NavReports != null)
                {
                    NavProperties.SetHasBadge(NavReports, lowStock > 0);
                    NavProperties.SetBadgeText(NavReports, lowStock > 0 ? lowStock.ToString() : string.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateNavBadges error: {ex.Message}");
            }
        }

        private void InitializeMainSystemDirect()
        {
            try
            {
                // Ana sistemi direkt olarak başlat (Welcome mode bypass)
                _isWelcomeMode = false;

                // Welcome content'lerini gizle
                HideWelcomeElements();

                // Main system'i göster
                ShowMainSystemElements();

                // Dashboard'ı otomatik yükle
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowDashboardCommand.Execute(null);
                }

                GlobalLogger.Instance?.LogInfo("Ana sistem otomatik olarak başlatıldı", "MainWindow");

            }
            catch (Exception ex)
            {
                GlobalLogger.Instance?.LogError($"Ana sistem otomatik başlatma hatası: {ex.Message}", "MainWindow");
            }
        }

        private void InitializeWelcomeModeDirect()
        {
            try
            {
                // Welcome mode'u aktif et
                _isWelcomeMode = true;

                // Main system'i gizle
                HideMainSystemElements();

                // Welcome content'i göster
                ShowWelcomeElements();

                // Welcome mode'u tam olarak başlat
                InitializeWelcomeMode();

                // Try to load a higher quality logo for sidebar when collapsed
                try
                {
                    var logoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Logos", "logo-3d.png");
                    var logoImage = this.FindName("SidebarLogoImage") as Image;
                    if (logoImage != null && System.IO.File.Exists(logoPath))
                    {
                        logoImage.Source = new BitmapImage(new Uri(logoPath));
                    }
                }
                catch { }

                // Initialize ekran koruyucu (config'e bağlı)
                if (_screensaverEnabled)
                {
                    InitializeScreensaver();
                }

                GlobalLogger.Instance?.LogInfo("🏠 Karşılama sayfası başlatıldı", "MainWindow");

            }
            catch (Exception ex)
            {
                GlobalLogger.Instance?.LogError($"Welcome mode başlatma hatası: {ex.Message}", "MainWindow");
            }
        }

        private void HideWelcomeElements()
        {
            try
            {
                // Welcome content'lerini gizle
                var welcomeContent = this.FindName("WelcomeContent") as FrameworkElement;
                if (welcomeContent != null)
                {
                    welcomeContent.Visibility = Visibility.Collapsed;
                }

                // Deprecated inline elements (WelcomeBackgroundImage/WelcomeOverlay) are intentionally kept collapsed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Welcome elements gizleme hatası: {ex.Message}");
            }
        }

        private void ShowMainSystemElements()
        {
            try
            {
                // Ana system content'i göster
                var mainSystemContent = this.FindName("MainSystemContent") as FrameworkElement;
                if (mainSystemContent != null)
                {
                    mainSystemContent.Visibility = Visibility.Visible;
                }

                // Header'ı göster  
                var headerBar = this.FindName("HeaderBar") as FrameworkElement;
                if (headerBar != null)
                {
                    headerBar.Visibility = Visibility.Visible;
                }

                // Main content frame'i göster
                var mainFrame = this.FindName("MainContentFrame") as FrameworkElement;
                if (mainFrame != null)
                {
                    mainFrame.Visibility = Visibility.Visible;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Main system elements gösterme hatası: {ex.Message}");
            }
        }

        private void HideMainSystemElements()
        {
            try
            {
                // Ana system content'i gizle
                var mainSystemContent = this.FindName("MainSystemContent") as FrameworkElement;
                if (mainSystemContent != null)
                {
                    mainSystemContent.Visibility = Visibility.Collapsed;
                }

                // Header'ı gizle  
                var headerBar = this.FindName("HeaderBar") as FrameworkElement;
                if (headerBar != null)
                {
                    headerBar.Visibility = Visibility.Collapsed;
                }

                // Main content frame'i gizle
                var mainFrame = this.FindName("MainContentFrame") as FrameworkElement;
                if (mainFrame != null)
                {
                    mainFrame.Visibility = Visibility.Collapsed;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Main system elements gizleme hatası: {ex.Message}");
            }
        }

        private void ShowWelcomeElements()
        {
            try
            {
                // Welcome content'i göster
                var welcomeContent = this.FindName("WelcomeContent") as FrameworkElement;
                if (welcomeContent != null)
                {
                    welcomeContent.Visibility = Visibility.Visible;
                }

                // Deprecated inline elements (WelcomeBackgroundImage/WelcomeOverlay) are intentionally kept collapsed
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Welcome elements gösterme hatası: {ex.Message}");
            }
        }

        private void SetupWindow()
        {
            try
            {
                // Window ayarları
                this.MinWidth = 1200;
                this.MinHeight = 800;

                // Welcome mode aktifse ana content'i gizle
                var mainContentFrame = this.FindName("MainContentFrame") as FrameworkElement;
                if (mainContentFrame != null)
                {
                    mainContentFrame.Visibility = _isWelcomeMode ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Window setup hatası: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InitializeWelcomeMode()
        {
            try
            {
                // Welcome mode timer'ı başlat
                _welcomeClockTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _welcomeClockTimer.Tick += WelcomeClockTimer_Tick;
                _welcomeClockTimer.Start();

                // İlk güncelleme
                UpdateWelcomeClock();

                // Arka plan resimlerini yükle
                LoadBackgroundImages();
                SetRandomBackgroundImage();

                // Resim galerisini yükle
                LoadImageGallery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Welcome mode init error: {ex.Message}");
            }
        }

        private void LoadBackgroundImages()
        {
            try
            {
                // Images klasörünü oluştur
                var imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Mevcut resimleri yükle
                var imageFiles = Directory.GetFiles(imagesPath, "*.jpg")
                    .Concat(Directory.GetFiles(imagesPath, "*.jpeg"))
                    .Concat(Directory.GetFiles(imagesPath, "*.png"))
                    .Concat(Directory.GetFiles(imagesPath, "*.bmp"))
                    .ToArray();

                _backgroundImages.Clear();
                foreach (var imageFile in imageFiles)
                {
                    _backgroundImages.Add(imageFile);
                }

                // Eğer resim yoksa default gradient arka plan oluştur
                if (_backgroundImages.Count == 0)
                {
                    CreateDefaultBackground();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Background images load error: {ex.Message}");
                CreateDefaultBackground();
            }
        }

        private void CreateDefaultBackground()
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

                if (WelcomeBackgroundImage != null)
                {
                    WelcomeBackgroundImage.Source = null;
                }
                this.Background = gradientBrush;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Default background error: {ex.Message}");
                this.Background = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
            }
        }

        private void LoadImageGallery()
        {
            try
            {
                if (WelcomeImagePanel == null) return;

                WelcomeImagePanel.Children.Clear();

                foreach (var imagePath in _backgroundImages)
                {
                    var previewBorder = CreateImagePreview(imagePath);
                    WelcomeImagePanel.Children.Add(previewBorder);
                }

                // Upload placeholder ekle
                var uploadPlaceholder = CreateUploadPlaceholder();
                WelcomeImagePanel.Children.Add(uploadPlaceholder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery load error: {ex.Message}");
            }
        }

        private Border CreateImagePreview(string imagePath)
        {
            var border = new Border
            {
                Width = 200,
                Height = 120,
                CornerRadius = new CornerRadius(15),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(3),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            try
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(imagePath)),
                    Stretch = Stretch.UniformToFill
                };
                border.Child = image;

                border.MouseLeftButtonDown += (s, e) =>
                {
                    SetBackgroundImage(imagePath);
                };
            }
            catch
            {
                border.Background = new SolidColorBrush(Colors.Gray);
                border.Child = new TextBlock
                {
                    Text = "Resim\nYüklenemedi",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White),
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Bold
                };
            }

            return border;
        }

        private Border CreateUploadPlaceholder()
        {
            var border = new Border
            {
                Width = 200,
                Height = 120,
                CornerRadius = new CornerRadius(15),
                Margin = new Thickness(0, 0, 0, 15),
                BorderBrush = new SolidColorBrush(Colors.Orange),
                BorderThickness = new Thickness(3),
                Background = new SolidColorBrush(Color.FromArgb(120, 255, 152, 0)),
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
                FontSize = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Yeni Resim\nEkle",
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Segoe UI")
            });

            border.Child = stackPanel;
            border.MouseLeftButtonDown += (s, e) => UploadImageBtn_Click(s, e);

            return border;
        }

        private void SetBackgroundImage(string imagePath)
        {
            try
            {
                if (WelcomeBackgroundImage != null)
                {
                    var bitmap = new BitmapImage(new Uri(imagePath));
                    WelcomeBackgroundImage.Source = bitmap;
                    this.Background = Brushes.Transparent;
                }
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

        private void WelcomeClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateWelcomeClock();
        }

        private void UpdateWelcomeClock()
        {
            try
            {
                var now = DateTime.Now;

                if (WelcomeTimeDisplay != null)
                {
                    WelcomeTimeDisplay.Text = now.ToString("HH:mm:ss");
                }

                if (WelcomeDateDisplay != null)
                {
                    WelcomeDateDisplay.Text = now.ToString("dd MMMM yyyy, dddd",
                        new System.Globalization.CultureInfo("tr-TR"));
                }

                if (WelcomeTemperatureDisplay != null)
                {
                    // Simulate temperature (replace with real weather API)
                    var temp = _random.Next(18, 28);
                    WelcomeTemperatureDisplay.Text = $"{temp}°C";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Welcome clock update error: {ex.Message}");
            }
        }

        // Event Handlers
        private void EnterSystemBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Welcome mode'dan çık
                ExitWelcomeMode();

                // Ana sistemi göster
                ShowMainSystem();

                ToastManager.ShowSuccess("Ana sisteme geçildi!", "MesTech");
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Sistem geçişi hatası: {ex.Message}", "MesTech");
            }
        }

        private void ExitWelcomeMode()
        {
            try
            {
                _isWelcomeMode = false;

                // Welcome timer'ı durdur
                _welcomeClockTimer?.Stop();

                // Welcome content'i gizle
                var welcomeContent = this.FindName("WelcomeContent") as FrameworkElement;
                if (welcomeContent != null)
                {
                    welcomeContent.Visibility = Visibility.Collapsed;
                }

                var welcomeBackgroundImage = this.FindName("WelcomeBackgroundImage") as FrameworkElement;
                if (welcomeBackgroundImage != null)
                {
                    welcomeBackgroundImage.Visibility = Visibility.Collapsed;
                }

                var welcomeOverlay = this.FindName("WelcomeOverlay") as FrameworkElement;
                if (welcomeOverlay != null)
                {
                    welcomeOverlay.Visibility = Visibility.Collapsed;
                }

                // Header'ı göster
                var headerBar = this.FindName("HeaderBar") as FrameworkElement;
                if (headerBar != null)
                {
                    headerBar.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exit welcome mode error: {ex.Message}");
            }
        }

        private void ShowMainSystem()
        {
            try
            {
                // Ana system content'i göster (sol menü + content area)
                var mainSystemContent = this.FindName("MainSystemContent") as FrameworkElement;
                if (mainSystemContent != null)
                {
                    mainSystemContent.Visibility = Visibility.Visible;
                    ToastManager.ShowInfo($"MainSystemContent Visibility: {mainSystemContent.Visibility}", "Debug");
                }

                // Frame'i kontrol et
                var mainFrame = this.FindName("MainContentFrame") as FrameworkElement;
                if (mainFrame != null)
                {
                    ToastManager.ShowInfo($"MainContentFrame bulundu! Visibility: {mainFrame.Visibility}, Width: {mainFrame.ActualWidth}, Height: {mainFrame.ActualHeight}", "Debug Frame");
                }
                else
                {
                    ToastManager.ShowError("MainContentFrame bulunamadı!", "Debug Frame");
                }

                // Normal arka plan
                this.Background = new SolidColorBrush(Color.FromRgb(245, 247, 250));

                // Dashboard'ı göster - DataContext kullan
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowDashboardCommand.Execute(null);
                }

                ToastManager.ShowInfo("Sol menüden farklı bölümlere erişebilirsiniz", "Navigasyon");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Show main system error: {ex.Message}");
                ToastManager.ShowError($"Ana sistem yükleme hatası: {ex.Message}", "Sistem");
            }
        }

        private void UploadImageBtn_Click(object sender, RoutedEventArgs e)
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
                    var imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    int addedCount = 0;

                    foreach (var fileName in openFileDialog.FileNames)
                    {
                        try
                        {
                            var destFileName = Path.Combine(imagesPath, Path.GetFileName(fileName));

                            if (!File.Exists(destFileName))
                            {
                                File.Copy(fileName, destFileName);
                                _backgroundImages.Add(destFileName);
                                addedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            ToastManager.ShowWarning($"Resim kopyalama hatası: {ex.Message}", "Resim");
                        }
                    }

                    if (addedCount > 0)
                    {
                        LoadImageGallery();
                        ToastManager.ShowSuccess($"{addedCount} resim başarıyla eklendi!", "Galeri");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Resim yükleme hatası: {ex.Message}", "Galeri");
            }
        }

        private void NextImageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backgroundImages.Count > 0)
                {
                    _currentImageIndex = (_currentImageIndex + 1) % _backgroundImages.Count;
                    SetBackgroundImage(_backgroundImages[_currentImageIndex]);
                    ToastManager.ShowInfo("Sonraki resim seçildi", "Galeri");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Resim değiştirme hatası: {ex.Message}", "Galeri");
            }
        }

        private void RandomImageBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetRandomBackgroundImage();
                ToastManager.ShowInfo("Rastgele resim seçildi", "Galeri");
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Rastgele resim hatası: {ex.Message}", "Galeri");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Ana sayfa yüklendiğinde çalışacak kod
            ShowWelcomeMessage();

            // Saat ve hava durumu widget'ını başlat
            InitializeWeatherClockWidget();
        }

        private void ShowWelcomeMessage()
        {
            try
            {
                // Welcome message artık popup olarak gösterilmiyor
                UpdateStatusBar("🚀 MesChain Stok Yönetimi 2.0 Hazır - Stok Takip Sistemi Aktif");
            }
            catch (Exception ex)
            {
                UpdateStatusBar($"Başlatma hatası: {ex.Message}");
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tab değiştiğinde gerekli işlemler
            if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
            {
                var tabName = selectedTab.Name ?? "Bilinmeyen";
                UpdateStatusBar($"Aktif sekme: {tabName}");
            }
        }

        private void UpdateStatusBar(string message)
        {
            // Status bar güncelleme
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.StatusMessage = message;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.RefreshCommand.Execute(null);
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ayarlar penceresi henüz geliştirilmekte...", "Bilgi",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var helpMessage = "MesChain Stok Yönetimi 2.0\n\n" +
                            "Sürüm: 2.0.0 PROFESSIONAL\n" +
                            "Geliştirici: MesChain\n\n" +
                            "✅ Özellikler:\n• 📊 Gerçek zamanlı stok takibi\n• 📱 Barkod okuma sistemi\n• 🛡️ Ekran koruyucu (3 dk)\n• 🔍 Log takip sistemi\n\n" +
                            "Stok yönetimi için 'Ürünler' sekmesini kullanın.";

            MessageBox.Show(helpMessage, "MesChain Stok Yönetimi 2.0 - Yardım",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void VersionProLink_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // Log Takip ekranına geç ve "V2.0 Pro Güncelleme Günlüğü" modunu aç
                if (DataContext is MainViewModel vm)
                {
                    vm.ShowLogsCommand.Execute(null);
                }
                GlobalLogger.Instance?.LogInfo($"V2.0 PRO değişiklik günlüğü açıldı - {DateTime.Now:yyyy-MM-dd HH:mm:ss}", "MainWindow");
                MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("CHANGELOG", $"V2.0 Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss} - Degisiklik gunlugu goruntulendi", "Version");
                ToastManager.ShowInfo("V2.0 PRO - Değişiklik Günlüğü açıldı", "Versiyon");
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Değişiklik günlüğü açılırken hata: {ex.Message}", "Versiyon");
            }
        }

        // Navigation Menu Event Handlers
        private void NavDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Dashboard yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowDashboardCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavDashboard });
                    UpdateStatusBar("📊 Dashboard yüklendi");

                    // Debug: View gerçekten değişti mi?
                    if (viewModel.CurrentView != null)
                    {
                        ToastManager.ShowSuccess($"Dashboard aktif: {viewModel.CurrentView.GetType().Name}", "Debug");
                    }
                    else
                    {
                        ToastManager.ShowError("CurrentView null!", "Debug");
                    }
                }
                else
                {
                    ToastManager.ShowError("DataContext MainViewModel değil!", "Debug");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Dashboard hatası: {ex.Message}", "Hata");
            }
        }

        private void NavProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Ürünler yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowProductsCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavProducts });
                    UpdateStatusBar("📦 Ürün Yönetimi yüklendi");

                    // Debug: View gerçekten değişti mi?
                    if (viewModel.CurrentView != null)
                    {
                        ToastManager.ShowSuccess($"Ürünler aktif: {viewModel.CurrentView.GetType().Name}", "Debug");
                    }
                    else
                    {
                        ToastManager.ShowError("CurrentView null!", "Debug");
                    }
                }
                else
                {
                    ToastManager.ShowError("DataContext MainViewModel değil!", "Debug");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Ürün yönetimi hatası: {ex.Message}", "Hata");
            }
        }

        private void NavStock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Stok takibi yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowStockCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavStock });
                    UpdateStatusBar("📊 Stok Takibi yüklendi");
                    ToastManager.ShowSuccess("Stok takip sistemi aktif!", "Stok");

                    // Debug: View gerçekten değişti mi?
                    if (viewModel.CurrentView != null)
                    {
                        ToastManager.ShowInfo($"Stok view: {viewModel.CurrentView.GetType().Name}", "Debug");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Stok takibi hatası: {ex.Message}", "Hata");
            }
        }

        private void NavOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowOrdersCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavOrders });
                    UpdateStatusBar("📋 Sipariş Yönetimi yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Sipariş yönetimi hatası: {ex.Message}", "Hata");
            }
        }

        private void NavCustomers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowCustomersCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavCustomers });
                    UpdateStatusBar("👥 Müşteri Yönetimi yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Müşteri yönetimi hatası: {ex.Message}", "Hata");
            }
        }

        private void NavBarcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowBarcodeCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavBarcode });
                    UpdateStatusBar("📱 Barkod Okuyucu yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Barkod okuyucu hatası: {ex.Message}", "Hata");
            }
        }

        private void NavReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowReportsCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavReports });
                    UpdateStatusBar("📊 Raporlar yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Raporlar hatası: {ex.Message}", "Hata");
            }
        }

        // STOK YERLEŞİM SİSTEMİ Click Event Handler'ları
        private void NavStockPlacement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 STOK YERLEŞİM SİSTEMİ yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowStockPlacementCommand.Execute(null);
                    // SetActiveNav(sender as Button, new[] { NavStockPlacement }); // DISABLED - Button not found in XAML
                    UpdateStatusBar("📍 STOK YERLEŞİM SİSTEMİ yüklendi");
                    ToastManager.ShowSuccess("STOK YERLEŞİM SİSTEMİ aktif!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"STOK YERLEŞİM SİSTEMİ hatası: {ex.Message}", "Hata");
            }
        }

        private void NavWarehouseManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Depo Yönetimi yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowWarehouseManagementCommand.Execute(null);
                    // SetActiveNav(sender as Button, new[] { NavWarehouseManagement }); // DISABLED - Button not found in XAML
                    UpdateStatusBar("🏢 Depo Yönetimi yüklendi");
                    ToastManager.ShowSuccess("Depo Yönetimi aktif!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Depo Yönetimi hatası: {ex.Message}", "Hata");
            }
        }

        private void NavLocationTracking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Konum Takibi yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowLocationTrackingCommand.Execute(null);
                    // SetActiveNav(sender as Button, new[] { NavLocationTracking }); // DISABLED - Button not found in XAML
                    UpdateStatusBar("🎯 Konum Takibi yüklendi");
                    ToastManager.ShowSuccess("Konum Takibi aktif!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Konum Takibi hatası: {ex.Message}", "Hata");
            }
        }

        private void NavWarehouseMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Depo Haritası yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowWarehouseMapCommand.Execute(null);
                    // SetActiveNav(sender as Button, new[] { NavWarehouseMap }); // DISABLED - Button not found in XAML
                    UpdateStatusBar("🗺️ Depo Haritası yüklendi");
                    ToastManager.ShowSuccess("Depo Haritası aktif!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Depo Haritası hatası: {ex.Message}", "Hata");
            }
        }

        private void NavMobileWarehouse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Mobil Depo yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowMobileWarehouseCommand.Execute(null);
                    // SetActiveNav(sender as Button, new[] { NavMobileWarehouse }); // DISABLED - Button not found in XAML
                    UpdateStatusBar("📱 Mobil Depo yüklendi");
                    ToastManager.ShowSuccess("Mobil Depo aktif!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Mobil Depo hatası: {ex.Message}", "Hata");
            }
        }

        private void NavLocationReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusBar("🔄 Konum Raporları yükleniyor...");

                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowLocationReportsCommand.Execute(null);
                    // SetActiveNav(sender as Button, new[] { NavLocationReports }); // DISABLED - Button not found in XAML
                    UpdateStatusBar("📋 Konum Raporları yüklendi");
                    ToastManager.ShowSuccess("Konum Raporları aktif!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Konum Raporları hatası: {ex.Message}", "Hata");
            }
        }

        private void NavExports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowExportsCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavExports });
                    UpdateStatusBar("📤 Dışa Aktarma yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Dışa aktarma hatası: {ex.Message}", "Hata");
            }
        }

        private void NavOpenCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowOpenCartCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavOpenCart });
                    UpdateStatusBar("🌐 OpenCart Entegrasyonu yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"OpenCart entegrasyon hatası: {ex.Message}", "Hata");
            }
        }

        private void NavSystemResources_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowSystemResourcesCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavSystemResources });
                    UpdateStatusBar("⚡ Sistem Kaynakları yüklendi");
                    ToastManager.ShowSuccess("🚀 Sistem Kaynakları entegre modülü açıldı!", "MesTech");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Sistem Kaynakları hatası: {ex.Message}", "Hata");
            }
        }

        private void NavLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowLogsCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavLogs });
                    UpdateStatusBar("🔍 Log Takip yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Log takip hatası: {ex.Message}", "Hata");
            }
        }

        private void NavSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowSettingsCommand.Execute(null);
                    SetActiveNav(sender as Button, new[] { NavSettings });
                    UpdateStatusBar("⚙️ Ayarlar yüklendi");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Ayarlar hatası: {ex.Message}", "Hata");
            }
        }

        private void NavReturnToWelcome_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Doğru ekran koruyucu/karşılama: tekil WelcomeWindow örneğini yeniden göster
                if (App.WelcomeWindowInstance == null)
                {
                    App.WelcomeWindowInstance = new Views.WelcomeWindow(this);
                }
                var welcomeWindow = App.WelcomeWindowInstance;
                if (!welcomeWindow.IsVisible) welcomeWindow.Show();
                else welcomeWindow.Activate();
                // Ana pencereyi geçici olarak gizle (kapatma yok)
                this.Hide();

                ToastManager.ShowInfo("Ana karşılama ekranına dönüldü", "MesTech Stok");
                GlobalLogger.Instance?.LogInfo("Kullanıcı ana karşılama ekranına döndü (WelcomeWindow)", "Navigation");
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Ana ekran hatası: {ex.Message}", "Navigation");
                System.Diagnostics.Debug.WriteLine($"Return to welcome error: {ex.Message}");
            }
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: Basit güvenlik sistemi kullanılıyor (SimpleSecurityService)
                // Şu anda basit logout yapılıyor

                // 1) Basit logout
                var sp = App.ServiceProvider;
                var simpleSecurity = sp?.GetService<SimpleSecurityService>();
                if (simpleSecurity != null)
                {
                    // TODO: Session ID'yi saklamak gerekiyor
                    // simpleSecurity.LogoutAsync(sessionId);
                }

                // 2) Login ekranına dön
                var login = new MesTechStok.Desktop.Views.LoginWindow();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Çıkış hata: {ex.Message}", "Oturum");
            }
        }

        // Aktif menü şeridi kontrolü ve badge güncelleme
        private void SetActiveNav(Button? clicked, Button[] all)
        {
            try
            {
                // Tüm nav butonlarını pasif işaretle
                var navButtons = new[]
                {
                    NavDashboard, NavProducts, NavStock, NavOrders, NavCustomers,
                    NavBarcode, NavReports, NavExports, NavOpenCart, NavSystemResources,
                    NavLogs, NavSettings, NavReturnToWelcome
                };

                foreach (var btn in navButtons)
                {
                    if (btn == null) continue;
                    // Attached property: NavProperties.IsActive = false
                    NavProperties.SetIsActive(btn, false);
                }

                if (clicked != null)
                {
                    NavProperties.SetIsActive(clicked, true);
                }

                // Örnek: Raporlar için badge sayacı (ileride gerçek veri ile beslenecek)
                _ = UpdateNavBadgesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetActiveNav error: {ex.Message}");
            }
        }

        private void InitializeWeatherClockWidget()
        {
            try
            {
                // Weather Clock Widget'ını bulup başlat
                var weatherWidget = this.FindName("WeatherClockWidget");
                if (weatherWidget != null)
                {
                    // Widget'ın z-index'ini en üste getir
                    Panel.SetZIndex((UIElement)weatherWidget, 1000);

                    // Widget başlatıldı mesajı
                    UpdateStatusBar("🌡️ Hava Durumu ve Saat Widget'ı aktif edildi");
                }
                else
                {
                    UpdateStatusBar("⚠️ Hava Durumu Widget'ı bulunamadı");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Widget başlatma hatası: {ex.Message}", "Widget Hatası",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void InitializeToastManager()
        {
            // Bu metod zaten ToastManager event'ini dinliyor
        }

        private void OnToastRequested(object? sender, ToastEventArgs e)
        {
            var toastItem = new ToastItem
            {
                Message = e.Message,
                Type = e.Type,
                Title = e.Title
            };

            _toastItems.Add(toastItem);

            // Toast'u bir süre sonra kaldır
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timer.Tick += (s, args) =>
            {
                _toastItems.Remove(toastItem);
                timer.Stop();
            };
            timer.Start();
        }

        private void SetupGlobalExceptionHandling()
        {
            try
            {
                // Global application exception handling
                Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    var errorMsg = $"Uygulama hatası: {e.Exception.Message}";
                    GlobalLogger.Instance.LogError($"Genel Uygulama Hatası: {e.Exception.Message}\nStack: {e.Exception.StackTrace}", "Application");

                    // Toast bildirimi göster
                    ToastManager.ShowError(errorMsg, "Application");

                    e.Handled = true; // Uygulamanın çökmesini engelle
                };

                // AppDomain exception handling
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    if (e.ExceptionObject is Exception ex)
                    {
                        var errorMsg = $"Sistem hatası: {ex.Message}";
                        GlobalLogger.Instance.LogError($"AppDomain Hatası: {ex.Message}\nStack: {ex.StackTrace}", "AppDomain");
                        ToastManager.ShowError(errorMsg, "AppDomain");
                    }
                };

                GlobalLogger.Instance.LogInfo("Global exception handling sistemi kuruldu", "MainWindow");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception handling kurulum hatası: {ex.Message}", "Kritik Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ======================== EKRAN KORUYUCU SİSTEMİ ========================

        private void InitializeScreensaver()
        {
            try
            {
                _lastActivityTime = DateTime.Now;
                _lastMousePosition = new Point(-1, -1);

                // Idle timer - her 30 saniyede kontrol et
                _idleTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(30)
                };
                _idleTimer.Tick += IdleTimer_Tick;
                _idleTimer.Start();

                // Screensaver image rotation timer
                _screensaverImageTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(15) // Her 15 saniyede resim değiştir
                };
                _screensaverImageTimer.Tick += ScreensaverImageTimer_Tick;

                // Activity monitoring
                this.MouseMove += MainWindow_MouseMove;
                this.KeyDown += MainWindow_KeyDown;
                this.MouseDown += MainWindow_MouseDown;

                GlobalLogger.Instance?.LogInfo("Ekran koruyucu sistemi başlatıldı", "Screensaver");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Screensaver init error: {ex.Message}");
            }
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var idleTime = DateTime.Now - _lastActivityTime;

                if (idleTime >= _idleTimeout && !_isScreensaverActive && !_isWelcomeMode)
                {
                    // Ekran koruyucuyu aktif et
                    ActivateScreensaver();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Idle timer error: {ex.Message}");
            }
        }

        private void ScreensaverImageTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (_isScreensaverActive)
                {
                    SetRandomBackgroundImage();

                    // Screensaver aktifken sıcaklığı güncelle
                    if (WelcomeTemperatureDisplay != null)
                    {
                        var temp = _random.Next(15, 32);
                        WelcomeTemperatureDisplay.Text = $"{temp}°C";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Screensaver image timer error: {ex.Message}");
            }
        }

        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                var currentPos = e.GetPosition(this);

                // Fare hareket etmişse aktiviteyi kaydet
                if (_lastMousePosition.X >= 0 &&
                    (Math.Abs(currentPos.X - _lastMousePosition.X) > 5 ||
                     Math.Abs(currentPos.Y - _lastMousePosition.Y) > 5))
                {
                    RegisterActivity();
                }

                _lastMousePosition = currentPos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Mouse move error: {ex.Message}");
            }
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            RegisterActivity();

            // F11 - Fullscreen Toggle
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            // ESC - Exit Fullscreen
            else if (e.Key == Key.Escape && this.WindowStyle == WindowStyle.None)
            {
                ExitFullscreen();
                e.Handled = true;
            }
            // Ctrl+D - Dashboard
            else if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowDashboardCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Ctrl+P - Products
            else if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.ShowProductsCommand.Execute(null);
                }
                e.Handled = true;
            }
            // Ctrl+O - Orders (Optimized)
            else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (DataContext is MainViewModel viewModel)
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    viewModel.ShowOrdersCommand.Execute(null);
                    stopwatch.Stop();
                    ToastManager.ShowInfo($"⚡ Hızlı sipariş açılışı: {stopwatch.ElapsedMilliseconds}ms", "Performans");
                }
                e.Handled = true;
            }
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RegisterActivity();
        }

        private void RegisterActivity()
        {
            try
            {
                _lastActivityTime = DateTime.Now;

                // Eğer screensaver aktifse, deaktif et
                if (_isScreensaverActive)
                {
                    DeactivateScreensaver();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Register activity error: {ex.Message}");
            }
        }

        private void ActivateScreensaver()
        {
            try
            {
                _isScreensaverActive = true;

                // Welcome mode'a geç
                ShowWelcomeMode();

                // Resim rotasyonunu başlat
                _screensaverImageTimer?.Start();

                // Toast bildirimi göster
                ToastManager.ShowInfo("Ekran koruyucu etkinleştirildi", "MesChain");

                GlobalLogger.Instance?.LogInfo("Ekran koruyucu etkinleştirildi", "Screensaver");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Activate screensaver error: {ex.Message}");
            }
        }

        private void DeactivateScreensaver()
        {
            try
            {
                if (!_isScreensaverActive) return;

                _isScreensaverActive = false;

                // Resim rotasyonunu durdur
                _screensaverImageTimer?.Stop();

                // Toast bildirimi göster
                ToastManager.ShowSuccess("Sistem etkin - Hoş geldiniz!", "MesChain");

                GlobalLogger.Instance?.LogInfo("Ekran koruyucu deaktif edildi", "Screensaver");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Deactivate screensaver error: {ex.Message}");
            }
        }

        private void ShowWelcomeMode()
        {
            try
            {
                // Entegre (gömülü) welcome yerine ayrı WelcomeWindow kullan
                var welcomeWindow = new Views.WelcomeWindow(this);
                welcomeWindow.Show();
                welcomeWindow.Activate();
                this.Hide();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Show welcome mode error: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Tüm timer'ları durdur
                _welcomeClockTimer?.Stop();
                _idleTimer?.Stop();
                _screensaverImageTimer?.Stop();

                GlobalLogger.Instance?.LogInfo("Uygulama kapatıldı - Tüm timer'lar durduruldu", "MainWindow");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        public void ShowToastNotification(string message, string type)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "success":
                        ToastManager.ShowSuccess(message, "MesChain");
                        break;
                    case "error":
                        ToastManager.ShowError(message, "MesChain");
                        break;
                    case "warning":
                        ToastManager.ShowWarning(message, "MesChain");
                        break;
                    case "info":
                    default:
                        ToastManager.ShowInfo(message, "MesChain");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Toast notification error: {ex.Message}");
            }
        }

        #region Gallery Panel Management

        private void InitializeGalleryPanel()
        {
            try
            {
                // Gallery panelini başlat
                LoadGalleryImages();

                // Panel başlangıçta kapalı
                _isGalleryOpen = false;

                ToastManager.ShowInfo("🖼️ Arka Plan Galerisi hazır!", "Galeri");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery panel init error: {ex.Message}");
            }
        }

        private void LoadGalleryImages()
        {
            try
            {
                // Gallery removed (no-op)
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gallery images load error: {ex.Message}");
            }
        }

        private Border CreateGalleryImagePreview(string imagePath)
        {
            var border = new Border
            {
                Width = 140,
                Height = 90,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(5),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3)),
                BorderThickness = new Thickness(2),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            try
            {
                var image = new Image
                {
                    Source = new BitmapImage(new Uri(imagePath)),
                    Stretch = Stretch.UniformToFill
                };
                border.Child = image;

                // Tooltip ekle
                border.ToolTip = Path.GetFileName(imagePath);

                // Double click event
                border.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                    {
                        SetMainBackgroundImage(imagePath);
                        ToastManager.ShowSuccess($"Arka plan değiştirildi: {Path.GetFileName(imagePath)}", "Galeri");
                    }
                };
            }
            catch
            {
                border.Background = new SolidColorBrush(Colors.Gray);
                border.Child = new TextBlock
                {
                    Text = "❌",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 20
                };
            }

            return border;
        }

        private Border CreateGalleryPlaceholder()
        {
            var border = new Border
            {
                Width = 140,
                Height = 90,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(5),
                BorderBrush = new SolidColorBrush(Colors.Orange),
                BorderThickness = new Thickness(2),
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 152, 0)),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var textBlock = new TextBlock
            {
                Text = "📁\nResim Yok",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.White),
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };

            border.Child = textBlock;
            // Gallery removed: no click handler

            return border;
        }

        private void SetMainBackgroundImage(string imagePath)
        {
            try
            {
                // Ana sistem arka planını değiştir
                var bitmap = new BitmapImage(new Uri(imagePath));
                this.Background = new ImageBrush(bitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Main background set error: {ex.Message}");
                ToastManager.ShowError($"Arka plan ayarlanamadı: {ex.Message}", "Galeri");
            }
        }

        // Gallery removed

        // Gallery removed

        // Gallery removed

        // Gallery removed

        private void UploadBackgroundBtn_Click(object sender, RoutedEventArgs e)
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
                    var imagesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    int addedCount = 0;

                    foreach (var fileName in openFileDialog.FileNames)
                    {
                        try
                        {
                            var destFileName = Path.Combine(imagesPath, Path.GetFileName(fileName));

                            if (!File.Exists(destFileName))
                            {
                                File.Copy(fileName, destFileName);
                                _backgroundImages.Add(destFileName);
                                addedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"File copy error: {ex.Message}");
                        }
                    }

                    if (addedCount > 0)
                    {
                        LoadGalleryImages();
                        LoadImageGallery(); // Welcome mode gallery'sini de güncelle
                        ToastManager.ShowSuccess($"{addedCount} resim eklendi!", "Galeri");
                    }
                    else
                    {
                        ToastManager.ShowWarning("Hiç yeni resim eklenmedi (dosyalar zaten mevcut)", "Galeri");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload background error: {ex.Message}");
                ToastManager.ShowError($"Resim yükleme hatası: {ex.Message}", "Galeri");
            }
        }

        private void RandomBackgroundBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_backgroundImages.Count > 0)
                {
                    var randomIndex = _random.Next(_backgroundImages.Count);
                    var randomImage = _backgroundImages[randomIndex];

                    SetMainBackgroundImage(randomImage);

                    ToastManager.ShowSuccess($"Rastgele arka plan: {Path.GetFileName(randomImage)}", "Galeri");
                }
                else
                {
                    ToastManager.ShowWarning("Galerede resim bulunamadı. Önce resim yükleyin!", "Galeri");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Random background error: {ex.Message}");
                ToastManager.ShowError($"Rastgele arka plan hatası: {ex.Message}", "Galeri");
            }
        }

        #endregion

        private void TriggerApplicationReady()
        {
            try
            {
                // Application monitoring service'e uygulama hazır olduğunu bildir
                var serviceProvider = App.ServiceProvider;
                if (serviceProvider != null)
                {
                    var appMonitoring = serviceProvider.GetService<IApplicationMonitoringService>();
                    if (appMonitoring != null)
                    {
                        appMonitoring.RecordApplicationReady();
                        GlobalLogger.Instance?.LogInfo("✅ MainWindow hazır - Application ready event tetiklendi", "MainWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance?.LogError($"Application ready trigger error: {ex.Message}", "MainWindow");
            }
        }

        private void ToggleFullscreen()
        {
            try
            {
                if (this.WindowStyle == WindowStyle.None && this.WindowState == WindowState.Maximized)
                {
                    // Exit fullscreen
                    ExitFullscreen();
                    ToastManager.ShowInfo("🖥️ Normal ekran moduna geçildi", "Ekran Modu");
                }
                else
                {
                    // Enter fullscreen
                    EnterFullscreen();
                    ToastManager.ShowInfo("📺 Tam ekran moduna geçildi (ESC ile çıkış)", "Ekran Modu");
                }
            }
            catch (Exception ex)
            {
                ToastManager.ShowError($"Ekran modu değiştirilirken hata: {ex.Message}", "Hata");
            }
        }

        private void EnterFullscreen()
        {
            try
            {
                // Save current state
                _previousWindowState = this.WindowState;
                _previousWindowStyle = this.WindowStyle;

                // Set fullscreen
                this.WindowStyle = WindowStyle.None;
                this.WindowState = WindowState.Maximized;
                this.Topmost = true;

                GlobalLogger.Instance?.LogInfo("Tam ekran moduna geçildi", "MainWindow");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance?.LogError($"Tam ekran modu hatası: {ex.Message}", "MainWindow");
            }
        }

        private void ExitFullscreen()
        {
            try
            {
                // Restore previous state
                this.Topmost = false;
                this.WindowStyle = _previousWindowStyle;
                this.WindowState = _previousWindowState;

                GlobalLogger.Instance?.LogInfo("Normal ekran moduna dönüldü", "MainWindow");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance?.LogError($"Normal ekran modu hatası: {ex.Message}", "MainWindow");
            }
        }

        // Password Authentication Methods
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string enteredPassword = PasswordBox.Password;

                if (string.IsNullOrEmpty(enteredPassword))
                {
                    ShowLoginError("Lütfen şifrenizi giriniz");
                    return;
                }

                if (enteredPassword == MASTER_PASSWORD)
                {
                    // Şifre doğru - sistemi aç
                    _isAuthenticated = true;
                    HidePasswordOverlay();

                    // Ana sistemi başlat
                    if (_isWelcomeMode)
                    {
                        InitializeWelcomeModeDirect();
                    }
                    else
                    {
                        InitializeMainSystemDirect();
                    }
                }
                else
                {
                    // Yanlış şifre
                    ShowLoginError("Hatalı şifre! Tekrar deneyiniz.");
                    PasswordBox.Password = "";
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowLoginError("Giriş hatası oluştu");
                GlobalLogger.Instance?.LogError($"Login error: {ex.Message}", "MainWindow");
            }
        }

        private void ShowLoginError(string message)
        {
            if (ErrorMessage != null)
            {
                ErrorMessage.Text = message;
                ErrorMessage.Visibility = Visibility.Visible;

                // 3 saniye sonra hata mesajını gizle
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(3);
                timer.Tick += (s, e) =>
                {
                    ErrorMessage.Visibility = Visibility.Collapsed;
                    timer.Stop();
                };
                timer.Start();
            }
        }

        private void HidePasswordOverlay()
        {
            if (PasswordOverlay != null)
            {
                PasswordOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowPasswordOverlay()
        {
            if (PasswordOverlay != null)
            {
                PasswordOverlay.Visibility = Visibility.Visible;
                PasswordBox?.Focus();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Enter tuşu ile login
            if (e.Key == Key.Enter && PasswordOverlay?.Visibility == Visibility.Visible)
            {
                LoginButton_Click(this, new RoutedEventArgs());
            }
            base.OnKeyDown(e);
        }
    }

    // Bu sınıflar MainWindow.xaml.cs dışına, kendi dosyasına taşınmalı ama geçici olarak burada
    public class ToastItem
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info"; // "Info", "Success", "Warning", "Error"
    }

    public class ToastEventArgs : EventArgs
    {
        public string Message { get; }
        public string Title { get; }
        public string Type { get; }

        public ToastEventArgs(string message, string title, string type)
        {
            Message = message;
            Title = title;
            Type = type;
        }
    }

    // Password Authentication Methods - MOVED INSIDE MainWindow CLASS
}
