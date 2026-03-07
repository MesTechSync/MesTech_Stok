# EMİRNAME: FAZ 0 — MİMARİ YENİDEN YAPILANDIRMA & ENTEGRATÖR ALTYAPISI

**Belge No:** ENT-STOK-FAZ0-001  
**Tarih:** 05 Mart 2026  
**Yayıncı:** Komutan (MesTech)  
**Analiz Yapan:** Claude Opus 4.6  
**Öncelik:** KRİTİK — Tüm fazların ön koşulu  
**Durum:** AKTİF

---

## 1. VİZYON: NEYİ İNŞA EDİYORUZ?

### 1.1 Eski Anlayış (Terk Edilen)
```
MesTech_Stok = Tek masaüstü uygulama + OpenCart bağlantısı
```

### 1.2 Yeni Anlayış (Kabul Edilen)

```
MesTech Ekosistemi = Birden Fazla Yazılım Merkezi + Ortak Domain Çekirdeği

┌─────────────┐    ┌──────────────┐    ┌─────────────┐    ┌──────────────┐
│ MesTech_Stok │    │MesTech_Trndyl│    │ MesTech_N11  │    │ Gelecek App  │
│  (Desktop)   │    │  (Desktop)   │    │  (Desktop?)  │    │  (Web/Mobil)  │
│  Stok Merkezi│    │ Pazar Merkezi│    │ Pazar Merkezi│    │    ???        │
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘    └──────┬───────┘
       │                   │                   │                   │
       └───────────────────┴───────────────────┴───────────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │   MesTech.Domain (Çekirdek)  │
                    │                              │
                    │  • Ürün Arşivi (Product)      │
                    │  • Stok Hareketleri           │
                    │  • Sipariş Yönetimi           │
                    │  • Entegrasyon Kontratları     │
                    │  • Domain Olayları (Events)    │
                    │                              │
                    └──────────────┬──────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │      PostgreSQL (Merkezi)     │
                    └─────────────────────────────┘
```

**Kritik kavram:** MesTech_Stok "tek merkez" değildir. **Birden fazla yazılım merkezi**, ortak bir domain çekirdeği üzerinden haberleşir. Her merkez kendi alanında uzman, ama ürün verisi ve stok hareketleri ortak dilde konuşur.

### 1.3 Bu Ne Anlama Geliyor?

1. **Ürün Arşivi** tek bir yerde yaşar (Domain katmanı) — her yazılım buraya ulaşır
2. **OpenCart senkronizasyonu** bir "dal" dır — Trendyol, N11, Hepsiburada başka dallar olacak
3. **Her yazılım bağımsız çalışabilir** — ama ortak Domain çekirdeğini paylaşır
4. **Gelecekte Web API, mobil uygulama** eklense bile Domain değişmez — sadece yeni bir "kapı" açılır

---

## 2. HEDEF MİMARİ: CLEAN ARCHITECTURE + DDD

### 2.1 Katman Yapısı (İçten Dışa)

```
═══════════════════════════════════════════════════════════════
  KATMAN 4 — INFRASTRUCTURE (En Dış)
  EF Core, PostgreSQL, HTTP Clients, File System, Serilog
═══════════════════════════════════════════════════════════════
  KATMAN 3 — PRESENTATION (Dış)
  WPF Desktop UI, [Gelecekte: Web API, Console Worker]
═══════════════════════════════════════════════════════════════
  KATMAN 2 — APPLICATION (Orta)
  Use Case'ler, CQRS Commands/Queries, DTO'lar, Mapping
═══════════════════════════════════════════════════════════════
  KATMAN 1 — DOMAIN (Çekirdek — Hiçbir Şeye Bağımlı Değil)
  Entity'ler, Value Object'ler, Domain Event'ler,
  Repository Interface'leri, Domain Service'ler
═══════════════════════════════════════════════════════════════
```

**Altın kural:** Oklar her zaman içe doğru gösterir. Domain hiçbir şeye bağımlı değildir. Infrastructure Domain'i bilir ama Domain Infrastructure'ı bilmez.

### 2.2 Hedef Solution Yapısı

