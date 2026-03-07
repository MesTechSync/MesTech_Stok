# TAKIM 4 RAPORU: DEVOPS & ALTYAPI ANALiZi

**Kontrolor:** Claude Opus 4.6
**Tarih:** 06 Mart 2026
**Emirname Ref:** ENT-MD-001-T4

---

## A. BACKUP SiSTEMi DERiN ANALiZ

### A.1 config.json Tam Analizi

**Dosya:** `MesTech_BackupSystem/BackupSystem/config.json`

**Yedekleme Hedefleri (4 OpenCart instance):**

| Site Adi | Path | Port | DB | Aciklama |
|----------|------|------|----|----------|
| MesTech_Master_Template | /Users/mezbjen/Desktop/MesTech_Stok/Opencart_Master_Template | 9000 | opencart_master | Temiz kurulum sablonu |
| MesTech_Stok_Main | /Users/mezbjen/Desktop/MesTech_Stok/Opencart_Stok | 8000 | opencart_stok | Ana stok sistemi |
| MesTech_Stok_Test | /Users/mezbjen/Desktop/MesTech_Stok/Opencart_Stok_02 | 8080 | opencart_stok_02 | Test/yedek sistem |
| MesTech_Test_Clean | /Users/mezbjen/Desktop/MesTech_Stok/Opencart_Stok_Test_NEW | 8080 | opencart_test_new | Cache sorunlari cozulmus test |

**KRITIK BULGU - Path Uyumsuzlugu:**
- config.json'daki tum path'ler **macOS** formatinda: `/Users/mezbjen/Desktop/MesTech_Stok/...`
- Mevcut calisma ortami **Windows**: `c:\MesChain-Sync-Enterprise\MesChain\MesTech\...`
- Opencart_Stok config.php'deki path: `c:/MesChain-Sync-Enterprise/MesChain-Sync-Enterprise/MesTech/MesTech_Opencart/Opencart_Stok/`
- **Sonuc:** BackupSystem mevcut ortamda CALISMAZ. Path'ler guncellenmeli.

**Port Catismasi:**
- MesTech_Stok_Test ve MesTech_Test_Clean AYNI PORT'u kullaniyor: **8080**
- Bu ikisi ayni anda calisamaz.

**Schedule (Zamanlanmis Yedekleme):**
- config.json'da schedule TANIMLI DEGIL
- Cron job desteyi var ama yapilandirilmamis
- `developer_mode.auto_cleanup: true` ile eski yedekler otomatik silinir

**Retansiyon Politikasi:**
```json
"max_backups": 10,
"developer_mode": {
    "keep_recent": 5,
    "auto_cleanup": true
}
```
- Genel mod: Son 10 yedek tutulur
- Developer mod: Son 5 yedek tutulur, otomatik temizlik ACIK

**Sikistirma:**
```json
"compression_level": 6
```
- Dengeli seviye (1-9 arasi, 6 orta)
- performance_optimizer.py dosya boyutuna gore dinamik ayar yapiyor

**Veritabani Bilgileri:**
```json
"database": {
    "host": "localhost",
    "port": 3306,
    "username": "root",
    "password": "1234"
}
```
- **KRITIK GUVENLIK:** Sifre plaintext olarak config.json'da! (`"password": "1234"`)
- root kullanici kullaniliyor — ozel kullanici olusturulmali

**Exclude Pattern'leri:**
```json
"exclude_patterns": [
    "*/cache/*", "*/logs/*", "*/session/*",
    "*/.git/*", "*/.DS_Store", "*/node_modules/*",
    "*/vendor/composer/installed.json",
    "*/storage/backup/*", "*/system/storage/cache/*",
    "*/system/storage/logs/*", "*/system/storage/session/*",
    "*.tmp", "*.log"
]
```
- Mantikli ve kapsamli exclude listesi

### A.2 Hangi Projeleri Yedekliyor?

- **SADECE OpenCart instance'larini** yedekliyor
- MesTech_Stok (WPF): YEDEKLENMIYOR
- MesTech_Trendyol: YEDEKLENMIYOR
- MesTech_Dashboard: YEDEKLENMIYOR
- BackupSystem adi: "OpenCart Yedekleme Sistemi" — sadece OpenCart icin tasarlanmis

