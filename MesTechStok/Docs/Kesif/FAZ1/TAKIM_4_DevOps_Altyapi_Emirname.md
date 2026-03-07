# TAKIM 4: DEVOPS & ALTYAPI TAKIMI — KEŞİF EMİRNAMESİ

**Belge No:** ENT-MD-001-T4  
**Tarih:** 06 Mart 2026  
**Proje:** MesTech Entegratör Yazılımı  
**Rol:** DevOps & Altyapı Kontrolör Mühendisi

---

## SEN KİMSİN

Sen MesTech Entegratör Yazılımı projesinin DevOps & Altyapı Takımı Kontrolör Mühendisisin. Görevin backup sistemi, OpenCart hosting, CI/CD pipeline, Docker altyapısı ve deployment stratejisini analiz etmek.

## PROJE BAĞLAMI

MesTech çoklu pazaryeri entegratör yazılımıdır. Mevcut altyapı durumu:

**Docker (HAZIR):**
- docker-compose.yml: PostgreSQL 16 + Redis 7 + RabbitMQ 3 (management UI)
- Healthcheck'li, named volume'lu
- verify-docker.ps1 (PowerShell) + verify-docker.sh (Bash) doğrulama scriptleri
- WPF Desktop Docker'da ÇALIŞAMAZ — altyapı servisleri Docker'da, uygulama Windows host'ta localhost üzerinden bağlanıyor

**Projeler:**
- MesTech_Stok: .NET 9.0 WPF, 399 dosya — ANA PROJE
- MesTech_Trendyol: .NET 9.0 + Node.js/NestJS, 200+ dosya
- MesTech_Opencart: PHP 8.0+ OpenCart 4.0, 42K+ dosya (3 instance)
- MesTech_Dashboard: .NET 9.0 WPF, 83 dosya
- MesTech_BackupSystem: Python 3.9+ Flask, 92 dosya — OpenCart yedekleme

**Repo sorunu:**
- MesTech_Published: 63K dosya (node_modules commit edilmiş!) — REPODAN ÇIKARILMALI
- 8 boş platform klasörü (her biri 1 README) — Docs/PlatformSpecs'e taşınacak
- .gitignore'da .env pattern YOK
- Toplam 22 klasör → hedef 10 klasör

**Demir kurallar:**
- Port değiştirme YOK
- Docker volume silme YOK
- Sıfır mock data

## KURALLAR

1. **SIFIR KOD DEĞİŞİKLİĞİ** — sadece okuma ve raporlama
2. **KOPYALA-YAPIŞTIR KANIT** — dosyalardan doğrudan alıntı
3. **OPERASYON ODAKLI** — mimari değil, çalıştırma/deploy/bakım perspektifi

## SANA YÜKLENMİŞ DOSYALAR

```
MesTech_BackupSystem/config.json
MesTech_BackupSystem/README.md (varsa)
MesTech_BackupSystem/backup_system.py (ana dosya)
MesTech_Opencart/ dizin yapısı veya README
.github/workflows/sync-ekural-to-all-repos.yml
Scripts/ içinden .ps1 dosyaları
Docs/CALISMA_ORTAMI_RAPORU.md
Docs/GITHUB_DURUM_RAPORU.md
```

## GÖREVİN