```
MesTechStok.sln
│
├── src/
│   │
│   ├── MesTech.Domain/                      # KATMAN 1 — Çekirdek
│   │   ├── Entities/
│   │   │   ├── Product.cs                   # Aggregate Root
│   │   │   ├── StockMovement.cs
│   │   │   ├── InventoryLot.cs
│   │   │   ├── Warehouse.cs                 # Aggregate Root
│   │   │   ├── WarehouseZone.cs
│   │   │   ├── WarehouseRack.cs
│   │   │   ├── WarehouseShelf.cs
│   │   │   ├── WarehouseBin.cs
│   │   │   ├── Category.cs
│   │   │   ├── Supplier.cs
│   │   │   ├── Customer.cs
│   │   │   ├── Order.cs                     # Aggregate Root
│   │   │   ├── OrderItem.cs
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── Permission.cs
│   │   │   └── SyncLog.cs                   # YENİ — Entegrasyon log
│   │   │
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs                     # YENİ — Fiyat + Döviz birlikte
│   │   │   ├── Barcode.cs                   # YENİ — Barkod doğrulama mantığı
│   │   │   ├── SKU.cs                       # YENİ — SKU format kuralları
│   │   │   ├── UnitOfMeasure.cs             # YENİ — Birim dönüşümleri
│   │   │   ├── StockLevel.cs                # YENİ — Min/Max/Reorder kapsülleme
│   │   │   └── LocationCode.cs              # YENİ — Lokasyon adresleme
│   │   │
│   │   ├── Enums/
│   │   │   ├── StockMovementType.cs         # BİRLEŞTİRİLMİŞ — Enum+String tek yapı
│   │   │   ├── WarehouseType.cs
│   │   │   ├── OrderStatus.cs
│   │   │   ├── LotStatus.cs
│   │   │   └── SyncDirection.cs             # YENİ — Push/Pull/Bidirectional
│   │   │
│   │   ├── Events/                          # YENİ — Domain Events
│   │   │   ├── StockChangedEvent.cs         # Stok değiştiğinde
│   │   │   ├── ProductCreatedEvent.cs       # Ürün oluşturulduğunda
│   │   │   ├── ProductUpdatedEvent.cs       # Ürün güncellendiğinde
│   │   │   ├── PriceChangedEvent.cs         # Fiyat değiştiğinde
│   │   │   ├── OrderPlacedEvent.cs          # Sipariş oluştuğunda
│   │   │   ├── LowStockDetectedEvent.cs     # Düşük stok alarm
│   │   │   └── SyncRequestedEvent.cs        # Senkronizasyon talebi
│   │   │
│   │   ├── Interfaces/                      # Repository + Service kontratları
│   │   │   ├── IProductRepository.cs
│   │   │   ├── IStockMovementRepository.cs
│   │   │   ├── IWarehouseRepository.cs
│   │   │   ├── IOrderRepository.cs
│   │   │   ├── IUnitOfWork.cs               # YENİ — Transaction yönetimi
│   │   │   └── IDomainEventDispatcher.cs    # YENİ — Event dağıtımı
│   │   │
│   │   ├── Services/                        # Domain servisleri (saf iş kuralları)
│   │   │   ├── StockCalculationService.cs   # WAC, FEFO, stok seviye kontrol
│   │   │   ├── PricingService.cs            # Fiyatlama kuralları
│   │   │   └── BarcodeValidationService.cs  # Barkod doğrulama kuralları
│   │   │
│   │   └── Exceptions/
│   │       ├── InsufficientStockException.cs
│   │       ├── DuplicateSKUException.cs
│   │       └── InvalidBarcodeException.cs
│   │
│   ├── MesTech.Application/                 # KATMAN 2 — Use Case'ler
│   │   ├── Commands/                        # Yazma operasyonları
│   │   │   ├── AddStock/
│   │   │   │   ├── AddStockCommand.cs
│   │   │   │   └── AddStockHandler.cs
│   │   │   ├── RemoveStock/
│   │   │   ├── CreateProduct/
│   │   │   ├── UpdateProduct/
│   │   │   ├── TransferStock/
│   │   │   ├── AdjustStock/
│   │   │   ├── PlaceOrder/
│   │   │   └── SyncPlatform/               # YENİ — Entegrasyon komutu
│   │   │
│   │   ├── Queries/                         # Okuma operasyonları
│   │   │   ├── GetProductById/
│   │   │   ├── GetLowStockProducts/
│   │   │   ├── GetStockMovements/
│   │   │   ├── GetInventoryValue/
│   │   │   └── GetSyncStatus/              # YENİ — Entegrasyon durumu
│   │   │
│   │   ├── DTOs/                            # Veri transfer objeleri
│   │   │   ├── ProductDto.cs
│   │   │   ├── StockMovementDto.cs
│   │   │   ├── SyncResultDto.cs             # YENİ
│   │   │   └── PlatformMappingDto.cs        # YENİ
│   │   │
│   │   ├── Interfaces/                      # Dış dünya kontratları
│   │   │   ├── IIntegratorAdapter.cs        # YENİ — Platform adaptör arayüzü
│   │   │   ├── IIntegratorOrchestrator.cs   # YENİ — Çoklu platform orkestrasyon
│   │   │   ├── IConflictResolver.cs         # YENİ — Çakışma çözücü
│   │   │   ├── INotificationService.cs      # YENİ — Bildirim servisi
│   │   │   └── IReportService.cs
│   │   │
│   │   ├── Mapping/
│   │   │   └── MappingProfile.cs            # AutoMapper / Mapster profili
│   │   │
│   │   └── Behaviors/                       # Cross-cutting concerns
│   │       ├── LoggingBehavior.cs
│   │       ├── ValidationBehavior.cs
│   │       └── TransactionBehavior.cs
│   │
│   ├── MesTech.Infrastructure/              # KATMAN 4 — Altyapı
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs              # MEVCUT — Taşınacak
│   │   │   ├── Configurations/              # Fluent API konfigürasyonları
│   │   │   │   ├── ProductConfiguration.cs
│   │   │   │   ├── StockMovementConfiguration.cs
│   │   │   │   └── ... (her entity için ayrı dosya)
│   │   │   ├── Repositories/
│   │   │   │   ├── ProductRepository.cs
│   │   │   │   ├── StockMovementRepository.cs
│   │   │   │   └── ...
│   │   │   ├── UnitOfWork.cs
│   │   │   └── Migrations/
│   │   │
│   │   ├── Integration/                     # YENİ — Entegratör altyapısı
│   │   │   ├── Adapters/
│   │   │   │   ├── OpenCartAdapter.cs       # OpenCart IIntegratorAdapter impl.
│   │   │   │   ├── TrendyolAdapter.cs       # Trendyol IIntegratorAdapter impl.
│   │   │   │   └── ...                      # Gelecek platformlar
│   │   │   ├── IntegratorOrchestrator.cs
│   │   │   ├── ConflictResolver.cs
│   │   │   ├── SyncQueueService.cs          # BullMQ benzeri offline queue
│   │   │   └── DeltaSyncEngine.cs
│   │   │
│   │   ├── Services/
│   │   │   ├── DomainEventDispatcher.cs
│   │   │   ├── TokenRotationService.cs      # MEVCUT — Taşınacak
│   │   │   ├── CircuitBreakerService.cs     # MEVCUT — Taşınacak
│   │   │   └── LoggingService.cs            # MEVCUT — Taşınacak
│   │   │
│   │   ├── Security/
│   │   │   ├── AuthService.cs
│   │   │   ├── AuthorizationService.cs
│   │   │   └── SecretManager.cs             # YENİ — Vault/UserSecrets
│   │   │
│   │   └── DependencyInjection.cs           # Tüm Infrastructure DI kaydı
│   │
│   ├── MesTech.Desktop/                     # KATMAN 3 — WPF Presentation
│   │   ├── Views/                           # MEVCUT — 38 XAML (dokunulmaz)
│   │   ├── ViewModels/                      # MEVCUT — MVVM (refactor edilecek)
│   │   ├── Converters/
│   │   ├── Resources/
│   │   ├── App.xaml.cs                      # DI container setup
│   │   └── appsettings.json                 # Sadece UI ayarları
│   │
│   └── [Gelecek: MesTech.WebApi/]           # KATMAN 3 — Web API Presentation
│       ├── Controllers/
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── MesTech.Domain.Tests/                # Domain birim testleri
│   ├── MesTech.Application.Tests/           # Use case testleri
│   ├── MesTech.Infrastructure.Tests/        # Entegrasyon testleri
│   └── MesTech.Desktop.Tests/               # UI testleri (mevcut taşınacak)
│
├── docs/
│   ├── architecture/                        # Mimari dokümanlar
│   ├── emirnames/                           # Tüm emirnameler
│   └── api/                                 # API dokümantasyonu
│
└── MesTechStok.sln
```

