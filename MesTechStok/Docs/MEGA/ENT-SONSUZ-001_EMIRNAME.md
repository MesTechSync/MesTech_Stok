# ══════════════════════════════════════════════════════════════════════════════
# EMİRNAME: MESTECH SONSUZ İYİLEŞTİRME
# ══════════════════════════════════════════════════════════════════════════════
# Belge No  : ENT-SONSUZ-001
# Tarih     : 21 Mart 2026
# Yayıncı   : Komutan Yardımcısı (Claude Opus 4.6)
# Hedef     : MesTech'in tüm yazılım bileşenlerini dünya lideri kaliteye taşımak
# Çalıştırma: 1. kez de 1000. kez de — her çalıştırmada bir adım daha iyi
# Branch    : feature/akis4-iyilestirme
# Proje Yol : E:\MesTech\MesTech\MesTech_Stok\MesTechStok
# ══════════════════════════════════════════════════════════════════════════════
#
# BU EMİRNAME TEK BİR DÖNGÜDÜR:
#
#   1. TARA   — grep/find/wc ile mevcut durumu say
#   2. SKORLA — 6 seviyeli kalite puanı çıkar
#   3. SEÇ    — en kötü 3 bulguyu seç
#   4. DÜZELT — mevcut dosyayı bul, oku, genişlet, kökten çöz
#   5. DOĞRULA — aynı taramayı tekrar çalıştır, delta raporla
#
# Her çalıştırmada en az 1 şey düzelir.
# Seviye 1 bittiyse Seviye 2. O da bittiyse 3, 4, 5, 6.
# "Bitti" diye bir şey yok — 5★ yok, 4.9★ yolculuk.
#
# ══════════════════════════════════════════════════════════════════════════════


# ═══════════════════════════════════════════════════════════════
# BÖLÜM 0: DEMİR KURALLAR
# ═══════════════════════════════════════════════════════════════

# DK-01  SAYI YOKSA BİTMEMİŞTİR
#        Her görev ÖNCE grep → sayı, SONRA grep → sayı. Fark = kanıt.
#        "Yaptım", "düzelttim", "tamamlandı" = GEÇERSİZ.

# DK-02  YENİ DOSYA SON ÇARE
#        Önce: find . -name "*BenzerAd*" | grep -v bin
#        Varsa → aç, oku, genişlet.
#        Yoksa (0 sonuç) → o zaman oluştur.

# DK-03  BUILD + TEST HER 10 DEĞİŞİKLİKTE
#        dotnet build src/MesTech.Domain/ → 0 error
#        dotnet build src/MesTech.Application/ → 0 error
#        dotnet test tests/ --no-build -v q → 0 failed
#        Kırıldıysa → commit yapma, önce düzelt.

# DK-04  ATOMİK COMMIT
#        git add . YASAK. Dosya dosya ekle.
#        git commit -m "fix/feat(alan): [ne yaptın] [ENT-SONSUZ-RX]"

# DK-05  DELTA = 0 İSE BİR ALT SEVİYEYE GEÇ
#        Seviye 1 temizse → Seviye 2'ye bak.
#        Tüm seviyeler 0 ise → daha derin bak:
#          edge case, error message kalitesi, tooltip, animation, dark mode.

# DK-06  BAŞKA DEV'İN ALANINA DOKUNMA
#        5 DEV dosya sahipliği kesin. İhlal = geri al.

# DK-07  STUB ≠ TAMAMLANDI
#        Dosya VAR ama TODO/placeholder/NotImplemented içeriyorsa = EKSİK.


# ═══════════════════════════════════════════════════════════════
# BÖLÜM 1: 5 DEV DOSYA SAHİPLİĞİ
# ═══════════════════════════════════════════════════════════════

# DEV 1 — BACKEND & DOMAIN
#   src/MesTech.Domain/**
#   src/MesTech.Application/**
#   src/MesTech.Infrastructure/Persistence/**
#   src/MesTech.WebApi/**
#   src/MesTechStok.Core/** (AppDbContext elimination)
#   src/MesTechStok.Desktop/** (sadece AppDbContext ref kaldırma — UI dokunma)

