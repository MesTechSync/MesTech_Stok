# Canli Smoke Test — 50 Kontrol Noktasi

> EMR-18 D-03 | Deploy sonrasi canli sistem dogrulama

---

## Tarih: ___
## Test Eden: ___
## Ortam: Production
## API URL: https://api.mestech.tr
## Panel URL: https://panel.mestech.tr

## Skor: ___/50

---

### Bolum 1: AUTH + GIRIS (5 kontrol)

- [ ] 1.1: Login sayfasi yukleniyor (HTTP 200, <3s)
- [ ] 1.2: Yanlis sifre → hata mesaji (kullanici adi sizdirilmiyor, generic "Gecersiz bilgi" mesaji)
- [ ] 1.3: 5 yanlis deneme → lockout veya rate-limit (429 veya gecici engel)
- [ ] 1.4: Dogru credential → Dashboard / JWT token donuyor
- [ ] 1.5: Sayfa yenileme / token refresh → session korunuyor (401 degil)

### Bolum 2: HEALTH CHECK + ALTYAPI (5 kontrol)

- [ ] 2.1: `GET /health` → HTTP 200, response body `Healthy`
- [ ] 2.2: PostgreSQL baglantisi aktif (health check detayinda `postgres: Healthy`)
- [ ] 2.3: Redis baglantisi aktif (cache islemleri calisiyor)
- [ ] 2.4: RabbitMQ baglantisi aktif (management UI erisilebidir: port 3692)
- [ ] 2.5: Seq log ingestion calisiyor (uygulama loglar Seq UI'da gorunuyor: port 3580)

### Bolum 3: URUN YONETIMI (5 kontrol)

- [ ] 3.1: Urun listesi endpoint'i calisiyor (`GET /api/products` → 200 + JSON array)
- [ ] 3.2: Urun detay getirme (`GET /api/products/{id}` → 200 + urun detayi)
- [ ] 3.3: Urun ekleme (`POST /api/products` → 201 Created)
- [ ] 3.4: Urun guncelleme (`PUT /api/products/{id}` → 200 OK)
- [ ] 3.5: Urun arama/filtreleme calisiyor (query parameters ile)

### Bolum 4: STOK YONETIMI (5 kontrol)

- [ ] 4.1: Stok listesi calisiyor (`GET /api/stock` → 200)
- [ ] 4.2: Stok hareketi ekleme (giris/cikis) → basarili response
- [ ] 4.3: Stok miktari dogru guncelleniyor (hareket sonrasi kontrol)
- [ ] 4.4: Dusuk stok uyarisi tetikleniyor (threshold altinda)
- [ ] 4.5: Stok raporu endpoint'i calisiyor

### Bolum 5: SIPARIS YONETIMI (5 kontrol)

- [ ] 5.1: Siparis listesi calisiyor (`GET /api/orders` → 200)
- [ ] 5.2: Yeni siparis olusturma basarili
- [ ] 5.3: Siparis durumu guncelleme basarili (status transition)
- [ ] 5.4: Siparis detayi getirme → urun satirlari dahil
- [ ] 5.5: Siparis → stok dusumu otomatik calisiyor

### Bolum 6: PAZARYERI ENTEGRASYONU (5 kontrol)

- [ ] 6.1: Trendyol adapter baglantisi aktif (API key ile test ping)
- [ ] 6.2: Hepsiburada adapter calisiyor
- [ ] 6.3: N11 adapter calisiyor
- [ ] 6.4: Pazaryeri siparis senkronizasyonu baslatilabiliyor
- [ ] 6.5: Urun fiyat/stok guncelleme pazaryerine push ediliyor

### Bolum 7: E-FATURA + MUHASEBE (5 kontrol)

- [ ] 7.1: E-fatura olusturma endpoint'i calisiyor
- [ ] 7.2: Fatura PDF uretimi basarili
- [ ] 7.3: Sovos/GiB provider baglantisi aktif
- [ ] 7.4: Cari hesap listesi calisiyor
- [ ] 7.5: Gelir-gider raporu endpoint'i calisiyor

### Bolum 8: KARGO ENTEGRASYONU (5 kontrol)

- [ ] 8.1: Kargo firmasi listesi calisiyor (Yurtici, Aras, Surat)
- [ ] 8.2: Kargo etiketi olusturma basarili
- [ ] 8.3: Gonderi takip numarasi alinabiliyor
- [ ] 8.4: Kargo durum sorgulama calisiyor
- [ ] 8.5: Toplu kargo islem baslatilabiliyor

### Bolum 9: PERFORMANS + GUVENLIK (5 kontrol)

- [ ] 9.1: API response suresi <500ms (ortalama endpoint'ler)
- [ ] 9.2: CORS ayarlari dogru (izin verilen origin'ler)
- [ ] 9.3: JWT token expiry calisiyor (suresi dolmus token → 401)
- [ ] 9.4: Rate limiting aktif (asiri istek → 429)
- [ ] 9.5: HTTPS zorunlulugu aktif (HTTP → HTTPS redirect)

### Bolum 10: IZLEME + LOGLAMA (5 kontrol)

- [ ] 10.1: Prometheus metrikleri aliniyor (`/metrics` endpoint veya prometheus scrape)
- [ ] 10.2: Grafana dashboard'lar yukleniyor
- [ ] 10.3: Seq'te son 5dk log kaydi gorunuyor
- [ ] 10.4: Container restart sayisi 0 (ilk 30dk)
- [ ] 10.5: Disk kullanimi normal (<80% root, <70% data volumes)

---

## Sonuc Tablosu

| Bolum | Basarili | Basarisiz | Notlar |
|-------|----------|-----------|--------|
| 1. Auth + Giris | /5 | | |
| 2. Health Check | /5 | | |
| 3. Urun Yonetimi | /5 | | |
| 4. Stok Yonetimi | /5 | | |
| 5. Siparis Yonetimi | /5 | | |
| 6. Pazaryeri | /5 | | |
| 7. E-Fatura | /5 | | |
| 8. Kargo | /5 | | |
| 9. Performans | /5 | | |
| 10. Izleme | /5 | | |
| **TOPLAM** | **/50** | | |

## Karar

- [ ] **GECTI** (45+/50) → Canli kalir
- [ ] **SARTLI GECTI** (35-44/50) → 24 saat icinde fix + re-test
- [ ] **KALDI** (<35/50) → Rollback baslatilir (bkz. `Rollback_Plan.md`)

## Imza

| Rol | Kisi | Tarih | Onay |
|-----|------|-------|------|
| Test Eden | | | |
| Komutan | | | |
