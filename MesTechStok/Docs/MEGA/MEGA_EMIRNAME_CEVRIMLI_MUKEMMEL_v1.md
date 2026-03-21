# ══════════════════════════════════════════════════════════════════════════════
# ÇEVRİMLİ MÜKEMMEL MEGA EMİRNAME — MesTech Tam Transformasyon
# ══════════════════════════════════════════════════════════════════════════════
# Belge No  : ENT-MEGA-001
# Versiyon  : 1.0
# Tarih     : 21 Mart 2026
# Yayıncı   : Komutan Yardımcısı (Claude Opus 4.6)
# Amaç      : Tüm yazılım bileşenlerini dünya lideri kaliteye taşımak
# Çalıştırma: Tekrar tekrar — her çalıştırmada daha iyi
# ══════════════════════════════════════════════════════════════════════════════
#
# BU EMİRNAME NEDİR:
#
# Tek bir emirname. 18 bileşen. 5 DEV paralel.
# Her bileşen 5 fazlı ÇEVRİMLİ model ile çalışır:
#   FAZ 1: KEŞİF  → mevcut durumu say (grep + wc + find)
#   FAZ 2: DELTA  → hedef ile kıyasla, eksik bul
#   FAZ 3: PLAN   → sadece deltaları kapat (zaten olan şeyi tekrarlama)
#   FAZ 4: UYGULA → kökten, derinlemesine, yeni dosya açmadan
#   FAZ 5: DOĞRULA → Faz 1'i tekrar çalıştır → delta=0 mı?
#
# İLK ÇALIŞTIRMA = tam keşif + tam uygulama
# İKİNCİ ÇALIŞTIRMA = delta keşif → kalan eksikleri kapat → daha derin
# ÜÇÜNCÜ ÇALIŞTIRMA = micro-delta → polish → dünya standartı
#
# HER ÇALIŞTIRMADA KALİTE ARTAR, HİÇBİR ŞEY BOZULMAZ.
#
# ══════════════════════════════════════════════════════════════════════════════


# ═══════════════════════════════════════════════════════════════════════════════
# BÖLÜM 0: DEMİR KURALLAR — BURASI ANAYASA
# ═══════════════════════════════════════════════════════════════════════════════

## DK-01: SAYI YOKSA BİTMEMİŞTİR
Her görev çıktısında ÖNCE ve SONRA sayısal kanıt zorunlu.
```
ÖRNEK:
  ÖNCE: grep -rl '#2855AC' src/ | wc -l → 179
  SONRA: grep -rl '#2855AC' src/ | wc -l → 0
```
"Yaptım", "tamamlandı", "düzelttim" → GEÇERSİZ. Sadece `grep` çıktısı geçerli.

## DK-02: YENİ DOSYA OLUŞTURMA YASAK (İSTİSNA HARİCİ)
Aynı işi yapan 2. dosya oluşturmak KESİNLİKLE YASAK.
```
YANLIŞ: ProductView2.axaml oluştur
DOĞRU:  ProductView.axaml'ı bul → oku → eksiklerini tamamla
```
İstisna: Gerçekten yeni bir bileşen ekleniyorsa (örn: yeni adapter, yeni view)
ve bu bileşen MEVCUT kodda YOKSA → O zaman yeni dosya oluşturulabilir.
Kesinlik testi: `find . -name "*BenzerAd*" | wc -l` → 0 ise → oluşturabilirsin.

## DK-03: MEVCUT KOD BOZULMAZ
```bash
# Her batch sonrası ZORUNLU:
dotnet build src/MesTech.Domain/ 2>&1 | tail -5          # 0 error
dotnet build src/MesTech.Application/ 2>&1 | tail -5     # 0 error
dotnet build src/MesTech.Infrastructure/ 2>&1 | tail -5   # 0 error
dotnet test tests/ --no-build -v q 2>&1 | tail -10        # 0 failed
```
Build kırıldıysa → commit yapma → ÖNCE düzelt.

## DK-04: ATOMİK COMMIT
Her görev ayrı commit. `git add .` YASAK.
```bash
git add src/MesTech.Avalonia/Views/ProductListView.axaml
git commit -m "fix(avalonia): ProductListView placeholder→real content [MEGA-01-TEMA]"
```

## DK-05: KEŞİF FAZINDA KOD YAZILMAZ
Faz 1 (Keşif) sırasında ASLA kod yazılmaz. Sadece ölç, say, raporla.
Faz 1 çıktısı: sayılar. Faz 4'te kod yazılır.

## DK-06: DELTA=0 İSE ATLA
Bir bileşenin keşfi sonunda tüm hedefler zaten karşılanıyorsa → o bileşeni atla.
Boş iş yapma. Zaman kaybetme. Sonrakine geç.

## DK-07: DEV SINIR İHLALİ = GERİ AL
Her DEV kendi dosya alanında çalışır. Başka DEV'in alanına dokunursa →
o commit GERİ ALINIR. Çakışma toleransı = 0.

## DK-08: "İÇERİK" İLE "STUB" FARKI
Bir dosyanın VAR OLMASI ile İÇERİĞİN DOLU OLMASI farklı şeylerdir.
```
STUB = TODO, NotImplementedException, "placeholder", mock data, boş method
İÇERİK = gerçek logic, gerçek API çağrısı, gerçek veri bağlama
```
Hedef: STUB → 0, İÇERİK → %100.


# ═══════════════════════════════════════════════════════════════════════════════
# BÖLÜM 1: 5 DEV DOSYA SAHİPLİK MATRİSİ
# ═══════════════════════════════════════════════════════════════════════════════