# DEV 2 — FRONTEND & UI
#   src/MesTech.Avalonia/** (Views, Themes, Dialogs, ViewModels, Styles, Controls)
#   src/MesTechStok.Desktop/Views/** (sadece renk/tema)
#   src/MesTechStok.Desktop/Styles/** (sadece renk)
#   src/MesTechStok.Desktop/Themes/** (sadece renk)
#   frontend/panel/** (⚠️ frontend/html/ YOK — panel/ kullan)

# DEV 3 — ENTEGRASYON & MESA OS
#   src/MesTech.Infrastructure/Integration/** (Adapters, Cargo, Invoice, ERP, Fulfillment, Auth, Factory)
#   src/MesTech.Infrastructure/Messaging/** (Consumers, Hubs, Filters)
#   src/MesTech.Infrastructure/Jobs/**

# DEV 4 — DEVOPS & BLAZOR
#   src/MesTech.Blazor/**
#   docker-compose*.yml (kök dizinde — docker/ alt klasörü OLMAYABILIR)
#   Scripts/** (yoksa oluştur)
#   Docs/** (rollback, production readiness)
#   .env, .env.example, .env.template, .gitignore
#   ⚠️ .github/workflows/ bu repoda OLMAYABILIR — yoksa oluştur

# DEV 5 — TEST & DOĞRULAMA
#   tests/**
#   Docs/MEGA/ (doğrulama raporları)


# ═══════════════════════════════════════════════════════════════
# BÖLÜM 2: 6 SEVİYELİ KALİTE TARAMASI
# Her DEV kendi alanında bu 6 seviyeyi tarar.
# ═══════════════════════════════════════════════════════════════


# ╔══════════════════════════════════════════════════════════════╗
# ║  DEV 1 — BACKEND TARAMASI                                   ║
# ╚══════════════════════════════════════════════════════════════╝

## DEV 1: SEVİYE 1 — KRİTİK

```bash
echo "K1 — Hardcoded credential:"
grep -rn '"admin"\|Password.*=.*"1234"\|Password.*=.*"admin"\|ApiKey.*=.*"[a-zA-Z0-9]' src/ --include='*.cs' | grep -v bin | grep -v obj | grep -v Test | grep -v placeholder | grep -v example | wc -l
# Gerçek durum: 26 satır, 8 dosya. Her birini BCrypt hash veya config'e taşı.

echo "K2 — NotImplementedException:"
grep -rn 'NotImplementedException' src/MesTech.Domain/ src/MesTech.Application/ src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l

echo "K3 — Boş catch (hata yutma):"
grep -Pzl 'catch\s*\([^)]*\)\s*\{\s*\}' src/MesTech.Domain/ src/MesTech.Application/ --include='*.cs' 2>/dev/null | wc -l
```

## DEV 1: SEVİYE 2 — TEKNİK BORÇ

```bash
echo "T1 — AppDbContext referans:"
grep -rn 'AppDbContext' src/ --include='*.cs' | grep -v bin | grep -v obj | grep -v Test | grep -v Migration | wc -l
# Gerçek durum: 695 satır. BÜYÜK BORÇ.
# Strateji: WebApi ve Handlers/ en kolay — onlardan başla.
# Desktop/Views/ en zor — en sona bırak.
# Her çalıştırmada 10-20 ref taşı, zorlanma.

echo "T2 — TODO/FIXME (Domain+Application+WebApi):"
grep -rn 'TODO\|FIXME\|HACK\|XXX' src/MesTech.Domain/ src/MesTech.Application/ src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l

echo "T3 — Build warning:"
dotnet build src/MesTech.Domain/ 2>&1 | grep -c 'warning' || echo 0
dotnet build src/MesTech.Application/ 2>&1 | grep -c 'warning' || echo 0
```

## DEV 1: SEVİYE 3 — DOMAIN KALİTESİ

