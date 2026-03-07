# TAKIM 3 RAPORU: FRONTEND & DESKTOP ANALiZi
Kontrolor: Claude Opus 4.6
Tarih: 06 Mart 2026
Emirname Ref: ENT-MD-001-T3

---

## A. Dashboard Yapilandirmasi

### A.1. modules.json Icerigi

**2 farkli modules.json dosyasi tespit edildi:**

**1. Kok config (`MesTech_Dashboard/config/modules.json`):**
```json
{
  "Modules": {
    "Trendyol": {
      "DisplayName": "Trendyol Yonetimi",
      "ExecutablePath": "",
      "IconName": "ShoppingCart",
      "IsEnabled": true,
      "Description": "Trendyol pazaryeri entegrasyonu."
    },
    "Stok": {
      "DisplayName": "Stok Yonetimi",
      "ExecutablePath": "",
      "IconName": "PackageVariantClosed",
      "IsEnabled": true,
      "Description": "Merkezi stok ve envanter yonetimi."
    }
  }
}
```
> **Bulgu:** Sadece 2 modul tanimli. ExecutablePath bos -- henuz baglanti yapilmamis.

**2. WPF icerisindeki config (`src/MesTech.Dashboard.WPF/config/modules.json`):**
```json
{
  "modules": [
    { "id": "dashboard", "name": "Ana Dashboard", "enabled": true, "type": "internal" },
    { "id": "trendyol", "name": "Trendyol Entegrasyonu", "enabled": true, "type": "external",
      "path": "..\\..\\..\\MesTech_Trendyol", "executable": "start.bat" },
    { "id": "n11", "name": "N11 Entegrasyonu", "enabled": false, "type": "external",
      "path": "..\\..\\..\\MesTech_N11" },
    { "id": "hepsiburada", "name": "Hepsiburada Entegrasyonu", "enabled": false, "type": "external",
      "path": "..\\..\\..\\MesTech_Hepsiburada" }
  ]
}
```
> **Bulgu:** 4 modul tanimli. N11 ve Hepsiburada devre disi (`enabled: false`). Moduller external .bat dosyasi ile baslatiliyor.

**UYUMSUZLUK:** Iki modules.json dosyasi farkli format kullaniyor (biri obje, digeri dizi). ModuleService.cs sadece kok config'deki "Modules" section'ini okuyor.

### A.2. ModuleService -- Gercek Modul Listesi

`ModuleService.cs` dosyasinda (`LoadDefaultModules` metodu) config'den okunamadigi durumda **12 varsayilan modul** tanimli:

| # | Modul Adi | DisplayName | Icon | ExecutablePath | Durum |
|---|-----------|-------------|------|----------------|-------|
| 1 | Trendyol | Trendyol Entegrasyonu | ShoppingCart | ../MesTech_Trendyol/trendyol_dashboard.html | Aktif (HTML) |
| 2 | Stock | Stok Yonetimi | Package | ../MesTech_Stok/.../MesTechStok.exe | Aktif (EXE) |
| 3 | Orders | Siparis Yonetimi | ShoppingBasket | (bos) | Gelistirilmemis |
| 4 | Analytics | Analitik Raporlar | ChartLine | (bos) | Gelistirilmemis |
| 5 | Customers | Musteri Yonetimi | AccountMultiple | (bos) | Gelistirilmemis |
| 6 | Finance | Finans Yonetimi | CashMultiple | (bos) | Gelistirilmemis |
| 7 | Settings | Sistem Ayarlari | Settings | (bos) | Gelistirilmemis |
| 8 | Reports | Rapor Merkezi | FileDocument | (bos) | Gelistirilmemis |
| 9 | Backup | Yedekleme Sistemi | Backup | (bos) | Gelistirilmemis |
| 10 | API | API Yonetimi | Api | (bos) | Gelistirilmemis |
| 11 | Users | Kullanici Yonetimi | Account | (bos) | Gelistirilmemis |
| 12 | Logs | Log Goruntuyeci | FileDocumentOutline | (bos) | Gelistirilmemis |

> **Bulgu:** 12 modulden sadece 2'si (Trendyol + Stock) calisan path'e sahip. Diger 10 modul tiklandiginda "Henuz gelistirilmemistir" mesaji gosteriyor.

### A.3. SignalR Baglanti Yapisi

**BULGU: SignalR Client paketi kurulu ancak KULLANILMIYOR.**

- `MesTech.Dashboard.WPF.csproj` satir 39: `Microsoft.AspNetCore.SignalR.Client Version="9.0.0"` referansi mevcut.
- Ancak tum .cs dosyalarinda `SignalR`, `HubConnection`, `Hub` anahtar kelimelerinin **hicbiri bulunamadi**.
- SignalR altyapisi hazirlanmis ama entegrasyon henuz kodlanmamis.

### A.4. Login/Auth Ekran Akisi

