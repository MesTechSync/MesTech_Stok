# MesTechStok – Canlıya Alma Kılavuzu (Production Readiness)

Rapor Tarihi: 14 Ağustos 2025

---

## 0) Amaç ve Kapsam

Bu kılavuz, MesTechStok .NET 9 WPF uygulamasını üretim ortamına güvenle almak için gerekli tüm adımları bir arada sunar. Her başlık altında:
- Eksiklerin listesi (To‑Do kontrol listesi)
- Net SQL gereklilikleri (idempotent T‑SQL)
- Uygulama ve yayınlama adımları (EF Migrations, DI, paketleme)
- Test ve doğrulama kriterleri

Kaynak: `Rapor_1408` klasörü altındaki mimari, modül, akış, API, veri, log ve tasarım raporları.

---

## -1) Legacy Uyarı (Değişiklikten Önce G‑D‑G‑E Kapısı)

Her değişiklikten önce aşağıdaki kapıyı açın; herhangi biri “hayır” ise önce doğrulayın:
- Gerçek mi? (Log/DB ile mevcut davranış doğrulandı mı?)
- Doğru mu? (Doğru dosya/ortam mı? `appsettings.user.json` öncelikli mi?)
- Gerekli mi? (Bugün çözdüğümüz problem için şart mı?)
- Etkisi ve geri dönüşü net mi? (Yedek/rollback hazır mı?)

Not: Ayrıntı notu için `Docs/OPERASYON_NOTLARI/LEGACY_UYARI_VE_DOGRULAMA.md`.

---

## 1) Veritabanı Gereklilikleri (SQL)

### 1.1. Oturum ve İzolasyon Ayarları (Öneri)
```sql
-- Öneri: Snapshot isolation (okuma sırasında kilitlenmeyi azaltır)
IF DB_ID(N'MesTechStok') IS NOT NULL
BEGIN
    ALTER DATABASE MesTechStok SET ALLOW_SNAPSHOT_ISOLATION ON;
    ALTER DATABASE MesTechStok SET READ_COMMITTED_SNAPSHOT ON;
END
```

### 1.2. Tablo ve Şema Gereksinimleri (Idempotent)
Not: Aşağıdaki komutlar mevcut tabloları bozmadan eksikleri tamamlar.

#### 1.2.1. Products
```sql
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        Name            NVARCHAR(100)    NOT NULL,
        Description     NVARCHAR(500)    NULL,
        SKU             NVARCHAR(50)     NOT NULL,
        Barcode         NVARCHAR(50)     NOT NULL,
        GTIN            NVARCHAR(14)     NULL,
        UPC             NVARCHAR(20)     NULL,
        EAN             NVARCHAR(20)     NULL,
        PurchasePrice   DECIMAL(18,2)    NOT NULL CONSTRAINT DF_Products_PurchasePrice DEFAULT (0),
        SalePrice       DECIMAL(18,2)    NOT NULL CONSTRAINT DF_Products_SalePrice DEFAULT (0),
        ListPrice       DECIMAL(18,2)    NULL,
        Stock           INT              NOT NULL CONSTRAINT DF_Products_Stock DEFAULT (0),
        MinimumStock    INT              NOT NULL CONSTRAINT DF_Products_MinimumStock DEFAULT (0),
        MaximumStock    INT              NULL,
        Category        NVARCHAR(100)    NOT NULL CONSTRAINT DF_Products_Category DEFAULT (N''),
        SubCategory     NVARCHAR(100)    NULL,
        Brand           NVARCHAR(100)    NULL,
        ImageUrl        NVARCHAR(500)    NULL,
        ImageUrls       NVARCHAR(MAX)    NULL, -- ";" ile çoklu görsel desteği
        ExternalReference NVARCHAR(200)  NULL,
        OpenCartId      INT              NULL,
        IsActive        BIT              NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedDate     DATETIME2        NOT NULL CONSTRAINT DF_Products_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedDate     DATETIME2        NULL,
        RowVersion      ROWVERSION       NOT NULL
    );
END

-- Benzersizlik ve performans indeksleri
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Products_Barcode' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_SKU' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_SKU ON dbo.Products(SKU);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_Category' AND object_id = OBJECT_ID('dbo.Products'))
    CREATE INDEX IX_Products_Category ON dbo.Products(Category);
```

