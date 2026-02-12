# 🏭 MesTech Stok Takip Sistemi - Stok Yerleşim Sistemi Geliştirme Planı

**Tarih:** 4 Temmuz 2025 (Güncelleme: 16 Ağustos 2025)  
**Versiyon:** 1.0  
**Durum:** SPESİFİK ÖZELLIK PLANI ✅  
**Öncelik:** YÜKSEK (Kritik Eksik)  
**Tahmini Süre:** 14-20 Hafta  
**Cross-Reference:** [MASTER_DOKUMANTASYON_YAPISI.md](./MASTER_DOKUMANTASYON_YAPISI.md) | [YAZILIM_GELISTIRME_ONCELIKLERI.md](./YAZILIM_GELISTIRME_ONCELIKLERI.md)

---

## 📊 **MEVCUT DURUM ANALİZİ**

### **🔍 Tespit Edilen Eksiklikler:**

#### **1. ❌ YERLEŞİM SİSTEMİ EKSİKLİKLERİ:**
- **Basit Alanlar:** Sadece `Location`, `Shelf`, `Bin` (çok yetersiz)
- **Koordinat Sistemi:** X, Y, Z koordinatları yok
- **Görsel Harita:** Depo haritası ve ürün konumları yok
- **Hiyerarşik Yapı:** Depo → Bölüm → Raf → Göz → Pozisyon yok
- **QR Kod Entegrasyonu:** Konum bazlı QR kod yok

#### **2. ❌ DEPO ORGANİZASYONU EKSİKLİKLERİ:**
- **Bölüm Yönetimi:** Depo içi bölümler tanımlanmamış
- **Raf Sistemi:** Raf numaralandırma ve organizasyon yok
- **Göz Yönetimi:** Raf gözleri ve pozisyonları yok
- **Zemin Planı:** Depo zemin planı ve ölçeklendirme yok

#### **3. ❌ ÜRÜN KONUM TAKİBİ EKSİKLİKLERİ:**
- **Gerçek Zamanlı Konum:** Ürün nerede bilinmiyor
- **Konum Geçmişi:** Ürünün nereden nereye taşındığı takip edilmiyor
- **Çoklu Konum:** Aynı ürün birden fazla yerde olabilir
- **Konum Optimizasyonu:** En uygun konum önerisi yok

---

## 🏗️ **GELİŞTİRME FAZLARI**

### **📋 FAZ 1: VERİTABANI MODELLERİ (2-3 Hafta)**

#### **1.1 Yeni Model Sınıfları:**

