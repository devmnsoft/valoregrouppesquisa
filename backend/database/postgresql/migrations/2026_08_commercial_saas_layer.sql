-- Valora Insight(TM) commercial SaaS layer. Safe for clean and previously provisioned databases.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS monthly_price numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS annual_price numeric(14,2);
ALTER TABLE valorapesquisa.plans ADD COLUMN IF NOT EXISTS display_order integer NOT NULL DEFAULT 0;

ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS billing_cycle text NOT NULL DEFAULT 'monthly';
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS contracted_value numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS discount_value numeric(14,2) NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS renewal_at timestamptz;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS due_at timestamptz;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS canceled_at timestamptz;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS trial_ends_at timestamptz;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS payment_method text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS financial_contact text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS financial_email text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS financial_phone text;
ALTER TABLE valorapesquisa.subscriptions ADD COLUMN IF NOT EXISTS notes text;
UPDATE valorapesquisa.subscriptions SET status = CASE status
 WHEN 'current' THEN 'active' WHEN 'trial' THEN 'trialing'
 WHEN 'overdue' THEN 'past_due' WHEN 'delinquent' THEN 'past_due'
 WHEN 'cancelled' THEN 'canceled' ELSE status END;

CREATE UNIQUE INDEX IF NOT EXISTS ux_subscriptions_active_organization
 ON valorapesquisa.subscriptions(organization_id) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.subscription_usage (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid REFERENCES valorapesquisa.subscriptions(id), competence date NOT NULL, metric_code text NOT NULL,
 quantity bigint NOT NULL DEFAULT 0 CHECK (quantity >= 0), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE (organization_id, competence, metric_code));

CREATE TABLE IF NOT EXISTS valorapesquisa.invoices (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid NOT NULL REFERENCES valorapesquisa.subscriptions(id), competence date NOT NULL, amount numeric(14,2) NOT NULL CHECK(amount >= 0),
 due_at timestamptz NOT NULL, status text NOT NULL DEFAULT 'draft' CHECK(status IN ('draft','open','paid','overdue','canceled')),
 paid_at timestamptz, payment_method text, reference text, notes text, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_invoices_organization_status ON valorapesquisa.invoices(organization_id,status,due_at);

CREATE TABLE IF NOT EXISTS valorapesquisa.invoice_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), invoice_id uuid NOT NULL REFERENCES valorapesquisa.invoices(id) ON DELETE CASCADE,
 description text NOT NULL, quantity numeric(12,2) NOT NULL DEFAULT 1 CHECK(quantity > 0), unit_amount numeric(14,2) NOT NULL CHECK(unit_amount >= 0),
 total_amount numeric(14,2) NOT NULL CHECK(total_amount >= 0), created_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS valorapesquisa.payments (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid NOT NULL REFERENCES valorapesquisa.subscriptions(id), invoice_id uuid REFERENCES valorapesquisa.invoices(id),
 amount numeric(14,2) NOT NULL CHECK(amount > 0), paid_at timestamptz NOT NULL, method text NOT NULL, reference text,
 status text NOT NULL DEFAULT 'confirmed', notes text, created_at timestamptz NOT NULL DEFAULT now());

