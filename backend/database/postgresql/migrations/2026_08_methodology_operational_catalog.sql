BEGIN;
-- Operational catalog complements the canonical Methodology Studio schema. All
-- calculated results point to immutable, versioned rules instead of code constants.
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_maturity_levels (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id),
 code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, maturity_level integer NOT NULL CHECK(maturity_level>0),
 score_min numeric(8,2) NOT NULL, score_max numeric(8,2) NOT NULL, verifiable_criteria text NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(methodology_version_id,code), CHECK(score_max>=score_min));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_evidence_criteria (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id), concept_id uuid NOT NULL REFERENCES valorapesquisa.methodology_concepts(id),
 code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, expected_source text NOT NULL, evidence_strength integer NOT NULL CHECK(evidence_strength BETWEEN 1 AND 5), usage_rule text NOT NULL, evidence_required boolean NOT NULL DEFAULT true,
 status varchar(20) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_scoring_rules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id), dimension_id uuid REFERENCES valorapesquisa.methodology_dimensions(id), concept_id uuid REFERENCES valorapesquisa.methodology_concepts(id),
 code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, weight numeric(8,4) NOT NULL CHECK(weight>0), score_min numeric(8,2) NOT NULL, score_max numeric(8,2) NOT NULL, minimum_answers integer NOT NULL DEFAULT 1 CHECK(minimum_answers>0), rule_json jsonb NOT NULL,
 status varchar(20) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(methodology_version_id,code), CHECK(score_max>=score_min), CHECK(dimension_id IS NOT NULL OR concept_id IS NOT NULL));
CREATE TABLE IF NOT EXISTS valorapesquisa.methodology_indicator_rules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id), scoring_rule_id uuid NOT NULL REFERENCES valorapesquisa.methodology_scoring_rules(id), code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, rule_json jsonb NOT NULL, status varchar(20) NOT NULL DEFAULT 'active', metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(methodology_version_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.question_bank_options (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), question_id uuid NOT NULL REFERENCES valorapesquisa.methodology_question_bank(id), code varchar(80) NOT NULL, label text NOT NULL, score numeric(8,2), display_order integer NOT NULL DEFAULT 0, metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(question_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid, methodology_version_id uuid NOT NULL REFERENCES valorapesquisa.methodology_versions(id), scoring_rule_id uuid REFERENCES valorapesquisa.methodology_scoring_rules(id), code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL, status varchar(20) NOT NULL DEFAULT 'draft' CHECK(status IN('draft','published','archived')), version_number integer NOT NULL DEFAULT 1 CHECK(version_number>0), metadata_json jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(organization_id,code,version_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_template_sections (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_templates(id), code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL DEFAULT '', display_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(template_id,code));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_template_questions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), section_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_template_sections(id), question_id uuid NOT NULL REFERENCES valorapesquisa.methodology_question_bank(id), weight numeric(8,4) NOT NULL CHECK(weight>0), display_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(section_id,question_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.diagnostic_template_publications (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.diagnostic_templates(id), publication_number integer NOT NULL CHECK(publication_number>0), snapshot_json jsonb NOT NULL, justification text NOT NULL, published_by_user_id uuid, published_at timestamptz NOT NULL DEFAULT now(), created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(template_id,publication_number));
CREATE INDEX IF NOT EXISTS ix_maturity_levels_version ON valorapesquisa.methodology_maturity_levels(methodology_version_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_evidence_criteria_concept ON valorapesquisa.methodology_evidence_criteria(concept_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_scoring_rules_version ON valorapesquisa.methodology_scoring_rules(methodology_version_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_diagnostic_templates_methodology ON valorapesquisa.diagnostic_templates(methodology_version_id,status) WHERE deleted_at IS NULL;
COMMIT;