```csharp
// 1. Depo Bölümü (Zone)
public class WarehouseZone
{
    public int Id { get; set; }
    public string Name { get; set; } // "A Bölümü", "B Bölümü"
    public string Code { get; set; } // "A", "B", "C"
    public int WarehouseId { get; set; }
    public virtual Warehouse Warehouse { get; set; }
    
    // Fiziksel Özellikler
    public decimal? Width { get; set; } // m
    public decimal? Length { get; set; } // m
    public decimal? Height { get; set; } // m
    public decimal? Area { get; set; } // m²
    
    // Konum Bilgileri
    public int? FloorNumber { get; set; } // Kat numarası
    public string? BuildingSection { get; set; } // "Doğu", "Batı"
    
    // Özellikler
    public bool HasClimateControl { get; set; }
    public bool HasSecurity { get; set; }
    public string? TemperatureRange { get; set; } // "18-22°C"
    public string? HumidityRange { get; set; } // "40-60%"
    
    // Organizasyon
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation
    public virtual ICollection<WarehouseRack> Racks { get; set; }
}

// 2. Depo Rafı (Rack)
public class WarehouseRack
{
    public int Id { get; set; }
    public string Name { get; set; } // "A-01", "B-02"
    public string Code { get; set; } // "A01", "B02"
    public int ZoneId { get; set; }
    public virtual WarehouseZone Zone { get; set; }
    
    // Fiziksel Özellikler
    public decimal? Width { get; set; } // cm
    public decimal? Depth { get; set; } // cm
    public decimal? Height { get; set; } // cm
    public int ShelfCount { get; set; } // Raf seviyesi sayısı
    public int BinCount { get; set; } // Göz sayısı
    
    // Konum Bilgileri
    public int? RowNumber { get; set; } // Sıra numarası
    public int? ColumnNumber { get; set; } // Sütun numarası
    public string? Orientation { get; set; } // "North", "South", "East", "West"
    
    // Özellikler
    public string? RackType { get; set; } // "Pallet", "Shelf", "Hanging"
    public decimal? MaxWeight { get; set; } // kg
    public bool IsMovable { get; set; }
    
    // Navigation
    public virtual ICollection<WarehouseShelf> Shelves { get; set; }
}

// 3. Raf Seviyesi (Shelf)
public class WarehouseShelf
{
    public int Id { get; set; }
    public string Name { get; set; } // "A-01-01", "B-02-03"
    public string Code { get; set; } // "A0101", "B0203"
    public int RackId { get; set; }
    public virtual WarehouseRack Rack { get; set; }
    
    // Fiziksel Özellikler
    public int LevelNumber { get; set; } // 1, 2, 3 (alttan yukarı)
    public decimal? Height { get; set; } // cm
    public decimal? MaxWeight { get; set; } // kg
    
    // Konum Bilgileri
    public decimal? DistanceFromGround { get; set; } // cm
    public string? Accessibility { get; set; } // "Easy", "Medium", "Hard"
    
    // Navigation
    public virtual ICollection<WarehouseBin> Bins { get; set; }
}

// 4. Raf Gözü (Bin)
public class WarehouseBin
{
    public int Id { get; set; }
    public string Name { get; set; } // "A-01-01-01", "B-02-03-05"
    public string Code { get; set; } // "A010101", "B020305"
    public int ShelfId { get; set; }
    public virtual WarehouseShelf Shelf { get; set; }
    
    // Fiziksel Özellikler
    public int BinNumber { get; set; } // Göz numarası
    public decimal? Width { get; set; } // cm
    public decimal? Depth { get; set; } // cm
    public decimal? Height { get; set; } // cm
    public decimal? Volume { get; set; } // cm³
    
    // Konum Bilgileri
    public int? XPosition { get; set; } // X koordinatı (cm)
    public int? YPosition { get; set; } // Y koordinatı (cm)
    public int? ZPosition { get; set; } // Z koordinatı (cm)
    
    // Özellikler
    public string? BinType { get; set; } // "Small", "Medium", "Large", "Pallet"
    public decimal? MaxWeight { get; set; } // kg
    public bool IsActive { get; set; }
    public bool IsReserved { get; set; }
    
    // Navigation
    public virtual ICollection<ProductLocation> ProductLocations { get; set; }
}

// 5. Ürün Konumu (Product Location)
public class ProductLocation
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public virtual Product Product { get; set; }
    public int BinId { get; set; }
    public virtual WarehouseBin Bin { get; set; }
    
    // Konum Detayları
    public int Quantity { get; set; } // Bu konumdaki miktar
    public string? Position { get; set; } // "Ön", "Arka", "Sol", "Sağ"
    public string? Notes { get; set; } // "Üstte", "Altta", "Ortada"
    
    // Takip Bilgileri
    public DateTime PlacedDate { get; set; }
    public DateTime? LastMovedDate { get; set; }
    public string? PlacedBy { get; set; }
    public string? LastMovedBy { get; set; }
    
    // Özellikler
    public bool IsPrimary { get; set; } // Ana konum mu?
    public bool IsActive { get; set; }
    
    // Navigation
    public virtual ICollection<LocationMovement> Movements { get; set; }
}

// 6. Konum Hareketi (Location Movement)
public class LocationMovement
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public virtual Product Product { get; set; }
    
    // Hareket Detayları
    public int? FromBinId { get; set; }
    public virtual WarehouseBin? FromBin { get; set; }
    public int? ToBinId { get; set; }
    public virtual WarehouseBin? ToBin { get; set; }
    
    public int Quantity { get; set; }
    public string MovementType { get; set; } // "PLACE", "MOVE", "REMOVE", "ADJUST"
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    
    // Takip Bilgileri
    public DateTime MovementDate { get; set; }
    public string? MovedBy { get; set; }
    public string? Reference { get; set; } // Sipariş no, sayım no vb.
}
```

#### **1.2 Mevcut Model Güncellemeleri:**

