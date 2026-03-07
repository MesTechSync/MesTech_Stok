# DOCKER KONTEYNERİZASYON ANALİZİ & GENİŞLEME YOL HARİTASI

**Belge No:** ENT-STOK-DOCKER-001  
**Tarih:** 06 Mart 2026  
**Analist:** Claude Opus 4.6 (Komutan Yardımcısı)  
**Kaynak Dokümanlar:** ENT-STOK-FAZ0-001, ENT-UNIFIED-001, Keşif Raporları  
**Durum:** ANALİZ TAMAMLANDI — Komutan onayı bekleniyor

---

## 1. MEVCUT DURUMUN RÖNTGEN'İ

### 1.1 FAZ 0 Sonrası Mimari Harita

```
MesTechStok.sln (Faz 0 Tamamlanmış)
│
├── MesTech.Domain           → net9.0         (0 NuGet, saf C#)
├── MesTech.Application      → net9.0         (MediatR, FluentValidation, Mapster)
├── MesTech.Infrastructure   → net9.0         (EF Core 9.0.6, Npgsql 9.0.4, Polly, BCrypt)
├── MesTech.Desktop          → net9.0-windows (WPF/XAML, 38 View)
└── MesTech.Tests            → net9.0         (xUnit)
```

### 1.2 Kritik Mimari Gerçek: WPF ve Docker

| Bileşen | Linux Docker? | Windows Docker? | Açıklama |
|---------|:---:|:---:|---|
| MesTech.Desktop (WPF) | ❌ İMKANSIZ | ⚠️ Kısıtlı | WPF = Windows Presentation Foundation. GUI framework'ü Linux'ta çalışmaz. Windows container'da da render için display server gerekir |
| MesTech.Domain | ✅ | ✅ | Saf C#, platform bağımsız |
| MesTech.Application | ✅ | ✅ | Platform bağımsız |
| MesTech.Infrastructure | ✅ | ✅ | EF Core + Npgsql, platform bağımsız |
| PostgreSQL | ✅ | ✅ | Standart container |
| Redis | ✅ | ✅ | Standart container |
| RabbitMQ | ✅ | ✅ | Standart container |

**Sonuç:** WPF Desktop UI, Docker container içinde **çalışamaz**. Ancak Domain, Application, Infrastructure katmanları tamamen platform bağımsızdır. Bu katmanları doğrulamak için **headless test harness** veya **Web API gateway** kullanılabilir.

---

## 2. DOCKER STRATEJİSİ: ÜÇ KATMANLI YAKLAŞIM

### 2.1 Genel Bakış

```
┌──────────────────────────────────────────────────────────────────┐
│                    DOCKER COMPOSE ORTAMI                         │
│                                                                  │
│  KATMAN A — ALTYAPI SERVİSLERİ (Şimdi)                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                      │
│  │PostgreSQL│  │  Redis   │  │ RabbitMQ │                       │
│  │  17 +    │  │  7-alp   │  │  3-mgmt  │                       │
│  │ pgvector │  │          │  │          │                       │
│  │ :5432    │  │ :6379    │  │:5672/15672│                      │
│  └──────────┘  └──────────┘  └──────────┘                      │
│       │                                                          │
│  KATMAN B — DOĞRULAMA SERVİSİ (Şimdi)                          │
│  ┌─────────────────────────────────┐                            │
│  │   MesTech.HealthCheck           │                            │
│  │   (Console App / net9.0)        │                            │
│  │   • DB Migration doğrulama      │                            │
│  │   • CRUD operasyon testi        │                            │
│  │   • Event dispatch testi        │                            │
│  │   • Redis bağlantı testi        │                            │
│  │   • RabbitMQ bağlantı testi     │                            │
│  └─────────────────────────────────┘                            │
│       │                                                          │
│  KATMAN C — WEB API GATEWAY (Gelecek — Faz 1+)                 │
│  ┌─────────────────────────────────┐                            │
│  │   MesTech.WebApi                │                            │
│  │   (ASP.NET Core / net9.0)       │                            │
│  │   • REST API endpoints          │                            │
│  │   • Swagger/OpenAPI             │                            │
│  │   • Trendyol webhook receiver   │                            │
│  │   • Hangfire dashboard          │                            │
│  └─────────────────────────────────┘                            │
│                                                                  │
│  ═══════════ DOCKER SINIRI ════════════                          │
│                                                                  │
│  KATMAN D — MASAÜSTÜ (Docker DIŞI)                              │
│  ┌─────────────────────────────────┐                            │
│  │   MesTech.Desktop (WPF)         │                            │
│  │   Windows host'ta çalışır       │                            │
│  │   Docker servislere bağlanır    │                            │
│  └─────────────────────────────────┘                            │
└──────────────────────────────────────────────────────────────────┘
```

