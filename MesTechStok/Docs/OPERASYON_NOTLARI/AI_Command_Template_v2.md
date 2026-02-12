# Yapay Zekâ Emir Şablonu v2 (AI Command Template Advanced)

Bu şablon, yapay zekâyı **A++++++ kalite, izlenebilir, hatasız ve
inovatif** şekilde çalıştırmak için hazırlanmıştır.
Her proje başlangıcında uygulanmalı ve proje boyunca güncel
tutulmalıdır.

---

## 1) Çekirdek Emirler (Geliştirilmiş)

- **Sistematik çalış, hiçbir adımı atlama, her adım arasında kontrol
  kapısı aç.**
- **Her adımı bitirince önce kendi kendini test et, sonra bana kanıtlı
  raporla.**
- **Eksikleri tespit et, sınıflandır (kritik/orta/düşük), çözüm öner
  ve uygula.**
- **Ezbere değil, bağlama uygun, bilinçli ve gerekçeli yaz.**
- **Her kritik noktada: doğruluk, gereklilik, etki, rollback planı var
  mı kontrol et.**
- **Devam → aynı kaliteyle ilerle, motivasyonu ve titizliği
  kaybetme.**
- **İnovasyon fırsatlarını da belirle, sadece var olanı uygulama.**

---

## 2) Kontrol & Tekrar Kalıpları (Disiplin)

- "Bu adım %100 tamamlandı mı? Kanıtı nedir? (log, test, çıktı, ekran
  görüntüsü)"
- "Kontrol listesi olarak tekrar et, eksik varsa işaretle."
- "Eksik varsa raporla, yoksa ✅ işaretle ve devam et."
- "Devam etmeden önce kritik adımları otomatik test et, log kaydını
  doğrula."
- "Adımı bitirdiysen, bir üst bakış açısıyla yeniden kontrol et
  (meta-check)."

---

## 3) Motivasyon & Ritim

- "Devam" komutu ritmi sürdürür, motivasyonu kaybettirmez.
- Büyük görevleri **alt parçalara böl** (ör. Veritabanı → Servisler →
  UI → Loglama → Test).
- Her parça sonunda: **A++++ kalite raporu hazırla, log ve test kanıtı
  sun.**
- Gerektiğinde "tekrar kontrol" komutu ile kendi çıktını yeniden
  incele.
- Gereksiz tekrar veya kopya üretme → öz, net, kanıtlı ilerle.

---

## 4) Teknik Disiplin ve Loglama

- Her kritik adımı logla (başlangıç, bitiş, hata, metrik).
- Global hata yakalama stratejilerini kontrol et.
- Performans ölçümlerini ekle (süre, bellek, sorgu sayısı).
- Log analizini raporla: kaç hata, kaç uyarı, hangi senaryo başarısız?
- Gerektiğinde otomatik hata çözüm önerileri sun.

---

## 5) Uygulama Senaryosu (Canlıya Alma Örneği)

1. **SQL Şema**
   - Eksikleri tamamla, indeksleri kontrol et, veri bütünlüğünü
     doğrula.
   - Kanıt: SQL çıktısı + log.
2. **EF Migrations**
   - Çalıştır, `__EFMigrationsHistory` tablosunu kontrol et.
   - Kanıt: Migration kaydı + log.
3. **Servis Katmanı**
   - Zorunlu metodlar eksiksiz mi? Test data kaldırıldı mı?
   - Kanıt: Unit test + log.
4. **UI & Excel**
   - Görseller, Excel sihirbazı, kolon ayarları doğrulandı mı?
   - Kanıt: Görsel rapor + log.
5. **Barkod Entegrasyonu**
   - Donanım testi + log kaydı doğrulandı mı?
   - Kanıt: Okunan barkod + ürün eşleşme logu.
6. **Log & Telemetri**
   - Serilog dosya logları ve telemetri tabloları aktif mi?
   - Kanıt: Log dosyası + SQL kayıtları.
7. **Smoke Test**
   - 8 adım planı uygula, tek tek işaretle.
   - Kanıt: Test listesi + sonuç raporu.

---

## 6) Emir Kullanım Örneği (İleri Seviye)

- "SQL şemayı kontrol et, eksikleri tamamla, log kaydıyla doğrula,
  sonra raporla."\
- "EF migrations çalıştır, tablo doldu mu? Log kanıtını sun."\
- "Eksikleri listele, kritik/orta/düşük seviyede sınıflandır, çöz ve
  tekrar test et."\
- "Her adımı bitirince meta-check yap ve bana kanıtlı A++++ raporu
  hazırla."\
- "Eksik yoksa ✅ işaretle, inovasyon önerisi varsa +⭐ not et."

---

## 7) İnovasyon ve Geliştirme Emirleri

- "Bu adımı yaparken yenilikçi geliştirme fırsatlarını da belirle."\
- "IoT, yapay zekâ, otomasyon, entegrasyon gibi alanlarda ek değer
  öner."\
- "Sadece var olanı değil, gelecekte işimize yarayacak ekstra
  modülleri de öner."

---

## 8) Not

Bu şablon, önceki sürümden farklı olarak **loglama, hata yakalama,
performans ve inovasyon odaklı** geliştirilmiştir.
Amaç: **Titizlik, netlik, tekrar, izlenebilirlik, profesyonellik ve
inovasyon** ile tam hakimiyet sağlamak.
