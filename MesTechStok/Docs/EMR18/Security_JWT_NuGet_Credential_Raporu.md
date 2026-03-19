# Guvenlik Audit: JWT + NuGet + Credential — 2026-03-19

**Auditor:** G-01b (Read-only analiz)
**Kapsam:** `MesTech_Stok/MesTechStok` — WebApi, Infrastructure, Desktop, Blazor

---

## 1. JWT Token

| Kontrol | Durum | Not |
|---------|-------|-----|
| Token suresi | 480dk (8 saat) | Onerilen: 60dk access. 480dk uzun — P1 |
| Secret from env/user-secrets | ✅ | appsettings placeholder: `CHANGE_IN_USER_SECRETS_minimum_32_chars` |
| Secret bos kontrolu | ✅ | Constructor'da `InvalidOperationException` firlatiyor |
| Secret uzunluk kontrolu | ✅ | Min 32 char zorunlu (HMAC-SHA256) |
| ValidateLifetime | ✅ | `TokenValidationParameters.ValidateLifetime = true` |
| ValidateIssuer | ✅ | `ValidIssuer = "mestech"` |
| ValidateAudience | ✅ | `ValidAudience = "mestech-clients"` |
| ValidateIssuerSigningKey | ✅ | Aktif |
| ClockSkew | 2dk | Onerilen: 5dk. 2dk biraz dar ama kabul edilebilir |
| Algorithm kontrolu | ✅ | Header alg === HmacSha256 kontrolu var |
| Refresh token | ❌ | Mekanizma YOK — sadece access token. P1 |
| Login endpoint auth | ⚠️ P0 | Placeholder login: BCrypt yok, herhangi bir user/pass ile token donuyor |
| Login rate limiting | ❌ P0 | `/api/v1/auth` grubunda `RequireRateLimiting` YOK — brute-force acik |

### JWT Detay Bulgulari

- **P0 — Placeholder Login (AuthEndpoints.cs:16-54):** Login endpoint herhangi bir bos olmayan kullanici adi/sifre ciftini kabul edip JWT donduruyor. BCrypt dogrulama ve gercek User entity sorgusu "Dalga 9 Task 5"e ertelenmis. Bu haliyle production'a GIDEMEZ.
- **P0 — Auth Rate Limiting Eksik:** `AuthEndpoints` grubunda `.RequireRateLimiting("PerApiKey")` cagirisi YOK. Brute-force saldirisi engelsiz.
- **P1 — 480dk Token Suresi:** 8 saatlik access token uzun. Refresh token mekanizmasi olmadigi icin kisaltmak oturum deneyimini bozabilir, ancak refresh token eklendikten sonra 60dk'ya cekilmeli.
- **P1 — Refresh Token Yok:** Token expire olunca kullanici yeniden login olmak zorunda. Refresh token mekanizmasi eklenmeli.

---

## 2. NuGet

### Preview Paketler

| Paket | Versiyon | Risk |
|-------|----------|------|
| LiveChartsCore.SkiaSharpView.WPF | 2.0.0-rc6.1 | Dusuk — UI chart kutuphanesi, guvenlik etkisi minimal |

- **Not:** `Directory.Packages.props` dosyasinda aciklama mevcut: "Upgrade to 2.0.0 stable when released." Kabul edilebilir.

### Vulnerable Paketler

- `dotnet list package --vulnerable` komutu calistirilamadi (Bash kisitlamasi). **Onerilen aksiyon:** CI pipeline'da `dotnet list package --vulnerable --include-transitive` gate'i zaten mevcut olmali — dogrulanmali.

### Onerilen Aksiyon

- LiveChartsCore stable cikinca guncelle (P2)
- CI'da vulnerability gate'i dogrula (P2)

---

## 3. Credential in Git

### .env Dosyalari

| Kontrol | Durum |
|---------|-------|
| `.env` tracked in git | ❌ Yok (temiz) |
| `.env` in src/ directory | ❌ Yok (temiz) |
| `.gitignore` .env kurali | Dogrulanamadi (Bash kisitlamasi) |

### Hardcoded Secret Taramasi

| Dosya | Durum | Not |
|-------|-------|-----|
| WebApi/appsettings.json — Jwt:Secret | ✅ Guvenli | `CHANGE_IN_USER_SECRETS_minimum_32_chars` placeholder |
| WebApi/appsettings.json — MinIO:SecretKey | ✅ Guvenli | `** USER SECRETS **` placeholder |
| Desktop/appsettings.json — ConnectionString | ✅ Guvenli | `** USER SECRETS **` placeholder |
| Desktop/appsettings.json — ApiKey degerler | ✅ Guvenli | `** USER SECRETS **` placeholder |
| Blazor/appsettings.json — ConnectionString | ✅ Guvenli | `CONFIGURED_VIA_USER_SECRETS` placeholder |
| Sandbox/appsettings.Sandbox.json | ✅ Guvenli | Tum credential'lar `Notes` olarak user-secrets yonlendirmesi |
| ERP/Parasut — ClientId, ClientSecret | ✅ Guvenli | Bos string (placeholder) |
| Desktop/appsettings.json — UpcDatabaseApiKey | ⚠️ P2 | Deger: `"trial"` — dusuk risk ama placeholder olmali |
| WebApi/appsettings.json — ValidApiKeys | ✅ Guvenli | Bos array `[]` |

### Git History Credential

- Git log taramasi Bash kisitlamasi nedeniyle yapilamadi. CI pipeline'daki `secret-scan` adimi bu riski kapatiyor olmali — dogrulanmali.

---

## 4. HTTPS

