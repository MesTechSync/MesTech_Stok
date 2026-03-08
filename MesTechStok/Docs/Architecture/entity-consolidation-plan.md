# Entity Consolidation Plan

**Tarih**: 2026-03-08
**Gorev**: 1.D2-04
**Yazar**: DEV 1 (Backend & Domain Architect)
**Durum**: Plan — kod degisikligi yok

---

## 1. Mevcut Durum

6 temel entity, Domain ve Core/Data katmanlarinda tekrarli tanimlanmis:

| Entity | Domain (DDD) | Core/Models (MVVM) | Core/Data (EF) | Application DTO | Toplam |
|--------|-------------|--------------------|-----------------|--------------------|--------|
| Product | 140 satir, MT | 158 satir, INPC | 327 satir, EF | 35 satir (ProductDto) | **4 kopya** |
| Category | 30 satir, MT | — | 103 satir, EF | 34 satir (platform DTO) | **3 kopya** |
| Customer | 62 satir, MT | — | 204 satir, EF | — | **2 kopya** |
| Order | 68 satir, MT | — | 120 satir, EF | — | **2 kopya** |
| User | 27 satir | — | 46 satir, EF | — | **2 kopya** |
| Supplier | 46 satir, MT | — | 143 satir, EF | — | **2 kopya** |

**Toplam**: ~1,543 satir entity kodu, onemli kismi tekrar.

MT = Multi-Tenant (ITenantEntity), INPC = INotifyPropertyChanged, EF = Entity Framework Core

## 2. Kritik Sorunlar

### 2.1 ID Tip Uyumsuzlugu
- Domain katmani: `Guid` (BaseEntity uzerinden)
- Core/Data Supplier: `int`
- Core/Data User: `int`
- Core/Data Category.ParentCategoryId: `int?` vs Domain'deki `Guid?`
- **Risk**: FK mapping hatalari, katmanlar arasi donusum kayiplari

### 2.2 Multi-Tenant Asimetrisi
- Domain: 5/6 entity `ITenantEntity` implement ediyor (User haric)
- Core/Data: Hicbir entity multi-tenant degil
- **Risk**: Dalga 2 1.D2-05 (Multi-Tenant) icin blocker

### 2.3 Property Fragmantasyonu
- Core/Data Product: OpenCart entegrasyonu, GS1 standartlari (327 satir)
- Domain Product: Domain logic (AdjustStock, IsLowStock, domain events)
- Core/Models Product: MVVM binding (INotifyPropertyChanged, AIInsights)
- **Sonuc**: Hicbir entity tek basina "tam" degil

### 2.4 Timestamp Patlamasi
Order icin ornek:
- Domain: 2 alan (OrderDate, RequiredDate)
- Core/Data: 6 alan (OrderDate, RequiredDate, CreatedAt, UpdatedAt, ModifiedDate, LastModifiedAt)
- Gercek fark: sadece audit alanlari, ama 4 farkli isimlendirme var

## 3. Hedef Mimari

```
Domain Layer (tek yetkili kaynak)
  └── Product : AggregateRoot, ITenantEntity
        ├── Domain logic (AdjustStock, IsLowStock, domain events)
        ├── Tum property'ler (Core/Data'daki dahil)
        └── Multi-tenant, RowVersion

Infrastructure/Persistence Layer (EF mapping)
  └── ProductConfiguration : IEntityTypeConfiguration<Product>
        ├── Domain entity'yi dogrudan map eder
        ├── OpenCart shadow properties (veya Owned Entity)
        └── Computed column / value converter'lar

Application Layer
  └── ProductDto (API response / query sonucu)
        └── Mapster ile Domain → DTO donusumu

Desktop/Presentation Layer
  └── ProductViewModel : ObservableObject
        ├── Domain Product'i wrap eder
        └── INotifyPropertyChanged CommunityToolkit.Mvvm ile
```

## 4. Konsolidasyon Stratejisi

### Faz A: Domain Entity'leri Zenginlestir (Dalga 3)

Oncelik sirasi: Product > Category > Customer > Order > Supplier > User

Her entity icin:
1. Core/Data'daki tum property'leri Domain entity'ye tasi
2. OpenCart/platform-spesifik alanlari `Owned Entity` veya `Value Object` olarak modelleyerek Domain'i kirletme
3. Audit alanlari (`CreatedDate`, `ModifiedDate`, `CreatedBy`, `ModifiedBy`) icin `IAuditableEntity` interface'i ekle
4. ID tipi `Guid` olarak standardize et (migration gerektirir)

### Faz B: EF Configuration'a Gec (Dalga 3)