**Giris Akisi:**
```
Program.cs (Main) --> App() --> WorkingLoginWindow.xaml
    |
    +--> Kullanici adi: "admin" (sabit)
    +--> Sifre: "admin123" (hardcoded)
    |
    +--> AuthenticationService.LoginAsync()
    |       DefaultUsername = "admin"
    |       DefaultPassword = "admin123" (string karsilastirma)
    |
    +--> Basarili --> MainDashboard.xaml (tam ekran, WindowStyle=None)
    +--> Basarisiz --> "Gecersiz sifre" hata mesaji
```

**KRITIK GUVENLIK BULGULARI:**
1. Sifre hardcoded: `AuthenticationService.cs:17` -- `private const string DefaultPassword = "admin123";`
2. WorkingLoginWindow.xaml'da sifre acik metin: `Content="Kullanici: admin | Sifre: admin123"` (satir 74)
3. DbContext'te User tablosu ve PasswordHash alani var ama AuthenticationService veritabani KULLANMIYOR -- sabit string karsilastirmasi yapiyor.
4. SHA256 hash metodu mevcut ama ChangePassword akisinda bile veritabanina kaydetmiyor.

### A.5. Ana Ekranlar ve Navigasyon Yapisi

**Dashboard Layout (MainDashboard.xaml):**
```
+------------------------------------------------------------+
|  [DUYURULAR BANNER] - Kayan metin bildirimleri              |
+------------------------------------------------------------+
|                                    |                        |
|  MesTech Hizmetleri                |  Medya Galerisi        |
|  (WrapPanel - 12 ikon buton)       |  (Resim slideshow)     |
|                                    |  [<] [>] [+]           |
|  [Trendyol] [Stok] [Siparis]      |                        |
|  [Analitik] [Musteri] [Finans]    |                        |
|  [Ayarlar] [Rapor] [Yedekleme]    |                        |
|  [API]     [Kullanici] [Log]      |                        |
|                                    |                        |
+------------------------------------------------------------+
|  [OK] Sistem Durumu: Aktif    HH:mm    [Cikis]             |
+------------------------------------------------------------+
```

- **Navigasyon tipi:** WrapPanel icerisinde ikon butonlar (sidebar degil, grid tarzinda)
- **Modul baslama:** `ModuleService.LaunchModuleAsync()` ile dis process baslatiliyor (`Process.Start`)
- **Pencere stili:** Tam ekran (`WindowState="Maximized"`, `WindowStyle="None"`)

### A.6. Tema Sistemi

**MaterialDesignThemes 5.1.0 + Custom renk paleti:**

| Renk | Hex | Kullanim |
|------|-----|----------|
| Primary | #2C3E50 | Ana arka plan, basliklar |
| Secondary | #3498DB | Vurgular, ikonlar, hover |
| Accent | #E74C3C | Hata mesajlari, cikis butonu |
| Success | #27AE60 | Basarili durum, ekleme butonu |
| Warning | #F39C12 | Uyari mesajlari |
| Background | #34495E | Panel arka planlari |
| Surface | #ECF0F1 | Yuzey rengi |

- **Tema tabanı:** `BundledTheme BaseTheme="Dark" PrimaryColor="Blue" SecondaryColor="LightBlue"`
- **Gradient:** `BackgroundGradientBrush` ile degrade arka plan
- App.xaml'da global Color + SolidColorBrush + LinearGradientBrush tanimlari

### A.7. SQLite Veritabani Yapisi

**DashboardDbContext -- 3 tablo:**

| Tablo | Entity | Onemli Alanlar | Indexler |
|-------|--------|----------------|----------|
| Users | User | Id, Username (unique), Email (unique), PasswordHash, Role, IsActive, CreatedAt, LastLoginAt | Username, Email |
| SystemSettings | SystemSettings | Id, Key (unique), Value, Category, IsReadOnly | Key |
| AuditLogs | AuditLog | Id, UserId (FK), Username, Action, Entity, EntityId, Timestamp, IpAddress | Timestamp, (Entity+EntityId), Username |

**Seed Data:**
- 1 admin kullanici: admin / admin123
- 5 sistem ayari: ApplicationName, ApplicationVersion, SessionTimeoutMinutes, LogLevel, MaxLoginAttempts

**Repository Pattern:** BaseRepository, UserRepository, SystemSettingsRepository, AuditLogRepository + UnitOfWork

---

## B. Stok Desktop View Haritasi

### B.1. Proje Teknoloji Ozeti

