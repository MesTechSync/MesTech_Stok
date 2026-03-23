# ARSIV MANIFEST — V5 Borç Kapatma
Tarih: 2026-03-23

## Core/Services/ (39 dosya)
| Kaynak | Yeni Karşılık | Durum |
|--------|---------------|-------|
| Core/Services/Abstract/*.cs (13) | Application/Features/* CQRS handlers | ARŞİVLENDİ |
| Core/Services/Concrete/*.cs (11) | Application/Features/* CQRS handlers | ARŞİVLENDİ |
| Core/Services/Other (15) | Application/Features/* veya Infrastructure | ARŞİVLENDİ |

## Desktop/Handlers/ (12 dosya)
| Kaynak | Yeni Karşılık | Durum |
|--------|---------------|-------|
| Desktop/Handlers/*.cs (12) | H32'de InfraDbContext'e taşınmış, Application CQRS mevcut | ARŞİVLENDİ |

## Desktop/Services/ (57 dosya)
| Kaynak | Yeni Karşılık | Durum |
|--------|---------------|-------|
| SqlBacked*.cs (4) | Application CQRS handlers (Product/Inventory/Order/Reports) | ARŞİVLENDİ |
| Enhanced*.cs (5) | Application CQRS handlers | ARŞİVLENDİ |
| Mock*.cs (3) | Gerçek implementasyonlar Application+Infrastructure'da | ARŞİVLENDİ |
| Telemetry*.cs (3) | Application/Features/Logging CQRS handlers | ARŞİVLENDİ |
| Other services (42) | Application/Infrastructure equivalents | ARŞİVLENDİ |

## Desktop'ta KALAN (hardware-dependent)
- BarcodeHardwareService.cs
- GlobalBarcodeService.cs
- IBarcodeService.cs

**TOPLAM: 108 dosya arşivlendi, 3 dosya Desktop'ta kaldı (donanım).**
