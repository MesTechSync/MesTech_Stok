# fast-audit.ps1 — MesTech Hızlı Denetim Betiği (PowerShell)
# Bölüm 9.1 — Her commit sonrası <60sn
# v4++ DÜZELTME (14 Mar 2026): Yol çelişkisi giderildi.
# Önceki sorun: Bash "frontend/" arıyordu, PowerShell "MesTech_Trendyol\**" arıyordu.
# Gerçek durum: İKİ ayrı frontend yolu mevcut:
#   1. frontend\panel\pages\          (MDS panel, Bitrix24-inspired, 146+ sayfa)
#   2. MesTech_Trendyol\apps\web-dashboard\src\pages\  (Trendyol-spesifik, 108+ sayfa)
# Bu betik HER İKİSİNİ de tarar.

param([switch]$NoRestore)

Write-Host "=== FAST AUDIT — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" -ForegroundColor Cyan

# Repo kökünü bul
$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot   = git -C $ScriptDir rev-parse --show-toplevel 2>$null
if (-not $RepoRoot) { $RepoRoot = Resolve-Path "$ScriptDir\..\..\..\.." }
$Sln        = Get-ChildItem -Path "$RepoRoot\MesTech_Stok" -Filter "*.sln" -Recurse -Depth 3 | Select-Object -First 1

# ── Build ──────────────────────────────────────────────────────────────────────
$buildArgs = @($Sln.FullName, "--no-restore")
$BuildOut   = dotnet build @buildArgs 2>&1 | Out-String
$Errors     = ([regex]::Matches($BuildOut, "error CS")).Count
$DomainAppWarn = ($BuildOut -split "`n" |
    Where-Object { $_ -match "warning (CS|CA|MA)" -and $_ -notmatch "MesTechStok.Desktop" }).Count
Write-Host "Build: error=$Errors domain_app_warning=$DomainAppWarn"

# ── Test ───────────────────────────────────────────────────────────────────────
$TestOut = dotnet test $Sln.FullName --no-build --filter "Category=Unit & Category!=UIAutomation" --verbosity quiet 2>&1 |
    Select-Object -Last 3
Write-Host "Tests: $($TestOut -join ' | ')"

# ── Frontend Yol Keşfi (v4++ düzeltme) ────────────────────────────────────────
$FrontendPanel    = Join-Path $RepoRoot "frontend\panel\pages"
$FrontendTrendyol = Join-Path $RepoRoot "MesTech_Trendyol\apps\web-dashboard\src\pages"
$SrcCs            = Join-Path $RepoRoot "MesTech_Stok\MesTechStok\src"

Write-Host "Frontend paths:"
Write-Host "  Panel    : $FrontendPanel ($(if(Test-Path $FrontendPanel){'EXISTS'}else{'BULUNAMADI'}))"
Write-Host "  Trendyol : $FrontendTrendyol ($(if(Test-Path $FrontendTrendyol){'EXISTS'}else{'BULUNAMADI'}))"

# ── Güvenlik Denetimleri ───────────────────────────────────────────────────────
# innerHTML: her iki frontend'i tara (XSS risk — K-11 kuralı)
$InnerHtmlPanel    = if (Test-Path $FrontendPanel) {
    (Select-String -Path (Get-ChildItem $FrontendPanel -Recurse -Include *.html,*.js) `
        -Pattern 'innerHTML\s*=' -ErrorAction SilentlyContinue).Count
} else { 0 }
$InnerHtmlTrendyol = if (Test-Path $FrontendTrendyol) {
    (Select-String -Path (Get-ChildItem $FrontendTrendyol -Recurse -Include *.html,*.js) `
        -Pattern 'innerHTML\s*=' -ErrorAction SilentlyContinue).Count
} else { 0 }
$InnerHtml = $InnerHtmlPanel + $InnerHtmlTrendyol

