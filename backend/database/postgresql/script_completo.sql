-- Valora Insight - bootstrap canonico PostgreSQL
-- Fonte oficial a partir da Fase 1. Idempotente e nao destrutivo.
BEGIN;
SELECT pg_advisory_xact_lock(hashtextextended('valorapesquisa:script_completo', 0));
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
SET LOCAL search_path TO valorapesquisa, public;

-- COMPATIBILIDADE PARA BANCOS EXISTENTES
-- O bootstrap converge contratos antigos antes de normalizar, indexar ou
-- semear cada entidade, permanecendo aditivo e seguro para reexecução.

CREATE OR REPLACE FUNCTION valorapesquisa.set_updated_at()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$;

CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (version text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now(), applied_by text NOT NULL DEFAULT current_user, application_version text);
ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS checksum text;
ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS applied_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS applied_by text NOT NULL DEFAULT current_user;
ALTER TABLE valorapesquisa.schema_migrations ADD COLUMN IF NOT EXISTS application_version text;
CREATE TABLE IF NOT EXISTS valorapesquisa.organizations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name text NOT NULL, slug text NOT NULL UNIQUE, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS slug text;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS status text DEFAULT 'active';
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.organizations SET name=COALESCE(NULLIF(name,''),'Organização sem nome'),slug=COALESCE(NULLIF(slug,''),'legacy-'||replace(id::text,'-','')),status=COALESCE(NULLIF(status,''),'active'),created_at=COALESCE(created_at,now());
ALTER TABLE valorapesquisa.organizations ALTER COLUMN name SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN slug SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN status SET DEFAULT 'active';
ALTER TABLE valorapesquisa.organizations ALTER COLUMN status SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN created_at SET DEFAULT now();
ALTER TABLE valorapesquisa.organizations ALTER COLUMN created_at SET NOT NULL;
-- Nunca reutilize o nome histórico ux_organizations_slug aqui. Em instalações
-- antigas esse nome foi criado como índice não-único; IF NOT EXISTS o ignorava
-- e o ON CONFLICT(slug) abaixo falhava por não encontrar uma chave elegível.
WITH duplicate_keys AS (
  SELECT id, row_number() OVER (PARTITION BY slug ORDER BY created_at NULLS LAST, id) AS occurrence
  FROM valorapesquisa.organizations
)
UPDATE valorapesquisa.organizations o
SET slug=o.slug || '-legacy-' || left(replace(o.id::text,'-',''),8), updated_at=now()
FROM duplicate_keys d WHERE d.id=o.id AND d.occurrence>1;
CREATE UNIQUE INDEX IF NOT EXISTS ux_organizations_slug_v2 ON valorapesquisa.organizations(slug);
INSERT INTO valorapesquisa.organizations(name,slug,status)
VALUES('Valora Group','valora-platform','active')
ON CONFLICT(slug) DO UPDATE SET name=EXCLUDED.name,status='active',deleted_at=NULL,updated_at=now();
CREATE TABLE IF NOT EXISTS valorapesquisa.business_groups (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), name text NOT NULL, tax_id text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.legal_entities (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), business_group_id uuid REFERENCES valorapesquisa.business_groups(id), legal_name text NOT NULL, trade_name text, cnpj text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_cnpj_active ON valorapesquisa.legal_entities(cnpj) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.units (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), legal_entity_id uuid NOT NULL REFERENCES valorapesquisa.legal_entities(id), name text NOT NULL, code text, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.departments (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), unit_id uuid REFERENCES valorapesquisa.units(id), name text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.users (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), email text NOT NULL, name text NOT NULL, password_hash text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz, UNIQUE(organization_id,email));
-- Access catalog is converged before any seed, index, trigger or query uses its columns.
CREATE TABLE IF NOT EXISTS valorapesquisa.modules (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, category text NOT NULL DEFAULT 'core', status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
ALTER TABLE valorapesquisa.modules ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.modules ADD COLUMN IF NOT EXISTS category text NOT NULL DEFAULT 'core';
ALTER TABLE valorapesquisa.modules ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.modules ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.modules ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.modules ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

CREATE TABLE IF NOT EXISTS valorapesquisa.permissions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, description text, module_code text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS module_code text;
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS functional_group text NOT NULL DEFAULT 'general';
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS risk_level text NOT NULL DEFAULT 'low';
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS is_system boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS assignable_to_custom_roles boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS display_order integer NOT NULL DEFAULT 100;
ALTER TABLE valorapesquisa.permissions ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'active';
CREATE INDEX IF NOT EXISTS ix_permissions_module_code ON valorapesquisa.permissions(module_code);

CREATE TABLE IF NOT EXISTS valorapesquisa.roles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), code text NOT NULL, name text NOT NULL, is_system boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz, UNIQUE(organization_id,code));
ALTER TABLE valorapesquisa.roles ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.roles ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.roles ADD COLUMN IF NOT EXISTS version bigint NOT NULL DEFAULT 1;
ALTER TABLE valorapesquisa.roles ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.roles ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE TABLE IF NOT EXISTS valorapesquisa.role_permissions (role_id uuid NOT NULL REFERENCES valorapesquisa.roles(id), permission_id uuid NOT NULL REFERENCES valorapesquisa.permissions(id), created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(role_id, permission_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.user_sessions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), expires_at timestamptz NOT NULL, revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.refresh_tokens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), token_hash text NOT NULL UNIQUE, expires_at timestamptz NOT NULL, revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.password_reset_tokens (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), token_hash text NOT NULL UNIQUE, expires_at timestamptz NOT NULL, used_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, is_public boolean NOT NULL, is_active boolean NOT NULL, is_legacy boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_limits (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES valorapesquisa.plans(id), limit_key text NOT NULL, limit_value integer, period text NOT NULL DEFAULT 'lifetime', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(plan_id,limit_key));
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_capabilities (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES valorapesquisa.plans(id), capability_key text NOT NULL, enabled boolean NOT NULL DEFAULT true, metadata jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(plan_id,capability_key));
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS capability text;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS capability_code text;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS capability_key text;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS enabled boolean DEFAULT true;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS is_enabled boolean DEFAULT true;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS metadata jsonb DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.plan_capabilities ADD COLUMN IF NOT EXISTS updated_at timestamptz;
UPDATE valorapesquisa.plan_capabilities SET capability_key=COALESCE(NULLIF(capability_key,''),NULLIF(capability_code,''),NULLIF(capability,''),'legacy-'||replace(id::text,'-','')),capability_code=COALESCE(NULLIF(capability_code,''),NULLIF(capability_key,''),NULLIF(capability,''),'legacy-'||replace(id::text,'-','')),capability=COALESCE(NULLIF(capability,''),NULLIF(capability_key,''),NULLIF(capability_code,''),'legacy-'||replace(id::text,'-','')),enabled=COALESCE(enabled,is_enabled,true),is_enabled=COALESCE(is_enabled,enabled,true),metadata=COALESCE(metadata,'{}'::jsonb),created_at=COALESCE(created_at,now());
DELETE FROM valorapesquisa.plan_capabilities a USING valorapesquisa.plan_capabilities b WHERE a.plan_id=b.plan_id AND a.capability_key=b.capability_key AND a.id>b.id;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN capability SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN capability_code SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN capability_key SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN enabled SET DEFAULT true;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN enabled SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN is_enabled SET DEFAULT true;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN is_enabled SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN metadata SET DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN metadata SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN created_at SET DEFAULT now();
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN created_at SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_capabilities_plan_key ON valorapesquisa.plan_capabilities(plan_id,capability_key);
CREATE TABLE IF NOT EXISTS valorapesquisa.subscriptions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), plan_id uuid NOT NULL REFERENCES valorapesquisa.plans(id), status text NOT NULL, starts_at timestamptz NOT NULL DEFAULT now(), ends_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.usage_monthly (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), usage_key text NOT NULL, year int NOT NULL, month int NOT NULL, quantity bigint NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,usage_key,year,month));
CREATE TABLE IF NOT EXISTS valorapesquisa.usage_lifetime (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), usage_key text NOT NULL, quantity bigint NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,usage_key));
CREATE TABLE IF NOT EXISTS valorapesquisa.modules (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, category text NOT NULL DEFAULT 'core', status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_modules (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), module_id uuid REFERENCES valorapesquisa.modules(id), module_code text NOT NULL, enabled boolean NOT NULL DEFAULT true, source text NOT NULL DEFAULT 'plan', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,module_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_settings (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL UNIQUE REFERENCES valorapesquisa.organizations(id), settings jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_branding (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL UNIQUE REFERENCES valorapesquisa.organizations(id), primary_color text NOT NULL DEFAULT '#0b3d4d', secondary_color text NOT NULL DEFAULT '#d7a94b', logo_url text, public_slug text UNIQUE, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS white_label_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS primary_color text DEFAULT '#0b3d4d';
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS secondary_color text DEFAULT '#d7a94b';
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS logo_url text;
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS public_slug text;
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.organization_branding ADD COLUMN IF NOT EXISTS version bigint NOT NULL DEFAULT 1;
CREATE TABLE IF NOT EXISTS valorapesquisa.forms (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.form_versions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id), version int NOT NULL, language text NOT NULL DEFAULT 'pt-BR', is_immutable boolean NOT NULL DEFAULT true, max_score int NOT NULL DEFAULT 125, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(form_id,version,language));
-- Converge contratos antigos/parciais antes de ler ou popular forms. Alguns
-- bancos legados possuem title NOT NULL, enquanto outros usam apenas code/name.
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS title text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS slug text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS form_key text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS questions_count integer DEFAULT 0;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS version bigint DEFAULT 1;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS code text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS status text DEFAULT 'active';
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.forms
SET code = COALESCE(NULLIF(code, ''), NULLIF(form_key, ''), NULLIF(slug, ''), 'legacy-' || replace(id::text, '-', '')),
    form_key = COALESCE(NULLIF(form_key, ''), NULLIF(code, ''), NULLIF(slug, ''), 'legacy-' || replace(id::text, '-', '')),
    slug = COALESCE(NULLIF(slug, ''), NULLIF(form_key, ''), NULLIF(code, ''), 'legacy-' || replace(id::text, '-', '')),
    name = COALESCE(NULLIF(name, ''), NULLIF(title, ''), 'Formulário sem nome'),
    title = COALESCE(NULLIF(title, ''), NULLIF(name, ''), 'Formulário sem título'),
    status = COALESCE(NULLIF(status, ''), 'active'),
    questions_count = COALESCE(questions_count, 0),
    version = COALESCE(version, 1);
-- Bancos parciais podem ter recebido duplicatas antes dos índices oficiais.
-- Preservamos todas as linhas e tornamos apenas as chaves técnicas inequívocas.
WITH duplicates AS (
  SELECT id, row_number() OVER (PARTITION BY code ORDER BY created_at NULLS LAST, id) AS occurrence
  FROM valorapesquisa.forms
)
UPDATE valorapesquisa.forms f SET code=f.code || '-' || left(replace(f.id::text,'-',''),8)
FROM duplicates d WHERE f.id=d.id AND d.occurrence>1;
WITH duplicates AS (
  SELECT id, row_number() OVER (PARTITION BY slug ORDER BY created_at NULLS LAST, id) AS occurrence
  FROM valorapesquisa.forms
)
UPDATE valorapesquisa.forms f SET slug=f.slug || '-' || left(replace(f.id::text,'-',''),8)
FROM duplicates d WHERE f.id=d.id AND d.occurrence>1;
WITH duplicates AS (
  SELECT id, row_number() OVER (PARTITION BY form_key ORDER BY created_at NULLS LAST, id) AS occurrence
  FROM valorapesquisa.forms
)
UPDATE valorapesquisa.forms f SET form_key=f.form_key || '-' || left(replace(f.id::text,'-',''),8)
FROM duplicates d WHERE f.id=d.id AND d.occurrence>1;
ALTER TABLE valorapesquisa.forms ALTER COLUMN code SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN title SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN name SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN slug SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN form_key SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN status SET DEFAULT 'active';
ALTER TABLE valorapesquisa.forms ALTER COLUMN status SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN questions_count SET DEFAULT 0;
ALTER TABLE valorapesquisa.forms ALTER COLUMN questions_count SET NOT NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN version SET DEFAULT 1;
ALTER TABLE valorapesquisa.forms ALTER COLUMN version SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_forms_slug ON valorapesquisa.forms(slug);
CREATE UNIQUE INDEX IF NOT EXISTS ux_forms_form_key ON valorapesquisa.forms(form_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_forms_code ON valorapesquisa.forms(code);
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id);
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS category text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS estimated_minutes int NOT NULL DEFAULT 15;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS current_draft_version_id uuid;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS latest_published_version_id uuid;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS created_by_user_id uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS version bigint NOT NULL DEFAULT 1;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.forms SET organization_id=(SELECT id FROM valorapesquisa.organizations WHERE slug='valora-platform') WHERE organization_id IS NULL;
ALTER TABLE valorapesquisa.forms ALTER COLUMN organization_id SET NOT NULL;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id);
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS version int;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS version_number int;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS max_score int DEFAULT 125;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS maximum_score int DEFAULT 125;
UPDATE valorapesquisa.form_versions fv SET organization_id=f.organization_id FROM valorapesquisa.forms f WHERE f.id=fv.form_id AND fv.organization_id IS NULL;
UPDATE valorapesquisa.form_versions SET version=COALESCE(version,version_number,1),version_number=COALESCE(version_number,version,1),max_score=COALESCE(max_score,maximum_score,125);
ALTER TABLE valorapesquisa.form_versions ALTER COLUMN organization_id SET NOT NULL;
ALTER TABLE valorapesquisa.form_versions ALTER COLUMN version SET NOT NULL;
ALTER TABLE valorapesquisa.form_versions ALTER COLUMN version_number SET NOT NULL;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'published';
ALTER TABLE valorapesquisa.form_versions ALTER COLUMN maximum_score SET DEFAULT 125;
UPDATE valorapesquisa.form_versions SET maximum_score=COALESCE(maximum_score,max_score,125),max_score=COALESCE(max_score,maximum_score,125);
ALTER TABLE valorapesquisa.form_versions ALTER COLUMN maximum_score SET NOT NULL;
ALTER TABLE valorapesquisa.form_versions ALTER COLUMN max_score SET NOT NULL;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS published_at timestamptz;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS published_by_user_id uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS updated_at timestamptz;
ALTER TABLE valorapesquisa.form_versions ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

CREATE TABLE IF NOT EXISTS valorapesquisa.form_section_versions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
    form_version_id uuid NOT NULL REFERENCES valorapesquisa.form_versions(id),
    title text NOT NULL,
    description text,
    position int NOT NULL CHECK (position >= 0),
    version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE TABLE IF NOT EXISTS valorapesquisa.question_versions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
    section_id uuid NOT NULL REFERENCES valorapesquisa.form_section_versions(id),
    code text NOT NULL,
    type text NOT NULL CHECK (type IN ('likert_1_5','single_choice','multiple_choice','short_text','long_text','heading','description','separator')),
    title text NOT NULL,
    description text,
    required boolean NOT NULL DEFAULT false,
    dimension_code text,
    weight numeric(8,2) NOT NULL DEFAULT 1 CHECK (weight >= 0),
    position int NOT NULL CHECK (position >= 0),
    settings jsonb NOT NULL DEFAULT '{}'::jsonb,
    version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE(section_id, code)
);