```
┌──────────┬──────────────────────────────────────────────────────────────────┐
│ DEV 1    │ BACKEND & DOMAIN & WEBAPI                                       │
│          │ src/MesTech.Domain/**                                            │
│          │ src/MesTech.Application/**                                       │
│          │ src/MesTech.Infrastructure/Persistence/**                        │
│          │ src/MesTech.WebApi/**                                            │
│          │ Core.AppDbContext elimination                                    │
├──────────┼──────────────────────────────────────────────────────────────────┤
│ DEV 2    │ FRONTEND & UI (WPF + AVALONIA + HTML)                           │
│          │ src/MesTechStok.Desktop/Views/**                                 │
│          │ src/MesTech.Avalonia/Views/**                                    │
│          │ src/MesTech.Avalonia/Themes/**                                   │
│          │ src/MesTech.Avalonia/Dialogs/**                                  │
│          │ frontend/html/**                                                 │
│          │ Tema token, renk migrasyonu, placeholder temizliği               │
├──────────┼──────────────────────────────────────────────────────────────────┤
│ DEV 3    │ API & ENTEGRASYON & ERP & FULFILLMENT                           │
│          │ src/MesTech.Infrastructure/Integration/**                        │
│          │ src/MesTech.Infrastructure/Jobs/**                               │
│          │ MESA OS Consumer'lar (MassTransit)                               │
│          │ Adapter derinleştirme, consumer → MediatR geçişi                 │
├──────────┼──────────────────────────────────────────────────────────────────┤
│ DEV 4    │ DEVOPS & GÜVENLİK & BLAZOR                                      │
│          │ docker/**                                                        │
│          │ .github/workflows/**                                             │
│          │ Scripts/**                                                        │
│          │ src/MesTech.Blazor/**                                            │
│          │ Credential temizlik, güvenlik, production readiness               │
├──────────┼──────────────────────────────────────────────────────────────────┤
│ DEV 5    │ TEST & KALİTE & DOĞRULAMA                                       │
│          │ tests/**                                                          │
│          │ Tüm skip/stub testleri gerçeğe çevirme                           │
│          │ Coverage artırma                                                  │
│          │ Her BİLEŞEN'in Faz 5 doğrulaması                                 │
└──────────┴──────────────────────────────────────────────────────────────────┘
```


# ═══════════════════════════════════════════════════════════════════════════════
# BÖLÜM 2: 18 BİLEŞEN — HER BİRİ ÇEVRİMLİ 5 FAZLI
# ═══════════════════════════════════════════════════════════════════════════════


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 01: TEMA & TASARIM SİSTEMİ                                      ┃
# ┃  Sahip: DEV 2 | Destek: DEV 4 (Blazor tema)                              ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF (kod yazmadan sadece ölç)

```bash
cd "E:/MesTech/MesTech/MesTech_Stok/MesTechStok"

echo "════ B01-KEŞİF: TEMA ════"

# 1.1 Tema dosyası varlığı
echo "1.1 Tema dosyaları:"
find src/ -name "*Theme*.axaml" -o -name "*Token*.axaml" -o -name "*DesignToken*" | grep -v bin | grep -v obj

# 1.2 Tema resource key sayısı
echo "1.2 x:Key toplam:"
grep -rc 'x:Key' src/MesTech.Avalonia/Themes/ --include='*.axaml' 2>/dev/null | awk -F: '{s+=$NF}END{print s}'

# 1.3 ESKI hardcoded #2855AC — kaç DOSYADA?
echo "1.3 Hardcoded #2855AC dosya sayısı:"
grep -rl '#2855AC\|#2855ac' src/ --include='*.axaml' --include='*.xaml' | grep -v bin | grep -v obj | wc -l

# 1.4 DynamicResource kullanımı (token'a geçiş kanıtı)
echo "1.4 DynamicResource kullanım sayısı:"
grep -rn 'DynamicResource' src/ --include='*.axaml' | grep -v bin | grep -v obj | wc -l

# 1.5 Stil dosyası sayısı
echo "1.5 Stil/tema dosya sayısı:"
find src/ -path "*/Themes/*" -o -path "*/Styles/*" | grep -E '\.axaml$|\.xaml$' | grep -v bin | grep -v obj | wc -l

# 1.6 Hardcoded renk sayısı (tema token kullanmayan)
echo "1.6 Hardcoded renk kullanımı (token dışı):"
grep -rn '#[0-9A-Fa-f]\{6\}' src/MesTech.Avalonia/ --include='*.axaml' | grep -v 'x:Key\|Color>' | grep -v bin | grep -v obj | wc -l

# 1.7 Inter font embed durumu
echo "1.7 Font embed:"
find src/ -name "*.ttf" -o -name "*.otf" -o -name "*.woff2" | grep -v bin | grep -v obj | wc -l

# 1.8 WCAG kontrast kontrol (kırmızı bayrak dosyaları)
echo "1.8 Düşük kontrast riski (#EEE, #DDD, #CCC text veya bg):"
grep -rn '#[ED][ED][ED]\|#[CD][CD][CD]' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
```

## FAZ 2 — DELTA (hedefle kıyasla)

| Metrik | HEDEF | MEVCUT (Faz 1) | DELTA |
|--------|-------|----------------|-------|
| MesTechTheme.axaml var mı | VAR | ___ | ___ |
| MesTechDesignTokens.axaml var mı | VAR | ___ | ___ |
| MesTechDarkTokens.axaml var mı | VAR | ___ | ___ |
| x:Key tema resource sayısı | ≥300 | ___ | ___ |
| Hardcoded #2855AC dosya | 0 | ___ | ___ |
| DynamicResource kullanım | ≥500 | ___ | ___ |
| Stil/tema dosya sayısı | ≥25 | ___ | ___ |
| Hardcoded renk (token dışı) | 0 | ___ | ___ |
| Font embed | ≥1 (Inter) | ___ | ___ |
| Düşük kontrast riski | 0 | ___ | ___ |

**Delta = 0 ise → BİLEŞEN 01 ATLANDI. Sonrakine geç.**

## FAZ 3 — PLAN (sadece delta görevleri)

> Bu plan sadece DELTA > 0 olan satırlar için geçerlidir.
> Delta = 0 olan satırlar kesinlikle dokunulmaz.

G01-01: #2855AC → DynamicResource geçişi (batch 20 dosya)
  - Önce: `grep -rl '#2855AC' src/MesTech.Avalonia/ | head -20`
  - Her dosyada: `#2855AC` → `{DynamicResource MesPrimaryColor}`
  - Sonra: aynı grep → 20 azalmış olmalı
  - Commit: `fix(theme): migrate 20 files from hardcoded to DynamicResource [MEGA-B01]`

G01-02: Eksik tema token'ları ekle (MesTechDesignTokens.axaml)
  - Mevcut token sayısını say
  - Eksik olanları ekle: ButtonPrimary, ButtonSecondary, CardBackground, etc.
  - Her UI bileşeni (Button, Card, Input, DataGrid, Dialog) için token grubu

G01-03: Dark mode token'ları (MesTechDarkTokens.axaml)
  - Her DesignToken'ın dark karşılığı

G01-04: Font embed (Inter veya Segoe UI)
  - `src/MesTech.Avalonia/Assets/Fonts/Inter-Regular.ttf`
  - App.axaml'da FontFamily referansı

G01-05: WCAG kontrast düzeltmeleri
  - Düşük kontrast riskli dosyaları bul → fix

## FAZ 4 — UYGULA

> Yalnızca Faz 3'teki G01-XX görevleri uygulanır.
> Her görev sonrası: dotnet build → dotnet test → ÖNCE/SONRA sayı.

