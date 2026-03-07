# BİRLEŞİK ÇAPRAZ ANALİZ — STOK + TRENDYOL
## Entegratör Yazılımı Mimari Karar Dokümanı

**Belge No:** ENT-UNIFIED-001  
**Tarih:** 05 Mart 2026  
**Analist:** Komutan (Claude Opus 4.6)  
**Kaynaklar:** ENT-STOK-001 Keşif Raporu (790 satır) + ENT-TRENDYOL-001 Keşif Raporu (810 satır)  
**Durum:** KESİN KARARLAR — Faz 0 bu doküman üzerinden devam eder

---

## 1. İKİ DÜNYANIN RÖNTGEN KARŞILAŞTIRMASI

### 1.1 Büyük Tablo

| Boyut | MesTech_Stok | MesTech_Trendyol | Sonuç |
|-------|-------------|------------------|-------|
| **Platform** | WPF Desktop (.exe) | Web API + NestJS + HTML | İKİ FARKLI DÜNYA |
| **Framework** | .NET 9.0 (Sdk) | .NET 9.0 (Web) + Node.js 18 | Çekirdek ortak, kabuk farklı |
| **Proje sayısı** | 3 (Core/Desktop/Tests) | 7+ C# + 3 Node.js | Trendyol daha dağınık |
| **Mimari desen** | MVVM (tek katman DI) | Clean Arch + CQRS + MediatR | Trendyol daha modern |
| **Veritabanı** | SQL Server + PostgreSQL + SQLite | SQL Server + PostgreSQL | PostgreSQL ortak payda |
| **ORM** | EF Core 9.0.6 | EF Core 8.0.0 + Prisma 5.7 | EF Core ortak payda |
| **Model sayısı** | 32 tablo | 7 tablo | Stok çok daha zengin |
| **UI** | 38 WPF View | 52 HTML sayfa | Birleştirilemez |
| **Cache** | Yok | Redis 7 | Stok'a eklenecek |
| **Message Bus** | Yok | RabbitMQ + MassTransit | Stok'a eklenecek |
| **Background Jobs** | DispatcherTimer | Hangfire (5 recurring) | Hangfire'a geçiş |
| **Docker** | Yok | Tam (Compose + CI/CD) | Standart olacak |
| **CI/CD** | Yok | GitHub Actions | Standart olacak |
| **Resilience** | Polly 8.2 (aktif Circuit Breaker) | Polly 7.2 (config var, aktif değil) | Stok daha olgun |
| **Secret Mgmt** | appsettings.json (düz metin) | appsettings.json + .env (düz metin) | İkisi de zayıf |
| **Auth** | BCrypt + RBAC (tam) | Yok (API key bazlı) | Stok daha olgun |
| **Test** | xUnit (kapsam bilinmiyor) | xUnit (Unit + Integration ayrı) | Trendyol daha yapılandırılmış |

### 1.2 Product Modeli — Alan Bazlı Çapraz Eşleştirme

Bu tablo Domain entity tasarımının temelidir:

