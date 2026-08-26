-- Revisao operacional 2026-08-19: este arquivo permanece a fonte canonica; alteracoes de configuracao desta entrega nao exigem mutacao de esquema.
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

-- Contrato mínimo de API keys também pertence à compatibilidade inicial. Ele
-- precisa existir antes de qualquer fase posterior tentar semear ou indexar a
-- tabela em instalações antigas/parciais.
CREATE TABLE IF NOT EXISTS valorapesquisa.api_keys(id uuid PRIMARY KEY DEFAULT gen_random_uuid());
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
DO $initial_api_key_legacy$
DECLARE legacy_column text;
BEGIN
  FOREACH legacy_column IN ARRAY ARRAY['secret_hash','hash','api_key_hash'] LOOP
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='api_keys' AND column_name=legacy_column) THEN
      EXECUTE format('UPDATE valorapesquisa.api_keys SET key_hash=%I::text WHERE key_hash IS NULL AND %I IS NOT NULL',legacy_column,legacy_column);
    END IF;
  END LOOP;
END $initial_api_key_legacy$;
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
-- Keep the communication ledger compatible with both fresh databases and installations
-- created by older versions of the complete script.  Application writes intentionally
-- store only a masked recipient; the legacy hash therefore cannot remain mandatory.
ALTER TABLE valorapesquisa.communications
  ADD COLUMN IF NOT EXISTS recipient_hash text;
ALTER TABLE valorapesquisa.communications
  ALTER COLUMN recipient_hash DROP NOT NULL,
  ADD COLUMN IF NOT EXISTS survey_id uuid REFERENCES valorapesquisa.surveys(id),
  ADD COLUMN IF NOT EXISTS response_id uuid REFERENCES valorapesquisa.responses(id),
  ADD COLUMN IF NOT EXISTS event_type text,
  ADD COLUMN IF NOT EXISTS recipient_masked text,
  ADD COLUMN IF NOT EXISTS provider_message_id text,
  ADD COLUMN IF NOT EXISTS error_code text,
  ADD COLUMN IF NOT EXISTS error_message text,
  ADD COLUMN IF NOT EXISTS attempts integer NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS correlation_id text,
  ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS sent_at timestamptz;
CREATE TABLE IF NOT EXISTS valorapesquisa.notifications (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id), title text NOT NULL, message text NOT NULL, read_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
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
ALTER TABLE valorapesquisa.lgpd_consents
  ADD COLUMN IF NOT EXISTS survey_id uuid,
  ADD COLUMN IF NOT EXISTS response_id uuid,
  ADD COLUMN IF NOT EXISTS participant_email_hash text,
  ADD COLUMN IF NOT EXISTS consent_text text,
  ADD COLUMN IF NOT EXISTS consent_version varchar(32),
  ADD COLUMN IF NOT EXISTS accepted boolean NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS accepted_at timestamptz,
  ADD COLUMN IF NOT EXISTS ip_hash text,
  ADD COLUMN IF NOT EXISTS user_agent text,
  ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.lgpd_consents ALTER COLUMN consent_type SET DEFAULT 'diagnostic_response';
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
ALTER TABLE valorapesquisa.support_tickets ADD COLUMN IF NOT EXISTS user_id uuid, ADD COLUMN IF NOT EXISTS description text, ADD COLUMN IF NOT EXISTS type text NOT NULL DEFAULT 'duvida', ADD COLUMN IF NOT EXISTS priority text NOT NULL DEFAULT 'normal', ADD COLUMN IF NOT EXISTS module text, ADD COLUMN IF NOT EXISTS route text, ADD COLUMN IF NOT EXISTS assigned_user_id uuid, ADD COLUMN IF NOT EXISTS resolution_summary text, ADD COLUMN IF NOT EXISTS reopen_reason text, ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, ADD COLUMN IF NOT EXISTS correlation_id text, ADD COLUMN IF NOT EXISTS closed_at timestamptz, ADD COLUMN IF NOT EXISTS resolved_at timestamptz, ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE valorapesquisa.support_tickets ADD COLUMN IF NOT EXISTS entity_type text, ADD COLUMN IF NOT EXISTS entity_id uuid, ADD COLUMN IF NOT EXISTS reopened_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_support_tickets_org_status ON valorapesquisa.support_tickets(organization_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_support_tickets_correlation ON valorapesquisa.support_tickets(correlation_id) WHERE correlation_id IS NOT NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.support_ticket_comments (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), ticket_id uuid NOT NULL REFERENCES valorapesquisa.support_tickets(id), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid, comment text NOT NULL, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.support_ticket_history (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), ticket_id uuid NOT NULL REFERENCES valorapesquisa.support_tickets(id), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid, action text NOT NULL, from_status text, to_status text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.customer_feedback (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid, type text NOT NULL, rating smallint CHECK(rating BETWEEN 1 AND 5), module text, message text NOT NULL, blocked_usage boolean NOT NULL DEFAULT false, status text NOT NULL DEFAULT 'received', converted_ticket_id uuid REFERENCES valorapesquisa.support_tickets(id), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
ALTER TABLE valorapesquisa.customer_feedback ADD COLUMN IF NOT EXISTS impact_level text, ADD COLUMN IF NOT EXISTS improvement_id uuid, ADD COLUMN IF NOT EXISTS decision_reason text;
CREATE INDEX IF NOT EXISTS ix_customer_feedback_org_status ON valorapesquisa.customer_feedback(organization_id,status,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.customer_success_scores (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), score numeric(5,2), status text NOT NULL, explanation_json jsonb NOT NULL DEFAULT '[]'::jsonb, calculated_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
ALTER TABLE valorapesquisa.customer_success_scores ADD COLUMN IF NOT EXISTS explanation text, ADD COLUMN IF NOT EXISTS adoption_summary_json jsonb NOT NULL DEFAULT '{}'::jsonb, ADD COLUMN IF NOT EXISTS risks_json jsonb NOT NULL DEFAULT '[]'::jsonb, ADD COLUMN IF NOT EXISTS recommendations_json jsonb NOT NULL DEFAULT '[]'::jsonb, ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE TABLE IF NOT EXISTS valorapesquisa.customer_success_events (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid, event_type text NOT NULL, description text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.usage_analytics_snapshots (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), period_start date NOT NULL, period_end date NOT NULL, active_users integer NOT NULL DEFAULT 0, logins integer NOT NULL DEFAULT 0, diagnostics_created integer NOT NULL DEFAULT 0, diagnostics_published integer NOT NULL DEFAULT 0, public_links integer NOT NULL DEFAULT 0, responses integer NOT NULL DEFAULT 0, reports_generated integer NOT NULL DEFAULT 0, certificates_generated integer NOT NULL DEFAULT 0, actions_created integer NOT NULL DEFAULT 0, actions_completed integer NOT NULL DEFAULT 0, blocked_features integer NOT NULL DEFAULT 0, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_usage_snapshots_org_period ON valorapesquisa.usage_analytics_snapshots(organization_id,period_start DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.onboarding_checklists (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), status text NOT NULL DEFAULT 'not_started', notes text, blocked_reason text, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.onboarding_steps (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), checklist_id uuid NOT NULL REFERENCES valorapesquisa.onboarding_checklists(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), step_code text NOT NULL, status text NOT NULL DEFAULT 'not_started', completion_source text, completed_by_user_id uuid, completed_at timestamptz, blocked_reason text, notes text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, UNIQUE(checklist_id,step_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.upgrade_requests (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid, type text NOT NULL, current_plan text, requested_resource text, status text NOT NULL DEFAULT 'requested', assigned_user_id uuid, notes text, usage_event_id uuid, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, closed_at timestamptz, deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_upgrade_requests_org_status ON valorapesquisa.upgrade_requests(organization_id,status,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.operational_incidents (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), title text NOT NULL, description text, severity text NOT NULL DEFAULT 'medium', status text NOT NULL DEFAULT 'open', assigned_user_id uuid, root_cause text, corrective_action text, lessons_learned text, resolution_summary text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, resolved_at timestamptz, closed_at timestamptz, deleted_at timestamptz);
ALTER TABLE valorapesquisa.operational_incidents ADD COLUMN IF NOT EXISTS module text, ADD COLUMN IF NOT EXISTS mitigation text, ADD COLUMN IF NOT EXISTS created_by uuid;
CREATE TABLE IF NOT EXISTS valorapesquisa.incident_updates (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), incident_id uuid NOT NULL REFERENCES valorapesquisa.operational_incidents(id), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid, message text NOT NULL, status text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.release_notes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), version text NOT NULL, title text NOT NULL, content text NOT NULL, type text NOT NULL, visibility text NOT NULL DEFAULT 'client', status text NOT NULL DEFAULT 'draft', release_date date, published_at timestamptz, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_release_notes_version ON valorapesquisa.release_notes(version);
CREATE TABLE IF NOT EXISTS valorapesquisa.release_note_items (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), release_note_id uuid NOT NULL REFERENCES valorapesquisa.release_notes(id), item_type text NOT NULL, title text NOT NULL, content text, related_entity_type text, related_entity_id uuid, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.data_quality_runs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), status text NOT NULL DEFAULT 'running', started_at timestamptz NOT NULL DEFAULT now(), finished_at timestamptz, created_by_user_id uuid, summary_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.data_quality_issues (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.data_quality_runs(id), organization_id uuid REFERENCES valorapesquisa.organizations(id), check_code text NOT NULL, entity_type text NOT NULL, entity_id uuid, severity text NOT NULL, description text NOT NULL, status text NOT NULL DEFAULT 'open', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, resolved_at timestamptz, deleted_at timestamptz);
ALTER TABLE valorapesquisa.data_quality_issues ADD COLUMN IF NOT EXISTS recommended_action text, ADD COLUMN IF NOT EXISTS resolution_reason text;
CREATE TABLE IF NOT EXISTS valorapesquisa.product_improvement_backlog (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), source_type text NOT NULL, source_id uuid, title text NOT NULL, description text, priority text NOT NULL DEFAULT 'normal', status text NOT NULL DEFAULT 'received', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz, deleted_at timestamptz);
ALTER TABLE valorapesquisa.product_improvement_backlog ADD COLUMN IF NOT EXISTS impact text, ADD COLUMN IF NOT EXISTS estimated_effort text, ADD COLUMN IF NOT EXISTS assigned_user_id uuid, ADD COLUMN IF NOT EXISTS release_note_id uuid, ADD COLUMN IF NOT EXISTS decision_reason text;
CREATE INDEX IF NOT EXISTS ix_product_backlog_org_status ON valorapesquisa.product_improvement_backlog(organization_id,status,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.platform_governance_events (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid, action text NOT NULL, entity_type text NOT NULL, entity_id uuid, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
-- Contrato canônico e evolução não destrutiva para instalações anteriores.  A
-- criação com IF NOT EXISTS acima não acrescenta colunas a tabelas legadas.
ALTER TABLE valorapesquisa.platform_governance_events
 ADD COLUMN IF NOT EXISTS organization_id uuid,
 ADD COLUMN IF NOT EXISTS user_id uuid,
 ADD COLUMN IF NOT EXISTS code varchar(100),
 ADD COLUMN IF NOT EXISTS status varchar(40) NOT NULL DEFAULT 'recorded',
 ADD COLUMN IF NOT EXISTS data jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS methodology_version integer NOT NULL DEFAULT 1,
 ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1,
 ADD COLUMN IF NOT EXISTS created_by uuid,
 ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(),
 ADD COLUMN IF NOT EXISTS deleted_at timestamptz,
 ADD COLUMN IF NOT EXISTS module varchar(80),
 ADD COLUMN IF NOT EXISTS entity_type varchar(80),
 ADD COLUMN IF NOT EXISTS entity_id uuid,
 ADD COLUMN IF NOT EXISTS action varchar(100),
 ADD COLUMN IF NOT EXISTS before_json jsonb,
 ADD COLUMN IF NOT EXISTS after_json jsonb,
 ADD COLUMN IF NOT EXISTS reason text,
 ADD COLUMN IF NOT EXISTS severity varchar(30) NOT NULL DEFAULT 'information',
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS correlation_id text,
 ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now(),
 ADD COLUMN IF NOT EXISTS survey_id uuid,
 ADD COLUMN IF NOT EXISTS cycle_id uuid;
-- Eventos do pipeline usam code/status/data e não possuem necessariamente a
-- tripla de auditoria action/entity_type/entity_id.
ALTER TABLE valorapesquisa.platform_governance_events
 ALTER COLUMN action DROP NOT NULL,
 ALTER COLUMN entity_type DROP NOT NULL;
DO $platform_governance_contract$
BEGIN
 IF (SELECT data_type FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='platform_governance_events' AND column_name='methodology_version') <> 'integer' THEN
  ALTER TABLE valorapesquisa.platform_governance_events ALTER COLUMN methodology_version DROP DEFAULT;
  ALTER TABLE valorapesquisa.platform_governance_events ALTER COLUMN methodology_version TYPE integer
   USING CASE WHEN trim(methodology_version::text) ~ '^[0-9]+$' THEN methodology_version::text::integer
              WHEN trim(methodology_version::text) ~ '^[0-9]+([.][0-9]+)?$' THEN methodology_version::text::numeric::integer ELSE 1 END;
  ALTER TABLE valorapesquisa.platform_governance_events ALTER COLUMN methodology_version SET DEFAULT 1;
 END IF;
END $platform_governance_contract$;
CREATE INDEX IF NOT EXISTS ix_platform_governance_events_organization ON valorapesquisa.platform_governance_events(organization_id,created_at DESC) WHERE deleted_at IS NULL;
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'unread', ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, ADD COLUMN IF NOT EXISTS correlation_id text, ADD COLUMN IF NOT EXISTS updated_at timestamptz, ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE TABLE IF NOT EXISTS valorapesquisa.integrations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), provider text NOT NULL, status text NOT NULL DEFAULT 'inactive', config jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.audit_logs (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid, action text NOT NULL, entity_type text, entity_id text, message text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), ip_hash text, user_agent text, severity varchar(32) NOT NULL DEFAULT 'info', module varchar(80));
DO $audit_contract$ BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='actor_id') AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='user_id') THEN ALTER TABLE valorapesquisa.audit_logs RENAME COLUMN actor_id TO user_id; ELSE ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS user_id uuid; IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='actor_id') THEN UPDATE valorapesquisa.audit_logs SET user_id=actor_id WHERE user_id IS NULL; ALTER TABLE valorapesquisa.audit_logs DROP COLUMN actor_id; END IF; END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='entity_name') AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='entity_type') THEN ALTER TABLE valorapesquisa.audit_logs RENAME COLUMN entity_name TO entity_type; ELSE ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS entity_type text; IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='entity_name') THEN UPDATE valorapesquisa.audit_logs SET entity_type=entity_name WHERE entity_type IS NULL; ALTER TABLE valorapesquisa.audit_logs DROP COLUMN entity_name; END IF; END IF;
  IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='details') AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='metadata_json') THEN ALTER TABLE valorapesquisa.audit_logs RENAME COLUMN details TO metadata_json; ELSE ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS metadata_json jsonb; IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='audit_logs' AND column_name='details') THEN UPDATE valorapesquisa.audit_logs SET metadata_json=details WHERE metadata_json IS NULL; ALTER TABLE valorapesquisa.audit_logs DROP COLUMN details; END IF; END IF;
END $audit_contract$;
-- Keep the repository insert contract available on clean and partially migrated databases.
ALTER TABLE valorapesquisa.audit_logs
 ADD COLUMN IF NOT EXISTS message text,
 ADD COLUMN IF NOT EXISTS correlation_id text,
 ADD COLUMN IF NOT EXISTS created_at timestamptz,
 ADD COLUMN IF NOT EXISTS ip_hash text,
 ADD COLUMN IF NOT EXISTS user_agent text,
 ADD COLUMN IF NOT EXISTS severity varchar(32) NOT NULL DEFAULT 'info',
 ADD COLUMN IF NOT EXISTS module varchar(80);
