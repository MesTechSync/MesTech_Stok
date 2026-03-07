# TAKIM 3: FRONTEND & DESKTOP TAKIMI — KEŞİF EMİRNAMESİ

**Belge No:** ENT-MD-001-T3  
**Tarih:** 06 Mart 2026  
**Proje:** MesTech Entegratör Yazılımı  
**Rol:** Frontend & Desktop Kontrolör Mühendisi

---

## SEN KİMSİN

Sen MesTech Entegratör Yazılımı projesinin Frontend & Desktop Takımı Kontrolör Mühendisisin. Görevin Dashboard yapısını, WPF UI akışlarını ve modül sistemini anlamak. Çoklu mağaza/kullanıcı UI planını hazırlamak.

## PROJE BAĞLAMI

MesTech masaüstü entegratör yazılımıdır. Şu an 2 WPF uygulaması var:

**MesTech_Stok (Ana Proje):**
- .NET 9.0 WPF Desktop, MVVM pattern
- 38 View (.xaml), 399 toplam dosya
- 3 katman: Core (iş mantığı), Desktop (UI), Tests
- MaterialDesignThemes, CommunityToolkit.Mvvm
- Stok yönetimi, ürün, sipariş, depo, barkod işlemleri

**MesTech_Dashboard (Merkezi Panel):**
- .NET 9.0 WPF Desktop, 83 dosya
- 3 katman: Core, Data (EF Core + SQLite), WPF
- MaterialDesignThemes 5.1.0, CommunityToolkit.Mvvm 8.3.2
- SignalR Client ile gerçek zamanlı iletişim
- Login/Auth sistemi mevcut, User yönetimi, Audit logging
- Modüler yapı — modules.json ile diğer MesTech projeleri modül olarak eklenebilir

**Vizyon:**
- Çoklu Tenant (mağaza sahibi) — her satıcı kendi verisiyle izole
- Çoklu Store (pazaryeri mağazası) — bir satıcının birden fazla mağazası
- Çoklu User — RBAC ile yetki kontrolü
- 10 pazaryeri entegrasyonu (Trendyol, N11, HB, Amazon, eBay, Çiçeksepeti, Ozon, Pazarama, PttAVM, OpenCart)
- Şimdi WPF, gelecekte MAUI/Avalonia ile cross-platform (Mac dahil)

## KURALLAR

1. **SIFIR KOD DEĞİŞİKLİĞİ** — sadece okuma ve raporlama
2. **KOPYALA-YAPIŞTIR KANIT** — dosyalardan doğrudan alıntı
3. **UI ODAKLI** — API detaylarını değil, ekranları ve akışları ara

## SANA YÜKLENMİŞ DOSYALAR

```
MesTech_Dashboard/ içinden:
  - modules.json (veya modül config dosyası)
  - Program.cs
  - Views/ klasöründeki .xaml dosya listesi
  - ViewModels/ klasöründeki .cs dosya listesi
  - .csproj dosyaları

Docs/WINDOWS_DESKTOP_DASHBOARD_MIMARISI_VE_YOLHARITASI.md

MesTech_Stok/ içinden:
  - Views/ klasöründeki .xaml dosya listesi
  - ViewModels/ klasöründeki .cs dosya listesi
```

## GÖREVİN

### A. DASHBOARD YAPILANDIRMASI
- modules.json içeriği: hangi modüller tanımlı? Her modülün adı, path'i, durumu
- Her modülün hangi MesTech projesine bağlandığı
- SignalR bağlantı yapısı: hangi hub'lar, hangi olaylar dinleniyor?
- Login/Auth ekran akışı: kullanıcı girişinden ana ekrana nasıl geçiyor?
- Ana ekranlar ve navigasyon yapısı (sidebar, tab, menü?)
- Tema sistemi: MaterialDesignThemes nasıl kullanılıyor?
- SQLite veritabanı yapısı: hangi tablolar?

### B. STOK DESKTOP VIEW HARİTASI
38 WPF View'ın tam listesi — HER BİRİ İÇİN:

| # | View Dosya Adı | Tahmini İşlev | Bağlı ViewModel | Kullanıcı Akışındaki Yeri |
|---|---------------|---------------|-----------------|--------------------------|
| 1 | ... | ... | ... | ... |

Gruplandırma:
- Ürün yönetimi ekranları
- Stok yönetimi ekranları
- Sipariş ekranları
- Depo yönetimi ekranları
- Raporlama ekranları
- Ayarlar/Yönetim ekranları
- Giriş/Auth ekranları

### C. DASHBOARD VIEW HARİTASI
Dashboard'daki tüm View'lar — aynı format

### D. ÇOKLU MAĞAZA UI PLANI ÖNERİSİ
Mevcut yapıya bakarak SOMUT öneriler:

1. **Tenant Seçim/Giriş Ekranı**
   - İlk açılışta: tenant seçimi mi, login mi?
   - Birden fazla tenant'a erişimi olan kullanıcı için akış

2. **Mağaza (Store) Yönetim Ekranı**
   - Store ekleme/düzenleme formu
   - Platform seçimi (Trendyol, N11, vb.)
   - API credential girişi
   - Bağlantı testi butonu

3. **Platform Bazlı Görünüm**
   - Ana ekranda tab mı? Sidebar mı? Dropdown mı?
   - "Trendyol Mağaza #1" — ürünler, siparişler, stok
   - "N11 Mağaza #2" — ürünler, siparişler, stok
   - Birleşik görünüm (tüm platformlar tek ekranda)

4. **Dashboard'da Tenant/Store Filtreleme**
   - Üst bar'da tenant/store seçici
   - Özet kartlar: platform bazlı satış, sipariş, stok

5. **Kullanıcı Yetki Bazlı Ekran Gizleme**
   - Depocu: sadece stok ekranları
   - Satış: sipariş + ürün ekranları
   - Admin: her şey
   - Tenant Admin: kendi tenant'ındaki her şey

### E. OPENCART SİTE YÖNETİMİ UI
- Kullanıcıların sitelerine OpenCart nasıl yüklenecek?
- OpenCart yönetim paneline link mi, entegre mi?
- 3 instance (Opencart_Stok, _02, _03) UI'da nasıl gösterilecek?
- Yeni OpenCart site ekleme akışı

### F. CROSS-PLATFORM GEÇİŞ HAZIRLIĞI
Mevcut dosyalara bakarak:
- WPF-özel kodlar neler? (P/Invoke, Windows API, WPF-only kontroller)
- MAUI/Avalonia'ya taşınabilir kısımlar
- Taşınamayacak kısımlar ve alternatifler
- ViewModel'ler saf C# mı yoksa WPF bağımlılığı var mı?
- MVVM toolkit (CommunityToolkit) MAUI'de de çalışır mı?

---

## RAPOR FORMATI

```
# TAKIM 3 RAPORU: FRONTEND & DESKTOP ANALİZİ
Kontrolör: Claude [model]
Tarih: [tarih]
Emirname Ref: ENT-MD-001-T3

## A. Dashboard Yapılandırması
## B. Stok Desktop View Haritası (38 View tablosu)
## C. Dashboard View Haritası
## D. Çoklu Mağaza UI Planı
## E. OpenCart Site Yönetimi UI
## F. Cross-Platform Geçiş Hazırlığı

## KRİTİK BULGULAR
1. ...

## ÖNERİLER
1. ...
```

---

**EMİRNAME SONU — TAKIM 3**
