# MesTech Build Quality Check Script
# Haftalık çalıştırılmalı

Write-Host " MesTech Build Kalite Kontrolü..."

# Build yap ve warning sayısını kontrol et
$buildOutput = dotnet build --verbosity normal 2>&1
$warningCount = ($buildOutput -split "`n" | Where-Object { $_ -match "warning" }).Count

Write-Host " Warning sayısı: $warningCount"

if ($warningCount -gt 500) {
    Write-Host "🚨 KRİTİK: Warning sayısı çok yüksek!" -ForegroundColor Red
    exit 1
} elseif ($warningCount -gt 300) {
    Write-Host " UYARI: Warning sayısı yüksek" -ForegroundColor Yellow
} else {
    Write-Host "✅ Warning seviyesi kabul edilebilir" -ForegroundColor Green
}

# Self-test çalıştır
Write-Host " Self-test çalıştırılıyor..."
$env:MESTECH_SELFTEST = "1"

try {
    # EXE'yi self-test modunda çalıştır
    $exePath = "src\MesTechStok.Desktop\bin\Debug\net9.0-windows\win-x64\MesTechStok.Desktop.exe"
    if (Test-Path $exePath) {
        Start-Process -FilePath $exePath -Wait -WindowStyle Hidden
        
        # Proof dosyası kontrol et
        $proofFiles = Get-ChildItem "." -Filter "selftest-proof-*.txt" | Sort-Object LastWriteTime -Descending
        if ($proofFiles) {
            Write-Host " Self-test başarılı"
        } else {
            Write-Host "❌ Self-test başarısız"
            exit 1
        }
    }
} finally {
    Remove-Item env:MESTECH_SELFTEST -ErrorAction SilentlyContinue
}

Write-Host " Build kalite kontrolü tamamlandı"
