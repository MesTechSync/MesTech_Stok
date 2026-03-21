# Siparis Yonetimi Rehberi

## Siparis Akisi
1. Musteri pazaryerinden siparis verir
2. MesTech siparisi otomatik ceker (her 5-15dk)
3. Siparis "Yeni" durumunda goruntulenir
4. Siparis onaylanir → stok duşer
5. Kargo firmasi secilir → gonderi olusturulur
6. Kargo takip no musteriye iletilir (otomatik)

## Siparis Durumlari
| Durum | Aciklama |
|-------|----------|
| Yeni | Siparis geldi, henuz islenmedi |
| Onaylandi | Siparis onaylandi, stok dusuruldu |
| Hazirlaniyor | Paketleme asamasinda |
| Kargoda | Kargo firmasina teslim edildi |
| Teslim Edildi | Musteri teslim aldi |
| Iptal | Siparis iptal edildi |
| Iade | Musteri iade talep etti |

## Toplu Siparis Isleme
1. Siparisler > Filtrele: "Yeni"
2. Toplu sec (checkbox)
3. "Toplu Onayla" veya "Toplu Kargola"
4. Kargo firmasini secin (Yurtici, Aras, Surat, MNG, PTT)

## Kargo Entegrasyonu
- Desteklenen firmalar: Yurtici Kargo, Aras Kargo, Surat Kargo, MNG, PTT, HepsiJet, Sendeo
- Otomatik gonderi olusturma
- Takip numarasi otomatik pazaryerine iletilir

## Fatura
- Siparis onaylandiginda otomatik fatura taslagi olusur
- E-Fatura entegrasyonu: Sovos veya GiB Portal
- Faturalar > Onayla → e-Fatura/e-Arsiv kesilir

## Iade Yonetimi
1. Musteri iade talep eder (pazaryeri uzerinden)
2. MesTech iade talebini otomatik ceker
3. Iade onaylanirsa stok geri eklenir
4. Iade tutari musteriye iade edilir