| Alan Grubu | MesTech_Stok | MesTech_Trendyol | Unified Domain Kararı |
|-----------|-------------|------------------|----------------------|
| **PK Tipi** | int (auto-increment) | Guid | **Guid** — dağıtık sistemde daha güvenli |
| **SKU** | string(50) UNIQUE | string(100) UNIQUE | **string(100) UNIQUE** — geniş olan alınır |
| **Barkod** | Barcode + GTIN + UPC + EAN (4 alan) | Barcode (1 alan) | **Barcode (ana) + ayrı BarcodeType tablosu** |
| **İsim** | Name string(200) | Name string(500) | **string(500)** |
| **Açıklama** | Description (sınırsız) | Description string(2000) | **string (sınırsız)** |
| **Alış Fiyatı** | PurchasePrice decimal(18,2) | Yok | **Kalır — Stok Domain'e özel** |
| **Satış Fiyatı** | SalePrice decimal(18,2) | Price decimal(18,2) | **SalePrice decimal(18,2)** |
| **İndirimli Fiyat** | Yok (DiscountRate var) | SalePrice (nullable) | **DiscountedPrice decimal?(nullable)** |
| **Liste Fiyatı** | ListPrice decimal(18,2) | Yok | **Kalır — opsiyonel** |
| **Stok** | Stock int | StockQuantity int | **Stock int** |
| **Min Stok** | MinimumStock int(5) | MinimumStock int | **MinimumStock int** |
| **Max Stok** | MaximumStock int(1000) | MaximumStockLevel int | **MaximumStock int** |
| **Reorder** | ReorderLevel + ReorderQuantity | Yok | **Kalır — Stok Domain'e özel** |
| **Vergi** | TaxRate decimal(5,2) %18 | VatRate int %18 | **TaxRate decimal(5,2)** |
| **Ağırlık** | Weight decimal (kg) | Weight decimal (gram) | **Weight decimal + WeightUnit enum (KG/GRAM)** |
| **Boyut** | Length/Width/Height ayrı | Dimensions string | **Length/Width/Height ayrı — daha yapısal** |
| **Kategori** | CategoryId FK → Category | Category string(200) | **CategoryId FK — düz metin değil** |
| **Tedarikçi** | SupplierId FK → Supplier | Yok | **SupplierId FK — Kalır** |
| **Renk/Beden** | Yok (VariantProcessor var) | Color/Size string | **Ayrı Variant sistemi — Faz 4** |
| **Görsel** | ImageStorageService (dosya) | ImageUrls JSON string | **Ayrı ProductImage tablosu** |
| **SEO** | Yok | Slug/MetaTitle/MetaDescription | **Platform-özel — mapping tablosunda** |
| **Analitik** | Yok | Rating/Review/Sales/ViewCount | **Platform-özel — mapping tablosunda** |
| **Lot/Parti** | IsBatchTracked + InventoryLot | Yok | **Kalır — Stok Domain'e özel** |
| **Seri No** | IsSerialized | Yok | **Kalır — Stok Domain'e özel** |
| **Son Kullanma** | IsPerishable + ExpiryDate | Yok | **Kalır — Stok Domain'e özel** |
| **Soft Delete** | Yok (IsActive + IsDiscontinued) | IsDeleted + DeletedAt + DeletedBy | **IsDeleted + DeletedAt — daha standart** |
| **Audit** | CreatedBy/ModifiedBy + Date | CreatedBy/UpdatedBy + At + Deleted | **Full audit: Created/Updated/Deleted + By + At** |

### 1.3 Stok Yönetimi Karşılaştırması

| Özellik | MesTech_Stok | MesTech_Trendyol | Unified |
|---------|-------------|------------------|---------|
| Çoklu depo | ✅ 6 tip | ❌ Tek sayaç | Stok lider — çoklu depo |
| Hareket kaydı | ✅ StockMovement tablosu | ❌ Yok | Stok lider — hareket sistemi |
| Lot/FEFO | ✅ InventoryLot | ❌ Yok | Stok lider |
| WAC maliyet | ✅ Formül aktif | ❌ Yok | Stok lider |
| Lokasyon | ✅ Zone→Rack→Shelf→Bin (devre dışı) | ❌ Yok | Stok lider |
| Platform sync | ⚠️ OpenCart (yarım) | ✅ Trendyol (tam) | Trendyol lider — adapter pattern |
| Background sync | ⚠️ DispatcherTimer | ✅ Hangfire 5 job | Trendyol lider — Hangfire |
| Batch update | ⚠️ BulkStockUpdate | ✅ 100'lü batch API | Trendyol lider — batch strateji |

**Sonuç:** Stok yönetiminin kalbi (depo, hareket, lot, maliyet) Stok reposundan gelecek. Platform senkronizasyonunun kalbi (adapter, batch, Hangfire, webhook) Trendyol reposundan gelecek.

---

## 2. KESİN MİMARİ KARARLAR

### KARAR 1: ProductPlatformMapping — SEÇENEk B KESİNLEŞTİ ✅

Trendyol keşfi bunu kesin olarak doğruladı. Trendyol'da zaten `TrendyolProductMapping` tablosu var — bu yaklaşım zaten test edilmiş.

