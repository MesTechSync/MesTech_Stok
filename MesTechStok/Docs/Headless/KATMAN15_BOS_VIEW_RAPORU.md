# KATMAN 1.5 BOŞ VIEW KÖK NEDEN ANALİZ RAPORU

**Tarih:** 31 Mart 2026
**Yazar:** DEV 5 — Test & Kalite Mühendisi
**Kaynak:** `BulkScreenshotLayer15Tests.cs` — DI + Testcontainers PostgreSQL
**Yöntem:** Her view'ın screenshot'ı görsel olarak incelendi + ViewModel kaynak kodu okundu

---

## 1. KATMAN 1 vs KATMAN 1.5 KARŞILAŞTIRMA

| Kategori | Katman 1 (DI yok) | Katman 1.5 (DI + PG) | Fark | Açıklama |
|----------|-------------------|----------------------|------|----------|
| **DATA (>20KB)** | 28 | **52** | **+24** | Veri gösteren view sayısı 2x |
| **MINIMAL (10-20KB)** | 49 | **100** | **+51** | Layout + boş DataGrid |
| **LOADING (<10KB)** | 89 | **20** | **-69** | Spinner sayısı %78 düştü |
| **FAIL** | 0 | **0** | 0 | 172/172 render |

**Sonuç:** DI eklenmesi 69 view'ı loading→minimal/data'ya yükseltti.

---

## 2. ANA KÖK NEDEN: PostgreSQL Migration Hatası (P0)

### Hata Detayı

```
Npgsql.PostgresException : 42601: syntax error at or near "["
POSITION: 116
```

### Neden Oluşuyor

EF Core `OnModelCreating` içinde `text[]` veya `jsonb[]` PostgreSQL array
column tanımı var. Testcontainers `postgres:17-alpine` container'ında
`EnsureCreatedAsync` çalıştırıldığında bu SQL syntax hatası veriyor.

### Etki Zinciri

```
Migration fail
  → DB şeması yok
    → Seed data yüklenemedi
      → Handler.Handle() → DbContext.Set<T>().ToListAsync() → EXCEPTION
        → BaseView.OnAttachedToVisualTree catch bloğu → HasError=true
          → View loading/error state'de kalıyor
```

### Fix Sorumlusu: DEV 1

AppDbContext OnModelCreating'deki array/jsonb column tanımlarını
Testcontainers uyumlu hale getir veya `postgres:17` (alpine olmayan) kullan.

---

## 3. 20 BOŞ VIEW DETAYLI ANALİZ

### GRUP A: Sadece DB Sorgusu Gereken View'lar (14 adet) — P1

Bu view'lar constructor'da yalnız `IMediator` ve/veya `ICurrentUserService` alıyor.
Migration fix'i ile **OTOMATIK** düzelecek — ek müdahale gereksiz.

| # | View | KB | Screenshot Görüntüsü | ViewModel Constructor | LoadAsync Query | Fix |
|---|------|----|---------------------|----------------------|-----------------|-----|
| 1 | InvoiceManagementAvaloniaView | 8 | "Kayit bulunamadi — fatura bulunamadi" | `(IMediator)` | `GetInvoicesQuery` | DEV 1 |
| 2 | InventoryAvaloniaView | 9 | ProgressBar + "Yukleniyor..." | `(IMediator)` | `GetInventoryPagedQuery` | DEV 1 |
| 3 | ReportsAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetDashboardSummaryQuery` | DEV 1 |
| 4 | StockUpdateAvaloniaView | 9 | ProgressBar spinner | `(IMediator)` | `GetProductsQuery` | DEV 1 |
| 5 | BordroAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetEmployeesQuery` | DEV 1 |
| 6 | CategoryAvaloniaView | 9 | ProgressBar spinner | `(IMediator)` | `GetCategoriesQuery` | DEV 1 |
| 7 | StockLotAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetStockLotsQuery` | DEV 1 |
| 8 | GelirGiderAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ITenantProvider)` | `GetIncomesQuery` | DEV 1 |
| 9 | SupplierFeedListAvaloniaView | 9 | ProgressBar spinner | `(IMediator)` | `GetFeedSourcesQuery` | DEV 1 |
| 10 | OrderDetailAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetOrderListQuery` | DEV 1 |
| 11 | IncomeExpenseListView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetIncomesQuery` | DEV 1 |
| 12 | StockValueReportView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetStockValueReportQuery` | DEV 1 |
| 13 | ReturnListAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetReturnListQuery` | DEV 1 |
| 14 | FulfillmentSettingsView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | `GetFulfillmentSettingsQuery` | DEV 1 |

**Ortak Pattern:** Hepsi `override async Task LoadAsync()` içinde `_mediator.Send(new GetXXXQuery(...))` çağırıyor. DB olmadığı için handler exception fırlatıyor, BaseView catch bloğu `HasError=true` yapıyor.

### GRUP B: DB + Platform/Adapter Bağımlılığı (3 adet) — P1

Constructor'da adapter veya platform servisi gerektirir. Migration fix tek başına yetmez — adapter stub da lazım.

