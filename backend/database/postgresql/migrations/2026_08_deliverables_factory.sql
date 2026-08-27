-- Valora Deliverables Factory: documentos rastreáveis, aprovações e acesso seguro.
-- Deliberadamente não remove nem renomeia estruturas legadas; pode ser reaplicado.
BEGIN;
SET search_path TO valorapesquisa, public;

CREATE TABLE IF NOT EXISTS report_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), name varchar(160) NOT NULL,
 description text, format varchar(16) NOT NULL DEFAULT 'pdf', methodology_version varchar(60), is_active boolean NOT NULL DEFAULT true,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_report_templates_global_name ON report_templates(name) WHERE organization_id IS NULL AND deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_report_templates_org_name ON report_templates(organization_id,name) WHERE organization_id IS NOT NULL AND deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS report_template_sections (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES report_templates(id), section_code varchar(80) NOT NULL,
 title varchar(180) NOT NULL, display_order integer NOT NULL DEFAULT 0, is_required boolean NOT NULL DEFAULT false,
 configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_report_template_section UNIQUE(template_id,section_code));

CREATE TABLE IF NOT EXISTS report_generation_jobs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), diagnostic_id uuid NOT NULL,
 result_id uuid, template_id uuid REFERENCES report_templates(id), status varchar(24) NOT NULL DEFAULT 'pending', requested_by uuid,
 started_at timestamptz, finished_at timestamptz, error_message text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_report_generation_job_status CHECK(status IN ('pending','processing','generated','approved','failed','revoked')));
CREATE INDEX IF NOT EXISTS ix_report_generation_jobs_org_status ON report_generation_jobs(organization_id,status,created_at DESC);

CREATE TABLE IF NOT EXISTS report_documents (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), diagnostic_id uuid NOT NULL,
 result_id uuid, generation_job_id uuid REFERENCES report_generation_jobs(id), title varchar(240) NOT NULL, format varchar(16) NOT NULL,
 status varchar(24) NOT NULL DEFAULT 'pending', file_path text, file_name varchar(240), file_size bigint, content_hash char(64),
 version_number integer NOT NULL DEFAULT 1, generated_at timestamptz, approved_at timestamptz, approved_by uuid,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 CONSTRAINT ck_report_document_status CHECK(status IN ('pending','processing','generated','approved','failed','revoked')),
 CONSTRAINT ck_report_document_version CHECK(version_number > 0));
CREATE INDEX IF NOT EXISTS ix_report_documents_org_status ON report_documents(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS report_document_sections (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), report_document_id uuid NOT NULL REFERENCES report_documents(id), section_code varchar(80) NOT NULL,
 title varchar(180) NOT NULL, content_json jsonb NOT NULL DEFAULT '{}'::jsonb, evidence_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 limitations_json jsonb NOT NULL DEFAULT '[]'::jsonb, display_order integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_report_document_section UNIQUE(report_document_id,section_code));

CREATE TABLE IF NOT EXISTS report_downloads (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), report_document_id uuid NOT NULL REFERENCES report_documents(id),
 downloaded_by uuid, ip_hash char(64), user_agent_hash char(64), outcome varchar(20) NOT NULL DEFAULT 'allowed', downloaded_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_report_downloads_document ON report_downloads(report_document_id,downloaded_at DESC);

CREATE TABLE IF NOT EXISTS report_share_links (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), report_document_id uuid NOT NULL REFERENCES report_documents(id),
 token_hash char(64) NOT NULL UNIQUE, expires_at timestamptz NOT NULL, allow_download boolean NOT NULL DEFAULT false, created_by uuid,
 access_count integer NOT NULL DEFAULT 0, revoked_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_report_share_links_active ON report_share_links(token_hash,expires_at) WHERE revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS certificate_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), name varchar(160) NOT NULL, title varchar(200) NOT NULL,
 is_active boolean NOT NULL DEFAULT true, configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_certificate_templates_global_name ON certificate_templates(name) WHERE organization_id IS NULL AND deleted_at IS NULL;

