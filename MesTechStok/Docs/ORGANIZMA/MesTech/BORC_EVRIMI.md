# BORÇ EVRİMİ TABLOSU — DEV 3 (Entegrasyon & Adapter)
# Son güncelleme: 2 Nisan 2026 — DEV3 TUR3-FULL

## KAPANAN BORÇLAR

| Borç | Başlangıç | Son | Kapanış Tarihi | Commit |
|------|-----------|-----|----------------|--------|
| Feed placeholder URL | 2 | 0 | 2 Nisan | b0c0a69b |
| Settlement parser 15/16 | 15 | 16/16 | 2 Nisan | 8664f366 |
| Trendyol eksik method | 37 | 46 (+9) | 2 Nisan | 47277b95 |
| Adapter NotImpl | 0 | 0 | — | — |
| Boş catch | 0 | 0 | — | — |
| TODO/FIXME | 0 | 0 | — | — |
| ERP dual interface | 1 gap | 0 | 2 Nisan | 52d78a97 |
| SOAP namespace revert | 4 bozuk | 0 | 2 Nisan | revert |
| Sözleşme fix (4 adet) | 4 hata | 0 | 2 Nisan | çeşitli |

## AÇIK BORÇLAR

| Borç | Değer | Hedef | Öncelik | Atanan | Durum |
|------|-------|-------|---------|--------|-------|
| Sandbox test dosyası | 0 | 16 | P2 | DEV5 | ACIK (G10812) |
| Hangfire job sözleşme | 42 gerçek / 21 sözleşme | güncelle | INFO | — | KAPANDI |
| AdapterMetrics dual (Prometheus+OTel) | 2 | 1 | INFO | — | PARKED (OTel geçişi bekliyor) |
| Test projesi build error | 5+ CS0246 | 0 | P0 | DEV5 | ACIK (G10811) |
| SOAP linter kuralı | tekrar kırıyor | istisna ekle | P0 | DEV4 | ACIK (G10810) |
| Dotnet process lock | 32-42 process | 0 | P0 | DEV4 | ACIK (G10813) |
| Trendyol credential | yok | env var | P1 | KOMUTAN | ACIK (G10816) |
| Katman 1 Headless | BLOCKED | çalışır | P1 | DEV3 | BLOCKED (G10814) |

## KEŞİF/TUR ORANI

| TUR | Keşif | Cross-DEV | Düzeltme |
|-----|-------|-----------|----------|
| T1-önceki | 7 tarama | 10 görev | 9 commit |
| T1-FULL | 3 bulgu (SOAP revert, test build, sandbox) | 3 görev | 1 revert |
| T2-FULL | 2 bulgu (process lock, credential) | 2 görev | 1 tool |
| T3-FULL | — | — | BORC_EVRIMI yazıldı |
