#!/bin/bash
# ═══════════════════════════════════════════════════
# MesTech Production Readiness Check
# İ-12 emirnamesi kapsamında oluşturuldu
# Kullanım: bash Scripts/production-readiness-check.sh
# ═══════════════════════════════════════════════════
set -uo pipefail

# Helper: safely count lines from stdin (handles pipefail + whitespace)
count_safe() {
  local result
  result="$(cat | wc -l | tr -d '[:space:]')"
  echo "${result:-0}"
}

# Detect repo root
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

REPORT_FILE="Docs/PRODUCTION_READINESS_REPORT.md"
mkdir -p Docs

echo "╔══════════════════════════════════════════════════════╗"
echo "║  MesTech Production Readiness Check                  ║"
echo "╚══════════════════════════════════════════════════════╝"
echo ""

# Initialize report
cat > "$REPORT_FILE" << 'HEADER'
# MesTech Production Readiness Report
HEADER

echo "**Tarih:** $(date '+%Y-%m-%d %H:%M:%S')" >> "$REPORT_FILE"
echo "**Oluşturan:** Otomatik script (Scripts/production-readiness-check.sh)" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

BLOCKERS=0
WARNINGS=0

# ═══ 1. BUILD ═══
echo "═══ 1. Build Kontrolü ═══"
echo "## 1. Build Durumu" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
BUILD_OUTPUT=$(dotnet build --nologo -v q 2>&1 || true)
BUILD_ERRORS=$(echo "$BUILD_OUTPUT" | { grep -c ': error ' || true; } | tr -d '[:space:]')
BUILD_ERRORS=${BUILD_ERRORS:-0}
BUILD_WARNINGS=$(echo "$BUILD_OUTPUT" | { grep -c ': warning ' || true; } | tr -d '[:space:]')
BUILD_WARNINGS=${BUILD_WARNINGS:-0}

echo '```' >> "$REPORT_FILE"
echo "$BUILD_OUTPUT" | tail -5 >> "$REPORT_FILE"
echo '```' >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"
echo "| Metrik | Sayı | Durum |" >> "$REPORT_FILE"
echo "|--------|------|-------|" >> "$REPORT_FILE"
echo "| Build error | $BUILD_ERRORS | $([ "$BUILD_ERRORS" -eq 0 ] && echo '✅' || echo '🔴 BLOCKER') |" >> "$REPORT_FILE"
echo "| Build warning | $BUILD_WARNINGS | $([ "$BUILD_WARNINGS" -lt 1500 ] && echo '✅' || echo '⚠️') |" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

[ "$BUILD_ERRORS" -gt 0 ] && BLOCKERS=$((BLOCKERS + 1))

echo "  Errors: $BUILD_ERRORS | Warnings: $BUILD_WARNINGS"

# ═══ 2. GÜVENLİK ═══
echo "═══ 2. Güvenlik Kontrolü ═══"
echo "## 2. Güvenlik" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

