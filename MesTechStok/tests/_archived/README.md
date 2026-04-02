# Archived Test Projects

## MesTech.Integration.Tests.Legacy

**Orijinal konum:** `tests/MesTech.Integration.Tests/`
**Arşiv tarihi:** 2 Nisan 2026
**Arşivleyen:** DEV4
**Neden:** 226 build error — API signature drift (Dalga 2 → Dalga 15 arası interface değişiklikleri)

### Neden silinmedi?
- 250 dosya, 417 test class
- Aktif proje (`src/MesTech.Tests.Integration/`) ile sadece 3 class name overlap
- 414 benzersiz test class — gelecekte migrate edilebilir

### Aktif test projesi
`src/MesTech.Tests.Integration/` — 155 dosya, CI slnf'de, 0 build error

### Geri yüklemek için
```bash
git mv tests/_archived/MesTech.Integration.Tests.Legacy tests/MesTech.Integration.Tests
# Build hataları fix edilmeli (interface signature uyumu)
```