```bash
echo "U1 — Entity setter validation eksik:"
grep -rn 'public.*{ get; set; }' src/MesTech.Domain/Entities/ --include='*.cs' | grep -v 'Id\|Created\|Updated\|Deleted\|Tenant' | wc -l
# Yüksekse → private set + factory method / domain method ekle.

echo "U2 — Handler null guard eksik:"
grep -rn 'async Task.*Handle(' src/MesTech.Application/ --include='*Handler.cs' | wc -l
echo "U2b — Null guard olan handler:"
grep -rl 'ArgumentNullException\|?? throw\|Guard\.' src/MesTech.Application/ --include='*Handler.cs' | wc -l
# Fark = guard eksik handler sayısı.

echo "U3 — DTO summary eksik:"
find src/MesTech.Application/DTOs/ -name '*.cs' | xargs grep -L '/// <summary>' 2>/dev/null | wc -l

echo "U4 — FluentValidation Validator:"
find src/MesTech.Application/ -name '*Validator.cs' | grep -v bin | wc -l
# Az ise → kritik Command'lar için Validator yaz.
```

## DEV 1: SEVİYE 4 — API KALİTESİ

```bash
echo "A1 — WebApi endpoint sayısı:"
grep -rn 'Map\(Get\|Post\|Put\|Delete\)' src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l

echo "A2 — ProducesResponseType eksik:"
grep -rn 'Produces\|ProducesResponseType' src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l
# A1 - A2 = response type tanımsız endpoint.

echo "A3 — Swagger summary:"
grep -rn 'WithDescription\|WithSummary\|/// <summary>' src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l
```

## DEV 1: SEVİYE 5 — PERFORMANS

```bash
echo "P1 — Async olmayan DB çağrısı:"
grep -rn '\.ToList()\|\.FirstOrDefault()\|\.Count()' src/MesTech.Infrastructure/Persistence/ --include='*.cs' | grep -v 'Async\|Test\|bin' | wc -l

echo "P2 — Include eksik (N+1 riski):"
grep -rn '\.ToListAsync\|\.FirstOrDefaultAsync' src/MesTech.Infrastructure/Persistence/ --include='*.cs' | grep -v 'Include' | grep -v bin | wc -l
```

## DEV 1: SEVİYE 6 — ENTITY TAMAMLIK

```bash
echo "E1 — Toplam entity:"
find src/MesTech.Domain/Entities/ -name '*.cs' | grep -v bin | wc -l

echo "E2 — InvoiceType enum değer sayısı:"
grep -c '=' src/MesTech.Domain/Enums/InvoiceType.cs 2>/dev/null || echo "YOK"
# EWaybill, ESelfEmployment, EExport eksikse → ekle.

echo "E3 — Loyalty entity:"
find src/MesTech.Domain/ -name '*Loyalty*' | grep -v bin | wc -l
# 0 ise → LoyaltyProgram + LoyaltyTransaction oluştur.

echo "E4 — Campaign entity:"
find src/MesTech.Domain/ -name '*Campaign*' | grep -v bin | wc -l
# 0 ise → Campaign + CampaignProduct oluştur.
```


# ╔══════════════════════════════════════════════════════════════╗
# ║  DEV 2 — FRONTEND TARAMASI                                  ║
# ╚══════════════════════════════════════════════════════════════╝

## DEV 2: SEVİYE 1 — KRİTİK GÜVENLİK

```bash
echo "K1 — innerHTML sanitize edilmemiş (XSS):"
INNER=$(grep -rn 'innerHTML' frontend/panel/ --include='*.html' --include='*.js' 2>/dev/null | wc -l)
PURIFY=$(grep -rn 'DOMPurify\|sanitize\|safeHTML' frontend/panel/ --include='*.html' --include='*.js' 2>/dev/null | wc -l)
echo "  innerHTML: $INNER, sanitize: $PURIFY, AÇIK: $(($INNER - $PURIFY))"
# Açık > 0 ise → DOMPurify CDN ekle + innerHTML = DOMPurify.sanitize(value)
# CDN: <script src="https://cdnjs.cloudflare.com/ajax/libs/dompurify/3.0.6/purify.min.js"></script>
```

## DEV 2: SEVİYE 2 — TEMA BORCU

