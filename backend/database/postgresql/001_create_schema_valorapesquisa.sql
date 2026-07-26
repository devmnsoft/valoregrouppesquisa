-- AVISO Fase 1: o bootstrap canonico agora e backend/database/postgresql/banco_completo.sql. Este script historico foi preservado para referencia/compatibilidade.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (script_name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