### 2.3 Bağımlılık Kuralları (Değiştirilemez)

```
MesTech.Domain         → HİÇBİR ŞEYE bağımlı değil (saf C#, sıfır NuGet)
MesTech.Application    → Sadece Domain'e bağımlı
MesTech.Infrastructure → Domain + Application'a bağımlı
MesTech.Desktop        → Application + Infrastructure'a bağımlı (Domain'e DİREKT DEĞİL)
```

**Neden bu kadar katı?**
- Yarın Trendyol yazılımı eklendiğinde, `MesTech.Domain` ve `MesTech.Application` NuGet paketi olarak paylaşılır
- Yarın Web API eklendiğinde, sadece yeni bir Presentation projesi açılır — Domain ve Application hiç değişmez
- Test yazmak kolaylaşır — Domain'i izole test edebilirsin

---

## 3. DEMİR KURALLAR

Bu emirname boyunca geçerli olan mutlak kurallar:

1. **DOMAIN KATMANI TEMİZ KALACAK** — EF Core, HttpClient, Serilog gibi altyapı bağımlılıkları Domain'e ASLA girmez
2. **MEVCUT İŞLEVSELLİK KORUNACAK** — 14 stok operasyonu, 38 view, barkod sistemi aynen çalışmaya devam edecek
3. **BİR SEFERDE BİR KATMAN** — Önce Domain, sonra Application, sonra Infrastructure, en son Desktop refactor
4. **HER ADIM DERLENMELI** — Yarım kalan refactor kabul edilmez, her commit derlenir ve çalışır
5. **MEVCUT TESTLERİ GEÇMELİ** — xUnit testleri her adımda yeşil kalmalı
6. **GERİYE UYUMLULUK** — Desktop UI (XAML) bu fazda DEĞİŞMEZ, sadece ViewModel bağlantıları güncellenir
7. **KANITLI İLERLEME** — Her görev için ÖNCE/SONRA derleme çıktısı + test sonucu

