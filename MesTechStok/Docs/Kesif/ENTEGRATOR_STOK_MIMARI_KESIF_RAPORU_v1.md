# === ENTEGRATÖR STOK MİMARİ KEŞİF RAPORU ===

**Kontrolör:** Claude Opus 4.6 (Ajan Mühendis)
**Tarih:** 05 Mart 2026
**İncelenen Repo:** `c:\MesChain-Sync-Enterprise\MesChain\MesTech\MesTech_Stok`
**Emirname Ref:** ENT-STOK-001
**Durum:** TAMAMLANDI — Sıfır kod değişikliği yapılmıştır.

---

## BÖLÜM 3.1: PROJE GENEL YAPISI

### Bulgular

**Dizin Yapısı (2 Seviye):**

```
MesTech_Stok/
├── .git/                              # Git repository
├── .gitignore
├── .vscode/                           # VS Code ayarları
├── README.md
├── MesTechStok/                       # Ana solution klasörü
│   ├── MesTechStok.sln               # Visual Studio Solution dosyası
│   ├── MesTechStok_Başlat.bat        # Başlatma scripti (batch)
│   ├── bin/Release/                   # Derlenmiş çıktılar
│   ├── Backups/                       # Yedekler ve eski projeler
│   ├── Docs/                          # Dokümantasyon
│   ├── Installer/                     # Kurulum dosyaları
│   ├── Logs/                          # Uygulama logları
│   ├── Scripts/                       # Yardımcı scriptler
│   └── src/                           # KAYNAK KOD KÖKÜ
│       ├── MesTechStok.Core/          # İş mantığı katmanı (Class Library)
│       ├── MesTechStok.Desktop/       # WPF masaüstü uygulaması
│       └── MesTechStok.Tests/         # Birim testleri (xUnit)
├── MesTechStok.Tests/                 # Test projesi (ayrı konum)
└── [20+ .md dokümantasyon dosyası]
```

**Repo Tipi:** Single-repo, multi-project (monorepo DEĞİL)
**Kanıt:** Tek `.sln` dosyası, `.git/` repo kökünde.

**Package Manager:** NuGet (.NET)
**Kanıt:** Bağımlılıklar `.csproj` dosyalarında `<PackageReference>` olarak tanımlı. npm/yarn/pnpm lock dosyası **YOK**.

**Dil:** C# (%100 .NET projesi)
**TypeScript:** KULLANILMIYOR — `tsconfig.json` **YOK**
**Kanıt:** 399 adet `.cs` kaynak dosyası tespit edildi.

**Target Framework:**
- `MesTechStok.Core` → `net9.0`
- `MesTechStok.Desktop` → `net9.0-windows`
- `MesTechStok.Tests` → `net9.0`

**Build Sistemi:** .NET 9.0 SDK / MSBuild
**Kanıt:** `.csproj` dosyalarında `<Project Sdk="Microsoft.NET.Sdk">`. Vite/Webpack/esbuild **YOK**.

**Ortam Değişkenleri:** `.env` dosyası **YOK**.
**Konfigürasyon yaklaşımı:** `appsettings.json` (ASP.NET Core standardı)

**`appsettings.json` Key İsimleri (MesTechStok.Core):**
```
ConnectionStrings:DefaultConnection
ConnectionStrings:PostgresConnection
ConnectionStrings:SqliteConnection
Logging:LogLevel:Default
Logging:LogLevel:Microsoft.EntityFrameworkCore
Serilog:Using / MinimumLevel / WriteTo
ApplicationSettings:CompanyName
ApplicationSettings:DatabaseType
ApplicationSettings:EnableBarcodeScanner
ApplicationSettings:EnableOpenCartIntegration
BarcodeScanner:ComPort
OpenCartSettings:ApiUrl
OpenCartSettings:ApiKey
```

**`appsettings.json` Key İsimleri (MesTechStok.Desktop):**
```
Database:Provider
ConnectionStrings:DefaultConnection
Authentication:SkipLogin
Application:Name / Version / DatabasePath
BarcodeView:Reader:FormatPreset
BarcodeView:Scan:BaseTimeoutMs
BarcodeSettings:AutoScanEnabled
OpenCartSettings:ApiUrl / ApiKey / AutoSyncEnabled
Resilience:CircuitBreaker:FailRateThreshold
UISettings:Theme / Language
WeatherApiSettings:OpenWeatherMapApiKey
BarcodeValidationSettings:EnableGS1Validation
```

---

## BÖLÜM 3.2: BACKEND MİMARİSİ

### Bulgular

**Framework:** .NET 9.0 (Microsoft.NET.Sdk)
**Proje Tipi:** WPF Desktop uygulaması — geleneksel bir web backend (Express/NestJS) **DEĞİLDİR**.

**API Yapısı:** Klasik REST API sunucusu **YOK**. Uygulama masaüstü mimarisindedir.
Dış iletişim OpenCart REST API üzerinden HTTP client ile yapılır.

**Mimari Katmanlar:**

| Katman | Proje | Açıklama |
|--------|-------|----------|
| **Core (Business Logic)** | `MesTechStok.Core` | Model, Service, Data Access katmanı |
| **Desktop (Presentation)** | `MesTechStok.Desktop` | WPF/XAML UI, ViewModels (MVVM) |
| **Tests** | `MesTechStok.Tests` | xUnit birim testleri |

**Service Organizasyonu (Backend İş Mantığı):**

