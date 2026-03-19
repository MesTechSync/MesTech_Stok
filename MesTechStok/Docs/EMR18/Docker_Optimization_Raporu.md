# EMR-18 X-02/X-03: Docker + Dockerfile Optimizasyonu Raporu

**Tarih:** 2026-03-19
**DEV:** DEV 4 (DevOps & Security Engineer)
**Kapsam:** Dockerfile, docker-compose.yml, .dockerignore

---

## 1. Dockerfile Review & Optimizasyon

**Dosya:** `src/MesTech.WebApi/Dockerfile`

### Onceki Durum (Review)

| Kontrol Noktasi | Durum | Not |
|-----------------|-------|-----|
| Multi-stage build | OK | 3 stage (build, publish, runtime) |
| Alpine-based | OK | sdk:9.0-alpine + aspnet:9.0-alpine |
| Non-root user | OK | `mestech` user/group |
| HEALTHCHECK | EKSIK | timeout=5s (yetersiz), start-period YOK |
| Layer caching | OK | csproj -> restore -> full copy -> build |
| .dockerignore | EKSIK | Bazi pattern'lar eksik |

### Yapilan Iyilestirmeler

1. **HEALTHCHECK gelistirildi:**
   - `--timeout=5s` -> `--timeout=10s` (DB baglantisi olan endpoint icin yeterli sure)
   - `--start-period=40s` eklendi (.NET warmup + EF migration suresi)
   - `wget --no-verbose --tries=1 --spider` -> `wget -qO-` (response body kontrolu)

2. **Runtime optimizasyonu:**
   - `--runtime linux-musl-x64` restore ve publish'e eklendi (Alpine-specific RID)
   - `--self-contained false` eklendi (framework-dependent = kucuk image)
   - `DOTNET_EnableDiagnostics=0` eklendi (production'da debug overhead azaltma)
   - ENV satirlari birlesti (tek layer)

3. **Guvenlik iyilestirmeleri:**
   - `COPY --chown=mestech:mestech` ile dosya sahipligi acik belirlendi
   - `/app` dizini olusturma ve chown tek RUN'da birlesti (layer azaltma)
   - OCI labels eklendi (image metadata, registry taramasi icin)

4. **Stage isimlendirme:**
   - `build` -> `restore` (1. stage amacini acikca belirtir)
   - `publish` stage artik `restore`'dan turetilir (full source COPY bu stage'de)

5. **Dokumantasyon:**
   - Build context aciklamasi header'a eklendi
   - Image boyutu hedefi (<150MB) belgelendi

### Tahmini Image Boyutu

| Katman | Boyut (tahmini) |
|--------|-----------------|
| aspnet:9.0-alpine base | ~85MB |
| Published WebAPI output | ~40-50MB |
| **Toplam** | **~125-135MB (<150MB hedef)** |

---

## 2. docker-compose.yml Review & Optimizasyon

**Dosya:** `docker-compose.yml`

### Onceki Durum

| Servis | Volume | HealthCheck | Restart | Network | Sorun |
|--------|--------|-------------|---------|---------|-------|
| PostgreSQL | OK | OK | OK | default | - |
| Redis | OK | OK | OK | default | Sifresiz, AOF persist YOK |
| RabbitMQ | OK | OK | OK | default | - |
| MySQL | OK | OK | OK | default | - |
| Seq | OK | OK | OK | default | - |
| MinIO | OK | OK | OK | default | - |
| WebAPI | YOK | YOK | YOK | YOK | Servis tanimli degil |
| Network | - | - | - | external:true | On-create gerekli |

### Yapilan Iyilestirmeler

1. **Redis guvenlik + persist:**
   - `--requirepass` ile sifre zorunlu kilindi (`REDIS_PASSWORD` env)
   - `--appendonly yes --appendfsync everysec` ile AOF persistence aktif
   - `--maxmemory 256mb --maxmemory-policy allkeys-lru` ile bellek limiti
   - Healthcheck'e `-a password` eklendi

2. **WebAPI servisi eklendi:**
   - Build context: mono-repo root (`../..`)
   - Dockerfile: `MesTech_Stok/MesTechStok/src/MesTech.WebApi/Dockerfile`
   - Port: 5100:5100
   - `depends_on` ile postgres, redis, rabbitmq saglik kontrolu
   - Connection string'ler Docker service name ile (container-arasi)
   - Healthcheck: 30s interval, 10s timeout, 40s start-period

3. **Network duzeltmesi:**
   - `external: true` -> `driver: bridge` (otomatik olusturma, `docker network create` gereksiz)

---

## 3. .dockerignore Review & Optimizasyon

**Dosya:** `.dockerignore`

### Eklenen Pattern'lar

| Pattern | Aciklama |
|---------|----------|
| `**/out/` | dotnet publish output |
| `**/.vscode/` | VS Code ayarlari |
| `**/*.sln.docstates` | Solution state dosyalari |
| `**/.gitmodules` | Git submodule config |
| `**/coveragereport/` | Coverage HTML raporu |
| `**/Dockerfile*` | Recursive COPY onleme |
| `**/docker-compose*` | Compose dosyalari |
| `**/.dockerignore` | Kendisi |
| `**/.github/` | CI/CD workflows |
| `**/.gitlab-ci.yml` | GitLab CI |
| `**/frontend/` | Frontend dosyalari (WebAPI image'da gereksiz) |
| `**/wwwroot/` | Static dosyalar |
| `**/*.log` | Log dosyalari |

---

## 4. Kontrol Matrisi (Final)

| Kontrol | Onceki | Sonraki |
|---------|--------|---------|
| Multi-stage build | 3 stage | 3 stage (isimler iyilestirildi) |
| Alpine-based | OK | OK + RID belirtildi |
| Non-root user | OK | OK + chown eklendi |
| HEALTHCHECK | Zayif | Guclu (timeout=10s, start-period=40s) |
| Layer caching | OK | OK (degisiklik yok) |
| .dockerignore | 20 satir | 40+ satir (kapsamli) |
| Redis persist | YOK | AOF aktif |
| Redis auth | YOK | requirepass aktif |
| WebAPI compose | YOK | Tam tanim (depends_on + healthcheck) |
| Network | external | bridge (auto-create) |
| Image boyutu | ~130MB | ~125-135MB (<150MB hedef) |
| OCI labels | YOK | 4 label |
| Diagnostics | Acik | Kapali (production) |

---

## 5. Dokunulmayan Dosyalar

CAKISMA KURALI geregi asagidaki dosyalara dokunulmadi:
- `src/` altindaki C# kaynak kodlari
- `frontend/` altindaki HTML/JS dosyalari
- `tests/` altindaki test dosyalari
- Adapter dosyalari

---

## 6. Sonraki Adimlar

1. `.env.example` dosyasi olustur (REDIS_PASSWORD dahil)
2. CI pipeline'a `docker build --target runtime` adimi ekle (image boyutu kontrolu)
3. Coolify deployment config'ini yeni Dockerfile ile guncelle
4. `docker compose up -d` ile entegrasyon testi yap
