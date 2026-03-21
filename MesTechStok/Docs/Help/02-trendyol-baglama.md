# Trendyol Magaza Baglama Rehberi

## Onkoşul
- Trendyol satici hesabiniz olmali
- Trendyol Satici Paneli'ne erisim yetkiniz olmali

## Adim 1: API Bilgilerini Alin
1. [Trendyol Satici Paneli](https://partner.trendyol.com) adresine gidin
2. Entegrasyon > API Bilgileri menusune gidin
3. Asagidaki bilgileri kopyalayin:
   - **Satici ID** (Supplier ID)
   - **API Key**
   - **API Secret**

## Adim 2: MesTech'te Magaza Ekleyin
1. MesTech panelinde Magazalar > Yeni Magaza Ekle
2. Platform olarak "Trendyol" secin
3. Magaza adini girin (ornek: "Trendyol Magazam")
4. API bilgilerini yapiştirin:
   - Satici ID
   - API Key
   - API Secret

## Adim 3: Baglanti Testi
- "Baglantiyi Test Et" butonuna tiklayin
- Basarili ise yesil onay isareti gorursunuz
- Basarisiz ise hata mesajini kontrol edin

## Adim 4: Ilk Senkronizasyon
- "Urunleri Cek" butonuna tiklayin
- Urunleriniz otomatik olarak MesTech'e aktarilir
- Kategori eslemesi yapin (Trendyol kategorisi → MesTech kategorisi)

## Hata Durumlari
| Hata | Cozum |
|------|-------|
| 401 Unauthorized | API Key/Secret yanlis. Trendyol panelinden tekrar alin. |
| 403 Forbidden | Satici ID hatali veya hesap aktif degil. |
| Timeout | Internet baglantinizi kontrol edin. 30sn sonra tekrar deneyin. |

## Desteklenen Islemler
- Urun listeleme ve guncelleme
- Siparis cekme ve durum guncelleme
- Stok senkronizasyonu (otomatik, her 15dk)
- Fiyat guncelleme
- Kargo entegrasyonu