```
KESİN TASARIM:

ProductPlatformMapping
├── Id: Guid (PK)
├── ProductId: Guid (FK → Product)
├── PlatformCode: string(50)           // "opencart", "trendyol", "n11", "hepsiburada"
├── ExternalProductId: string(200)     // Platformun kendi ürün ID'si
├── ExternalSKU: string(100)           // Platformun kendi SKU'su (TrendyolSku gibi)
├── ExternalCategoryId: string(100)    // Platformun kendi kategori ID'si
├── ExternalBrandId: string(100)       // Platformun kendi marka ID'si
├── ExternalUrl: string(1000)          // Platform ürün URL'i
├── PlatformSpecificData: jsonb        // Platform-özel alanlar (SEO, analitik vb.)
├── SyncStatus: enum                   // NotSynced/Syncing/Synced/Failed/PendingSync
├── LastSyncedAt: DateTime?
├── SyncErrorMessage: string?
├── IsActive: bool
├── CreatedAt: DateTime
├── UpdatedAt: DateTime
UNIQUE CONSTRAINT: (ProductId, PlatformCode)
```

**Neden jsonb?** Her platformun kendine özel alanları var:
- Trendyol: TrendyolApproved, RejectionReason, CargoDeliveryTime
- OpenCart: OpenCartCategoryId, SortOrder
- N11: N11ProductStatus, N11ShipmentTemplate

Bunları entity'ye statik alan olarak koymak yerine, platform-özel veriler `PlatformSpecificData` jsonb alanında saklanır. **PostgreSQL jsonb indeksleme desteği** ile sorgulanabilir kalır.

### KARAR 2: PK Tipi — Guid'e Geçiş

| Gerekçe | Açıklama |
|---------|----------|
| Dağıtık sistem | İki farklı DB, ileride daha fazla — int çakışabilir |
| Trendyol uyumu | Trendyol zaten Guid kullanıyor |
| URL güvenliği | `/products/1` vs `/products/a8f3b2c1-...` — Guid tahmin edilemez |
| NuGet paylaşımı | Shared Domain paketi int'e bağımlı olmamalı |

**Migration stratejisi:** Mevcut int PK'lar korunur, yeni Guid PK eklenir. Geçiş döneminde ikisi birlikte yaşar, sonra int deprecated olur.

### KARAR 3: Multi-User Altyapı (Login Şimdi Yok, Altyapı Hazır)

Komutanın talimatı: "Login olayına girmeyeceğiz, sistemi her şeyiyle bitirdiğimiz zaman login yapacağız. Ama çoklu kullanıcı yapısına uyumlu olmalıyız."

**Bu ne demek mimari olarak:**

```csharp
// Domain/Interfaces/ICurrentUserService.cs
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool IsAuthenticated { get; }
    Guid? TenantId { get; }          // Multi-tenant hazırlık
}

// ŞİMDİ: Geliştirme sürecinde sabit kullanıcı
// Infrastructure/Services/DevelopmentUserService.cs
public class DevelopmentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("00000000-0000-0000-0000-000000000001");
    public string? Username => "developer";
    public string? Email => "dev@mestech.com";
    public IReadOnlyList<string> Roles => new[] { "Admin" };
    public IReadOnlyList<string> Permissions => new[] { "*" };  // Tam yetki
    public bool IsAuthenticated => true;
    public Guid? TenantId => null;
}

// YARIN: Login eklendiğinde sadece bu implementasyon değişir
// Infrastructure/Services/JwtUserService.cs  (gelecek)
// Infrastructure/Services/WindowsAuthUserService.cs  (gelecek)
```

