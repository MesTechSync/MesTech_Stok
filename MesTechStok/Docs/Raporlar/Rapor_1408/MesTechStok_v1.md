--- KURALLAR
1- Bu dosyanın bulunduğu Klasör altına her bölüm adı altında ayrı md dosyası oluştur
2- Bu orjinal rapor dosyasını kesinlik ile değiştirme
3- Projeyi her bir yapı taşına kadar incele
4- Eksikler için ve yapacağın hataların önüne geçmek için bulgularını kontrol et.
5- toplayacağın bilgiler hatalı yanlış ve başkalarının hatalı görüşleri olabilir sen temiz bilgileir ile baştan topla ve doğruluğna emin olarak bu raporları oluştur.

````markdown
# STOK YAZILIMI İNCELEME VE GELİŞTİRME RAPORU

## 1. GİRİŞ

Bu rapor, 700 MB büyüklüğündeki stok yazılımının tüm modüllerini, dosya yapısını, algoritma akışını ve entegrasyon noktalarını eksiksiz analiz etmek amacıyla hazırlanmıştır.  
Hedef, yazılımın **stabil**, **akıcı** ve **eksiksiz** çalışmasını sağlamak; aynı zamanda bakım, geliştirme ve optimizasyon süreçlerini kolaylaştırmaktır.

Rapor, yazılımın tüm kritik bileşenlerini kapsar:

- Dosya ve klasör yapısı
- Modül bazlı fonksiyon analizi
- Algoritma akış şeması
- API entegrasyon noktaları (yapay zekâ dahil)
- Doldurma kılavuzu (örnek veri formatları ile)
- Log toplama ve hata inceleme süreçleri
- Tasarım standartları ve görsel uyum önerileri
- Geliştirme & bakım iş planı

---

## 2. AMAÇ

- Yazılımın mevcut durumunu **tam görünürlük** ile ortaya koymak
- Eksik ve zayıf alanları tespit edip iyileştirme önerileri sunmak
- Tüm modüllerin **senkron** çalışmasını garanti altına almak
- API ve yapay zekâ altyapısının uyumluluğunu sağlamak
- Kullanıcı deneyimini (UX/UI) güçlendirmek
- Stok takip, ekran koruyucu modülü, log yönetimi gibi kritik alanlarda **hata riskini en aza indirmek**

---

## 3. GENEL SİSTEM MİMARİSİ

Sistem 4 ana katmandan oluşmaktadır:

1. **Kullanıcı Arayüzü (Frontend)**  
   - Web tabanlı (React/Vue/Angular)  
   - Mobil uyumlu (Responsive design)  
   - Temel modüller:
     - **Ekran Koruyucu Modülü** (Firma bilgileri, renk & yazı ayarları, ek hizmetler)  
     - **Stok Takip Paneli** (Gerçek zamanlı stok verileri, hızlı ayar değişiklikleri)  
     - **Ayarlar** (Tüm modüllerle senkronize çalışır)  
     - **Log Görüntüleme** (Hataların dışa aktarımı)

2. **İş Mantığı Katmanı (Backend)**  
   - Programlama dili: PHP / Node.js / Python  
   - Görevler:
     - API yönetimi  
     - Stok hareketlerinin işlenmesi  
     - Ekran koruyucu ile stok takibinin veri senkronizasyonu  
     - Log ve hata yakalama sistemi

3. **Veritabanı Katmanı**  
   - MySQL / PostgreSQL  
   - Tablolar:
     - **firmalar**
     - **stoklar**
     - **ayarlar**
     - **loglar**
     - **api_kayitlari**
   - Özellikler:
     - Yüksek hız için indeksleme  
     - Veri bütünlüğü için yabancı anahtarlar

4. **API & Entegrasyon Katmanı**  
   - REST / GraphQL API  
   - Yapay zekâ API’leri:
     - OpenAI API (ChatGPT entegrasyonu)  
     - Google Vision API (ürün görsel analizi)  
     - TensorFlow/ONNX modelleri (otomatik stok tahmini)
   - Harici sistemler:
     - Kargo firmaları API’leri  
     - ERP entegrasyonu  
     - SMS/e-posta servisleri

