# ═══════════════════════════════════════════════════════════
# MesTech Stok — Docker Altyapı Doğrulama Scripti
# Kullanım: .\verify-docker.ps1
# ═══════════════════════════════════════════════════════════

param(
    [string]$ComposeDir = ".",
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"
$passed = 0
$failed = 0
$total = 0

function Test-Check {
    param([string]$Name, [scriptblock]$Check)
    $script:total++
    Write-Host -NoNewline "  [$script:total] $Name... " -ForegroundColor Cyan
    try {
        $result = & $Check 2>&1
        if ($LASTEXITCODE -eq 0 -or $null -eq $LASTEXITCODE) {
            Write-Host "GECTI" -ForegroundColor Green
            if ($Verbose) { Write-Host "      $result" -ForegroundColor DarkGray }
            $script:passed++
        } else {
            Write-Host "BASARISIZ" -ForegroundColor Red
            Write-Host "      $result" -ForegroundColor Yellow
            $script:failed++
        }
    } catch {
        Write-Host "HATA" -ForegroundColor Red
        Write-Host "      $($_.Exception.Message)" -ForegroundColor Yellow
        $script:failed++
    }
}

Write-Host ""
Write-Host "════════════════════════════════════════════" -ForegroundColor White
Write-Host " MesTech Stok — Docker Dogrulama" -ForegroundColor White
Write-Host "════════════════════════════════════════════" -ForegroundColor White
Write-Host ""

# ── ADIM 1: Docker çalışıyor mu? ──
Write-Host "[ADIM 1] Docker Engine Kontrolu" -ForegroundColor Yellow
Test-Check "Docker daemon aktif" {
    docker info --format '{{.ServerVersion}}' 2>$null
}

# ── ADIM 2: Container'lar çalışıyor mu? ──
Write-Host ""
Write-Host "[ADIM 2] Container Durumlari" -ForegroundColor Yellow
Test-Check "mestech-postgres container aktif" {
    $state = docker inspect -f '{{.State.Status}}' mestech-postgres 2>$null
    if ($state -ne "running") { throw "Durum: $state" }
    "running"
}
Test-Check "mestech-redis container aktif" {
    $state = docker inspect -f '{{.State.Status}}' mestech-redis 2>$null
    if ($state -ne "running") { throw "Durum: $state" }
    "running"
}
Test-Check "mestech-rabbitmq container aktif" {
    $state = docker inspect -f '{{.State.Status}}' mestech-rabbitmq 2>$null
    if ($state -ne "running") { throw "Durum: $state" }
    "running"
}

# ── ADIM 3: PostgreSQL sağlık kontrolü ──
Write-Host ""
Write-Host "[ADIM 3] PostgreSQL Kontrolleri" -ForegroundColor Yellow
Test-Check "PostgreSQL baglanti kabul ediyor" {
    docker exec mestech-postgres pg_isready -U mestech_user -d mestech_stok
}
Test-Check "uuid-ossp extension yuklu" {
    $result = docker exec mestech-postgres psql -U mestech_user -d mestech_stok -tAc "SELECT count(*) FROM pg_extension WHERE extname='uuid-ossp';"
    if ($result.Trim() -ne "1") { throw "uuid-ossp bulunamadi" }
    "uuid-ossp aktif"
}
Test-Check "pgvector extension yuklu" {
    $result = docker exec mestech-postgres psql -U mestech_user -d mestech_stok -tAc "SELECT count(*) FROM pg_extension WHERE extname='vector';"
    if ($result.Trim() -ne "1") { throw "vector bulunamadi" }
    "pgvector aktif"
}
Test-Check "pg_trgm extension yuklu" {
    $result = docker exec mestech-postgres psql -U mestech_user -d mestech_stok -tAc "SELECT count(*) FROM pg_extension WHERE extname='pg_trgm';"
    if ($result.Trim() -ne "1") { throw "pg_trgm bulunamadi" }
    "pg_trgm aktif"
}
Test-Check "citext extension yuklu" {
    $result = docker exec mestech-postgres psql -U mestech_user -d mestech_stok -tAc "SELECT count(*) FROM pg_extension WHERE extname='citext';"
    if ($result.Trim() -ne "1") { throw "citext bulunamadi" }
    "citext aktif"
}
Test-Check "Audit log tablosu mevcut" {
    $result = docker exec mestech-postgres psql -U mestech_user -d mestech_stok -tAc "SELECT count(*) FROM information_schema.tables WHERE table_name='_db_audit_log';"
    if ($result.Trim() -ne "1") { throw "_db_audit_log bulunamadi" }
    "_db_audit_log mevcut"
}

# ── ADIM 4: Redis sağlık kontrolü ──
Write-Host ""
Write-Host "[ADIM 4] Redis Kontrolleri" -ForegroundColor Yellow
Test-Check "Redis PING yanit veriyor" {
    $result = docker exec mestech-redis redis-cli -a mestech_redis_dev ping 2>$null
    if ($result -ne "PONG") { throw "Yanit: $result" }
    "PONG"
}
Test-Check "Redis maxmemory-policy dogru" {
    $result = docker exec mestech-redis redis-cli -a mestech_redis_dev CONFIG GET maxmemory-policy 2>$null
    if ($result -notcontains "allkeys-lru") { throw "Policy: $result" }
    "allkeys-lru"
}
Test-Check "Redis AOF aktif" {
    $result = docker exec mestech-redis redis-cli -a mestech_redis_dev CONFIG GET appendonly 2>$null
    if ($result -notcontains "yes") { throw "AOF: $result" }
    "appendonly yes"
}
Test-Check "Redis SET/GET calisiyor" {
    docker exec mestech-redis redis-cli -a mestech_redis_dev SET mestech:healthcheck:test "ok" EX 60 2>$null | Out-Null
    $result = docker exec mestech-redis redis-cli -a mestech_redis_dev GET mestech:healthcheck:test 2>$null
    if ($result.Trim() -ne "ok") { throw "GET sonucu: $result" }
    docker exec mestech-redis redis-cli -a mestech_redis_dev DEL mestech:healthcheck:test 2>$null | Out-Null
    "SET/GET basarili"
}

# ── ADIM 5: RabbitMQ sağlık kontrolü ──
Write-Host ""
Write-Host "[ADIM 5] RabbitMQ Kontrolleri" -ForegroundColor Yellow
Test-Check "RabbitMQ node calisiyor" {
    docker exec mestech-rabbitmq rabbitmq-diagnostics check_running 2>$null | Select-Object -First 1
}
Test-Check "RabbitMQ vhost 'mestech' mevcut" {
    $result = docker exec mestech-rabbitmq rabbitmqctl list_vhosts --formatter json 2>$null | ConvertFrom-Json
    $found = $result | Where-Object { $_.name -eq "mestech" }
    if (-not $found) { throw "mestech vhost bulunamadi" }
    "mestech vhost aktif"
}
Test-Check "RabbitMQ management API erisilebilir" {
    $response = Invoke-WebRequest -Uri "http://localhost:15672/api/overview" -Headers @{Authorization = "Basic $([Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('mestech_mq:mestech_mq_dev')))"} -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -ne 200) { throw "HTTP $($response.StatusCode)" }
    "HTTP 200 OK"
}

