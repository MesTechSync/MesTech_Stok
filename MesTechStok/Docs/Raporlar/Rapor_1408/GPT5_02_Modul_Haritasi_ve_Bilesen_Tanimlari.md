## Modül Haritası
- **Desktop (WPF)**: Views, ViewModels, Services, Components, Converters, Styles, Utils.
- **Core (Domain/Service)**: Data (EF Core), Services (Product/Order/Inventory/Auth), Resilience, Integrations.
- **Integrations/OpenCart**: `OpenCartClient`, DTOs, `OpenCartSyncService`, Telemetry, HTTP Handler.
- **Screensaver/SystemResources**: Görsel ekran koruyucu ve sistem kaynak izleme uygulamaları.

## Bileşen Tanımları (Özet)
- **ViewModels**: `MainViewModel` (sistem durumu, veri yükleme), `TelemetryViewModel`, `HealthMetricsViewModel`.
- **Views**: Dashboard, Products, Inventory, Orders, Reports, Logs; modern kontrol ve animasyonlar.
- **Services (Desktop)**: `ApplicationMonitoringService`, `BarcodeHardwareService`, `AuthorizationService`, `OpenCartHttpService`.
- **Services (Core)**:
  - `ProductService`, `InventoryService`, `OrderService`, `CustomerService`.
  - `AuthService` (rol/izin), `TokenRotationService` (var), `AdaptivePaginationService`.
  - `DeltaSyncService` (delta iskeleti), `SyncRetryService` (kuyruk), `EnhancedCircuitBreaker`.
- **Integrations**:
  - `OpenCartClient` (HTTP + telemetri + senkron durum dosyası), `OpenCartSyncService` (tam/delta senkron akışları).
  - `RetryAndCorrelationHandler` (üstel retry + korelasyon başlığı + hafif circuit mantığı).

## Veri Modelleri (Ana)
- Ürün/Category/Supplier/Warehouse, Sipariş/OrderItem, StokHareketi, Kullanıcı/Rol/İzin.
- Telemetri: `ApiCallLog`, `CircuitStateLog`, Barkod: `BarcodeScanLog`, Senkron: `SyncRetryItem`, `InventoryLot`, `OfflineQueueItem`.

## Etkileşimler
- Desktop VM → Core servisleri (DI) → EF Core ve/veya OpenCart HTTP.
- OpenCart senkronu → Retry/CircuitBreaker → `SqlServerResilienceTelemetry` → DB logları.
- Hatalı senkron öğeleri → `SyncRetryItems` → zamanlayıcıyla tekrar.
