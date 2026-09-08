-- Canonical modular SaaS foundation for Valora Insight.
-- Additive, idempotent and compatible with the existing organization/access model.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.saas_modules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(80) NOT NULL UNIQUE,
 commercial_name varchar(160) NOT NULL, description text NOT NULL, base_price numeric(14,2) NOT NULL DEFAULT 0 CHECK(base_price >= 0),
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('active','inactive')),
 icon varchar(60) NOT NULL, main_route varchar(200) NOT NULL, menu_category varchar(80) NOT NULL,
 requires_contract boolean NOT NULL DEFAULT true, access_module_code varchar(80) NOT NULL,
 display_order integer NOT NULL DEFAULT 100, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.saas_module_features (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), module_id uuid NOT NULL REFERENCES valorapesquisa.saas_modules(id),
 code varchar(120) NOT NULL, name varchar(160) NOT NULL, description text,
 permission_code varchar(160), status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('active','inactive')),
 display_order integer NOT NULL DEFAULT 100, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz, UNIQUE(module_id,code));

CREATE TABLE IF NOT EXISTS valorapesquisa.saas_module_prices (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), module_id uuid NOT NULL REFERENCES valorapesquisa.saas_modules(id),
 currency char(3) NOT NULL DEFAULT 'BRL', billing_cycle varchar(20) NOT NULL DEFAULT 'monthly' CHECK(billing_cycle IN ('monthly','yearly')),
 amount numeric(14,2) NOT NULL CHECK(amount >= 0), status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('active','inactive')),
 effective_from timestamptz NOT NULL DEFAULT now(), effective_until timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(module_id,currency,billing_cycle,effective_from));