```
src/MesTechStok.Core/Services/
├── Abstract/                    # Interface tanımları
│   ├── IAuthService.cs
│   ├── IInventoryService.cs
│   ├── IStockService.cs
│   ├── IProductService.cs
│   ├── IOrderService.cs
│   ├── ICustomerService.cs
│   ├── ILocationService.cs
│   ├── IWarehouseOptimizationService.cs
│   ├── IMobileWarehouseService.cs
│   ├── IQRCodeService.cs
│   ├── ILoggingService.cs
│   └── PermissionConstants.cs
├── Concrete/                    # Implementasyonlar
│   ├── AuthService.cs
│   ├── InventoryService.cs      # 493 satır — Ana stok mantığı
│   ├── StockService.cs          # 248 satır
│   ├── ProductService.cs
│   ├── CustomerService.cs
│   ├── OrderService.cs
│   ├── LocationService.cs
│   ├── LoggingService.cs
│   ├── QRCodeService.cs
│   ├── MobileWarehouseService.cs
│   └── WarehouseOptimizationService.cs
├── Barcode/
│   └── BarcodeValidationService.cs
├── Configuration/
│   └── AdvancedConfigurationManager.cs
├── MultiTenant/
│   ├── MultiTenantService.cs    # Multi-tenant altyapı (hazır)
│   └── Http/HttpContextShim.cs
├── Neural/
│   └── NeuralServices.cs        # AI/Neural servisler (TBD)
├── Resilience/
│   └── EnhancedCircuitBreaker.cs # Circuit Breaker pattern
├── Security/
│   └── TokenRotationService.cs  # Token yönetimi
├── AdaptivePaginationService.cs
├── AIConfigurationService.cs
├── DeltaSyncService.cs
├── OpenCartClient.cs            # [BOŞ — 1 satır]
├── OrderStatusStateMachine.cs
├── SqlServerResilienceTelemetry.cs
└── SyncRetryService.cs
```

**Desktop Servis Katmanı (UI-bound):**

```
src/MesTechStok.Desktop/Services/
├── SqlBacked*.cs                # SQL-backed servis implementasyonları
│   ├── SqlBackedInventoryService.cs
│   ├── SqlBackedOrderService.cs
│   ├── SqlBackedProductService.cs
│   └── SqlBackedReportsService.cs
├── Enhanced*.cs                 # Gelişmiş servisler
│   ├── EnhancedInventoryService.cs
│   ├── EnhancedOrderService.cs
│   ├── EnhancedProductService.cs
│   ├── EnhancedReportsService.cs
│   ├── EnhancedCustomerService.cs
│   └── EnhancedExcelImportService.cs
├── Mock*.cs                     # Mock servisler (test/development)
├── OpenCart*.cs                  # OpenCart entegrasyon servisleri
│   ├── OpenCartHttpService.cs
│   ├── OpenCartQueueWorker.cs
│   ├── OpenCartHealthService.cs
│   ├── OpenCartInitializer.cs
│   └── OpenCartSettingsOptions.cs
├── Barcode*.cs                  # Barkod servisleri
│   ├── BarcodeHardwareService.cs
│   └── GlobalBarcodeService.cs
├── PdfReportService.cs          # PDF rapor üretimi
├── AuthorizationService.cs      # Yetkilendirme
├── SimpleAuthService.cs         # Basit kimlik doğrulama
├── DatabaseService.cs           # Veritabanı yönetimi
├── ImageStorageService.cs       # Görsel depolama
├── OfflineQueueService.cs       # Çevrimdışı kuyruk
├── DocumentStorageService.cs    # Döküman depolama
├── AdvancedBusinessIntelligenceService.cs
├── ApplicationMonitoringService.cs
├── IoTSmartWarehouseService.cs  # IoT entegrasyon (hazır)
├── FutureModulesService.cs      # Gelecek modüller (placeholder)
└── NextGenerationInnovationServices.cs
```

**ORM / Veritabanı Katmanı:** Entity Framework Core 9.0.6
**Veritabanı Desteği:**
- **SQL Server** (birincil) — `Microsoft.EntityFrameworkCore.SqlServer 9.0.6`
- **PostgreSQL** — `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.2`
- **SQLite** — `Microsoft.EntityFrameworkCore.Sqlite 9.0.6` (geliştirme/fallback)

**Migration Sistemi:** EF Core Code-First Migrations
**Mevcut Migration Sayısı:** 2 adet
- `20250816202107_A6Plus_Production_Initial.cs`
- `20250818183706_InitialMigration.cs`

**Son Migration Tarihi:** 18 Ağustos 2025

---

## BÖLÜM 3.3: VERİTABANI ŞEMASI

### Bulgular

**Şema Tanım Kaynakları:**
- `src/MesTechStok.Core/Data/AppDbContext.cs` — 57.3KB EF Core konfigürasyon (FluentAPI)
- `src/MesTechStok.Core/create_mes_stok.sql` — SQL şema dosyası
- 2 adet migration dosyası

**Model (Tablo) Listesi — Tüm Tanımlı Varlıklar:**

