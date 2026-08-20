-- Formal, immutable and auditable Valora Insight deliverables.
-- Safe on a clean database after the canonical schema and safe to re-run.
BEGIN;
SET search_path TO valorapesquisa, public;

CREATE TABLE IF NOT EXISTS generated_reports (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id),
 diagnosis_id uuid NOT NULL, title varchar(180) NOT NULL, format varchar(16) NOT NULL,
 methodology_version varchar(60) NOT NULL, trace_code varchar(64) NOT NULL UNIQUE,
 status varchar(24) NOT NULL DEFAULT 'completed', generated_by uuid NULL,
 generated_at timestamptz NOT NULL DEFAULT now(), error_message text NULL,
 CONSTRAINT ck_generated_reports_format CHECK (format IN ('pdf','docx','xlsx','json')),
 CONSTRAINT ck_generated_reports_status CHECK (status IN ('processing','completed','failed','revoked'))
);
CREATE INDEX IF NOT EXISTS ix_generated_reports_org_diagnosis ON generated_reports(organization_id, diagnosis_id, generated_at DESC);

CREATE TABLE IF NOT EXISTS report_files (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), report_id uuid NOT NULL REFERENCES generated_reports(id),
 storage_key text NOT NULL UNIQUE, file_name varchar(240) NOT NULL, content_type varchar(160) NOT NULL,
 byte_length bigint NOT NULL CHECK (byte_length > 0), sha256 char(64) NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS export_jobs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), diagnosis_id uuid NOT NULL,
 format varchar(16) NOT NULL CHECK (format IN ('xlsx','json','docx')), status varchar(24) NOT NULL DEFAULT 'processing',
 requested_by uuid NULL, requested_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz NULL, error_message text NULL
);
CREATE TABLE IF NOT EXISTS export_files (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), export_job_id uuid NOT NULL REFERENCES export_jobs(id), storage_key text NOT NULL UNIQUE,
 file_name varchar(240) NOT NULL, content_type varchar(160) NOT NULL, byte_length bigint NOT NULL CHECK (byte_length > 0),
 sha256 char(64) NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS certificates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), diagnosis_id uuid NOT NULL,
 validation_code varchar(64) NOT NULL UNIQUE, version integer NOT NULL DEFAULT 1 CHECK (version > 0),
 methodology_version varchar(60) NOT NULL, score numeric(8,2) NOT NULL, maturity_level varchar(100) NOT NULL,
 storage_key text NOT NULL UNIQUE, status varchar(24) NOT NULL DEFAULT 'valid' CHECK(status IN ('valid','revoked','expired')),
 issued_by uuid NULL, issued_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz NULL,
 UNIQUE(diagnosis_id, version)
);
CREATE TABLE IF NOT EXISTS certificate_validation_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), certificate_id uuid NULL REFERENCES certificates(id), validation_code_hash char(64) NOT NULL,
 was_valid boolean NOT NULL, ip_hash char(64) NULL, user_agent_hash char(64) NULL, validated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS share_links (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), diagnosis_id uuid NOT NULL,
 document_id uuid NULL, token_hash char(64) NOT NULL UNIQUE, expires_at timestamptz NOT NULL, allow_download boolean NOT NULL DEFAULT false,
 created_by uuid NULL, created_at timestamptz NOT NULL DEFAULT now(), revoked_at timestamptz NULL,
 CONSTRAINT ck_share_expiry CHECK (expires_at > created_at)
);
CREATE INDEX IF NOT EXISTS ix_share_links_active ON share_links(token_hash, expires_at) WHERE revoked_at IS NULL;
CREATE TABLE IF NOT EXISTS download_audit_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id),
 resource_type varchar(40) NOT NULL, resource_id uuid NOT NULL, actor_user_id uuid NULL, share_link_id uuid NULL REFERENCES share_links(id),
 outcome varchar(20) NOT NULL CHECK(outcome IN ('allowed','denied','not_found','failed')), ip_hash char(64) NULL,
 occurred_at timestamptz NOT NULL DEFAULT now()
);

INSERT INTO permissions(code,name,description,module_code) VALUES
('reports.download','Baixar relatórios','Baixa relatórios formais autorizados.','organizational_intelligence'),
('exports.read','Visualizar exportações','Consulta o histórico de exportações.','organizational_intelligence'),
('exports.generate','Gerar exportações','Gera exportações técnicas autorizadas.','organizational_intelligence'),
('certificates.read','Visualizar certificados','Consulta certificados da organização.','organizational_intelligence'),
('certificates.generate','Emitir certificados','Emite certificados para diagnósticos concluídos.','organizational_intelligence'),
('certificates.download','Baixar certificados','Baixa certificados autorizados.','organizational_intelligence'),
('certificates.validate','Validar certificados','Consulta a validade pública de certificados.','organizational_intelligence'),
('share_links.manage','Gerenciar compartilhamentos','Cria e revoga links seguros.','organizational_intelligence')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code,updated_at=now();
INSERT INTO role_permissions(role_id,permission_id,created_at)
SELECT r.id,p.id,now() FROM roles r CROSS JOIN permissions p
WHERE r.code='admin_valora' AND p.code IN ('reports.read','reports.generate','reports.download','exports.read','exports.generate','certificates.read','certificates.generate','certificates.download','certificates.validate','share_links.manage')
ON CONFLICT(role_id,permission_id) DO NOTHING;
INSERT INTO schema_migrations(version,checksum) VALUES('2026_08_formal_deliverables','sha256:formal-deliverables-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();
COMMIT;
