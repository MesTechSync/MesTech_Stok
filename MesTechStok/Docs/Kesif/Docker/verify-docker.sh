#!/bin/bash
# ═══════════════════════════════════════════════════════════
# MesTech Stok — Docker Altyapı Doğrulama Scripti
# Kullanım: chmod +x verify-docker.sh && ./verify-docker.sh
# ═══════════════════════════════════════════════════════════

set -e

GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

PASSED=0
FAILED=0
TOTAL=0

check() {
    local name="$1"
    local cmd="$2"
    TOTAL=$((TOTAL + 1))
    printf "  [%d] %s... " "$TOTAL" "$name"
    
    if output=$(eval "$cmd" 2>&1); then
        printf "${GREEN}GECTI${NC}\n"
        PASSED=$((PASSED + 1))
    else
        printf "${RED}BASARISIZ${NC}\n"
        printf "      %s\n" "$output"
        FAILED=$((FAILED + 1))
    fi
}

echo ""
echo "════════════════════════════════════════════"
echo " MesTech Stok — Docker Dogrulama"
echo "════════════════════════════════════════════"
echo ""

# Load .env if exists
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

REDIS_PASS=${REDIS_PASSWORD:-mestech_redis_dev}
PG_USER=${POSTGRES_USER:-mestech_user}
PG_DB=${POSTGRES_DB:-mestech_stok}

# ── ADIM 1: Docker ──
printf "${YELLOW}[ADIM 1] Docker Engine${NC}\n"
check "Docker daemon aktif" "docker info --format '{{.ServerVersion}}'"

# ── ADIM 2: Container'lar ──
printf "\n${YELLOW}[ADIM 2] Container Durumlari${NC}\n"
check "mestech-postgres aktif" "[ \$(docker inspect -f '{{.State.Status}}' mestech-postgres 2>/dev/null) = 'running' ]"
check "mestech-redis aktif" "[ \$(docker inspect -f '{{.State.Status}}' mestech-redis 2>/dev/null) = 'running' ]"
check "mestech-rabbitmq aktif" "[ \$(docker inspect -f '{{.State.Status}}' mestech-rabbitmq 2>/dev/null) = 'running' ]"

# ── ADIM 3: PostgreSQL ──
printf "\n${YELLOW}[ADIM 3] PostgreSQL${NC}\n"
check "PostgreSQL baglanti" "docker exec mestech-postgres pg_isready -U $PG_USER -d $PG_DB"
check "uuid-ossp extension" "[ \$(docker exec mestech-postgres psql -U $PG_USER -d $PG_DB -tAc \"SELECT count(*) FROM pg_extension WHERE extname='uuid-ossp';\") -eq 1 ]"
check "pgvector extension" "[ \$(docker exec mestech-postgres psql -U $PG_USER -d $PG_DB -tAc \"SELECT count(*) FROM pg_extension WHERE extname='vector';\") -eq 1 ]"
check "pg_trgm extension" "[ \$(docker exec mestech-postgres psql -U $PG_USER -d $PG_DB -tAc \"SELECT count(*) FROM pg_extension WHERE extname='pg_trgm';\") -eq 1 ]"
check "citext extension" "[ \$(docker exec mestech-postgres psql -U $PG_USER -d $PG_DB -tAc \"SELECT count(*) FROM pg_extension WHERE extname='citext';\") -eq 1 ]"
check "Audit log tablosu" "[ \$(docker exec mestech-postgres psql -U $PG_USER -d $PG_DB -tAc \"SELECT count(*) FROM information_schema.tables WHERE table_name='_db_audit_log';\") -eq 1 ]"

# ── ADIM 4: Redis ──
printf "\n${YELLOW}[ADIM 4] Redis${NC}\n"
check "Redis PING" "[ \$(docker exec mestech-redis redis-cli -a $REDIS_PASS ping 2>/dev/null) = 'PONG' ]"
check "Redis maxmemory-policy" "docker exec mestech-redis redis-cli -a $REDIS_PASS CONFIG GET maxmemory-policy 2>/dev/null | grep -q allkeys-lru"
check "Redis AOF aktif" "docker exec mestech-redis redis-cli -a $REDIS_PASS CONFIG GET appendonly 2>/dev/null | grep -q yes"
check "Redis SET/GET" "docker exec mestech-redis redis-cli -a $REDIS_PASS SET mestech:test ok EX 10 2>/dev/null && [ \$(docker exec mestech-redis redis-cli -a $REDIS_PASS GET mestech:test 2>/dev/null) = 'ok' ]"

# ── ADIM 5: RabbitMQ ──
printf "\n${YELLOW}[ADIM 5] RabbitMQ${NC}\n"
check "RabbitMQ node" "docker exec mestech-rabbitmq rabbitmq-diagnostics check_running 2>/dev/null | head -1"
check "RabbitMQ vhost 'mestech'" "docker exec mestech-rabbitmq rabbitmqctl list_vhosts 2>/dev/null | grep -q mestech"

# ── ADIM 6: Volume'lar ──
printf "\n${YELLOW}[ADIM 6] Volumes${NC}\n"
check "mestech_pgdata" "docker volume inspect mestech_pgdata --format '{{.Name}}'"
check "mestech_redis_data" "docker volume inspect mestech_redis_data --format '{{.Name}}'"
check "mestech_rabbitmq_data" "docker volume inspect mestech_rabbitmq_data --format '{{.Name}}'"

# ── ADIM 7: Network ──
printf "\n${YELLOW}[ADIM 7] Network${NC}\n"
check "mestech-network" "docker network inspect mestech-network --format '{{.Name}}'"

# ── SONUÇ ──
echo ""
echo "════════════════════════════════════════════"
if [ $FAILED -eq 0 ]; then
    printf " SONUC: ${GREEN}$PASSED GECTI / $FAILED BASARISIZ / $TOTAL TOPLAM${NC}\n"
    echo "════════════════════════════════════════════"
    echo ""
    printf " ${GREEN}Tum altyapi servisleri SAGLIKLI${NC}\n"
    printf " ${CYAN}Sonraki adimlar:${NC}\n"
    echo "   1. dotnet ef database update (migration uygula)"
    echo "   2. Desktop uygulamasini baslat"
    echo "   3. CRUD islemlerini dogrula"
else
    printf " SONUC: ${RED}$PASSED GECTI / $FAILED BASARISIZ / $TOTAL TOPLAM${NC}\n"
    echo "════════════════════════════════════════════"
    printf " ${RED}$FAILED kontrol basarisiz!${NC}\n"
    printf " ${YELLOW}docker compose logs <servis> ile detay goruntuleyin${NC}\n"
fi
echo ""
exit $FAILED
