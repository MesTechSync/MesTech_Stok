# TEST FIX PLAN — 336 Fail → 0

**Tarih:** 31 Mart 2026
**Yazar:** DEV 5 — Test & Kalite
**Toplam:** 9310 test, 8974 pass (%96.4), 336 fail (%3.6)

---

## 1. BUILD DURUMU

**234 build error** — TAMAMI parser test dosyalarından.
Parser dışı testler build ediliyor ama runtime'da 336 fail.

### Build Error Kaynağı

```
ParseAsync(stream, tenantId, ct) — yeni signature
Testler eski: ParseAsync(stream, ct) kullanıyor
```

**Etkilenen Dosyalar (11 parser test):**
- AmazonSettlementParserTests, CiceksepetiSettlementParserTests
- HepsiburadaSettlementParserTests, N11SettlementParserTests
- PazaramaSettlementParserTests, TrendyolSettlementParserTests
- OpenCartSettlementParserTests, OFXParserTests
- MT940ParserTests, Camt053ParserTests, RealFormatTests

**Fix:** Her test dosyasında `ParseAsync` çağrısına `tenantId` parametresi ekle.
**Sorumlu:** DEV 3 (parser sahibi) + DEV 5 (test düzeltme)

---

## 2. FIX PATTERN KATEGORİLERİ

### Pattern A: Mock Setup Eksikliği (NullRef) — ~80 fail

**Belirti:** `NullReferenceException` veya `Moq.MockException`
**Kök Neden:** Mock repository `GetByIdAsync` gibi metotlar setup edilmemiş
— `null` dönüyor, handler null referans alıyor.

**Fix Pattern:**
```csharp
// ÖNCE (eksik mock setup — NullRef):
var sut = new Handler(repo.Object, uow.Object);
await sut.Handle(new Command(id), CancellationToken.None);

// SONRA (mock setup eklendi):
repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new Entity { Id = id, TenantId = tenantId });
var sut = new Handler(repo.Object, uow.Object);
await sut.Handle(new Command(id), CancellationToken.None);
```

**Etkilenen Test Sınıfları (~80):**
- PlaceOrderEdgeCaseTests (6) — OrderRepository.GetByIdAsync
- BulkUpdateHandlerTests (7) — ProductRepository.GetByIdAsync
- OrderToStockDeductionChainTests (7) — ProductRepository batch
- AutoShipmentServiceTests (9) — StoreRepository.GetByTenantIdAsync
- AutoShipmentProdTests (9) — benzer pattern
- PlatformCommissionRateProviderTests (15) — CommissionRate entity
- Diğer handler testleri (~27)

### Pattern B: Handler İş Mantığı Değişikliği — ~40 fail

**Belirti:** `Moq.MockException: Expected invocation on the mock once, but was 0 times`
**Kök Neden:** Handler'ın çağırdığı method değişti (ör: `AddAsync` → `SaveChangesAsync`)

**Fix Pattern:**
```csharp
// ÖNCE (eski handler AddAsync çağırıyordu):
_repo.Verify(r => r.AddAsync(It.IsAny<Entity>(), ...), Times.Once);

// SONRA (handler artık UoW.SaveChangesAsync kullanıyor):
_uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
```

**Etkilenen Test Sınıfları (~40):**
- GLEventHandlerTests (5) — ✅ DÜZELTILDI (commit 1f60a983)
- GLDataLossRegressionTests (2) — ✅ DÜZELTILDI
- OrderToAccountingChainTests (7) — aynı pattern
- InvoiceHandlerTests (6) — BulkCreateInvoice
- CommandHandlerBatch8Tests (6)
- CqrsHandlerHardeningTests (5)
- Diğer (~9)

### Pattern C: Domain Entity API Değişikliği — ~30 fail

**Belirti:** `ArgumentException`, `InvalidOperationException`, assertion mismatch
**Kök Neden:** Entity factory method'ları veya property'leri değişti.

**Fix Pattern:**
```csharp
// ÖNCE (eski Order.Create):
var order = Order.Create(tenantId, "ORD-001");

// SONRA (yeni Order.CreateManual):
var order = Order.CreateManual(tenantId, customerId, "Müşteri", "email", "SALE");
```

