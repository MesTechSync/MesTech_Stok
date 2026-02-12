# Rapor 4: API Entegrasyon Mimarisi (Yapay Zeka Dahil)

**Rapor Tarihi:** 14 Ağustos 2025
**Referans Doküman:** `MesTechStok_v1.md` (Bölüm 3, 4, 7.5)

---

## 1. Amaç

Bu rapor, MesTech Stok yazılımının harici ve yapay zeka (AI) tabanlı API'lerle nasıl entegre olacağını, bu entegrasyonun mimarisini, kullanılacak teknolojileri ve en iyi uygulamaları detaylandırmaktadır.

---

## 2. API Entegrasyon Altyapısı

Tüm API entegrasyonları, `.NET`'in modern ve performanslı `IHttpClientFactory` altyapısı üzerine kurulacaktır. Bu yaklaşım, aşağıdaki avantajları sağlar:

-   **Bağlantı Yönetimi:** `HttpClient` örneklerini verimli bir şekilde yöneterek "socket exhaustion" (soket tükenmesi) sorununu önler.
-   **Merkezi Yapılandırma:** Tüm API istemcileri (client) için temel adres, zaman aşımları (timeout) ve başlık (header) gibi ayarlar merkezi olarak `App.xaml.cs` içinde yapılandırılabilir.
-   **Resilience (Dayanıklılık):** Polly gibi kütüphanelerle kolayca entegre olarak yeniden deneme (retry) ve devre kesici (circuit breaker) gibi dayanıklılık desenlerinin uygulanmasını sağlar.

### Örnek Yapılandırma (`App.xaml.cs`):

```csharp
// DI Konteynerine IHttpClientFactory eklenir
builder.Services.AddHttpClient();

// Pazaryeri API'si için isimlendirilmiş bir istemci yapılandırması
builder.Services.AddHttpClient("MarketplaceAPI", client =>
{
    client.BaseAddress = new Uri("https://api.pazaryeri.com/v2/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddTransientHttpErrorPolicy(policyBuilder =>
    policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)))
);
```

---

## 3. Yapay Zeka (AI) API Entegrasyon Senaryoları

`MesTechStok_v1.md`'de belirtilen AI yetenekleri, aşağıdaki somut senaryolarla hayata geçirilebilir.

### 3.1. Akıllı Ürün Kategorizasyonu

-   **Amaç:** Yeni bir ürün eklendiğinde, ürünün adı ve açıklamasına göre en uygun kategorinin otomatik olarak önerilmesi.
-   **API:** **Azure OpenAI Service** (GPT-4o veya benzeri bir model).
-   **Akış:**
    1.  Kullanıcı, ürün adı ve açıklamasını girer.
    2.  `IProductService`, bu metinleri Azure OpenAI API'sine bir "prompt" (istek metni) ile gönderir.
    3.  **Prompt Örneği:** `"Aşağıdaki ürün açıklamasını analiz et ve şu kategorilerden en uygun olanını tek kelime olarak döndür: [Elektronik, Giyim, Ev Eşyası, Kitap]. Ürün Adı: Kablosuz Ergonomik Klavye. Açıklama: 2.4Ghz bağlantıya sahip, sessiz tuşlu, şarj edilebilir klavye."`
    4.  API'den dönen kategori (örn: "Elektronik"), arayüzde kullanıcıya öneri olarak sunulur.

### 3.2. Otomatik Stok Tahminlemesi

-   **Amaç:** Bir ürünün geçmiş satış verilerine bakarak gelecekteki stok ihtiyacını tahmin etmek.
-   **API:** **Azure Machine Learning** veya özel bir **TensorFlow/ONNX** modeli.
-   **Akış:**
    1.  Arka planda çalışan bir servis, belirli periyotlarla (örn: haftalık) her ürünün geçmiş satış verilerini (`StockMovements` tablosundan) toplar.
    2.  Bu zaman serisi verisi, tahminleme modelini barındıran bir API endpoint'ine gönderilir.
    3.  Model, gelecek dönem için bir satış tahmini ve önerilen minimum stok seviyesini döndürür.
    4.  Bu tahmin, ürün detay ekranında kullanıcıya "AI Önerisi" olarak gösterilir.

### 3.3. Fatura ve İrsaliyeden Otomatik Veri Çıkarma

-   **Amaç:** Taranmış veya PDF formatındaki bir faturadan ürün bilgilerini (SKU, miktar, fiyat) otomatik olarak okuyup stok giriş formunu doldurmak.
-   **API:** **Azure AI Document Intelligence**.
    -   **Akış:**
    1.  Kullanıcı, fatura dosyasını (PDF, JPG, PNG) sisteme yükler.
    2.  Dosya, Azure AI Document Intelligence API'sine gönderilir.
    3.  API, belgeyi analiz eder ve içindeki tablo yapısını, metinleri tanıyarak yapılandırılmış bir JSON formatında geri döndürür.
    4.  Dönen JSON verisi, sistem tarafından ayrıştırılarak stok girişi yapılacak ürünler listesine otomatik olarak eklenir. Kullanıcı sadece kontrol edip onaylar.

---

## 4. Güvenlik ve Yönetim

-   **API Anahtarları:** Tüm API anahtarları, Windows DPAPI veya Azure Key Vault kullanılarak **şifrelenmiş** bir şekilde saklanmalıdır. Asla kaynak kodunda veya düz metin dosyalarında tutulmamalıdır.
-   **Token Yönetimi:** `TokenRotationService`, OAuth2 gibi token tabanlı kimlik doğrulama kullanan API'ler için erişim token'larını süresi dolmadan önce arka planda yenilemekle sorumlu olacaktır. Bu, işlemlerin kesintiye uğramasını engeller.
