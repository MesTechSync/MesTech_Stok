# ╔═══════════════════════════════════════════════════════════════╗
# ║ KAYIP ÖNLEME KURALLARI — TÜM AGENT'LAR İÇİN ZORUNLU        ║
# ╠═══════════════════════════════════════════════════════════════╣
# ║ DK-09: 1 düzelt → 1 commit. Toplu commit YASAK.             ║
# ║        lint-staged stash commit'siz kodu SİLER.              ║
# ║ DK-10: git commit --no-verify kullan. Hook kodu kaybediyor.  ║
# ║ DK-11: Branch'te bitirdin → hemen main'e merge et.           ║
# ║        Merge edilmeyen kod YOK demektir.                     ║
# ║ DK-12: git stash YASAK. Commit et veya discard et.           ║
# ║ DK-13: git add . YASAK. Dosya dosya ekle.                   ║
# ║ DK-14: Soru sorma. Karar ver. Uygula.                       ║
# ║ DK-15: Spotlight WelcomeWindow KORUMALI TASARIM.            ║
# ║        Katman yapısı (Layer 0-5) değiştirilemez.            ║
# ║        Login güvenlik akışı (brute-force, audit) dokunulmaz.║
# ║        İyileştirme = zarifleştirme, kolaya kaçma YASAK.     ║
# ║        Köklü layout değişikliği = ÖNCE tasarım onayı.      ║
# ╚═══════════════════════════════════════════════════════════════╝
# MesTechStok — Claude Code Instructions

## Project Overview
MesTechStok is a multi-layer enterprise stock management platform with Avalonia desktop (cross-platform), Blazor web, WPF desktop, and HTML dashboard frontends.

## Architecture
- **MesTech.Domain**: Domain entities, value objects, events (Clean Architecture core)
- **MesTech.Application**: CQRS commands/queries, validators, DTOs, MediatR handlers
- **MesTech.Infrastructure**: Persistence (EF Core + PostgreSQL), Integration adapters, Messaging (MassTransit + RabbitMQ), Jobs (Hangfire)
- **MesTech.WebApi**: ASP.NET Core Web API (JWT + Swagger)
- **MesTech.Avalonia**: Cross-platform desktop UI (Avalonia 11)
- **MesTech.Blazor**: Web SaaS UI (Blazor Server)
- **MesTechStok.Desktop**: Legacy WPF desktop (MaterialDesign)
- **MesTechStok.Core**: Legacy shared layer (being eliminated → CQRS migration)

## Golden Rules — MUST FOLLOW
1. **NEVER modify existing 88 WPF Views** — Only ADD new screens
2. **Build must pass after every change** — `dotnet build` → 0 errors
3. **Tests must not regress** — `dotnet test` → 0 failed
4. **Additive approach**: ADD new screens, DON'T CHANGE existing ones
5. **No secrets in code**: Use environment variables or user-secrets
6. **git add . YASAK** — dosya belirterek ekle

## Tech Stack
- .NET 9.0, Avalonia 11, Blazor Server, WPF, PostgreSQL, EF Core 9.0
- MediatR (CQRS), MassTransit (messaging), Hangfire (jobs)
- MaterialDesignThemes, CommunityToolkit.Mvvm
- BCrypt.Net-Next 4.0.3 (auth)
- Web: Bootstrap 5.3, FontAwesome 6.4, Chart.js

## Key Paths
- Domain: `src/MesTech.Domain/`
- Application: `src/MesTech.Application/`
- Infrastructure: `src/MesTech.Infrastructure/`
- WebApi: `src/MesTech.WebApi/`
- Avalonia UI: `src/MesTech.Avalonia/`
- Blazor UI: `src/MesTech.Blazor/`
- WPF Desktop: `src/MesTechStok.Desktop/`
- Legacy Core: `src/MesTechStok.Core/`
- Unit Tests: `src/MesTech.Tests.Unit/`
- Integration Tests: `src/MesTech.Tests.Integration/`
- Architecture Tests: `src/MesTech.Tests.Architecture/`

## Coding Conventions
- C#: PascalCase for public members, _camelCase for private fields
- XAML/AXAML: Use MaterialDesign/Avalonia controls and theming
- HTML/JS: Bootstrap 5 components, vanilla JS (no framework)
- Always use UTF-8 encoding
- Indent: 4 spaces for C#/XAML, 2 spaces for HTML/JS/CSS/JSON
- Commit format: `<type>(<scope>): <description>` (Conventional Commits, English)

