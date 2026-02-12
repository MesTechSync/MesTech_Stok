# MesTech Stok Takip Sistemi v2.0 - PowerShell Kurulum
# Gelişmiş yetki kontrolü ve hata yönetimi ile

$Host.UI.RawUI.WindowTitle = "MesTech Stok Takip v2.0 - PowerShell Kurulum"
$Host.UI.RawUI.BackgroundColor = "DarkBlue"
$Host.UI.RawUI.ForegroundColor = "White"
Clear-Host

Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "              MesTech STOK TAKiP SiSTEMi v2.0 - KURULUM" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[INFO] Self-Contained .NET 9 Deployment - Runtime Gerektirmez" -ForegroundColor Green
Write-Host "[INFO] Windows 10/11 x64 Uyumlu" -ForegroundColor Green
Write-Host ""

# Yönetici yetkisi kontrolü
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")

if (-not $isAdmin) {
    Write-Host "[UYARI] Bu kurulum yönetici yetkileri gerektirebilir." -ForegroundColor Yellow
    Write-Host "[BILGI] Gerekirse UAC onay penceresi açılacaktır." -ForegroundColor Cyan
}

$InstallDir = "$env:ProgramFiles\MesTech\StokTakip"
$SourceDir = Join-Path $PSScriptRoot "..\src\MesTechStok.Desktop\bin\Release\Publish"

Write-Host "[1/6] Kurulum parametreleri hazırlanıyor..." -ForegroundColor Cyan
Write-Host "      Kaynak: $SourceDir" -ForegroundColor Gray
Write-Host "      Hedef: $InstallDir" -ForegroundColor Gray

# Kaynak dizin kontrolü
if (-not (Test-Path $SourceDir)) {
    Write-Host "[HATA] Kaynak dizin bulunamadı: $SourceDir" -ForegroundColor Red
    Write-Host "[ÇÖZÜM] Önce 'dotnet publish' komutunu çalıştırın." -ForegroundColor Yellow
    Read-Host "Devam etmek için Enter'a basın"
    exit 1
}

Write-Host "[2/6] Hedef dizin oluşturuluyor..." -ForegroundColor Cyan
try {
    if (-not (Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }
    Write-Host "      ✓ Dizin hazırlandı: $InstallDir" -ForegroundColor Green
} catch {
    Write-Host "[HATA] Dizin oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Devam etmek için Enter'a basın"
    exit 1
}

Write-Host "[3/6] Uygulama dosyaları kopyalanıyor..." -ForegroundColor Cyan
try {
    Copy-Item -Path "$SourceDir\*" -Destination $InstallDir -Recurse -Force
    $fileCount = (Get-ChildItem -Path $InstallDir -Recurse).Count
    Write-Host "      ✓ $fileCount dosya başarıyla kopyalandı" -ForegroundColor Green
} catch {
    Write-Host "[HATA] Dosya kopyalama başarısız: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Devam etmek için Enter'a basın"
    exit 1
}

Write-Host "[4/6] Desktop kısayolu oluşturuluyor..." -ForegroundColor Cyan
try {
    $WshShell = New-Object -comObject WScript.Shell
    $Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\MesTech Stok Takip v2.0.lnk")
    $Shortcut.TargetPath = "$InstallDir\MesTechStok.Desktop.exe"
    $Shortcut.WorkingDirectory = $InstallDir
    $Shortcut.IconLocation = "$InstallDir\MesTechStok.Desktop.exe,0"
    $Shortcut.Description = "MesTech Stok Takip Sistemi v2.0"
    $Shortcut.Save()
    Write-Host "      ✓ Desktop kısayolu oluşturuldu" -ForegroundColor Green
} catch {
    Write-Host "[UYARI] Desktop kısayolu oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "[5/6] Başlat menüsü kısayolu oluşturuluyor..." -ForegroundColor Cyan
try {
    $StartMenuDir = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\MesTech"
    if (-not (Test-Path $StartMenuDir)) {
        New-Item -ItemType Directory -Path $StartMenuDir -Force | Out-Null
    }
    
    $Shortcut = $WshShell.CreateShortcut("$StartMenuDir\MesTech Stok Takip v2.0.lnk")
    $Shortcut.TargetPath = "$InstallDir\MesTechStok.Desktop.exe"
    $Shortcut.WorkingDirectory = $InstallDir
    $Shortcut.IconLocation = "$InstallDir\MesTechStok.Desktop.exe,0"
    $Shortcut.Description = "MesTech Stok Takip Sistemi v2.0"
    $Shortcut.Save()
    Write-Host "      ✓ Başlat menüsü kısayolu oluşturuldu" -ForegroundColor Green
} catch {
    Write-Host "[UYARI] Başlat menüsü kısayolu oluşturulamadı: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "[6/6] Kurulum son kontroller..." -ForegroundColor Cyan
if (Test-Path "$InstallDir\MesTechStok.Desktop.exe") {
    Write-Host "      ✓ Ana uygulama dosyası mevcut" -ForegroundColor Green
} else {
    Write-Host "      ✗ Ana uygulama dosyası bulunamadı!" -ForegroundColor Red
}

if (Test-Path "$InstallDir\appsettings.json") {
    Write-Host "      ✓ Yapılandırma dosyası mevcut" -ForegroundColor Green
} else {
    Write-Host "      ✗ Yapılandırma dosyası bulunamadı!" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host "                        KURULUM TAMAMLANDI!" -ForegroundColor Yellow
Write-Host "========================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[BAŞARILI] MesTech Stok Takip Sistemi kuruldu:" -ForegroundColor Green
Write-Host "           $InstallDir" -ForegroundColor Gray
Write-Host ""
Write-Host "[KISAYOLLAR]" -ForegroundColor Cyan
Write-Host "           * Desktop: MesTech Stok Takip v2.0" -ForegroundColor Gray  
Write-Host "           * Start Menu: Programs\MesTech\" -ForegroundColor Gray
Write-Host ""
Write-Host "[NOT] Sistem .NET Runtime gerektirmez (Self-Contained)" -ForegroundColor Green
Write-Host ""

$Launch = Read-Host "Uygulamayı şimdi başlatmak ister misiniz? (E/H)"
if ($Launch -eq "E" -or $Launch -eq "e") {
    Write-Host "[BAŞLATILIYOR] MesTech Stok Takip Sistemi..." -ForegroundColor Green
    Start-Process -FilePath "$InstallDir\MesTechStok.Desktop.exe" -WorkingDirectory $InstallDir
}

Write-Host ""
Write-Host "Kurulum tamamlandı. Bu pencereyi kapatabilirsiniz." -ForegroundColor Green
Read-Host "Çıkış için Enter'a basın"