#### 1.2.2. Customers
```sql
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        Name            NVARCHAR(150)   NOT NULL,
        Code            NVARCHAR(50)    NULL,
        Email           NVARCHAR(150)   NULL,
        Phone           NVARCHAR(50)    NULL,
        Type            NVARCHAR(20)    NULL,  -- INDIVIDUAL | CORPORATE | VIP
        IsVip           BIT             NOT NULL CONSTRAINT DF_Customers_IsVip DEFAULT (0),
        BillingAddress  NVARCHAR(500)   NULL,
        ShippingAddress NVARCHAR(500)   NULL,
        City            NVARCHAR(100)   NULL,
        State           NVARCHAR(100)   NULL,
        PostalCode      NVARCHAR(20)    NULL,
        Country         NVARCHAR(100)   NULL,
        CreditLimit     DECIMAL(18,2)   NULL,
        DiscountRate    DECIMAL(5,2)    NULL,  -- %
        PaymentTermDays INT             NULL,
        Currency        NVARCHAR(10)    NULL,
        DocumentUrls    NVARCHAR(MAX)   NULL,  -- JSON veya ";" listesi
        CreatedDate     DATETIME2       NOT NULL CONSTRAINT DF_Customers_Created DEFAULT (SYSUTCDATETIME()),
        UpdatedDate     DATETIME2       NULL,
        RowVersion      ROWVERSION      NOT NULL
    );
END

-- Hızlı arama indeksleri
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Name' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE INDEX IX_Customers_Name ON dbo.Customers(Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_Code' AND object_id = OBJECT_ID('dbo.Customers'))
    CREATE INDEX IX_Customers_Code ON dbo.Customers(Code);
```

#### 1.2.3. StockMovements
```sql
IF OBJECT_ID(N'dbo.StockMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovements
    (
        Id             BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovements PRIMARY KEY,
        ProductId      INT           NOT NULL,
        Quantity       INT           NOT NULL,  -- giriş +, çıkış -
        PreviousStock  INT           NOT NULL,
        NewStock       INT           NOT NULL,
        MovementType   NVARCHAR(20)  NOT NULL, -- IN | OUT | ADJUSTMENT
        UnitPrice      DECIMAL(18,2) NULL,
        Notes          NVARCHAR(500) NULL,
        Reference      NVARCHAR(100) NULL,
        MovementDate   DATETIME2     NOT NULL CONSTRAINT DF_StockMovements_Date DEFAULT (SYSUTCDATETIME()),
        CreatedBy      NVARCHAR(100) NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_ProductId_Date' AND object_id = OBJECT_ID('dbo.StockMovements'))
    CREATE INDEX IX_StockMovements_ProductId_Date ON dbo.StockMovements(ProductId, MovementDate DESC);
```

#### 1.2.4. Telemetri ve Log Tabloları (API Dayanıklılık)
```sql
IF OBJECT_ID(N'dbo.ApiCallLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiCallLogs
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApiCallLogs PRIMARY KEY,
        Endpoint      NVARCHAR(200)  NOT NULL,
        Method        NVARCHAR(10)   NOT NULL,
        Success       BIT            NOT NULL,
        StatusCode    INT            NULL,
        Category      NVARCHAR(50)   NULL,
        DurationMs    INT            NULL,
        CorrelationId NVARCHAR(64)   NULL,
        TimestampUtc  DATETIME2      NOT NULL CONSTRAINT DF_ApiCallLogs_Ts DEFAULT (SYSUTCDATETIME())
    );
END

IF OBJECT_ID(N'dbo.CircuitStateLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CircuitStateLogs
    (
        Id                    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CircuitStateLogs PRIMARY KEY,
        PreviousState         NVARCHAR(20)  NOT NULL,
        NewState              NVARCHAR(20)  NOT NULL,
        FailureRate           DECIMAL(9,4)  NULL,
        WindowTotalCalls      INT           NULL,
        CorrelationId         NVARCHAR(64)  NULL,
        TransitionTimeUtc     DATETIME2     NOT NULL CONSTRAINT DF_CircuitStateLogs_Ts DEFAULT (SYSUTCDATETIME())
    );
END
```