-- Compatibilidade: a instalação histórica já pode possuir certificates.
CREATE TABLE IF NOT EXISTS certificates (id uuid PRIMARY KEY DEFAULT gen_random_uuid());
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS organization_id uuid REFERENCES organizations(id);
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS diagnostic_id uuid;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS result_id uuid;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS certificate_code varchar(64);
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS participant_name varchar(240);
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS title varchar(240);
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS status varchar(24) NOT NULL DEFAULT 'draft';
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS issued_at timestamptz;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS expires_at timestamptz;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS revoked_at timestamptz;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS revoked_reason text;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS file_path text;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS content_hash char(64);
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();
ALTER TABLE certificates ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
CREATE UNIQUE INDEX IF NOT EXISTS ux_certificates_certificate_code ON certificates(certificate_code) WHERE certificate_code IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_certificates_org_status ON certificates(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS certificate_downloads (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), certificate_id uuid NOT NULL REFERENCES certificates(id),
 downloaded_by uuid, ip_hash char(64), user_agent_hash char(64), outcome varchar(20) NOT NULL DEFAULT 'allowed', downloaded_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_certificate_downloads_certificate ON certificate_downloads(certificate_id,downloaded_at DESC);

CREATE TABLE IF NOT EXISTS certificate_validation_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), certificate_id uuid REFERENCES certificates(id), validation_code_hash char(64) NOT NULL,
 validation_status varchar(24) NOT NULL DEFAULT 'not_found', ip_hash char(64), user_agent_hash char(64), validated_at timestamptz NOT NULL DEFAULT now());
ALTER TABLE certificate_validation_events ADD COLUMN IF NOT EXISTS validation_status varchar(24) NOT NULL DEFAULT 'not_found';

CREATE TABLE IF NOT EXISTS deliverable_files (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), resource_type varchar(40) NOT NULL,
 resource_id uuid NOT NULL, storage_key text NOT NULL UNIQUE, file_name varchar(240) NOT NULL, content_type varchar(160) NOT NULL,
 file_size bigint NOT NULL, content_hash char(64) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_deliverable_files_resource ON deliverable_files(organization_id,resource_type,resource_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS deliverable_access_tokens (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), resource_type varchar(40) NOT NULL,
 resource_id uuid NOT NULL, token_hash char(64) NOT NULL UNIQUE, purpose varchar(40) NOT NULL, expires_at timestamptz NOT NULL,
 created_by uuid, revoked_at timestamptz, last_used_at timestamptz, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_deliverable_access_tokens_active ON deliverable_access_tokens(token_hash,expires_at) WHERE revoked_at IS NULL;

CREATE TABLE IF NOT EXISTS deliverable_audit_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid REFERENCES organizations(id), actor_user_id uuid, event_type varchar(100) NOT NULL,
 resource_type varchar(40) NOT NULL, resource_id uuid, outcome varchar(24) NOT NULL, correlation_id text,
 ip_hash char(64), user_agent_hash char(64), metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_deliverable_audit_events_org_time ON deliverable_audit_events(organization_id,occurred_at DESC);

CREATE TABLE IF NOT EXISTS executive_report_approvals (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), report_document_id uuid NOT NULL REFERENCES report_documents(id),
 version_number integer NOT NULL, status varchar(24) NOT NULL, decided_by uuid NOT NULL, decision_notes text, decided_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_executive_report_approval UNIQUE(report_document_id,version_number));

CREATE TABLE IF NOT EXISTS executive_report_snapshots (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), report_document_id uuid NOT NULL REFERENCES report_documents(id),
 version_number integer NOT NULL, methodology_name varchar(160) NOT NULL, methodology_version varchar(60) NOT NULL,
 evidence_json jsonb NOT NULL DEFAULT '[]'::jsonb, limitations_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 snapshot_json jsonb NOT NULL, content_hash char(64) NOT NULL, created_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT uq_executive_report_snapshot UNIQUE(report_document_id,version_number));
-- A tabela homônima de versões anteriores usava survey/cycle. As colunas novas
-- são aditivas e anuláveis para preservar snapshots históricos.
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS report_document_id uuid REFERENCES report_documents(id);
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS version_number integer;
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS methodology_name varchar(160);
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS methodology_version varchar(60);
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS evidence_json jsonb NOT NULL DEFAULT '[]'::jsonb;
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS snapshot_json jsonb;
ALTER TABLE executive_report_snapshots ADD COLUMN IF NOT EXISTS content_hash char(64);
CREATE UNIQUE INDEX IF NOT EXISTS ux_executive_report_snapshot_document_version
 ON executive_report_snapshots(report_document_id,version_number) WHERE report_document_id IS NOT NULL;

INSERT INTO report_templates(name,description,format,methodology_version,metadata_json)
VALUES ('Relatório Executivo Valora','Síntese executiva baseada exclusivamente em resultados e evidências registradas.','pdf','current',
        '{"requiresResult":true,"requiresEvidence":true,"recordsLimitations":true}'::jsonb)
ON CONFLICT DO NOTHING;
INSERT INTO certificate_templates(name,title,configuration_json)
VALUES ('Certificado Valora','Certificado Valora Insight','{"publicValidation":true}'::jsonb)
ON CONFLICT DO NOTHING;

COMMIT;
