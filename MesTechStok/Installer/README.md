# 📦 MesTech Stok Takip Sistemi v2.0 - Kurulum Paketi

## 🎯 **Genel Bakış**
**MesTech Stok Takip Sistemi v2.0** - Self-contained .NET 9 WPF Desktop uygulaması.  
✅ Windows 10/11 x64 uyumlu  
✅ .NET Runtime kurulumu gerektirmez  
✅ Bağımsız çalışır (Self-Contained Deployment)

---

## 📋 **Sistem Gereksinimleri**
- **İşletim Sistemi:** Windows 10 (1903+) veya Windows 11
- **Mimari:** x64 (64-bit)  
- **RAM:** Minimum 2 GB, Önerilen 4 GB
- **Disk Alanı:** ~500 MB (kurulum sonrası)
- **Ek Gereksinim:** Yönetici yetkileri (kurulum için)

---

## 🚀 **Kurulum Seçenekleri**

### **1️⃣ Hızlı Kurulum (Batch)**
```bash
MesTech_Stok_Kurulum.bat
```
- Çift tıklayın ve yönergeleri izleyin
- Otomatik dizin oluşturma
- Desktop ve Start Menu kısayolları

### **2️⃣ Gelişmiş Kurulum (PowerShell)**
```powershell
.\MesTech_Stok_Kurulum.ps1
```
- Detaylı hata kontrolü
- İlerleme göstergeleri  
- Gelişmiş yetki yönetimi

---

## 📂 **Kurulum Sonrası Yapı**
```
C:\Program Files\MesTech\StokTakip\
├── MesTechStok.Desktop.exe          # Ana uygulama
├── MesTechStok.Core.dll              # İş mantığı katmanı
├── appsettings.json                  # Yapılandırma
├── *.dll                             # Tüm bağımlılıklar
└── runtime/                          # .NET 9 Runtime (dahili)
```

---

## 🎮 **İlk Kullanım**

### **Uygulama Başlatma**
- Desktop kısayolu: `MesTech Stok Takip v2.0`
- Start Menu: `Programs > MesTech > MesTech Stok Takip v2.0`
- Manuel: `C:\Program Files\MesTech\StokTakip\MesTechStok.Desktop.exe`

### **Temel Yapılandırma**
1. **Veritabanı:** İlk açılışta SQLite otomatik oluşturulur
2. **Bağlantılar:** `appsettings.json` dosyasından düzenlenebilir
3. **Loglar:** `%APPDATA%\MesTech\Logs\` dizininde tutulur

---

## 🔧 **Özellikler**

### **📊 Stok Yönetimi**
- ✅ Ürün ekleme/düzenleme/silme
- ✅ Stok seviyeleri takibi
- ✅ Kritik stok uyarıları
- ✅ Stok hareketleri geçmişi

### **📱 Barkod Entegrasyonu**
- ✅ USB/HID barkod okuyucu desteği
- ✅ Kamera ile QR Code okuma
- ✅ Otomatik ürün tanıma
- ✅ Bulk barkod işlemleri

### **📈 Raporlama**
- ✅ Excel export (ClosedXML)
- ✅ PDF raporları (iTextSharp)
- ✅ Gelişmiş filtreleme
- ✅ Zamanlı raporlar

### **🌐 OpenCart Entegrasyonu**
- ✅ Ürün senkronizasyonu
- ✅ Stok güncelleme
- ✅ Sipariş takibi
- ✅ Otomatik fiyat güncellemeleri

### **🗃️ Veritabanı Desteği**
- ✅ SQLite (varsayılan)
- ✅ SQL Server
- ✅ PostgreSQL
- ✅ MySQL (MariaDB)

---

## 🛠️ **Sorun Giderme**

### **❌ Uygulama Açılmıyor**
```powershell
# Event Viewer kontrolü
Get-EventLog -LogName Application -Source "MesTechStok*" -Newest 10
```

### **❌ Veritabanı Hatası**
1. `%APPDATA%\MesTech\Logs\` dizinindeki log dosyalarını kontrol edin
2. `appsettings.json` bağlantı stringlerini doğrulayın
3. Veritabanı sunucusu erişilebilir durumda mı kontrol edin

### **❌ Barkod Okuyucu Tanınmıyor**
1. USB bağlantısını kontrol edin
2. Device Manager'da "Human Interface Devices" altında görünüyor mu?
3. Başka bir USB portunu deneyin

### **❌ Excel Export Çalışmıyor**
```powershell
# Microsoft Visual C++ Redistributable gerekli
# https://aka.ms/vs/17/release/vc_redist.x64.exe
```

---

## 📞 **Destek & İletişim**

### **📧 Teknik Destek**
- **Email:** support@mestech.com.tr
- **Telefon:** +90 XXX XXX XX XX
- **Çalışma Saatleri:** 09:00 - 18:00 (Pazartesi-Cuma)

### **🌐 Kaynak Linkler**
- **Resmi Site:** https://www.mestech.com.tr
- **Dokümantasyon:** https://docs.mestech.com.tr/stok-takip
- **Video Eğitimler:** https://www.youtube.com/@MesTechTutorials

---

## 📝 **Sürüm Notları - v2.0**

### **🆕 Yeni Özellikler**
- ✅ .NET 9 Self-Contained deployment
- ✅ Modern WPF arayüzü (MahApps.Metro.IconPacks)
- ✅ Gelişmiş barkod desteği (ZXing.Net + OpenCV)
- ✅ PostgreSQL desteği
- ✅ OpenCart REST API entegrasyonu
- ✅ Çok dilli destek altyapısı
- ✅ Gelişmiş loglama (Serilog)

### **🔄 İyileştirmeler**
- ⚡ %40 daha hızlı başlangıç süresi
- 💾 Optimized database queries
- 🎨 Responsive arayüz tasarımı
- 🔒 Gelişmiş güvenlik (BCrypt.Net)
- 📊 Daha detaylı raporlama

### **🐛 Düzeltilen Hatalar**
- Excel export memory leak sorunu
- Barkod okuyucu disconnection problemi
- Unicode karakter desteği
- High DPI display uyumluluk

---

## 🏆 **Lisans & Telif Hakları**
```
Copyright (c) 2024 MesTech Software Solutions
Tüm hakları saklıdır.

Bu yazılım MesTech Software Solutions tarafından geliştirilmiştir.
Yetkisiz kopyalama, dağıtım ve kullanım yasaktır.
```

---

**Son Güncellenme:** 17 Ağustos 2025  
**Versiyon:** 2.0.0  
**Build:** Release/win-x64/self-contained
