-- 2026-09-16 - ciclo de vida canonico da entrega executiva (prepare -> review -> publish -> share)
BEGIN;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS editorial_status varchar(30) NOT NULL DEFAULT 'draft';
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS processing_status varchar(30) NOT NULL DEFAULT 'awaiting_generation';
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS version_number integer NOT NULL DEFAULT 1;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS template_code varchar(80);
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS sections_json jsonb NOT NULL DEFAULT '[]'::jsonb;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS executive_notes text;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS reviewer_user_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS source_result_hash char(64);
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS methodology_name varchar(160);
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS methodology_version varchar(60);
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS published_at timestamptz;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS published_by uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS parent_deliverable_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS command_id varchar(80);
ALTER TABLE valorapesquisa.formal_deliverables ADD COLUMN IF NOT EXISTS document_id uuid;
ALTER TABLE valorapesquisa.formal_deliverables ALTER COLUMN title TYPE varchar(240);

ALTER TABLE valorapesquisa.formal_deliverable_templates ADD COLUMN IF NOT EXISTS template_code varchar(80);
ALTER TABLE valorapesquisa.formal_deliverable_templates ADD COLUMN IF NOT EXISTS configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb;

CREATE INDEX IF NOT EXISTS ix_formal_deliverables_org_editorial
  ON valorapesquisa.formal_deliverables(organization_id, editorial_status, created_at DESC)
  WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_formal_deliverables_org_processing
  ON valorapesquisa.formal_deliverables(organization_id, processing_status, created_at DESC)
  WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_formal_deliverables_org_type
  ON valorapesquisa.formal_deliverables(organization_id, deliverable_type, created_at DESC)
  WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_formal_deliverables_org_command
  ON valorapesquisa.formal_deliverables(organization_id, command_id)
  WHERE command_id IS NOT NULL AND deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_secure_share_links_deliverable
  ON valorapesquisa.secure_share_links(organization_id, deliverable_id, created_at DESC)
  WHERE deleted_at IS NULL;

INSERT INTO valorapesquisa.formal_deliverable_templates(organization_id, deliverable_type, name, version, template_json, template_code, configuration_json, is_active)
SELECT NULL, 'executive_report', 'Relatorio Executivo Valora', 1,
       '{"sections":["resumo","leitura","dimensoes","evidencias","limitacoes"]}'::jsonb,
       'executive_valora',
       '{"requiresResult":true,"formalMaturityCertification":false}'::jsonb,
       true
WHERE NOT EXISTS (
  SELECT 1 FROM valorapesquisa.formal_deliverable_templates
  WHERE template_code='executive_valora' AND deleted_at IS NULL);

INSERT INTO valorapesquisa.formal_deliverable_templates(organization_id, deliverable_type, name, version, template_json, template_code, configuration_json, is_active)
SELECT NULL, 'certificate', 'Certificado de Participacao Valora', 1,
       '{"certificateType":"participation"}'::jsonb,
       'certificate_participation',
       '{"certificateType":"participation","formalMaturityCertification":false}'::jsonb,
       true
WHERE NOT EXISTS (
  SELECT 1 FROM valorapesquisa.formal_deliverable_templates
  WHERE template_code='certificate_participation' AND deleted_at IS NULL);

INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES('2026_09_16_executive_delivery_lifecycle','sha256:executive-delivery-lifecycle-v1')
ON CONFLICT(version) DO NOTHING;
COMMIT;

-- Seed de desenvolvimento (idempotente). Nao inventa scores.
BEGIN;
DO $$
DECLARE
  org_a uuid;
  org_b uuid;
  user_a uuid;
  result_a uuid;
  diag_a uuid;
  draft_id uuid := 'aaaaaaaa-bbbb-cccc-dddd-000000000001';
  published_id uuid := 'aaaaaaaa-bbbb-cccc-dddd-000000000002';