### 2.2 WPF Desktop → Docker Bağlantısı

Desktop uygulaması Windows host'ta çalışmaya devam eder, ancak tüm altyapı servisleri Docker'dadır:

```
Desktop (Windows Host)
    │
    ├── PostgreSQL  → localhost:5432 (Docker exposed)
    ├── Redis       → localhost:6379 (Docker exposed)
    └── RabbitMQ    → localhost:5672 (Docker exposed)
```

`appsettings.json` connection string'leri zaten `localhost` kullanıyorsa, Desktop doğrudan Docker servislerine bağlanır. Ek değişiklik gerekmez.

---

## 3. DOCKER COMPOSE — TAM TANIMLAMA

### 3.1 Dosya Yapısı

```
MesTech_Stok/
├── docker/
│   ├── docker-compose.yml              # Ana compose (altyapı)
│   ├── docker-compose.healthcheck.yml  # Doğrulama servisi
│   ├── docker-compose.override.yml     # Geliştirme ortamı override
│   ├── .env                            # Ortam değişkenleri (git'e GİRMEZ)
│   ├── .env.example                    # Şablon (.env olmadan nasıl oluşturulacağı)
│   ├── postgres/
│   │   └── init.sql                    # İlk çalıştırmada çalışacak SQL
│   ├── rabbitmq/
│   │   └── rabbitmq.conf              # RabbitMQ konfigürasyonu
│   └── healthcheck/
│       └── Dockerfile                  # HealthCheck console app
└── MesTechStok/
    └── src/
        └── MesTech.HealthCheck/        # YENİ — Docker doğrulama projesi
            ├── MesTech.HealthCheck.csproj
            └── Program.cs
```

### 3.2 docker-compose.yml (Ana)

```yaml
# MesTech Entegratör Stok — Docker Altyapı Servisleri
# Versiyon: 1.0.0
# Kullanım: docker compose up -d

services:
  # ═══════════════════════════════════════════
  # KATMAN A — ALTYAPI SERVİSLERİ
  # ═══════════════════════════════════════════

  postgres:
    image: pgvector/pgvector:pg17
    container_name: mestech-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-mestech_stok}
      POSTGRES_USER: ${POSTGRES_USER:-mestech_user}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD zorunludur}
      PGDATA: /var/lib/postgresql/data/pgdata
      TZ: Europe/Istanbul
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - mestech_pgdata:/var/lib/postgresql/data
      - ./postgres/init.sql:/docker-entrypoint-initdb.d/01-init.sql:ro
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-mestech_user} -d ${POSTGRES_DB:-mestech_stok}"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    networks:
      - mestech-network

  redis:
    image: redis:7-alpine
    container_name: mestech-redis
    restart: unless-stopped
    command: >
      redis-server
      --requirepass ${REDIS_PASSWORD:-mestech_redis_2026}
      --maxmemory 256mb
      --maxmemory-policy allkeys-lru
      --appendonly yes
    ports:
      - "${REDIS_PORT:-6379}:6379"
    volumes:
      - mestech_redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "${REDIS_PASSWORD:-mestech_redis_2026}", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - mestech-network

  rabbitmq:
    image: rabbitmq:3-management-alpine
    container_name: mestech-rabbitmq
    restart: unless-stopped
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:-mestech_mq}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:?RABBITMQ_PASSWORD zorunludur}
      RABBITMQ_DEFAULT_VHOST: ${RABBITMQ_VHOST:-mestech}
    ports:
      - "${RABBITMQ_PORT:-5672}:5672"       # AMQP protokolü
      - "${RABBITMQ_MGMT_PORT:-15672}:15672" # Yönetim paneli
    volumes:
      - mestech_rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_running"]
      interval: 15s
      timeout: 10s
      retries: 5
      start_period: 30s
    networks:
      - mestech-network

# ═══════════════════════════════════════════
# VOLUMES — Kalıcı veri depolama
# DEMİR KURAL: Docker volume silme YASAK
# ═══════════════════════════════════════════
volumes:
  mestech_pgdata:
    name: mestech_pgdata
  mestech_redis_data:
    name: mestech_redis_data
  mestech_rabbitmq_data:
    name: mestech_rabbitmq_data

# ═══════════════════════════════════════════
# NETWORK — İç ağ
# ═══════════════════════════════════════════
networks:
  mestech-network:
    name: mestech-network
    driver: bridge
```