**Audit trail hazırlığı — şimdiden her entity'de:**
```csharp
// Domain/Entities/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    
    // Audit — ICurrentUserService'den dolacak
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
    
    // Soft Delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Domain Events
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**SaveChanges interceptor — audit otomatik dolar:**
```csharp
// Infrastructure/Persistence/AuditInterceptor.cs
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _user;
    
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
            entry.Entity.UpdatedBy = _user.Username ?? "system";
            
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedBy = _user.Username ?? "system";
            }
        }
        return base.SavingChangesAsync(...);
    }
}
```

**Login eklendiğinde ne değişecek?**
1. `DevelopmentUserService` → `JwtUserService` veya `WindowsAuthUserService` ile değiştirilir
2. DI kaydı güncellenir: `services.AddScoped<ICurrentUserService, JwtUserService>()`
3. Login UI eklenir (WPF: LoginWindow zaten var, Web: JWT middleware)
4. **Domain, Application, Infrastructure katmanları HİÇ DEĞİŞMEZ** — sadece implementasyon swap

Bu yaklaşımla RBAC altyapısı (MesTech_Stok'ta zaten User → Role → Permission zinciri var) Login günü geldiğinde tak-çıkar şeklinde aktifleşir.

### KARAR 4: Birleşme Stratejisi — Shared Domain NuGet + Event Bus

İki repo birleştirilmeyecek. Her biri kendi yaşam döngüsünü koruyacak. Ortak paylaşım noktaları:

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                    MesTech.Domain (NuGet)                        │
│              Ortak Entity + Interface + Event                    │
│                                                                 │
│    Product, BaseEntity, IIntegratorAdapter,                      │
│    ProductPlatformMapping, StockMovementType,                    │
│    IDomainEvent, ICurrentUserService                            │
│                                                                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
              NuGet referans (her iki repo)
                         │
         ┌───────────────┴───────────────┐
         │                               │
┌────────┴────────┐            ┌─────────┴────────┐
│  MesTech_Stok   │            │ MesTech_Trendyol │
│  (Desktop WPF)  │            │  (Web API)       │
│                 │            │                  │
│  ✅ Stok yönetim│            │  ✅ Trendyol API │
│  ✅ OpenCart     │            │  ✅ Sipariş/İade │
│  ✅ Depo/Lot    │            │  ✅ Hangfire     │
│  ✅ Barkod      │            │  ✅ Redis cache  │
│  ✅ Raporlama   │            │  ✅ Web Dashboard│
│                 │            │                  │
│  PostgreSQL     │            │  PostgreSQL      │
│  (mestech_stok) │            │  (mestech_trndyl)│
└────────┬────────┘            └─────────┬────────┘
         │                               │
         └───────────┬───────────────────┘
                     │
              ┌──────┴──────┐
              │  RabbitMQ   │
              │  Event Bus  │
              │             │
              │  Events:    │
              │  • StockChanged     │
              │  • PriceChanged     │
              │  • ProductCreated   │
              │  • OrderReceived    │
              │  • SyncRequested    │
              └─────────────┘
```

**Veri akışı örnekleri:**

**Senaryo 1: Stok Desktop'ta değişti**
1. Kullanıcı Desktop'ta stok ekler → `AddStockCommand`
2. Domain event fırlatılır → `StockChangedEvent`
3. Event handler RabbitMQ'ya publish eder
4. Trendyol servisi consume eder → `TrendyolAdapter.PushStockUpdateAsync()`
5. Trendyol API'ye güncelleme gönderilir
6. SyncLog kaydedilir

**Senaryo 2: Trendyol'dan sipariş geldi**
1. Hangfire job siparişleri çeker → `OrderReceivedEvent`
2. RabbitMQ'ya publish edilir
3. Stok Desktop consume eder → stok düşer
4. Stok düşüşü → `StockChangedEvent` → OpenCart'a da push

### KARAR 5: Altyapı Standardizasyonu

Trendyol'un üstün olduğu altyapılar Stok tarafına da taşınacak:

| Altyapı | Mevcut (Stok) | Hedef | Kaynak |
|---------|---------------|-------|--------|
| Background Jobs | DispatcherTimer | **Hangfire** | Trendyol'dan öğrenilecek |
| Cache | Yok | **Redis 7** | Trendyol'dan taşınacak |
| Message Bus | Yok | **RabbitMQ + MassTransit** | Trendyol'dan taşınacak |
| Docker | Yok | **Docker Compose** | Trendyol'dan adapte edilecek |
| CI/CD | Yok | **GitHub Actions** | Trendyol pipeline'ı temel alınacak |
| EF Core | 9.0.6 | **9.0.6** (Trendyol 8.0 → yükseltilecek) | Stok versiyonu standart |
| DB | Çoklu | **PostgreSQL 16** (tek standart) | Komutan kararı |

### KARAR 6: Unified StockMovementType