## FAZ 5 — DOĞRULA

> Faz 1'deki AYNI komutları TEKRAR çalıştır.
> Yeni sayıları Faz 2 tablosunun "MEVCUT" sütununa yaz.
> Kalan delta varsa → Faz 3'e dön → sadece kalan deltaları kapat.


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 02: SHELL & NAVİGASYON & GÜVENLİK                               ┃
# ┃  Sahip: DEV 2 (UI) + DEV 1 (Auth backend)                                 ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B02-KEŞİF: SHELL & GÜVENLİK ════"

# 2.1 Hardcoded admin/admin
echo "2.1 Hardcoded admin credential:"
grep -rn 'admin.*admin\|"admin"\|Password.*=.*"' src/ --include='*.cs' | grep -v bin | grep -v Test | grep -v obj | wc -l
grep -rn 'admin.*admin\|"admin"\|Password.*=.*"' src/ --include='*.cs' | grep -v bin | grep -v Test | grep -v obj

# 2.2 BruteForce koruması
echo "2.2 BruteForce dosyaları:"
find src/ -name "*BruteForce*" -o -name "*LoginAttempt*" | grep -v bin | grep -v obj

# 2.3 Session timeout
echo "2.3 Session timeout tanımı:"
grep -rn 'SessionTimeout\|Timeout\|Expir' src/ --include='*.cs' | grep -i 'session\|auth\|token' | grep -v bin | wc -l

# 2.4 Keyboard shortcut
echo "2.4 Keyboard shortcut sayısı:"
grep -rn 'KeyBinding\|InputBinding\|KeyGesture\|Ctrl+\|Alt+' src/ --include='*.axaml' --include='*.xaml' | grep -v bin | wc -l

# 2.5 Core.AppDbContext referansları (tüm proje)
echo "2.5 Core.AppDbContext referans sayısı:"
grep -rn 'AppDbContext\|Core\.Data' src/ --include='*.cs' | grep -v bin | grep -v obj | grep -v Test | wc -l

# 2.6 ServiceLocator anti-pattern
echo "2.6 ServiceLocator referans sayısı:"
grep -rn 'ServiceLocator\|Locator\.' src/ --include='*.cs' | grep -v bin | grep -v obj | grep -v Test | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Hardcoded admin credential | 0 | ___ | ___ |
| BruteForce koruması dosya | ≥2 | ___ | ___ |
| Session timeout tanımı | ≥1 | ___ | ___ |
| Keyboard shortcut | ≥30 | ___ | ___ |
| Core.AppDbContext referans | 0 | ___ | ___ |
| ServiceLocator referans | 0 | ___ | ___ |

## FAZ 3-5: (Aynı ÇEVRİMLİ model)


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 03: DASHBOARD & KPI                                              ┃
# ┃  Sahip: DEV 2 (UI) + DEV 1 (Query/Handler)                                ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B03-KEŞİF: DASHBOARD ════"

# 3.1 Dashboard query/handler
echo "3.1 Dashboard CQRS dosya sayısı:"
find src/MesTech.Application/ -path "*Dashboard*" -o -path "*dashboard*" | grep -v bin | wc -l

# 3.2 Dashboard view'lar (4 katman)
echo "3.2 Dashboard view dosyaları:"
find src/ -name "*Dashboard*" -o -name "*dashboard*" | grep -E '\.axaml$|\.xaml$|\.razor$|\.html$' | grep -v bin | wc -l

# 3.3 Dashboard API endpoint
echo "3.3 Dashboard endpoint:"
grep -rn 'dashboard\|Dashboard' src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l

# 3.4 KPI widget sayısı
echo "3.4 KPI widget (tüm katmanlar):"
grep -rn 'KPI\|kpi\|Widget\|widget' src/ --include='*.axaml' --include='*.razor' --include='*.html' | grep -v bin | wc -l

# 3.5 Gerçek API bağlantısı vs mock data
echo "3.5 Mock data sayısı (dashboard):"
grep -rn 'mock\|Mock\|dummy\|sample\|hardcoded' src/ --include='*.axaml.cs' --include='*.razor' | grep -i 'dashboard' | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Dashboard CQRS handler | ≥8 | ___ | ___ |
| Dashboard view (4 katman) | ≥12 | ___ | ___ |
| Dashboard WebAPI endpoint | ≥4 | ___ | ___ |
| KPI widget | ≥10 | ___ | ___ |
| Mock data kullanımı | 0 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 04: ÜRÜN YÖNETİMİ                                                ┃
# ┃  Sahip: DEV 2 (UI) + DEV 1 (CQRS) + DEV 5 (Test)                         ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B04-KEŞİF: ÜRÜN ════"

echo "4.1 Product entity + VO + enum:"
find src/MesTech.Domain/ -name "*Product*" | grep -v bin | wc -l

echo "4.2 Product CQRS (Command+Query+Handler):"
find src/MesTech.Application/ -path "*Product*" | grep -v bin | wc -l

echo "4.3 Product view (4 katman):"
find src/ -name "*Product*" | grep -E '\.axaml$|\.xaml$|\.razor$|\.html$' | grep -v bin | wc -l

echo "4.4 Bulk import dosyası:"
find src/ -name "*Bulk*Import*" -o -name "*bulk*import*" | grep -v bin | wc -l

echo "4.5 CategoryMapping entity:"
find src/MesTech.Domain/ -name "*CategoryMapping*" | grep -v bin | wc -l

echo "4.6 Product test sayısı:"
find tests/ -name "*Product*" | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Product domain dosya | ≥15 | ___ | ___ |
| Product CQRS dosya | ≥30 | ___ | ___ |
| Product view (4 katman) | ≥12 | ___ | ___ |
| Bulk import | ≥2 | ___ | ___ |
| CategoryMapping | ≥1 | ___ | ___ |
| Product test dosya | ≥14 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 05: SİPARİŞ & KARGO                                              ┃
# ┃  Sahip: DEV 3 (Adapter) + DEV 2 (UI) + DEV 1 (Domain)                     ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B05-KEŞİF: SİPARİŞ & KARGO ════"

echo "5.1 Kargo adapter dosyaları + satır:"
find src/MesTech.Infrastructure/Integration/Cargo/ -name "*.cs" | grep -v bin | wc -l
find src/MesTech.Infrastructure/Integration/Cargo/ -name "*.cs" | grep -v bin | xargs wc -l 2>/dev/null | tail -1

echo "5.2 Kargo view dosyaları (Avalonia):"
find src/MesTech.Avalonia/ -name "*Cargo*" -o -name "*Shipping*" | grep -v bin | wc -l

