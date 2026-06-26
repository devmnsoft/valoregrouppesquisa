CREATE SCHEMA IF NOT EXISTS valorapesquisa;
CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (script_name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