| # | Model/Tablo | Dosya | İlişki |
|---|------------|-------|--------|
| 1 | `Product` | `Data/Models/Product.cs` (327 satır) | Stok ana varlığı |
| 2 | `StockMovement` | `Data/Models/StockMovement.cs` (186 satır) | Stok hareketleri |
| 3 | `InventoryLot` | `Data/Models/InventoryLot.cs` (42 satır) | Lot/parti takibi |
| 4 | `Warehouse` | `Data/Models/Warehouse.cs` (188 satır) | Depo yönetimi |
| 5 | `Category` | `Data/Models/Category.cs` (103 satır) | Ürün kategorileri |
| 6 | `Supplier` | `Data/Models/Supplier.cs` (143 satır) | Tedarikçiler |
| 7 | `Customer` | `Data/Models/Customer.cs` | Müşteriler |
| 8 | `Order` | `Data/Models/Order.cs` (118 satır) | Siparişler |
| 9 | `OrderItem` | `Data/Models/OrderItem.cs` | Sipariş kalemleri |
| 10 | `User` | `Data/Models/User.cs` | Kullanıcılar |
| 11 | `Role` | `Data/Models/Role.cs` | Roller |
| 12 | `Permission` | `Data/Models/Permission.cs` | İzinler |
| 13 | `UserRole` | `Data/Models/UserRole.cs` | Kullanıcı-Rol ilişkisi |
| 14 | `RolePermission` | `Data/Models/RolePermission.cs` | Rol-İzin ilişkisi |
| 15 | `Session` | `Data/Models/Session.cs` | Oturum yönetimi |
| 16 | `AccessLog` | `Data/Models/AccessLog.cs` | Erişim logları |
| 17 | `LogEntry` | `Data/Models/LogEntry.cs` | Genel log kayıtları |
| 18 | `BarcodeScanLog` | `Data/Models/BarcodeScanLog.cs` | Barkod tarama logları |
| 19 | `OfflineQueueItem` | `Data/Models/OfflineQueueItem.cs` | Çevrimdışı kuyruk |
| 20 | `ApiCallLog` | `Data/Models/ApiCallLog.cs` | API çağrı logları |
| 21 | `CircuitStateLog` | `Data/Models/CircuitStateLog.cs` | Circuit breaker logları |
| 22 | `SyncRetryItem` | `Data/Models/SyncRetryItem.cs` | Sync tekrar kuyruğu |
| 23 | `CompanySettings` | `Data/Models/CompanySettings.cs` | Şirket ayarları |
| 24 | `WarehouseZone` | `Data/Models/WarehouseZone.cs` | Depo bölgeleri |
| 25 | `WarehouseRack` | `Data/Models/WarehouseRack.cs` | Raf sistemi |
| 26 | `WarehouseShelf` | `Data/Models/WarehouseShelf.cs` | Raf katları |
| 27 | `WarehouseBin` | `Data/Models/WarehouseBin.cs` | Bölmeler |
| 28 | `ProductLocation` | `Data/Models/ProductLocation.cs` | Ürün konumu |
| 29 | `LocationMovement` | `Data/Models/LocationMovement.cs` | Konum hareketleri |
| 30 | `OptimizationModels` | `Data/Models/OptimizationModels.cs` | Optimizasyon modelleri |
| 31 | `MobileModels` | `Data/Models/MobileModels.cs` | Mobil modeller |
| 32 | `MobileWarehouseModels` | `Data/Models/MobileWarehouseModels.cs` | Mobil depo modelleri |

**Stok Tabloları — Ana Şema Yapısı:**

**Product (Ürün) — Temel Alanlar:**
```
Id: int (PK, auto-increment)
SKU: string(50) — Stok kodu, UNIQUE
Barcode: string(50) — Barkod
GTIN: string(14) — GS1 Global Trade Item Number
UPC: string(20) — Universal Product Code
EAN: string(20) — European Article Number
Name: string(200) — Ürün adı
Description: string — Açıklama
Stock: int — Mevcut stok miktarı
MinimumStock: int (default: 5) — Minimum stok eşiği
MaximumStock: int (default: 1000) — Maksimum kapasite
ReorderLevel: int (default: 10) — Yeniden sipariş seviyesi
ReorderQuantity: int (default: 50) — Yeniden sipariş miktarı
PurchasePrice: decimal(18,2) — Alış fiyatı (ağırlıklı ortalama)
SalePrice: decimal(18,2) — Satış fiyatı
ListPrice: decimal(18,2) — Liste/MSRP fiyatı
TaxRate: decimal(5,2) — Vergi oranı (default: %18 KDV)
DiscountRate: decimal(5,2) — İndirim oranı
Weight: decimal — Ağırlık (kg)
Length/Width/Height: decimal — Boyutlar (cm)
Location: string(50) — Basit konum
Shelf: string(20) — Raf
Bin: string(20) — Bölme
IsBatchTracked: bool — Parti takibi aktif mi
IsSerialized: bool — Seri numarası takibi
IsPerishable: bool — Son kullanma takibi
IsActive: bool — Aktif mi
IsDiscontinued: bool — Üretimden kalktı mı
SupplierId: int? (FK) — Tedarikçi
CategoryId: int? (FK) — Kategori
OpenCartProductId: int? — OpenCart ürün ID
OpenCartCategoryId: int? — OpenCart kategori ID
CreatedBy/ModifiedBy: string — Denetim izi
CreatedDate/ModifiedDate: DateTime
```