echo "5.3 Order CQRS:"
find src/MesTech.Application/ -path "*Order*" | grep -v bin | wc -l

echo "5.4 OrderShipment akışı (otomatik gönderim):"
grep -rn 'OrderShipment\|AutoShip\|SendShipment' src/ --include='*.cs' | grep -v bin | grep -v Test | wc -l

echo "5.5 OrderBulkProcess:"
grep -rn 'BulkOrder\|OrderBulk\|BatchOrder' src/ --include='*.cs' | grep -v bin | wc -l

echo "5.6 Kargo label/etiket:"
grep -rn 'Label\|label\|Etiket\|etiket' src/MesTech.Infrastructure/Integration/Cargo/ --include='*.cs' | grep -v bin | wc -l

echo "5.7 Kargo tracking:"
grep -rn 'Track\|track' src/MesTech.Infrastructure/Integration/Cargo/ --include='*.cs' | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Kargo adapter .cs dosya | ≥14 | ___ | ___ |
| Kargo adapter toplam satır | ≥4000 | ___ | ___ |
| Kargo Avalonia view | ≥5 | ___ | ___ |
| Order CQRS dosya | ≥15 | ___ | ___ |
| OrderShipment akış dosya | ≥3 | ___ | ___ |
| OrderBulkProcess | ≥2 | ___ | ___ |
| Kargo label desteği | ≥7 (her adapter) | ___ | ___ |
| Kargo tracking desteği | ≥7 (her adapter) | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 06: STOK & BARKOD & DEPO                                         ┃
# ┃  Sahip: DEV 1 (Domain) + DEV 2 (UI)                                       ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B06-KEŞİF: STOK & BARKOD & DEPO ════"

echo "6.1 Stock entity + VO:"
find src/MesTech.Domain/ -name "*Stock*" -o -name "*Inventory*" | grep -v bin | wc -l

echo "6.2 Warehouse entity:"
find src/MesTech.Domain/ -name "*Warehouse*" | grep -v bin | wc -l

echo "6.3 SerialNumber / IMEI dosyaları:"
find src/ -name "*Serial*" -o -name "*IMEI*" | grep -v bin | wc -l

echo "6.4 FIFO maliyet hesaplama:"
grep -rn 'FIFO\|fifo\|CostCalculat' src/ --include='*.cs' | grep -v bin | wc -l

echo "6.5 Stock view (4 katman):"
find src/ -name "*Stock*" -o -name "*Inventory*" -o -name "*Warehouse*" -o -name "*Barcode*" | grep -E '\.axaml$|\.xaml$|\.razor$|\.html$' | grep -v bin | wc -l

echo "6.6 Stock CQRS:"
find src/MesTech.Application/ -path "*Stock*" -o -path "*Warehouse*" -o -path "*Inventory*" | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Stock domain dosya | ≥11 | ___ | ___ |
| Warehouse entity | ≥8 | ___ | ___ |
| SerialNumber/IMEI | ≥2 | ___ | ___ |
| FIFO maliyet | ≥2 | ___ | ___ |
| Stock view (4 katman) | ≥16 | ___ | ___ |
| Stock CQRS dosya | ≥13 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 07: MUHASEBE & FİNANS                                            ┃
# ┃  Sahip: DEV 1 (Domain+CQRS) + DEV 2 (Avalonia UI)                         ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B07-KEŞİF: MUHASEBE ════"

echo "7.1 Finance entity:"
find src/MesTech.Domain/ -name "*Finance*" -o -name "*Account*" -o -name "*Commission*" -o -name "*Reconcil*" | grep -v bin | wc -l

echo "7.2 Finance CQRS:"
find src/MesTech.Application/ -path "*Finance*" -o -path "*Account*" | grep -v bin | wc -l

echo "7.3 Finance Avalonia view:"
find src/MesTech.Avalonia/ -name "*Finance*" -o -name "*Account*" -o -name "*Commission*" | grep -v bin | wc -l

echo "7.4 Finance endpoint (WebApi):"
grep -rn 'finance\|Finance\|accounting\|Accounting' src/MesTech.WebApi/ --include='*.cs' | grep -v bin | wc -l

echo "7.5 CultureInfo.InvariantCulture kullanımı (para birimi güvenliği):"
grep -rn 'InvariantCulture' src/ --include='*.cs' | grep -v bin | grep -v Test | wc -l
echo "7.5b CultureInfo OLMAYAN decimal.Parse:"
grep -rn 'decimal\.\(Parse\|TryParse\)' src/ --include='*.cs' | grep -v 'InvariantCulture' | grep -v bin | grep -v Test | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Finance entity dosya | ≥10 | ___ | ___ |
| Finance CQRS dosya | ≥8 | ___ | ___ |
| Finance Avalonia view | ≥10 | ___ | ___ |
| Finance WebApi endpoint | ≥5 | ___ | ___ |
| CultureInfo olmayan Parse | 0 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 08: E-FATURA & E-BELGE                                           ┃
# ┃  Sahip: DEV 3 (Provider) + DEV 1 (Domain) + DEV 2 (UI)                    ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B08-KEŞİF: E-FATURA ════"

echo "8.1 Invoice provider dosyaları + satır:"
find src/MesTech.Infrastructure/Integration/Invoice/ -name "*.cs" | grep -v bin | wc -l
find src/MesTech.Infrastructure/Integration/Invoice/ -name "*.cs" | grep -v bin | xargs wc -l 2>/dev/null | tail -1

echo "8.2 InvoiceType enum değerleri:"
grep -A 30 'enum InvoiceType' src/MesTech.Domain/ -r --include='*.cs' | grep -v bin

echo "8.3 UBL-TR builder:"
find src/ -name "*UBL*" -o -name "*Ubl*" | grep -v bin | wc -l

echo "8.4 XAdES imza:"
find src/ -name "*XAdES*" -o -name "*Xades*" -o -name "*DigitalSign*" | grep -v bin | wc -l

echo "8.5 QuestPDF kullanımı:"
grep -rn 'QuestPDF\|Document.Create' src/ --include='*.cs' | grep -v bin | wc -l

echo "8.6 Invoice Avalonia view:"
find src/MesTech.Avalonia/ -name "*Invoice*" -o -name "*Fatura*" | grep -v bin | wc -l

