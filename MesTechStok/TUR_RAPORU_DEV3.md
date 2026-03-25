# DEV 3 — Entegrasyon & Adapter TUR RAPORU

## TUR 1 — 2026-03-25

### BİLİM ADAMI TARAMA
| Metrik | Değer |
|--------|-------|
| Build error | 0 |
| NotImplementedException | 0 |
| GetCategories stub | 0 |
| Gerçek boş catch | 0 |
| PII log sızıntısı | 25 |
| Guid.Empty placeholder | 11 |
| TODO | 1 (DEV 1 alanı) |
| Orphan event | 0 |
| Toplam dosya (Integration+Jobs+Messaging) | 242 .cs |

### CERRAH AMELİYAT
| # | Dosya | İşlem | Commit |
|---|-------|-------|--------|
| 1 | PiiLogMaskHelper.cs | MaskEmail/MaskTaxNumber/MaskPhone helper | 7529c2a7 |
| 2 | OpenCartAdapter.cs | Email maskeleme 2 log | 89220994 |
| 3 | 9 Invoice Provider | TaxNumber maskeleme 23 log | d6726854 |

### MÜHENDİS DELTA
| Metrik | ÖNCE | SONRA | DELTA |
|--------|------|-------|-------|
| PII log sızıntısı | 25 | 0 | -25 ✅ |

---

## TUR 2 — 2026-03-25

### BİLİM ADAMI TARAMA (DERİN)
| Metrik | Değer |
|--------|-------|
| Settlement mapping gap | 1 (eBay) |
| Logger eksik handler | 1 (LeadConvertedBridgeHandler) |
| Guid.Empty tenant fallback (sessiz) | 1 |
| Adapter stub method | 0 |
| CancellationToken eksik | 0 |
| DI registration eksik | 0 |
| Retry pattern | tüm adapter'larda mevcut |

### CERRAH AMELİYAT
| # | Dosya | İşlem | Commit |
|---|-------|-------|--------|
| 4 | EbaySettlementMapping.cs | eBay Finances API mapping modeli | 73f38b6e |
| 5 | LeadConvertedBridgeHandler.cs | Logger + Guid.Empty uyarısı | a5e44a41 |

### MÜHENDİS DELTA
| Metrik | ÖNCE | SONRA | DELTA |
|--------|------|-------|-------|
| Settlement mapping gap | 1 | 0 | -1 ✅ |
| Logger eksik handler | 1 | 0 | -1 ✅ |

### DEP (Bağımlılık — başka DEV gerekiyor)
- DEP-DEV1: `InvoiceSentEvent`'e `OrderId` eklenmeli → `MesaBridgeHandlers.cs:191` Guid.Empty düzelir
- DEP-DEV1: `SystemUserId` Domain sabiti tanımlanmalı → `ScheduledReportGenerationJob.cs:100` Guid.Empty düzelir
- DEP-DEV1: Settlement parser overload'lardaki Guid.Empty (7 adet) — tenant zorunluluğu mimarisi kararı

### KALAN BORÇ
| Kalem | Sayı | Durum |
|-------|------|-------|
| PII sızıntı | 0 | KAPANDI |
| NotImplementedException | 0 | TEMİZ |
| GetCategories stub | 0 | TEMİZ |
| Boş catch | 0 | TEMİZ |
| Orphan event | 0 | TEMİZ |
| Settlement gap | 0 | KAPANDI |
| Logger eksik | 0 | KAPANDI |
| Guid.Empty (DEP) | 11 | DEV 1 BAĞIMLI |
| TODO | 1 | DEV 1 BAĞIMLI |

### KARAR
DEV 3 alanı temiz. Kalan borçlar DEV 1 bağımlı (Domain değişikliği gerekiyor).
Durum: **KEŞİF FAZINA GEÇTİ**

---

## TUR 3 — 2026-03-25 (KEŞİF FAZI)

### BİLİM ADAMI — CAPABILITY GAP ANALİZİ
| Metrik | Değer |
|--------|-------|
| Build error | 0 |
| IShipmentCapable gap | 5 (Trendyol, AmazonTr, AmazonEu, Ozon, PttAvm — SupportsShipment=true ama interface yok) |
| Orphan event | 0 |
| PII sızıntı | 0 |

### CERRAH AMELİYAT
| # | Dosya | İşlem | Commit |
|---|-------|-------|--------|
| 7 | TrendyolAdapter.cs | IShipmentCapableAdapter + SendShipmentAsync | e56f31f6 |
| 8 | AmazonTrAdapter.cs + AmazonEuAdapter.cs | IShipmentCapableAdapter + SP-API feed | 22007bee |
| 9 | OzonAdapter.cs + PttAvmAdapter.cs | IShipmentCapableAdapter | bf50a103 |

### MÜHENDİS DELTA
| Metrik | ÖNCE | SONRA | DELTA |
|--------|------|-------|-------|
| IShipmentCapable adapter | 7 | 12 | +5 ✅ |
| SupportsShipment=true w/o interface | 5 | 0 | -5 ✅ |

### KARAR
Tüm `SupportsShipment => true` adapter'lar artık `IShipmentCapableAdapter` implemente ediyor.
Sonraki hedef: ISettlementCapable gap (HB, ÇS, Ozon, PttAvm, Shopify, WooCommerce eksik) veya başka DEV.