CRED_COUNT=$( { grep -rn 'password\s*=\|apikey\s*=\|secret\s*=' src/ --include='*.cs' 2>/dev/null || true; } | { grep -iv 'test\|mock\|sample\|example\|placeholder\|your_\|CONFIGURE_VIA\|///\|//.*=' || true; } | wc -l | tr -d '[:space:]')
CRED_COUNT=${CRED_COUNT:-0}
NIE_COUNT=$( { grep -rn 'NotImplementedException' src/MesTech.Application/ src/MesTech.Infrastructure/ 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
NIE_COUNT=${NIE_COUNT:-0}
ENV_EXAMPLE=$([ -f .env.example ] && echo "✅ Mevcut" || echo "❌ YOK")

echo "| Kontrol | Sayı | Durum |" >> "$REPORT_FILE"
echo "|---------|------|-------|" >> "$REPORT_FILE"
echo "| Hardcoded credential | $CRED_COUNT | $([ "$CRED_COUNT" -eq 0 ] && echo '✅' || echo '🔴 BLOCKER') |" >> "$REPORT_FILE"
echo "| NotImplementedException (App+Infra) | $NIE_COUNT | $([ "$NIE_COUNT" -eq 0 ] && echo '✅' || echo '🔴 BLOCKER') |" >> "$REPORT_FILE"
echo "| .env.example | - | $ENV_EXAMPLE |" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

[ "$CRED_COUNT" -gt 0 ] && BLOCKERS=$((BLOCKERS + 1))
[ "$NIE_COUNT" -gt 0 ] && BLOCKERS=$((BLOCKERS + 1))

echo "  Credentials: $CRED_COUNT | NIE: $NIE_COUNT | .env.example: $ENV_EXAMPLE"

# ═══ 3. KALİTE ═══
echo "═══ 3. Kalite Metrikleri ═══"
echo "## 3. Kalite Metrikleri" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

PLACEHOLDER=$( { find src/ -name '*.axaml' -exec grep -l 'PLACEHOLDER\|ComingSoon\|coming.soon' {} \; 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
PLACEHOLDER=${PLACEHOLDER:-0}
OLD_COLOR=$( { grep -rn '#2855AC\|#2855ac' src/ --include='*.axaml' 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
OLD_COLOR=${OLD_COLOR:-0}
EMPTY_CATCH=$( { grep -rn 'catch\s*{' src/MesTechStok.Desktop/ --include='*.cs' 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
EMPTY_CATCH=${EMPTY_CATCH:-0}
TODO_COUNT=$( { grep -rn 'TODO\|FIXME' src/ --include='*.cs' 2>/dev/null || true; } | { grep -iv test || true; } | wc -l | tr -d '[:space:]')
TODO_COUNT=${TODO_COUNT:-0}

echo "| Metrik | Sayı | Hedef | Durum |" >> "$REPORT_FILE"
echo "|--------|------|-------|-------|" >> "$REPORT_FILE"
echo "| Placeholder (.axaml) | $PLACEHOLDER | 0 | $([ "$PLACEHOLDER" -eq 0 ] && echo '✅' || echo '⚠️') |" >> "$REPORT_FILE"
echo "| Eski renk referans | $OLD_COLOR | 0 | $([ "$OLD_COLOR" -eq 0 ] && echo '✅' || echo '⚠️') |" >> "$REPORT_FILE"
echo "| Boş catch (WPF) | $EMPTY_CATCH | <80 | $([ "$EMPTY_CATCH" -lt 80 ] && echo '✅' || echo '⚠️') |" >> "$REPORT_FILE"
echo "| TODO/FIXME (non-test) | $TODO_COUNT | <5 | $([ "$TODO_COUNT" -lt 5 ] && echo '✅' || echo '⚠️') |" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

[ "$PLACEHOLDER" -gt 0 ] && WARNINGS=$((WARNINGS + 1))
[ "$OLD_COLOR" -gt 0 ] && WARNINGS=$((WARNINGS + 1))

echo "  Placeholder: $PLACEHOLDER | Old color: $OLD_COLOR | Empty catch: $EMPTY_CATCH | TODOs: $TODO_COUNT"

# ═══ 4. ADAPTER & ENTEGRASYON ═══
echo "═══ 4. Adapter Durumu ═══"
echo "## 4. Adapter & Entegrasyon" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

PLATFORM_ADAPTER=$( { find src/MesTech.Infrastructure/Integration/Adapters -name '*Adapter.cs' 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
PLATFORM_ADAPTER=${PLATFORM_ADAPTER:-0}
CARGO_ADAPTER=$( { find src/MesTech.Infrastructure/Integration/Adapters \( -name '*Kargo*' -o -name '*Cargo*' -o -name '*HepsiJet*' -o -name '*Sendeo*' \) 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
CARGO_ADAPTER=${CARGO_ADAPTER:-0}
INVOICE_PROVIDER=$( { find src/MesTech.Infrastructure -name '*InvoiceProvider*' -not -name '*Mock*' 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
INVOICE_PROVIDER=${INVOICE_PROVIDER:-0}

echo "| Tip | Sayı |" >> "$REPORT_FILE"
echo "|-----|------|" >> "$REPORT_FILE"
echo "| Platform adapter | $PLATFORM_ADAPTER |" >> "$REPORT_FILE"
echo "| Kargo adapter | $CARGO_ADAPTER |" >> "$REPORT_FILE"
echo "| Fatura provider | $INVOICE_PROVIDER |" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

echo "  Platform: $PLATFORM_ADAPTER | Kargo: $CARGO_ADAPTER | Fatura: $INVOICE_PROVIDER"

# ═══ 5. AVALONIA PARİTY ═══
echo "═══ 5. Avalonia Parity ═══"
echo "## 5. Avalonia Parity" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

WPF_VIEW=$( { find src/MesTechStok.Desktop -name "*.xaml" -not -name "App.xaml" -not -name "*.Styles.*" 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
WPF_VIEW=${WPF_VIEW:-0}
AVAL_VIEW=$( { find src/MesTech.Avalonia -name "*.axaml" -not -name "App.axaml" -not -name "*.Styles.*" -not -path "*/Themes/*" 2>/dev/null || true; } | wc -l | tr -d '[:space:]')
AVAL_VIEW=${AVAL_VIEW:-0}

echo "| Panel | Sayı |" >> "$REPORT_FILE"
echo "|-------|------|" >> "$REPORT_FILE"
echo "| WPF view | $WPF_VIEW |" >> "$REPORT_FILE"
echo "| Avalonia view | $AVAL_VIEW |" >> "$REPORT_FILE"
echo "| Kapsama | ${AVAL_VIEW}/${WPF_VIEW} |" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

echo "  WPF: $WPF_VIEW | Avalonia: $AVAL_VIEW"

# ═══ 6. DOCKER & ALTYAPI ═══
echo "═══ 6. Altyapı ═══"
echo "## 6. Altyapı Dosyaları" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

DOCKERFILE_EXISTS=$(find . -name 'Dockerfile' 2>/dev/null | head -1)

echo "| Dosya | Durum |" >> "$REPORT_FILE"
echo "|-------|-------|" >> "$REPORT_FILE"
echo "| .env.example | $([ -f .env.example ] && echo '✅' || echo '❌') |" >> "$REPORT_FILE"
echo "| .env.production.template | $([ -f .env.production.template ] && echo '✅' || echo '❌') |" >> "$REPORT_FILE"
echo "| docker-compose.yml | $([ -f docker-compose.yml ] && echo '✅' || echo '❌') |" >> "$REPORT_FILE"
echo "| Dockerfile | $([ -n "$DOCKERFILE_EXISTS" ] && echo '✅' || echo '❌') |" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

# ═══ 7. GO/NO-GO ═══
echo "═══ 7. Go/No-Go ═══"
echo "## 7. Go/No-Go Kararı" >> "$REPORT_FILE"
echo "" >> "$REPORT_FILE"

if [ "$BLOCKERS" -eq 0 ]; then
  echo "**KARAR: ✅ GO — Production deploy için hazır (koşullu)**" >> "$REPORT_FILE"
  echo "" >> "$REPORT_FILE"
  echo "### Koşullar:" >> "$REPORT_FILE"
  echo "1. Platform sandbox credential'ları .env dosyasına girilmeli" >> "$REPORT_FILE"
  echo "2. Sovos/GİB test ortamında e-fatura doğrulanmalı" >> "$REPORT_FILE"
  echo "3. SSL sertifikası hazırlanmalı" >> "$REPORT_FILE"
  echo "4. PostgreSQL backup stratejisi aktif olmalı" >> "$REPORT_FILE"
  echo "" >> "$REPORT_FILE"
  echo "  ✅ GO — $BLOCKERS blocker, $WARNINGS uyarı"
else
  echo "**KARAR: ❌ NO-GO — $BLOCKERS blocker mevcut**" >> "$REPORT_FILE"
  echo "" >> "$REPORT_FILE"
  echo "  ❌ NO-GO — $BLOCKERS blocker"
fi

echo "" >> "$REPORT_FILE"
echo "---" >> "$REPORT_FILE"
echo "*Otomatik oluşturuldu — Scripts/production-readiness-check.sh*" >> "$REPORT_FILE"

echo ""
echo "╔══════════════════════════════════════════════════════╗"
echo "║  Rapor oluşturuldu: $REPORT_FILE"
echo "╚══════════════════════════════════════════════════════╝"