- **.NET 9.0 WPF**, self-contained win-x64
- **UseWindowsForms: true** (barkod kamera icin)
- **Katmanlar:** MesTechStok.Desktop, MesTechStok.Core, MesTech.Domain, MesTech.Application, MesTech.Infrastructure
- **MVVM:** CommunityToolkit.Mvvm 8.2.2
- **Ikonlar:** MahApps.Metro.IconPacks 6.0.0
- **Barkod:** ZXing.Net + OpenCvSharp4 + AForge.Video.DirectShow
- **Export:** ClosedXML (Excel), iTextSharp (PDF)
- **Veritabani:** SQLite + SQL Server + PostgreSQL destegi
- **HTTP:** Polly (retry/resilience) ile OpenCart entegrasyonu

### B.2. View Listesi (33 Aktif View + 8 Component + 5 Style/Theme)

#### URUN YONETIMI EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 1 | ProductsView.xaml | Ana urun listeleme/filtreleme | MainViewModel | Sol menu > Urunler |
| 2 | NewProductsView.xaml | Yeni urun ekleme formu | MainViewModel | Urunler > Yeni Urun |
| 3 | ProductEditDialog.xaml | Urun duzenleme popup | (code-behind) | Urunler > Duzenle |
| 4 | ProductImageViewer.xaml | Urun resmi goruntuleme | (code-behind) | Urunler > Resim |
| 5 | ProductImportWizard.xaml | Toplu urun iceri aktarma | (code-behind) | Urunler > Ice Aktar |
| 6 | ProductUploadPopup.xaml | Urun yukleme popup | (code-behind) | Urunler > Yukle |
| 7 | ProductUploadPopup_Enhanced.xaml | Gelistirilmis yukleme | (code-behind) | Alternatif yukleme |
| 8 | ProductUploadPopup_Modern.xaml | Modern yukleme UI | (code-behind) | Alternatif yukleme |
| 9 | PriceUpdateDialog.xaml | Fiyat guncelleme | (code-behind) | Urunler > Fiyat |
| 10 | CategoryManagerDialog.xaml | Kategori yonetimi | (code-behind) | Urunler > Kategoriler |
| 11 | ImageMapWizard.xaml | Resim eslestirme sihirbazi | (code-behind) | Urunler > Resim Eslestir |

#### STOK YONETIMI EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 12 | InventoryView.xaml | Envanter takibi | MainViewModel | Sol menu > Stok |
| 13 | StockUpdateDialog.xaml | Stok guncelleme popup | (code-behind) | Stok > Guncelle |
| 14 | AddStockLotDialog.xaml | Stok lot ekleme | (code-behind) | Stok > Lot Ekle |
| 15 | StockPlacementView.xaml | Stok yerlestirme sistemi | StockPlacementViewModel | Sol menu > Stok Yerlesim |

#### DEPO YONETIMI EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 16 | WarehouseManagementView.xaml | Depo yonetimi ana ekrani | WarehouseManagementViewModel | Sol menu > Depo Yonetimi |

#### SIPARIS EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 17 | OrdersView.xaml | Siparis listeleme/takip | MainViewModel | Sol menu > Siparisler |
| 18 | CustomersView.xaml | Musteri yonetimi | MainViewModel | Sol menu > Musteriler |
| 19 | CustomerEditPopup.xaml | Musteri duzenleme | (code-behind) | Musteriler > Duzenle |

#### BARKOD EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 20 | BarcodeView.xaml | Barkod okuma/kamera | MainViewModel | Sol menu > Barkod |
| 21 | BarcodeIntegrationDialog.xaml | Barkod entegrasyon ayarlari | (code-behind) | Barkod > Ayarlar |
| 22 | BarcodeProductPopup.xaml | Barkod ile urun bilgisi | (code-behind) | Barkod okuma sonrasi |

#### RAPORLAMA EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 23 | ReportsView.xaml | Raporlama ana ekrani | MainViewModel | Sol menu > Raporlar |
| 24 | ExportsView.xaml | Veri disari aktarma (Excel/PDF) | MainViewModel | Sol menu > Disari Aktar |
| 25 | DashboardView.xaml | Ozet dashboard/istatistik | MainViewModel | Sol menu > Dashboard |
| 26 | SimpleDashboardView.xaml | Basit dashboard gorunumu | (code-behind) | Alternatif dashboard |

#### OPENCART ENTEGRASYON EKRANI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 27 | OpenCartView.xaml | OpenCart site yonetimi | (code-behind) | Sol menu > OpenCart |

#### SISTEM/IZLEME EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 28 | HealthMetricsView.xaml | Sistem saglik metrikleri | HealthMetricsViewModel | Sol menu > Sistem |
| 29 | TelemetryView.xaml | Telemetri verileri | TelemetryViewModel | Sol menu > Telemetri |
| 30 | SystemResourcesView.xaml | CPU/RAM/Disk izleme | (code-behind) | Sol menu > Sistem |
| 31 | LogView.xaml | Log goruntuleme | LogCommandViewModel | Sol menu > Loglar |
| 32 | LogMonitoringView.xaml | Canli log izleme | (code-behind) | Sol menu > Log Izleme |