```csharp
// Domain/Enums/StockMovementType.cs — İki repodan birleştirilmiş

public enum StockMovementType
{
    // === GİRİŞ HAREKETLERİ ===
    StockIn = 1,              // Genel giriş
    Purchase = 10,            // Satın alma
    BarcodeReceive = 11,      // Barkod ile kabul
    Production = 12,          // Üretimden giriş
    CustomerReturn = 13,      // Müşteriden iade (Stok)
    Found = 14,               // Sayımda bulunan
    PlatformReturn = 15,      // YENİ — Platform iadesi (Trendyol/OpenCart)
    
    // === ÇIKIŞ HAREKETLERİ ===
    StockOut = 2,             // Genel çıkış
    Sale = 20,                // Satış
    BarcodeSale = 21,         // Barkod ile satış
    Consumption = 22,         // Sarf/Tüketim
    Loss = 23,                // Fire/Kayıp
    PlatformSale = 24,        // YENİ — Platform satışı (Trendyol sipariş)
    
    // === DÜZELTME & TRANSFER ===
    Adjustment = 3,           // Sayım düzeltme
    Transfer = 30,            // Depolar arası transfer
    
    // === ENTEGRASYON SYNC ===
    PlatformSync = 6,         // YENİ — Genel platform senkronizasyonu
    OpenCartSync = 60,        // OpenCart sync
    TrendyolSync = 61,        // Trendyol sync
    MarketplaceSync = 62,     // Diğer pazaryeri sync
}
```

---

## 3. GÜNCELLENMİŞ FAZ 0 — ETKİLENEN GÖREVLER

Trendyol keşfi sonrası Faz 0'da şu değişiklikler yapılır:

### Görev 0.2 GÜNCELLEMESİ: Domain Projesi

**Eklenenler:**
- `BaseEntity` → Guid PK + Full Audit + Soft Delete + Domain Events
- `ProductPlatformMapping` entity (yeni)
- `ProductImage` entity (yeni — görseller ayrı tablo)
- `ProductBarcode` entity (yeni — çoklu barkod desteği)
- `ICurrentUserService` interface (yeni — multi-user altyapısı)
- `SyncLog` entity (yeni)
- `SyncDirection` enum: Push, Pull, Bidirectional
- `SyncStatus` enum: NotSynced, Syncing, Synced, Failed, PendingSync
- `PlatformSync` hareket tipi StockMovementType enum'a eklendi
- `WeightUnit` enum: Kilogram, Gram

**Kaldırılanlar:**
- Product entity'den `OpenCartProductId` → ProductPlatformMapping'e taşındı
- Product entity'den `OpenCartCategoryId` → ProductPlatformMapping'e taşındı

### Görev 0.3 GÜNCELLEMESİ: Application Projesi

**Eklenenler:**
- `IIntegratorAdapter` interface'inde webhook desteği:
```csharp
// Trendyol keşfinden gelen webhook ihtiyacı
Task<WebhookResult> ProcessWebhookAsync(string eventType, string payload);
Task<bool> ValidateWebhookSignatureAsync(string signature, string payload);
```
- `SyncPlatformCommand`'a batch desteği (Trendyol 100'lü batch'lerden öğrenildi)
- `IBackgroundJobService` interface (Hangfire hazırlığı)
- `ICacheService` interface (Redis hazırlığı)

### Görev 0.4 GÜNCELLEMESİ: Infrastructure Projesi

**Eklenenler:**
- `Integration/Adapters/TrendyolAdapter.cs` iskelet (keşfiden gelen 10 endpoint bilgisiyle)
- `Integration/Adapters/OpenCartAdapter.cs` iskelet
- `Messaging/RabbitMqEventPublisher.cs` (MassTransit altyapısı)
- `Caching/RedisCacheService.cs`
- `BackgroundJobs/HangfireJobService.cs`
- `Persistence/AuditInterceptor.cs` (multi-user audit)
- `Services/DevelopmentUserService.cs` (geçici — login gelene kadar)

**Docker eklentisi (Yeni Görev 0.8):**
```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16-alpine
    ports: ["5432:5432"]
  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports: ["5672:5672", "15672:15672"]
```

### YENİ GÖREV 0.8: Docker Compose Altyapısı

Trendyol'da Docker zaten çalışıyor. Stok tarafına da aynı standartta eklenir:

| Servis | Image | Port | Amaç |
|--------|-------|------|------|
| PostgreSQL 16 | postgres:16-alpine | 5432 | Merkezi veritabanı |
| Redis 7 | redis:7-alpine | 6379 | Cache + distributed lock |
| RabbitMQ 3 | rabbitmq:3-management | 5672 / 15672 | Event bus + yönetim UI |

---

## 4. GÖNCELLENMİŞ GÖREV SIRASI VE BAĞIMLILIKLAR