CREATE TABLE IF NOT EXISTS valorapesquisa.question_option_versions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
    question_id uuid NOT NULL REFERENCES valorapesquisa.question_versions(id),
    label text NOT NULL,
    value text NOT NULL,
    score numeric(8,2) CHECK (score IS NULL OR score BETWEEN 1 AND 5),
    position int NOT NULL CHECK (position >= 0),
    version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    UNIQUE(question_id, value)
);
ALTER TABLE valorapesquisa.form_section_versions ADD COLUMN IF NOT EXISTS position int;
ALTER TABLE valorapesquisa.form_section_versions ADD COLUMN IF NOT EXISTS display_order int;
UPDATE valorapesquisa.form_section_versions SET position=COALESCE(position,display_order,0),display_order=COALESCE(display_order,position,0);
ALTER TABLE valorapesquisa.form_section_versions ALTER COLUMN position SET NOT NULL;
ALTER TABLE valorapesquisa.form_section_versions ALTER COLUMN display_order SET NOT NULL;
ALTER TABLE valorapesquisa.question_versions ADD COLUMN IF NOT EXISTS position int;
ALTER TABLE valorapesquisa.question_versions ADD COLUMN IF NOT EXISTS display_order int;
UPDATE valorapesquisa.question_versions SET position=COALESCE(position,display_order,0),display_order=COALESCE(display_order,position,0);
ALTER TABLE valorapesquisa.question_versions ALTER COLUMN position SET NOT NULL;
ALTER TABLE valorapesquisa.question_versions ALTER COLUMN display_order SET NOT NULL;
ALTER TABLE valorapesquisa.question_option_versions ADD COLUMN IF NOT EXISTS position int;
ALTER TABLE valorapesquisa.question_option_versions ADD COLUMN IF NOT EXISTS display_order int;
UPDATE valorapesquisa.question_option_versions SET position=COALESCE(position,display_order,0),display_order=COALESCE(display_order,position,0);
ALTER TABLE valorapesquisa.question_option_versions ALTER COLUMN position SET NOT NULL;
ALTER TABLE valorapesquisa.question_option_versions ALTER COLUMN display_order SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_forms_organization_status ON valorapesquisa.forms(organization_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_form_sections_version_position ON valorapesquisa.form_section_versions(form_version_id,position) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_question_versions_section_position ON valorapesquisa.question_versions(section_id,position) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_question_option_versions_question_position ON valorapesquisa.question_option_versions(question_id,position) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.form_translations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_version_id uuid NOT NULL REFERENCES valorapesquisa.form_versions(id), language text NOT NULL, title text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(form_version_id,language));
CREATE TABLE IF NOT EXISTS valorapesquisa.dimensions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_version_id uuid NOT NULL REFERENCES valorapesquisa.form_versions(id), code text NOT NULL, name text NOT NULL, display_order int NOT NULL, max_score int NOT NULL DEFAULT 25, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(form_version_id,code));
ALTER TABLE valorapesquisa.dimensions ADD COLUMN IF NOT EXISTS display_order int;
CREATE TABLE IF NOT EXISTS valorapesquisa.questions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), dimension_id uuid REFERENCES valorapesquisa.dimensions(id), code text NOT NULL, text text NOT NULL, display_order int NOT NULL, min_value int, max_value int, is_qualitative boolean NOT NULL DEFAULT false, is_required boolean NOT NULL DEFAULT true, max_text_length int, anonymity_protected boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(dimension_id,code));
-- Compatibilidade do contrato de perguntas. CREATE TABLE IF NOT EXISTS não
-- converge instalações anteriores, que podem exigir title/type/form/tenant.
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id);
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS form_id uuid REFERENCES valorapesquisa.forms(id);
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS form_version_id uuid REFERENCES valorapesquisa.form_versions(id);
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS dimension_id uuid REFERENCES valorapesquisa.dimensions(id);
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS code text;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS title text;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS text text;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS type text;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS min_value int;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS max_value int;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS required boolean DEFAULT true;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS is_required boolean DEFAULT true;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS is_active boolean DEFAULT true;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS is_qualitative boolean DEFAULT false;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS weight numeric(8,2) DEFAULT 1.00;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS version bigint DEFAULT 1;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS updated_at timestamptz;
CREATE OR REPLACE FUNCTION valorapesquisa.compatible_scale_question_type()
RETURNS text LANGUAGE plpgsql STABLE AS $$
DECLARE checks text;
BEGIN
 SELECT string_agg(pg_get_constraintdef(c.oid),' ') INTO checks FROM pg_constraint c
 JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
 WHERE n.nspname='valorapesquisa' AND t.relname='questions' AND c.contype='c'
   AND pg_get_constraintdef(c.oid) ILIKE '%type%';
 IF checks ILIKE '%likert_1_5%' THEN RETURN 'likert_1_5'; END IF;
 RETURN 'scale';
END $$;
CREATE OR REPLACE FUNCTION valorapesquisa.compatible_text_question_type()
RETURNS text LANGUAGE plpgsql STABLE AS $$
DECLARE checks text;
BEGIN
 SELECT string_agg(pg_get_constraintdef(c.oid),' ') INTO checks FROM pg_constraint c
 JOIN pg_class t ON t.oid=c.conrelid JOIN pg_namespace n ON n.oid=t.relnamespace
 WHERE n.nspname='valorapesquisa' AND t.relname='questions' AND c.contype='c'
   AND pg_get_constraintdef(c.oid) ILIKE '%type%';
 IF checks ILIKE '%long_text%' THEN RETURN 'long_text'; END IF;
 IF checks ILIKE '%''text''%' THEN RETURN 'text'; END IF;
 RETURN valorapesquisa.compatible_scale_question_type();
END $$;
ALTER TABLE valorapesquisa.dimensions ADD COLUMN IF NOT EXISTS position int;
UPDATE valorapesquisa.dimensions SET position=COALESCE(position,display_order,0),display_order=COALESCE(display_order,position,0);
ALTER TABLE valorapesquisa.dimensions ALTER COLUMN position SET NOT NULL;
ALTER TABLE valorapesquisa.dimensions ALTER COLUMN display_order SET NOT NULL;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS display_order int;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS position int;
UPDATE valorapesquisa.questions SET position=COALESCE(position,display_order,0),display_order=COALESCE(display_order,position,0);
UPDATE valorapesquisa.questions q SET
 organization_id=COALESCE(q.organization_id,f.organization_id), form_id=COALESCE(q.form_id,f.id),
 form_version_id=COALESCE(q.form_version_id,fv.id), title=COALESCE(NULLIF(q.title,''),NULLIF(q.text,''),'Pergunta sem título'),
 text=COALESCE(NULLIF(q.text,''),NULLIF(q.title,''),'Pergunta sem texto'),
 type=COALESCE(NULLIF(q.type,''),CASE WHEN q.is_qualitative THEN valorapesquisa.compatible_text_question_type() ELSE valorapesquisa.compatible_scale_question_type() END),
 required=COALESCE(q.required,q.is_required,true),is_required=COALESCE(q.is_required,q.required,true),
 is_active=COALESCE(q.is_active,true),is_qualitative=COALESCE(q.is_qualitative,false),
 weight=COALESCE(q.weight,1.00),version=COALESCE(q.version,1),created_at=COALESCE(q.created_at,now())
FROM valorapesquisa.dimensions d JOIN valorapesquisa.form_versions fv ON fv.id=d.form_version_id
JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE q.dimension_id=d.id;
ALTER TABLE valorapesquisa.questions ALTER COLUMN position SET NOT NULL;
ALTER TABLE valorapesquisa.questions ALTER COLUMN display_order SET NOT NULL;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS is_required boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS max_text_length int;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS anonymity_protected boolean NOT NULL DEFAULT false;
CREATE UNIQUE INDEX IF NOT EXISTS ux_questions_dimension_code_v2 ON valorapesquisa.questions(dimension_id,code);

CREATE TABLE IF NOT EXISTS valorapesquisa.question_options (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id), value int NOT NULL, label text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(question_id,value));
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS position int;
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS display_order int;
UPDATE valorapesquisa.question_options SET position=COALESCE(position,display_order,value,0),display_order=COALESCE(display_order,position,value,0);
ALTER TABLE valorapesquisa.question_options ALTER COLUMN position SET NOT NULL;
ALTER TABLE valorapesquisa.question_options ALTER COLUMN display_order SET NOT NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.surveys (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), form_version_id uuid NOT NULL REFERENCES valorapesquisa.form_versions(id), name text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.survey_cycles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), name text NOT NULL, starts_at timestamptz, ends_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.survey_scopes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), unit_id uuid REFERENCES valorapesquisa.units(id), department_id uuid REFERENCES valorapesquisa.departments(id), created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.survey_links (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), token_hash text NOT NULL UNIQUE, expires_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.survey_invites (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), email_hash text, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.participants (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), email_hash text, name text, created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.responses (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), participant_id uuid REFERENCES valorapesquisa.participants(id), qualitative text, submitted_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.response_answers (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), response_id uuid NOT NULL REFERENCES valorapesquisa.responses(id), question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id), numeric_value int, text_value text, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(response_id,question_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.result_scores (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), response_id uuid NOT NULL UNIQUE REFERENCES valorapesquisa.responses(id), total_score int NOT NULL, max_score int NOT NULL DEFAULT 125, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.dimension_scores (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), result_score_id uuid NOT NULL REFERENCES valorapesquisa.result_scores(id), dimension_id uuid NOT NULL REFERENCES valorapesquisa.dimensions(id), score int NOT NULL, max_score int NOT NULL DEFAULT 25, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(result_score_id,dimension_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.results (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), response_id uuid NOT NULL REFERENCES valorapesquisa.responses(id), result_score_id uuid REFERENCES valorapesquisa.result_scores(id), public_token_hash text UNIQUE, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.result_recommendations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), result_id uuid NOT NULL REFERENCES valorapesquisa.results(id), dimension_id uuid REFERENCES valorapesquisa.dimensions(id), text text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.certificates (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), result_id uuid NOT NULL REFERENCES valorapesquisa.results(id), validation_code text NOT NULL UNIQUE, issued_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.certificate_validations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), certificate_id uuid NOT NULL REFERENCES valorapesquisa.certificates(id), validated_at timestamptz NOT NULL DEFAULT now(), ip_hash text);
CREATE TABLE IF NOT EXISTS valorapesquisa.reports (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), name text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.exports (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), export_type text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.emails (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), recipient_hash text NOT NULL, subject text, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.whatsapp_messages (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), recipient_hash text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.communications (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), channel text NOT NULL, recipient_hash text NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.notifications (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id), title text NOT NULL, body text NOT NULL, read_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.action_plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), result_id uuid REFERENCES valorapesquisa.results(id), title text NOT NULL, status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
ALTER TABLE valorapesquisa.action_plans
  ADD COLUMN IF NOT EXISTS priority text NOT NULL DEFAULT 'medium',
  ADD COLUMN IF NOT EXISTS owner_name text,
  ADD COLUMN IF NOT EXISTS executive_sponsor text,
  ADD COLUMN IF NOT EXISTS due_at timestamptz,
  ADD COLUMN IF NOT EXISTS complexity text NOT NULL DEFAULT 'medium',
  ADD COLUMN IF NOT EXISTS expected_impact text,
  ADD COLUMN IF NOT EXISTS evidence jsonb NOT NULL DEFAULT '[]'::jsonb,
  ADD COLUMN IF NOT EXISTS linked_indicator text,
  ADD COLUMN IF NOT EXISTS history jsonb NOT NULL DEFAULT '[]'::jsonb;