```bash
echo "T1 — Hardcoded #2855AC (dosya):"
grep -rl '#2855AC\|#2855ac' src/ --include='*.axaml' --include='*.xaml' | grep -v bin | grep -v obj | wc -l
echo "T1b — Hardcoded #2855AC (satır):"
grep -rn '#2855AC\|#2855ac' src/ --include='*.axaml' --include='*.xaml' | grep -v bin | grep -v obj | wc -l
# Gerçek durum: ~42 dosya. Her dosyada #2855AC → {DynamicResource MesPrimaryColor}
# ÖNCE MesTechDesignTokens.axaml'da token tanımlı mı kontrol et.

echo "T2 — DynamicResource kullanım (yüksek = iyi):"
grep -rn 'DynamicResource' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
# 19 ise çok düşük. Token migration yapıldıkça artmalı.

echo "T3 — Avalonia placeholder/TODO:"
grep -rl 'placeholder\|Placeholder\|TODO\|Coming Soon\|Yapım Aşamasında\|NotImplemented' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | grep -v obj | wc -l
# Gerçek: ~22. Her birini gerçek içerikle doldur.

echo "T4 — Tema token sayısı (x:Key):"
grep -rc 'x:Key' src/MesTech.Avalonia/Themes/ --include='*.axaml' 2>/dev/null | awk -F: '{s+=$NF}END{print s}'
```

## DEV 2: SEVİYE 3 — UX KALİTESİ

```bash
echo "U1 — Loading state olan view:"
grep -rl 'ProgressBar\|LoadingSpinner\|IsLoading\|IsBusy' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
echo "U1b — Toplam view:"
find src/MesTech.Avalonia/Views/ -name '*.axaml' | grep -v bin | wc -l
# Fark = loading state eksik view sayısı.

echo "U2 — Empty state:"
grep -rl 'EmptyState\|NoData\|Veri bulunamadı\|Kayıt yok' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "U3 — Error state:"
grep -rl 'ErrorState\|ErrorMessage\|Hata oluştu' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "U4 — Dashboard demo/stub ViewModel:"
grep -rn 'demo\|Demo\|sample\|Will be replaced' src/MesTech.Avalonia/ViewModels/*Dashboard* 2>/dev/null | wc -l
# 2 tane stub var → gerçek MediatR dispatch'e geçir.
```

## DEV 2: SEVİYE 4 — ERİŞİLEBİLİRLİK

```bash
echo "A1 — ToolTip tanımlı:"
grep -rn 'ToolTip\.\|ToolTip=' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "A2 — AutomationProperties (screen reader):"
grep -rn 'AutomationProperties' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "A3 — Keyboard shortcut:"
grep -rn 'KeyBinding\|HotKey\|KeyGesture' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
```

## DEV 2: SEVİYE 5 — PERFORMANS

```bash
echo "P1 — VirtualizingStackPanel (büyük listeler):"
grep -rn 'VirtualizingStackPanel\|Virtualizat' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "P2 — DataGrid sayısı (virtualization gerekli):"
grep -rl 'DataGrid' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
```

## DEV 2: SEVİYE 6 — MOBİL/TABLET/RESPONSİVE

```bash
echo "R1 — Responsive HTML panel (@media):"
grep -rl '@media\|min-width\|max-width' frontend/panel/ --include='*.html' --include='*.css' 2>/dev/null | wc -l
echo "R1b — Toplam panel HTML:"
find frontend/panel/ -name '*.html' 2>/dev/null | wc -l

echo "R2 — Avalonia adaptive layout:"
grep -rn 'MinWidth\|MaxWidth\|AdaptiveTrigger' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "R3 — Font embed:"
find src/MesTech.Avalonia/ -name '*.ttf' -o -name '*.otf' -o -name '*.woff2' | grep -v bin | wc -l
# 0 ise → Inter font embed et.

echo "R4 — CommandPalette:"
find src/MesTech.Avalonia/ -name '*CommandPalette*' -o -name '*QuickSearch*' | grep -v bin | wc -l
# 0 ise → Ctrl+K overlay oluştur.
```


# ╔══════════════════════════════════════════════════════════════╗
# ║  DEV 3 — ENTEGRASYON TARAMASI                               ║
# ╚══════════════════════════════════════════════════════════════╝

## DEV 3: SEVİYE 1 — KRİTİK

