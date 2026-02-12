# Rapor 8: Eksik Tespit, İyileştirme ve İş Planı

**Rapor Tarihi:** 14 Ağustos 2025
**Referans Doküman:** `MesTechStok_v1.md` (Bölüm 8, 9)

---

## 1. Amaç

Bu rapor, MesTech Stok projesinin kod tabanında ve mimarisinde yapılan detaylı analizler sonucunda tespit edilen tüm eksiklikleri, hataları, güvenlik zafiyetlerini ve performans darboğazlarını listeler. Ayrıca, bu sorunları çözmek için önceliklendirilmiş, somut adımlardan oluşan bir iş planı ve yol haritası sunar.

---

## 2. Genel Değerlendirme

Proje, Dependency Injection ve katmanlı mimari gibi modern .NET prensipleriyle doğru bir temel üzerine kurulmuştur. Ancak, mevcut haliyle bir "iskelet" veya "prototip" aşamasındadır. Kritik işlevler ya eksiktir ya da içleri boştur. Güvenlik ve performansla ilgili ciddi zafiyetler barındırmaktadır.

**Sonuç:** Proje, bu raporda belirtilen kritik ve yüksek öncelikli maddeler giderilmeden canlı (production) ortama **kesinlikle alınamaz.**

---

## 3. Tespit Edilen Eksiklikler ve Sorunlar

Aşağıdaki tablo, `MesTechStok_v1.md`'de genel olarak bahsedilen sorunların, kod analiziyle somutlaştırılmış halidir.

| ID | Kategori | Sorun / Eksiklik | Etkilenen Alan(lar) | Öncelik |
| :-- | :--- | :--- | :--- | :--- |
| **SEC-01** | **Güvenlik** | **Kritik SQL Injection Zafiyeti** | `SettingsView.xaml.cs` | **KRİTİK** |
| **SEC-02** | **Güvenlik** | Hassas Verilerin (API Key) Güvensiz Depolanması | Ayarlar, Veritabanı | **KRİTİK** |
| **ARC-01** | **Mimari** | **`ICustomerService` Tamamen Eksik** | `Core`, `Desktop` | **YÜKSEK** |
| **ARC-02** | **Mimari** | Kritik Servislerin İçinin Boş Olması | `TokenRotationService`, `SyncRetryService` | **YÜKSEK** |
| **PERF-01**| **Performans**| **UI Donması (UI Thread Blocking)** | `ProductsView`, `SettingsView` (`async void`) | **YÜKSEK** |
| **DB-01** | **Veritabanı**| Eş Zamanlılık (Concurrency) Yönetimi Eksikliği | Tüm ana tablolar | **YÜKSEK** |
| **CODE-01**| **Kod Kalitesi**| Yapılandırılmış Loglama Sisteminin Olmaması | Proje Geneli | **YÜKSEK** |
| **DB-02** | **Veritabanı**| N+1 Sorgu Problemi Riski | Veri listeleme sorguları | **ORTA** |
| **PERF-02**| **Performans**| Değişiklik İzlemenin Gereksiz Kullanımı | Sadece okuma amaçlı sorgular | **ORTA** |
| **TEST-01**| **Kalite** | Birim ve Entegrasyon Testlerinin Olmaması | Proje Geneli | **ORTA** |
| **UI-01** | **UI/UX** | Tutarlı Bir Tasarım Sisteminin Olmaması | Proje Geneli | **DÜŞÜK** |

---

## 4. İyileştirme İş Planı ve Yol Haritası

Bu plan, projeyi adım adım sağlam ve güvenilir bir hale getirmeyi hedefler.

### **Aşama 1: Stabilizasyon ve Güvenlik (Süre: 1 Hafta)**

Bu aşamanın amacı, en kritik riskleri ortadan kaldırmaktır.

1.  **Görev SEC-01:** `SettingsView.xaml.cs`'deki SQL Injection zafiyetini, tüm veritabanı işlemlerini EF Core'a taşıyarak giderin.
2.  **Görev SEC-02:** Hassas verileri (API anahtarları, şifreler) Windows DPAPI kullanarak şifreleyin.
3.  **Görev PERF-01:** `async void` olay yöneticilerini, `ICommand` deseni ve `async Task` metotları ile değiştirerek UI donmalarını tamamen engelleyin.
4.  **Görev CODE-01:** Serilog kütüphanesini projeye entegre edin. Tüm `try-catch` bloklarına ve önemli metotlara yapısal loglama ekleyin.

### **Aşama 2: Temel İşlevselliği Tamamlama (Süre: 2 Hafta)**

Bu aşama, eksik olan temel modülleri hayata geçirmeyi hedefler.

1.  **Görev ARC-01:** `ICustomerService` arayüzünü, `RealCustomerService` sınıfını ve ilgili `ViewModel`/`View`'ları oluşturarak müşteri yönetimi işlevini tamamlayın.
2.  **Görev ARC-02:** `TokenRotationService` ve `SyncRetryService` (Polly kütüphanesi ile) başta olmak üzere, içi boş olan tüm servisleri implemente edin.
3.  **Görev DB-01:** Tüm ana tablolara `RowVersion` kolonu ekleyin ve `DbUpdateConcurrencyException`'ı yakalayıp yöneten mekanizmayı kurun.
4.  **Görev TEST-01:** Projeye bir xUnit test projesi ekleyin. Bu aşamada yazılan yeni servisler için ilk birim testlerini yazın.

### **Aşama 3: Optimizasyon ve Geliştirme (Süre: 2+ Hafta)**

Bu aşama, uygulamanın performansını, kod kalitesini ve kullanıcı deneyimini iyileştirmeye odaklanır.

1.  **Görev DB-02 & PERF-02:** Tüm veri okuma sorgularını gözden geçirin. Gereken yerlerde `Include()` ve `.AsNoTracking()` metotlarını ekleyerek veritabanı performansını optimize edin.
2.  **Stok Yönetimini Geliştirin:** `StockMovements` tablosunu ve ilgili denetim (audit) mantığını implemente edin.
3.  **Görev UI-01:** MahApps.Metro veya MaterialDesignInXamlToolkit kütüphanesini entegre ederek uygulama genelinde tutarlı ve modern bir görünüm sağlayın.
4.  **Yapay Zeka Entegrasyonu:** Rapor 4'te belirtilen AI senaryolarından birini (örn: Akıllı Kategorizasyon) pilot olarak geliştirin.
5.  **Sürekli Test:** Mevcut ve yeni tüm özellikler için birim ve entegrasyon test kapsamını artırmaya devam edin.
