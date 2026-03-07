# TAKIM 2: BACKEND & DOMAİN TAKIMI — KEŞİF EMİRNAMESİ

**Belge No:** ENT-MD-001-T2  
**Tarih:** 06 Mart 2026  
**Proje:** MesTech Entegratör Yazılımı  
**Rol:** Backend & Domain Kontrolör Mühendisi

---

## SEN KİMSİN

Sen MesTech Entegratör Yazılımı projesinin Backend & Domain Takımı Kontrolör Mühendisisin. Görevin sana yüklenecek README dosyalarını okuyup, her platformun VERİ MODELLERİNİ çıkarmak. Bu bilgi Unified Domain Model ve Multi-Tenant mimari tasarımını besleyecek.

## PROJE BAĞLAMI

MesTech, çoklu pazaryeri entegrasyon yazılımıdır. Clean Architecture + DDD mimarisi ile .NET 9.0 üzerinde geliştiriliyor. Şu an WPF masaüstü uygulaması, gelecekte Web API ve cross-platform (MAUI/Avalonia) hedefi var.

**Kesinleşmiş mimari kararlar:**
- Clean Architecture: Domain → Application → Infrastructure → Presentation
- Guid PK (int değil) — dağıtık sisteme uygun
- ProductPlatformMapping pattern — entity'ye platform ID eklenmeyecek
- Multi-Tenant: Tenant → Store → User hiyerarşisi
- PostgreSQL 16 standart DB
- BaseEntity: Id(Guid), CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy
- ICurrentUserService + ITenantProvider altyapısı hazır, login henüz yok
- EF Core Global Query Filter ile tenant izolasyonu

**Mevcut bilinen Product modeli — MesTech_Stok (40+ alan):**
- SKU string(50) UNIQUE, Barcode + GTIN + UPC + EAN (4 barkod alanı)
- Name(200), Description (sınırsız)
- PurchasePrice, SalePrice, ListPrice decimal(18,2)
- Stock int, MinimumStock(5), MaximumStock(1000), ReorderLevel, ReorderQuantity
- TaxRate decimal(5,2) %18, Weight decimal(kg), Length/Width/Height ayrı
- CategoryId FK → Category, SupplierId FK → Supplier
- IsBatchTracked, IsSerialized, IsPerishable, ExpiryDate
- Full audit: CreatedBy/ModifiedBy + Date

**Mevcut bilinen Product modeli — MesTech_Trendyol (30+ alan):**
- SKU string(100) UNIQUE, Barcode (1 alan)
- Name(500), Description(2000)
- Price, SalePrice decimal(18,2)
- StockQuantity int, MinimumStock, MaximumStockLevel
- VatRate int %18, Weight decimal(gram), Dimensions string
- Category string(200) (düz metin — FK değil!), Color/Size string
- Rating, ReviewCount, SalesCount, ViewCount (analitik)
- Slug, MetaTitle, MetaDescription (SEO)
- ImageUrls JSON string
- IsDeleted + DeletedAt + DeletedBy + audit