echo "8.7 Invoice test sayısı:"
find tests/ -name "*Invoice*" | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Invoice provider .cs dosya | ≥18 | ___ | ___ |
| InvoiceType enum (e-SMM, e-İhracat dahil) | ≥8 değer | ___ | ___ |
| UBL-TR builder | ≥2 | ___ | ___ |
| XAdES imza | ≥1 | ___ | ___ |
| QuestPDF PDF üretimi | ≥3 | ___ | ___ |
| Invoice Avalonia view | ≥10 | ___ | ___ |
| Invoice test dosya | ≥13 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 09: CRM & MÜŞTERİ                                                ┃
# ┃  Sahip: DEV 1 (Domain) + DEV 2 (UI)                                       ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B09-KEŞİF: CRM ════"

echo "9.1 CRM entity (Customer, Pipeline, Message, Loyalty, Campaign):"
find src/MesTech.Domain/ -name "*Customer*" -o -name "*Pipeline*" -o -name "*Message*" -o -name "*Loyalty*" -o -name "*Campaign*" | grep -v bin | wc -l

echo "9.2 Loyalty entity (Sentos eksik):"
find src/ -name "*Loyalty*" | grep -v bin | wc -l

echo "9.3 Campaign entity (Sentos eksik):"
find src/ -name "*Campaign*" | grep -v bin | wc -l

echo "9.4 CRM CQRS:"
find src/MesTech.Application/ -path "*Crm*" -o -path "*Customer*" | grep -v bin | wc -l

echo "9.5 CRM Avalonia view:"
find src/MesTech.Avalonia/ -name "*Crm*" -o -name "*Customer*" -o -name "*Pipeline*" | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| CRM entity dosya | ≥10 | ___ | ___ |
| Loyalty entity | ≥2 | ___ | ___ |
| Campaign entity | ≥2 | ___ | ___ |
| CRM CQRS dosya | ≥10 | ___ | ___ |
| CRM Avalonia view | ≥7 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 10: PLATFORM ADAPTER & DROPSHIPPING                              ┃
# ┃  Sahip: DEV 3                                                              ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B10-KEŞİF: PLATFORM & DROPSHIP ════"

echo "10.1 Platform adapter dosya + satır:"
find src/MesTech.Infrastructure/Integration/Adapters/ -name "*.cs" | grep -v bin | wc -l
find src/MesTech.Infrastructure/Integration/Adapters/ -name "*.cs" | grep -v bin | xargs wc -l 2>/dev/null | tail -1

echo "10.2 Her adapter NotImplementedException:"
grep -rn 'NotImplementedException\|throw new NotImpl' src/MesTech.Infrastructure/Integration/Adapters/ --include='*.cs' | grep -v bin | wc -l

echo "10.3 Her adapter Polly retry:"
grep -rn 'Polly\|RetryPolicy\|WaitAndRetry' src/MesTech.Infrastructure/Integration/Adapters/ --include='*.cs' | grep -v bin | wc -l

echo "10.4 Her adapter rate limiting:"
grep -rn 'Semaphore\|RateLimit\|Throttle' src/MesTech.Infrastructure/Integration/Adapters/ --include='*.cs' | grep -v bin | wc -l

echo "10.5 Dropshipping entity:"
find src/MesTech.Domain/ -name "*Dropship*" | grep -v bin | wc -l

echo "10.6 Dropshipping CQRS:"
find src/MesTech.Application/ -path "*Dropship*" | grep -v bin | wc -l

echo "10.7 Adapter test sayısı:"
find tests/ -name "*Adapter*" | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Platform adapter .cs dosya | ≥39 | ___ | ___ |
| Platform adapter toplam satır | ≥13000 | ___ | ___ |
| NotImplementedException | 0 | ___ | ___ |
| Polly retry kullanımı | ≥10 (her adapter) | ___ | ___ |
| Rate limiting kullanımı | ≥10 (her adapter) | ___ | ___ |
| Dropshipping entity | ≥7 | ___ | ___ |
| Dropshipping CQRS | ≥34 | ___ | ___ |
| Adapter test dosya | ≥30 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 11: RAPORLAMA & BİLDİRİM                                         ┃
# ┃  Sahip: DEV 1 (Query) + DEV 2 (UI) + DEV 4 (Alert)                        ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B11-KEŞİF: RAPOR & BİLDİRİM ════"

echo "11.1 Report query/handler:"
find src/MesTech.Application/ -path "*Report*" | grep -v bin | wc -l

echo "11.2 NotificationSetting entity:"
find src/MesTech.Domain/ -name "*Notification*" | grep -v bin | wc -l

echo "11.3 Report view (4 katman):"
find src/ -name "*Report*" | grep -E '\.axaml$|\.xaml$|\.razor$|\.html$' | grep -v bin | wc -l

echo "11.4 Prometheus alert rule:"
find . -name "*.yml" -o -name "*.yaml" | xargs grep -l 'alert\|Alert' 2>/dev/null | wc -l

echo "11.5 Build warning (core 4 proje):"
dotnet build src/MesTech.Domain/ 2>&1 | grep -c 'warning'
dotnet build src/MesTech.Application/ 2>&1 | grep -c 'warning'
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Report query/handler dosya | ≥9 | ___ | ___ |
| NotificationSetting entity | ≥2 | ___ | ___ |
| Report view (4 katman) | ≥8 | ___ | ___ |
| Prometheus alert YAML | ≥1 | ___ | ___ |
| Build warning (Domain) | 0 | ___ | ___ |
| Build warning (Application) | 0 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 12: TEMİZLİK & TEKNİK BORÇ                                      ┃
# ┃  Sahip: DEV 5 (koordine) + TÜM DEV'ler kendi alanında                     ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF (KRİTİK — tüm teknik borçların canlı tablosu)

```bash
echo "════ B12-KEŞİF: TEKNİK BORÇ ════"

echo "12.1 Boş catch bloğu:"
grep -rn 'catch\s*(' src/ --include='*.cs' | grep -v bin | grep -v Test | wc -l
echo "12.1b Boş catch (hata yutma - sadece boş body):"
grep -Pzro 'catch\s*\([^)]*\)\s*\{\s*\}' src/ --include='*.cs' 2>/dev/null | grep -v bin | wc -l

echo "12.2 TODO/FIXME/HACK/XXX sayısı:"
grep -rn 'TODO\|FIXME\|HACK\|XXX' src/ --include='*.cs' --include='*.axaml' --include='*.razor' | grep -v bin | wc -l

echo "12.3 NotImplementedException:"
grep -rn 'NotImplementedException' src/ --include='*.cs' | grep -v bin | grep -v Test | wc -l

echo "12.4 innerHTML (XSS riski):"
grep -rn 'innerHTML' frontend/ --include='*.html' --include='*.js' 2>/dev/null | wc -l

echo "12.5 DOMPurify sanitize:"
grep -rn 'DOMPurify\|sanitize' frontend/ --include='*.html' --include='*.js' 2>/dev/null | wc -l

echo "12.6 Avalonia placeholder view:"
grep -rn 'placeholder\|Placeholder\|TODO\|Coming Soon' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "12.7 Blazor TODO/STUB:"
grep -rn 'TODO\|STUB\|NotImplemented\|Coming Soon' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' 2>/dev/null | grep -v bin | wc -l

echo "12.8 HTML mock data:"
grep -rn 'mock\|Mock\|dummy\|sample.*data\|sampleData\|hardcoded' frontend/ --include='*.html' --include='*.js' 2>/dev/null | wc -l

echo "12.9 Test skip/ignore:"
grep -rn '\[Skip\]\|\[Ignore\]' tests/ --include='*.cs' | grep -v bin | wc -l

echo "12.10 Test NotImplementedException:"
grep -rn 'NotImplementedException' tests/ --include='*.cs' | grep -v bin | wc -l
```

