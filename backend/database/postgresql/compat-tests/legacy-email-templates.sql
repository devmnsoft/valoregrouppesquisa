DROP SCHEMA IF EXISTS valorapesquisa CASCADE;
CREATE SCHEMA valorapesquisa;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE valorapesquisa.email_templates (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text UNIQUE, subject text NOT NULL, body text NOT NULL);
