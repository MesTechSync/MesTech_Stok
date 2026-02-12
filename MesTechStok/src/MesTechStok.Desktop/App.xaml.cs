using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Interop;

// ALPHA TEAM FIX: Core services integration
using MesTechStok.Core.Services.Abstract;
using MesTechStok.Core.Services.Concrete;
using MesTechStok.Core.Data;
using MesTechStok.Core.Integrations.OpenCart;

// Desktop services
using MesTechStok.Desktop.Services;
using MesTechStok.Core.Interfaces;

// 🧠 NEURAL SYSTEM INTEGRATION
using MesTechStok.Desktop.Neural.Windows;
using MesTechStok.Desktop.Neural.Themes;
using MesTechStok.Desktop.ViewModels;
using MesTechStok.Desktop.Views; // Other views
using GlobalLogger = MesTechStok.Desktop.Utils.GlobalLogger;
// duplicate using removed

namespace MesTechStok.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// ALPHA TEAM: Core integration completed
/// EMERGENCY FIX: Configuration path correction
/// </summary>
public partial class App : Application
{
    private IHost? _host;
    public static IServiceProvider? ServiceProvider { get; private set; }
    public static WelcomeWindow? WelcomeWindowInstance { get; set; }
    private static Mutex? _singleInstanceMutex;
    private static EventWaitHandle? _activateEvent;

