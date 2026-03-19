# Post-Deploy Izleme Plani — Ilk 48 Saat

> EMR-18 D-04 | Deploy sonrasi izleme ve mudahale protokolu

---

## Saat 0-2: Aktif Gozlem (KRITIK DONEM)

### Gorevler
- [ ] Container loglari surekli izle: `docker compose logs -f --tail=100`
- [ ] Grafana dashboard'u acik tut — CPU, RAM, disk, network metrikleri
- [ ] `/health` endpoint'ini her 5 dakikada bir ping'le:
  ```bash
  watch -n 300 'curl -s https://api.mestech.tr/health'
  ```
- [ ] Seq UI'da ERROR ve FATAL seviye log kontrolu (filtre: `@Level in ['Error', 'Fatal']`)
- [ ] Container restart sayisini izle: `docker ps --format "{{.Names}}: {{.Status}}"`
- [ ] PostgreSQL connection pool doluluk orani kontrolu
- [ ] Redis memory kullanimi kontrolu: `redis-cli INFO memory | grep used_memory_human`
- [ ] RabbitMQ kuyruk birikimi kontrolu (Management UI: port 3692)

### Eskalasyon Kriterleri (Saat 0-2)
| Durum | Aksiyon |
|-------|---------|
| /health DOWN >2dk | Rollback hazirligina basla |
| Container 2+ restart | Log analizi + root cause |
| Error rate >10% | Trafik yonlendirmesini durdur |
| DB connection timeout | Connection string + pool kontrolu |
| RAM >85% | Container memory limit kontrolu |

### Kayit
- [ ] Saat 0:30 durum: ___
- [ ] Saat 1:00 durum: ___
- [ ] Saat 1:30 durum: ___
- [ ] Saat 2:00 durum: ___

---

## Saat 2-8: Pasif Gozlem

### Gorevler
- [ ] Monitoring alert'leri aktif (Grafana alert rules kontrol)
- [ ] Seq'te otomatik alert kurulumu dogrula (Error threshold: 5/dk)
- [ ] Her saat basinda `/health` kontrol (otomatik cron veya Coolify health)
- [ ] Ilk gercek kullanici islemlerini izle:
  - [ ] Ilk basarili login
  - [ ] Ilk urun kaydi
  - [ ] Ilk siparis islemi
  - [ ] Ilk pazaryeri senkronizasyonu
- [ ] Disk I/O ve PostgreSQL slow query loglari kontrol
- [ ] MinIO bucket erisimi dogrula (belge upload/download)
- [ ] RabbitMQ consumer'lar mesaj isliyor mu kontrol

### Otomatik Izleme Checklist
- [ ] Coolify health check yesil
- [ ] Prometheus scrape basarili (target up)
- [ ] Grafana alert kanalı calisiyor (test alert gonder)

### Kayit
- [ ] Saat 4:00 durum: ___
- [ ] Saat 6:00 durum: ___
- [ ] Saat 8:00 durum: ___

---

## Saat 8-24: Ilk Gun Raporu

### Gorevler
- [ ] Container uptime kontrol (hicbir restart olmamali)
- [ ] Toplam hata sayisi rapor: `Seq → Events → @Level = 'Error' → Count`
- [ ] API response time ortalamasi (Grafana'dan oku):
  - p50: ___ ms
  - p95: ___ ms
  - p99: ___ ms
- [ ] PostgreSQL:
  - [ ] Aktif baglanti sayisi: ___
  - [ ] Slow query sayisi (>1s): ___
  - [ ] DB boyutu: ___
- [ ] Redis:
  - [ ] Cache hit rate: ___%
  - [ ] Memory kullanimi: ___
- [ ] RabbitMQ:
  - [ ] Kuyruk birikimi: ___ mesaj
  - [ ] Consumer sayisi: ___
  - [ ] Dead letter kuyrugu: ___ mesaj
- [ ] Disk kullanimi:
  - [ ] Root partition: ___%
  - [ ] PG data volume: ___
  - [ ] MinIO volume: ___

### Ilk Gun Raporu Sablonu

```
ILGI GUN RAPORU — [TARIH]
=========================
Deploy saati: ___
Rapor saati: ___

Durum: STABIL / UYARI / KRITIK

Container restart: ___
Toplam error: ___
API p95 response: ___ ms
DB baglanti: ___/max
Disk: ___%

Ozel notlar:
-
-

Karar: DEVAM / IZLE / ROLLBACK
Imza: ___
```

---

## Saat 24-48: Ikinci Gun

### Gorevler
- [ ] Birinci gun raporunu gozden gecir — trend analizi
- [ ] PostgreSQL backup dogrula:
  - [ ] Cron job calisti mi: `ls -la /backups/pg/` veya Coolify backup log
  - [ ] Backup dosya boyutu makul mu (>0 byte, beklenen aralikta)
  - [ ] Backup restore testi (staging ortaminda): `pg_restore --dry-run`
- [ ] Log rotation calisiyor mu (Docker log driver + Seq retention)
- [ ] Uzun sureli memory leak kontrolu:
  - [ ] Container memory trendi (Grafana 24h gorsel)
  - [ ] .NET GC metrikleri (artis trendi var mi?)
- [ ] Pazaryeri entegrasyonlari 24 saat boyunca calisti mi:
  - [ ] Trendyol siparis sync: ___
  - [ ] Hepsiburada siparis sync: ___
  - [ ] N11 siparis sync: ___
- [ ] Hangfire job'lari calisiyor mu:
  - [ ] Basarili job sayisi: ___
  - [ ] Basarisiz job sayisi: ___
  - [ ] Retry kutugundeki job sayisi: ___
- [ ] Kullanici geri bildirimi topla

### Ikinci Gun Raporu Sablonu

```
IKINCI GUN RAPORU — [TARIH]
============================
48 saat doldu.

Toplam uptime: ___
Container restart (48h): ___
Error trendi: AZALAN / SABIT / ARTAN
Memory trendi: SABIT / YUKSELEN
Disk trendi: ___

Pazaryeri sync basari orani: ___%
Backup durumu: BASARILI / BASARISIZ

Karar: STABIL ILAN / IZLEMEYE DEVAM / ROLLBACK
Imza: ___
```

---

## 48 Saat Sonrasi

### Stabil Ilan Kriterleri (TUMU saglanmali)
- [ ] Container restart: 0
- [ ] Error rate: <1%
- [ ] API p95: <500ms
- [ ] DB connection pool: <60% dolu
- [ ] Memory trendi: sabit (artis yok)
- [ ] Disk kullanimi: <70%
- [ ] Backup: En az 1 basarili restore testi
- [ ] Pazaryeri sync: >95% basari orani

### Stabil Ilan Sonrasi
1. Izleme sikligini azalt (aktif → pasif)
2. Monitoring alert threshold'larini finalize et
3. Operasyonel dokumantasyonu guncelle
4. Takim retrospektif toplantisi planla
5. Sonraki dalga (D+1) planlamasina gec

---

## Iletisim Protokolu

| Seviye | Durum | Bildirim |
|--------|-------|----------|
| INFO | Normal isleyis | Gunluk rapor |
| WARN | Metrik threshold asildi | Anlik bildirim + 1 saat izle |
| ERROR | Servis kesintisi <5dk | Anlik bildirim + mudahale |
| CRITICAL | Servis kesintisi >5dk veya veri kaybi riski | Rollback baslatilir + Komutan bilgilendirilir |