1. `Infrastructure/Persistence/Configurations/` altinda her entity icin IEntityTypeConfiguration yaz
2. Core/Data/Models/ entity'lerini sil, yerine Domain entity'lerini dogrudan kullan
3. Core/Data/AppDbContext DbSet<> referanslarini Domain namespace'ine cevir
4. Shadow property'ler ile platform-spesifik DB alanlari koru

### Faz C: MVVM Model'i Doniistiir (Dalga 3)

1. Core/Models/Product.cs → Desktop/ViewModels/ProductViewModel.cs
2. Domain Product'i wrap eden ObservableObject (CommunityToolkit.Mvvm)
3. INotifyPropertyChanged artik ViewModel'de, entity'de degil

### Faz D: Mapping Katmani (Dalga 3)

1. Mapster profilleri: Domain ↔ DTO, Domain ↔ ViewModel
2. Application/Mapping/ dizininde tanimla
3. AutoMapper yerine Mapster (zaten csproj'da var, daha performansli)

## 5. Risk ve Bagimlilk Matrisi

| Islem | Etkilenen Katman | Risk | Onkosul |
|-------|-----------------|------|---------|
| Product birlestime | Domain, Core/Data, Desktop | Yuksek — en buyuk entity | Migration plan |
| ID tip degisikligi (int→Guid) | Core/Data, Desktop, Tests | Yuksek — FK degisir | Tam test coverage |
| Core/Data Model silme | Core/Data, Desktop | Orta — DbContext degisir | EF Configuration hazir |
| MVVM model donusumu | Desktop | Dusuk — sadece UI katmani | ViewModel pattern |
| Multi-Tenant ekleme | Tum katmanlar | Yuksek — global query filter | ITenantProvider hazir |

## 6. Dokunulmayacak Entity'ler

Asagidaki entity'ler **sadece** Core/Data'da tanimli, Domain'de karsiligi yok. Bunlar konsolidasyon kapsaminda degil:

- `Warehouse`, `WarehouseZone`, `WarehouseRack`, `WarehouseShelf`, `WarehouseBin`
- `StockMovement`, `InventoryLot`, `InventoryCount`, `InventoryAdjustment`
- `OrderItem`, `Invoice`, `InvoiceItem`
- `UserRole`, `Role`, `AuditLog`, `Setting`, `Notification`
- `ProductLocation`, `ProductBarcode`, `PriceHistory`, `StockAlert`

Bu entity'ler Dalga 3'te Domain'e tasima adayi olabilir, ama su an kapsam disinda.

## 7. Dosya Referanslari

### Domain Entities (kaynak — zenginlestirilecek)
| Dosya | Satir |
|-------|-------|
| `src/MesTech.Domain/Entities/Product.cs` | 140 |
| `src/MesTech.Domain/Entities/Category.cs` | 30 |
| `src/MesTech.Domain/Entities/Customer.cs` | 62 |
| `src/MesTech.Domain/Entities/Order.cs` | 68 |
| `src/MesTech.Domain/Entities/User.cs` | 27 |
| `src/MesTech.Domain/Entities/Supplier.cs` | 46 |

### Core/Data Entities (hedef — silinecek, EF config'e tasinacak)
| Dosya | Satir |
|-------|-------|
| `src/MesTechStok.Core/Data/Models/Product.cs` | 327 |
| `src/MesTechStok.Core/Data/Models/Category.cs` | 103 |
| `src/MesTechStok.Core/Data/Models/Customer.cs` | 204 |
| `src/MesTechStok.Core/Data/Models/Order.cs` | 120 |
| `src/MesTechStok.Core/Data/Models/User.cs` | 46 |
| `src/MesTechStok.Core/Data/Models/Supplier.cs` | 143 |

### Core/Models (hedef — ViewModel'e donusturulecek)
| Dosya | Satir |
|-------|-------|
| `src/MesTechStok.Core/Models/Product.cs` | 158 |

### Application DTOs (korunacak, mapping eklenecek)
| Dosya | Satir |
|-------|-------|
| `src/MesTech.Application/DTOs/ProductDto.cs` | 35 |
| `src/MesTech.Application/DTOs/CategoryDto.cs` | 34 |

## 8. Zaman Cizelgesi

Bu plan **Dalga 3** icin referans dokumanidir. Dalga 2'de kod degisikligi yapilmaz.

- **Dalga 3 Hafta 1**: Product entity birlestime + EF Configuration + Migration
- **Dalga 3 Hafta 2**: Category, Customer, Order birlestime
- **Dalga 3 Hafta 3**: Supplier, User birlestime + MVVM donusumu
- **Dalga 3 Hafta 4**: Mapping katmani + regression testleri