### 3.3 .env.example

```bash
# MesTech Entegratör Stok — Ortam Değişkenleri Şablonu
# Bu dosyayı .env olarak kopyalayın ve değerleri güncelleyin
# cp .env.example .env
# ÖNEMLİ: .env dosyası git'e ASLA commit edilmez!

# PostgreSQL
POSTGRES_DB=mestech_stok
POSTGRES_USER=mestech_user
POSTGRES_PASSWORD=BURAYA_GUCLU_SIFRE_YAZIN
POSTGRES_PORT=5432

# Redis
REDIS_PASSWORD=BURAYA_GUCLU_SIFRE_YAZIN
REDIS_PORT=6379

# RabbitMQ
RABBITMQ_USER=mestech_mq
RABBITMQ_PASSWORD=BURAYA_GUCLU_SIFRE_YAZIN
RABBITMQ_VHOST=mestech
RABBITMQ_PORT=5672
RABBITMQ_MGMT_PORT=15672
```

### 3.4 postgres/init.sql

```sql
-- MesTech Stok — PostgreSQL ilk çalıştırma scripti
-- Bu script sadece container ilk oluşturulduğunda çalışır
-- Volume silinmediği sürece tekrar çalışmaz

-- pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;

-- uuid-ossp (Guid PK desteği)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- pg_trgm (fuzzy text search — ürün arama için)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- citext (case-insensitive text — SKU, barkod için)
CREATE EXTENSION IF NOT EXISTS citext;

-- Audit log tablosu (uygulama dışı, DB seviyesi izleme)
CREATE TABLE IF NOT EXISTS _audit_log (
    id BIGSERIAL PRIMARY KEY,
    table_name TEXT NOT NULL,
    operation TEXT NOT NULL,  -- INSERT, UPDATE, DELETE
    old_data JSONB,
    new_data JSONB,
    changed_by TEXT DEFAULT 'system',
    changed_at TIMESTAMPTZ DEFAULT NOW()
);

-- Log rotate: 90 günden eski audit kayıtlarını temizle
-- (Cron job veya pg_cron ile çalıştırılabilir)

-- Bilgi mesajı
DO $$
BEGIN
    RAISE NOTICE 'MesTech Stok DB başlatıldı — Extensions: vector, uuid-ossp, pg_trgm, citext';
END $$;
```

### 3.5 docker-compose.healthcheck.yml

```yaml
# Doğrulama servisi — tüm altyapı bağlantılarını test eder
# Kullanım: docker compose -f docker-compose.yml -f docker-compose.healthcheck.yml up healthcheck

services:
  healthcheck:
    build:
      context: ../MesTechStok/src/MesTech.HealthCheck/
      dockerfile: ../../../docker/healthcheck/Dockerfile
    container_name: mestech-healthcheck
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      rabbitmq:
        condition: service_healthy
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=${POSTGRES_DB:-mestech_stok};Username=${POSTGRES_USER:-mestech_user};Password=${POSTGRES_PASSWORD}"
      Redis__Configuration: "redis:6379,password=${REDIS_PASSWORD:-mestech_redis_2026}"
      RabbitMQ__Host: "rabbitmq"
      RabbitMQ__Port: "5672"
      RabbitMQ__Username: "${RABBITMQ_USER:-mestech_mq}"
      RabbitMQ__Password: "${RABBITMQ_PASSWORD}"
      RabbitMQ__VirtualHost: "${RABBITMQ_VHOST:-mestech}"
    networks:
      - mestech-network
```

