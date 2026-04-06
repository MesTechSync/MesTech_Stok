# EMR-CAPRAZ-SIM-001 — DEV 5 TAM TARAMA RAPORU
# Tarih: 5 Nisan 2026
# Metod: 172 Avalonia view + 120+ menü öğesi + 10 seviye profesyonel standart

---

## MENÜ YAPISI — 25 BÖLÜM, 120+ ÖĞE

### 1. ANA SAYFA
| # | Menü | View | ViewModel | DURUM |
|---|------|------|-----------|-------|
| 1 | Hoş Geldiniz | AppHubView | AppHubViewModel | VAR |

### 2. DASHBOARD
| # | Menü | View | ViewModel | DURUM |
|---|------|------|-----------|-------|
| 1 | Kontrol Paneli | DashboardAvaloniaView | DashboardAvaloniaViewModel | VAR |

**10 SEVİYE ANALİZ:**
| Svy | Olması Gereken | Mevcut | Eksik |
|-----|---------------|--------|-------|
| 1 | Sayfa açılır | ✅ VAR | - |
| 2 | KPI kartları (ürün, sipariş, ciro, stok) | ✅ 7 ref | - |
| 3 | Gerçek API'den (demo değil) | ✅ MediatR Send | - |
| 4 | Platform ayrı kart (Trendyol/HB/N11) | ❌ 0 ref | **DEV4** |
| 5 | Mini sparkline grafik (7 gün trend) | ✅ 8 ref (LiveCharts) | - |
| 6 | Tarih filtresi (bugün/hafta/ay) | ⚠️ 1 ref | **DEV2** |
| 7 | Kritik uyarılar (düşük stok, bekleyen) | ✅ 5 ref | - |
| 8 | Hızlı aksiyonlar (yeni sipariş → git) | ❌ 0 ref | **DEV2** |
| 9 | Platform sağlık (API bağlı mı) | ⚠️ 1 ref | **DEV6** |
| 10 | Otomatik yenileme (30sn) | ✅ 19 ref (Timer) | - |

### 3. ÜRÜN YÖNETİMİ
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Ürün Listesi | ProductsAvaloniaView | VAR |
| 2 | Toplu İçe Aktarma | ImportProductsAvaloniaView | VAR |
| 3 | Toplu Ürün İşlem | BulkProductAvaloniaView | VAR |
| 4 | Ürün Varyant Matrisi | ProductVariantMatrixView | VAR |
| 5 | AI Ürün Açıklama | ProductDescriptionAIView | VAR |
| 6 | Ürün Çekme | ProductFetchAvaloniaView | VAR |

**10 SEVİYE ANALİZ (Ürün Listesi):**
| Svy | Olması Gereken | Mevcut | Eksik |
|-----|---------------|--------|-------|
| 1 | Ürün listesi açılır | ✅ VAR | - |
| 2 | Platformdan çekilir | ✅ 17 ref | - |
| 3 | Sayfalama (20/50/100) | ❌ 0 ref | **DEV2 P1** |
| 4 | Arama (barkod, ad, model, SKU) | ✅ 35 ref | - |
| 5 | Filtreler (kategori, platform, stok) | ✅ 10 ref | - |
| 6 | Sıralama (fiyat/stok/tarih ↑↓) | ❌ 0 ref | **DEV2 P2** |
| 7 | Toplu seçim + toplu işlem | ❌ 0 ref | **DEV2 P1** |
| 8 | Hızlı düzenleme (inline edit) | ❌ 0 ref | **DEV2 P2** |
| 9 | Ürün detay (görseller, varyantlar) | ❌ 0 ref | **DEV2 P2** |
| 10 | Export (Excel/CSV) + Import | ❌ 0 ref | **DEV2 P2** |

### 4. SİPARİŞ YÖNETİMİ
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Siparişler | OrdersAvaloniaView | VAR |
| 2 | Sipariş Listesi | OrderListAvaloniaView | VAR |
| 3 | Sipariş Detay | OrderDetailAvaloniaView | VAR |
| 4 | Sipariş Kanban | OrderKanbanView | VAR |
| 5 | Gecikmiş Siparişler | StaleOrdersAvaloniaView | VAR |

