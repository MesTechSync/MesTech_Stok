**1) Yönetim Yetki Paneli (Admin Panel)**

**1.1. Rol & Yetki Sistemi**

* Admin
* Süper
  Admin
* Operasyon
* Finans
* Destek
* Yapay
  Zeka Panel Yetkilisi
* Kullanıcı
  (firma / bireysel)

Her rol için **okuma / yazma / silme / onaylama**
yetkileri seçilebilir olacak.

**1.2. Kullanıcı Yönetimi**

* Tüm
  kullanıcıları listeleme
* Kullanıcı
  bilgilerini düzenleme
* Hesap
  onaylama / askıya alma
* Komisyon
  oranı belirleme
* Ödeme
  limitleri belirleme
* Chat
  kullanım paketlerini atama
* Ürün
  yükleme limitlerini belirleme
* Firma
  doğrulama (KYC / KYB) dosyalarını görme-onaylama

---

**2) Excel ile Ürün Yükleme Modülü**

**2.1. Template Yapısı**

Kullanıcı panelinde indirilebilen bir  **Excel şablonu
(.xlsx)** :

**Zorunlu Alanlar:**

* Ürün
  adı
* Kategori
* Alt
  kategori
* Miktar
* Minimum
  sipariş
* Ülke
  / Şehir
* Fiyat
* Para
  birimi
* Açıklama
* Görsel
  URL’si (isteğe bağlı)

**2.2. Yükleme Süreci**

* Kullanıcı
  Excel yükler → sistem doğrular → hatalı satırlar listelenir.
* Doğrulama:
  * Boş
    alan kontrolü
  * Kategori
    uyumluluğu
  * Fiyat
    – sayı kontrolü
  * Görsel
    URL doğrulama
* Onay
  sonrası ürünler otomatik yayına alınır veya "Admin onayı" moduna
  düşer (site ayarına göre).

**2.3. Toplu Güncelleme**

* Mevcut
  ürünleri Excel ile güncelleme
* Stok
  değiştirme
* Fiyat
  toplu güncelleme

---

**3) Ödeme Hakları – Çekim – Komisyon Paneli**

**3.1. Kullanıcı Bazlı Komisyon Sistemi**

* Kullanıcıya
  özel komisyon oranı (% veya sabit fiyat)
* Kategoriye
  göre komisyon oranı
* Ülkeye
  göre komisyon oranı

**3.2. Ödeme / Kazanç Yönetimi**

* Kullanıcının
  bakiyesini görüntüleyebilme
* Kullanıcı
  çekim talebi oluşturabilme
* Talep
  ekranı:
  * **Talep
    ID**
  * Kullanıcı
    adı
  * IBAN
    / banka bilgisi
  * Tutar
  * Durum
    (Beklemede / Onaylandı / Reddedildi)

**3.3. Admin İşlemleri**

* Talebi
  onaylama / reddetme
* Not
  ekleme
* Log
  kayıtları (kim, ne zaman işlem yaptı)

---

**4) Chat Sistemi (Paketli Kullanım + Admin Kontrol)**

**4.1. Chat Paketleri**

Yönetilebilir paketler:

* Ücretsiz
  (günlük/aylık x mesaj)
* Standart
  (sınırsız – aylık abonelik)
* Premium
  (gelişmiş filtre + AI önerileri)
* Ek
  mesaj satın alma sistemi

**4.2. Chat Özellikleri**

* Alıcı
  – satıcı arasında birebir chat
* Ürün
  üzerinden chat başlatma
* Dosya,
  resim gönderme
* Çevrim
  içi durumu
* Okundu
  bilgisi
* Kullanıcı
  engelleme
* Mesaj
  filtreleme (“Yalnızca doğrulanmış firmalar” gibi)

**4.3. Admin Chat Kontrol Paneli**

* Tüm
  chat konuşmalarını canlı izleme
* Konuşma
  arama filtresi (kelime, kullanıcı, tarih)
* Riskli
  kelime uyarı sistemi (“ödemesiz gönder”, “dolandırıcılık” vb.)
* Sohbete
  müdahale edebilme (uyarı göndermek)
* Mesajları
  silme, düzenleme yetkisi

---

**5) Yapay Zeka Destekli Analiz Sistemi (AI Modülü)**

AI, kullanıcıların yüklediği ürünlere göre otomatik öneriler
sunacak.

**5.1. Ürün Satış Tavsiyeleri**

**Örnek: Domates yükleyen bir kullanıcı için:**

* En
  çok domates talep eden ülkeler
* En
  yüksek fiyat veren bölgeler
* Son
  30 gün trend analizi
* Rakip
  ortalama fiyat aralığı
* En
  hızlı dönüş yapılan marketler
* Önerilen
  etiketler (SEO / kategori optimizasyonu)

**5.2. Alıcı Arama Önerileri**

**Örnek: “Kiremit” arayan kullanıcı için:**

* En
  fazla kiremit satışı olan ülkeler
* Yüksek
  dönüş oranı olan tedarikçiler
* Bölgesel
  fiyat analizi
* Hacim
  / üretim güçlü ülkeler
* Lojistik
  uygunluğu (mesafe – navlun mantığı)

**5.3. Yönetici Paneli AI Görünümü**

* Son
  öneriler listesi
* Trend
  ürün raporu
* Günün
  en çok aranan ürünleri
* Ülke
  bazlı fiyat hareket grafikleri
* Riskli
  ticari davranış tespiti (şüpheli müşteri, hatalı fiyat vs.)

**5.4. AI Teknik Gereksinimleri**

* Ürün
  kategorizasyonu için makine öğrenimi
* Fiyat,
  talep, lokasyon analizleri için data pipeline
* Chat
  GPT veya özel model entegrasyonu
* Trend
  tahmini (time-series forecasting)

---

**6) Kullanıcı Paneli Özellikleri**

* Ürün
  yükleme (Excel / manuel)
* Ürün
  düzenleme
* Mesaj
  merkezi (Chat)
* Sipariş/talep
  geçmişi
* Finans
  paneli → bakiye + çekim talebi
* Paket
  satın alma (Chat / Premium özellikler)
* AI
  öneri merkezi (kişiye özel dashboard)

---

**7) Genel Sistem Özellikleri**

**7.1. Loglama**

* Her
  işlem kayıt altına alınır
* Silme,
  düzenleme logu
* Chat
  logları
* Ödeme
  logları

**7.2. Bildirim Sistemi**

* E-posta
  bildirimleri
* Web
  push bildirimleri
* Mobil
  bildirim entegrasyonu

**7.3. Güvenlik Gereksinimleri**

* 2FA
* IP
  takibi
* Şüpheli
  işlem algılayıcı
* Anti-spam
* Dosya
  yükleme antivirüs taraması

---

**8) Dashboard (Admin için)**

* Günlük
  aktif kullanıcı
* Yüklenen
  ürün sayısı
* En
  çok yüklenen ürün kategorileri
* En
  çok aranan ürünler
* En
  çok mesaj atılan kategori
* Son
  30 gün ticaret hacmi
* AI
  trend önerileri

---

**9) Ek Modüller (Opsiyonel)**

* Lojistik
  hesaplama modülü (navlun, uzaklık)
* Fatura
  oluşturma modülü
* API
  entegrasyonu (mobil uygulama için)