ALTER TABLE valorapesquisa.audit_logs ALTER COLUMN entity_id TYPE text USING entity_id::text, ALTER COLUMN metadata_json TYPE jsonb USING metadata_json::jsonb, ALTER COLUMN metadata_json SET DEFAULT '{}'::jsonb, ALTER COLUMN created_at SET DEFAULT now();
UPDATE valorapesquisa.audit_logs SET metadata_json='{}'::jsonb WHERE metadata_json IS NULL;
UPDATE valorapesquisa.audit_logs SET created_at=now() WHERE created_at IS NULL;
ALTER TABLE valorapesquisa.audit_logs ALTER COLUMN metadata_json SET NOT NULL, ALTER COLUMN created_at SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_audit_logs_organization_created_at ON valorapesquisa.audit_logs(organization_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_logs_correlation_id ON valorapesquisa.audit_logs(correlation_id) WHERE correlation_id IS NOT NULL;
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

-- Reconcile installations created by earlier releases before enforcing the
-- canonical organization contract. This block is safe to execute repeatedly.
UPDATE valorapesquisa.organizations SET status = 'active' WHERE status IS NULL OR btrim(status) = '';
UPDATE valorapesquisa.organizations SET default_language_code = 'pt-BR' WHERE default_language_code IS NULL OR btrim(default_language_code) = '';
UPDATE valorapesquisa.organizations SET time_zone = 'America/Belem' WHERE time_zone IS NULL OR btrim(time_zone) = '';
UPDATE valorapesquisa.organizations SET onboarding_status = 'pending' WHERE onboarding_status IS NULL OR btrim(onboarding_status) = '';
UPDATE valorapesquisa.organizations SET version = 1 WHERE version IS NULL OR version < 1;
UPDATE valorapesquisa.organizations SET created_at = now() WHERE created_at IS NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN status SET DEFAULT 'active';
ALTER TABLE valorapesquisa.organizations ALTER COLUMN status SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN default_language_code SET DEFAULT 'pt-BR';
ALTER TABLE valorapesquisa.organizations ALTER COLUMN default_language_code SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN time_zone SET DEFAULT 'America/Belem';
ALTER TABLE valorapesquisa.organizations ALTER COLUMN time_zone SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN onboarding_status SET DEFAULT 'pending';
ALTER TABLE valorapesquisa.organizations ALTER COLUMN onboarding_status SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN version SET DEFAULT 1;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN version SET NOT NULL;
ALTER TABLE valorapesquisa.organizations ALTER COLUMN created_at SET DEFAULT now();
ALTER TABLE valorapesquisa.organizations ALTER COLUMN created_at SET NOT NULL;

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
WITH d AS (SELECT id,limit_key,row_number() OVER(PARTITION BY plan_id,lower(limit_key) ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.plan_limits)
UPDATE valorapesquisa.plan_limits x SET limit_key=x.limit_key||'-legacy-'||left(replace(x.id::text,'-',''),8),updated_at=now() FROM d WHERE d.id=x.id AND d.n>1;
WITH d AS (SELECT id,capability_key,row_number() OVER(PARTITION BY plan_id,lower(capability_key) ORDER BY created_at NULLS LAST,id) n FROM valorapesquisa.plan_capabilities)
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
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_limits_plan_key_ci ON valorapesquisa.plan_limits(plan_id,lower(limit_key));
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_capabilities_plan_key_ci ON valorapesquisa.plan_capabilities(plan_id,lower(capability_key));
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

-- Vocabulário canônico da Administração Master. O módulo físico permanece restrito
-- ao catálogo de módulos já existente e a autorização detalhada usa o código.
INSERT INTO valorapesquisa.permissions(code,name,description,module_code,functional_group,risk_level,display_order) VALUES
('organizations.read','Visualizar organizações','Consulta organizações com isolamento por tenant.','organization','organizations','high',1100),
('organizations.manage','Gerenciar organizações','Cria, altera e ativa organizações.','organization','organizations','critical',1110),
('questions.read','Visualizar perguntas','Consulta questionários e perguntas oficiais.','forms','questions','low',1120),
('questions.manage','Gerenciar perguntas','Mantém perguntas e versões oficiais.','forms','questions','high',1130),
('intelligence.read','Visualizar IA','Consulta execuções, evidências e insights.','operations','intelligence','high',1140),
('intelligence.manage','Gerenciar IA','Reprocessa e revisa execuções de IA.','operations','intelligence','critical',1150),
('intelligence.generate','Gerar análise de IA','Inicia análise a partir de diagnóstico e resultado selecionados.','operations','intelligence','high',1151),
('intelligence.review','Revisar análise de IA','Acessa a fila de revisão humana.','operations','intelligence','high',1152),
('insights.read','Visualizar insights','Consulta interpretações e suas evidências.','operations','insights','high',1153),
('insights.manage','Gerenciar insights','Gerencia o ciclo de vida de interpretações.','operations','insights','high',1154),
('insights.approve','Aprovar insights','Aprova uma interpretação após revisão humana.','operations','insights','critical',1155),
('insights.reject','Rejeitar insights','Rejeita uma interpretação com motivo obrigatório.','operations','insights','high',1156),
('insights.convert_to_action','Converter insight em ação','Cria ação vinculada à evidência.','operations','insights','critical',1157),
('insights.convert_to_decision','Converter insight em decisão','Cria decisão vinculada à evidência.','operations','insights','critical',1158),
('ai_runs.read','Visualizar execuções de IA','Consulta execução, eventos e consumo.','operations','ai_runs','high',1159),
('ai_runs.manage','Gerenciar execuções de IA','Gerencia reprocessamento e arquivamento.','operations','ai_runs','critical',1160),
('integrations.read','Visualizar integrações','Consulta conexões, webhooks e API keys sem expor segredos.','operations','integrations','high',1160),
('integrations.manage','Gerenciar integrações','Mantém conexões e credenciais protegidas.','operations','integrations','critical',1170),
('notifications.read','Visualizar notificações','Consulta entregas e erros de notificação.','communications','notifications','high',1180),
('notifications.manage','Gerenciar notificações','Reenvia e mantém templates de notificação.','communications','notifications','high',1190),
('notifications.mark_read','Marcar notificação como lida','Registra leitura somente para o destinatário autenticado.','communications','notifications','low',1191),
('communication.read','Visualizar central de comunicação','Consulta o painel de comunicação da organização.','communications','communication','high',1192),
('communication.manage','Gerenciar central de comunicação','Gerencia mensagens e operações da central.','communications','communication','high',1193),
('communication.templates.read','Visualizar templates de comunicação','Consulta templates e versões.','communications','communication_templates','medium',1194),
('communication.templates.manage','Gerenciar templates de comunicação','Cria e versiona templates validados.','communications','communication_templates','high',1195),
('communication.outbox.read','Visualizar outbox','Consulta fila, entregas e falhas da organização.','communications','communication_outbox','high',1196),
('communication.outbox.manage','Gerenciar outbox','Reprocessa mensagens com trilha de auditoria.','communications','communication_outbox','critical',1197),
('communication.reminders.read','Visualizar lembretes','Consulta regras e execuções de lembrete.','communications','communication_reminders','medium',1198),
('communication.reminders.manage','Gerenciar lembretes','Cria e altera regras de lembrete.','communications','communication_reminders','high',1199),
('jobs.read','Visualizar jobs','Consulta filas, tentativas e correlações.','operations','jobs','high',1200),
('jobs.manage','Gerenciar jobs','Reprocessa ou cancela jobs.','operations','jobs','critical',1210),
('logs.read','Visualizar logs','Consulta eventos operacionais sanitizados.','operations','logs','high',1220),
('support.read','Visualizar suporte','Consulta tickets e referências de erro.','operations','support','medium',1230),
('support.manage','Gerenciar suporte','Prioriza, atribui e resolve tickets.','operations','support','high',1240)
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
UPDATE valorapesquisa.roles
SET name='Administrador Valora',is_system=true,deleted_at=NULL,updated_at=now()
WHERE code='admin_valora' AND organization_id IS NULL;

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

-- Valora Executive Deliverables (idempotent consolidation, 2026-08).
BEGIN;
CREATE TABLE IF NOT EXISTS valorapesquisa.formal_deliverables (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, diagnostic_id uuid,
 result_id uuid, deliverable_type varchar(40) NOT NULL, title varchar(200) NOT NULL,
 description text, status varchar(30) NOT NULL DEFAULT 'pending', generated_by_user_id uuid,
 file_id uuid, share_link_id uuid, metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz
);
CREATE TABLE IF NOT EXISTS valorapesquisa.formal_deliverable_files (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, deliverable_id uuid NOT NULL,
 file_name varchar(255) NOT NULL, content_type varchar(160) NOT NULL, file_extension varchar(16) NOT NULL,
 file_path text NOT NULL, file_size_bytes bigint NOT NULL CHECK(file_size_bytes > 0), checksum char(64) NOT NULL,
 storage_provider varchar(40) NOT NULL DEFAULT 'database', created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz
);
CREATE TABLE IF NOT EXISTS valorapesquisa.formal_deliverable_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid, deliverable_type varchar(40) NOT NULL,
 name varchar(160) NOT NULL, version integer NOT NULL DEFAULT 1, template_json jsonb NOT NULL DEFAULT '{}',
 is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz
);
CREATE TABLE IF NOT EXISTS valorapesquisa.formal_deliverable_generation_jobs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, deliverable_id uuid,
 status varchar(30) NOT NULL DEFAULT 'queued', requested_by_user_id uuid, error_message text,
 requested_at timestamptz NOT NULL DEFAULT now(), started_at timestamptz, completed_at timestamptz
);
CREATE TABLE IF NOT EXISTS valorapesquisa.secure_share_links (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, deliverable_id uuid,
 diagnostic_id uuid, result_id uuid, token_hash char(64) NOT NULL UNIQUE, public_slug varchar(80) NOT NULL UNIQUE,
 title varchar(200) NOT NULL, status varchar(20) NOT NULL DEFAULT 'active', expires_at timestamptz NOT NULL,
 max_access_count integer, access_count integer NOT NULL DEFAULT 0, allow_download boolean NOT NULL DEFAULT false,
 requires_pin boolean NOT NULL DEFAULT false, pin_hash char(64), created_by_user_id uuid, revoked_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 CHECK (max_access_count IS NULL OR max_access_count > 0), CHECK (NOT requires_pin OR pin_hash IS NOT NULL)
);
CREATE TABLE IF NOT EXISTS valorapesquisa.secure_share_link_access_logs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), share_link_id uuid NOT NULL, access_type varchar(20) NOT NULL,
 was_allowed boolean NOT NULL, denial_reason varchar(80), ip_hash char(64), user_agent_hash char(64), accessed_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS valorapesquisa.certificate_issuances (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, deliverable_id uuid,
 diagnostic_id uuid, result_id uuid, public_code varchar(80) NOT NULL UNIQUE, score numeric(10,2), maturity_level varchar(100),
 issued_by_user_id uuid, issued_at timestamptz NOT NULL DEFAULT now(), status varchar(20) NOT NULL DEFAULT 'valid', revoked_at timestamptz
);
CREATE TABLE IF NOT EXISTS valorapesquisa.certificate_download_logs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), certificate_issuance_id uuid NOT NULL, organization_id uuid NOT NULL,
 actor_user_id uuid, share_link_id uuid, was_allowed boolean NOT NULL, downloaded_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS valorapesquisa.report_generation_logs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, deliverable_id uuid, diagnostic_id uuid,
 result_id uuid, format varchar(16) NOT NULL, status varchar(30) NOT NULL, generated_by_user_id uuid,
 detail text, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS valorapesquisa.public_result_sessions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, share_link_id uuid NOT NULL,
 session_hash char(64) NOT NULL UNIQUE, expires_at timestamptz NOT NULL, last_access_at timestamptz NOT NULL DEFAULT now(),
 created_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz
);
-- Repair partially provisioned installations before indexes or repositories use these columns.
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS diagnostic_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS result_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS file_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS share_link_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}';
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS diagnostic_id uuid;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS result_id uuid;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS deliverable_id uuid;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS public_slug varchar(80);
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS status varchar(20) NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS max_access_count integer;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS access_count integer NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS allow_download boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS requires_pin boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS pin_hash char(64);
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS revoked_at timestamptz;
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.secure_share_links ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_formal_deliverables_org_result ON valorapesquisa.formal_deliverables(organization_id,result_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_formal_deliverable_files_deliverable ON valorapesquisa.formal_deliverable_files(deliverable_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_secure_share_links_lookup ON valorapesquisa.secure_share_links(token_hash,status,expires_at) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_share_access_link ON valorapesquisa.secure_share_link_access_logs(share_link_id,accessed_at DESC);
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('deliverables.read','Consultar entregáveis','Consulta entregas formais da organização.','organizational_intelligence'),
 ('deliverables.manage','Gerenciar entregáveis','Gerencia entregas formais da organização.','organizational_intelligence'),
 ('reports.download','Baixar relatórios','Baixa relatórios executivos autorizados.','organizational_intelligence'),
 ('certificates.download','Baixar certificados','Baixa certificados autorizados.','certificates'),
 ('share_links.read','Consultar links seguros','Consulta compartilhamentos seguros.','organizational_intelligence'),
 ('share_links.manage','Gerenciar links seguros','Cria e revoga compartilhamentos seguros.','organizational_intelligence'),
 ('public_results.manage','Gerenciar resultados públicos','Gerencia a publicação de resultados.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code;
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at)
SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN
('deliverables.read','deliverables.manage','reports.read','reports.generate','reports.download','certificates.read','certificates.generate','certificates.download','share_links.read','share_links.manage','public_results.manage')
ON CONFLICT(role_id,permission_id) DO NOTHING;
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
ALTER TABLE valorapesquisa.valora_actions DROP CONSTRAINT IF EXISTS valora_actions_status_check;
ALTER TABLE valorapesquisa.valora_actions ADD CONSTRAINT valora_actions_status_check
 CHECK(status IN('recommended','planned','in_progress','waiting','overdue','completed','cancelled','reviewed','replanned'));
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

-- Premium organizational journeys. Columns precede indexes so clean and
-- partially provisioned installations converge safely.
CREATE TABLE IF NOT EXISTS valorapesquisa.survey_campaigns (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid,
 name varchar(180) NOT NULL, audience_description text, starts_at timestamptz, ends_at timestamptz, status varchar(30) NOT NULL DEFAULT 'draft',
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_survey_campaigns_org_status ON valorapesquisa.survey_campaigns(organization_id,status,ends_at) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_priorities (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), diagnostic_id uuid,
 origin varchar(80) NOT NULL, evidence_reference text NOT NULL, title varchar(200) NOT NULL, impact varchar(30) NOT NULL,
 urgency varchar(30) NOT NULL, owner_user_id uuid REFERENCES valorapesquisa.users(id), recommended_action text NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'open', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_priorities_org_status ON valorapesquisa.organizational_priorities(organization_id,status,urgency) WHERE deleted_at IS NULL;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('onboarding.read','Visualizar onboarding','Consulta o progresso da configuração organizacional.','organization'),
 ('onboarding.manage','Gerenciar onboarding','Mantém a configuração guiada da organização.','organization'),
 ('campaigns.read','Visualizar campanhas','Consulta campanhas e adesão da coleta.','surveys'),
 ('campaigns.manage','Gerenciar campanhas','Publica campanhas, convites e lembretes.','surveys'),
 ('respondents.read','Visualizar respondentes','Consulta participação conforme regras de privacidade.','organization'),
 ('respondents.manage','Gerenciar respondentes','Mantém públicos e convites autorizados.','organization'),
 ('evidence.read','Visualizar evidências','Consulta evidências e sua rastreabilidade.','organizational_intelligence'),
 ('evidence.manage','Gerenciar evidências','Classifica e vincula evidências autorizadas.','organizational_intelligence'),
 ('indexes.read','Visualizar índices','Consulta índices oficiais e sua composição.','organizational_intelligence'),
 ('priorities.read','Visualizar prioridades','Consulta riscos e oportunidades priorizados.','organizational_intelligence'),
 ('priorities.manage','Gerenciar prioridades','Atribui responsáveis e atualiza decisões prioritárias.','organizational_intelligence'),
 ('leadership.read','Visualizar lideranças','Consulta a jornada autorizada de lideranças.','organizational_intelligence'),
 ('leadership.manage','Gerenciar lideranças','Mantém acompanhamento e desenvolvimento de lideranças.','organizational_intelligence'),
 ('settings.manage','Gerenciar configurações','Mantém configurações permitidas da organização.','settings'),
 ('branding.manage','Gerenciar marca','Mantém identidade visual autorizada da organização.','organization')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();

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

-- 38. VALORABOT: orientação determinística, histórico, dúvidas e feedback.
BEGIN;
CREATE TABLE IF NOT EXISTS valorapesquisa.valorabot_knowledge_base(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), intent text NOT NULL, question_patterns text NOT NULL, answer text NOT NULL,
 action_label text, action_url text, priority integer NOT NULL DEFAULT 0, is_active boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE UNIQUE INDEX IF NOT EXISTS ux_valorabot_knowledge_intent ON valorapesquisa.valorabot_knowledge_base(intent);
CREATE TABLE IF NOT EXISTS valorapesquisa.valorabot_sessions(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id),
 context text, created_at timestamptz NOT NULL DEFAULT now(), last_activity_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valorabot_sessions_activity ON valorapesquisa.valorabot_sessions(last_activity_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.valorabot_messages(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), session_id uuid NOT NULL REFERENCES valorapesquisa.valorabot_sessions(id) ON DELETE CASCADE,
 role text NOT NULL CHECK(role IN('user','assistant')), content text NOT NULL, intent text, confidence numeric(4,3), created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valorabot_messages_session ON valorapesquisa.valorabot_messages(session_id,created_at);
CREATE TABLE IF NOT EXISTS valorapesquisa.valorabot_unanswered_questions(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), session_id uuid NOT NULL REFERENCES valorapesquisa.valorabot_sessions(id) ON DELETE CASCADE,
 question text NOT NULL, normalized_question text NOT NULL, status text NOT NULL DEFAULT 'open' CHECK(status IN('open','reviewed','resolved')),
 created_at timestamptz NOT NULL DEFAULT now(), resolved_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_valorabot_unanswered_status ON valorapesquisa.valorabot_unanswered_questions(status,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.valorabot_feedback(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), session_id uuid NOT NULL REFERENCES valorapesquisa.valorabot_sessions(id) ON DELETE CASCADE,
 message_id uuid NOT NULL REFERENCES valorapesquisa.valorabot_messages(id) ON DELETE CASCADE, helpful boolean NOT NULL, comment text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(session_id,message_id));

INSERT INTO valorapesquisa.valorabot_knowledge_base(intent,question_patterns,answer,action_label,action_url,priority) VALUES
('valora_insight','valora insight|metodologia valora|metodologia|observacao|evidencias','Valora Insight™ transforma evidências em uma leitura responsável: Observação, Evidências, Correlação, Causa provável, Impacto organizacional, Prioridade e Plano de evolução. Uma causa só é apresentada como hipótese e exige ao menos 3 evidências convergentes.','Conhecer o diagnóstico','/diagnostico-gratuito',100),
('free_diagnostic','diagnostico gratuito|diagnostico|comecar avaliacao','O diagnóstico gratuito coleta respostas estruturadas, calcula a maturidade pelas dimensões avaliadas e entrega uma devolutiva. Responda com sinceridade; o sistema não inventa dados quando as evidências são insuficientes.','Iniciar diagnóstico','/diagnostico-gratuito',90),
('result','resultado|devolutiva|pontuacao','O resultado apresenta a leitura consolidada e o nível de evidência disponível. Se houver menos de 3 evidências convergentes, não conclui causa ou prioridade com segurança.','Ver orientações','/contato',80),
('certificate','certificado|baixar certificado|download certificado|erro certificado','O certificado fica disponível após a conclusão e o processamento do diagnóstico. Se o download falhar, atualize a página, confira o link recebido e tente novamente; persistindo, fale com a equipe e informe a referência exibida na tela.','Validar certificado','/certificado/validar',90),
('lgpd','lgpd|privacidade|dados pessoais|excluir dados','A Valora trata dados conforme finalidade, necessidade e segurança. Na área LGPD você pode consultar informações e solicitar atendimento sobre seus direitos. Nunca envie dados sensíveis pelo chat.','Consultar LGPD','/lgpd',90),
('plans','planos|plano|assinatura|preco','Os planos liberam capacidades conforme o contexto contratado. Consulte a página de planos ou converse com a equipe para entender as funcionalidades, sem compromisso.','Conhecer planos','/planos',70),
('access','login|entrar|acesso|senha|esqueci senha','Para acessar, use a página Entrar. Se esqueceu a senha, escolha a recuperação de acesso. O ValoraBot nunca solicita senha, token ou documento.','Entrar','/entrar',85),
('survey_error','erro pesquisa|nao consigo responder|responder pesquisa|enviar respostas','Se a pesquisa não carregar ou não enviar, confira a conexão, mantenha a mesma janela aberta e tente novamente. Não duplique envios. Se persistir, informe ao suporte o horário e a referência do erro, sem enviar respostas pessoais.','Falar com suporte','https://wa.me/5591992545353',90),
('dashboard','dashboard|painel|indicadores','O Dashboard consolida indicadores permitidos para a organização. Estados sem dados são informativos: ausência de evidência não é resultado negativo.','Abrir Dashboard','/Dashboard',65),
('heatmap','heatmap|mapa de calor','Heatmap evidencia intensidades e lacunas entre dimensões no mesmo conjunto observado. Ele não determina causa sozinho e deve ser interpretado com as demais evidências.','Abrir Inteligência','/Intelligence/heatmap',75),
('benchmark','benchmark|comparacao|ranking','Benchmark compara referências de forma contextual e agregada. A Valora não o utiliza como ranking público, nem como prova isolada de desempenho ou causalidade.','Abrir Benchmark','/Intelligence/benchmark',80),
('action','action|plano de acao|acao','Action transforma uma prioridade sustentada por evidências em ação com responsável, prazo, indicador e critério de conclusão.','Abrir Action','/OperationalIntelligence/ActionPlans',75),
('evolution','evolution|evolucao|historico','Evolution acompanha mudanças entre ciclos comparáveis. Tendências só são apresentadas quando existe histórico suficiente.','Abrir Evolution','/Intelligence/evolution',70),
('journey','journey|jornada|marcos','Journey registra marcos da evolução organizacional e conecta decisões, ações e novos ciclos de evidência.','Abrir Journey','/Intelligence/journey',70),
('executive_report','executive report|relatorio executivo|relatorio','Executive Report organiza a leitura para decisão executiva, preservando limites de evidência e evitando transformar sintomas em causas.','Abrir relatório','/Intelligence/executive-report',70),
('whatsapp','whatsapp|telefone|contato|atendimento humano','O WhatsApp oficial da Valora Group é +55 91 99254-5353.','Abrir WhatsApp','https://wa.me/5591992545353',100)
ON CONFLICT(intent) DO UPDATE SET question_patterns=EXCLUDED.question_patterns,answer=EXCLUDED.answer,action_label=EXCLUDED.action_label,action_url=EXCLUDED.action_url,priority=EXCLUDED.priority,is_active=true,updated_at=now();
INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('2026_08_valorabot','sha256:valorabot-deterministic-v1') ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum;
COMMIT;

-- Assets oficiais: /img/brand/valora-logo-full.svg e /img/brand/valora-symbol.svg (com fallback textual acessível).
-- Contas e credenciais de demonstração não pertencem ao bootstrap canônico.
-- Use seeds/seed_demo.sql somente via VALORA_SEED_DEMO=true em Development.

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
  IF NOT EXISTS (SELECT 1 FROM valorapesquisa.plan_capabilities pc JOIN valorapesquisa.plans p ON p.id=pc.plan_id WHERE p.code IN('free','professional','corporate','enterprise')) THEN RAISE EXCEPTION 'Validação falhou: capabilities dos planos ausentes'; END IF;
  IF to_regclass('valorapesquisa.result_scores') IS NULL OR to_regclass('valorapesquisa.certificates') IS NULL OR to_regclass('valorapesquisa.organizational_intelligence_runs') IS NULL THEN RAISE EXCEPTION 'Validação falhou: tabelas de resultado, certificado ou inteligência ausentes'; END IF;
  RAISE NOTICE 'Validação Valora concluída: formulário oficial, 5 dimensões, 25 perguntas quantitativas, 1 qualitativa e capabilities OK';
END $validation$;

-- Núcleo Metodológico Valora™: catálogo global, versionado e somente administrável
-- pela plataforma. Leituras organizacionais permanecem segregadas nas tabelas abaixo.
BEGIN;
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_concepts(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(80) NOT NULL UNIQUE, name varchar(180) NOT NULL,
 pillar varchar(100) NOT NULL, definition text NOT NULL, evolution_guidance text NOT NULL,
 related_indices text[] NOT NULL DEFAULT '{}', deprecated_terms text[] NOT NULL DEFAULT '{}', version integer NOT NULL DEFAULT 1,
 is_active boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_methodology_concepts_pillar ON valorapesquisa.methodology_concepts(pillar) WHERE deleted_at IS NULL;
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS strategic_purpose text NOT NULL DEFAULT 'Orientar decisões e evolução organizacional com evidências.';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS diagnostic_questions text[] NOT NULL DEFAULT '{}';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS related_indicators text[] NOT NULL DEFAULT '{}';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS organizational_impacts text[] NOT NULL DEFAULT '{}';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS maturity_level varchar(30) NOT NULL DEFAULT 'structuring';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS methodology_version varchar(30) NOT NULL DEFAULT '1.0';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS display_order integer NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}';
CREATE INDEX IF NOT EXISTS ix_methodology_concepts_order ON valorapesquisa.methodology_concepts(display_order, pillar) WHERE deleted_at IS NULL AND status='active';
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_relations(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), source_concept_id uuid NOT NULL REFERENCES valorapesquisa.methodology_concepts(id),
 target_concept_id uuid NOT NULL REFERENCES valorapesquisa.methodology_concepts(id), relation_type varchar(60) NOT NULL,
 influence_weight numeric(5,4) NOT NULL DEFAULT 1, rationale text NOT NULL, version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(source_concept_id,target_concept_id,relation_type,version));
CREATE INDEX IF NOT EXISTS ix_methodology_relations_source ON valorapesquisa.methodology_relations(source_concept_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_methodology_relations_target ON valorapesquisa.methodology_relations(target_concept_id) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_evidence_patterns(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), concept_id uuid NOT NULL REFERENCES valorapesquisa.methodology_concepts(id),
 pattern_type varchar(40) NOT NULL CHECK(pattern_type IN('expected','low_maturity','risk','opportunity')),
 description text NOT NULL, minimum_occurrences integer NOT NULL DEFAULT 3 CHECK(minimum_occurrences>=1), weight numeric(5,4) NOT NULL DEFAULT 1,
 version integer NOT NULL DEFAULT 1, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_maturity_levels(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(30) NOT NULL UNIQUE, name varchar(100) NOT NULL,
 minimum_score numeric(5,2) NOT NULL, maximum_score numeric(5,2) NOT NULL, description text NOT NULL,
 version integer NOT NULL DEFAULT 1, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_inference_rules(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(80) NOT NULL UNIQUE, name varchar(180) NOT NULL,
 minimum_evidence integer NOT NULL DEFAULT 3 CHECK(minimum_evidence>=3), condition_definition jsonb NOT NULL DEFAULT '{}',
 outcome_definition jsonb NOT NULL DEFAULT '{}', methodology_version integer NOT NULL DEFAULT 1, is_active boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

INSERT INTO valorapesquisa.methodology_maturity_levels(code,name,minimum_score,maximum_score,description) VALUES
 ('initial','Nível 1 — Inicial',0,25,'Práticas incipientes e dependentes de iniciativas isoladas.'),
 ('structuring','Nível 2 — Estruturante',26,50,'Práticas em estruturação, ainda com integração limitada.'),
 ('integrated','Nível 3 — Integrado',51,75,'Capacidades conectadas e evidências recorrentes.'),
 ('mature','Nível 4 — Maduro',76,100,'Arquitetura integrada, adaptativa e sustentável.') ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,minimum_score=EXCLUDED.minimum_score,maximum_score=EXCLUDED.maximum_score,description=EXCLUDED.description,updated_at=now();

INSERT INTO valorapesquisa.methodology_concepts(code,name,pillar,definition,evolution_guidance,related_indices,deprecated_terms) VALUES
 ('company','Empresa','Fundamentos','Entidade econômica e jurídica na qual uma organização opera.','Alinhar identidade empresarial e funcionamento organizacional.','{}','{}'),
 ('organization','Organização','Fundamentos','Sistema humano e técnico orientado por propósito, relações, decisões e resultados.','Compreender relações e padrões antes de intervir.','{IMO}','{}'),
 ('system','Sistema','Fundamentos','Menor unidade de análise Valora: conjunto interdependente de capacidades, fluxos e decisões.','Evoluir interfaces, critérios e ciclos de aprendizagem.','{IIS}','{}'),
 ('organizational-architecture','Arquitetura Organizacional','Arquitetura','Configuração integrada de cultura, governança, liderança, pessoas e sistemas.','Tratar causas sistêmicas e efeitos cascata.','{IMO,IIS}','{"estrutura organizacional"}'),
 ('organizational-maturity','Maturidade Organizacional','Arquitetura','Capacidade recorrente de operar, aprender, integrar e evoluir de modo sustentável.','Fortalecer consistência e integração entre capacidades.','{IMO}','{}'),
 ('systemic-clarity','Clareza Sistêmica™','Arquitetura','Compreensão compartilhada de propósito, papéis, responsabilidades, critérios e interfaces.','Tornar explícitos papéis, decisões e indicadores.','{ICS}','{"clareza operacional"}'),
 ('organizational-intelligence','Inteligência Organizacional','Inteligência','Capacidade de converter evidências e aprendizagem em decisões melhores.','Instituir ciclos confiáveis de evidência, decisão e aprendizagem.','{IIO}','{}'),
 ('organizational-governance','Governança Organizacional','Governança','Capacidade organizacional que orienta decisões, responsabilidades, riscos e resultados.','Definir critérios, alçadas, accountability e indicadores.','{IGO}','{}'),
 ('organizational-culture','Cultura Organizacional','Cultura','Padrões compartilhados que orientam comportamentos, relações e decisões.','Alinhar práticas recorrentes aos princípios declarados.','{ICO}','{}'),
 ('leadership','Liderança','Liderança','Capacidade de produzir clareza, contexto, desenvolvimento e responsabilidade.','Fortalecer decisões, aprendizagem e segurança para contribuir.','{ILI}','{}'),
 ('people','Pessoas','Pessoas','Participantes do sistema organizacional, nunca indicadores isolados ou objetos de controle.','Criar condições de contribuição, desenvolvimento e pertencimento.','{IPO}','{}'),
 ('organizational-development','Desenvolvimento Organizacional','Evolução','Capacidade planejada de transformar a arquitetura e sustentar aprendizagem.','Conectar diagnóstico, ação, aprendizagem e novo ciclo.','{IDO}','{}'),
 ('organizational-sustainability','Sustentabilidade Organizacional','Evolução','Capacidade de preservar resultados, relações e adaptação no tempo.','Equilibrar resultados presentes e capacidade futura.','{ISO}','{}'),
 ('organizational-role','Papel Organizacional','Clareza','Contribuição esperada de uma função no sistema.','Explicitar propósito, interfaces e decisões do papel.','{ICS}','{}'),
 ('organizational-responsibility','Responsabilidade Organizacional','Clareza','Compromisso explícito associado a um resultado ou decisão.','Vincular responsabilidade a autonomia e critérios.','{ICS,IAC}','{}'),
 ('accountability','Accountability','Governança','Prática de assumir, prestar contas e aprender sobre compromissos e resultados.','Criar acordos verificáveis e ciclos de prestação de contas.','{IAC}','{}'),
 ('responsible-autonomy','Autonomia Responsável™','Governança','Liberdade para decidir dentro de contexto, critérios, limites e accountability.','Clarificar alçadas e ampliar autonomia com evidências.','{IAR}','{"autonomia"}'),
 ('organizational-decision','Decisão Organizacional','Governança','Escolha rastreável orientada por contexto, critérios e evidências.','Registrar critérios, responsáveis e aprendizagem.','{IGO,IIO}','{}'),
 ('organizational-indicators','Indicadores Organizacionais','Inteligência','Evidências quantitativas ou qualitativas para compreender o sistema, não controlar pessoas.','Interpretar indicadores em conjunto e no tempo.','{IIO}','{}'),
 ('organizational-results','Resultados Organizacionais','Resultados','Efeitos sustentáveis produzidos pela arquitetura organizacional.','Relacionar resultados às capacidades e evidências que os sustentam.','{IMO,ISO}','{}')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,pillar=EXCLUDED.pillar,definition=EXCLUDED.definition,evolution_guidance=EXCLUDED.evolution_guidance,related_indices=EXCLUDED.related_indices,deprecated_terms=EXCLUDED.deprecated_terms,updated_at=now(),deleted_at=NULL;

WITH edges(source,target,rationale) AS (VALUES
 ('systemic-clarity','organizational-role','Clareza explicita a contribuição esperada de cada papel.'),
 ('systemic-clarity','organizational-responsibility','Clareza torna responsabilidades verificáveis.'),
 ('systemic-clarity','organizational-decision','Clareza melhora critérios e alçadas de decisão.'),
 ('systemic-clarity','responsible-autonomy','Clareza oferece contexto e limites para autonomia.'),
 ('systemic-clarity','accountability','Clareza permite compromissos e prestação de contas.'),
 ('organizational-governance','organizational-decision','Governança define critérios e alçadas.'),
 ('organizational-governance','responsible-autonomy','Governança define limites e mecanismos de responsabilidade.'),
 ('leadership','systemic-clarity','Liderança produz contexto e alinhamento.'),
 ('leadership','organizational-culture','Práticas de liderança reforçam padrões culturais.'),
 ('organizational-culture','organizational-decision','Padrões culturais condicionam decisões recorrentes.'),
 ('organizational-intelligence','organizational-decision','Evidência e aprendizagem qualificam decisões.'),
 ('organizational-intelligence','organizational-governance','Inteligência fortalece governança adaptativa.'))
INSERT INTO valorapesquisa.methodology_relations(source_concept_id,target_concept_id,relation_type,influence_weight,rationale)
SELECT s.id,t.id,'influences',1,e.rationale FROM edges e JOIN valorapesquisa.methodology_concepts s ON s.code=e.source JOIN valorapesquisa.methodology_concepts t ON t.code=e.target
ON CONFLICT(source_concept_id,target_concept_id,relation_type,version) DO UPDATE SET rationale=EXCLUDED.rationale,updated_at=now(),deleted_at=NULL;

INSERT INTO valorapesquisa.methodology_inference_rules(code,name,minimum_evidence,condition_definition,outcome_definition) VALUES
 ('convergent-evidence','Evidências convergentes mínimas',3,'{"distinctEvidence":true,"isolatedIndicator":false}','{"belowMinimum":"Dados insuficientes","confidence":{"3":"Moderada","4-6":"Alta","7+":"Muito Alta"}}')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,minimum_evidence=EXCLUDED.minimum_evidence,condition_definition=EXCLUDED.condition_definition,outcome_definition=EXCLUDED.outcome_definition,updated_at=now();
COMMIT;

-- Persistência multiempresa dos módulos profissionais. O payload versionado evita
-- inventar resultados durante a implantação e mantém escopo, autoria e auditoria.
DO $modules$
DECLARE table_name text;
BEGIN
 FOREACH table_name IN ARRAY ARRAY[
 'inference_runs','inference_results','inference_evidence','inference_rule_matches',
 'metrics_catalog','metric_values','metric_targets','metric_history','metric_alerts',
 'valora_indices','index_values','index_components','index_history','index_interpretations',
 'radar_snapshots','radar_dimensions','radar_interpretations','heatmap_snapshots','heatmap_cells','heatmap_interpretations','heatmap_alerts',
 'insight_runs','insights','insight_evidence','insight_related_concepts','insight_actions',
 'action_items','action_history',
 'evolution_cycles','evolution_index_history','evolution_trends','evolution_alerts','evolution_projections',
 'journey_events','journey_event_links','journey_narratives','journey_milestones',
 'benchmark_runs','benchmark_groups','benchmark_results','benchmark_interpretations','external_benchmark_reference_sets',
 'executive_reports','executive_report_sections','executive_report_exports','executive_report_access_links',
 'one_on_one_sessions','one_on_one_topics','one_on_one_commitments','one_on_one_history','one_on_one_suggestions',
 'integration_connectors','integration_exports','integration_audit_events',
 'configuration_change_history','permission_change_history','data_export_history','governance_cycles']
 LOOP
  EXECUTE format('CREATE TABLE IF NOT EXISTS valorapesquisa.%I (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), code varchar(100), status varchar(40) NOT NULL DEFAULT ''draft'', data jsonb NOT NULL DEFAULT ''{}''::jsonb, methodology_version integer NOT NULL DEFAULT 1, version integer NOT NULL DEFAULT 1, created_by uuid REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz)',table_name);
  EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS organization_id uuid',table_name);
  EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now()',table_name);
  EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS deleted_at timestamptz',table_name);
  EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON valorapesquisa.%I(organization_id,created_at DESC) WHERE deleted_at IS NULL','ix_'||table_name||'_organization',table_name);
 END LOOP;
END $modules$;

-- 41. PIPELINE VIVO DE INTELIGÊNCIA ORGANIZACIONAL (aditivo, rastreável e multiempresa)
BEGIN;
ALTER TABLE valorapesquisa.responses ADD COLUMN IF NOT EXISTS form_id uuid REFERENCES valorapesquisa.forms(id);
ALTER TABLE valorapesquisa.response_answers ADD COLUMN IF NOT EXISTS answer_json jsonb NOT NULL DEFAULT '{}';
ALTER TABLE valorapesquisa.response_answers ADD COLUMN IF NOT EXISTS answer_text text;
ALTER TABLE valorapesquisa.response_answers ADD COLUMN IF NOT EXISTS score numeric(10,4);
ALTER TABLE valorapesquisa.response_answers ADD COLUMN IF NOT EXISTS max_score numeric(10,4);
CREATE TABLE IF NOT EXISTS valorapesquisa.question_concept_mappings(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id),
 form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id), question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id),
 concept_code varchar(80) NOT NULL, capability_code varchar(80) NOT NULL, dimension_code varchar(80) NOT NULL,
 weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0), polarity smallint NOT NULL DEFAULT 1 CHECK(polarity IN(-1,1)),
 evidence_type varchar(40) NOT NULL DEFAULT 'quantitative_response', calculation_rule jsonb NOT NULL DEFAULT '{"normalization":"score_percentage"}',
 is_official boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_question_concept_mapping ON valorapesquisa.question_concept_mappings(question_id,concept_code,coalesce(organization_id,'00000000-0000-0000-0000-000000000000'::uuid)) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_question_concept_form ON valorapesquisa.question_concept_mappings(form_id,question_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.question_metric_mappings(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id),
 question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id), metric_code varchar(80) NOT NULL, weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),
 is_official boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_question_metric_mapping ON valorapesquisa.question_metric_mappings(question_id,metric_code,coalesce(organization_id,'00000000-0000-0000-0000-000000000000'::uuid)) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.question_index_mappings(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id),
 question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id), index_code varchar(12) NOT NULL, weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),
 is_official boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_question_index_mapping ON valorapesquisa.question_index_mappings(question_id,index_code,coalesce(organization_id,'00000000-0000-0000-0000-000000000000'::uuid)) WHERE deleted_at IS NULL;

-- A pergunta qualitativa oficial possui weight=0 porque não pontua. As tabelas
-- de mapeamento, porém, exigem peso estritamente positivo. Normalize resíduos
-- de execuções legadas antes dos seeds, sem alterar o peso da pergunta fonte.
UPDATE valorapesquisa.question_concept_mappings SET weight=1.00 WHERE weight IS NULL OR weight<=0;
UPDATE valorapesquisa.question_metric_mappings SET weight=1.00 WHERE weight IS NULL OR weight<=0;
UPDATE valorapesquisa.question_index_mappings SET weight=1.00 WHERE weight IS NULL OR weight<=0;

CREATE TABLE IF NOT EXISTS valorapesquisa.evidence_items(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid REFERENCES valorapesquisa.surveys(id),
 response_id uuid REFERENCES valorapesquisa.responses(id), form_id uuid REFERENCES valorapesquisa.forms(id), question_id uuid REFERENCES valorapesquisa.questions(id),
 concept_code varchar(80) NOT NULL, capability_code varchar(80) NOT NULL, dimension_code varchar(80) NOT NULL, evidence_type varchar(40) NOT NULL,
 source_type varchar(40) NOT NULL, source_id uuid NOT NULL, normalized_value numeric(10,4), raw_value text, weight numeric(8,4) NOT NULL DEFAULT 1,
 confidence_weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(confidence_weight BETWEEN 0 AND 1), text_excerpt varchar(500), metadata_json jsonb NOT NULL DEFAULT '{}',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_evidence_response_question_concept ON valorapesquisa.evidence_items(response_id,question_id,concept_code) WHERE deleted_at IS NULL AND response_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_evidence_org_concept ON valorapesquisa.evidence_items(organization_id,concept_code,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_evidence_source ON valorapesquisa.evidence_items(source_type,source_id) WHERE deleted_at IS NULL;
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS metric_code varchar(80);
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS index_code varchar(12);
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS polarity smallint NOT NULL DEFAULT 1 CHECK(polarity IN(-1,1));
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS score numeric(10,4);
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS mapping_status varchar(30);
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS source_reference text;
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS can_be_used_for_inference boolean NOT NULL DEFAULT false;
UPDATE valorapesquisa.evidence_items SET metric_code=metadata_json->>'metricCode',index_code=metadata_json->>'indexCode',
 polarity=coalesce((metadata_json->>'polarity')::smallint,1)
WHERE metric_code IS NULL OR index_code IS NULL;
CREATE INDEX IF NOT EXISTS ix_evidence_org_metric ON valorapesquisa.evidence_items(organization_id,metric_code,created_at DESC) WHERE deleted_at IS NULL AND metric_code IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_evidence_org_index ON valorapesquisa.evidence_items(organization_id,index_code,created_at DESC) WHERE deleted_at IS NULL AND index_code IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_evidence_org_survey ON valorapesquisa.evidence_items(organization_id,survey_id,created_at DESC) WHERE deleted_at IS NULL;
UPDATE valorapesquisa.evidence_items
SET metadata_json=metadata_json || jsonb_build_object(
  'mappingStatus',CASE WHEN concept_code<>'unmapped' AND metric_code IS NOT NULL AND index_code IS NOT NULL THEN 'mapped' ELSE 'pending_mapping' END,
  'missingMappings',array_remove(ARRAY[CASE WHEN concept_code='unmapped' THEN 'concept' END,CASE WHEN metric_code IS NULL THEN 'metric' END,CASE WHEN index_code IS NULL THEN 'index' END],NULL)),
  updated_at=now()
WHERE NOT metadata_json ? 'mappingStatus';
UPDATE valorapesquisa.evidence_items
SET mapping_status=CASE WHEN concept_code<>'unmapped' AND metric_code IS NOT NULL AND index_code IS NOT NULL THEN 'mapped' ELSE 'pending_mapping' END,
    source_reference=coalesce(source_reference,source_id::text),
    can_be_used_for_inference=(normalized_value IS NOT NULL AND concept_code<>'unmapped' AND metric_code IS NOT NULL AND index_code IS NOT NULL),
    updated_at=now()
WHERE mapping_status IS NULL OR mapping_status='pending' OR source_reference IS NULL;
ALTER TABLE valorapesquisa.evidence_items ALTER COLUMN mapping_status SET DEFAULT 'pending_mapping';
CREATE INDEX IF NOT EXISTS ix_evidence_mapping_status ON valorapesquisa.evidence_items(organization_id,mapping_status,created_at DESC) WHERE deleted_at IS NULL;

-- Evolui a tabela histórica de notificações sem destruir mensagens existentes.
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS type varchar(60) NOT NULL DEFAULT 'information';
-- Bancos legados podem possuir notifications sem read_at. Garanta a coluna antes do índice parcial de não lidas.
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS read_at timestamptz;
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS message text;
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS related_module varchar(80);
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS related_entity_id uuid;
DO $notification_message_migration$
DECLARE
  has_message boolean;
  has_body boolean;
  has_title boolean;
BEGIN
  SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='notifications' AND column_name='message') INTO has_message;
  SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='notifications' AND column_name='body') INTO has_body;
  SELECT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='notifications' AND column_name='title') INTO has_title;

  IF has_message THEN
    IF has_body THEN
      EXECUTE $sql$
        UPDATE valorapesquisa.notifications
           SET message = body
         WHERE (message IS NULL OR btrim(message) = '')
           AND body IS NOT NULL
           AND btrim(body) <> ''
      $sql$;
      -- Mantém a coluna legada para compatibilidade, mas novas gravações usam somente message.
      EXECUTE 'ALTER TABLE valorapesquisa.notifications ALTER COLUMN body DROP NOT NULL';
    END IF;
    IF has_title THEN
      EXECUTE $sql$
        UPDATE valorapesquisa.notifications
           SET message = title
         WHERE (message IS NULL OR btrim(message) = '')
           AND title IS NOT NULL
           AND btrim(title) <> ''
      $sql$;
    END IF;
    EXECUTE $sql$
      UPDATE valorapesquisa.notifications
         SET message = 'Notificação'
       WHERE message IS NULL OR btrim(message) = ''
    $sql$;
  END IF;
END
$notification_message_migration$;
ALTER TABLE valorapesquisa.notifications ALTER COLUMN message SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_notifications_user_unread ON valorapesquisa.notifications(organization_id,user_id,created_at DESC) WHERE read_at IS NULL;

-- Valora Communication Center(TM). Every table below is deliberately evolved in
-- two phases (CREATE + ADD COLUMN) so this block is safe for both clean and
-- partially migrated installations.
ALTER TABLE valorapesquisa.notifications
  ADD COLUMN IF NOT EXISTS notification_type varchar(60) NOT NULL DEFAULT 'internal',
  ADD COLUMN IF NOT EXISTS severity varchar(20) NOT NULL DEFAULT 'information',
  ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'active',
  ADD COLUMN IF NOT EXISTS source_type varchar(80),
  ADD COLUMN IF NOT EXISTS source_id uuid,
  ADD COLUMN IF NOT EXISTS created_by_user_id uuid REFERENCES valorapesquisa.users(id),
  ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(),
  ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