CREATE TABLE IF NOT EXISTS valorapesquisa.lgpd_consents (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), participant_id uuid REFERENCES valorapesquisa.participants(id), consent_type text NOT NULL, granted_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.privacy_requests (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), requester_hash text NOT NULL, request_type text NOT NULL, protocol text NOT NULL DEFAULT encode(gen_random_bytes(24), 'hex'), status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS protocol text;
UPDATE valorapesquisa.privacy_requests SET protocol = encode(gen_random_bytes(24), 'hex') WHERE protocol IS NULL OR btrim(protocol) = '';
ALTER TABLE valorapesquisa.privacy_requests ALTER COLUMN protocol SET DEFAULT encode(gen_random_bytes(24), 'hex');
ALTER TABLE valorapesquisa.privacy_requests ALTER COLUMN protocol SET NOT NULL;
DO $$
DECLARE definition text;
BEGIN
  SELECT pg_get_indexdef(i.indexrelid) INTO definition
  FROM pg_index i JOIN pg_class c ON c.oid=i.indexrelid JOIN pg_namespace n ON n.oid=c.relnamespace
  WHERE n.nspname='valorapesquisa' AND c.relname='idx_privacy_requests_protocol';
  IF definition IS NOT NULL AND definition NOT ILIKE 'CREATE UNIQUE INDEX idx_privacy_requests_protocol ON valorapesquisa.privacy_requests USING btree (protocol)' THEN
    DROP INDEX valorapesquisa.idx_privacy_requests_protocol;
    definition := NULL;
  END IF;
  IF definition IS NULL THEN
    CREATE UNIQUE INDEX idx_privacy_requests_protocol ON valorapesquisa.privacy_requests(protocol);
  END IF;
END $$;
CREATE TABLE IF NOT EXISTS valorapesquisa.support_tickets (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), subject text NOT NULL, status text NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.integrations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), provider text NOT NULL, status text NOT NULL DEFAULT 'inactive', config jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.audit_logs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), actor_id uuid, action text NOT NULL, entity_name text, entity_id uuid, details jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS details jsonb;
ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS correlation_id text;
CREATE TABLE IF NOT EXISTS valorapesquisa.operational_logs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), level text NOT NULL, message text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.migration_batches (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), source_type text NOT NULL, source_name text NOT NULL, mode text NOT NULL, status text NOT NULL DEFAULT 'created', requested_by text, started_at timestamptz NOT NULL DEFAULT now(), finished_at timestamptz, total_records int NOT NULL DEFAULT 0, valid_records int NOT NULL DEFAULT 0, invalid_records int NOT NULL DEFAULT 0, imported_records int NOT NULL DEFAULT 0, skipped_records int NOT NULL DEFAULT 0, conflict_records int NOT NULL DEFAULT 0, error_records int NOT NULL DEFAULT 0, summary_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.migration_source_files (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), batch_id uuid REFERENCES valorapesquisa.migration_batches(id), file_name text NOT NULL, content_type text, size_bytes bigint NOT NULL, sha256 text NOT NULL, stored_path text, status text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.migration_records (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), batch_id uuid NOT NULL REFERENCES valorapesquisa.migration_batches(id), source_file_id uuid REFERENCES valorapesquisa.migration_source_files(id), legacy_collection text NOT NULL, legacy_id text, target_entity text NOT NULL, target_id uuid, action text NOT NULL, status text NOT NULL, input_json jsonb NOT NULL DEFAULT '{}'::jsonb, normalized_json jsonb NOT NULL DEFAULT '{}'::jsonb, error_code text, error_message text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.migration_mappings (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), batch_id uuid NOT NULL REFERENCES valorapesquisa.migration_batches(id), legacy_collection text NOT NULL, legacy_id text NOT NULL, target_entity text NOT NULL, target_id uuid NOT NULL, mapping_key text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(batch_id,legacy_collection,legacy_id,target_entity));
CREATE TABLE IF NOT EXISTS valorapesquisa.migration_conflicts (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), batch_id uuid NOT NULL REFERENCES valorapesquisa.migration_batches(id), legacy_collection text NOT NULL, legacy_id text, target_entity text NOT NULL, target_id uuid, conflict_type text NOT NULL, severity text NOT NULL, legacy_value_json jsonb NOT NULL DEFAULT '{}'::jsonb, current_value_json jsonb NOT NULL DEFAULT '{}'::jsonb, resolution text, resolved_by text, resolved_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.migration_checkpoints (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), migration_name text NOT NULL UNIQUE, checkpoint_data jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.rollback_records (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), batch_id uuid NOT NULL REFERENCES valorapesquisa.migration_batches(id), target_entity text NOT NULL, target_id uuid NOT NULL, operation text NOT NULL, before_json jsonb, after_json jsonb, status text NOT NULL, rolled_back_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.outbox_messages (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), aggregate_id uuid, message_type text NOT NULL, payload jsonb NOT NULL, status text NOT NULL DEFAULT 'pending', created_at timestamptz NOT NULL DEFAULT now(), processed_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.idempotency_keys (key text PRIMARY KEY, organization_id uuid REFERENCES valorapesquisa.organizations(id), request_hash text NOT NULL, response_body jsonb, created_at timestamptz NOT NULL DEFAULT now(), expires_at timestamptz NOT NULL);
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_usage_counters (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), metric_key text NOT NULL, period_start date NOT NULL, consumed bigint NOT NULL DEFAULT 0 CHECK(consumed>=0), reserved bigint NOT NULL DEFAULT 0 CHECK(reserved>=0), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(organization_id,metric_key,period_start));
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_usage_reservations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), metric_key text NOT NULL, quantity bigint NOT NULL CHECK(quantity>0), status text NOT NULL DEFAULT 'reserved' CHECK(status IN ('reserved','confirmed','released','expired')), idempotency_key text NOT NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(organization_id,idempotency_key));
CREATE TABLE IF NOT EXISTS valorapesquisa.user_scopes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), scope_type text NOT NULL CHECK(scope_type IN ('business_group','legal_entity','unit','department')), scope_id uuid NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), created_by_user_id uuid REFERENCES valorapesquisa.users(id), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_history (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), subscription_id uuid NOT NULL REFERENCES valorapesquisa.subscriptions(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), previous_status text, new_status text NOT NULL, previous_plan_id uuid REFERENCES valorapesquisa.plans(id), new_plan_id uuid REFERENCES valorapesquisa.plans(id), changed_by uuid REFERENCES valorapesquisa.users(id), reason text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_subscription_history_organization ON valorapesquisa.subscription_history(organization_id,created_at DESC);

ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS public_name text;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS email text;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS phone text;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS default_language_code text NOT NULL DEFAULT 'pt-BR';
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS time_zone text NOT NULL DEFAULT 'America/Belem';
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS onboarding_status text NOT NULL DEFAULT 'pending';
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS version bigint NOT NULL DEFAULT 1;

ALTER TABLE valorapesquisa.users ADD COLUMN IF NOT EXISTS phone text;
ALTER TABLE valorapesquisa.users ADD COLUMN IF NOT EXISTS access_version bigint NOT NULL DEFAULT 1;
ALTER TABLE valorapesquisa.users ADD COLUMN IF NOT EXISTS password_reset_required boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.users ADD COLUMN IF NOT EXISTS last_login_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email_active ON valorapesquisa.users(lower(email)) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.addresses (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 legal_entity_id uuid REFERENCES valorapesquisa.legal_entities(id), address_type text NOT NULL DEFAULT 'headquarters',
 street text, number text, complement text, district text, city text, state char(2), postal_code text, country_code char(2) NOT NULL DEFAULT 'BR',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.user_roles (
 user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), role_id uuid NOT NULL REFERENCES valorapesquisa.roles(id),
 created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(user_id, role_id));

-- A legacy user_sessions table can predate any of these columns.  Keep the
-- compatibility columns nullable here: historical rows must never receive an
-- arbitrary tenant or user merely to satisfy a constraint.
ALTER TABLE valorapesquisa.user_sessions
    ADD COLUMN IF NOT EXISTS organization_id uuid,
    ADD COLUMN IF NOT EXISTS user_id uuid,
    ADD COLUMN IF NOT EXISTS expires_at timestamptz,
    ADD COLUMN IF NOT EXISTS revoked_at timestamptz,
    ADD COLUMN IF NOT EXISTS status text DEFAULT 'active',
    ADD COLUMN IF NOT EXISTS last_used_at timestamptz,
    ADD COLUMN IF NOT EXISTS ip_hash text,
    ADD COLUMN IF NOT EXISTS user_agent text,
    ADD COLUMN IF NOT EXISTS revocation_reason text,
    ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();

UPDATE valorapesquisa.user_sessions
SET status = 'active'
WHERE status IS NULL;

ALTER TABLE valorapesquisa.user_sessions
    ALTER COLUMN status SET DEFAULT 'active';

CREATE INDEX IF NOT EXISTS ix_user_sessions_user_active
    ON valorapesquisa.user_sessions(user_id, expires_at)
    WHERE revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.refresh_token_families (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), session_id uuid NOT NULL REFERENCES valorapesquisa.user_sessions(id),
 created_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz, revocation_reason text);
ALTER TABLE valorapesquisa.refresh_tokens ADD COLUMN IF NOT EXISTS family_id uuid REFERENCES valorapesquisa.refresh_token_families(id);
ALTER TABLE valorapesquisa.refresh_tokens ADD COLUMN IF NOT EXISTS session_id uuid REFERENCES valorapesquisa.user_sessions(id);
ALTER TABLE valorapesquisa.refresh_tokens ADD COLUMN IF NOT EXISTS used_at timestamptz;
ALTER TABLE valorapesquisa.refresh_tokens ADD COLUMN IF NOT EXISTS replaced_by_id uuid REFERENCES valorapesquisa.refresh_tokens(id);
ALTER TABLE valorapesquisa.refresh_tokens ADD COLUMN IF NOT EXISTS revocation_reason text;
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_family ON valorapesquisa.refresh_tokens(family_id, created_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_replaced_by ON valorapesquisa.refresh_tokens(replaced_by_id) WHERE replaced_by_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_session_active ON valorapesquisa.refresh_tokens(session_id,expires_at) WHERE revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.login_attempts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), email_hash text NOT NULL, ip_hash text,
 succeeded boolean NOT NULL, attempted_at timestamptz NOT NULL DEFAULT now(), blocked_until timestamptz);
CREATE INDEX IF NOT EXISTS ix_login_attempts_window ON valorapesquisa.login_attempts(email_hash, attempted_at DESC);

-- Instalações anteriores podem conter apenas parte da estrutura de recuperação.
-- Todas as colunas são garantidas antes de qualquer constraint ou índice que as use.
ALTER TABLE valorapesquisa.password_reset_tokens
    ADD COLUMN IF NOT EXISTS organization_id uuid,
    ADD COLUMN IF NOT EXISTS user_id uuid,
    ADD COLUMN IF NOT EXISTS token_hash text,
    ADD COLUMN IF NOT EXISTS expires_at timestamptz,
    ADD COLUMN IF NOT EXISTS used_at timestamptz,
    ADD COLUMN IF NOT EXISTS request_ip_hash text,
    ADD COLUMN IF NOT EXISTS user_agent text,
    ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now(),
    ADD COLUMN IF NOT EXISTS updated_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_password_reset_valid ON valorapesquisa.password_reset_tokens(token_hash, expires_at) WHERE used_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_consents (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), consent_type text NOT NULL, version text NOT NULL,
 accepted_at timestamptz NOT NULL DEFAULT now(), ip_hash text, UNIQUE(organization_id,user_id,consent_type,version));
CREATE TABLE IF NOT EXISTS valorapesquisa.onboarding_steps (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 step_code text NOT NULL, status text NOT NULL DEFAULT 'pending', completed_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(organization_id,step_code));

CREATE TABLE IF NOT EXISTS valorapesquisa.user_invitations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 email text NOT NULL, normalized_email text NOT NULL, name text NOT NULL, token_hash text,
 status text NOT NULL DEFAULT 'pending' CHECK(status IN ('pending','accepted','cancelled','expired')),
 expires_at timestamptz NOT NULL, accepted_at timestamptz, cancelled_at timestamptz, invited_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 resend_count integer NOT NULL DEFAULT 0, last_sent_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.user_invitation_roles (
 invitation_id uuid NOT NULL REFERENCES valorapesquisa.user_invitations(id) ON DELETE CASCADE, role_id uuid NOT NULL REFERENCES valorapesquisa.roles(id),
 created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(invitation_id,role_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.user_invitation_scopes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), invitation_id uuid NOT NULL REFERENCES valorapesquisa.user_invitations(id) ON DELETE CASCADE,
 scope_type text NOT NULL CHECK(scope_type IN ('business_group','legal_entity','unit','department')), scope_id uuid NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(invitation_id,scope_type,scope_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.email_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_key text NOT NULL, language_code text NOT NULL DEFAULT 'pt-BR',
 subject_template text NOT NULL, body_template text NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
 UNIQUE(template_key,language_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.email_jobs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), recipient_email text NOT NULL,
 subject text NOT NULL, template_key text NOT NULL, payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 status text NOT NULL DEFAULT 'queued' CHECK(status IN ('queued','processing','sent','failed','retrying','dead_letter','cancelled')),
 idempotency_key text NOT NULL, attempts integer NOT NULL DEFAULT 0, max_attempts integer NOT NULL DEFAULT 5,
 next_attempt_at timestamptz NOT NULL DEFAULT now(), last_error text, sent_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(idempotency_key));
CREATE INDEX IF NOT EXISTS ix_email_jobs_dispatch ON valorapesquisa.email_jobs(next_attempt_at,created_at) WHERE status IN ('queued','retrying');

ALTER TABLE valorapesquisa.outbox_messages ADD COLUMN IF NOT EXISTS idempotency_key text;
ALTER TABLE valorapesquisa.outbox_messages ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.outbox_messages ADD COLUMN IF NOT EXISTS next_attempt_at timestamptz NOT NULL DEFAULT now();
CREATE UNIQUE INDEX IF NOT EXISTS ux_outbox_idempotency ON valorapesquisa.outbox_messages(idempotency_key) WHERE idempotency_key IS NOT NULL;

-- CONVERGÊNCIA DE CHAVES NATURAIS ------------------------------------------------
-- CREATE TABLE IF NOT EXISTS não adiciona constraints a uma tabela que já
-- existia. Esta fase é, por isso, deliberadamente anterior a TODOS os seeds a
-- seguir. Linhas legadas não-canônicas são preservadas e recebem uma chave
-- técnica inequívoca; IDs e todas as referências continuam inalterados.
CREATE TABLE IF NOT EXISTS valorapesquisa.constraint_convergence_audit (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), table_name text NOT NULL,
 canonical_key text NOT NULL, preserved_row_id text NOT NULL,
 resolution text NOT NULL, recorded_at timestamptz NOT NULL DEFAULT now());

