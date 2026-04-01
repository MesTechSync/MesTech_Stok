# Trendyol Marketplace API Keşif Notu
# Tarih: 1 Nisan 2026
# Kaynak: developers.trendyol.com (v2.0 + v3.0)
# Amaç: TrendyolAdapter eksik endpoint tespiti + geliştirme planı

## 1. Authentication

- **Yöntem:** HTTP Basic Authentication
- **Header:** `Authorization: Basic base64(ApiKey:ApiSecret)`
- **Gerekli:** ApiKey, ApiSecret, SupplierId (satıcı no)
- **Sandbox:** `https://stage-apigw.trendyol.com` (IP yetkilendirme gerekli)
- **Production:** `https://apigw.trendyol.com`
- **MesTech durumu:** TAM UYUMLU (TrendyolAdapter satır 148-153)

## 2. Rate Limiting

| Kural | Değer |
|-------|-------|
| Limit | 50 request / 10 saniye (aynı endpoint'e) |
| Aşım | HTTP 429 Too Many Requests |
| Retry-After | Header'dan okunur, yoksa 11s varsayılan |
| **MesTech** | SemaphoreSlim(100), Polly 5-attempt 429 retry, Retry-After parse |

## 3. Product Endpoint'leri

| # | Method | URL | Açıklama | MesTech |
|---|--------|-----|----------|---------|
| P1 | POST | `/integration/product/sellers/{sid}/v2/products` | Ürün oluşturma | ✅ TAM |
| P2 | PUT | `/integration/product/sellers/{sid}/products` | Ürün güncelleme | ❌ EKSİK |
| P3 | GET | `/integration/product/sellers/{sid}/products` | Ürün listeleme | ✅ TAM |
| P4 | DELETE | `/integration/product/sellers/{sid}/products` | Ürün silme | ❌ EKSİK |
| P5 | POST | `/integration/inventory/sellers/{sid}/products/price-and-inventory` | Stok+fiyat güncelle | ✅ TAM |
| P6 | GET | `...products/batch-requests/{batchId}` | Batch sonuç | ✅ TAM |
| P7 | GET | `/integration/product/brands` | Marka listesi | ✅ TAM |
| P8 | GET | `/integration/product/product-categories` | Kategori ağacı | ✅ TAM |
| P9 | GET | `...product-categories/{catId}/attributes` | Kategori özellikleri | ✅ TAM |
| P10 | GET | `/integration/sellers/{sid}/addresses` | İade/gönderim adresleri | ❌ EKSİK |
| P11 | GET | `/integration/product/shipment-providers` | Kargo firma listesi | ❌ EKSİK |
| P12 | POST | `...sellers/{sid}/v2/products/archive` | Ürün arşivleme | ✅ TAM |
| P13 | POST | `...sellers/{sid}/v2/products/unlock` | Arşivden çıkarma | ✅ TAM |

## 4. Order Endpoint'leri

| # | Method | URL | Açıklama | MesTech |
|---|--------|-----|----------|---------|
| O1 | GET | `/integration/order/sellers/{sid}/orders` | Sipariş listele | ✅ TAM |
| O2 | PUT | `...shipment-packages/{pkgId}` | Paket durum güncelle | ✅ TAM |
| O3 | PUT | `...shipment-packages/{pkgId}/tracking-details` | Takip no güncelle | ⚠️ KISMI |
| O4 | PUT | `...shipment-packages/{pkgId}/items/unsupplied` | Paket iptal | ❌ EKSİK |
| O5 | POST | `...shipment-packages/{pkgId}/split-packages` | Paket bölme | ⚠️ KISMI |
| O6 | POST | `...orders/invoiceLinks` | Fatura link gönder | ✅ TAM |
| O7 | POST | `...orders/invoice-file` | Fatura dosya yükle | ✅ TAM |
| O8 | POST | `...sellers/{sid}/invoices` | Fatura bilgisi gönder | ✅ TAM |

## 5. Claim / İade Endpoint'leri

| # | Method | URL | Açıklama | MesTech |
|---|--------|-----|----------|---------|
| R1 | GET | `...sellers/{sid}/claims` | İade talepleri | ✅ TAM |
| R2 | POST | `...claims/{id}/approve` | İade onayla | ✅ TAM |
| R3 | POST | `...claims/{id}/issue` | İade reddet | ✅ TAM |
| R4 | GET | `...claims/compensation` | Tazminat sorgula | ✅ TAM |
| R5 | GET | Claim Audit Information | İade audit | ❌ EKSİK |

## 6. Finance / Settlement Endpoint'leri

| # | Method | URL | Açıklama | MesTech |
|---|--------|-----|----------|---------|
| F1 | GET | `/integration/finance/sellers/{sid}/settlement` | Hesap kesimi özet | ✅ TAM |
| F2 | GET | `...sellers/{sid}/settlements` | Settlement detay | ✅ TAM |
| F3 | GET | `...sellers/{sid}/cargo-invoices` | Kargo faturaları | ✅ TAM |
| F4 | GET | Current Account Statement | Cari hesap ekstre | ❌ EKSİK |

## 7. Webhook Endpoint'leri

| # | Method | URL | Açıklama | MesTech |
|---|--------|-----|----------|---------|
| W1 | POST | `...sellers/{sid}/webhooks` | Webhook oluştur | ✅ TAM |
| W2 | GET | filter webhooks | Webhook listele | ❌ EKSİK |
| W3 | PUT | update webhook | Webhook güncelle | ❌ EKSİK |
| W4 | DELETE | delete webhook | Webhook sil | ❌ STUB |
| W5 | PUT | activate/deactivate | Webhook aktif/deaktif | ❌ EKSİK |

**Webhook bilgiler:**
- Max 15 webhook/satıcı (deaktif dahil)
- Auth: BASIC_AUTHENTICATION veya API_KEY
- 13 event tipi: CREATED, PICKING, INVOICED, SHIPPED, CANCELLED, DELIVERED, ...
- Hata durumunda 5dk retry, başarısız → otomatik deaktivasyon

## 8. Q&A (Soru-Cevap) Endpoint'leri

| # | Method | URL | Açıklama | MesTech |
|---|--------|-----|----------|---------|
| Q1 | GET | `/integration/qna/sellers/{sid}/questions/filter` | Soru filtreleme | ⚠️ YANLIŞ PATH |
| Q2 | GET | `...questions/{id}` | Soru detayı | ❌ EKSİK |
| Q3 | POST | `...questions/{id}/answers` | Soru yanıtlama | ⚠️ YANLIŞ PATH |

**NOT:** MesTech adapter'da path `/integration/product/sellers/...` kullanılıyor, doğru path `/integration/qna/sellers/...` olmalı.

## 9. Eksik Endpoint Özeti (10 adet)

1. **Product Update** (PUT) — ürün güncelleme
2. **Product Delete** (DELETE) — ürün silme
3. **Shipment Providers** (GET) — kargo firma listesi
4. **Seller Addresses** (GET) — iade/gönderim adresleri
5. **Cancel Package** (PUT unsupplied) — paket iptal
6. **Webhook CRUD** (GET/PUT/DELETE) — listeleme, güncelleme, silme
7. **Current Account Statement** — cari hesap ekstre
8. **Claim Audit Information** — iade denetim bilgisi
9. **Q&A path düzeltmesi** — /product/ → /qna/
10. **Tracking Details** (PUT ayrı endpoint) — takip numarası güncelleme

## 10. Mevcut Adapter Metrikleri

| Metrik | Değer |
|--------|-------|
| Dosya | TrendyolAdapter.cs |
| Satır | 2098 |
| Method sayısı | 35+ |
| Interface | 8 (IIntegratorAdapter + 7 capability) |
| Rate limit | SemaphoreSlim(100) |
| Circuit breaker | Polly AdvancedCircuitBreaker |
| Retry | 5 attempt, exponential backoff |
| Webhook | Create + Process (CRUD eksik) |

## 11. Öncelik Sırası (Implementasyon)

| Öncelik | Endpoint | Neden |
|---------|----------|-------|
| P1 | Product Update (PUT) | Mevcut ürün güncelleme yapılamıyor |
| P1 | Cancel Package (unsupplied) | Sipariş iptal akışı eksik |
| P2 | Webhook CRUD | Webhook yönetimi |
| P2 | Q&A path düzeltme | Soru-cevap çalışmıyor |
| P2 | Shipment Providers | Kargo firma listesi |
| P3 | Product Delete | Ürün silme (arşivleme var) |
| P3 | Seller Addresses | İade adresi |
| P3 | Current Account | Cari hesap |
| P3 | Claim Audit | İade denetim |
| P3 | Tracking Details | Ayrı endpoint (mevcut PUT ile çalışıyor) |
