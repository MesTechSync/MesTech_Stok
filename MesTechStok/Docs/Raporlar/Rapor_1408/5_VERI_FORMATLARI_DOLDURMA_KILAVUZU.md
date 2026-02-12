# 5. VERİ FORMATLARI VE DOLDURMA KILAVUZU - .NET MODELS

**Claude Rapor Tarihi:** 14 Ağustos 2025  
**Kaynak:** MesTechStok Entity Framework Models + JSON Serialization  
**Teknoloji:** .NET 9 Entity Framework Core + Newtonsoft.Json  

---

## 🗂️ GERÇEK VERİ MODELLERİ ANALİZİ

### Entity Framework Core Models (Gerçek Kod)

#### **Product Entity (285 satır - Comprehensive Model)**

```csharp
// Product.cs - Gerçek implementation
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Barcode { get; set; } = string.Empty;

    // GS1 Standards Support - Advanced barcode types
    [MaxLength(14)]
    public string? GTIN { get; set; }

    [MaxLength(20)]
    public string? UPC { get; set; }

    [MaxLength(20)]
    public string? EAN { get; set; }

    // Precision pricing fields
    [Column(TypeName = "decimal(18,2)")]
    public decimal PurchasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ListPrice { get; set; }

    // Stock management
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public int? MaximumStock { get; set; }

    // Category and classification
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? SubCategory { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    // Product lifecycle
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? UpdatedDate { get; set; }
    
    // Advanced fields for enterprise usage
    [MaxLength(1000)]
    public string? UsageInstructions { get; set; }
    
    [MaxLength(200)]
    public string? ImporterInfo { get; set; }
    
    [MaxLength(200)]
    public string? ManufacturerInfo { get; set; }

    // External system integration
    public int? OpenCartId { get; set; }
    public string? ExternalReference { get; set; }
}
```

---

## 📊 ÖRNEK VERİ FORMATLARI (JSON)

### 1. **Product JSON Data Format**

```json
{
  "id": 1001,
  "name": "Samsung Galaxy S24 Ultra",
  "description": "512GB Hafıza, 12GB RAM, Titanium Gray",
  "sku": "SGS24U-512-TG",
  "barcode": "8801643506674",
  "gtin": "08801643506674",
  "upc": "8801643506674",
  "ean": "8801643506674",
  "purchasePrice": 25000.00,
  "salePrice": 32999.00,
  "listPrice": 34999.00,
  "stock": 25,
  "minimumStock": 5,
  "maximumStock": 100,
  "category": "Elektronik",
  "subCategory": "Akıllı Telefon",
  "brand": "Samsung",
  "isActive": true,
  "createdDate": "2025-08-14T10:30:00Z",
  "updatedDate": "2025-08-14T14:22:15Z",
  "usageInstructions": "Şarj cihazı dahil değildir. İlk kullanımdan önce 3 saat şarj edin.",
  "importerInfo": "ABC İthalat Ltd. Şti. - Tel: 0212-555-0123",
  "manufacturerInfo": "Samsung Electronics Co., Ltd. - Güney Kore",
  "openCartId": 2547,
  "externalReference": "TRENDYOL-12345678"
}
```

### 2. **Bulk Product Import Format**

```json
{
  "importMetadata": {
    "batchId": "BATCH-20250814-001",
    "importDate": "2025-08-14T15:30:00Z",
    "source": "Excel Import",
    "totalItems": 150,
    "validItems": 147,
    "errorItems": 3
  },
  "products": [
    {
      "name": "iPhone 15 Pro Max",
      "sku": "IPH15PM-256-NT",
      "barcode": "0194253433361",
      "purchasePrice": 28000.00,
      "salePrice": 35999.00,
      "stock": 15,
      "minimumStock": 3,
      "category": "Elektronik",
      "brand": "Apple"
    },
    {
      "name": "MacBook Air M3",
      "sku": "MBA-M3-512-MG",
      "barcode": "0195949123456",
      "purchasePrice": 32000.00,
      "salePrice": 41999.00,
      "stock": 8,
      "minimumStock": 2,
      "category": "Bilgisayar",
      "brand": "Apple"
    }
  ]
}
```

---

## 🔄 STATİK TAKIP VERİ FORMATLARI

### **StockMovement Entity**

```csharp
// StockMovement.cs - Inventory tracking
public class StockMovement
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product Product { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT

    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? UnitPrice { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; } // Order ID, Return ID, etc.

    public DateTime MovementDate { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
}
```

### **Stock Movement JSON Format**

```json
{
  "id": 5023,
  "productId": 1001,
  "product": {
    "id": 1001,
    "name": "Samsung Galaxy S24 Ultra",
    "sku": "SGS24U-512-TG"
  },
  "movementType": "OUT",
  "quantity": -2,
  "previousStock": 27,
  "newStock": 25,
  "unitPrice": 32999.00,
  "notes": "Satış - Trendyol Sipariş #TY789456123",
  "reference": "ORDER-TY789456123",
  "movementDate": "2025-08-14T16:45:30Z",
  "createdBy": "SYSTEM-AUTO"
}
```