DO $converge$
DECLARE target record; changed record;
BEGIN
  FOR target IN SELECT * FROM (VALUES
    ('modules','code','created_at'),('permissions','code','created_at'),
    ('plans','code','created_at'),('forms','code','created_at')
  ) AS natural_key(table_name,column_name,order_column)
  LOOP
    FOR changed IN EXECUTE format(
      'SELECT id_text, old_key FROM (SELECT %1$I::text id_text, %2$I::text old_key, row_number() OVER (PARTITION BY %2$I ORDER BY %3$I NULLS LAST, %1$I) occurrence FROM valorapesquisa.%4$I) d WHERE occurrence>1',
      'id',target.column_name,target.order_column,target.table_name)
    LOOP
      EXECUTE format('UPDATE valorapesquisa.%I SET %I=%I || ''-legacy-'' || left(md5(%I::text),8) WHERE %I::text=$1',
        target.table_name,target.column_name,target.column_name,
        'id','id') USING changed.id_text;
      INSERT INTO valorapesquisa.constraint_convergence_audit(table_name,canonical_key,preserved_row_id,resolution)
      VALUES(target.table_name,changed.old_key,changed.id_text,'chave legada renomeada; linha e referências preservadas');
    END LOOP;
  END LOOP;
END $converge$;

-- schema_migrations pode não ter PK em bancos parciais e não possui UUID.
WITH d AS (SELECT ctid,version,row_number() OVER(PARTITION BY version ORDER BY applied_at NULLS LAST,ctid) n FROM valorapesquisa.schema_migrations)
UPDATE valorapesquisa.schema_migrations x SET version=x.version||'-legacy-'||d.n::text FROM d WHERE x.ctid=d.ctid AND d.n>1;