```
GÖREV 0.1 (PostgreSQL) ← Değişiklik yok
    │
    ▼
GÖREV 0.8 (Docker Compose — YENİ) ← PostgreSQL + Redis + RabbitMQ
    │
    ▼
GÖREV 0.2 (Domain) ← GÜNCELLENDİ: Guid PK, BaseEntity, 
    │                   ProductPlatformMapping, ICurrentUserService
    ▼
GÖREV 0.3 (Application) ← GÜNCELLENDİ: Webhook, Batch, Cache, BackgroundJob interface
    │
    ▼
GÖREV 0.4 (Infrastructure) ← GÜNCELLENDİ: Redis, RabbitMQ, Hangfire, 
    │                          TrendyolAdapter iskelet, AuditInterceptor
    ▼
GÖREV 0.5 (Desktop Bağlama) ← Değişiklik yok
    │
    ▼
GÖREV 0.6 (Secret Management) ← Paralel çalışabilir
    │
    ▼
GÖREV 0.7 (Domain Events + RabbitMQ publish) ← GÜNCELLENDİ: MassTransit ile
```

---

## 5. GÜNCELLENMİŞ BAŞARI KRİTERLERİ

Orijinal 12 kritere eklenenler:

| # | Kriter | Doğrulama |
|---|--------|-----------|
| K13 | `ProductPlatformMapping` entity Domain'de tanımlı | Dosya mevcut |
| K14 | Product entity'de platform-özel ID yok (OpenCartProductId kaldırılmış) | `grep OpenCartProductId Domain/` → boş |
| K15 | `ICurrentUserService` interface Domain'de, `DevelopmentUserService` Infrastructure'da | Dosya konumu |
| K16 | `BaseEntity` Guid PK + Full Audit + Soft Delete + Domain Events içeriyor | Kod incelemesi |
| K17 | Docker Compose ile PostgreSQL + Redis + RabbitMQ ayağa kalkıyor | `docker compose up -d` + bağlantı testi |
| K18 | `SyncStatus` ve `SyncDirection` enumları Domain'de tanımlı | Dosya mevcut |
| K19 | `AuditInterceptor` her SaveChanges'ta CreatedBy/UpdatedBy otomatik dolduruyor | Birim testi |

---

## 6. TOPLAM DURUM TABLOSU

```
İKİ REPO KEŞFİ          ✅ TAMAMLANDI (1600+ satır rapor)
ÇAPRAZ ANALİZ            ✅ TAMAMLANDI (bu doküman)
MİMARİ KARARLAR          ✅ KESİNLEŞTİ (6 karar)
FAZ 0 EMİRNAMESİ         ✅ GÜNCELLENDİ (8 görev)
                         │
                         ▼
SONRAKİ ADIM:            FAZ 0 UYGULAMASI
                         Görev 0.1 → 0.8 → 0.2 → ... sırasıyla
```

---

## 7. KOMUTAN İÇİN ÖZET — TEK SAYFA

**Ne bulduk?**
- Stok: %78 olgun masaüstü uygulaması, güçlü stok yönetimi, zayıf entegrasyon
- Trendyol: Tam web API platformu, güçlü entegrasyon, zayıf stok yönetimi
- İkisi arasında sıfır bağlantı — tamamen izole

**Ne karar verdik?**
1. ✅ ProductPlatformMapping tablosu — entity'ye platform ID'si eklenmeyecek
2. ✅ Guid PK — dağıtık sisteme uygun
3. ✅ ICurrentUserService — login yok ama altyapı hazır, tak-çıkar
4. ✅ Shared Domain NuGet + RabbitMQ event bus — iki repo birleşmeyecek ama konuşacak
5. ✅ PostgreSQL standart DB
6. ✅ Docker Compose (PostgreSQL + Redis + RabbitMQ)

**Her reponun üstünlüğü nerede?**
- Stok lider: Depo yönetimi, lot/FEFO, WAC maliyet, barkod, RBAC
- Trendyol lider: Platform API, Hangfire, Redis, RabbitMQ, Docker, CI/CD, webhook

**Ne yapacağız?**
Her reponun en iyisini alıp ortak Domain çekirdeğinde buluşturacağız. Faz 0 bu temeli atıyor.

---

**ÇAPRAZ ANALİZ SONU**  
**Komutan MesTech — "İki gözle bak, bir karar ver."**