### A.3 Web Arayuzu (Port 5001)

**Dosya:** `MesTech_BackupSystem/BackupSystem/web_interface.py`

**API Endpoint'leri:**
| Endpoint | Method | Islem |
|----------|--------|-------|
| `/` | GET | Ana sayfa (istatistik + yedek listesi) |
| `/api/backups` | GET | Yedek listesi |
| `/api/statistics` | GET | Istatistikler |
| `/api/backup/create` | POST | Yeni yedek olustur |
| `/api/backup/restore` | POST | Geri yukle |
| `/api/backup/download/<name>` | GET | Yedek indir (ZIP) |
| `/api/backup/details/<name>` | GET | Yedek detaylari |
| `/api/backup/delete/<name>` | DELETE | Yedek sil |
| `/api/sites` | GET | Site listesi |
| `/api/system/validate` | GET | Sistem gereksinimleri kontrolu |
| `/api/health` | GET | Saglik kontrolu |

**Guvenlik Uyarisi:**
```python
app.config['SECRET_KEY'] = 'mestech_backup_secret_key_2025'
```
- SECRET_KEY hardcoded — environment variable'a tasinmali
- CORS tamamen acik: `cors_allowed_origins="*"`
- Authentication/authorization YOK

### A.4 Geri Yukleme (Restore) Proseduru

- `backup_system.restore_backup(backup_name, site_name)` metodu MEVCUT
- Web arayuzunden `/api/backup/restore` ile tetiklenebilir
- Thread ile arka planda calisir
- Socket.IO ile `restore_completed` / `restore_error` event'leri emit eder
- **Eksik:** Restore oncesi mevcut durumun otomatik yedekelenmesi yok

### A.5 Flask-SocketIO Kullanimi

- **Amac:** Gercek zamanli ilerleme bildirimi
- `backup_completed`, `backup_error`, `restore_completed`, `restore_error` event'leri
- `request_progress` handler tanimli ama **IMPLEMENTASYON EKSIK** ("gelecekte implementasyon icin" yorumu)
- Yedekleme/geri yukleme thread ile arka planda calisir, sonuc Socket.IO ile bildirilir

### A.6 performance_optimizer.py

**Dosya:** `MesTech_BackupSystem/BackupSystem/performance_optimizer.py`

**BackupOptimizer sinifi:**
- CPU/RAM/Disk bilgilerine gore optimal thread sayisi hesaplama
- Dosya boyutuna gore dinamik sikistirma seviyesi (1-9)
- Paralel dosya kopyalama (ThreadPoolExecutor)
- Optimize edilmis ZIP olusturma/cikarma
- SHA-256 ile dosya butunluk dogrulama
- Sistem kaynak izleme (CPU >%90 veya RAM >%95 ise duraklatma)

**DatabaseOptimizer sinifi:**
- Optimize edilmis mysqldump komutu (`--single-transaction`, `--compress`, `--opt`)
- Optimize edilmis mysql import komutu
- Connection pooling desteyi

### A.7 security_manager.py

**Dosya:** `MesTech_BackupSystem/BackupSystem/security_manager.py`

**SecurityManager sinifi:**
- **Sifreleme:** Fernet (AES-128-CBC) — `cryptography` kutuphanesi
  - `encryption_enabled` flag ile acilip kapatilabilir (varsayilan: KAPALI)
  - PBKDF2-HMAC-SHA256 ile parola tabanli anahtar turetme (100.000 iterasyon)
  - Anahtar dosyasi: `.backup_key` (chmod 600)
- **Dijital imza:** HMAC-SHA256 ile yedek imzalama
- **Guvenli silme:** 3-pass random overwrite
- **Butunluk manifesti:** Dosya hash + boyut + izin bilgisi
- **Path sanitization:** Path traversal saldirilarini onleme
- **Erisim loglama:** Ayri security.log dosyasi

**Durum:** Modul MEVCUT ama `encryption_enabled: false` (varsayilan). Config'de security bolumu YOK.

### A.8 Entegrasyon Durumu