CREATE TABLE IF NOT EXISTS valorapesquisa.saas_plans (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(60) NOT NULL UNIQUE, name varchar(120) NOT NULL,
 description text NOT NULL, monthly_price numeric(14,2) NOT NULL DEFAULT 0 CHECK(monthly_price >= 0),
 annual_price numeric(14,2) NOT NULL DEFAULT 0 CHECK(annual_price >= 0),
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('active','inactive')),
 is_public boolean NOT NULL DEFAULT true, display_order integer NOT NULL DEFAULT 100,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.saas_plan_modules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), plan_id uuid NOT NULL REFERENCES valorapesquisa.saas_plans(id),
 module_id uuid NOT NULL REFERENCES valorapesquisa.saas_modules(id), included boolean NOT NULL DEFAULT true,
 access_mode varchar(20) NOT NULL DEFAULT 'full' CHECK(access_mode IN ('full','read_only')),
 limits_jsonb jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(plan_id,module_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.client_subscriptions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 plan_id uuid NOT NULL REFERENCES valorapesquisa.saas_plans(id), status varchar(24) NOT NULL DEFAULT 'active'
 CHECK(status IN ('trialing','active','suspended','expired','cancelled')),
 billing_cycle varchar(20) NOT NULL DEFAULT 'monthly' CHECK(billing_cycle IN ('monthly','yearly')),
 contracted_price numeric(14,2) NOT NULL DEFAULT 0 CHECK(contracted_price >= 0),
 starts_at timestamptz NOT NULL DEFAULT now(), trial_ends_at timestamptz, ends_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE UNIQUE INDEX IF NOT EXISTS ux_client_subscriptions_active ON valorapesquisa.client_subscriptions(client_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.client_subscription_modules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), subscription_id uuid NOT NULL REFERENCES valorapesquisa.client_subscriptions(id),
 client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id), module_id uuid NOT NULL REFERENCES valorapesquisa.saas_modules(id),
 status varchar(24) NOT NULL DEFAULT 'active' CHECK(status IN ('active','read_only','suspended','expired','cancelled')),
 source varchar(24) NOT NULL DEFAULT 'plan' CHECK(source IN ('plan','addon','trial','manual')),
 contracted_at timestamptz NOT NULL DEFAULT now(), suspended_at timestamptz, cancelled_at timestamptz,
 updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(subscription_id,module_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.client_module_usage (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 module_id uuid NOT NULL REFERENCES valorapesquisa.saas_modules(id), metric_code varchar(100) NOT NULL,
 period_start date NOT NULL, period_end date NOT NULL, used_quantity bigint NOT NULL DEFAULT 0 CHECK(used_quantity >= 0),
 limit_quantity bigint CHECK(limit_quantity IS NULL OR limit_quantity >= 0), metadata_jsonb jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(client_id,module_id,metric_code,period_start), CHECK(period_end >= period_start));

CREATE TABLE IF NOT EXISTS valorapesquisa.client_users (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), status varchar(24) NOT NULL DEFAULT 'active'
 CHECK(status IN ('invited','active','inactive','blocked')),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(client_id,user_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.client_profiles (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 code varchar(80) NOT NULL, name varchar(120) NOT NULL, description text, is_system boolean NOT NULL DEFAULT false,
 status varchar(20) NOT NULL DEFAULT 'active' CHECK(status IN ('active','inactive')),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(client_id,code));

CREATE TABLE IF NOT EXISTS valorapesquisa.client_user_profiles (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 client_user_id uuid NOT NULL REFERENCES valorapesquisa.client_users(id), profile_id uuid NOT NULL REFERENCES valorapesquisa.client_profiles(id),
 assigned_by_user_id uuid REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(client_user_id,profile_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.client_profile_permissions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 profile_id uuid NOT NULL REFERENCES valorapesquisa.client_profiles(id), permission_code varchar(160) NOT NULL,
 module_id uuid REFERENCES valorapesquisa.saas_modules(id), granted_by_user_id uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(profile_id,permission_code));

CREATE TABLE IF NOT EXISTS valorapesquisa.module_access_audit (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 module_id uuid NOT NULL REFERENCES valorapesquisa.saas_modules(id), actor_user_id uuid REFERENCES valorapesquisa.users(id),
 action varchar(100) NOT NULL, previous_status varchar(24), new_status varchar(24), reason text,
 correlation_id varchar(128) NOT NULL, metadata_jsonb jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_audit_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), client_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid REFERENCES valorapesquisa.client_subscriptions(id), actor_user_id uuid REFERENCES valorapesquisa.users(id),
 event_type varchar(120) NOT NULL, reason text, correlation_id varchar(128) NOT NULL,
 before_jsonb jsonb, after_jsonb jsonb, created_at timestamptz NOT NULL DEFAULT now());

CREATE INDEX IF NOT EXISTS ix_saas_modules_menu ON valorapesquisa.saas_modules(menu_category,display_order) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_client_subscription_modules_access ON valorapesquisa.client_subscription_modules(client_id,module_id,status);
CREATE INDEX IF NOT EXISTS ix_client_module_usage_period ON valorapesquisa.client_module_usage(client_id,period_start,period_end);
CREATE INDEX IF NOT EXISTS ix_client_users_status ON valorapesquisa.client_users(client_id,status) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_module_access_audit_client ON valorapesquisa.module_access_audit(client_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_subscription_audit_client ON valorapesquisa.subscription_audit_events(client_id,created_at DESC);

INSERT INTO valorapesquisa.saas_modules(code,commercial_name,description,base_price,icon,main_route,menu_category,requires_contract,access_module_code,display_order) VALUES
 ('diagnostics','Diagnósticos','Ciclos de diagnóstico organizacional com acompanhamento e recálculo.',149,'activity','/Diagnostics','Diagnóstico',true,'surveys',10),
 ('forms','Formulários','Criação, versionamento, publicação, preview e arquivamento de formulários.',99,'file-text','/Forms','Diagnóstico',true,'forms',20),
 ('surveys','Pesquisas','Convites, distribuição, respostas e progresso de campanhas.',129,'file-question','/Surveys','Diagnóstico',true,'surveys',30),
 ('results','Resultados','Visão executiva, evidências e limitações metodológicas.',179,'chart-radar','/Results','Inteligência',true,'results',40),
 ('reports','Relatórios','Geração, download e compartilhamento de entregáveis.',129,'file-text','/Reports','Inteligência',true,'results',50),
 ('certificates','Certificados','Emissão, download, validação e auditoria de certificados.',79,'certificate','/Certificates','Inteligência',true,'certificates',60),
 ('action_center','Action Center','Planos, responsáveis, prazos e execução rastreável.',149,'check-circle','/ActionCenter','Ações',true,'organizational_intelligence',70),
 ('evolution','Evolution','Ciclos, snapshots e evolução histórica preservada.',149,'activity','/Evolution','Evolução',true,'organizational_intelligence',80),
 ('journey','Journey','Linha do tempo e memória organizacional auditável.',119,'file-text','/Journey','Evolução',true,'organizational_intelligence',90),
 ('indicators','Indicators','KPIs estratégicos, filtros e medições rastreáveis.',139,'chart-radar','/Indicators','Inteligência',true,'organizational_intelligence',100),
 ('benchmarks','Benchmarks','Comparações anônimas por segmento e coorte.',199,'layers','/Benchmarks','Inteligência',true,'organizational_intelligence',110),
 ('methodology_studio','Methodology Studio','Catálogos, dimensões, vínculos e versões oficiais.',249,'sparkles','/Methodology','Governança',true,'organizational_intelligence',120),
 ('valora_ai','IA Valora','Insights explicáveis com evidência e revisão humana.',299,'brain','/Insights','Inteligência',true,'organizational_intelligence',130),
 ('data_hub','Data Hub','Importações, qualidade, integrações e logs de processamento.',199,'layers','/DataHub','Dados',true,'operations',140),
 ('governance','Governance','Decisões, responsáveis, reuniões e auditoria.',179,'shield','/Governance','Governança',true,'organizational_intelligence',150),
 ('security_compliance','Security & Compliance','LGPD, controles, incidentes e revisões de acesso.',199,'shield','/SecurityCompliance','Governança',true,'identity',160),
 ('administration','Administração','Usuários, perfis, estrutura e configurações do cliente.',0,'settings','/Admin','Administração do Cliente',false,'organization',170),
 ('subscriptions','Planos e Assinaturas','Plano, módulos, limites, consumo e upgrade.',0,'credit-card','/Subscription','Planos e Assinaturas',false,'organization',180),
 ('success_center','Suporte / Success Center','Onboarding, ajuda, chamados e saúde da conta.',0,'message-circle','/SuccessCenter','Suporte',false,'organization',190)
ON CONFLICT(code) DO UPDATE SET commercial_name=excluded.commercial_name,description=excluded.description,base_price=excluded.base_price,
 icon=excluded.icon,main_route=excluded.main_route,menu_category=excluded.menu_category,requires_contract=excluded.requires_contract,
 access_module_code=excluded.access_module_code,display_order=excluded.display_order,updated_at=now();

INSERT INTO valorapesquisa.saas_module_prices(module_id,currency,billing_cycle,amount,effective_from)
SELECT id,'BRL','monthly',base_price,'2026-09-01'::timestamptz FROM valorapesquisa.saas_modules
ON CONFLICT(module_id,currency,billing_cycle,effective_from) DO UPDATE SET amount=excluded.amount,status='active',updated_at=now();

INSERT INTO valorapesquisa.saas_module_features(module_id,code,name,description,permission_code,display_order)
SELECT id,code||'.access','Acesso ao módulo','Permissão funcional básica do módulo.',
 CASE code WHEN 'forms' THEN 'forms.read' WHEN 'surveys' THEN 'surveys.read' WHEN 'results' THEN 'results.read'
 WHEN 'reports' THEN 'reports.read' WHEN 'certificates' THEN 'certificates.read' WHEN 'action_center' THEN 'action.read'
 WHEN 'evolution' THEN 'evolution.read' WHEN 'journey' THEN 'journey.read' WHEN 'indicators' THEN 'indicators.read'
 WHEN 'benchmarks' THEN 'benchmarks.view' WHEN 'methodology_studio' THEN 'methodology.read' WHEN 'valora_ai' THEN 'insights.read'
 WHEN 'governance' THEN 'governance.read' WHEN 'security_compliance' THEN 'security_compliance.read' ELSE NULL END,10
FROM valorapesquisa.saas_modules
ON CONFLICT(module_id,code) DO UPDATE SET name=excluded.name,description=excluded.description,permission_code=excluded.permission_code,updated_at=now();

INSERT INTO valorapesquisa.saas_plans(code,name,description,monthly_price,annual_price,display_order) VALUES
 ('free','Free','Essenciais para iniciar o primeiro diagnóstico.',0,0,10),
 ('start','Start','Operação estruturada para equipes em evolução.',499,4990,20),
 ('growth','Growth','Inteligência, governança e escala organizacional.',1199,11990,30),
 ('enterprise','Enterprise','Plataforma completa, limites personalizados e suporte dedicado.',0,0,40)
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,monthly_price=excluded.monthly_price,
 annual_price=excluded.annual_price,display_order=excluded.display_order,updated_at=now();

INSERT INTO valorapesquisa.saas_plan_modules(plan_id,module_id,included,access_mode)
SELECT p.id,m.id,true,'full' FROM valorapesquisa.saas_plans p JOIN valorapesquisa.saas_modules m ON
 (p.code='free' AND m.code IN ('diagnostics','forms','surveys','results','administration','subscriptions','success_center')) OR
 (p.code='start' AND m.code IN ('diagnostics','forms','surveys','results','reports','certificates','action_center','evolution','administration','subscriptions','success_center')) OR
 (p.code='growth' AND m.code NOT IN ('methodology_studio')) OR p.code='enterprise'
ON CONFLICT(plan_id,module_id) DO UPDATE SET included=true,access_mode=excluded.access_mode,updated_at=now();

INSERT INTO valorapesquisa.client_subscriptions(client_id,plan_id,status,billing_cycle,contracted_price,starts_at,ends_at)
SELECT DISTINCT ON (s.organization_id) s.organization_id,sp.id,
 CASE WHEN s.status IN ('active','trialing','suspended','expired','cancelled') THEN s.status ELSE 'suspended' END,
 'monthly',sp.monthly_price,s.starts_at,s.ends_at
FROM valorapesquisa.subscriptions s JOIN valorapesquisa.plans p ON p.id=s.plan_id
JOIN valorapesquisa.saas_plans sp ON sp.code=CASE WHEN p.code='professional' THEN 'growth' ELSE p.code END
WHERE s.deleted_at IS NULL ORDER BY s.organization_id,s.created_at DESC
ON CONFLICT(client_id) WHERE deleted_at IS NULL DO UPDATE SET plan_id=excluded.plan_id,status=excluded.status,ends_at=excluded.ends_at,updated_at=now();

INSERT INTO valorapesquisa.client_subscription_modules(subscription_id,client_id,module_id,status,source)
SELECT cs.id,cs.client_id,spm.module_id,CASE WHEN cs.status IN ('active','trialing') THEN 'active' ELSE 'read_only' END,'plan'
FROM valorapesquisa.client_subscriptions cs JOIN valorapesquisa.saas_plan_modules spm ON spm.plan_id=cs.plan_id AND spm.included
WHERE cs.deleted_at IS NULL
ON CONFLICT(subscription_id,module_id) DO NOTHING;

INSERT INTO valorapesquisa.client_users(client_id,user_id,status)
SELECT organization_id,id,CASE WHEN status IN ('active','inactive','blocked') THEN status ELSE 'inactive' END
FROM valorapesquisa.users WHERE organization_id IS NOT NULL AND deleted_at IS NULL
ON CONFLICT(client_id,user_id) DO UPDATE SET status=excluded.status,updated_at=now();

INSERT INTO valorapesquisa.client_profiles(client_id,code,name,description,is_system)
SELECT o.id,p.code,p.name,p.description,true FROM valorapesquisa.organizations o CROSS JOIN (VALUES
 ('client_admin','Admin do Cliente','Administração completa do cliente.'),('manager','Gestor','Gestão de ciclos, resultados e ações.'),
 ('analyst','Analista','Análise de evidências, resultados e relatórios.'),('consultant','Consultor','Operação consultiva com escopo autorizado.'),
 ('respondent','Respondente','Participação em pesquisas autorizadas.'),('executive_viewer','Visualizador Executivo','Consulta executiva sem alterações.')) AS p(code,name,description)
ON CONFLICT(client_id,code) DO UPDATE SET name=excluded.name,description=excluded.description,updated_at=now();
