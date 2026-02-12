# 6. LOG TOPLAMA VE HATA İNCELEME - SERILOG .NET

**Claude Rapor Tarihi:** 14 Ağustos 2025  
**Kaynak:** MesTechStok .NET Logging Architecture Analysis  
**Teknoloji:** Serilog + ILogger<T> + Structured Logging  

---

## 🔍 GERÇEK LOGGING ALTYAPıSı ANALİZİ

### Mevcut Logging Dependencies

```xml
<!-- MesTechStok.Core.csproj - Logging packages needed -->
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.6" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Debug" Version="2.0.0" />
<PackageReference Include="Serilog.Settings.Configuration" Version="8.0.0" />
```

---

## 📝 SERILOG KONFİGÜRASYONU

### Program.cs / App.xaml.cs Setup

```csharp
// App.xaml.cs - Serilog initialization
using Serilog;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Serilog configuration
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "MesTechStok")
            .Enrich.WithProperty("Version", GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0")
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .WriteTo.File(
                path: "Logs/mestech-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                shared: true)
            .WriteTo.Debug(
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}")
            .CreateLogger();

        // Register with DI container
        services.AddLogging(builder => builder.AddSerilog());
        
        // Application startup logging
        Log.Information("MesTechStok uygulaması başlatılıyor...");
        Log.Information("OS: {OS}, User: {User}", Environment.OSVersion, Environment.UserName);
        
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("MesTechStok uygulaması kapatılıyor. Exit Code: {ExitCode}", e.ApplicationExitCode);
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
```

### appsettings.json Configuration

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.Debug"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/mestech-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj} {Properties:j}{NewLine}{Exception}",
          "shared": true
        }
      },
      {
        "Name": "Debug",
        "Args": {
          "outputTemplate": "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithEnvironmentUserName"],
    "Properties": {
      "Application": "MesTechStok",
      "Environment": "Development"
    }
  }
}
```

---

## 🛠️ STRUCTURED LOGGING İMPLEMENTASYONU

### 1. **Product Service Logging**

```csharp
// ProductService.cs - Comprehensive logging example
public class ProductService : IProductService
{
    private readonly StockDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(StockDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        using var activity = _logger.BeginScope("Barkod ile ürün arama: {Barcode}", barcode);
        
        try
        {
            _logger.LogInformation("Barkod arama başlatıldı: {Barcode}", barcode);
            
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);

            if (product != null)
            {
                _logger.LogInformation("Ürün bulundu: {ProductId} - {ProductName}", 
                    product.Id, product.Name);
                return product;
            }
            else
            {
                _logger.LogWarning("Barkod için ürün bulunamadı: {Barcode}", barcode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barkod arama hatası: {Barcode}", barcode);
            throw;
        }
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        using var activity = _logger.BeginScope("Yeni ürün oluşturma");
        
        _logger.LogInformation("Yeni ürün oluşturuluyor: {@Product}", new {
            product.Name,
            product.SKU,
            product.Barcode,
            product.Category,
            product.SalePrice
        });

        try
        {
            // Validation logging
            if (await _context.Products.AnyAsync(p => p.Barcode == product.Barcode))
            {
                _logger.LogWarning("Duplicate barcode attempt: {Barcode} for product {Name}", 
                    product.Barcode, product.Name);
                throw new InvalidOperationException($"Barkod zaten mevcut: {product.Barcode}");
            }

            product.CreatedDate = DateTime.Now;
            _context.Products.Add(product);
            
            var result = await _context.SaveChangesAsync();
            
            _logger.LogInformation("Ürün başarıyla oluşturuldu: {ProductId} - {ProductName}, DB Changes: {Changes}", 
                product.Id, product.Name, result);

            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ürün oluşturma hatası: {@Product}", new {
                product.Name,
                product.SKU,
                product.Barcode
            });
            throw;
        }
    }
}
```

### 2. **MainViewModel Event Logging**

```csharp
// MainViewModel.cs - User interaction logging
public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Ürün yükleme başlatıldı");
        StatusMessage = "Ürünler yükleniyor...";

        try
        {
            Products.Clear();
            var products = await _productService.GetAllProductsAsync();
            
            foreach (var product in products)
            {
                Products.Add(product);
            }

            stopwatch.Stop();
            
            _logger.LogInformation("Ürün yükleme tamamlandı: {ProductCount} ürün, {ElapsedMs}ms", 
                Products.Count, stopwatch.ElapsedMilliseconds);
                
            StatusMessage = $"{Products.Count} ürün yüklendi";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, "Ürün yükleme hatası - {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            StatusMessage = "Hata: Ürünler yüklenemedi";
        }
    }

    [RelayCommand]
    private async Task SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return;

        _logger.LogInformation("Ürün arama başlatıldı: {SearchTerm}", searchTerm);
        
        try
        {
            var results = await _productService.SearchProductsByNameAsync(searchTerm);
            
            Products.Clear();
            foreach (var product in results)
            {
                Products.Add(product);
            }

            _logger.LogInformation("Arama tamamlandı: {SearchTerm} - {ResultCount} sonuç", 
                searchTerm, Products.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Arama hatası: {SearchTerm}", searchTerm);
        }
    }
}
```

---

## 🔧 BARKOD OKUYUCU LOGGING

### Serial Port Integration Logging

```csharp
// BarcodeService.cs - Hardware integration logging
public class BarcodeService : IBarcodeService
{
    private readonly SerialPort _serialPort;
    private readonly IProductService _productService;
    private readonly ILogger<BarcodeService> _logger;