### 1.3. Veri Bütünlüğü Kuralları
```sql
-- Satış fiyatı < alış fiyatı ise uyarı amaçlı CHECK (örnek)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Products_PriceLogic')
    ALTER TABLE dbo.Products WITH NOCHECK
    ADD CONSTRAINT CK_Products_PriceLogic CHECK (SalePrice >= 0 AND PurchasePrice >= 0);
```

### 1.4. İndeks ve Performans Notları
- Sık aranan alanlar için indeksler eklendi: `Barcode`, `SKU`, `Category`, `Customers.Name`, `Customers.Code`.
- Stok hareketlerinde `ProductId, MovementDate DESC` bileşik indeks eklendi.

---

## 2) EF Core Migrations ve Bağlantı (Critical)

### 2.1. Bağlantı Dizeleri (appsettings.json – örnek)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=MesTechStok;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### 2.2. Migration Adımları (PowerShell)
```powershell
# EF CLI yoksa yükleyin
dotnet tool install --global dotnet-ef

# Çekirdek proje klasöründe çalıştırın
cd MesTechStok\src\MesTechStok.Core

# İlk baseline veya eksikse ekleyin
dotnet ef migrations add InitialCreate
dotnet ef database update

# Sonraki şema değişiklikleri için
dotnet ef migrations add AddTelemetryAndCustomers
dotnet ef database update
```

### 2.3. Doğrulama
- Uygulama açılışında bağlantı kurulmalı; “Invalid column name” hataları sıfırlanmalı.
- Migration tablosu `__EFMigrationsHistory` dolmalı.

---

## 3) Servis Katmanı Eksikleri ve Tamamlama (High)

### 3.1. IProductService – Implementasyon Gereksinimleri
- GetAll/GetById/GetByBarcode
- Paged listeleme (server‑side)
- Bulk update (Excel import)
- Stok güncelleme (hareket yazımı)
- Barkod benzersizlik kontrolü

### 3.2. ICustomerService – Implementasyon Gereksinimleri
- Create/Update (null/boş alanlar güvenle kabul)
- Adres alanları (şehir, il/ilçe, posta, ülke)
- DocumentUrls JSON kaydı (ekler)
- Paged arama (isim/kod/telefon)

### 3.3. Desktop → Core DI Bağlantısı
- `CustomersView` ve `ProductsView` gerçek `Core` servisleri kullanmalı.
- Test verisi/toparlama kodları kaldırılmalı.

Kontrol Listesi:
- [ ] IProductService tüm zorunlu metodlar yazıldı
- [ ] ICustomerService CRUD ve arama yazıldı
- [ ] DI kayıtları (App.xaml.cs/Program) tamam
- [ ] Test data kaldırıldı; gerçek DB akışı çalışıyor

---

## 4) UI/UX ve Kullanılabilirlik (Medium)

### 4.1. Ürün Listesi ve Görsel Standardizasyonu
- Görseller 160x160 çerçevede `UniformToFill` + yüksek kalite ölçekleme.
- Arka plan işleyici 1200x1800 tuval üzerine normalize eder (düzen).
- Görsel önizleme: Tek tık büyüt/geri; ESC ile kapanış.

### 4.2. Ürün Yükleme Popup (Okunabilirlik + Görseller)
- Metin kontrastı ve font boyutları artırıldı (14px+).
- “Henüz görsel yok” uyarısı var.
- Düzenle modunda mevcut görsel/video listesi otomatik yüklenir.

### 4.3. Excel İçe Aktar (Sihirbaz)
- Üst barda buton görünür: “Excel’den İçe Aktar”.
- Sütun eşleştirme profili; `;` ayracı ile 1..8 görsel desteği.

### 4.4. Kolon Ayarları ve Kalıcılık
- Kolon görünürlüğü/konumu kullanıcı ayarlarında kalıcı.
- “Kolon Ayarla” butonu ürünlerin yanında görünür konumda.