---

## 4. GÖREV LİSTESİ (SIRALI)

### GÖREV 0.1: PostgreSQL Geçişi
**Amaç:** Veritabanını SQL Server/SQLite'dan PostgreSQL'e geçirmek
**Neden:** Komutan kararı — açık kaynak, MESA OS deneyimi, pgvector potansiyeli

**Adımlar:**
1. PostgreSQL 16 kurulumu (Windows veya Docker)
2. `appsettings.json` → `ConnectionStrings:DefaultConnection` PostgreSQL'e çevirme
3. EF Core provider değişikliği: `Npgsql.EntityFrameworkCore.PostgreSQL` birincil yapma
4. Yeni initial migration oluşturma (PostgreSQL dialect)
5. Mevcut verileri migrate etme (varsa)
6. SQLite'ı development fallback olarak tutma

**Kanıt:** `dotnet ef database update` başarılı + tüm CRUD operasyonları çalışıyor

**Dikkat:** 
- `Database:Provider` ayarı `PostgreSQL` olarak güncellenmeli
- `appsettings.Development.json` ayrı oluşturulmalı
- Connection string'de şifre **asla** commit edilmemeli → `.NET User Secrets` kullanılacak

---

### GÖREV 0.2: MesTech.Domain Projesi Oluşturma
**Amaç:** Saf domain katmanını ayırmak — hiçbir altyapı bağımlılığı olmadan

**Adımlar:**

1. Yeni proje oluştur:
```bash
dotnet new classlib -n MesTech.Domain -f net9.0
dotnet sln add src/MesTech.Domain/MesTech.Domain.csproj
```

2. **Entity'leri taşı** (Core/Data/Models/ → Domain/Entities/):
   - `Product.cs` — Navigation property'ler ve EF attribute'ları KALDIRILACAK, saf POCO
   - `StockMovement.cs`
   - `InventoryLot.cs`
   - `Warehouse.cs`, `WarehouseZone.cs`, `WarehouseRack.cs`, `WarehouseShelf.cs`, `WarehouseBin.cs`
   - `Category.cs`, `Supplier.cs`, `Customer.cs`
   - `Order.cs`, `OrderItem.cs`
   - `User.cs`, `Role.cs`, `Permission.cs`, `UserRole.cs`, `RolePermission.cs`
   - Audit modelleri: `AccessLog.cs`, `LogEntry.cs`, `ApiCallLog.cs`, `BarcodeScanLog.cs`
   - Queue modelleri: `OfflineQueueItem.cs`, `SyncRetryItem.cs`
   - Settings: `CompanySettings.cs`

3. **Entity temizliği** — Her entity'den kaldırılacaklar:
   - `[Required]`, `[MaxLength]`, `[Column]` gibi EF/DataAnnotation attribute'ları → Bunlar Infrastructure'daki Fluent API'ye taşınacak
   - `virtual` navigation property'ler → Interface'lerle erişilecek
   - EF-spesifik `ICollection<>` → Domain'de sadece `IReadOnlyCollection<>`

4. **Enum birleştirmesi** — `StockMovementType`:
```csharp
// Domain/Enums/StockMovementType.cs
public enum StockMovementType
{
    // Giriş hareketleri
    StockIn = 1,
    Purchase = 10,
    BarcodeReceive = 11,
    Production = 12,
    Return = 13,
    Found = 14,
    
    // Çıkış hareketleri
    StockOut = 2,
    Sale = 20,
    BarcodeSale = 21,
    Consumption = 22,
    Loss = 23,
    
    // Düzeltme/Transfer
    Adjustment = 3,
    Transfer = 30,
    
    // Entegrasyon
    OpenCartSync = 6,
    TrendyolSync = 60,        // YENİ
    MarketplaceSync = 61,     // YENİ — Genel pazaryeri
    
    // Uyumluluk (deprecated — geçiş sürecinde)
    [Obsolete("Use StockIn instead")]
    In = 7,
    [Obsolete("Use StockOut instead")]
    Out = 8
}
```

5. **Value Object'leri oluştur:**