**StockMovement (Stok Hareketi) — Temel Alanlar:**
```
Id: int (PK)
ProductId: int (FK → Product)
MovementType: StockMovementType enum
    StockIn = 1, StockOut = 2, Adjustment = 3,
    BarcodeSale = 4, BarcodeReceive = 5, OpenCartSync = 6
Quantity: int
PreviousStock: int
NewStock: int
DocumentNumber: string — Belge no
DocumentUrl: string — Belge URL
FromLocation/ToLocation: string — Konum transferi
FromWarehouseId/ToWarehouseId: int? — Depo transferi
SupplierId/CustomerId: int?
BatchNumber: string — Parti numarası
SerialNumber: string — Seri numarası
ScannedBarcode: string — Taranan barkod
ExpiryDate: DateTime? — Son kullanma
UnitCost: decimal — Birim maliyet
TotalCost: decimal — Toplam maliyet
IsApproved: bool — Onaylandı mı
ApprovedBy: string — Onaylayan
IsReversed: bool — İptal edildi mi
ReversalMovementId: int? — İptal referansı
ProcessedBy: string — İşleyen
Date: DateTime — İşlem tarihi
```

**Hareket String Sabitleri:**
```
IN, OUT, ADJUSTMENT, TRANSFER, RETURN, LOSS, FOUND,
SALE, PURCHASE, PRODUCTION, CONSUMPTION
```

**Foreign Key İlişkileri:**
- `Product.CategoryId` → `Category.Id`
- `Product.SupplierId` → `Supplier.Id`
- `StockMovement.ProductId` → `Product.Id`
- `InventoryLot.ProductId` → `Product.Id`
- `OrderItem.ProductId` → `Product.Id`
- `OrderItem.OrderId` → `Order.Id`
- `UserRole.UserId` → `User.Id`
- `UserRole.RoleId` → `Role.Id`
- `RolePermission.RoleId` → `Role.Id`
- `RolePermission.PermissionId` → `Permission.Id`
- `WarehouseZone` → `Warehouse`
- `WarehouseRack` → `WarehouseZone`
- `WarehouseShelf` → `WarehouseRack`
- `WarehouseBin` → `WarehouseShelf`
- `ProductLocation` → `Product`, `WarehouseBin`
- `LocationMovement` → `ProductLocation`

**SQL Dosyaları:**
- `create_mes_stok.sql` — Ana şema oluşturma
- `ACIL_DB_SCHEMA_FIX.sql` — Acil şema düzeltmeleri
- `EXCEL_IMPORT_ENHANCEMENT_SQLITE.sql` — Excel import geliştirmeleri
- `Database_Schema_A6Plus.sql` — A6+ şema yedekleri

