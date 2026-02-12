# Legacy Uyarı ve Doğrulama Notu

Bu not, eski/yeni bilgiler karışmasın ve üretim kalitesi korunarak ilerlenmesi için uygulanacak kontrol kapılarını tanımlar.

## 1) Değişiklik Öncesi 4 Soru (G‑D‑G‑E)
- Gerçek mi? (Mevcut davranış/şema gerçekten bu mu?)
- Doğru mu? (Doğru kaynak/dosya mı? `appsettings.user.json` öncelikli mi?)
- Gerekli mi? (Şu anki sorun için bu değişikliğe ihtiyaç var mı?)
- Etkisi ve geri dönüşü net mi? (Rollback planı hazır mı?)

Bu sorular “evet” olmadan değişiklik uygulanmaz.

## 2) Kaynak Öncelikleri
1. Çalışan uygulama davranışı ve loglar
2. Veritabanı gerçek durumu (`__EFMigrationsHistory`, mevcut tablolar/indeksler)
3. `appsettings.user.json` (kullanıcı/ortam bazlı override)
4. Kod (Desktop/Core) – canlı kod tabanı
5. Dokümanlar – yalnızca referans; koddan saparsa kod kazanır

## 3) Sık Karışan Başlıklar ve Kurallar
- Barkod: Sadece `MesTechStok.Desktop/appsettings.user.json` aktif ayardır. Varsayılan dosyayı yayın paketine taşımayın. ROI yüzdeleri yüzde veya kesir formatında desteklenir; logda aktif preset yazdırılır.
- Ekran koruyucu: Eski EXE’ler `MesTechStok/Backups/Eski_EkranKoruyucu/<tarih_saat>` altında arşivlenir. Yayın yolunda tek EXE kalır.
- Ürün modülü: Eski “NewProductsView” fallback yoluna dönülmez.
- Migrations: Üretimde `EnsureCreated` kullanılmaz; EF migrations uygulanır. `__EFMigrationsHistory` zorunludur.
- Excel İçe Aktar: ‘;’ ile çoklu görsel (en fazla 8) beklenir; kategori butonları aktif.

## 4) Uygulama Adımları
1. Log ve DB ile mevcut durumu doğrula.
2. Gerekliyse değişiklik yap; değilse dokümanı güncelle.
3. Smoke test maddesini işle ve sonucu not düş.
4. Rollback noktasını doğrula (yedek/versiyon).

## 5) Referans
- `Docs/Raporlar/Rapor_1408/99_Canliya_Alma_Kilavuzu.md` – Canlı öncesi/sonrası kontrol listeleri
- `Backups/Yedek_Barkod_Ayarlari/` – Barkod ayar yedekleri
- `Backups/Eski_EkranKoruyucu/` – Eski ekran koruyucu derlemeleri


