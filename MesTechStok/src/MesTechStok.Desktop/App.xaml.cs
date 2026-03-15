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
using Microsoft.EntityFrameworkCore.Infrastructure;
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

using MesTechStok.Desktop.ViewModels;
using MesTechStok.Desktop.Views; // Other views
using GlobalLogger = MesTechStok.Desktop.Utils.GlobalLogger;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Services;
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
    /// <summary>WPF code-behind için izole DI köprüsü — SINIF B ServiceLocator yerine (D-01)</summary>
    public static IServiceLocatorBridge Services { get; private set; } = null!;
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
        catch { /* Intentional: startup cleanup — exception swallowed by design */ }
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
                catch { /* Intentional: background loop exit on dispose — exception swallowed by design */ }
            })
            { IsBackground = true, Name = "ActivationListener" };
            thread.Start();
        }
        catch { /* Intentional: startup cleanup — exception swallowed by design */ }
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
        catch { /* Intentional: startup configuration — exception swallowed, proceed with defaults */ }
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
                catch
                {
                    // Intentional: Win32 P/Invoke (ShowWindow/SetForegroundWindow) — target process may exit between enumeration and call.
                }
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
            // Dalga 2 4.D2-04: Seq log aggregation (http://localhost:5341, UI: http://localhost:8080)
            .WriteTo.Seq("http://localhost:5341")
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
            catch
            {
                // Intentional: diagnostic Process.GetCurrentProcess() — non-critical startup telemetry.
            }

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
                    catch
                    {
                        // Intentional: log file cleanup — file may be locked or already deleted concurrently.
                    }
                }
            }
            catch
            {
                // Intentional: log cleanup block — failure must not abort app startup.
            }

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
                            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=mestech_stok;Username=mestech_user;Password=CONFIGURE_VIA_USER_SECRETS",
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
            Services = _host.Services.GetRequiredService<IServiceLocatorBridge>(); // D-01: IServiceLocatorBridge köprüsü

            // DALGA 7.5 Gemini P1: DB bağlantı testi — crash yerine ConnectionErrorWindow göster
            if (!TestDatabaseConnection())
            {
                Shutdown(-1);
                return;
            }

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

            // Login geçici olarak devre dışı - ileride kayıt sistemi eklenecek
            // Doğrudan WelcomeWindow (ekran koruyucu) ile başla
            WelcomeWindowInstance = new Views.WelcomeWindow(targetModule);
            WelcomeWindowInstance.Show();
            MainWindow = WelcomeWindowInstance;

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
        try { _activateEvent?.Dispose(); }
        catch { /* Intentional: named-pipe event disposal during shutdown — suppress all exceptions. */ }
        try { _singleInstanceMutex?.ReleaseMutex(); _singleInstanceMutex?.Dispose(); }
        catch { /* Intentional: mutex release/disposal during shutdown — suppress all exceptions. */ }
        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // [DALGA 4] Core DbContext — FROZEN, backward compat only
        // Kanonik context: MesTech.Infrastructure.Persistence.AppDbContext (aşağıda)
        // Bu registration mevcut Core servisleri (ProductService vb.) için korunuyor.
        // Yeni entity/repo/servis Infrastructure.AppDbContext kullanmalıdır.
#pragma warning disable CS0618 // Obsolete Core.AppDbContext — intentional backward compat
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection yapılandırılmamış. dotnet user-secrets veya appsettings.json kullanın.");

            options.UseNpgsql(connectionString);
            Log.Information("Core Database provider: PostgreSQL (Bridge — Dalga 1)");

            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true);
            options.EnableDetailedErrors(true);

            // FAZ 0: AuditInterceptor — otomatik audit alanları + soft delete
            options.AddInterceptors(new MesTech.Infrastructure.Persistence.AuditInterceptor(
                new MesTech.Infrastructure.Security.DevelopmentUserService()));
        }, ServiceLifetime.Scoped);