```csharp
// Domain/ValueObjects/Money.cs
public record Money(decimal Amount, string Currency = "TRY")
{
    public static Money TRY(decimal amount) => new(amount, "TRY");
    public static Money USD(decimal amount) => new(amount, "USD");
    public static Money EUR(decimal amount) => new(amount, "EUR");
}

// Domain/ValueObjects/StockLevel.cs
public record StockLevel(int Current, int Minimum, int Maximum, int ReorderLevel, int ReorderQuantity)
{
    public bool IsLow => Current <= Minimum;
    public bool IsCritical => Current <= ReorderLevel / 2;
    public bool NeedsReorder => Current <= ReorderLevel;
    public int ReorderAmount => NeedsReorder ? ReorderQuantity : 0;
}
```

6. **Repository Interface'leri tanımla:**
```csharp
// Domain/Interfaces/IProductRepository.cs
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetBySKUAsync(string sku);
    Task<Product?> GetByBarcodeAsync(string barcode);
    Task<IReadOnlyList<Product>> GetLowStockAsync();
    Task<IReadOnlyList<Product>> GetByCategoryAsync(int categoryId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
}
```

7. **Domain Event'leri oluştur:**
```csharp
// Domain/Events/StockChangedEvent.cs
public record StockChangedEvent(
    int ProductId,
    string SKU,
    int PreviousQuantity,
    int NewQuantity,
    StockMovementType MovementType,
    DateTime OccurredAt
) : IDomainEvent;
```

**Kanıt:**
- `dotnet build MesTech.Domain` → 0 error, 0 warning
- Domain projesi `<PackageReference>` bölümü BOŞ (sıfır NuGet bağımlılığı)
- `dotnet list MesTech.Domain package` → "No packages found"

---

### GÖREV 0.3: MesTech.Application Projesi Oluşturma
**Amaç:** Use Case katmanını oluşturmak — Domain'e bağımlı, altyapıdan bağımsız

**Adımlar:**

1. Yeni proje:
```bash
dotnet new classlib -n MesTech.Application -f net9.0
dotnet sln add src/MesTech.Application/MesTech.Application.csproj
dotnet add src/MesTech.Application reference src/MesTech.Domain
```

2. **Bağımlılıklar (sadece bunlar):**
```xml
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="FluentValidation" Version="11.*" />
<PackageReference Include="Mapster" Version="7.*" />  <!-- veya AutoMapper -->
```

3. **Entegratör Interface'leri tanımla:**
```csharp
// Application/Interfaces/IIntegratorAdapter.cs
public interface IIntegratorAdapter
{
    string PlatformName { get; }
    string PlatformCode { get; }  // "opencart", "trendyol", "n11"
    bool IsEnabled { get; }
    
    // Bağlantı
    Task<HealthCheckResult> CheckHealthAsync();
    Task<bool> TestConnectionAsync();
    
    // Ürün
    Task<SyncResult> PushProductAsync(ProductDto product);
    Task<SyncResult> PushBulkProductsAsync(IEnumerable<ProductDto> products);
    Task<IReadOnlyList<ExternalProductDto>> PullProductsAsync(DateTime? since = null);
    
    // Stok
    Task<SyncResult> PushStockUpdateAsync(string sku, int quantity);
    Task<SyncResult> PushBulkStockUpdateAsync(IDictionary<string, int> skuQuantities);
    
    // Fiyat
    Task<SyncResult> PushPriceUpdateAsync(string sku, decimal price);
    
    // Sipariş
    Task<IReadOnlyList<ExternalOrderDto>> PullOrdersAsync(DateTime? since = null);
    Task<SyncResult> PushOrderStatusAsync(string externalOrderId, string status);
    
    // Katalog
    Task<SyncResult> FullCatalogSyncAsync();
    Task<SyncResult> DeltaSyncAsync(DateTime since);
}

// Application/Interfaces/IIntegratorOrchestrator.cs
public interface IIntegratorOrchestrator
{
    IReadOnlyList<IIntegratorAdapter> RegisteredAdapters { get; }
    
    Task RegisterAdapterAsync(IIntegratorAdapter adapter);
    Task RemoveAdapterAsync(string platformCode);
    
    Task<OrchestratorResult> SyncAllPlatformsAsync();
    Task<SyncResult> SyncPlatformAsync(string platformCode);
    
    Task<IReadOnlyList<ConflictDto>> DetectConflictsAsync();
    Task<SyncResult> ResolveConflictAsync(int conflictId, ConflictResolution resolution);
    
    // Event-driven: Stok değiştiğinde otomatik tüm platformlara push
    Task HandleStockChangedAsync(StockChangedEvent domainEvent);
    Task HandlePriceChangedAsync(PriceChangedEvent domainEvent);
    Task HandleProductCreatedAsync(ProductCreatedEvent domainEvent);
}
```

