# Rollback Plani

> EMR-18 D-04 | Canli deploy basarisiz olursa geri alma protokolu

---

## Tetik Kosullari

Asagidakilerden **herhangi biri** gerceklesirse rollback baslatilir:

| # | Kosul | Esik Degeri |
|---|-------|-------------|
| T1 | `/health` endpoint DOWN | 5+ dakika kesintisiz |
| T2 | Container restart dongusu | 3+ restart (10dk icinde) |
| T3 | Kritik hata orani | >5% (toplam istek icinde) |
| T4 | Veritabani erisim hatasi | Connection pool tamamen dolu veya timeout |
| T5 | Veri butunlugu riski | Duplicate kayit, kayip veri, tutarsiz stok |
| T6 | Guvenlik ihlali | Yetkisiz erisim, credential sizintisi |

---

## Rollback Adimlari

### Yontem 1: Coolify Rollback (TERCIH EDILEN)

```
1. Coolify Dashboard'a git → Applications → mestech-webapi
2. "Deployments" sekmesine tik
3. Onceki basarili deployment'i bul
4. "Rollback" butonuna tikla
5. Deployment tamamlanana kadar bekle (~2-3dk)
6. /health endpoint'ini kontrol et
7. Smoke test Bolum 1-2 tekrarla (Auth + Health)
```

**Tahmini sure:** 3-5 dakika

### Yontem 2: Git Revert + Push

```bash
# 1. Mevcut HEAD'i revert et
git revert HEAD --no-edit

# 2. Push et (Coolify auto-deploy tetiklenecek)
git push origin main

# 3. Coolify deployment'in tamamlanmasini bekle
# 4. /health kontrolu
curl -s https://api.mestech.tr/health
```

**Tahmini sure:** 5-10 dakika (build + deploy suresi dahil)

### Yontem 3: Manuel Docker Rollback (Coolify erisim yoksa)

```bash
# 1. SSH ile sunucuya baglan
ssh deploy@<VPS_IP>

# 2. Calisan container'i durdur
docker stop mestech-webapi

# 3. Onceki image'a geri don
docker tag mestech-webapi:previous mestech-webapi:latest
docker compose up -d webapi

# 4. Health check
docker exec mestech-webapi wget -qO- http://localhost:5100/health
```

**Tahmini sure:** 5-8 dakika

---

## Veritabani Migration Rollback

### EF Core Migration Geri Alma

```bash
# 1. Mevcut migration'lari listele
dotnet ef migrations list --project src/MesTech.Infrastructure --startup-project src/MesTech.WebApi

# 2. Onceki migration'a geri don
dotnet ef database update <OncekiMigrationAdi> \
  --project src/MesTech.Infrastructure \
  --startup-project src/MesTech.WebApi \
  --connection "Host=<DB_HOST>;Port=5432;Database=mestech_stok;Username=mestech_user;Password=<PW>"

# 3. Dogrulama
dotnet ef migrations list --project src/MesTech.Infrastructure --startup-project src/MesTech.WebApi
```

### Dikkat Edilecekler
- Migration rollback **veri kaybina** neden olabilir (yeni kolonlar/tablolar silinir)
- **Deploy oncesi** son calisan migration adini kaydet: `_______________`
- Rollback oncesi PostgreSQL snapshot al:
  ```bash
  pg_dump -h localhost -p 3432 -U mestech_user mestech_stok > rollback_backup_$(date +%Y%m%d_%H%M).sql
  ```

---

## DNS Rollback

- DNS TTL deploy oncesi **300s (5dk)** olmali — hizli propagasyon
- Eger VPS degisecekse: DNS A record'u eski IP'ye dondur
- Propagasyon suresi: TTL degeri kadar (max 5dk)

```bash
# DNS kontrol
dig +short mestech.tr
dig +short api.mestech.tr
dig +short panel.mestech.tr
```

---

## Rollback Sonrasi Kontroller

| # | Kontrol | Sonuc |
|---|---------|-------|
| 1 | `/health` → 200 Healthy | [ ] |
| 2 | Login calisiyor | [ ] |
| 3 | Urun listesi yukleniyor | [ ] |
| 4 | Stok miktarlari dogru | [ ] |
| 5 | Pazaryeri sync baslatilabiliyor | [ ] |
| 6 | Container restart sayisi: 0 | [ ] |
| 7 | Seq'te yeni ERROR log yok | [ ] |

---

## Iletisim Plani

### Rollback Baslatildiginda

| Sira | Kime | Nasil | Icerik |
|------|------|-------|--------|
| 1 | Komutan (Proje Lideri) | Anlik mesaj | "Rollback baslatildi: [sebep]. Tahmini sure: [X] dk." |
| 2 | DEV Ekibi | Grup mesaji | "Production rollback aktif. Root cause analizi basliyor." |
| 3 | Aktif kullanicilar | Uygulama ici bildirim | "Sistem bakimi nedeniyle kisa sureli kesinti. Tahmini sure: 10dk." |

### Rollback Tamamlandiginda

| Sira | Kime | Icerik |
|------|------|--------|
| 1 | Komutan | "Rollback tamamlandi. Sistem stabil. Root cause: [aciklama]" |
| 2 | DEV Ekibi | "Sistem eski versiyona donduruldu. Post-mortem toplantisi: [tarih]" |

---

## Post-Mortem (Rollback Sonrasi 24 Saat Icinde)

### Rapor Sablonu

```
ROLLBACK POST-MORTEM — [TARIH]
===============================

1. OZET
   Deploy zamani: ___
   Hata tespit zamani: ___
   Rollback baslama: ___
   Rollback tamamlanma: ___
   Toplam kesinti suresi: ___

2. ROOT CAUSE
   [Hatanin temel nedeni]

3. ETKI
   - Etkilenen kullanici sayisi: ___
   - Veri kaybi: VAR / YOK
   - Gelir etkisi: ___

4. NEDEN YAKALANMADI?
   - CI/CD'de eksik test: ___
   - Staging ortaminda test edilmedi mi?: ___
   - Review sirasinda kacti mi?: ___

5. AKSIYONLAR
   - [ ] [Aksiyon 1 — Sorumlu — Tarih]
   - [ ] [Aksiyon 2 — Sorumlu — Tarih]
   - [ ] [Aksiyon 3 — Sorumlu — Tarih]

6. ONLEME
   - CI/CD'ye eklenecek test: ___
   - Pre-deploy checklist'e eklenecek madde: ___
   - Monitoring'e eklenecek alert: ___

Imza: ___
```

---

## Onemli Bilgiler

| Bilgi | Deger |
|-------|-------|
| Onceki calisan commit hash | _(deploy oncesi doldur)_ |
| Onceki calisan migration | _(deploy oncesi doldur)_ |
| Onceki calisan Docker image tag | _(deploy oncesi doldur)_ |
| VPS IP | _(deploy oncesi doldur)_ |
| Coolify Dashboard URL | _(deploy oncesi doldur)_ |
| PostgreSQL backup dizini | `/backups/pg/` |
| Docker compose dosyasi | `docker-compose.yml` |