---

## 4. DOĞRULAMA MATRİSİ — NE KONTROL EDİLMELİ?

### 4.1 Servis Sağlık Kontrolleri

| # | Kontrol | Komut | Beklenen Sonuç |
|---|---------|-------|----------------|
| H1 | PostgreSQL ayakta mı? | `docker exec mestech-postgres pg_isready` | `/var/run/postgresql:5432 - accepting connections` |
| H2 | PostgreSQL extensions yüklü mü? | `docker exec mestech-postgres psql -U mestech_user -d mestech_stok -c "\dx"` | vector, uuid-ossp, pg_trgm, citext |
| H3 | Redis ayakta mı? | `docker exec mestech-redis redis-cli -a $PASS ping` | `PONG` |
| H4 | Redis memory policy doğru mu? | `docker exec mestech-redis redis-cli -a $PASS CONFIG GET maxmemory-policy` | `allkeys-lru` |
| H5 | RabbitMQ ayakta mı? | `docker exec mestech-rabbitmq rabbitmq-diagnostics check_running` | `Health check passed` |
| H6 | RabbitMQ vhost oluştu mu? | `docker exec mestech-rabbitmq rabbitmqctl list_vhosts` | `mestech` |
| H7 | Yönetim paneli erişilebilir mi? | `curl http://localhost:15672` | HTTP 200 (login sayfası) |

### 4.2 Veri Kalıcılık Kontrolleri

| # | Kontrol | Senaryo | Beklenen |
|---|---------|---------|----------|
| V1 | PostgreSQL veri kalıcılığı | Container restart → veri durumu | Veri korunur |
| V2 | Redis veri kalıcılığı | Container restart → cache durumu | AOF ile korunur |
| V3 | RabbitMQ kuyruk kalıcılığı | Container restart → kuyruk durumu | Durable queue'lar korunur |
| V4 | Volume integrity | `docker volume ls` | 3 volume mevcut |

### 4.3 Uygulama Katmanı Kontrolleri

| # | Kontrol | Yöntem | Beklenen |
|---|---------|--------|----------|
| A1 | EF Migration çalışıyor mu? | HealthCheck app → `DbContext.Database.MigrateAsync()` | 32 tablo oluştu |
| A2 | Product CRUD çalışıyor mu? | HealthCheck app → Create/Read/Update/Delete | 4/4 başarılı |
| A3 | StockMovement kaydı | HealthCheck app → `AddStockCommand` | Hareket kaydedildi |
| A4 | Domain Event dispatch | HealthCheck app → StockChangedEvent | Event handler tetiklendi |
| A5 | UnitOfWork transaction | HealthCheck app → Multi-op commit/rollback | Atomik çalışıyor |
| A6 | Guid PK üretimi | HealthCheck app → `Product.Id` | Valid Guid (v4) |
| A7 | Audit interceptor | HealthCheck app → SaveChanges → CreatedBy/UpdatedBy | "developer" (DevelopmentUserService) |
| A8 | Soft delete | HealthCheck app → Delete → IsDeleted=true | Fiziksel silme yok |
| A9 | Redis bağlantısı | HealthCheck app → Set/Get | Değer okundu |
| A10 | RabbitMQ bağlantısı | HealthCheck app → Publish/Consume | Mesaj alındı |

---

## 5. MEVCUT HİZMET ENVANTERİ VE GENİŞLEME PLANI

### 5.1 Mevcut Hizmetler — Faz 0 Sonrası

```
MEVCUT (Çalışıyor)                   DURUMU
──────────────────                    ──────
PostgreSQL (pgvector:pg17)            ✅ Docker'da çalışacak
EF Core 9.0.6 + Npgsql               ✅ Infrastructure'da mevcut
Domain Event Dispatcher (MediatR)     ✅ In-process çalışıyor
UnitOfWork + Repository Pattern       ✅ Infrastructure'da mevcut
DevelopmentUserService                ✅ Geçici auth
Polly Circuit Breaker                 ✅ Core'dan taşındı
Token Rotation Service                ✅ Core'dan taşındı
Serilog (File + Console)              ✅ Logging çalışıyor
BCrypt Auth                           ✅ Infrastructure'da
```