Kontrol Listesi:
- [ ] Görsel kutuları düzgün ve tutarlı render ediliyor
- [ ] ESC ile görsel viewer kapanıyor
- [ ] Excel içe aktar sihirbazı çalışıyor (çoklu görsel)
- [ ] Kolon ayarları kalıcı

---

## 5) Barkod Okuyucu Entegrasyonu (High)

### 5.1. Donanım Ayarı
- `System.IO.Ports` ile COM port, baud vb. konfigürasyon (`appsettings.json`).

### 5.2. Akış
- DataReceived → barkod → `IProductService.GetProductByBarcodeAsync` → UI güncelle.

### 5.3. Testler
- [ ] Barkod okunduğunda doğru ürün açılıyor
- [ ] Hatalı barkodda uyarı loglanıyor

---

## 6) Serilog ve Telemetri (High)

### 6.1. Dosya Logları
- Günlük döndürme, 30 gün retansiyon, yapılandırılmış format.

### 6.2. DB Telemetri
- API çağrıları ve devre durumu `ApiCallLogs`, `CircuitStateLogs`’a yazılır.

### 6.3. Konfigürasyon
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.Debug"],
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [
      { "Name": "File", "Args": { "path": "Logs/mestech-.log", "rollingInterval": "Day", "retainedFileCountLimit": 30 } },
      { "Name": "Debug" }
    ]
  }
}
```

Doğrulama:
- [ ] Uygulama başlangıç/kapanış logları dosyada
- [ ] API/CB logları tablolarına yazılıyor

---

## 7) Ayarlar ve Ekran Koruyucu Senkronu (Medium)
- Ayarlarda şirket adı değişince ekran koruyucuya anında yansıma (event/observer).
- Ayarlar paneli stok takibiyle tutarlı kontrol merkezi gibi davranır.

Kontrol Listesi:
- [ ] Ayar değişikliği anında ekran koruyucuda görüldü
- [ ] Uygulamayı yeniden başlatmadan güncelleniyor

---

## 8) Yayınlama (Build/Publish) ve Paketleme

### 8.1. Build Komutu (Self‑contained)
```powershell
dotnet publish MesTechStok\src\MesTechStok.Desktop\MesTechStok.Desktop.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

### 8.2. Dağıtım
- Çıktı: `bin/Release/net9.0-windows/win-x64/publish/`
- Gerekli `appsettings*.json` dosyaları dahil.

### 8.3. Rollback Planı
- Yayından önce veri tabanı yedeği.
- Yeni sürümü paralel klasöre dağıt, smoke test sonrası ana kısayolu yönlendir.

---

## 9) Smoke Test Planı (10 Dakika)
1. Giriş ve ana pencere yüklenmesi (loglar kontrol)
2. Ürün listeleme (sayfalama, arama)
3. Ürün düzenle popup → mevcut görseller geliyor mu?
4. Görsel ekle/sil/kapak yap → UI ve disk kontrolü
5. Excel içe aktarma → ilk 10 satır önizleme, eşleme profili
6. Barkod okutma → ürün açılıyor mu?
7. Müşteri ekle/düzenle popup → adres defteri + ek dosya kaydı
8. Ayar değiştir → ekran koruyucu anında güncelleniyor mu?

Başarılıysa: “Go/No‑Go” toplantısında “Go”.

---

## 10) Canlı Öncesi Check‑List (Tamamlanması Zorunlu)
- [ ] SQL şema eksikleri tamamlandı (bu dosyadaki scriptler uygulandı)
- [ ] EF migrations başarıyla çalıştı, `__EFMigrationsHistory` dolu
- [ ] IProductService ve ICustomerService gerçek implementasyonlarla çalışıyor
- [ ] Test data tamamen kaldırıldı
- [ ] Serilog dosya logları ve DB telemetri aktif
- [ ] Ekran koruyucu anlık ayar güncellemesini alıyor
- [ ] Excel içe aktarma ve çoklu görsel desteği test edildi
- [ ] Kolon ayarları kalıcı ve görünür buton yerleşimi doğru
- [ ] Barkod entegrasyonu gerçek cihazla test edildi
- [ ] Yayın paketi üretildi ve smoke test geçti

---

