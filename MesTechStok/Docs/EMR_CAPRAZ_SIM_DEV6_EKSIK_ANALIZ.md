# EMR-CAPRAZ-SIM-001 — DEV 6 EKSİK ANALİZ RAPORU
# Avalonia 10 Bölüm × 10+ Seviye Profesyonel Karşılaştırma
# Tarih: 5 Nisan 2026 | Hazırlayan: DEV 6 (Rapor Tutucu)

---

## METODOLOJİ

1. 172 Avalonia view dosyası tarandı (grep/find/wc)
2. MainWindowViewModel.cs'den 140+ menü öğesi çıkarıldı
3. Profesyonel platformlarla karşılaştırıldı:
   - Trendyol Seller Panel (14 özellik)
   - Multi-Channel Integration (ikas, Sentos, ChannelAdvisor — 14 özellik)
   - E-Ticaret Stok Yönetimi (14 özellik)
   - Sipariş Yönetimi OMS (14 özellik)
   - Kargo Entegrasyonu (14 özellik)
   - Türkiye Muhasebe (e-Fatura, KDV — 15 özellik)
4. **TOPLAM 85 profesyonel özellik** ile MesTech karşılaştırıldı

---

## BÖLÜM 1: DASHBOARD (Kontrol Paneli)

### Ne Olmalı (Profesyonel Standart)
| # | Özellik | Kaynak |
|---|---------|--------|
| 1 | KPI kartları (ciro, sipariş, stok, kar) | Trendyol/ikas |
| 2 | Platform bazlı ayrı kartlar (Trendyol/HB/N11 ayrı) | ChannelAdvisor |
| 3 | 7/30 günlük trend grafiği (sparkline) | Shopify Admin |
| 4 | Tarih filtresi (bugün/hafta/ay/özel) | Tüm platformlar |
| 5 | Kritik uyarılar (düşük stok, bekleyen sipariş, API hatası) | Sentos |
| 6 | Hızlı aksiyonlar (yeni sipariş → siparişlere git) | ikas |
| 7 | Platform sağlık göstergesi (API bağlı mı?) | ChannelAdvisor |
| 8 | Otomatik yenileme (30sn) | Linnworks |
| 9 | Satış performans skorları (ODR, iptal oranı) | Trendyol |
| 10 | Son aktivite akışı (son 10 sipariş/stok değişikliği) | Shopify |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1 | ✅ VAR | AccountingDashboardAvaloniaView.axaml: 4 KPI kart (Gelir/Gider/Kar/Bakiye) |
| 2 | ✅ VAR | ViewModel: summary.SalesByPlatform iterasyonu |
| 3 | ❌ YOK | SkeletonLoader ShowChart=True ama gerçek chart control yok |
| 4 | ❌ YOK | Hardcoded DateTime.Now, DatePicker yok |
| 5 | ❌ YOK | Alert/Warning bileşeni yok |
| 6 | ✅ VAR | Yenile butonu + F5 KeyBinding |
| 7 | ❌ YOK | Platform health indicator yok |
| 8 | ❌ YOK | DispatcherTimer/AutoRefresh yok |
| 9 | ❌ YOK | Performans skorkartı (ODR, iptal) yok |
| 10 | ❌ YOK | Son aktivite akışı yok |

