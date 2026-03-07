# DEV 3: API & Entegrasyon — Eksik Bilgi Raporu

**Tarih:** 07 Mart 2026
**Hazirlayan:** DEV 3 — API & Entegrasyon Takimi

---

## TRENDYOL (Mevcut TrendyolApiClient ile cekilebilir)

| Yetenek | Durum | Aciklama |
|---------|-------|----------|
| Urun listesi (GET) | MEVCUT | `GetProductsAsync` — sayfalama destekli |
| Urun olusturma (POST) | MEVCUT | `CreateProductAsync` — v2 endpoint |
| Urun guncelleme (PUT) | MEVCUT | `UpdateProductAsync` |
| Siparis listesi (GET) | MEVCUT | `GetOrdersAsync` — tarih filtreli |
| Siparis durumu guncelleme | MEVCUT | `UpdateOrderStatusAsync` |
| Kategori listesi (GET) | MEVCUT | `GetCategoriesAsync` |
| Kategori nitelikleri (GET) | MEVCUT | `GetCategoryAttributesAsync` |
| Stok guncelleme (PUT) | MEVCUT | `UpdateStockAsync` — toplu |
| Fiyat guncelleme (PUT) | MEVCUT | `UpdatePriceAsync` — toplu |
| Health Check | MEVCUT | `HealthCheckAsync` |
| Marka listesi | KONTROL GEREKLI | Endpoint mevcut ama client'ta metod yok |
| Webhook endpoint kaydi | KONTROL GEREKLI | Trendyol webhook desteigi arastirma gerekli |
| Rate Limiting | MEVCUT | 100 concurrent, per-second limit |
| Retry Policy (Polly) | MEVCUT | Exponential backoff, transient error handling |
| Basic Auth | MEVCUT | SupplierId:ApiKey base64 encoded |

---

## OPENCART (Mevcut OpenCartClient ile cekilebilir)

| Yetenek | Durum | Aciklama |
|---------|-------|----------|
| Urun listesi (GET) | MEVCUT | `GetAllProductsAsync` — sayfalama destekli |
| Urun olusturma (POST) | MEVCUT | `CreateProductAsync` |
| Urun guncelleme (PUT) | MEVCUT | `UpdateProductAsync` |
| Urun silme (DELETE) | MEVCUT | `DeleteProductAsync` |
| Urun stok guncelleme | MEVCUT | `UpdateProductStockAsync` |
| Urun fiyat guncelleme | MEVCUT | `UpdateProductPriceAsync` |
| SKU ile urun arama | MEVCUT | `GetProductBySkuAsync` |
| Siparis listesi (GET) | MEVCUT | `GetAllOrdersAsync` — tarih filtreli |
| Yeni siparisler (GET) | MEVCUT | `GetNewOrdersAsync` |
| Siparis durumu guncelleme | MEVCUT | `UpdateOrderStatusAsync` |
| Kategori listesi (GET) | MEVCUT | `GetCategoriesAsync` |
| Kategori olusturma (POST) | MEVCUT | `CreateCategoryAsync` |
| Musteri bilgileri (GET) | MEVCUT | `GetCustomerByIdAsync`, `GetCustomerByEmailAsync` |
| Toplu urun senkronizasyonu | MEVCUT | `BulkSyncProductsAsync` |
| Toplu stok guncelleme | MEVCUT | `BulkUpdateStockAsync` |
| Baglanti testi | MEVCUT | `TestConnectionAsync` |
| Event-driven bildirimler | MEVCUT | `ApiCallSuccess`, `ApiCallError`, `ConnectionStatusChanged` events |
| Resilience/Retry | MEVCUT | `RetryAndCorrelationHandler` ile retry + correlation ID |
| Kargo yonetimi | YOK | OpenCart'ta kargo API mevcut degil |

---

## DIGER 8 PLATFORM (Kod yok — sadece iskelet adapter)

| Platform | Durum | Auth Tipi | Notlar |
|----------|-------|-----------|--------|
| Ciceksepeti | ISKELET | API Key | Webhook destekli adapter iskeleti hazir |
| Hepsiburada | ISKELET | Basic Auth | Stok ornegi mevcut — FAZ 2 |
| N11 | ISKELET | SOAP | SOAP wrapper gerekli — `SoapAuthProvider` hazir |
| Pazarama | ISKELET | API Key | Basit API Key — `ApiKeyAuthProvider` hazir |
| Amazon TR | ISKELET | OAuth2 | OAuth2 flow — `OAuth2AuthProvider` iskeleti hazir |
| eBay | ISKELET | OAuth2 | OAuth2 + multi-currency gerekli |
| PTT AVM | ISKELET | API Key | Kargo entegrasyon oncelikli |
| Ozon | ISKELET | API Key | FBO/FBS modeli gerekli |

**Tum iskelet adapter'lar:**
- `IIntegratorAdapter` implement ediyor
- `dotnet build` basarili
- `TestConnectionAsync` uyari mesaji donduruyor
- `NotImplementedException` ile FAZ 2'ye isaretli

---

## AUTH PROVIDER DURUMU

| Provider | Durum | Platformlar |
|----------|-------|-------------|
| `ApiKeyAuthProvider` | HAZIR | Trendyol, Ciceksepeti, Ozon, Pazarama, PttAVM |
| `BasicAuthProvider` | HAZIR | Hepsiburada |
| `OAuth2AuthProvider` | ISKELET | Amazon, eBay (token endpoint TODO) |
| `SoapAuthProvider` | HAZIR | N11 |

---

## SONUC

- **2 platform tam implement:** Trendyol, OpenCart
- **8 platform iskelet hazir:** Derleniyor, FAZ 2'de doldurulacak
- **4 auth provider hazir:** Tum auth tipleri kapsanmis
- **0 hardcoded API key:** Grep ile dogrulanmis
- **Baglanti testi:** `TestConnectionAsync` Trendyol ve OpenCart icin calisir durumda