- BackupSystem bagimsiz bir Flask uygulamasi
- Docker'a ALINMAMIS — localhost'ta Python venv ile calisiyor
- Ana proje (MesTech_Stok WPF) ile ENTEGRASYON YOK
- MySQL kullaniliyor (OpenCart icin), ana proje PostgreSQL kullaniyor — farkli DB engine'ler

---

## B. OPENCART HOSTING DERiN ANALiZ

### B.1 Instance'lar

**3 instance repoda, 4 instance config.json'da:**

| Instance | Port | DB | Amac | Path Formati |
|----------|------|----|------|-------------|
| Opencart_Stok | 8000 | opencart_stok | Ana uretim sistemi | Windows (config.php) |
| Opencart_Stok_02 | 8080 | opencart_stok_02 | Test/yedek sistemi | macOS (config.php) |
| Opencart_Stok_03 | 9000 | opencart_stok_03 | 3. instance | macOS (config.php) |

**KRITIK: Path tutarsizligi:**
- Opencart_Stok config.php: `c:/MesChain-Sync-Enterprise/MesChain-Sync-Enterprise/MesTech/MesTech_Opencart/Opencart_Stok/` (Windows, YANLIS path — "MesChain-Sync-Enterprise" 2 kez tekrarlanmis)
- Opencart_Stok_02 config.php: `/Users/mezbjen/Desktop/MesTech_Stok/Opencart_Stok_02/` (macOS)
- Opencart_Stok_03 config.php: `/Users/mezbjen/Desktop/MesTech_Stok/Opencart_Stok_03/` (macOS)

**Sonuc:** Instance 02 ve 03 macOS path'leriyle konfigüre edilmis, Windows'ta calismaz.

### B.2 Nerede Calisiyor?

- **Localhost** uzerinde PHP built-in server veya Apache/MAMP
- Docker'da DEGIL
- VPS/shared hosting'de DEGIL
- Her instance ayri port uzerinden calisiyor
- MySQL 3306 portunda localhost

### B.3 MySQL Yapilandirmasi

- Her instance AYRI veritabani kullaniyor
- Tum DB'ler ayni MySQL sunucusunda (`localhost:3306`)
- Ortak credentials: `root` / `1234`
- DB prefix: `oc_` (standart OpenCart)
- Driver: `mysqli`

### B.4 PHP Gereksinimleri

- OpenCart 4.0.2.3 versiyonu
- PHP 8.0+ gerekli
- config.php'de: `error_reporting(E_ALL & ~E_DEPRECATED)` — PHP 8 uyumluluk ayari
- Extensions: mysqli, zip, gd, curl (standart OpenCart gereksinimleri)

### B.5 Deploy Stratejisi

- **Manuel kurulum** — script ile otomatik deploy YOK
- Docker image YOK
- `opencart-4.0.2.3` klasoru repoda: temiz kurulum arsivi (tum core dosyalar repoda)
- `backup_original` klasoru: 3 instance'in yedekleri

### B.6 42K Dosya Sorunu

- OpenCart core dosyalari (admin/, catalog/, system/, extension/, image/, install/) **TUMU REPODA**
- `opencart-4.0.2.3` klasoru: temiz OpenCart indirme arsivi de repoda
- 3 instance x ~14K dosya = ~42K dosya
- **Bu dosyalar repoda OLMAMALI** — .gitignore ile haric tutulup deployment mekanizmasi kurulmali

### B.7 Multi-tenant Yaklasim

- Her instance AYRI bir OpenCart kurulumu (fiziksel kopya)
- Tek instance multi-store DEGIL
- Farkli portlarda, farkli DB'lerde calisiyor
- Storage path'leri ayri: `storagestok`, `storagestok2`, `storagestok3`

### B.8 BackupSystem ile Iliski

- BackupSystem config.json'da 4 OpenCart sitesi tanimli (Master Template dahil)
- config.json path'leri macOS formati — Windows'ta CALISMAZ
- BackupSystem bu instance'lari yedeklemek icin TASARLANMIS ama mevcut path konfigurasyonu UYUMSUZ

---

## C. CI/CD PIPELINE ANALiZi

### C.1 sync-ekural-to-all-repos.yml Detayli Analiz