**SKOR: 3/10 (30%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-D01 | DEV2 | P1 | Dashboard sparkline/chart ekle (LiveCharts veya OxyPlot) |
| HH-DEV2-D02 | DEV2 | P2 | Dashboard tarih filtresi (DatePicker) ekle |
| HH-DEV2-D03 | DEV2 | P1 | Dashboard kritik uyarı banner'ı (düşük stok, bekleyen sipariş) |
| HH-DEV2-D04 | DEV2 | P2 | Platform sağlık göstergesi (yeşil/kırmızı nokta) |
| HH-DEV2-D05 | DEV2 | P2 | Dashboard otomatik yenileme (DispatcherTimer 30sn) |
| HH-DEV1-D01 | DEV1 | P2 | Dashboard performans skorkartı query (ODR, iptal oranı) |
| HH-DEV2-D06 | DEV2 | P2 | Son aktivite akışı (son 10 olay listesi) |

---

## BÖLÜM 2: ÜRÜN YÖNETİMİ

### Ne Olmalı (Profesyonel Standart)
| # | Özellik | Kaynak |
|---|---------|--------|
| 1 | Ürün listesi (tüm platformlardan) | Temel |
| 2 | Sayfalama (20/50/100 per page) | Trendyol |
| 3 | Arama (barkod, ürün adı, model kodu, stok kodu) | Trendyol |
| 4 | Filtreler (kategori, stok durumu, platform, onay durumu) | Trendyol |
| 5 | Sıralama (fiyat ↑↓, stok ↑↓, tarih ↑↓) | ikas |
| 6 | Toplu seçim + toplu işlem (stok/fiyat güncelle, platforma gönder, pasife al) | Trendyol |
| 7 | Hızlı düzenleme (inline edit — stok ve fiyat) | Sentos |
| 8 | Ürün detay popup/panel (tam özellik, görseller, varyantlar) | Trendyol |
| 9 | Export (Excel/CSV) + Import (Excel şablonuyla) | Tüm platformlar |
| 10 | Varyant matrisi (renk × beden tablosu) | Trendyol |
| 11 | Ürün görselleri yönetimi (sürükle-bırak, sıralama) | Shopify |
| 12 | Platform bazlı fiyat/stok override | ChannelAdvisor |
| 13 | Ürün klonlama (mevcut üründen yeni oluştur) | ikas |
| 14 | Ürün onay durumu (Trendyol onay bekleyen, reddedilen) | Trendyol |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1 | ✅ VAR | ProductsAvaloniaView.axaml: DataGrid + Grid/List toggle |
| 2 | ❌ YOK | Hardcoded limit=50, PageSize/NextPage yok |
| 3 | ✅ VAR | SearchBox + multi-field smart search (ViewModel:86-105) |
| 4 | ✅ VAR | Platform ComboBox, OutOfStock/LowStock/Discounted toggle |
| 5 | ❌ YOK | CanUserSortColumns=True ama explicit sort binding yok |
| 6 | ❌ YOK | Checkbox column yok, bulk action yok |
| 7 | ❌ YOK | DataGrid IsReadOnly=True |
| 8 | ❌ YOK | ShowDetail command yok |
| 9 | ❌ YOK | Export/Import butonları yok |
| 10 | ✅ VAR | ProductVariantMatrixView.axaml mevcut |
| 11 | ❌ YOK | Görsel yönetim UI yok |
| 12 | ❌ YOK | Platform bazlı fiyat override yok |
| 13 | ❌ YOK | Klonlama özelliği yok |
| 14 | ❌ YOK | Onay durumu filtresi/göstergesi yok |

**SKOR: 4/14 (29%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-P01 | DEV2 | P0 | Ürün sayfalama (PageSize dropdown + Next/Prev) |
| HH-DEV2-P02 | DEV2 | P1 | Toplu seçim checkbox + toplu işlem toolbar |
| HH-DEV2-P03 | DEV2 | P2 | Inline edit (stok/fiyat çift tıkla düzenle) |
| HH-DEV2-P04 | DEV2 | P1 | Ürün detay panel/dialog |
| HH-DEV2-P05 | DEV2 | P2 | Export (Excel/CSV) + Import butonları |
| HH-DEV2-P06 | DEV2 | P2 | Ürün görselleri yönetim paneli |
| HH-DEV6-P01 | DEV6 | P2 | Platform bazlı fiyat override endpoint |
| HH-DEV1-P01 | DEV1 | P2 | Ürün klonlama command handler |
| HH-DEV3-P01 | DEV3 | P2 | Platform onay durumu adapter entegrasyonu |

---

## BÖLÜM 3: SİPARİŞ YÖNETİMİ

### Ne Olmalı (Profesyonel Standart)
| # | Özellik | Kaynak |
|---|---------|--------|
| 1 | Tüm platformlardan sipariş listesi | Temel |
| 2 | Platform filtresi | Temel |
| 3 | Durum filtresi (Bekleyen/Hazırlanıyor/Kargoda/Teslim/İptal) | Trendyol |
| 4 | Tarih filtresi (bugün/hafta/özel aralık) | Trendyol |
| 5 | Sipariş detayı (müşteri, adres, ürünler, fiyat) | Temel |
| 6 | Kargo işlemi (firma seç, takip no gir, gönder) | Trendyol |
| 7 | Siparişten fatura kes (otomatik e-fatura) | Türkiye zorunlu |
| 8 | Toplu kargoya ver (seçili siparişleri toplu işle) | ikas |
| 9 | Kargo etiketi yazdır (PDF/ZPL termal) | Trendyol |
| 10 | İptal yönetimi (iptal onay/red, stok iade) | Trendyol |
| 11 | İade yönetimi (iade talebi, inceleme, onay/red) | Trendyol |
| 12 | Sipariş notları (müşteri ile iletişim) | Shopify |
| 13 | Sipariş Kanban (sürükle-bırak durum değiştir) | Sentos |
| 14 | Sipariş performans (işleme süresi, SLA) | ChannelAdvisor |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1 | ✅ VAR | OrdersAvaloniaView.axaml: DataGrid |
| 2 | ✅ VAR | Platform kolonu mevcut |
| 3 | ✅ VAR | Status ComboBox filtre |
| 4 | ❌ YOK | DatePicker yok |
| 5 | ❌ YOK | ShowDetail command yok (OrderDetailAvaloniaView.axaml VAR ama bağlantı yok) |
| 6 | ❌ YOK | Kargo butonu sipariş listesinde yok |
| 7 | ❌ YOK | CreateInvoice butonu yok |
| 8 | ❌ YOK | Toplu kargo butonu yok (BulkShipmentAvaloniaView.axaml VAR ama bağlantı?) |
| 9 | ❌ YOK | Print/Label butonu sipariş listesinde yok |
| 10 | ❌ YOK | İptal işlemi UI yok |
| 11 | ✅ VAR | ReturnListAvaloniaView.axaml + ReturnDetailAvaloniaView.axaml |
| 12 | ❌ YOK | Sipariş notları alanı yok |
| 13 | ✅ VAR | OrderKanbanView.axaml mevcut |
| 14 | ✅ VAR | StaleOrdersAvaloniaView.axaml (gecikmiş sipariş izleme) |

**SKOR: 5/14 (36%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-O01 | DEV2 | P1 | Sipariş detay navigasyonu (tıkla → OrderDetail aç) |
| HH-DEV2-O02 | DEV2 | P0 | Sipariş listesinden kargo gönder akışı |
| HH-DEV2-O03 | DEV2 | P1 | Siparişten fatura kes butonu |
| HH-DEV2-O04 | DEV2 | P1 | Toplu kargo (checkbox + toplu gönder) |
| HH-DEV2-O05 | DEV2 | P2 | Tarih filtresi (DatePicker) |
| HH-DEV2-O06 | DEV2 | P2 | İptal yönetimi UI (iptal onay/red) |
| HH-DEV2-O07 | DEV2 | P2 | Sipariş notları alanı |

---

## BÖLÜM 4: STOK YÖNETİMİ

### Ne Olmalı (Profesyonel Standart)
| # | Özellik | Kaynak |
|---|---------|--------|
| 1 | Stok listesi | Temel |
| 2 | Stok miktarı görünür | Temel |
| 3 | Depo bazlı stok | Profesyonel |
| 4 | Kritik stok uyarısı (min altı kırmızı) | Trendyol |
| 5 | Stok hareketi ekle (giriş/çıkış/transfer/düzeltme) | ERP |
| 6 | Hareket geçmişi | ERP |
| 7 | Lot yönetimi (SKT, lot no) | FEFO/FIFO |
| 8 | FEFO/FIFO kuralları | Profesyonel |
| 9 | Platform stok senkronizasyonu butonu | Multi-channel |
| 10 | Stok raporu (PDF/Excel) | Temel |
| 11 | Stok sayım (barkod tarayıcı ile) | ERP |
| 12 | Ölü stok / yaşlandırma analizi | ChannelAdvisor |
| 13 | Stok rezervasyonu (sipariş için) | Profesyonel |
| 14 | Stok tahmini (AI demand forecast) | İleri |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1 | ✅ VAR | StockAvaloniaView.axaml |
| 2 | ✅ VAR | InventoryAvaloniaView.axaml: "Miktar" kolonu |
| 3 | ✅ VAR | Depo ComboBox filtre |
| 4 | ✅ VAR | StockAlertAvaloniaView.axaml: 3 seviye renk kodlu |
| 5 | ✅ VAR | AddMovementCommand butonu |
| 6 | ✅ VAR | StockTransferAvaloniaView: Transfer Geçmişi DataGrid |
| 7 | ✅ VAR | StockLotAvaloniaView.axaml: Lot formu |
| 8 | ⚠️ KISMI | SKT DatePicker var ama FEFO algoritma UI yok |
| 9 | ❌ YOK | PlatformSync butonu stok viewlarında YOK |
| 10 | ✅ VAR | Reports/StockValueReportView: Excel+PDF export |
| 11 | ✅ VAR | BarcodeReaderView.axaml: Sayım/Giriş/Çıkış |
| 12 | ❌ YOK | Yaşlandırma analizi (30/60/90/180 gün) yok |
| 13 | ❌ YOK | Stok rezervasyonu UI yok |
| 14 | ⚠️ KISMI | MockStockPredictionService var ama UI yok |

**SKOR: 9/14 (64%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV6-S01 | DEV6 | P0 | Platform stok sync endpoint + UI butonu bağlantısı |
| HH-DEV2-S01 | DEV2 | P2 | FEFO/FIFO sıralama UI göstergesi |
| HH-DEV1-S01 | DEV1 | P2 | Stok yaşlandırma analizi query handler |
| HH-DEV2-S02 | DEV2 | P2 | Stok tahmin dashboard (AI prediction görselleştirme) |

---

## BÖLÜM 5: KARGO YÖNETİMİ

### Ne Olmalı (Profesyonel Standart)
| # | Özellik | Kaynak |
|---|---------|--------|
| 1 | Kargo firmaları listesi | Temel |
| 2 | Bağlantı durumu (yeşil/kırmızı) | Temel |
| 3 | Sevkiyat oluştur | Temel |
| 4 | Takip no otomatik alınır | Trendyol |
| 5 | Kargo etiketi PDF | Trendyol |
| 6 | Toplu sevkiyat | ikas |
| 7 | Kargo takip (durum sorgula) | Temel |
| 8 | İade kargo | Trendyol |
| 9 | Kargo maliyet raporu | Profesyonel |
| 10 | Desi hesaplama (volumetrik ağırlık) | Kargo firmalar |
| 11 | Kargo performans analizi (teslimat oranı, süre) | ChannelAdvisor |
| 12 | Kargo manifest (günlük toplam) | Lojistik |
| 13 | Adres doğrulama (il/ilçe/mahalle) | Profesyonel |
| 14 | Çoklu koli (multi-package per order) | Profesyonel |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1 | ✅ VAR | CargoProvidersAvaloniaView.axaml |
| 2 | ✅ VAR | ConnectionStatusColor Ellipse |
| 3 | ✅ VAR | CreateShipmentCommand butonu |
| 4 | ✅ VAR | CargoSettings: otomatik güncelle toggle |
| 5 | ✅ VAR | LabelPreviewAvaloniaView: Yazdır+İndir |
| 6 | ✅ VAR | BulkShipmentAvaloniaView: checkbox+toplu gönder |
| 7 | ✅ VAR | CargoTrackingAvaloniaView: firma filtre+DataGrid |
| 8 | ❌ YOK | İade kargo UI yok |
| 9 | ❌ YOK | Maliyet raporu yok |
| 10 | ❌ YOK | Desi hesaplama aracı yok |
| 11 | ❌ YOK | Performans analizi yok |
| 12 | ❌ YOK | Günlük manifest yok |
| 13 | ❌ YOK | Adres doğrulama yok |
| 14 | ❌ YOK | Çoklu koli desteği yok |

**SKOR: 7/14 (50%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV3-K01 | DEV3 | P1 | İade kargo oluşturma adapter entegrasyonu |
| HH-DEV2-K01 | DEV2 | P1 | İade kargo UI (firma seç, iade etiket) |
| HH-DEV1-K01 | DEV1 | P2 | Kargo maliyet raporu query handler |
| HH-DEV2-K02 | DEV2 | P2 | Desi hesaplama aracı (en×boy×yükseklik → desi) |
| HH-DEV3-K02 | DEV3 | P2 | Kargo performans analizi (teslimat oranı API) |

---

## BÖLÜM 6: FİNANS / FATURA

### Ne Olmalı (Profesyonel Standart — Türkiye)
| # | Özellik | Kaynak |
|---|---------|--------|
| 1 | Fatura listesi | Temel |
| 2 | Fatura oluştur (siparişten otomatik) | Zorunlu |
| 3 | E-fatura gönder (GİB UBL-TR 1.2) | Yasal zorunluluk |
| 4 | E-arşiv fatura | Yasal zorunluluk |
| 5 | İptal faturası | Zorunlu |
| 6 | Komisyon takibi (platform kesintileri) | Profesyonel |
| 7 | Mutabakat (hakediş vs fatura eşleşme) | Profesyonel |
| 8 | KDV hesaplama ve raporlama | Zorunlu |
| 9 | BA-BS form verisi | Zorunlu (aylık) |
| 10 | Gelir-gider takibi (P&L) | ERP |
| 11 | Cari hesap (tedarikçi/müşteri bakiye) | ERP |
| 12 | Toplu fatura (günlük siparişler → toplu kes) | Profesyonel |
| 13 | Vergi takvimi hatırlatıcı | Profesyonel |
| 14 | ERP entegrasyonu (Parasüt/Logo push) | Profesyonel |
| 15 | Denetim izi (10 yıl saklama) | Yasal zorunluluk |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1 | ✅ VAR | InvoiceListAvaloniaView.axaml: 6 kolon DataGrid |
| 2 | ✅ VAR | InvoiceCreateAvaloniaView.axaml: 3 adımlı wizard |
| 3 | ✅ VAR | EInvoiceAvaloniaView.axaml + InvoiceManagement |
| 4 | ⚠️ KISMI | E-arşiv altyapı var ama PDF indirme butonu eksik |
| 5 | ✅ VAR | İptal Et butonu (CancelInvoiceCommand) |
| 6 | ✅ VAR | KomisyonAvaloniaView: platform KPI + DataGrid |
| 7 | ✅ VAR | MutabakatAvaloniaView + SettlementAvaloniaView |
| 8 | ✅ VAR | KdvRaporAvaloniaView.axaml |
| 9 | ✅ VAR | BaBsEndpoints mevcut (UI: AccountingEndpoints üzerinden) |
| 10 | ✅ VAR | GelirGiderAvaloniaView + IncomeExpenseDashboardView |
| 11 | ✅ VAR | CariHesaplarAvaloniaView.axaml |
| 12 | ✅ VAR | BulkInvoiceAvaloniaView.axaml |
| 13 | ✅ VAR | VergiTakvimiAvaloniaView.axaml |
| 14 | ✅ VAR | ErpDashboardView + ErpAccountMappingView + ErpSettingsAvaloniaView |
| 15 | ✅ VAR | AuditLogAvaloniaView.axaml |

**SKOR: 14/15 (93%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-F01 | DEV2 | P2 | E-arşiv PDF indirme butonu fatura listesine ekle |

---

## BÖLÜM 7: MAĞAZA YÖNETİMİ

### Ne Olmalı
| # | Özellik |
|---|---------|
| 1 | Mağaza listesi |
| 2 | Bağlantı durumu |
| 3 | Yeni mağaza ekle wizard |
| 4 | API credential masked |
| 5 | Bağlantı test et |
| 6 | Test başarılı → Kaydet aktif |
| 7 | Düzenle |
| 8 | Sil (soft delete + onay) |
| 9 | Mağaza seçici (aktif mağaza) |
| 10 | Platform bazlı ayarlar |

### MesTech Durumu
| # | Durum | Kanıt |
|---|-------|-------|
| 1-7 | ✅ VAR | StoreManagement + StoreWizard + StoreSettings + StoreDetail |
| 8 | ❌ YOK | Sil butonu listede yok |
| 9 | ✅ VAR | Platform seçici ComboBox |
| 10 | ✅ VAR | StoreSettings platform bazlı |

**SKOR: 9/10 (90%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-M01 | DEV2 | P2 | Mağaza sil butonu + onay dialog |

---

## BÖLÜM 8: KATEGORİ EŞLEŞTİRME

### MesTech Durumu
| # | Özellik | Durum |
|---|---------|-------|
| 1-8 | Kategori listesi, platform kategorileri, eşleştirme UI, AI otomatik, bulk sync, attribute | ✅ VAR |
| 9 | Kategori bazlı komisyon oranı | ❌ YOK (ayrı modülde) |
| 10 | Kategori sync refresh | ✅ VAR |

**SKOR: 9/10 (90%)**

---

## BÖLÜM 9: RAPORLAR

### MesTech Durumu
| # | Özellik | Durum |
|---|---------|-------|
| 1-5 | Rapor ekranı, satış/stok raporu, tarih filtresi | ✅ VAR |
| 6 | Platform filtresi | ❌ YOK |
| 7 | Grafikler | ⚠️ KISMI (SkeletonLoader hazır, chart yok) |
| 8 | Tablo+grafik birlikte | ✅ VAR |
| 9 | Export (Excel/PDF) | ✅ VAR |
| 10 | Zamanlanmış rapor (otomatik e-posta) | ❌ YOK |

**SKOR: 7/10 (70%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-R01 | DEV2 | P2 | Rapor platform filtresi dropdown |
| HH-DEV2-R02 | DEV2 | P1 | Rapor grafik (LiveCharts/OxyPlot) gerçek chart |
| HH-DEV4-R01 | DEV4 | P2 | Zamanlanmış rapor (Hangfire + e-posta) |

---

## BÖLÜM 10: AYARLAR

### MesTech Durumu
| # | Özellik | Durum |
|---|---------|-------|
| 1-8 | Genel, Platform API, Kullanıcı, Rol, Kargo, Fatura, Bildirim, ERP | ✅ VAR |
| 9 | Yedekleme ayarları | ❌ YOK |
| 10 | Lisans/abonelik bilgisi | ❌ YOK (BillingAvaloniaView.axaml VAR ama bağlantı?) |

**SKOR: 8/10 (80%)**

### Eksik Görevler
| Görev ID | DEV | Öncelik | Açıklama |
|----------|-----|---------|----------|
| HH-DEV2-A01 | DEV2 | P2 | Yedekleme ayarları UI |
| HH-DEV2-A02 | DEV2 | P2 | Lisans/abonelik bilgi sayfası bağlantı |

---

## GENEL ÖZET TABLOSU

| Bölüm | MesTech | Profesyonel | Kapsam | Eksik |
|-------|---------|-------------|--------|-------|
| Dashboard | 3/10 | 10 | 30% | 7 |
| Ürün Yönetimi | 4/14 | 14 | 29% | 10 |
| Sipariş Yönetimi | 5/14 | 14 | 36% | 9 |
| Stok Yönetimi | 9/14 | 14 | 64% | 5 |
| Kargo Yönetimi | 7/14 | 14 | 50% | 7 |
| Finans/Fatura | 14/15 | 15 | 93% | 1 |
| Mağaza Yönetimi | 9/10 | 10 | 90% | 1 |
| Kategori Eşleştirme | 9/10 | 10 | 90% | 1 |
| Raporlar | 7/10 | 10 | 70% | 3 |
| Ayarlar | 8/10 | 10 | 80% | 2 |
| **TOPLAM** | **75/121** | **121** | **62%** | **46** |

---

## DEV GÖREV DAĞILIMI (v3.13 Scope)

| DEV | Görev Sayısı | Sorumluluk Alanı |
|-----|-------------|------------------|
| DEV 1 | 4 | Domain handler: performans skorkart, stok yaşlandırma, ürün klonla, kargo maliyet query |
| DEV 2 | 30 | UI/UX: Sayfalama, toplu işlem, inline edit, detay panel, grafik, filtreler, export, navigasyon |
| DEV 3 | 4 | Adapter: İade kargo, kargo performans, platform onay durumu, desi hesaplama |
| DEV 4 | 1 | DevOps: Zamanlanmış rapor (Hangfire + e-posta) |
| DEV 5 | Tümü | Her P0/P1 tamamlanan için test |
| DEV 6 | 2 | Endpoint: Platform stok sync, platform bazlı fiyat override |

---

## KRİTİK YOLLAR (Canlıya en çok etkileyen)

### P0 — MUTLAKA GEREKLİ
1. **Ürün sayfalama** (DEV2) — 9103 ürün gösterilemiyor
2. **Sipariş → Kargo gönder** (DEV2) — Temel iş akışı eksik
3. **Platform stok sync butonu** (DEV6) — Stok değişikliği platformlara gitmiyor

### P1 — ÖNEMLİ
1. Sipariş detay navigasyonu (DEV2)
2. Toplu işlem (ürün + sipariş) (DEV2)
3. Dashboard grafik (DEV2)
4. Dashboard uyarılar (DEV2)
5. İade kargo (DEV3)
6. Rapor grafik (DEV2)
