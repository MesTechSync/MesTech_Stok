# MesTech Log Cleanup Script
# Günlük çalıştırılmalı

Write-Host " MesTech Log Temizlik Başlıyor..."

if (Test-Path "Logs") {
    # 30 günden eski logları sil
    $oldLogs = Get-ChildItem "Logs" -Filter "*.log" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }
    
    if ($oldLogs) {
        Write-Host " $($oldLogs.Count) eski log dosyası siliniyor..."
        $oldLogs | Remove-Item -Force
        Write-Host " Eski loglar temizlendi"
    } else {
        Write-Host "✅ Silinecek eski log yok"
    }
    
    # Log boyut kontrolü
    $totalSize = (Get-ChildItem "Logs" -Filter "*.log" | Measure-Object -Property Length -Sum).Sum
    $totalSizeMB = [math]::Round($totalSize/1MB,2)
    
    Write-Host "📊 Toplam log boyutu: $totalSizeMB MB"
    
    if ($totalSizeMB -gt 100) {
        Write-Host "⚠️ UYARI: Log boyutu hala yüksek!"
    }
} else {
    Write-Host " Log dizini bulunamadı"
}

Write-Host " Log temizlik tamamlandı"