**Dosya:** `.github/workflows/sync-ekural-to-all-repos.yml`

**Tetikleyiciler:**
1. `push` — `Docs/eKural/kural.md` degistiginde, sadece `main` branch
2. `workflow_dispatch` — Manuel tetikleme (target_repos + force_sync parametreleri)

**Hedef Repolar (14 adet):**
```
MesTech-AI, MesTech-Amazon, MesTech-Ciceksepeti, MesTech-Dashboard,
MesTech-Ebay, MesTech-Hepsiburada, MesTech-N11, MesTech-Opencart,
MesTech-Ozon, MesTech-Pazarama, MesTech-PttAVM, MesTech-Security,
MesTech-Stok, MesTech-Trendyol
```

**Calisma Akisi (3 Job):**
1. **prepare-sync:** Master hash hesapla, sync ID olustur, hedef repo listesi belirle
2. **sync-ekural:** Matrix strategy ile paralel sync (fail-fast: false)
   - Target repo checkout
   - Hash karsilastirmasi (guncel ise atla)
   - kural.md kopyala + repo-sync-info.md olustur + .ekural-sync tracking dosyasi
   - git commit & push
3. **sync-summary:** GitHub Step Summary olustur

**PAT Yonetimi:**
- `secrets.MESTECH_SYNC_TOKEN` kullaniliyor
- Token ile hem master hem target repolara erisim
- Git config: `MesTech AutoSync Bot` / `autosync@mestech.com`

**Basari/Hata Durumu:**
- `fail-fast: false` — bir repo basarisiz olsa bile diger repolar sync edilir
- Verify adimi: icerik dogrulama (`grep -q "eKural"`) basarisiz olursa `exit 1`
- Summary job `if: always()` ile her durumda calisir

### C.2 Build Otomasyonu

- **.NET build otomasyonu YOK** — CI/CD pipeline sadece eKural sync icin
- xUnit test workflow'u YOK
- Publish/deployment otomasyonu YOK

### C.3 MesTech_Published Yerine CI/CD

Mevcut durum: MesTech_Published klasorunde 63K+ dosya (node_modules dahil) elle commit edilmis.

**Onerilen CI/CD Akisi:**
```
push to main → .NET build → test → publish → artifact upload
```
- GitHub Actions ile `.NET publish` otomasyonu
- Artifact olarak GitHub Releases'e yukleme
- node_modules ASLA commit edilmemeli

---

## D. SCRIPTS ANALiZi

### D.1 Script Envanteri

| Script | Amac | Parametre | Simple vs Normal |
|--------|------|-----------|-----------------|
| `github-simple.ps1` | Git islemleri rehberi + interaktif menu | Interaktif (upload/pull/status/help) | Basit versiyon: tek menu, temel komutlar |
| `github-upload-rules.ps1` | Git islemleri tam arac seti | Interaktif menu (7 secenek) | Tam versiyon: Upload, Pull, Branch, Commit, Upstream Sync, Status, eKural |
| `setup-github-actions-simple.ps1` | GitHub Actions setup dogrulama | -TestWorkflow, -Force | Basit: Token, dosya yapisi, API erisim kontrolu |
| `setup-github-actions.ps1` | GitHub repo + workflow + secret olusturma | -CreateRepos, -SetupWorkflows, -SetupSecrets, -DryRun, -GitHubToken, -OrgName | Tam: 10 repo tanimli, repo olusturma, workflow setup, secret yonetimi |
| `sync-ekural-manual-simple.ps1` | eKural manuel sync (API tabanli) | -TargetRepos, -All, -Force, -DryRun, -CreateMissing | Basit: GitHub API ile direkt dosya guncelleme |
| `sync-ekural-manual.ps1` | eKural manuel sync (git clone tabanli) | -DryRun, -TargetRepos, -Force, -UseGitHubAPI, -TriggerGitHubActions, -SkipPush | Tam: Clone + commit + push, GitHub Actions tetikleme |
| `sync-status-dashboard-simple.ps1` | Sync durum kontrolu | -CheckGitHubActions, -ShowDetails | Basit: API ile hash karsilastirma |
| `sync-status-dashboard.ps1` | Sync durum dashboard (coklu format) | -Detailed, -CheckOnline, -OutputFormat (Console/JSON/CSV/HTML), -OutputFile | Tam: Online/offline kontrol, HTML rapor, CSV/JSON export |