    public async Task StartListeningAsync()
    {
        _logger.LogInformation("Barkod okuyucu dinleme başlatılıyor: Port {Port}, Baud {BaudRate}", 
            _serialPort.PortName, _serialPort.BaudRate);

        try
        {
            _serialPort.DataReceived += async (sender, e) =>
            {
                var rawData = _serialPort.ReadLine().Trim();
                
                _logger.LogDebug("Ham barkod verisi alındı: {RawData}", rawData);

                if (IsValidBarcode(rawData))
                {
                    _logger.LogInformation("Geçerli barkod okundu: {Barcode}", rawData);
                    
                    var product = await _productService.GetProductByBarcodeAsync(rawData);
                    
                    if (product != null)
                    {
                        _logger.LogInformation("Barkod eşleşti: {Barcode} -> {ProductName}", 
                            rawData, product.Name);
                        
                        BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs 
                        { 
                            Barcode = rawData, 
                            Product = product,
                            Timestamp = DateTime.Now
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Barkod sistemde bulunamadı: {Barcode}", rawData);
                    }
                }
                else
                {
                    _logger.LogWarning("Geçersiz barkod formatı: {RawData}", rawData);
                }
            };

            _serialPort.Open();
            _logger.LogInformation("Barkod okuyucu başarıyla başlatıldı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Barkod okuyucu başlatma hatası: Port {Port}", _serialPort.PortName);
            throw;
        }
    }
}
```

---

## 📊 PERFORMANSa VE METRİK LOGGING

### Entity Framework Performance Logging

```csharp
// DbContext logging configuration
public class StockDbContext : DbContext
{
    private readonly ILogger<StockDbContext> _logger;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .LogTo(message => _logger.LogInformation(message), LogLevel.Information)
            .EnableSensitiveDataLogging() // Only in development
            .EnableDetailedErrors();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var changes = await base.SaveChangesAsync(cancellationToken);
            stopwatch.Stop();
            
            _logger.LogInformation("Database changes saved: {Changes} entities, {ElapsedMs}ms", 
                changes, stopwatch.ElapsedMilliseconds);
                
            return changes;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database save failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

---

## 🚨 HATA YAKALAMA VE ALERT SİSTEMİ

### Global Exception Handler

```csharp
// App.xaml.cs - Global exception handling
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Yakalanmamış UI thread hatası");
        
        MessageBox.Show(
            "Beklenmeyen bir hata oluştu. Uygulama kapatılacak.\n\nHata detayları log dosyasına kaydedildi.",
            "Kritik Hata",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
            
        e.Handled = true;
        Environment.Exit(1);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal((Exception)e.ExceptionObject, "Yakalanmamış AppDomain hatası. Terminating: {IsTerminating}", 
            e.IsTerminating);
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Gözlemlenmemiş Task hatası");
        e.SetObserved();
    }
}
```

---

## 📈 LOG ANALİZ VE RAPORLAMA

### Log Aggregation and Analysis

```csharp
// LogAnalysisService.cs - Log file analysis
public class LogAnalysisService
{
    private readonly ILogger<LogAnalysisService> _logger;

    public async Task<LogAnalysisReport> GenerateDailyReportAsync(DateTime date)
    {
        var logFile = $"Logs/mestech-{date:yyyyMMdd}.log";
        
        if (!File.Exists(logFile))
        {
            _logger.LogWarning("Log dosyası bulunamadı: {LogFile}", logFile);
            return new LogAnalysisReport { Date = date };
        }

        var lines = await File.ReadAllLinesAsync(logFile);
        
        var report = new LogAnalysisReport
        {
            Date = date,
            TotalLogEntries = lines.Length,
            ErrorCount = lines.Count(l => l.Contains("[ERR]")),
            WarningCount = lines.Count(l => l.Contains("[WRN]")),
            InfoCount = lines.Count(l => l.Contains("[INF]")),
            
            // Business metrics
            ProductQueries = lines.Count(l => l.Contains("Barkod arama") || l.Contains("Ürün yükleme")),
            DatabaseOperations = lines.Count(l => l.Contains("Database changes saved")),
            BarcodeScans = lines.Count(l => l.Contains("Barkod okundu")),
            
            // Performance metrics
            AverageResponseTime = CalculateAverageResponseTime(lines),
            SlowQueries = lines.Where(l => l.Contains("ms") && ExtractMilliseconds(l) > 1000).Count()
        };

        _logger.LogInformation("Günlük log analizi tamamlandı: {@Report}", report);
        return report;
    }
}
```

---

## 🎯 LOG YÖNETİM ÖNCELİKLERİ

| Component | Implementation Status | Priority | Action Required |
|-----------|----------------------|----------|-----------------|
| **Serilog Setup** | ❌ Not configured | Critical | Initial configuration |
| **Service Logging** | ❌ Missing implementations | Critical | Add logging to all services |
| **Performance Metrics** | ❌ Not implemented | High | Add stopwatch timing |
| **Error Handling** | ❌ Basic only | High | Global exception handlers |
| **Log Analysis** | ❌ Manual only | Medium | Automated analysis tools |

Bu logging sistemi, MesTechStok uygulamasının **üretim ortamında izlenebilirliğini** ve **hata ayıklama kapasitesini** sağlayacaktır.
