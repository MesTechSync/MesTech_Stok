# 336 PRE-EXISTING TEST FAIL ANALİZ RAPORU

**Tarih:** 31 Mart 2026
**Yazar:** DEV 5 — Test & Kalite
**Toplam:** 9310 test, 8974 pass (%96.4), 336 fail (%3.6)

---

## 1. KATEGORİ DAĞILIMI

| Kategori | Fail Sayısı | % | Sorumlu DEV |
|----------|-------------|---|-------------|
| Settlement/Bank Parser | **111** | %33 | DEV 3 |
| Accounting Service | **15** | %4 | DEV 1 |
| AutoShipment (Integration) | **18** | %5 | DEV 3 |
| Event Handler (GL/Journal) | **12** | %4 | DEV 1 |
| Edge Case / Bulk Operation | **25** | %7 | DEV 1+5 |
| Chain Tests (Order→Stock) | **14** | %4 | DEV 1 |
| CQRS Handler Constructor | **30** | %9 | DEV 1+5 |
| Validator (3 adet) | **3** | %1 | DEV 5 |
| Domain Entity/Service | **15** | %4 | DEV 1 |
| Diğer (Handler, Filter, vb) | **93** | %28 | Çeşitli |

---

## 2. EN BÜYÜK FAIL GRUPLARI — KÖK NEDEN

### Grup 1: Settlement/Bank Parsers (111 fail) — %33

**Etkilenen Sınıflar:**
- AmazonSettlementParser (11), CiceksepetiSettlementParser (10)
- HepsiburadaSettlementParser (9), N11SettlementParser (10)
- PazaramaSettlementParser (11), TrendyolSettlementParser (8)
- OpenCartSettlementParser (8), OFXParser (8)
- MT940Parser (8), Camt053Parser (11), RealFormatTests (8)

**Kök Neden:** Parser interface/method signature değişti — testler eski API kullanıyor.
DEV 3 parser'ları refactor etti (ParseAsync dönüş tipi, parameter ekleme)
ama testler güncellenmedi.

**Fix:** DEV 3+5 — Parser testlerini yeni API'ye göre güncelle.

### Grup 2: AutoShipment (18 fail) — %5

**Etkilenen Sınıflar:**
- AutoShipmentServiceTests (9), AutoShipmentProdTests (9)

**Kök Neden:** `AutoShipmentService` constructor'ına yeni bağımlılıklar eklendi
(`ILogger`, `INotificationService`). Testler güncellenmedi.

**Fix:** DEV 3+5 — Constructor'a NullLogger + Mock ekle.

### Grup 3: Event Handlers / GL Journal (12 fail) — %4

**Etkilenen Sınıflar:**
- CommissionChargedGLHandlerTests (2)
- InvoiceCancelledReversalHandlerTests (1)
- OrderShippedCostHandlerTests (2)
- OrderToAccountingChainTests (7)

**Kök Neden:** GL Journal Entry oluşturma mantığı değişti —
`JournalEntry.Create()` factory method parametreleri güncellendi.

**Fix:** DEV 1 — Handler testlerini yeni JournalEntry API'sine uyumla.

### Grup 4: Edge Case / Bulk (25 fail) — %7

**Etkilenen Sınıflar:**
- BulkOperationEdgeCaseTests (7), PlaceOrderEdgeCaseTests (6)
- BulkUpdateHandlerTests (7), CQRS Hardening (5)

**Kök Neden:** Karışık — handler constructor değişiklikleri + domain entity
API değişiklikleri + yeni validation kuralları.

**Fix:** DEV 5 — Handler testlerini constructor uyumla + edge case güncelle.

### Grup 5: PlatformCommissionRateProvider (15 fail) — %4

**Kök Neden:** CommissionRate entity yapısı değişti — yeni field'lar eklendi,
constructor parametreleri güncellendi. Test mock'ları eski yapıda.

**Fix:** DEV 1+5 — Mock data'yı yeni entity yapısına güncelle.

### Grup 6: CQRS Handler Constructor Mismatch (30 fail) — %9

**Kök Neden:** Handler'lara `ILogger<T>` parametresi eklendi ama testler
güncellenmedi. `new Handler(repo)` → `new Handler(repo, logger)` olmalı.

**Fix:** DEV 5 — Tüm handler testlerine `NullLogger<T>.Instance` ekle.

---

## 3. FIX ÖNCELİK PLANI

### Faz 1 — Constructor Mismatch (Kolay, ~60 fail) — DEV 5

Handler + Service testlerine `NullLogger<T>.Instance` veya `Mock.Of<ILogger>()` ekle.
Tahmini süre: 1 oturum.

### Faz 2 — Parser API Update (Orta, ~111 fail) — DEV 3+5

Settlement parser testlerini yeni `ParseAsync` signature'a göre güncelle.
Tahmini süre: 2 oturum.

### Faz 3 — Domain/Event Logic (Zor, ~50 fail) — DEV 1+5

GL JournalEntry, CommissionRate, Order entity API değişikliklerini
testlere yansıt.
Tahmini süre: 2 oturum.

### Faz 4 — Edge Case Refinement (~115 fail) — DEV 5

BulkOperation, PlaceOrder edge case'leri + küçük handler testleri.
Tahmini süre: 2 oturum.

---

## 4. HEDEF

| Metrik | Şimdi | Faz 1 | Faz 2 | Faz 3 | Faz 4 |
|--------|-------|-------|-------|-------|-------|
| Pass | 8974 | 9034 | 9145 | 9195 | **9310** |
| Fail | 336 | 276 | 165 | 115 | **0** |
| % | 96.4 | 97.0 | 98.2 | 98.8 | **100** |