## 11) Post‑Go‑Live İzleme
- Log dosyaları: `Logs/mestech-*.log` → hata/uyarı taraması
- `ApiCallLogs`/`CircuitStateLogs` son 24 saat sorguları (Rapor 6’daki SQL)
- Kullanıcı geri bildirimleri: Okunabilirlik, performans, barkod başarısı

---

## 12) Ek – Sık Karşılaşılan Hatalar ve Çözümleri
**Invalid column name**: Migrations/SQL eksik → Bölüm 1 ve 2’yi uygulayın.

**Boş alanlarda kayıt hatası**: Customer/Product kolonlarını NULL kabul eder hale getirin (scriptler) ve servis katmanında `null` gönderin; UI doğrulaması zorunlu alanları işaretlesin.

**Görseller görünmüyor**: Düzenle popup’ında DB’den son halini tekrar yükleme (aktif). Diskte `%LOCALAPPDATA%/MesTechStok/Images/Products/` varlığı kontrol edin.

**Barkod çalışmıyor**: COM port/baud yanlış olabilir; `appsettings.json` kontrol edin ve cihazı test edin.

---

## 13) Referans
- `01_Genel_Sistem_Mimarisi.md` – Katmanlar ve MVVM
- `02_Modul_Haritasi_ve_Bilesen_Tanimlari.md` – Modül durumu
- `03_Algoritma_Akisi_ve_Veri_Isleme.md` – Async akış
- `04_API_Entegrasyon_Mimarisi.md` – Dayanıklılık & HTTP
- `05_Doldurma_Kilavuzu_ve_Veri_Formatlari.md` – JSON/Model örnekleri
- `06_Log_Toplama_ve_Hata_Inceleme.md` – Loglama ve SQL inceleme
- `07_Tasarim_Standartlari_ve_Gorsel_Uyum.md` – UI/UX standartları
- `08_Eksik_Tespit_ve_Is_Plani.md` – Öncelikli iş kalemleri


---

## 14) Canlıya Alma Çizelgesi ve Durum (Güncel)

| Madde | Durum | Kanıt/Not |
| :-- | :-- | :-- |
| SQL şema ve indeksler (idempotent) | Tamamlandı | Açılışta `Ensure*` yordamları çalıştı; loglar temiz |
| EF Core migrations hizalaması | Geçici (EnsureCreated) | Mevcut DB’ye migration sonrası hizalama bir sonraki sprintte yapılacak |
| Core servis implementasyonları | Tamamlandı | `IProductService`/`ICustomerService` DI ile aktif |
| Loglama (Serilog) | Tamamlandı | `Logs/mestech-*.log` oluşuyor |
| Telemetri tabloları | Tamamlandı | `ApiCallLogs`, `CircuitStateLogs` oluşturuldu |
| Barkod entegrasyonu | Tamamlandı | Loglarda kamera açılış/kapanış, tarama döngüsü |
| Excel içe aktarma sihirbazı | Çalışıyor (Kontrol edilecek) | UI butonu görünür, profil/çoklu görsel desteği var |
| Ürün görsel standardizasyonu | Tamamlandı | 160x160 `UniformToFill` + yüksek kalite |
| Ekran koruyucu ayar yansıması | Kontrol edilecek | Ayar değişikliği anlık yansıtma testi yapılacak |
| Self‑contained publish | Tamamlandı | `publish/` klasörü üretildi ve EXE çalışıyor |
| Smoke test (8 adım) | Başlatılacak | Aşağıdaki planla tek tek işaretlenecek |

### 14.1. Smoke Test Çizelgesi (İşaretleme)

- [ ] Ana pencere/başlangıç logları OK
- [ ] Ürün listeleme: sayfalama/arama OK
- [ ] Ürün düzenle popup: mevcut görseller geliyor
- [ ] Görsel ekle/sil/kapak yap: UI + disk OK
- [ ] Excel içe aktarma: önizleme + eşleme + çoklu görsel OK
- [ ] Barkod okutma: ürün açılıyor
- [ ] Müşteri popup: adres defteri + ek dosya kaydı OK
- [ ] Ayar değişikliği: ekran koruyucuya anında yansıma


