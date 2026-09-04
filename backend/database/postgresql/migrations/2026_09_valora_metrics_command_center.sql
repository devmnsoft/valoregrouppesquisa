-- Valora Metrics™ and Command Center™ canonical, additive and legacy-safe schema.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.valora_metrics (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL,
    code varchar(80) NOT NULL, name varchar(160) NOT NULL, description text NOT NULL,
    formula text NOT NULL, category varchar(80) NOT NULL, valora_dimension varchar(80) NOT NULL,
    interpretation text NOT NULL, trend_direction varchar(30) NOT NULL DEFAULT 'insufficient_data',
    responsible_user_id uuid, periodicity varchar(30) NOT NULL, last_calculated_at timestamptz,
    data_quality_status varchar(30) NOT NULL DEFAULT 'insufficient_data', status varchar(20) NOT NULL DEFAULT 'active',
    created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_valora_metrics_org_code UNIQUE (organization_id, code),
    CONSTRAINT ck_valora_metrics_formula CHECK (length(trim(formula)) > 0)
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_metric_sources (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, metric_id uuid NOT NULL REFERENCES valorapesquisa.valora_metrics(id),
    source_name varchar(160) NOT NULL, source_type varchar(40) NOT NULL, quality_status varchar(30) NOT NULL DEFAULT 'pending',
    last_synchronized_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), CONSTRAINT uq_valora_metric_source UNIQUE(metric_id, source_name)
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_metric_values (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, metric_id uuid NOT NULL REFERENCES valorapesquisa.valora_metrics(id),
    value numeric(18,4), measured_at timestamptz NOT NULL, sample_size integer NOT NULL DEFAULT 0,
    quality_status varchar(30) NOT NULL DEFAULT 'insufficient_data', calculation_correlation_id varchar(128), created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_valora_metric_value UNIQUE(metric_id, measured_at), CONSTRAINT ck_valora_metric_sample CHECK(sample_size >= 0)
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_metric_targets (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, metric_id uuid NOT NULL REFERENCES valorapesquisa.valora_metrics(id),
    target_value numeric(18,4) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, responsible_user_id uuid,
    status varchar(20) NOT NULL DEFAULT 'active', created_at timestamptz NOT NULL DEFAULT now(), CONSTRAINT ck_valora_metric_target_period CHECK(period_end >= period_start)
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_metric_history (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, metric_id uuid NOT NULL REFERENCES valorapesquisa.valora_metrics(id),
    event_type varchar(60) NOT NULL, previous_data jsonb, current_data jsonb NOT NULL DEFAULT '{}'::jsonb,
    actor_user_id uuid, correlation_id varchar(128), occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_metric_alerts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, metric_id uuid REFERENCES valorapesquisa.valora_metrics(id),
    severity varchar(20) NOT NULL, origin varchar(80) NOT NULL, message text NOT NULL, recommended_action text NOT NULL, resolution_url text NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'open', read_at timestamptz, read_by_user_id uuid, resolved_at timestamptz, resolved_by_user_id uuid,
    resolution_note text, created_at timestamptz NOT NULL DEFAULT now(), CONSTRAINT ck_valora_alert_severity CHECK(severity IN ('low','medium','high','critical')),
    CONSTRAINT ck_valora_alert_status CHECK(status IN ('open','read','resolved','archived'))
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_command_center_widgets (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), code varchar(80) NOT NULL UNIQUE, name varchar(120) NOT NULL,
    description text NOT NULL, module varchar(60) NOT NULL, default_position integer NOT NULL DEFAULT 0, active boolean NOT NULL DEFAULT true
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_dashboard_preferences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, user_id uuid NOT NULL, widget_id uuid NOT NULL REFERENCES valorapesquisa.valora_command_center_widgets(id),
    visible boolean NOT NULL DEFAULT true, display_order integer NOT NULL DEFAULT 0, settings jsonb NOT NULL DEFAULT '{}'::jsonb, updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_valora_dashboard_preference UNIQUE(organization_id,user_id,widget_id)
);
CREATE TABLE IF NOT EXISTS valorapesquisa.valora_user_alert_preferences (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL, user_id uuid NOT NULL, origin varchar(80) NOT NULL,
    minimum_severity varchar(20) NOT NULL DEFAULT 'medium', in_app boolean NOT NULL DEFAULT true, email boolean NOT NULL DEFAULT false,
    updated_at timestamptz NOT NULL DEFAULT now(), CONSTRAINT uq_valora_alert_preference UNIQUE(organization_id,user_id,origin)
);

CREATE INDEX IF NOT EXISTS ix_valora_metrics_org_status ON valorapesquisa.valora_metrics(organization_id,status);
CREATE INDEX IF NOT EXISTS ix_valora_metric_values_metric_date ON valorapesquisa.valora_metric_values(metric_id,measured_at DESC);
CREATE INDEX IF NOT EXISTS ix_valora_metric_alerts_org_status_severity ON valorapesquisa.valora_metric_alerts(organization_id,status,severity);
CREATE INDEX IF NOT EXISTS ix_valora_metric_history_metric_date ON valorapesquisa.valora_metric_history(metric_id,occurred_at DESC);

INSERT INTO valorapesquisa.valora_command_center_widgets(code,name,description,module,default_position)
VALUES ('executive_metrics','Indicadores executivos','Indicadores oficiais com qualidade e tendência.','metrics',10),
       ('critical_alerts','Alertas críticos','Riscos que exigem resolução rastreável.','alerts',20),
       ('next_actions','Próximas ações','Próximos passos baseados em sinais disponíveis.','actions',30),
       ('evolution','Evolução','Histórico de ciclos comparáveis.','metrics',40)
ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module=EXCLUDED.module,default_position=EXCLUDED.default_position;