**Etkilenen Test Sınıfları (~30):**
- EdgeCaseTests (NegativeAmount, BoundaryValue) — PricingService API
- ReturnRequest entity tests — Create method parametreleri
- Order entity tests — Place/Ship/Deliver method zinciri
- AccountingEdgeCaseDetailTests — KDV hesaplama

### Pattern D: Parser API Signature Değişikliği — 111 fail (BUILD ERROR)

**Fix Pattern:**
```csharp
// ÖNCE:
var result = await parser.ParseAsync(stream, CancellationToken.None);

// SONRA:
var tenantId = Guid.NewGuid();
var result = await parser.ParseAsync(stream, tenantId, CancellationToken.None);
```

**Toplu Sed Komutu (DEV 3 fix sonrası):**
```bash
# Her parser test dosyasında ParseAsync çağrısına tenantId ekle
sed -i 's/ParseAsync(stream, CancellationToken/ParseAsync(stream, Guid.NewGuid(), CancellationToken/g' \
  src/MesTech.Tests.Unit/Accounting/Parsers/*Tests.cs
```

### Pattern E: Idempotency/Filter Testleri — ~5 fail

**Belirti:** Cache mock setup eksik
**Kök Neden:** IdempotencyFilter artık `IDistributedCache` yerine
`IMemoryCache` kullanıyor.

**Fix Pattern:**
```csharp
// Mock cache response ekle
_cache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cachedValue))
    .Returns(true);
```

---

## 3. ÖNCELİK SIRASI

| Faz | Kategori | Fail | Zorluk | Sorumlu | Durum |
|-----|----------|------|--------|---------|-------|
| 0 | Parser BUILD FIX | 234 build err | Kolay | DEV 3 | BEKLENİYOR |
| 1 | Pattern B (Verify) | ~40 | Kolay | DEV 5 | KISMİ (%30) |
| 2 | Pattern A (Mock) | ~80 | Orta | DEV 5 | PLANLI |
| 3 | Pattern C (Entity) | ~30 | Zor | DEV 1+5 | PLANLI |
| 4 | Pattern D (Parser) | ~111 | Toplu sed | DEV 3+5 | Faz 0 sonrası |
| 5 | Pattern E (Filter) | ~5 | Kolay | DEV 5 | PLANLI |

---

## 4. BATCH FIX STRATEJİSİ

### Batch 1: Verify Pattern (Pattern B) — ~40 fail
```bash
# AddAsync verify → SaveChangesAsync verify
grep -rn "Verify(r => r.AddAsync" src/MesTech.Tests.Unit/ --include="*.cs" -l
# Her dosyada: AddAsync verify'ı → UoW.SaveChangesAsync verify'a çevir
```

### Batch 2: Mock Setup (Pattern A) — ~80 fail
```bash
# GetByIdAsync mock eksik — her test dosyasına entity dönen setup ekle
grep -rn "new.*Handler(" src/MesTech.Tests.Unit/ --include="*.cs" | \
  grep -v "NullLogger\|Mock.Of" | \
  # Her handler'ın constructor'daki repo'lar için mock setup ekle
```

### Batch 3: Entity API (Pattern C) — ~30 fail
```bash
# Order.Create → Order.CreateManual
# ReturnRequest.Create parametreleri güncelle
# PricingService method signature güncelle
```

### Batch 4: Parser API (Pattern D) — ~111 fail
```bash
# DEV 3 fix sonrası — toplu sed
sed -i 's/ParseAsync(stream, CancellationToken/ParseAsync(stream, tenantId, CancellationToken/g'
```

---

## 5. HEDEF TAKVİM

| Aşama | Fail | Kümülatif Pass % | Bağımlılık |
|-------|------|------------------|------------|
| Başlangıç | 336 | 96.4% | — |
| Faz 0 (Parser build fix) | -234 build err | — | DEV 3 |
| Faz 1 (Verify pattern) | -40 | 97.5% | — |
| Faz 2 (Mock setup) | -80 | 98.4% | — |
| Faz 3 (Entity API) | -30 | 98.7% | DEV 1 |
| Faz 4 (Parser runtime) | -111 | 99.8% | DEV 3 |
| Faz 5 (Filter) | -5 | **100%** | — |