---

## 4. MODÜL HARİTASI

| Modül Adı | Açıklama | Entegrasyon | Senkronizasyon |
|-----------|----------|-------------|----------------|
| **Ekran Koruyucu** | Firma bilgileri, renk/yazı ayarları, ek hizmetler. | Ayarlar modülü, API katmanı | Stok Takip Paneli ile çift yönlü |
| **Stok Takip** | Ürün stoklarını yönetir, değişiklikleri anında yansıtır. | Veritabanı, Log sistemi | Ekran Koruyucu ile çift yönlü |
| **Ayarlar** | Tüm modüllerin yapılandırma merkezi. | API, Veritabanı | Tüm modüllerle senkron |
| **Log Yönetimi** | Hataları kaydeder ve dışa aktarır. | Backend hata yakalama sistemi | Raporlama aracı ile |
| **API Yönetimi** | Yapay zekâ ve üçüncü parti servislerle haberleşir. | OpenAI, Google Vision vb. | Backend ile senkron |

---

## 5. MODÜL VE BİLEŞEN TANIMLARI

### 5.1. Ekran Koruyucu Modülü
- Firma bilgileri, renk/yazı ayarları, ek hizmetler  
- Ek Hizmetler API üzerinden dinamik yüklenir  
- Ayar değişiklikleri stok takip ve API modülleri ile senkron

### 5.2. Stok Takip Modülü
- Ürün stoklarının yönetimi, kritik stok uyarıları  
- Anlık senkronizasyon ile ekran koruyucu ve API modüllerine yansır

### 5.3. API Yönetim Modülü
- API tanımlama, yetkilendirme ve sürüm yönetimi  
- Yapay zekâ ve üçüncü parti servislerle haberleşir

### 5.4. Log Sistemi
- Minimal ama kritik log alanları tutulur  
- Loglar JSON veya CSV formatında dışa aktarılabilir

### 5.5. Tasarım Ölçütleri
- Renk paleti, font tipleri, responsive ölçüler, grid ve padding/margin standartları belirlenmiş

---

## 6. ALGORİTMA AKIŞI VE VERİ İŞLEME MANTIĞI

