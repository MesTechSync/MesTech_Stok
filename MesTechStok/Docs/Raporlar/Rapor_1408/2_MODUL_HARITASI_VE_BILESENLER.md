# 2. MODÜL HARİTASI VE BİLEŞEN TANIMLARI - MesTechStok .NET

**Claude Rapor Tarihi:** 14 Ağustos 2025  
**Kaynak:** Gerçek Kod Analizi + Project Structure  
**Teknoloji:** .NET 9 WPF Multi-Project Solution  

---

## 🗂️ GERÇEK PROJE YAPISI

### .NET Solution Modülleri

| Modül Adı | Proje Türü | Gerçek Durum | Senkronizasyon |
|-----------|------------|--------------|----------------|
| **MesTechStok.Core** | .NET 9 Class Library | Interface tanımlı, impl eksik | EF Core + DI |
| **MesTechStok.Desktop** | WPF Application | MVVM + test data | CommunityToolkit.Mvvm |
| **MesTechStok.MainPanel** | WPF Control Library | Widget kontrolleri | Desktop integration |
| **MesTechStok.Screensaver** | WPF Window | Timer-based lock | Standalone |
| **MesTechStok.SystemResources** | .NET Service | Resource monitoring | Background service |

---

## 🔍 DETAYLI MODÜL ANALİZİ

### 1. **MesTechStok.Core (.NET 9 Class Library)**

#### **Package Dependencies (Gerçek):**
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.6" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.6" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.2" />
<PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
<PackageReference Include="System.IO.Ports" Version="9.0.6" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

#### **Services Architecture:**
```csharp
// IProductService.cs - 20+ metodlu interface
public interface IProductService
{
    Task<Product?> GetProductByBarcodeAsync(string barcode);
    Task<PagedResult<Product>> GetProductsPagedAsync(int page, int pageSize);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
    Task<bool> IsBarcodeUniqueAsync(string barcode, int? excludeProductId = null);
    Task<bool> BulkUpdateProductsAsync(IEnumerable<Product> products);
    // ... 15+ additional methods
}
```

#### **Entity Framework Models:**
```csharp
// Product.cs - 285 satırlık comprehensive model
public class Product 
{
    [Key] public int Id { get; set; }
    [Required][MaxLength(100)] public string Name { get; set; }
    [Required][MaxLength(50)] public string Barcode { get; set; }
    
    // GS1 Standards support
    [MaxLength(14)] public string? GTIN { get; set; }
    [MaxLength(20)] public string? UPC { get; set; }
    [MaxLength(20)] public string? EAN { get; set; }
    
    // Decimal precision pricing
    [Column(TypeName = "decimal(18,2)")] public decimal PurchasePrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal SalePrice { get; set; }
}
```

---

### 2. **MesTechStok.Desktop (WPF Application)**

#### **Package Dependencies:**
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.6" />
```

#### **MainViewModel Analysis (838 satır):**
```csharp
public partial class MainViewModel : ViewModelBase
{
    // ALPHA TEAM integration notes found in code
    [ObservableProperty] private ObservableCollection<Product> products = new();
    [ObservableProperty] private string barcodeStatus = "Bağlı değil";
    [ObservableProperty] private string openCartStatus = "Bağlı değil";
    
    // Test data being used - needs real service integration
    private Product testProduct = new Product
    {
        Name = "Test Ürün",
        SKU = "TEST-001", 
        Barcode = "1234567890123"
    };
}
```

#### **WPF Views:**
- **ProductsView** - Ana ürün yönetimi
- **InventoryView** - Stok takip paneli  
- **CustomersView** - Müşteri yönetimi (Desktop service kullanıyor)
- **CategoryManagerDialog** - Kategori yönetimi (Topmost/Focus working)

---

### 3. **MesTechStok.MainPanel (WPF Control Library)**

#### **Widget Kontrolleri:**
- **Dashboard Cards** - Günlük hareket özeti
- **Stock Level Indicators** - Kritik stok uyarıları
- **Quick Action Buttons** - Hızlı işlem menüleri
- **Real-time Charts** - Stok trend grafikleri

---

### 4. **MesTechStok.Screensaver (WPF Window)**

#### **Özellikler:**
- **Timer-based Lock** - Belirli süre sonra kilitleme
- **Company Info Display** - Firma bilgileri gösterimi
- **Password Protection** - Şifre koruması
- **Full Screen Mode** - Tam ekran koruyucu

```csharp
// Screensaver configuration
<PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
</PropertyGroup>
```

---

### 5. **MesTechStok.SystemResources (.NET Service)**

#### **System Monitoring:**
- **CPU Usage** - İşlemci kullanım izleme
- **Memory Usage** - Bellek kullanım takibi
- **Disk I/O** - Disk aktivite monitoring
- **Network Statistics** - Ağ istatistikleri

---

## 🔗 MODÜL ENTEGRASYON MATRİSİ

| Kaynak Modül | Hedef Modül | Entegrasyon Türü | Durum |
|---------------|-------------|-------------------|-------|
| Desktop → Core | Service Injection | DI Container | ⚠️ Eksik impl |
| Desktop → MainPanel | WPF Control Host | XAML Include | ✅ Çalışıyor |
| Core → EF Core | Database Access | DbContext | ❌ Migrations disabled |
| Desktop → Screensaver | Process Launch | Standalone EXE | ✅ Timer-based |
| All → SystemResources | Performance Monitor | Background Service | ✅ Active |

---

## 🚨 KRİTİK BAĞIMLILIK SORUNLARI

### **Desktop Layer Issues:**
```csharp
// PROBLEM: Desktop layer still using local services
// CustomersView.xaml.cs:
private readonly EnhancedCustomerService _customerService; // Should be Core ICustomerService

// SOLUTION NEEDED: Proper DI injection
private readonly ICustomerService _customerService; // From Core
```

### **Missing Core Implementations:**
- `IProductService` metodları boş
- `ICustomerService` tamamen eksik  
- Database context yapılandırması eksik

---

## 📋 ENTEGRASYON ÖNCELİKLERİ

1. **Critical:** Core service implementations
2. **High:** Database migrations + connection strings  
3. **Medium:** Desktop → Core service binding
4. **Low:** UI polish + additional features

Bu modül haritası, projenin **gerçek .NET WPF yapısını** ve **mevcut entegrasyon durumunu** yansıtmaktadır.
