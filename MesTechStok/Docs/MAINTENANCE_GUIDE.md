#  MesTech Stok Takip Sistemi - Rutin Bakım Klavuzu

**Oluşturulma Tarihi:** 20.08.2025 01:03  
**Sürüm:** 1.0  
**Durum:** Aktif

---

## 📋 İÇİNDEKİLER

1. [Günlük Bakım İşlemleri](#günlük-bakım-işlemleri)
2. [Haftalık Bakım İşlemleri](#haftalık-bakım-işlemleri)
3. [Aylık Bakım İşlemleri](#aylık-bakım-işlemleri)
4. [Kritik Sistem Kontrolleri](#kritik-sistem-kontrolleri)
5. [Performans İzleme](#performans-izleme)
6. [Sorun Giderme](#sorun-giderme)
7. [Otomatik Script'ler](#otomatik-scriptler)

---

##  GÜNLÜK BAKIM İŞLEMLERİ

###  Sabah Kontrolleri (09:00)
- [ ] **Uygulama Durumu Kontrolü**
  `powershell
  Get-Process "*MesTech*" -ErrorAction SilentlyContinue
  `
- [ ] **Log Dosyası Boyut Kontrolü**
  `powershell
  Scripts\daily-log-cleanup.ps1
  `
- [ ] **Self-Test Çalıştırma**
  `powershell
  $env:MESTECH_SELFTEST = "1"
  # Uygulamayı başlat
  Remove-Item env:MESTECH_SELFTEST
  `

###  Akşam Kontrolleri (17:00)
- [ ] **Günlük Log Analizi**
- [ ] **Sistem Kaynak Kullanımı**
- [ ] **Backup Kontrolü**

---

##  HAFTALIK BAKIM İŞLEMLERİ

### 🔍 Pazartesi - Sistem Analizi
- [ ] **Build Quality Check**
  `powershell
  Scripts\weekly-build-check.ps1
  `
- [ ] **Warning Analizi ve Raporlama**
- [ ] **Performance Metrics Toplama**

###  Çarşamba - Temizlik İşlemleri
- [ ] **Eski Log Dosyalarını Temizle**
- [ ] **Geçici Dosyaları Temizle**
- [ ] **Unused Code Detection**

###  Cuma - İyileştirme Planlaması
- [ ] **Kod Kalitesi Metrikleri**
- [ ] **İyileştirme Önerileri**
- [ ] **Sonraki Hafta Planı**

---

## 🗓️ AYLIK BAKIM İŞLEMLERİ

### 🔴 Kritik Priorite
1. **Build Warning Temizliği**
   - Hedef: <200 warning
   - Süre: 2-3 gün
   - Sorumlu: Senior Developer

2. **Security Audit**
   - Hardcoded values kontrolü
   - Dependency security check
   - Code vulnerability scan

3. **Database Maintenance**
   - Index optimization
   - Statistics update
   - Cleanup operations

###  Orta Priorite
1. **Refactoring**
   - Büyük dosyaları böl (>50KB)
   - Code duplication temizliği
   - Architecture review

2. **NuGet Package Updates**
   - Security updates (Kritik)
   - Minor version updates
   - Breaking change analysis

###  Düşük Priorite
1. **Documentation Update**
2. **UI/UX İyileştirmeleri**
3. **Performance Optimizasyonları**

---

##  KRİTİK SİSTEM KONTROLLERİ

###  Günlük Kontroller
`powershell
# Sistem sağlık skoru kontrolü
function Test-SystemHealth {
    $score = 0
    
    # Uygulama çalışıyor mu? (+20)
    if (Get-Process "*MesTech*" -ErrorAction SilentlyContinue) { $score += 20 }
    
    # Build başarılı mı? (+20)
    if (-not ((dotnet build --verbosity quiet 2>&1) -match "FAILED")) { $score += 20 }
    
    # Log sistemi çalışıyor mu? (+20)
    if ((Test-Path "Logs") -and (Get-ChildItem "Logs" -Filter "*.log").Count -gt 0) { $score += 20 }
    
    # Self-test geçiyor mu? (+20)
    # Self-test proof dosyası var mı?
    if (Get-ChildItem "." -Filter "selftest-proof-*.txt") { $score += 20 }
    
    # EXE güncel mi? (+20)
    $exePath = "src\MesTechStok.Desktop\bin\Debug\net9.0-windows\win-x64\MesTechStok.Desktop.exe"
    if (Test-Path $exePath) {
        $exeAge = (Get-Date) - (Get-Item $exePath).LastWriteTime
        if ($exeAge.TotalDays -lt 1) { $score += 20 }
    }
    
    return $score
}

$health = Test-SystemHealth
Write-Host " Sistem Sağlık Skoru: $health/100"

if ($health -ge 80) { Write-Host " SİSTEM SAĞLIKLI" -ForegroundColor Green }
elseif ($health -ge 60) { Write-Host " İYİLEŞTİRME GEREKLİ" -ForegroundColor Yellow }
else { Write-Host " KRİTİK DURUM!" -ForegroundColor Red }
`

###  KPI Metrikleri
- **Build Success Rate:** >95%
- **Warning Count:** <200
- **System Uptime:** >99%
- **Self-Test Pass Rate:** 100%
- **Log Error Rate:** <1%

---

##  PERFORMANS İZLEME

###  Bellek İzleme
`powershell
$process = Get-Process "*MesTech*" -ErrorAction SilentlyContinue
if ($process) {
    $memoryMB = [math]::Round($process.WorkingSet64/1MB,2)
    Write-Host " Bellek Kullanımı: $memoryMB MB"
    
    if ($memoryMB -gt 500) {
        Write-Host " UYARI: Yüksek bellek kullanımı!" -ForegroundColor Yellow
    }
}
`

###  Disk İzleme
`powershell
# Log dizini boyut kontrolü
if (Test-Path "Logs") {
    $logSizeMB = [math]::Round((Get-ChildItem "Logs" -Recurse | Measure-Object -Property Length -Sum).Sum/1MB,2)
    Write-Host " Log Boyutu: $logSizeMB MB"
    
    if ($logSizeMB -gt 100) {
        Write-Host " KRİTİK: Log boyutu çok yüksek!" -ForegroundColor Red
    }
}
`

---

##  SORUN GİDERME

###  Yaygın Problemler ve Çözümleri

#### Problem: Uygulama Başlamıyor
`powershell
# 1. Process kontrolü
Get-Process "*MesTech*" | Stop-Process -Force

# 2. EXE kontrolü
$exePath = "src\MesTechStok.Desktop\bin\Debug\net9.0-windows\win-x64\MesTechStok.Desktop.exe"
Test-Path $exePath

# 3. Build yeniden
dotnet clean
dotnet build

# 4. Yeniden başlat
Start-Process -FilePath $exePath
`

#### Problem: Yüksek Warning Sayısı
`powershell
# 1. Warning türlerini analiz et
dotnet build --verbosity normal 2>&1 | Select-String "warning"

# 2. En yaygın warning'leri tespit et
# 3. Öncelik sırasına göre düzelt
`

#### Problem: Log Dosyaları Çok Büyük
`powershell
# Otomatik temizlik
Scripts\daily-log-cleanup.ps1
`

---

##  OTOMATİK SCRİPT'LER

###  Kullanım Kılavuzu

#### Günlük Otomatik Çalıştırma
`powershell
# Windows Task Scheduler ile
schtasks /create /tn "MesTech Daily Cleanup" /tr "powershell.exe -File Scripts\daily-log-cleanup.ps1" /sc daily /st 08:00
`

#### Haftalık Otomatik Çalıştırma
`powershell
# Windows Task Scheduler ile
schtasks /create /tn "MesTech Weekly Check" /tr "powershell.exe -File Scripts\weekly-build-check.ps1" /sc weekly /d MON /st 09:00
`

---

##  DESTEK VE ESKALASYON

###  Normal Durum (0-2 Problem)
- **Aksiyon:** Rutin bakım devam
- **Raporlama:** Haftalık

###  Dikkat Gerektiren (3-5 Problem)
- **Aksiyon:** Günlük takip
- **Raporlama:** Günlük
- **Sorumlu:** Team Lead

###  Kritik Durum (6+ Problem)
- **Aksiyon:** Acil müdahale
- **Raporlama:** Anlık
- **Sorumlu:** Senior Developer + Team Lead
- **Eskalasyon:** Manager

---

##  BAKIM TAKVİMİ

| Gün | Saat | İşlem | Süre |
|-----|------|-------|------|
| Her Gün | 09:00 | Sistem Durumu Kontrolü | 5 dk |
| Her Gün | 17:00 | Log Analizi | 10 dk |
| Pazartesi | 10:00 | Build Quality Check | 30 dk |
| Çarşamba | 14:00 | Temizlik İşlemleri | 45 dk |
| Cuma | 16:00 | İyileştirme Planı | 30 dk |
| Ay Sonu | - | Kapsamlı İnceleme | 4 saat |

---

##  SÜREKLI İYİLEŞTİRME

###  Metrik Takibi
- Build warning trend
- System performance
- Error rate statistics
- Maintenance time spent

###  Hedefler
- **Q1 2025:** Build warnings <100
- **Q2 2025:** Full automation
- **Q3 2025:** Predictive maintenance
- **Q4 2025:** Zero-downtime updates

---

##  CHANGELOG

| Tarih | Sürüm | Değişiklik |
|-------|-------|------------|
| 20.08.2025 | 1.0 | İlk sürüm oluşturuldu |

---

**Bu dokümantasyon düzenli olarak güncellenmeli ve takım tarafından gözden geçirilmelidir.**
