DROP SCHEMA IF EXISTS valorapesquisa CASCADE;
CREATE SCHEMA valorapesquisa;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE valorapesquisa.plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text UNIQUE, name text NOT NULL);
CREATE TABLE valorapesquisa.plan_limits (limit_key text PRIMARY KEY, limit_value int NOT NULL DEFAULT 0);
