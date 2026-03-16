#!/bin/bash
# ══════════════════════════════════════════════════════════════
# MesTech Sürekli Sağlık Kontrolü — L4e Denetim Scripti
# Her deploy sonrası veya günlük çalıştırılır
# CI entegrasyonu: .github/workflows/ci.yml health-dashboard job
# ══════════════════════════════════════════════════════════════
set -euo pipefail

REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || pwd)
SLN=$(find "$REPO_ROOT" -name "*.sln" -maxdepth 4 -not -path "*/node_modules/*" | head -1)
SLN_DIR=$(dirname "$SLN")
SRC_DIR="$SLN_DIR/src"
FRONTEND_DIR=""
for c in "$REPO_ROOT/MesTech_Trendyol/apps/web-dashboard" "$REPO_ROOT/frontend"; do
  [ -d "$c" ] && FRONTEND_DIR="$c" && break
done
NOW=$(date "+%Y-%m-%d %H:%M:%S")

echo "══════════════════════════════════════"
echo "  MesTech Surekli Saglik Kontrolu"
echo "  $NOW"
echo "══════════════════════════════════════"
echo ""

# 1. BUILD
echo "=== BUILD ==="
BUILD_OUT=$(dotnet build "$SLN" --verbosity quiet 2>&1 || true)
ERRORS=$(echo "$BUILD_OUT" | grep -c "error CS" 2>/dev/null || echo "0")
WARNINGS=$(echo "$BUILD_OUT" | grep -c "warning CS\|warning CA" 2>/dev/null || echo "0")
echo "Error:   $ERRORS"
echo "Warning: $WARNINGS"
echo ""

# 2. TEST
echo "=== TEST ==="
TEST_OUT=$(timeout 300 dotnet test "$SLN" --no-build --verbosity quiet 2>&1 || true)
TEST_LINE=$(echo "$TEST_OUT" | grep -E "Passed|Failed|Total" | tail -1 || echo "N/A")
echo "Result:  $TEST_LINE"
FACT_COUNT=$(grep -rn "\[Fact\]\|\[Theory\]" --include="*.cs" "$SLN_DIR" 2>/dev/null | wc -l || echo "0")
echo "Methods: $FACT_COUNT"
echo ""

# 3. BORC METRIKLERI
echo "=== BORC ==="
TODO_COUNT=$(grep -rn "TODO" --include="*.cs" "$SRC_DIR" 2>/dev/null | grep -v test -c || echo "0")
NIE_COUNT=$(grep -rn "NotImplementedException" --include="*.cs" "$SRC_DIR" 2>/dev/null | grep -v test -c || echo "0")
EMPTY_CATCH=$(grep -rn "catch\s*{" --include="*.cs" "$SRC_DIR" -A1 2>/dev/null | grep -E "^\s*\}" -c || echo "0")
STUB_TEST=$(grep -rn "Assert.True(true)" --include="*.cs" "$SLN_DIR" 2>/dev/null | wc -l || echo "0")
echo "TODO:           $TODO_COUNT"
echo "NIE:            $NIE_COUNT"
echo "Empty catch:    $EMPTY_CATCH"
echo "Assert.True(t): $STUB_TEST"
echo ""

# 4. ENVANTER
echo "=== ENVANTER ==="
ENTITY=$(find "$SRC_DIR" -path "*/Entities/*.cs" -name "*.cs" 2>/dev/null | wc -l || echo "0")
CMD=$(find "$SRC_DIR" -path "*/Commands/*" -name "*Command.cs" 2>/dev/null | wc -l || echo "0")
QRY=$(find "$SRC_DIR" -path "*/Queries/*" -name "*Query.cs" 2>/dev/null | wc -l || echo "0")
ENDPOINT=$(find "$SRC_DIR" -name "*Endpoints.cs" 2>/dev/null | wc -l || echo "0")
WPF=$(find "$SRC_DIR" -name "*.xaml" -path "*/Views/*" 2>/dev/null | wc -l || echo "0")
AVALONIA=$(find "$SRC_DIR" -name "*.axaml" -path "*/Views/*" 2>/dev/null | wc -l || echo "0")
BLAZOR=$(find "$SRC_DIR" -name "*.razor" -path "*/Pages/*" 2>/dev/null | wc -l || echo "0")
HTML=$(find "$FRONTEND_DIR" "$REPO_ROOT/frontend" -name "*.html" -path "*/pages/*" 2>/dev/null | sort -u | wc -l || echo "0")
echo "Entity:         $ENTITY"
echo "CQRS Command:   $CMD"
echo "CQRS Query:     $QRY"
echo "WebAPI Endpoint: $ENDPOINT"
echo "WPF View:       $WPF"
echo "Avalonia View:  $AVALONIA"
echo "Blazor Page:    $BLAZOR"
echo "HTML Page:      $HTML"
echo "Test Method:    $FACT_COUNT"
echo ""

# 5. GUVENLIK
echo "=== GUVENLIK ==="
CRED=$(grep -rn "ApiKey\s*=\s*\"[A-Za-z0-9]" --include="*.cs" "$SRC_DIR" 2>/dev/null | grep -v "placeholder\|YOUR_\|test-" -c || echo "0")
if [ -n "$FRONTEND_DIR" ]; then
  INNERHTML=$(grep -rn "innerHTML\s*=" "$FRONTEND_DIR" "$REPO_ROOT/frontend" --include="*.html" --include="*.js" 2>/dev/null | wc -l || echo "0")
else
  INNERHTML="N/A"
fi
echo "Hardcoded key:  $CRED"
echo "innerHTML:      $INNERHTML"
echo ""

# 6. PANEL PARITE
echo "=== PANEL PARITE ==="
WPF_LEE=$(grep -rln "LoadingOverlay\|EmptyState\|ErrorState" "$SRC_DIR/MesTechStok.Desktop" --include="*.xaml" 2>/dev/null | wc -l || echo "0")
BLAZOR_LOAD=$(grep -rln "isLoading\|IsLoading" "$SRC_DIR/MesTech.Blazor" --include="*.razor" 2>/dev/null | wc -l || echo "0")
AVALONIA_STUB=$(grep -rln "Avalonia port\|Stub view" "$SRC_DIR/MesTech.Avalonia" --include="*.axaml" 2>/dev/null | wc -l || echo "0")
echo "WPF L/E/E:      $WPF_LEE/$WPF"
echo "Blazor Loading: $BLAZOR_LOAD/$BLAZOR"
echo "Avalonia Stub:  $AVALONIA_STUB/$AVALONIA"
echo ""

# 7. SKOR
SCORE=100
[ "$ERRORS" -gt 0 ] 2>/dev/null && SCORE=$((SCORE - 30))
[ "$INNERHTML" != "N/A" ] && [ "$INNERHTML" -gt 500 ] 2>/dev/null && SCORE=$((SCORE - 20))
[ "$CRED" -gt 0 ] 2>/dev/null && SCORE=$((SCORE - 30))
[ "$NIE_COUNT" -gt 50 ] 2>/dev/null && SCORE=$((SCORE - 10))
echo "══════════════════════════════════════"
echo "  SAGLIK SKORU: $SCORE/100"
echo "  Tarih: $NOW"
echo "══════════════════════════════════════"
