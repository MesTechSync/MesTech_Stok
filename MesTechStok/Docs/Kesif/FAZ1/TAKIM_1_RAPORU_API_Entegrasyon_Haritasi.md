# TAKIM 1 RAPORU: API & ENTEGRASYON HARITASI

**Kontrolor:** Claude Opus 4.6
**Tarih:** 06 Mart 2026
**Emirname Ref:** ENT-MD-001-T1
**Durum:** TAMAMLANDI

---

## PLATFORM 1: N11

### A. Kimlik Dogrulama (Auth)
- **Yontem:** SOAP Token (XML Header Authentication)
- **Gerekli credential'lar:** `appKey`, `appSecret`
- **Token yenileme:** YOK — her istekte appKey/appSecret gonderiliyor
- **Sandbox/test ortami:** README'DE YOK
- **Base URL:** README'DE YOK (SOAP Web Service URL belirtilmemis)

> **KANIT:** README satir 27-34:
> ```xml
> <soapenv:Header>
>     <aut:Authentication>
>         <aut:appKey>your-app-key</aut:appKey>
>         <aut:appSecret>your-app-secret</aut:appSecret>
>     </aut:Authentication>
> </soapenv:Header>
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** SOAP `ProductService` uzerinden — paging bilgisi README'DE YOK
- **Urun olusturma (POST):** `SaveProductRequest` ile SOAP call — zorunlu alanlar: `title`, `category.id`, `price`
- **Urun guncelleme (PUT/PATCH):** README'DE YOK (ayri endpoint belirtilmemis, SaveProduct upsert olabilir)
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — v1.1.0'da "Bulk operations" eklenmis, batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — `GetTopLevelCategoriesRequest`, `CategoryService` mevcut
- **Gorsel yukleme:** README'DE YOK

> **KANIT:** README satir 56-78: `SaveProductRequest` ornegi, `GetTopLevelCategoriesRequest` ornegi

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** VAR — "Stok senkronizasyonu" ozellik olarak listelenmis, endpoint detayi README'DE YOK
- **Toplu stok guncelleme:** README'DE YOK
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** README'DE YOK

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** `OrderService` uzerinden — filtre detaylari README'DE YOK
- **Siparis onaylama:** "Durum guncelleme" olarak belirtilmis, endpoint README'DE YOK
- **Siparis iptali:** "Iptal/iade islemleri" listelenmis, endpoint README'DE YOK
- **Kargo bildirimi:** `ShipmentService` mevcut — takip no formati README'DE YOK
- **Iade yonetimi:** "Iptal/iade islemleri" listelenmis, endpoint README'DE YOK

> **KANIT:** README satir 23-24: `OrderService`, `ShipmentService`; satir 46-48: siparis listesi, durum guncelleme, kargo takibi, iptal/iade

### E. Webhook / Bildirim
- **Webhook destegi:** README'DE YOK
- **Hangi olaylar:** README'DE YOK
- **Callback formati:** README'DE YOK

### F. Kisitlamalar & Limitler
- **Rate limit:** README'DE YOK
- **Batch size:** README'DE YOK (bulk operations var ama boyut belirtilmemis)
- **API versiyonu:** v1.2.0'da REST API destegi eklenmis, orijinal SOAP tabanli

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — `ProductService` + `SaveProductRequest` mevcut
- **UpdateStockAsync()** → KISMI — stok senkronizasyonu var ama endpoint detayi yok
- **UpdatePriceAsync()** → KISMI — "Fiyat guncelleme" listelenmis ama endpoint detayi yok
- **GetOrdersAsync()** → UYGUN — `OrderService` mevcut
- **GetCategoriesAsync()** → UYGUN — `GetTopLevelCategoriesRequest` ornekle gosterilmis
- **SendShipmentAsync()** → KISMI — `ShipmentService` var ama detay yok
- **HandleWebhookAsync()** → YOK — webhook destegi belirtilmemis

---

## PLATFORM 2: HEPSIBURADA

### A. Kimlik Dogrulama (Auth)
- **Yontem:** HTTP Basic Auth (Username/Password) + MerchantId
- **Gerekli credential'lar:** `Username`, `Password`, `MerchantId`
- **Token yenileme:** README'DE YOK
- **Sandbox/test ortami:** VAR — `https://mpop-sit.hepsiburada.com` (SIT ortami)
- **Base URL:** Production: README'DE YOK (sadece SIT verilmis), Sandbox: `https://mpop-sit.hepsiburada.com`