## FAZ 2 — DELTA (TEKNİK BORÇ SIFIR MATRİSİ)

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Boş catch bloğu | <30 | ___ | ___ |
| TODO/FIXME/HACK | <10 | ___ | ___ |
| NotImplementedException (src) | 0 | ___ | ___ |
| innerHTML (sanitize edilmemiş) | 0 | ___ | ___ |
| DOMPurify kullanım | = innerHTML sayısı | ___ | ___ |
| Avalonia placeholder | 0 | ___ | ___ |
| Blazor TODO/STUB | 0 | ___ | ___ |
| HTML mock data | 0 | ___ | ___ |
| Test skip/ignore | 0 | ___ | ___ |
| Test NotImplemented | 0 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 13: MESA OS ENTEGRASYON                                          ┃
# ┃  Sahip: DEV 3                                                              ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B13-KEŞİF: MESA OS ════"

echo "13.1 MassTransit consumer sayısı:"
find src/ -name "*Consumer*" | grep -v bin | grep -v Test | wc -l

echo "13.2 Consumer'lar MediatR kullanıyor mu (log-only değil gerçek işlem):"
grep -rn 'IMediator\|_mediator\|Send(\|Publish(' src/ --include='*Consumer*.cs' | grep -v bin | grep -v Test | wc -l
echo "13.2b Consumer'lar sadece log mu yapıyor:"
grep -rn '_logger\.Log\|LogInformation\|LogWarning' src/ --include='*Consumer*.cs' | grep -v bin | grep -v Test | wc -l

echo "13.3 Idempotency key kontrolü:"
grep -rn 'Idempotency\|idempotent\|MessageId\|DeduplicateId' src/ --include='*.cs' | grep -v bin | wc -l

echo "13.4 DLQ monitoring:"
find src/ -name "*Dlq*" -o -name "*DeadLetter*" | grep -v bin | wc -l

echo "13.5 SignalR Hub (WebSocket canlı):"
find src/ -name "*Hub*" -o -name "*SignalR*" | grep -v bin | wc -l

echo "13.6 Health check (MESA bağlantı):"
grep -rn 'HealthCheck\|AddHealthChecks\|MapHealthChecks' src/ --include='*.cs' | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| MassTransit consumer | ≥13 | ___ | ___ |
| Consumer MediatR kullanım | = consumer sayısı | ___ | ___ |
| Consumer log-only | 0 | ___ | ___ |
| Idempotency kontrolü | ≥5 | ___ | ___ |
| DLQ monitoring | ≥2 | ___ | ___ |
| SignalR Hub | ≥1 | ___ | ___ |
| Health check | ≥3 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 14: ERP ENTEGRASYON                                              ┃
# ┃  Sahip: DEV 3                                                              ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B14-KEŞİF: ERP ════"

echo "14.1 ERP adapter dosya + satır:"
find src/MesTech.Infrastructure/Integration/Erp/ -name "*.cs" 2>/dev/null | grep -v bin | wc -l
find src/MesTech.Infrastructure/Integration/Erp/ -name "*.cs" 2>/dev/null | grep -v bin | xargs wc -l 2>/dev/null | tail -1

echo "14.2 IErp capability interface:"
grep -rn 'IErp.*Capable\|IERPAdapter\|IErpAdapter' src/ --include='*.cs' | grep -v bin | wc -l

echo "14.3 ERP Polly retry:"
grep -rn 'Polly\|RetryPolicy' src/MesTech.Infrastructure/Integration/Erp/ --include='*.cs' 2>/dev/null | grep -v bin | wc -l

echo "14.4 ErpProviderFactory:"
find src/ -name "*ErpFactory*" -o -name "*ErpProviderFactory*" | grep -v bin | wc -l

echo "14.5 Conflict resolver (çift yönlü sync):"
find src/ -name "*Conflict*" -o -name "*Reconcil*" | grep -v bin | wc -l

echo "14.6 ERP test sayısı:"
find tests/ -name "*Erp*" -o -name "*ERP*" | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| ERP adapter .cs dosya | ≥28 | ___ | ___ |
| ERP adapter toplam satır | ≥7500 | ___ | ___ |
| IErp capability interface | ≥8 | ___ | ___ |
| Polly retry (ERP) | ≥5 | ___ | ___ |
| ErpProviderFactory | ≥1 | ___ | ___ |
| Conflict resolver | ≥2 | ___ | ___ |
| ERP test dosya | ≥5 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 15: BLAZOR SSR & PWA                                             ┃
# ┃  Sahip: DEV 4                                                              ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B15-KEŞİF: BLAZOR ════"

echo "15.1 Blazor .razor sayfa:"
find src/MesTech.Blazor/ -name "*.razor" 2>/dev/null | grep -v bin | wc -l

echo "15.2 Blazor TODO/STUB satır:"
grep -rn 'TODO\|STUB\|NotImplemented\|placeholder' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' 2>/dev/null | grep -v bin | wc -l

echo "15.3 Gerçek API bağlantısı (HttpClient/MesTechApiClient):"
grep -rn 'HttpClient\|MesTechApiClient\|apiClient\|_client' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' 2>/dev/null | grep -v bin | wc -l

echo "15.4 IStringLocalizer kullanımı:"
grep -rn 'IStringLocalizer\|@inject.*Localizer' src/MesTech.Blazor/ --include='*.razor' --include='*.cs' 2>/dev/null | grep -v bin | wc -l

echo "15.5 EditForm sayısı:"
grep -rn '<EditForm\|EditForm' src/MesTech.Blazor/ --include='*.razor' 2>/dev/null | grep -v bin | wc -l

echo "15.6 ErrorBoundary kullanımı:"
grep -rn 'ErrorBoundary\|ContentState' src/MesTech.Blazor/ --include='*.razor' 2>/dev/null | grep -v bin | wc -l