### 5.2 Eksik Hizmetler — Genişletme Gerekli

```
EKSİK (Eklenmeli)                    ÖNCELİK     GEREKSİNİM
──────────────────                    ────────     ──────────
Redis Cache                           🔴 YÜKSEK   Ürün/kategori cache, distributed lock
RabbitMQ Event Bus                     🔴 YÜKSEK   Repolar arası event yayılımı
Hangfire Background Jobs               🟡 ORTA     Scheduled sync, retry queue
Web API Gateway                        🟡 ORTA     Webhook receiver, REST endpoint
Seq / ELK Log Aggregation             🟢 DÜŞÜK    Merkezi log görüntüleme
Prometheus + Grafana                   🟢 DÜŞÜK    Metrik izleme
```

### 5.3 Genişleme Yol Haritası — Servislerin Ekleniş Sırası

```
ŞİMDİ (Faz 0.8 — Docker Altyapı)
├── PostgreSQL 17 + pgvector          ← Container
├── Redis 7                           ← Container  
├── RabbitMQ 3 + Management           ← Container
└── MesTech.HealthCheck               ← Doğrulama console app

FAZ 1 (Entegrasyon Altyapısı)
├── Redis entegrasyonu                ← ICacheService implementasyonu
│   • ProductCache (5 dk TTL)
│   • CategoryCache (30 dk TTL)
│   • Distributed Lock (stok güncelleme)
│
├── RabbitMQ entegrasyonu             ← MassTransit + Event Bus
│   • StockChangedEvent → Exchange: mestech.stock.changed
│   • PriceChangedEvent → Exchange: mestech.price.changed
│   • ProductCreatedEvent → Exchange: mestech.product.created
│   • Trendyol repo consume eder
│
└── Hangfire                          ← IBackgroundJobService implementasyonu
    • OpenCart delta sync (her 5 dk)
    • Stok seviye kontrolü (her saat)
    • Senkronizasyon retry (her 15 dk)
    • Log temizleme (günlük)

FAZ 2 (Web API Gateway)
├── MesTech.WebApi projesi            ← ASP.NET Core Minimal API
│   • POST /api/webhooks/trendyol     ← Webhook receiver
│   • GET  /api/products              ← Ürün listesi
│   • GET  /api/stock/{sku}           ← Stok sorgulama
│   • POST /api/stock/sync            ← Manuel sync tetikleme
│   • GET  /api/health                ← Sağlık kontrolü
│
├── Swagger/OpenAPI                    ← Otomatik API dokümantasyonu
└── Hangfire Dashboard                 ← /hangfire endpoint

FAZ 3+ (İzleme & Gözlemleme)
├── Seq veya Loki                     ← Merkezi log
├── Prometheus                        ← Metrik toplama
├── Grafana                           ← Dashboard
└── AlertManager                      ← Alarm sistemi
```

---

## 6. DESKTOP ↔ DOCKER ENTEGRASYONU

### 6.1 Connection String Güncellemesi

Desktop uygulaması şu anda `.NET User Secrets` ile çalışıyor. Docker servislere bağlanmak için:

```json
// appsettings.Development.json (Desktop)
{
  "Database": {
    "Provider": "PostgreSQL"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=mestech_stok;Username=mestech_user;Password=*** USER SECRETS ***"
  },
  "Redis": {
    "Configuration": "localhost:6379,password=*** USER SECRETS ***",
    "InstanceName": "MesTech_",
    "DefaultCacheDurationMinutes": 30
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "*** USER SECRETS ***",
    "Password": "*** USER SECRETS ***",
    "VirtualHost": "mestech"
  }
}
```

### 6.2 Geliştirme Ortamı Akışı

```
Geliştirici İş Akışı:
═════════════════════

1. docker compose up -d                    ← Altyapıyı başlat
2. docker compose ps                       ← Servislerin durumunu kontrol et
3. Visual Studio'da MesTech.Desktop başlat ← WPF uygulaması açılır
4. Desktop → localhost:5432 (PostgreSQL)   ← Veri okuma/yazma
5. Desktop → localhost:6379 (Redis)        ← Cache operasyonları
6. Desktop → localhost:5672 (RabbitMQ)     ← Event publish
7. Geliştirme bitti → docker compose stop  ← Servisleri durdur (volume korunur)
```