> **KANIT:** README satir 29-38:
> ```json
> {
>   "Hepsiburada": {
>     "Username": "your-username",
>     "Password": "your-password",
>     "MerchantId": "your-merchant-id",
>     "BaseUrl": "https://mpop-sit.hepsiburada.com",
>     "Version": "v1"
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Product API uzerinden — paging detayi README'DE YOK
- **Urun olusturma (POST):** `CreateProductAsync(product)` — zorunlu alanlar: `Title`, `CategoryId`, `Brand`, `Price`, `StockQuantity`
- **Urun guncelleme (PUT/PATCH):** README'DE YOK (ayri endpoint belirtilmemis)
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — "Toplu urun yukleme" ve v2.0.0'da "Bulk operations destegi", batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — `GetCategoriesAsync()` mevcut, Category API listelenmis
- **Gorsel yukleme:** Minimum 3 resim, HD kalite — endpoint detayi README'DE YOK

> **KANIT:** README satir 72-80: `CreateProductAsync` ornegi; satir 69: `GetCategoriesAsync()`; satir 44-48: toplu urun yukleme, kategori eslestirme, variant yonetimi, marka onayi, resim optimizasyonu

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** VAR — `UpdateStockAsync(HepsiburadaSku, StockQuantity)` acik ornekle gosterilmis
- **Toplu stok guncelleme:** README'DE YOK (tek tek foreach dongusu ornekte)
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** Gunluk 4 kez (08:00, 12:00, 16:00, 20:00 is akisi)

> **KANIT:** README satir 86-99: `SyncInventoryAsync` foreach ornegi; satir 94: `UpdateStockAsync(product.HepsiburadaSku, product.StockQuantity)`

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** `GetOrdersAsync(DateTime)` — tarih filtresi var
- **Siparis onaylama:** "Otomatik faturalama" olarak belirtilmis, endpoint README'DE YOK
- **Siparis iptali:** "Iade/iptal islemleri" listelenmis, endpoint README'DE YOK
- **Kargo bildirimi:** HepsiJet entegrasyonu — endpoint README'DE YOK
- **Iade yonetimi:** "Iade/iptal islemleri" listelenmis, endpoint README'DE YOK

> **KANIT:** README satir 83: `GetOrdersAsync(DateTime.Today.AddDays(-1))`; satir 53: HepsiJet entegrasyonu

### E. Webhook / Bildirim
- **Webhook destegi:** README'DE YOK
- **Hangi olaylar:** README'DE YOK
- **Callback formati:** README'DE YOK

### F. Kisitlamalar & Limitler
- **Rate limit:** README'DE YOK
- **Batch size:** README'DE YOK
- **API versiyonu:** v1

> **KANIT:** README satir 36: `"Version": "v1"`

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — `CreateProductAsync` + toplu yukleme mevcut
- **UpdateStockAsync()** → UYGUN — `UpdateStockAsync(sku, qty)` ornek kodda gosterilmis
- **UpdatePriceAsync()** → KISMI — fiyat yonetimi listelenmis ama ayri endpoint yok
- **GetOrdersAsync()** → UYGUN — `GetOrdersAsync(DateTime)` ornekle gosterilmis
- **GetCategoriesAsync()** → UYGUN — `GetCategoriesAsync()` ornekle gosterilmis
- **SendShipmentAsync()** → KISMI — HepsiJet entegrasyonu var ama endpoint detayi yok
- **HandleWebhookAsync()** → YOK — webhook destegi belirtilmemis

---

## PLATFORM 3: AMAZON TR

### A. Kimlik Dogrulama (Auth)
- **Yontem:** OAuth2 (SP-API) — ClientId + ClientSecret + RefreshToken
- **Gerekli credential'lar:** `ClientId`, `ClientSecret`, `RefreshToken`, `MarketplaceId`
- **Token yenileme:** VAR — RefreshToken mekanizmasi (sure README'DE YOK)
- **Sandbox/test ortami:** README'DE YOK
- **Base URL:** README'DE YOK, MarketplaceId: `A33AVAJ2PDY3EV` (Turkiye)

> **KANIT:** README satir 27-35:
> ```json
> {
>   "Amazon": {
>     "ClientId": "your-client-id",
>     "ClientSecret": "your-client-secret",
>     "RefreshToken": "your-refresh-token",
>     "MarketplaceId": "A33AVAJ2PDY3EV"
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Products API — `GetProductsAsync()` — paging detayi README'DE YOK
- **Urun olusturma (POST):** README'DE YOK (ayri create endpoint belirtilmemis)
- **Urun guncelleme (PUT/PATCH):** README'DE YOK
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — v1.5.0'da "Bulk islem destegi", batch boyutu README'DE YOK
- **Kategori eslestirme:** "Kategori yonetimi" listelenmis — endpoint detayi README'DE YOK
- **Gorsel yukleme:** README'DE YOK

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** VAR — Inventory API, "Stok senkronizasyonu" listelenmis — endpoint detayi README'DE YOK
- **Toplu stok guncelleme:** README'DE YOK
- **Stok sorgulama:** VAR — "Stok raporlari" Reports API uzerinden
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** Stok guncelleme gunde 4 kez, siparis sync her 15 dakika

> **KANIT:** README satir 67-69: "Siparis senkronizasyonu: Her 15 dakika", "Stok guncelleme: Gunde 4 kez", "Fiyat guncelleme: Gunde 2 kez"

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** Orders API — `GetOrdersAsync()` — filtre detayi README'DE YOK
- **Siparis onaylama:** "Durum guncelleme" listelenmis, endpoint README'DE YOK
- **Siparis iptali:** README'DE YOK
- **Kargo bildirimi:** "Kargo takibi" listelenmis — endpoint README'DE YOK
- **Iade yonetimi:** "Iade islemleri" listelenmis — endpoint README'DE YOK

### E. Webhook / Bildirim
- **Webhook destegi:** README'DE YOK
- **Hangi olaylar:** README'DE YOK
- **Callback formati:** README'DE YOK

### F. Kisitlamalar & Limitler
- **Rate limit:** Dakikada maksimum 100 istek, throttling durumunda otomatik bekleme
- **Batch size:** README'DE YOK
- **API versiyonu:** SP-API v2

> **KANIT:** README satir 86-87: "Dakikada maksimum 100 istek", "Throttling durumunda otomatik bekle"; satir 95: "v2.1.0: SP-API v2 destegi"

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — Products API + GetProductsAsync mevcut
- **UpdateStockAsync()** → KISMI — Inventory API var ama endpoint detayi yok
- **UpdatePriceAsync()** → KISMI — "Fiyat guncelleme" listelenmis ama endpoint yok
- **GetOrdersAsync()** → UYGUN — Orders API + GetOrdersAsync mevcut
- **GetCategoriesAsync()** → KISMI — kategori yonetimi var ama endpoint detayi yok
- **SendShipmentAsync()** → KISMI — kargo takibi var ama endpoint detayi yok
- **HandleWebhookAsync()** → YOK — webhook destegi belirtilmemis

---

## PLATFORM 4: EBAY

### A. Kimlik Dogrulama (Auth)
- **Yontem:** OAuth 2.0 (Authorization Code Grant)
- **Gerekli credential'lar:** `ClientId`, `ClientSecret`, `RedirectUri`, `Environment`, `Scopes`
- **Token yenileme:** VAR — OAuth 2.0 token flow (401 Unauthorized = "Token suresi dolmus")
- **Sandbox/test ortami:** VAR — `"Environment": "sandbox"` secenegi mevcut
- **Base URL:** README'DE YOK (ancak Environment parametresi production/sandbox secimi sagliyor)

> **KANIT:** README satir 30-43:
> ```json
> {
>   "eBay": {
>     "ClientId": "your-client-id",
>     "ClientSecret": "your-client-secret",
>     "RedirectUri": "your-redirect-uri",
>     "Environment": "production",
>     "Scopes": ["https://api.ebay.com/oauth/api_scope", ...]
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Browse API + Sell API — paging detayi README'DE YOK
- **Urun olusturma (POST):** `CreateListingAsync(listing)` — zorunlu alanlar: `Title`, `Description`, `Price`, `Currency`
- **Urun guncelleme (PUT/PATCH):** README'DE YOK (ayri update endpoint belirtilmemis)
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — "Bulk listing operations" listelenmis, batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — "Kategori secimi" SEO bolumunde belirtilmis, endpoint README'DE YOK
- **Gorsel yukleme:** "Image upload failures" sorun giderme'de belirtilmis — format/boyut detayi README'DE YOK

> **KANIT:** README satir 75-82: `CreateListingAsync` ornegi; satir 48-51: Listing Management ozellikleri

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** VAR — "Inventory sync" listelenmis — endpoint detayi README'DE YOK
- **Toplu stok guncelleme:** README'DE YOK
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** README'DE YOK

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** `GetOrdersAsync(DateTime)` — tarih filtresi var (son 7 gun ornegi)
- **Siparis onaylama:** README'DE YOK (ayri endpoint belirtilmemis)
- **Siparis iptali:** `OrderCancellation` webhook event'i mevcut — endpoint README'DE YOK
- **Kargo bildirimi:** "Fulfillment tracking" listelenmis — endpoint README'DE YOK
- **Iade yonetimi:** "Return/refund handling" listelenmis — endpoint README'DE YOK

> **KANIT:** README satir 85: `GetOrdersAsync(DateTime.Today.AddDays(-7))`; satir 53-57: Order Management ozellikleri

### E. Webhook / Bildirim
- **Webhook destegi:** VAR
- **Hangi olaylar:** `ItemSold`, `OrderCancellation`
- **Callback formati:** POST JSON

> **KANIT:** README satir 89-103: Webhook endpoint ornegi — `eBayNotification` model, `NotificationType` switch: "ItemSold", "OrderCancellation"

### F. Kisitlamalar & Limitler
- **Rate limit:** VAR — 429 Too Many Requests hatasi belirtilmis, spesifik limit README'DE YOK
- **Batch size:** README'DE YOK
- **API versiyonu:** REST APIs (Trading API legacy, REST modern) — spesifik versiyon README'DE YOK

> **KANIT:** README satir 174: "429 Too Many Requests: Rate limit"

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — CreateListingAsync + Sell API mevcut
- **UpdateStockAsync()** → KISMI — "Inventory sync" var ama endpoint detayi yok
- **UpdatePriceAsync()** → KISMI — "Pricing strategies" var ama endpoint detayi yok
- **GetOrdersAsync()** → UYGUN — GetOrdersAsync(DateTime) ornekle gosterilmis
- **GetCategoriesAsync()** → KISMI — kategori secimi var ama endpoint detayi yok
- **SendShipmentAsync()** → KISMI — "Fulfillment tracking" var ama endpoint detayi yok
- **HandleWebhookAsync()** → UYGUN — Webhook ornegi acikca gosterilmis (ItemSold, OrderCancellation)

---

## PLATFORM 5: CICEKSEPETI

### A. Kimlik Dogrulama (Auth)
- **Yontem:** API Key + Secret Key
- **Gerekli credential'lar:** `ApiKey`, `SecretKey`
- **Token yenileme:** README'DE YOK
- **Sandbox/test ortami:** README'DE YOK
- **Base URL:** `https://api.ciceksepeti.com`, Version: `v1`

> **KANIT:** README satir 27-35:
> ```json
> {
>   "Ciceksepeti": {
>     "ApiKey": "your-api-key",
>     "SecretKey": "your-secret-key",
>     "BaseUrl": "https://api.ciceksepeti.com",
>     "Version": "v1"
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Product API — `SyncProductsAsync()` — paging detayi README'DE YOK
- **Urun olusturma (POST):** "Urun ekleme/guncelleme" listelenmis — zorunlu alanlar README'DE YOK
- **Urun guncelleme (PUT/PATCH):** "Urun ekleme/guncelleme" birlikte listelenmis
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — "Toplu islem optimizasyonu" v1.2.0'da, batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — Category API + "Kategori ataması" listelenmis
- **Gorsel yukleme:** VAR — "Resim yukleme" listelenmis — format/boyut detayi README'DE YOK

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** VAR — Stock API, "Anlik stok guncelleme" listelenmis
- **Toplu stok guncelleme:** VAR — "Toplu stok islemleri" listelenmis, batch boyutu README'DE YOK
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** README'DE YOK (gunluk is akisi: 12:00 ogle stok guncellemesi)

> **KANIT:** README satir 52-55: "Anlik stok guncelleme", "Minimum stok uyarilari", "Toplu stok islemleri"

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** Order API — `GetOrdersAsync()` — filtre detayi README'DE YOK
- **Siparis onaylama:** "Otomatik onay" listelenmis — endpoint README'DE YOK
- **Siparis iptali:** "Iptal/iade islemleri" listelenmis — endpoint README'DE YOK
- **Kargo bildirimi:** "Kargo entegrasyonu" listelenmis — endpoint README'DE YOK
- **Iade yonetimi:** "Iptal/iade islemleri" listelenmis — endpoint README'DE YOK

> **KANIT:** README satir 47-50: gercek zamanli siparis, otomatik onay, kargo, iptal/iade

### E. Webhook / Bildirim
- **Webhook destegi:** VAR — v1.3.0'da "Webhook destegi eklendi"
- **Hangi olaylar:** Siparis webhook'u (OrderWebhookModel)
- **Callback formati:** POST JSON

> **KANIT:** README satir 67-73: `[HttpPost("webhook/order")]`, `OrderWebhookModel`; satir 114: "v1.3.0: Webhook destegi eklendi"

### F. Kisitlamalar & Limitler
- **Rate limit:** VAR — 429 Too Many Requests belirtilmis, spesifik limit README'DE YOK
- **Batch size:** README'DE YOK
- **API versiyonu:** v1

> **KANIT:** README satir 105: "429 Too Many Requests - Rate limit asildi"

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — `SyncProductsAsync()` ornekle gosterilmis
- **UpdateStockAsync()** → UYGUN — Stock API + anlik stok guncelleme + toplu stok
- **UpdatePriceAsync()** → KISMI — "Fiyat yonetimi" listelenmis ama endpoint detayi yok
- **GetOrdersAsync()** → UYGUN — `GetOrdersAsync()` ornekle gosterilmis
- **GetCategoriesAsync()** → KISMI — Category API var ama endpoint detayi yok
- **SendShipmentAsync()** → KISMI — kargo entegrasyonu var ama endpoint detayi yok
- **HandleWebhookAsync()** → UYGUN — Webhook ornegi acikca gosterilmis

---

## PLATFORM 6: OZON

### A. Kimlik Dogrulama (Auth)
- **Yontem:** API Key + Client ID (Header-based)
- **Gerekli credential'lar:** `ClientId`, `ApiKey`
- **Token yenileme:** YOK — her istekte ClientId + ApiKey gonderiliyor
- **Sandbox/test ortami:** VAR — "Test environment'i yapilandirin" belirtilmis, URL README'DE YOK
- **Base URL:** `https://api-seller.ozon.ru`, Version: `v2`

> **KANIT:** README satir 29-37:
> ```json
> {
>   "Ozon": {
>     "ClientId": "your-client-id",
>     "ApiKey": "your-api-key",
>     "BaseUrl": "https://api-seller.ozon.ru",
>     "Version": "v2"
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Products API — paging detayi README'DE YOK
- **Urun olusturma (POST):** `CreateProductAsync(product)` — zorunlu alanlar: `Name`, `NameRu`, `CategoryId`, `Price`, `CurrencyCode`
- **Urun guncelleme (PUT/PATCH):** README'DE YOK
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — v1.2.0'da "Bulk operations", batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — `CategoryId` zorunlu alan, "Kategori eslestirme" listelenmis
- **Gorsel yukleme:** README'DE YOK

> **KANIT:** README satir 60-69: `CreateProductAsync` ornegi — OzonProduct modeli

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** README'DE YOK (ayri stok API'si listelenmemis, Products API altinda olabilir)
- **Toplu stok guncelleme:** README'DE YOK
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** VAR — FBO (Fulfillment by Ozon) + FBS (Fulfillment by Seller)
- **Minimum sync sikligi:** README'DE YOK

> **KANIT:** README satir 82-83: "FBO (Fulfillment by Ozon)", "FBS (Fulfillment by Seller)"

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** Orders API — `GetOrdersAsync(OrderFilter)` — filtreler: `Since` (tarih), `Status` (durum)
- **Siparis onaylama:** README'DE YOK
- **Siparis iptali:** README'DE YOK
- **Kargo bildirimi:** "Kargo secenekleri" listelenmis — endpoint README'DE YOK
- **Iade yonetimi:** README'DE YOK

> **KANIT:** README satir 72-76: `GetOrdersAsync(new OrderFilter { Since = ..., Status = "awaiting_packaging" })`

### E. Webhook / Bildirim
- **Webhook destegi:** README'DE YOK
- **Hangi olaylar:** README'DE YOK
- **Callback formati:** README'DE YOK

### F. Kisitlamalar & Limitler
- **Rate limit:** VAR — 429 Rate Limit belirtilmis, spesifik limit README'DE YOK
- **Batch size:** README'DE YOK
- **API versiyonu:** v2

> **KANIT:** README satir 120: "429 Rate Limit: Istek sikligini azaltin"; satir 35: `"Version": "v2"`

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — Products API + CreateProductAsync mevcut
- **UpdateStockAsync()** → YOK — ayri stok API'si/endpoint'i belirtilmemis
- **UpdatePriceAsync()** → KISMI — "Fiyat stratejileri" listelenmis ama endpoint yok
- **GetOrdersAsync()** → UYGUN — GetOrdersAsync(OrderFilter) ornekle gosterilmis
- **GetCategoriesAsync()** → KISMI — CategoryId zorunlu ama kategori listesi endpoint'i yok
- **SendShipmentAsync()** → KISMI — FBO/FBS var ama kargo bildirimi endpoint'i yok
- **HandleWebhookAsync()** → YOK — webhook destegi belirtilmemis

---

## PLATFORM 7: PAZARAMA

### A. Kimlik Dogrulama (Auth)
- **Yontem:** API Key + Secret Key + Seller ID
- **Gerekli credential'lar:** `ApiKey`, `SecretKey`, `SellerId`
- **Token yenileme:** README'DE YOK
- **Sandbox/test ortami:** VAR — "Test ortaminda dogrulama yapin" belirtilmis, URL README'DE YOK
- **Base URL:** `https://isg.pazarama.com/api`

> **KANIT:** README satir 28-36:
> ```json
> {
>   "Pazarama": {
>     "ApiKey": "your-api-key",
>     "SecretKey": "your-secret-key",
>     "BaseUrl": "https://isg.pazarama.com/api",
>     "SellerId": "your-seller-id"
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Product Management API — paging detayi README'DE YOK
- **Urun olusturma (POST):** `CreateProductAsync(product)` — zorunlu alanlar: `Title`, `Description`, `CategoryId`, `Price`, `Stock`
- **Urun guncelleme (PUT/PATCH):** README'DE YOK
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — v1.1.0'da "Bulk operations destegi", batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — Category API listelenmis, `CategoryId` zorunlu alan
- **Gorsel yukleme:** "Gorsel optimizasyonu" listelenmis — endpoint/format README'DE YOK

> **KANIT:** README satir 57-68: `CreateProductAsync` ornegi — PazaramaProduct modeli

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** VAR — Inventory API listelenmis — endpoint detayi README'DE YOK
- **Toplu stok guncelleme:** README'DE YOK
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** README'DE YOK

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** Order Management API — `GetOrdersAsync(DateTime)` — tarih filtresi var
- **Siparis onaylama:** "Otomatik onay sistemi" listelenmis — endpoint README'DE YOK
- **Siparis iptali:** README'DE YOK
- **Kargo bildirimi:** "Kargo entegrasyonu" listelenmis — endpoint README'DE YOK
- **Iade yonetimi:** "Iade islemleri" listelenmis — endpoint README'DE YOK

> **KANIT:** README satir 71: `GetOrdersAsync(DateTime.Today.AddDays(-1))`; satir 47-51: siparis ozellikleri

### E. Webhook / Bildirim
- **Webhook destegi:** README'DE YOK
- **Hangi olaylar:** README'DE YOK
- **Callback formati:** README'DE YOK

### F. Kisitlamalar & Limitler
- **Rate limit:** README'DE YOK
- **Batch size:** README'DE YOK
- **API versiyonu:** README'DE YOK

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — Product Management API + CreateProductAsync mevcut
- **UpdateStockAsync()** → KISMI — Inventory API var ama endpoint detayi yok
- **UpdatePriceAsync()** → KISMI — "Fiyat yonetimi" listelenmis ama endpoint yok
- **GetOrdersAsync()** → UYGUN — GetOrdersAsync(DateTime) ornekle gosterilmis
- **GetCategoriesAsync()** → KISMI — Category API var ama endpoint detayi yok
- **SendShipmentAsync()** → KISMI — kargo entegrasyonu var ama endpoint detayi yok
- **HandleWebhookAsync()** → YOK — webhook destegi belirtilmemis

---

## PLATFORM 8: PTTAVM

### A. Kimlik Dogrulama (Auth)
- **Yontem:** API Key + Merchant Code (+ ayri PTT Kargo Token)
- **Gerekli credential'lar:** `MerchantCode`, `ApiKey`, `PttCargoToken`
- **Token yenileme:** README'DE YOK
- **Sandbox/test ortami:** README'DE YOK
- **Base URL:** `https://api.pttavm.com`

> **KANIT:** README satir 28-36:
> ```json
> {
>   "PttAvm": {
>     "MerchantCode": "your-merchant-code",
>     "ApiKey": "your-api-key",
>     "BaseUrl": "https://api.pttavm.com",
>     "PttCargoToken": "cargo-api-token"
>   }
> }
> ```

### B. Urun API'leri (Product)
- **Urun listeleme (GET):** Product API — paging detayi README'DE YOK
- **Urun olusturma (POST):** `CreateProductAsync(product)` — zorunlu alanlar: `ProductName`, `CategoryCode`, `Price`, `CargoPrice`, `DeliveryTime`
- **Urun guncelleme (PUT/PATCH):** README'DE YOK
- **Urun silme/pasif (DELETE):** README'DE YOK
- **Toplu (batch) islem:** VAR — v1.1.0'da "Bulk operations", batch boyutu README'DE YOK
- **Kategori eslestirme:** VAR — `CategoryCode` zorunlu alan, "PTT AVM kategori yapisi" belirtilmis
- **Gorsel yukleme:** README'DE YOK

> **KANIT:** README satir 57-68: `CreateProductAsync` ornegi — PttAvmProduct modeli

### C. Stok API'leri (Inventory)
- **Stok guncelleme:** README'DE YOK (ayri Inventory/Stock API listelenmemis)
- **Toplu stok guncelleme:** README'DE YOK
- **Stok sorgulama:** README'DE YOK
- **Coklu depo destegi:** README'DE YOK
- **Minimum sync sikligi:** README'DE YOK

### D. Siparis API'leri (Order)
- **Siparis listeleme (GET):** Order API — endpoint detayi README'DE YOK
- **Siparis onaylama:** README'DE YOK
- **Siparis iptali:** README'DE YOK
- **Kargo bildirimi:** VAR — `CreateShipmentAsync(order)` + `TrackShipmentAsync(trackingNumber)` acikca orneklenmis
- **Iade yonetimi:** README'DE YOK

> **KANIT:** README satir 71: `TrackShipmentAsync(trackingNumber)`; satir 76-88: `CreateShipmentAsync` ornegi — PttCargoRequest modeli

### E. Webhook / Bildirim
- **Webhook destegi:** README'DE YOK
- **Hangi olaylar:** README'DE YOK
- **Callback formati:** README'DE YOK

### F. Kisitlamalar & Limitler
- **Rate limit:** README'DE YOK
- **Batch size:** README'DE YOK
- **API versiyonu:** README'DE YOK

### G. IIntegratorAdapter Uyum Degerlendirmesi
- **SyncProductsAsync()** → UYGUN — Product API + CreateProductAsync mevcut
- **UpdateStockAsync()** → YOK — ayri stok API'si/endpoint'i belirtilmemis
- **UpdatePriceAsync()** → YOK — fiyat guncelleme endpoint'i belirtilmemis
- **GetOrdersAsync()** → KISMI — Order API var ama ornek kod/detay yok
- **GetCategoriesAsync()** → KISMI — CategoryCode zorunlu ama kategori listesi endpoint'i yok
- **SendShipmentAsync()** → UYGUN — CreateShipmentAsync + TrackShipmentAsync ornekle gosterilmis
- **HandleWebhookAsync()** → YOK — webhook destegi belirtilmemis

---

## CAPRAZ KARSILASTIRMA MATRISI

### TABLO 1: CAPRAZ API KARSILASTIRMA MATRISI

| Ozellik | Trendyol | N11 | HB | Amazon | eBay | Ciceksepeti | Ozon | Pazarama | PttAVM | OpenCart |
|---------|----------|-----|----|--------|------|-------------|------|----------|--------|---------|
| Auth tipi | API Key | SOAP Token | Basic Auth | OAuth2 (SP-API) | OAuth 2.0 | API Key | API Key+ClientId | API Key | API Key+MerchantCode | REST API |
| Base URL | api.trendyol.com | README'DE YOK | mpop-sit.hepsiburada.com | README'DE YOK | README'DE YOK | api.ciceksepeti.com | api-seller.ozon.ru | isg.pazarama.com/api | api.pttavm.com | - |
| Urun GET | ✅ | ✅ SOAP | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Urun POST | ✅ | ✅ SOAP | ✅ | ❓ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Urun batch | 100'lu | ✅ boyut? | ✅ boyut? | ✅ boyut? | ✅ boyut? | ✅ boyut? | ✅ boyut? | ✅ boyut? | ✅ boyut? | - |
| Stok update | ✅ | ✅ detay yok | ✅ ornekli | ✅ detay yok | ✅ detay yok | ✅ toplu | ❓ detay yok | ✅ detay yok | ❌ | ✅ |
| Stok batch | ✅ | ❓ | ❓ | ❓ | ❓ | ✅ | ❓ | ❓ | ❌ | - |
| Siparis GET | ✅ | ✅ | ✅ tarih filtre | ✅ | ✅ tarih filtre | ✅ | ✅ tarih+status | ✅ tarih filtre | ✅ detay yok | ✅ |
| Kargo bildir | ✅ | ✅ ShipmentSvc | ❓ HepsiJet | ❓ | ❓ fulfillment | ❓ | ❓ FBO/FBS | ❓ | ✅ ornekli | - |
| Webhook | ✅ | ❌ | ❌ | ❌ | ✅ (ItemSold, Cancel) | ✅ (Order) | ❌ | ❌ | ❌ | - |
| Rate limit | ? | ❓ | ❓ | 100/dk | ✅ (429) | ✅ (429) | ✅ (429) | ❓ | ❓ | - |
| Kategori agaci | ✅ | ✅ ornekli | ✅ ornekli | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Coklu depo | ❌ | ❓ | ❓ | ❓ | ❓ | ❓ | ✅ FBO/FBS | ❓ | ❓ | - |
| Iade yonetimi | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ❓ | ✅ | ❓ | - |

**Aciklama:** ✅ = Mevcut, ❌ = Yok, ❓ = README'de detay yok ama muhtemel, - = Kapsam disi

### TABLO 2: IIntegratorAdapter UYUM MATRISI

| Metod | Trendyol | N11 | HB | Amazon | eBay | Ciceksepeti | Ozon | Pazarama | PttAVM | OpenCart |
|-------|----------|-----|----|--------|------|-------------|------|----------|--------|---------|
| SyncProducts | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM |
| UpdateStock | ✅ TAM | ⚠️ KISMI | ✅ TAM | ⚠️ KISMI | ⚠️ KISMI | ✅ TAM | ❌ YOK | ⚠️ KISMI | ❌ YOK | ✅ TAM |
| UpdatePrice | ✅ TAM | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ❌ YOK | ✅ TAM |
| GetOrders | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ✅ TAM | ⚠️ KISMI | ✅ TAM |
| GetCategories | ✅ TAM | ✅ TAM | ✅ TAM | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ✅ TAM |
| SendShipment | ✅ TAM | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ⚠️ KISMI | ✅ TAM | ❌ YOK |
| HandleWebhook | ✅ TAM | ❌ YOK | ❌ YOK | ❌ YOK | ✅ TAM | ✅ TAM | ❌ YOK | ❌ YOK | ❌ YOK | ❌ YOK |

**Aciklama:** ✅ TAM = Ornekle kanitli, ⚠️ KISMI = Ozellik var ama endpoint/ornek detayi yok, ❌ YOK = README'de hic belirtilmemis

### TABLO 3: SON ONERI — IIntegratorAdapter Interface Tasarimi

**1. TUM platformlarda ORTAK olan metodlar → interface'e GIRER (zorunlu):**

| Metod | Kapsam | Aciklama |
|-------|--------|----------|
| `SyncProductsAsync()` | 10/10 platform | Tum platformlarda urun API mevcut |
| `GetOrdersAsync()` | 10/10 platform | Tum platformlarda siparis API mevcut |
| `GetCategoriesAsync()` | 10/10 platform | Tum platformlarda kategori yapisi var |

**2. COGU platformda olan metodlar → interface'e GIRER ama opsiyonel flag ile:**

| Metod | Kapsam | Oneri |
|-------|--------|-------|
| `UpdateStockAsync()` | 8/10 (Ozon ve PttAVM haric) | Interface'de `SupportsStockUpdate` flag'i ile |
| `UpdatePriceAsync()` | 9/10 (PttAVM haric) | Interface'de `SupportsPriceUpdate` flag'i ile |
| `SendShipmentAsync()` | 9/10 (OpenCart haric) | Interface'de `SupportsShipment` flag'i ile |

**3. Sadece BAZI platformlarda olan metodlar → opsiyonel interface (IWebhookAdapter):**

| Metod | Kapsam | Oneri |
|-------|--------|-------|
| `HandleWebhookAsync()` | 3/10 (Trendyol, eBay, Ciceksepeti) | Ayri `IWebhookCapableAdapter` interface'i |

---

## KRITIK BULGULAR

### 1. README Derinlik Farki (YUKSEK ONCELIK)
Trendyol ve OpenCart README'leri diger 8 platformdan cok daha detayli. 8 yeni platformun README'leri genellikle:
- Endpoint URL'lerini vermiyor (sadece servis adi)
- Request/response format detaylarini icermiyor
- Paging, batch size, rate limit gibi operasyonel detaylar eksik
- **Oneri:** Her platform icin API dokumantasyonu incelenerek README'ler zenginlestirilmeli

### 2. Auth Cesitliligi (ORTA ONCELIK)
4 farkli auth yontemi tespit edildi:
- **SOAP Token:** N11 (appKey/appSecret XML header)
- **Basic Auth:** Hepsiburada (Username/Password)
- **OAuth 2.0:** Amazon (SP-API), eBay (Authorization Code Grant)
- **API Key:** Ciceksepeti, Ozon, Pazarama, PttAVM (header-based key/secret)
- **Oneri:** IIntegratorAdapter icinde `IAuthenticationProvider` abstraction'i ile her platform kendi auth stratejisini implement etmeli

### 3. Protokol Farki: SOAP vs REST (ORTA ONCELIK)
- **SOAP:** N11 (v1.2.0'da REST eklenmis ama orijinal SOAP)
- **REST:** Diger 7 platform
- **Oneri:** N11 adapter'i icinde SOAP-to-REST translation layer veya ayri SOAP client wrapper gerekli

### 4. Webhook Destegi Zayif (ORTA ONCELIK)
Sadece 3 platform (Trendyol, eBay, Ciceksepeti) webhook destegi belirtmis. Diger 7 platform icin:
- **Oneri:** Polling-based sync mekanizmasi varsayilan strateji olmali
- Hangfire recurring job'lar tum platformlar icin planlanmali

### 5. Stok API Eksikligi (YUKSEK ONCELIK)
Ozon ve PttAVM'de ayri stok guncelleme API'si/endpoint'i README'de belirtilmemis.
- **Oneri:** Bu 2 platformun resmi API dokumantasyonu incelenmeli — stok guncelleme muhtemelen Product API altinda olabilir

### 6. Coklu Depo Sadece Ozon (DUSUK ONCELIK)
Sadece Ozon FBO/FBS ile coklu depo destegi gosteriyor.
- **Oneri:** `IMultiWarehouseAdapter` interface'i suan icin sadece Ozon adapter'inda implement edilecek, gelecekte genisletilebilir

### 7. Uluslararasi Platform Farklilik (ORTA ONCELIK)
- **eBay:** Multi-currency, global shipping, cross-border trade
- **Ozon:** Rusca dil zorunlulugu, RUB para birimi, data localization
- **Oneri:** `ICurrencyConverter` ve `ILocalizationProvider` interface'leri gerekli

---

## ONERILER

### 1. Interface Tasarimi
```
IIntegratorAdapter (zorunlu)
├── SyncProductsAsync()
├── GetOrdersAsync()
├── GetCategoriesAsync()
├── UpdateStockAsync()     // SupportsStockUpdate flag
├── UpdatePriceAsync()     // SupportsPriceUpdate flag
└── SendShipmentAsync()    // SupportsShipment flag

IWebhookCapableAdapter (opsiyonel)
└── HandleWebhookAsync()

IMultiWarehouseAdapter (opsiyonel)
└── GetWarehousesAsync()
└── UpdateWarehouseStockAsync()

IAuthenticationProvider (zorunlu, her platform kendi impl)
├── AuthenticateAsync()
└── RefreshTokenAsync()    // OAuth2 platformlari icin
```

### 2. Oncelik Sirasi (Gelistirme)
1. **Ciceksepeti** — En fazla ornek kod, webhook var, API Key auth (basit)
2. **Hepsiburada** — Stok guncelleme ornegi var, Turkiye'nin en buyuk pazaryeri
3. **N11** — SOAP ornegi var, kategori ornegi var (SOAP wrapper gerekli)
4. **Pazarama** — API Key auth, standart REST, basit yapi
5. **Amazon TR** — OAuth2 karmasikligi, SP-API, rate limit belirli
6. **eBay** — OAuth2, webhook var, uluslararasi karmasiklik
7. **PttAVM** — Kargo entegrasyonu guclu, stok API eksik
8. **Ozon** — Uluslararasi, dil/para birimi karmasikligi, stok API eksik

### 3. Eksik Bilgi Tamamlama
Her 8 platform icin resmi API dokumantasyonu incelenerek su bilgiler tamamlanmali:
- Tam endpoint URL'leri (path + HTTP method)
- Request/response JSON/XML sema'lari
- Paging mekanizmasi (offset/cursor/page)
- Kesin rate limit degerleri
- Batch size limitleri
- Hata kodlari ve retry stratejileri

### 4. Polling Stratejisi
Webhook destegi olmayan 7 platform icin Hangfire recurring job plani:
- Siparis sync: Her 5-15 dakika
- Stok sync: Her 30 dakika - 1 saat
- Fiyat sync: Gunde 2-4 kez
- Kategori sync: Gunde 1 kez

---

**RAPOR SONU — TAKIM 1**
