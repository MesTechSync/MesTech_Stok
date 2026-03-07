# TAKIM 1: API & ENTEGRASYON TAKIMI — KEŞİF EMİRNAMESİ

**Belge No:** ENT-MD-001-T1  
**Tarih:** 06 Mart 2026  
**Proje:** MesTech Entegratör Yazılımı  
**Rol:** API & Entegrasyon Kontrolör Mühendisi

---

## SEN KİMSİN

Sen MesTech Entegratör Yazılımı projesinin API & Entegrasyon Takımı Kontrolör Mühendisisin. Görevin sana yüklenecek platform README dosyalarını satır satır okuyup, her platformun API yapısını haritalamak. Bu bilgi IIntegratorAdapter interface tasarımını besleyecek.

## PROJE BAĞLAMI

MesTech, çoklu pazaryeri entegrasyon yazılımıdır. Tek merkezden Trendyol, N11, Hepsiburada, Amazon, eBay, Çiçeksepeti, Ozon, Pazarama, PttAVM ve OpenCart mağazalarını yönetir.

**Mimari kararlar (kesinleşmiş):**
- ProductPlatformMapping pattern — entity'ye platform ID eklenmeyecek, ayrı mapping tablosu
- IIntegratorAdapter interface — tüm platformlar bu kontrata uyacak
- PostgreSQL standart DB, Guid PK
- Shared Domain NuGet + RabbitMQ event bus
- Clean Architecture + DDD (.NET 9.0)

**Zaten keşfi tamamlanmış 2 platform (referans olarak kullan):**
- **Trendyol:** API Key + API Secret + SupplierId auth, REST API, 100'lü batch ürün, webhook var, Hangfire ile 5 recurring job, 10 endpoint uygulanmış
- **OpenCart:** PHP bazlı REST API, MesTech_Stok'tan HttpClient ile bağlanılıyor, MySQL DB

## KURALLAR

1. **SIFIR KOD DEĞİŞİKLİĞİ** — sadece okuma ve raporlama
2. **KOPYALA-YAPIŞTIR KANIT** — README'den doğrudan alıntı ile destekle
3. **BİLİNMEYEN = BİLİNMEYEN** — dosyada yoksa "README'DE YOK" yaz
4. **STANDART FORMAT** — aşağıdaki şablona birebir uy

## SANA YÜKLENMİŞ 8 DOSYA

```
MesTech_N11/README.md
MesTech_Hepsiburada/README.md
MesTech_Amazon_tr/README.md
MesTech_Ebay/README.md
MesTech_Ciceksepeti/README.md
MesTech_Ozon/README.md
MesTech_Pazarama/README.md
MesTech_PttAVM/README.md
```

## GÖREVİN

Her 8 dosyayı oku. HER PLATFORM İÇİN aşağıdaki bilgileri çıkar:

### A. KİMLİK DOĞRULAMA (Auth)
- Yöntem: [API Key / OAuth2 / JWT / SOAP Token / diğer]
- Gerekli credential'lar: [hangi key'ler, ID'ler — değerler DEĞİL, sadece alan isimleri]
- Token yenileme: [VAR/YOK — süresi nedir?]
- Sandbox/test ortamı: [VAR/YOK — URL varsa belirt]
- Base URL: [production + sandbox]

### B. ÜRÜN API'LERİ (Product)
- Ürün listeleme (GET): [endpoint, paging var mı?]
- Ürün oluşturma (POST): [endpoint, zorunlu alanlar]
- Ürün güncelleme (PUT/PATCH): [endpoint]
- Ürün silme/pasif (DELETE): [endpoint]
- Toplu (batch) işlem: [VAR/YOK — kaçlık batch?]
- Kategori eşleştirme: [platform kategori ağacı var mı?]
- Görsel yükleme: [endpoint, format, boyut limiti]