# ── ADIM 6: Volume kontrolü ──
Write-Host ""
Write-Host "[ADIM 6] Volume Kontrolleri" -ForegroundColor Yellow
Test-Check "mestech_pgdata volume mevcut" {
    docker volume inspect mestech_pgdata --format '{{.Name}}' 2>$null
}
Test-Check "mestech_redis_data volume mevcut" {
    docker volume inspect mestech_redis_data --format '{{.Name}}' 2>$null
}
Test-Check "mestech_rabbitmq_data volume mevcut" {
    docker volume inspect mestech_rabbitmq_data --format '{{.Name}}' 2>$null
}

# ── ADIM 7: Network kontrolü ──
Write-Host ""
Write-Host "[ADIM 7] Network Kontrolu" -ForegroundColor Yellow
Test-Check "mestech-network mevcut" {
    docker network inspect mestech-network --format '{{.Name}}' 2>$null
}

# ── SONUÇ ──
Write-Host ""
Write-Host "════════════════════════════════════════════" -ForegroundColor White
Write-Host " SONUC: $passed GECTI / $failed BASARISIZ / $total TOPLAM" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "════════════════════════════════════════════" -ForegroundColor White

if ($failed -eq 0) {
    Write-Host ""
    Write-Host " Tum altyapi servisleri SAGLIKLI" -ForegroundColor Green
    Write-Host " Desktop uygulamasi baslatilanilir." -ForegroundColor Green
    Write-Host ""
    Write-Host " Sonraki adim:" -ForegroundColor Cyan
    Write-Host "   1. dotnet ef database update (migration uygula)" -ForegroundColor White
    Write-Host "   2. Visual Studio uzerinde MesTech.Desktop baslat" -ForegroundColor White
    Write-Host "   3. Dashboard + CRUD islemlerini dogrula" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host " $failed kontrol basarisiz! Yukaridaki hatalari inceleyin." -ForegroundColor Red
    Write-Host " docker compose logs <servis-adi> ile detay gorebilirsiniz." -ForegroundColor Yellow
}

Write-Host ""
exit $failed