4. **İlk Command/Query örnekleri:**
```csharp
// Application/Commands/AddStock/AddStockCommand.cs
public record AddStockCommand(
    int ProductId,
    int Quantity,
    decimal UnitCost,
    string? BatchNumber,
    DateTime? ExpiryDate,
    string? DocumentNumber,
    bool SyncToPlatforms = true     // Entegratörlere push et
) : IRequest<AddStockResult>;

// Application/Commands/SyncPlatform/SyncPlatformCommand.cs
public record SyncPlatformCommand(
    string PlatformCode,            // "opencart", "trendyol"
    SyncDirection Direction,        // Push, Pull, Bidirectional
    DateTime? Since = null          // Delta sync başlangıç
) : IRequest<SyncResult>;
```

**Kanıt:**
- `dotnet build MesTech.Application` → 0 error
- Application → Domain referansı var, Infrastructure referansı **YOK**

---

### GÖREV 0.4: MesTech.Infrastructure Projesi Oluşturma
**Amaç:** Mevcut Core katmanını Infrastructure'a dönüştürmek

**Adımlar:**

1. `MesTechStok.Core` → `MesTech.Infrastructure` olarak yeniden adlandır/oluştur
2. Referanslar:
```bash
dotnet add src/MesTech.Infrastructure reference src/MesTech.Domain
dotnet add src/MesTech.Infrastructure reference src/MesTech.Application
```

3. **Taşınacak parçalar:**

| Kaynak (Core) | Hedef (Infrastructure) | Not |
|---------------|----------------------|-----|
| `Data/AppDbContext.cs` | `Persistence/AppDbContext.cs` | EF konfigürasyonu burada kalır |
| `Data/Models/*.cs` | ❌ KALDIRILDI | Entity'ler Domain'e taşındı |
| `Services/Abstract/I*.cs` | ❌ KALDIRILDI | Interface'ler Domain'e taşındı |
| `Services/Concrete/*.cs` | `Services/*.cs` | Application interface'lerini implement eder |
| `Services/Barcode/*` | `Services/Barcode/*` | Donanım bağımlı → Infrastructure |
| `Services/Resilience/*` | `Services/Resilience/*` | Polly → Infrastructure |
| `Services/Security/*` | `Security/*` | Auth → Infrastructure |
| `Services/MultiTenant/*` | `MultiTenant/*` | Şimdilik aynen taşınır |
| `OpenCartClient.cs` | `Integration/Adapters/OpenCartAdapter.cs` | BOŞ dosya → implement edilecek |
| `DeltaSyncService.cs` | `Integration/DeltaSyncEngine.cs` | Entegratör altyapısı |
| `SyncRetryService.cs` | `Integration/SyncQueueService.cs` | Queue mekanizması |

4. **Repository implementasyonları oluştur:**
```csharp
// Infrastructure/Persistence/Repositories/ProductRepository.cs
public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    
    public ProductRepository(AppDbContext context) => _context = context;
    
    public async Task<Product?> GetByIdAsync(int id)
        => await _context.Products.FindAsync(id);
    
    // ... diğer metotlar
}
```

5. **UnitOfWork pattern:**
```csharp
// Infrastructure/Persistence/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _dispatcher;
    
    public async Task<int> SaveChangesAsync()
    {
        var events = _context.GetDomainEvents();  // Entity'lerdeki olayları topla
        var result = await _context.SaveChangesAsync();
        await _dispatcher.DispatchAsync(events);  // Olayları yayınla
        return result;
    }
}
```

**Kanıt:**
- `dotnet build MesTech.Infrastructure` → 0 error
- Infrastructure → Domain + Application referansı var
- Infrastructure → Desktop referansı **YOK** (önceden Core → Desktop bağımlılığı varsa koparılmış olmalı)

---

### GÖREV 0.5: Desktop Katmanı Bağlama
**Amaç:** WPF Desktop'u yeni mimariyle çalışır hale getirmek

**Adımlar:**

1. Desktop referanslarını güncelle:
```bash
dotnet add src/MesTech.Desktop reference src/MesTech.Application
dotnet add src/MesTech.Desktop reference src/MesTech.Infrastructure
# Domain'e DİREKT referans VERMEMEK tercih edilir ama geçiş sürecinde gerekebilir
```

2. **Desktop servis temizliği — Duplikasyon çözümü:**