CREATE TABLE IF NOT EXISTS valorapesquisa.notification_recipients(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), notification_id uuid NOT NULL REFERENCES valorapesquisa.notifications(id),
 organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id), email text,
 status varchar(30) NOT NULL DEFAULT 'pending', read_at timestamptz, delivered_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.notification_templates(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), template_key varchar(120) NOT NULL,
 name text NOT NULL, title_template text NOT NULL, message_template text NOT NULL, allowed_variables jsonb NOT NULL DEFAULT '[]'::jsonb,
 status varchar(30) NOT NULL DEFAULT 'active', version integer NOT NULL DEFAULT 1, created_by_user_id uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.notification_events(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), notification_id uuid NOT NULL REFERENCES valorapesquisa.notifications(id),
 recipient_id uuid REFERENCES valorapesquisa.notification_recipients(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 event_type varchar(40) NOT NULL, actor_user_id uuid REFERENCES valorapesquisa.users(id), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.communication_outbox(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 recipient_user_id uuid REFERENCES valorapesquisa.users(id), recipient_email text, subject text NOT NULL, body_html text, body_text text,
 message_type varchar(60) NOT NULL, status varchar(30) NOT NULL DEFAULT 'pending', provider varchar(60), scheduled_at timestamptz NOT NULL DEFAULT now(),
 sent_at timestamptz, failed_at timestamptz, error_message text, retry_count integer NOT NULL DEFAULT 0,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.communication_delivery_attempts(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), outbox_id uuid NOT NULL REFERENCES valorapesquisa.communication_outbox(id),
 organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), attempt_number integer NOT NULL, provider varchar(60), status varchar(30) NOT NULL,
 provider_message_id text, error_code varchar(100), error_message text, started_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(outbox_id,attempt_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.email_templates(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), template_key varchar(120) NOT NULL,
 name text NOT NULL, subject_template text NOT NULL, body_html_template text, body_text_template text,
 allowed_variables jsonb NOT NULL DEFAULT '[]'::jsonb, current_version integer NOT NULL DEFAULT 1, status varchar(30) NOT NULL DEFAULT 'active',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
ALTER TABLE valorapesquisa.email_templates
  ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id),
  ADD COLUMN IF NOT EXISTS template_key varchar(120),
  ADD COLUMN IF NOT EXISTS name text NOT NULL DEFAULT 'Template de e-mail',
  ADD COLUMN IF NOT EXISTS subject_template text,
  ADD COLUMN IF NOT EXISTS body_html_template text,
  ADD COLUMN IF NOT EXISTS body_text_template text,
  ADD COLUMN IF NOT EXISTS allowed_variables jsonb NOT NULL DEFAULT '[]'::jsonb,
  ADD COLUMN IF NOT EXISTS current_version integer NOT NULL DEFAULT 1,
  ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'active',
  ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(),
  ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE TABLE IF NOT EXISTS valorapesquisa.email_template_versions(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), email_template_id uuid NOT NULL REFERENCES valorapesquisa.email_templates(id), organization_id uuid REFERENCES valorapesquisa.organizations(id),
 version integer NOT NULL, subject_template text NOT NULL, body_html_template text, body_text_template text, allowed_variables jsonb NOT NULL DEFAULT '[]'::jsonb,
 created_by_user_id uuid REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(email_template_id,version));
CREATE TABLE IF NOT EXISTS valorapesquisa.reminder_rules(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), name text NOT NULL,
 reminder_type varchar(60) NOT NULL, delay_minutes integer NOT NULL CHECK(delay_minutes >= 0), template_key varchar(120), is_active boolean NOT NULL DEFAULT true,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_by_user_id uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.reminder_jobs(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), reminder_rule_id uuid NOT NULL REFERENCES valorapesquisa.reminder_rules(id),
 organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), source_type varchar(80) NOT NULL, source_id uuid,
 recipient_user_id uuid REFERENCES valorapesquisa.users(id), recipient_email text, status varchar(30) NOT NULL DEFAULT 'scheduled', scheduled_at timestamptz NOT NULL,
 processed_at timestamptz, outbox_id uuid REFERENCES valorapesquisa.communication_outbox(id), error_message text,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.message_audit_logs(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 message_type varchar(60) NOT NULL, message_id uuid, action varchar(60) NOT NULL, actor_user_id uuid REFERENCES valorapesquisa.users(id),
 recipient_hash text, correlation_id varchar(120), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now());

-- Repair the two operational tables most commonly found incomplete in legacy
-- databases before creating any indexes or issuing runtime queries against them.
ALTER TABLE valorapesquisa.notification_recipients
  ADD COLUMN IF NOT EXISTS notification_id uuid REFERENCES valorapesquisa.notifications(id),
  ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id),
  ADD COLUMN IF NOT EXISTS user_id uuid REFERENCES valorapesquisa.users(id),
  ADD COLUMN IF NOT EXISTS email text,
  ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'pending',
  ADD COLUMN IF NOT EXISTS read_at timestamptz,
  ADD COLUMN IF NOT EXISTS delivered_at timestamptz,
  ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now(),
  ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.communication_outbox
  ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id),
  ADD COLUMN IF NOT EXISTS recipient_user_id uuid REFERENCES valorapesquisa.users(id),
  ADD COLUMN IF NOT EXISTS recipient_email text,
  ADD COLUMN IF NOT EXISTS subject text,
  ADD COLUMN IF NOT EXISTS body_html text,
  ADD COLUMN IF NOT EXISTS body_text text,
  ADD COLUMN IF NOT EXISTS message_type varchar(60) NOT NULL DEFAULT 'email',
  ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'pending',
  ADD COLUMN IF NOT EXISTS provider varchar(60),
  ADD COLUMN IF NOT EXISTS scheduled_at timestamptz NOT NULL DEFAULT now(),
  ADD COLUMN IF NOT EXISTS sent_at timestamptz,
  ADD COLUMN IF NOT EXISTS failed_at timestamptz,
  ADD COLUMN IF NOT EXISTS error_message text,
  ADD COLUMN IF NOT EXISTS retry_count integer NOT NULL DEFAULT 0,
  ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
  ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now(),
  ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(),
  ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

