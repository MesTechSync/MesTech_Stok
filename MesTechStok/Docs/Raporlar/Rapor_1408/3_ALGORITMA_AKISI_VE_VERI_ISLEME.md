# 3. ALGORİTMA AKIŞI VE VERİ İŞLEME MANTIĞI - .NET MVVM

**Claude Rapor Tarihi:** 14 Ağustos 2025  
**Kaynak:** MesTechStok WPF MVVM Architecture Analysis  
**Pattern:** .NET 9 + CommunityToolkit.Mvvm + Entity Framework Core  

---

## 🔄 GERÇEK MVVM ALGORİTMA AKIŞI

### .NET WPF Async/Await Flow

```mermaid
flowchart TD
    A[WPF Application Startup] --> B[App.xaml.cs DI Container Setup]
    B --> C[MainWindow + MainViewModel Initialization]
    C --> D[Service Dependencies Injection]
    D --> E{User Interaction}
    
    E --> F[ICommand RelayCommand Trigger]
    F --> G[ViewModel Async Method Start]
    G --> H[StatusMessage = "Loading..."]
    H --> I[Core Service Method Call]
    
    I --> J{Service Implementation Status}
    J --> K[Entity Framework DbContext Query]
    J --> L[Test Data Return - Current State]
    
    K --> M[(Multi-Database: SQL Server/PostgreSQL)]
    M --> N[Entity Models Loading]
    N --> O[ObservableCollection Update]
    
    L --> O
    O --> P[PropertyChanged Event]
    P --> Q[WPF DataBinding UI Update]
    Q --> R[StatusMessage = "Complete"]
    R --> S[ILogger.LogInformation]
```

---

## 💻 GERÇEK KOD AKIŞ ANALİZİ

### 1. **Application Startup Flow**

```csharp
// App.xaml.cs - Gerçek DI setup
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Service container configuration
        var services = new ServiceCollection();
        
        // Database context - CURRENTLY MISSING CONNECTION
        services.AddDbContext<StockDbContext>(options =>
            options.UseSqlServer("CONNECTION_STRING_NEEDED"));
        
        // Core services - INTERFACES ONLY, NO IMPLEMENTATIONS
        services.AddScoped<IProductService, ProductService>(); // ProductService is EMPTY
        services.AddScoped<IInventoryService, InventoryService>(); // Working
        
        // Build container
        ServiceProvider = services.BuildServiceProvider();
        
        // Main window startup
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}
```

### 2. **MVVM Command Execution Flow**

```csharp
// MainViewModel.cs - Gerçek implementation (838 satır)
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<Product> products = new();
    
    [ObservableProperty] 
    private string statusMessage = "Hazır";
    
    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        StatusMessage = "Ürünler yükleniyor...";
        
        try
        {
            Products.Clear();
            
            // CURRENT ISSUE: Service returns empty - no implementation
            var items = await _productService.GetAllProductsAsync(); // Returns empty
            
            // FALLBACK: Using test data
            if (!items.Any())
            {
                Products.Add(testProduct); // Test data fallback
            }
            else
            {
                foreach(var item in items)
                    Products.Add(item);
            }
            
            StatusMessage = $"{Products.Count} ürün yüklendi";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ürün yükleme hatası");
            StatusMessage = "Hata: Ürünler yüklenemedi";
        }
    }
}
```

### 3. **Entity Framework Query Flow**

```csharp
// ProductService.cs - SHOULD BE IMPLEMENTED LIKE THIS
public class ProductService : IProductService
{
    private readonly StockDbContext _context;
    private readonly ILogger<ProductService> _logger;
    
    public async Task<IEnumerable<Product>> GetAllProductsAsync()
    {
        // CURRENT STATUS: Method is EMPTY - needs implementation
        
        // SHOULD BE:
        return await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
    
    public async Task<Product?> GetProductByBarcodeAsync(string barcode)
    {
        // CRITICAL FOR BARCODE SCANNER INTEGRATION
        return await _context.Products
            .FirstOrDefaultAsync(p => p.Barcode == barcode && p.IsActive);
    }
}
```

---

## 🔍 BARKOD OKUYUCU ENTEGRASYON AKIŞI

### Serial Port Integration Pattern

```csharp
// BarcodeService.cs - System.IO.Ports implementation needed
public class BarcodeService : IBarcodeService
{
    private readonly SerialPort _serialPort;
    private readonly IProductService _productService;
    private readonly ILogger<BarcodeService> _logger;
    
    public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
    
    public async Task StartListeningAsync()
    {
        _serialPort.DataReceived += async (sender, e) =>
        {
            var barcode = _serialPort.ReadLine().Trim();
            _logger.LogInformation("Barkod okundu: {Barcode}", barcode);
            
            // Real-time product lookup
            var product = await _productService.GetProductByBarcodeAsync(barcode);
            
            if (product != null)
            {
                BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs 
                { 
                    Barcode = barcode, 
                    Product = product,
                    Timestamp = DateTime.Now
                });
            }
        };
        
        _serialPort.Open();
    }
}
```

---

## 📊 VERİ İŞLEME PIPELINE'I

### 1. **Async Data Loading Pattern**

```csharp
// Pagination support - implemented in IProductService
public async Task<PagedResult<Product>> GetProductsPagedAsync(
    int page, int pageSize, 
    string? searchTerm = null, 
    string? category = null)
{
    var query = _context.Products.AsQueryable();
    
    // Search filtering
    if (!string.IsNullOrEmpty(searchTerm))
    {
        query = query.Where(p => p.Name.Contains(searchTerm) || 
                                 p.Barcode.Contains(searchTerm) ||
                                 p.SKU.Contains(searchTerm));
    }
    
    // Category filtering
    if (!string.IsNullOrEmpty(category))
    {
        query = query.Where(p => p.Category == category);
    }
    
    var totalItems = await query.CountAsync();
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    return new PagedResult<Product>
    {
        Items = items,
        TotalItems = totalItems,
        Page = page,
        PageSize = pageSize
    };
}
```

### 2. **Real-time UI Update Pattern**

```csharp
// ObservableCollection binding pattern
[ObservableProperty]
private ObservableCollection<Product> products = new();

[ObservableProperty]
private Product? selectedProduct;

// Automatic UI update when SelectedProduct changes
partial void OnSelectedProductChanged(Product? value)
{
    if (value != null)
    {
        // Load related data
        LoadProductDetailsAsync(value.Id);
    }
}
```

---

## 🚨 GERÇEK DURUM vs HEDEF AKIŞ

### **Mevcut Durum:**
- ✅ MVVM pattern doğru implementasyonda
- ✅ CommunityToolkit.Mvvm kullanılıyor
- ✅ Async/await pattern hazır
- ❌ **Service implementations boş**
- ❌ **Database connection eksik**
- ❌ **Test data fallback kullanılıyor**

### **Hedef Durum:**
1. **Core service implementations** tamamlanacak
2. **Database migrations** oluşturulacak
3. **Connection strings** yapılandırılacak
4. **Test data dependency** kaldırılacak
5. **Real-time barcode integration** aktif edilecek

---

## 🎯 ALGORİTMA OPTİMİZASYON ÖNCELİKLERİ

1. **Critical:** Service method implementations
2. **High:** EF Core database setup + migrations  
3. **High:** Real connection string configuration
4. **Medium:** Barcode scanner SerialPort integration
5. **Low:** UI responsiveness optimizations

Bu akış analizi, projenin **mevcut MVVM mimarisini** ve **kritik eksiklikleri** gerçek kod bazında göstermektedir.
