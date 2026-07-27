-- Fase 2D: contrato transacional de identidade e autenticacao.
-- A migration e somente aditiva; tokens em claro nunca sao persistidos.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
SET search_path TO valorapesquisa, public;

ALTER TABLE organizations ADD COLUMN IF NOT EXISTS public_name text;
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS email text;
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS phone text;
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS default_language_code text NOT NULL DEFAULT 'pt-BR';
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS time_zone text NOT NULL DEFAULT 'America/Belem';
ALTER TABLE organizations ADD COLUMN IF NOT EXISTS onboarding_status text NOT NULL DEFAULT 'pending';

ALTER TABLE users ADD COLUMN IF NOT EXISTS phone text;
ALTER TABLE users ADD COLUMN IF NOT EXISTS password_reset_required boolean NOT NULL DEFAULT false;
ALTER TABLE users ADD COLUMN IF NOT EXISTS last_login_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email_active ON users(lower(email)) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS addresses (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id),
 legal_entity_id uuid REFERENCES legal_entities(id), address_type text NOT NULL DEFAULT 'headquarters',
 street text, number text, complement text, district text, city text, state char(2), postal_code text, country_code char(2) NOT NULL DEFAULT 'BR',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS user_roles (
 user_id uuid NOT NULL REFERENCES users(id), role_id uuid NOT NULL REFERENCES roles(id),
 created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(user_id, role_id));

ALTER TABLE user_sessions ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'active';
ALTER TABLE user_sessions ADD COLUMN IF NOT EXISTS last_used_at timestamptz;
ALTER TABLE user_sessions ADD COLUMN IF NOT EXISTS ip_hash text;
ALTER TABLE user_sessions ADD COLUMN IF NOT EXISTS user_agent text;
ALTER TABLE user_sessions ADD COLUMN IF NOT EXISTS revocation_reason text;
CREATE INDEX IF NOT EXISTS ix_user_sessions_user_active ON user_sessions(user_id, expires_at) WHERE revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS refresh_token_families (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), session_id uuid NOT NULL REFERENCES user_sessions(id),
 created_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz, revocation_reason text);
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS family_id uuid REFERENCES refresh_token_families(id);
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS session_id uuid REFERENCES user_sessions(id);
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS used_at timestamptz;
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS replaced_by_id uuid REFERENCES refresh_tokens(id);
ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS revocation_reason text;
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_family ON refresh_tokens(family_id, created_at DESC);

CREATE TABLE IF NOT EXISTS login_attempts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), email_hash text NOT NULL, ip_hash text,
 succeeded boolean NOT NULL, attempted_at timestamptz NOT NULL DEFAULT now(), blocked_until timestamptz);
CREATE INDEX IF NOT EXISTS ix_login_attempts_window ON login_attempts(email_hash, attempted_at DESC);

ALTER TABLE password_reset_tokens ADD COLUMN IF NOT EXISTS request_ip_hash text;
ALTER TABLE password_reset_tokens ADD COLUMN IF NOT EXISTS user_agent text;
ALTER TABLE password_reset_tokens ADD COLUMN IF NOT EXISTS updated_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_password_reset_valid ON password_reset_tokens(token_hash, expires_at) WHERE used_at IS NULL;

CREATE TABLE IF NOT EXISTS organization_consents (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id),
 user_id uuid NOT NULL REFERENCES users(id), consent_type text NOT NULL, version text NOT NULL,
 accepted_at timestamptz NOT NULL DEFAULT now(), ip_hash text, UNIQUE(organization_id,user_id,consent_type,version));
CREATE TABLE IF NOT EXISTS onboarding_steps (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id),
 step_code text NOT NULL, status text NOT NULL DEFAULT 'pending', completed_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,step_code));

CREATE TABLE IF NOT EXISTS email_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_key text NOT NULL, language_code text NOT NULL DEFAULT 'pt-BR',
 subject_template text NOT NULL, body_template text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
 UNIQUE(template_key,language_code));
CREATE TABLE IF NOT EXISTS email_jobs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), recipient_email text NOT NULL,
 subject text NOT NULL, template_key text NOT NULL, payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 status text NOT NULL DEFAULT 'queued' CHECK(status IN ('queued','processing','sent','failed','retrying','dead_letter','cancelled')),
 idempotency_key text NOT NULL, attempts integer NOT NULL DEFAULT 0, max_attempts integer NOT NULL DEFAULT 5,
 next_attempt_at timestamptz NOT NULL DEFAULT now(), last_error text, sent_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(idempotency_key));
CREATE INDEX IF NOT EXISTS ix_email_jobs_dispatch ON email_jobs(next_attempt_at,created_at) WHERE status IN ('queued','retrying');

ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS idempotency_key text;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_at timestamptz NOT NULL DEFAULT now();
CREATE UNIQUE INDEX IF NOT EXISTS ux_outbox_idempotency ON outbox_messages(idempotency_key) WHERE idempotency_key IS NOT NULL;

INSERT INTO permissions(code,name) VALUES
 ('organization.current.read','Consultar organizacao atual'),('organization.current.update','Atualizar organizacao atual'),
 ('users.read','Consultar usuarios'),('users.create','Criar usuarios'),('users.update','Atualizar usuarios'),
 ('users.disable','Desabilitar usuarios'),('sessions.read','Consultar sessoes'),('sessions.revoke','Revogar sessoes')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name;
INSERT INTO schema_migrations(version,checksum) VALUES('20260729_004_transactional_identity_auth','phase2d-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();
COMMIT;
