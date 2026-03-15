# run-both-ui.ps1 — Dalga 10: Run WPF Desktop + Avalonia PoC side by side
# Usage: pwsh Scripts/run-both-ui.ps1
# Both processes run in parallel; close either window to stop that process.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MesTech Stok — Dual UI Launcher" -ForegroundColor Cyan
Write-Host "  WPF Desktop + Avalonia (parallel)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$wpfProject  = Join-Path $root "src/MesTechStok.Desktop/MesTechStok.Desktop.csproj"
$avaloniaProject = Join-Path $root "src/MesTech.Avalonia/MesTech.Avalonia.csproj"

# Verify projects exist
if (-not (Test-Path $wpfProject)) {
    Write-Host "[ERROR] WPF project not found: $wpfProject" -ForegroundColor Red
    exit 1
}
if (-not (Test-Path $avaloniaProject)) {
    Write-Host "[ERROR] Avalonia project not found: $avaloniaProject" -ForegroundColor Red
    exit 1
}

Write-Host "[1/2] Starting WPF Desktop..." -ForegroundColor Yellow
$wpfProc = Start-Process "dotnet" -ArgumentList "run --project `"$wpfProject`"" -PassThru

Write-Host "[2/2] Starting Avalonia PoC..." -ForegroundColor Yellow
$avaloniaProc = Start-Process "dotnet" -ArgumentList "run --project `"$avaloniaProject`"" -PassThru

Write-Host ""
Write-Host "Both UIs launched. PIDs: WPF=$($wpfProc.Id), Avalonia=$($avaloniaProc.Id)" -ForegroundColor Green
Write-Host "Close either application window to stop that process." -ForegroundColor Gray
Write-Host "Press Ctrl+C here to stop both." -ForegroundColor Gray

try {
    # Wait for either process to exit
    while (-not $wpfProc.HasExited -and -not $avaloniaProc.HasExited) {
        Start-Sleep -Milliseconds 500
    }
}
finally {
    # Clean up remaining process
    if (-not $wpfProc.HasExited) {
        Write-Host "Stopping WPF..." -ForegroundColor Yellow
        Stop-Process -Id $wpfProc.Id -Force -ErrorAction SilentlyContinue
    }
    if (-not $avaloniaProc.HasExited) {
        Write-Host "Stopping Avalonia..." -ForegroundColor Yellow
        Stop-Process -Id $avaloniaProc.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Both UIs stopped." -ForegroundColor Cyan
}