    // Win32 helpers to reliably bring window to foreground
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    private static void BringAppToForeground()
    {
        try
        {
            // Prefer WelcomeWindow if available, else MainWindow, else any visible window
            var target = (Window?)WelcomeWindowInstance ?? Application.Current.MainWindow ?? Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsVisible);
            if (target == null)
                return;

            if (target.WindowState == WindowState.Minimized)
                target.WindowState = WindowState.Normal;

            // Ensure handle and use Win32 to foreground
            var handle = new WindowInteropHelper(target).EnsureHandle();
            ShowWindow(handle, SW_RESTORE);
            // Topmost toggle helps on some shells
            var oldTopMost = target.Topmost;
            target.Topmost = true;
            target.Topmost = oldTopMost;
            target.Show();
            target.Activate();
            target.Focus();
            SetForegroundWindow(handle);
        }
        catch { /* ignore */ }
    }

    private static void StartActivationListener()
    {
        try
        {
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\MesTechStok.Desktop.Activate");
            var thread = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        _activateEvent!.WaitOne();
                        Application.Current?.Dispatcher?.Invoke(BringAppToForeground);
                    }
                }
                catch { /* exit loop on dispose */ }
            })
            { IsBackground = true, Name = "ActivationListener" };
            thread.Start();
        }
        catch { /* ignore */ }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global Exception Handlers
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Prevent multiple instances (stale EXE confusion / file locks)
        var createdNew = false;
        try
        {
            _singleInstanceMutex = new Mutex(true, "Global\\MesTechStok.Desktop.SingleInstance", out createdNew);
        }
        catch { /* ignore and proceed */ }
        if (!createdNew)
        {
            // Signal running instance to bring itself to foreground, then exit silently
            try
            {
                var evt = EventWaitHandle.OpenExisting("Global\\MesTechStok.Desktop.Activate");
                evt.Set();
            }
            catch
            {
                // Fallback: try to bring any existing process main window to front
                try
                {
                    var current = Process.GetCurrentProcess();
                    var procs = Process.GetProcessesByName(current.ProcessName)
                        .Where(p => p.Id != current.Id);
                    foreach (var p in procs)
                    {
                        var h = p.MainWindowHandle;
                        if (h != IntPtr.Zero)
                        {
                            ShowWindow(h, SW_RESTORE);
                            SetForegroundWindow(h);
                            break;
                        }
                    }
                }
                catch { }
            }
            Shutdown(0);
            return;
        }

        // ALPHA TEAM: Configure Serilog first - UTF-8 ENCODING FIX
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDir);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.With(new MesTechStok.Desktop.Diagnostics.CorrelationIdEnricher())
            .WriteTo.File(
                path: Path.Combine(logDir, "mestech-.log"),
                rollingInterval: Serilog.RollingInterval.Day,
                retainedFileCountLimit: 30,
                encoding: System.Text.Encoding.UTF8,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] (CorrId={CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}")
            // ✅ DOS PENCERE FIX: Console output kaldırıldı
            .CreateLogger();

        try
        {
            // EMERGENCY FIX: Set working directory to application directory
            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(appDir))
            {
                Directory.SetCurrentDirectory(appDir);
            }

            // AUTO-START LOGGING: Initialize GlobalLogger at startup
            GlobalLogger.Instance.LogInfo("🚀 MesTech Stok Takip Sistemi başlatıldı", "Application");
            GlobalLogger.Instance.LogInfo($"📂 Çalışma dizini: {Directory.GetCurrentDirectory()}", "Application");
            GlobalLogger.Instance.LogInfo("🔧 Sistem hazırlığı başlatılıyor...", "Application");

            // Diagnostic: log executable path and PID to avoid shortcut/instance confusion
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "<unknown>";
                var pid = Process.GetCurrentProcess().Id;
                Log.Information("Startup EXE: {ExePath} (PID={Pid})", exePath, pid);
                GlobalLogger.Instance.LogInfo($"🆔 EXE: {exePath} | PID: {pid}", "Application");
            }
            catch { }

            // 30 günlük log temizliği (günlük dosyaları)
            try
            {
                var logFiles = Directory.GetFiles(logDir, "mestech-*.log");
                var threshold = DateTime.Now.AddDays(-30);
                foreach (var lf in logFiles)
                {
                    try
                    {
                        var fi = new FileInfo(lf);
                        if (fi.LastWriteTime < threshold) fi.Delete();
                    }
                    catch { }
                }
            }
            catch { }

            // ALPHA TEAM: Build the host with proper Core integration
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    // EMERGENCY FIX: Make appsettings.json optional with fallback
                    var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                    Log.Information($"Looking for configuration at: {appSettingsPath}");

                    if (File.Exists(appSettingsPath))
                    {
                        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                        // Kullanıcı özel ayarları (override)
                        config.AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);
                        Log.Information("Configuration files loaded (appsettings + user override)");
                    }
                    else
                    {
                        Log.Warning("appsettings.json not found, using default configuration");
                        // Add in-memory configuration as fallback
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=MesTechStok;Integrated Security=true;MultipleActiveResultSets=True;Min Pool Size=2;Max Pool Size=10;",
                            ["Logging:LogLevel:Default"] = "Information",
                            ["Application:Name"] = "MesTech Stok Takip Sistemi",
                            ["Application:Version"] = "2.0 Professional",
                            // BarcodeView defaults
                            ["BarcodeView:Overscan:WidthMultiplier"] = "2.2",
                            ["BarcodeView:Overscan:HeightMultiplier"] = "1.2",
                            ["BarcodeView:Reader:Profile"] = "Standard",
                            ["BarcodeView:Reader:FormatPreset"] = "RetailPlus2D",
                            ["BarcodeView:Reader:UseROI"] = "true",
                            ["BarcodeView:Reader:RoiTopPercent"] = "0.25",
                            ["BarcodeView:Reader:RoiHeightPercent"] = "0.50",
                            ["BarcodeView:Reader:RoiLeftPercent"] = "0.05",
                            ["BarcodeView:Reader:RoiWidthPercent"] = "0.90",
                            ["BarcodeView:Reader:DecodeCooldownMs"] = "350",
                            ["BarcodeView:Reader:TryHarder"] = "true",
                            ["BarcodeView:Reader:TryInverted"] = "false",
                            ["BarcodeView:Reader:Priority2D"] = "true",
                            ["BarcodeView:Reader:UseClahe"] = "true",
                            ["BarcodeView:Reader:Thresholding"] = "Adaptive",
                            ["BarcodeView:Reader:QrFallbackOpenCV"] = "true",
                            ["BarcodeView:Scan:BaseTimeoutMs"] = "8000",
                            ["BarcodeView:Scan:AbsoluteTimeoutMs"] = "20000",
                            ["BarcodeView:Camera:FrameWidth"] = "1280",
                            ["BarcodeView:Camera:FrameHeight"] = "720",
                            ["BarcodeView:Camera:Fps"] = "30",
                            ["BarcodeView:Reader:DecodeScale"] = "1.0",
                            ["BarcodeView:Preview:Scale"] = "1.0",
                            ["BarcodeView:PreviewEnabled"] = "true",
                            // Screensaver defaults
                            ["Screensaver:Enabled"] = "false"
                        });
                        // Kullanıcı özel dosyasını yine de oku (opsiyonel)
                        config.AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true);
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services, context.Configuration);
                })
                .UseSerilog()
                .Build();

            ServiceProvider = _host.Services;

            // Bridge Core ServiceLocator with application's service provider
            MesTechStok.Core.Diagnostics.ServiceLocator.SetProvider(ServiceProvider);

            // ALPHA TEAM: Initialize database (bloklu) – şema garanti edilmeden UI işlemlerine başlanmasın
            InitializeDatabaseAsync().GetAwaiter().GetResult();

            // AUTO-START MONITORING: Initialize all monitoring services
            InitializeMonitoringServices();

            // HEADLESS SELF-TEST: Ortam değişkeni veya argüman ile tetikle
            var selfTestEnv = Environment.GetEnvironmentVariable("MESTECH_SELFTEST");
            var selfTestArg = e.Args.Any(a => a.Equals("--selftest", StringComparison.OrdinalIgnoreCase));
            if (string.Equals(selfTestEnv, "1", StringComparison.OrdinalIgnoreCase) || selfTestArg)
            {
                try
                {
                    RunSelfTestAsync().GetAwaiter().GetResult();
                    Log.Information("SELFTEST completed successfully. Exiting application.");
                    Shutdown(0);
                    return;
                }
                catch (Exception stex)
                {
                    Log.Error(stex, "SELFTEST failed: {Message}", stex.Message);
                    Shutdown(-2);
                    return;
                }
            }

            // Komut satırı parametrelerini kontrol et
            var args = e.Args;
            string? targetModule = null;

            // --module=barcode gibi parametreleri ara
            foreach (var arg in args)
            {
                if (arg.StartsWith("--module=", StringComparison.OrdinalIgnoreCase))
                {
                    targetModule = arg.Substring(9).ToLowerInvariant();
                    break;
                }
            }

            // Start listener to handle second-instance activation requests
            StartActivationListener();

            // Login atla ayarı
            var skipLogin = _host.Services.GetRequiredService<IConfiguration>
                ()?.GetSection("Authentication")?.GetValue<bool>("SkipLogin") ?? false;

            if (skipLogin)
            {
                // 🚀 **NEURAL SYSTEM LAUNCH** - Replace legacy WelcomeWindow with Neural Interface
                NeuralThemeManager.InitializeDefaultThemes();
                NeuralThemeManager.ApplyTheme("Neural", this);

                var neuralMainWindow = new NeuralMainWindow();
                neuralMainWindow.Show();

                // Set as main window
                MainWindow = neuralMainWindow;

                // Add debug marker for neural system
#if DEBUG
                try
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    var suffix = string.IsNullOrWhiteSpace(exePath) ? " [NEURAL-DEBUG]" : $" [NEURAL-DEBUG] {exePath}";
                    neuralMainWindow.Title = (neuralMainWindow.Title ?? string.Empty) + suffix;
                }
                catch { }
