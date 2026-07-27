BEGIN;
SET search_path TO valorapesquisa, public;

CREATE TABLE IF NOT EXISTS plan_usage_counters (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES organizations(id),
    metric_key text NOT NULL,
    period_start date NOT NULL,
    consumed bigint NOT NULL DEFAULT 0 CHECK (consumed >= 0),
    reserved bigint NOT NULL DEFAULT 0 CHECK (reserved >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (organization_id, metric_key, period_start)
);

CREATE TABLE IF NOT EXISTS plan_usage_reservations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES organizations(id),
    metric_key text NOT NULL,
    quantity bigint NOT NULL CHECK (quantity > 0),
    status text NOT NULL DEFAULT 'reserved' CHECK (status IN ('reserved','confirmed','released','expired')),
    idempotency_key text NOT NULL,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (organization_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS ix_plan_usage_reservations_active
    ON plan_usage_reservations (organization_id, metric_key, expires_at)
    WHERE status = 'reserved';

CREATE TABLE IF NOT EXISTS user_scopes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES users(id),
    organization_id uuid NOT NULL REFERENCES organizations(id),
    business_group_id uuid REFERENCES business_groups(id),
    legal_entity_id uuid REFERENCES legal_entities(id),
    unit_id uuid REFERENCES units(id),
    department_id uuid REFERENCES departments(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_user_scopes_active
    ON user_scopes (user_id, organization_id,
        COALESCE(business_group_id, '00000000-0000-0000-0000-000000000000'::uuid),
        COALESCE(legal_entity_id, '00000000-0000-0000-0000-000000000000'::uuid),
        COALESCE(unit_id, '00000000-0000-0000-0000-000000000000'::uuid),
        COALESCE(department_id, '00000000-0000-0000-0000-000000000000'::uuid))
    WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS subscription_history (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id uuid NOT NULL REFERENCES subscriptions(id),
    organization_id uuid NOT NULL REFERENCES organizations(id),
    previous_status text,
    new_status text NOT NULL,
    previous_plan_id uuid REFERENCES plans(id),
    new_plan_id uuid REFERENCES plans(id),
    changed_by uuid REFERENCES users(id),
    reason text,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_subscription_history_organization
    ON subscription_history (organization_id, created_at DESC);

INSERT INTO schema_migrations(version, checksum)
VALUES ('20260727_003_identity_multitenant_core', 'phase2b-identity-core-v1')
ON CONFLICT (version) DO NOTHING;

COMMIT;
