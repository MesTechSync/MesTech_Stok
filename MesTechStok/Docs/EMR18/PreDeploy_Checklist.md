# Pre-Deploy Checklist — 2026-03-19

> EMR-18 D-01 | MesTech Stok WebAPI | Coolify/Docker deploy oncesi kontrol listesi

---

## Kod Kalite

- [x] `dotnet build` 0 error (Release) — Build basarili (background task onaylandi)
- [ ] `dotnet test` 0 fail — Gerçek: _deploy oncesi calistirilacak_
- [ ] P0 guvenlik bulgusu 0 — `Security_JWT_NuGet_Credential_Raporu.md` son kontrol
- [ ] Preview NuGet 0 — **1 adet preview var:** `LiveChartsCore.SkiaSharpView.WPF 2.0.0-rc6.1` (Desktop-only, WebAPI'de yok — KABUL EDİLEBİLİR, ancak stable cikinca guncelle)
- [ ] Git history'de credential 0 — `git log --all -p | grep -i "password\|secret\|apikey"` kontrolu yapilacak

## Dosya Kontrol

- [x] `docker-compose.yml` gecerli YAML — 7 servis tanimli (postgres, redis, rabbitmq, mysql, seq, webapi, minio)
- [ ] `docker-compose.production.yml` gecerli YAML — **MEVCUT DEGiL**, docker-compose.yml env-var tabanli (production/dev ayrimini `ASPNETCORE_ENVIRONMENT` ile yapiyor)
- [x] `Dockerfile` multi-stage build calisiyor — 3-stage Alpine build mevcut (`src/MesTech.WebApi/Dockerfile`)
  - Stage 1: `restore` (SDK 9.0-alpine, layer-cached csproj restore)
  - Stage 2: `publish` (Release, linux-musl-x64)
  - Stage 3: `runtime` (ASP.NET 9.0-alpine, non-root `mestech` user, HEALTHCHECK, port 5100)
- [ ] nginx.conf syntax OK — **nginx config bu submodule'de YOK** (ana repo `nginx/` dizininde veya Coolify/Traefik seviyesinde)
- [ ] `.env.production.template` eksiksiz — **Zorunlu env-var'lar (docker-compose.yml'den):**
  - `POSTGRES_PASSWORD` (zorunlu, hata verir yoksa)
  - `RABBITMQ_USER` (zorunlu)
  - `RABBITMQ_PASSWORD` (zorunlu)
  - `MYSQL_ROOT_PASSWORD` (zorunlu)
  - `SEQ_ADMIN_PASSWORD` (zorunlu)
  - `REDIS_PASSWORD` (varsayilan: `mestech_redis_dev` — production'da degistir!)
  - `MINIO_ROOT_USER` (varsayilan: `mestech`)
  - `MINIO_ROOT_PASSWORD` (varsayilan: `mestech_minio_dev` — production'da degistir!)
  - `ASPNETCORE_ENVIRONMENT` (varsayilan: `Production`)
- [ ] `frontend/panel/` dizini dolu — **Bu submodule'de frontend dizini YOK** (HTML sayfalari ana repo `frontend/` altinda)

## Altyapi Kontrol

- [ ] Coolify VPS erisilebidir (SSH)
- [ ] DNS: `mestech.tr` → VPS IP (A record)
- [ ] DNS: `panel.mestech.tr` → VPS IP
- [ ] DNS: `api.mestech.tr` → VPS IP (WebAPI icin)
- [ ] Coolify GitHub repo bagli (`MesTechSync/MesTech`)
- [ ] Coolify environment variables doldurulmus (yukardaki `.env` listesi)
- [ ] PostgreSQL backup cron tanimli (`Scripts/backup-postgres.sh` mevcut)
- [ ] SSL/TLS Let's Encrypt Traefik'te aktif
- [ ] Docker network `mestech-network` bridge modunda calisacak
- [ ] Container health check'ler tum servisler icin tanimli (tanimli: EVET)

## Port Haritasi (Production)

| Servis | Host Port | Container Port | Notlar |
|--------|-----------|----------------|--------|
| PostgreSQL | 3432 | 5432 | pgvector/pg17 |
| Redis | 3679 | 6379 | Alpine, AOF aktif |
| RabbitMQ AMQP | 3672 | 5672 | Management + Prometheus plugin |
| RabbitMQ UI | 3692 | 15672 | |
| RabbitMQ Metrics | 3693 | 15692 | |
| MySQL | 3306 | 3306 | OpenCart |
| Seq Ingestion | 3341 | 5341 | |
| Seq UI | 3580 | 80 | |
| WebAPI | 5100 | 5100 | .NET 9, /health endpoint |
| MinIO S3 | 3900 | 9000 | |
| MinIO Console | 3901 | 9001 | |

## Rollback Hazirligi

- Onceki calisan commit hash: _(deploy oncesi `git log --oneline -1` ile kaydet)_
- Rollback yontemi: Coolify Rollback butonu VEYA `git revert HEAD && git push`
- DB migration rollback: `dotnet ef database update [previous-migration] --connection "..."`
- DNS TTL: 300s (5dk) — hizli propagasyon icin deploy oncesi dusurulmus olmali

## Son Kontrol Imzasi

| Kontrol | Kisi | Tarih | Onay |
|---------|------|-------|------|
| Kod Kalite | | | [ ] |
| Dosya Kontrol | | | [ ] |
| Altyapi Kontrol | | | [ ] |
| Rollback Test | | | [ ] |
| **DEPLOY ONAY** | **Komutan** | | [ ] |
