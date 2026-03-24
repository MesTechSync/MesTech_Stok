# BORÇ EVRİM TABLOSU — DEV 1
Son güncelleme: 2026-03-24 TUR 1

| Borç Kalemi          | T1 Başlangıç | T1 Son | Trend    | Durum     |
|----------------------|--------------|--------|----------|-----------|
| EF Config gap        | 16           | 0      | ↓↓ hızlı | KAPANDI   |
| Guid.Empty (kritik)  | 5 bug        | 0 bug  | ↓↓ hızlı | KAPANDI   |
| Guid.Empty (toplam)  | 40           | 36     | ↓ normal | AKTİF*   |
| Orphan Event         | 50→0 (YP)   | 0      | ✅       | YP        |
| Core ref (DEV 1)     | 0            | 0      | ✅       | TEMİZ     |
| NotImpl (Domain/App) | 0            | 0      | ✅       | TEMİZ     |
| TODO/FIXME           | 0            | 0      | ✅       | TEMİZ     |

*Guid.Empty 36 kalan: 5 validator (doğru), 4 JWT (doğru), 11 parser overload (DEV 3),
4 bank parser (tasarım), 12 diğer (defensive fallback/system user/MESA bridge)

## Hız Analizi
- En hızlı kapanan: EF Config gap (1 turda 16→0 = 16/tur)
- Tüm kritik borçlar 1 turda kapandı

## PARKED KALEMLER
- Yok — tüm kalemler aktif veya kapandı

## BAĞIMLILIK (DEP) — başka DEV'lere
- Core ref 84 toplam: Desktop 37 (DEV 2), Core self 37 (DEV 2), Tests 6 (DEV 5)
- Guid.Empty parser overload: DEV 3 (Settlement/Banking)
- Handler-Validator gap 134: DEV 5 (test yazma)
- Desktop build 57 error: DEV 2 (Core service interface eksik)

## 4 COMMIT
1. `1eada055` fix(domain): GetSupplierPerformanceQuery TenantId [ENT-DEV1]
2. `54304be5` fix(domain): InvoiceCreatedEvent/OrderReceivedEvent TenantId [ENT-DEV1]
3. `c7713c4e` feat(persistence): 3 Dropship EF configs [ENT-DEV1]
4. `c6c8e8b0` feat(persistence): 13 Accounting EF configs [ENT-DEV1]