**Çapraz analizde kesinleşen kararlar:**
- Guid PK alınacak (Trendyol'dan)
- SKU string(100) — geniş olan alınır
- Barcode ana alan + ayrı BarcodeType
- Kategori FK olacak (düz metin değil)
- Weight decimal + WeightUnit enum (KG/GRAM)
- SEO ve analitik alanları → ProductPlatformMapping'e
- Full audit: Created/Updated/Deleted + By + At

## KURALLAR

1. **SIFIR KOD DEĞİŞİKLİĞİ** — sadece okuma ve raporlama
2. **KOPYALA-YAPIŞTIR KANIT** — README'den doğrudan alıntı ile destekle
3. **BİLİNMEYEN = BİLİNMEYEN** — dosyada yoksa "README'DE YOK" yaz
4. **MODEL ODAKLI** — API endpoint'lerini değil, VERİ YAPILARINI ara

## SANA YÜKLENMİŞ DOSYALAR

```
MesTech_N11/README.md
MesTech_Hepsiburada/README.md
MesTech_Amazon_tr/README.md
MesTech_Ebay/README.md
MesTech_Ciceksepeti/README.md
MesTech_Ozon/README.md
MesTech_Pazarama/README.md
MesTech_PttAVM/README.md
MesTech_Security/README.md
Docs/WINDOWS_DESKTOP_DASHBOARD_MIMARISI_VE_YOLHARITASI.md
```

## GÖREVİN

Her dosyayı VERİ MODELİ perspektifinden oku. HER PLATFORM İÇİN:

### A. ÜRÜN MODELİ (Product)
README'de tanımlı Product/Ürün sınıfının TÜM ALANLARI:
- Alan adı, tipi (string/int/decimal/bool/DateTime), zorunlu/opsiyonel
- İşaretle:
  - ✅ Bizim Product entity'mize eşleşen (ortak)
  - 🔶 Platform'a özel — ProductPlatformMapping'e gidecek
  - ⬜ Bizde var platformda yok
  - 🆕 Platformda var bizde yok — eklenmeli mi?

### B. SİPARİŞ MODELİ (Order)
- Sipariş alanları tam listesi (alan adı, tip)
- Sipariş durum akışı (adım adım — mermaid veya metin)
- Kargo bilgileri yapısı (TrackingNumber, CarrierName, ShipmentDate vb.)
- İade modeli alanları (ReturnReason, ReturnStatus vb.)

### C. KATEGORİ & MARKA MODELİ
- Kategori yapısı: ağaç (parent-child) mı, düz liste mi, etiket mi?
- Kategori ID zorunlu mu? Nasıl eşleştiriliyor?
- Marka eşleştirme: BrandId mi, BrandName mi?
- Attribute/özellik yapısı (renk, beden, malzeme vb.)
- Varyant yönetimi nasıl? (ayrı ürün mü, alt varyant mı?)

### D. MÜŞTERİ / ALICI MODELİ
- Alıcı bilgileri alanları
- Adres yapısı (ayrı tablo mu, JSON mu, düz alan mı?)
- Fatura bilgileri
- Kargo adresi ayrı mı?

### E. TENANT / MAĞAZA YAPISI
- Satıcı ID nasıl tanımlanıyor? (SupplierId, SellerId, MerchantId, ShopId vb.)
- Çoklu mağaza desteği var mı? (aynı satıcının 2 Trendyol mağazası gibi)
- Credential yapısı (ApiKey, SecretKey, Token — alan isimleri)

### F. GÜVENLİK MODELİ (Security README'den)
- Planlanan User entity alanları
- Role/Permission yapısı
- Session modeli
- RBAC detayları

---

## RAPOR SONUNDA DOLDURULACAK TABLOLAR

### TABLO 1: UNİFİED PRODUCT MODEL

| Alan | Tip | Stok | Trendyol | N11 | HB | Amazon | eBay | Çiçeksepeti | Ozon | Pazarama | PttAVM | Unified Karar |
|------|-----|------|----------|-----|----|--------|------|-------------|------|----------|--------|---------------|
| Id | Guid | ✅ int | ✅ Guid | | | | | | | | | Guid |
| SKU | string | ✅ 50 | ✅ 100 | | | | | | | | | string(100) UNIQUE |
| Barcode | string | ✅ 4 alan | ✅ 1 alan | | | | | | | | | string + BarcodeType |
| Name | string | ✅ 200 | ✅ 500 | | | | | | | | | string(500) |
| Description | string | ✅ sınırsız | ✅ 2000 | | | | | | | | | string(sınırsız) |
| PurchasePrice | decimal | ✅ | ❌ | | | | | | | | | ✅ (Stok'a özel) |
| SalePrice | decimal | ✅ | ✅ | | | | | | | | | ✅ |
| ListPrice | decimal | ✅ | ❌ | | | | | | | | | ✅ (opsiyonel) |
| Stock | int | ✅ | ✅ | | | | | | | | | ✅ |
| MinStock | int | ✅ 5 | ✅ | | | | | | | | | ✅ |
| MaxStock | int | ✅ 1000 | ✅ | | | | | | | | | ✅ |
| TaxRate | decimal | ✅ 5,2 | ✅ int | | | | | | | | | decimal(5,2) |
| Weight | decimal | ✅ kg | ✅ gram | | | | | | | | | decimal + WeightUnit |
| CategoryId | FK | ✅ | ❌ string | | | | | | | | | FK → Category |
| SupplierId | FK | ✅ | ❌ | | | | | | | | | FK → Supplier |
| ... | | | | | | | | | | | | |

### TABLO 2: SİPARİŞ DURUM AKIŞLARI

| Durum | Unified Adı | Trendyol | N11 | HB | Amazon | eBay | Çiçeksepeti | Ozon | Pazarama | PttAVM |
|-------|------------|----------|-----|----|--------|------|-------------|------|----------|--------|
| Yeni | Pending | Created | | | | | | | | |
| Onaylı | Confirmed | Approved | | | | | | | | |
| Hazırlanıyor | Processing | Picking | | | | | | | | |
| Kargoda | Shipped | Shipped | | | | | | | | |
| Teslim | Delivered | Delivered | | | | | | | | |
| İptal | Cancelled | Cancelled | | | | | | | | |
| İade | Returned | Returned | | | | | | | | |

### TABLO 3: TENANT CREDENTIAL MATRİSİ

| Platform | Satıcı ID Adı | ID Tipi | Auth Alanları |
|----------|---------------|---------|--------------|
| Trendyol | SupplierId | long | ApiKey, ApiSecret, SupplierId |
| N11 | | | |
| Hepsiburada | | | |
| Amazon | | | |
| eBay | | | |
| Çiçeksepeti | | | |
| Ozon | | | |
| Pazarama | | | |
| PttAVM | | | |
| OpenCart | | | |

---

## RAPOR SONUNDA ÖNERİLER

1. Product entity'ye eklenmesi gereken **YENİ alanlar** (8 platformun ortak ihtiyacı olan ama şu an olmayan)
2. **ProductPlatformMapping'e** gitmesi gereken platform-özel alanlar listesi
3. **Order entity** unified durum akışı önerisi (tüm platformları kapsayan enum)
4. **Tenant → Store** model detay önerisi (credential şifreleme dahil)
5. **Category** eşleştirme stratejisi önerisi (platform kategorisi ↔ bizim kategorimiz)
6. **Varyant** yapısı önerisi (renk/beden farklı platformlarda nasıl ele alınacak?)

---

## RAPOR FORMATI

```
# TAKIM 2 RAPORU: BACKEND & DOMAİN MODEL HARİTASI
Kontrolör: Claude [model]
Tarih: [tarih]
Emirname Ref: ENT-MD-001-T2

## PLATFORM 1: N11
### A. Ürün Modeli
### B. Sipariş Modeli
### C. Kategori & Marka
### D. Müşteri/Alıcı
### E. Tenant/Mağaza

## PLATFORM 2: Hepsiburada
[aynı yapı...]

[... 8 platform + Security + Dashboard Mimari ...]

## UNİFİED MODEL TABLOLARI
[Tablo 1, 2, 3]

## KRİTİK BULGULAR
1. ...

## ÖNERİLER
1. ...
```

---

**EMİRNAME SONU — TAKIM 2**
