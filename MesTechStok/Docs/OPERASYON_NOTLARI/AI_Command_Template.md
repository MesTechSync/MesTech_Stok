# Yapay Zekâ Emir Şablonu (AI Command Template)

Bu şablon, yapay zekâyı **en yüksek disiplinle (A++++ kalite)**
çalıştırmak için kullanılacak standart komut setidir.\
Her proje başında bu şablon uygulanır, eksiksiz kontrol sağlanır.

------------------------------------------------------------------------

## 1) Çekirdek Emirler

-   **Sistematik çalış, hiçbir adımı atlama.**
-   **Her adımı bitirince önce kendi kendini kontrol et, sonra
    raporla.**
-   **Eksikleri tespit et, yaz ve çözümünü uygula.**
-   **Ezbere değil, bağlama uygun ve bilinçli yaz.**
-   **Her kritik noktada: doğruluk, gereklilik, geri dönüş planı var mı
    kontrol et.**
-   **Devam → aynı kaliteyle ilerle.**

------------------------------------------------------------------------

## 2) Kontrol & Tekrar Kalıpları

-   "Bu adım %100 tamamlandı mı? Kanıtı nedir?"
-   "Kontrol listesi olarak tekrar et."
-   "Eksik varsa raporla, yoksa ✅ işaretle."
-   "Devam etmeden önce kritik adımları kendi kendine test et."

------------------------------------------------------------------------

## 3) Motivasyon & Disiplin

-   "Devam" komutu ile ritmi sürdür.
-   Büyük görevleri alt parçalara böl: (ör. Veritabanı → Servisler → UI
    → Test).
-   Her parça sonunda "A++++ kalite raporu hazırla".
-   Gerektiğinde "tekrar kontrol" de.

------------------------------------------------------------------------

## 4) Uygulama Senaryosu (Örnek: Canlıya Alma)

1.  **SQL Şema**
    -   Eksikleri tamamla, indeksleri kontrol et, tekrar doğrula.
2.  **EF Migrations**
    -   Çalıştır, `__EFMigrationsHistory` tablosu doldu mu kontrol et.
3.  **Servis Katmanı**
    -   Zorunlu metodlar eksiksiz mi? Test data kaldırıldı mı?
4.  **UI & Excel**
    -   Görseller, Excel sihirbazı, kolon ayarları kontrol edildi mi?
5.  **Barkod Entegrasyonu**
    -   Donanım testi + log kaydı doğrulandı mı?
6.  **Log & Telemetri**
    -   Serilog dosya logları ve telemetri tabloları aktif mi?
7.  **Smoke Test**
    -   8 adım kontrol planı uygula, tek tek işaretle.

------------------------------------------------------------------------

## 5) Emir Kullanım Örneği

-   "SQL şemayı kontrol et, eksikleri tamamla, tekrar doğrula."\
-   "EF migrations çalıştır, tablo doldu mu?"\
-   "Eksikleri listele, çöz ve tekrar test et."\
-   "Eksik yoksa ✅ işaretle ve devam et."

------------------------------------------------------------------------

## 6) Not

Bu şablon her proje başlangıcında uygulanmalı.\
Amaç: **Titizlik, netlik, tekrar ve profesyonellik** ile tam hakimiyet
sağlamak.
