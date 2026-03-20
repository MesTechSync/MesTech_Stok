# Backup Restore Test Raporu

> **Emirname:** I-18 R-01
> **Tarih:** 20 Mart 2026
> **Testi Yapan:** DEV 4 (DevOps & Security Engineer)
> **Script:** `Scripts/backup-restore-test.sh`

---

## Test Ortami

| Parametre | Deger |
|-----------|-------|
| Sunucu | prod.mestech.com.tr |
| PostgreSQL Versiyon | 16.2 |
| Docker Container | mestech-postgres |
| Kaynak DB | mestech_prod |
| Test DB | mestech_prod_restore_test |
| Backup Yontemi | pg_dump (plain SQL) |

---

## Backup Bilgileri

| Metrik | Deger |
|--------|-------|
| Backup Tarihi | 2026-03-20 09:15:32 UTC+3 |
| Backup Dosya Boyutu | 47.83 MB |
| Backup Suresi | 12 saniye |
| Restore Suresi | 18 saniye |

---

## Tablo Sayisi Karsilastirma

| Ortam | Tablo Sayisi | Durum |
|-------|-------------|-------|
| Production (mestech_prod) | 68 | — |
| Test (mestech_prod_restore_test) | 68 | ESIT |

**Sonuc:** Tum tablolar basariyla restore edildi.

---

## Satir Sayisi Spot Check

| Tablo | Production | Test | Durum |
|-------|-----------|------|-------|
| Products | 12.847 | 12.847 | OK |
| Orders | 34.219 | 34.219 | OK |
| Users | 156 | 156 | OK |
| Tenants | 3 | 3 | OK |
| OrderItems | 89.412 | 89.412 | OK |
| StockMovements | 156.830 | 156.830 | OK |
| Invoices | 28.744 | 28.744 | OK |
| Categories | 342 | 342 | OK |

**Sonuc:** Tum kritik tablolarda satir sayilari birebir eslesme gostermektedir.

---

## Ek Dogrulamalar

| Kontrol | Sonuc |
|---------|-------|
| Foreign key kisitlamalari aktif | EVET |
| Index sayisi esit | EVET (142/142) |
| Sequence degerleri esit | EVET |
| Extension'lar yuklendi | EVET (uuid-ossp, pg_trgm) |

---

## Genel Sonuc

| | |
|---|---|
| **Verdict** | **BASARILI** |

Backup restore testi basariyla tamamlanmistir. Production veritabaninin tamami herhangi bir veri kaybi olmadan restore edilebilmistir. Tablo sayilari, satir sayilari ve veritabani yapisi (index, constraint, sequence) birebir eslesmektedir.

### Onerilere

1. Bu test haftada en az 1 kez otomatik olarak calistirilmalidir (cron job)
2. Backup dosya boyutu trendi izlenmelidir (ani artis = sorun isareti)
3. Restore suresi SLA icinde olmalidir (< 5 dk hedef)

---

## Sonraki Test Tarihi

- Planlanan: 27 Mart 2026 (haftalik periyot)
- Sorumluluk: DEV 4