**"Simple" vs Normal Fark:**
- Simple versiyonlar: Unicode sorunlarini onlemek icin emoji'siz, daha kisa
- Normal versiyonlar: Tam ozellikli, emoji'li, daha fazla parametre
- **Duplike DEGIL** — simple versiyonlar farkli implementasyon yaklasimlari kullaniyor (ornegin API-tabanli vs git-clone-tabanli)

**Aktif Kullanim:**
- Tum scriptler eKural sync mekanizmasi icin tasarlanmis
- GitHub orgName: `MezBjen`
- 14 hedef repo tanimli

### D.2 MesTech_Stok Scriptleri

**Dosya:** `MesTech_Stok/MesTechStok/Scripts/`
| Script | Amac |
|--------|------|
| `init-db.sql` | PostgreSQL 17 ilk kurulum: pgvector, uuid-ossp, pg_trgm, pg_stat_statements extension'lari |
| `daily-log-cleanup.ps1` | 30 gunluk log temizligi, boyut uyarisi (>100MB) |
| `weekly-build-check.ps1` | (Okunamadi — muhtemelen haftalik build kontrolu) |

---

## E. ORTAM YONETiMi

### E.1 Gelistirme Ortami Gereksinimleri

| Bilesken | Versiyon | Kullanim Yeri |
|----------|----------|--------------|
| Windows | 11 Pro 10.0.26200 | Host isletim sistemi |
| .NET SDK | 9.0 | MesTech_Stok (WPF), MesTech_Dashboard, MesTech_Trendyol |
| Node.js | ? (Belirtilmemis) | MesTech_Trendyol (NestJS) |
| PHP | 8.0+ | MesTech_Opencart (OpenCart 4.0) |
| Python | 3.9+ | MesTech_BackupSystem (Flask) |
| MySQL | 8.x (localhost:3306) | OpenCart veritabanlari |
| Docker Desktop | Gerekli | PostgreSQL 17 + Redis 7 + RabbitMQ 3 |
| Visual Studio | 2022+ | WPF gelistirme |
| PostgreSQL | 17 (Docker, pgvector/pgvector:pg17) | Ana uygulama DB |
| Redis | 7 (Docker, redis:7-alpine) | Cache + distributed lock |
| RabbitMQ | 3 (Docker, rabbitmq:3-management-alpine) | Event bus |

### E.2 Production Ortami

- **TANIMLANMAMIS** — production deployment stratejisi YOK
- WPF Desktop uygulamasi: MesTech_Published olarak elle paketlenmis
- OpenCart: localhost'ta calisma durumunda
- Staging/test ortami: RESMI OLARAK YOK (Opencart_Stok_02 test amacli kullaniliyor)

### E.3 .env Yonetimi