# Dinamik kod yürütme (K-12 kuralı) — pattern string olarak tutulur, çalıştırılmaz
$DangerPattern = 'eval[(]'
$DynExecCs = (Select-String -Path (Get-ChildItem "$SrcCs" -Recurse -Filter *.cs -ErrorAction SilentlyContinue) `
    -Pattern $DangerPattern -ErrorAction SilentlyContinue).Count
$DynExecPanel = if (Test-Path $FrontendPanel) {
    (Select-String -Path (Get-ChildItem $FrontendPanel -Recurse -Filter *.js -ErrorAction SilentlyContinue) `
        -Pattern $DangerPattern -ErrorAction SilentlyContinue).Count
} else { 0 }
$DynExecTrendyol = if (Test-Path $FrontendTrendyol) {
    (Select-String -Path (Get-ChildItem $FrontendTrendyol -Recurse -Filter *.js -ErrorAction SilentlyContinue) `
        -Pattern $DangerPattern -ErrorAction SilentlyContinue).Count
} else { 0 }
$DynamicExec = $DynExecCs + $DynExecPanel + $DynExecTrendyol

# Hardcoded credential (K-07, K-08 kuralları)
$Cred = (Select-String -Path (Get-ChildItem "$SrcCs" -Recurse -Filter *.cs -ErrorAction SilentlyContinue) `
    -Pattern 'ApiKey\s*=\s*"[A-Za-z0-9]' -ErrorAction SilentlyContinue |
    Where-Object { $_.Line -notmatch 'user-secrets|//|Test|Mock|Fake|Sample|""' }).Count

# Core contamination (K-04 kuralı)
$Core = (Select-String -Path (Get-ChildItem "$SrcCs" -Recurse -Filter *.cs -ErrorAction SilentlyContinue) `
    -Pattern "Core\.AppDbContext" -ErrorAction SilentlyContinue |
    Where-Object { $_.Line -notmatch "MesTechStok\.Core|//FROZEN" }).Count

# Boş catch — Domain + App + Infra (K-15 kuralı)
$EmptyCatchPaths = @("$SrcCs\MesTech.Domain","$SrcCs\MesTech.Application","$SrcCs\MesTech.Infrastructure") |
    Where-Object { Test-Path $_ }
$EmptyCatch = if ($EmptyCatchPaths) {
    $allFiles = $EmptyCatchPaths | ForEach-Object { Get-ChildItem $_ -Recurse -Filter *.cs }
    (Select-String -Path $allFiles -Pattern 'catch\s*\(.*\)\s*\{' -Context 0,1 -ErrorAction SilentlyContinue |
        Where-Object { $_.Context.PostContext -match '^\s*\}' }).Count
} else { 0 }

Write-Host "Security: innerHTML_panel=$InnerHtmlPanel innerHTML_trendyol=$InnerHtmlTrendyol total=$InnerHtml"
Write-Host "Security: dynamic_exec=$DynamicExec credential=$Cred"
Write-Host "Quality : core_contamination=$Core empty_catch=$EmptyCatch (target <100)"

# ── Skor Hesaplama ─────────────────────────────────────────────────────────────
$Score = 100
if ($Errors -gt 0)          { $Score -= 30 }
if ($DomainAppWarn -gt 0)   { $Score -= 15 }
if ($InnerHtml -gt 0)       { $Score -= 20 }
if ($DynamicExec -gt 0)     { $Score -= 25 }
if ($Cred -gt 0)            { $Score -= 30 }
if ($Core -gt 0)            { $Score -= 10 }
if ($EmptyCatch -gt 100)    { $Score -= 5  }

$color = if ($Score -ge 80) { "Green" } elseif ($Score -ge 70) { "Yellow" } else { "Red" }
Write-Host "=== FAST AUDIT SCORE: $Score/100 — $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" -ForegroundColor $color

if ($Score -lt 70) {
    Write-Host "⛔ COMMIT BLOKLANDI (skor < 70)" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Geçti" -ForegroundColor Green