BEGIN
  SELECT id INTO org_a FROM valorapesquisa.organizations WHERE deleted_at IS NULL ORDER BY created_at LIMIT 1;
  SELECT id INTO org_b FROM valorapesquisa.organizations WHERE deleted_at IS NULL AND id <> org_a ORDER BY created_at LIMIT 1;
  IF org_a IS NULL OR org_b IS NULL THEN
    RAISE NOTICE 'Seed de entregaveis ignorado: sao necessarias duas organizacoes.';
    RETURN;
  END IF;
  SELECT id INTO user_a FROM valorapesquisa.users WHERE organization_id=org_a AND deleted_at IS NULL ORDER BY created_at LIMIT 1;
  SELECT r.id, s.id INTO result_a, diag_a
  FROM valorapesquisa.results r
  JOIN valorapesquisa.responses rsp ON rsp.id=r.response_id
  JOIN valorapesquisa.surveys s ON s.id=rsp.survey_id
  WHERE r.organization_id=org_a AND r.result_score_id IS NOT NULL AND rsp.submitted_at IS NOT NULL
  ORDER BY rsp.submitted_at DESC LIMIT 1;
  IF result_a IS NULL THEN
    RAISE NOTICE 'Seed de entregaveis ignorado: organizacao A sem resultado elegivel.';
    RETURN;
  END IF;

  INSERT INTO valorapesquisa.formal_deliverables(
    id,organization_id,diagnostic_id,result_id,deliverable_type,title,status,generated_by_user_id,
    editorial_status,processing_status,version_number,template_code,sections_json,executive_notes,
    reviewer_user_id,source_result_hash,methodology_name,methodology_version,command_id,metadata_json)
  VALUES (
    draft_id,org_a,diag_a,result_a,'executive_report','Relatorio em revisao (seed)',
    'in_review',user_a,'in_review','awaiting_generation',1,'executive_valora',
    '["objective","results","limitations"]'::jsonb,'Observacoes executivas de seed.',
    user_a,NULL,'Valora Insight','seed','seed-draft-v1','{"seed":true,"missingDataAlerts":["Seed sem arquivo gerado."]}'::jsonb)
  ON CONFLICT (id) DO NOTHING;

  INSERT INTO valorapesquisa.formal_deliverables(
    id,organization_id,diagnostic_id,result_id,deliverable_type,title,status,generated_by_user_id,
    editorial_status,processing_status,version_number,template_code,sections_json,executive_notes,
    reviewer_user_id,published_at,published_by,source_result_hash,methodology_name,methodology_version,
    command_id,metadata_json)
  VALUES (
    published_id,org_a,diag_a,result_a,'executive_report','Relatorio publicado (seed)',
    'published',user_a,'published','available',1,'executive_valora',
    '["objective","results","dimensions","limitations","recommendations"]'::jsonb,'Versao publicada de seed.',
    user_a,now(),user_a,NULL,'Valora Insight','seed','seed-published-v1',
    '{"seed":true,"limitations":["Seed de desenvolvimento; arquivo real depende da publicacao."]}'::jsonb)
  ON CONFLICT (id) DO NOTHING;

  INSERT INTO valorapesquisa.secure_share_links(
    id,organization_id,deliverable_id,diagnostic_id,result_id,token_hash,public_slug,title,status,
    expires_at,allow_download,created_by_user_id)
  VALUES
    ('bbbbbbbb-bbbb-cccc-dddd-000000000001',org_a,published_id,diag_a,result_a,
     '1111111111111111111111111111111111111111111111111111111111111111',
     replace('bbbbbbbb-bbbb-cccc-dddd-000000000001'::text,'-',''),'Seed link valido','active',
     now() + interval '7 days', true, user_a),
    ('bbbbbbbb-bbbb-cccc-dddd-000000000002',org_a,published_id,diag_a,result_a,
     '2222222222222222222222222222222222222222222222222222222222222222',
     replace('bbbbbbbb-bbbb-cccc-dddd-000000000002'::text,'-',''),'Seed link expirado','active',
     now() - interval '1 day', false, user_a),
    ('bbbbbbbb-bbbb-cccc-dddd-000000000003',org_a,published_id,diag_a,result_a,
     '3333333333333333333333333333333333333333333333333333333333333333',
     replace('bbbbbbbb-bbbb-cccc-dddd-000000000003'::text,'-',''),'Seed link revogado','revoked',
     now() + interval '7 days', false, user_a)
  ON CONFLICT (id) DO NOTHING;

  UPDATE valorapesquisa.secure_share_links
  SET revoked_at=now()
  WHERE id='bbbbbbbb-bbbb-cccc-dddd-000000000003' AND revoked_at IS NULL;
END $$;
INSERT INTO valorapesquisa.schema_migrations(version,checksum)
VALUES('2026_09_16_executive_delivery_seed','sha256:executive-delivery-seed-v1')
ON CONFLICT(version) DO NOTHING;
COMMIT;
