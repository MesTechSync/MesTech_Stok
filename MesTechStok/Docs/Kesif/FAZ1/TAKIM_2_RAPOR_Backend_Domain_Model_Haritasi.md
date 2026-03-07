# TAKIM 2 RAPORU: BACKEND & DOMAIN MODEL HARITASI

**Kontrolor:** Claude Opus 4.6
**Tarih:** 06 Mart 2026
**Emirname Ref:** ENT-MD-001-T2

---

## KRITIK ON NOT

README dosyalarinin buyuk cogunlugu **genel kurulum/kullanim kilavuzu** niteliginde olup, emirnamede talep edilen **detayli entity alan tanimlari** (alan adi, tip, zorunlu/opsiyonel) icermemektedir. Asagidaki analiz, README'lerde yer alan **kod ornekleri, config yapilari ve API aciklamalarindan** cikarilabilecek MAKSIMUM veri modeli bilgisini icermektedir. "README'DE YOK" ifadesi sikca kullanilacaktir.

---

## PLATFORM 1: N11

**Kaynak:** `MesTech_N11/Docs/README.md`

### A. Urun Modeli (Product)

README'deki kod orneginden cikarilan alanlar:

| Alan | Tip | Zorunlu | Eslesme |
|------|-----|---------|---------|
| title | string | Evet | Bizim Name alanina eslesir |
| category.id | int | Evet | Bizim CategoryId FK'ya eslesir |
| price | decimal | Evet | Bizim SalePrice'a eslesir |

**Kanit (satir 70-78):**
```csharp
product = new Product
{
    title = "Urun Basligi",
    category = new Category { id = 12345 },
    price = 99.99m
};
```

- SKU: README'DE YOK
- Barcode: README'DE YOK
- Description: README'DE YOK (ama beklenir)
- Stock: README'DE YOK (ama "Stok senkronizasyonu" ozellik olarak belirtilmis - satir 43)
- Weight/Dimensions: README'DE YOK
- TaxRate: README'DE YOK
- Images: README'DE YOK

### B. Siparis Modeli (Order)

- Siparis alanlari: README'DE YOK
- Durum akisi: README'DE YOK (sadece "Durum guncelleme" ozellik olarak belirtilmis - satir 47)
- Kargo bilgileri: README'DE YOK (sadece "Kargo takibi" ozellik olarak belirtilmis - satir 48)
- Iade modeli: README'DE YOK (sadece "Iptal/iade islemleri" ozellik olarak belirtilmis - satir 49)

**Bilinen siparis islemleri (satir 44-48):** Siparis listesi alma, Durum guncelleme, Kargo takibi, Iptal/iade islemleri

### C. Kategori & Marka Modeli

- **Kategori yapisi:** Agac (parent-child) - `GetTopLevelCategoriesRequest` (satir 57) ifadesi top-level kategorilerin var oldugunu, dolayisiyla hiyerarsik yapida oldugunu gosterir
- **Kategori ID:** int tipinde, zorunlu (satir 74: `category = new Category { id = 12345 }`)
- **Marka eslestirme:** README'DE YOK
- **Attribute/ozellik yapisi:** README'DE YOK
- **Varyant yonetimi:** README'DE YOK

### D. Musteri / Alici Modeli

README'DE YOK — hicbir alan bilgisi mevcut degil.

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| appKey | string | API erisim anahtari |
| appSecret | string | API gizli anahtar |

**Kanit (satir 27-34):** SOAP Header Authentication yapisi
- **Satici ID:** Ayri bir SellerId/MerchantId README'DE YOK
- **Coklu magaza destegi:** README'DE YOK
- **Auth tipi:** SOAP Header (appKey + appSecret)

---

## PLATFORM 2: HEPSIBURADA

**Kaynak:** `MesTech_Hepsiburada/Docs/README.md`

### A. Urun Modeli (Product)

README'deki kod orneginden cikarilan alanlar:

