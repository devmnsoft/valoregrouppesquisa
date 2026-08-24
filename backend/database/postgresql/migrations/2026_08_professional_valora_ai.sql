-- Camada profissional de IA Valora Insight. Idempotente e preserva todo o histórico.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_prompt_templates (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code text NOT NULL UNIQUE, name text NOT NULL,
 objective text NOT NULL, status text NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_prompt_versions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), template_id uuid NOT NULL REFERENCES valorapesquisa.ai_prompt_templates(id),
 version integer NOT NULL, system_instructions text NOT NULL, user_template text NOT NULL, output_schema jsonb NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(template_id, version));
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_runs (
 id uuid PRIMARY KEY, organization_id uuid NOT NULL, diagnosis_id uuid, prompt_version_id uuid REFERENCES valorapesquisa.ai_prompt_versions(id),
 provider text, model text, status text NOT NULL, correlation_id text NOT NULL, origin_user_id uuid, origin_job_id uuid,
 started_at timestamptz, completed_at timestamptz, duration_ms bigint, error_message text,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_ai_runs_org_created ON valorapesquisa.ai_runs(organization_id, created_at DESC);
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_run_inputs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.ai_runs(id), evidence_pack jsonb NOT NULL,
 input_hash text NOT NULL, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_run_outputs (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.ai_runs(id), output_json jsonb,
 raw_output text, publication_status text NOT NULL DEFAULT 'draft', created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_run_validations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.ai_runs(id), is_valid boolean NOT NULL,
 violations jsonb NOT NULL DEFAULT '[]', created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_guardrail_violations (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.ai_runs(id), code text NOT NULL,
 detail text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_review_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.ai_runs(id), reviewer_id uuid NOT NULL,
 from_status text, to_status text NOT NULL, note text, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_usage_metrics (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), run_id uuid NOT NULL REFERENCES valorapesquisa.ai_runs(id), organization_id uuid NOT NULL,
 provider text NOT NULL, model text NOT NULL, input_tokens integer NOT NULL DEFAULT 0, output_tokens integer NOT NULL DEFAULT 0,
 estimated_cost numeric(14,6) NOT NULL DEFAULT 0, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_ai_usage_org_month ON valorapesquisa.ai_usage_metrics(organization_id, occurred_at);
CREATE TABLE IF NOT EXISTS valorapesquisa.ai_provider_settings (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid, provider text NOT NULL, model text NOT NULL,
 secret_reference text, enabled boolean NOT NULL DEFAULT false, require_human_review boolean NOT NULL DEFAULT true,
 monthly_run_limit integer NOT NULL DEFAULT 0 CHECK (monthly_run_limit >= 0), alert_threshold numeric(4,3) NOT NULL DEFAULT .8,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());

INSERT INTO valorapesquisa.ai_prompt_templates(code,name,objective,status)
SELECT p.code,p.name,p.objective,'active' FROM (VALUES
 ('executive_reading','Leitura executiva','Interpretar evidências para decisão executiva'),('insights','Geração de insights','Gerar insights rastreáveis'),
 ('risks','Análise de riscos','Identificar riscos evidenciados'),('probable_causes','Causas prováveis','Distinguir causas prováveis de sintomas'),
 ('recommendations','Recomendações','Recomendar evolução vinculada a causas'),('action_plan','Plano de ação','Estruturar ações priorizadas'),
 ('executive_report','Relatório executivo','Compor relatório metodológico'),('dashboard_summary','Resumo para dashboard','Resumir sem inventar informação'),
 ('dimension_interpretation','Interpretação por dimensão','Interpretar dimensões e conceitos'),('historical_evolution','Evolução histórica','Comparar evolução com evidências')
) p(code,name,objective) ON CONFLICT(code) DO UPDATE SET name=excluded.name, objective=excluded.objective, updated_at=now();

INSERT INTO valorapesquisa.ai_prompt_versions(template_id,version,system_instructions,user_template,output_schema)
SELECT id,1,
 'A IA do Valora não é chatbot. Interprete organizações. Nunca invente dados ou estatísticas, conclua sem evidência, trate sintomas como causas, use frases motivacionais, julgamento moral ou culpa pessoal. Declare insuficiência de dados quando necessário.',
 'Analise exclusivamente o evidence pack minimizado: {{evidence_pack}}',
 '{"type":"array","items":{"required":["title","interpretation","evidence_ids","impact","priority","priority_justification","confidence","analysis_limitations"]}}'::jsonb
FROM valorapesquisa.ai_prompt_templates ON CONFLICT(template_id,version) DO NOTHING;
