-- Sprint 64 complete local bootstrap. Idempotent; do not use as destructive migration.

CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE EXTENSION IF NOT EXISTS citext;

CREATE OR REPLACE FUNCTION valorapesquisa.set_updated_at() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN NEW.updated_at = now(); RETURN NEW; END; $$;

CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (version text primary key, applied_at timestamptz not null default now());

CREATE TABLE IF NOT EXISTS valorapesquisa.organizations (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    name text not null, public_name text, slug citext not null unique, document text, email citext, phone text, status text not null default 'active', plan_id text null, settings_json jsonb not null default '{}'::jsonb, brand_json jsonb not null default '{}'::jsonb, company_id uuid null
);

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_settings (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null unique, settings jsonb not null default '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_branding (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, logo_url text, primary_color text, custom_domain text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.units (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, company_id uuid null, name text not null, slug citext, status text not null default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.departments (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, unit_id uuid null, name text not null, status text not null default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.employees (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, department_id uuid null, name text not null, email citext, status text not null default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.roles (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    code citext not null unique, name text not null, description text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.permissions (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    code citext not null unique, name text not null, description text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.role_permissions (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    role_id uuid not null, permission_id uuid not null, unique(role_id,permission_id)
);

CREATE TABLE IF NOT EXISTS valorapesquisa.users (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, name text null, email citext not null unique, password_hash text null, role text not null default 'empresa_admin', role_id uuid null, status text not null default 'active', phone text null, last_login_at timestamptz null
);