```mermaid
flowchart TD
    A[Kullanıcı Girişi] --> B[Ekran Koruyucu Başlat]
    B --> C[Ayarlar ve Firma Bilgileri Yükle]
    C --> D[Stok Takip Modülü]
    D --> E{Stok Seviyesi Değişti mi?}
    E -- Evet --> F[Senkronizasyon Başlat]
    F --> G[API ve Yapay Zeka Servisleri ile Haberleş]
    G --> H[Log Kaydı Oluştur]
    E -- Hayır --> I[Normal Çalışma Devam]
    H --> I
````

* Stok değişiklikleri anlık olarak ekran koruyucu ve API modüllerine yansır
* Yapay zekâ servisleri tahmin ve öneri üretir
* Hatalar loglanır ve dışa aktarılır

---

## 7. HAZIR DOLDURMA KILAVUZU VE ÖRNEK VERİ FORMATLARI

### 7.1. Ekran Koruyucu Modülü

```json
{
  "firma_adi": "HidLight Medya",
  "vergi_no": "1234567890",
  "adres": "Örnek Mah. Örnek Cad. No:10 İstanbul",
  "telefon": "+90 212 123 45 67",
  "email": "destek@hidlight.com",
  "renk_temasi": "#1A73E8",
  "yazi_rengi": "#FFFFFF",
  "ek_hizmetler": [
    {"id": 1, "ad": "Stok Analizi", "durum": "aktif"},
    {"id": 2, "ad": "Tahminleme (AI)", "durum": "pasif"}
  ]
}
```

### 7.2. Stok Takip Modülü

```json
{
  "urun_id": 1001,
  "sku": "ABC-123",
  "ad": "Erkek Spor Ayakkabı",
  "kategori": "Ayakkabı",
  "stok": 45,
  "min_stok": 10,
  "fiyat": 499.90,
  "otomatik_siparis": true
}
```

### 7.3. API Yönetim Modülü

```json
{
  "api_adi": "Ürün Listesi API",
  "yontem": "GET",
  "endpoint": "/api/v1/products",
  "auth": "Bearer Token",
  "rate_limit": "1000 requests/hour"
}
```

### 7.4. Log Sistemi

```json
{
  "timestamp": "2025-08-14T11:35:12Z",
  "level": "ERROR",
  "module": "API Yönetimi",
  "message": "API yanıt süresi aşıldı",
  "endpoint": "/api/v1/products"
}
```

### 7.5. Yapay Zekâ API Entegrasyonu

```json
{
  "api": "OpenAI GPT-5",
  "endpoint": "/v1/predict_stock",
  "method": "POST",
  "payload": {
    "product_id": 1023,
    "historical_sales": [45, 50, 48, 60, 52]
  },
  "response": {
    "predicted_stock": 55,
    "recommendation": "Sipariş verilebilir"
  }
}
```

---

## 8. EKSİK TESPİT, İYİLEŞTİRME VE İŞ PLANI

| Modül          | Eksik / Sorun                 | Öncelik | Geliştirme Adımı                               |
| -------------- | ----------------------------- | ------- | ---------------------------------------------- |
| Ekran Koruyucu | Görsel optimizasyon, UI yavaş | Yüksek  | WebP format, virtual scroll                    |
| Stok Takip     | Kritik stok yavaş yansıma     | Yüksek  | Event-driven senkronizasyon                    |
| API Yönetimi   | Dokümantasyon eksik           | Orta    | Swagger/OpenAPI dokümantasyonu                 |
| Log Sistemi    | Dashboard ve alert eksik      | Orta    | Dashboard ve alert ekleme                      |
| Yapay Zeka     | Entegrasyon eksik             | Yüksek  | AI modüllerinin tüm stok modülüne entegrasyonu |
| Tasarım/UX     | Kontrast, responsive sorun    | Orta    | WCAG testi ve grid standardizasyonu            |

---

## 9. SONUÇ VE YOL HARİTASI

* Yazılımın temel işlevleri çalışıyor ancak bazı modüllerde performans ve senkronizasyon eksikleri mevcut
* Öncelikli geliştirmeler:

  1. Ekran Koruyucu ve Stok Takip Senkronizasyonu
  2. Yapay Zekâ Modülleri Entegrasyonu
  3. API ve Log Sistemi Geliştirmeleri
  4. Tasarım ve UX İ


yileştirmeleri

**Yol Haritası Özet Tablosu**

| Aşama | Süre    | Sorumlu Modül               | Hedef                                                    |
| ----- | ------- | --------------------------- | -------------------------------------------------------- |
| 1     | 1 Hafta | Ekran Koruyucu & Stok Takip | Anlık senkronizasyon ve UI optimizasyon                  |
| 2     | 2 Hafta | Yapay Zeka API              | Tahminleme ve öneri sistemi entegrasyonu                 |
| 3     | 1 Hafta | API Yönetimi & Log          | Dokümantasyon, log dashboard ve alert sistemi            |
| 4     | 1 Hafta | Tasarım & UX                | Renk kontrastı, responsive test ve grid standardizasyonu |
| 5     | 1 Hafta | Tüm Modüller                | Entegre test ve QA süreci, stabil çalışma                |

**Ek Notlar:**

* Versiyon kontrolü (Git) ile tüm değişiklikler takip edilmeli
* Performans izleme ve QA süreci sürekli uygulanmalı
* Yapay zekâ API anahtarları güvenli şekilde saklanmalı ve erişim kontrolleri uygulanmalı

---

## 10. EKLER

* Tüm modüller için doldurma kılavuzları ve örnek JSON verileri mevcut
* Algoritma akışı ve senkronizasyon diyagramları hazır

---

**Rapor Hazırdır ve Tek Dosya Olarak Kullanılabilir.**

```

---