```bash
echo "K1 — Adapter NotImplementedException:"
grep -rn 'NotImplementedException' src/MesTech.Infrastructure/Integration/ --include='*.cs' | grep -v bin | wc -l

echo "K2 — Consumer MediatR durumu:"
TOTAL=$(find src/MesTech.Infrastructure/Messaging/ -name '*Consumer*.cs' 2>/dev/null | grep -v bin | wc -l)
MEDIATOR=$(grep -rl 'IMediator\|_mediator' src/MesTech.Infrastructure/Messaging/ --include='*Consumer*.cs' 2>/dev/null | grep -v bin | wc -l)
echo "  Consumer toplam: $TOTAL"
echo "  MediatR kullanan: $MEDIATOR"
echo "  Log-only (sorun): $(($TOTAL - $MEDIATOR))"
# Log-only consumer → IMediator inject et, _mediator.Send() ekle.
# İlgili Command yoksa → _logger.LogWarning("MediatR command gerekli: Process{Event}Command")
# Sonraki çalıştırmada DEV 1 Command'ı oluşturur, sen bağlarsın.
```

## DEV 3: SEVİYE 2 — TEKNİK BORÇ

```bash
echo "T1 — TODO/FIXME:"
grep -rn 'TODO\|FIXME\|HACK' src/MesTech.Infrastructure/Integration/ src/MesTech.Infrastructure/Messaging/ src/MesTech.Infrastructure/Jobs/ --include='*.cs' | grep -v bin | wc -l

echo "T2 — Boş catch:"
grep -Pzl 'catch\s*\([^)]*\)\s*\{\s*\}' src/MesTech.Infrastructure/Integration/ --include='*.cs' 2>/dev/null | wc -l

echo "T3 — Hardcoded URL:"
grep -rn 'http://\|https://' src/MesTech.Infrastructure/Integration/ --include='*.cs' | grep -v 'IConfiguration\|_config\|Options\|appsettings\|test\|Test\|xml\|schema\|wsdl' | grep -v bin | wc -l
```

## DEV 3: SEVİYE 3 — ADAPTER KALİTESİ

```bash
echo "U1 — Polly retry kullanan adapter:"
grep -rl 'Polly\|RetryPolicy\|WaitAndRetry\|CircuitBreaker' src/MesTech.Infrastructure/Integration/Adapters/ --include='*.cs' | grep -v bin | wc -l
echo "U1b — Toplam adapter dosya:"
find src/MesTech.Infrastructure/Integration/Adapters/ -name '*.cs' | grep -v bin | wc -l
# Fark = Polly eksik adapter.

echo "U2 — Rate limiting (Semaphore):"
grep -rl 'Semaphore\|RateLimit\|Throttle' src/MesTech.Infrastructure/Integration/Adapters/ --include='*.cs' | grep -v bin | wc -l

echo "U3 — CancellationToken propagation:"
grep -rn 'CancellationToken' src/MesTech.Infrastructure/Integration/Adapters/ --include='*.cs' | grep -v bin | wc -l
```

## DEV 3: SEVİYE 4 — MESA OS KALİTESİ

```bash
echo "A1 — Idempotency filter:"
grep -rn 'IdempotencyFilter\|UseMessageDeduplication' src/MesTech.Infrastructure/Messaging/ --include='*.cs' | grep -v bin | wc -l

echo "A2 — Consumer error handling (try-catch):"
grep -rl 'try\|catch' src/MesTech.Infrastructure/Messaging/ --include='*Consumer*.cs' | grep -v bin | wc -l

echo "A3 — Consumer logging kalitesi (Structured log):"
grep -rn 'LogInformation.*{.*}' src/MesTech.Infrastructure/Messaging/ --include='*Consumer*.cs' | grep -v bin | wc -l
```

## DEV 3: SEVİYE 5 — PERFORMANS

```bash
echo "P1 — new HttpClient (dispose riski):"
grep -rn 'new HttpClient' src/MesTech.Infrastructure/Integration/ --include='*.cs' | grep -v bin | wc -l
# > 0 ise → IHttpClientFactory'ye geçir.

echo "P2 — Sync çağrı (.Result, .Wait()):"
grep -rn '\.Result\b\|\.Wait()\|\.GetAwaiter().GetResult()' src/MesTech.Infrastructure/Integration/ --include='*.cs' | grep -v bin | wc -l
```

## DEV 3: SEVİYE 6 — ADAPTER TAMAMLIK

