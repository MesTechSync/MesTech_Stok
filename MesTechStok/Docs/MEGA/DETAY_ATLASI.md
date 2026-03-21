# DETAY ATLASI — MesTechStok Codebase Gerçek Durum
# Tarih: 21 Mart 2026
# Branch: feature/akis4-iyilestirme (commit d936821)
# Tarama: Claude Opus 4.6 — grep/find/wc kanıtlı, derin tarama

---

## GENEL METRİKLER

| Metrik | Değer | Kaynak |
|--------|-------|--------|
| Build | 0 error, 0 warning | `dotnet build MesTechStok.sln` |
| Test dosya | 83 | `find tests/ -name '*.cs'` |
| assert/Should | 195 | `grep -rn 'Assert\.\|Should\.' tests/` |
| Entity | 103 | `find src/MesTech.Domain/Entities/ -name '*.cs'` |
| Handler | 231 | `find src/MesTech.Application/ -name '*Handler.cs'` |
| WebApi endpoint | 227 | `grep -rn 'Map(Get\|Post\|Put\|Delete)' src/MesTech.WebApi/` |
| Avalonia view | 133 | `find src/MesTech.Avalonia/Views/ -name '*.axaml'` |
| Blazor razor | 98 | `find src/MesTech.Blazor/ -name '*.razor'` |
| Panel HTML | 35 | `find frontend/panel/ -name '*.html'` |
| Consumer | 13 | `find Messaging/ -name '*Consumer*.cs'` |
| FluentValidation | 15 | `find Application/ -name '*Validator.cs'` |
| Tema token (x:Key) | 101 | `grep -rc 'x:Key' Themes/*.axaml` |

---

## DEV 1 — BACKEND & DOMAIN

### SEVİYE 1: KRİTİK

| Metrik | Değer | Komut |
|--------|-------|-------|
| Hardcoded credential | **12 satır, 5 dosya** | `grep -rn '"admin"\|Password.*=.*"' src/ --include='*.cs'` |

Credential dosyaları:
1. `src/MesTech.Avalonia/ViewModels/UserManagementAvaloniaViewModel.cs`
2. `src/MesTechStok.Core/Data/AppDbContext.cs`
3. `src/MesTechStok.Core/Services/Concrete/AuthService.cs`
4. `src/MesTechStok.Desktop/MainWindow.xaml.cs`
5. `src/MesTechStok.Desktop/Services/LogAnalysisService.cs`

| NotImplementedException (Domain+App+WebApi) | **0** ✅ | temiz |

### SEVİYE 2: TEKNİK BORÇ

| Metrik | Değer | Detay |
|--------|-------|-------|
| AppDbContext ref (satır) | **312** | Migration hariç |
| AppDbContext ref (dosya) | **136** | unique dosya |
| — Domain | 0 ✅ | temiz |
| — Application | 3 | |
| — Infrastructure | 95 | **en büyük borç** |
| — WebApi | 1 | kolay |
| — Core | 17 | |
| — Desktop | 23 | en zor (UI bağımlı) |
| TODO/FIXME (Domain+App+WebApi) | **2** | neredeyse temiz |
| Build warning | **0** ✅ | |

### SEVİYE 3: DOMAIN KALİTESİ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Entity public setter | **787** | Id/Created/Updated hariç — encapsulation yok |
| Handler toplam | 231 | |
| Handler guard olan | 63 | |
| Handler guard eksik | **168** | %73 guard yok |
| FluentValidation Validator | **15** | 231 handler'a karşı çok az |

### SEVİYE 4: API KALİTESİ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| WebApi endpoint | 227 | |
| ProducesResponseType | **3** | 227'den sadece 3'ünde! |
| Swagger summary | 221 | iyi |

### SEVİYE 5: PERFORMANS

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Sync DB (.ToList/.FirstOrDefault) | **10** | az — kabul edilebilir |

### SEVİYE 6: ENTITY TAMAMLIK

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Toplam entity | 103 | |
| InvoiceType enum değer | 11 | EWaybill/ESelfEmployment/EExport eklenebilir |
| Loyalty entity | **0** | eksik |
| Campaign entity | **0** | eksik |

---

## DEV 2 — FRONTEND & UI

### SEVİYE 1: KRİTİK GÜVENLİK

| Metrik | Değer | Yorum |
|--------|-------|-------|
| innerHTML (frontend/panel/) | 19 | |
| DOMPurify/sanitize | 7 | |
| **XSS AÇIK** | **12** | innerHTML - sanitize |

