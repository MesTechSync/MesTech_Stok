## Kapsam ve Amaç
- **Hedef**: .NET/WPF tabanlı MesTech Stok sisteminin mimarisini gerçek koda dayalı netleştirmek.
- **Kaynak**: `MesTechStok.Core`, `MesTechStok.Desktop`, `MesTechStok.Screensaver`, `MesTechStok.SystemResources` ve EF Core şeması.

## Üst Düzey Mimari
- **Sunum**: `MesTechStok.Desktop` (WPF, MVVM, DI, Serilog), `MesTechStok.Screensaver`, `MesTechStok.SystemResources`.
- **Uygulama/Domain**: `MesTechStok.Core` – EF Core `AppDbContext`, domain servisleri, güvenlik, senkronizasyon, dayanıklılık.
- **Entegrasyon**: OpenCart istemcisi/senkronizasyonu, Retry + CircuitBreaker, korelasyon kimliği.
- **Veri/Telemetri**: SQL Server; `ApiCallLogs`, `CircuitStateLogs`, `BarcodeScanLogs`, `SyncRetryItems`.

## Çekirdek Bileşenler
- EF Core: `Products`, `Categories`, `Suppliers`, `Customers`, `Warehouses`, `StockMovements`, `Orders`, `OrderItems`.
- Güvenlik: `Users`, `Roles`, `UserRoles`, `Permissions`, `RolePermissions`.
- Ayarlar/Telemetri: `CompanySettings`, `ApiCallLogs`, `CircuitStateLogs`, `BarcodeScanLogs`.
- Senkronizasyon: `InventoryLots`, `OfflineQueue`, `SyncRetryItems`.

## Entegrasyon Mimarisi
- HTTP hattı: `RetryAndCorrelationHandler` – üstel backoff + jitter, `X-Correlation-ID`, devre durumları, telemetri.
- OpenCart: `IOpenCartClient` + `OpenCartClient`, `OpenCartSyncService` – çift yönlü ürün/sipariş senkronu.

## Telemetri ve Loglama
- Serilog dosya logları; Desktop `GlobalLogger` ring buffer.
- DB telemetri: `ApiCallLogs` (endpoint/süre/durum), `CircuitStateLogs` (geçişler/oranlar), `BarcodeScanLogs`.

## Güvenlik
- Rol/izin matrisi; demo tohumlamada BCrypt hash; doğrulama yolunda tutarlılık gerektirir.

## Dağıtım/Konfigürasyon
- SQL Server önerilir; migrations + canlıda yardımcı ek kolon/indeks oluşturucular mevcut.
- `appsettings.json` + `appsettings.user.json` (override). Dayanıklılık eşikleri konfigürasyonla yönetilir.

## Veri Akışı (Özet)
WPF UI → Core Servisler → EF Core / OpenCart HTTP → Retry/CircuitBreaker → Telemetri → Hata durumunda `SyncRetryItems` kuyruk.