---

## 7. PORTLAR VE AĞ MATRİSİ

### 7.1 Port Haritası

| Servis | Container Port | Host Port | Protokol | Erişim |
|--------|:---:|:---:|---|---|
| PostgreSQL | 5432 | 5432 | TCP | Desktop, HealthCheck, WebApi |
| Redis | 6379 | 6379 | TCP | Desktop, HealthCheck, WebApi |
| RabbitMQ AMQP | 5672 | 5672 | TCP/AMQP | Desktop, Trendyol, HealthCheck |
| RabbitMQ Mgmt | 15672 | 15672 | HTTP | Tarayıcı (yönetim paneli) |
| WebApi (Gelecek) | 8080 | 8080 | HTTP | Webhook, Swagger |
| Hangfire (Gelecek) | 8080 | 8080 | HTTP | /hangfire endpoint |
| Seq (Gelecek) | 5341/80 | 5341/8081 | HTTP | Log görüntüleme |

### 7.2 Demir Kural: Port Değiştirme YASAK

Emirname gereği, yukarıdaki portlar belirlendiğinde değiştirilmez. Çakışma olursa `.env` ile override edilir ama varsayılan portlar standarttır.

### 7.3 Docker Network Topolojisi

```
mestech-network (bridge)
│
├── mestech-postgres    (172.20.0.2)
├── mestech-redis       (172.20.0.3)
├── mestech-rabbitmq    (172.20.0.4)
├── mestech-healthcheck (172.20.0.5)  ← Geçici, kontrol sonrası kapanır
└── mestech-webapi      (172.20.0.6)  ← Gelecek
│
│  ← Docker bridge →
│
└── Host (Windows) ← Desktop WPF uygulaması buradan bağlanır
```

---

## 8. GÜVENLİK KONTROL LİSTESİ

| # | Kontrol | Durum | Açıklama |
|---|---------|:---:|---|
| S1 | `.env` dosyası `.gitignore`'da | ⬜ Yapılacak | Şifreler commit edilmez |
| S2 | `.env.example` şifresiz | ⬜ Yapılacak | Sadece placeholder değerler |
| S3 | PostgreSQL şifre zorunlu | ✅ | `${POSTGRES_PASSWORD:?}` — boşsa başlamaz |
| S4 | RabbitMQ şifre zorunlu | ✅ | `${RABBITMQ_PASSWORD:?}` — boşsa başlamaz |
| S5 | Redis şifre zorunlu | ✅ | `--requirepass` ile korunuyor |
| S6 | Volume'lar named | ✅ | Anonymous volume yok, silinmez |
| S7 | Network izolasyonu | ✅ | `mestech-network` bridge, sadece servislere açık |
| S8 | Prod'da port kapatma | ⬜ Gelecek | Prod'da sadece iç ağdan erişim |

---

## 9. DOĞRULAMA KOMUT SIRASI

Desktop ile Docker servislerinin birlikte çalıştığını doğrulamak için sıralı test prosedürü:

