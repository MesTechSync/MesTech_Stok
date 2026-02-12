# Rapor 5: Doldurma Kılavuzu ve Veri Formatları

**Rapor Tarihi:** 14 Ağustos 2025
**Referans Doküman:** `MesTechStok_v1.md` (Bölüm 7)

---

## 1. Amaç

Bu rapor, MesTech Stok yazılımının veritabanı ve API'leri tarafından kullanılan temel veri yapılarının formatlarını ve örneklerini sunar. Bu, hem veritabanı bütünlüğünü sağlamak hem de API entegrasyonlarını kolaylaştırmak için bir referans niteliğindedir.

---

## 2. Veritabanı Varlıkları (Entities) ve Örnek Veriler

Bu yapılar, `MesTechStok.Core\Entities` klasöründe tanımlanacak olan C# sınıflarına karşılık gelir.

### 2.1. `Product` (Ürün) Tablosu

```csharp
public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } // Örn: "MST-001"
    public string Name { get; set; } // Örn: "4K Web Kamerası"
    public string Description { get; set; }
    public decimal Price { get; set; } // Örn: 1499.50
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; }
    public byte[] RowVersion { get; set; } // Concurrency için
}
```

### 2.2. `Stock` (Stok) Tablosu

Stok miktarını doğrudan `Product` tablosunda tutmak yerine, ayrı bir tabloda yönetmek daha esnektir.

```csharp
public class Stock
{
    public int Id { get; set; }
    public int ProductId { get; set; } // Foreign Key to Product
    public virtual Product Product { get; set; }
    public int Quantity { get; set; } // Mevcut miktar
    public int MinStockLevel { get; set; } // Kritik stok seviyesi
    public DateTime LastUpdated { get; set; }
}
```

### 2.3. `StockMovement` (Stok Hareketi) Tablosu

Her stok giriş ve çıkışını kaydeden, denetim (audit) amaçlı en önemli tablolardan biridir.

```csharp
public class StockMovement
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public int QuantityChanged { get; set; } // Giriş için pozitif, çıkış için negatif
    public int NewQuantity { get; set; } // Hareket sonrası yeni miktar
    public string MovementType { get; set; } // "Purchase", "Sale", "Adjustment", "Return"
    public DateTime Timestamp { get; set; }
    public int? UserId { get; set; } // İşlemi yapan kullanıcı
    public string Notes { get; set; } // "Fatura No: 12345"
}
```

---

## 3. API Veri Formatları (JSON)

### 3.1. Harici Sistemden Ürün Çekme (GET /api/products)

```json
{
  "pagination": {
    "page": 1,
    "pageSize": 100,
    "totalItems": 1520
  },
  "data": [
    {
      "externalId": "prod_abc123",
      "sku": "MST-001",
      "name": "Kablosuz Klavye",
      "price": 799.99,
      "stock": 42,
      "lastModifiedUtc": "2025-08-14T12:00:00Z"
    }
  ]
}
```

### 3.2. Harici Sisteme Stok Güncellemesi Gönderme (PUT /api/stock)

```json
{
  "updates": [
    {
      "sku": "MST-001",
      "newQuantity": 41,
      "reason": "Online Sale"
    },
    {
      "sku": "MST-002",
      "newQuantity": 25,
      "reason": "Manual Adjustment"
    }
  ],
  "idempotencyKey": "e8a3b4c1-dd2a-4f8b-9f8c-3a4b5c6d7e8f"
}
```
- **`idempotencyKey`**: Bu anahtar, ağ sorunları nedeniyle aynı isteğin tekrar gönderilmesi durumunda, sunucunun bu işlemi yalnızca bir kez yapmasını garanti altına alır. Her bir "batch" güncelleme için benzersiz bir GUID olmalıdır.

### 3.3. Log Sistemi Çıktısı (JSON Format)

`MesTechStok_v1.md`'de belirtildiği gibi, loglar yapısal ve dışa aktarılabilir olmalıdır.

```json
{
  "Timestamp": "2025-08-14T14:20:15.123Z",
  "Level": "Error",
  "MessageTemplate": "Failed to synchronize stock for SKU {Sku}",
  "Properties": {
    "Sku": "MST-001",
    "ApiEndpoint": "https://api.pazaryeri.com/v2/stock",
    "UserId": 101
  },
  "Exception": "System.Net.Http.HttpRequestException: Response status code does not indicate success: 503 (Service Unavailable)."
}
```
Bu yapı, logların daha sonra Log Analiz araçları (ELK Stack, Seq, Datadog) tarafından kolayca filtrelenmesini, aranmasını ve üzerinde uyarılar (alert) oluşturulmasını sağlar.