| Desktop Servis | Karar | Gerekçe |
|---------------|-------|---------|
| `MockProductService.cs` | 🗑️ SİL | Test'e taşınacak |
| `MockInventoryService.cs` | 🗑️ SİL | Test'e taşınacak |
| `MockOrderService.cs` | 🗑️ SİL | Test'e taşınacak |
| `MockReportsService.cs` | 🗑️ SİL | Test'e taşınacak |
| `MockCustomerService.cs` | 🗑️ SİL | Test'e taşınacak |
| `MockWarehouseService.cs` | 🗑️ SİL | Test'e taşınacak |
| `MockAuthService.cs` | 🗑️ SİL | Test'e taşınacak |
| `SqlBackedProductService.cs` | 🔄 ABSORB | Mantığı Infrastructure'a taşınacak |
| `SqlBackedInventoryService.cs` | 🔄 ABSORB | Mantığı Infrastructure'a taşınacak |
| `SqlBackedOrderService.cs` | 🔄 ABSORB | Mantığı Infrastructure'a taşınacak |
| `SqlBackedReportsService.cs` | 🔄 ABSORB | Mantığı Infrastructure'a taşınacak |
| `EnhancedProductService.cs` | 🔄 FACADE | Application Command/Query'lere facade olacak |
| `EnhancedInventoryService.cs` | 🔄 FACADE | Application Command/Query'lere facade olacak |
| `EnhancedOrderService.cs` | 🔄 FACADE | Application Command/Query'lere facade olacak |
| `EnhancedReportsService.cs` | 🔄 FACADE | Application Command/Query'lere facade olacak |
| `EnhancedCustomerService.cs` | 🔄 FACADE | Application Command/Query'lere facade olacak |
| `OpenCartHttpService.cs` | 🔄 TAŞI | Infrastructure/Integration/Adapters/ |
| `OpenCartQueueWorker.cs` | 🔄 TAŞI | Infrastructure/Integration/ |
| `OpenCartHealthService.cs` | 🔄 TAŞI | Infrastructure/Integration/ |
| `BarcodeHardwareService.cs` | ✅ KALIR | Donanım erişimi — Desktop'a özel |
| `GlobalBarcodeService.cs` | ✅ KALIR | UI-bound barkod servisi |
| `DatabaseService.cs` | 🔄 TAŞI | Infrastructure'a |
| `PdfReportService.cs` | 🔄 TAŞI | Infrastructure'a |

3. **DI Container güncelleme** (`App.xaml.cs`):
```csharp
services.AddScoped<IProductRepository, ProductRepository>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AddStockCommand).Assembly));
services.AddScoped<IIntegratorOrchestrator, IntegratorOrchestrator>();
// Adapter'lar
services.AddScoped<IIntegratorAdapter, OpenCartAdapter>();
```

4. **ViewModel güncelleme** — MediatR ile:
```csharp
// Eski:
await _inventoryService.AddStockAsync(productId, quantity, cost);

// Yeni:
await _mediator.Send(new AddStockCommand(productId, quantity, cost));
```

**Kanıt:**
- Uygulama başlıyor (`MesTechStok_Başlat.bat`)
- Dashboard görünüyor
- Ürün CRUD çalışıyor
- Stok ekleme/çıkarma çalışıyor
- Barkod tarama çalışıyor
- Tüm xUnit testleri geçiyor

---

### GÖREV 0.6: Secret Management
**Amaç:** Hassas verileri appsettings.json'dan ayırmak

**Adımlar:**

1. `.NET User Secrets` aktifleştir:
```bash
dotnet user-secrets init --project src/MesTech.Desktop
```

2. Hassas verileri taşı:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=mestech_stok;Username=mestech_user;Password=XXXXX"
dotnet user-secrets set "OpenCartSettings:ApiKey" "XXXXX"
dotnet user-secrets set "WeatherApiSettings:OpenWeatherMapApiKey" "XXXXX"
```

3. `appsettings.json`'dan hassas değerleri kaldır, sadece yapı kalacak:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "** USER SECRETS **"
  }
}
```

4. Ortam dosyaları oluştur:
```
appsettings.json                  # Ortak ayarlar
appsettings.Development.json      # Dev ortamı (SQLite fallback)
appsettings.Production.json       # Prod ortamı (PostgreSQL)
```

5. `.gitignore` güncelle:
```
appsettings.*.json
!appsettings.json
!appsettings.Development.json.template
```

**Kanıt:** `appsettings.json`'da hiçbir şifre/key yok + uygulama User Secrets ile çalışıyor

---

### GÖREV 0.7: Domain Event Mekanizması
**Amaç:** Stok değişikliklerinin otomatik olarak entegratörlere yayılması için altyapı

**Bu neden kritik?**
Yarın OpenCart adapter'ı hazır olduğunda, stok değiştiği anda otomatik olarak:
1. `StockChangedEvent` fırlatılır
2. `IntegratorOrchestrator` bu event'i yakalar
3. Kayıtlı tüm adapter'lara (OpenCart, Trendyol, ...) push yapar

Bu sayede **Desktop UI hiçbir şey bilmez** — sadece "stok ekle" der, gerisini event sistemi halleder.

**Adımlar:**