```csharp
// Product.cs'e eklenecek
public class Product
{
    // ... mevcut alanlar ...
    
    // Yeni Konum Alanları
    public virtual ICollection<ProductLocation> ProductLocations { get; set; }
    
    [NotMapped]
    public string FullLocationPath
    {
        get
        {
            var primaryLocation = ProductLocations?.FirstOrDefault(pl => pl.IsPrimary);
            if (primaryLocation == null) return "Konum Belirtilmemiş";
            
            return $"{primaryLocation.Bin?.Shelf?.Rack?.Zone?.Name} → " +
                   $"{primaryLocation.Bin?.Shelf?.Rack?.Name} → " +
                   $"{primaryLocation.Bin?.Shelf?.Name} → " +
                   $"{primaryLocation.Bin?.Name}";
        }
    }
    
    [NotMapped]
    public string QuickLocationCode
    {
        get
        {
            var primaryLocation = ProductLocations?.FirstOrDefault(pl => pl.IsPrimary);
            return primaryLocation?.Bin?.Code ?? "N/A";
        }
    }
}
```

---

### **🎨 FAZ 2: UI TASARIMI VE KULLANICI DENEYİMİ (3-4 Hafta)**

#### **2.1 Depo Haritası Görünümü:**

##### **Ana Özellikler:**
- **2D/3D Depo Haritası:** Gerçek zamanlı depo görünümü
- **Zoom ve Pan:** Yakınlaştırma ve kaydırma
- **Katman Yönetimi:** Bölüm, raf, göz katmanları
- **Renk Kodlaması:** Stok durumuna göre renkler
- **QR Kod Entegrasyonu:** Her konum için QR kod

##### **Görsel Tasarım:**
```
🎨 Depo Haritası Tasarım Özellikleri:
├── 🗺️ 2D Zemin Planı: Gerçek ölçekli depo haritası
├── 🏗️ 3D Raf Görünümü: Raf yükseklikleri ve gözler
├── 🎨 Renk Kodlaması: Stok durumuna göre renkler
├── 🔍 Zoom Kontrolleri: %25, %50, %100, %200
├── 📱 Responsive Tasarım: Mobil uyumlu
├── 🌙 Tema Desteği: Açık/koyu tema
└── 🖱️ Mouse Kontrolleri: Tıklama, sürükleme, yakınlaştırma
```

##### **Konum Bilgi Kartları:**
```
📋 Konum Bilgi Kartı:
├── 🏷️ Konum Kodu: A-01-01-01
├── 📦 Ürün Sayısı: 15 adet
├── 💰 Toplam Değer: ₺2,450
├── 📊 Doluluk Oranı: %75
├── 🔍 QR Kod: Konum tarama
├── 📍 Koordinatlar: X:120, Y:80, Z:150
└── 📝 Notlar: "Kırılabilir ürünler"
```

#### **2.2 Ürün Yerleştirme Sihirbazı:**

##### **Adım Adım Yerleştirme:**
```
🔮 Ürün Yerleştirme Sihirbazı:
├── 1️⃣ Ürün Seçimi: Barkod/SKU ile ürün bulma
├── 2️⃣ Miktar Girişi: Yerleştirilecek miktar
├── 3️⃣ Konum Seçimi: Haritadan konum seçimi
├── 4️⃣ Optimizasyon: En uygun konum önerisi
├── 5️⃣ Onay: Yerleştirme onayı
└── 6️⃣ Tamamlama: QR kod oluşturma
```

##### **Akıllı Konum Önerisi:**
```
🧠 Akıllı Konum Önerisi Algoritması:
├── 📏 Boyut Uyumluluğu: Ürün boyutu ↔ Göz boyutu
├── 🏷️ Kategori Yakınlığı: Benzer ürünler yakın
├── 📦 Stok Yoğunluğu: Boş alan optimizasyonu
├── 🚚 Erişim Kolaylığı: Sık kullanılan ürünler önde
├── 🌡️ İklim Gereksinimleri: Sıcaklık/humidity uyumu
└── ⚠️ Güvenlik: Tehlikeli ürünler güvenli alanlarda
```

---

### **🔧 FAZ 3: SERVİS KATMANI VE İŞ MANTIĞI (2-3 Hafta)**

#### **3.1 Yerleşim Servisleri:**

