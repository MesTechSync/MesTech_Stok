param(
  [switch]$Release
)

$ErrorActionPreference = 'SilentlyContinue'
$desktopRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $desktopRoot

Write-Host "🔍 MesTechStok.Desktop başlatılıyor..." -ForegroundColor Cyan

# 1) Var olan süreçleri kapat
Get-Process -Name "MesTechStok.Desktop" -ErrorAction SilentlyContinue | ForEach-Object {
  Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
}
Start-Sleep 1

# 2) Derle (Debug varsayılan, -Release ile Release)
# PowerShell 5.1'de ternary operatör yok, if/else kullanıyoruz
$conf = 'Debug'
if ($Release) { $conf = 'Release' }
Write-Host "🔧 Build ($conf)" -ForegroundColor Yellow
& dotnet build -c $conf --verbosity minimal | Out-Null

# 3) EXE yolunu bul
$exePaths = @(
  Join-Path $desktopRoot "bin\$conf\net9.0-windows\MesTechStok.Desktop.exe" ,
  Get-ChildItem -Path (Join-Path $desktopRoot 'bin') -Recurse -Filter 'MesTechStok.Desktop.exe' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if(-not $exePaths){ Write-Host "❌ EXE bulunamadı" -ForegroundColor Red; exit 1 }
$exe = $exePaths
$workDir = Split-Path $exe -Parent

Write-Host ("✅ EXE: {0}" -f $exe) -ForegroundColor Green

# 4) Çalıştır
$proc = Start-Process -FilePath $exe -WorkingDirectory $workDir -PassThru
Write-Host ("🚀 Başlatıldı | PID: {0}" -f $proc.Id) -ForegroundColor Green

# 5) Pencere oluşumunu bekle
for($i=0; $i -lt 12; $i++){
  Start-Sleep 1
  try { $proc.Refresh() } catch {}
  if($proc.MainWindowHandle -ne [IntPtr]::Zero){ break }
}

# 6) Öne getir
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public static class WinApi {
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@ -ErrorAction SilentlyContinue | Out-Null
if($proc.MainWindowHandle -ne [IntPtr]::Zero){
  [WinApi]::ShowWindow($proc.MainWindowHandle, 9) | Out-Null  # SW_RESTORE
  [WinApi]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
  Write-Host "🪟 Pencere öne getirildi" -ForegroundColor Cyan
} else {
  Write-Host "⚠️ Pencere tanımlanamadı (arka planda olabilir)" -ForegroundColor Yellow
}

# 7) Özet
$pathOut = $exe
try { $pathOut = $proc.MainModule.FileName } catch {}
Write-Host ("RUNNING | PID={0} | PATH={1} | TITLE={2}" -f $proc.Id, $pathOut, $proc.MainWindowTitle) -ForegroundColor Cyan