### SEVİYE 2: TEMA BORCU

| Metrik | Değer | Yorum |
|--------|-------|-------|
| #2855AC hardcoded (dosya) | **42** | 1 Avalonia + 41 Desktop |
| DynamicResource kullanım | **21** | çok düşük |
| Placeholder/TODO view | **22** | |
| Tema token (x:Key) | 101 | |
| Demo/stub ViewModel | **2** | Dashboard |

#2855AC dağılımı:
- `src/MesTech.Avalonia/Themes/MesTechDesignTokens.axaml` — 1 (token tanımı, olması gereken)
- `src/MesTechStok.Desktop/Views/**` — 39 dosya (tema tokena geçmeli)
- `src/MesTechStok.Desktop/Resources/Themes/**` — 2 dosya

### SEVİYE 3: UX KALİTESİ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Loading state olan view | 128/133 | %96 ✅ |
| Empty state | **2/133** | çok düşük |
| Error state | 85/133 | %64 |

### SEVİYE 4: ERİŞİLEBİLİRLİK

| Metrik | Değer | Yorum |
|--------|-------|-------|
| ToolTip | **7** | çok az |
| AutomationProperties | **0** | screen reader desteği yok |
| KeyBinding/HotKey | **0** | klavye kısayolu yok |

### SEVİYE 5: PERFORMANS

| Metrik | Değer | Yorum |
|--------|-------|-------|
| VirtualizingStackPanel | **0** | hiç yok |
| DataGrid (virtualization gerekli) | **88** | 88 DataGrid ama 0 virtualization! |

### SEVİYE 6: MOBİL/TABLET/RESPONSİVE

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Responsive panel (@media) | 32/35 | %91 ✅ |
| Adaptive layout (MinWidth/MaxWidth) | 131 | iyi |
| Font embed (.ttf/.otf) | **0** | |
| CommandPalette | **0** | |

---

## DEV 3 — ENTEGRASYON & MESA OS

### SEVİYE 1: KRİTİK

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Adapter NotImplementedException | **0** ✅ | temiz |
| Consumer toplam | 13 | |
| Consumer MediatR kullanan | 12 | |
| Log-only consumer | **1** | MesaDlqConsumer (DLQ — beklenen) |

Consumer listesi:
1. AccountingApprovalConsumer ✅ MediatR
2. AccountingRejectionConsumer ✅ MediatR
3. AiAdvisoryRecommendationConsumer ✅ MediatR
4. AiDocumentExtractedConsumer ✅ MediatR
5. AiEInvoiceDraftGeneratedConsumer ✅ MediatR
6. AiErpReconciliationDoneConsumer ✅ MediatR
7. AiReconciliationSuggestedConsumer ✅ MediatR
8. BotEFaturaRequestedConsumer ✅ MediatR
9. DocumentClassifiedConsumer ✅ MediatR
10. NotificationSentConsumer ✅ MediatR
11. MesaMeetingScheduledConsumer ✅ MediatR
12. MesaEventConsumers ✅ MediatR (çoklu)
13. MesaDlqConsumer — LOG-ONLY (DLQ, beklenen davranış)

### SEVİYE 2: TEKNİK BORÇ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| TODO/FIXME | **4** | |
| Hardcoded URL | **0** ✅ | URL'ler IConfiguration/Options üzerinden |

### SEVİYE 3: ADAPTER KALİTESİ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Polly kullanan adapter | 14 | |
| Toplam adapter dosya | 28 | |
| **Polly eksik** | **14** | %50 |
| Rate limiting | 12 | |
| CancellationToken | 308 | çok iyi |

### SEVİYE 4: MESA OS KALİTESİ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Idempotency filter | 5 | |
| Consumer try-catch | 11/13 | iyi |

### SEVİYE 5: PERFORMANS

| Metrik | Değer | Yorum |
|--------|-------|-------|
| new HttpClient (dispose riski) | **4** | IHttpClientFactory'ye geçmeli |
| .Result/.Wait() (sync deadlock) | **4** | async'e geçmeli |

### SEVİYE 6: ADAPTER TAMAMLIK

| Metrik | Değer |
|--------|-------|
| ERP adapter dosya | 24 |
| Fulfillment dosya | 2 |
| Kargo/Shipping dosya | 10 |

---

## DEV 4 — DEVOPS & BLAZOR

### SEVİYE 1: KRİTİK GÜVENLİK

