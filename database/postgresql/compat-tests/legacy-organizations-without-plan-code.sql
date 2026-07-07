DROP SCHEMA IF EXISTS valorapesquisa CASCADE;
CREATE SCHEMA valorapesquisa;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE TABLE valorapesquisa.organizations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name text NOT NULL, slug citext NOT NULL UNIQUE, plan_id text);