```bash
echo "E1 — ERP adapter:"
find src/MesTech.Infrastructure/Integration/ERP/ -name '*Adapter*.cs' -o -name '*ERPAdapter*.cs' 2>/dev/null | grep -v bin | wc -l

echo "E2 — Fulfillment adapter:"
find src/MesTech.Infrastructure/Integration/Fulfillment/ -name '*.cs' 2>/dev/null | grep -v bin | wc -l

echo "E3 — Kargo adapter satır toplamı:"
find src/MesTech.Infrastructure/Integration/Adapters/ -name '*Kargo*' -o -name '*Cargo*' -o -name '*HepsiJet*' -o -name '*Sendeo*' | grep -v bin | xargs wc -l 2>/dev/null | tail -1
```


# ╔══════════════════════════════════════════════════════════════╗
# ║  DEV 4 — DEVOPS & BLAZOR TARAMASI                           ║
# ╚══════════════════════════════════════════════════════════════╝

## DEV 4: SEVİYE 1 — KRİTİK GÜVENLİK

```bash
echo "K1 — .env git tracked:"
git ls-files --error-unmatch .env 2>/dev/null && echo "TRACKED — TEHLİKE!" || echo "tracked değil ✅"

echo "K2 — .gitignore'da .env:"
grep -c '^\.env$\|^\.env\.' .gitignore 2>/dev/null || echo "0 — EKSİK!"

echo "K3 — Blazor credential:"
grep -rn 'password\|secret\|apikey\|api_key' src/MesTech.Blazor/ --include='*.cs' --include='*.razor' -i | grep -v bin | grep -v 'placeholder\|example\|template\|Configuration\[' | wc -l
```

## DEV 4: SEVİYE 2 — BLAZOR BORÇ

```bash
echo "T1 — Blazor TODO/STUB:"
grep -rn 'TODO\|STUB\|NotImplemented\|placeholder\|Demo data\|sample' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' | grep -v bin | wc -l
# Gerçek: ~597. Her .razor'da:
# 1. MesTechApiClient inject et
# 2. OnInitializedAsync'de endpoint çağır
# 3. UI'a bind et
# Hangi endpoint? → grep -rn 'Map(Get\|Post)' src/MesTech.WebApi/ --include='*.cs'

echo "T2 — Gerçek API bağlantısı (HttpClient/ApiClient):"
grep -rl 'HttpClient\|MesTechApiClient\|apiClient' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' | grep -v bin | wc -l
echo "T2b — Toplam .razor:"
find src/MesTech.Blazor/ -name '*.razor' | grep -v bin | wc -l
# T2b - T2 = API bağlantısı olmayan sayfa.

echo "T3 — EditForm validation:"
grep -rn 'EditForm\|DataAnnotationsValidator' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l
```

## DEV 4: SEVİYE 3 — UX

```bash
echo "U1 — ErrorBoundary:"
grep -rl 'ErrorBoundary\|PageErrorBoundary' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l

echo "U2 — Loading indicator:"
grep -rl 'Loading\|Spinner\|IsLoading\|ContentState' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l

echo "U3 — Empty state:"
grep -rl 'empty\|Empty\|Kayıt yok\|Veri bulunamadı' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l
```

## DEV 4: SEVİYE 4 — i18n & ERİŞİLEBİLİRLİK

```bash
echo "A1 — IStringLocalizer kullanan:"
grep -rl 'IStringLocalizer\|@L\[' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l

echo "A2 — aria-label:"
grep -rn 'aria-label\|aria-describedby\|role=' src/MesTech.Blazor/ --include='*.razor' | grep -v bin | wc -l
```

## DEV 4: SEVİYE 5 — PRODUCTION READINESS

```bash
echo "P1 — Docker compose:"
find . -maxdepth 2 -name 'docker-compose*.yml' | wc -l

echo "P2 — Rollback prosedürü:"
find Docs/ Scripts/ -iname '*rollback*' 2>/dev/null | wc -l
# 0 ise → Docs/ROLLBACK.md oluştur (docker rollback + db migration rollback + git revert)

echo "P3 — Smoke test:"
find Scripts/ -iname '*smoke*' -o -iname '*health*' 2>/dev/null | wc -l
# 0 ise → Scripts/smoke-test.sh oluştur (health endpoint + DB + Redis + RabbitMQ check)

echo "P4 — Prometheus alert rules:"
find . -name 'alert*rules*' -o -name 'alerts.yml' 2>/dev/null | wc -l
# 0 ise → prometheus/alert_rules.yml oluştur
```