| # | View | KB | Screenshot Görüntüsü | ViewModel Constructor | Ek Bağımlılık | Fix |
|---|------|----|---------------------|----------------------|---------------|-----|
| 15 | PlatformSyncAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | Platform sync handler | DEV 1+3 |
| 16 | CargoTrackingAvaloniaView | 9 | "Kargo verileri yukleniyor..." | `(IMediator, ITenantProvider)` | Cargo tracking handler | DEV 1+3 |
| 17 | PlatformListAvaloniaView | 9 | ProgressBar spinner | `(IMediator, ICurrentUserService)` | Platform list handler | DEV 1+3 |

**Not:** Bu view'ların handler'ları sadece DB query yapmıyor, aynı zamanda `IAdapterFactory` üzerinden platform status da çekiyor. `InMemoryAdapterFactory` zaten DI'da kayıtlı ama handler'ın iç mantığında DB gerekiyor.

### GRUP C: DB + UI Servis Bağımlılığı (3 adet) — P2

Constructor'da `IDialogService`, `IThemeService` gibi Avalonia-specific servisler var. HeadlessTestApp'te bu servisler kayıtlı değil.

| # | View | KB | Screenshot Görüntüsü | ViewModel Constructor | Eksik Servis | Fix |
|---|------|----|---------------------|----------------------|--------------|-----|
| 18 | SettingsAvaloniaView | 6 | "Ayarlar yukleniyor..." + Tekrar Dene | `(IMediator, ICurrentUserService, IDialogService, IThemeService)` | `IDialogService`, `IThemeService` | DEV 2+5 |
| 19 | LogViewerAvaloniaView | 6 | "Log kayıtları yükleniyor..." + Tekrar Dene | `(IMediator, ICurrentUserService)` | Serilog query API | DEV 4 |
| 20 | CargoAvaloniaView | 6 | "Kargo verileri yukleniyor..." + Tekrar Dene | `(IDialogService, IMediator, ICurrentUserService)` | `IDialogService` | DEV 3+5 |

**Not:** SettingsAvaloniaView en küçük (6KB) çünkü DI'dan `IDialogService` resolve edilemiyor → ViewModel constructor patlar → DataContext null → BaseView.InitializeAsync hiç çağrılmıyor → sadece XAML default loading state görünüyor.

---

## 4. SCREENSHOT GÖRSEL KANIT ÖZETİ

| Ekran Durumu | Açıklama | Kaç View | Örnek |
|-------------|----------|----------|-------|
| ProgressBar + "Yukleniyor..." | BaseView loading state — ViewModel resolve OK ama LoadAsync fail | 14 | InventoryAvaloniaView |
| "Kayit bulunamadi" | Handler graceful empty response — crash yok | 1 | InvoiceManagementAvaloniaView |
| "...yukleniyor..." + Tekrar Dene | DI resolve fail veya DB exception caught | 5 | SettingsAvaloniaView |

**Önemli:** Hiçbir view CRASH vermiyor. Tümü graceful degradation yapıyor — bu BaseView.OnAttachedToVisualTree try/catch bloğunun doğru çalıştığının kanıtı.

---

## 5. 3 AŞAMALI ÇÖZÜM PLANI

### Aşama 1: Migration Fix (DEV 1) — P0 — Tahmini etki: 14 view düzelir

```
Görev: AppDbContext OnModelCreating → text[]/jsonb[] array column tanımlarını
       Testcontainers PG17 ile uyumlu hale getir.
Alternatif: Testcontainers image → postgres:17 (alpine olmayan)
Test: dotnet test tests/MesTech.Tests.Headless/ --filter "BulkScreenshotLayer15"
Beklenen: GRUP A 14 view loading→data'ya geçer
```

### Aşama 2: Adapter/Handler Genişletme (DEV 3) — P1 — Tahmini etki: 3 view

```
Görev: Platform sync/list/tracking handler'larında DB fail olunca
       graceful fallback döndür (boş liste, default status).
       InMemoryAdapterFactory'ye sync status dummy data ekle.
Test: GRUP B 3 view'ın screenshot boyutu >10KB olmalı
```

### Aşama 3: Stub UI Servisler (DEV 5) — P2 — Tahmini etki: 3 view

```
Görev: HeadlessDialogService : IDialogService (no-op stub)
       HeadlessThemeService : IThemeService (default theme)
       BulkScreenshotLayer15Tests.BuildDiContainer()'a kaydet.
Test: GRUP C 3 view'ın DataContext != null olmalı
```

### Beklenen Nihai Sonuç

| Kategori | Şimdi | Aşama 1 | Aşama 2 | Aşama 3 |
|----------|-------|---------|---------|---------|
| DATA (>20KB) | 52 | 66 | 69 | **72** |
| MINIMAL (10-20KB) | 100 | 100 | 100 | **100** |
| LOADING (<10KB) | 20 | 6 | 3 | **0** |

---

## 6. GOREV_HAVUZU REFERANSLARI

| Görev ID | Açıklama | Öncelik | Sorumlu |
|----------|----------|---------|---------|
| G538 | BetweenConverter IMultiValueConverter bug | P1 | DEV 2 — **KAPANDI** |
| G539 | 163 x:Name root cleanup | P3 | DEV 5 — KAPANDI |
| **YENİ** | PG Migration `[]` array syntax Testcontainers uyumu | **P0** | DEV 1 |
| **YENİ** | HeadlessDialogService + HeadlessThemeService stub | P2 | DEV 5 |