| Kontrol | Durum | Not |
|---------|-------|-----|
| UseHttpsRedirection (WebApi) | ❌ | WebApi Program.cs'de `UseHttpsRedirection` CAGRILMIYOR |
| UseHsts (Blazor) | ✅ | Blazor Program.cs'de `app.UseHsts()` var |
| UseHttpsRedirection (Blazor) | ❌ | Blazor'da da `UseHttpsRedirection` bulunamadi |

### Hardcoded http:// URL'ler

| Dosya | URL | Risk |
|-------|-----|------|
| MesTechApiClient.cs | `http://localhost:5100` | Dusuk — localhost fallback |
| RealtimeDashboardEndpoint.cs | `http://localhost:{port}/` | Dusuk — lokal HttpListener |
| App.xaml.cs | `http://localhost:5341` (Seq) | Dusuk — lokal servis |
| RealMesaEventPublisher.cs | `http://localhost:3000` | Dusuk — lokal MESA |
| MesaStatusEndpoint.cs | `http://localhost:{port}/` | Dusuk — lokal listener |
| ProductionMesaAIService.cs | `http://localhost:3000/api` | Dusuk — lokal AI |
| WebApi/Program.cs | `http://localhost:5341` (Seq) | Dusuk — lokal Seq |
| SpeechToExpenseService.cs | `http://localhost:3101` | Dusuk — lokal servis |
| AdvisoryAgentV2/Client | `http://localhost:3101` | Dusuk — lokal servis |

**Degerlendirme:** Tum http:// URL'leri localhost/lokal servisler icin. Production'da Traefik SSL (Coolify) bu servisleri internal network'te tutuyor. `UseHttpsRedirection` eksikligi WebApi ve Blazor'da P1 olarak not edilmeli — production reverse-proxy arkasinda olsa bile defense-in-depth icin eklenmeli.

---

## 5. Rate Limiting

### Konfigrasyon

| Parametre | Deger | Degerlendirme |
|-----------|-------|---------------|
| Policy adi | `PerApiKey` | ✅ API key bazli partitioning |
| PermitLimit | 100 | ✅ Makul (100 istek/dk) |
| Window | 1 dakika | ✅ Fixed window |
| QueueLimit | 0 | ✅ Kuyruk yok — asim hemen reddedilir |
| RejectionStatusCode | 429 | ✅ RFC uyumlu |

### Endpoint Kapsami

| Endpoint Grubu | Rate Limited | Not |
|----------------|-------------|-----|
| 51 endpoint dosyasi | ✅ | `RequireRateLimiting("PerApiKey")` mevcut |
| AuthEndpoints | ❌ P0 | Login brute-force riski |
| HealthEndpoints | ❌ | Kabul edilebilir (monitoring) |
| SeedEndpoints | ❌ P1 | Veri seed endpoint'i korumasiz |
| WebhookEndpoints | ❌ P1 | Webhook flood riski |

---

## P0 Bulgulari (deploy engeller)

| # | Bulgu | Dosya | Aksiyon |
|---|-------|-------|---------|
| P0-1 | Placeholder login — BCrypt dogrulama YOK | `WebApi/Endpoints/AuthEndpoints.cs:16-54` | Gercek User entity + BCrypt.Verify eklenmeli |
| P0-2 | Auth endpoint rate limiting YOK | `WebApi/Endpoints/AuthEndpoints.cs:13` | `.RequireRateLimiting("PerApiKey")` veya ozel auth limiter eklenmeli |

## P1 Bulgulari (ilk haftada)

| # | Bulgu | Dosya | Aksiyon |
|---|-------|-------|---------|
| P1-1 | JWT token suresi 480dk (8 saat) | `Infrastructure/Auth/JwtTokenOptions.cs:12` | Refresh token eklenince 60dk'ya cek |
| P1-2 | Refresh token mekanizmasi YOK | `Infrastructure/Auth/JwtTokenService.cs` | Refresh token entity + endpoint ekle |
| P1-3 | UseHttpsRedirection eksik (WebApi) | `WebApi/Program.cs` | `app.UseHttpsRedirection()` ekle |
| P1-4 | SeedEndpoints rate limiting YOK | `WebApi/Endpoints/SeedEndpoints.cs` | `.RequireRateLimiting("PerApiKey")` ekle |
| P1-5 | WebhookEndpoints rate limiting YOK | `WebApi/Endpoints/WebhookEndpoints.cs` | `.RequireRateLimiting("PerApiKey")` ekle |

## P2 Bulgulari (sonraki sprint)

| # | Bulgu | Dosya | Aksiyon |
|---|-------|-------|---------|
| P2-1 | LiveChartsCore preview paketi (rc6.1) | `Directory.Packages.props:10` | Stable cikinca guncelle |
| P2-2 | UpcDatabaseApiKey = "trial" | `Desktop/appsettings.json:92` | Placeholder'a cevir |
| P2-3 | CI vulnerability gate dogrulama | `.github/workflows/ci.yml` | `dotnet list package --vulnerable` adimi dogrula |
| P2-4 | UseHttpsRedirection eksik (Blazor) | `Blazor/Program.cs` | Defense-in-depth icin ekle |

---

## Ozet Skor

| Alan | Skor | Not |
|------|------|-----|
| JWT Token Guvenlik | 6/10 | Iyi altyapi, placeholder login P0 |
| NuGet Guvenlik | 9/10 | Sadece 1 preview, gercek risk yok |
| Credential Yonetimi | 9/10 | User-secrets disiplini cok iyi |
| HTTPS | 6/10 | Reverse proxy var ama defense-in-depth eksik |
| Rate Limiting | 8/10 | 51/55 endpoint korunuyor, auth acik |
| **Genel** | **7.6/10** | **2 P0, 5 P1, 4 P2** |