echo "15.7 PWA manifest + service worker:"
find src/MesTech.Blazor/ -name "manifest*" -o -name "service-worker*" -o -name "sw.js" 2>/dev/null | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Blazor .razor sayfa | ≥73 | ___ | ___ |
| Blazor TODO/STUB | 0 | ___ | ___ |
| Gerçek API bağlantısı | ≥50 | ___ | ___ |
| IStringLocalizer | ≥40 | ___ | ___ |
| EditForm | ≥15 | ___ | ___ |
| ErrorBoundary | ≥10 | ___ | ___ |
| PWA manifest+SW | ≥2 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 16: AVALONIA CROSS-PLATFORM                                      ┃
# ┃  Sahip: DEV 2                                                              ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B16-KEŞİF: AVALONIA ════"

echo "16.1 Avalonia .axaml toplam:"
find src/MesTech.Avalonia/ -name "*.axaml" | grep -v bin | wc -l

echo "16.2 Avalonia placeholder view:"
grep -rl 'placeholder\|Placeholder\|TODO\|Coming Soon\|Yapım Aşamasında' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "16.3 Avalonia ViewModel sayısı:"
find src/MesTech.Avalonia/ -name "*ViewModel*" | grep -v bin | wc -l

echo "16.4 Command Palette:"
find src/ -name "*CommandPalette*" | grep -v bin | wc -l

echo "16.5 Animation tanım:"
grep -rn 'Animation\|Transition\|@keyframes' src/MesTech.Avalonia/ --include='*.axaml' --include='*.cs' | grep -v bin | wc -l

echo "16.6 İkon (StreamGeometry):"
grep -rn 'StreamGeometry\|PathGeometry\|PathIcon' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l

echo "16.7 Responsive/adaptive layout:"
grep -rn 'AdaptiveTrigger\|VisualState\|MinWidth\|MaxWidth' src/MesTech.Avalonia/ --include='*.axaml' | grep -v bin | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Avalonia .axaml toplam | ≥149 | ___ | ___ |
| Avalonia placeholder | 0 | ___ | ___ |
| Avalonia ViewModel | ≥30 | ___ | ___ |
| Command Palette | ≥1 | ___ | ___ |
| Animation tanım | ≥20 | ___ | ___ |
| İkon tanım | ≥50 | ___ | ___ |
| Responsive layout | ≥10 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 17: i18n & EĞİTİM & DESTEK                                      ┃
# ┃  Sahip: DEV 4 (Blazor i18n) + DEV 2 (Avalonia i18n)                       ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B17-KEŞİF: i18n & EĞİTİM ════"

echo "17.1 SharedResource .resx dosyaları:"
find src/ -name "*.resx" | grep -v bin | wc -l

echo "17.2 .resx toplam key sayısı:"
grep -c '<data name=' src/ -r --include='*.resx' 2>/dev/null | awk -F: '{s+=$NF}END{print s}'

echo "17.3 LanguageSelector bileşeni:"
find src/ -name "*LanguageSelector*" -o -name "*LanguageSwitcher*" | grep -v bin | wc -l

echo "17.4 MesAkademi/Academy dosyaları:"
find src/ -name "*Akademi*" -o -name "*Academy*" -o -name "*Tutorial*" | grep -v bin | wc -l

echo "17.5 Onboarding wizard:"
find src/ -name "*Onboarding*" -o -name "*Welcome*Wizard*" | grep -v bin | wc -l

echo "17.6 JavaScript i18n engine:"
find frontend/ -name "*i18n*" -o -name "*locale*" 2>/dev/null | wc -l

echo "17.7 Changelog/FAQ:"
find Docs/ src/ -name "*Changelog*" -o -name "*FAQ*" -o -name "*SSS*" 2>/dev/null | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| .resx dosya sayısı | ≥6 (TR+EN×3 katman) | ___ | ___ |
| .resx toplam key | ≥500 | ___ | ___ |
| LanguageSelector | ≥2 (Avalonia+Blazor) | ___ | ___ |
| MesAkademi dosya | ≥5 | ___ | ___ |
| Onboarding wizard | ≥1 | ___ | ___ |
| JS i18n engine | ≥2 | ___ | ___ |
| Changelog/FAQ | ≥2 | ___ | ___ |


# ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
# ┃  BİLEŞEN 18: PRODUCTION READINESS                                         ┃
# ┃  Sahip: DEV 4 (DevOps) + DEV 5 (Test)                                     ┃
# ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

## FAZ 1 — KEŞİF

```bash
echo "════ B18-KEŞİF: PRODUCTION ════"

echo "18.1 Docker compose dosya sayısı:"
find . -name "docker-compose*.yml" | wc -l

echo "18.2 Rollback prosedürü:"
find Docs/ Scripts/ -name "*Rollback*" -o -name "*rollback*" 2>/dev/null | wc -l

echo "18.3 Smoke test script:"
find Scripts/ -name "*smoke*" -o -name "*health*" 2>/dev/null | wc -l

echo "18.4 Load test raporu:"
find Docs/ -name "*LOAD*" -o -name "*load_test*" 2>/dev/null | wc -l

echo "18.5 SSL/nginx config:"
find docker/ nginx/ -name "*.conf" 2>/dev/null | wc -l

echo "18.6 Backup script:"
find Scripts/ -name "*backup*" 2>/dev/null | wc -l

echo "18.7 Toplam test sayısı:"
dotnet test tests/ --no-build -v q --list-tests 2>&1 | tail -5

echo "18.8 Test coverage:"
# dotnet test /p:CollectCoverage=true → reportgenerator
find . -name "coverage.cobertura.xml" -o -name "*.coverage" 2>/dev/null | wc -l

echo "18.9 Credential audit (repo'da API key):"
grep -rn 'api[_-]key\|apiKey\|secret\|password' src/ --include='*.cs' --include='*.json' | grep -v bin | grep -v Test | grep -v 'placeholder\|example\|template\|TODO\|Configuration\[' | wc -l
```

## FAZ 2 — DELTA

| Metrik | HEDEF | MEVCUT | DELTA |
|--------|-------|--------|-------|
| Docker compose | ≥6 | ___ | ___ |
| Rollback prosedürü | ≥1 | ___ | ___ |
| Smoke test script | ≥1 | ___ | ___ |
| Load test raporu | ≥1 | ___ | ___ |
| SSL/nginx config | ≥2 | ___ | ___ |
| Backup script | ≥1 | ___ | ___ |
| Toplam test | ≥4200 | ___ | ___ |
| Test coverage raporu | ≥1 | ___ | ___ |
| Repo'da credential | 0 | ___ | ___ |