-- Chaves compostas: apenas a chave natural da ocorrência excedente é
-- rebatizada. Isso evita DELETE silencioso e não invalida FKs pelo ID.
WITH d AS (SELECT id,limit_key,row_number() OVER(PARTITION BY plan_id,limit_key ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.plan_limits)
UPDATE valorapesquisa.plan_limits x SET limit_key=x.limit_key||'-legacy-'||left(replace(x.id::text,'-',''),8),updated_at=now() FROM d WHERE d.id=x.id AND d.n>1;
WITH d AS (SELECT id,capability_key,row_number() OVER(PARTITION BY plan_id,capability_key ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.plan_capabilities)
UPDATE valorapesquisa.plan_capabilities x SET capability_key=x.capability_key||'-legacy-'||left(replace(x.id::text,'-',''),8),updated_at=now() FROM d WHERE d.id=x.id AND d.n>1;
WITH d AS (
 SELECT id, row_number() OVER(PARTITION BY form_id,version,language ORDER BY created_at NULLS LAST,id) n,
        max(version) OVER(PARTITION BY form_id,language) max_version
 FROM valorapesquisa.form_versions
)
UPDATE valorapesquisa.form_versions x SET version=d.max_version+d.n-1,version_number=d.max_version+d.n-1,updated_at=now() FROM d WHERE d.id=x.id AND d.n>1;
WITH d AS (SELECT id,language,row_number() OVER(PARTITION BY form_version_id,language ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.form_translations)
UPDATE valorapesquisa.form_translations x SET language=x.language||'-legacy-'||left(replace(x.id::text,'-',''),8) FROM d WHERE d.id=x.id AND d.n>1;
WITH d AS (SELECT id,code,row_number() OVER(PARTITION BY form_version_id,code ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.dimensions)
UPDATE valorapesquisa.dimensions x SET code=x.code||'-legacy-'||left(replace(x.id::text,'-',''),8) FROM d WHERE d.id=x.id AND d.n>1;
WITH d AS (SELECT id,code,row_number() OVER(PARTITION BY dimension_id,code ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.questions)
UPDATE valorapesquisa.questions x SET code=x.code||'-legacy-'||left(replace(x.id::text,'-',''),8) FROM d WHERE d.id=x.id AND d.n>1;

-- Nomes versionados evitam o caso perigoso em que um índice antigo, com o
-- mesmo nome porém sem UNIQUE, faz CREATE UNIQUE INDEX IF NOT EXISTS “pular”.
CREATE UNIQUE INDEX IF NOT EXISTS ux_modules_code_v2 ON valorapesquisa.modules(code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_permissions_code_v2 ON valorapesquisa.permissions(code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_plans_code_v2 ON valorapesquisa.plans(code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_limits_plan_key_v2 ON valorapesquisa.plan_limits(plan_id,limit_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_capabilities_plan_key_v2 ON valorapesquisa.plan_capabilities(plan_id,capability_key);
CREATE UNIQUE INDEX IF NOT EXISTS ux_forms_code_v2 ON valorapesquisa.forms(code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_versions_identity_v2 ON valorapesquisa.form_versions(form_id,version,language);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_translations_identity_v2 ON valorapesquisa.form_translations(form_version_id,language);
CREATE UNIQUE INDEX IF NOT EXISTS ux_dimensions_identity_v2 ON valorapesquisa.dimensions(form_version_id,code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_questions_identity_v2 ON valorapesquisa.questions(dimension_id,code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_schema_migrations_version_v2 ON valorapesquisa.schema_migrations(version);

INSERT INTO valorapesquisa.modules(code,name,category,status) VALUES
('identity','Identidade','core','active'),('organization','Organização','core','active'),('forms','Diagnósticos','product','active'),('surveys','Pesquisas','product','active'),('distribution','Distribuição','product','active'),('responses','Respostas','product','active'),('results','Resultados','product','active'),('certificates','Certificados','product','active'),('communications','Comunicações','support','active'),('audit','Auditoria','governance','active'),('settings','Configurações','governance','active'),('operations','Operações','governance','active')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,category=EXCLUDED.category,status=EXCLUDED.status,updated_at=now();

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('organization.current.read','Visualizar organização','Consulta a organização corrente.','identity'),
('organization.current.update','Atualizar organização','Atualiza a organização corrente.','identity'),
('users.read','Visualizar usuários','Consulta usuários do tenant.','identity'),
('users.create','Criar usuários','Cria usuários no tenant.','identity'),
('users.update','Atualizar usuários','Atualiza usuários no tenant.','identity'),
('users.disable','Desativar usuários','Desativa usuários no tenant.','identity'),
('sessions.read','Visualizar sessões','Consulta sessões próprias.','identity'),
('sessions.revoke','Revogar sessões','Revoga sessões próprias.','identity')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('organization.read','Visualizar organização','Consulta os dados organizacionais.','organization'),('organization.update','Atualizar organização','Atualiza dados organizacionais.','organization'),
('organization.branding.read','Visualizar branding','Consulta a identidade visual.','organization'),('organization.branding.update','Atualizar branding','Atualiza a identidade visual.','organization'),
('organization.subscription.read','Visualizar assinatura','Consulta plano e assinatura.','organization'),('organization.usage.read','Visualizar consumo','Consulta consumo e limites.','organization'),
('organization.onboarding.read','Visualizar onboarding','Consulta o onboarding.','identity'),('organization.onboarding.update','Atualizar onboarding','Conclui passos manuais.','identity'),
('invitations.read','Visualizar convites','Consulta convites de usuários.','identity'),('invitations.create','Criar convites','Cria convites de usuários.','identity'),('invitations.resend','Reenviar convites','Reenvia convites pendentes.','identity'),('invitations.cancel','Cancelar convites','Cancela convites pendentes.','identity'),
('business_groups.read','Visualizar grupos','Consulta grupos econômicos.','organization'),('business_groups.create','Criar grupos','Cria grupos econômicos.','organization'),('business_groups.update','Atualizar grupos','Atualiza grupos econômicos.','organization'),('business_groups.disable','Desativar grupos','Desativa grupos econômicos.','organization'),('business_groups.delete','Excluir grupos','Exclui logicamente grupos econômicos.','organization'),
('legal_entities.read','Visualizar empresas','Consulta pessoas jurídicas.','organization'),('legal_entities.create','Criar empresas','Cria pessoas jurídicas.','organization'),('legal_entities.update','Atualizar empresas','Atualiza pessoas jurídicas.','organization'),('legal_entities.disable','Desativar empresas','Desativa pessoas jurídicas.','organization'),('legal_entities.delete','Excluir empresas','Exclui logicamente pessoas jurídicas.','organization'),
('units.read','Visualizar unidades','Consulta unidades.','organization'),('units.create','Criar unidades','Cria unidades.','organization'),('units.update','Atualizar unidades','Atualiza unidades.','organization'),('units.disable','Desativar unidades','Desativa unidades.','organization'),('units.delete','Excluir unidades','Exclui logicamente unidades.','organization'),
('departments.read','Visualizar setores','Consulta setores.','organization'),('departments.create','Criar setores','Cria setores.','organization'),('departments.update','Atualizar setores','Atualiza setores.','organization'),('departments.disable','Desativar setores','Desativa setores.','organization'),('departments.delete','Excluir setores','Exclui logicamente setores.','organization')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code;


INSERT INTO valorapesquisa.permissions(code,name,description,module_code,functional_group,risk_level,display_order) VALUES
('users.assign_roles','Associar papéis','Altera os papéis de um usuário.','identity','users','high',50),('users.assign_scopes','Associar escopos','Altera os escopos de um usuário.','identity','users','high',60),
('roles.read','Visualizar papéis','Consulta papéis e suas permissões.','identity','roles','low',100),('roles.create','Criar papéis','Cria papéis personalizados.','identity','roles','medium',110),('roles.update','Editar papéis','Edita papéis personalizados.','identity','roles','high',120),('roles.delete','Excluir papéis','Exclui papéis personalizados sem usuários.','identity','roles','high',130),('roles.assign_permissions','Associar permissões','Substitui permissões e invalida sessões afetadas.','identity','roles','critical',140),
('forms.read','Visualizar formulários','Consulta diagnósticos.','forms','forms','low',200),('forms.create','Criar formulários','Cria diagnósticos.','forms','forms','medium',210),('forms.update','Editar formulários','Edita diagnósticos.','forms','forms','medium',220),('forms.publish','Publicar formulários','Publica diagnósticos.','forms','forms','high',230),('forms.archive','Arquivar formulários','Arquiva diagnósticos.','forms','forms','high',240),('forms.restore','Restaurar formulários','Restaura diagnósticos.','forms','forms','medium',250),
('surveys.read','Visualizar pesquisas','Consulta pesquisas.','surveys','surveys','low',300),('surveys.create','Criar pesquisas','Cria pesquisas.','surveys','surveys','medium',310),('surveys.update','Editar pesquisas','Edita pesquisas.','surveys','surveys','medium',320),('surveys.publish','Publicar pesquisas','Publica pesquisas.','surveys','surveys','high',330),('surveys.distribute','Distribuir pesquisas','Distribui pesquisas.','distribution','surveys','high',340),('surveys.close','Encerrar pesquisas','Encerra pesquisas.','surveys','surveys','high',350),
('responses.read','Visualizar respostas','Consulta respostas.','responses','responses','high',400),('responses.export','Exportar respostas','Exporta respostas.','responses','responses','high',410),('responses.anonymize','Anonimizar respostas','Remove identificadores de respostas.','responses','responses','critical',420),
('results.read','Visualizar resultados','Consulta resultados.','results','results','low',500),('results.export','Exportar resultados','Exporta resultados.','results','results','medium',510),('results.compare','Comparar resultados','Compara ciclos.','results','results','medium',520),
('certificates.read','Visualizar certificados','Consulta certificados.','certificates','certificates','low',600),('certificates.generate','Gerar certificados','Gera certificados.','certificates','certificates','medium',610),('certificates.revoke','Revogar certificados','Revoga certificados.','certificates','certificates','high',620),
('communications.read','Visualizar comunicações','Consulta comunicações.','communications','communications','low',700),('communications.send','Enviar comunicações','Envia comunicações.','communications','communications','high',710),('communications.retry','Reprocessar comunicações','Reprocessa falhas.','communications','communications','high',720),('communications.cancel','Cancelar comunicações','Cancela envios.','communications','communications','high',730),
('audit.read','Visualizar auditoria','Consulta trilha de auditoria.','audit','governance','high',800),('operations.read','Visualizar operações','Consulta operações.','operations','operations','high',900),('operations.execute','Executar operações','Executa operação administrativa.','operations','operations','critical',910),('settings.read','Visualizar configurações','Consulta configurações.','settings','settings','low',1000),('settings.update','Editar configurações','Edita configurações.','settings','settings','high',1010)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,functional_group=EXCLUDED.functional_group,risk_level=EXCLUDED.risk_level,display_order=EXCLUDED.display_order,updated_at=now();

-- Known legacy aliases are converged to the single canonical code; unknown codes remain nullable and visible for review.
UPDATE valorapesquisa.permissions SET code='responses.read',module_code='responses',updated_at=now() WHERE code='canViewResponses' AND NOT EXISTS(SELECT 1 FROM valorapesquisa.permissions WHERE code='responses.read');
UPDATE valorapesquisa.permissions SET module_code=split_part(code,'.',1),updated_at=now() WHERE module_code IS NULL AND split_part(code,'.',1)=ANY(ARRAY['identity','organization','forms','surveys','distribution','responses','results','certificates','communications','audit','settings','operations']);
CREATE TABLE IF NOT EXISTS valorapesquisa.permission_migration_reviews(permission_id uuid PRIMARY KEY REFERENCES valorapesquisa.permissions(id),permission_code text NOT NULL,reason text NOT NULL,first_seen_at timestamptz NOT NULL DEFAULT now(),last_seen_at timestamptz NOT NULL DEFAULT now(),resolved_at timestamptz);
ALTER TABLE valorapesquisa.permission_migration_reviews ADD COLUMN IF NOT EXISTS permission_id uuid;
ALTER TABLE valorapesquisa.permission_migration_reviews ADD COLUMN IF NOT EXISTS permission_code text;
ALTER TABLE valorapesquisa.permission_migration_reviews ADD COLUMN IF NOT EXISTS reason text;
ALTER TABLE valorapesquisa.permission_migration_reviews ADD COLUMN IF NOT EXISTS first_seen_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.permission_migration_reviews ADD COLUMN IF NOT EXISTS last_seen_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.permission_migration_reviews ADD COLUMN IF NOT EXISTS resolved_at timestamptz;
UPDATE valorapesquisa.permission_migration_reviews SET first_seen_at=COALESCE(first_seen_at,now()),last_seen_at=COALESCE(last_seen_at,first_seen_at,now()),permission_code=COALESCE(permission_code,'legacy-review'),reason=COALESCE(reason,'revisão legada importada');
-- O erro reportado ocorria exatamente no ON CONFLICT(permission_id) abaixo:
-- uma tabela legada já existia sem PK/UNIQUE e o CREATE TABLE era ignorado.
-- Consolidamos somente duplicatas desta fila de revisão (mantendo a ocorrência
-- mais antiga e o last_seen_at mais recente) e instalamos uma chave nova cujo
-- nome não colide com o índice legado ux_permission_migration_reviews.
WITH ranked AS (
 SELECT ctid,permission_id,row_number() OVER(PARTITION BY permission_id ORDER BY first_seen_at NULLS LAST,ctid) n,
        max(last_seen_at) OVER(PARTITION BY permission_id) newest_seen
 FROM valorapesquisa.permission_migration_reviews
), merged AS (
 UPDATE valorapesquisa.permission_migration_reviews r SET last_seen_at=ranked.newest_seen
 FROM ranked WHERE r.ctid=ranked.ctid AND ranked.n=1 RETURNING r.permission_id
)
DELETE FROM valorapesquisa.permission_migration_reviews r USING ranked
WHERE r.ctid=ranked.ctid AND ranked.n>1;
CREATE UNIQUE INDEX IF NOT EXISTS ux_permission_migration_reviews_permission_v2 ON valorapesquisa.permission_migration_reviews(permission_id);
INSERT INTO valorapesquisa.permission_migration_reviews(permission_id,permission_code,reason) SELECT id,code,'module_code não pôde ser inferido com segurança' FROM valorapesquisa.permissions WHERE module_code IS NULL ON CONFLICT(permission_id) DO UPDATE SET permission_code=EXCLUDED.permission_code,last_seen_at=now();


DO $$
DECLARE t text; trigger_name text; expected text; current_definition text;
BEGIN
  FOREACH t IN ARRAY ARRAY['organizations','business_groups','legal_entities','units','departments','users','roles','plans','plan_limits','plan_capabilities','subscriptions','usage_monthly','usage_lifetime','modules','organization_modules','organization_settings','organization_branding','surveys','survey_cycles','survey_invites','responses','result_scores','reports','exports','emails','whatsapp_messages','communications','action_plans','privacy_requests','support_tickets','integrations','migration_checkpoints'] LOOP
    trigger_name := format('trg_%s_updated_at', t);
    expected := format('CREATE TRIGGER %I BEFORE UPDATE ON valorapesquisa.%I FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at()', trigger_name, t);
    SELECT pg_get_triggerdef(g.oid) INTO current_definition FROM pg_trigger g JOIN pg_class c ON c.oid=g.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='valorapesquisa' AND c.relname=t AND g.tgname=trigger_name AND NOT g.tgisinternal;
    IF current_definition IS NULL THEN EXECUTE expected;
    ELSIF regexp_replace(current_definition, '\s+', ' ', 'g') <> regexp_replace(expected, '\s+', ' ', 'g') THEN
      EXECUTE format('DROP TRIGGER %I ON valorapesquisa.%I', trigger_name, t); EXECUTE expected;
    END IF;
  END LOOP;
END $$;

INSERT INTO valorapesquisa.plans(code,name,is_public,is_active,is_legacy) VALUES
('free','Gratuito',true,true,false),('professional','Profissional',true,true,false),('corporate','Corporativo',true,true,false),('enterprise','Enterprise',true,true,false),('essential','Essential legado',false,false,true),('growth','Growth legado',false,false,true)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,is_public=EXCLUDED.is_public,is_active=EXCLUDED.is_active,is_legacy=EXCLUDED.is_legacy,updated_at=now();
WITH configured_limits(limit_key, free_value, professional_value, corporate_value, enterprise_value) AS (VALUES
('legalEntities'::text,1::integer,1::integer,1::integer,NULL::integer),('units'::text,1::integer,1::integer,NULL::integer,NULL::integer),('departments'::text,3::integer,20::integer,NULL::integer,NULL::integer),
('users'::text,3::integer,20::integer,100::integer,NULL::integer),('managers'::text,1::integer,5::integer,25::integer,NULL::integer),('activeSurveys'::text,1::integer,5::integer,25::integer,NULL::integer),
('monthlyResponses'::text,100::integer,1000::integer,10000::integer,NULL::integer),('lifetimeResponses'::text,500::integer,NULL::integer,NULL::integer,NULL::integer),
('monthlyEmailInvites'::text,100::integer,2000::integer,20000::integer,NULL::integer),('diagnosticCycles'::text,1::integer,12::integer,NULL::integer,NULL::integer),
('languages'::text,1::integer,2::integer,4::integer,NULL::integer),('storageMb'::text,100::integer,2048::integer,10240::integer,NULL::integer))
INSERT INTO valorapesquisa.plan_limits(plan_id,limit_key,limit_value)
SELECT p.id,l.limit_key,CASE p.code WHEN 'free' THEN l.free_value WHEN 'professional' THEN l.professional_value WHEN 'corporate' THEN l.corporate_value ELSE l.enterprise_value END
FROM plans p CROSS JOIN configured_limits l WHERE p.code IN ('free','professional','corporate','enterprise')
ON CONFLICT(plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value,updated_at=now();
WITH capabilities(capability_key) AS (VALUES
('officialValoraProgram'),('shareLink'),('shareEmail'),('whatsappPreview'),('basicResult'),
('crossSurveyAnalysis'),('crossDepartmentAnalysis'),('actionPlan'),('organizationReport'),
('multipleUnits'),('unitComparison'),('consolidatedReports'),('franchiseMode'),
('multipleLegalEntities'),('businessGroupManagement'),('intercompanyComparison'),('groupDashboard'),
('whiteLabel'),('integrations'),('executiveFollowUp'))
INSERT INTO valorapesquisa.plan_capabilities(plan_id,capability,capability_code,capability_key,enabled,is_enabled)
SELECT p.id,c.capability_key,c.capability_key,c.capability_key,
CASE p.code
 WHEN 'free' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','basicResult')
 WHEN 'professional' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','crossSurveyAnalysis','crossDepartmentAnalysis','actionPlan','organizationReport')
 WHEN 'corporate' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','crossSurveyAnalysis','crossDepartmentAnalysis','actionPlan','organizationReport','multipleUnits','unitComparison','consolidatedReports','franchiseMode')
 ELSE true END,
CASE p.code
 WHEN 'free' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','basicResult')
 WHEN 'professional' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','crossSurveyAnalysis','crossDepartmentAnalysis','actionPlan','organizationReport')
 WHEN 'corporate' THEN c.capability_key IN ('officialValoraProgram','shareLink','shareEmail','whatsappPreview','crossSurveyAnalysis','crossDepartmentAnalysis','actionPlan','organizationReport','multipleUnits','unitComparison','consolidatedReports','franchiseMode')
 ELSE true END
FROM plans p CROSS JOIN capabilities c WHERE p.code IN ('free','professional','corporate','enterprise')
ON CONFLICT(plan_id,capability_key) DO UPDATE SET capability=EXCLUDED.capability,capability_code=EXCLUDED.capability_code,enabled=EXCLUDED.enabled,is_enabled=EXCLUDED.is_enabled,updated_at=now();
INSERT INTO valorapesquisa.forms(organization_id,code,name,title,slug,form_key,status,questions_count,version,estimated_minutes,created_at)
SELECT id,'valora-official','Pesquisa Oficial Valora','Pesquisa Oficial Valora','valora-official','valora-official','active',25,1,15,now()
FROM valorapesquisa.organizations WHERE slug='valora-platform'
ON CONFLICT(code) DO UPDATE SET organization_id=EXCLUDED.organization_id,name=EXCLUDED.name,title=EXCLUDED.title,slug=EXCLUDED.slug,form_key=EXCLUDED.form_key,status='active',questions_count=EXCLUDED.questions_count,version=GREATEST(forms.version,1),estimated_minutes=15,deleted_at=NULL,updated_at=now();
INSERT INTO valorapesquisa.form_versions(form_id,organization_id,version,version_number,language,is_immutable,maximum_score,max_score,status,published_at) SELECT id,organization_id,1,1,'pt-BR',true,125,125,'published',now() FROM forms WHERE code='valora-official' ON CONFLICT(form_id,version,language) DO UPDATE SET organization_id=EXCLUDED.organization_id,version_number=1,maximum_score=125,max_score=125,status='published',is_immutable=true,published_at=COALESCE(form_versions.published_at,now()),updated_at=now();
INSERT INTO valorapesquisa.form_translations(form_version_id,language,title) SELECT id,lang,'Valora Insight' FROM form_versions CROSS JOIN (VALUES('pt-BR'),('en'),('es'),('zh-Hans')) l(lang) ON CONFLICT(form_version_id,language) DO NOTHING;
WITH fv AS (SELECT id FROM form_versions WHERE form_id=(SELECT id FROM forms WHERE code='valora-official') AND version=1 AND language='pt-BR'), d(code,name,ord) AS (VALUES ('culture','Cultura e Propósito',1),('governance','Gestão e Governança',2),('leadership','Liderança',3),('people','Pessoas e Talentos',4),('growth','Resultados e Crescimento',5)) INSERT INTO valorapesquisa.dimensions(form_version_id,code,name,position,display_order,max_score) SELECT fv.id,d.code,d.name,d.ord,d.ord,25 FROM fv,d ON CONFLICT(form_version_id,code) DO UPDATE SET name=EXCLUDED.name,position=EXCLUDED.position,display_order=EXCLUDED.display_order,max_score=EXCLUDED.max_score;
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
INSERT INTO valorapesquisa.questions(organization_id,form_id,form_version_id,dimension_id,code,title,text,description,type,min_value,max_value,required,is_required,is_active,is_qualitative,weight,position,display_order,version,deleted_at,created_at,updated_at,max_text_length,anonymity_protected)
SELECT f.organization_id,f.id,fv.id,d.id,o.code,o.text,o.text,NULL,valorapesquisa.compatible_scale_question_type(),1,5,true,true,true,false,1.00,o.display_order,o.display_order,1,NULL,now(),now(),NULL,false
FROM official o JOIN dimensions d ON d.code=o.dimension_code JOIN form_versions fv ON fv.id=d.form_version_id JOIN forms f ON f.id=fv.form_id AND f.code='valora-official'
ON CONFLICT(dimension_id,code) DO UPDATE SET organization_id=EXCLUDED.organization_id,form_id=EXCLUDED.form_id,form_version_id=EXCLUDED.form_version_id,title=EXCLUDED.title,text=EXCLUDED.text,type=EXCLUDED.type,position=EXCLUDED.position,display_order=EXCLUDED.display_order,min_value=1,max_value=5,required=true,is_required=true,is_active=true,is_qualitative=false,weight=1.00,version=1,deleted_at=NULL,updated_at=now();
-- A versão oficial contém 25 questões pontuadas (5 x 5) e uma questão aberta,
-- que amplia contexto sem alterar a pontuação máxima de 125.
INSERT INTO valorapesquisa.questions(organization_id,form_id,form_version_id,dimension_id,code,title,text,description,type,min_value,max_value,required,is_required,is_active,is_qualitative,weight,position,display_order,version,deleted_at,created_at,updated_at,max_text_length,anonymity_protected)
SELECT f.organization_id,f.id,fv.id,d.id,'qualitative-work-feeling',
 'Na sua percepção, o que mais ajudaria esta organização a evoluir?',
 'Na sua percepção, o que mais ajudaria esta organização a evoluir?',
 'Resposta aberta, opcional e não pontuada.',valorapesquisa.compatible_text_question_type(),1,5,
 false,false,true,true,0.00,6,6,1,NULL,now(),now(),2000,true
FROM valorapesquisa.forms f
JOIN valorapesquisa.form_versions fv ON fv.form_id=f.id AND fv.version=1 AND fv.language='pt-BR'
JOIN valorapesquisa.dimensions d ON d.form_version_id=fv.id AND d.code='growth'
WHERE f.code='valora-official'
ON CONFLICT(dimension_id,code) DO UPDATE SET title=EXCLUDED.title,text=EXCLUDED.text,
 description=EXCLUDED.description,type=EXCLUDED.type,required=false,is_required=false,is_active=true,
 is_qualitative=true,weight=0.00,position=6,display_order=6,deleted_at=NULL,updated_at=now();
INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('script_completo_2026_07','script-completo-v1') ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();
-- Fase 2G: invariantes multiempresa, RBAC por escopo e reservas de limites.
-- Migration aditiva e idempotente; não remove dados ou tabelas legadas.
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
SET LOCAL search_path TO valorapesquisa, public;

ALTER TABLE valorapesquisa.business_groups ADD COLUMN IF NOT EXISTS code text;
ALTER TABLE valorapesquisa.business_groups ADD COLUMN IF NOT EXISTS type text NOT NULL DEFAULT 'economic_group';
ALTER TABLE valorapesquisa.business_groups ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS cnpj_root text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS registration_status text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS head_office_or_branch text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS legal_nature text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS company_size text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS share_capital numeric(18,2);
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS primary_cnae text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS opening_date date;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS data_source text;
ALTER TABLE valorapesquisa.legal_entities ADD COLUMN IF NOT EXISTS last_lookup_at timestamptz;
ALTER TABLE valorapesquisa.units ADD COLUMN IF NOT EXISTS type text;
ALTER TABLE valorapesquisa.units ADD COLUMN IF NOT EXISTS region text;
ALTER TABLE valorapesquisa.units ADD COLUMN IF NOT EXISTS state char(2);
ALTER TABLE valorapesquisa.units ADD COLUMN IF NOT EXISTS city text;
ALTER TABLE valorapesquisa.units ADD COLUMN IF NOT EXISTS manager_user_id uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.departments ADD COLUMN IF NOT EXISTS legal_entity_id uuid REFERENCES valorapesquisa.legal_entities(id);
ALTER TABLE valorapesquisa.departments ADD COLUMN IF NOT EXISTS unit_id uuid REFERENCES valorapesquisa.units(id);
ALTER TABLE valorapesquisa.departments ADD COLUMN IF NOT EXISTS parent_department_id uuid REFERENCES valorapesquisa.departments(id);
ALTER TABLE valorapesquisa.departments ADD COLUMN IF NOT EXISTS code text;
ALTER TABLE valorapesquisa.departments ADD COLUMN IF NOT EXISTS type text;
ALTER TABLE valorapesquisa.departments ADD COLUMN IF NOT EXISTS manager_user_id uuid REFERENCES valorapesquisa.users(id);

-- NULL não participa de UNIQUE composto no PostgreSQL. Consolide vínculos de
-- eventuais duplicatas globais antes de estabelecer a invariável correta.
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
INSERT INTO valorapesquisa.user_roles(user_id, role_id, created_at)
SELECT ur.user_id, role_map.canonical_id, ur.created_at
FROM user_roles ur JOIN role_map ON role_map.duplicate_id = ur.role_id
WHERE role_map.duplicate_id <> role_map.canonical_id
ON CONFLICT DO NOTHING;
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
INSERT INTO valorapesquisa.role_permissions(role_id, permission_id, created_at)
SELECT role_map.canonical_id, rp.permission_id, rp.created_at
FROM role_permissions rp JOIN role_map ON role_map.duplicate_id = rp.role_id
WHERE role_map.duplicate_id <> role_map.canonical_id
ON CONFLICT DO NOTHING;
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
DELETE FROM valorapesquisa.user_roles ur USING role_map
WHERE ur.role_id = role_map.duplicate_id AND role_map.duplicate_id <> role_map.canonical_id;
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
DELETE FROM valorapesquisa.role_permissions rp USING role_map
WHERE rp.role_id = role_map.duplicate_id AND role_map.duplicate_id <> role_map.canonical_id;
DELETE FROM valorapesquisa.roles duplicate
USING roles canonical
WHERE duplicate.organization_id IS NULL AND canonical.organization_id IS NULL
  AND duplicate.code = canonical.code AND duplicate.id > canonical.id;
CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_global_code
    ON valorapesquisa.roles(code) WHERE organization_id IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_tenant_code_active
    ON valorapesquisa.roles(organization_id, code)
    WHERE organization_id IS NOT NULL AND deleted_at IS NULL;

-- Converge the single invitation contract after all legacy columns are available.
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS email text;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS normalized_email text;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS token_hash text;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS invited_by_user_id uuid;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS expires_at timestamptz;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS accepted_at timestamptz;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS cancelled_at timestamptz;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS status text DEFAULT 'pending';
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS resend_count integer DEFAULT 0;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS last_sent_at timestamptz;
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.user_invitations ADD COLUMN IF NOT EXISTS updated_at timestamptz;
DO $$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='user_invitations' AND column_name='invited_by') THEN
    UPDATE valorapesquisa.user_invitations SET invited_by_user_id=invited_by WHERE invited_by_user_id IS NULL;
  END IF;
END $$;
UPDATE valorapesquisa.user_invitations
SET normalized_email=lower(trim(email)), name=COALESCE(NULLIF(name,''),split_part(email,'@',1)),
    status=CASE WHEN accepted_at IS NOT NULL THEN 'accepted' WHEN cancelled_at IS NOT NULL THEN 'cancelled' WHEN expires_at<now() THEN 'expired' ELSE COALESCE(status,'pending') END,
    resend_count=COALESCE(resend_count,0);
ALTER TABLE valorapesquisa.user_invitations ALTER COLUMN normalized_email SET NOT NULL, ALTER COLUMN name SET NOT NULL,
  ALTER COLUMN status SET NOT NULL, ALTER COLUMN resend_count SET NOT NULL, ALTER COLUMN invited_by_user_id SET NOT NULL;
ALTER TABLE valorapesquisa.user_invitations DROP COLUMN IF EXISTS invited_by;
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_invitations_pending
    ON valorapesquisa.user_invitations(organization_id, normalized_email) WHERE status='pending';
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_invitations_token ON valorapesquisa.user_invitations(token_hash) WHERE token_hash IS NOT NULL;

INSERT INTO valorapesquisa.roles(code,name,is_system) VALUES
('admin_valora','Administrador Valora',true),
('consultor_valora','Consultor Valora',true),
('admin_grupo','Administrador de grupo',true),
('empresa_admin','Administrador da empresa',true),
('gestor_pesquisa','Gestor de pesquisa',true),
('analista_resultados','Analista de resultados',true),
('gestor_unidade','Gestor de unidade',true),
('gestor_area','Gestor de área',true),
('participante','Participante',true),
('convidado_externo','Convidado externo',true)
ON CONFLICT DO NOTHING;

INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES ('20260731_006_multiempresa_rbac_plan_limits','phase-02g-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();

-- 19. MIGRACAO DE ESTRUTURAS LEGADAS E CONVERGENCIA FASE 2J

-- Converge historical installations without losing counter or reservation data.
DO $$
BEGIN
  IF to_regclass('valorapesquisa.plan_usage_counters') IS NULL THEN
    CREATE TABLE valorapesquisa.plan_usage_counters (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), metric_key text NOT NULL, period_start date NOT NULL, consumed bigint NOT NULL DEFAULT 0, reserved bigint NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
  ELSE
    ALTER TABLE valorapesquisa.plan_usage_counters ADD COLUMN IF NOT EXISTS id uuid DEFAULT gen_random_uuid();
    ALTER TABLE valorapesquisa.plan_usage_counters ADD COLUMN IF NOT EXISTS metric_key text;
    ALTER TABLE valorapesquisa.plan_usage_counters ADD COLUMN IF NOT EXISTS period_start date DEFAULT date_trunc('month', CURRENT_DATE)::date;
    ALTER TABLE valorapesquisa.plan_usage_counters ADD COLUMN IF NOT EXISTS consumed bigint DEFAULT 0;
    ALTER TABLE valorapesquisa.plan_usage_counters ADD COLUMN IF NOT EXISTS reserved bigint DEFAULT 0;
    ALTER TABLE valorapesquisa.plan_usage_counters ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_counters' AND column_name='resource_code') THEN
      UPDATE valorapesquisa.plan_usage_counters SET metric_key=resource_code WHERE metric_key IS NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_counters' AND column_name='used_value') THEN
      UPDATE valorapesquisa.plan_usage_counters SET consumed=used_value WHERE consumed=0;
    END IF;
    ALTER TABLE valorapesquisa.plan_usage_counters DROP COLUMN IF EXISTS resource_code;
    ALTER TABLE valorapesquisa.plan_usage_counters DROP COLUMN IF EXISTS used_value;
  END IF;
END $$;
ALTER TABLE valorapesquisa.plan_usage_counters ALTER COLUMN id SET NOT NULL, ALTER COLUMN metric_key SET NOT NULL, ALTER COLUMN period_start SET NOT NULL, ALTER COLUMN consumed SET NOT NULL, ALTER COLUMN reserved SET NOT NULL, ALTER COLUMN created_at SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_usage_counters_period ON valorapesquisa.plan_usage_counters(organization_id,metric_key,period_start);

DO $$
BEGIN
  IF to_regclass('valorapesquisa.plan_usage_reservations') IS NULL THEN
    CREATE TABLE valorapesquisa.plan_usage_reservations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), metric_key text NOT NULL, quantity bigint NOT NULL, status text NOT NULL DEFAULT 'reserved', idempotency_key text NOT NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
  ELSE
    ALTER TABLE valorapesquisa.plan_usage_reservations ADD COLUMN IF NOT EXISTS metric_key text;
    ALTER TABLE valorapesquisa.plan_usage_reservations ADD COLUMN IF NOT EXISTS quantity bigint;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_reservations' AND column_name='resource_code') THEN
      UPDATE valorapesquisa.plan_usage_reservations SET metric_key=resource_code WHERE metric_key IS NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_reservations' AND column_name='amount') THEN
      UPDATE valorapesquisa.plan_usage_reservations SET quantity=amount WHERE quantity IS NULL;
    END IF;
    ALTER TABLE valorapesquisa.plan_usage_reservations DROP COLUMN IF EXISTS resource_code;
    ALTER TABLE valorapesquisa.plan_usage_reservations DROP COLUMN IF EXISTS amount;
  END IF;
END $$;
ALTER TABLE valorapesquisa.plan_usage_reservations ALTER COLUMN metric_key SET NOT NULL, ALTER COLUMN quantity SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_usage_reservation_idempotency ON valorapesquisa.plan_usage_reservations(organization_id,idempotency_key);
CREATE INDEX IF NOT EXISTS ix_plan_usage_reservations_active ON valorapesquisa.plan_usage_reservations(organization_id,metric_key,expires_at) WHERE status='reserved';

-- Collapse the historical optional scope columns into the canonical polymorphic scope.
ALTER TABLE valorapesquisa.user_scopes ADD COLUMN IF NOT EXISTS scope_type text;
ALTER TABLE valorapesquisa.user_scopes ADD COLUMN IF NOT EXISTS scope_id uuid;
ALTER TABLE valorapesquisa.user_scopes ADD COLUMN IF NOT EXISTS created_by_user_id uuid;
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='user_scopes' AND column_name='business_group_id') THEN
    UPDATE valorapesquisa.user_scopes SET scope_type='business_group',scope_id=business_group_id WHERE scope_id IS NULL AND business_group_id IS NOT NULL;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='user_scopes' AND column_name='legal_entity_id') THEN
    UPDATE valorapesquisa.user_scopes SET scope_type='legal_entity',scope_id=legal_entity_id WHERE scope_id IS NULL AND legal_entity_id IS NOT NULL;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='user_scopes' AND column_name='unit_id') THEN
    UPDATE valorapesquisa.user_scopes SET scope_type='unit',scope_id=unit_id WHERE scope_id IS NULL AND unit_id IS NOT NULL;
  END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='user_scopes' AND column_name='department_id') THEN
    UPDATE valorapesquisa.user_scopes SET scope_type='department',scope_id=department_id WHERE scope_id IS NULL AND department_id IS NOT NULL;
  END IF;
END $$;
DELETE FROM valorapesquisa.user_scopes WHERE scope_type IS NULL OR scope_id IS NULL;
ALTER TABLE valorapesquisa.user_scopes ALTER COLUMN scope_type SET NOT NULL, ALTER COLUMN scope_id SET NOT NULL;
ALTER TABLE valorapesquisa.user_scopes DROP COLUMN IF EXISTS business_group_id, DROP COLUMN IF EXISTS legal_entity_id,
  DROP COLUMN IF EXISTS unit_id, DROP COLUMN IF EXISTS department_id;
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_scopes_active ON valorapesquisa.user_scopes(organization_id,user_id,scope_type,scope_id) WHERE deleted_at IS NULL;
DROP INDEX IF EXISTS ux_legal_entities_org_cnpj_active;
CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_cnpj_active ON valorapesquisa.legal_entities(cnpj) WHERE deleted_at IS NULL;

INSERT INTO valorapesquisa.schema_migrations(version, checksum) VALUES ('20260730_phase_02j', 'script-completo-v1') ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum, applied_at=now();

-- 35. VALIDACOES FINAIS
DO $validation$
BEGIN
  IF to_regnamespace('valorapesquisa') IS NULL THEN RAISE EXCEPTION 'Schema valorapesquisa ausente'; END IF;
  IF to_regclass('valorapesquisa.organizations') IS NULL OR to_regclass('valorapesquisa.users') IS NULL THEN RAISE EXCEPTION 'Tabelas obrigatorias ausentes'; END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_counters' AND column_name IN ('resource_code','used_value')) THEN RAISE EXCEPTION 'Colunas legadas ainda presentes em plan_usage_counters'; END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_reservations' AND column_name='amount') THEN RAISE EXCEPTION 'Coluna legada amount ainda presente'; END IF;
END
$validation$;


-- Phase 2V.2 is registered only after catalog validations succeed. A changed checksum requires an explicit new migration version.
DO $migration$
DECLARE expected_checksum constant text := 'sha256:phase-2v2-access-v1'; actual_checksum text;
BEGIN
 SELECT checksum INTO actual_checksum FROM valorapesquisa.schema_migrations WHERE version='2026_08_phase_2v2_permissions_convergence';
 IF actual_checksum IS NOT NULL AND actual_checksum<>expected_checksum THEN RAISE EXCEPTION 'schema_migrations: checksum divergente para 2026_08_phase_2v2_permissions_convergence (banco=%, esperado=%)',actual_checksum,expected_checksum; END IF;
 IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='permissions' AND column_name='module_code' AND data_type='text' AND is_nullable='YES') THEN RAISE EXCEPTION 'permissions.module_code: contrato incompatível; esperado text nullable'; END IF;
 IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname='valorapesquisa' AND tablename='permissions' AND indexname='ix_permissions_module_code' AND pg_get_indexdef(indexname::regclass) LIKE '%(module_code)%') THEN RAISE EXCEPTION 'permissions.module_code: índice ix_permissions_module_code ausente ou incompatível'; END IF;
 IF EXISTS (SELECT code FROM valorapesquisa.permissions GROUP BY code HAVING count(*)>1) THEN RAISE EXCEPTION 'permissions.code: códigos duplicados'; END IF;
 IF EXISTS (SELECT 1 FROM valorapesquisa.permissions p WHERE p.module_code IS NOT NULL AND NOT EXISTS(SELECT 1 FROM valorapesquisa.modules m WHERE m.code=p.module_code)) THEN RAISE EXCEPTION 'permissions.module_code: módulo conhecido ausente do catálogo'; END IF;
END $migration$;
INSERT INTO valorapesquisa.schema_migrations(version,checksum,applied_at,applied_by,application_version)
VALUES('2026_08_phase_2v2_permissions_convergence','sha256:phase-2v2-access-v1',now(),current_user,current_setting('valora.application_version',true))
ON CONFLICT(version) DO UPDATE SET applied_at=valorapesquisa.schema_migrations.applied_at;

-- 35.1 VALORA ENTERPRISE V6: carteira, CRM, integrações, automações e API segura
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS account_status text NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS account_health text NOT NULL DEFAULT 'healthy';
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS email text;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS last_activity_at timestamptz;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS trial_ends_at timestamptz;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS usage_percent integer NOT NULL DEFAULT 0 CHECK(usage_percent BETWEEN 0 AND 100);
CREATE INDEX IF NOT EXISTS ix_organizations_enterprise_portfolio ON valorapesquisa.organizations(account_status,account_health,created_at DESC) WHERE deleted_at IS NULL;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS billing_cycle text NOT NULL DEFAULT 'monthly';
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS contracted_value numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS renewal_at timestamptz;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS payment_method text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS financial_contact text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS notes text;

CREATE TABLE IF NOT EXISTS valorapesquisa.crm_leads(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), name text NOT NULL, company_name text, email text, phone text,
 commercial_status text NOT NULL DEFAULT 'new' CHECK(commercial_status IN('new','contact','meeting','proposal','negotiation','won','lost','active_customer')),
 intended_plan text, owner_name text, next_action_at timestamptz, notes text, converted_organization_id uuid REFERENCES valorapesquisa.organizations(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_crm_leads_pipeline ON valorapesquisa.crm_leads(commercial_status,next_action_at) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.enterprise_items(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id),
 kind text NOT NULL CHECK(kind IN('plan','subscription','integration','template','alert','automation','branding')),
 name text NOT NULL, status text NOT NULL DEFAULT 'active', configuration jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_enterprise_items_scope ON valorapesquisa.enterprise_items(kind,organization_id,status) WHERE deleted_at IS NULL;

-- Convergência de API keys. Este bloco deliberadamente antecede qualquer
-- índice, FK ou uso de key_hash: CREATE TABLE IF NOT EXISTS sozinho não migra
-- uma tabela criada por versões antigas.
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE IF NOT EXISTS valorapesquisa.api_keys(
  id uuid PRIMARY KEY DEFAULT gen_random_uuid()
);

ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS id uuid DEFAULT gen_random_uuid();
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS organization_id uuid;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS key_prefix text;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS key_hash text;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS scopes text[] DEFAULT '{}';
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS status text DEFAULT 'active';
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS last_used_at timestamptz;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS use_count bigint DEFAULT 0;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS updated_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS revoked_at timestamptz;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

UPDATE valorapesquisa.api_keys SET id=gen_random_uuid() WHERE id IS NULL;
-- Colunas legadas só podem aparecer em SQL dinâmico: uma referência estática
-- falha no parse mesmo quando protegida por uma condição.
DO $api_key_legacy$
DECLARE legacy_column text;
BEGIN
  FOREACH legacy_column IN ARRAY ARRAY['secret_hash','hash','api_key_hash'] LOOP
    IF EXISTS (
      SELECT 1 FROM information_schema.columns
      WHERE table_schema='valorapesquisa' AND table_name='api_keys'
        AND column_name=legacy_column
    ) THEN
      EXECUTE format(
        'UPDATE valorapesquisa.api_keys SET key_hash=%I::text WHERE key_hash IS NULL AND %I IS NOT NULL',
        legacy_column, legacy_column
      );
    END IF;
  END LOOP;
END $api_key_legacy$;

UPDATE valorapesquisa.api_keys
SET key_hash=COALESCE(NULLIF(key_hash,''),encode(digest('valora-api-key:'||id::text,'sha256'),'hex')),
    key_prefix=COALESCE(NULLIF(key_prefix,''),'legacy_'||left(replace(id::text,'-',''),12)),
    name=COALESCE(NULLIF(name,''),'Chave migrada '||left(id::text,8)),
    status=COALESCE(NULLIF(status,''),'active'),
    scopes=COALESCE(scopes,'{}'::text[]),
    use_count=COALESCE(use_count,0),
    created_at=COALESCE(created_at,now()),
    updated_at=COALESCE(updated_at,created_at,now()),
    organization_id=COALESCE(
      (SELECT o.id FROM valorapesquisa.organizations o WHERE o.id=api_keys.organization_id),
      (SELECT o.id FROM valorapesquisa.organizations o WHERE o.slug='valora-platform')
    );

-- Mantém todas as chaves antigas, mas torna hashes repetidos inequivocamente
-- técnicos antes de criar a garantia usada pelo repositório.
WITH duplicate_hashes AS (
  SELECT id,key_hash,row_number() OVER (PARTITION BY key_hash ORDER BY created_at,id) occurrence
  FROM valorapesquisa.api_keys
)
UPDATE valorapesquisa.api_keys k
SET key_hash=encode(digest('valora-api-key:duplicate:'||d.key_hash||':'||d.id::text,'sha256'),'hex'),
    updated_at=now()
FROM duplicate_hashes d WHERE d.id=k.id AND d.occurrence>1;

ALTER TABLE valorapesquisa.api_keys ALTER COLUMN id SET DEFAULT gen_random_uuid();
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN id SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN organization_id SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN name SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN key_prefix SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN key_hash SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN scopes SET DEFAULT '{}';
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN scopes SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN status SET DEFAULT 'active';
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN status SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN use_count SET DEFAULT 0;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN use_count SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN created_at SET DEFAULT now();
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN created_at SET NOT NULL;
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN updated_at SET DEFAULT now();
ALTER TABLE valorapesquisa.api_keys ALTER COLUMN updated_at SET NOT NULL;
DO $api_key_constraints$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
    WHERE conrelid='valorapesquisa.api_keys'::regclass AND contype='p'
  ) THEN
    ALTER TABLE valorapesquisa.api_keys ADD CONSTRAINT pk_api_keys PRIMARY KEY(id);
  END IF;
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
    WHERE conrelid='valorapesquisa.api_keys'::regclass
      AND contype='f' AND conname='fk_api_keys_organizations'
  ) THEN
    ALTER TABLE valorapesquisa.api_keys ADD CONSTRAINT fk_api_keys_organizations
      FOREIGN KEY(organization_id) REFERENCES valorapesquisa.organizations(id);
  END IF;
END $api_key_constraints$;
DO $api_key_index$
BEGIN
  -- IF NOT EXISTS não corrige um índice homônimo antigo que não seja UNIQUE.
  IF EXISTS (
    SELECT 1 FROM pg_class i JOIN pg_namespace n ON n.oid=i.relnamespace
    JOIN pg_index x ON x.indexrelid=i.oid
    WHERE n.nspname='valorapesquisa' AND i.relname='ux_api_keys_hash' AND NOT x.indisunique
  ) THEN
    DROP INDEX valorapesquisa.ux_api_keys_hash;
  END IF;
END $api_key_index$;
CREATE UNIQUE INDEX IF NOT EXISTS ux_api_keys_hash ON valorapesquisa.api_keys(key_hash);
CREATE INDEX IF NOT EXISTS ix_api_keys_tenant_active ON valorapesquisa.api_keys(organization_id,status) WHERE revoked_at IS NULL AND deleted_at IS NULL;
INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('2026_08_enterprise_v6','sha256:enterprise-v6-portfolio-crm-automation-api-v1') ON CONFLICT(version) DO NOTHING;

-- 36. COMMIT
COMMIT;



-- 38. VALORA ACTION™ E EVOLUTION™ (aditivo e idempotente)
BEGIN;
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('organizational_intelligence.action.create','Criar ação baseada em evidências','Cria ações rastreáveis no Valora Action™.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.deleted_at IS NULL AND r.code IN('admin_valora','consultor_valora','empresa_admin','gestor_pesquisa','analista_resultados')
 AND p.code='organizational_intelligence.action.create' ON CONFLICT(role_id,permission_id) DO NOTHING;

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_actions(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), code text NOT NULL,
 title text NOT NULL, description text NOT NULL, evidence_justification text NOT NULL, capability text NOT NULL,
 priority text NOT NULL CHECK(priority IN('critical','high','medium','low')), owner_name text, executive_sponsor text, due_at timestamptz,
 complexity text NOT NULL CHECK(complexity IN('low','medium','high')), indicators text NOT NULL, expected_result text NOT NULL,
 completion_criteria text NOT NULL, status text NOT NULL DEFAULT 'recommended' CHECK(status IN('recommended','planned','in_progress','waiting','completed','cancelled','reviewed')),
 created_by uuid REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_valora_actions_code ON valorapesquisa.valora_actions(organization_id,code) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_valora_actions_priority ON valorapesquisa.valora_actions(organization_id,status,priority,due_at) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_action_history(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), action_id uuid NOT NULL REFERENCES valorapesquisa.valora_actions(id) ON DELETE CASCADE,
 status text NOT NULL, notes text NOT NULL, changed_by uuid REFERENCES valorapesquisa.users(id), changed_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valora_action_history_action ON valorapesquisa.valora_action_history(action_id,changed_at DESC);
DROP TRIGGER IF EXISTS trg_valora_actions_updated_at ON valorapesquisa.valora_actions;
CREATE TRIGGER trg_valora_actions_updated_at BEFORE UPDATE ON valorapesquisa.valora_actions FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at();
INSERT INTO valorapesquisa.plan_capabilities(plan_id,capability,capability_code,capability_key,enabled,is_enabled)
SELECT id,x.code,x.code,x.code,true,true FROM valorapesquisa.plans CROSS JOIN (VALUES('valora_action'),('valora_evolution'),('valora_heatmap'),('valora_journey')) x(code)
WHERE lower(plans.code) IN('professional','corporate','enterprise')
ON CONFLICT(plan_id,capability_key) DO UPDATE SET enabled=true,is_enabled=true,updated_at=now();
INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('2026_08_valora_action_evolution','sha256:valora-action-evolution-v1') ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum;
COMMIT;

-- 36.1 INTELIGÊNCIA ORGANIZACIONAL: leituras agregadas, insights e jornada (sem dados pessoais)
BEGIN;
INSERT INTO valorapesquisa.modules(code,name,category,status) VALUES('organizational_intelligence','Inteligência Organizacional','intelligence','active')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,category=EXCLUDED.category,status='active',deleted_at=NULL,updated_at=now();
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('organizational_intelligence.read','Consultar inteligência organizacional','Consulta leituras agregadas e jornada.','organizational_intelligence'),
 ('organizational_intelligence.generate','Gerar inteligência organizacional','Gera uma leitura determinística a partir de evidências.','organizational_intelligence'),
 ('organizational_intelligence.journey.create','Criar marco da jornada','Registra marcos organizacionais sem dados pessoais.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.deleted_at IS NULL AND r.code IN('admin_valora','consultor_valora','empresa_admin','gestor_pesquisa','analista_resultados')
  AND p.code IN('organizational_intelligence.read','organizational_intelligence.generate','organizational_intelligence.journey.create')
ON CONFLICT(role_id,permission_id) DO NOTHING;

CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_intelligence_runs(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 maturity_index numeric(6,2) NOT NULL CHECK(maturity_index BETWEEN 0 AND 100), culture_trust_index numeric(6,2) NOT NULL CHECK(culture_trust_index BETWEEN 0 AND 100),
 governance_execution_index numeric(6,2) NOT NULL CHECK(governance_execution_index BETWEEN 0 AND 100), structural_gap numeric(6,2) NOT NULL CHECK(structural_gap BETWEEN 0 AND 100),
 strongest_dimension text NOT NULL, weakest_dimension text NOT NULL, evidence_count integer NOT NULL CHECK(evidence_count>=0),
 confidence_level text NOT NULL CHECK(confidence_level IN('very_high','high','moderate','low')), warning text, heatmap jsonb NOT NULL DEFAULT '[]',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_oi_runs_tenant_date ON valorapesquisa.organizational_intelligence_runs(organization_id,created_at DESC);
ALTER TABLE valorapesquisa.organizational_intelligence_runs DROP CONSTRAINT IF EXISTS organizational_intelligence_runs_confidence_level_check;
ALTER TABLE valorapesquisa.organizational_intelligence_runs ADD CONSTRAINT organizational_intelligence_runs_confidence_level_check CHECK(confidence_level IN('high','medium','low','insufficient_evidence','very_high','moderate'));
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_intelligence_insights(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.organizational_intelligence_runs(id) ON DELETE CASCADE,
 dimension text NOT NULL, observation text NOT NULL, evidence text NOT NULL, correlation text NOT NULL, probable_cause text NOT NULL,
 impact text NOT NULL, priority text NOT NULL CHECK(priority IN('high','medium','low')), evolution_plan text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_oi_insights_run ON valorapesquisa.organizational_intelligence_insights(run_id,created_at);
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_journey_events(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), title text NOT NULL,
 description text NOT NULL, event_type text NOT NULL DEFAULT 'milestone', occurred_at timestamptz NOT NULL, created_by uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_oi_journey_tenant_date ON valorapesquisa.organizational_journey_events(organization_id,occurred_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_indicator_definitions(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL, description text NOT NULL, category text NOT NULL,
 weight numeric(6,3) NOT NULL DEFAULT 1 CHECK(weight>0), is_active boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
WITH d AS (SELECT id,code,row_number() OVER(PARTITION BY code ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.valora_indicator_definitions)
UPDATE valorapesquisa.valora_indicator_definitions x SET code=x.code||'-legacy-'||left(replace(x.id::text,'-',''),8),updated_at=now() FROM d WHERE d.id=x.id AND d.n>1;
CREATE UNIQUE INDEX IF NOT EXISTS ux_valora_indicator_definitions_code_v2 ON valorapesquisa.valora_indicator_definitions(code);
INSERT INTO valorapesquisa.valora_indicator_definitions(code,name,description,category,weight) VALUES
 ('organizational_maturity','Maturidade organizacional','Média ponderada das dimensões efetivamente avaliadas.','maturity',1),
 ('culture_trust','Cultura e confiança','Leitura das dimensões identificadas como cultura, confiança, pessoas ou liderança.','culture',1),
 ('governance_execution','Governança e execução','Leitura das dimensões identificadas como governança, execução, processos ou estratégia.','governance',1),
 ('structural_gap','Gap estrutural','Diferença entre as dimensões mais forte e mais frágil.','structure',1)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,category=EXCLUDED.category,weight=EXCLUDED.weight,is_active=true,updated_at=now();
DO $triggers$ DECLARE table_name text; BEGIN FOREACH table_name IN ARRAY ARRAY['organizational_intelligence_runs','organizational_journey_events','valora_indicator_definitions'] LOOP EXECUTE format('DROP TRIGGER IF EXISTS trg_%s_updated_at ON valorapesquisa.%I',table_name,table_name); EXECUTE format('CREATE TRIGGER trg_%s_updated_at BEFORE UPDATE ON valorapesquisa.%I FOR EACH ROW EXECUTE FUNCTION valorapesquisa.set_updated_at()',table_name,table_name); END LOOP; END $triggers$;
INSERT INTO valorapesquisa.plan_capabilities(plan_id,capability,capability_code,capability_key,enabled,is_enabled)
SELECT id,'organizational_intelligence','organizational_intelligence','organizational_intelligence',true,true FROM valorapesquisa.plans WHERE lower(code) IN('professional','corporate','enterprise')
ON CONFLICT(plan_id,capability_key) DO UPDATE SET capability=EXCLUDED.capability,capability_code=EXCLUDED.capability_code,enabled=true,is_enabled=true,updated_at=now();
INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('2026_08_organizational_intelligence','sha256:organizational-intelligence-v1') ON CONFLICT(version) DO NOTHING;
COMMIT;

-- 37. VALORA V10: monetização e base operacional de produção (aditiva e idempotente)
BEGIN;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS discount_value numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS due_at timestamptz;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS financial_email text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS financial_phone text;
-- Supports the active-subscription upsert in SubscriptionRepository.
CREATE UNIQUE INDEX IF NOT EXISTS ux_subscriptions_active_organization
    ON valorapesquisa.subscriptions(organization_id)
    WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_subscriptions_status_due ON valorapesquisa.subscriptions(status,due_at) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.manual_payments(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), subscription_id uuid NOT NULL REFERENCES valorapesquisa.subscriptions(id),
 organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), amount numeric(14,2) NOT NULL CHECK(amount>0),
 paid_at timestamptz NOT NULL, method text NOT NULL, reference text, notes text, registered_by uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_manual_payments_tenant_date ON valorapesquisa.manual_payments(organization_id,paid_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.campaigns(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id),
 unit_id uuid REFERENCES valorapesquisa.units(id), department_id uuid REFERENCES valorapesquisa.departments(id), name text NOT NULL,
 audience text, response_goal integer NOT NULL DEFAULT 0 CHECK(response_goal>=0), starts_at timestamptz, ends_at timestamptz,
 public_token_hash text, status text NOT NULL DEFAULT 'draft' CHECK(status IN('draft','scheduled','active','paused','closed','cancelled')),
 created_by uuid REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_campaigns_tenant_status ON valorapesquisa.campaigns(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_campaigns_survey ON valorapesquisa.campaigns(survey_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.processing_jobs(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id),
 type text NOT NULL, status text NOT NULL DEFAULT 'pending' CHECK(status IN('pending','processing','completed','failed','cancelled')),
 payload jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text NOT NULL, attempts integer NOT NULL DEFAULT 0,
 error_message text, created_at timestamptz NOT NULL DEFAULT now(), started_at timestamptz, completed_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_processing_jobs_queue ON valorapesquisa.processing_jobs(status,created_at);
CREATE INDEX IF NOT EXISTS ix_processing_jobs_tenant ON valorapesquisa.processing_jobs(organization_id,type,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.operational_errors(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), correlation_id text NOT NULL, module text NOT NULL,
 organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id), severity text NOT NULL,
 technical_message text NOT NULL, friendly_message text NOT NULL, status text NOT NULL DEFAULT 'open', owner text, resolution text,
 created_at timestamptz NOT NULL DEFAULT now(), resolved_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_operational_errors_correlation ON valorapesquisa.operational_errors(correlation_id);
CREATE INDEX IF NOT EXISTS ix_operational_errors_triage ON valorapesquisa.operational_errors(status,severity,created_at DESC);

INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES('2026_08_valora_v10_operations','sha256:v10-monetization-campaign-jobs-errors-v1') ON CONFLICT(version) DO NOTHING;
COMMIT;

-- Assets oficiais: /img/brand/valora-logo-full.svg (com fallback textual acessível).
-- Conta técnica de homologação local. A credencial abaixo é BCrypt cost 12 e
-- nunca representa senha em texto puro no banco.
BEGIN;
UPDATE valorapesquisa.users u SET organization_id=o.id,name='Super Administrador Valora',
 password_hash='$2y$12$9P5OMINImu0isB5uMCSqmOC0JIZjfuv/IDSEjC0WyepMU2gIQr9nm',status='active',
 password_reset_required=false,deleted_at=NULL,updated_at=now()
FROM valorapesquisa.organizations o
WHERE lower(u.email)='superadmin@valoragroup.local' AND o.slug='valora-platform';
INSERT INTO valorapesquisa.users(organization_id,email,name,password_hash,status,password_reset_required,created_at,updated_at,deleted_at)
SELECT o.id,'superadmin@valoragroup.local','Super Administrador Valora',
 '$2y$12$9P5OMINImu0isB5uMCSqmOC0JIZjfuv/IDSEjC0WyepMU2gIQr9nm','active',false,now(),now(),NULL
FROM valorapesquisa.organizations o WHERE o.slug='valora-platform'
 AND NOT EXISTS(SELECT 1 FROM valorapesquisa.users WHERE lower(email)='superadmin@valoragroup.local');
INSERT INTO valorapesquisa.user_roles(user_id,role_id,created_at)
SELECT u.id,r.id,now() FROM valorapesquisa.users u CROSS JOIN LATERAL (
 SELECT id FROM valorapesquisa.roles WHERE code='admin_valora' AND deleted_at IS NULL
 ORDER BY organization_id NULLS FIRST LIMIT 1) r
WHERE lower(u.email)='superadmin@valoragroup.local' ON CONFLICT(user_id,role_id) DO NOTHING;
-- admin_valora recebe o catálogo completo, inclusive permissões acrescentadas
-- por fases futuras do próprio bootstrap.
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at)
SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.code='admin_valora' AND r.deleted_at IS NULL ON CONFLICT(role_id,permission_id) DO NOTHING;
COMMIT;

-- Validações finais executáveis: qualquer regressão essencial interrompe o
-- bootstrap com uma mensagem objetiva em vez de deixar um banco semiconfigurado.
DO $validation$
BEGIN
  IF EXISTS (SELECT 1 FROM valorapesquisa.forms WHERE title IS NULL) THEN RAISE EXCEPTION 'Validação falhou: forms.title contém NULL'; END IF;
  IF EXISTS (SELECT 1 FROM valorapesquisa.forms WHERE name IS NULL) THEN RAISE EXCEPTION 'Validação falhou: forms.name contém NULL'; END IF;
  IF NOT EXISTS (SELECT 1 FROM valorapesquisa.forms WHERE code='valora-official' AND title='Pesquisa Oficial Valora') THEN RAISE EXCEPTION 'Validação falhou: forms.code=valora-official ausente ou título incorreto'; END IF;
  IF EXISTS (SELECT code FROM valorapesquisa.modules GROUP BY code HAVING count(*)>1) THEN RAISE EXCEPTION 'Validação falhou: modules.code duplicado'; END IF;
  IF EXISTS (SELECT code FROM valorapesquisa.permissions GROUP BY code HAVING count(*)>1) THEN RAISE EXCEPTION 'Validação falhou: permissions.code duplicado'; END IF;
  IF EXISTS (SELECT code FROM valorapesquisa.plans GROUP BY code HAVING count(*)>1) THEN RAISE EXCEPTION 'Validação falhou: plans.code duplicado'; END IF;
  IF EXISTS (SELECT plan_id,capability_key FROM valorapesquisa.plan_capabilities GROUP BY plan_id,capability_key HAVING count(*)>1) THEN RAISE EXCEPTION 'Validação falhou: plan_capabilities(plan_id,capability_key) duplicado'; END IF;
  IF EXISTS (SELECT form_id,version,language FROM valorapesquisa.form_versions GROUP BY form_id,version,language HAVING count(*)>1) THEN RAISE EXCEPTION 'Validação falhou: form_versions(form_id,version,language) duplicado'; END IF;
  IF (SELECT count(*) FROM valorapesquisa.dimensions d JOIN valorapesquisa.form_versions fv ON fv.id=d.form_version_id JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE f.code='valora-official' AND d.code IN('culture','governance','leadership','people','growth')) <> 5 THEN RAISE EXCEPTION 'Validação falhou: dimensões oficiais incompletas'; END IF;
  IF (SELECT count(*) FROM valorapesquisa.questions q JOIN valorapesquisa.dimensions d ON d.id=q.dimension_id JOIN valorapesquisa.form_versions fv ON fv.id=d.form_version_id JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE f.code='valora-official' AND q.deleted_at IS NULL AND q.is_qualitative=false) <> 25 THEN RAISE EXCEPTION 'Validação falhou: o formulário oficial deve conter 25 perguntas quantitativas'; END IF;
  IF (SELECT count(*) FROM valorapesquisa.questions q JOIN valorapesquisa.dimensions d ON d.id=q.dimension_id JOIN valorapesquisa.form_versions fv ON fv.id=d.form_version_id JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE f.code='valora-official' AND q.deleted_at IS NULL AND q.is_qualitative=true) <> 1 THEN RAISE EXCEPTION 'Validação falhou: o formulário oficial deve conter 1 pergunta qualitativa'; END IF;
  IF NOT EXISTS (SELECT 1 FROM valorapesquisa.form_versions fv JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE f.code='valora-official' AND fv.maximum_score=125 AND fv.max_score=125) THEN RAISE EXCEPTION 'Validação falhou: pontuação máxima oficial deve ser 125'; END IF;
  IF EXISTS (SELECT 1 FROM valorapesquisa.questions q JOIN valorapesquisa.dimensions d ON d.id=q.dimension_id JOIN valorapesquisa.form_versions fv ON fv.id=d.form_version_id JOIN valorapesquisa.forms f ON f.id=fv.form_id WHERE f.code='valora-official' AND (q.title IS NULL OR q.text IS NULL OR q.type IS NULL OR q.organization_id IS NULL OR q.form_id IS NULL OR q.form_version_id IS NULL OR q.position IS NULL OR q.display_order IS NULL OR q.weight IS NULL OR q.version IS NULL OR q.is_active IS NULL OR q.required IS NULL OR q.is_required IS NULL)) THEN RAISE EXCEPTION 'Validação falhou: contrato de questions contém NULL obrigatório'; END IF;
  IF NOT EXISTS (SELECT 1 FROM valorapesquisa.users u JOIN valorapesquisa.user_roles ur ON ur.user_id=u.id JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE lower(u.email)='superadmin@valoragroup.local' AND u.status='active' AND r.code='admin_valora') THEN RAISE EXCEPTION 'Validação falhou: super administrador não configurado'; END IF;
  IF NOT EXISTS (SELECT 1 FROM valorapesquisa.plan_capabilities pc JOIN valorapesquisa.plans p ON p.id=pc.plan_id WHERE p.code IN('free','professional','corporate','enterprise')) THEN RAISE EXCEPTION 'Validação falhou: capabilities dos planos ausentes'; END IF;
  IF to_regclass('valorapesquisa.result_scores') IS NULL OR to_regclass('valorapesquisa.certificates') IS NULL OR to_regclass('valorapesquisa.organizational_intelligence_runs') IS NULL THEN RAISE EXCEPTION 'Validação falhou: tabelas de resultado, certificado ou inteligência ausentes'; END IF;
  RAISE NOTICE 'Validação Valora concluída: formulário oficial, 5 dimensões, 25 perguntas quantitativas, 1 qualitativa e capabilities OK';
END $validation$;
