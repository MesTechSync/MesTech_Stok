# Rapor 6: Log Toplama ve Hata İnceleme Süreçleri

**Rapor Tarihi:** 14 Ağustos 2025
**Referans Doküman:** `MesTechStok_v1.md` (Bölüm 5.4, 7.4)

---

## 1. Amaç

Bu rapor, MesTech Stok yazılımında hataların nasıl yakalanacağını, kaydedileceğini (loglanacağını) ve analiz edileceğini tanımlayan standart bir süreç oluşturmayı hedefler. Etkili bir loglama stratejisi, sorunları proaktif olarak tespit etmek ve kullanıcıları etkilemeden önce çözmek için kritiktir.

---

## 2. Loglama Stratejisi ve Teknolojisi

-   **Temel Prensip:** Asla bir hatayı sessizce yutma (`catch (Exception) {}`). Yakalanan her istisna (`exception`) mutlaka loglanmalıdır.
-   **Teknoloji:** **Serilog**. .NET ekosistemindeki en güçlü ve esnek yapısal loglama kütüphanesidir.
-   **Yapısal Loglama (Structured Logging):** Loglar, düz metin yerine zengin ve filtrelenebilir **JSON** formatında tutulacaktır. Bu, "ne oldu?" sorusunun yanı sıra "hangi koşullar altında oldu?" sorusuna da cevap verir.

### Örnek Loglama Kullanımı:

```csharp
// Bir servisin constructor'ı
public RealProductService(ILogger<RealProductService> logger)
{
    _logger = logger;
}

// Metot içinde kullanım
public async Task UpdateProductPriceAsync(int productId, decimal newPrice, int userId)
{
    _logger.LogInformation("Attempting to update price for ProductId: {ProductId} by UserId: {UserId}", productId, userId);

    try
    {
        // ... veritabanı işlemi ...
        _logger.LogInformation("Successfully updated price for ProductId: {ProductId}", productId);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Hata loglarken, ilgili tüm bağlamı (context) eklemek çok önemlidir.
        _logger.LogError(ex, "Concurrency conflict while updating ProductId: {ProductId} by UserId: {UserId}", productId, userId);
        throw; // Hatayı tekrar fırlatarak üst katmanın haberdar olması sağlanır.
    }
}
```

---

## 3. Log Seviyeleri ve Anlamları

Doğru log seviyesini kullanmak, logları analiz ederken gürültüyü azaltır.

-   **`Verbose` / `Debug`:** Sadece geliştirme ortamında aktif olmalıdır. Metot giriş/çıkışları, değişken değerleri gibi çok detaylı bilgiler içerir.
-   **`Information`:** Normal ve beklenen uygulama akışını belirtir. Örnek: "Kullanıcı giriş yaptı", "Rapor oluşturuldu", "Senkronizasyon tamamlandı".
-   **`Warning`:** Beklenmedik ancak uygulamanın çalışmasını engellemeyen durumlar. Örnek: "API yanıtı 3 saniyeden uzun sürdü", "Yapılandırma dosyası bulunamadı, varsayılan ayarlar kullanılıyor".
-   **`Error`:** Bir işlemin başarısız olmasına neden olan yakalanmış hatalar. Örnek: "Veritabanına kayıt yapılamadı", "Harici API'den geçersiz yanıt alındı".
-   **`Fatal`:** Uygulamanın tamamen çökmesine neden olan, yakalanamayan kritik hatalar. Örnek: "Veritabanı bağlantısı kurulamıyor", "Gerekli bir DLL dosyası eksik".

---

## 4. Log Depolama ve Analiz (Sink'ler)

Serilog, logları birden fazla hedefe aynı anda gönderebilir (`Sink`).

### 4.1. Yerel Dosya (File Sink)

-   **Amaç:** Her makinede, uygulamanın çalıştığı yerde logları tutmak.
-   **Yapılandırma:**
    -   Loglar `Logs` klasöründe `mestech-YYYYMMDD.json` formatında tutulacaktır.
    -   Her gün yeni bir log dosyası oluşturulacaktır (`rollingInterval: RollingInterval.Day`).
    -   Performansı etkilememek için yazma işlemi asenkron olacaktır.
    -   Son 7 güne ait loglar saklanacak, eskiler otomatik silinecektir.

### 4.2. Merkezi Log Sunucusu (Öneri: Seq)

-   **Amaç:** Tüm kullanıcıların loglarını tek bir merkezi sunucuda toplayarak, geliştiricilerin kullanıcının makinesine erişmeden hataları gerçek zamanlı olarak görmesini, aramasını ve filtrelemesini sağlamak.
-   **Teknoloji:** **Seq**. Serilog ile mükemmel uyumlu, kurulumu ve kullanımı çok kolay bir log sunucusudur.
-   **Faydaları:**
    -   **Gerçek Zamanlı Analiz:** Hatalar oluştuğu anda ekrana düşer.
    -   **Güçlü Sorgulama:** "Son 1 saat içinde 'MST-001' SKU'su için oluşan tüm hataları göster" gibi sorgular yapılabilir.
    -   **Uyarılar (Alerting):** Belirli bir hata (örn: `Fatal` seviyesinde bir log) oluştuğunda ekibe e-posta veya Slack bildirimi gönderebilir.

---

## 5. Hata İnceleme Süreci

1.  **Kullanıcı Bildirimi:** Kullanıcı bir sorun bildirdiğinde, ilk olarak yaklaşık zaman ve yaptığı işlem sorulur.
2.  **Merkezi Log Sunucusu (Seq) Kontrolü:** Geliştirici, Seq arayüzünden ilgili zaman aralığını ve kullanıcı ID'sini filtreleyerek oluşan `Error` veya `Fatal` seviyesindeki logları inceler.
3.  **Bağlam (Context) Analizi:** Yapısal loglama sayesinde, hatanın oluştuğu andaki `ProductId`, `UserId`, `ApiEndpoint` gibi tüm değişkenler görülebilir. Bu, hatayı yeniden oluşturmayı (reproduce) kolaylaştırır.
4.  **Çözüm ve Doğrulama:** Hata giderildikten sonra, aynı filtrelenmiş sorgu izlenerek hatanın tekrar oluşmadığı doğrulanır.
