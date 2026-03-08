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

-- Ek database'ler (manuel olusturulur veya setup-staging.sh ile):
-- CREATE DATABASE mestech_mesa;     -- MESA OS icin
-- CREATE DATABASE mestech_staging;  -- Staging ortami icin
-- Not: PostgreSQL init scripts sadece POSTGRES_DB (mestech_stok) icinde calisir.
-- Ek database'ler icin setup-staging.sh scriptini calistirin.
