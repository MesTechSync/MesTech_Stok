-- ═══════════════════════════════════════════════════════════
-- MesTech Stok — PostgreSQL İlk Çalıştırma Scripti
-- Bu script SADECE container ilk oluşturulduğunda çalışır
-- Volume silinmediği sürece tekrar çalışmaz
-- ═══════════════════════════════════════════════════════════

-- pgvector — Gelecekte AI/embedding aramaları için
CREATE EXTENSION IF NOT EXISTS vector;

-- uuid-ossp — Guid PK üretimi (EF Core uuid_generate_v4)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- pg_trgm — Fuzzy text search (ürün arama, SKU arama)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- citext — Case-insensitive text (SKU, barkod karşılaştırma)
CREATE EXTENSION IF NOT EXISTS citext;

-- ═══════════════════════════════════════════════════════════
-- DB seviyesi audit log (uygulama EF audit'inden bağımsız)
-- ═══════════════════════════════════════════════════════════
CREATE TABLE IF NOT EXISTS _db_audit_log (
    id          BIGSERIAL PRIMARY KEY,
    table_name  TEXT NOT NULL,
    operation   TEXT NOT NULL CHECK (operation IN ('INSERT', 'UPDATE', 'DELETE')),
    old_data    JSONB,
    new_data    JSONB,
    changed_by  TEXT DEFAULT 'system',
    changed_at  TIMESTAMPTZ DEFAULT NOW()
);

-- Index: Son 30 günlük audit sorgulama hızı
CREATE INDEX IF NOT EXISTS idx_db_audit_log_changed_at 
    ON _db_audit_log (changed_at DESC);

CREATE INDEX IF NOT EXISTS idx_db_audit_log_table_name 
    ON _db_audit_log (table_name);

-- ═══════════════════════════════════════════════════════════
-- Bilgi mesajı
-- ═══════════════════════════════════════════════════════════
DO $$
BEGIN
    RAISE NOTICE '════════════════════════════════════════════';
    RAISE NOTICE 'MesTech Stok DB baslatildi';
    RAISE NOTICE 'Extensions: vector, uuid-ossp, pg_trgm, citext';
    RAISE NOTICE 'Audit log tablosu: _db_audit_log';
    RAISE NOTICE '════════════════════════════════════════════';
END $$;