#### AYARLAR/GIRIS EKRANLARI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 33 | SettingsView.xaml | Uygulama ayarlari | MainViewModel | Sol menu > Ayarlar |
| 34 | SettingsOverlayWindow.xaml | Ayarlar overlay penceresi | (code-behind) | Ayarlar popup |
| 35 | LoginWindow.xaml | Giris ekrani | (code-behind) | Uygulama baslangi |
| 36 | WelcomeWindow.xaml | Karsilama ekrani | (code-behind) | Giris sonrasi ilk ekran |
| 37 | SimpleTestView.xaml | Test gorunumu | (code-behind) | Gelistirici |
| 38 | TestWidgetWindow.xaml | Widget test penceresi | (code-behind) | Gelistirici |

#### NEURAL/AI EKRANI

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 39 | NeuralMainWindow.xaml | AI/Neural islem penceresi | (code-behind) | Ozel erisim |

#### COMPONENT'LER (UserControl)

| # | Component | Islev |
|---|-----------|-------|
| C1 | BlurryClockView.xaml | Bulanik saat widget |
| C2 | FlurryClockComponent.xaml | Animasyonlu saat |
| C3 | KPIWidget.xaml | KPI gosterge karti |
| C4 | LiveBackgroundView.xaml | Canli arka plan |
| C5 | LoadingSpinner.xaml | Yukleniyor animasyonu |
| C6 | PaginationComponent.xaml | Sayfalama kontrolu |
| C7 | ToastNotification.xaml | Bildirim popup |
| C8 | WeatherWidgetView.xaml | Hava durumu widget |

### B.3. ViewModel Listesi

| ViewModel | Base | Kullanim |
|-----------|------|----------|
| MainViewModel | ViewModelBase | Ana ViewModel -- urun, stok, siparis, bildirimler |
| ViewModelBase | ObservableObject | MVVM temel sinif |
| StockPlacementViewModel | ViewModelBase | Stok yerlestirme sistemi |
| WarehouseManagementViewModel | ViewModelBase | Depo yonetimi |
| HealthMetricsViewModel | ViewModelBase | Sistem saglik metrikleri |
| TelemetryViewModel | ViewModelBase | OpenCart telemetri izleme |
| LogCommandViewModel | ViewModelBase | Log filtreleme/goruntuleme |

### B.4. Navigasyon Yapisi (MainWindow.xaml)

```
+---------+------------------------------------------+
| Header  | [Firma Adi]  [DB Bilgisi]                |
+---------+------------------------------------------+
|         |                                           |
| Sol     |  ContentPresenter                         |
| Menu    |  (View degistirme code-behind ile)        |
| (Liste  |                                           |
| Buton)  |  Urunler | Stok | Siparisler | Barkod    |
|         |  OpenCart | Raporlar | Ayarlar | Sistem   |
|         |                                           |
+---------+------------------------------------------+
| Status  | [Sistem durumu] [Saat] [Bildirim]         |
+---------+------------------------------------------+
```

- **Navigasyon:** Sol taraf NavMenuItemStyle butonlar + Badge sistemi
- **View degistirme:** Code-behind Click event handler'lar ile (MVVM Navigation yok)
- **Karsilama ekrani:** Welcome modu -- arka plan resimli, mini menu panelli

---

## C. Dashboard View Haritasi