# ═══════════════════════════════════════════════════════════════════════════════
# BÖLÜM 3: ÇALIŞTIRMA TALİMATI
# ═══════════════════════════════════════════════════════════════════════════════

## İLK ÇALIŞTIRMA (hiç uygulanmamış)

```
1. Her BİLEŞEN'in FAZ 1'ini sırayla çalıştır
2. Çıktıları MEGA_DURUM_RAPORU.md'ye yaz
3. Her BİLEŞEN'in FAZ 2'sini doldur (delta tabloları)
4. Delta > 0 olanlar için FAZ 3 planı oluştur
5. 5 DEV paralel: her DEV kendi sahiplik alanındaki görevleri uygula (FAZ 4)
6. Her görev sonrası ÖNCE/SONRA kanıt
7. Tüm görevler bittikten sonra FAZ 5: tüm keşifleri tekrar çalıştır
8. Kalan delta > 0 ise → FAZ 3'e dön
```

## İKİNCİ ÇALIŞTIRMA (zaten kısmen uygulanmış — iyileştirme)

```
1. Her BİLEŞEN'in FAZ 1'ini tekrar çalıştır
2. Önceki MEGA_DURUM_RAPORU.md ile kıyasla
3. Delta = 0 olan bileşenler → ATLA
4. Delta > 0 olan bileşenler → ama bu sefer DELTAlar daha küçük
5. Daha az iş, daha yüksek kalite, daha derin detay
6. "Zaten yapılmışı tekrarlama" kuralı burada çalışır
```

## ÜÇÜNCÜ ÇALIŞTIRMA (micro-polish)

```
1. Neredeyse tüm deltalar 0 olacak
2. Kalan micro-deltalar: 1-2 TODO kalmış, 3-4 placeholder var gibi
3. Bu çalıştırma "dünya lideri" seviyeye taşır
4. Faz 5 sonucu: tüm deltalar = 0 → EMİRNAME TAMAMLANDI
```


# ═══════════════════════════════════════════════════════════════════════════════
# BÖLÜM 4: HEDEF METRİK ÖZET TABLOSU (TEK BAKIŞTA)
# ═══════════════════════════════════════════════════════════════════════════════

| # | Bileşen | KRİTİK Metrik | HEDEF | Sahip |
|---|---------|---------------|-------|-------|
| 01 | Tema | Hardcoded #2855AC | 0 dosya | DEV 2 |
| 01 | Tema | DynamicResource | ≥500 | DEV 2 |
| 02 | Shell | Hardcoded admin | 0 | DEV 1 |
| 02 | Shell | Core.AppDbContext ref | 0 | DEV 1 |
| 02 | Shell | ServiceLocator ref | 0 | DEV 1 |
| 03 | Dashboard | Mock data | 0 | DEV 2 |
| 04 | Ürün | Bulk import | ≥2 dosya | DEV 1+2 |
| 05 | Sipariş | OrderBulkProcess | ≥2 dosya | DEV 1 |
| 06 | Stok | FIFO maliyet | ≥2 dosya | DEV 1 |
| 07 | Muhasebe | CultureInfo olmayan Parse | 0 | DEV 1 |
| 08 | E-Fatura | UBL-TR builder | ≥2 dosya | DEV 3 |
| 09 | CRM | Loyalty entity | ≥2 | DEV 1 |
| 09 | CRM | Campaign entity | ≥2 | DEV 1 |
| 10 | Platform | NotImplementedException | 0 | DEV 3 |
| 11 | Rapor | Prometheus alert | ≥1 YAML | DEV 4 |
| 12 | Temizlik | Boş catch | <30 | TÜM |
| 12 | Temizlik | innerHTML (XSS) | 0 sanitize edilmemiş | DEV 2 |
| 12 | Temizlik | Avalonia placeholder | 0 | DEV 2 |
| 12 | Temizlik | Blazor TODO/STUB | 0 | DEV 4 |
| 12 | Temizlik | HTML mock data | 0 | DEV 2 |
| 13 | MESA | Consumer log-only | 0 (tümü MediatR) | DEV 3 |
| 13 | MESA | SignalR Hub | ≥1 | DEV 3 |
| 14 | ERP | Conflict resolver | ≥2 dosya | DEV 3 |
| 15 | Blazor | Gerçek API bağlantı | ≥50 | DEV 4 |
| 16 | Avalonia | Placeholder view | 0 | DEV 2 |
| 17 | i18n | .resx key | ≥500 | DEV 2+4 |
| 18 | Production | Rollback prosedürü | ≥1 | DEV 4 |
| 18 | Production | Load test raporu | ≥1 | DEV 5 |
| 18 | Production | Credential audit | 0 repo'da | DEV 4 |


# ═══════════════════════════════════════════════════════════════════════════════
# BÖLÜM 5: NEDEN BU D10/D11'DEN FARKLI
# ═══════════════════════════════════════════════════════════════════════════════
#
# D10: "Bu dosyaları oluştur" → Agent oluşturdu → Ama derinlik yok
# D11: "Bu dosyaları iyileştir" → Agent iyileştirdi → Ama keşif sona atılmış
# MEGA: "Önce ölç → deltaları bul → sadece deltaları kapat → tekrar ölç"
#
# FARK 1: Keşif her bileşenin İÇİNDE, ayrı belge değil
# FARK 2: Hedef metrik sayısal ve ölçülebilir (0, ≥500, ≤30)
# FARK 3: Delta=0 ise ATLA — boş iş yapma
# FARK 4: Her çalıştırma kendi durumunu ölçer (idempotent)
# FARK 5: Yeni dosya oluşturma YASAK — mevcut genişletilir
# FARK 6: "Stub var" ile "içerik var" ayrımı net
# FARK 7: 18 ayrı emirname yerine 1 mega emirname — bağlam kaybı sıfır
#
# ══════════════════════════════════════════════════════════════════════════════


# ═══════════════════════════════════════════════════════════════════════════════
# EMİRNAME SONU
# ═══════════════════════════════════════════════════════════════════════════════
# Bu emirname ilk kez çalıştırıldığında: tam keşif + tam uygulama
# İkinci kez çalıştırıldığında: delta keşif + kalan eksikler
# Üçüncü kez çalıştırıldığında: micro-polish → dünya lideri kalite
# Her seferinde daha iyi, hiçbir şey bozulmaz, kuşgeçirmez.
#
# "Dünya lideri olmak istiyorsan, önce dünya lideri gibi ÖLÇ."
# ═══════════════════════════════════════════════════════════════════════════════
