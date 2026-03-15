#!/bin/bash
# fast-audit.sh — MesTech Hızlı Denetim Betiği
# Bölüm 9.1 — Her commit sonrası <60sn
# v4++ DÜZELTME (14 Mar 2026): Yol çelişkisi giderildi.
# Önceki sorun: Bash "frontend/" arıyordu, PowerShell "MesTech_Trendyol/**" arıyordu.
# Gerçek durum: İKİ ayrı frontend yolu mevcut:
#   1. frontend/panel/pages/          (MDS panel, Bitrix24-inspired, 146+ sayfa)
#   2. MesTech_Trendyol/apps/web-dashboard/src/pages/  (Trendyol-spesifik, 108+ sayfa)
# Bu betik HER İKİSİNİ de tarar.

set -euo pipefail

echo "=== FAST AUDIT — $(date '+%Y-%m-%d %H:%M:%S') ==="

# Repo kökünü bul
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel 2>/dev/null || echo "$SCRIPT_DIR/../../../..")"
SLN="$(find "$REPO_ROOT/MesTech_Stok" -name "*.sln" -maxdepth 3 | head -1)"
SLN_DIR="$(dirname "$SLN")"

# ── Build ──────────────────────────────────────────────────────────────────────
BUILD_OUT=$(dotnet build "$SLN" --no-restore 2>&1)
ERRORS=$(echo "$BUILD_OUT" | grep -c "error CS" || true)
DOMAIN_APP_WARN=$(echo "$BUILD_OUT" | grep -E "warning (CS|CA|MA)" | grep -v "MesTechStok.Desktop" | wc -l || echo 0)
echo "Build: error=$ERRORS domain_app_warning=$DOMAIN_APP_WARN"

# ── Test ───────────────────────────────────────────────────────────────────────
RESULT=$(dotnet test "$SLN" --no-build --filter "Category=Unit & Category!=UIAutomation" --verbosity quiet 2>&1 | tail -3)
echo "Tests: $RESULT"

# ── Frontend Yol Keşfi (v4++ düzeltme) ────────────────────────────────────────
FRONTEND_PANEL="$REPO_ROOT/frontend/panel/pages"
FRONTEND_TRENDYOL="$REPO_ROOT/MesTech_Trendyol/apps/web-dashboard/src/pages"
SRC_CS="$REPO_ROOT/MesTech_Stok/MesTechStok/src"

echo "Frontend paths:"
echo "  Panel    : ${FRONTEND_PANEL} ($([ -d "$FRONTEND_PANEL" ] && echo "EXISTS" || echo "BULUNAMADI"))"
echo "  Trendyol : ${FRONTEND_TRENDYOL} ($([ -d "$FRONTEND_TRENDYOL" ] && echo "EXISTS" || echo "BULUNAMADI"))"

# ── Güvenlik Denetimleri ───────────────────────────────────────────────────────
# innerHTML: her iki frontend'i tara (XSS risk pattern — K-11 kuralı)
INNERHTML_PANEL=$([ -d "$FRONTEND_PANEL" ] && grep -rn "innerHTML\s*=" "$FRONTEND_PANEL" \
  --include="*.html" --include="*.js" 2>/dev/null | wc -l || echo 0)
INNERHTML_TRENDYOL=$([ -d "$FRONTEND_TRENDYOL" ] && grep -rn "innerHTML\s*=" "$FRONTEND_TRENDYOL" \
  --include="*.html" --include="*.js" 2>/dev/null | wc -l || echo 0)
INNERHTML=$((INNERHTML_PANEL + INNERHTML_TRENDYOL))

# Dinamik kod yürütme: CS + her iki frontend (K-12 kuralı)
DANGER_PATTERN="eval[(]"
EVAL_CS=$(grep -rn "$DANGER_PATTERN" "$SRC_CS" --include="*.cs" 2>/dev/null | wc -l || echo 0)
EVAL_PANEL=$([ -d "$FRONTEND_PANEL" ] && grep -rn "$DANGER_PATTERN" "$FRONTEND_PANEL" \
  --include="*.js" 2>/dev/null | wc -l || echo 0)
EVAL_TRENDYOL=$([ -d "$FRONTEND_TRENDYOL" ] && grep -rn "$DANGER_PATTERN" "$FRONTEND_TRENDYOL" \
  --include="*.js" 2>/dev/null | wc -l || echo 0)
DYNAMIC_EXEC=$((EVAL_CS + EVAL_PANEL + EVAL_TRENDYOL))

# Hardcoded credential (K-07, K-08 kuralları)
CRED=$(grep -rn 'ApiKey\s*=\s*"[A-Za-z0-9]' "$SRC_CS" --include="*.cs" 2>/dev/null \
  | grep -v 'user-secrets\|//\|Test\|Mock\|Fake\|Sample\|""' | wc -l || echo 0)

# Core contamination (K-04 kuralı)
CORE=$(grep -rn "Core\.AppDbContext" "$SRC_CS" --include="*.cs" 2>/dev/null \
  | grep -v "MesTechStok.Core\|//FROZEN" | wc -l || echo 0)

# Boş catch — Domain + App + Infra katmanları (K-15 kuralı)
EMPTY_CATCH=$(grep -rn "catch\s*(.*)\s*{" \
  "$SRC_CS/MesTech.Domain" "$SRC_CS/MesTech.Application" "$SRC_CS/MesTech.Infrastructure" \
  --include="*.cs" -A1 2>/dev/null | grep -E "^\s+\}" | wc -l || echo 0)

echo "Security: innerHTML_panel=$INNERHTML_PANEL innerHTML_trendyol=$INNERHTML_TRENDYOL innerHTML_total=$INNERHTML"
echo "Security: dynamic_exec=$DYNAMIC_EXEC credential=$CRED"
echo "Quality : core_contamination=$CORE empty_catch=$EMPTY_CATCH (target <100)"

# ── Skor Hesaplama ─────────────────────────────────────────────────────────────
SCORE=100
[ "$ERRORS" -gt 0 ]           && SCORE=$((SCORE - 30))
[ "$DOMAIN_APP_WARN" -gt 0 ]  && SCORE=$((SCORE - 15))
[ "$INNERHTML" -gt 0 ]        && SCORE=$((SCORE - 20))
[ "$DYNAMIC_EXEC" -gt 0 ]     && SCORE=$((SCORE - 25))
[ "$CRED" -gt 0 ]             && SCORE=$((SCORE - 30))
[ "$CORE" -gt 0 ]             && SCORE=$((SCORE - 10))
[ "$EMPTY_CATCH" -gt 100 ]    && SCORE=$((SCORE - 5))

echo "=== FAST AUDIT SCORE: $SCORE/100 — $(date '+%Y-%m-%d %H:%M:%S') ==="
if [ "$SCORE" -lt 70 ]; then
  echo "⛔ COMMIT BLOKLANDI (skor < 70)"
  exit 1
fi
echo "✅ Geçti"