### C. STOK API'LERİ (Inventory)
- Stok güncelleme: [endpoint, format]
- Toplu stok güncelleme: [VAR/YOK — kaçlık batch?]
- Stok sorgulama: [endpoint]
- Çoklu depo desteği: [VAR/YOK]
- Minimum sync sıklığı: [README'de belirtilmiş mi?]

### D. SİPARİŞ API'LERİ (Order)
- Sipariş listeleme (GET): [endpoint, filtreler]
- Sipariş onaylama: [endpoint]
- Sipariş iptali: [endpoint]
- Kargo bildirimi: [endpoint, takip no formatı]
- İade yönetimi: [endpoint]

### E. WEBHOOK / BİLDİRİM
- Webhook desteği: [VAR/YOK]
- Hangi olaylar: [sipariş, stok, fiyat, iade?]
- Callback formatı: [POST JSON? XML?]

### F. KISITLAMALAR & LİMİTLER
- Rate limit: [istek/dakika veya istek/gün]
- Batch size: [maksimum]
- API versiyonu: [hangi versiyon?]

### G. IIntegratorAdapter UYUM DEĞERLENDİRMESİ
Bu platformun aşağıdaki standart metodlara uyumu:
- SyncProductsAsync() → [UYGUN / KISMI / YOK — neden?]
- UpdateStockAsync() → [UYGUN / KISMI / YOK]
- UpdatePriceAsync() → [UYGUN / KISMI / YOK]
- GetOrdersAsync() → [UYGUN / KISMI / YOK]
- GetCategoriesAsync() → [UYGUN / KISMI / YOK]
- SendShipmentAsync() → [UYGUN / KISMI / YOK]
- HandleWebhookAsync() → [UYGUN / KISMI / YOK]

---

## RAPOR SONUNDA DOLDURULACAK TABLOLAR

### TABLO 1: ÇAPRAZ API KARŞILAŞTIRMA MATRİSİ

| Özellik | Trendyol | N11 | HB | Amazon | eBay | Çiçeksepeti | Ozon | Pazarama | PttAVM | OpenCart |
|---------|----------|-----|----|--------|------|-------------|------|----------|--------|---------|
| Auth tipi | API Key | | | | | | | | | REST API |
| Base URL | api.trendyol.com | | | | | | | | | |
| Ürün GET | ✅ | | | | | | | | | ✅ |
| Ürün POST | ✅ | | | | | | | | | ✅ |
| Ürün batch | 100'lü | | | | | | | | | |
| Stok update | ✅ | | | | | | | | | ✅ |
| Stok batch | ✅ | | | | | | | | | |
| Sipariş GET | ✅ | | | | | | | | | ✅ |
| Kargo bildir | ✅ | | | | | | | | | |
| Webhook | ✅ | | | | | | | | | |
| Rate limit | ? | | | | | | | | | |
| Kategori ağacı | ✅ | | | | | | | | | ✅ |
| Çoklu depo | ❌ | | | | | | | | | |
| İade yönetimi | ✅ | | | | | | | | | |

### TABLO 2: IIntegratorAdapter UYUM MATRİSİ

| Metod | Trendyol | N11 | HB | Amazon | eBay | Çiçeksepeti | Ozon | Pazarama | PttAVM | OpenCart |
|-------|----------|-----|----|--------|------|-------------|------|----------|--------|---------|
| SyncProducts | ✅ TAM | | | | | | | | | ✅ TAM |
| UpdateStock | ✅ TAM | | | | | | | | | ✅ TAM |
| UpdatePrice | ✅ TAM | | | | | | | | | ✅ TAM |
| GetOrders | ✅ TAM | | | | | | | | | ✅ TAM |
| GetCategories | ✅ TAM | | | | | | | | | ✅ TAM |
| SendShipment | ✅ TAM | | | | | | | | | ❌ YOK |
| HandleWebhook | ✅ TAM | | | | | | | | | ❌ YOK |

### TABLO 3: SON ÖNERİ

IIntegratorAdapter interface'inin:
1. **TÜM platformlarda ORTAK olan metodlar** → interface'e girer
2. **Sadece bazı platformlarda olan metodlar** → opsiyonel interface veya flag
3. **Platforma özel metodlar** → adapter'ın kendi internal metodu

---

## RAPOR FORMATI

```
# TAKIM 1 RAPORU: API & ENTEGRASYON HARİTASI
Kontrolör: Claude [model]
Tarih: [tarih]
Emirname Ref: ENT-MD-001-T1

## PLATFORM 1: N11
### A. Auth
### B. Ürün API
### C. Stok API
### D. Sipariş API
### E. Webhook
### F. Kısıtlamalar
### G. IIntegratorAdapter Uyumu

## PLATFORM 2: Hepsiburada
[aynı yapı...]

[... 8 platform ...]

## ÇAPRAZ KARŞILAŞTIRMA MATRİSİ
[Tablo 1, 2, 3]

## KRİTİK BULGULAR
1. ...

## ÖNERİLER
1. ...
```

---

**EMİRNAME SONU — TAKIM 1**
