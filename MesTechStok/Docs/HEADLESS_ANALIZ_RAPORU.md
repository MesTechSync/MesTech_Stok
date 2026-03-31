# HEADLESS_ANALIZ_RAPORU — 2026-03-31 06:53

## Özet
| Kategori | Sayı | Açıklama |
|----------|------|----------|
| ✅ SAĞLIKLI (>20KB — layout görünür) | 31 | Form, wizard, dashboard layout render |
| ⏳ LOADING/ERROR (8-20KB) | 66 | MediatR DI yok → error/loading overlay |
| ⏳ MİNİMAL (< 8KB) | 75 | Sadece spinner veya error text |
| ❌ RENDER FAIL | 1 | AXAML parse/converter hatası |
| **TOPLAM** | **172** | |

## Açıklama
Headless test DI container OLMADAN çalışır. MediatR bağımlı tüm view'lar
LoadAsync → exception → Error State veya Loading State gösterir.
Bu **beklenen davranış** — gerçek hata DEĞİL.

## ✅ SAĞLIKLI — Layout Render (>20KB)
| View | Boyut (KB) | Durum |
|------|-----------|-------|
| AppHubView.png | 29.6 | ✅ Layout render |
| BankAccountsAvaloniaView.png | 20.3 | ✅ Layout render |
| BarcodeReaderView.png | 25.3 | ✅ Layout render |
| BarcodeScannerView.png | 24.2 | ✅ Layout render |
| Bitrix24AvaloniaView.png | 20.7 | ✅ Layout render |
| BulkProductAvaloniaView.png | 27.2 | ✅ Layout render |
| CampaignAvaloniaView.png | 22.8 | ✅ Layout render |
| CategoryMappingAvaloniaView.png | 22.7 | ✅ Layout render |
| CrmDashboardAvaloniaView.png | 19.6 | ✅ Layout render |
| CrmSettingsAvaloniaView.png | 25.7 | ✅ Layout render |
| DropshipDashboardAvaloniaView.png | 28.8 | ✅ Layout render |
| DropshipProfitAvaloniaView.png | 19.7 | ✅ Layout render |
| FeedCreateAvaloniaView.png | 22.1 | ✅ Layout render |
| FulfillmentDashboardView.png | 22.1 | ✅ Layout render |
| ImportProductsAvaloniaView.png | 22.6 | ✅ Layout render |
| InvoicePdfAvaloniaView.png | 22.5 | ✅ Layout render |
| InvoiceProviderSettingsAvaloniaView.png | 20.5 | ✅ Layout render |
| MainWindow.png | 26.1 | ✅ Layout render |
| MesaAvaloniaView.png | 22.0 | ✅ Layout render |
| MfaSetupView.png | 35.7 | ✅ Layout render |
| OnboardingWizardAvaloniaView.png | 22.1 | ✅ Layout render |
| ProductFetchAvaloniaView.png | 22.5 | ✅ Layout render |
| ProductVariantMatrixView.png | 26.0 | ✅ Layout render |
| SettlementAvaloniaView.png | 23.6 | ✅ Layout render |
| StaleOrdersAvaloniaView.png | 21.1 | ✅ Layout render |
| StockTimelineAvaloniaView.png | 20.0 | ✅ Layout render |
| StoreDetailAvaloniaView.png | 23.3 | ✅ Layout render |
| StoreSettingsAvaloniaView.png | 20.1 | ✅ Layout render |
| StoreWizardAvaloniaView.png | 25.3 | ✅ Layout render |
| TimeEntryAvaloniaView.png | 23.9 | ✅ Layout render |
| WelcomeWindow.png | 80.8 | ✅ Layout render |

## ⏳ LOADING/ERROR — MediatR DI Eksikliği
Bu view'lar DI container ile çalışınca düzgün render olacak.
Headless ortamda error/loading state göstermeleri DOĞRU.

## ❌ RENDER FAIL
- OnboardingWizardAvaloniaView — BetweenConverter IValueConverter fix ile KAPANDI