#pragma warning restore CS0618


        // ALPHA TEAM: Core Business Services (FROZEN — use Clean Architecture for new code)
        services.AddScoped<MesTechStok.Core.Services.Abstract.IProductService, ProductService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.ICustomerService, MesTechStok.Core.Services.Concrete.CustomerService>();

        // EMERGENCY FIX: Missing IStockService registration - MainViewModel dependency
        services.AddScoped<MesTechStok.Core.Services.Abstract.IStockService, MesTechStok.Core.Services.Concrete.StockService>();

        // STOK YERLEŞİM SİSTEMİ SERVİSLERİ - ENHANCED
        // Dalga 2 Görev 2.05: Mock → Real (DEV 1 Multi-Tenant blocker resolved)
        services.AddScoped<MesTechStok.Core.Services.Abstract.ILocationService, MesTechStok.Desktop.Services.MockLocationService>();
        Log.Information("DI registered: ILocationService -> MockLocationService (Desktop)");
        services.AddScoped<MesTechStok.Core.Services.Abstract.IQRCodeService, MesTechStok.Core.Services.Concrete.QRCodeService>();
        services.AddScoped<MesTechStok.Core.Services.Abstract.IWarehouseOptimizationService, MesTechStok.Desktop.Services.MockWarehouseOptimizationService>();

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

        // Product Data Services (SqlBacked primary, Enhanced as fallback)
        services.AddScoped<IProductDataService, SqlBackedProductService>();
        services.AddSingleton<ImageStorageService>();
        services.AddScoped<SqlBackedReportsService>();
        services.AddSingleton<PdfReportService>();

        // ALPHA TEAM: Desktop Services (Adapters for Core)
        services.AddScoped<IDatabaseService, DatabaseService>();
        services.AddSingleton<ISystemResourceService, SystemResourceService>();
        services.AddScoped<SimpleSecurityService>();
        // TODO: Basit güvenlik sistemi kullanılıyor (SimpleSecurityService)
        // services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IOfflineQueueService, MesTechStok.Desktop.Services.OfflineQueueService>();
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
        services.AddSingleton<MesTechStok.Core.Integrations.Barcode.IBarcodeScannerService>(sp =>
            new MesTechStok.Core.Integrations.Barcode.BarcodeScannerService(
                sp.GetRequiredService<IServiceScopeFactory>(),
                data =>
                {
                    using var scope = sp.GetRequiredService<IServiceScopeFactory>().CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                    mediator.Send(new MesTech.Application.Commands.CreateBarcodeScanLog.CreateBarcodeScanLogCommand(
                        data.Barcode, data.Format, data.Source, data.DeviceId,
                        data.IsValid, data.ValidationMessage, data.RawLength, data.CorrelationId)).GetAwaiter().GetResult();
                }));
        services.AddSingleton<IGlobalBarcodeService, GlobalBarcodeService>();

        // WAREHOUSE & LOCATION SERVICES: LocationService fix tamamlandı
        // TODO: Diğer interface/implementation mismatch'leri düzeltilecek
        // Already registered above: IWarehouseOptimizationService (mock)
        // services.AddScoped<MesTechStok.Core.Services.Abstract.IMobileWarehouseService, MesTechStok.Core.Services.Concrete.MobileWarehouseService>();

        // FAZ 0: Unified Entegratör Altyapı Servisleri
        services.AddScoped<MesTech.Domain.Interfaces.ICurrentUserService, MesTech.Infrastructure.Security.DevelopmentUserService>();
        services.AddScoped<MesTech.Domain.Interfaces.IEventPublisher, MesTech.Infrastructure.Messaging.InMemoryEventPublisher>();

        // === DALGA 1 GÖREV 2.03: Clean Architecture DI Entegrasyonu ===

        // MediatR — Application CQRS handlers + Desktop handlers (DbContext-backed)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblies(
                typeof(MesTech.Application.Commands.CreateProduct.CreateProductHandler).Assembly,
                typeof(MesTechStok.Desktop.Handlers.GetCategoriesPagedHandler).Assembly));

        // Infrastructure AppDbContext (Clean Architecture — Core AppDbContext ile birlikte çalışır)
        services.AddScoped<MesTech.Infrastructure.Persistence.AuditInterceptor>();
        var infraConnectionString = configuration.GetConnectionString("PostgreSQL")
            ?? configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<MesTech.Infrastructure.Persistence.AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(infraConnectionString!, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MesTech.Infrastructure.Persistence.AppDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            });
            options.AddInterceptors(sp.GetRequiredService<MesTech.Infrastructure.Persistence.AuditInterceptor>());
        });

        // Clean Architecture Repositories
        services.AddScoped<MesTech.Domain.Interfaces.IProductRepository, MesTech.Infrastructure.Persistence.Repositories.ProductRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IStockMovementRepository, MesTech.Infrastructure.Persistence.Repositories.StockMovementRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IWarehouseRepository, MesTech.Infrastructure.Persistence.Repositories.WarehouseRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IOrderRepository, MesTech.Infrastructure.Persistence.Repositories.OrderRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.ITenantRepository, MesTech.Infrastructure.Persistence.Repositories.TenantRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IStoreRepository, MesTech.Infrastructure.Persistence.Repositories.StoreRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.ICategoryRepository, MesTech.Infrastructure.Persistence.Repositories.CategoryRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.ISupplierRepository, MesTech.Infrastructure.Persistence.Repositories.SupplierRepository>();

        // Dalga 4–7 Repositories (Accounting, Barcode, Quotation, Invoice, Bitrix24)
        services.AddScoped<MesTech.Domain.Interfaces.IIncomeRepository, MesTech.Infrastructure.Persistence.Repositories.IncomeRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IExpenseRepository, MesTech.Infrastructure.Persistence.Repositories.ExpenseRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.ICariHesapRepository, MesTech.Infrastructure.Persistence.Repositories.CariHesapRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.ICariHareketRepository, MesTech.Infrastructure.Persistence.Repositories.CariHareketRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IBarcodeScanLogRepository, MesTech.Infrastructure.Persistence.Repositories.BarcodeScanLogRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IQuotationRepository, MesTech.Infrastructure.Persistence.Repositories.QuotationRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IInvoiceRepository, MesTech.Infrastructure.Persistence.Repositories.InvoiceRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IProductSetRepository, MesTech.Infrastructure.Persistence.Repositories.ProductSetRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IProductVariantRepository, MesTech.Infrastructure.Persistence.Repositories.ProductVariantRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IBitrix24DealRepository, MesTech.Infrastructure.Persistence.Repositories.Bitrix24DealRepository>();
        services.AddScoped<MesTech.Domain.Interfaces.IBitrix24ContactRepository, MesTech.Infrastructure.Persistence.Repositories.Bitrix24ContactRepository>();

        // Integration Layer (Adapters, Factory, Orchestrator, Webhook, TokenCache)
        MesTech.Infrastructure.DependencyInjection.IntegrationServiceRegistration.AddIntegrationServices(services, configuration);

        // UnitOfWork + Domain Events
        services.AddScoped<MesTech.Domain.Interfaces.IDomainEventDispatcher, MesTech.Infrastructure.Services.DomainEventDispatcher>();
        services.AddScoped<MesTech.Domain.Interfaces.IUnitOfWork, MesTech.Infrastructure.Persistence.UnitOfWork>();

        // Domain Services (saf iş kuralları — dış bağımlılığı yok)
        services.AddSingleton<MesTech.Domain.Services.StockCalculationService>();
        services.AddSingleton<MesTech.Domain.Services.PricingService>();
        services.AddSingleton<MesTech.Domain.Services.BarcodeValidationService>();

        // Dalga 4 Domain Services — finansal modüller
        services.AddSingleton<MesTech.Domain.Services.BalanceCalculationService>();
        services.AddSingleton<MesTech.Domain.Services.ReturnPolicyService>();

        // Tenant Provider — DesktopTenantProvider (login sonrasi SetTenant ile tenant degisir)
        services.AddSingleton<MesTech.Infrastructure.Security.DesktopTenantProvider>();
        services.AddSingleton<MesTech.Domain.Interfaces.ITenantProvider>(sp =>
            sp.GetRequiredService<MesTech.Infrastructure.Security.DesktopTenantProvider>());

        // NOT: Redis, RabbitMQ, Hangfire, HealthChecks → Dalga 2'de aktifleştirilecek (dış bağımlılık gerektirir)

        // ALPHA TEAM: ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<TelemetryViewModel>();
        services.AddScoped<MesTechStok.Desktop.ViewModels.Documents.DocumentManagerViewModel>();

        // ALPHA TEAM: Views (DI registration for constructor injection support)
        services.AddTransient<Views.QuotationView>();
        // D-11 follow-up: ApiHealthDashboardView — constructor injection (no ServiceLocator)
        services.AddTransient<Views.ApiHealthDashboardView>();

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



    /// <summary>
    /// DALGA 7.5 Gemini P1: DB bağlantı testi — crash yerine kullanıcıya retry imkanı sunar.
    /// </summary>
    private bool TestDatabaseConnection()
    {
        if (ServiceProvider == null)
            return false;

        try
        {
            using var scope = ServiceProvider.CreateScope();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

            // First attempt
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (context.Database.CanConnect())
                {
                    Log.Information("Database connection test: SUCCESS");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Database connection test: FAILED on first attempt");
            }

            // Connection failed — show ConnectionErrorWindow with retry
            var connInfo = MaskConnectionString(connectionString);
            var errorMsg = "Veritabanına bağlanılamıyor. PostgreSQL servisi çalıştığından ve bağlantı bilgilerinin doğru olduğundan emin olun.\n\nDocker kullanıyorsanız: docker compose up -d";

            var window = new Views.ConnectionErrorWindow(connInfo, errorMsg, () =>
            {
                try
                {
                    using var retryScope = ServiceProvider.CreateScope();
                    var retryContext = retryScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var canConnect = retryContext.Database.CanConnect();
                    if (canConnect)
                        Log.Information("Database connection test: SUCCESS (retry)");
                    return canConnect;
                }
                catch (Exception retryEx)
                {
                    Log.Warning(retryEx, "Database connection retry failed");
                    return false;
                }
            });

            var result = window.ShowDialog();
            return result == true && window.ConnectionSucceeded;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database connection test: unexpected error");
            MessageBox.Show(
                $"Veritabanı bağlantı testi sırasında beklenmeyen hata:\n{ex.Message}",
                "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    /// <summary>
    /// Bağlantı dizesindeki şifreyi gizler (Password=***).
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(boş bağlantı dizesi)";

        try
        {
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"(Password|Pwd)\s*=\s*[^;]+",
                "$1=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        catch
        {
            return "(bağlantı dizesi okunamadı)";
        }
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

                // DALGA 7.7 FIX: EnsureCreatedAsync() does nothing when DB already exists
                // (Docker creates the empty database via POSTGRES_DB). Telemetry tables are
                // created by raw SQL (EnsureTelemetryTablesCreatedAsync), but entity tables
                // (Products, Categories, etc.) never get created.
                // Fix: Check if "Products" table exists; if not, force CreateTables().
                var conn = context.Database.GetDbConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                bool coreTablesExist;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='Products')";
                    coreTablesExist = (bool)(await cmd.ExecuteScalarAsync().ConfigureAwait(false))!;
                }

                if (!coreTablesExist)
                {
                    Log.Information("Core entity tables missing — creating schema from Core model");
                    var dbCreator = context
                        .GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalDatabaseCreator>();
                    await dbCreator.CreateTablesAsync().ConfigureAwait(false);
                    Log.Information("Core database schema created successfully");
                }
                else
                {
                    Log.Information("Core entity tables already exist, skipping schema creation");
                }

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
