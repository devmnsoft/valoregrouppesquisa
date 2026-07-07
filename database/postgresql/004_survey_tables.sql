CREATE TABLE IF NOT EXISTS valorapesquisa.forms (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NULL REFERENCES valorapesquisa.organizations(id), name text NOT NULL, description text, category text, time_min int, scoring_method text, status text NOT NULL DEFAULT 'draft', is_global boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), is_deleted boolean NOT NULL DEFAULT false);
CREATE TABLE IF NOT EXISTS valorapesquisa.form_dimensions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id), name text NOT NULL, description text, display_order int NOT NULL DEFAULT 0, max_score numeric(10,2));
CREATE TABLE IF NOT EXISTS valorapesquisa.questions (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id), dimension_id uuid NULL REFERENCES valorapesquisa.form_dimensions(id), text text NOT NULL, help text, type text NOT NULL, weight numeric(10,2) NOT NULL DEFAULT 1, max_score numeric(10,2), required boolean NOT NULL DEFAULT true, display_order int NOT NULL DEFAULT 0, settings_json jsonb NOT NULL DEFAULT '{}'::jsonb);
CREATE TABLE IF NOT EXISTS valorapesquisa.question_options (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), question_id uuid NOT NULL REFERENCES valorapesquisa.questions(id), text text NOT NULL, score numeric(10,2), is_correct boolean, display_order int NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS valorapesquisa.surveys (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), form_id uuid NOT NULL REFERENCES valorapesquisa.forms(id), title text NOT NULL, description text, status text NOT NULL DEFAULT 'draft', token_hash text NOT NULL UNIQUE, public_slug text UNIQUE, public_url text, starts_at timestamptz, expires_at timestamptz, is_free boolean NOT NULL DEFAULT false, is_featured boolean NOT NULL DEFAULT false, visible_on_home boolean NOT NULL DEFAULT false, allow_repeat boolean NOT NULL DEFAULT false, require_identification boolean NOT NULL DEFAULT true, lgpd_required boolean NOT NULL DEFAULT true, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), is_deleted boolean NOT NULL DEFAULT false);

CREATE TABLE IF NOT EXISTS valorapesquisa.survey_links (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), survey_id uuid NOT NULL REFERENCES valorapesquisa.surveys(id), token_hash text NOT NULL, public_url text NOT NULL, status text NOT NULL DEFAULT 'active', starts_at timestamptz, expires_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), created_by uuid, updated_by uuid, is_deleted boolean NOT NULL DEFAULT false);
CREATE INDEX IF NOT EXISTS ix_survey_links_survey ON valorapesquisa.survey_links(survey_id);
ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS revoked_at timestamptz;
ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS plan_id text;
ALTER TABLE valorapesquisa.survey_links ADD COLUMN IF NOT EXISTS revoked_at timestamptz;


-- COMPATIBILIDADE PARA BANCOS EXISTENTES
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS title text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS name text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS slug citext;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS category text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS time_min int;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS scoring_method text;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS is_global boolean NOT NULL DEFAULT false;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS min_score int;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS max_score int;
ALTER TABLE valorapesquisa.forms ADD COLUMN IF NOT EXISTS score_ranges jsonb;
ALTER TABLE valorapesquisa.form_dimensions ADD COLUMN IF NOT EXISTS position int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.form_dimensions ADD COLUMN IF NOT EXISTS display_order int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.form_dimensions ADD COLUMN IF NOT EXISTS weight numeric(8,2) DEFAULT 1;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS position int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS display_order int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS min_value int DEFAULT 1;
ALTER TABLE valorapesquisa.questions ADD COLUMN IF NOT EXISTS max_value int DEFAULT 5;
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS label text;
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS value int;
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS position int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS text text;
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS score numeric(10,2);
ALTER TABLE valorapesquisa.question_options ADD COLUMN IF NOT EXISTS display_order int NOT NULL DEFAULT 0;