---

## ⚙️ AYARLAR VE KONFİGÜRASYON FORMATLARI

### **Application Settings JSON**

```json
{
  "database": {
    "connectionString": "Server=.\\SQLEXPRESS;Database=MesTechStok;Trusted_Connection=true;TrustServerCertificate=true;",
    "provider": "SqlServer",
    "enableMigrations": true,
    "commandTimeout": 30
  },
  "barcode": {
    "enabled": true,
    "portName": "COM3",
    "baudRate": 9600,
    "autoConnect": true,
    "scanTimeout": 5000,
    "allowDuplicateScans": false
  },
  "openCart": {
    "apiUrl": "https://yourstore.com/index.php?route=api/",
    "restKey": "YOUR_OPENCART_REST_KEY",
    "merchantId": "12345",
    "defaultLanguageId": 1,
    "syncEnabled": true,
    "syncInterval": 300000
  },
  "azureAI": {
    "openAI": {
      "endpoint": "https://your-resource.openai.azure.com/",
      "apiKey": "YOUR_AZURE_OPENAI_KEY",
      "deploymentName": "gpt-4",
      "maxTokens": 100,
      "temperature": 0.3
    },
    "vision": {
      "endpoint": "https://your-region.api.cognitive.microsoft.com/",
      "apiKey": "YOUR_VISION_API_KEY",
      "features": ["Categories", "Description", "Tags"]
    }
  },
  "logging": {
    "level": "Information",
    "retentionDays": 30,
    "maxFileSize": "10MB",
    "enableFileLogging": true,
    "enableConsoleLogging": true
  },
  "ui": {
    "theme": "Modern",
    "language": "tr-TR",
    "enableAnimations": true,
    "autoRefreshInterval": 30000,
    "defaultPageSize": 20
  }
}
```

---

## 📈 RAPORLAMA VE ANALİTİK VERİ FORMATLARI

### **Daily Stock Report JSON**

```json
{
  "reportMetadata": {
    "reportId": "RPT-20250814-DAILY",
    "generatedDate": "2025-08-14T18:00:00Z",
    "reportType": "DailyStockSummary",
    "dateRange": {
      "startDate": "2025-08-14T00:00:00Z",
      "endDate": "2025-08-14T23:59:59Z"
    }
  },
  "summary": {
    "totalProducts": 1247,
    "totalValue": 2547890.50,
    "lowStockItems": 23,
    "outOfStockItems": 3,
    "movementsToday": 156,
    "salesValue": 45230.00
  },
  "lowStockAlert": [
    {
      "productId": 1089,
      "name": "iPhone 15 Pro",
      "sku": "IPH15P-128-BL",
      "currentStock": 2,
      "minimumStock": 5,
      "category": "Elektronik",
      "lastSaleDate": "2025-08-14T14:30:00Z"
    }
  ],
  "topMovingProducts": [
    {
      "productId": 1001,
      "name": "Samsung Galaxy S24 Ultra",
      "totalMovements": 8,
      "netChange": -5,
      "value": 164995.00
    }
  ]
}
```

---

## 🔐 GÜVENLİK VE AUTHENTICATION

### **User Authentication Data**

```csharp
// UserAccount.cs - DPAPI encrypted storage
public class UserAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty; // BCrypt hashed

    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = "User"; // Admin, User, Viewer

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime? LastLoginDate { get; set; }

    // DPAPI encrypted sensitive data
    public string? EncryptedApiKeys { get; set; }
}
```

---

## 🚨 VERİ VALİDASYON KURALLARI

### **Product Validation Rules**

```csharp
// Product validation attributes
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur")
            .MaximumLength(100).WithMessage("Ürün adı maksimum 100 karakter olabilir");

        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU zorunludur")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("SKU sadece büyük harf, rakam ve tire içerebilir");

        RuleFor(x => x.Barcode)
            .NotEmpty().WithMessage("Barkod zorunludur")
            .Must(BeValidBarcode).WithMessage("Geçersiz barkod formatı");

        RuleFor(x => x.SalePrice)
            .GreaterThan(0).WithMessage("Satış fiyatı 0'dan büyük olmalıdır")
            .GreaterThanOrEqualTo(x => x.PurchasePrice).WithMessage("Satış fiyatı alış fiyatından düşük olamaz");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Stok negatif olamaz");

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stok negatif olamaz");
    }

    private bool BeValidBarcode(string barcode)
    {
        // EAN-13, UPC-A validation logic
        return barcode.Length >= 8 && barcode.All(char.IsDigit);
    }
}
```

Bu veri formatları kılavuzu, projenin **gerçek Entity Framework modellerini** ve **JSON serialization** gereksinimlerini kapsamaktadır.