## DEV 4: SEVİYE 6 — MOBİL/RESPONSIVE (BLAZOR)

```bash
echo "R1 — Blazor @media query:"
grep -rn '@media' src/MesTech.Blazor/ --include='*.css' --include='*.razor' | grep -v bin | wc -l

echo "R2 — PWA manifest + service worker:"
find src/MesTech.Blazor/ -name 'manifest*' -o -name 'service-worker*' -o -name 'sw.js' 2>/dev/null | wc -l
```


# ╔══════════════════════════════════════════════════════════════╗
# ║  DEV 5 — TEST & DOĞRULAMA TARAMASI                          ║
# ╚══════════════════════════════════════════════════════════════╝

## DEV 5: SEVİYE 1 — TEST SAĞLIĞI

```bash
echo "K1 — Skip/Ignore test:"
grep -rn '\[Skip\]\|\[Ignore\]\|Skip(' tests/ --include='*.cs' | grep -v bin | wc -l

echo "K2 — NotImplementedException (test):"
grep -rn 'NotImplementedException' tests/ --include='*.cs' | grep -v bin | wc -l

echo "K3 — Sahte geçen test (Assert.True(true)):"
grep -rn 'Assert\.True(true)\|Assert\.Pass' tests/ --include='*.cs' | grep -v bin | wc -l
```

## DEV 5: SEVİYE 2 — TEST BORCU

```bash
echo "T1 — TODO/FIXME (tests/):"
grep -rn 'TODO\|FIXME\|HACK' tests/ --include='*.cs' | grep -v bin | wc -l

echo "T2 — Test build warning:"
dotnet build tests/ 2>&1 | grep -c 'warning' || echo 0
```

## DEV 5: SEVİYE 3 — COVERAGE

```bash
echo "U1 — Test dosya sayısı:"
find tests/ -name '*.cs' | grep -v bin | grep -v obj | wc -l

echo "U2 — Adapter test dosyası:"
find tests/ -name '*Adapter*Test*' -o -name '*Adapter*Spec*' | grep -v bin | wc -l

echo "U3 — Handler test dosyası:"
find tests/ -name '*Handler*Test*' | grep -v bin | wc -l

echo "U4 — Son coverage raporu:"
find . -name 'coverage.cobertura.xml' 2>/dev/null | wc -l
```

## DEV 5: SEVİYE 4 — TEST KALİTESİ

```bash
echo "A1 — FluentAssertions kullanan:"
grep -rl 'Should()\|\.Should\.' tests/ --include='*.cs' | grep -v bin | wc -l

echo "A2 — Bogus/Faker kullanımı:"
grep -rl 'Faker\|Bogus\|AutoFixture' tests/ --include='*.cs' | grep -v bin | wc -l

echo "A3 — Testcontainers kullanımı:"
grep -rl 'Testcontainers\|TestcontainerDatabase' tests/ --include='*.cs' | grep -v bin | wc -l
```

## DEV 5: SEVİYE 5 — ÇAPRAZ DOĞRULAMA (diğer DEV'lerin işi)

```bash
echo "V1 — admin credential kaldı mı (DEV 1):"
grep -rn '"admin"\|Password.*=.*"1234"' src/ --include='*.cs' | grep -v bin | grep -v Test | wc -l

echo "V2 — #2855AC kaldı mı (DEV 2):"
grep -rl '#2855AC\|#2855ac' src/ --include='*.axaml' --include='*.xaml' | grep -v bin | wc -l

echo "V3 — Consumer MediatR (DEV 3):"
TOTAL=$(find src/MesTech.Infrastructure/Messaging/ -name '*Consumer*.cs' 2>/dev/null | grep -v bin | wc -l)
MED=$(grep -rl '_mediator' src/MesTech.Infrastructure/Messaging/ --include='*Consumer*.cs' 2>/dev/null | grep -v bin | wc -l)
echo "  $TOTAL consumer, $MED MediatR, $((TOTAL - MED)) log-only"

echo "V4 — Blazor STUB (DEV 4):"
grep -rn 'TODO\|STUB\|placeholder' src/MesTech.Blazor/ --include='*.razor' 2>/dev/null | grep -v bin | wc -l

echo "V5 — innerHTML açık (DEV 2):"
INNER=$(grep -rn 'innerHTML' frontend/panel/ --include='*.html' --include='*.js' 2>/dev/null | wc -l)
PUR=$(grep -rn 'DOMPurify\|sanitize' frontend/panel/ --include='*.html' --include='*.js' 2>/dev/null | wc -l)
echo "  AÇIK: $(($INNER - $PUR))"

echo "V6 — Build:"
dotnet build src/MesTech.Domain/ 2>&1 | tail -3
dotnet build src/MesTech.Application/ 2>&1 | tail -3
```