**10 SEVİYE ANALİZ (Sipariş Listesi):**
| Svy | Olması Gereken | Mevcut | Eksik |
|-----|---------------|--------|-------|
| 1 | Sipariş listesi | ✅ VAR | - |
| 2 | Tüm platformlar | ✅ 7 ref | - |
| 3 | Platform filtresi | ✅ 4 ref | - |
| 4 | Durum filtresi | ✅ 5 ref | - |
| 5 | Tarih filtresi | ✅ 22 ref | - |
| 6 | Sipariş detayına git | ❌ 0 ref (OrderListVM'de) | **DEV2 P1** |
| 7 | Kargo işlemi | ⚠️ 1 ref | **DEV2 P1** |
| 8 | Fatura kes (siparişten) | ❌ 0 ref | **DEV2 P1** |
| 9 | Toplu kargoya ver | ❌ 0 ref | **DEV2 P1** |
| 10 | Kargo etiketi yazdır | ❌ 0 ref | **DEV2 P2** |

### 5. STOK YÖNETİMİ
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Stok Takibi | StockAvaloniaView | VAR |
| 2 | Envanter | InventoryAvaloniaView | VAR |
| 3 | Stok Güncelleme | StockMovementAvaloniaView | VAR |
| 4 | Stok Yerleşim | StockPlacementAvaloniaView | VAR |
| 5 | Lot Ekleme | StockLotAvaloniaView | VAR |
| 6 | Depo Arası Transfer | StockTransferAvaloniaView | VAR |
| 7 | Stok Uyarıları | StockAlertAvaloniaView | VAR |
| 8 | Depo Yönetimi | WarehouseAvaloniaView | VAR |
| 9 | Stok Hareket Geçmişi | StockTimelineAvaloniaView | VAR |

**10 SEVİYE ANALİZ (Stok Takibi):**
| Svy | Olması Gereken | Mevcut | Eksik |
|-----|---------------|--------|-------|
| 1 | Stok listesi | ✅ VAR | - |
| 2 | Stok miktarı | ✅ 14 ref | - |
| 3 | Depo bazlı stok | ❌ 0 ref (StockVM) | **DEV1 P1** |
| 4 | Kritik stok uyarısı | ✅ 2 ref + ayrı StockAlertView | - |
| 5 | Stok hareketi ekle | ⚠️ 1 ref | - |
| 6 | Hareket geçmişi | ✅ 2 ref + StockTimelineView | - |
| 7 | Lot yönetimi | ❌ 0 ref (StockVM) ama StockLotView VAR | **DEV1 P2** |
| 8 | FEFO/FIFO | ❌ 0 ref | **DEV1 P2** |
| 9 | Platform sync | ✅ 5 ref | - |
| 10 | Stok raporu | ❌ 0 ref export | **DEV1 P2** |

### 6. KARGO YÖNETİMİ
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Kargo Takip | CargoTrackingAvaloniaView | VAR |
| 2 | Kargo Firmaları | CargoProvidersAvaloniaView | VAR |
| 3 | Toplu Gönderim | BulkShipmentAvaloniaView | VAR |
| 4 | Etiket Yazdır | LabelPreviewAvaloniaView | VAR |
| 5 | Kargo Ayarları | CargoSettingsAvaloniaView | VAR |

**10 SEVİYE ANALİZ:**
| Svy | Olması Gereken | Mevcut | Eksik |
|-----|---------------|--------|-------|
| 1 | Kargo firmaları listesi | ✅ VAR | - |
| 2 | 7 firma görünür | ✅ 19 ref | - |
| 3 | Bağlantı durumu | ✅ 4 ref | - |
| 4 | Sevkiyat oluştur | ⚠️ 1 ref (field only) | **DEV6 P0** |
| 5 | Takip no otomatik | ❌ 0 ref CargoProvidersVM | **DEV2** |
| 6 | Kargo etiketi PDF | ❌ 0 ref CargoProvidersVM ama LabelPreviewView VAR | ⚠️ |
| 7 | Toplu sevkiyat | ❌ 0 ref CargoProvidersVM ama BulkShipmentView VAR | ⚠️ |
| 8 | Kargo takip | ❌ 0 ref CargoProvidersVM ama CargoTrackingView VAR | ⚠️ |
| 9 | İade kargo | ❌ 0 ref | **DEV3 P2** |
| 10 | Maliyet raporu | ❌ 0 ref | **DEV1 P2** |

**NOT:** Seviye 6-8 ayrı view olarak MEVCUT ama CargoProvidersVM'den navigasyon bağlantısı yok.

### 7. FATURA / E-FATURA
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Fatura Yönetimi | InvoiceManagementAvaloniaView | VAR |
| 2 | Fatura Listesi | InvoiceListAvaloniaView | VAR |
| 3 | Fatura Oluştur | InvoiceCreateAvaloniaView | VAR |
| 4 | Toplu Fatura | BulkInvoiceAvaloniaView | VAR |
| 5 | Provider Ayarları | InvoiceProviderSettingsAvaloniaView | VAR |
| 6 | Fatura Raporları | InvoiceReportAvaloniaView | VAR |
| 7 | Fatura PDF | InvoicePdfAvaloniaView | VAR |
| 8 | E-Fatura | EInvoiceAvaloniaView | VAR |

**10 SEVİYE:** Fatura bölümü EN OLGUN bölüm (7/10).

### 8. MAĞAZA YÖNETİMİ
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Mağaza Ekle Wizard | StoreWizardAvaloniaView | VAR |
| 2 | Mağaza Detay | StoreDetailAvaloniaView | VAR |
| 3 | Mağaza Ayarları | StoreSettingsAvaloniaView | VAR |
| 4 | Mağaza Yönetimi | StoreManagementAvaloniaView | VAR |

**10 SEVİYE ANALİZ:**
| Svy | Olması Gereken | Mevcut | Eksik |
|-----|---------------|--------|-------|
| 1-3 | Liste + görünüm + durum | ✅ VAR | - |
| 4 | Yeni mağaza ekle | ✅ StoreWizardView VAR | - |
| 5 | API credential | ⚠️ 1 ref | - |
| 6 | Bağlantı test | ❌ 0 ref StoreManagementVM | **DEV2 P0** |
| 7 | Kaydet aktif | ❌ 0 ref | **DEV2 P0** |
| 8 | Düzenle | ❌ 0 ref | **DEV2 P0** |
| 9 | Sil | ❌ 0 ref | **DEV2 P0** |
| 10 | Mağaza seçici | ⚠️ 1 ref | - |

### 9. KATEGORİ EŞLEŞTİRME
| # | Menü | View | DURUM |
|---|------|------|-------|
| 1 | Kategoriler | CategoryAvaloniaView | VAR |
| 2 | Kategori Eşleştirme | CategoryMappingAvaloniaView | VAR |

**10 SEVİYE:** Eşleştirme arayüzü var (20 ref) ama kaydet (0), attribute (0), komisyon (0) eksik.

### 10-25. DİĞER BÖLÜMLER

| Bölüm | Menü Sayısı | Olgunluk |
|-------|------------|----------|
| CRM | 4 menü (Leads, Contacts, Kanban, Customers) | ⚠️ View var, iş mantığı sınırlı |
| Platform (15 pazaryeri) | 15 menü | ✅ View'lar var |
| Dropshipping | 6 menü | ⚠️ View var, entegrasyon sınırlı |
| Fulfillment | 4 menü | ⚠️ View var |
| ERP | 2 menü | ⚠️ View var |
| Muhasebe | 10 menü | ✅ Zengin (KDV, Mizan, K/Z, Mutabakat) |
| Finans | 7 menü | ✅ İyi (K/Z, gider, banka, nakit akış) |
| Raporlar | 5 menü | ⚠️ Grafik eksik |
| Sistem | 5 menü (Health, Log, Audit, MFA, Backup) | ✅ İyi |
| Araçlar | 4 menü (Kampanya, Export, Barkod, AI) | ⚠️ Kısmi |

---

## TOPLAM EKSİK ÖZETİ

### P0 — KRİTİK (temel işlev çalışmıyor)
| ID | Bölüm | Eksik | Sorumlu DEV |
|----|-------|-------|-------------|
| HH-DEV2-006 | Mağaza | Test bağlantı + Kaydet + Düzenle + Sil butonları YOK | DEV2 |
| HH-DEV6-001 | Kargo | Sevkiyat oluşturma akışı endpoint YOK | DEV6 |
| HH-DEV3-001 | Stok | Platform stok push (Trendyol'a stok gönderme) | DEV3 |

### P1 — ÖNEMLİ (kısmi işlev)
| ID | Bölüm | Eksik | Sorumlu DEV |
|----|-------|-------|-------------|
| HH-DEV2-001 | Ürün | Sayfalama YOK (9103 ürün gösterilemiyor) | DEV2 |
| HH-DEV2-003 | Ürün | Toplu seçim + toplu işlem YOK | DEV2 |
| HH-DEV2-005 | Sipariş | Detay navigasyon + fatura kes + toplu kargo YOK | DEV2 |
| HH-DEV2-007 | Kargo | TrackingNo/Etiket/TopluSevk/Takip/İade/Maliyet | DEV2 |
| HH-DEV2-008 | Fatura | Detay popup + komisyon + mutabakat | DEV2 |
| HH-DEV2-011 | Kategori | Dropdown eşleştirme + kaydet | DEV2 |
| HH-DEV1-001 | Stok | Depo bazlı stok görüntüleme | DEV1 |
| HH-DEV4-001 | Dashboard | Platform ayrı KPI kartları | DEV4 |
| HH-DEV2-016 | Dashboard | Tarih filtresi + Hızlı aksiyonlar | DEV2 |

### P2 — GELİŞTİRME (kalite artırma)
| ID | Bölüm | Eksik | Sorumlu DEV |
|----|-------|-------|-------------|
| HH-DEV2-002 | Ürün | Sıralama (fiyat/stok/tarih) | DEV2 |
| HH-DEV2-004 | Ürün | Inline edit + detay popup + export | DEV2 |
| HH-DEV2-009 | Raporlar | Stok/sipariş raporu + grafik + platform filtre | DEV2 |
| HH-DEV2-010 | Ayarlar | Rol/kargo/fatura ayarları + yedekleme + lisans | DEV2 |
| HH-DEV1-002 | Stok | Lot/FEFO + stok raporu export | DEV1 |
| HH-DEV4-002 | Dashboard | Hızlı aksiyonlar panel | DEV4 |

---

## DEV DAĞILIMI ÖZETİ

| DEV | P0 | P1 | P2 | TOPLAM |
|-----|----|----|----|----|
| DEV1 | 0 | 1 | 2 | 3 |
| DEV2 | 1 | 7 | 4 | 12 |
| DEV3 | 1 | 0 | 0 | 1 |
| DEV4 | 0 | 1 | 1 | 2 |
| DEV5 | RAPOR TUTUCU | - | - | - |
| DEV6 | 1 | 0 | 0 | 1 |
| DEV7 | 18 SENARYO GÖRSEL TEST | - | - | - |

---

## BÖLÜM OLGUNLUK PUANI

| Bölüm | Puan | Seviye |
|-------|------|--------|
| Dashboard | 7/10 | ⭐⭐⭐ İyi |
| Ürün Yönetimi | 4/10 | ⭐⭐ Zayıf |
| Sipariş Yönetimi | 5/10 | ⭐⭐ Zayıf |
| Stok Yönetimi | 5/10 | ⭐⭐ Zayıf |
| Kargo Yönetimi | 3/10 | ⭐ Kritik |
| Fatura / E-Fatura | 7/10 | ⭐⭐⭐ İyi |
| Mağaza Yönetimi | 4/10 | ⭐⭐ Zayıf |
| Kategori Eşleştirme | 4/10 | ⭐⭐ Zayıf |
| Raporlar | 3/10 | ⭐ Kritik |
| Ayarlar | 4/10 | ⭐⭐ Zayıf |
| **ORTALAMA** | **4.6/10** | **⭐⭐ Zayıf** |

**Profesyonel hedef: 8/10 — mevcut: 4.6/10**