| Alan | Tip | Zorunlu | Eslesme |
|------|-----|---------|---------|
| Title | string | Evet | Bizim Name |
| CategoryId | string | Evet | Bizim CategoryId (DIKKAT: string, FK degil!) |
| Brand | string | Evet | Bizde yok - Marka alani |
| Price | decimal | Evet | Bizim SalePrice |
| StockQuantity | int | Evet | Bizim Stock |
| HepsiburadaSku | string | Evet | Bizim SKU (satir 95'te referans) |

**Kanit (satir 72-80):**
```csharp
var product = new HepsiburadaProduct
{
    Title = "Urun Basligi",
    CategoryId = "12345",
    Brand = "Marka Adi",
    Price = 99.99m,
    StockQuantity = 100
};
```

- Barcode: README'DE YOK
- Description: README'DE YOK
- Weight/Dimensions: README'DE YOK
- TaxRate: README'DE YOK
- Images: README'DE YOK (ama "Resim optimizasyonu" ozellik - satir 48, "Minimum 3 urun resmi" - satir 125)

**Onemli Bulgular:**
- Brand alani zorunlu ve ayri bir onay sureci var (satir 47: "Marka onayi sureci")
- Variant yonetimi mevcut (satir 46)
- CategoryId string tipinde (int veya Guid degil!)
- HepsiburadaSku platformda ayri SKU (satir 95)

### B. Siparis Modeli (Order)

- Siparis alanlari: README'DE YOK
- Durum akisi: README'DE YOK
- Kargo: "HepsiJet" entegrasyonu var (satir 53)
- Iade: "Iade/iptal islemleri" mevcut (satir 54)
- Fatura: "Otomatik faturalama" ozellik olarak belirtilmis (satir 52)

### C. Kategori & Marka Modeli

- **Kategori yapisi:** README'DE YOK (ama "Kategori eslestirme" ozellik - satir 45)
- **Kategori ID:** string tipinde (satir 75: `CategoryId = "12345"`)
- **Marka eslestirme:** BrandName (string) — ayri onay sureci var (satir 47, 120)
- **Attribute/ozellik yapisi:** README'DE YOK ("Teknik ozellikler" gerekli - satir 128)
- **Varyant yonetimi:** Var (satir 46) — detay README'DE YOK

### D. Musteri / Alici Modeli

README'DE YOK

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| Username | string | Kullanici adi |
| Password | string | Sifre |
| MerchantId | string | Satici kimlik numarasi |
| BaseUrl | string | API base URL |
| Version | string | API versiyon |

**Kanit (satir 30-38):**
```json
{
  "Username": "your-username",
  "Password": "your-password",
  "MerchantId": "your-merchant-id"
}
```

- **Satici ID Adi:** MerchantId
- **Coklu magaza destegi:** README'DE YOK
- **Auth tipi:** Username + Password + MerchantId (REST)

---

## PLATFORM 3: AMAZON TR

**Kaynak:** `MesTech_Amazon_tr/Docs/README.md`

### A. Urun Modeli (Product)

README'DE DETAYLI MODEL YOK — sadece `GetProductsAsync()` metod cagrisi mevcut (satir 63).

Bilinen ozellikler: Urun listeleme, Fiyat guncelleme, Stok senkronizasyonu, Kategori yonetimi (satir 41-44)

### B. Siparis Modeli (Order)

README'DE DETAYLI MODEL YOK — sadece `GetOrdersAsync()` metod cagrisi mevcut (satir 62).

Bilinen ozellikler: Otomatik siparis alma, Durum guncelleme, Kargo takibi, Iade islemleri (satir 47-50)

### C. Kategori & Marka Modeli

README'DE YOK

### D. Musteri / Alici Modeli

README'DE YOK

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| ClientId | string | OAuth Client ID |
| ClientSecret | string | OAuth Client Secret |
| RefreshToken | string | OAuth Refresh Token |
| MarketplaceId | string | Pazaryeri kimlik (TR: A33AVAJ2PDY3EV) |

**Kanit (satir 28-35):**
```json
{
  "ClientId": "your-client-id",
  "ClientSecret": "your-client-secret",
  "RefreshToken": "your-refresh-token",
  "MarketplaceId": "A33AVAJ2PDY3EV"
}
```

- **Satici ID Adi:** MarketplaceId uzerinden tanimlanir (SellerId ayri belirtilmemis)
- **Auth tipi:** OAuth 2.0 (ClientId + ClientSecret + RefreshToken)
- **API tipi:** SP-API (Selling Partner API) — eski MWS'den gecis (satir 20, 96)

---

## PLATFORM 4: EBAY

**Kaynak:** `MesTech_Ebay/Docs/README.md`

### A. Urun Modeli (Product)

README'deki kod orneginden cikarilan alanlar:

| Alan | Tip | Zorunlu | Eslesme |
|------|-----|---------|---------|
| Title | string | Evet | Bizim Name |
| Description | string | Evet | Bizim Description |
| Price | decimal | Evet | Bizim SalePrice |
| Currency | string | Evet | Bizde yok - para birimi |

**Kanit (satir 75-81):**
```csharp
var listing = new ItemListing
{
    Title = "Product Title",
    Description = "Product Description",
    Price = 29.99m,
    Currency = "USD"
};
```

**Onemli Bulgular:**
- **Currency (para birimi)** alani var — multi-currency destek (satir 60-63)
- Listing tipi: Fixed price / Auction (satir 48)
- **Uluslararasi ticaret** ozellikleri: Cross-border trade, Global shipping (satir 60-63)

- SKU: README'DE YOK
- Barcode: README'DE YOK
- Stock: README'DE YOK
- Weight/Dimensions: README'DE YOK
- TaxRate: README'DE YOK
- CategoryId: README'DE YOK
- Brand: README'DE YOK
- Images: README'DE YOK

### B. Siparis Modeli (Order)

- Siparis alanlari: README'DE YOK
- Durum akisi: Webhook'tan cikarilan durumlar (satir 93-103):
  - `ItemSold` — Urun satildi
  - `OrderCancellation` — Siparis iptali
- Kargo: "Fulfillment tracking" mevcut (satir 55)
- Iade: "Return/refund handling" mevcut (satir 56)

### C. Kategori & Marka Modeli

- **Kategori yapisi:** README'DE YOK (ama "Kategori secimi" SEO best practice olarak belirtilmis - satir 111)
- **Marka:** README'DE YOK
- **Attribute:** README'DE YOK
- **Varyant:** README'DE YOK

### D. Musteri / Alici Modeli

- "Customer communication" ozellik olarak mevcut (satir 57)
- "Message management", "Feedback system" var (satir 121-122)
- Detayli alan bilgisi: README'DE YOK

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| ClientId | string | OAuth Client ID |
| ClientSecret | string | OAuth Client Secret |
| RedirectUri | string | OAuth Redirect URI |
| Environment | string | "production" veya "sandbox" |
| Scopes | string[] | OAuth scope listesi |

**Kanit (satir 30-43):**
- **Auth tipi:** OAuth 2.0 (en detayli auth yapisindan biri)
- **Coklu ortam:** Production/Sandbox switch destegi
- **Satici ID:** Ayri bir SellerId README'DE YOK

---

## PLATFORM 5: CICEKSEPETI

**Kaynak:** `MesTech_Ciceksepeti/Docs/README.md`

### A. Urun Modeli (Product)

README'DE DETAYLI MODEL YOK — sadece service cagrilari mevcut (satir 62-63).

Bilinen ozellikler: Urun ekleme/guncelleme, Kategori atamasi, Resim yukleme, Fiyat yonetimi (satir 41-44)

### B. Siparis Modeli (Order)

- Siparis alanlari: README'DE YOK
- **Webhook modeli mevcut** (satir 67-74): `OrderWebhookModel` — alan detaylari YOK
- Bilinen: Gercek zamanli siparis alma, Otomatik onay, Kargo entegrasyonu, Iptal/iade (satir 47-50)

### C. Kategori & Marka Modeli

README'DE YOK (sadece "Kategori atamasi" ozellik olarak belirtilmis - satir 42)

### D. Musteri / Alici Modeli

README'DE YOK

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| ApiKey | string | API erisim anahtari |
| SecretKey | string | API gizli anahtar |
| BaseUrl | string | API base URL |
| Version | string | API versiyon |

**Kanit (satir 28-35):**
- **Auth tipi:** ApiKey + SecretKey (REST)
- **Satici ID:** Ayri bir SellerId/MerchantId README'DE YOK

---

## PLATFORM 6: OZON

**Kaynak:** `MesTech_Ozon/Docs/README.md`

### A. Urun Modeli (Product)

README'deki kod orneginden cikarilan alanlar:

| Alan | Tip | Zorunlu | Eslesme |
|------|-----|---------|---------|
| Name | string | Evet | Bizim Name |
| NameRu | string | Evet | Bizde yok - Rusca isim (lokalizasyon) |
| CategoryId | int | Evet | Bizim CategoryId |
| Price | string | Evet | Bizim SalePrice (DIKKAT: string tipinde!) |
| CurrencyCode | string | Evet | Bizde yok - para birimi |

**Kanit (satir 62-68):**
```csharp
var product = new OzonProduct
{
    Name = "Product Name",
    NameRu = "Nazvanie tovara",
    CategoryId = 12345,
    Price = "1500.00",
    CurrencyCode = "RUB"
};
```

**Onemli Bulgular:**
- **Coklu dil destegi** (Name + NameRu) — lokalizasyon gereksinimi
- **Price string tipinde** — diger platformlardan farkli!
- **CurrencyCode** — para birimi alani (RUB)
- **FBO/FBS** fulfillment modelleri var (satir 82-83)

### B. Siparis Modeli (Order)

Kod orneginden cikarilan bilgi (satir 72-76):
- **OrderFilter.Since**: DateTime — baslangic tarihi
- **OrderFilter.Status**: string — siparis durumu (ornek: "awaiting_packaging")

Bilinen durumlar: `awaiting_packaging` (tek ornek)

### C. Kategori & Marka Modeli

- **Kategori ID:** int tipinde (satir 65)
- **Kategori eslestirme:** Mevcut (satir 45)
- **Varyant yonetimi:** Var (satir 45)
- Detaylar: README'DE YOK

### D. Musteri / Alici Modeli

README'DE YOK

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| ClientId | string | Ozon Client ID |
| ApiKey | string | API anahtari |
| BaseUrl | string | API base URL |
| Version | string | API versiyon (v2) |

**Kanit (satir 30-37):**
- **Auth tipi:** ClientId + ApiKey (REST)
- **Satici ID:** ClientId uzerinden tanimlanir

---

## PLATFORM 7: PAZARAMA

**Kaynak:** `MesTech_Pazarama/Docs/README.md`

### A. Urun Modeli (Product)

README'deki kod orneginden cikarilan alanlar:

| Alan | Tip | Zorunlu | Eslesme |
|------|-----|---------|---------|
| Title | string | Evet | Bizim Name |
| Description | string | Evet | Bizim Description |
| CategoryId | string | Evet | Bizim CategoryId (DIKKAT: string!) |
| Price | decimal | Evet | Bizim SalePrice |
| Stock | int | Evet | Bizim Stock |

**Kanit (satir 60-67):**
```csharp
var product = new PazaramaProduct
{
    Title = "Urun Basligi",
    Description = "Urun aciklamasi",
    CategoryId = "12345",
    Price = 149.99m,
    Stock = 50
};
```

### B. Siparis Modeli (Order)

README'DE DETAYLI MODEL YOK — sadece `GetOrdersAsync(DateTime.Today.AddDays(-1))` (satir 71)

### C. Kategori & Marka Modeli

- **CategoryId:** string tipinde
- Detaylar: README'DE YOK

### D. Musteri / Alici Modeli

README'DE YOK

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| ApiKey | string | API erisim anahtari |
| SecretKey | string | API gizli anahtar |
| BaseUrl | string | API base URL |
| SellerId | string | Satici kimlik numarasi |

**Kanit (satir 28-36):**
- **Satici ID Adi:** SellerId (string)
- **Auth tipi:** ApiKey + SecretKey + SellerId (REST)
- **Onemli:** SellerId acikca tanimlanmis — Trendyol'un SupplierId'sine benzer yapi

---

## PLATFORM 8: PTTAVM

**Kaynak:** `MesTech_PttAVM/Docs/README.md`

### A. Urun Modeli (Product)

README'deki kod orneginden cikarilan alanlar:

| Alan | Tip | Zorunlu | Eslesme |
|------|-----|---------|---------|
| ProductName | string | Evet | Bizim Name |
| CategoryCode | string | Evet | Bizim CategoryId (DIKKAT: Code, ID degil!) |
| Price | decimal | Evet | Bizim SalePrice |
| CargoPrice | decimal | Evet | Bizde yok - kargo ucreti |
| DeliveryTime | string | Evet | Bizde yok - teslimat suresi |

**Kanit (satir 61-67):**
```csharp
var product = new PttAvmProduct
{
    ProductName = "Urun Adi",
    CategoryCode = "CAT123",
    Price = 199.99m,
    CargoPrice = 9.99m,
    DeliveryTime = "1-2 is gunu"
};
```

**Onemli Bulgular:**
- **CargoPrice** alani var — kargo ucreti urune baglanmis
- **DeliveryTime** string alani — teslimat suresi urune baglanmis
- **CategoryCode** string (ID degil, kod)

### B. Siparis Modeli (Order)

Kargo kodu orneginden cikarilan alanlar (satir 76-88):

| Alan | Tip | Aciklama |
|------|-----|----------|
| order.CustomerName | string | Musteri adi |
| order.ShippingAddress | string | Kargo adresi |
| order.Id | ? | Siparis ID |

**PTT Kargo Request modeli (satir 78-84):**

| Alan | Tip | Aciklama |
|------|-----|----------|
| ReceiverName | string | Alici adi |
| ReceiverAddress | string | Alici adresi |
| SenderCode | string | Gonderen (satici) kodu |
| ServiceType | string | Servis tipi (ornek: "Express") |
| TrackingNumber | string | Takip numarasi (response'tan - satir 87) |

### C. Kategori & Marka Modeli

- **Kategori ID tipi:** string (CategoryCode)
- Detaylar: README'DE YOK

### D. Musteri / Alici Modeli

Kargo kodundan cikarilan (satir 80-81):
- **CustomerName**: string
- **ShippingAddress**: string (tek alan — yapisal degil)

### E. Tenant / Magaza Yapisi

| Alan | Tip | Aciklama |
|------|-----|----------|
| MerchantCode | string | Satici kodu |
| ApiKey | string | API anahtari |
| BaseUrl | string | API base URL |
| PttCargoToken | string | PTT Kargo ozel token |

**Kanit (satir 28-36):**
- **Satici ID Adi:** MerchantCode
- **Ozel alan:** PttCargoToken — entegre kargo sistemi icin ayri token
- **Auth tipi:** MerchantCode + ApiKey + PttCargoToken (REST)

---

## PLATFORM 9: SECURITY (Guvenlik Modulu)

**Kaynak:** `MesTech_Security/Docs/README.md`

### F. Guvenlik Modeli

#### User Entity (Cikarilabilen Alanlar)

Kod orneginden (satir 43-48):

| Alan | Tip | Zorunlu | Aciklama |
|------|-----|---------|----------|
| Id | ? (muhtemelen Guid) | Evet | Kullanici ID (satir 53) |
| Email | string | Evet | E-posta adresi |
| UserName | string | Evet | Kullanici adi |
| IsActive | bool | Evet | Aktif durumu |
| CreatedDate | DateTime | Evet | Olusturma tarihi |
| Password | string (hashed) | Evet | Sifre (BCrypt ile hashlenir - satir 144) |

**Kanit (satir 43-48):**
```csharp
var user = new User
{
    Email = request.Email,
    UserName = request.Username,
    IsActive = true,
    CreatedDate = DateTime.UtcNow
};
```

#### Role & Permission Yapisi

**Roller (satir 64-89):**

| Rol | Izinler |
|-----|---------|
| SuperAdmin | `*` (tum izinler) |
| Admin | users.read, users.write, system.config |
| Manager | products.read, products.write, orders.read |
| Operator | products.read, orders.read |

**Izin yapisi:** Nokta-ayirici string pattern (`kaynak.islem` formati)

#### Session Modeli

JWT Token yapilandirilmasi (satir 96-104):

| Alan | Tip | Deger |
|------|-----|-------|
| SecretKey | string | Min 32 karakter |
| Issuer | string | "MesTech.Security" |
| Audience | string | "MesTech.APIs" |
| ExpirationMinutes | int | 60 |
| RefreshTokenExpirationDays | int | 7 |

#### SecurityAuditLog Entity (satir 163-174):

| Alan | Tip | Aciklama |
|------|-----|----------|
| UserId | ? | Kullanici ID |
| Action | string | Yapilan islem |
| Resource | string | Etkilenen kaynak |
| IpAddress | string | IP adresi |
| UserAgent | string | Tarayici/istemci bilgisi |
| Timestamp | DateTime | Islem zamani |
| Success | bool | Basarili mi? |

#### RBAC Detaylari

- **Authentication:** JWT Token + OAuth 2.0 + OpenID Connect + MFA + SSO (satir 23-26)
- **Authorization:** RBAC + Permission-based + Resource-level + API Gateway (satir 29-32)
- **Password Policy (satir 206-212):** MinLength(8), RequireUppercase, RequireLowercase, RequireDigit, RequireSpecialChar
- **Lockout Policy (satir 213-216):** MaxFailedAttempts(5), LockoutDuration(15dk)
- **Session (satir 217-220):** TimeoutMinutes(30), SlidingExpiration(true)
- **Sifreleme:** AES (satir 124-136), BCrypt password hashing (satir 140-153)

---

## PLATFORM 10: DASHBOARD MIMARI

**Kaynak:** `Docs/WINDOWS_DESKTOP_DASHBOARD_MIMARISI_VE_YOLHARITASI.md`

### Mimari Karar Ozeti

Bu README bir platform degil, **sistem mimari dokumanidir**. Domain model acisindan onemli cikarimlar:

1. **Modul Sistemi:** IModule interface (ModuleName, Version, DashboardView, Initialize/Start/Stop, IsHealthy)
2. **Kullanici Rolleri (mevcut):** super_admin, admin, marketplace_manager (satir 68)
3. **Veritabani:** SQLite yerel DB planlaniyor (satir 119)
4. **Published Cikti:** Tek uygulama icinde DLL modulleri

**Domain icin onemli NOT:** Bu dokuman Clean Architecture + DDD ile yazilmis domain modelini degil, WPF masaustu uygulamasinin UI mimarisini tanimlar. Emirnamedeki `.NET 9 + PostgreSQL 16` karari ile bu dokumandaki `SQLite` cakismaktadir — **PostgreSQL 16 kesinlesmis karar olarak onceliklidir.**

---

## UNIFIED MODEL TABLOLARI

### TABLO 1: UNIFIED PRODUCT MODEL

| Alan | Tip | Stok | Trendyol | N11 | HB | Amazon | eBay | Ciceksepeti | Ozon | Pazarama | PttAVM | Unified Karar |
|------|-----|------|----------|-----|----|--------|------|-------------|------|----------|--------|---------------|
| Id | Guid | int | Guid | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **Guid** (kesinlesmis) |
| SKU | string | 50 | 100 | README'DE YOK | var (HepsiburadaSku) | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **string(100) UNIQUE** (kesinlesmis) |
| Barcode | string | 4 alan | 1 alan | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **string + BarcodeType** (kesinlesmis) |
| Name | string | 200 | 500 | var (title) | var (Title) | README'DE YOK | var (Title) | README'DE YOK | var (Name) | var (Title) | var (ProductName) | **string(500)** (kesinlesmis) |
| Description | string | sinirsiz | 2000 | README'DE YOK | README'DE YOK | README'DE YOK | var (Description) | README'DE YOK | README'DE YOK | var (Description) | README'DE YOK | **string(sinirsiz)** (kesinlesmis) |
| PurchasePrice | decimal | var | yok | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **var** (Stok'a ozel) |
| SalePrice | decimal | var | var | var (price) | var (Price) | README'DE YOK | var (Price) | README'DE YOK | var (Price-string!) | var (Price) | var (Price) | **decimal(18,2)** |
| ListPrice | decimal | var | yok | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **decimal(18,2) opsiyonel** |
| Stock | int | var | var | README'DE YOK | var (StockQuantity) | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | var (Stock) | README'DE YOK | **int** |
| MinStock | int | 5 | var | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **int default(5)** |
| MaxStock | int | 1000 | var | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **int default(1000)** |
| TaxRate | decimal | 5,2 | int | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **decimal(5,2)** (kesinlesmis) |
| Weight | decimal | kg | gram | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **decimal + WeightUnit** (kesinlesmis) |
| CategoryId | FK | var (int) | yok (string) | var (int) | var (string) | README'DE YOK | README'DE YOK | README'DE YOK | var (int) | var (string) | var (CategoryCode-string) | **FK -> Category** (kesinlesmis) |
| SupplierId | FK | var | yok | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **FK -> Supplier** |
| Brand | string | yok | yok | README'DE YOK | var (Brand) | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | **Oneri: BrandId FK ekle** |
| Currency | string | yok | yok | README'DE YOK | README'DE YOK | README'DE YOK | var (Currency) | README'DE YOK | var (CurrencyCode) | README'DE YOK | README'DE YOK | **Oneri: CurrencyCode enum** |
| NameLocalized | string | yok | yok | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | var (NameRu) | README'DE YOK | README'DE YOK | **Oneri: PlatformMapping'e** |
| CargoPrice | decimal | yok | yok | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | var (CargoPrice) | **Oneri: PlatformMapping'e** |
| DeliveryTime | string | yok | yok | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | var (DeliveryTime) | **Oneri: PlatformMapping'e** |

### TABLO 2: SIPARIS DURUM AKISLARI

| Durum | Unified Adi | Trendyol | N11 | HB | Amazon | eBay | Ciceksepeti | Ozon | Pazarama | PttAVM |
|-------|------------|----------|-----|----|--------|------|-------------|------|----------|--------|
| Yeni | Pending | Created | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |
| Onayli | Confirmed | Approved | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |
| Hazirlaniyor | Processing | Picking | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | awaiting_packaging | README'DE YOK | README'DE YOK |
| Kargoda | Shipped | Shipped | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |
| Teslim | Delivered | Delivered | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |
| Iptal | Cancelled | Cancelled | README'DE YOK | README'DE YOK | README'DE YOK | OrderCancellation | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |
| Iade | Returned | Returned | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |
| Satildi | Sold | — | README'DE YOK | README'DE YOK | README'DE YOK | ItemSold | README'DE YOK | README'DE YOK | README'DE YOK | README'DE YOK |

**NOT:** Siparis durum akislari icin README'ler yetersizdir. Sadece Trendyol (emirnameden), eBay (webhook'tan) ve Ozon (filter'dan) kismi bilgi vermektedir.

### TABLO 3: TENANT CREDENTIAL MATRISI

| Platform | Satici ID Adi | ID Tipi | Auth Alanlari | Auth Tipi |
|----------|---------------|---------|---------------|-----------|
| Trendyol | SupplierId | long | ApiKey, ApiSecret, SupplierId | REST API Key |
| N11 | (README'DE YOK) | (README'DE YOK) | appKey, appSecret | SOAP Header |
| Hepsiburada | MerchantId | string | Username, Password, MerchantId | REST Basic |
| Amazon | MarketplaceId | string | ClientId, ClientSecret, RefreshToken, MarketplaceId | OAuth 2.0 |
| eBay | (README'DE YOK) | (README'DE YOK) | ClientId, ClientSecret, RedirectUri, Scopes | OAuth 2.0 |
| Ciceksepeti | (README'DE YOK) | (README'DE YOK) | ApiKey, SecretKey | REST API Key |
| Ozon | ClientId | string | ClientId, ApiKey | REST API Key |
| Pazarama | SellerId | string | ApiKey, SecretKey, SellerId | REST API Key |
| PttAVM | MerchantCode | string | MerchantCode, ApiKey, PttCargoToken | REST API Key |

---

## KRITIK BULGULAR

### 1. README DOSYALARI YETERSIZ — DETAYLI ENTITY MODELI ICERMIYOR

**En kritik bulgu:** 8 platform README'sinin hicbiri **tam entity alan tanimlamasi** icermemektedir. README'ler genel kurulum/kullanim rehberleri olup, DDD/Clean Architecture icin gereken detayli veri modeli bilgisi mevcut degildir.

**Etki:** Unified Domain Model olusturmak icin README'ler tek basina YETERLI DEGILDIR.

**Cozum Onerisi:** Her platformun **gercek API dokumanlarini** (N11 SOAP WSDL, Hepsiburada API docs, Amazon SP-API docs, vb.) inceleyerek detayli entity modelleri cikarilmalidir.

### 2. KATEGORI ID TIPI TUTARSIZLIGI

| Platform | CategoryId Tipi |
|----------|----------------|
| Stok | int (FK) |
| N11 | int |
| Hepsiburada | string |
| Ozon | int |
| Pazarama | string |
| PttAVM | string (CategoryCode) |

**Karar:** Unified entity'de `Guid` FK kullanilacak (kesinlesmis karar). Platform-ozel kategori ID'leri `CategoryPlatformMapping` tablosunda tutulacak.

### 3. PARA BIRIMI GEREKSINIMI

eBay (`Currency: "USD"`) ve Ozon (`CurrencyCode: "RUB"`) multi-currency destek gerektirmektedir. Mevcut Stok ve Trendyol modellerinde para birimi alani yoktur (TL varsayimi).

**Karar Gerekli:** Uluslararasi pazaryerleri icin `CurrencyCode` enum/string alani gereklidir.

### 4. FIYAT TIPI TUTARSIZLIGI

- Cogu platform: `decimal`
- Ozon: `string` ("1500.00")

**Karar:** Unified entity'de `decimal(18,2)` kalsin, Ozon adapter'i string<->decimal donusumu yapsin.

### 5. BRAND (MARKA) ALANI EKSIKLIGI

Hepsiburada'da Brand zorunlu ve onay sureci var. Mevcut Stok ve Trendyol modellerinde ayri bir Brand alani yok.

### 6. KARGO MODELI CESITLILIGI

- PttAVM: Entegre PTT Kargo (PttCargoToken)
- Hepsiburada: HepsiJet
- Ozon: FBO/FBS secenekleri
- eBay: Global Shipping Program

Her platformun farkli kargo modeli, Unified Order entity'de esnek bir `ShippingProvider` ve `FulfillmentType` yapisi gerektirmektedir.

### 7. AUTH TIPI CESITLILIGI

3 farkli auth pattern mevcut:
- **SOAP:** N11 (appKey/appSecret header'da)
- **OAuth 2.0:** Amazon, eBay (token refresh mekanizmasi)
- **REST API Key:** Trendyol, Ciceksepeti, Ozon, Pazarama, PttAVM
- **Basic Auth:** Hepsiburada (Username/Password)

**Karar Gerekli:** `PlatformCredential` entity'si esnek bir JSON veya key-value yapida olmali.

---

## ONERILER

### 1. Product Entity'ye Eklenmesi Gereken YENI Alanlar

| Alan | Tip | Gerekce |
|------|-----|---------|
| BrandId | Guid FK -> Brand | Hepsiburada zorunlu kilar, diger platformlarda da yaygin |
| CurrencyCode | string(3) / enum | eBay (USD), Ozon (RUB) — uluslararasi platformlar icin sart |

### 2. ProductPlatformMapping'e Gitmesi Gereken Platform-Ozel Alanlar

| Alan | Platform | Aciklama |
|------|----------|----------|
| PlatformCategoryId | Tum platformlar | Her platformun kendi kategori ID'si |
| PlatformSKU | Tum platformlar | Platformdaki SKU (HepsiburadaSku gibi) |
| CargoPrice | PttAVM | Kargo ucreti |
| DeliveryTime | PttAVM | Teslimat suresi |
| NameLocalized | Ozon | Lokalize isim (Rusca vb.) |
| CurrencyCode | eBay, Ozon | Platform para birimi |
| ListingType | eBay | Fixed Price / Auction |
| FulfillmentType | Ozon | FBO / FBS |
| Slug, MetaTitle, MetaDescription | Trendyol | SEO alanlari (kesinlesmis) |
| Rating, ReviewCount, SalesCount, ViewCount | Trendyol | Analitik alanlari (kesinlesmis) |
| ImageUrls | Trendyol | Platform gorsel URL'leri |

### 3. Order Entity Unified Durum Akisi Onerisi

```
OrderStatus enum:
    Pending = 0,        // Yeni siparis (tum platformlar)
    Confirmed = 1,      // Onaylandi
    Processing = 2,     // Hazirlaniyor (Trendyol: Picking, Ozon: awaiting_packaging)
    Shipped = 3,        // Kargoya verildi
    InTransit = 4,      // Yolda (kargo takibi)
    Delivered = 5,      // Teslim edildi
    Cancelled = 6,      // Iptal edildi (eBay: OrderCancellation)
    ReturnRequested = 7,// Iade talep edildi
    Returned = 8,       // Iade tamamlandi
    Refunded = 9        // Para iadesi yapildi
```

**NOT:** Bu enum README'lerden cikarilabilecek MAKSIMUM bilgiyle olusturulmustur. Her platformun gercek API'sindeki durum akislari incelendikten sonra GUNCELLENMELI.

### 4. Tenant -> Store Model Detay Onerisi

```
Tenant (Kiralaci/Firma)
    |-- Id: Guid
    |-- Name: string
    |-- TaxNumber: string
    |-- CreatedAt/UpdatedAt/IsDeleted (BaseEntity)
    |
    |-- Stores (1:N)
        |-- Id: Guid
        |-- TenantId: Guid FK
        |-- PlatformType: enum (Trendyol, N11, Hepsiburada, Amazon, eBay, Ciceksepeti, Ozon, Pazarama, PttAVM)
        |-- StoreName: string
        |-- IsActive: bool
        |-- Credentials: (sifrelenmis JSON veya ayri tablo)
        |   |-- Id: Guid
        |   |-- StoreId: Guid FK
        |   |-- Key: string (ornek: "ApiKey", "MerchantId", "ClientSecret")
        |   |-- Value: string (AES-256 sifrelenmis)
        |   |-- CreatedAt/UpdatedAt
        |
        |-- BaseEntity audit alanlari
```

**Credential sifreleme:**
- AES-256 sifreleme (Security README'de AES ornegi mevcut - satir 124-136)
- Key-Value yapisi: Her platformun farkli credential alanlari oldugu icin esnek yapida olmali
- Master key: appsettings/environment variable'dan

### 5. Category Eslestirme Stratejisi Onerisi

```
Category (Bizim unified kategori agacimiz)
    |-- Id: Guid
    |-- ParentId: Guid? (hiyerarsik — N11'deki GetTopLevelCategories bunu dogrular)
    |-- Name: string
    |-- Level: int
    |
    |-- CategoryPlatformMappings (1:N)
        |-- Id: Guid
        |-- CategoryId: Guid FK
        |-- PlatformType: enum
        |-- PlatformCategoryId: string (int olan platformlar icin de string sakla — evrensel)
        |-- PlatformCategoryName: string (opsiyonel, debug icin)
        |-- IsActive: bool
```

**Strateji:**
- Kendi kategori agacimiz **master** olacak
- Her platform kategorisi ayri mapping tablosunda tutulacak
- PlatformCategoryId `string` olacak cunku: N11/Ozon int, Hepsiburada/Pazarama string, PttAVM code
- Otomatik eslestirme icin AI oneri sistemi planlanabilir (MesTech_AI moduluyle)

### 6. Varyant Yapisi Onerisi

```
Product (Ana urun)
    |-- Id: Guid
    |-- ... (unified alanlar)
    |-- HasVariants: bool
    |
    |-- ProductVariants (1:N)
        |-- Id: Guid
        |-- ProductId: Guid FK (ana urun)
        |-- SKU: string(100) UNIQUE
        |-- Barcode: string
        |-- VariantAttributes: (1:N)
        |   |-- AttributeName: string (ornek: "Renk", "Beden", "Malzeme")
        |   |-- AttributeValue: string (ornek: "Kirmizi", "XL", "Pamuk")
        |-- Price: decimal? (null ise ana urunun fiyati)
        |-- Stock: int
        |-- IsActive: bool
        |
        |-- VariantPlatformMappings (1:N)
            |-- PlatformType: enum
            |-- PlatformVariantId: string
```

**Gerekce:**
- Hepsiburada: Varyant yonetimi acikca belirtilmis (satir 46)
- Ozon: Varyant yonetimi belirtilmis (satir 45)
- Trendyol: Color/Size alanlari mevcut (emirnameden)
- eBay: Listing yonetimi varyant iceriyor (satir 48)

**Yaklasim:** Ana urun + alt varyant modeli. Her varyant kendi SKU/Barcode/Stock'una sahip. Platform-ozel varyant ID'leri ayri mapping'te.

---

## SONUC

Bu rapor, 8 platform README'si + Security README'si + Dashboard Mimari dokumani uzerinden **MAKSIMUM cikarilabilecek veri modeli bilgisini** icermektedir.

**Kritik aksiyon:** README dosyalari Unified Domain Model icin **yetersizdir**. Bir sonraki adim olarak su kaynaklarin incelenmesi onerilir:

1. **Her platformun resmi API dokumanlarinda** detayli entity semateri
2. **Mevcut kod tabani** icinde tanimlanmis entity/model siniflari (varsa)
3. **Postman collection'lari** veya API response ornekleri (varsa)

Bu ek kaynaklar ile Tablo 1, 2 ve 3'teki "README'DE YOK" alanlari doldurulabilir ve gercek Unified Domain Model kesinlestirilebilir.

---

**RAPOR SONU — TAKIM 2**
**Kontrolor:** Claude Opus 4.6
**Tarih:** 06 Mart 2026
**Emirname Ref:** ENT-MD-001-T2