#endif
            }
            else
            {
                // Giriş ekranı ile başla
                var loginWindow = new Views.LoginWindow();
                loginWindow.Show();
                loginWindow.Activate();
                loginWindow.Focus();

                // Add a visible DEBUG marker with EXE path in title to confirm correct binary
#if DEBUG
                try
                {
                    var exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    var suffix = string.IsNullOrWhiteSpace(exePath) ? " [DEBUG]" : $" [DEBUG] {exePath}";
                    loginWindow.Title = (loginWindow.Title ?? string.Empty) + suffix;
                }
                catch { }
#endif

                // Login sonrası hedef modülü ayarla
                if (!string.IsNullOrEmpty(targetModule))
                {
                    loginWindow.TargetModule = targetModule;
                }
            }

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application startup failed");
            MessageBox.Show($"Uygulama başlatılamadı: {ex.Message}", "Kritik Hata",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _activateEvent?.Dispose(); } catch { }
        try { _singleInstanceMutex?.ReleaseMutex(); _singleInstanceMutex?.Dispose(); } catch { }
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Core Database Context (PRIMARY) – Yalnızca SQL Server
        // 🔥 A++++ FIX: Thread-safe DbContext configuration
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Server=(localdb)\\MSSQLLocalDB;Database=MesTechStok;Integrated Security=true;MultipleActiveResultSets=True;Min Pool Size=2;Max Pool Size=10;";
            // Sağlayıcı zorunlu: SQL Server
            options.UseSqlServer(connectionString);
            // 🎯 CRITICAL: Enable thread safety and connection pooling
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true);
            options.EnableDetailedErrors(true);
            Log.Information($"Database provider: SQL Server - Connection: {connectionString}");
        }, ServiceLifetime.Scoped); // Explicit scope for thread safety


        // ALPHA TEAM: Core Business Services
        services.AddScoped<MesTechStok.Core.Services.Abstract.IProductService, ProductService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.ICustomerService, MesTechStok.Core.Services.Concrete.CustomerService>();

        // EMERGENCY FIX: Missing IStockService registration - MainViewModel dependency
        services.AddScoped<MesTechStok.Core.Services.Abstract.IStockService, MesTechStok.Core.Services.Concrete.StockService>();

        // STOK YERLEŞİM SİSTEMİ SERVİSLERİ - ENHANCED
        // AI COMMAND TEMPLATE V2 EMERGENCY FIX: Using MockLocationService for MainViewModel dependency
        // Original LocationService disabled due to model inconsistencies
        services.AddScoped<MesTechStok.Core.Services.Abstract.ILocationService, MesTechStok.Desktop.Services.MockLocationService>();
        Log.Information("DI registered: ILocationService -> MockLocationService (Desktop)");
        services.AddScoped<MesTechStok.Core.Services.Abstract.IQRCodeService, MesTechStok.Core.Services.Concrete.QRCodeService>();
        // WAREHOUSE OPTIMIZATION: temporary mock to satisfy DI (Core impl excluded from build)
        services.AddScoped<MesTechStok.Core.Services.Abstract.IWarehouseOptimizationService, MesTechStok.Desktop.Services.MockWarehouseOptimizationService>();

        // MOBILE WAREHOUSE SERVICE: Desktop mock to satisfy DI until Core impl is aligned
        services.AddScoped<MesTechStok.Core.Services.Abstract.IMobileWarehouseService, MesTechStok.Desktop.Services.MockMobileWarehouseService>();
        Log.Information("DI registered: IMobileWarehouseService -> MockMobileWarehouseService (Desktop)");

        // FAZ 1 GÖREV 1.1: Authentication Service
        services.AddScoped<IAuthService, AuthService>();

        // Enhanced Logging Service
        services.AddScoped<MesTechStok.Core.Services.Abstract.ILoggingService, MesTechStok.Core.Services.Concrete.LoggingService>();
        services.AddScoped<LogAnalysisService>();

        // Telemetry Service registration (DB-backed)
        services.AddScoped<ITelemetryService, TelemetryService>();

        // Resilience Options
        services.Configure<ResilienceOptions>(configuration.GetSection("Resilience"));
        services.Configure<OpenCartSettingsOptions>(configuration.GetSection("OpenCartSettings"));

        // ALPHA TEAM: Desktop Services (Adapters for Core)
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddScoped<ISystemResourceService, SystemResourceService>();
        services.AddScoped<SimpleSecurityService>();
        // TODO: Basit güvenlik sistemi kullanılıyor (SimpleSecurityService)
        // services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IOfflineQueueService, OfflineQueueService>();
        services.AddSingleton<IOpenCartQueueWorker, OpenCartQueueWorker>();
        services.AddSingleton<IOpenCartInitializer, OpenCartInitializer>();
        services.AddSingleton<IOpenCartHealthService, OpenCartHealthService>();

        // AI Configuration Service - A++++ Enterprise Integration
        services.AddHttpClient<MesTechStok.Core.Services.IAIConfigurationService, MesTechStok.Core.Services.AIConfigurationService>();

        // OpenCart Client + Sync Service – use Core's defaults (internal handler + noop telemetry)
        services.AddSingleton<IOpenCartClient>(sp =>
        {
            var oc = sp.GetRequiredService<IOptions<OpenCartSettingsOptions>>().Value;
            return new OpenCartClient(oc.ApiUrl, oc.ApiKey);
        });
        // Note: SqlServerResilienceTelemetry is internal to Core; Desktop registers no telemetry override.
        // Core'daki InMemorySyncHealthProvider internal olduğu için DI'ya eklemiyoruz
        services.AddScoped<IOpenCartSyncService, OpenCartSyncService>();

        // AUTO-MONITORING SERVICES: Otomatik izleme servisleri
        services.AddSingleton<IClickTrackingService, ClickTrackingService>();
        services.AddSingleton<IApplicationMonitoringService, ApplicationMonitoringService>();

        // API INTEGRATION SERVICES: Gerçek API entegrasyonları
        services.Configure<MesTechStok.Core.Services.Weather.WeatherApiSettings>(configuration.GetSection("WeatherApiSettings"));
        services.Configure<MesTechStok.Core.Services.Barcode.BarcodeValidationSettings>(configuration.GetSection("BarcodeValidationSettings"));

        services.AddHttpClient<MesTechStok.Core.Services.Weather.IWeatherService, MesTechStok.Core.Services.Weather.OpenWeatherMapService>();
        services.AddHttpClient<MesTechStok.Core.Services.Barcode.IBarcodeValidationService, MesTechStok.Core.Services.Barcode.GS1BarcodeValidationService>();

        // OpenCart Real API Client devre dışı: Referans sınıf build dışında tutuluyor
        // Not: Gerçek client sınıfı yorum satırında ve interface mevcut değil; ihtiyaç halinde yeniden etkinleştirilecek

        // Barcode services
        services.AddSingleton<MesTechStok.Desktop.Services.IBarcodeService, MesTechStok.Desktop.Services.BarcodeHardwareService>();
        // USB HID barcode (Core) – global keystroke wedge desteği
        services.AddSingleton<MesTechStok.Core.Integrations.Barcode.IBarcodeScannerService, MesTechStok.Core.Integrations.Barcode.BarcodeScannerService>();
        services.AddSingleton<IGlobalBarcodeService, GlobalBarcodeService>();

        // WAREHOUSE & LOCATION SERVICES: LocationService fix tamamlandı
        // TODO: Diğer interface/implementation mismatch'leri düzeltilecek
        // Already registered above: IWarehouseOptimizationService (mock)
        // services.AddScoped<MesTechStok.Core.Services.Abstract.IMobileWarehouseService, MesTechStok.Core.Services.Concrete.MobileWarehouseService>();

        // ALPHA TEAM: ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<TelemetryViewModel>();

        // Telemetry query service
        services.AddScoped<ITelemetryQueryService, TelemetryQueryService>();

        // Telemetry retention background service
        services.AddHostedService<TelemetryRetentionService>();
        services.AddScoped<ITelemetryRetentionService, TelemetryRetentionService>();        // ALPHA TEAM: Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog();
        });
    }



    private async System.Threading.Tasks.Task InitializeDatabaseAsync()
    {
        try
        {
            if (ServiceProvider != null)
            {
                using var scope = ServiceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

                // Yalnızca SQL Server stratejisi
                // İlk kurulumda tablo var ise migration gereksiz; yoksa oluştur
                await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

                // 1) Telemetry tabloları – migration gelene kadar güvenli oluştur
                await context.EnsureTelemetryTablesCreatedAsync().ConfigureAwait(false);

                // 2) Ürün alanları – şema eksikse seed/okuma öncesi tamamla
                await context.EnsureProductRegulatoryColumnsCreatedAsync().ConfigureAwait(false);
                await context.EnsureProductExtendedColumnsCreatedAsync().ConfigureAwait(false);
                await context.EnsureProductAllColumnsCreatedAsync().ConfigureAwait(false);

                // 3) Concurrency & Sync sütunları – Customers/Orders vb. için RowVersion/SyncedAt
                await context.EnsureConcurrencyAndSyncColumnsCreatedAsync().ConfigureAwait(false);

                // 4) Üretim indeksleri (filtered unique vs.)
                await context.EnsureProductionIndexesExtendedAsync().ConfigureAwait(false);

                // 5) Temel indeksler (SKU/Barcode vb.)
                await context.EnsureIndexesCreatedAsync().ConfigureAwait(false);

                // 6) Seed işlemleri – şema tamamlandıktan sonra
                // Admin/rol/izin seed – tek sefer
                // TODO: Users tablosu oluşturulduktan sonra aktif edilecek
                // await context.SeedAuthenticationDataAsync().ConfigureAwait(false);
                // Demo data seed – TEMPORARILY DISABLED due to column mismatch
                // await context.SeedDemoDataAsync().ConfigureAwait(false);

                // Safety net: If still no active products, force seed once more
                // TEMPORARILY DISABLED - schema mismatch needs fixing
                // if (!await context.Products.AnyAsync(p => p.IsActive).ConfigureAwait(false))
                // {
                //     await context.SeedDemoDataAsync().ConfigureAwait(false);
                // }

                // FAZ 1 GÖREV 1.1: Seed Authentication Data - TEMPORARILY DISABLED
                // await context.SeedAuthenticationDataAsync();

                Log.Information("Database initialized successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database initialization failed");
            throw;
        }
    }

    // PostgreSQL ile ilgili tüm yardımcılar kaldırıldı – tek sağlayıcı: SQL Server

    private void InitializeMonitoringServices()
    {
        try
        {
            if (ServiceProvider == null) return;

            // 1. APPLICATION MONITORING: Uygulama performans izleme
            var appMonitoring = ServiceProvider.GetService<IApplicationMonitoringService>();
            if (appMonitoring != null)
            {
                appMonitoring.StartMonitoring();
                GlobalLogger.Instance.LogInfo("✅ Application monitoring otomatik başlatıldı", "Startup");
            }

            // 2. CLICK TRACKING: Otomatik tıklama izleme
            var clickTracking = ServiceProvider.GetService<IClickTrackingService>();
            if (clickTracking != null)
            {
                clickTracking.StartTracking();
                GlobalLogger.Instance.LogInfo("✅ Click tracking otomatik başlatıldı", "Startup");
            }

            // 3. SYSTEM RESOURCE MONITORING: Sistem kaynak izleme
            var systemResource = ServiceProvider.GetService<ISystemResourceService>();
            if (systemResource is SystemResourceService resourceService)
            {
                resourceService.Start();
                GlobalLogger.Instance.LogInfo("✅ System resource monitoring otomatik başlatıldı", "Startup");
            }

            // 4. OFFLINE QUEUE WORKER: OpenCart kuyruk işçisi
            var queueWorker = ServiceProvider.GetService<IOpenCartQueueWorker>();
            queueWorker?.Start();
            GlobalLogger.Instance.LogInfo("✅ OpenCart queue worker başlatıldı", "Startup");

            // 5. OpenCart Client init (API URL + Key)
            var ocInit = ServiceProvider.GetService<IOpenCartInitializer>();
            ocInit?.Initialize();

            // 6. AUTO SYNC bootstrap (OpenCart senkron sağlık + jitter interval)
            var ocSettings = ServiceProvider.GetService<IOptions<OpenCartSettingsOptions>>()?.Value;
            if (ocSettings?.AutoSyncEnabled == true)
            {
                var syncSvc = ServiceProvider.GetService<IOpenCartSyncService>();
                if (syncSvc != null)
                {
                    var baseInterval = TimeSpan.FromMinutes(Math.Max(1, ocSettings.SyncIntervalMinutes));
                    var r = new Random();
                    var jitterFactor = 1 + (r.NextDouble() - 0.5) * 0.2; // ±%10
                    var jittered = TimeSpan.FromMilliseconds(baseInterval.TotalMilliseconds * jitterFactor);
                    _ = syncSvc.StartAutoSyncAsync(jittered);
                    GlobalLogger.Instance.LogInfo($"✅ OpenCart AutoSync başlatıldı (interval={jittered})", "Startup");
                }
            }

            GlobalLogger.Instance.LogInfo("🚀 TÜM İZLEME SERVİSLERİ OTOMATİK OLARAK BAŞLATILDI", "Startup");

            // 7. USB HID Barcode dinleyici – uygulama genelinde aktif
            try
            {
                var hid = ServiceProvider.GetService<MesTechStok.Core.Integrations.Barcode.IBarcodeScannerService>();
                if (hid != null)
                {
                    _ = hid.StartScanningAsync();
                    GlobalLogger.Instance.LogInfo("✅ USB HID barkod dinleyici başlatıldı", "Startup");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"USB HID dinleyici başlatma hatası: {ex.Message}", "Startup");
            }

            // 8. Global Barcode Service - Ürün popup desteği
            try
            {
                var globalBarcode = ServiceProvider.GetService<IGlobalBarcodeService>();
                if (globalBarcode != null)
                {
                    _ = globalBarcode.StartListeningAsync();
                    GlobalLogger.Instance.LogInfo("✅ Global barcode service başlatıldı - Ürün popup desteği aktif", "Startup");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Global barcode service başlatma hatası: {ex.Message}", "Startup");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Monitoring services initialization failed");
            GlobalLogger.Instance.LogError($"Monitoring servisler başlatma hatası: {ex.Message}", "Startup");
        }
    }

    // Minimal headless self-test to verify CorrelationId + logging + DB telemetry wiring
    private async System.Threading.Tasks.Task RunSelfTestAsync()
    {
        // Start a correlation scope
        using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew($"SELF-{Guid.NewGuid():N}".Substring(0, 12));
        var corrId = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId ?? "<none>";

        // Write Serilog and GlobalLogger entries
        Log.Information("SELFTEST start (CorrId={CorrId})", corrId);
        GlobalLogger.Instance.LogAudit("SELFTEST", $"mode=AppStartup corrId={corrId}", "Startup");

        // Write a DB telemetry record
        if (ServiceProvider != null)
        {
            using var scope = ServiceProvider.CreateScope();
            var telemetry = scope.ServiceProvider.GetService<ITelemetryService>();
            if (telemetry == null)
            {
                GlobalLogger.Instance.LogError("SELFTEST: ITelemetryService bulunamadı", "Startup");
                throw new InvalidOperationException("ITelemetryService not registered");
            }

            await telemetry.LogApiCallAsync(
                endpoint: "/self-test/app",
                method: "GET",
                success: true,
                statusCode: 200,
                durationMs: 1,
                category: "SELFTEST",
                correlationId: corrId
            ).ConfigureAwait(false);
        }

        GlobalLogger.Instance.LogInfo($"SELFTEST OK (corrId={corrId})", "Startup");
    }

    /// <summary>
    /// Global unhandled exception handler
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Log.Fatal(exception, "CRITICAL UNHANDLED EXCEPTION: {Message}", exception?.Message);

        MessageBox.Show(
            $"Kritik Uygulama Hatası:\n\n{exception?.Message}\n\nDetaylar logga yazıldı. Uygulama kapatılacak.",
            "Kritik Hata", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    /// <summary>
    /// WPF UI thread exception handler
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UI THREAD EXCEPTION: {Message}", e.Exception.Message);

        // İç exception varsa onu da logla
        if (e.Exception.InnerException != null)
        {
            Log.Error(e.Exception.InnerException, "INNER EXCEPTION: {Message}", e.Exception.InnerException.Message);
        }

        string errorMessage = $"Uygulama Hatası:\n\n{e.Exception.Message}";
        if (e.Exception.InnerException != null)
        {
            errorMessage += $"\n\nDetay: {e.Exception.InnerException.Message}";
        }

        MessageBox.Show(errorMessage, "Uygulama Hatası", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true; // Exception'ı handle et, uygulama devam etsin
    }
}