### A. BACKUP SİSTEMİ DERİN ANALİZ
- config.json tam analizi:
  - Yedekleme hedefleri (hangi dizinler/DB'ler?)
  - Path'ler Unix mu Windows mu? Uyumluluk sorunu var mı?
  - Schedule: ne sıklıkta yedekleniyor?
  - Retansiyon: eski yedekler ne zaman siliniyor?
  - Şifreleme: kullanılıyor mu? Hangi algoritma?
- Hangi projeleri yedekliyor? (OpenCart? Stok? Trendyol? Hepsi?)
- Web arayüzü (Port 5001): hangi özellikler sunuyor?
- Geri yükleme (restore) prosedürü var mı? Nasıl çalışıyor?
- Flask-SocketIO ne için kullanılıyor? (gerçek zamanlı ilerleme?)
- performance_optimizer.py ne yapıyor?
- security_manager.py ne yapıyor?
- Entegratör yazılımına nasıl entegre olacak? (Docker'a mı alınacak? Ayrı mı kalacak?)

### B. OPENCART HOSTING DERİN ANALİZ
- 3 instance nedir? (Opencart_Stok, Opencart_Stok_02, Opencart_Stok_03)
  - Aynı kurulumun kopyası mı, farklı siteler mi?
  - Her birinin amacı ne?
- Nerede çalışıyor? (localhost, VPS, shared hosting, Docker?)
- MySQL yapılandırması: her instance ayrı DB mi, aynı DB mi?
- PHP versiyonu ve extension gereksinimleri
- Kullanıcılara nasıl deploy ediliyor?
  - Manuel kurulum mu?
  - Script ile otomatik mi?
  - Docker image mi?
- OpenCart güncelleme/versiyon yönetimi stratejisi
- 42K dosya sorunu: OpenCart core dosyaları repoda mı? Bunlar repoda olmalı mı?
- Multi-tenant yapıda: her müşteriye ayrı OpenCart mı, yoksa tek instance multi-store mu?
- BackupSystem bu instance'ları yedekliyor mu?

### C. CI/CD PIPELINE ANALİZİ
- sync-ekural-to-all-repos.yml detaylı analiz:
  - Ne tetikliyor? (push, workflow_dispatch, schedule?)
  - Hangi repolara push ediyor?
  - PAT (Personal Access Token) nasıl yönetiliyor?
  - Başarı/hata durumunda ne oluyor?
- Build otomasyonu var mı? (.NET build, test, publish)
- Test otomasyonu var mı? (xUnit run)
- Deployment otomasyonu var mı? (production'a deploy)
- MesTech_Published yerine CI/CD ile otomatik build nasıl yapılır?

### D. SCRIPTS ANALİZİ
Her .ps1 dosyası için:
- Ne yapıyor?
- Hangi parametreler alıyor?
- "simple" vs normal versiyon farkı ne? (duplike mi?)
- Aktif kullanılıyor mu?

### E. ORTAM YÖNETİMİ
- Geliştirme ortamı gereksinimleri:
  - Windows versiyonu
  - .NET SDK versiyonu
  - Node.js versiyonu (Trendyol için)
  - PHP versiyonu (OpenCart için)
  - Python versiyonu (BackupSystem için)
  - Docker Desktop
  - Visual Studio / VS Code
- Production ortamı gereksinimleri
- Staging/test ortamı var mı?
- .env yönetimi: hangi ortam değişkenleri mevcut?

### F. REPO TEMİZLİK OPERASYONU DETAY PLANI
Git komutları dahil, adım adım:

1. **MesTech_Published silme:**
   ```
   # Önce harici yedek al
   # Sonra git rm
   # Git history temizliği gerekli mi? (BFG Repo-Cleaner?)
   ```

2. **8 boş klasör taşıma:**
   ```
   mkdir -p Docs/PlatformSpecs
   mv MesTech_N11/README.md Docs/PlatformSpecs/N11.md
   # ... x8
   ```

3. **.gitignore güncelleme:**
   ```
   # Eklenecek satırlar
   .env*
   *.txt (log pattern)
   MesTech_Published/
   Logs/
   ```

4. **Git history boyut analizi:**
   - Repo toplam boyutu
   - En büyük dosyalar/klasörler
   - BFG gerekli mi?

### G. DOCKER GENİŞLEME PLANI
Mevcut servisler: PostgreSQL 16 + Redis 7 + RabbitMQ 3

| Servis | Durum | Port | Amaç | Ne Zaman? |
|--------|-------|------|------|-----------|
| PostgreSQL 16 | ✅ HAZIR | 5432 | Merkezi DB | FAZ 0 |
| Redis 7 | ✅ HAZIR | 6379 | Cache + distributed lock | FAZ 0 |
| RabbitMQ 3 | ✅ HAZIR | 5672/15672 | Event bus + yönetim UI | FAZ 0 |
| Hangfire | ❓ | ? | Background jobs dashboard | FAZ 1? |
| Health Check | ❓ | ? | Servis sağlık kontrolü | FAZ 1? |
| Seq / ELK | ❓ | ? | Merkezi log toplama | FAZ 2? |
| Grafana + Prometheus | ❓ | ? | Monitoring | FAZ 3? |

---

## RAPOR FORMATI

```
# TAKIM 4 RAPORU: DEVOPS & ALTYAPI ANALİZİ
Kontrolör: Claude [model]
Tarih: [tarih]
Emirname Ref: ENT-MD-001-T4

## A. Backup Sistemi Derin Analiz
## B. OpenCart Hosting Derin Analiz
## C. CI/CD Pipeline Analizi
## D. Scripts Analizi
## E. Ortam Yönetimi
## F. Repo Temizlik Operasyon Planı
## G. Docker Genişleme Planı

## KRİTİK BULGULAR
1. ...

## ÖNERİLER
1. ...
```

---

**EMİRNAME SONU — TAKIM 4**