```csharp
// ILocationService Interface
public interface ILocationService
{
    // Konum Yönetimi
    Task<WarehouseBin> GetBinByCodeAsync(string binCode);
    Task<ProductLocation> PlaceProductAsync(int productId, int binId, int quantity, string notes);
    Task<ProductLocation> MoveProductAsync(int productId, int fromBinId, int toBinId, int quantity);
    Task<ProductLocation> RemoveProductAsync(int productId, int binId, int quantity);
    
    // Konum Arama
    Task<List<WarehouseBin>> FindAvailableBinsAsync(Product product, int quantity);
    Task<List<WarehouseBin>> FindBinsByProductAsync(int productId);
    Task<List<ProductLocation>> GetProductLocationsAsync(int productId);
    
    // Optimizasyon
    Task<WarehouseBin> GetOptimalBinAsync(Product product, int quantity);
    Task<List<WarehouseBin>> GetNearbyBinsAsync(int binId, int radius);
    
    // Raporlama
    Task<LocationReport> GetLocationReportAsync(int warehouseId);
    Task<BinUtilizationReport> GetBinUtilizationReportAsync(int warehouseId);
}

// LocationService Implementation
public class LocationService : ILocationService
{
    // ... implementasyon detayları ...
    
    public async Task<WarehouseBin> GetOptimalBinAsync(Product product, int quantity)
    {
        // 1. Boyut uyumluluğu kontrolü
        var sizeCompatibleBins = await GetSizeCompatibleBinsAsync(product);
        
        // 2. Kategori yakınlığı hesaplama
        var categoryProximityBins = await GetCategoryProximityBinsAsync(product, sizeCompatibleBins);
        
        // 3. Stok yoğunluğu analizi
        var optimalBins = await AnalyzeStockDensityAsync(categoryProximityBins);
        
        // 4. Erişim kolaylığı değerlendirmesi
        var accessibilityBins = await EvaluateAccessibilityAsync(optimalBins);
        
        // 5. En uygun konumu seç
        return accessibilityBins.OrderBy(b => b.OptimalityScore).First();
    }
}
```

#### **3.2 QR Kod Entegrasyonu:**

```csharp
// IQRCodeService Interface
public interface IQRCodeService
{
    // QR Kod Oluşturma
    Task<byte[]> GenerateLocationQRCodeAsync(string binCode);
    Task<byte[]> GenerateProductQRCodeAsync(int productId);
    Task<byte[]> GenerateMovementQRCodeAsync(int movementId);
    
    // QR Kod Okuma
    Task<LocationInfo> ReadLocationQRCodeAsync(byte[] qrCodeImage);
    Task<ProductInfo> ReadProductQRCodeAsync(byte[] qrCodeImage);
    
    // QR Kod Yönetimi
    Task<string> GetQRCodeContentAsync(string binCode);
    Task<bool> ValidateQRCodeAsync(string qrCodeContent);
}

// QR Kod İçerik Formatı
public class LocationQRCodeContent
{
    public string Type { get; set; } = "LOCATION";
    public string BinCode { get; set; }
    public string ZoneName { get; set; }
    public string RackName { get; set; }
    public string ShelfName { get; set; }
    public string Coordinates { get; set; } // "X:120,Y:80,Z:150"
    public string QRCodeVersion { get; set; } = "1.0";
    public DateTime GeneratedDate { get; set; }
}
```

---

### **📱 FAZ 4: MOBİL UYGULAMA ENTEGRASYONU (3-4 Hafta)**

#### **4.1 Mobil Depo Yönetimi:**

##### **Ana Özellikler:**
- **QR Kod Tarama:** Kamera ile konum tarama
- **Gerçek Zamanlı Güncelleme:** Anlık stok değişiklikleri
- **Offline Çalışma:** İnternet olmadan da çalışma
- **Sesli Komutlar:** "Ürün bul", "Konum göster" gibi

##### **Mobil UI Tasarımı:**
```
📱 Mobil Depo Yönetimi:
├── 🎯 Ana Ekran: Hızlı erişim butonları
├── 🔍 Arama: Ürün, konum, barkod arama
├── 📍 Harita: Basitleştirilmiş depo haritası
├── 📷 Kamera: QR kod ve barkod tarama
├── 📊 Stok: Anlık stok bilgileri
├── 🚀 Hızlı İşlemler: Yerleştirme, taşıma, çıkarma
└── 📋 Geçmiş: Son işlemler ve hareketler
```

---

### **📊 FAZ 5: RAPORLAMA VE ANALİTİK (2-3 Hafta)**

#### **5.1 Konum Raporları:**

