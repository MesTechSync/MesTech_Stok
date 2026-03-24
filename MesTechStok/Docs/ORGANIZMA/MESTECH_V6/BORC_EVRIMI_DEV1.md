# BORÇ EVRİM TABLOSU — DEV 1
Son güncelleme: 2026-03-24 TUR 4

| Borç Kalemi               | T1 Başlangıç | T1 Son | T4 Son | Trend    | Durum     |
|---------------------------|--------------|--------|--------|----------|-----------|
| EF Config gap             | 16           | 0      | 0      | ✅       | KAPANDI   |
| Guid.Empty (kritik bug)   | 5            | 0      | 0      | ✅       | KAPANDI   |
| Guid.Empty (toplam)       | 40           | 36     | 36     | ■ takılı | DEP-DEV3  |
| Empty catch (App)         | 2            | 0      | 0      | ✅       | KAPANDI   |
| Event handler coverage    | 32/50        | 50/50  | 50/50  | ✅       | KAPANDI   |
| Multi-tenant event TenantId| 0/50        | 8/50   | 50/50  | ✅       | KAPANDI   |
| NotImpl (Domain/App)      | 0            | 0      | 0      | ✅       | TEMİZ     |
| TODO/FIXME (Domain/App)   | 0            | 0      | 0      | ✅       | TEMİZ     |
| Core ref (DEV 1 alanı)   | 0            | 0      | 0      | ✅       | TEMİZ     |
| Lead.Score property       | EKSİK        | —      | EKSİK  | ■ yeni   | KEŞFEDİLDİ|
| Test build error          | 2 (pre-ex)   | 2      | 0      | ✅       | KAPANDI   |

## DARBOĞAZ ANALİZİ

Takılı kalemler:
  ⚠ Guid.Empty (toplam 36): 4 turdur 36'da
    → KÖK NEDEN: Kalan 36 Guid.Empty doğru kullanım (validator, JWT, defensive fallback)
      veya DEV 3 alanında (11 settlement parser backward-compat overload)
    → STRATEJİ: DEP — DEV 3 parser refactor yapmalı
    → DEV 1 müdahalesi YOK — doğru kullanım bırakılır

## PARKED KALEMLER
- Guid.Empty parser overload (36): DEP-DEV3 — doğru kullanım + DEV 3 parser backward-compat

## KEŞFEDİLEN YENİ ÖZELLİKLER
- **Lead.Score**: CRM Lead entity'de Score property yok. V4 Atlas'ta bahsedilmiş ama implement
  edilmemiş. Property + UpdateScore() metodu + LeadScoringService yazılabilir.

## BAĞIMLILIK (DEP) — başka DEV'lere
- Core ref 84: Desktop 37 (DEV 2), Core self 37 (DEV 2), Tests 6 (DEV 5)
- Guid.Empty parser overload: DEV 3 (Settlement/Banking)
- Handler-Validator gap 134: DEV 5 (test yazma)

## 10 COMMIT
1. `1eada055` fix(domain): GetSupplierPerformanceQuery TenantId
2. `54304be5` fix(domain): InvoiceCreatedEvent/OrderReceivedEvent TenantId
3. `c7713c4e` feat(persistence): 3 Dropship EF configs
4. `c6c8e8b0` feat(persistence): 13 Accounting EF configs
5. `f40ffcac` fix(app): empty catch → LogDebug
6. `700cef96` feat(app): 18 Application event handler services
7. `2be63561` fix(domain): 6 financial event TenantId
8. `27e0f1c2` fix(domain): 6 product/stock event TenantId
9. `2bf8a58b` fix(domain): 21 remaining event TenantId — %100 coverage
10. (bu commit) docs: BORC_EVRIMI güncellemesi