CREATE TABLE IF NOT EXISTS valorapesquisa.billing_ledger (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 subscription_id uuid REFERENCES valorapesquisa.subscriptions(id), invoice_id uuid REFERENCES valorapesquisa.invoices(id), payment_id uuid REFERENCES valorapesquisa.payments(id),
 entry_type text NOT NULL CHECK(entry_type IN ('debit','credit','adjustment')), amount numeric(14,2) NOT NULL,
 description text NOT NULL, occurred_at timestamptz NOT NULL DEFAULT now(), reference text, created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_billing_ledger_organization ON valorapesquisa.billing_ledger(organization_id,occurred_at DESC);

INSERT INTO valorapesquisa.plans(code,name,description,monthly_price,annual_price,is_public,is_active,is_legacy,display_order)
VALUES ('free','Grátis','Para conhecer o Valora Insight™.',0,0,true,true,false,10),
 ('start','Start','Para iniciar a gestão de diagnósticos.',149,1490,true,true,false,20),
 ('growth','Growth','Escala, inteligência e colaboração.',399,3990,true,true,false,30),
 ('enterprise','Enterprise','Governança multiempresa e limites personalizados.',0,NULL,false,true,false,40)
ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,monthly_price=EXCLUDED.monthly_price,
 annual_price=EXCLUDED.annual_price,is_active=EXCLUDED.is_active,display_order=EXCLUDED.display_order,updated_at=now();

INSERT INTO valorapesquisa.plan_limits(plan_id,limit_key,limit_value,period)
SELECT p.id,v.key,v.value,v.period FROM valorapesquisa.plans p CROSS JOIN (VALUES
 ('diagnosticsCreated',1,'lifetime'),('diagnosticsPublished',1,'lifetime'),('responsesPerMonth',50,'monthly'),('users',2,'lifetime'),('units',1,'lifetime')) v(key,value,period)
WHERE p.code='free' ON CONFLICT(plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value,period=EXCLUDED.period,updated_at=now();

INSERT INTO valorapesquisa.plan_limits(plan_id,limit_key,limit_value,period)
SELECT p.id,v.key,v.value,v.period FROM valorapesquisa.plans p JOIN (VALUES
 ('start','diagnosticsCreated',5,'lifetime'),('start','diagnosticsPublished',5,'lifetime'),('start','responsesPerMonth',500,'monthly'),('start','users',5,'lifetime'),('start','units',3,'lifetime'),
 ('growth','diagnosticsCreated',25,'lifetime'),('growth','diagnosticsPublished',25,'lifetime'),('growth','responsesPerMonth',5000,'monthly'),('growth','users',25,'lifetime'),('growth','units',15,'lifetime'),
 ('enterprise','diagnosticsCreated',-1,'lifetime'),('enterprise','diagnosticsPublished',-1,'lifetime'),('enterprise','responsesPerMonth',-1,'monthly'),('enterprise','users',-1,'lifetime'),('enterprise','units',-1,'lifetime'))
 v(plan,key,value,period) ON v.plan=p.code
ON CONFLICT(plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value,period=EXCLUDED.period,updated_at=now();

INSERT INTO valorapesquisa.plan_features(plan_id,feature_key,enabled)
SELECT p.id,f.feature,(p.code,f.feature) IN (
 ('free','reports'),('start','reports'),('start','certificates'),
 ('growth','reports'),('growth','certificates'),('growth','ai'),('growth','benchmark'),('growth','exports'),
 ('enterprise','reports'),('enterprise','certificates'),('enterprise','ai'),('enterprise','benchmark'),('enterprise','exports'))
FROM valorapesquisa.plans p CROSS JOIN (VALUES ('reports'),('certificates'),('ai'),('benchmark'),('exports')) f(feature)
WHERE p.code IN ('free','start','growth','enterprise')
ON CONFLICT(plan_id,feature_key) WHERE deleted_at IS NULL DO UPDATE SET enabled=EXCLUDED.enabled,updated_at=now();

INSERT INTO valorapesquisa.permissions(code,name,module_code,status)
SELECT code,name,'organization','active' FROM (VALUES
 ('plans.read','Consultar planos'),('plans.manage','Gerenciar planos'),('subscriptions.read','Consultar assinaturas'),
 ('subscriptions.manage','Gerenciar assinaturas'),('billing.read','Consultar cobrança'),('billing.manage','Gerenciar cobrança'),
 ('usage.read','Consultar consumo'),('usage.manage','Gerenciar consumo'),('upgrades.manage','Gerenciar upgrades')) p(code,name)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,module_code=EXCLUDED.module_code,status='active';

INSERT INTO valorapesquisa.role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM valorapesquisa.roles r CROSS JOIN valorapesquisa.permissions p
WHERE lower(r.code)='admin_valora' AND r.organization_id IS NULL
  AND p.code IN ('plans.read','plans.manage','subscriptions.read','subscriptions.manage','billing.read','billing.manage','usage.read','usage.manage','upgrades.manage')
ON CONFLICT DO NOTHING;
