# Barkodlu Stok Takip Sistemi - Geliştirme Kılavuzu

**Durum:** GÜNLÜK GELİŞTİRME REHBERİ ✅  
**Cross-Reference:** [MASTER_DOKUMANTASYON_YAPISI.md](./MASTER_DOKUMANTASYON_YAPISI.md) | [YAZILIM_GELISTIRME_ONCELIKLERI.md](./YAZILIM_GELISTIRME_ONCELIKLERI.md)

## 1. Giriş ve Felsefe

Bu doküman, "Windows Odaklı Üst Düzey Barkodlu Stok Takip Sistemi" projesinin geliştirme sürecine rehberlik etmek amacıyla hazırlanmıştır. Temel felsefemiz, büyük ve karmaşık dosyalar yerine, her birinin net ve tek bir sorumluluğu olan küçük, anlaşılır ve modüler dosyalar oluşturmaktır.

Bu yaklaşım sayesinde:
- **Yapay Zeka (AI) ile Uyum:** Yapay zeka geliştirme asistanları, küçük ve odaklanmış kod parçalarını daha kolay anlayabilir, analiz edebilir ve geliştirebilir.
- **Bakım ve Genişletilebilirlik:** Kod tabanının bakımı kolaylaşır ve yeni özellikler eklemek basitleşir.
- **Takım Çalışması:** Geliştiriciler, projenin farklı modülleri üzerinde aynı anda daha rahat çalışabilir.

Mimari, OpenCart gibi başarılı sistemlerin modüler yapısından ve modern .NET masaüstü uygulamaları için standart olan **MVVM (Model-View-ViewModel)** tasarım deseninden ilham almaktadır.

---

## 2. Önerilen Proje Yapısı

Proje, Visual Studio'da bir "Solution" altında birden fazla "Project" içerecek şekilde yapılandırılacaktır. Bu, sorumlulukların net bir şekilde ayrılmasını sağlar.

```
MesTechStok/
├── .gitignore
├── README.md
├── MesTechStok.sln
└── src/
    ├── MesTechStok.Desktop/ (Ana Masaüstü Uygulaması - WPF)
    │   ├── App.xaml
    │   ├── App.xaml.cs
    │   ├── MainWindow.xaml
    │   ├── MainWindow.xaml.cs
    │   ├── Assets/
    │   │   ├── Fonts/
    │   │   └── Images/ (Uygulama ikonları, arka plan resimleri vs.)
    │   ├── Components/ (Tekrar Kullanılabilir UI Bileşenleri)
    │   │   ├── BlurryClockView.xaml
    │   │   ├── WeatherWidgetView.xaml
    │   │   └── LiveBackgroundView.xaml
    │   ├── Views/ (Ana Sayfalar/Ekranlar)
    │   │   ├── DashboardView.xaml
    │   │   ├── InventoryView.xaml
    │   │   ├── ProductsView.xaml
    │   │   └── OrdersView.xaml
    │   ├── ViewModels/ (View'ların Arkasındaki Mantık)
    │   │   ├── ViewModelBase.cs
    │   │   ├── MainViewModel.cs
    │   │   ├── DashboardViewModel.cs
    │   │   ├── InventoryViewModel.cs
    │   │   ├── ProductsViewModel.cs
    │   │   └── OrdersViewModel.cs
    │   └── Styles/
    │       └── ModernTheme.xaml (Tüm uygulama stilleri)
    │
    └── MesTechStok.Core/ (Çekirdek Mantık - Class Library)
        ├── Config/
        │   └── AppSettings.cs (Uygulama ayarları)
        ├── Data/ (Veritabanı İşlemleri)
        │   ├── Models/ (Entity Framework Modelleri)
        │   │   ├── Product.cs
        │   │   ├── StockMovement.cs
        │   │   └── Order.cs
        │   └── AppDbContext.cs (Veritabanı bağlantısı)
        ├── Services/ (İş Mantığı Servisleri)
        │   ├── Abstract/ (Interface'ler)
        │   │   ├── IInventoryService.cs
        │   │   ├── IProductService.cs
        │   │   └── IOrderService.cs
        │   └── Concrete/ (Interface'leri uygulayan sınıflar)
        │       ├── InventoryService.cs
        │       ├── ProductService.cs
        │       └── OrderService.cs
        ├── Integrations/ (Dış Sistem Entegrasyonları)
        │   ├── Barcode/
        │   │   ├── IBarcodeScannerService.cs
        │   │   └── BarcodeScannerService.cs
        │   └── OpenCart/
        │       ├── IOpenCartClient.cs
        │       ├── OpenCartClient.cs (API isteklerini yönetir)
        │       ├── Dtos/ (API için Veri Transfer Nesneleri)
        │       │   ├── OpenCartProduct.cs
        │       │   └── OpenCartOrder.cs
        │       └── OpenCartSyncService.cs (Senkronizasyon mantığını yönetir)
        └── Utils/
            └── Logger.cs (Loglama işlemleri için)
```

