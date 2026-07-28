-- Valora Insight - bootstrap canonico PostgreSQL
-- Fonte oficial a partir da Fase 1. Idempotente e nao destrutivo.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
SET search_path TO valorapesquisa, public;

CREATE OR REPLACE FUNCTION valorapesquisa.set_updated_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE TABLE IF NOT EXISTS schema_migrations (version text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now(), applied_by text NOT NULL DEFAULT current_user);
CREATE TABLE IF NOT EXISTS organizations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name text NOT NULL, slug text NOT NULL UNIQUE, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS business_groups (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), name text NOT NULL, tax_id text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS legal_entities (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), business_group_id uuid REFERENCES business_groups(id), legal_name text NOT NULL, trade_name text, cnpj text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_org_cnpj_active ON legal_entities(organization_id, cnpj) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS units (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), legal_entity_id uuid NOT NULL REFERENCES legal_entities(id), name text NOT NULL, code text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS departments (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), unit_id uuid REFERENCES units(id), name text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS users (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), email text NOT NULL, name text NOT NULL, password_hash text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz, UNIQUE(organization_id,email));
CREATE TABLE IF NOT EXISTS roles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), code text NOT NULL, name text NOT NULL, is_system boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz, UNIQUE(organization_id,code));
CREATE TABLE IF NOT EXISTS permissions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, description text, module_code text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS role_permissions (role_id uuid NOT NULL REFERENCES roles(id), permission_id uuid NOT NULL REFERENCES permissions(id), created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(role_id, permission_id));
CREATE TABLE IF NOT EXISTS user_sessions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), user_id uuid NOT NULL REFERENCES users(id), expires_at timestamptz NOT NULL, revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS refresh_tokens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), user_id uuid NOT NULL REFERENCES users(id), token_hash text NOT NULL UNIQUE, expires_at timestamptz NOT NULL, revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS password_reset_tokens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), user_id uuid NOT NULL REFERENCES users(id), token_hash text NOT NULL UNIQUE, expires_at timestamptz NOT NULL, used_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, is_public boolean NOT NULL, is_active boolean NOT NULL, is_legacy boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS plan_limits (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES plans(id), limit_key text NOT NULL, limit_value integer, period text NOT NULL DEFAULT 'lifetime', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(plan_id,limit_key));
CREATE TABLE IF NOT EXISTS plan_capabilities (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES plans(id), capability_key text NOT NULL, enabled boolean NOT NULL DEFAULT true, metadata jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(plan_id,capability_key));
CREATE TABLE IF NOT EXISTS subscriptions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), plan_id uuid NOT NULL REFERENCES plans(id), status text NOT NULL, starts_at timestamptz NOT NULL DEFAULT now(), ends_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS usage_monthly (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), usage_key text NOT NULL, year int NOT NULL, month int NOT NULL, quantity bigint NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,usage_key,year,month));
CREATE TABLE IF NOT EXISTS usage_lifetime (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), usage_key text NOT NULL, quantity bigint NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,usage_key));
CREATE TABLE IF NOT EXISTS modules (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, category text NOT NULL DEFAULT 'core', status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS organization_modules (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), module_id uuid REFERENCES modules(id), module_code text NOT NULL, enabled boolean NOT NULL DEFAULT true, source text NOT NULL DEFAULT 'plan', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,module_code));
CREATE TABLE IF NOT EXISTS organization_settings (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL UNIQUE REFERENCES organizations(id), settings jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS organization_branding (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL UNIQUE REFERENCES organizations(id), primary_color text NOT NULL DEFAULT '#0b3d4d', secondary_color text NOT NULL DEFAULT '#d7a94b', logo_url text, public_slug text UNIQUE, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS forms (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS form_versions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_id uuid NOT NULL REFERENCES forms(id), version int NOT NULL, language text NOT NULL DEFAULT 'pt-BR', is_immutable boolean NOT NULL DEFAULT true, max_score int NOT NULL DEFAULT 125, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(form_id,version,language));
CREATE TABLE IF NOT EXISTS form_translations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_version_id uuid NOT NULL REFERENCES form_versions(id), language text NOT NULL, title text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(form_version_id,language));
CREATE TABLE IF NOT EXISTS dimensions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_version_id uuid NOT NULL REFERENCES form_versions(id), code text NOT NULL, name text NOT NULL, display_order int NOT NULL, max_score int NOT NULL DEFAULT 25, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(form_version_id,code));
CREATE TABLE IF NOT EXISTS questions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), dimension_id uuid REFERENCES dimensions(id), code text NOT NULL, text text NOT NULL, display_order int NOT NULL, min_value int, max_value int, is_qualitative boolean NOT NULL DEFAULT false, is_required boolean NOT NULL DEFAULT true, max_text_length int, anonymity_protected boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(dimension_id,code));
ALTER TABLE questions ADD COLUMN IF NOT EXISTS is_required boolean NOT NULL DEFAULT true;
ALTER TABLE questions ADD COLUMN IF NOT EXISTS max_text_length int;
ALTER TABLE questions ADD COLUMN IF NOT EXISTS anonymity_protected boolean NOT NULL DEFAULT false;
CREATE TABLE IF NOT EXISTS question_options (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), question_id uuid NOT NULL REFERENCES questions(id), value int NOT NULL, label text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(question_id,value));
CREATE TABLE IF NOT EXISTS surveys (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), form_version_id uuid NOT NULL REFERENCES form_versions(id), name text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS survey_cycles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), survey_id uuid NOT NULL REFERENCES surveys(id), name text NOT NULL, starts_at timestamptz, ends_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS survey_scopes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), survey_id uuid NOT NULL REFERENCES surveys(id), unit_id uuid REFERENCES units(id), department_id uuid REFERENCES departments(id), created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS survey_links (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), survey_id uuid NOT NULL REFERENCES surveys(id), token_hash text NOT NULL UNIQUE, expires_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS survey_invites (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), survey_id uuid NOT NULL REFERENCES surveys(id), email_hash text, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS participants (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), email_hash text, name text, created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS responses (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), survey_id uuid NOT NULL REFERENCES surveys(id), participant_id uuid REFERENCES participants(id), qualitative text, submitted_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS response_answers (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), response_id uuid NOT NULL REFERENCES responses(id), question_id uuid NOT NULL REFERENCES questions(id), numeric_value int, text_value text, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(response_id,question_id));
CREATE TABLE IF NOT EXISTS result_scores (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), response_id uuid NOT NULL UNIQUE REFERENCES responses(id), total_score int NOT NULL, max_score int NOT NULL DEFAULT 125, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS dimension_scores (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), result_score_id uuid NOT NULL REFERENCES result_scores(id), dimension_id uuid NOT NULL REFERENCES dimensions(id), score int NOT NULL, max_score int NOT NULL DEFAULT 25, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(result_score_id,dimension_id));
CREATE TABLE IF NOT EXISTS results (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), response_id uuid NOT NULL REFERENCES responses(id), result_score_id uuid REFERENCES result_scores(id), public_token_hash text UNIQUE, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS result_recommendations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), result_id uuid NOT NULL REFERENCES results(id), dimension_id uuid REFERENCES dimensions(id), text text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS certificates (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), result_id uuid NOT NULL REFERENCES results(id), validation_code text NOT NULL UNIQUE, issued_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS certificate_validations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), certificate_id uuid NOT NULL REFERENCES certificates(id), validated_at timestamptz NOT NULL DEFAULT now(), ip_hash text);
CREATE TABLE IF NOT EXISTS reports (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), name text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS exports (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), export_type text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS emails (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), recipient_hash text NOT NULL, subject text, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS whatsapp_messages (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), recipient_hash text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS communications (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), channel text NOT NULL, recipient_hash text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS notifications (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), user_id uuid REFERENCES users(id), title text NOT NULL, body text NOT NULL, read_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS action_plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), result_id uuid REFERENCES results(id), title text NOT NULL, status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS lgpd_consents (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), participant_id uuid REFERENCES participants(id), consent_type text NOT NULL, granted_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz);
CREATE TABLE IF NOT EXISTS privacy_requests (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), requester_hash text NOT NULL, request_type text NOT NULL, status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS support_tickets (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), subject text NOT NULL, status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS integrations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), provider text NOT NULL, status text NOT NULL DEFAULT 'inactive', config jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS audit_logs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), actor_id uuid, action text NOT NULL, entity_name text, entity_id uuid, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS operational_logs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), level text NOT NULL, message text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS migration_mappings (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), source_system text NOT NULL, source_id text NOT NULL, target_table text NOT NULL, target_id uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(source_system,source_id,target_table));
CREATE TABLE IF NOT EXISTS migration_conflicts (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), source_system text NOT NULL, source_id text NOT NULL, conflict_type text NOT NULL, status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS migration_checkpoints (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), migration_name text NOT NULL UNIQUE, checkpoint_data jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS rollback_records (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), migration_name text NOT NULL, rollback_data jsonb NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS outbox_messages (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), aggregate_id uuid, message_type text NOT NULL, payload jsonb NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), processed_at timestamptz);
CREATE TABLE IF NOT EXISTS idempotency_keys (key text PRIMARY KEY, organization_id uuid REFERENCES organizations(id), request_hash text NOT NULL, response_body jsonb, created_at timestamptz NOT NULL DEFAULT now(), expires_at timestamptz NOT NULL);
CREATE TABLE IF NOT EXISTS plan_usage_counters (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), metric_key text NOT NULL, period_start date NOT NULL, consumed bigint NOT NULL DEFAULT 0 CHECK(consumed>=0), reserved bigint NOT NULL DEFAULT 0 CHECK(reserved>=0), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(organization_id,metric_key,period_start));
CREATE TABLE IF NOT EXISTS plan_usage_reservations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), metric_key text NOT NULL, quantity bigint NOT NULL CHECK(quantity>0), status text NOT NULL DEFAULT 'reserved' CHECK(status IN ('reserved','confirmed','released','expired')), idempotency_key text NOT NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(organization_id,idempotency_key));
CREATE INDEX IF NOT EXISTS ix_plan_usage_reservations_active ON plan_usage_reservations(organization_id,metric_key,expires_at) WHERE status='reserved';
CREATE TABLE IF NOT EXISTS user_scopes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES users(id), organization_id uuid NOT NULL REFERENCES organizations(id), business_group_id uuid REFERENCES business_groups(id), legal_entity_id uuid REFERENCES legal_entities(id), unit_id uuid REFERENCES units(id), department_id uuid REFERENCES departments(id), created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS subscription_history (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), subscription_id uuid NOT NULL REFERENCES subscriptions(id), organization_id uuid NOT NULL REFERENCES organizations(id), previous_status text, new_status text NOT NULL, previous_plan_id uuid REFERENCES plans(id), new_plan_id uuid REFERENCES plans(id), changed_by uuid REFERENCES users(id), reason text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_subscription_history_organization ON subscription_history(organization_id,created_at DESC);

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
CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_replaced_by ON refresh_tokens(replaced_by_id) WHERE replaced_by_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_session_active ON refresh_tokens(session_id,expires_at) WHERE revoked_at IS NULL;

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

