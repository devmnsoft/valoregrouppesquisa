CREATE TABLE IF NOT EXISTS valorapesquisa.free_survey_security_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    survey_id uuid NULL,
    response_id uuid NULL,
    event_type text NOT NULL,
    ip_hash text NULL,
    email_hash text NULL,
    user_agent_hash text NULL,
    reason text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE INDEX IF NOT EXISTS ix_free_survey_security_events_created_at ON valorapesquisa.free_survey_security_events(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_free_survey_security_events_hashes ON valorapesquisa.free_survey_security_events(ip_hash,email_hash,user_agent_hash);