**KRITIK EKSIKLIK:**
- `.env` dosyasi MEVCUT DEGIL (MesTech_Stok icinde)
- `.gitignore`'da `.env` pattern'i YOK (sadece genel pattern'ler var)
- docker-compose.yml'de env var'lar varsayilan degerlerle tanimli:
  ```yaml
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-*** ENV_SIFRE ***}
  RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:-*** ENV_SIFRE ***}
  ```
- OpenCart config.php'lerde DB sifreleri hardcoded: `root` / `1234`
- BackupSystem config.json'da DB sifreleri hardcoded: `root` / `1234`
- Web interface SECRET_KEY hardcoded: `mestech_backup_secret_key_2025`

**Sifreler Toplam Envanter:**
| Konum | Sifre | Risk |
|-------|-------|------|
| docker-compose.yml | *** ENV_SIFRE *** (varsayilan) | ORTA — env var ile override edilebilir |
| OpenCart config.php (x3) | root/1234 | YUKSEK — plaintext, root kullanici |
| BackupSystem config.json | root/1234 | YUKSEK — plaintext, root kullanici |
| web_interface.py | SECRET_KEY hardcoded | YUKSEK — session hijack riski |

---

## F. REPO TEMiZLiK OPERASYON PLANI

### F.1 Mevcut Durum

```
MesTech/ (22 klasor)
├── DevTrednyol/          (typo: "Trednyol")
├── Docs/
├── Logs/
├── MesTech_AI/           (sadece Docs/README.md + AI dosyalari)
├── MesTech_Amazon_tr/    (sadece Docs/)
├── MesTech_BackupSystem/
├── MesTech_Ciceksepeti/  (sadece Docs/)
├── MesTech_Dashboard/
├── MesTech_Ebay/         (sadece Docs/)
├── MesTech_Hepsiburada/  (sadece Docs/)
├── MesTech_N11/          (sadece Docs/)
├── MesTech_Opencart/
├── MesTech_Ozon/         (sadece Docs/)
├── MesTech_Pazarama/     (sadece Docs/)
├── MesTech_PttAVM/       (sadece Docs/)
├── MesTech_Published/    (63K+ dosya, node_modules!)
├── MesTech_Security/     (sadece Docs/)
├── MesTech_Stok/
├── MesTech_Trendyol/
└── Scripts/
```

**Bos/minimal platform klasorleri (10 adet):**
MesTech_AI (AI dosyalari var, tamamen bos degil), MesTech_Amazon_tr, MesTech_Ciceksepeti, MesTech_Ebay, MesTech_Hepsiburada, MesTech_N11, MesTech_Ozon, MesTech_Pazarama, MesTech_PttAVM, MesTech_Security

### F.2 Adim Adim Plan

**ADIM 1: MesTech_Published Silme**
```bash
# 1. Once harici yedek al
cp -r MesTech_Published /harici/yedek/MesTech_Published_backup_$(date +%Y%m%d)

# 2. .gitignore'a ekle (once!)
echo "MesTech_Published/" >> .gitignore
git add .gitignore
git commit -m "chore: add MesTech_Published to gitignore"

# 3. Git index'ten kaldir
git rm -r --cached MesTech_Published/
git commit -m "chore: remove MesTech_Published from tracking (63K+ files incl node_modules)"

# 4. Git history temizligi (OPSIYONEL ama TAVSIYE EDILEN)
# BFG Repo-Cleaner ile:
java -jar bfg.jar --delete-folders MesTech_Published
git reflog expire --expire=now --all && git gc --prune=now --aggressive

# NOT: BFG sonrasi force push gerekir — tum ekip uyarilmali
```

**ADIM 2: Bos Platform Klasorlerini Tasi**
```bash
mkdir -p Docs/PlatformSpecs

# Her klasordeki Docs icerigini tasi
for dir in MesTech_Amazon_tr MesTech_Ciceksepeti MesTech_Ebay MesTech_Hepsiburada MesTech_N11 MesTech_Ozon MesTech_Pazarama MesTech_PttAVM MesTech_Security; do
  if [ -f "$dir/Docs/README.md" ]; then
    platform=$(echo $dir | sed 's/MesTech_//')
    cp "$dir/Docs/README.md" "Docs/PlatformSpecs/${platform}.md"
  fi
done

# MesTech_AI ozel durum (AI dosyalari var)
cp -r MesTech_AI/Docs/* Docs/PlatformSpecs/AI/

# Eski klasorleri sil
git rm -r MesTech_Amazon_tr MesTech_Ciceksepeti MesTech_Ebay MesTech_Hepsiburada MesTech_N11 MesTech_Ozon MesTech_Pazarama MesTech_PttAVM MesTech_Security
git rm -r MesTech_AI  # AI dosyalari tasindiktan sonra
git add Docs/PlatformSpecs/
git commit -m "refactor: move platform specs to Docs/PlatformSpecs, remove empty dirs"
```

**ADIM 3: .gitignore Guncelleme**
Mevcut .gitignore'a eklenmesi gerekenler:
```gitignore
# Environment files (KRITIK EKSIK)
.env
.env.*
*.env

# MesTech Published (CI/CD ile olusturulmali)
MesTech_Published/

# Logs
Logs/
logs/

# OpenCart storage (buyuk ve degisken)
**/storage/cache/
**/storage/session/
**/storage/logs/

# BackupSystem backups
**/backups/
**/backup_original/

# Python virtual environments (zaten var ama tekrar kontrol)
**/venv/
```

**ADIM 4: Repo Boyut Analizi**
```bash
# Toplam boyut
git count-objects -vH

# En buyuk dosyalar (git history dahil)
git rev-list --objects --all | \
  git cat-file --batch-check='%(objecttype) %(objectname) %(objectsize) %(rest)' | \
  grep '^blob' | sort -rnk3 | head -20

# BFG gerekli mi?
# MesTech_Published 63K dosya + node_modules = muhtemelen yuzlerce MB
# EVET, BFG TAVSIYE EDILIR
```

### F.3 Hedef Yapi (Temizlik Sonrasi)

```
MesTech/ (10 klasor — hedef)
├── Docs/
│   ├── eKural/
│   ├── Kesif/
│   └── PlatformSpecs/    (tasindi)
├── MesTech_BackupSystem/
├── MesTech_Dashboard/
├── MesTech_Opencart/
├── MesTech_Stok/         (ANA PROJE)
├── MesTech_Trendyol/
├── Scripts/
├── .github/workflows/
├── .gitignore
└── docker-compose.yml    (ust seviyeye tasinabilir)
```

**Not:** `DevTrednyol` klasoru (typo) da kontrol edilmeli — muhtemelen MesTech_Trendyol ile birlestirilmeli veya silinmeli.

---

## G. DOCKER GENiSLEME PLANI

### G.1 Mevcut Docker Altyapisi

**Dosya:** `MesTech_Stok/MesTechStok/docker-compose.yml`

| Servis | Image | Port | Volume | Healthcheck |
|--------|-------|------|--------|-------------|
| PostgreSQL 17 | pgvector/pgvector:pg17 | 5432 | mestech_pgdata | pg_isready |
| Redis 7 | redis:7-alpine | 6379 | mestech_redis_data | redis-cli ping |
| RabbitMQ 3 | rabbitmq:3-management-alpine | 5672, 15672 | mestech_rabbitmq_data | rabbitmq-diagnostics ping |

**Onemli Notlar:**
- PostgreSQL 17 (pgvector dahil) — emirnamede 16 deniyor ama gercekte **17 + pgvector**
- init-db.sql: `vector`, `uuid-ossp`, `pg_trgm`, `pg_stat_statements` extension'lari
- Environment variable'lar varsayilan degerlerle: `${POSTGRES_PASSWORD:-*** ENV_SIFRE ***}`
- `restart: unless-stopped` — otomatik yeniden baslatma
- Named volume'lar kullaniliyor (veri kaybi onlenir)

### G.2 Genisleme Plani

| Servis | Durum | Port | Amac | Ne Zaman? | Oncelik |
|--------|-------|------|------|-----------|---------|
| PostgreSQL 17+pgvector | HAZIR | 5432 | Merkezi DB + AI vektör arama | FAZ 0 | - |
| Redis 7 | HAZIR | 6379 | Cache + distributed lock | FAZ 0 | - |
| RabbitMQ 3 | HAZIR | 5672/15672 | Event bus + yonetim UI | FAZ 0 | - |
| MySQL 8 | GEREKLI | 3306 | OpenCart DB'leri | FAZ 1 | YUKSEK |
| Seq | ONERILIR | 5341/80 | Merkezi yapilandirilmis log toplama (.NET icin ideal) | FAZ 1 | ORTA |
| Hangfire Dashboard | DEGERLENDIR | 5050 | Background jobs izleme (eger Hangfire kullanilirsa) | FAZ 2 | DUSUK |
| Grafana + Prometheus | ONERILIR | 3000/9090 | Monitoring + alerting | FAZ 3 | DUSUK |

**MySQL Eklenmesi (FAZ 1 Oncelik):**
Mevcut durumda OpenCart'lar localhost MySQL kullaniyor. Tutarlilik icin Docker'a alinabilir:
```yaml
mysql:
  image: mysql:8.0
  container_name: mestech-mysql
  restart: unless-stopped
  environment:
    MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD:-changeme}
  ports:
    - "3306:3306"
  volumes:
    - mestech_mysql_data:/var/lib/mysql
  healthcheck:
    test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
    interval: 10s
    timeout: 5s
    retries: 5
```

---

## KRiTiK BULGULAR

### Severity: KRITIK

1. **GUVENLIK — Plaintext Sifreler:** Tum OpenCart config.php'lerde ve BackupSystem config.json'da DB sifreleri plaintext (`root/1234`). Web interface SECRET_KEY hardcoded.

2. **PATH UYUMSUZLUGU:** BackupSystem config.json ve 2/3 OpenCart config.php macOS path'leriyle yapilandirilmis. Windows ortaminda CALISMAZ.

3. **MesTech_Published:** 63K+ dosya (node_modules dahil) repoda commit edilmis. **304 MB** yer kapliyor. Repo boyutunu gereksiz yere sisiyor, clone surelerini uzatiyor. Icerigi: `MesTech_Published_02_07_16_59` (tarih damgali publish) + `Docs`.

4. **.gitignore'da .env YOK:** Hassas veriler (env dosyalari) yanlislikla commit edilebilir.

### Severity: YUKSEK

5. **CI/CD Eksik:** .NET build, test, publish icin HICBIR otomasyon yok. Sadece eKural sync workflow'u mevcut.

6. **Production Stratejisi YOK:** Deployment, staging, monitoring TANIMLANMAMIS.

7. **BackupSystem Kapsaminin Darligi:** Sadece OpenCart yedekliyor — ana proje (WPF + PostgreSQL), Trendyol (NestJS) yedekleme DISINDA.

8. **OpenCart Core Repoda:** 42K+ OpenCart core dosyasi git'te takip ediliyor — gereksiz, vendor dependency gibi ele alinmali.

### Severity: ORTA

9. **Port Catismasi:** Opencart_Stok_02 ve Opencart_Stok_Test_NEW ayni port'u (8080) kullaniyor.

10. **10 Bos Platform Klasoru:** Repo'yu karistiriyor, temizlenmeli.

11. **DevTrednyol Typo:** Klasor adi hatali ("Trednyol" -> "Trendyol"), MesTech_Trendyol ile iliskisi belirsiz.

12. **Scheduled Backup YOK:** Otomatik yedekleme zamanlayicisi yapilandirilmamis.

---

## ONERiLER

### Acil (FAZ 1 icinde)

1. **Sifre Yonetimi:** Tum plaintext sifreleri `.env` dosyasina tasi. `.env` pattern'ini `.gitignore`'a ekle. Docker-compose.yml zaten env var destekliyor — sadece `.env` dosyasi olusturulmali.

2. **MesTech_Published Temizligi:** Yukaridaki adim adim plani uygula. BFG ile git history'den de temizle.

3. **Bos Klasor Temizligi:** 10 platform klasorunu `Docs/PlatformSpecs/`'e tasi.

4. **Path Duzeltme:** Tum config dosyalarindaki path'leri Windows ortamina uygun hale getir veya ortam degiskenleri ile dinamik yap.

### Kisa Vade (FAZ 2)

5. **CI/CD Pipeline Kur:**
   - `.NET build + test` workflow'u (push/PR tetiklemeli)
   - `dotnet publish` ile artifact olusturma (MesTech_Published yerine)
   - OpenCart icin deployment scripti

6. **BackupSystem'i Genislet:**
   - PostgreSQL yedekleme desteyi ekle (pg_dump)
   - Path'leri platform-agnostic yap (Path konfigurasyonu)
   - Docker'a al (Flask uygulamasi icin ideal)

7. **Seq/ELK Kurulumu:** Merkezi log toplama icin Docker'a Seq ekle.

### Orta Vade (FAZ 3)

8. **OpenCart Docker'a Tasima:** PHP + Apache + MySQL stack'i Docker'da calistir — tutarli ortam, kolay deploy.

9. **Monitoring:** Grafana + Prometheus ile servis izleme ve alerting.

10. **Staging Ortami:** Production oncesi test ortami tanimla ve dokumanla.

---

**RAPOR SONU — TAKIM 4**