INSERT INTO permissions(code,name,description,module_code) VALUES
('organization.current.read','Visualizar organização','Consulta a organização corrente.','identity'),
('organization.current.update','Atualizar organização','Atualiza a organização corrente.','identity'),
('users.read','Visualizar usuários','Consulta usuários do tenant.','identity'),
('users.create','Criar usuários','Cria usuários no tenant.','identity'),
('users.update','Atualizar usuários','Atualiza usuários no tenant.','identity'),
('users.disable','Desativar usuários','Desativa usuários no tenant.','identity'),
('sessions.read','Visualizar sessões','Consulta sessões próprias.','identity'),
('sessions.revoke','Revogar sessões','Revoga sessões próprias.','identity')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code;


DO $$ DECLARE t text; BEGIN FOREACH t IN ARRAY ARRAY['organizations','business_groups','legal_entities','units','departments','users','roles','plans','plan_limits','plan_capabilities','subscriptions','usage_monthly','usage_lifetime','modules','organization_modules','organization_settings','organization_branding','surveys','survey_cycles','survey_invites','responses','result_scores','reports','exports','emails','whatsapp_messages','communications','action_plans','privacy_requests','support_tickets','integrations','migration_checkpoints'] LOOP EXECUTE format('DROP TRIGGER IF EXISTS trg_%I_updated_at ON valorapesquisa.%I', t, t); EXECUTE format('CREATE TRIGGER trg_%I_updated_at BEFORE UPDATE ON valorapesquisa.%I FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at()', t, t); END LOOP; END $$;