CREATE TABLE IF NOT EXISTS valorapesquisa.user_profiles (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    user_id uuid not null unique, display_name text, phone text, locale text default 'pt-BR'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.user_sessions (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    user_id uuid not null, refresh_token_hash text, status text not null default 'active', expires_at timestamptz
);

CREATE TABLE IF NOT EXISTS valorapesquisa.password_reset_tokens (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    user_id uuid not null, token_hash text not null, status text not null default 'active', expires_at timestamptz not null
);

CREATE TABLE IF NOT EXISTS valorapesquisa.plans (

    id text primary key,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    name text not null, badge text null, public_subtitle text null, description text null, price_label text not null default '', price_complement text null, cta_label text null, display_order int not null default 0, highlight boolean not null default false, recommended boolean not null default false, visible_on_public_pricing boolean not null default true, internal_only boolean not null default false, status text not null default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.plan_limits (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    plan_id text not null, limit_key text not null, limit_value int not null default 0, unique(plan_id,limit_key)
);

CREATE TABLE IF NOT EXISTS valorapesquisa.plan_capabilities (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    plan_id text not null, capability_key text not null, capability_level text not null default 'enabled', capability_type text not null default 'feature', unique(plan_id,capability_key)
);

CREATE TABLE IF NOT EXISTS valorapesquisa.subscriptions (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, plan_id text not null, status text not null default 'active', started_at timestamptz default now(), trial_ends_at timestamptz, cancelled_at timestamptz, billing_status text not null default 'ok'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.usage_monthly (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, month date not null, responses_count int not null default 0, active_surveys_count int not null default 0, unique(organization_id,month)
);

CREATE TABLE IF NOT EXISTS valorapesquisa.modules (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    code citext not null unique, name text not null, description text, status text not null default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_modules (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, module_id uuid not null, enabled boolean not null default true, unique(organization_id,module_id)
);

CREATE TABLE IF NOT EXISTS valorapesquisa.forms (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, title text not null, slug citext, description text, status text not null default 'active', min_score int, max_score int, score_ranges jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.form_dimensions (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    form_id uuid not null, name text not null, position int not null, weight numeric(8,2) default 1
);

CREATE TABLE IF NOT EXISTS valorapesquisa.questions (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    form_id uuid not null, dimension_id uuid null, text text not null, position int not null, type text not null default 'scale', min_value int default 1, max_value int default 5, required boolean default true
);

CREATE TABLE IF NOT EXISTS valorapesquisa.question_options (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    question_id uuid not null, label text not null, value int not null, position int not null
);

CREATE TABLE IF NOT EXISTS valorapesquisa.surveys (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, form_id uuid not null, title text not null, description text, status text not null default 'active', starts_at timestamptz, expires_at timestamptz, token_hash text, is_free boolean default false, featured_on_home boolean default false, visible_on_home boolean default false, plan_id text null, revoked_at timestamptz null
);

CREATE TABLE IF NOT EXISTS valorapesquisa.survey_links (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    survey_id uuid not null, token_hash text not null, public_token_hash text, public_url text, status text not null default 'active', starts_at timestamptz, expires_at timestamptz, revoked_at timestamptz null
);

CREATE TABLE IF NOT EXISTS valorapesquisa.survey_invites (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    survey_id uuid not null, email citext not null, token_hash text, status text not null default 'pending'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.survey_participants (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    survey_id uuid not null, employee_id uuid null, name text, email citext, status text not null default 'invited'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.responses (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid not null, survey_id uuid not null, form_id uuid not null, participant_name text, participant_email citext, status text not null default 'completed', completed_at timestamptz, result_token_hash text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.response_answers (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    response_id uuid not null, question_id uuid not null, dimension_id uuid null, answer_value int, answer_text text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.result_scores (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    response_id uuid not null unique, total_score int not null, max_score int, level text, result_json jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.dimension_scores (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    response_id uuid not null, dimension_id uuid null, dimension_name text, score int, max_score int
);

CREATE TABLE IF NOT EXISTS valorapesquisa.certificates (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    response_id uuid not null, survey_id uuid, participant_name text, masked_email text, company_name text, total_score int, level text, validation_code citext not null unique, validation_url text, issued_at timestamptz default now(), payload jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.certificate_validations (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    certificate_id uuid not null, validation_code citext not null, status text not null default 'valid', validated_at timestamptz, validation_ip text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.communications (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, type text not null, subject text, body text, status text not null default 'draft'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.email_jobs (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, response_id uuid null, template_code text, from_email citext default 'valoragroup@mnsoft.com.br', to_email citext, subject text, body text, status text not null default 'queued', attempts int not null default 0, next_attempt_at timestamptz default now(), dead_letter_reason text
);

CREATE TABLE IF NOT EXISTS valorapesquisa.whatsapp_jobs (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, phone text, message text, status text not null default 'queued'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.email_templates (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    code citext not null unique, from_email citext not null, subject text not null, body text not null, status text not null default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.notifications (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, user_id uuid null, title text, message text, status text default 'unread'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.support_tickets (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, user_id uuid null, subject text not null, status text default 'open', priority text default 'normal'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.support_ticket_messages (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    ticket_id uuid not null, user_id uuid null, message text not null, is_internal boolean default false
);

CREATE TABLE IF NOT EXISTS valorapesquisa.audit_logs (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, user_id uuid null, action text not null, entity text, entity_id uuid, correlation_id text, metadata jsonb default '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.operational_logs (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    level text not null, message text not null, correlation_id text, metadata jsonb default '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.system_events (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    event_type text not null, status text default 'open', payload jsonb default '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.api_keys (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, name text not null, token_hash text not null, status text default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.webhooks (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, url text not null, secret_hash text, status text default 'active'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.integration_events (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, provider text, event_type text, status text, payload jsonb default '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.import_batches (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, source text, status text default 'pending'
);

CREATE TABLE IF NOT EXISTS valorapesquisa.import_errors (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    batch_id uuid not null, row_number int, message text, payload jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.compare_reports (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    organization_id uuid null, title text, status text default 'ready', payload jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.backup_runs (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    status text not null default 'pending', started_at timestamptz default now(), finished_at timestamptz, details jsonb
);

CREATE TABLE IF NOT EXISTS valorapesquisa.repair_runs (

    id uuid primary key default gen_random_uuid(),
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    created_by uuid null,
    updated_by uuid null,
    is_deleted boolean not null default false,
    deleted_at timestamptz null,
    deleted_by uuid null,
    operation text not null, status text not null default 'pending', started_at timestamptz default now(), finished_at timestamptz, details jsonb
);

DROP TRIGGER IF EXISTS trg_organizations_updated_at ON valorapesquisa.organizations; CREATE TRIGGER trg_organizations_updated_at BEFORE UPDATE ON valorapesquisa.organizations FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_organization_settings_updated_at ON valorapesquisa.organization_settings; CREATE TRIGGER trg_organization_settings_updated_at BEFORE UPDATE ON valorapesquisa.organization_settings FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_organization_branding_updated_at ON valorapesquisa.organization_branding; CREATE TRIGGER trg_organization_branding_updated_at BEFORE UPDATE ON valorapesquisa.organization_branding FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_units_updated_at ON valorapesquisa.units; CREATE TRIGGER trg_units_updated_at BEFORE UPDATE ON valorapesquisa.units FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_departments_updated_at ON valorapesquisa.departments; CREATE TRIGGER trg_departments_updated_at BEFORE UPDATE ON valorapesquisa.departments FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_employees_updated_at ON valorapesquisa.employees; CREATE TRIGGER trg_employees_updated_at BEFORE UPDATE ON valorapesquisa.employees FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_roles_updated_at ON valorapesquisa.roles; CREATE TRIGGER trg_roles_updated_at BEFORE UPDATE ON valorapesquisa.roles FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_permissions_updated_at ON valorapesquisa.permissions; CREATE TRIGGER trg_permissions_updated_at BEFORE UPDATE ON valorapesquisa.permissions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_role_permissions_updated_at ON valorapesquisa.role_permissions; CREATE TRIGGER trg_role_permissions_updated_at BEFORE UPDATE ON valorapesquisa.role_permissions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_users_updated_at ON valorapesquisa.users; CREATE TRIGGER trg_users_updated_at BEFORE UPDATE ON valorapesquisa.users FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_user_profiles_updated_at ON valorapesquisa.user_profiles; CREATE TRIGGER trg_user_profiles_updated_at BEFORE UPDATE ON valorapesquisa.user_profiles FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_user_sessions_updated_at ON valorapesquisa.user_sessions; CREATE TRIGGER trg_user_sessions_updated_at BEFORE UPDATE ON valorapesquisa.user_sessions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_password_reset_tokens_updated_at ON valorapesquisa.password_reset_tokens; CREATE TRIGGER trg_password_reset_tokens_updated_at BEFORE UPDATE ON valorapesquisa.password_reset_tokens FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_plans_updated_at ON valorapesquisa.plans; CREATE TRIGGER trg_plans_updated_at BEFORE UPDATE ON valorapesquisa.plans FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_plan_limits_updated_at ON valorapesquisa.plan_limits; CREATE TRIGGER trg_plan_limits_updated_at BEFORE UPDATE ON valorapesquisa.plan_limits FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_plan_capabilities_updated_at ON valorapesquisa.plan_capabilities; CREATE TRIGGER trg_plan_capabilities_updated_at BEFORE UPDATE ON valorapesquisa.plan_capabilities FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_subscriptions_updated_at ON valorapesquisa.subscriptions; CREATE TRIGGER trg_subscriptions_updated_at BEFORE UPDATE ON valorapesquisa.subscriptions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_usage_monthly_updated_at ON valorapesquisa.usage_monthly; CREATE TRIGGER trg_usage_monthly_updated_at BEFORE UPDATE ON valorapesquisa.usage_monthly FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_modules_updated_at ON valorapesquisa.modules; CREATE TRIGGER trg_modules_updated_at BEFORE UPDATE ON valorapesquisa.modules FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_organization_modules_updated_at ON valorapesquisa.organization_modules; CREATE TRIGGER trg_organization_modules_updated_at BEFORE UPDATE ON valorapesquisa.organization_modules FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_forms_updated_at ON valorapesquisa.forms; CREATE TRIGGER trg_forms_updated_at BEFORE UPDATE ON valorapesquisa.forms FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_form_dimensions_updated_at ON valorapesquisa.form_dimensions; CREATE TRIGGER trg_form_dimensions_updated_at BEFORE UPDATE ON valorapesquisa.form_dimensions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_questions_updated_at ON valorapesquisa.questions; CREATE TRIGGER trg_questions_updated_at BEFORE UPDATE ON valorapesquisa.questions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_question_options_updated_at ON valorapesquisa.question_options; CREATE TRIGGER trg_question_options_updated_at BEFORE UPDATE ON valorapesquisa.question_options FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_surveys_updated_at ON valorapesquisa.surveys; CREATE TRIGGER trg_surveys_updated_at BEFORE UPDATE ON valorapesquisa.surveys FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_survey_links_updated_at ON valorapesquisa.survey_links; CREATE TRIGGER trg_survey_links_updated_at BEFORE UPDATE ON valorapesquisa.survey_links FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_survey_invites_updated_at ON valorapesquisa.survey_invites; CREATE TRIGGER trg_survey_invites_updated_at BEFORE UPDATE ON valorapesquisa.survey_invites FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_survey_participants_updated_at ON valorapesquisa.survey_participants; CREATE TRIGGER trg_survey_participants_updated_at BEFORE UPDATE ON valorapesquisa.survey_participants FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_responses_updated_at ON valorapesquisa.responses; CREATE TRIGGER trg_responses_updated_at BEFORE UPDATE ON valorapesquisa.responses FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_response_answers_updated_at ON valorapesquisa.response_answers; CREATE TRIGGER trg_response_answers_updated_at BEFORE UPDATE ON valorapesquisa.response_answers FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_result_scores_updated_at ON valorapesquisa.result_scores; CREATE TRIGGER trg_result_scores_updated_at BEFORE UPDATE ON valorapesquisa.result_scores FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_dimension_scores_updated_at ON valorapesquisa.dimension_scores; CREATE TRIGGER trg_dimension_scores_updated_at BEFORE UPDATE ON valorapesquisa.dimension_scores FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_certificates_updated_at ON valorapesquisa.certificates; CREATE TRIGGER trg_certificates_updated_at BEFORE UPDATE ON valorapesquisa.certificates FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_certificate_validations_updated_at ON valorapesquisa.certificate_validations; CREATE TRIGGER trg_certificate_validations_updated_at BEFORE UPDATE ON valorapesquisa.certificate_validations FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_communications_updated_at ON valorapesquisa.communications; CREATE TRIGGER trg_communications_updated_at BEFORE UPDATE ON valorapesquisa.communications FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_email_jobs_updated_at ON valorapesquisa.email_jobs; CREATE TRIGGER trg_email_jobs_updated_at BEFORE UPDATE ON valorapesquisa.email_jobs FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_whatsapp_jobs_updated_at ON valorapesquisa.whatsapp_jobs; CREATE TRIGGER trg_whatsapp_jobs_updated_at BEFORE UPDATE ON valorapesquisa.whatsapp_jobs FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_email_templates_updated_at ON valorapesquisa.email_templates; CREATE TRIGGER trg_email_templates_updated_at BEFORE UPDATE ON valorapesquisa.email_templates FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_notifications_updated_at ON valorapesquisa.notifications; CREATE TRIGGER trg_notifications_updated_at BEFORE UPDATE ON valorapesquisa.notifications FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_support_tickets_updated_at ON valorapesquisa.support_tickets; CREATE TRIGGER trg_support_tickets_updated_at BEFORE UPDATE ON valorapesquisa.support_tickets FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_support_ticket_messages_updated_at ON valorapesquisa.support_ticket_messages; CREATE TRIGGER trg_support_ticket_messages_updated_at BEFORE UPDATE ON valorapesquisa.support_ticket_messages FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_audit_logs_updated_at ON valorapesquisa.audit_logs; CREATE TRIGGER trg_audit_logs_updated_at BEFORE UPDATE ON valorapesquisa.audit_logs FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_operational_logs_updated_at ON valorapesquisa.operational_logs; CREATE TRIGGER trg_operational_logs_updated_at BEFORE UPDATE ON valorapesquisa.operational_logs FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_system_events_updated_at ON valorapesquisa.system_events; CREATE TRIGGER trg_system_events_updated_at BEFORE UPDATE ON valorapesquisa.system_events FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_api_keys_updated_at ON valorapesquisa.api_keys; CREATE TRIGGER trg_api_keys_updated_at BEFORE UPDATE ON valorapesquisa.api_keys FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_webhooks_updated_at ON valorapesquisa.webhooks; CREATE TRIGGER trg_webhooks_updated_at BEFORE UPDATE ON valorapesquisa.webhooks FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_integration_events_updated_at ON valorapesquisa.integration_events; CREATE TRIGGER trg_integration_events_updated_at BEFORE UPDATE ON valorapesquisa.integration_events FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_import_batches_updated_at ON valorapesquisa.import_batches; CREATE TRIGGER trg_import_batches_updated_at BEFORE UPDATE ON valorapesquisa.import_batches FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_import_errors_updated_at ON valorapesquisa.import_errors; CREATE TRIGGER trg_import_errors_updated_at BEFORE UPDATE ON valorapesquisa.import_errors FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_compare_reports_updated_at ON valorapesquisa.compare_reports; CREATE TRIGGER trg_compare_reports_updated_at BEFORE UPDATE ON valorapesquisa.compare_reports FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_backup_runs_updated_at ON valorapesquisa.backup_runs; CREATE TRIGGER trg_backup_runs_updated_at BEFORE UPDATE ON valorapesquisa.backup_runs FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

DROP TRIGGER IF EXISTS trg_repair_runs_updated_at ON valorapesquisa.repair_runs; CREATE TRIGGER trg_repair_runs_updated_at BEFORE UPDATE ON valorapesquisa.repair_runs FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();

CREATE INDEX IF NOT EXISTS ix_organizations_company_id ON valorapesquisa.organizations (company_id);

CREATE INDEX IF NOT EXISTS ix_organizations_slug ON valorapesquisa.organizations (slug);

CREATE INDEX IF NOT EXISTS ix_organizations_status ON valorapesquisa.organizations (status);

CREATE INDEX IF NOT EXISTS ix_organizations_created_at ON valorapesquisa.organizations (created_at);

CREATE INDEX IF NOT EXISTS ix_organization_settings_organization_id ON valorapesquisa.organization_settings (organization_id);

CREATE INDEX IF NOT EXISTS ix_organization_settings_created_at ON valorapesquisa.organization_settings (created_at);

CREATE INDEX IF NOT EXISTS ix_organization_branding_organization_id ON valorapesquisa.organization_branding (organization_id);

CREATE INDEX IF NOT EXISTS ix_organization_branding_created_at ON valorapesquisa.organization_branding (created_at);

CREATE INDEX IF NOT EXISTS ix_units_organization_id ON valorapesquisa.units (organization_id);

CREATE INDEX IF NOT EXISTS ix_units_company_id ON valorapesquisa.units (company_id);

CREATE INDEX IF NOT EXISTS ix_units_slug ON valorapesquisa.units (slug);

CREATE INDEX IF NOT EXISTS ix_units_status ON valorapesquisa.units (status);

CREATE INDEX IF NOT EXISTS ix_units_created_at ON valorapesquisa.units (created_at);

CREATE INDEX IF NOT EXISTS ix_departments_organization_id ON valorapesquisa.departments (organization_id);

CREATE INDEX IF NOT EXISTS ix_departments_status ON valorapesquisa.departments (status);

CREATE INDEX IF NOT EXISTS ix_departments_created_at ON valorapesquisa.departments (created_at);

CREATE INDEX IF NOT EXISTS ix_employees_organization_id ON valorapesquisa.employees (organization_id);

CREATE INDEX IF NOT EXISTS ix_employees_email ON valorapesquisa.employees (email);

CREATE INDEX IF NOT EXISTS ix_employees_status ON valorapesquisa.employees (status);

CREATE INDEX IF NOT EXISTS ix_employees_created_at ON valorapesquisa.employees (created_at);

CREATE INDEX IF NOT EXISTS ix_roles_created_at ON valorapesquisa.roles (created_at);

CREATE INDEX IF NOT EXISTS ix_permissions_created_at ON valorapesquisa.permissions (created_at);

CREATE INDEX IF NOT EXISTS ix_role_permissions_created_at ON valorapesquisa.role_permissions (created_at);

CREATE INDEX IF NOT EXISTS ix_users_organization_id ON valorapesquisa.users (organization_id);

CREATE INDEX IF NOT EXISTS ix_users_email ON valorapesquisa.users (email);

CREATE INDEX IF NOT EXISTS ix_users_status ON valorapesquisa.users (status);

CREATE INDEX IF NOT EXISTS ix_users_created_at ON valorapesquisa.users (created_at);

CREATE INDEX IF NOT EXISTS ix_user_profiles_user_id ON valorapesquisa.user_profiles (user_id);

CREATE INDEX IF NOT EXISTS ix_user_profiles_created_at ON valorapesquisa.user_profiles (created_at);

CREATE INDEX IF NOT EXISTS ix_user_sessions_user_id ON valorapesquisa.user_sessions (user_id);

CREATE INDEX IF NOT EXISTS ix_user_sessions_status ON valorapesquisa.user_sessions (status);

CREATE INDEX IF NOT EXISTS ix_user_sessions_created_at ON valorapesquisa.user_sessions (created_at);

CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_user_id ON valorapesquisa.password_reset_tokens (user_id);

CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_status ON valorapesquisa.password_reset_tokens (status);

CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_created_at ON valorapesquisa.password_reset_tokens (created_at);

CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_token_hash ON valorapesquisa.password_reset_tokens (token_hash);

CREATE INDEX IF NOT EXISTS ix_plans_status ON valorapesquisa.plans (status);

CREATE INDEX IF NOT EXISTS ix_plans_created_at ON valorapesquisa.plans (created_at);

CREATE INDEX IF NOT EXISTS ix_plan_limits_created_at ON valorapesquisa.plan_limits (created_at);

CREATE INDEX IF NOT EXISTS ix_plan_capabilities_created_at ON valorapesquisa.plan_capabilities (created_at);

CREATE INDEX IF NOT EXISTS ix_subscriptions_organization_id ON valorapesquisa.subscriptions (organization_id);

CREATE INDEX IF NOT EXISTS ix_subscriptions_status ON valorapesquisa.subscriptions (status);

CREATE INDEX IF NOT EXISTS ix_subscriptions_created_at ON valorapesquisa.subscriptions (created_at);

CREATE INDEX IF NOT EXISTS ix_usage_monthly_organization_id ON valorapesquisa.usage_monthly (organization_id);

CREATE INDEX IF NOT EXISTS ix_usage_monthly_created_at ON valorapesquisa.usage_monthly (created_at);

CREATE INDEX IF NOT EXISTS ix_modules_status ON valorapesquisa.modules (status);

CREATE INDEX IF NOT EXISTS ix_modules_created_at ON valorapesquisa.modules (created_at);

CREATE INDEX IF NOT EXISTS ix_organization_modules_organization_id ON valorapesquisa.organization_modules (organization_id);

CREATE INDEX IF NOT EXISTS ix_organization_modules_created_at ON valorapesquisa.organization_modules (created_at);

CREATE INDEX IF NOT EXISTS ix_forms_organization_id ON valorapesquisa.forms (organization_id);

CREATE INDEX IF NOT EXISTS ix_forms_slug ON valorapesquisa.forms (slug);

CREATE INDEX IF NOT EXISTS ix_forms_status ON valorapesquisa.forms (status);

CREATE INDEX IF NOT EXISTS ix_forms_created_at ON valorapesquisa.forms (created_at);

CREATE INDEX IF NOT EXISTS ix_form_dimensions_form_id ON valorapesquisa.form_dimensions (form_id);

CREATE INDEX IF NOT EXISTS ix_form_dimensions_created_at ON valorapesquisa.form_dimensions (created_at);

CREATE INDEX IF NOT EXISTS ix_questions_form_id ON valorapesquisa.questions (form_id);

CREATE INDEX IF NOT EXISTS ix_questions_created_at ON valorapesquisa.questions (created_at);

CREATE INDEX IF NOT EXISTS ix_question_options_created_at ON valorapesquisa.question_options (created_at);

CREATE INDEX IF NOT EXISTS ix_surveys_organization_id ON valorapesquisa.surveys (organization_id);

CREATE INDEX IF NOT EXISTS ix_surveys_form_id ON valorapesquisa.surveys (form_id);

CREATE INDEX IF NOT EXISTS ix_surveys_status ON valorapesquisa.surveys (status);

CREATE INDEX IF NOT EXISTS ix_surveys_created_at ON valorapesquisa.surveys (created_at);

CREATE INDEX IF NOT EXISTS ix_surveys_token_hash ON valorapesquisa.surveys (token_hash);

CREATE INDEX IF NOT EXISTS ix_survey_links_survey_id ON valorapesquisa.survey_links (survey_id);

CREATE INDEX IF NOT EXISTS ix_survey_links_status ON valorapesquisa.survey_links (status);

CREATE INDEX IF NOT EXISTS ix_survey_links_created_at ON valorapesquisa.survey_links (created_at);

CREATE INDEX IF NOT EXISTS ix_survey_links_token_hash ON valorapesquisa.survey_links (token_hash);

CREATE INDEX IF NOT EXISTS ix_survey_links_public_token_hash ON valorapesquisa.survey_links (public_token_hash);

CREATE INDEX IF NOT EXISTS ix_survey_invites_survey_id ON valorapesquisa.survey_invites (survey_id);

CREATE INDEX IF NOT EXISTS ix_survey_invites_email ON valorapesquisa.survey_invites (email);

CREATE INDEX IF NOT EXISTS ix_survey_invites_status ON valorapesquisa.survey_invites (status);

CREATE INDEX IF NOT EXISTS ix_survey_invites_created_at ON valorapesquisa.survey_invites (created_at);

CREATE INDEX IF NOT EXISTS ix_survey_invites_token_hash ON valorapesquisa.survey_invites (token_hash);

CREATE INDEX IF NOT EXISTS ix_survey_participants_survey_id ON valorapesquisa.survey_participants (survey_id);

CREATE INDEX IF NOT EXISTS ix_survey_participants_email ON valorapesquisa.survey_participants (email);

CREATE INDEX IF NOT EXISTS ix_survey_participants_status ON valorapesquisa.survey_participants (status);

CREATE INDEX IF NOT EXISTS ix_survey_participants_created_at ON valorapesquisa.survey_participants (created_at);

CREATE INDEX IF NOT EXISTS ix_responses_organization_id ON valorapesquisa.responses (organization_id);

CREATE INDEX IF NOT EXISTS ix_responses_survey_id ON valorapesquisa.responses (survey_id);

CREATE INDEX IF NOT EXISTS ix_responses_form_id ON valorapesquisa.responses (form_id);

CREATE INDEX IF NOT EXISTS ix_responses_status ON valorapesquisa.responses (status);

CREATE INDEX IF NOT EXISTS ix_responses_created_at ON valorapesquisa.responses (created_at);

CREATE INDEX IF NOT EXISTS ix_response_answers_response_id ON valorapesquisa.response_answers (response_id);

CREATE INDEX IF NOT EXISTS ix_response_answers_created_at ON valorapesquisa.response_answers (created_at);

CREATE INDEX IF NOT EXISTS ix_result_scores_response_id ON valorapesquisa.result_scores (response_id);

CREATE INDEX IF NOT EXISTS ix_result_scores_created_at ON valorapesquisa.result_scores (created_at);

CREATE INDEX IF NOT EXISTS ix_dimension_scores_response_id ON valorapesquisa.dimension_scores (response_id);

CREATE INDEX IF NOT EXISTS ix_dimension_scores_created_at ON valorapesquisa.dimension_scores (created_at);

CREATE INDEX IF NOT EXISTS ix_certificates_survey_id ON valorapesquisa.certificates (survey_id);

CREATE INDEX IF NOT EXISTS ix_certificates_response_id ON valorapesquisa.certificates (response_id);

CREATE INDEX IF NOT EXISTS ix_certificates_created_at ON valorapesquisa.certificates (created_at);

CREATE INDEX IF NOT EXISTS ix_certificates_validation_code ON valorapesquisa.certificates (validation_code);

CREATE INDEX IF NOT EXISTS ix_certificate_validations_status ON valorapesquisa.certificate_validations (status);

CREATE INDEX IF NOT EXISTS ix_certificate_validations_created_at ON valorapesquisa.certificate_validations (created_at);

CREATE INDEX IF NOT EXISTS ix_certificate_validations_validation_code ON valorapesquisa.certificate_validations (validation_code);

CREATE INDEX IF NOT EXISTS ix_communications_organization_id ON valorapesquisa.communications (organization_id);

CREATE INDEX IF NOT EXISTS ix_communications_status ON valorapesquisa.communications (status);

CREATE INDEX IF NOT EXISTS ix_communications_created_at ON valorapesquisa.communications (created_at);

CREATE INDEX IF NOT EXISTS ix_email_jobs_organization_id ON valorapesquisa.email_jobs (organization_id);

CREATE INDEX IF NOT EXISTS ix_email_jobs_response_id ON valorapesquisa.email_jobs (response_id);

CREATE INDEX IF NOT EXISTS ix_email_jobs_status ON valorapesquisa.email_jobs (status);

CREATE INDEX IF NOT EXISTS ix_email_jobs_created_at ON valorapesquisa.email_jobs (created_at);

CREATE INDEX IF NOT EXISTS ix_whatsapp_jobs_organization_id ON valorapesquisa.whatsapp_jobs (organization_id);

CREATE INDEX IF NOT EXISTS ix_whatsapp_jobs_status ON valorapesquisa.whatsapp_jobs (status);

CREATE INDEX IF NOT EXISTS ix_whatsapp_jobs_created_at ON valorapesquisa.whatsapp_jobs (created_at);

CREATE INDEX IF NOT EXISTS ix_email_templates_status ON valorapesquisa.email_templates (status);

CREATE INDEX IF NOT EXISTS ix_email_templates_created_at ON valorapesquisa.email_templates (created_at);

CREATE INDEX IF NOT EXISTS ix_notifications_organization_id ON valorapesquisa.notifications (organization_id);

CREATE INDEX IF NOT EXISTS ix_notifications_user_id ON valorapesquisa.notifications (user_id);

CREATE INDEX IF NOT EXISTS ix_notifications_status ON valorapesquisa.notifications (status);

CREATE INDEX IF NOT EXISTS ix_notifications_created_at ON valorapesquisa.notifications (created_at);

CREATE INDEX IF NOT EXISTS ix_support_tickets_organization_id ON valorapesquisa.support_tickets (organization_id);

CREATE INDEX IF NOT EXISTS ix_support_tickets_user_id ON valorapesquisa.support_tickets (user_id);

CREATE INDEX IF NOT EXISTS ix_support_tickets_status ON valorapesquisa.support_tickets (status);

CREATE INDEX IF NOT EXISTS ix_support_tickets_created_at ON valorapesquisa.support_tickets (created_at);

CREATE INDEX IF NOT EXISTS ix_support_ticket_messages_user_id ON valorapesquisa.support_ticket_messages (user_id);

CREATE INDEX IF NOT EXISTS ix_support_ticket_messages_created_at ON valorapesquisa.support_ticket_messages (created_at);

CREATE INDEX IF NOT EXISTS ix_audit_logs_organization_id ON valorapesquisa.audit_logs (organization_id);

CREATE INDEX IF NOT EXISTS ix_audit_logs_user_id ON valorapesquisa.audit_logs (user_id);

CREATE INDEX IF NOT EXISTS ix_audit_logs_created_at ON valorapesquisa.audit_logs (created_at);

CREATE INDEX IF NOT EXISTS ix_operational_logs_created_at ON valorapesquisa.operational_logs (created_at);

CREATE INDEX IF NOT EXISTS ix_system_events_status ON valorapesquisa.system_events (status);

CREATE INDEX IF NOT EXISTS ix_system_events_created_at ON valorapesquisa.system_events (created_at);

CREATE INDEX IF NOT EXISTS ix_api_keys_organization_id ON valorapesquisa.api_keys (organization_id);

CREATE INDEX IF NOT EXISTS ix_api_keys_status ON valorapesquisa.api_keys (status);

CREATE INDEX IF NOT EXISTS ix_api_keys_created_at ON valorapesquisa.api_keys (created_at);

CREATE INDEX IF NOT EXISTS ix_api_keys_token_hash ON valorapesquisa.api_keys (token_hash);

CREATE INDEX IF NOT EXISTS ix_webhooks_organization_id ON valorapesquisa.webhooks (organization_id);

CREATE INDEX IF NOT EXISTS ix_webhooks_status ON valorapesquisa.webhooks (status);

CREATE INDEX IF NOT EXISTS ix_webhooks_created_at ON valorapesquisa.webhooks (created_at);

CREATE INDEX IF NOT EXISTS ix_integration_events_organization_id ON valorapesquisa.integration_events (organization_id);

CREATE INDEX IF NOT EXISTS ix_integration_events_status ON valorapesquisa.integration_events (status);

CREATE INDEX IF NOT EXISTS ix_integration_events_created_at ON valorapesquisa.integration_events (created_at);

CREATE INDEX IF NOT EXISTS ix_import_batches_organization_id ON valorapesquisa.import_batches (organization_id);

CREATE INDEX IF NOT EXISTS ix_import_batches_status ON valorapesquisa.import_batches (status);

CREATE INDEX IF NOT EXISTS ix_import_batches_created_at ON valorapesquisa.import_batches (created_at);

CREATE INDEX IF NOT EXISTS ix_import_errors_created_at ON valorapesquisa.import_errors (created_at);

CREATE INDEX IF NOT EXISTS ix_compare_reports_organization_id ON valorapesquisa.compare_reports (organization_id);

CREATE INDEX IF NOT EXISTS ix_compare_reports_status ON valorapesquisa.compare_reports (status);

CREATE INDEX IF NOT EXISTS ix_compare_reports_created_at ON valorapesquisa.compare_reports (created_at);

CREATE INDEX IF NOT EXISTS ix_backup_runs_status ON valorapesquisa.backup_runs (status);

CREATE INDEX IF NOT EXISTS ix_backup_runs_created_at ON valorapesquisa.backup_runs (created_at);

CREATE INDEX IF NOT EXISTS ix_repair_runs_status ON valorapesquisa.repair_runs (status);

CREATE INDEX IF NOT EXISTS ix_repair_runs_created_at ON valorapesquisa.repair_runs (created_at);


INSERT INTO valorapesquisa.plans(id,name,description,price_label,display_order,status) VALUES
('free','Free','1 pesquisa ativa, 10 respostas/mês, 1 gestor, resultado básico, certificado simples, e-mail de resultado','Grátis',10,'active'),
('essential','Essential','3 pesquisas ativas, 150 respostas/mês, 2 gestores','Sob consulta',20,'active'),
('professional','Professional','12 pesquisas ativas, 1000 respostas/mês, 8 gestores','Sob consulta',30,'active'),
('corporate','Corporate','Pesquisas ilimitadas, 10000 respostas/mês, 50 gestores, múltiplas unidades, relatórios consolidados','Sob consulta',40,'active'),
('enterprise','Enterprise','Limites ilimitados, white label, múltiplas organizações, acompanhamento executivo','Sob consulta',50,'active')
ON CONFLICT (id) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,price_label=EXCLUDED.price_label,display_order=EXCLUDED.display_order,status=EXCLUDED.status,updated_at=now();
INSERT INTO valorapesquisa.plan_limits(plan_id,limit_key,limit_value) VALUES
('free','active_surveys',1),('free','responses_per_month',10),('free','managers',1),
('essential','active_surveys',3),('essential','responses_per_month',150),('essential','managers',2),
('professional','active_surveys',12),('professional','responses_per_month',1000),('professional','managers',8),
('corporate','active_surveys',2147483647),('corporate','responses_per_month',10000),('corporate','managers',50),
('enterprise','active_surveys',2147483647),('enterprise','responses_per_month',2147483647),('enterprise','managers',2147483647)
ON CONFLICT (plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value,updated_at=now();
INSERT INTO valorapesquisa.plan_capabilities(plan_id,capability_key,capability_level,capability_type) VALUES
('free','basic_result','enabled','feature'),('free','simple_certificate','enabled','feature'),('free','result_email','enabled','feature'),
('corporate','multi_units','enabled','feature'),('corporate','consolidated_reports','enabled','feature'),
('enterprise','white_label','enabled','feature'),('enterprise','multi_organizations','enabled','feature'),('enterprise','executive_followup','enabled','feature')
ON CONFLICT (plan_id,capability_key) DO UPDATE SET capability_level=EXCLUDED.capability_level,capability_type=EXCLUDED.capability_type,updated_at=now();
INSERT INTO valorapesquisa.organizations(name,public_name,slug,status,plan_id) VALUES ('Valora Group','Valora Group','valora','active','enterprise') ON CONFLICT (slug) DO UPDATE SET name=EXCLUDED.name,public_name=EXCLUDED.public_name,status='active',plan_id='enterprise',updated_at=now();


INSERT INTO valorapesquisa.modules(code,name) VALUES
('dashboard','dashboard'),
('clientes','clientes'),
('financeiro','financeiro'),
('planos','planos'),
('usuarios','usuarios'),
('funcionarios','funcionarios'),
('formularios','formularios'),
('pesquisas','pesquisas'),
('convitesEmail','convitesEmail'),
('respostas','respostas'),
('relatorios','relatorios'),
('certificados','certificados'),
('actionPlans','actionPlans'),
('valorabot','valorabot'),
('support','support'),
('lgpd','lgpd'),
('integrations','integrations'),
('exportacoes','exportacoes'),
('benchmark','benchmark'),
('whiteLabel','whiteLabel'),
('backup','backup'),
('logs','logs'),
('diagnosticosGratuitos','diagnosticosGratuitos'),
('operacaoAssistida','operacaoAssistida'),
('comunicacoes','comunicacoes') ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name,updated_at=now();

INSERT INTO valorapesquisa.roles(code,name) VALUES
('admin_valora','admin_valora'),
('consultor_valora','consultor_valora'),
('empresa_admin','empresa_admin'),
('gestor_pesquisa','gestor_pesquisa'),
('analista_resultados','analista_resultados'),
('gestor_area','gestor_area'),
('participante','participante'),
('convidado_externo','convidado_externo') ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name;

INSERT INTO valorapesquisa.permissions(code,name) VALUES
('canAccessPortal','canAccessPortal'),
('canManageCompanies','canManageCompanies'),
('canViewFinance','canViewFinance'),
('canManagePlans','canManagePlans'),
('canCreateSurveys','canCreateSurveys'),
('canCreateForms','canCreateForms'),
('canManageCompanyUsers','canManageCompanyUsers'),
('canViewResponses','canViewResponses'),
('canViewReports','canViewReports'),
('canSendInvites','canSendInvites'),
('canViewLogs','canViewLogs'),
('canBackup','canBackup'),
('canHandleSupport','canHandleSupport'),
('canManageGlobalSettings','canManageGlobalSettings'),
('canManageCertificates','canManageCertificates'),
('canManageCommunications','canManageCommunications'),
('canOperateRepairs','canOperateRepairs'),
('canViewDiagnostics','canViewDiagnostics') ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name;

INSERT INTO valorapesquisa.role_permissions(role_id,permission_id) SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p WHERE r.code='admin_valora' ON CONFLICT DO NOTHING;


INSERT INTO valorapesquisa.users(organization_id,name,email,password_hash,role,role_id,status)
SELECT o.id,'Administrador Valora','admin@valoragroup.local','$2a$11$xk8e7Bq6S9JKpClQ9Yz0IOO6fHy0p0c3Q2sFI6T5VxOoD5xvDeA5K',r.code,r.id,'active' FROM valorapesquisa.organizations o, valorapesquisa.roles r WHERE o.slug='valora' AND r.code='admin_valora'
ON CONFLICT (email) DO UPDATE SET status='active',role=EXCLUDED.role,role_id=EXCLUDED.role_id,updated_at=now();
-- Senha local temporária: Valora@123Local! Alterar imediatamente. Nunca usar em produção.



WITH org AS (SELECT id FROM valorapesquisa.organizations WHERE slug='valora'), f AS (
INSERT INTO valorapesquisa.forms(organization_id,title,slug,description,status,min_score,max_score,score_ranges)
SELECT id,'Valora Insight — Diagnóstico de Maturidade Empresarial','valora-insight','Diagnóstico oficial Valora Insight','active',25,125,'[{"label":"Crítico","min":25,"max":55},{"label":"Em estruturação","min":56,"max":85},{"label":"Estruturada","min":86,"max":110},{"label":"Alta maturidade","min":111,"max":125}]'::jsonb FROM org
ON CONFLICT DO NOTHING RETURNING id), form_id AS (SELECT id FROM f UNION SELECT id FROM valorapesquisa.forms WHERE slug='valora-insight' LIMIT 1), dims(name,pos) AS (VALUES ('Cultura e Propósito',1),('Gestão e Governança',2),('Liderança',3),('Pessoas e Talentos',4),('Resultados e Crescimento',5))
INSERT INTO valorapesquisa.form_dimensions(form_id,name,position) SELECT form_id.id,dims.name,dims.pos FROM form_id,dims ON CONFLICT DO NOTHING;
WITH fd AS (SELECT d.id,d.name,d.position,f.id form_id FROM valorapesquisa.form_dimensions d JOIN valorapesquisa.forms f ON f.id=d.form_id WHERE f.slug='valora-insight'), nums(n) AS (VALUES (1),(2),(3),(4),(5))
INSERT INTO valorapesquisa.questions(form_id,dimension_id,text,position,type,min_value,max_value,required)
SELECT fd.form_id,fd.id,fd.name || ' - pergunta ' || nums.n, ((fd.position-1)*5)+nums.n,'scale',1,5,true FROM fd,nums ON CONFLICT DO NOTHING;
WITH org AS (SELECT id FROM valorapesquisa.organizations WHERE slug='valora'), form_id AS (SELECT id FROM valorapesquisa.forms WHERE slug='valora-insight'), s AS (
INSERT INTO valorapesquisa.surveys(organization_id,form_id,title,description,status,expires_at,is_free,featured_on_home,visible_on_home,plan_id,token_hash)
SELECT org.id,form_id.id,'Diagnóstico gratuito Valora Insight','Pesquisa gratuita oficial','active','2036-12-31 23:59:59+00',true,true,true,'free',encode(digest('valora-local-dev-public-token','sha256'),'hex') FROM org,form_id ON CONFLICT DO NOTHING RETURNING id)
INSERT INTO valorapesquisa.survey_links(survey_id,token_hash,public_token_hash,public_url,status,expires_at)
SELECT id,encode(digest('valora-local-dev-public-token','sha256'),'hex'),encode(digest('valora-local-dev-public-token','sha256'),'hex'),'https://valoragroup.mnsoft.com.br/index.html?survey=' || id || '&token={publicToken}&org=valora','active','2036-12-31 23:59:59+00' FROM s ON CONFLICT DO NOTHING;


INSERT INTO valorapesquisa.email_templates(code,from_email,subject,body) VALUES
('result_email','valoragroup@mnsoft.com.br','result_email','Template operacional result_email sem senha'),
('invite_email','valoragroup@mnsoft.com.br','invite_email','Template operacional invite_email sem senha'),
('certificate_email','valoragroup@mnsoft.com.br','certificate_email','Template operacional certificate_email sem senha'),
('test_email','valoragroup@mnsoft.com.br','test_email','Template operacional test_email sem senha'),
('operational_alert','valoragroup@mnsoft.com.br','operational_alert','Template operacional operational_alert sem senha') ON CONFLICT (code) DO UPDATE SET from_email=EXCLUDED.from_email,subject=EXCLUDED.subject,body=EXCLUDED.body,updated_at=now();

INSERT INTO valorapesquisa.schema_migrations(version) VALUES ('scriptbd_completo_sprint_64') ON CONFLICT DO NOTHING;
SET search_path TO valorapesquisa;
CREATE TABLE IF NOT EXISTS report_definitions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, description text, report_type text NOT NULL, required_module_code text NOT NULL DEFAULT 'relatorios', minimum_plan_code text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS generated_reports (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), survey_id uuid REFERENCES surveys(id), response_id uuid REFERENCES responses(id), report_definition_id uuid REFERENCES report_definitions(id), title text NOT NULL, format text NOT NULL CHECK (format IN ('html','json','csv','excel_compatible','pdf_pending')), status text NOT NULL DEFAULT 'generated', generated_by uuid, generated_at timestamptz, payload_json jsonb NOT NULL DEFAULT '{}'::jsonb, file_name text, mime_type text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS certificate_validations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), certificate_id uuid NOT NULL REFERENCES certificates(id), validation_code text NOT NULL, status text NOT NULL, validated_at timestamptz, validation_ip_hash text, user_agent text, created_at timestamptz NOT NULL DEFAULT now());
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS participant_email_masked text; ALTER TABLE certificates ADD COLUMN IF NOT EXISTS company_name text; ALTER TABLE certificates ADD COLUMN IF NOT EXISTS total_score numeric(10,2); ALTER TABLE certificates ADD COLUMN IF NOT EXISTS level text; ALTER TABLE certificates ADD COLUMN IF NOT EXISTS validation_url text; ALTER TABLE certificates ADD COLUMN IF NOT EXISTS payload_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE TABLE IF NOT EXISTS export_jobs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), requested_by uuid, entity text NOT NULL, format text NOT NULL CHECK (format IN ('csv','json','excel_compatible')), status text NOT NULL DEFAULT 'queued' CHECK (status IN ('queued','processing','completed','failed')), filter_json jsonb NOT NULL DEFAULT '{}'::jsonb, result_file_name text, result_mime_type text, result_payload text, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, error_message text);
CREATE TABLE IF NOT EXISTS lgpd_consents (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), survey_id uuid REFERENCES surveys(id), response_id uuid REFERENCES responses(id), participant_email_hash text NOT NULL, consent_text text NOT NULL, consent_version text NOT NULL, accepted boolean NOT NULL, accepted_at timestamptz, ip_hash text, user_agent text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS privacy_requests (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), requester_email_hash text NOT NULL, requester_email_masked text NOT NULL, request_type text NOT NULL CHECK (request_type IN ('data_export','anonymization','deletion','consent_revoke')), status text NOT NULL DEFAULT 'open' CHECK (status IN ('open','in_review','completed','rejected')), description text, response_id uuid REFERENCES responses(id), requested_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, handled_by uuid, result_json jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS email_templates (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), code text NOT NULL, name text NOT NULL, subject text NOT NULL, body_html text NOT NULL, body_text text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, is_deleted boolean NOT NULL DEFAULT false, UNIQUE(code, organization_id));
ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS certificate_id uuid; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS template_code text DEFAULT 'survey_invite'; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS from_email text DEFAULT 'no-reply@localhost'; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS from_name text DEFAULT 'Valora'; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS to_email_masked text DEFAULT '***'; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS to_email_encrypted_or_plain_dev text; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS body_html text; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS body_text text; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS next_attempt_at timestamptz; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS last_attempt_at timestamptz; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS sent_at timestamptz; ALTER TABLE email_jobs ADD COLUMN IF NOT EXISTS dead_letter_reason text;
CREATE INDEX IF NOT EXISTS idx_report_definitions_code ON report_definitions(code); CREATE INDEX IF NOT EXISTS idx_generated_reports_org ON generated_reports(organization_id); CREATE INDEX IF NOT EXISTS idx_certificates_validation_code ON certificates(validation_code); CREATE INDEX IF NOT EXISTS idx_export_jobs_org ON export_jobs(organization_id); CREATE INDEX IF NOT EXISTS idx_lgpd_consents_org ON lgpd_consents(organization_id); CREATE INDEX IF NOT EXISTS idx_privacy_requests_org ON privacy_requests(organization_id); CREATE INDEX IF NOT EXISTS idx_email_templates_code ON email_templates(code); CREATE INDEX IF NOT EXISTS idx_email_jobs_status ON email_jobs(status);
CREATE INDEX IF NOT EXISTS idx_plans_code ON plans(code); CREATE INDEX IF NOT EXISTS idx_plans_status ON plans(status); CREATE INDEX IF NOT EXISTS idx_modules_code ON modules(code); CREATE INDEX IF NOT EXISTS idx_modules_status ON modules(status); CREATE INDEX IF NOT EXISTS idx_organization_modules_organization_id ON organization_modules(organization_id); CREATE INDEX IF NOT EXISTS idx_subscriptions_organization_id ON subscriptions(organization_id); CREATE INDEX IF NOT EXISTS idx_subscriptions_status ON subscriptions(status); CREATE INDEX IF NOT EXISTS idx_usage_monthly_organization_month ON usage_monthly(organization_id, month);
INSERT INTO report_definitions(code,name,description,report_type,required_module_code,minimum_plan_code) VALUES ('response_summary','Relatório por resposta','Resumo seguro por resposta','response','relatorios','free'),('survey_summary','Relatório por pesquisa','Resumo agregado por pesquisa','survey','relatorios','essential'),('organization_executive','Relatório executivo','Resumo executivo da organização','organization','relatorios','growth') ON CONFLICT (code) DO NOTHING;
INSERT INTO email_templates(code,name,subject,body_html,body_text,status) VALUES ('survey_invite','Convite de pesquisa','Convite para pesquisa','<p>Você recebeu um convite.</p>','Você recebeu um convite.','active'),('result_available','Resultado disponível','Seu resultado está disponível','<p>Seu resultado está disponível.</p>','Seu resultado está disponível.','active'),('certificate_issued','Certificado emitido','Seu certificado foi emitido','<p>Seu certificado foi emitido.</p>','Seu certificado foi emitido.','active'),('password_reset','Redefinição de senha','Redefinição de senha','<p>Redefina sua senha.</p>','Redefina sua senha.','active'),('privacy_request_received','Solicitação recebida','Solicitação LGPD recebida','<p>Recebemos sua solicitação.</p>','Recebemos sua solicitação.','active'),('privacy_request_completed','Solicitação concluída','Solicitação LGPD concluída','<p>Sua solicitação foi concluída.</p>','Sua solicitação foi concluída.','active') ON CONFLICT (code, organization_id) DO NOTHING;
