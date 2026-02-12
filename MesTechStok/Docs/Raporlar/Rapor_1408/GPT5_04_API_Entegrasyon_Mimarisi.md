## OpenCart Entegrasyonu
- **İstemci**: `OpenCartClient` (Authorization header, JSON, senkron durum dosyası `sync-state.json`).
- **Sağlık Testi**: `/api/system/info` ile bağlantı doğrulama.
- **Senkron Servisi**: `OpenCartSyncService` – ürünleri içe/dışa aktarır, siparişleri çeker ve durum günceller.

## HTTP Politika Zinciri
- **Retry & Correlation**: `RetryAndCorrelationHandler`
  - Üstel backoff: varsayılan `[1,2,4,8,16]` sn + jitter.
  - Yeniden deneme kriterleri: 408/429/5xx (501/505 hariç) + geçici istisnalar.
  - Başlıklara `X-Correlation-ID` eklenir (kaynak: `CorrelationContext`).
  - Pencere bazlı devre durumu: `Closed → Open → HalfOpen` geçişleri.
- **Circuit Breaker (Core)**: `EnhancedCircuitBreaker`
  - Ayarlar: başarısızlık eşiği, süre penceresi, success threshold, timeout.
  - Metrik toplama ve olay yayımı (`StateChanged`).

## Telemetri Kalıcılığı
- **`SqlServerResilienceTelemetry`**
  - `OnApiCall` → `ApiCallLogs` tablosuna endpoint, süre, sonuç, kategori, statusCode, CorrelationId.
  - `OnCircuitStateChange` → `CircuitStateLogs` tablosuna durum geçişi, oranlar, zaman.

## Konfigürasyon (Örnek)
- `Resilience:CircuitBreaker` → `FailRateThreshold`, `SlidingWindowSeconds`, `OpenStateDurationSeconds`, `HalfOpenMaxCalls`, `MinimumThroughput`.
- `Resilience:Retry:BackoffSeconds` → yeniden deneme paternleri.
- `OpenCartSettings` → `ApiUrl`, `ApiKey`, `AutoSyncEnabled`, `SyncIntervalMinutes`.

## Hata Yönetimi
- Başarısız her öğe için `SyncRetryService` kuyruk kaydı; zamanlayıcı ile tekrar deneme.
- Telemetri yazımı best-effort; hatalar yutulur, ana akış kesilmez.
