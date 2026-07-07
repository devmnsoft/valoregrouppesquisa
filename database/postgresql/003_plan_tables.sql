CREATE TABLE IF NOT EXISTS valorapesquisa.plans (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, description text, monthly_price numeric(12,2) NOT NULL DEFAULT 0, annual_price numeric(12,2) NOT NULL DEFAULT 0, display_order int NOT NULL DEFAULT 0, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_limits (plan_id uuid PRIMARY KEY REFERENCES valorapesquisa.plans(id) ON DELETE CASCADE, active_surveys int NOT NULL DEFAULT 0, responses_per_month int NOT NULL DEFAULT 0, users int NOT NULL DEFAULT 0, managers int NOT NULL DEFAULT 0, forms int NOT NULL DEFAULT 0, public_links int NOT NULL DEFAULT 0, email_invites_per_month int NOT NULL DEFAULT 0, storage_mb int NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_capabilities (plan_id uuid NOT NULL REFERENCES valorapesquisa.plans(id) ON DELETE CASCADE, capability_code text NOT NULL, enabled boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(plan_id, capability_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.subscriptions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL UNIQUE REFERENCES valorapesquisa.organizations(id), plan_id uuid NOT NULL REFERENCES valorapesquisa.plans(id), status text NOT NULL DEFAULT 'active', started_at timestamptz NOT NULL DEFAULT now(), trial_ends_at timestamptz, cancelled_at timestamptz, billing_status text NOT NULL DEFAULT 'ok', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS valorapesquisa.usage_monthly (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), period_month date NOT NULL, metric_key text NOT NULL, metric_value int NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), created_by uuid, updated_by uuid, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(organization_id, period_month, metric_key));


-- COMPATIBILIDADE PARA BANCOS EXISTENTES
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS monthly_price numeric(12,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS annual_price numeric(12,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS display_order int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
CREATE UNIQUE INDEX IF NOT EXISTS ux_plans_code ON valorapesquisa.plans(code);
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS active_surveys int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS responses_per_month int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS users int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS managers int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS forms int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS public_links int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS email_invites_per_month int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS storage_mb int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.plan_limits ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS capability_code text;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS enabled boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