---

## 3. Modül ve Dosya Sorumlulukları

### `MesTechStok.Desktop` (Arayüz Katmanı)
Bu proje, kullanıcının gördüğü ve etkileşime girdiği her şeyi içerir. Kesinlikle iş mantığı (business logic) veya veri erişim kodu içermelidir.

- **Views:** Sadece XAML kodundan oluşan, uygulamanın pencereleri ve sayfalarıdır. C# kodu (code-behind) minimumda tutulmalıdır.
- **ViewModels:** Her View'ın bir ViewModel'i vardır. View'daki buton tıklamaları, veri girişleri gibi olayları yönetir ve `MesTechStok.Core` katmanındaki servisleri çağırır. Arayüzün tüm mantığı buradadır.
- **Components:** Canlı arka plan, flu saat gibi uygulamanın farklı yerlerinde kullanılabilecek küçük ve bağımsız UI parçalarıdır.
- **Styles:** Uygulamanın renkleri, yazı tipleri, buton stilleri gibi tüm görsel teması burada tanımlanır.

### `MesTechStok.Core` (Çekirdek Mantık Katmanı)
Bu proje, uygulamanın beynidir. Arayüzden tamamen bağımsızdır.

- **Data/Models:** Veritabanındaki tabloları temsil eden sınıflardır (Örn: `Product`, `Order`).
- **Services:** Uygulamanın ana iş mantığını içerir. Örneğin, `InventoryService` envanter ekleme, çıkarma, sorgulama gibi işlemleri yapar. ViewModel'ler bu servisleri kullanır.
- **Integrations/OpenCart:** OpenCart ile olan tüm iletişimi bu klasör yönetir.
    - `OpenCartClient`: OpenCart API'sine istekleri gönderen ve yanıtları alan sınıftır.
    - `OpenCartSyncService`: Hangi verilerin ne zaman senkronize edileceği mantığını yönetir. Örneğin, "her 5 dakikada bir stokları senkronize et" gibi.
- **Integrations/Barcode:** Barkod okuyucudan gelen veriyi dinleyen ve işleyen servisleri içerir.
- **Config:** Uygulama ayarlarını (veritabanı bağlantı dizesi, OpenCart API bilgileri vb.) tutar.

---

## 4. Yapay Zeka ile Geliştirme Akışı

Bu yapı ile yapay zekadan en iyi şekilde faydalanmak için aşağıdaki adımları izleyin:

1.  **Odaklanmış İstekler Yapın:** "Bana bir stok takip uygulaması yap" gibi genel bir istek yerine, çok daha spesifik olun.
2.  **Dosya Bazında İlerleyin:**
    - **Örnek İstek 1:** "Aşağıdaki `Product.cs` modelini `MesTechStok.Core/Data/Models/` klasörüne oluştur. Modelde `Id`, `Name`, `Sku`, `Barcode`, `StockQuantity` ve `Price` alanları bulunsun."
    - **Örnek İstek 2:** "`MesTechStok.Core/Services/Abstract/IProductService.cs` arayüzünü oluştur. İçinde `GetProductByBarcode(string barcode)` ve `UpdateStock(int productId, int newQuantity)` metodları olsun."
    - **Örnek İstek 3:** "Şimdi `IProductService` arayüzünü `MesTechStok.Core/Services/Concrete/ProductService.cs` sınıfında implemente et (uygula)."
3.  **Bağlam Sağlayın:** Bir dosya üzerinde çalışmasını isterken, ilişkili diğer dosyaları (örneğin interface'i veya modeli) ve bu kılavuz dosyasını referans olarak gösterin. Bu, yapay zekanın projenin genel yapısını anlamasına yardımcı olur.

Bu yöntemle, yapay zeka büyük bir projeyi bir kerede oluşturmaya çalışmak yerine, bir yapbozun parçalarını tek tek tamamlar gibi çalışır. Bu, daha kaliteli ve hatasız kod üretmesini sağlar.

---

## 5. Geleceğe Yönelik Notlar (macOS ve Çapraz Platform)

Bu mimari, gelecekte uygulamayı diğer platformlara taşımayı kolaylaştırır:

- **`MesTechStok.Core` Projesi:** Bu proje .NET Standard veya .NET 6/7/8 üzerine kurulu olduğu için hiçbir değişiklik yapılmadan macOS, Linux, iOS veya Android'de çalışabilir. Tüm iş mantığımız zaten bu katmandadır.
- **Yeni Arayüz Projeleri:** Gelecekte bir macOS sürümü yapmak istediğimizde, tek yapmamız gereken `MesTechStok.Desktop` projesi gibi yeni bir arayüz projesi (`MesTechStok.macOS` gibi) oluşturmak ve bu yeni projenin `MesTechStok.Core`'u referans almasını sağlamaktır.

İş mantığını arayüzden bu kadar net ayırmak, bize muazzam bir esneklik kazandırır. 