| Metrik | Değer | Yorum |
|--------|-------|-------|
| .env git tracked | hayır ✅ | temiz |
| .gitignore'da .env | **YOK** ⚠️ | eklenmeli |

### SEVİYE 2: BLAZOR BORÇ

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Blazor STUB/TODO (satır) | **78** | |
| Blazor STUB/TODO (dosya) | **39** | |
| API bağlantılı razor | 75 | |
| **API bağlantısız razor** | **23** | |
| Toplam razor | 98 | |
| EditForm validation | 33 | |

### SEVİYE 3: UX

| Metrik | Değer | Yorum |
|--------|-------|-------|
| **ErrorBoundary** | **1/98** | 98 sayfada sadece 1! |
| Loading indicator | 76 | iyi |
| Auth/AuthorizeView | 74 | iyi |

### ALTYAPI

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Dockerfile | 1 | `src/MesTech.WebApi/Dockerfile` |
| docker/ klasörü | YOK | gerekirse oluşturulacak |
| .github/workflows/ | YOK | gerekirse oluşturulacak |
| Scripts/ | 8 dosya | mevcut |

---

## DEV 5 — TEST & KALİTE

### TODO/FIXME DAĞILIMI (src/ — test hariç)

| Katman | Sayı |
|--------|------|
| Domain | 0 ✅ |
| Application | 0 ✅ |
| Infrastructure | 4 |
| Avalonia | 4 |
| **Blazor** | **43** |
| WebApi | 2 |
| Desktop | 1 |
| Core | 0 ✅ |
| **TOPLAM** | **54** |

### TEST DURUMU

| Metrik | Değer | Yorum |
|--------|-------|-------|
| Skip/Ignore test | **0** ✅ | temiz |
| NotImplementedException test | **0** ✅ | temiz |
| Test dosya toplam | 83 | |
| assert/Should sayısı | 195 | artırılmalı |

### TEST PROJE DAĞILIMI

| Proje | Dosya |
|-------|-------|
| MesTech.Integration.Tests | 65 |
| MesTech.Tests.E2E | 8 |
| MesTechStok.Avalonia.Tests | 5 |
| MesTech.Blazor.Tests | 3 |
| MesTech.Tests.Performance | 2 |

---

## ÖNCELİK MATRİSİ — Round 1 Hedefleri

### P0 (İlk çalıştırmada MUTLAKA)

| # | DEV | Bulgu | Değer |
|---|-----|-------|-------|
| 1 | DEV1 | Hardcoded credential | 12 satır, 5 dosya |
| 2 | DEV1 | AppDbContext (WebApi+App başla) | 4 dosya kolay |
| 3 | DEV2 | innerHTML XSS açık | 12 açık |
| 4 | DEV2 | #2855AC → DynamicResource | 42 dosya |
| 5 | DEV3 | new HttpClient → IHttpClientFactory | 4 dosya |
| 6 | DEV3 | .Result/.Wait() → async | 4 dosya |
| 7 | DEV4 | .gitignore'a .env ekle | 1 satır |
| 8 | DEV4 | Blazor ErrorBoundary (1→10+) | 98 sayfada 1 |

### P1 (İlk 3 çalıştırmada)

| # | DEV | Bulgu | Değer |
|---|-----|-------|-------|
| 9 | DEV1 | Handler guard eksik | 168 handler |
| 10 | DEV1 | ProducesResponseType | 3/227 |
| 11 | DEV2 | Empty state | 2/133 |
| 12 | DEV2 | Placeholder view | 22 |
| 13 | DEV2 | VirtualizingStackPanel | 0 (88 DataGrid!) |
| 14 | DEV3 | Polly eksik adapter | 14/28 |
| 15 | DEV4 | Blazor API bağlantısız | 23 razor |
| 16 | DEV5 | TODO/FIXME | 54 toplam |

### P2 (Sürekli iyileştirme — sonsuz döngü)

| # | DEV | Bulgu | Değer |
|---|-----|-------|-------|
| 17 | DEV1 | Entity public setter → private set | 787 |
| 18 | DEV2 | ToolTip | 7 → artır |
| 19 | DEV2 | AutomationProperties | 0 → ekle |
| 20 | DEV2 | Font embed | 0 |
| 21 | DEV2 | CommandPalette | 0 |
| 22 | DEV4 | Blazor STUB temizlik | 39 dosya |
| 23 | DEV5 | Test coverage artır | 195 assert |