| # | View Dosya Adi | Tahmini Islev | Bagli ViewModel | Kullanici Akisindaki Yeri |
|---|----------------|---------------|-----------------|---------------------------|
| 1 | App.xaml | Uygulama kaynagi, tema tanimlari | - | Baslangic |
| 2 | WorkingLoginWindow.xaml | Kullanici giris ekrani | LoginViewModel | Uygulama acilisi |
| 3 | LoginView.xaml | Login gorunumu (devre disi) | LoginViewModel | (Compile'dan cikarilmis) |
| 4 | MainDashboard.xaml | Ana panel -- modul ikonlari + galeri | MainDashboardViewModel | Giris sonrasi |
| 5 | SimpleTestWindow.xaml | Test penceresi | - | Gelistirici |

**Dashboard Toplam:** 3 aktif View (WorkingLoginWindow, MainDashboard, SimpleTestWindow)
- LoginView.xaml .csproj'da `Compile Remove` ve `Page Remove` ile devre disi birakilmis

**Dashboard .cs Dosya Dagilimi:**
- 3 ViewModel (Base, Login, MainDashboard)
- 4 Service (Authentication, Module, Notification, Image)
- 3 Model (ModuleInfo, Notification, User)
- 13 Converter (Boolean, Color, DateTime, Enum, Math, vb.)
- 3 Entity (User, AuditLog, SystemSettings)
- 4 Repository + UnitOfWork
- 2 Core Service (Settings, User)

---

## D. Coklu Magaza UI Plani Onerisi

### D.1. Tenant Secim/Giris Ekrani

**Mevcut durum:** Tek kullanici (admin/admin123), tenant kavrami yok.

**Oneri:**
```
Uygulama Baslatma Akisi:
1. WorkingLoginWindow --> Kullanici adi + Sifre girisi
2. Basarili giris --> Tenant Secim Ekrani (eger birden fazla tenant'a erisimliyse)
   |
   +-- Tek tenant --> dogrudan MainDashboard
   +-- Coklu tenant --> TenantSelectionView
       [Magaza A - Trendyol+N11]  [Magaza B - HB+Amazon]  [Yeni Tenant Ekle]
3. Tenant secildikten sonra --> MainDashboard (secili tenant context'i ile)
```

**Gerekli yeni View'lar:**
- `TenantSelectionView.xaml` -- tenant secim karti grid
- `TenantSettingsView.xaml` -- tenant ayarlari
- User entity'ye TenantId FK eklenmeli

### D.2. Magaza (Store) Yonetim Ekrani

**Oneri -- StoreManagementView.xaml:**
```
+--------------------------------------------------------+
|  MAGAZA YONETIMI                           [+ Yeni]    |
+--------------------------------------------------------+
|  [Trendyol]          [N11]           [Hepsiburada]     |
|  Magaza: XYZ Shop    Magaza: ABC     (Baglanti yok)    |
|  Durum: Aktif        Durum: Aktif    [Baglan]          |
|  Son Sync: 5dk once  Son Sync: 1sa                     |
|  [Duzenle] [Test]    [Duzenle]       [API Key Gir]     |
+--------------------------------------------------------+
```

**Store ekleme formu:**
1. Platform secimi (dropdown: Trendyol, N11, HB, Amazon, eBay, Ciceksepeti, Ozon, Pazarama, PttAVM, OpenCart)
2. Magaza adi girisi
3. API credential alanlari (platform bazli degisen form)
4. [Baglanti Testi] butonu
5. Sync ayarlari (otomatik sync suresi, bildirimleri)

### D.3. Platform Bazli Gorunum

**Oneri -- MainDashboard'a entegre:**

```
+------------------------------------------------------------+
| [Tum Platformlar v]  |  [Trendyol Shop #1 v]  | [Admin]   |
+------------------------------------------------------------+
|                                                             |
|  Tab yapisi:                                                |
|  [Ozet] [Urunler] [Siparisler] [Stok] [Raporlar]          |
|                                                             |
|  Ozet Tab:                                                  |
|  +----------+ +----------+ +----------+ +----------+       |
|  | Trendyol | | N11      | | HB       | | OpenCart  |      |
|  | 150 urun | | 80 urun  | | 45 urun  | | 200 urun |      |
|  | 23 siparis| | 12 sip.  | | 8 sip.   | | 15 sip.  |      |
|  +----------+ +----------+ +----------+ +----------+       |
+------------------------------------------------------------+
```

**Navigasyon onerisi:** Ust bar'da **dropdown store secici** + tab bazli icerik
- Mevcut WrapPanel ikon sistemi korunabilir
- Her modul ikonuna tiklandiginda secili store context'inde acilir

### D.4. Dashboard'da Tenant/Store Filtreleme

**Ust bar onerisi:**
```
[Tenant: MesTech A.S. v] | [Magaza: Trendyol-Shop1 v] | [Tum Magazalar] | HH:mm | [Admin v]
```

- `TenantStoreSelector` component olarak yazilmali (reusable)
- Secim degistikce tum alt View'lar filtrelenmeli
- MainDashboardViewModel'e `SelectedTenantId` ve `SelectedStoreId` property eklenmeli

### D.5. Kullanici Yetki Bazli Ekran Gizleme

**Mevcut durum:** User.Role = string ("Admin", "User") -- tekil rol.

**Oneri -- RBAC genisletmesi:**

| Rol | Gorebilecegi Ekranlar |
|-----|----------------------|
| SuperAdmin | Tum tenant'lar, tum ekranlar |
| TenantAdmin | Kendi tenant'i -- tum ekranlar |
| StoreManager | Kendi magazasi -- urun, siparis, stok, rapor |
| Warehouse | Stok, depo, barkod, stok yerlesim |
| Sales | Siparis, urun, musteri |
| Viewer | Sadece dashboard + raporlar (salt okunur) |

**Implementasyon:**
- MainWindow navigasyon butonlarinda `Visibility` binding ile rol bazli gizleme
- ModuleService'e `RequiredRole` property eklenmeli
- AuthenticationService'e `HasPermission(string permission)` metodu eklenmeli

---

## E. OpenCart Site Yonetimi UI

### E.1. Mevcut OpenCart Altyapisi

**MesTech_Opencart dizini (3 instance):**
```
MesTech_Opencart/
  +-- Opencart_Stok/        (1. OpenCart instance)
  +-- Opencart_Stok_02/     (2. OpenCart instance)
  +-- Opencart_Stok_03/     (3. OpenCart instance)
  +-- opencart-4.0.2.3/     (OpenCart kaynak kodu)
  +-- opencart_structure.sql (DB semasi)
  +-- setup_admin.sql
  +-- BackupSystem/
```

**MesTech_Stok icerisinde OpenCart entegrasyonu (kapsamli):**
- `Core/Integrations/OpenCart/` -- 20+ dosya: OpenCartClient, OpenCartSyncService, DTO'lar, Telemetri
- `Desktop/Services/` -- IOpenCartService, OpenCartHttpService, MockOpenCartService, OpenCartQueueWorker, OpenCartHealthService, OpenCartInitializer, OpenCartSettingsOptions
- `Desktop/Views/OpenCartView.xaml` -- Yonetim arayuzu

### E.2. OpenCart View Yapisi

`OpenCartView.xaml` -- UserControl olarak implement edilmis:
- Modern kart tasarimi (OpenCartCard stili)
- Buton stilleri: PrimaryButton, SuccessButton, DangerButton
- 3 OpenCart instance'i yonetim kartlari

### E.3. OpenCart UI Onerisi

```
OpenCartView.xaml (Mevcut -- Genisletilecek)
+----------------------------------------------------------+
| OPENCART SITE YONETIMI                                    |
+----------------------------------------------------------+
| [+ Yeni Site Ekle]                                        |
+----------------------------------------------------------+
| +-- Opencart_Stok ----+  +-- Opencart_Stok_02 --+        |
| | URL: stok.mestech.co |  | URL: shop2.mestech.co |       |
| | Durum: [Cevrimici]   |  | Durum: [Cevrimici]    |       |
| | Urun: 150            |  | Urun: 80              |       |
| | Son Sync: 5dk once   |  | Son Sync: 10dk once   |       |
| |                      |  |                        |       |
| | [Sync] [Yonetim] [X] |  | [Sync] [Yonetim] [X]  |       |
| +----------------------+  +------------------------+       |
|                                                            |
| +-- Opencart_Stok_03 --+  +-- [+ Yeni Ekle] ------+      |
| | URL: shop3.mestech.co |  |                        |      |
| | Durum: [Cevrimdisi]   |  |  Yeni OpenCart site    |      |
| | [Baslat] [Ayarlar]    |  |  eklemek icin tiklayin |      |
| +----------------------+  +------------------------+       |
+----------------------------------------------------------+
| SYNC DURUM OZETI                                          |
| [==============================] %85 basarili              |
| Son hata: 3 urun sync edilemedi                           |
+----------------------------------------------------------+
```

**Yeni site ekleme akisi:**
1. OpenCart URL girisi
2. API anahtari girisi (REST API token)
3. Baglanti testi
4. Sync konfigurasyonu (ne siklika, hangi veriler)
5. Basarili --> Kart listeye eklenir

**OpenCart Admin Panel erisimi:** "Yonetim" butonuna tiklandiginda tarayicida `{opencart_url}/admin` acilir (dis link)

---

## F. Cross-Platform Gecis Hazirligi

### F.1. WPF-Ozel Kodlar (TASINAMAZ)

#### F.1.1. P/Invoke ve Windows API Cagrilari (KRITIK)

| Dosya | Satir | API | Aciklama |
|-------|-------|-----|----------|
| `Core/Integrations/Barcode/HidBarcodeListener.cs` | 30-43 | `SetWindowsHookEx`, `UnhookWindowsHookEx`, `CallNextHookEx`, `GetModuleHandle` | Windows Keyboard Hook (WH_KEYBOARD_LL) -- barkod okuyucu icin |
| `Desktop/App.xaml.cs` | 52-56 | `SetForegroundWindow`, `ShowWindow`, `WindowInteropHelper` | Win32 pencere yonetimi, tek instance kontrolu (Mutex) |
| `Desktop/Services/SystemResourceService.cs` | 70-94 | `CreateJobObjectW`, `SetInformationJobObject`, `AssignProcessToJobObject`, `OpenProcess` | CPU throttling Job Object API |
| `Desktop/Services/SystemResourceService.cs` | 53-66 | PerformanceCounter, WMI | Windows sistem kaynak izleme |

#### F.1.2. Windows-Only Paketler ve Kontroller

| Kategori | Dosyalar | Sorun |
|----------|----------|-------|
| **WindowsForms** | csproj: `UseWindowsForms=true` | AForge.Video.DirectShow kamera icin gerekli |
| **AForge** | AForge.Video.DirectShow (kamera) | Sadece Windows DirectShow API |
| **OpenCvSharp4** | OpenCvSharp4.runtime.win | Windows-native runtime |
| **System.Management** | SystemResourceService.cs | Windows WMI API |
| **System.Drawing.Common** | csproj satir 82 | Windows GDI+ bagimli |
| **WPF-only UI** | DropShadowEffect, Storyboard animasyonlar | WPF render pipeline |
| **PasswordBox** | LoginWindow, MainWindow | WPF-ozel kontrol |
| **Microsoft.Win32.OpenFileDialog** | MainDashboardViewModel:214 | Windows file picker |
| **DispatcherTimer** | MainDashboardViewModel:53, OpenCartView.xaml.cs:75-88 | WPF-specific timer |
| **PackIconMaterial (MahApps)** | Tum View'lar | WPF ikon paketi |

### F.2. ViewModel'lerin WPF Bagimliligi

**MainViewModel.cs (Stok):**
- `using System.Windows;` (satir 7) -- `System.Windows.Input` icin
- `using System.Windows.Input;` (satir 8) -- ICommand icin
- **Bunlar CommunityToolkit.Mvvm ile tasinabilir** -- ICommand MAUI'de de calisir

**MainDashboardViewModel.cs (Dashboard):**
- `using System.Windows.Media;` (satir 11) -- **WPF bagimli**
- `using System.Windows.Threading;` (satir 12) -- **DispatcherTimer WPF bagimli**
- `Microsoft.Win32.OpenFileDialog` (satir 214) -- **Windows bagimli**

**Dashboard BaseViewModel.cs -- OZEL SORUN:**
- Custom `INotifyPropertyChanged` implementasyonu (CommunityToolkit.Mvvm KULLANMIYOR)
- MAUI/Avalonia gecisi icin `ObservableObject`'e migrate edilmeli

### F.3. Tasinabilir Kisimlar

| Katman | Durum | Aciklama |
|--------|-------|----------|
| **MesTechStok.Core** | TASINABILIR | Is mantigi, servisler, modeller -- saf C# |
| **MesTech.Domain** | TASINABILIR | Domain entity'ler -- saf C# |
| **MesTech.Application** | TASINABILIR | Uygulama servisleri, interface'ler |
| **MesTech.Infrastructure** | BUYUK OLCUDE TASINABILIR | EF Core, repository'ler |
| **ViewModel'ler** | KISMEN TASINABILIR | System.Windows.* referanslari cikarilmali |
| **View'lar (.xaml)** | TASINAMAZ | Tamamen yeniden yazilmali |
| **OpenCart Entegrasyonu** | TASINABILIR | HTTP client + DTO'lar saf C# |

### F.4. CommunityToolkit.Mvvm MAUI Uyumlulugu

**EVET -- CommunityToolkit.Mvvm MAUI'de de calisir.**

- `ObservableObject`, `ObservableProperty`, `RelayCommand` -- **platform bagimsidir**
- Mevcut kullanim: `[ObservableProperty]` attribute, `SetProperty()` -- **dogrudan tasinir**
- `ICommand` interface'i .NET Standard'da -- MAUI'de de gecerli

### F.5. Kutuphane Platform Uyumluluk Tablosu

| Kutuphane | Surum | MAUI | Avalonia | Not |
|-----------|-------|------|----------|-----|
| CommunityToolkit.Mvvm | 8.2-8.3 | EVET | EVET | Dogrudan tasinir |
| MediatR | 12.4.1 | EVET | EVET | Platform-agnostic |
| Serilog | 4.0/3.1.1 | EVET | EVET | Platform-agnostic |
| Polly | 8.2.0 | EVET | EVET | Platform-agnostic |
| ClosedXML | 0.102.2 | EVET | EVET | Excel export |
| iTextSharp | 3.4.6 | EVET | EVET | PDF export |
| EF Core (SQLite/PG/SQL) | 9.0.x | EVET | EVET | DB-agnostic |
| ZXing.Net | 0.16.9 | KISMI | KISMI | Platform-specific binding gerekir |
| OpenCvSharp4 | 4.9.0 | KISMI | KISMI | Runtime degismeli |
| AForge | 2.2.5 | HAYIR | HAYIR | Tamamen degistirilmeli |
| MaterialDesignThemes | 5.1.0 | HAYIR | KISMI | Material.Avalonia var |
| MahApps.Metro.IconPacks | 6.0.0 | HAYIR | KISMI | Avalonia ikon paketi var |

### F.6. MAUI/Avalonia Gecis Plani Onerisi

**Tavsiye: Avalonia (WPF'e yakin syntax, AXAML formati)**

| Adim | Aciklama | Zorluk |
|------|----------|--------|
| 1 | Core/Domain/Application/Infrastructure katmanlarini aynen koru | Kolay |
| 2 | ViewModel'lerden System.Windows.* referanslarini cikar | Orta |
| 3 | Dashboard BaseViewModel'i ObservableObject'e migrate et | Kolay |
| 4 | DispatcherTimer yerine System.Timers.Timer veya Avalonia DispatcherTimer | Kolay |
| 5 | OpenFileDialog yerine platform-agnostic file picker | Orta |
| 6 | P/Invoke kodlarini (HidBarcodeListener, SystemResourceService) yeniden yaz | Zor |
| 7 | MahApps ikonlarini Avalonia ikon paketine gecir | Orta |
| 8 | DropShadowEffect/Storyboard'lari Avalonia animasyon API'sina cevir | Zor |
| 9 | AForge kamerayi cross-platform kamera kutuphanesine degistir | Zor |
| 10 | System.Drawing.Common yerine SkiaSharp veya ImageSharp | Orta |
| 11 | View'lari AXAML formatinda yeniden yaz (Avalonia XAML) | Zor |
| 12 | MaterialDesignThemes yerine Material.Avalonia | Orta |

**Tahmini gecis suresi:** 2-4 hafta refactoring (P/Invoke modulleri + UI binding guncelleme)

---

## KRITIK BULGULAR

### 1. GUVENLIK -- Hardcoded Sifre (ONCELIK: ACIL)
- `AuthenticationService.cs:16-17` -- Admin sifresi kaynak kodda acik: `admin123`
- `WorkingLoginWindow.xaml:74` -- UI'da sifre gorunuyor
- DbContext'te User tablosu var ama kullanilmiyor -- veritabani tabanli auth'a gecilmeli

### 2. MODULES.JSON UYUMSUZLUGU (ONCELIK: YUKSEK)
- Kok config ve WPF config farkli format kullaniyor
- ModuleService sadece birini okuyor -- diger dosya ise atik
- Tek bir modules.json standardina gecilmeli

### 3. SIGNALR ENTEGRASYONU EKSIK (ONCELIK: ORTA)
- NuGet paketi kurulu (9.0.0) ama hicbir kod yazilmamis
- Gercek zamanli bildirim ve modul arasi iletisim icin gerekli

### 4. NAVIGATION PATTERN EKSIK (ONCELIK: ORTA)
- Stok uygulamasinda View degistirme tamamen code-behind Click event ile
- MVVM Navigation pattern (INavigationService) kullanilmali
- Dashboard'da da benzer durum -- modul baslama Process.Start ile

### 5. CROSS-PLATFORM ENGELLER (ONCELIK: BILGI)
- `UseWindowsForms=true` -- AForge kamera bagimliliginden
- 3 Windows-only paket: AForge, OpenCvSharp4.runtime.win, System.Management
- Gecis icin bu 3 paket cross-platform alternatifleriyle degistirilmeli

### 6. 10/12 MODUL GELISTIRILMEMIS (ONCELIK: BILGI)
- Dashboard'da 12 modulden 10'u "henuz gelistirilmemistir" mesaji gosteriyor
- Sadece Trendyol (HTML) ve Stok (EXE) calisiyor

---

## ONERILER

### 1. Guvenlik Oncelikleri
- [ ] AuthenticationService'i veritabani tabanli auth'a gecir (User tablosu hazir)
- [ ] Hardcoded sifreleri kaldir, bcrypt/Argon2 hash kullan
- [ ] JWT veya session token sistemi ekle
- [ ] Login denemesi sinirlamasi (MaxLoginAttempts zaten ayarlarda tanimli)

### 2. Modul Sistemi Standardizasyonu
- [ ] Tek modules.json formati belirle (WPF icindeki dizi formati daha esnek)
- [ ] Her modul icin IModule interface'i implement et (yol haritasi dokumandaki gibi)
- [ ] Modul durum izleme: IsRunning, LastHeartbeat, HealthCheck

### 3. Multi-Tenant Altyapisi
- [ ] User entity'ye TenantId, Tenant entity olustur
- [ ] Store entity: TenantId + PlatformType + ApiCredentials
- [ ] DbContext'e tenant filtreleme (global query filter)
- [ ] UI: TenantSelectionView + StoreManagementView

### 4. Navigasyon Modernizasyonu
- [ ] INavigationService interface'i olustur
- [ ] ContentControl + DataTemplate bazli View degistirme
- [ ] Code-behind Click handler'lari ViewModel Command'larina tasi

### 5. Cross-Platform Hazirlik
- [ ] Core/Domain/Application katmanlarinin Windows bagimliligini kontrol et
- [ ] ViewModel'leri saf C# yap (System.Windows referanslarini kaldir)
- [ ] Kamera sistemi icin cross-platform kutuphane arastirmasi (Camera.MAUI, vb.)
- [ ] Pilot: Basit bir View'i Avalonia'ya tasima denemesi

### 6. OpenCart UI Gelistirmesi
- [ ] OpenCartView'a 3 instance karti ekle
- [ ] Yeni site ekleme wizard'i
- [ ] Sync durum ozeti ve hata raporlama
- [ ] Canli sync progress gostergesi (SignalR ile)

---

**RAPOR SONU -- TAKIM 3**