```bash
# ═══════════════════════════════════════════
# ADIM 1: Altyapıyı Başlat
# ═══════════════════════════════════════════
cd docker/
cp .env.example .env
# .env dosyasındaki şifreleri güncelle!
docker compose up -d

# ═══════════════════════════════════════════
# ADIM 2: Servislerin Sağlığını Kontrol Et
# ═══════════════════════════════════════════
docker compose ps
# Beklenen: 3 servis "healthy" durumunda

# PostgreSQL
docker exec mestech-postgres pg_isready -U mestech_user -d mestech_stok
docker exec mestech-postgres psql -U mestech_user -d mestech_stok -c "SELECT * FROM pg_extension;"

# Redis
docker exec mestech-redis redis-cli -a SIFRE ping

# RabbitMQ
docker exec mestech-rabbitmq rabbitmq-diagnostics check_running
curl -u mestech_mq:SIFRE http://localhost:15672/api/overview

# ═══════════════════════════════════════════
# ADIM 3: EF Migration Uygula (İlk seferde)
# ═══════════════════════════════════════════
cd ../MesTechStok/
dotnet ef database update --project src/MesTech.Infrastructure --startup-project src/MesTech.Desktop

# ═══════════════════════════════════════════
# ADIM 4: Tabloları Doğrula
# ═══════════════════════════════════════════
docker exec mestech-postgres psql -U mestech_user -d mestech_stok -c "\dt"
# Beklenen: 32+ tablo listelenir

# ═══════════════════════════════════════════
# ADIM 5: Desktop'u Başlat
# ═══════════════════════════════════════════
# Visual Studio'da MesTech.Desktop başlat VEYA:
dotnet run --project src/MesTech.Desktop
# Dashboard açılmalı, CRUD işlemleri çalışmalı

# ═══════════════════════════════════════════
# ADIM 6: CRUD Testi
# ═══════════════════════════════════════════
# Desktop UI'dan:
# 1. Yeni ürün ekle → DB'ye yazılmalı
# 2. Stok ekle → StockMovement kaydı oluşmalı
# 3. Ürün güncelle → UpdatedAt/UpdatedBy dolmalı
# 4. Ürün sil → IsDeleted=true olmalı (soft delete)

# PostgreSQL'den doğrula:
docker exec mestech-postgres psql -U mestech_user -d mestech_stok \
  -c "SELECT \"Id\", \"Name\", \"Stock\", \"IsDeleted\", \"CreatedAt\" FROM \"Products\" LIMIT 5;"
```

---

## 10. KOMUTAN NOTU VE KARAR NOKTALARI

### 10.1 Karar Bekleyen Konular

| # | Konu | Seçenekler | Tavsiye |
|---|------|-----------|---------|
| K1 | HealthCheck projesi oluşturulsun mu? | (a) Console app (b) xUnit integration test (c) İkisi birden | **(c) İkisi birden** — Console app Docker'da çalışır, xUnit CI/CD'de |
| K2 | Web API ne zaman başlasın? | (a) Faz 0.8'de iskelet (b) Faz 1'de tam (c) Faz 2'de | **(a) İskelet** — `/health` endpoint yeterli |
| K3 | Seq/Loki log aggregation eklensin mi? | (a) Şimdi (b) Faz 2 (c) İhtiyaç olunca | **(b) Faz 2** — Öncelik entegrasyon |
| K4 | Prod ortamı için ayrı compose dosyası? | (a) Şimdi (b) Deploy zamanı | **(b) Deploy zamanı** — şimdi dev yeterli |

### 10.2 Özet Tablo

```
DOCKER ALTYAPI SERVİSLERİ
═════════════════════════
PostgreSQL 17 + pgvector     → ŞİMDİ eklenecek (docker-compose.yml)
Redis 7                      → ŞİMDİ eklenecek (docker-compose.yml)
RabbitMQ 3 + Management      → ŞİMDİ eklenecek (docker-compose.yml)

UYGULAMA KATMANI
════════════════
MesTech.HealthCheck          → ŞİMDİ oluşturulacak (doğrulama)
Redis ICacheService impl.    → FAZ 1'de eklenecek
RabbitMQ MassTransit impl.   → FAZ 1'de eklenecek
Hangfire                     → FAZ 1'de eklenecek
MesTech.WebApi               → FAZ 2'de eklenecek (veya Faz 0.8'de iskelet)

DESKTOP
═══════
WPF Desktop                  → Docker DIŞI çalışmaya devam eder
Connection strings           → Docker servislerine yönlendirilir
```

### 10.3 Demir Kurallar — Docker Özel

1. **Docker volume silme YASAK** — `mestech_pgdata`, `mestech_redis_data`, `mestech_rabbitmq_data` korunur
2. **Port değiştirme YASAK** — Belirlenen portlar standarttır, çakışmada `.env` override
3. **`.env` dosyası git'e GİRMEZ** — Sadece `.env.example` commit edilir
4. **Sıfır mock data** — Docker init script'leri sadece extensions ve yapısal tablolar oluşturur, test verisi KOYMAZ
5. **Her compose up idempotent** — Tekrar çalıştırıldığında mevcut veri bozulmaz

---

**ANALİZ SONU**  
**Komutan Yardımcısı — "Altyapı sağlamsa, entegrasyon sağlamdır."**
