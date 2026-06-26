CREATE SCHEMA IF NOT EXISTS valora;
CREATE SCHEMA IF NOT EXISTS billing;
CREATE SCHEMA IF NOT EXISTS communication;
CREATE SCHEMA IF NOT EXISTS audit;
CREATE TABLE IF NOT EXISTS valora.schema_migrations (script_name text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
