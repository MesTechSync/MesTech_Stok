-- MesTech Stok - PostgreSQL 17 Initial Setup
-- pgvector extension + temel yapılandırma

-- pgvector extension (AI/vektörel arama desteği)
CREATE EXTENSION IF NOT EXISTS vector;

-- UUID desteği
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Full-text search desteği (Türkçe)
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- Performans izleme
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

-- Bağlantı doğrulama
SELECT version();
SELECT * FROM pg_available_extensions WHERE name = 'vector';
