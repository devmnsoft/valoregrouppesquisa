CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.email_jobs (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NULL,
    response_id uuid NULL,
    recipient_email text,
    to_email text,
    subject text NOT NULL,
    body text,
    template_key text NOT NULL DEFAULT 'result-ready',
    payload_json jsonb NOT NULL DEFAULT '{}'::jsonb,
    status text NOT NULL DEFAULT 'pending',
    attempts int NOT NULL DEFAULT 0,
    last_error text,
    scheduled_at timestamptz NOT NULL DEFAULT now(),
    processing_at timestamptz,
    sent_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    created_by uuid,
    updated_by uuid,
    is_deleted boolean NOT NULL DEFAULT false
);

ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS to_email text;
ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS body text;
UPDATE valorapesquisa.email_jobs SET to_email = recipient_email WHERE to_email IS NULL AND recipient_email IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_email_jobs_status ON valorapesquisa.email_jobs(status);
CREATE INDEX IF NOT EXISTS ix_email_jobs_created_at ON valorapesquisa.email_jobs(created_at);
CREATE INDEX IF NOT EXISTS ix_email_jobs_status_created_at ON valorapesquisa.email_jobs(status, created_at);
