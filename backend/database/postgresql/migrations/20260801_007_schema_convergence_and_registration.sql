BEGIN;
SET search_path TO valorapesquisa, public;

-- Converge historical installations without losing counter or reservation data.
DO $$
BEGIN
  IF to_regclass('valorapesquisa.plan_usage_counters') IS NULL THEN
    CREATE TABLE plan_usage_counters (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), metric_key text NOT NULL, period_start date NOT NULL, consumed bigint NOT NULL DEFAULT 0, reserved bigint NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
  ELSE
    ALTER TABLE plan_usage_counters ADD COLUMN IF NOT EXISTS id uuid DEFAULT gen_random_uuid();
    ALTER TABLE plan_usage_counters ADD COLUMN IF NOT EXISTS metric_key text;
    ALTER TABLE plan_usage_counters ADD COLUMN IF NOT EXISTS period_start date DEFAULT date_trunc('month', CURRENT_DATE)::date;
    ALTER TABLE plan_usage_counters ADD COLUMN IF NOT EXISTS consumed bigint DEFAULT 0;
    ALTER TABLE plan_usage_counters ADD COLUMN IF NOT EXISTS reserved bigint DEFAULT 0;
    ALTER TABLE plan_usage_counters ADD COLUMN IF NOT EXISTS created_at timestamptz DEFAULT now();
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_counters' AND column_name='resource_code') THEN
      UPDATE plan_usage_counters SET metric_key=resource_code WHERE metric_key IS NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_counters' AND column_name='used_value') THEN
      UPDATE plan_usage_counters SET consumed=used_value WHERE consumed=0;
    END IF;
    ALTER TABLE plan_usage_counters DROP COLUMN IF EXISTS resource_code;
    ALTER TABLE plan_usage_counters DROP COLUMN IF EXISTS used_value;
  END IF;
END $$;
ALTER TABLE plan_usage_counters ALTER COLUMN id SET NOT NULL, ALTER COLUMN metric_key SET NOT NULL, ALTER COLUMN period_start SET NOT NULL, ALTER COLUMN consumed SET NOT NULL, ALTER COLUMN reserved SET NOT NULL, ALTER COLUMN created_at SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_usage_counters_period ON plan_usage_counters(organization_id,metric_key,period_start);

DO $$
BEGIN
  IF to_regclass('valorapesquisa.plan_usage_reservations') IS NULL THEN
    CREATE TABLE plan_usage_reservations (id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES organizations(id), metric_key text NOT NULL, quantity bigint NOT NULL, status text NOT NULL DEFAULT 'reserved', idempotency_key text NOT NULL, expires_at timestamptz NOT NULL, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now());
  ELSE
    ALTER TABLE plan_usage_reservations ADD COLUMN IF NOT EXISTS metric_key text;
    ALTER TABLE plan_usage_reservations ADD COLUMN IF NOT EXISTS quantity bigint;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_reservations' AND column_name='resource_code') THEN
      UPDATE plan_usage_reservations SET metric_key=resource_code WHERE metric_key IS NULL;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='valorapesquisa' AND table_name='plan_usage_reservations' AND column_name='amount') THEN
      UPDATE plan_usage_reservations SET quantity=amount WHERE quantity IS NULL;
    END IF;
    ALTER TABLE plan_usage_reservations DROP COLUMN IF EXISTS resource_code;
    ALTER TABLE plan_usage_reservations DROP COLUMN IF EXISTS amount;
  END IF;
END $$;
ALTER TABLE plan_usage_reservations ALTER COLUMN metric_key SET NOT NULL, ALTER COLUMN quantity SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_usage_reservation_idempotency ON plan_usage_reservations(organization_id,idempotency_key);
CREATE INDEX IF NOT EXISTS ix_plan_usage_reservations_active ON plan_usage_reservations(organization_id,metric_key,expires_at) WHERE status='reserved';
DROP INDEX IF EXISTS ux_legal_entities_org_cnpj_active;
CREATE UNIQUE INDEX IF NOT EXISTS ux_legal_entities_cnpj_active ON legal_entities(cnpj) WHERE deleted_at IS NULL;
COMMIT;
