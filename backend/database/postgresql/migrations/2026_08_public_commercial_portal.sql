-- Public commercial portal support. Idempotent and non-destructive.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.public_signup_attempts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), email_hash text NOT NULL, ip_hash text,
 status text NOT NULL CHECK (status IN ('started','completed','rejected')), reason_code text,
 organization_id uuid REFERENCES valorapesquisa.organizations(id), created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_public_signup_attempts_created ON valorapesquisa.public_signup_attempts(created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.email_confirmations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 token_hash text NOT NULL UNIQUE, expires_at timestamptz NOT NULL, confirmed_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS valorapesquisa.commercial_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id),
 lead_id uuid, event_type text NOT NULL, source text NOT NULL DEFAULT 'public_portal', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 occurred_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_commercial_events_occurred ON valorapesquisa.commercial_events(occurred_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.lead_notes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid NOT NULL, author_user_id uuid REFERENCES valorapesquisa.users(id),
 note text NOT NULL CHECK(length(note) BETWEEN 1 AND 4000), created_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS valorapesquisa.onboarding_states (
 organization_id uuid PRIMARY KEY REFERENCES valorapesquisa.organizations(id), status text NOT NULL DEFAULT 'pending',
 current_step text NOT NULL DEFAULT 'organization_profile', completed_at timestamptz, updated_at timestamptz NOT NULL DEFAULT now());

INSERT INTO valorapesquisa.permissions(code,name,module_code,status)
SELECT code,name,'commercial','active' FROM (VALUES
 ('leads.read','Consultar leads'),('leads.manage','Gerenciar leads'),
 ('trials.read','Consultar trials'),('trials.manage','Gerenciar trials'),
 ('commercial.read','Consultar operação comercial'),('commercial.manage','Gerenciar operação comercial'),
 ('onboarding.read','Consultar onboarding'),('onboarding.manage','Gerenciar onboarding')) p(code,name)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,module_code=EXCLUDED.module_code,status='active';

INSERT INTO valorapesquisa.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE lower(r.code)='admin_valora' AND r.organization_id IS NULL
AND p.code IN ('leads.read','leads.manage','trials.read','trials.manage','commercial.read','commercial.manage','onboarding.read','onboarding.manage')
ON CONFLICT DO NOTHING;
