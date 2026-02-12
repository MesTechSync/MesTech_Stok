# Rapor 7: Tasarım Standartları ve Görsel Uyum

**Rapor Tarihi:** 14 Ağustos 2025
**Referans Doküman:** `MesTechStok_v1.md` (Bölüm 5.5, 8)

---

## 1. Amaç

Bu rapor, MesTech Stok yazılımının kullanıcı arayüzü (UI) ve kullanıcı deneyimi (UX) için uyulması gereken standartları, kuralları ve en iyi uygulamaları tanımlar. Amaç, uygulama genelinde profesyonel, tutarlı, sezgisel ve estetik bir görünüm sağlamaktır.

---

## 2. UI/UX Temel Felsefesi

-   **Netlik (Clarity):** Arayüzdeki her eleman anlaşılır ve amacı belli olmalıdır. Kullanıcı "Bu buton ne işe yarıyor?" diye düşünmemelidir.
-   **Verimlilik (Efficiency):** Kullanıcılar, görevlerini en az tıklama ve en az ekran değiştirme ile tamamlayabilmelidir.
-   **Tutarlılık (Consistency):** Bir butona basıldığında ne olacağı, bir metin kutusunun nasıl davranacağı gibi etkileşimler uygulama genelinde aynı olmalıdır.
-   **Geri Bildirim (Feedback):** Sistem, kullanıcının her eylemine (başarılı, hatalı, beklemede) anında görsel bir yanıt vermelidir.

---

## 3. UI Kütüphanesi ve Stil Rehberi

Projede, standart WPF kontrolleri yerine modern ve zengin özelliklere sahip bir UI kütüphanesi kullanılacaktır. Bu, hem geliştirme sürecini hızlandırır hem de görsel tutarlılığı garanti eder.

-   **Önerilen Kütüphane:** **MahApps.Metro** veya **MaterialDesignInXamlToolkit**.
    -   **Gerekçe:** Hazır stiller, temalar (koyu/açık mod), modern pencere çerçeveleri, ikonlar ve zengin kontroller (tarih seçiciler, dialoglar, ilerleme çubukları) sunarlar.

### 3.1. Renk Paleti

| Kullanım Alanı | Renk Kodu | Açıklama |
| :--- | :--- | :--- |
| **Ana Eylem (Primary)** | `#007ACC` | Kaydet, Ekle, Onayla gibi birincil butonlar. |
| **İkincil Eylem (Secondary)** | `#6c757d` | İptal, Kapat, Geri gibi ikincil butonlar. |
| **Başarı (Success)** | `#28a745` | İşlem başarılı mesajları, pozitif durumlar. |
| **Tehlike (Danger)** | `#dc3545` | Hata mesajları, silme onayı, uyarılar. |
| **Uyarı (Warning)** | `#ffc107` | Dikkat gerektiren durumlar. |
| **Arka Plan (Background)** | `#F5F5F5` | Pencerelerin ve ana alanların arka plan rengi. |
| **Metin (Foreground)** | `#212529` | Standart metin rengi. |

### 3.2. Tipografi

-   **Font Ailesi:** Segoe UI (Windows'un varsayılan, modern ve okunaklı fontu).
-   **Boyutlar:**
    -   **Pencere Başlığı:** 16px, Kalın
    -   **Büyük Başlık (H1):** 24px, Kalın
    -   **Orta Başlık (H2):** 18px, Yarı Kalın
    -   **Normal Metin/Label:** 14px
    -   **Küçük Metin (Hint):** 12px, İtalik

### 3.3. Boşluklar ve Hizalama (Spacing & Alignment)

-   **Temel Birim:** **8px**. Tüm `Margin` ve `Padding` değerleri bu birimin katları olmalıdır (8, 16, 24, 32).
-   **Kural:** Bir formdaki etiket (Label) ile metin kutusu (TextBox) arasında 8px boşluk olmalıdır. İki farklı form grubu arasında ise 24px boşluk olmalıdır. Bu, görsel hiyerarşiyi ve okunabilirliği artırır.
-   **Hizalama:** Tüm elemanlar bir grid yapısına göre hizalanmalıdır. XAML'deki `Grid` kontrolü bu iş için mükemmeldir. Rastgele yerleşimden kaçınılmalıdır.

---

## 4. Kontrol Standartları

-   **Butonlar:**
    -   Birincil eylem butonu (örn: Kaydet) her zaman formun sağ altında ve vurgulu renkte olmalıdır.
    -   İkincil eylem butonu (örn: İptal) birincil butonun solunda ve daha az dikkat çeken bir stilde olmalıdır.
-   **Veri Tabloları (`DataGrid`):**
    -   **Sanalizasyon:** `EnableRowVirtualization="True"` ve `VirtualizationMode="Recycling"` özellikleri binlerce satırda bile yüksek performans için **zorunludur**.
    -   **Okunabilirlik:** Satırlar arasında hafif bir renk farkı (`AlternatingRowBackground`) olmalıdır.
    -   **İşlevsellik:** Kullanıcılar kolon başlıklarına tıklayarak sıralama yapabilmelidir.
-   **Formlar:**
    -   **Zorunlu Alanlar:** Doldurulması zorunlu alanlar, etiketlerinin yanında bir yıldız (`*`) işareti ile belirtilmelidir.
    -   **Doğrulama (Validation):** Hatalı veri girildiğinde (örn: e-posta formatı yanlış), metin kutusunun kenarı kırmızıya dönmeli ve yanında bir hata ikonu ile açıklayıcı bir mesaj (`ToolTip`) gösterilmelidir. Doğrulama, kullanıcı "Kaydet" butonuna basmadan önce, alan odağını kaybettiğinde anında yapılmalıdır.

Bu standartlar, `App.xaml` içindeki merkezi `ResourceDictionary` dosyalarında `Style` olarak tanımlanacak ve tüm uygulama tarafından ortak olarak kullanılacaktır. Bu, projenin bakımını ve gelecekteki tasarım değişikliklerini çok kolaylaştıracaktır.
