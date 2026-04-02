# BORÇ EVRİM TABLOSU — DEV 1
Son güncelleme: 2026-04-02 TUR 17

| Borç Kalemi               | Başlangıç | Şimdi | Trend    | Durum     |
|---------------------------|-----------|-------|----------|-----------|
| KÖK-1 VM DbContext        | ~20       | 0     | ✅       | KAPANDI   |
| KÖK-1 Infra IDbContextFactory | 8    | 0     | ✅       | KAPANDI (02d40336) |
| KÖK-1 Repo+Service       | 126       | 126   | ■ KASITLI| UnitOfWork pattern DOĞRU |
| Build error (Application) | 9+        | 0     | ✅       | KAPANDI   |
| Build error (WebApi)      | var       | 0     | ✅       | KAPANDI   |
| TODO/FIXME (DEV1 alanı)  | 0         | 0     | ✅       | TEMİZ     |
| Domain event orphan       | 18        | 0     | ✅       | KAPANDI (74/74 handled) |
| EF Migration drift        | 98        | 0     | ✅       | KAPANDI   |
| CompanySettings column    | 4 eksik   | 0     | ✅       | ALTER TABLE uygulandı |
| STUB handler              | 2         | 0     | ✅       | KAPANDI (ef0ab8c6) |
| ApproveAccountingEntry EP | 1 orphan  | 0     | ✅       | KAPANDI (4b7f4237) |
| Entity test gap           | 40        | 32    | ↓ azalıyor| 8 yeni test yazıldı |
| Accounting handler test   | 45 eksik  | 0     | ✅       | KAPANDI (44/44) |
| KÖK-1 DI test             | 0         | 8     | ✅ yeni  | 8 test yazıldı |
| ViewModel MediatR wiring  | 8 hardcoded| 0    | ✅       | KAPANDI (5 VM fix) |
| CancellationToken sync    | 5 repo    | 0     | ✅       | KAPANDI   |
| Katman 1 screenshot       | —         | 172   | ✅       | %100 render |
| Katman 1.5 seed           | EKSİK    | TAM   | ✅       | 1fe0950b  |
| Sözleşme hataları         | 8+        | —     | raporlandı| G29,G37,G40 |

## DARBOĞAZ ANALİZİ

KÖK-1 Repo+Service (126): UnitOfWork BOZULUR — refactor GEREKSİZ (G40).
Entity test gap (32): Devam eden iş — her turda 5-8 entity testi yazılıyor.

## PARKED KALEMLER
- KÖK-1 Repo IDbContextFactory (126): KASITLI BIRAKILDI — UnitOfWork pattern doğru
- Core ref Desktop (125): WPF ARŞİVLENECEK — Avalonia geçişi sonrası

## KEŞFEDİLEN YENİ ÖZELLİKLER
- Z6 CommissionCharged→GL: PARKED (handler kodu var ama CommissionChargedEvent tanımı kontrol gerekli)
- Röntgen: 4 platform (Etsy/Shopify/WooCommerce/Zalando) generic entity model kullanıyor — platform-spesifik entity YOK, PlatformType enum ile ayrılıyor (DOĞRU mimari)
