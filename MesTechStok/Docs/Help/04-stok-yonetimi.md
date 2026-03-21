# Stok Yonetimi Rehberi

## Genel Bakis
MesTech tum pazaryerlerinizdeki stoklari tek merkezden yonetmenizi saglar.
Bir urununuzu Trendyol'da sattiniz → MesTech otomatik olarak Hepsiburada,
N11 ve diger kanallardaki stogunu da gunceller.

## Urun Ekleme
1. Urunler > Yeni Urun Ekle
2. Zorunlu alanlar:
   - Urun Adi
   - SKU (Stok Kodu) — benzersiz olmali
   - Barkod (EAN-13 veya Code128)
   - Fiyat (KDV dahil)
   - Stok Adedi
   - Kategori

## Stok Hareketi
- **Giris**: Tedarikci'den mal geldi → stok artar
- **Cikis**: Siparis onaylandi → stok azalir (otomatik)
- **Transfer**: Depo A → Depo B arasi transfer
- **Sayim**: Fiziksel sayim sonucu stok duzeltme

## Kritik Stok Uyarisi
- Urunler > Urun Duzenle > Kritik Stok Seviyesi alanini doldurun
- Stok bu seviyeye dusunce otomatik bildirim alirsiniz
- E-posta + panel bildirimi

## Coklu Depo
- Depolar > Yeni Depo Ekle
- Her depoya farkli stok atanabilir
- Siparis geldiginde en yakin depodan karsilanir

## Toplu Islemler
- Urunler > Icer Aktar (Excel/CSV)
- Sablonu indirin → doldurun → yukleyin
- Desteklenen formatlar: .xlsx, .csv, .xml

## Senkronizasyon
- Otomatik: Her 15 dakikada bir
- Manuel: Magazalar > [Magaza] > "Simdi Senkronize Et"
- Stok degisikligi aninda ilgili platformlara iletilir
