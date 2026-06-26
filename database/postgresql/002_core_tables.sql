CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE IF NOT EXISTS valora.organizations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name text NOT NULL, public_name text, slug text NOT NULL UNIQUE, document text, email text, phone text, status text NOT NULL DEFAULT 'active', plan_id text NOT NULL DEFAULT 'free', settings_json jsonb NOT NULL DEFAULT '{}'::jsonb, brand_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), created_by uuid, updated_by uuid, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS valora.users (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NULL REFERENCES valora.organizations(id), name text NOT NULL, email text NOT NULL UNIQUE, password_hash text NOT NULL, role text NOT NULL, status text NOT NULL DEFAULT 'active', phone text, last_login_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), created_by uuid, updated_by uuid, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_users_organization ON valora.users(organization_id);

CREATE TABLE IF NOT EXISTS valora.units (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valora.organizations(id), name text NOT NULL, slug text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), created_by uuid, updated_by uuid, is_deleted boolean NOT NULL DEFAULT false);