## Build Verification
```bash
dotnet build src/MesTech.Domain/ 2>&1 | tail -5          # 0 error
dotnet build src/MesTech.Application/ 2>&1 | tail -5     # 0 error
dotnet build src/MesTech.Infrastructure/ 2>&1 | tail -5  # 0 error
dotnet test tests/ --no-build -v q 2>&1 | tail -10       # 0 failed
```

# ═══════════════════════════════════
# MEGA EMİRNAME — ÇEVRİMLİ İYİLEŞTİRME KURALLARI
# ═══════════════════════════════════

## DEMİR KURALLAR (HER DEV İÇİN GEÇERLİ)

DK-01: SAYI YOKSA BİTMEMİŞTİR — her görev ÖNCE/SONRA grep kanıtı ile.
DK-02: YENİ DOSYA OLUŞTURMA YASAK — önce `find . -name "*BenzerAd*"` çalıştır, varsa genişlet.
DK-03: BUILD+TEST HER BATCH SONRASI — `dotnet build` 0 error + `dotnet test` 0 failed.
DK-04: ATOMİK COMMIT — `git add .` YASAK. Dosya dosya ekle.
DK-05: KEŞİF FAZINDA KOD YAZILMAZ — önce ölç, sonra planla, sonra yaz.
DK-06: DELTA=0 İSE ATLA — zaten yapılmış şeyi tekrarlama.
DK-07: DEV SINIR İHLALİ YASAK — sadece kendi dosya alanına dokun.
DK-08: STUB ≠ TAMAMLANDI — dosya var ama TODO/placeholder içeriyorsa = eksik.

## 5 DEV DOSYA SAHİPLİK MATRİSİ

```
DEV 1 — BACKEND & DOMAIN:
  src/MesTech.Domain/**
  src/MesTech.Application/**
  src/MesTech.Infrastructure/Persistence/**
  src/MesTech.WebApi/**
  Core.AppDbContext elimination (MesTechStok.Core/ ve Desktop/ içindeki ref'ler)

DEV 2 — FRONTEND & UI:
  src/MesTech.Avalonia/Views/**
  src/MesTech.Avalonia/Themes/**
  src/MesTech.Avalonia/Dialogs/**
  src/MesTech.Avalonia/ViewModels/**
  src/MesTechStok.Desktop/Views/**
  frontend/panel/**
  Tema token, renk migrasyonu, placeholder temizliği, innerHTML→DOMPurify

DEV 3 — ENTEGRASYON & MESA OS:
  src/MesTech.Infrastructure/Integration/**
  src/MesTech.Infrastructure/Messaging/**
  src/MesTech.Infrastructure/Jobs/**
  Consumer MediatR geçişi, adapter derinleştirme

DEV 4 — DEVOPS & BLAZOR:
  docker/**
  .github/workflows/**
  Scripts/**
  src/MesTech.Blazor/**
  Credential temizlik, Blazor STUB temizlik, production readiness

DEV 5 — TEST & KALİTE:
  tests/**
  Coverage raporları, skip/stub test temizliği
  Doğrulama raporları (her P seviyesi sonunda Faz 1 tekrar)
```

## ÇEVRİMLİ MODEL

Her görev şu döngüyü takip eder:
1. KEŞİF: grep/find ile mevcut durumu say
2. DELTA: hedef metrik ile kıyasla
3. PLAN: sadece delta>0 olanlar için görev listesi
4. UYGULA: mevcut dosyayı bul→oku→genişlet (yeni dosya son çare)
5. DOĞRULA: aynı grep komutunu tekrar çalıştır → delta azaldı mı?

## REFERANS DOSYALAR

- `Docs/MEGA/ENT-SONSUZ-001_EMIRNAME.md` — **AKTİF EMİRNAME** — 6 seviyeli sonsuz iyileştirme döngüsü
- `Docs/MEGA/DETAY_ATLASI.md` — codebase kanıtlı mevcut durum (21 Mart 2026, derin tarama)
- `Docs/MEGA/prompts/DEV{1-5}_PROMPT.md` — 5 DEV başlangıç prompt'ları
- `Docs/MEGA/MEGA_DELTA_RAPORU_v1.md` — delta tabloları (referans)
- `Docs/MEGA/MEGA_EMIRNAME_CEVRIMLI_MUKEMMEL_v1.md` — eski emirname (referans)
