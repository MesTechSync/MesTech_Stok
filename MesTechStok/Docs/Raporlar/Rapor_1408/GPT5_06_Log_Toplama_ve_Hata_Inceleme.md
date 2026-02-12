## Log Kaynakları ve Toplama
- **Serilog (Desktop)**: Uygulama klasörü `Logs/` altında günlük döndürme ile `mestech-.log`.
- **GlobalLogger (Desktop)**: Bellek içi ring buffer; UI açılmasa da geçmiş korunur.
- **Veritabanı Telemetrisi (Core)**: `ApiCallLogs`, `CircuitStateLogs`, `BarcodeScanLogs` tabloları.
- **Senkronizasyon Kuyruğu**: `SyncRetryItems` – başarısız öğelerin nedeni, kategori, yeniden deneme zamanı.

## Korelasyon Kimliği (CorrelationId)
- `CorrelationContext` ile AsyncLocal bazlı kimlik üretimi/taşınması.
- HTTP çağrılarında `X-Correlation-ID` başlığı eklenir (`RetryAndCorrelationHandler`).
- Analiz için aynı `CorrelationId` ile dosya logu + DB logları birleştirilebilir.

## Hata Kategorileri ve Dayanıklılık
- `RetryAndCorrelationHandler`: 408/429/5xx ve geçici hatalarda üstel retry + jitter.
- Pencere bazlı hata oranı ile devre durum geçişleri (Closed/Open/HalfOpen).
- `SqlServerResilienceTelemetry` devre geçişlerini `CircuitStateLogs` tablosuna kalıcı yazar.

## İnceleme Sorguları (SQL)
- Son 24 saatte başarısız API çağrıları:
```sql
SELECT TOP 200 * FROM ApiCallLogs WITH (NOLOCK)
WHERE TimestampUtc >= DATEADD(day, -1, SYSUTCDATETIME()) AND Success = 0
ORDER BY TimestampUtc DESC;
```
- Devre açık/yarı-açık geçişleri:
```sql
SELECT TOP 200 * FROM CircuitStateLogs WITH (NOLOCK)
ORDER BY TransitionTimeUtc DESC;
```
- Belirli CorrelationId için tüm izler:
```sql
DECLARE @cid nvarchar(64) = N'<CorrelationId>';
SELECT 'API' AS Src, * FROM ApiCallLogs WHERE CorrelationId = @cid
UNION ALL
SELECT 'CB'  AS Src, * FROM CircuitStateLogs WHERE CorrelationId = @cid
UNION ALL
SELECT 'QR'  AS Src, * FROM BarcodeScanLogs WHERE CorrelationId = @cid;
```

## Dışa Aktarım ve Saklama
- Dosya logları günlük döndürme ile 30 gün tutulur (Serilog). İhtiyaca göre arşiv/retention artırılmalı.
- DB telemetri tablolarında indeksler mevcut; periyodik arşiv ve temizlik (örn. 90 gün) önerilir.

## Alarm ve Dashboard Önerisi
- Başarısızlık oranı, açık devre sayısı, 5xx dağılımı ve en yavaş endpointler için bir dashboard.
- Kritik eşikler: FailRate ≥ 20%, ardışık retry sayısı ≥ 5, Open state süresi ≥ 2dk.

## Gizlilik ve Uyumluluk
- PII veri loglarına sınırlama; `ApiCallLogs` only-meta yaklaşımı sürdürülmeli.
- Uzak anahtarlar/secret’lar loglara yazılmamalıdır.
