# P0 BLOCKER: XAdES Dijital İmza

## Durum

GİB'e gönderilen e-faturalar mali mühür veya nitelikli e-imza ile
imzalanmalıdır (UBL-TR → XAdES-BES format). Bu imza olmadan GİB
faturayı reddeder.

## Mevcut Durum

| Provider | XAdES Durumu | Açıklama |
|----------|-------------|----------|
| **Sovos (Foriba)** | Provider hallediyor | Sovos kendi sunucusunda imzalıyor → MesTech tarafında imza GEREKMİYOR |
| **Paraşüt** | Provider hallediyor | Paraşüt kendi imzalıyor → MesTech tarafında imza GEREKMİYOR |
| **E-Logo** | Provider halleder | E-Logo kendi imzalar → stub, entegrasyon yapılınca sorun yok |
| **BirFatura** | Provider halleder | BirFatura kendi imzalar → stub |
| **Dijital Planet** | Provider halleder | Dijital Planet kendi imzalar → stub |
| **GİB Portal** | ⚠️ XAdES GEREKLİ | Doğrudan GİB'e gönderimlerde imza GEREKİYOR → ŞU AN YOK |
| **HB Fatura** | HB halleder | Hepsiburada kendi altyapısında imzalar → stub |
| **Trendyol e-Faturam** | TR halleder | Trendyol kendi altyapısında imzalar → stub |
| **MockInvoiceProvider** | — | Test provider, imza gerektirmez |

## Teknik Detay

### XAdES-BES Nedir?
- XML Advanced Electronic Signatures (XAdES) — Baseline Electronic Signature
- e-Fatura UBL-TR XML dokümanına gömülü dijital imza
- Nitelikli elektronik sertifika (NES) veya mali mühür gerektirir
- X.509 sertifika + RSA-SHA256/512 imza algoritması

### Gereksinim
```
XAdES-BES imza oluşturmak için:
1. Nitelikli Elektronik Sertifika (NES) — TÜBİTAK BİLGEM, E-Güven vb.
2. Mali Mühür Cihazı (HSM) veya yazılım tabanlı imzalama
3. .NET XML dijital imza kütüphanesi (System.Security.Cryptography.Xml)
4. XAdES profil desteği (XAdES-BES minimum)
```

### Grep Sonucu
```
grep -rn "XAdES\|XmlDsig\|SignedXml\|DigitalSignature\|X509\|MaliMuhur" src/ → 0 sonuç
```

Projede hiçbir dijital imza kodu bulunmamaktadır.

## Karar

**Sovos veya Paraşüt üzerinden gönderim yapıldığında XAdES sorunu YOK.**

Bu iki provider, imzalama işlemini kendi altyapılarında gerçekleştirir.
MesTech yalnızca UBL-TR XML üretir ve provider'a gönderir — imzalama
provider sorumluluğundadır.

**GİB Portal doğrudan kullanılacaksa XAdES modülü ayrı bir emirnamede
implement edilmeli.**

## Aksiyon

1. ✅ GİB Portal provider'ı "mock" olarak kalacak
2. ✅ Kullanıcıya "GİB Portal doğrudan kullanım için XAdES modülü gerekiyor" uyarısı gösterilecek
3. ✅ Provider ayarları ekranında P0 uyarı banner'ı gösterilecek
4. ⏳ XAdES modülü ayrı emirname ile implement edilecek (HSM entegrasyonu gerekli)

## Risk Değerlendirmesi

| Senaryo | Risk | Etki |
|---------|------|------|
| Sovos/Paraşüt kullanımı | ✅ Düşük | Provider imzalıyor, sorun yok |
| GİB Portal doğrudan | ❌ Yüksek | XAdES olmadan ret alınır |
| Stub provider'lar | ⚠️ Orta | Entegrasyon yapılınca provider halleder |

## Sonuç

**Üretim ortamı için Sovos veya Paraşüt yeterlidir.** XAdES implementasyonu
yalnızca GİB Portal doğrudan kullanımı senaryosunda gereklidir ve ayrı bir
emirname konusudur.

---
*EMR-08 — 19 Mart 2026*
