-- Valora SaaS Administration & Customer Control Center.
-- Additive, non-destructive and safe for clean or partially provisioned databases.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customers (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 legal_name varchar(200) NOT NULL, trade_name varchar(160) NOT NULL, tax_id_normalized varchar(14) NOT NULL,
 plan_code varchar(60) NOT NULL DEFAULT 'free', status varchar(24) NOT NULL DEFAULT 'active' CHECK(status IN ('active','blocked','inactive')),
 blocked_at timestamptz, block_reason text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(organization_id), UNIQUE(tax_id_normalized), CHECK(tax_id_normalized ~ '^[0-9]{11}([0-9]{3})?$'));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_contacts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 name varchar(160) NOT NULL, email varchar(254) NOT NULL, phone varchar(30), contact_type varchar(30) NOT NULL DEFAULT 'primary', is_primary boolean NOT NULL DEFAULT false,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_users (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), status varchar(24) NOT NULL DEFAULT 'active' CHECK(status IN ('invited','active','inactive','blocked')),
 blocked_at timestamptz, block_reason text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(customer_id,user_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_user_profiles (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 name varchar(100) NOT NULL, description text, is_system boolean NOT NULL DEFAULT false, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE(customer_id,name));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_profile_permissions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), profile_id uuid NOT NULL REFERENCES valorapesquisa.saas_customer_user_profiles(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 permission_code varchar(120) NOT NULL, granted_by_user_id uuid REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(profile_id,permission_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_modules (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 module_code varchar(100) NOT NULL, enabled boolean NOT NULL DEFAULT true, enabled_at timestamptz, disabled_at timestamptz, updated_by_user_id uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(customer_id,module_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_feature_flags (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 feature_code varchar(120) NOT NULL, enabled boolean NOT NULL DEFAULT false, configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(customer_id,feature_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_plan_limits (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 limit_code varchar(100) NOT NULL, limit_value bigint NOT NULL CHECK(limit_value >= -1), consumed_value bigint NOT NULL DEFAULT 0 CHECK(consumed_value >= 0), period varchar(24) NOT NULL DEFAULT 'lifetime',
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(customer_id,limit_code));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_billing_accounts (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 billing_email varchar(254) NOT NULL, billing_tax_id_normalized varchar(14) NOT NULL, currency char(3) NOT NULL DEFAULT 'BRL', payment_terms_days integer NOT NULL DEFAULT 10 CHECK(payment_terms_days >= 0),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(customer_id));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_billing_invoices (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 invoice_number varchar(60) NOT NULL, status varchar(24) NOT NULL DEFAULT 'draft' CHECK(status IN ('draft','open','paid','overdue','canceled')),
 currency char(3) NOT NULL DEFAULT 'BRL', total_amount numeric(14,2) NOT NULL DEFAULT 0 CHECK(total_amount >= 0), issued_at timestamptz, due_at timestamptz NOT NULL, paid_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), UNIQUE(invoice_number));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_billing_invoice_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), invoice_id uuid NOT NULL REFERENCES valorapesquisa.saas_billing_invoices(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 description varchar(300) NOT NULL, quantity numeric(12,2) NOT NULL CHECK(quantity > 0), unit_amount numeric(14,2) NOT NULL CHECK(unit_amount >= 0), total_amount numeric(14,2) NOT NULL CHECK(total_amount >= 0),
 created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_payment_records (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), invoice_id uuid NOT NULL REFERENCES valorapesquisa.saas_billing_invoices(id), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 amount numeric(14,2) NOT NULL CHECK(amount > 0), currency char(3) NOT NULL DEFAULT 'BRL', method varchar(40) NOT NULL, external_reference varchar(160), paid_at timestamptz NOT NULL,
 recorded_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_access_blocks (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid REFERENCES valorapesquisa.saas_customers(id), customer_user_id uuid REFERENCES valorapesquisa.saas_customer_users(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 block_type varchar(40) NOT NULL, reason text NOT NULL, active boolean NOT NULL DEFAULT true, blocked_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), blocked_at timestamptz NOT NULL DEFAULT now(),
 unblocked_by_user_id uuid REFERENCES valorapesquisa.users(id), unblocked_at timestamptz, CHECK(customer_id IS NOT NULL OR customer_user_id IS NOT NULL));
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_admin_actions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid REFERENCES valorapesquisa.saas_customers(id), organization_id uuid REFERENCES valorapesquisa.organizations(id),
 actor_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), action varchar(100) NOT NULL, target_type varchar(60) NOT NULL, target_id uuid, reason text,
 correlation_id varchar(100) NOT NULL, before_json jsonb, after_json jsonb, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_customer_audit_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_id uuid NOT NULL REFERENCES valorapesquisa.saas_customers(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 actor_user_id uuid REFERENCES valorapesquisa.users(id), event_type varchar(120) NOT NULL, entity_type varchar(60) NOT NULL, entity_id uuid, summary text NOT NULL,
 correlation_id varchar(100) NOT NULL, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS valorapesquisa.saas_login_identifiers (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), customer_user_id uuid NOT NULL REFERENCES valorapesquisa.saas_customer_users(id), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 identifier_type varchar(16) NOT NULL CHECK(identifier_type IN ('email','cpf','cnpj')), normalized_value varchar(254) NOT NULL, verified_at timestamptz,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE(identifier_type,normalized_value));

CREATE INDEX IF NOT EXISTS ix_saas_customer_users_tenant ON valorapesquisa.saas_customer_users(organization_id,status);
CREATE INDEX IF NOT EXISTS ix_saas_invoices_tenant_status ON valorapesquisa.saas_billing_invoices(organization_id,status,due_at);
CREATE INDEX IF NOT EXISTS ix_saas_blocks_tenant_active ON valorapesquisa.saas_access_blocks(organization_id,active);
CREATE INDEX IF NOT EXISTS ix_saas_audit_tenant_date ON valorapesquisa.saas_customer_audit_events(organization_id,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_saas_admin_actions_date ON valorapesquisa.saas_admin_actions(created_at DESC);

INSERT INTO valorapesquisa.permissions(code,name,description,module_code) VALUES
 ('saas_admin.view','Consultar administração SaaS','Consultar a visão global da plataforma.','operations'),
 ('saas_admin.manage','Gerenciar administração SaaS','Gerenciar clientes da plataforma.','operations'),
 ('saas_customers.view','Consultar clientes SaaS','Consultar clientes autorizados.','operations'),
 ('saas_customers.manage','Gerenciar clientes SaaS','Cadastrar e atualizar clientes.','operations'),
 ('saas_customers.block','Bloquear clientes SaaS','Aplicar bloqueios reversíveis e auditados.','operations'),
 ('saas_users.manage','Gerenciar usuários SaaS','Gerenciar usuários de clientes.','identity'),
 ('saas_users.block','Bloquear usuários SaaS','Aplicar bloqueios reversíveis a usuários.','identity'),
 ('saas_modules.manage','Gerenciar módulos SaaS','Gerenciar módulos contratados.','operations'),
 ('saas_billing.view','Consultar cobrança SaaS','Consultar faturas e pagamentos.','organization'),
 ('saas_billing.manage','Gerenciar cobrança SaaS','Gerar faturas e registrar pagamentos.','organization'),
 ('saas_impersonation.use','Usar contexto de suporte','Entrar no contexto de cliente com auditoria.','operations'),
 ('organization_users.manage','Gerenciar usuários da organização','Gerenciar usuários apenas da própria organização.','identity'),
 ('organization_profiles.manage','Gerenciar perfis da organização','Gerenciar perfis apenas da própria organização.','identity')
ON CONFLICT(code) DO UPDATE SET name=excluded.name,description=excluded.description,module_code=excluded.module_code,updated_at=now();
