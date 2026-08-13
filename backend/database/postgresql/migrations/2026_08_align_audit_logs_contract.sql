BEGIN;

CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.audit_logs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NULL,
    user_id uuid NULL,
    action text NOT NULL,
    entity_type text NULL,
    entity_id text NULL,
    message text NULL,
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    correlation_id text NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

DO $migration$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'actor_id')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'user_id') THEN
        ALTER TABLE valorapesquisa.audit_logs RENAME COLUMN actor_id TO user_id;
    ELSE
        ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS user_id uuid;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'actor_id') THEN
            UPDATE valorapesquisa.audit_logs SET user_id = actor_id WHERE user_id IS NULL;
            ALTER TABLE valorapesquisa.audit_logs DROP COLUMN actor_id;
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'entity_name')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'entity_type') THEN
        ALTER TABLE valorapesquisa.audit_logs RENAME COLUMN entity_name TO entity_type;
    ELSE
        ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS entity_type text;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'entity_name') THEN
            UPDATE valorapesquisa.audit_logs SET entity_type = entity_name WHERE entity_type IS NULL;
            ALTER TABLE valorapesquisa.audit_logs DROP COLUMN entity_name;
        END IF;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'details')
       AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'metadata_json') THEN
        ALTER TABLE valorapesquisa.audit_logs RENAME COLUMN details TO metadata_json;
    ELSE
        ALTER TABLE valorapesquisa.audit_logs ADD COLUMN IF NOT EXISTS metadata_json jsonb;
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'valorapesquisa' AND table_name = 'audit_logs' AND column_name = 'details') THEN
            UPDATE valorapesquisa.audit_logs SET metadata_json = details WHERE metadata_json IS NULL;
            ALTER TABLE valorapesquisa.audit_logs DROP COLUMN details;
        END IF;
    END IF;
END $migration$;

ALTER TABLE valorapesquisa.audit_logs
    ADD COLUMN IF NOT EXISTS message text,
    ADD COLUMN IF NOT EXISTS correlation_id text,
    ADD COLUMN IF NOT EXISTS created_at timestamptz;

ALTER TABLE valorapesquisa.audit_logs
    ALTER COLUMN entity_id TYPE text USING entity_id::text,
    ALTER COLUMN metadata_json TYPE jsonb USING metadata_json::jsonb,
    ALTER COLUMN metadata_json SET DEFAULT '{}'::jsonb,
    ALTER COLUMN created_at SET DEFAULT now();

UPDATE valorapesquisa.audit_logs SET metadata_json = '{}'::jsonb WHERE metadata_json IS NULL;
UPDATE valorapesquisa.audit_logs SET created_at = now() WHERE created_at IS NULL;

ALTER TABLE valorapesquisa.audit_logs
    ALTER COLUMN metadata_json SET NOT NULL,
    ALTER COLUMN created_at SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_audit_logs_organization_created_at
    ON valorapesquisa.audit_logs (organization_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_logs_correlation_id
    ON valorapesquisa.audit_logs (correlation_id)
    WHERE correlation_id IS NOT NULL;

COMMIT;