**View / Stored Procedure:** Tespit edilmedi (Code-First yaklaşımı, tüm mantık C# servislerinde).

---

## BÖLÜM 3.4: FRONTEND MİMARİSİ

### Bulgular

**Framework:** WPF (Windows Presentation Foundation) — Web frontend DEĞİL
**Dil:** XAML + C#
**Kanıt:** `.csproj` → `<UseWPF>true</UseWPF>`, `<UseWindowsForms>true</UseWindowsForms>`

**State Management:** MVVM Pattern (CommunityToolkit.Mvvm 8.2.2)
**Kanıt:** `<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />`

**UI Kütüphanesi / İkon Seti:** MahApps.Metro.IconPacks 6.0.0
**Kanıt:** `<PackageReference Include="MahApps.Metro.IconPacks" Version="6.0.0" />`

**Sayfa/View Yapısı (38 XAML dosyası):**

| View | Dosya | İşlev |
|------|-------|-------|
| DashboardView | `Views/DashboardView.xaml` | Ana gösterge paneli |
| SimpleDashboardView | `Views/SimpleDashboardView.xaml` | Basit gösterge paneli |
| **ProductsView** | `Views/ProductsView.xaml` | **Ürün listesi/yönetimi** |
| **NewProductsView** | `Views/NewProductsView.xaml` | **Yeni ürün ekleme** |
| **ProductEditDialog** | `Views/ProductEditDialog.xaml` | **Ürün düzenleme** |
| **InventoryView** | `Views/InventoryView.xaml` | **Envanter yönetimi** |
| **StockUpdateDialog** | `Views/StockUpdateDialog.xaml` | **Stok güncelleme** |
| **AddStockLotDialog** | `Views/AddStockLotDialog.xaml` | **Lot stok ekleme** |
| **StockPlacementView** | `Views/StockPlacementView.xaml` | **Stok yerleşim** |
| **WarehouseManagementView** | `Views/WarehouseManagementView.xaml` | **Depo yönetimi** |
| **BarcodeView** | `Views/BarcodeView.xaml` | **Barkod tarama** |
| BarcodeIntegrationDialog | `Views/BarcodeIntegrationDialog.xaml` | Barkod entegrasyon |
| BarcodeProductPopup | `Views/BarcodeProductPopup.xaml` | Barkod ürün popup |
| OrdersView | `Views/OrdersView.xaml` | Sipariş yönetimi |
| CustomersView | `Views/CustomersView.xaml` | Müşteri yönetimi |
| CustomerEditPopup | `Views/CustomerEditPopup.xaml` | Müşteri düzenleme |
| OpenCartView | `Views/OpenCartView.xaml` | OpenCart entegrasyon |
| ProductUploadPopup | `Views/ProductUploadPopup.xaml` | Ürün yükleme |
| ProductUploadPopup_Modern | `Views/ProductUploadPopup_Modern.xaml` | Modern yükleme |
| ProductUploadPopup_Enhanced | `Views/ProductUploadPopup_Enhanced.xaml` | Gelişmiş yükleme |
| ProductImageViewer | `Views/ProductImageViewer.xaml` | Ürün görsel |
| ProductImportWizard | `Views/ProductImportWizard.xaml` | İçe aktarma sihirbazı |
| ImageMapWizard | `Views/ImageMapWizard.xaml` | Görsel eşleme |
| CategoryManagerDialog | `Views/CategoryManagerDialog.xaml` | Kategori yönetimi |
| PriceUpdateDialog | `Views/PriceUpdateDialog.xaml` | Fiyat güncelleme |
| ReportsView | `Views/ReportsView.xaml` | Raporlama |
| ExportsView | `Views/ExportsView.xaml` | Dışa aktarma |
| SettingsView | `Views/SettingsView.xaml` | Ayarlar |
| SettingsOverlayWindow | `Views/SettingsOverlayWindow.xaml` | Ayarlar overlay |
| LoginWindow | `Views/LoginWindow.xaml` | Giriş ekranı |
| WelcomeWindow | `Views/WelcomeWindow.xaml` | Karşılama |
| TelemetryView | `Views/TelemetryView.xaml` | Telemetri |
| HealthMetricsView | `Views/HealthMetricsView.xaml` | Sistem sağlık |
| SystemResourcesView | `Views/SystemResourcesView.xaml` | Sistem kaynakları |
| LogView | `Views/LogView.xaml` | Log görüntüleyici |
| LogMonitoringView | `Views/LogMonitoringView.xaml` | Log izleme |
| SimpleTestView | `Views/SimpleTestView.xaml` | Test arayüzü |

**ViewModel Yapısı (MVVM):**

```
src/MesTechStok.Desktop/ViewModels/
├── MainViewModel.cs             # Ana ViewModel — navigasyon ve state
├── ViewModelBase.cs             # Temel ViewModel sınıfı
├── StockPlacementViewModel.cs   # Stok yerleşim mantığı
├── WarehouseManagementViewModel.cs # Depo yönetim mantığı
├── HealthMetricsViewModel.cs    # Sağlık metrikleri
├── TelemetryViewModel.cs        # Telemetri
└── LogCommandViewModel.cs       # Log komutları
```

**Barkod/Kamera Entegrasyonu:**
- ZXing.Net 0.16.9 — Barkod okuma
- OpenCvSharp4 4.9.0 — Kamera görüntü işleme
- AForge.Video.DirectShow 2.2.5 — DirectShow video yakalama

**Excel/PDF Dışa Aktarma:**
- ClosedXML 0.102.2 — Excel üretimi
- iTextSharp.LGPLv2.Core 3.4.6 — PDF üretimi

**HTTP Resilience:**
- Polly 8.2.0 — Circuit Breaker, Retry, Timeout
- Microsoft.Extensions.Http.Polly 9.0.0

---

## BÖLÜM 3.5: API ENTEGRASYON KATMANI

### Bulgular

**Mevcut Dış API Entegrasyonları:**

| # | Sistem | Tip | Durum | Dosyalar |
|---|--------|-----|-------|----------|
| 1 | **OpenCart** | E-ticaret platformu | ✅ Aktif | `OpenCartHttpService.cs`, `OpenCartClient.cs` [boş], `IOpenCartService.cs`, `OpenCartQueueWorker.cs` |
| 2 | **OpenWeatherMap** | Hava durumu API | ⚠️ Konfigüre | `WeatherService.cs`, `WeatherApiSettings:OpenWeatherMapApiKey` |
| 3 | **Trendyol** [?] | E-ticaret pazaryeri | ❓ Ayrı repo | `MesTech_Trendyol/` dizininde — bu repoda DEĞİL |

**API İletişim Yöntemi:**
- REST çağrıları (HTTP Client)
- Çevrimdışı kuyruk sistemi (`OfflineQueueService.cs`, `OpenCartQueueWorker.cs`)
- Delta senkronizasyon (`DeltaSyncService.cs`)
- Sync retry mekanizması (`SyncRetryService.cs`)

**Kimlik Doğrulama (Dış API):**
- API Key bazlı (`OpenCartSettings:ApiKey`)
- Token Rotation servisi hazır (`TokenRotationService.cs`)
- Token tipleri: ApiKey, Bearer, Basic
- Otomatik token yenileme (8 saatlik döngü, 24 saat ömür)

**Rate Limiting / Retry Mekanizması:**
- ✅ Polly Circuit Breaker (Desktop: `ResilienceOptions.cs`)
- ✅ EnhancedCircuitBreaker (Core: `Resilience/EnhancedCircuitBreaker.cs`)
  - Failure threshold: 5 başarısız istek
  - Timeout: 30 saniye
  - Sample period: 1 dakika
  - %50 hata oranı eşiği
  - Health check: 1 dakika interval
- ✅ Retry/Timeout (Polly 8.2.0)
- ✅ Offline Queue — Bağlantı yokken kuyrukla, sonra gönder

**Hata Yönetimi Stratejisi:**
- Circuit Breaker pattern (Open → HalfOpen → Closed)
- Offline queue fallback
- Sync retry servisi
- API call logging (`ApiCallLog` modeli)
- SQL Server resilience telemetry (`SqlServerResilienceTelemetry.cs`)

**Veri Dönüşüm Katmanı:**
- `CoreProductServiceAdapter.cs` — Core ↔ Desktop servis adaptörü
- `VariantProductProcessor.cs` — Varyant ürün işleme
- `EnhancedExcelImportService.cs` — Excel import dönüşüm
- OpenCart ↔ Lokal DB senkronizasyon mapping

---

## BÖLÜM 3.6: STOK MODÜLÜ MEVCUT DURUM

### Bulgular

**Stok modülü VAR MI?** ✅ **EVET — BÜYÜK ÖLÇÜDE TAMAMLANMIŞ**

### 3.6.1 Mevcut Stok İşlevleri

| İşlev | Metot | Durum |
|-------|-------|-------|
| Stok Ekleme | `AddStockAsync()` | ✅ Aktif |
| Stok Çıkarma | `RemoveStockAsync()` | ✅ Aktif |
| Stok Düzeltme (Sayım) | `AdjustStockAsync()` | ✅ Aktif |
| Lot Bazlı Stok Ekleme | `AddStockWithLotAsync()` | ✅ Aktif |
| FEFO Çıkış | `RemoveStockFefoAsync()` | ✅ Aktif |
| Hareket İptali | `CancelStockMovementAsync()` | ✅ Aktif |
| Toplu Güncelleme | `BulkStockUpdateAsync()` | ✅ Aktif |
| Stok Seviye Kontrolü | `CheckStockLevelAsync()` | ✅ Aktif |
| Düşük Stok Listesi | `GetLowStockProductsAsync()` | ✅ Aktif |
| Kritik Stok Listesi | `GetCriticalStockProductsAsync()` | ✅ Aktif |
| Hareket Geçmişi | `GetProductStockMovementsAsync()` | ✅ Aktif |
| Tarih Aralığı Hareketler | `GetStockMovementsAsync()` | ✅ Aktif |
| Tip Bazlı Hareketler | `GetMovementsByTypeAsync()` | ✅ Aktif |
| Son N Gün Hareketler | `GetRecentMovementsAsync()` | ✅ Aktif |

### 3.6.2 Stok Birimi Yapısı

| Özellik | Durum | Detay |
|---------|-------|-------|
| SKU | ✅ | MaxLength 50, unique, required |
| Barkod | ✅ | MaxLength 50, required |
| GTIN | ✅ | 14 karakter (GS1 standardı) |
| UPC | ✅ | 20 karakter |
| EAN | ✅ | 20 karakter |
| Lot/Parti Takibi | ✅ | `IsBatchTracked` flag + `InventoryLot` modeli |
| Seri Numarası | ✅ | `IsSerialized` flag |
| Son Kullanma | ✅ | `IsPerishable` flag + `ExpiryDate` |
| FEFO Desteği | ✅ | `LotStatus` enum: Open, Closed, Expired |

### 3.6.3 Depo/Lokasyon Yapısı

**Çoklu Depo Desteği:** ✅ **TAM DESTEK**

**Depo Tipleri:**
- MAIN — Ana depo
- BRANCH — Şube deposu
- VIRTUAL — Sanal depo (dropship)
- CONSIGNMENT — Konsinye
- EXTERNAL — Dış depolama
- TEMPORARY — Geçici depo

**Depo Özellikleri:**
- Fiziksel: Alan (m²), Kapasite, Yükseklik
- İklim: Sıcaklık/Nem aralığı, klima kontrolü
- Güvenlik: Güvenlik sistemi, yangın koruması
- Lojistik: Yükleme rampası, raf sistemi, forklift
- Maliyet: Aylık maliyet, m² birim maliyet, maliyet merkezi

**Gelişmiş Lokasyon Hiyerarşisi:** ⚠️ **GEÇİCİ OLARAK DEVRE DIŞI**
```
Warehouse → WarehouseZone → WarehouseRack → WarehouseShelf → WarehouseBin
```
Modeller tanımlı, EF konfigürasyonu hazır, ancak aktif kullanımda değil. Basit lokasyon sistemi (Product.Location/Shelf/Bin) aktif.

### 3.6.4 Stok Hareket Tipleri

```csharp
enum StockMovementType {
    StockIn = 1,          // Giriş (satın alma, üretim)
    StockOut = 2,         // Çıkış (satış, iade)
    Adjustment = 3,       // Sayım düzeltme
    BarcodeSale = 4,      // Barkodlu satış
    BarcodeReceive = 5,   // Barkodlu kabul
    OpenCartSync = 6,     // E-ticaret senkronizasyon
    In = 7,               // Test uyumluluk
    Out = 8               // Test uyumluluk
}
```

String sabitler: IN, OUT, ADJUSTMENT, TRANSFER, RETURN, LOSS, FOUND, SALE, PURCHASE, PRODUCTION, CONSUMPTION

### 3.6.5 Fiyatlama Entegrasyonu

| Alan | Tip | Açıklama |
|------|-----|----------|
| PurchasePrice | decimal(18,2) | Alış fiyatı (WAC — Ağırlıklı Ortalama Maliyet) |
| SalePrice | decimal(18,2) | Satış fiyatı |
| ListPrice | decimal(18,2) | MSRP/Tavsiye fiyat |
| TaxRate | decimal(5,2) | Vergi (%18 KDV default) |
| DiscountRate | decimal(5,2) | İndirim oranı |
| ProfitMargin | hesaplanan | ((SalePrice - PurchasePrice) / SalePrice) * 100 |
| TotalValue | hesaplanan | Stock × PurchasePrice |
| WeightedAverageUnitCost | hesaplanan | WAC formülü |

**WAC Formülü (InventoryService.AddStockAsync):**
```
newAvgCost = ((prevQty × prevCost) + (inQty × inCost)) / (prevQty + inQty)
```

**Döviz:** `Currency` alanı Supplier/Customer modellerinde (default: "TRY")

### 3.6.6 Raporlama

| Rapor | Servis | Format |
|-------|--------|--------|
| Düşük Stok Raporu | `PdfReportService.ExportLowStockReportAsync()` | PDF |
| Kritik Stok Raporu | Dashboard/Alert sistemi | UI |
| Envanter Raporu | `GenerateInventoryReportAsync()` | Data |
| Envanter Değeri | `CalculateInventoryValueAsync()` | Data |
| Kategori Bazlı Değer | `CalculateCategoryInventoryValueAsync()` | Data |
| Stok Hareket Raporları | Tarih/Tip/Ürün bazlı | Data |
| PDF Üretimi | iTextSharp + Türkçe font (Segoe UI) | PDF |
| Excel Dışa Aktarma | ClosedXML | XLSX |

---

## BÖLÜM 3.7: DOCKER & ALTYAPI

### Bulgular

**Docker:** ❌ **KULLANILMIYOR** (Bu repo içinde)
**Kanıt:** `docker-compose*` ve `Dockerfile*` dosyaları **BULUNAMADI**.

**Dağıtım Yöntemi:** Self-contained Windows executable
**Kanıt:** `.csproj` → `<SelfContained>true</SelfContained>`, `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`

**Kurulum:** `Installer/` dizininde kurulum paketleri mevcut.
**Başlatma:** `MesTechStok_Başlat.bat` ve `RUN_APP.ps1` scriptleri.

**CI/CD Pipeline:** ❌ **YOK**
**Kanıt:** `.github/workflows/` ve `.gitlab-ci.yml` dosyaları **BULUNAMADI**.

**Ortam Ayrımı:** ❌ **RESMİ AYRIŞIM YOK**
- Tek `appsettings.json` dosyası (environment-specific konfigürasyon yok)
- `Database:Provider` ayarı ile DB seçimi yapılıyor (SqlServer / PostgreSQL / Sqlite)

**Not:** `MesTech_Trendyol/` dizininde Docker altyapısı **VAR** (ayrı repo, bu keşif kapsamı dışında).

---

## BÖLÜM 3.8: GÜVENLİK & KONFİGÜRASYON

### Bulgular

**Authentication Sistemi:**
- BCrypt parola hashleme (`BCrypt.Net-Next 4.0.3`)
- Session tabanlı kimlik doğrulama
- `IAuthService` interface: Login, Logout, GetCurrentUser, HasPermission
- `LoginWindow.xaml` — Giriş ekranı
- `Authentication:SkipLogin` ayarı (geliştirme bypass)

**Yetkilendirme (RBAC):**
- ✅ **Role-Based Access Control (RBAC)** tam implementasyon
- Modeller: `User` → `UserRole` → `Role` → `RolePermission` → `Permission`
- Permission'da `Module` alanı (modül bazlı yetkilendirme)
- `PermissionConstants.cs` — Sabit izin tanımları
- `AuthorizationService.cs` — Yetki kontrol servisi
- `IsUserInRoleAsync()`, `HasPermissionAsync()` metotları

**Kullanıcı Modeli:**
```
User: Id, Username, Email, PasswordHash, FirstName, LastName, Phone,
      IsActive, IsEmailConfirmed, LastLoginDate, FullName,
      CreatedDate, ModifiedDate
      Navigation: UserRoles, StockMovements
```

**Rol Modeli:**
```
Role: Id, Name, Description, IsActive, IsSystemRole,
      CreatedDate, ModifiedDate
      Navigation: UserRoles, RolePermissions
```

**İzin Modeli:**
```
Permission: Id, Name, Module, Description, IsActive, CreatedDate
            Navigation: RolePermissions
```

**Secret Management:**
- ⚠️ Hassas veriler (`ApiKey`, `ConnectionString`) doğrudan `appsettings.json` içinde
- Token Rotation servisi mevcut ancak OpenCart provider'ı TODO
- File-based token storage (`tokens.json`)
- **ÖNERİ:** Vault/secret manager entegrasyonu gerekli

**Multi-Tenant Altyapı:**
- ✅ Hazır (Core katmanında)
- `TenantInfo`: Id, Name, DatabaseConnectionString, DatabaseProvider
- `TenantSubscriptionInfo`: PlanType (Basic/Pro/Enterprise), StartDate, EndDate
- `TenantLimits`: MaxUsers, vb.
- Şu an aktif kullanımda DEĞİL — altyapı hazır

**Logging:**
- Serilog (File + Console sinks)
- `AccessLog` modeli — Erişim denetimi
- `ApiCallLog` — API çağrı loglama
- `CircuitStateLog` — Circuit breaker state logları
- `BarcodeScanLog` — Barkod tarama denetimi

**CORS Konfigürasyonu:** ❌ **UYGULANAMAZ** — Masaüstü uygulama, web sunucu yok.

**Rate Limiting:** Dış API'ler için Circuit Breaker pattern ile sağlanıyor (bkz. Bölüm 3.5).

---

## ÖZET MATRİS

| Alan | Durum | Teknoloji | Notlar |
|------|-------|-----------|--------|
| **Backend** | ✅ Mevcut | .NET 9.0, C#, EF Core 9.0.6 | Desktop uygulama mimarisi, web API yok |
| **Frontend** | ✅ Mevcut | WPF/XAML, MVVM (CommunityToolkit) | 38 View, 8 ViewModel |
| **Veritabanı** | ✅ Mevcut | SQL Server + PostgreSQL + SQLite | 32+ model, EF Code-First, 2 migration |
| **Stok Modülü** | ✅ Kapsamlı | CRUD, Lot, FEFO, Multi-depo, WAC | Gelişmiş lokasyon sistemi devre dışı |
| **Entegrasyonlar** | ⚠️ Kısmi | OpenCart (aktif), Trendyol (ayrı repo) | Token rotation hazır, OpenCart TODO |
| **Docker** | ❌ Yok | — | Self-contained Windows exe |
| **CI/CD** | ❌ Yok | — | Manuel build & deploy |
| **Güvenlik** | ✅ Mevcut | BCrypt, RBAC, Token Rotation | Secret management iyileştirme gerekli |

---

## KRİTİK BULGULAR

1. **Proje bir WEB UYGULAMASI DEĞİL, MASAÜSTÜ UYGULAMASIDIR.** WPF/.NET 9.0 mimarisi — Express/NestJS/React gibi web framework'ler beklenmemeli. Entegratör modülü bu mimari üzerine kurulacaktır.

2. **Stok modülü büyük ölçüde TAMAMLANMIŞ.** Temel CRUD, lot/parti takibi, FEFO, çoklu depo desteği, ağırlıklı ortalama maliyet hesabı, barkod entegrasyonu aktif. Sıfırdan yazılmasına GEREK YOK.

3. **OpenCart entegrasyonu yarı tamamlanmış.** `OpenCartClient.cs` dosyası BOŞ (1 satır). `OpenCartHttpService.cs` ve `OpenCartQueueWorker.cs` Desktop katmanında mevcut. Token provider'ları TODO durumunda.

4. **Multi-Tenant altyapı HAZIR ama AKTİF DEĞİL.** Core katmanında TenantInfo, Subscription, Limits modelleri tanımlı. Aktif kullanıma geçmemiş.

5. **Gelişmiş lokasyon hiyerarşisi (Zone → Rack → Shelf → Bin) KODLANMIŞ ama DEVRE DIŞI.** Modeller ve EF konfigürasyonu mevcut, basit Location/Shelf/Bin sistemi aktif.

6. **CI/CD ve Docker altyapısı YOK.** Manuel build ve deployment. Entegratör yazılımı için containerization stratejisi planlanmalı.

7. **Secret management zayıf.** API key'ler ve connection string'ler direkt appsettings.json içinde. Vault/secure configuration gerekli.

8. **Neural/AI servisleri tanımlı ama implementasyonu belirsiz.** `NeuralServices.cs`, `AIConfigurationService.cs` dosyaları mevcut.

---

## BİLİNMEYENLER

1. **OpenCart API'nin güncel durumu** — Canlı bağlantı var mı, test edilebilir durumda mı? [BİLİNMİYOR]
2. **Veritabanı canlı şeması** — EF migration'lar ile güncel DB arasında fark var mı? [BİLİNMİYOR — DB erişimi yapılamadı]
3. **MesTech_Trendyol ile entegrasyon planı** — Trendyol modülü ayrı repoda, bu proje ile nasıl birleşecek? [BİLİNMİYOR]
4. **`NeuralServices.cs` ve `AIConfigurationService.cs` içerikleri** — AI fonksiyonlarının kapsamı net değil [?]
5. **`FutureModulesService.cs` ve `NextGenerationInnovationServices.cs`** — Placeholder mı, aktif geliştirme mi? [?]
6. **IoTSmartWarehouseService.cs** — IoT entegrasyonu planı nedir? [BİLİNMİYOR]
7. **MobileWarehouseService** — Mobil uygulama var mı yoksa sadece altyapı mı? [?]
8. **Production ortamında hangi DB provider kullanılıyor?** — appsettings'te 3 farklı DB tanımlı [BİLİNMİYOR]

---

## ÖNERİLER

1. **Entegratör Modülü Stratejisi:**
   - Mevcut stok modülü zaten kapsamlı. Entegratör katmanı, mevcut `IStockService` ve `IInventoryService` interface'leri üzerine inşa edilmeli.
   - `OpenCartClient.cs` (boş) doldurulmalı ve mevcut Desktop OpenCart servisleri Core katmanına taşınmalı.

2. **Mimari İyileştirme — Servis Katmanı:**
   - Core ve Desktop arasında servis duplikasyonu var (örn: `ProductService` her iki katmanda). Adapter pattern ile temizlenmeli.
   - Mock servisler (7 adet) Core katmanındaki interface'lere bağlanmalı.

3. **Multi-Platform Hazırlık:**
   - Entegratör yazılımı web API olarak da sunulacaksa, Core katmanı zaten ayrılmış durumda — ASP.NET Core Web API projesi eklenebilir.
   - Multi-tenant altyapı aktifleştirilmeli.

4. **DevOps Temeli:**
   - Docker Compose ile SQL Server + uygulama konteynerı hazırlanmalı.
   - GitHub Actions CI/CD pipeline kurulmalı.
   - appsettings.Development.json / appsettings.Production.json ayrımı yapılmalı.

5. **Güvenlik Güçlendirme:**
   - `appsettings.json`'daki hassas veriler .NET User Secrets veya Azure Key Vault'a taşınmalı.
   - Token rotation provider'ları (OpenCart, Trendyol) implement edilmeli.

6. **Lokasyon Sistemi Aktivasyonu:**
   - Gelişmiş depo lokasyon hiyerarşisi (Zone → Rack → Shelf → Bin) zaten kodlanmış. Entegratör modülü ile birlikte aktifleştirilmeli.

7. **Test Kapsamı Genişletme:**
   - Mevcut test projesi mevcut (`MesTechStok.Tests`, xUnit). Entegratör modülü için entegrasyon testleri eklenmelidir.

---

**RAPOR SONU**
**Kontrolör: Claude Opus 4.6 — "Kanıtsız tespit, tespitsiz geliştirme yoktur."**
