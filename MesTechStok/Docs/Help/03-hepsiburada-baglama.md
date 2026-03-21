# Hepsiburada Magaza Baglama Rehberi

## Onkosul
- Hepsiburada satici hesabiniz olmali
- Hepsiburada Merchant API erisimi aktif olmali

## Adim 1: API Bilgilerini Alin
1. [Hepsiburada Merchant Center](https://merchant.hepsiburada.com) adresine gidin
2. Ayarlar > API Entegrasyon menusune gidin
3. Asagidaki bilgileri kopyalayin:
   - **Merchant ID**
   - **Username**
   - **Password**

## Adim 2: MesTech'te Magaza Ekleyin
1. Magazalar > Yeni Magaza Ekle
2. Platform: "Hepsiburada" secin
3. Magaza adini girin
4. API bilgilerini yapiştirin

## Adim 3: Baglanti Testi ve Senkronizasyon
- "Baglantiyi Test Et" → yesil onay
- "Urunleri Cek" → urunler aktarilir

## Desteklenen Islemler
- Urun CRUD (listeleme/guncelleme)
- Siparis cekme ve durum guncelleme
- Stok senkronizasyonu
- Listing ekleme (HB listing ID ile)
- Kargo entegrasyonu (HepsiJet dahil)
