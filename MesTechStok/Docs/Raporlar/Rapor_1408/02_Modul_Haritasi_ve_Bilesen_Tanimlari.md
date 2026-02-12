# Rapor 2: Modül Haritası ve Bileşen Tanımları

**Rapor Tarihi:** 14 Ağustos 2025
**Referans Doküman:** `MesTechStok_v1.md` (Bölüm 4, 5)

---

## 1. Amaç

Bu rapor, MesTech Stok projesini oluşturan tüm mantıksal ve fiziksel modülleri tanımlar, her birinin sorumluluklarını açıklar ve aralarındaki ilişkiyi gösteren bir harita sunar. Analiz, projenin gerçek dosya yapısı ve kod tabanı üzerinden yapılmıştır.

---

## 2. Proje Modül Haritası

Aşağıdaki tablo, `MesTechStok_v1.md`'de belirtilen kavramsal modüllerin projedeki gerçek karşılıklarını ve mevcut durumlarını özetlemektedir.

| Kavramsal Modül | Fiziksel Konum (Proje/Klasör) | Sorumlu Bileşenler | Durum |
| :--- | :--- | :--- | :--- |
| **Stok Takip** | `Core` & `Desktop` | `IProductService`, `ProductsView`, `ProductsViewModel` | **Kısmen Tamamlandı** |
| **Ayarlar** | `Desktop` | `SettingsView`, `SettingsViewModel` | **Kısmen Tamamlandı (Güvenlik Zafiyeti Var)** |
| **API Yönetimi** | `Core` | `IHttpClientFactory`, `TokenRotationService` (Boş) | **Başlangıç Aşamasında** |
| **Log Yönetimi** | Proje Geneli | `Serilog` (Entegre Edilecek) | **Mevcut Değil** |
| **Ekran Koruyucu** | `Desktop` | Oluşturulacak özel bir servis | **Mevcut Değil** |
| **Veritabanı** | `Core` & SQL Server | `AppDbContext`, `Migrations` | **Temel Seviyede** |

---

## 3. Modül ve Bileşenlerin Detaylı Tanımları

### 3.1. `MesTechStok.Core` (Çekirdek İş Mantığı Modülü)

Uygulamanın platformdan bağımsız beynidir. Tüm temel iş kuralları, veri yapıları ve harici sistemlerle iletişim mantığı burada yer alır.

-   **`\Entities`:**
    -   **Açıklama:** Veritabanı tablolarını temsil eden C# sınıflarıdır (POCOs).
    -   **Örnek:** `Product.cs`.
    -   **Durum:** Temel `Product` varlığı mevcut. `Customer`, `StockMovement`, `Log` gibi kritik varlıklar **eksik**.

-   **`\Services`:**
    -   **Açıklama:** Uygulamanın iş mantığını yürüten servislerin kontratları (Arayüzler - `interface`) ve somut uygulamaları (Sınıflar - `class`).
    -   **Örnek:** `IProductService` (Kontrat), `RealProductService` (Uygulama).
    -   **Durum:** `IProductService` temel seviyede mevcut. `ICustomerService` **tamamen eksik**. `TokenRotationService`, `SyncRetryService`, `OrderStatusStateMachine` gibi önemli servisler **oluşturulmuş ancak içleri boş**.

-   **`\Data`:**
    -   **Açıklama:** Entity Framework Core'un `DbContext` sınıfını içerir. Veritabanı bağlantısını, tablo yapılandırmalarını ve ilişkileri yönetir.
    -   **Örnek:** `AppDbContext.cs`.
    -   **Durum:** Mevcut. Ancak veri bütünlüğü için `CHECK` kısıtlamaları ve performans için `Index` tanımlamaları **eksik**.

### 3.2. `MesTechStok.Desktop` (Sunum Modülü)

Kullanıcının gördüğü ve etkileşimde bulunduğu WPF tabanlı arayüzdür.

-   **`\Views`:**
    -   **Açıklama:** XAML ile tasarlanmış pencereler ve kullanıcı kontrolleri.
    -   **Örnek:** `ProductsView.xaml`, `SettingsView.xaml`.
    -   **Durum:** Temel CRUD işlemleri için View'lar mevcut. Ancak modern bir görünüm için stil ve tema uygulaması **eksik**.

-   **`\ViewModels`:**
    -   **Açıklama:** View'ların arkasındaki mantık. Veri bağlama, komut yönetimi ve servislerle iletişim burada gerçekleşir.
    -   **Örnek:** `ProductsViewModel.cs`.
    -   **Durum:** Mevcut. Ancak `async void` kullanımı gibi desen hataları içeriyor ve bu da UI donmalarına neden oluyor.

-   **`App.xaml.cs`:**
    -   **Açıklama:** Uygulamanın giriş noktası. Dependency Injection (DI) konteyneri burada yapılandırılır ve tüm servisler kaydedilir.
    -   **Durum:** DI altyapısı doğru bir şekilde kurulmuş. Bu, projenin en güçlü yanlarından biridir.

### 3.3. Kavramsal Modüllerin Detayları

-   **Ekran Koruyucu Modülü:**
    -   **Açıklama:** Belirli bir süre işlem yapılmadığında uygulamayı kilitleyecek güvenlik modülü.
    -   **Gerekenler:** Kullanıcı aktivitesini dinleyen bir servis, bir zamanlayıcı (`DispatcherTimer`) ve bir kilit ekranı (`LockScreenView`).
    -   **Durum:** **Henüz tasarlanmadı ve kodlanmadı.**

-   **Log Yönetimi Modülü:**
    -   **Açıklama:** Uygulama içindeki hataları, uyarıları ve önemli olayları yapısal bir formatta dosyaya kaydeden sistem.
    -   **Gerekenler:** `Serilog` kütüphanesinin entegrasyonu ve tüm `try-catch` bloklarında `ILogger` servisinin kullanılması.
    -   **Durum:** **Mevcut değil.** Bu eksiklik, hata takibini imkansız hale getirmektedir.