CREATE INDEX IF NOT EXISTS ix_notification_recipients_user ON valorapesquisa.notification_recipients(organization_id,user_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_notification_events_notification ON valorapesquisa.notification_events(notification_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_communication_outbox_due ON valorapesquisa.communication_outbox(status,scheduled_at) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_delivery_attempts_outbox ON valorapesquisa.communication_delivery_attempts(outbox_id,attempt_number DESC);
CREATE INDEX IF NOT EXISTS ix_reminder_jobs_due ON valorapesquisa.reminder_jobs(status,scheduled_at);
CREATE INDEX IF NOT EXISTS ix_message_audit_org ON valorapesquisa.message_audit_logs(organization_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.intelligent_alerts(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), evidence_id uuid NOT NULL REFERENCES valorapesquisa.evidence_items(id),
 severity varchar(20) NOT NULL CHECK(severity IN('critical','high','moderate','informational')), indicator_code varchar(80), capability_code varchar(80),
 systemic_relation text NOT NULL, possible_impact text NOT NULL, priority varchar(20) NOT NULL, cta_module varchar(80) NOT NULL, status varchar(30) NOT NULL DEFAULT 'open',
 suggested_owner_id uuid REFERENCES valorapesquisa.users(id), source_module varchar(80) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_intelligent_alerts_open ON valorapesquisa.intelligent_alerts(organization_id,severity,created_at DESC) WHERE deleted_at IS NULL AND status='open';

-- Templates oficiais recebem mapeamento explícito. Não há inferência por texto da resposta:
-- o vínculo nasce da dimensão metodológica versionada do formulário.
INSERT INTO valorapesquisa.question_concept_mappings(form_id,question_id,concept_code,capability_code,dimension_code,weight,polarity,evidence_type,is_official)
SELECT q.form_id,q.id,
 CASE WHEN lower(d.code) LIKE '%govern%' THEN 'organizational-governance' WHEN lower(d.code) LIKE '%lider%' OR lower(d.code)='leadership' THEN 'leadership'
      WHEN lower(d.code) LIKE '%cultur%' THEN 'organizational-culture' WHEN lower(d.code) LIKE '%people%' OR lower(d.code) LIKE '%pessoa%' THEN 'people'
      ELSE 'organizational-maturity' END,
 d.code,d.code,CASE WHEN q.weight IS NULL OR q.weight<=0 THEN 1.00 ELSE q.weight END,1,CASE WHEN q.is_qualitative THEN 'qualitative_response' ELSE 'quantitative_response' END,true
FROM valorapesquisa.questions q JOIN valorapesquisa.dimensions d ON d.id=q.dimension_id JOIN valorapesquisa.forms f ON f.id=q.form_id
WHERE f.code='valora-official' AND q.deleted_at IS NULL
ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.question_metric_mappings(form_id,question_id,metric_code,weight,is_official)
SELECT q.form_id,q.id,'metric-'||d.code,CASE WHEN q.weight IS NULL OR q.weight<=0 THEN 1.00 ELSE q.weight END,true FROM valorapesquisa.questions q JOIN valorapesquisa.dimensions d ON d.id=q.dimension_id JOIN valorapesquisa.forms f ON f.id=q.form_id
WHERE f.code='valora-official' AND q.deleted_at IS NULL ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.question_index_mappings(form_id,question_id,index_code,weight,is_official)
SELECT q.form_id,q.id,CASE WHEN lower(d.code) LIKE '%govern%' THEN 'IGO' WHEN lower(d.code) LIKE '%lider%' OR lower(d.code)='leadership' THEN 'ILI'
 WHEN lower(d.code) LIKE '%cultur%' THEN 'ICO' WHEN lower(d.code) LIKE '%people%' OR lower(d.code) LIKE '%pessoa%' THEN 'IPO' ELSE 'IMO' END,CASE WHEN q.weight IS NULL OR q.weight<=0 THEN 1.00 ELSE q.weight END,true
FROM valorapesquisa.questions q JOIN valorapesquisa.dimensions d ON d.id=q.dimension_id JOIN valorapesquisa.forms f ON f.id=q.form_id
WHERE f.code='valora-official' AND q.deleted_at IS NULL ON CONFLICT DO NOTHING;

INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('2026_08_intelligence_pipeline','sha256:evidence-mapping-pipeline-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum;
COMMIT;

-- Centro Operacional de Processamento Valora (fila interna idempotente e auditável)
CREATE TABLE IF NOT EXISTS valorapesquisa.intelligence_processing_jobs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, survey_id uuid NULL, response_id uuid NULL,
 form_id uuid NULL, source_entity_id uuid NULL, trigger text NOT NULL, status text NOT NULL DEFAULT 'pending', priority int NOT NULL DEFAULT 5,
 attempts int NOT NULL DEFAULT 0, max_attempts int NOT NULL DEFAULT 3, scheduled_at timestamptz NOT NULL DEFAULT now(), started_at timestamptz NULL,
 completed_at timestamptz NULL, failed_at timestamptz NULL, next_attempt_at timestamptz NULL, locked_at timestamptz NULL, locked_by text NULL,
 error_code text NULL, error_message text NULL, correlation_id text NULL, run_id uuid NULL, idempotency_key text NOT NULL,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz NULL);
ALTER TABLE valorapesquisa.intelligence_processing_jobs ADD COLUMN IF NOT EXISTS run_id uuid NULL;
ALTER TABLE valorapesquisa.intelligence_processing_jobs ADD COLUMN IF NOT EXISTS idempotency_key text;
CREATE UNIQUE INDEX IF NOT EXISTS ux_intelligence_processing_active_key ON valorapesquisa.intelligence_processing_jobs(organization_id,idempotency_key) WHERE deleted_at IS NULL AND status IN ('pending','running','retry_scheduled');
CREATE INDEX IF NOT EXISTS ix_intelligence_processing_jobs_org ON valorapesquisa.intelligence_processing_jobs(organization_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_intelligence_processing_jobs_status ON valorapesquisa.intelligence_processing_jobs(status,next_attempt_at,locked_at);
CREATE INDEX IF NOT EXISTS ix_intelligence_processing_jobs_trigger ON valorapesquisa.intelligence_processing_jobs(trigger);
CREATE INDEX IF NOT EXISTS ix_intelligence_processing_jobs_survey ON valorapesquisa.intelligence_processing_jobs(survey_id) WHERE survey_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_intelligence_processing_jobs_response ON valorapesquisa.intelligence_processing_jobs(response_id) WHERE response_id IS NOT NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.intelligence_pipeline_runs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), job_id uuid NOT NULL REFERENCES valorapesquisa.intelligence_processing_jobs(id), organization_id uuid NOT NULL,
 trigger text NOT NULL, status text NOT NULL, correlation_id text NULL, started_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz NULL,
 error_code text NULL,error_message text NULL,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_intelligence_pipeline_runs_job ON valorapesquisa.intelligence_pipeline_runs(job_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_intelligence_pipeline_runs_org ON valorapesquisa.intelligence_pipeline_runs(organization_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.intelligence_pipeline_stage_runs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),job_id uuid NOT NULL REFERENCES valorapesquisa.intelligence_processing_jobs(id),run_id uuid NOT NULL,
 organization_id uuid NOT NULL,stage text NOT NULL,status text NOT NULL,records int NOT NULL DEFAULT 0,sufficient_evidence boolean NOT NULL DEFAULT false,
 message text NOT NULL,started_at timestamptz NOT NULL,completed_at timestamptz NULL,duration_ms bigint NULL,error_code text NULL,error_message text NULL,
 evidence_ids jsonb NOT NULL DEFAULT '[]'::jsonb,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_intelligence_stage_runs_job ON valorapesquisa.intelligence_pipeline_stage_runs(job_id,created_at);
CREATE INDEX IF NOT EXISTS ix_intelligence_stage_runs_run ON valorapesquisa.intelligence_pipeline_stage_runs(run_id,created_at);
CREATE INDEX IF NOT EXISTS ix_intelligence_stage_runs_status ON valorapesquisa.intelligence_pipeline_stage_runs(organization_id,status,stage);

CREATE TABLE IF NOT EXISTS valorapesquisa.intelligence_processing_failures (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL,job_id uuid NOT NULL,run_id uuid NULL,stage text NULL,error_code text NOT NULL,
 error_message text NOT NULL,correlation_id text NULL,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),resolved_at timestamptz NULL);
CREATE INDEX IF NOT EXISTS ix_intelligence_failures_job ON valorapesquisa.intelligence_processing_failures(job_id,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.intelligence_reprocess_requests (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL,job_id uuid NULL,requested_by uuid NULL,request_type text NOT NULL,status text NOT NULL,
 correlation_id text NULL,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_intelligence_reprocess_org ON valorapesquisa.intelligence_reprocess_requests(organization_id,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.module_refresh_queue (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL,job_id uuid NOT NULL,run_id uuid NULL,module text NOT NULL,status text NOT NULL DEFAULT 'pending',
 idempotency_key text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz NULL);
CREATE UNIQUE INDEX IF NOT EXISTS ux_module_refresh_key ON valorapesquisa.module_refresh_queue(organization_id,idempotency_key) WHERE deleted_at IS NULL;

-- Proveniência e versionamento preservam snapshots antigos sem remover dados reais.
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS source_hash text;
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS job_id uuid;
ALTER TABLE valorapesquisa.evidence_items ADD COLUMN IF NOT EXISTS run_id uuid;
ALTER TABLE valorapesquisa.metric_values ADD COLUMN IF NOT EXISTS source_hash text;
ALTER TABLE valorapesquisa.metric_values ADD COLUMN IF NOT EXISTS job_id uuid;
ALTER TABLE valorapesquisa.metric_values ADD COLUMN IF NOT EXISTS run_id uuid;
ALTER TABLE valorapesquisa.index_values ADD COLUMN IF NOT EXISTS source_hash text;
ALTER TABLE valorapesquisa.index_values ADD COLUMN IF NOT EXISTS job_id uuid;
ALTER TABLE valorapesquisa.index_values ADD COLUMN IF NOT EXISTS run_id uuid;
ALTER TABLE valorapesquisa.insights ADD COLUMN IF NOT EXISTS idempotency_key text;
ALTER TABLE valorapesquisa.insights ADD COLUMN IF NOT EXISTS job_id uuid;
ALTER TABLE valorapesquisa.insights ADD COLUMN IF NOT EXISTS run_id uuid;
ALTER TABLE valorapesquisa.insights ADD COLUMN IF NOT EXISTS is_current boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.insights ADD COLUMN IF NOT EXISTS superseded_at timestamptz;

-- Workspace Executivo por Ciclo Diagnóstico (aditivo, idempotente e não destrutivo)
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_cycles (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), form_id uuid REFERENCES valorapesquisa.forms(id), title text NOT NULL,
 description text, cycle_number int NOT NULL DEFAULT 1, methodology_version varchar(40) NOT NULL DEFAULT '1', status varchar(40) NOT NULL DEFAULT 'draft',
 opened_at timestamptz, published_at timestamptz, closed_at timestamptz, processed_at timestamptz, report_generated_at timestamptz,
 response_count int NOT NULL DEFAULT 0, evidence_count int NOT NULL DEFAULT 0, confidence_level numeric(6,2), processing_status varchar(40) NOT NULL DEFAULT 'pending',
 created_by uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE UNIQUE INDEX IF NOT EXISTS ux_diagnostic_cycles_survey ON valorapesquisa.diagnostic_cycles(organization_id,survey_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_diagnostic_cycles_org_status ON valorapesquisa.diagnostic_cycles(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;

DO $workspace$
DECLARE t text;
BEGIN
 FOREACH t IN ARRAY ARRAY['evidence_items','metric_values','index_values','inference_results','insights','valora_actions','evolution_cycles','journey_events','heatmap_snapshots','radar_snapshots','benchmark_runs','executive_reports','platform_governance_events','notifications','intelligent_alerts'] LOOP
  IF to_regclass('valorapesquisa.'||t) IS NOT NULL THEN
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS survey_id uuid',t);
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS cycle_id uuid',t);
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS source_hash text',t);
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS idempotency_key text',t);
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS job_id uuid',t);
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS run_id uuid',t);
   EXECUTE format('ALTER TABLE valorapesquisa.%I ADD COLUMN IF NOT EXISTS methodology_version varchar(40) NOT NULL DEFAULT ''1''',t);
   EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON valorapesquisa.%I(organization_id,survey_id) WHERE survey_id IS NOT NULL','ix_'||t||'_workspace_survey',t);
   EXECUTE format('CREATE INDEX IF NOT EXISTS %I ON valorapesquisa.%I(organization_id,cycle_id) WHERE cycle_id IS NOT NULL','ix_'||t||'_workspace_cycle',t);
  END IF;
 END LOOP;
END $workspace$;

-- Base organizacional Valora Insight (aditiva, multiempresa e historicamente versionada)
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_structure_nodes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 parent_id uuid REFERENCES valorapesquisa.organization_structure_nodes(id), type varchar(40) NOT NULL,
 code varchar(80), name varchar(180) NOT NULL, description text, leader_user_id uuid, executive_sponsor_user_id uuid,
 status varchar(30) NOT NULL DEFAULT 'active', display_order int NOT NULL DEFAULT 0, version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_structure_node_name ON valorapesquisa.organization_structure_nodes(organization_id,parent_id,type,lower(name)) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_structure_nodes_tree ON valorapesquisa.organization_structure_nodes(organization_id,parent_id,display_order) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_structure_edges (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 parent_node_id uuid NOT NULL REFERENCES valorapesquisa.organization_structure_nodes(id), child_node_id uuid NOT NULL REFERENCES valorapesquisa.organization_structure_nodes(id),
 relation_type varchar(40) NOT NULL DEFAULT 'reports_to', status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_structure_edges_active ON valorapesquisa.organization_structure_edges(organization_id,parent_node_id,child_node_id,relation_type) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_structure_snapshots (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), cycle_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_cycles(id),
 version int NOT NULL DEFAULT 1, status varchar(30) NOT NULL DEFAULT 'active', methodology_version varchar(40) NOT NULL DEFAULT '1',
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_structure_snapshot_cycle ON valorapesquisa.organization_structure_snapshots(organization_id,cycle_id,version) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_structure_snapshot_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), snapshot_id uuid NOT NULL REFERENCES valorapesquisa.organization_structure_snapshots(id),
 source_node_id uuid NOT NULL, parent_source_node_id uuid, type varchar(40) NOT NULL, code varchar(80), name varchar(180) NOT NULL,
 leader_user_id uuid, executive_sponsor_user_id uuid, status varchar(30) NOT NULL, version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_structure_snapshot_items ON valorapesquisa.organization_structure_snapshot_items(organization_id,snapshot_id,type);

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_positions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), node_id uuid REFERENCES valorapesquisa.organization_structure_nodes(id),
 code varchar(80), name varchar(180) NOT NULL, hierarchical_level varchar(80), status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_leadership_assignments (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), node_id uuid NOT NULL REFERENCES valorapesquisa.organization_structure_nodes(id),
 user_id uuid NOT NULL, position_id uuid REFERENCES valorapesquisa.organization_positions(id), assignment_type varchar(40) NOT NULL DEFAULT 'leader',
 starts_at timestamptz NOT NULL DEFAULT now(), ends_at timestamptz, status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_audiences (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), cycle_id uuid REFERENCES valorapesquisa.diagnostic_cycles(id),
 name varchar(180) NOT NULL, description text, minimum_aggregation_size int NOT NULL DEFAULT 5 CHECK (minimum_aggregation_size >= 3),
 anonymity_mode varchar(30) NOT NULL DEFAULT 'anonymous', opening_message text, lgpd_term text, communication_consent_enabled boolean NOT NULL DEFAULT false,
 status varchar(30) NOT NULL DEFAULT 'draft', version int NOT NULL DEFAULT 1, methodology_version varchar(40) NOT NULL DEFAULT '1',
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_diagnostic_audiences_org ON valorapesquisa.diagnostic_audiences(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_audience_segments (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), audience_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_audiences(id),
 segment_type varchar(60) NOT NULL, structure_node_id uuid REFERENCES valorapesquisa.organization_structure_nodes(id), value text, required boolean NOT NULL DEFAULT false,
 status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_participant_fields (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), audience_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_audiences(id),
 field_key varchar(80) NOT NULL, label varchar(180) NOT NULL, field_type varchar(40) NOT NULL, required boolean NOT NULL DEFAULT false,
 is_anonymous boolean NOT NULL DEFAULT true, is_sensitive boolean NOT NULL DEFAULT false, justification text, display_order int NOT NULL DEFAULT 0,
 status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_participant_segment_values (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), cycle_id uuid REFERENCES valorapesquisa.diagnostic_cycles(id),
 response_id uuid, audience_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_audiences(id), segment_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_audience_segments(id),
 value_hash text NOT NULL, structure_snapshot_item_id uuid REFERENCES valorapesquisa.organization_structure_snapshot_items(id), status varchar(30) NOT NULL DEFAULT 'active',
 version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_participant_segments_privacy ON valorapesquisa.diagnostic_participant_segment_values(organization_id,cycle_id,segment_id);

CREATE TABLE IF NOT EXISTS valorapesquisa.form_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), origin_template_id uuid REFERENCES valorapesquisa.form_templates(id),
 name varchar(180) NOT NULL, description text, objective text NOT NULL, recommended_audience text, estimated_minutes int,
 concepts_json jsonb NOT NULL DEFAULT '[]'::jsonb, indices_json jsonb NOT NULL DEFAULT '[]'::jsonb, metrics_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 coverage_score numeric(5,2) NOT NULL DEFAULT 0, is_official boolean NOT NULL DEFAULT false, version int NOT NULL DEFAULT 1,
 methodology_version varchar(40) NOT NULL DEFAULT '1', status varchar(30) NOT NULL DEFAULT 'draft', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_form_templates_catalog ON valorapesquisa.form_templates(is_official,organization_id,status,name) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.form_template_sections (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.form_templates(id), name varchar(180) NOT NULL,
 description text, display_order int NOT NULL DEFAULT 0, weight numeric(8,4) NOT NULL DEFAULT 1, status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.form_template_questions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.form_templates(id), section_id uuid REFERENCES valorapesquisa.form_template_sections(id),
 text text NOT NULL, question_type varchar(40) NOT NULL DEFAULT 'scale', scale_min int, scale_max int, weight numeric(8,4) NOT NULL DEFAULT 1,
 polarity varchar(20) NOT NULL DEFAULT 'positive', display_order int NOT NULL DEFAULT 0, status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.form_template_mappings (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.form_templates(id), question_id uuid NOT NULL REFERENCES valorapesquisa.form_template_questions(id),
 target_type varchar(40) NOT NULL, target_code varchar(100) NOT NULL, weight numeric(8,4) NOT NULL DEFAULT 1, status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1,
 methodology_version varchar(40) NOT NULL DEFAULT '1', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.form_template_versions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.form_templates(id), version int NOT NULL,
 snapshot_json jsonb NOT NULL, methodology_version varchar(40) NOT NULL, status varchar(30) NOT NULL DEFAULT 'published', correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(template_id,version));

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_onboarding_state (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), hidden boolean NOT NULL DEFAULT false,
 completed_at timestamptz, progress_percent int NOT NULL DEFAULT 0 CHECK(progress_percent BETWEEN 0 AND 100), status varchar(30) NOT NULL DEFAULT 'active',
 version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_onboarding_state_org ON valorapesquisa.organization_onboarding_state(organization_id) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_onboarding_steps (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), step_key varchar(80) NOT NULL,
 completed_at timestamptz, evidence_entity_type varchar(80), evidence_entity_id uuid, status varchar(30) NOT NULL DEFAULT 'pending', display_order int NOT NULL,
 version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_onboarding_step_org ON valorapesquisa.organization_onboarding_steps(organization_id,step_key) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.plan_features (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL, feature_key varchar(100) NOT NULL, enabled boolean NOT NULL DEFAULT false,
 status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_features ON valorapesquisa.plan_features(plan_id,feature_key) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_limits (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL, limit_key varchar(100) NOT NULL, limit_value bigint NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'active', version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_limits ON valorapesquisa.plan_limits(plan_id,limit_key) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.usage_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), subscription_id uuid,
 feature_key varchar(100) NOT NULL, quantity bigint NOT NULL DEFAULT 1, allowed boolean NOT NULL DEFAULT true, reason text, correlation_id text,
 status varchar(30) NOT NULL DEFAULT 'recorded', version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_usage_events_org ON valorapesquisa.usage_events(organization_id,feature_key,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), subscription_id uuid,
 action varchar(80) NOT NULL, before_json jsonb, after_json jsonb, reason text, correlation_id text, status varchar(30) NOT NULL DEFAULT 'recorded',
 version int NOT NULL DEFAULT 1, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_subscription_events_org ON valorapesquisa.subscription_events(organization_id,created_at DESC);

ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS template_id uuid REFERENCES valorapesquisa.form_templates(id);
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS audience_id uuid REFERENCES valorapesquisa.diagnostic_audiences(id);
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS structure_snapshot_id uuid REFERENCES valorapesquisa.organization_structure_snapshots(id);
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS template_snapshot_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS collection_starts_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS collection_ends_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS correlation_id text;

ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS legal_name varchar(180);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS cnpj varchar(14);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS segment varchar(120);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS cnae varchar(16);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS company_size varchar(40);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS approximate_employee_count int;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS leadership_count int;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS business_model varchar(40);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS region varchar(80);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS city varchar(120);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS state varchar(2);

-- Valora Insight™ — template oficial de maturidade estratégica v1.0 (seed não destrutivo).
ALTER TABLE valorapesquisa.form_templates ADD COLUMN IF NOT EXISTS code varchar(100);
ALTER TABLE valorapesquisa.form_templates ADD COLUMN IF NOT EXISTS slug varchar(180);
ALTER TABLE valorapesquisa.form_template_sections ADD COLUMN IF NOT EXISTS code varchar(100);
ALTER TABLE valorapesquisa.form_template_questions ADD COLUMN IF NOT EXISTS code varchar(100);
ALTER TABLE valorapesquisa.form_template_questions ADD COLUMN IF NOT EXISTS is_required boolean NOT NULL DEFAULT true;
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_templates_code ON valorapesquisa.form_templates(code) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_templates_slug ON valorapesquisa.form_templates(slug) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_template_sections_code ON valorapesquisa.form_template_sections(template_id,code) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_template_questions_code ON valorapesquisa.form_template_questions(template_id,code) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.form_template_scale_options (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.form_templates(id), value int NOT NULL CHECK(value BETWEEN 1 AND 5), label text NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(template_id,value));
CREATE TABLE IF NOT EXISTS valorapesquisa.form_template_rules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.form_templates(id), rule_type varchar(40) NOT NULL, code varchar(100) NOT NULL,
 configuration_json jsonb NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(template_id,rule_type,code));

INSERT INTO valorapesquisa.form_templates(code,slug,name,description,objective,recommended_audience,estimated_minutes,concepts_json,indices_json,metrics_json,coverage_score,is_official,version,methodology_version,status,metadata_json)
VALUES ('VALORA_STRATEGIC_MATURITY_V1','diagnostico-estrategico-maturidade-organizacional-v1','Diagnóstico Estratégico de Maturidade Organizacional',
'Descubra o que fortalece — e o que limita — o crescimento sustentável da sua organização.','Diagnosticar a maturidade organizacional de forma autoaplicada, gratuita e com resultado instantâneo.',
'Qualquer empresa, setor e porte',20,'["Cultura Organizacional","Governança Organizacional","Liderança","Pessoas","Resultados Organizacionais","Sustentabilidade Organizacional"]',
'["maturidade_organizacional"]','["score_principal","media_por_dimensao","score_esg"]',100,true,1,'1.0','published',
'{"publicName":"Valora Insight™","year":2026,"type":"strategic_maturity_autoapplied","format":"Autoaplicado","cost":"Gratuito","estimatedTime":"15-20 minutos","result":"Instantâneo","allowPublicResult":true,"certificateEnabled":true,"executiveReportEnabled":true,"certification":"Autoaplicado, não certifica formalmente.","methodology":"Diagnóstico de maturidade organizacional alinhado a modelos como EFQM e Kanban Maturity Model.","scope":"Aplicável a qualquer empresa, qualquer setor e qualquer porte.","repeat":"1 vez por ano para monitorar evolução.","brand":{"name":"Valora Group","email":"e-mail@valoragroup.com","website":"www.valoragroup.com","linkedin":"https://linkedin.com/company/america-cultura-empresarial"}}')
ON CONFLICT (code) WHERE deleted_at IS NULL DO UPDATE SET slug=EXCLUDED.slug,name=EXCLUDED.name,description=EXCLUDED.description,objective=EXCLUDED.objective,recommended_audience=EXCLUDED.recommended_audience,estimated_minutes=EXCLUDED.estimated_minutes,concepts_json=EXCLUDED.concepts_json,indices_json=EXCLUDED.indices_json,metrics_json=EXCLUDED.metrics_json,coverage_score=EXCLUDED.coverage_score,is_official=true,version=1,methodology_version='1.0',status='published',metadata_json=EXCLUDED.metadata_json,updated_at=now();

WITH t AS (SELECT id FROM valorapesquisa.form_templates WHERE code='VALORA_STRATEGIC_MATURITY_V1' AND deleted_at IS NULL), data(code,name,description,display_order) AS (VALUES
('culture_purpose','Cultura e Propósito','Propósito, valores, alinhamento e cultura.',1),('management_governance','Gestão e Governança','Papéis, decisões, indicadores e estabilidade.',2),
('leadership','Liderança','Direção, alinhamento, desenvolvimento e confiança.',3),('people_talents','Pessoas e Talentos','Atração, integração, desenvolvimento e retenção.',4),
('results_growth','Resultados e Crescimento','Resultados, produtividade e crescimento sustentável.',5),('sustainability_esg','Sustentabilidade e ESG','Bloco opcional, calculado separadamente.',6))
INSERT INTO valorapesquisa.form_template_sections(template_id,code,name,description,display_order,weight,status,version)
SELECT t.id,d.code,d.name,d.description,d.display_order,1,'active',1 FROM t CROSS JOIN data d
ON CONFLICT (template_id,code) WHERE deleted_at IS NULL DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,display_order=EXCLUDED.display_order,status='active',updated_at=now();

WITH t AS (SELECT id FROM valorapesquisa.form_templates WHERE code='VALORA_STRATEGIC_MATURITY_V1' AND deleted_at IS NULL), data(n,dimension,text,required) AS (VALUES
(1,'culture_purpose','As pessoas compreendem claramente o propósito e os valores da empresa.',true),(2,'culture_purpose','Existe alinhamento entre o que a liderança comunica e o que é praticado no dia a dia.',true),(3,'culture_purpose','Os colaboradores entendem como seu trabalho contribui para os resultados do negócio.',true),(4,'culture_purpose','A cultura da empresa favorece colaboração, responsabilidade e comprometimento.',true),(5,'culture_purpose','As decisões da empresa refletem seus valores e direcionamento estratégico.',true),
(6,'management_governance','Papéis e responsabilidades estão claramente definidos.',true),(7,'management_governance','As decisões importantes seguem critérios e processos bem estabelecidos.',true),(8,'management_governance','A empresa acompanha regularmente indicadores relevantes para o negócio.',true),(9,'management_governance','Os gestores possuem informações confiáveis para tomar decisões.',true),(10,'management_governance','A operação funciona com estabilidade sem depender excessivamente de poucas pessoas.',true),
(11,'leadership','Os líderes dão direção clara às equipes.',true),(12,'leadership','As lideranças atuam de forma alinhada entre si.',true),(13,'leadership','Os líderes desenvolvem pessoas e fortalecem talentos.',true),(14,'leadership','Os conflitos são tratados de forma construtiva e madura.',true),(15,'leadership','As lideranças inspiram confiança e engajamento.',true),
(16,'people_talents','A empresa atrai profissionais alinhados à sua cultura e objetivos.',true),(17,'people_talents','Novos colaboradores são integrados de forma estruturada.',true),(18,'people_talents','Existem oportunidades claras de desenvolvimento e crescimento profissional.',true),(19,'people_talents','Os talentos estratégicos tendem a permanecer na organização.',true),(20,'people_talents','O desempenho das pessoas é acompanhado e desenvolvido regularmente.',true),
(21,'results_growth','A empresa atinge suas metas com consistência.',true),(22,'results_growth','Existe equilíbrio entre crescimento, organização e capacidade de execução.',true),(23,'results_growth','Os processos favorecem produtividade e eficiência.',true),(24,'results_growth','Problemas recorrentes são tratados na causa, e não apenas nos sintomas.',true),(25,'results_growth','A empresa está preparada para sustentar o crescimento nos próximos anos.',true),
(26,'sustainability_esg','A empresa tem práticas ambientais, como redução de desperdício e energia eficiente.',false),(27,'sustainability_esg','Existe diversidade e inclusão em processos de contratação e desenvolvimento.',false))
INSERT INTO valorapesquisa.form_template_questions(template_id,section_id,code,text,question_type,scale_min,scale_max,weight,polarity,display_order,is_required,status,version,metadata_json)
SELECT t.id,s.id,'VALORA_MATURITY_Q'||lpad(d.n::text,2,'0'),d.text,'scale',1,5,1,'positive',d.n,d.required,'active',1,jsonb_build_object('dimensionCode',d.dimension,'maxScore',5,'mainScore',d.n<=25)
FROM t CROSS JOIN data d JOIN valorapesquisa.form_template_sections s ON s.template_id=t.id AND s.code=d.dimension AND s.deleted_at IS NULL
ON CONFLICT (template_id,code) WHERE deleted_at IS NULL DO UPDATE SET section_id=EXCLUDED.section_id,text=EXCLUDED.text,question_type='scale',scale_min=1,scale_max=5,weight=1,display_order=EXCLUDED.display_order,is_required=EXCLUDED.is_required,status='active',metadata_json=EXCLUDED.metadata_json,updated_at=now();

WITH t AS (SELECT id FROM valorapesquisa.form_templates WHERE code='VALORA_STRATEGIC_MATURITY_V1' AND deleted_at IS NULL), data(value,label) AS (VALUES (1,'Discordo totalmente'),(2,'Discordo parcialmente'),(3,'Nem concordo nem discordo'),(4,'Concordo parcialmente'),(5,'Concordo totalmente'))
INSERT INTO valorapesquisa.form_template_scale_options(template_id,value,label) SELECT t.id,d.value,d.label FROM t CROSS JOIN data d
ON CONFLICT(template_id,value) DO UPDATE SET label=EXCLUDED.label,updated_at=now();

WITH t AS (SELECT id FROM valorapesquisa.form_templates WHERE code='VALORA_STRATEGIC_MATURITY_V1' AND deleted_at IS NULL), data(rule_type,code,configuration_json) AS (VALUES
('scoring','main','{"questionStart":1,"questionEnd":25,"min":25,"max":125,"esgIncluded":false}'::jsonb),('scoring','esg','{"questions":[26,27],"required":false,"max":10,"separate":true}'::jsonb),
('level','attention','{"min":25,"max":55,"label":"🔴 Atenção","meaning":"Fragilidades críticas","priority":"Estabilidade operacional + governança básica."}'::jsonb),('level','evolution','{"min":56,"max":85,"label":"🟡 Evolução","meaning":"Fundamentos presentes, oportunidades de fortalecimento","priority":"Consistência procedural + fortalecimento de liderança."}'::jsonb),('level','consistency','{"min":86,"max":110,"label":"🟢 Consistência","meaning":"Sistemas estruturados, operação segura","priority":"Inovação + diferenciação competitiva."}'::jsonb),('level','excellence','{"min":111,"max":125,"label":"🔵 Excelência","meaning":"Maturidade elevada, geração sustentável de valor","priority":"Manutenção + liderança de mercado."}'::jsonb),
('public_section','benchmark','{"available":false,"message":"Comparativo setorial ainda não informado para este diagnóstico."}'::jsonb),('certificate','preview','{"enabled":true,"formal":false,"pdfMessage":"Geração de PDF ainda não configurada neste ambiente. O preview do certificado está disponível."}'::jsonb),('executive_report','preview','{"enabled":true,"pdfMessage":"Exportação PDF ainda não configurada neste ambiente. O preview executivo está disponível."}'::jsonb))
INSERT INTO valorapesquisa.form_template_rules(template_id,rule_type,code,configuration_json) SELECT t.id,d.rule_type,d.code,d.configuration_json FROM t CROSS JOIN data d
ON CONFLICT(template_id,rule_type,code) DO UPDATE SET configuration_json=EXCLUDED.configuration_json,updated_at=now();

WITH t AS (SELECT id FROM valorapesquisa.form_templates WHERE code='VALORA_STRATEGIC_MATURITY_V1' AND deleted_at IS NULL)
INSERT INTO valorapesquisa.form_template_versions(template_id,version,snapshot_json,methodology_version,status)
SELECT t.id,1,jsonb_build_object('templateCode','VALORA_STRATEGIC_MATURITY_V1','version','1.0','year',2026,'sections',6,'questions',27,'requiredQuestions',25,'optionalEsgQuestions',2),'1.0','published' FROM t
ON CONFLICT(template_id,version) DO UPDATE SET snapshot_json=EXCLUDED.snapshot_json,methodology_version='1.0',status='published',updated_at=now();
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS primary_contact_name varchar(180);
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS minimum_aggregation_size int NOT NULL DEFAULT 5;
ALTER TABLE valorapesquisa.organizations ADD COLUMN IF NOT EXISTS diagnostic_cycle_settings_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE UNIQUE INDEX IF NOT EXISTS ux_organizations_cnpj ON valorapesquisa.organizations(cnpj) WHERE cnpj IS NOT NULL AND deleted_at IS NULL;

ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS user_id uuid;
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS module varchar(80);
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS entity_type varchar(80);
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS entity_id uuid;
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS action varchar(100);
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS before_json jsonb;
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS after_json jsonb;
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS reason text;
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS correlation_id text;
CREATE INDEX IF NOT EXISTS ix_platform_governance_entity ON valorapesquisa.platform_governance_events(organization_id,entity_type,entity_id,created_at DESC) WHERE deleted_at IS NULL;

-- Ciclo operacional de coleta e devolutiva. As estruturas abaixo complementam
-- as tabelas canônicas existentes (email_jobs, certificate_validations e
-- executive_reports) sem criar conceitos concorrentes.
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_campaigns (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), cycle_id uuid REFERENCES valorapesquisa.diagnostic_cycles(id),
 name varchar(180) NOT NULL, audience_json jsonb NOT NULL DEFAULT '{}'::jsonb, sender_name varchar(180), reply_to_hash text,
 public_url text, status varchar(30) NOT NULL DEFAULT 'draft', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_diagnostic_campaigns_cycle ON valorapesquisa.diagnostic_campaigns(organization_id,cycle_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_campaign_messages (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 campaign_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_campaigns(id), channel varchar(30) NOT NULL DEFAULT 'email',
 subject text, body text NOT NULL, status varchar(30) NOT NULL DEFAULT 'draft', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_campaign_messages_campaign ON valorapesquisa.diagnostic_campaign_messages(campaign_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_campaign_recipients (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 campaign_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_campaigns(id), email_hash text, recipient_reference text,
 status varchar(30) NOT NULL DEFAULT 'pending', sent_at timestamptz, failed_at timestamptz, cancelled_at timestamptz,
 error_code varchar(80), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_campaign_recipients_status ON valorapesquisa.diagnostic_campaign_recipients(campaign_id,status,created_at) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_participation_snapshots (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), cycle_id uuid REFERENCES valorapesquisa.diagnostic_cycles(id),
 response_count int NOT NULL DEFAULT 0, target_count int, participation_rate numeric(7,4), minimum_sample_size int NOT NULL DEFAULT 5,
 segments_json jsonb NOT NULL DEFAULT '{}'::jsonb, status varchar(30) NOT NULL DEFAULT 'current', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_participation_snapshots_cycle ON valorapesquisa.diagnostic_participation_snapshots(organization_id,cycle_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.public_survey_tokens (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), cycle_id uuid REFERENCES valorapesquisa.diagnostic_cycles(id), token_hash text NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'active', expires_at timestamptz, last_used_at timestamptz, use_count int NOT NULL DEFAULT 0,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_survey_tokens_hash ON valorapesquisa.public_survey_tokens(token_hash) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.response_processing_status (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 response_id uuid NOT NULL REFERENCES valorapesquisa.responses(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id),
 status varchar(30) NOT NULL DEFAULT 'pending', attempt_count int NOT NULL DEFAULT 0, last_error_code varchar(80),
 queued_at timestamptz NOT NULL DEFAULT now(), started_at timestamptz, completed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_response_processing_active ON valorapesquisa.response_processing_status(response_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.result_access_tokens (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 response_id uuid NOT NULL REFERENCES valorapesquisa.responses(id), token_hash text NOT NULL, status varchar(30) NOT NULL DEFAULT 'active',
 expires_at timestamptz, last_accessed_at timestamptz, access_count int NOT NULL DEFAULT 0, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_result_access_tokens_hash ON valorapesquisa.result_access_tokens(token_hash) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.certificate_files (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 certificate_id uuid NOT NULL REFERENCES valorapesquisa.certificates(id), storage_key text, content_type varchar(100), content_hash text,
 status varchar(30) NOT NULL DEFAULT 'metadata-ready', generated_at timestamptz, expires_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_certificate_files_certificate ON valorapesquisa.certificate_files(organization_id,certificate_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.executive_report_snapshots (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), cycle_id uuid REFERENCES valorapesquisa.diagnostic_cycles(id),
 version int NOT NULL, report_json jsonb NOT NULL, limitations_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 status varchar(30) NOT NULL DEFAULT 'preview', generated_by uuid REFERENCES valorapesquisa.users(id),
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE (organization_id, cycle_id, version));
CREATE INDEX IF NOT EXISTS ix_executive_report_snapshots_cycle ON valorapesquisa.executive_report_snapshots(organization_id,cycle_id,version DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.executive_report_share_links (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 snapshot_id uuid NOT NULL REFERENCES valorapesquisa.executive_report_snapshots(id), token_hash text NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'active', expires_at timestamptz, revoked_at timestamptz, last_accessed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_executive_report_share_token ON valorapesquisa.executive_report_share_links(token_hash) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.email_delivery_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 email_job_id uuid NOT NULL REFERENCES valorapesquisa.email_jobs(id), event_type varchar(40) NOT NULL, status varchar(30) NOT NULL,
 provider_reference text, error_code varchar(80), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_email_delivery_events_job ON valorapesquisa.email_delivery_events(email_job_id,created_at DESC) WHERE deleted_at IS NULL;

-- Entrega comercial: complementos aditivos e idempotentes. Mantêm as tabelas
-- canônicas e permitem executar novamente este script sem substituir dados.
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS channel varchar(30) NOT NULL DEFAULT 'manual';
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS message_subject text;
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS message_body text;
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS scheduled_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS sent_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS cancelled_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_campaigns ADD COLUMN IF NOT EXISTS created_by uuid;
ALTER TABLE valorapesquisa.diagnostic_campaign_recipients ADD COLUMN IF NOT EXISTS recipient_hash text;
ALTER TABLE valorapesquisa.diagnostic_campaign_recipients ADD COLUMN IF NOT EXISTS recipient_masked text;
ALTER TABLE valorapesquisa.diagnostic_campaign_recipients ADD COLUMN IF NOT EXISTS queued_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_campaign_recipients ADD COLUMN IF NOT EXISTS responded_at timestamptz;
ALTER TABLE valorapesquisa.diagnostic_campaign_recipients ADD COLUMN IF NOT EXISTS error_message text;

CREATE TABLE IF NOT EXISTS valorapesquisa.communication_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id),
 code varchar(100) NOT NULL, name varchar(180) NOT NULL, subject text, body_html text, body_text text NOT NULL,
 variables_json jsonb NOT NULL DEFAULT '[]'::jsonb, is_system boolean NOT NULL DEFAULT false,
 status varchar(30) NOT NULL DEFAULT 'active', origin_template_id uuid REFERENCES valorapesquisa.communication_templates(id),
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_communication_templates_scope_code ON valorapesquisa.communication_templates(COALESCE(organization_id,'00000000-0000-0000-0000-000000000000'::uuid),code) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.public_result_access_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 result_token_id uuid REFERENCES valorapesquisa.result_access_tokens(id), event_type varchar(40) NOT NULL,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_public_result_access_events_token ON valorapesquisa.public_result_access_events(result_token_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.share_links (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 entity_type varchar(50) NOT NULL, entity_id uuid NOT NULL, token_hash text NOT NULL, scope varchar(80) NOT NULL DEFAULT 'read',
 status varchar(30) NOT NULL DEFAULT 'active', expires_at timestamptz, revoked_at timestamptz, created_by uuid,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_share_links_token_hash ON valorapesquisa.share_links(token_hash) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_share_links_entity ON valorapesquisa.share_links(organization_id,entity_type,entity_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.share_link_access_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 share_link_id uuid NOT NULL REFERENCES valorapesquisa.share_links(id), outcome varchar(30) NOT NULL,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_share_link_access_events_link ON valorapesquisa.share_link_access_events(share_link_id,created_at DESC);

-- Execuções de IA: entrada minimizada, saída, consumo e revisão humana permanecem
-- rastreáveis e sempre isolados pela organização da execução.
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_runs (
 id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 diagnosis_id uuid NOT NULL, prompt_code varchar(100) NOT NULL, prompt_version int NOT NULL,
 provider varchar(100) NOT NULL, model varchar(100) NOT NULL, status varchar(40) NOT NULL,
 correlation_id text NOT NULL, input_json jsonb NOT NULL, output_json jsonb, validation_json jsonb,
 input_tokens int, output_tokens int, estimated_cost numeric(14,6), error text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_valora_ai_runs_org_created ON valorapesquisa.valora_ai_runs(organization_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_valora_ai_runs_diagnosis ON valorapesquisa.valora_ai_runs(organization_id,diagnosis_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_reviews (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_runs(id),
 reviewer_id uuid NOT NULL, status varchar(40) NOT NULL, note text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valora_ai_reviews_run ON valorapesquisa.valora_ai_reviews(run_id,created_at DESC);

-- Intelligence Hub: trilha completa da evidência até a revisão humana. As FKs de
-- diagnóstico/resultado/metodologia permanecem lógicas para suportar upgrades de
-- bancos parciais sem impedir a instalação; o isolamento é sempre organization_id.
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS result_id uuid;
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS methodology_version_id uuid;
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS run_type varchar(60) NOT NULL DEFAULT 'insight_generation';
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS requested_by_user_id uuid;
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS started_at timestamptz;
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS error_message text;
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.valora_ai_runs ADD COLUMN IF NOT EXISTS deleted_at timestamptz;

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_run_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 ai_run_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_runs(id), event_type varchar(60) NOT NULL,
 status varchar(40), message text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valora_ai_run_events_run ON valorapesquisa.valora_ai_run_events(ai_run_id,created_at);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_evidence_packs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 diagnostic_id uuid NOT NULL, result_id uuid, methodology_version_id uuid, ai_run_id uuid REFERENCES valorapesquisa.valora_ai_runs(id),
 status varchar(40) NOT NULL DEFAULT 'built', evidence_count int NOT NULL DEFAULT 0, limitation text,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_valora_ai_evidence_packs_context ON valorapesquisa.valora_ai_evidence_packs(organization_id,diagnostic_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_evidence_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 evidence_pack_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_evidence_packs(id), evidence_type varchar(60) NOT NULL,
 source_type varchar(80) NOT NULL, source_id uuid, summary text NOT NULL, related_dimension varchar(160),
 related_index_code varchar(100), is_aggregate boolean NOT NULL DEFAULT true, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_valora_ai_evidence_items_pack ON valorapesquisa.valora_ai_evidence_items(evidence_pack_id,created_at);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_insights (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 diagnostic_id uuid NOT NULL, result_id uuid, ai_run_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_runs(id),
 insight_type varchar(60) NOT NULL, title varchar(240) NOT NULL, summary text NOT NULL, evidence_summary text NOT NULL,
 related_dimension varchar(160), related_index_code varchar(100), severity varchar(30) NOT NULL DEFAULT 'medium',
 priority varchar(30) NOT NULL DEFAULT 'medium', confidence_level varchar(30) NOT NULL, limitation text,
 recommendation text NOT NULL, status varchar(40) NOT NULL DEFAULT 'draft', reviewed_by_user_id uuid, reviewed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 CONSTRAINT ck_valora_ai_insights_status CHECK (status IN ('draft','generated','pending_review','approved','rejected','converted_to_action','converted_to_decision','archived')));
CREATE INDEX IF NOT EXISTS ix_valora_ai_insights_queue ON valorapesquisa.valora_ai_insights(organization_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_valora_ai_insights_context ON valorapesquisa.valora_ai_insights(organization_id,diagnostic_id,result_id);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_insight_evidence_links (
 insight_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_insights(id), evidence_item_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_evidence_items(id),
 created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(insight_id,evidence_item_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_review_queue (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 insight_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_insights(id), risk_flags jsonb NOT NULL DEFAULT '[]'::jsonb,
 status varchar(40) NOT NULL DEFAULT 'pending', assigned_to_user_id uuid, reviewed_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_valora_ai_review_queue_pending ON valorapesquisa.valora_ai_review_queue(organization_id,status,created_at) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_feedbacks (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 insight_id uuid REFERENCES valorapesquisa.valora_ai_insights(id), ai_run_id uuid REFERENCES valorapesquisa.valora_ai_runs(id),
 feedback_type varchar(40) NOT NULL, reason text NOT NULL, created_by_user_id uuid NOT NULL,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valora_ai_feedbacks_insight ON valorapesquisa.valora_ai_feedbacks(insight_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_guardrail_violations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 ai_run_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_runs(id), insight_id uuid REFERENCES valorapesquisa.valora_ai_insights(id),
 violation_code varchar(100) NOT NULL, description text NOT NULL, blocked boolean NOT NULL DEFAULT true,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valora_ai_guardrail_violations_run ON valorapesquisa.valora_ai_guardrail_violations(ai_run_id,created_at);

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_ai_prompt_execution_logs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 ai_run_id uuid NOT NULL REFERENCES valorapesquisa.valora_ai_runs(id), prompt_code varchar(100) NOT NULL,
 prompt_version int NOT NULL, provider varchar(100), model varchar(100), input_hash varchar(128) NOT NULL,
 output_hash varchar(128), input_tokens int, output_tokens int, duration_ms bigint, correlation_id text,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_valora_ai_prompt_logs_run ON valorapesquisa.valora_ai_prompt_execution_logs(ai_run_id,created_at);

-- Arquivo formal imutável. O conteúdo persistido permite download posterior sem
-- depender de filesystem local ou regeneração com dados potencialmente alterados.
CREATE TABLE IF NOT EXISTS valorapesquisa.formal_documents (
 id uuid PRIMARY KEY, organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), diagnosis_id uuid NOT NULL,
 format varchar(30) NOT NULL, file_name varchar(240) NOT NULL, content_type varchar(160) NOT NULL,
 content bytea NOT NULL, trace_code varchar(64) NOT NULL UNIQUE, generated_by uuid,
 generated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_formal_documents_org_diagnosis ON valorapesquisa.formal_documents(organization_id,diagnosis_id,generated_at DESC);

ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS survey_id uuid;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS scope varchar(80);
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS requested_by uuid;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS requested_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS completed_at timestamptz;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS failed_at timestamptz;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS file_path text;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS file_key text;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS error_message text;
ALTER TABLE valorapesquisa.exports ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_exports_organization_status ON valorapesquisa.exports(organization_id,status,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.export_files (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 export_id uuid NOT NULL REFERENCES valorapesquisa.exports(id), storage_key text, content_type varchar(100), content_hash text,
 status varchar(30) NOT NULL DEFAULT 'available', expires_at timestamptz, revoked_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_export_files_export ON valorapesquisa.export_files(organization_id,export_id,created_at DESC);

-- Administração SaaS: evolução aditiva para notificações, governança e saúde.
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.notifications ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS severity varchar(30) NOT NULL DEFAULT 'information';
ALTER TABLE valorapesquisa.platform_governance_events ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;

-- Contratos administrativos explícitos. O JSON legado é preservado para
-- compatibilidade, enquanto novos fluxos passam a ter colunas consultáveis.
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS privacy_min_group_size int NOT NULL DEFAULT 5;
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS allow_public_results boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS allow_certificates boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS allow_segmentation boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS public_branding_enabled boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS default_lgpd_term_version varchar(40) NOT NULL DEFAULT '1';
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS language varchar(10) NOT NULL DEFAULT 'pt-BR';
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS timezone varchar(80) NOT NULL DEFAULT 'America/Sao_Paulo';
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'active';
ALTER TABLE valorapesquisa.organization_settings ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;

ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS user_id uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS usage_code varchar(100);
ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS amount bigint;
ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS entity_type varchar(80);
ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS entity_id uuid;
ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS period_start timestamptz;
ALTER TABLE valorapesquisa.usage_events ADD COLUMN IF NOT EXISTS period_end timestamptz;
UPDATE valorapesquisa.usage_events SET usage_code=feature_key WHERE usage_code IS NULL;
UPDATE valorapesquisa.usage_events SET amount=quantity WHERE amount IS NULL;
CREATE INDEX IF NOT EXISTS ix_usage_events_period ON valorapesquisa.usage_events(organization_id,usage_code,period_start,period_end);

ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS requester_masked text;
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS protocol_code text;
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS response_message text;
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS created_by uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS assigned_to uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS completed_at timestamptz;
ALTER TABLE valorapesquisa.privacy_requests ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
UPDATE valorapesquisa.privacy_requests SET protocol_code=protocol WHERE protocol_code IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_privacy_requests_protocol_code ON valorapesquisa.privacy_requests(protocol_code) WHERE protocol_code IS NOT NULL;

ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS ip_hash text;
ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS user_agent text;
ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS severity varchar(32) NOT NULL DEFAULT 'info';
ALTER TABLE valorapesquisa.audit_logs ALTER COLUMN severity TYPE varchar(32);
ALTER TABLE valorapesquisa.audit_logs ALTER COLUMN severity SET DEFAULT 'info';
UPDATE valorapesquisa.audit_logs
SET severity=CASE lower(severity) WHEN 'debug' THEN 'debug' WHEN 'warning' THEN 'warning' WHEN 'error' THEN 'error' WHEN 'critical' THEN 'critical' ELSE 'info' END
WHERE severity IS NULL OR severity NOT IN ('debug','info','warning','error','critical');
ALTER TABLE valorapesquisa.audit_logs ALTER COLUMN severity SET NOT NULL;
DO $audit_severity_constraint$ BEGIN
 IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_audit_logs_severity' AND conrelid='valorapesquisa.audit_logs'::regclass) THEN
  ALTER TABLE valorapesquisa.audit_logs ADD CONSTRAINT ck_audit_logs_severity CHECK (severity IN ('debug','info','warning','error','critical'));
 END IF;
END $audit_severity_constraint$;
CREATE INDEX IF NOT EXISTS ix_audit_logs_severity ON valorapesquisa.audit_logs(severity);
ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS module varchar(80);
ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_audit_logs_admin_filters ON valorapesquisa.audit_logs(organization_id, module, severity, created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.configuration_change_history (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id),
 setting_key varchar(100) NOT NULL, before_json jsonb, after_json jsonb, reason text, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb);
-- A tabela pode ter sido criada antes pelo gerador genérico de módulos.
ALTER TABLE valorapesquisa.configuration_change_history
 ADD COLUMN IF NOT EXISTS user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS setting_key varchar(100),
 ADD COLUMN IF NOT EXISTS before_json jsonb,
 ADD COLUMN IF NOT EXISTS after_json jsonb,
 ADD COLUMN IF NOT EXISTS reason text,
 ADD COLUMN IF NOT EXISTS correlation_id text,
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_configuration_change_history_org ON valorapesquisa.configuration_change_history(organization_id,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.permission_change_history (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), user_id uuid REFERENCES valorapesquisa.users(id),
 role_id uuid REFERENCES valorapesquisa.roles(id), before_json jsonb, after_json jsonb, reason text, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb);
ALTER TABLE valorapesquisa.permission_change_history
 ADD COLUMN IF NOT EXISTS user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS role_id uuid REFERENCES valorapesquisa.roles(id),
 ADD COLUMN IF NOT EXISTS before_json jsonb,
 ADD COLUMN IF NOT EXISTS after_json jsonb,
 ADD COLUMN IF NOT EXISTS reason text,
 ADD COLUMN IF NOT EXISTS correlation_id text,
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_permission_change_history_org ON valorapesquisa.permission_change_history(organization_id,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.notification_reads (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), notification_id uuid NOT NULL REFERENCES valorapesquisa.notifications(id),
 user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), read_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(notification_id,user_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.support_access_sessions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), support_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 reason text NOT NULL, status varchar(30) NOT NULL DEFAULT 'requested', approved_by uuid REFERENCES valorapesquisa.users(id), starts_at timestamptz, expires_at timestamptz,
 revoked_at timestamptz, correlation_id text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_support_access_sessions_org ON valorapesquisa.support_access_sessions(organization_id,status,created_at DESC);

-- Entregáveis executivos: evolução aditiva do Action canônico, sem criar um
-- segundo conceito concorrente de plano de evolução.
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS insight_id uuid;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS inference_id uuid;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS concept_code varchar(100);
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS urgency varchar(30);
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS impact text;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS learning_record text;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS completed_at timestamptz;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS cancelled_at timestamptz;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS correlation_id text;
ALTER TABLE valorapesquisa.valora_actions ADD COLUMN IF NOT EXISTS idempotency_key text;
CREATE INDEX IF NOT EXISTS ix_valora_actions_origin
 ON valorapesquisa.valora_actions(organization_id,insight_id,inference_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.action_learning_records (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 action_id uuid NOT NULL REFERENCES valorapesquisa.valora_actions(id), learning_record text NOT NULL,
 created_by uuid REFERENCES valorapesquisa.users(id), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, source_hash text, idempotency_key text, status varchar(30) NOT NULL DEFAULT 'recorded',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_action_learning_action
 ON valorapesquisa.action_learning_records(organization_id,action_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.system_health_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), component varchar(80) NOT NULL, status varchar(30) NOT NULL,
 message text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_system_health_events_component
 ON valorapesquisa.system_health_events(component,created_at DESC) WHERE deleted_at IS NULL;

-- Enterprise & Integrações: estruturas operacionais isoladas por organização.
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS expires_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ux_api_keys_hash_active ON valorapesquisa.api_keys(key_hash) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_api_keys_organization_status ON valorapesquisa.api_keys(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.integration_connections (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), code varchar(80) NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'not_configured', last_execution_at timestamptz, last_error_code varchar(80), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(organization_id,code));
CREATE INDEX IF NOT EXISTS ix_integration_connections_org ON valorapesquisa.integration_connections(organization_id,status) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.integration_settings (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), connection_id uuid NOT NULL REFERENCES valorapesquisa.integration_connections(id),
 setting_key varchar(100) NOT NULL, value_protected text, value_hash text, status varchar(30) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(connection_id,setting_key));
CREATE TABLE IF NOT EXISTS valorapesquisa.api_key_usage_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), api_key_id uuid REFERENCES valorapesquisa.api_keys(id), scope varchar(100),
 route text, status_code int, ip_hash text, user_agent_hash text, correlation_id text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, status varchar(30) NOT NULL DEFAULT 'recorded',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_api_key_usage_org ON valorapesquisa.api_key_usage_events(organization_id,api_key_id,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.webhook_subscriptions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), name varchar(160) NOT NULL, endpoint_url text NOT NULL,
 events text[] NOT NULL DEFAULT '{}', secret_hash text, secret_protected text, status varchar(30) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_webhooks_org ON valorapesquisa.webhook_subscriptions(organization_id,status) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.webhook_deliveries (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), subscription_id uuid NOT NULL REFERENCES valorapesquisa.webhook_subscriptions(id),
 event_type varchar(100) NOT NULL, payload_sanitized jsonb NOT NULL DEFAULT '{}'::jsonb, response_sanitized text, response_status int, attempt_count int NOT NULL DEFAULT 0,
 next_attempt_at timestamptz, delivered_at timestamptz, status varchar(30) NOT NULL DEFAULT 'pending', idempotency_key text, correlation_id text,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_webhook_deliveries_pending ON valorapesquisa.webhook_deliveries(organization_id,status,next_attempt_at) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.powerbi_datasets (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), format varchar(20) NOT NULL, tables_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 storage_key text, content_hash text, size_bytes bigint, minimum_sample_size int NOT NULL DEFAULT 5, status varchar(30) NOT NULL DEFAULT 'preparing', expires_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, source_hash text, idempotency_key text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_powerbi_datasets_org ON valorapesquisa.powerbi_datasets(organization_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.powerbi_dataset_exports (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), dataset_id uuid NOT NULL REFERENCES valorapesquisa.powerbi_datasets(id),
 exported_by uuid REFERENCES valorapesquisa.users(id), ip_hash text, status varchar(30) NOT NULL DEFAULT 'completed', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_sessions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), diagnostic_id uuid, insight_id uuid, action_id uuid,
 participant_user_id uuid REFERENCES valorapesquisa.users(id), facilitator_user_id uuid REFERENCES valorapesquisa.users(id), agenda text NOT NULL, objective text NOT NULL,
 agreements text, perceived_risks text, next_steps text, confidentiality varchar(30) NOT NULL DEFAULT 'restricted', scheduled_at timestamptz, completed_at timestamptz,
 status varchar(30) NOT NULL DEFAULT 'draft', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
-- one_on_one_sessions já pode existir com apenas o contrato genérico. Evolua
-- todas as colunas específicas antes de criar índices ou atender o módulo.
ALTER TABLE valorapesquisa.one_on_one_sessions
 ADD COLUMN IF NOT EXISTS diagnostic_id uuid,
 ADD COLUMN IF NOT EXISTS insight_id uuid,
 ADD COLUMN IF NOT EXISTS action_id uuid,
 ADD COLUMN IF NOT EXISTS participant_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS facilitator_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS agenda text,
 ADD COLUMN IF NOT EXISTS objective text,
 ADD COLUMN IF NOT EXISTS agreements text,
 ADD COLUMN IF NOT EXISTS perceived_risks text,
 ADD COLUMN IF NOT EXISTS next_steps text,
 ADD COLUMN IF NOT EXISTS confidentiality varchar(30),
 ADD COLUMN IF NOT EXISTS scheduled_at timestamptz,
 ADD COLUMN IF NOT EXISTS completed_at timestamptz,
 ADD COLUMN IF NOT EXISTS status varchar(30),
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS correlation_id text,
 ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
UPDATE valorapesquisa.one_on_one_sessions
 SET agenda=COALESCE(NULLIF(btrim(agenda),''),'Sessão individual'),
     objective=COALESCE(NULLIF(btrim(objective),''),'Acompanhamento organizacional'),
     confidentiality=COALESCE(NULLIF(btrim(confidentiality),''),'restricted'),
     status=COALESCE(NULLIF(btrim(status),''),'draft'),
     metadata_json=COALESCE(metadata_json,'{}'::jsonb),
     updated_at=COALESCE(updated_at,created_at,now())
 WHERE agenda IS NULL OR btrim(agenda)='' OR objective IS NULL OR btrim(objective)=''
    OR confidentiality IS NULL OR btrim(confidentiality)='' OR status IS NULL OR btrim(status)=''
    OR metadata_json IS NULL OR updated_at IS NULL;
ALTER TABLE valorapesquisa.one_on_one_sessions
 ALTER COLUMN agenda SET NOT NULL,
 ALTER COLUMN objective SET NOT NULL,
 ALTER COLUMN confidentiality SET DEFAULT 'restricted',
 ALTER COLUMN confidentiality SET NOT NULL,
 ALTER COLUMN status SET DEFAULT 'draft',
 ALTER COLUMN status SET NOT NULL,
 ALTER COLUMN metadata_json SET DEFAULT '{}'::jsonb,
 ALTER COLUMN metadata_json SET NOT NULL,
 ALTER COLUMN updated_at SET DEFAULT now(),
 ALTER COLUMN updated_at SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_one_on_one_org ON valorapesquisa.one_on_one_sessions(organization_id,status,scheduled_at) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_notes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),
 note text NOT NULL, visibility varchar(30) NOT NULL DEFAULT 'restricted', status varchar(30) NOT NULL DEFAULT 'recorded', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_actions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),
 action_id uuid REFERENCES valorapesquisa.valora_actions(id), evidence_reference text NOT NULL, status varchar(30) NOT NULL DEFAULT 'created', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_runs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), comparison_type varchar(40) NOT NULL, dimension varchar(100),
 minimum_sample_size int NOT NULL DEFAULT 5, context text NOT NULL, status varchar(30) NOT NULL DEFAULT 'pending', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, source_hash text, idempotency_key text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
ALTER TABLE valorapesquisa.benchmark_runs
 ADD COLUMN IF NOT EXISTS comparison_type varchar(40) NOT NULL DEFAULT 'internal',
 ADD COLUMN IF NOT EXISTS dimension varchar(100),
 ADD COLUMN IF NOT EXISTS minimum_sample_size int NOT NULL DEFAULT 5,
 ADD COLUMN IF NOT EXISTS context text NOT NULL DEFAULT 'Contexto de benchmark não informado.',
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS correlation_id text,
 ADD COLUMN IF NOT EXISTS source_hash text,
 ADD COLUMN IF NOT EXISTS idempotency_key text;
CREATE INDEX IF NOT EXISTS ix_benchmark_runs_org ON valorapesquisa.benchmark_runs(organization_id,created_at DESC) WHERE deleted_at IS NULL;

-- Valora Benchmark™: snapshots imutáveis, comparações tenant-safe e referências anonimizadas.
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('benchmark.read','Visualizar Benchmark','Consulta snapshots e comparações agregadas.','organizational_intelligence'),
 ('benchmark.generate','Gerar Benchmark','Gera snapshot a partir de resultado real.','organizational_intelligence'),
 ('benchmark.compare','Comparar Benchmark','Compara ciclos e recortes autorizados.','organizational_intelligence'),
 ('benchmark.export','Exportar Benchmark','Exporta comparativos autorizados.','organizational_intelligence'),
 ('benchmark.admin','Administrar Benchmark','Configura amostra, anonimização e referências.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_settings (
 organization_id uuid PRIMARY KEY REFERENCES valorapesquisa.organizations(id), minimum_organizations int NOT NULL DEFAULT 5,
 minimum_responses int NOT NULL DEFAULT 50, external_enabled boolean NOT NULL DEFAULT false,
 require_anonymization boolean NOT NULL DEFAULT true, settings_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_snapshots (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 survey_id uuid REFERENCES valorapesquisa.surveys(id), response_batch_id uuid, result_id uuid REFERENCES valorapesquisa.result_scores(id),
 snapshot_type varchar(24) NOT NULL DEFAULT 'internal', maturity_score numeric(12,4) NOT NULL DEFAULT 0,
 maturity_level varchar(80) NOT NULL DEFAULT 'Não classificado', total_responses int NOT NULL DEFAULT 0,
 dimensions_json jsonb NOT NULL DEFAULT '[]'::jsonb, evidence_summary text NOT NULL DEFAULT 'Sem evidências agregadas.',
 generated_at timestamptz NOT NULL DEFAULT now(), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
ALTER TABLE valorapesquisa.benchmark_snapshots
 ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES valorapesquisa.organizations(id), ADD COLUMN IF NOT EXISTS survey_id uuid REFERENCES valorapesquisa.surveys(id),
 ADD COLUMN IF NOT EXISTS response_batch_id uuid, ADD COLUMN IF NOT EXISTS result_id uuid REFERENCES valorapesquisa.result_scores(id),
 ADD COLUMN IF NOT EXISTS snapshot_type varchar(24) NOT NULL DEFAULT 'internal', ADD COLUMN IF NOT EXISTS maturity_score numeric(12,4) NOT NULL DEFAULT 0,
 ADD COLUMN IF NOT EXISTS maturity_level varchar(80) NOT NULL DEFAULT 'Não classificado', ADD COLUMN IF NOT EXISTS total_responses int NOT NULL DEFAULT 0,
 ADD COLUMN IF NOT EXISTS dimensions_json jsonb NOT NULL DEFAULT '[]'::jsonb, ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT 'Sem evidências agregadas.',
 ADD COLUMN IF NOT EXISTS generated_at timestamptz NOT NULL DEFAULT now(), ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now(), ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(), ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE INDEX IF NOT EXISTS ix_benchmark_snapshots_org_generated ON valorapesquisa.benchmark_snapshots(organization_id,generated_at DESC) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_snapshot_dimensions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), snapshot_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_snapshots(id),
 dimension_code varchar(100) NOT NULL, score numeric(12,4), reference_score numeric(12,4), evidence_count int NOT NULL DEFAULT 0,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_comparisons (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), base_snapshot_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_snapshots(id),
 compared_snapshot_id uuid REFERENCES valorapesquisa.benchmark_snapshots(id), comparison_type varchar(40) NOT NULL, score_delta numeric(12,4), maturity_delta text NOT NULL DEFAULT 'Sem referência',
 strengths_json jsonb NOT NULL DEFAULT '[]'::jsonb, risks_json jsonb NOT NULL DEFAULT '[]'::jsonb, opportunities_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 recommendations_json jsonb NOT NULL DEFAULT '[]'::jsonb, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_benchmark_comparisons_org ON valorapesquisa.benchmark_comparisons(organization_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_reference_groups (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), name text NOT NULL, reference_type varchar(24) NOT NULL,
 minimum_organizations int NOT NULL DEFAULT 5, minimum_responses int NOT NULL DEFAULT 50, organization_count int NOT NULL DEFAULT 0, response_count int NOT NULL DEFAULT 0,
 aggregates_json jsonb NOT NULL DEFAULT '{}'::jsonb, status varchar(30) NOT NULL DEFAULT 'inactive', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_reference_group_members (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), reference_group_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_reference_groups(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 consented_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(reference_group_id,organization_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_insights (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), comparison_id uuid REFERENCES valorapesquisa.benchmark_comparisons(id),
 evidence_pack_json jsonb NOT NULL DEFAULT '{}'::jsonb, insight_json jsonb NOT NULL DEFAULT '{}'::jsonb, limitations_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_exports (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), snapshot_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_snapshots(id),
 format varchar(12) NOT NULL, status varchar(30) NOT NULL DEFAULT 'pending', storage_reference text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_comparison_groups (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), run_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_runs(id),
 group_reference text NOT NULL, sample_size int NOT NULL, comparable boolean NOT NULL DEFAULT false, status varchar(30) NOT NULL DEFAULT 'evaluated', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_results (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), run_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_runs(id),
 metric_code varchar(100) NOT NULL, difference numeric(12,4), interpretation text, internal_practices text, opportunities text, status varchar(30) NOT NULL DEFAULT 'available',
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
ALTER TABLE valorapesquisa.benchmark_results
 ADD COLUMN IF NOT EXISTS run_id uuid REFERENCES valorapesquisa.benchmark_runs(id),
 ADD COLUMN IF NOT EXISTS metric_code varchar(100),
 ADD COLUMN IF NOT EXISTS difference numeric(12,4),
 ADD COLUMN IF NOT EXISTS interpretation text,
 ADD COLUMN IF NOT EXISTS internal_practices text,
 ADD COLUMN IF NOT EXISTS opportunities text,
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS correlation_id text;

-- Valora Heatmap™ e Benchmark™ profissional. Migração aditiva para instalações
-- limpas ou parcialmente provisionadas; nenhuma leitura cria causas ou pessoas.
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('heatmap.read','Visualizar Heatmap','Consulta mapas organizacionais agregados.','organizational_intelligence'),
 ('heatmap.generate','Gerar Heatmap','Gera mapa somente a partir de resultados reais.','organizational_intelligence'),
 ('heatmap.manage','Gerenciar Heatmap','Administra snapshots, filtros e interpretações.','organizational_intelligence'),
 ('benchmark.manage','Gerenciar Benchmark','Administra comparações organizacionais não punitivas.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();

ALTER TABLE valorapesquisa.heatmap_snapshots
 ADD COLUMN IF NOT EXISTS diagnostic_id uuid REFERENCES valorapesquisa.surveys(id),
 ADD COLUMN IF NOT EXISTS result_id uuid REFERENCES valorapesquisa.result_scores(id),
 ADD COLUMN IF NOT EXISTS title text NOT NULL DEFAULT 'Heatmap organizacional',
 ADD COLUMN IF NOT EXISTS snapshot_type varchar(40) NOT NULL DEFAULT 'dimension',
 ADD COLUMN IF NOT EXISTS generated_by_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS generated_at timestamptz NOT NULL DEFAULT now(),
 ADD COLUMN IF NOT EXISTS filters_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT 'Sem evidências agregadas.',
 ADD COLUMN IF NOT EXISTS ai_summary text,
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.heatmap_cells
 ADD COLUMN IF NOT EXISTS heatmap_snapshot_id uuid REFERENCES valorapesquisa.heatmap_snapshots(id),
 ADD COLUMN IF NOT EXISTS dimension text,
 ADD COLUMN IF NOT EXISTS index_code varchar(40),
 ADD COLUMN IF NOT EXISTS area_name text,
 ADD COLUMN IF NOT EXISTS unit_name text,
 ADD COLUMN IF NOT EXISTS leadership_name text,
 ADD COLUMN IF NOT EXISTS score numeric(12,4),
 ADD COLUMN IF NOT EXISTS level varchar(40) NOT NULL DEFAULT 'amostra insuficiente',
 ADD COLUMN IF NOT EXISTS risk_level varchar(40) NOT NULL DEFAULT 'indeterminado',
 ADD COLUMN IF NOT EXISTS trend varchar(40) NOT NULL DEFAULT 'sem série comparável',
 ADD COLUMN IF NOT EXISTS response_count int NOT NULL DEFAULT 0,
 ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT 'Sem evidências agregadas.',
 ADD COLUMN IF NOT EXISTS recommendation text NOT NULL DEFAULT 'Ampliar a amostra antes de interpretar.',
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
CREATE INDEX IF NOT EXISTS ix_heatmap_snapshots_org_generated ON valorapesquisa.heatmap_snapshots(organization_id,generated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_heatmap_cells_snapshot ON valorapesquisa.heatmap_cells(heatmap_snapshot_id,score) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.heatmap_filters(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),heatmap_snapshot_id uuid REFERENCES valorapesquisa.heatmap_snapshots(id),
 filter_type varchar(40) NOT NULL,filter_value text,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.heatmap_ai_interpretations(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),heatmap_snapshot_id uuid NOT NULL REFERENCES valorapesquisa.heatmap_snapshots(id),
 evidence_summary text NOT NULL,interpretation text NOT NULL,limitations text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);

ALTER TABLE valorapesquisa.benchmark_comparisons
 ADD COLUMN IF NOT EXISTS diagnostic_id uuid REFERENCES valorapesquisa.surveys(id),
 ADD COLUMN IF NOT EXISTS result_id uuid REFERENCES valorapesquisa.result_scores(id),
 ADD COLUMN IF NOT EXISTS title text NOT NULL DEFAULT 'Comparação organizacional',
 ADD COLUMN IF NOT EXISTS status varchar(30) NOT NULL DEFAULT 'generated',
 ADD COLUMN IF NOT EXISTS baseline_label text NOT NULL DEFAULT 'Base',
 ADD COLUMN IF NOT EXISTS target_label text NOT NULL DEFAULT 'Referência',
 ADD COLUMN IF NOT EXISTS generated_by_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS generated_at timestamptz NOT NULL DEFAULT now(),
 ADD COLUMN IF NOT EXISTS filters_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT 'Comparação agregada.',
 ADD COLUMN IF NOT EXISTS ai_summary text;
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_comparison_items(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),benchmark_comparison_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_comparisons(id),
 dimension text,index_code varchar(40),baseline_score numeric(12,4),target_score numeric(12,4),difference numeric(12,4),sample_size int NOT NULL DEFAULT 0,
 evidence_summary text NOT NULL DEFAULT 'Sem evidências agregadas.',metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.benchmark_ai_insights(
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),benchmark_comparison_id uuid NOT NULL REFERENCES valorapesquisa.benchmark_comparisons(id),
 evidence_summary text NOT NULL,insight text NOT NULL,limitations text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_benchmark_comparison_items_comparison ON valorapesquisa.benchmark_comparison_items(benchmark_comparison_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_benchmark_ai_insights_comparison ON valorapesquisa.benchmark_ai_insights(benchmark_comparison_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.import_batches (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), import_type varchar(60) NOT NULL, file_name text,
 source_hash text NOT NULL, status varchar(30) NOT NULL DEFAULT 'uploaded', valid_rows int NOT NULL DEFAULT 0, invalid_rows int NOT NULL DEFAULT 0,
 confirmed_at timestamptz, idempotency_key text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_import_batches_org ON valorapesquisa.import_batches(organization_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.import_batch_rows (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), batch_id uuid NOT NULL REFERENCES valorapesquisa.import_batches(id),
 line_number int NOT NULL, data_json jsonb NOT NULL DEFAULT '{}'::jsonb, errors_json jsonb NOT NULL DEFAULT '[]'::jsonb, status varchar(30) NOT NULL DEFAULT 'pending',
 source_hash text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(batch_id,line_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.import_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), import_type varchar(60) NOT NULL, version int NOT NULL DEFAULT 1,
 columns_json jsonb NOT NULL, status varchar(30) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, correlation_id text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(organization_id,import_type,version));

-- Enterprise consolidation: additive compatibility for installations created by earlier releases.
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS name varchar(160);
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS provider varchar(100);
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS last_checked_at timestamptz;
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS last_success_at timestamptz;
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS last_failure_at timestamptz;
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS error_message text;
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS created_by uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.integration_connections ADD COLUMN IF NOT EXISTS disabled_at timestamptz;

CREATE TABLE IF NOT EXISTS valorapesquisa.integration_execution_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 connection_id uuid REFERENCES valorapesquisa.integration_connections(id), event_type varchar(80) NOT NULL, status varchar(30) NOT NULL,
 error_message text, correlation_id text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_integration_execution_events_org ON valorapesquisa.integration_execution_events(organization_id,created_at DESC) WHERE deleted_at IS NULL;

ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS key_prefix varchar(32);
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS scopes_json jsonb NOT NULL DEFAULT '[]'::jsonb;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS created_by uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.api_key_usage_events ADD COLUMN IF NOT EXISTS endpoint text;
ALTER TABLE valorapesquisa.api_key_usage_events ADD COLUMN IF NOT EXISTS method varchar(12);
ALTER TABLE valorapesquisa.api_key_usage_events ADD COLUMN IF NOT EXISTS scope_used varchar(100);

ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS url text;
ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS events_json jsonb NOT NULL DEFAULT '[]'::jsonb;
ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS last_delivery_at timestamptz;
ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS last_success_at timestamptz;
ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS last_failure_at timestamptz;
ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS created_by uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.webhook_subscriptions ADD COLUMN IF NOT EXISTS disabled_at timestamptz;
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS webhook_id uuid REFERENCES valorapesquisa.webhook_subscriptions(id);
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS entity_type varchar(100);
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS entity_id uuid;
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS payload_hash text;
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS last_status_code int;
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS last_error_message text;
ALTER TABLE valorapesquisa.webhook_deliveries ADD COLUMN IF NOT EXISTS failed_at timestamptz;
CREATE TABLE IF NOT EXISTS valorapesquisa.webhook_delivery_attempts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 delivery_id uuid NOT NULL REFERENCES valorapesquisa.webhook_deliveries(id), attempt_number int NOT NULL, status varchar(30) NOT NULL,
 status_code int, error_message text, response_hash text, correlation_id text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(delivery_id,attempt_number));

ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS name varchar(160);
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS scope varchar(80);
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS generated_by uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS generated_at timestamptz;
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS file_path text;
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS file_key text;
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS record_count bigint;
ALTER TABLE valorapesquisa.powerbi_datasets ADD COLUMN IF NOT EXISTS limitation text;

ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS title varchar(200);
ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS participant_name_masked varchar(160);
ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS confidentiality_level varchar(30);
ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS summary text;
ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS agreements_json jsonb NOT NULL DEFAULT '[]'::jsonb;
ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS next_steps_json jsonb NOT NULL DEFAULT '[]'::jsonb;
ALTER TABLE valorapesquisa.one_on_one_sessions ADD COLUMN IF NOT EXISTS created_by uuid REFERENCES valorapesquisa.users(id);

ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS file_path text;
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS total_rows int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS dry_run boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS requested_by uuid REFERENCES valorapesquisa.users(id);
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS validated_at timestamptz;
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS executed_at timestamptz;
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS cancelled_at timestamptz;
ALTER TABLE valorapesquisa.import_batches ADD COLUMN IF NOT EXISTS error_message text;

-- Operational readiness ledger. Dumps and sensitive filesystem paths must never be stored here.
CREATE TABLE IF NOT EXISTS valorapesquisa.schema_version (
 version varchar(50) PRIMARY KEY, description text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now());
ALTER TABLE valorapesquisa.schema_version ADD COLUMN IF NOT EXISTS checksum text;
ALTER TABLE valorapesquisa.schema_version ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
INSERT INTO valorapesquisa.schema_version(version, description) VALUES ('2026.08-go-live', 'Operational readiness baseline') ON CONFLICT (version) DO NOTHING;
CREATE TABLE IF NOT EXISTS valorapesquisa.operational_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), event_type varchar(80) NOT NULL,
 status varchar(30) NOT NULL, performed_by uuid REFERENCES valorapesquisa.users(id), performed_at timestamptz NOT NULL DEFAULT now(),
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, notes text, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_operational_events_type_date ON valorapesquisa.operational_events(event_type, performed_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.backup_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), event_type varchar(80) NOT NULL DEFAULT 'backup',
 status varchar(30) NOT NULL, performed_by uuid REFERENCES valorapesquisa.users(id), performed_at timestamptz NOT NULL DEFAULT now(),
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, notes text, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_backup_events_date ON valorapesquisa.backup_events(performed_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.restore_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), event_type varchar(80) NOT NULL DEFAULT 'restore',
 status varchar(30) NOT NULL, performed_by uuid REFERENCES valorapesquisa.users(id), performed_at timestamptz NOT NULL DEFAULT now(),
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, notes text, correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_restore_events_date ON valorapesquisa.restore_events(performed_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.configuration_validation_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), environment_name varchar(80) NOT NULL, overall_status varchar(30) NOT NULL,
 issues_json jsonb NOT NULL DEFAULT '[]'::jsonb, correlation_id text, checked_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now());
ALTER TABLE valorapesquisa.configuration_validation_events ADD COLUMN IF NOT EXISTS issues_count integer NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.configuration_validation_events ADD COLUMN IF NOT EXISTS critical_count integer NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.configuration_validation_events ADD COLUMN IF NOT EXISTS warning_count integer NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.configuration_validation_events ADD COLUMN IF NOT EXISTS checked_by uuid REFERENCES valorapesquisa.users(id);
CREATE TABLE IF NOT EXISTS valorapesquisa.maintenance_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES valorapesquisa.organizations(id), enabled boolean NOT NULL,
 message text, performed_by uuid REFERENCES valorapesquisa.users(id), performed_at timestamptz NOT NULL DEFAULT now(), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 correlation_id text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.deployment_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), version varchar(80) NOT NULL, environment_name varchar(80) NOT NULL,
 status varchar(30) NOT NULL, started_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz,
 performed_by uuid REFERENCES valorapesquisa.users(id), notes text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_deployment_events_environment_date ON valorapesquisa.deployment_events(environment_name, started_at DESC);

-- Entregáveis executivos 2026-08: índices operacionais aditivos para os fluxos
-- de replanejamento, acompanhamento de atraso e memória longitudinal.
CREATE INDEX IF NOT EXISTS ix_valora_actions_due_status
 ON valorapesquisa.valora_actions(organization_id,due_at,status)
 WHERE deleted_at IS NULL AND due_at IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_journey_events_survey_date
 ON valorapesquisa.journey_events(organization_id,survey_id,created_at DESC)
 WHERE deleted_at IS NULL AND survey_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_executive_reports_version
 ON valorapesquisa.executive_reports(organization_id,survey_id,version,created_at DESC)
 WHERE deleted_at IS NULL;
BEGIN;
-- Funil comercial público Valora Insight™ (idempotente e não destrutivo)
CREATE TABLE IF NOT EXISTS valorapesquisa.public_leads (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NULL, name varchar(180) NOT NULL,
 email_hash varchar(64) NOT NULL, email_masked varchar(254) NOT NULL, phone_hash varchar(64), phone_masked varchar(40),
 company_name varchar(220) NOT NULL, company_document_hash varchar(64), company_document_masked varchar(40),
 segment varchar(100), company_size varchar(60), role_title varchar(120), source varchar(80) NOT NULL DEFAULT 'public_free_diagnostic',
 status varchar(40) NOT NULL DEFAULT 'new', consent_version varchar(30) NOT NULL, consent_accepted_at timestamptz NOT NULL,
 communication_consent boolean NOT NULL DEFAULT false, last_result_level varchar(60), last_result_score numeric(8,2),
 plan_interest varchar(60), assigned_to uuid, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 converted_at timestamptz, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_public_leads_email_hash ON valorapesquisa.public_leads(email_hash);
CREATE INDEX IF NOT EXISTS ix_public_leads_pipeline ON valorapesquisa.public_leads(status,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.public_diagnostic_sessions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid REFERENCES valorapesquisa.public_leads(id), organization_id uuid,
 template_code varchar(100) NOT NULL, diagnostic_id uuid, response_id uuid, result_token_hash varchar(64), status varchar(40) NOT NULL DEFAULT 'started',
 started_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, abandoned_at timestamptz, ip_hash varchar(64), user_agent_hash varchar(64),
 source varchar(80) NOT NULL DEFAULT 'public_free_diagnostic', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_public_diagnostic_sessions_lead ON valorapesquisa.public_diagnostic_sessions(lead_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.public_lead_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid NOT NULL REFERENCES valorapesquisa.public_leads(id),
 session_id uuid REFERENCES valorapesquisa.public_diagnostic_sessions(id), event_type varchar(80) NOT NULL,
 message varchar(500) NOT NULL, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_public_lead_events_lead ON valorapesquisa.public_lead_events(lead_id,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.commercial_contact_requests (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid NOT NULL REFERENCES valorapesquisa.public_leads(id),
 session_id uuid REFERENCES valorapesquisa.public_diagnostic_sessions(id), request_type varchar(40) NOT NULL,
 requested_plan varchar(60), status varchar(40) NOT NULL DEFAULT 'requested', assigned_to uuid, notes text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE INDEX IF NOT EXISTS ix_commercial_contact_requests_queue ON valorapesquisa.commercial_contact_requests(status,created_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.lead_conversion_requests (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid NOT NULL REFERENCES valorapesquisa.public_leads(id), organization_id uuid,
 requested_plan varchar(60), request_type varchar(40) NOT NULL, status varchar(40) NOT NULL DEFAULT 'requested', assigned_to uuid,
 notes text, converted_organization_id uuid, converted_user_id uuid, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE INDEX IF NOT EXISTS ix_lead_conversion_requests_lead ON valorapesquisa.lead_conversion_requests(lead_id,status);

CREATE TABLE IF NOT EXISTS valorapesquisa.onboarding_commercial_steps (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), lead_id uuid NOT NULL REFERENCES valorapesquisa.public_leads(id), organization_id uuid,
 step_code varchar(80) NOT NULL, title varchar(180) NOT NULL, status varchar(40) NOT NULL DEFAULT 'not_started',
 is_automatic boolean NOT NULL DEFAULT false, completed_by uuid, completed_at timestamptz, reason text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_onboarding_commercial_step ON valorapesquisa.onboarding_commercial_steps(lead_id,step_code);

COMMIT;
-- Converges historical names for organizational units into the canonical units.* vocabulary.
-- Safe to execute repeatedly: role links are merged before aliases are removed.
BEGIN;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('units.read','Visualizar unidades','Consulta unidades.','organization'),
('units.create','Criar unidades','Cria unidades.','organization'),
('units.update','Atualizar unidades','Atualiza unidades.','organization'),
('units.disable','Desativar unidades','Desativa unidades.','organization'),
('units.delete','Excluir unidades','Exclui logicamente unidades.','organization')
ON CONFLICT(code) DO UPDATE SET module_code='organization',updated_at=now();

WITH aliases(alias_code, canonical_code) AS (VALUES
  ('organizational_units.read','units.read'), ('organization_units.read','units.read'),
  ('organizational_units.create','units.create'), ('organization_units.create','units.create'),
  ('organizational_units.update','units.update'), ('organization_units.update','units.update'),
  ('organizational_units.disable','units.disable'), ('organization_units.disable','units.disable'),
  ('organizational_units.delete','units.delete'), ('organization_units.delete','units.delete')
)
INSERT INTO valorapesquisa.role_permissions(role_id, permission_id, created_at)
SELECT rp.role_id, canonical.id, rp.created_at
FROM aliases a
JOIN valorapesquisa.permissions legacy ON legacy.code=a.alias_code
JOIN valorapesquisa.permissions canonical ON canonical.code=a.canonical_code
JOIN valorapesquisa.role_permissions rp ON rp.permission_id=legacy.id
ON CONFLICT(role_id,permission_id) DO NOTHING;

WITH aliases(alias_code) AS (VALUES
  ('organizational_units.read'),('organization_units.read'),('organizational_units.create'),('organization_units.create'),
  ('organizational_units.update'),('organization_units.update'),('organizational_units.disable'),('organization_units.disable'),
  ('organizational_units.delete'),('organization_units.delete')
), legacy AS (SELECT id FROM valorapesquisa.permissions WHERE code IN (SELECT alias_code FROM aliases))
DELETE FROM valorapesquisa.permission_migration_reviews WHERE permission_id IN (SELECT id FROM legacy);

WITH aliases(alias_code) AS (VALUES
  ('organizational_units.read'),('organization_units.read'),('organizational_units.create'),('organization_units.create'),
  ('organizational_units.update'),('organization_units.update'),('organizational_units.disable'),('organization_units.disable'),
  ('organizational_units.delete'),('organization_units.delete')
), legacy AS (SELECT id FROM valorapesquisa.permissions WHERE code IN (SELECT alias_code FROM aliases))
DELETE FROM valorapesquisa.role_permissions WHERE permission_id IN (SELECT id FROM legacy);

DELETE FROM valorapesquisa.permissions WHERE code IN (
  'organizational_units.read','organization_units.read','organizational_units.create','organization_units.create',
  'organizational_units.update','organization_units.update','organizational_units.disable','organization_units.disable',
  'organizational_units.delete','organization_units.delete');

INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES('2026_08_canonical_access_permissions','sha256:canonical-access-permissions-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();

COMMIT;


-- 52. BASE METODOLÓGICA OFICIAL VALORA INSIGHT™
-- Base metodológica oficial Valora Insight™. Aditiva, idempotente e sem recálculo histórico.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_versions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(30) NOT NULL UNIQUE, version integer NOT NULL,
 name varchar(160) NOT NULL, status varchar(20) NOT NULL CHECK(status IN ('draft','active','retired')),
 effective_from timestamptz NOT NULL, effective_to timestamptz, change_log text NOT NULL DEFAULT '',
 snapshot_json jsonb NOT NULL DEFAULT '{}'::jsonb, published_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
 CHECK(effective_to IS NULL OR effective_to > effective_from));

CREATE TABLE IF NOT EXISTS valorapesquisa.maturity_dimensions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, weight numeric(8,4) NOT NULL DEFAULT 1,
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('draft','active','inactive')),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code), CHECK(weight>0));

CREATE TABLE IF NOT EXISTS valorapesquisa.cognitive_concepts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(180) NOT NULL, description text NOT NULL, primary_dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id),
 related_dimension_ids uuid[] NOT NULL DEFAULT '{}', methodological_definition text NOT NULL, expected_evidence jsonb NOT NULL DEFAULT '[]',
 low_maturity_signs jsonb NOT NULL DEFAULT '[]', medium_maturity_signs jsonb NOT NULL DEFAULT '[]', high_maturity_signs jsonb NOT NULL DEFAULT '[]',
 associated_risks jsonb NOT NULL DEFAULT '[]', associated_opportunities jsonb NOT NULL DEFAULT '[]', possible_recommendations jsonb NOT NULL DEFAULT '[]',
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('draft','active','inactive')), version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));

CREATE TABLE IF NOT EXISTS valorapesquisa.cognitive_concept_relations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 source_concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), target_concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id),
 relation_type varchar(30) NOT NULL CHECK(relation_type IN ('probable_cause','impact','dependency','correlation','aggravating','mitigating','prerequisite')),
 intensity numeric(5,4) NOT NULL CHECK(intensity>0 AND intensity<=1), direction varchar(20) NOT NULL CHECK(direction IN ('positive','negative','bidirectional')),
 description text NOT NULL, interpretation_rule jsonb NOT NULL DEFAULT '{}', version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,source_concept_id,target_concept_id,relation_type));

CREATE TABLE IF NOT EXISTS valorapesquisa.official_questions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, text text NOT NULL, internal_description text NOT NULL DEFAULT '',
 response_type varchar(30) NOT NULL CHECK(response_type IN ('scale_1_5','multiple_choice','yes_no','qualitative_text','matrix','single_choice')),
 scale_json jsonb NOT NULL DEFAULT '{}', dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id),
 primary_concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),
 is_required boolean NOT NULL DEFAULT true, target_audience text[] NOT NULL DEFAULT '{}', assessed_maturity_level varchar(30),
 normalization_rule jsonb NOT NULL, status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('draft','active','inactive')),
 version integer NOT NULL DEFAULT 1, effective_from timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.official_question_options (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id) ON DELETE CASCADE,
 code varchar(50) NOT NULL, label text NOT NULL, normalized_value numeric(7,4), display_order integer NOT NULL DEFAULT 0,
 UNIQUE(question_id,code), CHECK(normalized_value IS NULL OR normalized_value BETWEEN 0 AND 100));

-- Corrige resíduos antes de fortalecer o contrato da tabela legada.
DO $mapping$ BEGIN
 IF to_regclass('valorapesquisa.question_concept_mappings') IS NOT NULL THEN
  UPDATE valorapesquisa.question_concept_mappings SET weight=1 WHERE weight IS NULL OR weight<=0;
  ALTER TABLE valorapesquisa.question_concept_mappings DROP CONSTRAINT IF EXISTS question_concept_mappings_weight_check;
  ALTER TABLE valorapesquisa.question_concept_mappings ADD CONSTRAINT question_concept_mappings_weight_check CHECK(weight>0) NOT VALID;
  ALTER TABLE valorapesquisa.question_concept_mappings VALIDATE CONSTRAINT question_concept_mappings_weight_check;
 END IF;
END $mapping$;
CREATE TABLE IF NOT EXISTS valorapesquisa.official_question_concepts (
 question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id) ON DELETE CASCADE,
 concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),
 is_primary boolean NOT NULL DEFAULT false, PRIMARY KEY(question_id,concept_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.scoring_rules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(180) NOT NULL, rule_json jsonb NOT NULL, status varchar(20) NOT NULL DEFAULT 'active',
 version integer NOT NULL DEFAULT 1, created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.maturity_levels (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(30) NOT NULL, name varchar(100) NOT NULL, minimum_score numeric(5,2) NOT NULL, maximum_score numeric(5,2) NOT NULL,
 description text NOT NULL, organizational_meaning text NOT NULL, typical_risks jsonb NOT NULL DEFAULT '[]', recommended_next_step text NOT NULL,
 display_order integer NOT NULL, UNIQUE(methodology_version_id,code), CHECK(minimum_score>=0 AND maximum_score<=100 AND maximum_score>minimum_score));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnosis_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(180) NOT NULL, audience text[] NOT NULL DEFAULT '{}', estimated_minutes integer NOT NULL CHECK(estimated_minutes>0),
 minimum_plan varchar(40) NOT NULL, enabled_deliverables jsonb NOT NULL DEFAULT '[]', scoring_rule_id uuid NOT NULL REFERENCES valorapesquisa.scoring_rules(id),
 dimensions_json jsonb NOT NULL DEFAULT '[]', status varchar(20) NOT NULL DEFAULT 'active', version integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnosis_template_questions (
 template_id uuid NOT NULL REFERENCES valorapesquisa.diagnosis_templates(id) ON DELETE CASCADE,
 question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id), display_order integer NOT NULL, is_required boolean NOT NULL DEFAULT true,
 PRIMARY KEY(template_id,question_id), UNIQUE(template_id,display_order));

-- Snapshot imutável: o diagnóstico publicado aponta para uma versão e seu conteúdo materializado.
DO $diagnosis_version$ BEGIN
 IF to_regclass('valorapesquisa.surveys') IS NOT NULL THEN
  ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS methodology_version_id uuid REFERENCES valorapesquisa.methodology_versions(id);
  ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS methodology_snapshot_json jsonb;
  ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS diagnosis_template_id uuid REFERENCES valorapesquisa.diagnosis_templates(id);
 END IF;
END $diagnosis_version$;

CREATE TABLE IF NOT EXISTS valorapesquisa.evidence_items_methodology (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), diagnosis_id uuid NOT NULL, question_id uuid NOT NULL REFERENCES valorapesquisa.official_questions(id),
 answer_id uuid NOT NULL, dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id), concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id),
 intensity numeric(7,4) NOT NULL CHECK(intensity BETWEEN 0 AND 100), polarity varchar(10) NOT NULL CHECK(polarity IN ('positive','negative','neutral')),
 confidence numeric(5,4) NOT NULL CHECK(confidence BETWEEN 0 AND 1), interpretation text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.recommendation_catalog (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, concept_id uuid NOT NULL REFERENCES valorapesquisa.cognitive_concepts(id), dimension_id uuid NOT NULL REFERENCES valorapesquisa.maturity_dimensions(id),
 trigger_condition jsonb NOT NULL CHECK(trigger_condition<>'{}'::jsonb), priority varchar(20) NOT NULL, description text NOT NULL, objective text NOT NULL,
 prerequisites jsonb NOT NULL DEFAULT '[]', mitigated_risks jsonb NOT NULL DEFAULT '[]', success_indicators jsonb NOT NULL DEFAULT '[]', suggested_actions jsonb NOT NULL DEFAULT '[]',
 status varchar(20) NOT NULL DEFAULT 'active', version integer NOT NULL DEFAULT 1, UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.recommendation_evidence (
 recommendation_id uuid NOT NULL REFERENCES valorapesquisa.recommendation_catalog(id), evidence_id uuid NOT NULL REFERENCES valorapesquisa.evidence_items_methodology(id),
 created_at timestamptz NOT NULL DEFAULT now(), PRIMARY KEY(recommendation_id,evidence_id));

INSERT INTO valorapesquisa.methodology_versions(code,version,name,status,effective_from,published_at,change_log)
VALUES('VALORA-2026.1',1,'Metodologia Valora Insight™ 2026.1','active','2026-01-01',now(),'Base cognitiva oficial inicial.') ON CONFLICT(code) DO NOTHING;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.maturity_dimensions(methodology_version_id,code,name,description,weight)
SELECT v.id,x.code,x.name,x.description,1 FROM v CROSS JOIN (VALUES
 ('clarity','Clareza Sistêmica','Propósito, papéis, responsabilidades e interfaces explícitos.'),('governance','Governança','Decisões, accountability, riscos e indicadores.'),
 ('leadership','Liderança','Contexto, direção e desenvolvimento.'),('culture_people','Cultura e Pessoas','Padrões, comunicação e capacidade humana.'),
 ('process_systems','Processos e Sistemas','Fluxos, tecnologia, repetibilidade e integração.'),('intelligence_learning','Inteligência e Aprendizagem','Evidências convertidas em decisão e evolução.'),
 ('sustainability','Sustentabilidade','Autonomia, resiliência e continuidade organizacional.')) x(code,name,description)
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,description=excluded.description,updated_at=now();
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), concepts(code,name,dimension,definition) AS (VALUES
 ('systemic_clarity','Clareza sistêmica','clarity','Compreensão compartilhada de propósito, papéis, critérios e interfaces.'),
 ('organizational_governance','Governança organizacional','governance','Sistema de direção, decisão, prestação de contas e supervisão.'),('leadership','Liderança','leadership','Capacidade de produzir contexto, direção e desenvolvimento.'),
 ('organizational_culture','Cultura organizacional','culture_people','Padrões compartilhados que orientam comportamentos.'),('people','Pessoas','culture_people','Condições para contribuição, desenvolvimento e pertencimento.'),
 ('processes','Processos','process_systems','Fluxos de valor explícitos, medidos e aprimorados.'),('systems','Sistemas','process_systems','Recursos técnicos e sociais integrados ao trabalho.'),
 ('organizational_learning','Aprendizagem organizacional','intelligence_learning','Capacidade de aprender com ciclos e evidências.'),('organizational_intelligence','Inteligência organizacional','intelligence_learning','Capacidade de converter evidência em decisão melhor.'),
 ('organizational_sustainability','Sustentabilidade organizacional','sustainability','Capacidade de sustentar resultados e adaptação no tempo.'),('organizational_autonomy','Autonomia organizacional','sustainability','Decisão distribuída com contexto, limites e responsabilidade.'),
 ('key_person_dependency','Dependência de pessoas específicas','sustainability','Concentração crítica de conhecimento ou decisão.'),('decision_making','Tomada de decisão','governance','Escolha rastreável por critérios e evidências.'),
 ('internal_communication','Comunicação interna','culture_people','Fluxo confiável de contexto, acordos e feedback.'),('indicators','Indicadores','intelligence_learning','Evidências quantitativas e qualitativas interpretadas em contexto.'),
 ('accountability','Accountability','governance','Assumir, prestar contas e aprender sobre compromissos.'),('organizational_development','Desenvolvimento organizacional','intelligence_learning','Transformação planejada da arquitetura organizacional.'))
INSERT INTO valorapesquisa.cognitive_concepts(methodology_version_id,code,name,description,primary_dimension_id,methodological_definition,expected_evidence,low_maturity_signs,medium_maturity_signs,high_maturity_signs,associated_risks,associated_opportunities,possible_recommendations)
SELECT v.id,c.code,c.name,c.definition,d.id,c.definition,'["práticas observáveis","registros recorrentes"]','["prática informal ou dependente"]','["prática definida, ainda irregular"]','["prática integrada, medida e aprendida"]','["descontinuidade","decisão sem evidência"]','["integração","aprendizagem"]','["instituir ciclo com responsável, indicador e revisão"]'
FROM v JOIN concepts c ON true JOIN valorapesquisa.maturity_dimensions d ON d.methodology_version_id=v.id AND d.code=c.dimension
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,methodological_definition=excluded.methodological_definition,updated_at=now();
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.maturity_levels(methodology_version_id,code,name,minimum_score,maximum_score,description,organizational_meaning,typical_risks,recommended_next_step,display_order)
SELECT v.id,x.* FROM v CROSS JOIN (VALUES
 ('initial','Inicial',0,19.99,'Práticas incipientes.','Alta dependência de iniciativas isoladas.','["descontinuidade"]','Estabelecer fundamentos explícitos.',1),
 ('structuring','Em estruturação',20,39.99,'Fundamentos em definição.','Existem iniciativas ainda pouco integradas.','["fragmentação"]','Formalizar papéis e rotinas.',2),
 ('developing','Em desenvolvimento',40,59.99,'Práticas em adoção.','Capacidades evoluem com consistência variável.','["execução irregular"]','Medir adoção e remover barreiras.',3),
 ('consistent','Consistente',60,74.99,'Práticas recorrentes.','A organização opera com previsibilidade.','["acomodação"]','Integrar capacidades e aprendizagem.',4),
 ('mature','Madura',75,89.99,'Práticas integradas.','Decisões e resultados são sustentáveis.','["otimização local"]','Ampliar adaptação sistêmica.',5),
 ('intelligent','Inteligente',90,100,'Práticas adaptativas.','O sistema aprende e evolui por evidências.','["excesso de confiança"]','Preservar aprendizagem e renovação.',6)) x(code,name,min,max,description,meaning,risks,next_step,display_order)
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,minimum_score=excluded.minimum_score,maximum_score=excluded.maximum_score;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.scoring_rules(methodology_version_id,code,name,rule_json)
SELECT id,'weighted-evidence-v1','Scoring ponderado por evidências','{"scale":"0-100","aggregation":"weighted_average","invalid":"ignore","zeroDenominator":"insufficient_evidence","confidence":"valid_required_ratio"}' FROM v ON CONFLICT(methodology_version_id,code) DO NOTHING;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), r AS (SELECT id FROM valorapesquisa.scoring_rules WHERE code='weighted-evidence-v1'), templates(code,name,audience,minutes,plan,deliverables) AS (VALUES
 ('essential','Diagnóstico Essencial',ARRAY['equipes'],15,'Free','["score","radar"]'::jsonb),('professional','Diagnóstico Profissional',ARRAY['organização'],30,'Professional','["score","radar","heatmap","action_plan"]'),
 ('executive','Diagnóstico Executivo',ARRAY['alta liderança'],25,'Professional','["executive_report","benchmark"]'),('leadership','Diagnóstico de Liderança',ARRAY['líderes'],20,'Professional','["score","insights"]'),
 ('governance','Diagnóstico de Governança',ARRAY['governança'],25,'Professional','["score","risks","recommendations"]'),('culture','Diagnóstico de Cultura',ARRAY['organização'],25,'Professional','["score","heatmap"]'),
 ('enterprise_units','Diagnóstico Enterprise por unidades',ARRAY['múltiplas unidades'],45,'Enterprise','["score","radar","heatmap","benchmark","action_plan","certificate"]'))
INSERT INTO valorapesquisa.diagnosis_templates(methodology_version_id,code,name,audience,estimated_minutes,minimum_plan,enabled_deliverables,scoring_rule_id)
SELECT v.id,t.code,t.name,t.audience,t.minutes,t.plan,t.deliverables,r.id FROM v CROSS JOIN r CROSS JOIN templates t
ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,estimated_minutes=excluded.estimated_minutes,enabled_deliverables=excluded.enabled_deliverables;

-- Uma pergunta oficial inicial por conceito garante cobertura canônica; novas versões são inseridas, nunca sobrescritas.
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.official_questions(methodology_version_id,code,text,internal_description,response_type,scale_json,dimension_id,primary_concept_id,weight,is_required,target_audience,normalization_rule,effective_from)
SELECT v.id,'VALORA_'||upper(c.code)||'_01','Em que medida '||lower(c.name)||' está formalizada, é praticada e revisada com evidências?',
 'Item basal oficial de '||c.name||'.','scale_1_5','{"minimum":1,"maximum":5,"labels":{"1":"não existe","5":"integrada e adaptativa"}}',c.primary_dimension_id,c.id,1,true,ARRAY['organização'],
 '{"type":"linear","minimum":1,"maximum":5,"outputMinimum":0,"outputMaximum":100}',v.effective_from
FROM v JOIN valorapesquisa.cognitive_concepts c ON c.methodology_version_id=v.id
ON CONFLICT(methodology_version_id,code) DO UPDATE SET text=excluded.text,internal_description=excluded.internal_description;
INSERT INTO valorapesquisa.official_question_concepts(question_id,concept_id,weight,is_primary)
SELECT q.id,q.primary_concept_id,1,true FROM valorapesquisa.official_questions q
ON CONFLICT(question_id,concept_id) DO UPDATE SET weight=1,is_primary=true;
WITH edges(source,target,type,direction,description) AS (VALUES
 ('systemic_clarity','organizational_governance','impact','positive','Baixa clareza sistêmica fragiliza a governança.'),
 ('organizational_governance','key_person_dependency','impact','negative','Governança fraca amplia dependência de pessoas específicas.'),
 ('indicators','decision_making','prerequisite','positive','Indicadores contextualizados qualificam a decisão.'),
 ('leadership','organizational_autonomy','impact','positive','Liderança que distribui contexto fortalece autonomia.'))
INSERT INTO valorapesquisa.cognitive_concept_relations(methodology_version_id,source_concept_id,target_concept_id,relation_type,intensity,direction,description,interpretation_rule)
SELECT s.methodology_version_id,s.id,t.id,e.type,.8,e.direction,e.description,'{"minimumEvidence":3,"causality":"hypothesis_only"}'
FROM edges e JOIN valorapesquisa.cognitive_concepts s ON s.code=e.source JOIN valorapesquisa.cognitive_concepts t ON t.code=e.target AND t.methodology_version_id=s.methodology_version_id
ON CONFLICT(methodology_version_id,source_concept_id,target_concept_id,relation_type) DO UPDATE SET description=excluded.description,interpretation_rule=excluded.interpretation_rule;
WITH v AS (SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1')
INSERT INTO valorapesquisa.recommendation_catalog(methodology_version_id,code,concept_id,dimension_id,trigger_condition,priority,description,objective,prerequisites,mitigated_risks,success_indicators,suggested_actions)
SELECT v.id,'REC_'||upper(c.code)||'_FOUNDATION',c.id,c.primary_dimension_id,'{"conceptScore":{"lessThan":60},"minimumEvidence":1}','high',
 'Estruturar '||lower(c.name)||' com responsabilidade e cadência de revisão.','Elevar a maturidade observável de '||lower(c.name)||'.','["responsável definido"]','["descontinuidade","dependência"]','["score do conceito","evidências recorrentes"]','["definir prática","registrar evidência","revisar resultado"]'
FROM v JOIN valorapesquisa.cognitive_concepts c ON c.methodology_version_id=v.id
ON CONFLICT(methodology_version_id,code) DO UPDATE SET trigger_condition=excluded.trigger_condition,description=excluded.description;

-- Catálogo fechado e concessão integral ao admin_valora.
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('methodology.read','Visualizar metodologia','Consulta a metodologia oficial versionada.','organizational_intelligence'),
 ('methodology.manage','Gerenciar metodologia','Publica e versiona a metodologia oficial.','organizational_intelligence'),
 ('dimensions.manage','Gerenciar dimensões','Administra dimensões metodológicas.','organizational_intelligence'),
 ('concepts.manage','Gerenciar conceitos','Administra o dicionário cognitivo.','organizational_intelligence'),
 ('cognitive_map.manage','Gerenciar mapa cognitivo','Administra relações cognitivas.','organizational_intelligence'),
 ('official_questions.manage','Gerenciar perguntas oficiais','Administra perguntas versionadas.','forms'),
 ('diagnosis_templates.manage','Gerenciar templates diagnósticos','Administra templates oficiais.','forms'),
 ('scoring_rules.manage','Gerenciar regras de score','Administra regras versionadas de scoring.','results'),
 ('maturity_levels.manage','Gerenciar níveis de maturidade','Administra faixas oficiais.','results'),
 ('recommendations.manage','Gerenciar recomendações','Administra catálogo baseado em evidências.','results')
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.code='admin_valora' AND p.code IN ('methodology.read','methodology.manage','dimensions.manage','concepts.manage','cognitive_map.manage','official_questions.manage','diagnosis_templates.manage','scoring_rules.manage','maturity_levels.manage','recommendations.manage')
ON CONFLICT(role_id,permission_id) DO NOTHING;

COMMIT;
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE TABLE IF NOT EXISTS valorapesquisa.api_keys(id uuid PRIMARY KEY DEFAULT gen_random_uuid());
-- Columns precede every constraint/index/use, including upgrades from legacy tables.
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS organization_id uuid;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS key_hash text;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS key_prefix text;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS scopes text[] DEFAULT '{}';
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS status text DEFAULT 'active';
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS expires_at timestamptz;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS last_used_at timestamptz;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS use_count bigint DEFAULT 0;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS created_by uuid;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS updated_at timestamptz DEFAULT now();
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS revoked_at timestamptz;
ALTER TABLE valorapesquisa.api_keys ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ux_api_keys_hash ON valorapesquisa.api_keys(key_hash) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.api_key_scopes(api_key_id uuid NOT NULL REFERENCES valorapesquisa.api_keys(id) ON DELETE CASCADE,scope text NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),PRIMARY KEY(api_key_id,scope));
CREATE TABLE IF NOT EXISTS valorapesquisa.integration_settings(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),provider text NOT NULL,status text NOT NULL DEFAULT 'disabled',configuration jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),UNIQUE(organization_id,provider));
CREATE TABLE IF NOT EXISTS valorapesquisa.webhook_subscriptions(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),name text NOT NULL,url text NOT NULL,secret_hash text NOT NULL,events text[] NOT NULL,status text NOT NULL DEFAULT 'active',max_attempts integer NOT NULL DEFAULT 6,last_sent_at timestamptz,last_error text,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.webhook_events(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),event_type text NOT NULL,aggregate_id uuid,payload jsonb NOT NULL,created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.webhook_delivery_attempts(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),webhook_id uuid NOT NULL REFERENCES valorapesquisa.webhook_subscriptions(id),event_id uuid NOT NULL REFERENCES valorapesquisa.webhook_events(id),attempt integer NOT NULL,status text NOT NULL,http_status integer,response_excerpt text,signature_prefix text,error text,next_attempt_at timestamptz,created_at timestamptz NOT NULL DEFAULT now(),completed_at timestamptz,UNIQUE(webhook_id,event_id,attempt));
CREATE INDEX IF NOT EXISTS ix_webhook_delivery_retry ON valorapesquisa.webhook_delivery_attempts(status,next_attempt_at);
CREATE TABLE IF NOT EXISTS valorapesquisa.integration_logs(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid REFERENCES valorapesquisa.organizations(id),api_key_id uuid REFERENCES valorapesquisa.api_keys(id),event_type text NOT NULL,status integer,endpoint text,scope_used text,correlation_id text,metadata jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_integration_logs_tenant_date ON valorapesquisa.integration_logs(organization_id,created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.email_templates(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid REFERENCES valorapesquisa.organizations(id),code text NOT NULL,subject text NOT NULL,body_html text NOT NULL,is_active boolean NOT NULL DEFAULT true,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.email_outbox(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),template_code text NOT NULL,recipient text NOT NULL,payload jsonb NOT NULL DEFAULT '{}',status text NOT NULL DEFAULT 'pending',attempts integer NOT NULL DEFAULT 0,next_attempt_at timestamptz NOT NULL DEFAULT now(),last_error text,created_at timestamptz NOT NULL DEFAULT now(),sent_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_email_outbox_dispatch ON valorapesquisa.email_outbox(status,next_attempt_at);
CREATE TABLE IF NOT EXISTS valorapesquisa.import_batches(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),type text NOT NULL,format text NOT NULL,checksum text NOT NULL,status text NOT NULL DEFAULT 'pending',total_rows integer NOT NULL DEFAULT 0,valid_rows integer NOT NULL DEFAULT 0,error_rows integer NOT NULL DEFAULT 0,created_by uuid,created_at timestamptz NOT NULL DEFAULT now(),completed_at timestamptz,rolled_back_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.import_batch_errors(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),batch_id uuid NOT NULL REFERENCES valorapesquisa.import_batches(id) ON DELETE CASCADE,row_number integer NOT NULL,field text,error_code text NOT NULL,message text NOT NULL,raw_data jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.external_data_sources(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),type text NOT NULL,name text NOT NULL,configuration jsonb NOT NULL DEFAULT '{}',status text NOT NULL DEFAULT 'disabled',last_sync_at timestamptz,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now());
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('integrations.read','Consultar integrações','Consultar integrações','enterprise'),('integrations.manage','Gerenciar integrações','Configurar integrações','enterprise'),('api_keys.read','Consultar API Keys','Consultar chaves','enterprise'),('api_keys.manage','Gerenciar API Keys','Gerenciar chaves','enterprise'),('webhooks.read','Consultar webhooks','Consultar webhooks','enterprise'),('webhooks.manage','Gerenciar webhooks','Gerenciar webhooks','enterprise'),('powerbi.read','Consultar BI','Consultar BI','enterprise'),('powerbi.manage','Gerenciar BI','Gerenciar BI','enterprise'),('imports.read','Consultar importações','Consultar lotes','enterprise'),('imports.manage','Gerenciar importações','Gerenciar lotes','enterprise'),('email_templates.manage','Gerenciar templates','Gerenciar templates de e-mail','enterprise'),('integration_logs.read','Consultar logs','Consultar logs de integração','enterprise') ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
INSERT INTO valorapesquisa.schema_migrations(version,checksum) VALUES('2026_08_professional_integrations','sha256:professional-integrations-v1') ON CONFLICT(version) DO NOTHING;
COMMIT;

-- Valora One-on-One™: contrato canônico e evolução idempotente de instalações parciais.
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_sessions(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id));
ALTER TABLE valorapesquisa.one_on_one_sessions
 ADD COLUMN IF NOT EXISTS leader_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS participant_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS diagnostic_id uuid,
 ADD COLUMN IF NOT EXISTS result_id uuid,
 ADD COLUMN IF NOT EXISTS title varchar(200),
 ADD COLUMN IF NOT EXISTS purpose text,
 ADD COLUMN IF NOT EXISTS scheduled_at timestamptz,
 ADD COLUMN IF NOT EXISTS started_at timestamptz,
 ADD COLUMN IF NOT EXISTS completed_at timestamptz,
 ADD COLUMN IF NOT EXISTS canceled_at timestamptz,
 ADD COLUMN IF NOT EXISTS duration_minutes integer,
 ADD COLUMN IF NOT EXISTS summary text,
 ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT '',
 ADD COLUMN IF NOT EXISTS ai_summary text,
 ADD COLUMN IF NOT EXISTS created_by_user_id uuid,
 ADD COLUMN IF NOT EXISTS private_notes text,
 ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now(),
 ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(),
 ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.one_on_one_sessions
 SET leader_user_id=COALESCE(leader_user_id,facilitator_user_id),
     title=COALESCE(NULLIF(btrim(title),''),NULLIF(btrim(agenda),''),'Sessão individual'),
     purpose=COALESCE(NULLIF(btrim(purpose),''),NULLIF(btrim(objective),''),'Acompanhamento organizacional'),
     duration_minutes=COALESCE(duration_minutes,60), metadata_json=COALESCE(metadata_json,'{}'::jsonb),
     created_at=COALESCE(created_at,now()), updated_at=COALESCE(updated_at,created_at,now());
CREATE INDEX IF NOT EXISTS ix_one_on_one_sessions_tenant_schedule ON valorapesquisa.one_on_one_sessions(organization_id,scheduled_at) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_one_on_one_sessions_leader ON valorapesquisa.one_on_one_sessions(organization_id,leader_user_id,status) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_session_topics (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),
 theme varchar(160) NOT NULL, observation text, evidence text, correlation text, probable_cause text, organizational_impact text, priority varchar(30), display_order integer NOT NULL DEFAULT 0,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_one_on_one_topics_session ON valorapesquisa.one_on_one_session_topics(organization_id,session_id,display_order) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_session_notes (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id), author_user_id uuid REFERENCES valorapesquisa.users(id),
 content text NOT NULL, visibility varchar(20) NOT NULL DEFAULT 'reportable', metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_one_on_one_session_notes_scope ON valorapesquisa.one_on_one_session_notes(organization_id,session_id,visibility) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_action_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id), action_id uuid REFERENCES valorapesquisa.valora_actions(id),
 description text NOT NULL, evidence_reference text NOT NULL, owner_user_id uuid REFERENCES valorapesquisa.users(id), due_at timestamptz, completed_at timestamptz, status varchar(30) NOT NULL DEFAULT 'open',
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_one_on_one_action_items_due ON valorapesquisa.one_on_one_action_items(organization_id,status,due_at) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_development_profiles (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), leader_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), strengths text, risks text, evidence_summary text,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(organization_id,leader_user_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_development_plans (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), profile_id uuid NOT NULL REFERENCES valorapesquisa.leadership_development_profiles(id), title varchar(200) NOT NULL, purpose text NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'active', starts_at timestamptz, target_at timestamptz, completed_at timestamptz, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_development_plan_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), plan_id uuid NOT NULL REFERENCES valorapesquisa.leadership_development_plans(id), evidence_reference text NOT NULL, description text NOT NULL,
 owner_user_id uuid REFERENCES valorapesquisa.users(id), status varchar(30) NOT NULL DEFAULT 'open', due_at timestamptz, completed_at timestamptz, progress numeric(5,2) NOT NULL DEFAULT 0, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.follow_up_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), session_id uuid REFERENCES valorapesquisa.one_on_one_sessions(id), plan_item_id uuid REFERENCES valorapesquisa.leadership_development_plan_items(id), event_type varchar(50) NOT NULL, description text NOT NULL,
 occurred_at timestamptz NOT NULL DEFAULT now(), created_by uuid REFERENCES valorapesquisa.users(id), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_metrics_snapshots (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), profile_id uuid NOT NULL REFERENCES valorapesquisa.leadership_development_profiles(id), metric_code varchar(100) NOT NULL, value numeric(12,4), evidence_count integer NOT NULL DEFAULT 0, limitation text,
 captured_at timestamptz NOT NULL DEFAULT now(), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_leadership_metrics_history ON valorapesquisa.leadership_metrics_snapshots(organization_id,profile_id,captured_at DESC) WHERE deleted_at IS NULL;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('one_on_one.read','Visualizar One-on-One','Consulta sessões autorizadas da organização.','organizational_intelligence'),
('one_on_one.manage','Gerenciar One-on-One','Atualiza e cancela sessões da organização.','organizational_intelligence'),
('one_on_one.schedule','Agendar One-on-One','Agenda sessões e pautas iniciais.','organizational_intelligence'),
('one_on_one.notes.manage','Gerenciar notas de One-on-One','Registra notas respeitando sua visibilidade.','organizational_intelligence'),
('one_on_one.feedback.manage','Gerenciar devolutivas de One-on-One','Registra decisões e devolutivas reportáveis.','organizational_intelligence'),
('leadership_development.read','Visualizar desenvolvimento de lideranças','Consulta perfis e evolução autorizados.','organizational_intelligence'),
('leadership_development.manage','Gerenciar desenvolvimento de lideranças','Mantém planos sustentados por evidências.','organizational_intelligence'),
('evolution.manage','Gerenciar jornada de evolução','Mantém marcos da evolução organizacional.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at)
SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN
('one_on_one.read','one_on_one.manage','one_on_one.schedule','one_on_one.notes.manage','one_on_one.feedback.manage','leadership_development.read','leadership_development.manage','evolution.read','evolution.manage','action.read','action.manage')
ON CONFLICT(role_id,permission_id) DO NOTHING;
-- Canonical permissions for the complete organizational-diagnostic workflow.
-- Kept as a final, idempotent migration so installations upgraded from any prior
-- script revision receive the same authorization vocabulary as the application.
INSERT INTO valorapesquisa.permissions(code,name,description,module_code)
VALUES
 ('diagnostics.read','Consultar diagnósticos','Consulta diagnósticos organizacionais e seus ciclos.','surveys'),
 ('diagnostics.manage','Gerenciar diagnósticos','Cria, publica, encerra e reprocessa diagnósticos.','surveys'),
 ('forms.manage','Gerenciar formulários','Gerencia formulários e perguntas do diagnóstico.','forms'),
 ('responses.submit','Enviar respostas','Registra respostas por um link público autorizado.','responses'),
 ('results.manage','Gerenciar resultados','Calcula e administra snapshots históricos de resultados.','results'),
 ('intelligence.process','Processar inteligência','Agenda e reprocessa a inteligência baseada em evidências.','organizational_intelligence'),
 ('certificates.validate','Validar certificados','Valida publicamente a autenticidade de certificados.','certificates'),
 ('administration.read','Consultar administração','Consulta a operação auditável da plataforma.','operations'),
 ('administration.manage','Gerenciar administração','Administra a operação e trata falhas do fluxo.','operations')
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 description=EXCLUDED.description,
 module_code=EXCLUDED.module_code,
 updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at)
SELECT role.id,permission.id,now()
FROM valorapesquisa.roles role
CROSS JOIN valorapesquisa.permissions permission
WHERE role.code='admin_valora'
  AND role.deleted_at IS NULL
  AND permission.code IN (
    'diagnostics.read','diagnostics.manage','forms.manage','responses.submit','results.manage',
    'intelligence.process','certificates.validate','administration.read','administration.manage')
ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Valora Decision Center™: governança organizacional baseada em evidências.
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_governance_cycles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),name text NOT NULL,period_label text NOT NULL,primary_diagnostic_id uuid,status text NOT NULL DEFAULT 'active' CHECK(status IN('draft','active','review','closed','canceled')),indicators_summary jsonb NOT NULL DEFAULT '{}',report_id uuid,learning_summary text,opened_at timestamptz NOT NULL DEFAULT now(),closed_at timestamptz,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_decisions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),diagnostic_id uuid,result_id uuid,governance_cycle_id uuid REFERENCES valorapesquisa.organizational_governance_cycles(id),title text NOT NULL,summary text NOT NULL,decision_type text NOT NULL DEFAULT 'manual',priority text NOT NULL DEFAULT 'medium' CHECK(priority IN('critical','high','medium','low')),status text NOT NULL DEFAULT 'draft' CHECK(status IN('draft','proposed','approved','in_execution','completed','canceled')),impact_level text NOT NULL DEFAULT 'medium',evidence_summary text NOT NULL,expected_outcome text NOT NULL,responsible_user_id uuid,decided_by_user_id uuid,decided_at timestamptz,due_at timestamptz,completed_at timestamptz,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_decision_evidence_links (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),decision_id uuid NOT NULL REFERENCES valorapesquisa.organizational_decisions(id),evidence_type text NOT NULL,evidence_id uuid,evidence_summary text NOT NULL,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(decision_id,evidence_type,evidence_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_decision_action_links (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),decision_id uuid NOT NULL REFERENCES valorapesquisa.organizational_decisions(id),action_id uuid NOT NULL,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(decision_id,action_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.organizational_decision_participants (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),decision_id uuid NOT NULL REFERENCES valorapesquisa.organizational_decisions(id),user_id uuid NOT NULL,participant_role text NOT NULL DEFAULT 'participant',created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(decision_id,user_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.intelligent_alerts (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),source_type text NOT NULL,source_id uuid,alert_type text NOT NULL,severity text NOT NULL CHECK(severity IN('critical','high','medium','low','info')),title text NOT NULL,message text NOT NULL,evidence_summary text NOT NULL,related_index_code text,related_dimension text,status text NOT NULL DEFAULT 'open' CHECK(status IN('open','acknowledged','in_progress','resolved','dismissed')),generated_by text NOT NULL DEFAULT 'rules_engine',assigned_to_user_id uuid,acknowledged_at timestamptz,resolved_at timestamptz,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.intelligent_alert_events (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),alert_id uuid NOT NULL REFERENCES valorapesquisa.intelligent_alerts(id),event_type text NOT NULL,performed_by_user_id uuid,evidence_summary text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.metric_snapshots (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),diagnostic_id uuid,result_id uuid,metric_code text NOT NULL,metric_name text NOT NULL,metric_group text NOT NULL CHECK(metric_group IN('strategic','tactical','operational')),score numeric(12,4),previous_score numeric(12,4),delta numeric(12,4),level text NOT NULL,trend text NOT NULL CHECK(trend IN('consistent_evolution','gradual_evolution','stability','regression','oscillation','insufficient_sample')),evidence_summary text NOT NULL,calculated_at timestamptz NOT NULL DEFAULT now(),metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.metric_targets (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),metric_code text NOT NULL,target_score numeric(12,4) NOT NULL,period_start date,period_end date,evidence_summary text NOT NULL,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.metric_thresholds (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid REFERENCES valorapesquisa.organizations(id),metric_code text NOT NULL,warning_delta numeric(12,4),critical_delta numeric(12,4),minimum_sample integer NOT NULL DEFAULT 1 CHECK(minimum_sample>0),evidence_rule text NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.governance_meetings (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),cycle_id uuid REFERENCES valorapesquisa.organizational_governance_cycles(id),title text NOT NULL,agenda text NOT NULL,status text NOT NULL DEFAULT 'scheduled' CHECK(status IN('scheduled','in_progress','completed','canceled')),scheduled_at timestamptz NOT NULL,participants_summary text,evidence_summary text,minutes_summary text,next_steps text,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.governance_meeting_items (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),meeting_id uuid NOT NULL REFERENCES valorapesquisa.governance_meetings(id),item_type text NOT NULL,title text NOT NULL,description text,evidence_summary text,position integer NOT NULL DEFAULT 0,created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.governance_meeting_decisions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),meeting_id uuid NOT NULL REFERENCES valorapesquisa.governance_meetings(id),decision_id uuid REFERENCES valorapesquisa.organizational_decisions(id),decision_summary text NOT NULL,evidence_summary text NOT NULL,created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.governance_review_notes (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),cycle_id uuid REFERENCES valorapesquisa.organizational_governance_cycles(id),meeting_id uuid REFERENCES valorapesquisa.governance_meetings(id),note_type text NOT NULL,note text NOT NULL,evidence_summary text NOT NULL,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_decisions_org_status ON valorapesquisa.organizational_decisions(organization_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_alerts_org_status_severity ON valorapesquisa.intelligent_alerts(organization_id,status,severity) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_metric_snapshots_org_code_date ON valorapesquisa.metric_snapshots(organization_id,metric_code,calculated_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_cycles_org_status ON valorapesquisa.organizational_governance_cycles(organization_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_governance_meetings_org_date ON valorapesquisa.governance_meetings(organization_id,scheduled_at DESC) WHERE deleted_at IS NULL;
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('decision_center.read','Consultar Decision Center','Consulta a visão executiva baseada em evidências.','organizational_intelligence'),('decisions.read','Consultar decisões','Consulta decisões organizacionais rastreáveis.','organizational_intelligence'),('decisions.manage','Gerenciar decisões','Registra e acompanha decisões humanas.','organizational_intelligence'),('decisions.approve','Aprovar decisões','Registra aprovação humana de decisões.','organizational_intelligence'),('alerts.read','Consultar alertas','Consulta alertas sustentados por evidências.','organizational_intelligence'),('alerts.manage','Gerenciar alertas','Atribui e acompanha alertas.','organizational_intelligence'),('alerts.resolve','Resolver alertas','Registra resolução humana de alertas.','organizational_intelligence'),('indicators.read','Consultar indicadores','Consulta indicadores e limitações de confiança.','organizational_intelligence'),('indicators.manage','Gerenciar indicadores','Configura metas e limites com evidência.','organizational_intelligence'),('governance.read','Consultar governança','Consulta ciclos de governança organizacional.','organizational_intelligence'),('governance.manage','Gerenciar governança','Gerencia ciclos e aprendizados.','organizational_intelligence'),('governance.meetings.read','Consultar reuniões de governança','Consulta pautas e atas autorizadas.','organizational_intelligence'),('governance.meetings.manage','Gerenciar reuniões de governança','Registra reuniões e encaminhamentos.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at) SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN('decision_center.read','decisions.read','decisions.manage','decisions.approve','alerts.read','alerts.manage','alerts.resolve','indicators.read','indicators.manage','governance.read','governance.manage','governance.meetings.read','governance.meetings.manage') ON CONFLICT(role_id,permission_id) DO NOTHING;

-- Valora Methodology Studio™: catálogo oficial versionado e governado.
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS version_number integer;
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS is_official boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS published_by_user_id uuid;
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE valorapesquisa.methodology_versions ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.methodology_versions SET version_number=COALESCE(version_number,version,1),status=CASE status WHEN 'active' THEN 'published' WHEN 'retired' THEN 'archived' ELSE status END;
ALTER TABLE valorapesquisa.methodology_versions ALTER COLUMN version_number SET NOT NULL;
ALTER TABLE valorapesquisa.methodology_versions ALTER COLUMN effective_from DROP NOT NULL;
ALTER TABLE valorapesquisa.methodology_versions DROP CONSTRAINT IF EXISTS methodology_versions_status_check;
ALTER TABLE valorapesquisa.methodology_versions ADD CONSTRAINT methodology_versions_status_check CHECK(status IN('draft','published','archived')) NOT VALID;

CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_publications(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),publication_number integer NOT NULL,published_at timestamptz NOT NULL DEFAULT now(),published_by_user_id uuid,justification text NOT NULL,snapshot_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(methodology_version_id,publication_number));
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS description text NOT NULL DEFAULT '';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS methodology_version_id uuid REFERENCES valorapesquisa.methodology_versions(id);
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS concept_type varchar(40) NOT NULL DEFAULT 'organizational';
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS parent_concept_id uuid REFERENCES valorapesquisa.methodology_concepts(id);
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS is_active boolean NOT NULL DEFAULT true;
ALTER TABLE valorapesquisa.methodology_concepts ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}';
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_concept_relationships(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),source_concept_id uuid NOT NULL REFERENCES valorapesquisa.methodology_concepts(id),target_concept_id uuid NOT NULL REFERENCES valorapesquisa.methodology_concepts(id),relationship_type varchar(50) NOT NULL,influence_weight numeric(6,4) NOT NULL DEFAULT 1 CHECK(influence_weight>0),rationale text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,source_concept_id,target_concept_id,relationship_type));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_dimensions(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(80) NOT NULL,name varchar(160) NOT NULL,description text NOT NULL,default_weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(default_weight>0),status varchar(20) NOT NULL DEFAULT 'active',metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_indices(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(20) NOT NULL,name varchar(180) NOT NULL,description text NOT NULL,scale_min numeric(8,2) NOT NULL DEFAULT 0,scale_max numeric(8,2) NOT NULL DEFAULT 100,level_1_label varchar(60) NOT NULL,level_1_min numeric(8,2) NOT NULL,level_1_max numeric(8,2) NOT NULL,level_2_label varchar(60) NOT NULL,level_2_min numeric(8,2) NOT NULL,level_2_max numeric(8,2) NOT NULL,level_3_label varchar(60) NOT NULL,level_3_min numeric(8,2) NOT NULL,level_3_max numeric(8,2) NOT NULL,level_4_label varchar(60) NOT NULL,level_4_min numeric(8,2) NOT NULL,level_4_max numeric(8,2) NOT NULL,calculation_strategy varchar(80) NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code),CHECK(scale_max>scale_min));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_index_rules(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_index_id uuid NOT NULL REFERENCES valorapesquisa.methodology_indices(id),code varchar(80) NOT NULL,rule_type varchar(40) NOT NULL,rule_json jsonb NOT NULL,weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),purpose text NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_index_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_question_bank(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(100) NOT NULL,question_text text NOT NULL,description text NOT NULL DEFAULT '',question_type varchar(40) NOT NULL,answer_scale jsonb NOT NULL DEFAULT '{}',default_weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(default_weight>0),is_required boolean NOT NULL DEFAULT true,is_official boolean NOT NULL DEFAULT true,status varchar(20) NOT NULL DEFAULT 'active',metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_question_mappings(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),question_id uuid NOT NULL REFERENCES valorapesquisa.methodology_question_bank(id),concept_id uuid REFERENCES valorapesquisa.methodology_concepts(id),dimension_id uuid REFERENCES valorapesquisa.methodology_dimensions(id),index_id uuid REFERENCES valorapesquisa.methodology_indices(id),weight numeric(8,4) NOT NULL DEFAULT 1 CHECK(weight>0),mapping_type varchar(30) NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,CHECK(concept_id IS NOT NULL OR dimension_id IS NOT NULL OR index_id IS NOT NULL));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_inference_rules(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(100) NOT NULL,name varchar(160) NOT NULL,origin text NOT NULL,purpose text NOT NULL,condition_json jsonb NOT NULL,result_json jsonb NOT NULL,minimum_evidence integer NOT NULL DEFAULT 1 CHECK(minimum_evidence>0),status varchar(20) NOT NULL DEFAULT 'active',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_output_schemas(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(100) NOT NULL,name varchar(160) NOT NULL,schema_json jsonb NOT NULL,status varchar(20) NOT NULL DEFAULT 'active',version_number integer NOT NULL DEFAULT 1,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code,version_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_guardrails(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(100) NOT NULL,name varchar(160) NOT NULL,description text NOT NULL,severity varchar(20) NOT NULL DEFAULT 'blocking',rule_json jsonb NOT NULL DEFAULT '{}',is_active boolean NOT NULL DEFAULT true,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_prompt_templates(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(100) NOT NULL,name varchar(160) NOT NULL,purpose text NOT NULL,system_prompt text NOT NULL,user_prompt_template text NOT NULL,output_schema_code varchar(100) NOT NULL,guardrail_code varchar(100) NOT NULL,status varchar(20) NOT NULL DEFAULT 'active',version_number integer NOT NULL DEFAULT 1,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code,version_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_report_templates(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),code varchar(100) NOT NULL,name varchar(160) NOT NULL,purpose text NOT NULL,template text NOT NULL,output_schema_code varchar(100) NOT NULL,status varchar(20) NOT NULL DEFAULT 'active',version_number integer NOT NULL DEFAULT 1,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(methodology_version_id,code,version_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_change_log(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),entity_type varchar(80) NOT NULL,entity_id uuid,operation varchar(30) NOT NULL,before_json jsonb,after_json jsonb,justification text NOT NULL,changed_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_methodology_settings(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),allowed_settings_json jsonb NOT NULL DEFAULT '{}',is_active boolean NOT NULL DEFAULT true,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(organization_id,methodology_version_id));
DO $diagnostic_methodology$ BEGIN IF to_regclass('valorapesquisa.surveys') IS NOT NULL THEN ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS question_version integer NOT NULL DEFAULT 1; ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS scoring_strategy varchar(80) NOT NULL DEFAULT 'weighted_average_v1'; ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS prompt_template_version integer NOT NULL DEFAULT 1; END IF; END $diagnostic_methodology$;
CREATE INDEX IF NOT EXISTS ix_methodology_versions_official ON valorapesquisa.methodology_versions(is_official,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_methodology_questions_version ON valorapesquisa.methodology_question_bank(methodology_version_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_methodology_mappings_question ON valorapesquisa.methodology_question_mappings(question_id) WHERE deleted_at IS NULL;

CREATE OR REPLACE FUNCTION valorapesquisa.validate_methodology_version(p_version_id uuid) RETURNS TABLE(code text,severity text,entity text,message text) LANGUAGE sql STABLE AS $$
 SELECT 'QUESTION_WITHOUT_CONCEPT','critical','question',q.code||' não possui conceito.' FROM valorapesquisa.methodology_question_bank q WHERE q.methodology_version_id=p_version_id AND q.is_official AND q.deleted_at IS NULL AND NOT EXISTS(SELECT 1 FROM valorapesquisa.methodology_question_mappings m WHERE m.question_id=q.id AND m.concept_id IS NOT NULL AND m.deleted_at IS NULL)
 UNION ALL SELECT 'QUESTION_WITHOUT_INDEX_OR_DIMENSION','critical','question',q.code||' não possui índice ou dimensão.' FROM valorapesquisa.methodology_question_bank q WHERE q.methodology_version_id=p_version_id AND q.is_official AND q.deleted_at IS NULL AND NOT EXISTS(SELECT 1 FROM valorapesquisa.methodology_question_mappings m WHERE m.question_id=q.id AND (m.index_id IS NOT NULL OR m.dimension_id IS NOT NULL) AND m.deleted_at IS NULL)
 UNION ALL SELECT 'INVALID_WEIGHT','critical','question',q.code||' possui peso inválido.' FROM valorapesquisa.methodology_question_bank q WHERE q.methodology_version_id=p_version_id AND q.deleted_at IS NULL AND q.default_weight<=0
 UNION ALL SELECT 'INDEX_WITHOUT_LEVELS','critical','index',i.code||' possui faixas inválidas.' FROM valorapesquisa.methodology_indices i WHERE i.methodology_version_id=p_version_id AND i.deleted_at IS NULL AND NOT(i.level_1_min<=i.level_1_max AND i.level_2_min<=i.level_2_max AND i.level_3_min<=i.level_3_max AND i.level_4_min<=i.level_4_max)
 UNION ALL SELECT 'PROMPT_WITHOUT_GUARDRAIL','critical','prompt',p.code||' não possui guardrail válido.' FROM valorapesquisa.methodology_prompt_templates p WHERE p.methodology_version_id=p_version_id AND p.deleted_at IS NULL AND NOT EXISTS(SELECT 1 FROM valorapesquisa.methodology_guardrails g WHERE g.methodology_version_id=p.methodology_version_id AND g.code=p.guardrail_code AND g.is_active AND g.deleted_at IS NULL)
 UNION ALL SELECT 'PROMPT_WITHOUT_SCHEMA','critical','prompt',p.code||' não possui schema válido.' FROM valorapesquisa.methodology_prompt_templates p WHERE p.methodology_version_id=p_version_id AND p.deleted_at IS NULL AND NOT EXISTS(SELECT 1 FROM valorapesquisa.methodology_output_schemas s WHERE s.methodology_version_id=p.methodology_version_id AND s.code=p.output_schema_code AND s.deleted_at IS NULL)
 UNION ALL SELECT 'INFERENCE_WITHOUT_PROVENANCE','critical','inference',r.code||' não informa origem e finalidade.' FROM valorapesquisa.methodology_inference_rules r WHERE r.methodology_version_id=p_version_id AND r.deleted_at IS NULL AND (btrim(r.origin)='' OR btrim(r.purpose)='') $$;
CREATE OR REPLACE FUNCTION valorapesquisa.publish_methodology_version(p_version_id uuid,p_actor_id uuid,p_justification text) RETURNS void LANGUAGE plpgsql AS $$ BEGIN
 IF EXISTS(SELECT 1 FROM valorapesquisa.validate_methodology_version(p_version_id) WHERE severity='critical') THEN RAISE EXCEPTION 'Versão metodológica inconsistente'; END IF;
 IF NOT EXISTS(SELECT 1 FROM valorapesquisa.methodology_versions WHERE id=p_version_id AND status='draft' AND deleted_at IS NULL) THEN RAISE EXCEPTION 'Somente versões draft podem ser publicadas'; END IF;
 UPDATE valorapesquisa.methodology_versions SET status='archived',is_official=false,updated_at=now() WHERE is_official AND status='published' AND id<>p_version_id;
 UPDATE valorapesquisa.methodology_versions SET status='published',is_official=true,published_at=now(),published_by_user_id=p_actor_id,updated_at=now() WHERE id=p_version_id;
 INSERT INTO valorapesquisa.methodology_publications(methodology_version_id,publication_number,published_by_user_id,justification,snapshot_json) SELECT id,COALESCE((SELECT max(publication_number)+1 FROM valorapesquisa.methodology_publications WHERE methodology_version_id=p_version_id),1),p_actor_id,p_justification,metadata_json FROM valorapesquisa.methodology_versions WHERE id=p_version_id;
 INSERT INTO valorapesquisa.methodology_change_log(methodology_version_id,entity_type,entity_id,operation,after_json,justification,changed_by_user_id) VALUES(p_version_id,'version',p_version_id,'publish',jsonb_build_object('status','published'),p_justification,p_actor_id); END $$;

-- Base oficial inicial: índices, conceitos, perguntas mapeadas, guardrails, schemas e prompts.
UPDATE valorapesquisa.methodology_versions SET is_official=true,status='published',description=COALESCE(description,'Metodologia oficial Valora Group.'),metadata_json=metadata_json||'{"owner":"Valora Group","immutable":true}'::jsonb WHERE code='VALORA-2026.1';
WITH v AS(SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), x(code,name) AS(VALUES ('IMO','Índice de Maturidade Organizacional'),('ICS','Índice de Clareza Sistêmica'),('IIO','Índice de Inteligência Organizacional'),('IGO','Índice de Governança Organizacional'),('ICO','Índice de Cultura Organizacional'),('ILI','Índice de Liderança'),('IPO','Índice de Pessoas'),('IDO','Índice de Desenvolvimento Organizacional'),('IAC','Índice de Accountability'),('IAR','Índice de Autonomia Responsável'),('IIS','Índice de Integração Sistêmica'),('ISO','Índice de Sustentabilidade Organizacional')) INSERT INTO valorapesquisa.methodology_indices(methodology_version_id,code,name,description,level_1_label,level_1_min,level_1_max,level_2_label,level_2_min,level_2_max,level_3_label,level_3_min,level_3_max,level_4_label,level_4_min,level_4_max,calculation_strategy) SELECT v.id,x.code,x.name,'Índice oficial Valora™, calculado exclusivamente por evidências versionadas.','Inicial',0,25,'Estruturante',26,50,'Integrado',51,75,'Maduro',76,100,'weighted_evidence_v1' FROM v CROSS JOIN x ON CONFLICT(methodology_version_id,code) DO UPDATE SET name=excluded.name,updated_at=now();
WITH v AS(SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), x(code,name) AS(VALUES ('clarity','Clareza'),('governance','Governança'),('leadership','Liderança'),('culture','Cultura'),('people','Pessoas'),('systems','Sistemas'),('organizational_intelligence','Inteligência organizacional'),('organizational_development','Desenvolvimento organizacional'),('sustainability','Sustentabilidade'),('accountability','Accountability'),('responsible_autonomy','Autonomia responsável'),('systemic_integration','Integração sistêmica'),('organizational_learning','Aprendizagem organizacional'),('decision_making','Tomada de decisão'),('key_person_dependency','Dependência de pessoas específicas'),('organizational_maturity','Maturidade organizacional')) INSERT INTO valorapesquisa.methodology_concepts(methodology_version_id,code,name,definition,description,pillar,strategic_purpose,evolution_guidance,methodology_version,status,version,display_order) SELECT v.id,x.code,x.name,'Conceito canônico da metodologia Valora™.','Conceito canônico da metodologia Valora™.','Metodologia Oficial','Orientar diagnóstico sistêmico baseado em evidências.','Evoluir por ciclos verificáveis.','VALORA-2026.1','active',1,row_number() over() FROM v CROSS JOIN x ON CONFLICT(code) DO UPDATE SET methodology_version_id=excluded.methodology_version_id,name=excluded.name,updated_at=now();
WITH v AS(SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), x(code,name,description) AS(VALUES ('no_fabrication','Não inventar dados','Nunca completar lacunas com fatos inventados.'),('evidence_required','Não interpretar sem evidência','Toda interpretação deve apontar evidências.'),('comparison_basis','Não comparar sem base','Comparações exigem base compatível.'),('no_blame','Não culpar pessoas','Interpretar o sistema, não buscar culpados.'),('no_moral_judgment','Não emitir julgamento moral','Evitar julgamento moral ou individual.'),('human_decision','Não substituir decisão humana','Recomendações apoiam decisão humana.'),('insufficient_evidence','Indicar limitação','Declarar evidência insuficiente.'),('systemic_logic','Preservar lógica sistêmica','Manter contexto e relações sistêmicas.'),('privacy','Preservar privacidade','Não expor dados individuais indevidos.')) INSERT INTO valorapesquisa.methodology_guardrails(methodology_version_id,code,name,description,rule_json) SELECT v.id,x.code,x.name,x.description,jsonb_build_object('required',true) FROM v CROSS JOIN x ON CONFLICT(methodology_version_id,code) DO UPDATE SET description=excluded.description,updated_at=now();
WITH v AS(SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1') INSERT INTO valorapesquisa.methodology_output_schemas(methodology_version_id,code,name,schema_json) SELECT id,'insight_v1','Insight Valora v1','{"type":"object","required":["evidence","limitations","recommendations"]}' FROM v ON CONFLICT(methodology_version_id,code,version_number) DO NOTHING;
WITH v AS(SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), x(code,name,purpose) AS(VALUES ('diagnostic','Diagnóstico','Interpretar diagnóstico'),('evidence','Evidências','Consolidar evidence pack'),('insights','Insights','Gerar insights'),('executive_report','Relatório executivo','Gerar síntese executiva'),('action_plan','Plano de ação','Propor evolução'),('benchmark','Benchmark','Comparar bases compatíveis'),('heatmap','Heatmap','Explicar concentrações'),('one_on_one','One-on-One','Apoiar conversa humana')) INSERT INTO valorapesquisa.methodology_prompt_templates(methodology_version_id,code,name,purpose,system_prompt,user_prompt_template,output_schema_code,guardrail_code) SELECT v.id,x.code,x.name,x.purpose,'Você é a IA Valora. Use somente evidências fornecidas e preserve a lógica sistêmica.','Analise {{evidence_pack}} conforme {{methodology_version}}.','insight_v1','evidence_required' FROM v CROSS JOIN x ON CONFLICT(methodology_version_id,code,version_number) DO UPDATE SET system_prompt=excluded.system_prompt,updated_at=now();
WITH v AS(SELECT id FROM valorapesquisa.methodology_versions WHERE code='VALORA-2026.1'), pairs(concept,index_code) AS(VALUES ('clarity','ICS'),('governance','IGO'),('leadership','ILI'),('culture','ICO'),('people','IPO'),('systems','IIS'),('organizational_intelligence','IIO'),('organizational_development','IDO'),('sustainability','ISO'),('accountability','IAC'),('responsible_autonomy','IAR'),('systemic_integration','IIS'),('organizational_learning','IIO'),('decision_making','IGO'),('key_person_dependency','IMO'),('organizational_maturity','IMO')), ins AS(INSERT INTO valorapesquisa.methodology_question_bank(methodology_version_id,code,question_text,description,question_type,answer_scale) SELECT v.id,'OFFICIAL_'||upper(p.concept),'Em que medida '||lower(c.name)||' é uma prática observável, recorrente e revisada com evidências?','Pergunta basal oficial vinculada ao mapa cognitivo.','scale_1_5','{"min":1,"max":5}' FROM v JOIN pairs p ON true JOIN valorapesquisa.methodology_concepts c ON c.code=p.concept ON CONFLICT(methodology_version_id,code) DO UPDATE SET question_text=excluded.question_text RETURNING id,methodology_version_id,code) INSERT INTO valorapesquisa.methodology_question_mappings(methodology_version_id,question_id,concept_id,index_id,weight,mapping_type) SELECT q.methodology_version_id,q.id,c.id,i.id,1,'primary' FROM ins q JOIN pairs p ON q.code='OFFICIAL_'||upper(p.concept) JOIN valorapesquisa.methodology_concepts c ON c.code=p.concept JOIN valorapesquisa.methodology_indices i ON i.methodology_version_id=q.methodology_version_id AND i.code=p.index_code WHERE NOT EXISTS(SELECT 1 FROM valorapesquisa.methodology_question_mappings m WHERE m.question_id=q.id AND m.concept_id=c.id AND m.index_id=i.id AND m.deleted_at IS NULL);

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('methodology.publish','Publicar metodologia','Publica versão validada e imutável.','organizational_intelligence'),('methodology.clone','Clonar metodologia','Cria draft a partir de versão rastreável.','organizational_intelligence'),('methodology.validate','Validar metodologia','Executa consistência pré-publicação.','organizational_intelligence'),('methodology.concepts.read','Consultar conceitos metodológicos','Consulta dicionário oficial.','organizational_intelligence'),('methodology.concepts.manage','Gerenciar conceitos metodológicos','Gerencia conceitos em draft.','organizational_intelligence'),('methodology.indexes.read','Consultar índices metodológicos','Consulta índices oficiais.','organizational_intelligence'),('methodology.indexes.manage','Gerenciar índices metodológicos','Gerencia índices em draft.','organizational_intelligence'),('methodology.questions.read','Consultar perguntas metodológicas','Consulta banco oficial.','forms'),('methodology.questions.manage','Gerenciar perguntas metodológicas','Gerencia perguntas em draft.','forms'),('methodology.prompts.read','Consultar prompts metodológicos','Consulta prompts versionados.','organizational_intelligence'),('methodology.prompts.manage','Gerenciar prompts metodológicos','Gerencia prompts em draft.','organizational_intelligence'),('methodology.guardrails.read','Consultar guardrails','Consulta proteção metodológica.','organizational_intelligence'),('methodology.guardrails.manage','Gerenciar guardrails','Gerencia guardrails em draft.','organizational_intelligence') ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at) SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code LIKE 'methodology.%' ON CONFLICT(role_id,permission_id) DO NOTHING;

COMMIT;

-- 2026-08-25 · Valora Action™, Evolution™ e Journey™
-- Migração aditiva: funciona tanto sobre as tabelas genéricas legadas quanto em banco limpo.
BEGIN;
CREATE TABLE IF NOT EXISTS valorapesquisa.action_plans (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),title text NOT NULL,status text NOT NULL DEFAULT 'draft',created_at timestamptz NOT NULL DEFAULT now());
ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS diagnostic_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS result_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS governance_cycle_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS summary text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS origin_type varchar(40) NOT NULL DEFAULT 'manual'; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS origin_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS priority varchar(20) NOT NULL DEFAULT 'medium'; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS owner_user_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS created_by_user_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS approved_by_user_id uuid; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS starts_at timestamptz; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS due_at timestamptz; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS completed_at timestamptz; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS expected_outcome text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'; ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.action_plans ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.action_plans SET status='draft' WHERE status NOT IN('draft','proposed','approved','in_execution','completed','canceled'); UPDATE valorapesquisa.action_plans SET priority='medium' WHERE priority NOT IN('critical','high','medium','low');

CREATE TABLE IF NOT EXISTS valorapesquisa.action_items(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),action_plan_id uuid NOT NULL REFERENCES valorapesquisa.action_plans(id),title text NOT NULL DEFAULT '',description text NOT NULL DEFAULT '',origin_type varchar(40) NOT NULL DEFAULT 'manual',priority varchar(20) NOT NULL DEFAULT 'medium',status varchar(30) NOT NULL DEFAULT 'pending',progress_percent integer NOT NULL DEFAULT 0,evidence_summary text NOT NULL DEFAULT '',expected_outcome text NOT NULL DEFAULT '',metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS action_plan_id uuid; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS diagnostic_id uuid; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS result_id uuid; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS title text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS description text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS origin_type varchar(40) NOT NULL DEFAULT 'manual'; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS origin_id uuid; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS related_dimension varchar(100); ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS related_index_code varchar(40); ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS priority varchar(20) NOT NULL DEFAULT 'medium'; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS responsible_user_id uuid; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS due_at timestamptz; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS completed_at timestamptz; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS progress_percent integer NOT NULL DEFAULT 0; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS expected_outcome text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS completion_evidence text; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS ai_recommendation_summary text; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'; ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.action_items ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.action_items SET status='pending' WHERE status NOT IN('pending','in_progress','blocked','completed','canceled','overdue'); UPDATE valorapesquisa.action_items SET priority='medium' WHERE priority NOT IN('critical','high','medium','low'); UPDATE valorapesquisa.action_items SET progress_percent=greatest(0,least(100,progress_percent));

CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_evidence_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),evidence_id uuid NOT NULL,evidence_summary text NOT NULL DEFAULT '',created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_decision_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),decision_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(action_item_id,decision_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_alert_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),alert_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(action_item_id,alert_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_indicator_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),indicator_code varchar(80) NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(action_item_id,indicator_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_status_history(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),from_status varchar(30),to_status varchar(30) NOT NULL,progress_percent integer,reason text,changed_by_user_id uuid,changed_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_comments(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),comment_text text NOT NULL,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz,deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.action_item_checkins(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),progress_percent integer NOT NULL CHECK(progress_percent BETWEEN 0 AND 100),note text,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.evolution_cycles(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),title text NOT NULL DEFAULT '',summary text NOT NULL DEFAULT '',status varchar(30) NOT NULL DEFAULT 'open',trend varchar(40) NOT NULL DEFAULT 'insufficient_sample',period_start timestamptz NOT NULL DEFAULT now(),evidence_summary text NOT NULL DEFAULT '',metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS diagnostic_id uuid; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS result_id uuid; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS governance_cycle_id uuid; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS title text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS summary text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS baseline_score numeric(10,2); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS current_score numeric(10,2); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS target_score numeric(10,2); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS baseline_level varchar(80); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS current_level varchar(80); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS trend varchar(40) NOT NULL DEFAULT 'insufficient_sample'; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS period_start timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS period_end timestamptz; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS learning_summary text; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS created_by_user_id uuid; ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.evolution_cycles ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.evolution_cycles SET status='open' WHERE status NOT IN('open','monitoring','reviewed','closed','archived');
CREATE TABLE IF NOT EXISTS valorapesquisa.evolution_measurements(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),evolution_cycle_id uuid NOT NULL REFERENCES valorapesquisa.evolution_cycles(id),dimension_code varchar(100),index_code varchar(40),score numeric(10,2),evidence_summary text NOT NULL,measured_at timestamptz NOT NULL DEFAULT now(),metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.evolution_milestones(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),evolution_cycle_id uuid NOT NULL REFERENCES valorapesquisa.evolution_cycles(id),title text NOT NULL,description text NOT NULL,evidence_summary text NOT NULL,occurred_at timestamptz NOT NULL,created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.evolution_snapshots(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),evolution_cycle_id uuid NOT NULL REFERENCES valorapesquisa.evolution_cycles(id),diagnostic_id uuid,result_id uuid,snapshot_type varchar(40) NOT NULL,score numeric(10,2),previous_score numeric(10,2),delta numeric(10,2),level varchar(80),trend varchar(40) NOT NULL DEFAULT 'insufficient_sample',completed_actions integer NOT NULL DEFAULT 0,delayed_actions integer NOT NULL DEFAULT 0,open_alerts integer NOT NULL DEFAULT 0,evidence_summary text NOT NULL,interpretation text NOT NULL,recommendation text NOT NULL,calculated_at timestamptz NOT NULL DEFAULT now(),metadata_json jsonb NOT NULL DEFAULT '{}',created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.journey_events(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),event_type varchar(50) NOT NULL DEFAULT 'manual_note',title text NOT NULL DEFAULT '',description text NOT NULL DEFAULT '',source_type varchar(50) NOT NULL DEFAULT 'manual',impact_level varchar(20) NOT NULL DEFAULT 'medium',evidence_summary text NOT NULL DEFAULT '',occurred_at timestamptz NOT NULL DEFAULT now(),metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS diagnostic_id uuid; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS result_id uuid; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS governance_cycle_id uuid; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS event_type varchar(50) NOT NULL DEFAULT 'manual_note'; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS title text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS description text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS source_type varchar(50) NOT NULL DEFAULT 'manual'; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS source_id uuid; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS impact_level varchar(20) NOT NULL DEFAULT 'medium'; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS related_dimension varchar(100); ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS related_index_code varchar(40); ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS evidence_summary text NOT NULL DEFAULT ''; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS occurred_at timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS created_by_user_id uuid; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'; ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.journey_events ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.journey_events SET event_type='manual_note' WHERE event_type NOT IN('diagnostic_created','result_calculated','insight_generated','alert_generated','decision_created','action_created','action_completed','evolution_snapshot','report_generated','governance_meeting','cycle_closed','manual_note','one_on_one_completed'); UPDATE valorapesquisa.journey_events SET impact_level='medium' WHERE impact_level NOT IN('critical','high','medium','low');
CREATE TABLE IF NOT EXISTS valorapesquisa.journey_event_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),journey_event_id uuid NOT NULL REFERENCES valorapesquisa.journey_events(id),link_type varchar(40) NOT NULL,linked_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(journey_event_id,link_type,linked_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.journey_event_evidence_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),journey_event_id uuid NOT NULL REFERENCES valorapesquisa.journey_events(id),evidence_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(journey_event_id,evidence_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.journey_event_action_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),journey_event_id uuid NOT NULL REFERENCES valorapesquisa.journey_events(id),action_item_id uuid NOT NULL REFERENCES valorapesquisa.action_items(id),created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(journey_event_id,action_item_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.journey_event_decision_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),journey_event_id uuid NOT NULL REFERENCES valorapesquisa.journey_events(id),decision_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(journey_event_id,decision_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.journey_event_alert_links(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),journey_event_id uuid NOT NULL REFERENCES valorapesquisa.journey_events(id),alert_id uuid NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(journey_event_id,alert_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.recommendation_queue(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),source_type varchar(50) NOT NULL,source_id uuid,observation text NOT NULL,evidence text NOT NULL,correlation text NOT NULL,impact text NOT NULL,priority varchar(20) NOT NULL DEFAULT 'medium',recommendation text NOT NULL,limitation text NOT NULL,status varchar(30) NOT NULL DEFAULT 'proposed',created_by_user_id uuid,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);

CREATE INDEX IF NOT EXISTS ix_action_plans_org_status ON valorapesquisa.action_plans(organization_id,status,due_at) WHERE deleted_at IS NULL; CREATE INDEX IF NOT EXISTS ix_action_items_org_status ON valorapesquisa.action_items(organization_id,status,due_at) WHERE deleted_at IS NULL; CREATE INDEX IF NOT EXISTS ix_action_items_plan ON valorapesquisa.action_items(action_plan_id,created_at) WHERE deleted_at IS NULL; CREATE INDEX IF NOT EXISTS ix_evolution_cycles_org ON valorapesquisa.evolution_cycles(organization_id,status,period_start DESC) WHERE deleted_at IS NULL; CREATE INDEX IF NOT EXISTS ix_evolution_snapshots_cycle ON valorapesquisa.evolution_snapshots(evolution_cycle_id,calculated_at DESC) WHERE deleted_at IS NULL; CREATE INDEX IF NOT EXISTS ix_journey_timeline ON valorapesquisa.journey_events(organization_id,occurred_at DESC) WHERE deleted_at IS NULL; CREATE INDEX IF NOT EXISTS ix_recommendation_queue_org ON valorapesquisa.recommendation_queue(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('action.read','Consultar Action','Consultar planos e ações','organizational_intelligence'),('action.manage','Gerenciar Action','Criar e acompanhar ações','organizational_intelligence'),('action.approve','Aprovar Action','Aprovar planos de ação','organizational_intelligence'),('action.complete','Concluir Action','Concluir ação com evidência','organizational_intelligence'),('action.comments.manage','Gerenciar comentários de Action','Registrar comentários e check-ins','organizational_intelligence'),('evolution.read','Consultar Evolution','Consultar ciclos e snapshots','organizational_intelligence'),('evolution.manage','Gerenciar Evolution','Abrir e encerrar ciclos','organizational_intelligence'),('evolution.snapshots.generate','Gerar snapshots de Evolution','Preservar leituras históricas','organizational_intelligence'),('journey.read','Consultar Journey','Consultar memória organizacional','organizational_intelligence'),('journey.manage','Gerenciar Journey','Gerenciar memória organizacional','organizational_intelligence'),('journey.events.create','Criar eventos da Journey','Registrar eventos manuais','organizational_intelligence'),('journey.events.manage','Gerenciar eventos da Journey','Ocultar logicamente eventos','organizational_intelligence') ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code;
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at) SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN('action.read','action.manage','action.approve','action.complete','action.comments.manage','evolution.read','evolution.manage','evolution.snapshots.generate','journey.read','journey.manage','journey.events.create','journey.events.manage') ON CONFLICT(role_id,permission_id) DO NOTHING;
COMMIT;

BEGIN;
-- Valora One-on-One™ v2: evidências, privacidade, compromissos e desenvolvimento.
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_agenda_items(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),title text NOT NULL,description text,source_type varchar(40) NOT NULL DEFAULT 'manual',evidence text,sort_order integer NOT NULL DEFAULT 0,discussed boolean NOT NULL DEFAULT false,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_commitments(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),responsible_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),title text NOT NULL,description text,due_at timestamptz NOT NULL,status varchar(30) NOT NULL DEFAULT 'pending',action_item_id uuid REFERENCES valorapesquisa.action_items(id),created_by_user_id uuid,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_feedbacks(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),from_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),to_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),feedback text NOT NULL,evidence text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_private_notes(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),session_id uuid NOT NULL REFERENCES valorapesquisa.one_on_one_sessions(id),created_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),note text NOT NULL,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_development_goals(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),leader_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),session_id uuid REFERENCES valorapesquisa.one_on_one_sessions(id),title text NOT NULL,evidence text NOT NULL,target_at timestamptz,status varchar(30) NOT NULL DEFAULT 'active',created_by_user_id uuid,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.one_on_one_ai_suggestions(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),session_id uuid REFERENCES valorapesquisa.one_on_one_sessions(id),leader_user_id uuid REFERENCES valorapesquisa.users(id),suggestion_type varchar(50) NOT NULL,observation text NOT NULL,evidence text NOT NULL,impact text NOT NULL,recommendation text NOT NULL,limitation text NOT NULL,status varchar(30) NOT NULL DEFAULT 'proposed',metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_profiles(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),role_title text,development_summary text,last_session_at timestamptz,metadata_json jsonb NOT NULL DEFAULT '{}',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(organization_id,user_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_development_snapshots(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),leader_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),session_id uuid REFERENCES valorapesquisa.one_on_one_sessions(id),summary text NOT NULL,evidence_summary text NOT NULL,indicators_json jsonb NOT NULL DEFAULT '{}',created_by_user_id uuid,created_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.leadership_risk_alerts(id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),leader_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),risk_type varchar(50) NOT NULL,observation text NOT NULL,evidence text NOT NULL,impact text NOT NULL,recommendation text NOT NULL,limitation text NOT NULL,status varchar(30) NOT NULL DEFAULT 'open',created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
UPDATE valorapesquisa.one_on_one_sessions SET status='draft' WHERE status IS NULL OR status NOT IN('draft','scheduled','completed','canceled','missed');
CREATE INDEX IF NOT EXISTS ix_ooo_agenda_session ON valorapesquisa.one_on_one_agenda_items(organization_id,session_id,sort_order) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_ooo_commitments_due ON valorapesquisa.one_on_one_commitments(organization_id,status,due_at) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_ooo_feedback_session ON valorapesquisa.one_on_one_feedbacks(organization_id,session_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_ooo_private_author ON valorapesquisa.one_on_one_private_notes(organization_id,session_id,created_by_user_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_ooo_ai_suggestions ON valorapesquisa.one_on_one_ai_suggestions(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_leadership_snapshots ON valorapesquisa.leadership_development_snapshots(organization_id,leader_user_id,created_at DESC) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_leadership_risks ON valorapesquisa.leadership_risk_alerts(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('one_on_one.read','Consultar One-on-One','Acessar painel One-on-One','organizational_intelligence'),('one_on_one.manage','Gerenciar One-on-One','Gerenciar acompanhamentos','organizational_intelligence'),('one_on_one.sessions.read','Consultar sessões One-on-One','Consultar sessões autorizadas','organizational_intelligence'),('one_on_one.sessions.manage','Gerenciar sessões One-on-One','Criar, agendar e concluir sessões','organizational_intelligence'),('one_on_one.private_notes.read','Consultar notas privadas autorizadas','Consultar somente notas próprias ou autorizadas','organizational_intelligence'),('one_on_one.private_notes.manage','Gerenciar notas privadas','Registrar notas privadas próprias','organizational_intelligence'),('leadership.read','Consultar lideranças','Consultar perfis de liderança','organizational_intelligence'),('leadership.manage','Gerenciar lideranças','Gerenciar perfis de liderança','organizational_intelligence'),('leadership.development.read','Consultar desenvolvimento de lideranças','Consultar evolução baseada em evidências','organizational_intelligence'),('leadership.development.manage','Gerenciar desenvolvimento de lideranças','Gerar snapshots e acompanhar evolução','organizational_intelligence') ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code;
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at) SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN('one_on_one.read','one_on_one.manage','one_on_one.sessions.read','one_on_one.sessions.manage','one_on_one.private_notes.read','one_on_one.private_notes.manage','leadership.read','leadership.manage','leadership.development.read','leadership.development.manage') ON CONFLICT(role_id,permission_id) DO NOTHING;
COMMIT;

-- 2026-08-25 · Valora Diagnostic Engine™
-- Modelo aditivo e idempotente do ciclo completo. As chaves técnicas são internas à aplicação.
BEGIN;
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostics (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 methodology_version_id uuid REFERENCES valorapesquisa.methodology_versions(id), title text NOT NULL, description text,
 status varchar(30) NOT NULL DEFAULT 'draft', start_at timestamptz, end_at timestamptz,
 created_by_user_id uuid REFERENCES valorapesquisa.users(id), closed_by_user_id uuid REFERENCES valorapesquisa.users(id), closed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 CHECK(end_at IS NULL OR start_at IS NULL OR end_at >= start_at));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_forms (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),diagnostic_id uuid NOT NULL REFERENCES valorapesquisa.diagnostics(id),form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id),form_version_id uuid REFERENCES valorapesquisa.form_versions(id),created_at timestamptz NOT NULL DEFAULT now(),UNIQUE(diagnostic_id,form_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_participants (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),diagnostic_id uuid NOT NULL REFERENCES valorapesquisa.diagnostics(id),user_id uuid REFERENCES valorapesquisa.users(id),email text NOT NULL,display_name text,status varchar(30) NOT NULL DEFAULT 'pending',metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,UNIQUE(diagnostic_id,email));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_invitations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),diagnostic_id uuid NOT NULL REFERENCES valorapesquisa.diagnostics(id),participant_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_participants(id),token_hash text NOT NULL UNIQUE,status varchar(30) NOT NULL DEFAULT 'pending',sent_at timestamptz,expires_at timestamptz,accepted_at timestamptz,resend_count integer NOT NULL DEFAULT 0,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_invitation_events (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),invitation_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_invitations(id),event_type varchar(40) NOT NULL,event_at timestamptz NOT NULL DEFAULT now(),actor_user_id uuid REFERENCES valorapesquisa.users(id),metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE TABLE IF NOT EXISTS valorapesquisa.form_sections (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id),form_version_id uuid REFERENCES valorapesquisa.form_versions(id),title text NOT NULL,description text,order_index integer NOT NULL DEFAULT 0,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS organization_id uuid; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS methodology_version_id uuid; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS form_id uuid; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS section_id uuid; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS title text; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS description text; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS question_type varchar(40) NOT NULL DEFAULT 'scale_1_5'; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS weight numeric(10,4) NOT NULL DEFAULT 1; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS order_index integer NOT NULL DEFAULT 0; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS validation_json jsonb NOT NULL DEFAULT '{}'::jsonb; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb; ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now(); ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
UPDATE valorapesquisa.questions SET title=COALESCE(title,text), weight=1 WHERE weight IS NULL OR weight<=0;
CREATE TABLE IF NOT EXISTS valorapesquisa.question_validation_rules (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id),rule_type varchar(40) NOT NULL,rule_json jsonb NOT NULL DEFAULT '{}'::jsonb,message text NOT NULL,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.response_sessions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),diagnostic_id uuid NOT NULL REFERENCES valorapesquisa.diagnostics(id),respondent_user_id uuid REFERENCES valorapesquisa.users(id),invitation_id uuid REFERENCES valorapesquisa.diagnostic_invitations(id),status varchar(30) NOT NULL DEFAULT 'in_progress',started_at timestamptz NOT NULL DEFAULT now(),submitted_at timestamptz,progress_percent numeric(5,2) NOT NULL DEFAULT 0,access_token_hash text,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,created_at timestamptz NOT NULL DEFAULT now(),updated_at timestamptz NOT NULL DEFAULT now(),deleted_at timestamptz,CHECK(progress_percent BETWEEN 0 AND 100));
CREATE TABLE IF NOT EXISTS valorapesquisa.response_answer_audit (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),response_answer_id uuid NOT NULL REFERENCES valorapesquisa.response_answers(id),session_id uuid REFERENCES valorapesquisa.response_sessions(id),action varchar(20) NOT NULL,old_value_json jsonb,new_value_json jsonb,actor_user_id uuid REFERENCES valorapesquisa.users(id),occurred_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_closure_requests (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),diagnostic_id uuid NOT NULL REFERENCES valorapesquisa.diagnostics(id),requested_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),status varchar(30) NOT NULL DEFAULT 'pending',response_summary_json jsonb NOT NULL DEFAULT '{}'::jsonb,requested_at timestamptz NOT NULL DEFAULT now(),processed_at timestamptz,failure_reason text,metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE INDEX IF NOT EXISTS ix_diagnostics_org_status ON valorapesquisa.diagnostics(organization_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_diagnostic_participants_diagnostic ON valorapesquisa.diagnostic_participants(diagnostic_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_diagnostic_invitations_diagnostic ON valorapesquisa.diagnostic_invitations(diagnostic_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_response_sessions_diagnostic ON valorapesquisa.response_sessions(diagnostic_id,status) WHERE deleted_at IS NULL;
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES ('diagnostics.read','Consultar diagnósticos','Consultar ciclos diagnósticos.','surveys'),('diagnostics.manage','Gerenciar diagnósticos','Criar e administrar ciclos diagnósticos.','surveys'),('diagnostics.close','Fechar diagnósticos','Encerrar ciclos com rastreabilidade.','surveys'),('diagnostics.calculate','Calcular diagnósticos','Calcular resultados e indicadores.','results'),('forms.manage','Gerenciar formulários','Construir e versionar formulários.','forms'),('questions.read','Consultar perguntas','Consultar perguntas do formulário.','forms'),('questions.manage','Gerenciar perguntas','Gerenciar perguntas em rascunhos.','forms'),('responses.manage','Gerenciar respostas','Revisar e administrar respostas.','responses'),('invitations.manage','Gerenciar convites','Enviar e reenviar convites.','distribution') ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,updated_at=now();
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id,created_at) SELECT r.id,p.id,now() FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p WHERE r.code='admin_valora' AND r.deleted_at IS NULL AND p.code IN('diagnostics.read','diagnostics.manage','diagnostics.close','diagnostics.calculate','forms.read','forms.manage','forms.publish','questions.read','questions.manage','responses.read','responses.manage','invitations.read','invitations.manage') ON CONFLICT(role_id,permission_id) DO NOTHING;


-- SaaS Plans & Subscription Control (contrato comercial canônico)
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_plans (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(40) NOT NULL UNIQUE, name varchar(100) NOT NULL,
 description text NOT NULL DEFAULT '', status varchar(20) NOT NULL DEFAULT 'active', monthly_price numeric(12,2) NOT NULL DEFAULT 0,
 annual_price numeric(12,2) NOT NULL DEFAULT 0, max_diagnostics integer NOT NULL DEFAULT 0, max_respondents integer NOT NULL DEFAULT 0,
 max_users integer NOT NULL DEFAULT 0, max_storage_mb integer NOT NULL DEFAULT 0, has_reports boolean NOT NULL DEFAULT false,
 has_certificates boolean NOT NULL DEFAULT false, has_heatmap boolean NOT NULL DEFAULT false, has_benchmark boolean NOT NULL DEFAULT false,
 has_action_center boolean NOT NULL DEFAULT false, has_evolution boolean NOT NULL DEFAULT false, has_journey boolean NOT NULL DEFAULT false,
 has_one_on_one boolean NOT NULL DEFAULT false, has_datahub boolean NOT NULL DEFAULT false, has_powerbi boolean NOT NULL DEFAULT false,
 has_api_access boolean NOT NULL DEFAULT false, has_webhooks boolean NOT NULL DEFAULT false, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 CHECK (monthly_price >= 0 AND annual_price >= 0));
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_plan_features (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES valorapesquisa.subscription_plans(id),
 feature_code varchar(60) NOT NULL, enabled boolean NOT NULL DEFAULT true, configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(plan_id,feature_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_subscriptions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 plan_id uuid NOT NULL REFERENCES valorapesquisa.subscription_plans(id), status varchar(20) NOT NULL DEFAULT 'active', started_at timestamptz NOT NULL DEFAULT now(),
 expires_at timestamptz, trial_ends_at timestamptz, canceled_at timestamptz, billing_email varchar(320),
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_organization_subscriptions_active ON valorapesquisa.organization_subscriptions(organization_id) WHERE deleted_at IS NULL;
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_usage_counters (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid NOT NULL REFERENCES valorapesquisa.organization_subscriptions(id), period_start date NOT NULL, period_end date NOT NULL,
 diagnostics_used integer NOT NULL DEFAULT 0, respondents_used integer NOT NULL DEFAULT 0, users_used integer NOT NULL DEFAULT 0,
 storage_mb_used integer NOT NULL DEFAULT 0, reports_generated integer NOT NULL DEFAULT 0, certificates_generated integer NOT NULL DEFAULT 0,
 api_calls_used integer NOT NULL DEFAULT 0, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(subscription_id,period_start), CHECK(period_end >= period_start));
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_usage_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid NOT NULL REFERENCES valorapesquisa.organization_subscriptions(id), metric varchar(60) NOT NULL, amount integer NOT NULL,
 blocked boolean NOT NULL DEFAULT false, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_upgrade_requests (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 current_plan_id uuid NOT NULL REFERENCES valorapesquisa.subscription_plans(id), requested_plan_id uuid NOT NULL REFERENCES valorapesquisa.subscription_plans(id),
 requested_by uuid NOT NULL REFERENCES valorapesquisa.users(id), reason varchar(1000) NOT NULL, billing_email varchar(320) NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'pending', reviewed_by uuid REFERENCES valorapesquisa.users(id), reviewed_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.billing_contacts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), name varchar(150) NOT NULL,
 email varchar(320) NOT NULL, phone varchar(40), primary_contact boolean NOT NULL DEFAULT false, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE TABLE IF NOT EXISTS valorapesquisa.billing_invoices (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid NOT NULL REFERENCES valorapesquisa.organization_subscriptions(id), number varchar(80) NOT NULL UNIQUE,
 status varchar(20) NOT NULL DEFAULT 'pending', amount numeric(12,2) NOT NULL, due_at timestamptz NOT NULL, paid_at timestamptz,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 deleted_at timestamptz, CHECK(amount >= 0));
CREATE TABLE IF NOT EXISTS valorapesquisa.plan_limit_overrides (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), subscription_id uuid NOT NULL REFERENCES valorapesquisa.organization_subscriptions(id),
 metric varchar(60) NOT NULL, limit_value integer NOT NULL, applied_by uuid NOT NULL REFERENCES valorapesquisa.users(id), reason text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_limit_overrides_active ON valorapesquisa.plan_limit_overrides(subscription_id,metric) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_subscription_usage_events_org_occurred ON valorapesquisa.subscription_usage_events(organization_id,occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_upgrade_requests_org_status ON valorapesquisa.subscription_upgrade_requests(organization_id,status);

INSERT INTO valorapesquisa.subscription_plans
(code,name,description,status,monthly_price,annual_price,max_diagnostics,max_respondents,max_users,max_storage_mb,has_reports,has_certificates,has_heatmap,has_benchmark,has_action_center,has_evolution,has_journey,has_one_on_one,has_datahub,has_powerbi,has_api_access,has_webhooks)
VALUES
('free','Free','Primeiro diagnóstico e relatório essencial.','active',0,0,1,50,2,100,true,false,false,false,false,false,false,false,false,false,false,false),
('start','Start','Operação inicial com relatórios e certificados básicos.','active',149,1490,5,500,5,1024,true,true,false,false,false,false,false,false,false,false,false,false),
('growth','Growth','Inteligência contínua e recursos avançados.','active',399,3990,25,5000,25,10240,true,true,true,true,true,true,true,false,false,false,false,false),
('enterprise','Enterprise','Governança, integrações e limites contratáveis.','active',0,0,250,100000,500,102400,true,true,true,true,true,true,true,true,true,true,true,true)
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,status=excluded.status,monthly_price=excluded.monthly_price,
 annual_price=excluded.annual_price,max_diagnostics=excluded.max_diagnostics,max_respondents=excluded.max_respondents,max_users=excluded.max_users,
 max_storage_mb=excluded.max_storage_mb,has_reports=excluded.has_reports,has_certificates=excluded.has_certificates,has_heatmap=excluded.has_heatmap,
 has_benchmark=excluded.has_benchmark,has_action_center=excluded.has_action_center,has_evolution=excluded.has_evolution,has_journey=excluded.has_journey,
 has_one_on_one=excluded.has_one_on_one,has_datahub=excluded.has_datahub,has_powerbi=excluded.has_powerbi,has_api_access=excluded.has_api_access,
 has_webhooks=excluded.has_webhooks,updated_at=now();
INSERT INTO valorapesquisa.subscription_plan_features(plan_id,feature_code)
SELECT p.id,f.code FROM valorapesquisa.subscription_plans p CROSS JOIN LATERAL (VALUES
 ('diagnostics'),('reports'),('certificates'),('heatmap'),('benchmark'),('action_center'),('evolution'),('journey'),('one_on_one'),('datahub'),('powerbi'),('analytics_api'),('webhooks')) f(code)
WHERE (f.code='diagnostics') OR (f.code='reports' AND p.has_reports) OR (f.code='certificates' AND p.has_certificates)
 OR (f.code='heatmap' AND p.has_heatmap) OR (f.code='benchmark' AND p.has_benchmark) OR (f.code='action_center' AND p.has_action_center)
 OR (f.code='evolution' AND p.has_evolution) OR (f.code='journey' AND p.has_journey) OR (f.code='one_on_one' AND p.has_one_on_one)
 OR (f.code='datahub' AND p.has_datahub) OR (f.code='powerbi' AND p.has_powerbi) OR (f.code='analytics_api' AND p.has_api_access)
 OR (f.code='webhooks' AND p.has_webhooks) ON CONFLICT(plan_id,feature_code) DO UPDATE SET enabled=true,updated_at=now();
INSERT INTO valorapesquisa.organization_subscriptions(organization_id,plan_id,status,metadata_json)
SELECT o.id,p.id,'active','{"automatic":"free_default"}'::jsonb FROM valorapesquisa.organizations o
JOIN valorapesquisa.subscription_plans p ON p.code='free' WHERE o.deleted_at IS NULL
AND NOT EXISTS(SELECT 1 FROM valorapesquisa.organization_subscriptions s WHERE s.organization_id=o.id AND s.deleted_at IS NULL)
ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
('plans.read','Consultar planos','Consultar catálogo comercial.','organization'),
('subscriptions.read','Consultar assinatura','Consultar assinatura da organização.','organization'),
('subscriptions.manage','Gerenciar assinaturas','Alterar assinaturas como Super Admin.','organization'),
('subscriptions.upgrade_request','Solicitar upgrade','Solicitar upgrade comercial.','organization'),
('billing.read','Consultar cobrança','Consultar contatos e faturas.','organization'),('billing.manage','Gerenciar cobrança','Gerenciar contatos e faturas.','organization'),
('usage.read','Consultar consumo','Consultar consumo da assinatura.','organization'),('usage.manage','Gerenciar consumo','Gerenciar contadores de consumo.','organization'),
('feature_access.read','Consultar acesso','Consultar recursos liberados pelo plano.','organization'),('feature_access.manage','Gerenciar acesso','Gerenciar exceções de acesso.','organization')
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,updated_at=now();


-- Valora Admin Hub: additive, tenant-owned governance contracts. This section is
-- deliberately idempotent so it can converge both clean and partially migrated databases.
CREATE TABLE IF NOT EXISTS valorapesquisa.organization_units (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 parent_unit_id uuid REFERENCES valorapesquisa.organization_units(id), name varchar(160) NOT NULL, code varchar(80),
 status varchar(20) NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
 deleted_at timestamptz, CHECK (status IN ('active','inactive')));
CREATE UNIQUE INDEX IF NOT EXISTS ux_organization_units_org_code_active
 ON valorapesquisa.organization_units(organization_id,code) WHERE deleted_at IS NULL AND code IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_organization_units_org_parent
 ON valorapesquisa.organization_units(organization_id,parent_unit_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.user_profiles (
 user_id uuid PRIMARY KEY REFERENCES valorapesquisa.users(id) ON DELETE CASCADE, organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 job_title varchar(160), phone varchar(40), locale varchar(12) NOT NULL DEFAULT 'pt-BR', timezone varchar(80) NOT NULL DEFAULT 'America/Sao_Paulo',
 avatar_url text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_user_profiles_org ON valorapesquisa.user_profiles(organization_id,user_id);

CREATE TABLE IF NOT EXISTS valorapesquisa.user_unit_assignments (
 organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 unit_id uuid NOT NULL REFERENCES valorapesquisa.organization_units(id), is_primary boolean NOT NULL DEFAULT false,
 created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, PRIMARY KEY(user_id,unit_id));
CREATE INDEX IF NOT EXISTS ix_user_unit_assignments_org_unit
 ON valorapesquisa.user_unit_assignments(organization_id,unit_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.platform_audit_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), actor_user_id uuid REFERENCES valorapesquisa.users(id), event_type varchar(120) NOT NULL,
 entity_type varchar(80), entity_id text, correlation_id text, ip_address inet, before_json jsonb, after_json jsonb,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_platform_audit_events_occurred ON valorapesquisa.platform_audit_events(occurred_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_audit_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 actor_user_id uuid REFERENCES valorapesquisa.users(id), event_type varchar(120) NOT NULL, entity_type varchar(80), entity_id text,
 correlation_id text, ip_address inet, before_json jsonb, after_json jsonb, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 occurred_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_organization_audit_events_org_occurred
 ON valorapesquisa.organization_audit_events(organization_id,occurred_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.notification_preferences (
 organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 channel varchar(20) NOT NULL, notification_type varchar(80) NOT NULL, enabled boolean NOT NULL DEFAULT true,
 digest_frequency varchar(20) NOT NULL DEFAULT 'immediate', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz,
 PRIMARY KEY(user_id,channel,notification_type));
CREATE INDEX IF NOT EXISTS ix_notification_preferences_org_user ON valorapesquisa.notification_preferences(organization_id,user_id);

INSERT INTO valorapesquisa.permissions(code,name,description,module_code,functional_group,risk_level,assignable_to_custom_roles,status)
VALUES
 ('admin.read','Consultar Admin Hub','Acessar o painel administrativo.','operations','administration','low',true,'active'),
 ('admin.manage','Gerenciar Admin Hub','Executar operações administrativas.','operations','administration','high',true,'active'),
 ('organizations.read','Consultar organizações','Consultar organizações autorizadas.','organization','administration','low',true,'active'),
 ('organizations.manage','Gerenciar organizações','Criar e alterar organizações.','organization','administration','high',true,'active'),
 ('units.manage','Gerenciar unidades','Criar, alterar e inativar unidades.','organization','administration','medium',true,'active'),
 ('users.manage','Gerenciar usuários','Criar, alterar e remover acessos.','identity','administration','high',true,'active'),
 ('roles.manage','Gerenciar papéis','Criar e alterar papéis customizados.','identity','administration','high',true,'active'),
 ('permissions.read','Consultar permissões','Consultar o catálogo canônico de permissões.','identity','administration','low',true,'active')
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,
 functional_group=excluded.functional_group,risk_level=excluded.risk_level,assignable_to_custom_roles=excluded.assignable_to_custom_roles,
 status=excluded.status,updated_at=now();

COMMIT;