1. Domain'de event interface:
```csharp
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
    
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

2. Product entity'ye event entegrasyonu:
```csharp
public class Product : BaseEntity
{
    public void AdjustStock(int quantity, StockMovementType type)
    {
        var previous = Stock;
        Stock += quantity;
        RaiseDomainEvent(new StockChangedEvent(Id, SKU, previous, Stock, type, DateTime.UtcNow));
    }
}
```

3. Infrastructure'da dispatcher:
```csharp
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    
    public async Task DispatchAsync(IEnumerable<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
            await _mediator.Publish(domainEvent);
    }
}
```

**Kanıt:** Birim testi — stok değiştiğinde event fırlatılıyor, handler çağrılıyor

---

## 5. TESLİMAT FORMATI

Her görev tamamlandığında aşağıdaki formatla rapor edilecek:

```
=== GÖREV [0.X] TAMAMLAMA RAPORU ===

DURUM: ✅ TAMAMLANDI / ⚠️ KISMİ / ❌ BAŞARISIZ

YAPILAN İŞLEMLER:
1. [İşlem açıklaması]
   KANIT: [komut + çıktı]
2. ...

DERLEME SONUCU:
$ dotnet build
[çıktı]

TEST SONUCU:
$ dotnet test
[çıktı]

OLUŞTURULAN/DEĞİŞEN DOSYALAR:
- [dosya yolu] — [ne yapıldı]
- ...

YAN ETKİLER:
- [varsa belirt]

BLOCKER/SORUN:
- [varsa belirt]
```

---

## 6. UYGULAMA SIRASI & BAĞIMLILIKLAR

```
GÖREV 0.1 (PostgreSQL)
    │
    ▼
GÖREV 0.2 (Domain) ──────────────────────┐
    │                                      │
    ▼                                      │
GÖREV 0.3 (Application) ─────────────┐    │
    │                                  │    │
    ▼                                  │    │
GÖREV 0.4 (Infrastructure) ◄──────────┘    │
    │                                       │
    ▼                                       │
GÖREV 0.5 (Desktop Bağlama) ◄──────────────┘
    │
    ▼
GÖREV 0.6 (Secret Management) ── Paralel çalışabilir
    │
    ▼
GÖREV 0.7 (Domain Events) ── 0.2 + 0.4 tamamlandıktan sonra
```

---

## 7. BAŞARI KRİTERLERİ — FAZ 0 TAMAMLANMA ŞARTLARI

Faz 0 tamamlandı sayılması için TÜMÜ sağlanmalıdır:

| # | Kriter | Doğrulama |
|---|--------|-----------|
| K1 | Solution 4+ proje içeriyor (Domain, Application, Infrastructure, Desktop) | `dotnet sln list` |
| K2 | Domain projesinin sıfır NuGet bağımlılığı var | `dotnet list package` |
| K3 | Tüm Entity'ler Domain katmanında | `find Domain/Entities -name "*.cs"` |
| K4 | Repository interface'leri Domain'de, implementasyonları Infrastructure'da | Dosya konumu |
| K5 | Desktop'ta Mock servis kalmamış | `find Desktop -name "Mock*"` → boş |
| K6 | StockMovementType tek bir enum olarak birleştirilmiş | Enum dosyası |
| K7 | PostgreSQL birincil DB olarak çalışıyor | `psql` bağlantı testi |
| K8 | appsettings.json'da şifre/key yok | `grep -i "password\|apikey" appsettings.json` |
| K9 | Uygulama başlıyor ve temel CRUD çalışıyor | Manuel test |
| K10 | IIntegratorAdapter interface'i tanımlı | Dosya mevcut |
| K11 | Domain Event mekanizması çalışıyor | Birim testi yeşil |
| K12 | Tüm mevcut xUnit testleri geçiyor | `dotnet test` |

---

## 8. KOMUTAN NOTU

Bu Faz 0, projenin en zorlu ama en değerli fazıdır. 399 dosyalık bir kod tabanını yeniden yapılandırıyoruz — ama bunu yaparken **tek bir işlevsellik kaybetmeden** yapmalıyız.

MESA OS'ta öğrendiğimiz dersi burada da uyguluyoruz: temeli sağlam atmazsan, üzerine ne koysan çatlar.

Faz 0 tamamlandığında elimizde:
- Herhangi bir platformla konuşabilen entegratör arayüzleri
- Stok değişikliklerini otomatik yayan event sistemi
- Yarın Trendyol, N11, Hepsiburada adapter'ı 1 hafta değil 1 günde eklenebilecek bir altyapı
- Yarın Web API açılsa Domain ve Application hiç değişmeyecek bir mimari

**Bu temeli doğru atmak, 6 ay sonra 6 haftalık iş tasarrufu demektir.**

---

**EMİRNAME SONU**  
**Komutan MesTech — "Temel sağlamsa bina sağlamdır."**