INSERT INTO plans(code,name,is_public,is_active,is_legacy) VALUES
('free','Gratuito',true,true,false),('professional','Profissional',true,true,false),('corporate','Corporativo',true,true,false),('enterprise','Enterprise',true,true,false),('essential','Essential legado',false,false,true),('growth','Growth legado',false,false,true)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,is_public=EXCLUDED.is_public,is_active=EXCLUDED.is_active,is_legacy=EXCLUDED.is_legacy,updated_at=now();
WITH configured_limits(limit_key, free_value, professional_value, corporate_value, enterprise_value) AS (VALUES
('legalEntities',1,1,1,NULL),('units',1,1,NULL,NULL),('departments',3,20,NULL,NULL),
('users',3,20,100,NULL),('managers',1,5,25,NULL),('activeSurveys',1,5,25,NULL),
('monthlyResponses',100,1000,10000,NULL),('lifetimeResponses',500,NULL,NULL,NULL),
('monthlyEmailInvites',100,2000,20000,NULL),('diagnosticCycles',1,12,NULL,NULL),
('languages',1,2,4,NULL),('storageMb',100,2048,10240,NULL))
INSERT INTO plan_limits(plan_id,limit_key,limit_value)
SELECT p.id,l.limit_key,CASE p.code WHEN 'free' THEN l.free_value WHEN 'professional' THEN l.professional_value WHEN 'corporate' THEN l.corporate_value ELSE l.enterprise_value END
FROM plans p CROSS JOIN configured_limits l WHERE p.code IN ('free','professional','corporate','enterprise')
ON CONFLICT(plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value,updated_at=now();
WITH capabilities(capability_key) AS (VALUES
('officialValoraProgram'),('shareLink'),('shareEmail'),('whatsappPreview'),('basicResult'),
('crossSurveyAnalysis'),('crossDepartmentAnalysis'),('actionPlan'),('organizationReport'),
('multipleUnits'),('unitComparison'),('consolidatedReports'),('franchiseMode'),
('multipleLegalEntities'),('businessGroupManagement'),('intercompanyComparison'),('groupDashboard'),
('whiteLabel'),('integrations'),('executiveFollowUp'))
INSERT INTO plan_capabilities(plan_id,capability_key,enabled)
SELECT p.id,c.capability_key,
CASE p.code
 WHEN 'free' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','basicResult')
 WHEN 'professional' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','crossSurveyAnalysis','crossDepartmentAnalysis','actionPlan','organizationReport')
 WHEN 'corporate' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','crossSurveyAnalysis','crossDepartmentAnalysis','actionPlan','organizationReport','multipleUnits','unitComparison','consolidatedReports','franchiseMode')
 ELSE true END
FROM plans p CROSS JOIN capabilities c WHERE p.code IN ('free','professional','corporate','enterprise')
ON CONFLICT(plan_id,capability_key) DO UPDATE SET enabled=EXCLUDED.enabled,updated_at=now();
INSERT INTO forms(code,name,status) VALUES('valora-official','Pesquisa Oficial Valora','active') ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,updated_at=now();
INSERT INTO form_versions(form_id,version,language,is_immutable,max_score) SELECT id,1,'pt-BR',true,125 FROM forms WHERE code='valora-official' ON CONFLICT(form_id,version,language) DO UPDATE SET max_score=125;
INSERT INTO form_translations(form_version_id,language,title) SELECT id,lang,'Valora Insight' FROM form_versions CROSS JOIN (VALUES('pt-BR'),('en'),('es'),('zh-Hans')) l(lang) ON CONFLICT(form_version_id,language) DO NOTHING;
WITH fv AS (SELECT id FROM form_versions WHERE form_id=(SELECT id FROM forms WHERE code='valora-official') AND version=1 AND language='pt-BR'), d(code,name,ord) AS (VALUES ('culture','Cultura e Propósito',1),('governance','Gestão e Governança',2),('leadership','Liderança',3),('people','Pessoas e Talentos',4),('growth','Resultados e Crescimento',5)) INSERT INTO dimensions(form_version_id,code,name,display_order,max_score) SELECT fv.id,d.code,d.name,d.ord,25 FROM fv,d ON CONFLICT(form_version_id,code) DO UPDATE SET name=EXCLUDED.name;
WITH official(code,dimension_code,display_order,text) AS (VALUES
('culture-q1','culture',1,'As pessoas compreendem claramente o propósito e os valores da empresa.'),
('culture-q2','culture',2,'Existe alinhamento entre o que a liderança comunica e o que é praticado no dia a dia.'),
('culture-q3','culture',3,'Os colaboradores entendem como seu trabalho contribui para os resultados do negócio.'),
('culture-q4','culture',4,'A cultura da empresa favorece colaboração, responsabilidade e comprometimento.'),
('culture-q5','culture',5,'As decisões da empresa refletem seus valores e direcionamento estratégico.'),
('governance-q1','governance',1,'Papéis e responsabilidades estão claramente definidos.'),
('governance-q2','governance',2,'As decisões importantes seguem critérios e processos bem estabelecidos.'),
('governance-q3','governance',3,'A empresa acompanha regularmente indicadores relevantes para o negócio.'),
('governance-q4','governance',4,'Os gestores possuem informações confiáveis para tomar decisões.'),
('governance-q5','governance',5,'A operação funciona com estabilidade sem depender excessivamente de poucas pessoas.'),
('leadership-q1','leadership',1,'Os líderes dão direção clara às equipes.'),
('leadership-q2','leadership',2,'As lideranças atuam de forma alinhada entre si.'),
('leadership-q3','leadership',3,'Os líderes desenvolvem pessoas e fortalecem talentos.'),
('leadership-q4','leadership',4,'Os conflitos são tratados de forma construtiva e madura.'),
('leadership-q5','leadership',5,'As lideranças inspiram confiança e engajamento.'),
('people-q1','people',1,'A empresa atrai profissionais alinhados à sua cultura e objetivos.'),
('people-q2','people',2,'Novos colaboradores são integrados de forma estruturada.'),
('people-q3','people',3,'Existem oportunidades claras de desenvolvimento e crescimento profissional.'),
('people-q4','people',4,'Os talentos estratégicos tendem a permanecer na organização.'),
('people-q5','people',5,'O desempenho das pessoas é acompanhado e desenvolvido regularmente.'),
('growth-q1','growth',1,'A empresa atinge suas metas com consistência.'),
('growth-q2','growth',2,'Existe equilíbrio entre crescimento, organização e capacidade de execução.'),
('growth-q3','growth',3,'Os processos favorecem produtividade e eficiência.'),
('growth-q4','growth',4,'Problemas recorrentes são tratados na causa, e não apenas nos sintomas.'),
('growth-q5','growth',5,'A empresa está preparada para sustentar o crescimento nos próximos anos.'))
INSERT INTO questions(dimension_id,code,text,display_order,min_value,max_value,is_qualitative,is_required,max_text_length,anonymity_protected)
SELECT d.id,o.code,o.text,o.display_order,1,5,false,true,NULL,false FROM official o JOIN dimensions d ON d.code=o.dimension_code JOIN form_versions fv ON fv.id=d.form_version_id JOIN forms f ON f.id=fv.form_id AND f.code='valora-official'
ON CONFLICT(dimension_id,code) DO UPDATE SET text=EXCLUDED.text,display_order=EXCLUDED.display_order,min_value=1,max_value=5,is_qualitative=false,is_required=true;
INSERT INTO questions(dimension_id,code,text,display_order,min_value,max_value,is_qualitative,is_required,max_text_length,anonymity_protected)
SELECT d.id,'qualitative-work-feeling','Em suas palavras, como você se sente trabalhando nesta empresa hoje?',6,NULL,NULL,true,false,4000,true FROM dimensions d JOIN form_versions fv ON fv.id=d.form_version_id JOIN forms f ON f.id=fv.form_id WHERE f.code='valora-official' AND d.code='growth'
ON CONFLICT(dimension_id,code) DO UPDATE SET text=EXCLUDED.text,is_qualitative=true,is_required=false,max_text_length=4000,anonymity_protected=true;
INSERT INTO schema_migrations(version,checksum) VALUES('0000_banco_completo_bootstrap','manual-phase1') ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();
COMMIT;
