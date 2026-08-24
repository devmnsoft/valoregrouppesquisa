
-- Valora One-on-One™: contrato canônico e evolução idempotente de instalações parciais.
ALTER TABLE valorapesquisa.one_on_one_sessions
 ADD COLUMN IF NOT EXISTS leader_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS participant_user_id uuid REFERENCES valorapesquisa.users(id),
 ADD COLUMN IF NOT EXISTS title varchar(200),
 ADD COLUMN IF NOT EXISTS purpose text,
 ADD COLUMN IF NOT EXISTS scheduled_at timestamptz,
 ADD COLUMN IF NOT EXISTS started_at timestamptz,
 ADD COLUMN IF NOT EXISTS completed_at timestamptz,
 ADD COLUMN IF NOT EXISTS canceled_at timestamptz,
 ADD COLUMN IF NOT EXISTS duration_minutes integer,
 ADD COLUMN IF NOT EXISTS summary text,
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