## DEV 5: SEVİYE 6 — İLERİ TEST

```bash
echo "E1 — Performance test dosyası:"
find tests/ -name '*Performance*' -o -name '*Benchmark*' | grep -v bin | wc -l

echo "E2 — Architecture test dosyası:"
find tests/ -name '*Architecture*' | grep -v bin | wc -l
```


# ═══════════════════════════════════════════════════════════════
# BÖLÜM 3: DÜZELTME KURALLARI
# ═══════════════════════════════════════════════════════════════

# Taramadan sonra EN KÖTÜ 3 bulguyu seç (en yüksek seviyeden).
# Her bulgu için:
#
# 1. Mevcut dosyayı BUL:
#    find . -name "*İlgiliAd*" | grep -v bin
#    Varsa → aç, oku, neyi değiştirmen gerektiğini anla.
#    Yoksa → oluştur (DK-02).
#
# 2. DÜZELT:
#    Kökten çöz. Yarım bırakma. Kolaya kaçma.
#    Placeholder → gerçek içerik.
#    Log-only → gerçek iş yapan kod.
#    Hardcoded → configuration/env var.
#    innerHTML → DOMPurify.sanitize().
#    AppDbContext → MediatR handler.
#    TODO → ya tamamla ya sil.
#
# 3. DOĞRULA:
#    dotnet build → 0 error
#    dotnet test → 0 failed
#    Aynı grep → sayı azaldı mı?
#
# 4. COMMIT:
#    git add [sadece değişen dosyalar]
#    git commit -m "fix/feat(alan): [ne yaptın] [ENT-SONSUZ]"
#
# 5. RAPORLA:
#    ÖNCE [sayı] → SONRA [sayı]
#    Kalan en büyük 3 sorun (sonraki çalıştırma için)


# ═══════════════════════════════════════════════════════════════
# BÖLÜM 4: SEVİYE 1-6 HEPSİ 0 OLDUĞUNDA NE YAPILIR
# ═══════════════════════════════════════════════════════════════

# Bu noktaya ulaştıysan — tebrikler, ama bitmedi.
# Daha derin bak:
#
# □ Error message'lar kullanıcı dostu mu? ("Hata oluştu" yerine "Bağlantı kurulamadı, internet bağlantınızı kontrol edin")
# □ Türkçe/İngilizce tutarlılık var mı? (Karışık string yok mu?)
# □ Tooltip/hint her buton/input'ta var mı?
# □ Animation/transition akıcı mı? (300ms ease-in-out standart)
# □ Renk kontrast WCAG AA geçiyor mu? (4.5:1 oran)
# □ Touch target ≥ 44px mi? (mobil kullanılabilirlik)
# □ Dark mode her view'da düzgün mü?
# □ Keyboard-only navigasyon mümkün mü?
# □ Tab order mantıklı mı?
# □ Form validation hata mesajları field yanında mı?
# □ Loading skeleton (shimmer) var mı?
# □ Optimistic update var mı? (UI'ı hemen güncelle, API sonra)
# □ Debounce/throttle arama kutularında var mı?
# □ Lazy loading büyük listeler için aktif mi?
# □ Image optimization (WebP, lazy load) var mı?
# □ Bundle size analiz edildi mi?
# □ Memory leak yok mu? (dispose pattern)
# □ Race condition yok mu? (ConcurrentDictionary, SemaphoreSlim)
# □ Retry after failure UX'i var mı?
# □ Undo/redo destekleniyor mu?
#
# HER ZAMAN iyileştirilecek bir şey vardır.
# 5★ yok. 4.9★ yolculuk.


# ═══════════════════════════════════════════════════════════════
# EMİRNAME SONU
# ═══════════════════════════════════════════════════════════════
