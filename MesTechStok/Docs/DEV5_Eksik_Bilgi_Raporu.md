# DEV 5: EKSIK BILGI RAPORU

**Tarih:** 2026-03-07
**Hazirlayan:** Test & Kalite Takimi (DEV 5)
**Durum:** AKTIF — yeni bilgi eklendikce guncellenir

---

## MEVCUT SISTEMDEN CEKILEBILEN BILGILER

### MesTech_Stok (Calisiyor)
- [x] Urun listesi (DB'den — Product entity, 70+ alan)
- [x] Stok miktarlari (DB'den — Product.Stock, MinimumStock, MaximumStock)
- [x] Stok hareket gecmisi (DB'den — StockMovement entity)
- [x] Siparis gecmisi (DB'den — Order + OrderItem entities)
- [x] Kategori agaci (DB'den — Category entity, hiyerarsik yapi)
- [x] Tedarikci listesi (DB'den — Supplier entity)
- [x] Depo yapisi (DB'den — Warehouse + Zone + Rack + Shelf + Bin)
- [x] Kullanici/Rol bilgileri (DB'den — User + Role + Permission)
- [x] Barkod verileri (DB'den — Product.Barcode + BarcodeScanLog)
- [x] WAC maliyet hesaplama (StockCalculationService.CalculateWAC)
- [x] FEFO lot yonetimi (InventoryLot entity + SelectLotsForConsumption)
- [x] Envanter deger hesaplama (CalculateInventoryValue)
- [x] Fiyat hesaplama (PricingService — KDV dahil/haric, indirim)
- [x] Multi-tenant izolasyonu (ITenantEntity + Global Query Filter)
- [x] Domain event sistemi (StockChangedEvent, OrderPlacedEvent vb.)
- [x] CQRS pattern (MediatR — AddStock, CreateProduct, SyncPlatform)
- [x] Offline kuyruk (OfflineQueueItem entity)

### Trendyol API (Adapter Yok — Contract Testleri Hazir)
- [x] API endpoint yapisi biliniyor (sapigw/suppliers/*/products)
- [x] Batch islem destegi (batchRequestId)
- [x] Stok/fiyat guncelleme endpoint (price-and-inventory)
- [?] Marka listesi (kontrol edilmeli)
- [?] Kargo firmalari (kontrol edilmeli)
- [?] Komisyon oranlari (dokumantasyon eksik)

### OpenCart (Adapter Var — Core Katmaninda)
- [x] Urunler (REST API uzerinden)
- [x] Siparisler (REST API uzerinden)
- [x] Stok durumu (REST API uzerinden)
- [x] Kategoriler (REST API uzerinden)
- [x] Retry + Circuit breaker (Polly entegrasyonu)
- [x] Correlation ID takibi
- [x] Telemetri ve hata kategorileme

### Barkod Tarama (Calisiyor)
- [x] USB HID barkod tarayici (HidBarcodeListener)
- [x] Kamera ile barkod okuma (ICameraBarcodeService — ZXing/OpenCV)
- [x] EAN-13, EAN-8, UPC, Code128 dogrulama

---

## CEKILEMEYEN / EKSIK BILGILER

### Diger 8 Platform (Kod Yok)
- [ ] N11 — adapter yazilmadi, API baglantisi yok
- [ ] Hepsiburada — adapter yazilmadi
- [ ] Amazon TR — adapter yazilmadi
- [ ] eBay — adapter yazilmadi
- [ ] Ciceksepeti — adapter yazilmadi
- [ ] Ozon — adapter yazilmadi
- [ ] Pazarama — adapter yazilmadi
- [ ] PttAVM — adapter yazilmadi

### README'lerde Eksik Bilgiler
- [ ] Detayli entity alan tanimlari (hicbir platformda yok)
- [ ] API rate limit degerleri (cogunda yok)
- [ ] Batch size limitleri (cogunda yok)
- [ ] Hata kodlari ve aciklamalari
- [ ] Webhook payload formatlari
- [ ] Sandbox test credential'lari

### Sistem Genelinde Eksik
- [ ] Performance benchmark verileri
- [ ] Test coverage raporu (coverlet yapilandirildi ama rapor uretilmedi)
- [ ] Uretim ortami metrikleri
- [ ] Kullanici sayisi ve yuk bilgileri
- [ ] Deployment dokumantasyonu
- [ ] Trendyol adapter implementasyonu (sadece contract testleri var)
- [ ] Diger platform adapter'lari

### Devre Disi Bilesenler
- [ ] LocationService.cs — model tutarsizliklari nedeniyle devre disi
- [ ] WarehouseOptimizationService.cs — devre disi
- [ ] MobileWarehouseService.cs — devre disi
- [ ] Infrastructure/Security katmani — bos (auth Core'da)

---

## TEST DURUM RAPORU

| Katman | Mevcut Test | Yeni Test | Toplam |
|--------|------------|-----------|--------|
| MesTechStok.Tests (Legacy) | 8 | 0 | 8 |
| MesTech.Tests.Unit | 0 | 102 | 102 |
| MesTech.Tests.Integration | 0 | 24 | 24 |
| **TOPLAM** | **8** | **126** | **134** |

### Test Kategorileri
- Domain koruma testleri: Product, Order, Category, Tenant, Store, InventoryLot
- Value Object testleri: Barcode, Money, SKU, StockLevel
- Domain Service testleri: BarcodeValidation, StockCalculation (WAC/FEFO), Pricing
- Application Handler testleri: CreateProduct, AddStock, SyncPlatform
- Tenant izolasyonu testleri: Global Query Filter, cross-tenant erisim engeli
- Adapter contract testleri: Trendyol (WireMock), OpenCart (WireMock)

---

**BU RAPOR DEGISMEZ — yeni bilgi cekildiginde guncellenir, mevcut bilgi silinmez.**