```csharp
// LocationReport Model
public class LocationReport
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; }
    
    // Genel İstatistikler
    public int TotalZones { get; set; }
    public int TotalRacks { get; set; }
    public int TotalShelves { get; set; }
    public int TotalBins { get; set; }
    
    // Doluluk Oranları
    public decimal ZoneUtilization { get; set; }
    public decimal RackUtilization { get; set; }
    public decimal ShelfUtilization { get; set; }
    public decimal BinUtilization { get; set; }
    
    // Konum Analizi
    public List<ZoneUtilization> ZoneUtilizations { get; set; }
    public List<RackUtilization> RackUtilizations { get; set; }
    public List<BinUtilization> BinUtilizations { get; set; }
    
    // Optimizasyon Önerileri
    public List<OptimizationSuggestion> Suggestions { get; set; }
}

// Optimizasyon Önerisi
public class OptimizationSuggestion
{
    public string Type { get; set; } // "REORGANIZE", "EXPAND", "CONSOLIDATE"
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal PotentialSavings { get; set; }
    public int EstimatedTime { get; set; } // dakika
    public string Priority { get; set; } // "LOW", "MEDIUM", "HIGH", "CRITICAL"
}
```

---

### **🧪 FAZ 6: TEST VE DOĞRULAMA (2-3 Hafta)**

#### **6.1 Test Senaryoları:**

```
🧪 Test Senaryoları:
├── 📍 Konum Yerleştirme Testi:
│   ├── Ürün yerleştirme
│   ├── Konum değiştirme
│   ├── Ürün çıkarma
│   └── Hata durumları
├── 🔍 Konum Arama Testi:
│   ├── Barkod ile arama
│   ├── QR kod ile arama
│   ├── Koordinat ile arama
│   └── Filtreleme testleri
├── 📊 Raporlama Testi:
│   ├── Doluluk raporları
│   ├── Optimizasyon önerileri
│   ├── Hareket geçmişi
│   └── Export işlemleri
└── 📱 Mobil Entegrasyon Testi:
    ├── QR kod tarama
    ├── Offline çalışma
    ├── Senkronizasyon
    └── Performans testleri
```

---

## 🎯 **SONUÇ VE ÖNERİLER**

### **📋 Tespit Edilen Ana Eksiklikler:**

1. **❌ Konum Sistemi:** Sadece basit alanlar, koordinat yok
2. **❌ Depo Organizasyonu:** Bölüm, raf, göz hiyerarşisi yok
3. **❌ Görsel Harita:** 2D/3D depo görünümü yok
4. **❌ QR Kod Entegrasyonu:** Konum bazlı QR kod yok
5. **❌ Optimizasyon:** Akıllı konum önerisi yok
6. **❌ Mobil Uygulama:** Depo yönetimi mobilde yok

### **🚀 Önerilen Geliştirme Sırası:**

1. **FAZ 1:** Veritabanı modelleri (2-3 hafta)
2. **FAZ 2:** UI tasarımı (3-4 hafta)
3. **FAZ 3:** Servis katmanı (2-3 hafta)
4. **FAZ 4:** Mobil entegrasyon (3-4 hafta)
5. **FAZ 5:** Raporlama (2-3 hafta)
6. **FAZ 6:** Test ve doğrulama (2-3 hafta)

**Toplam Süre:** 14-20 hafta

### **⭐ Kritik Başarı Faktörleri:**

- **Kullanıcı Deneyimi:** Basit ve sezgisel arayüz
- **Performans:** Hızlı konum arama ve güncelleme
- **Güvenilirlik:** Doğru konum bilgisi ve senkronizasyon
- **Ölçeklenebilirlik:** Büyük depolar için optimize edilmiş
- **Entegrasyon:** Mevcut sistemle uyumlu

---

## 🔄 **GELİŞTİRME DURUMU**

- [ ] **FAZ 1:** Veritabanı modelleri
- [ ] **FAZ 2:** UI tasarımı
- [ ] **FAZ 3:** Servis katmanı
- [ ] **FAZ 4:** Mobil entegrasyon
- [ ] **FAZ 5:** Raporlama
- [ ] **FAZ 6:** Test ve doğrulama

---

**📅 Son Güncelleme:** 4 Temmuz 2025  
**👨‍💻 Geliştirici:** MesTech Development Team  
**🎯 Hedef:** Stok yerleşim sistemi tam entegrasyonu
