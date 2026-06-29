ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS resend_count int NOT NULL DEFAULT 0;
ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS last_resend_at timestamptz;
ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS reviewed_at timestamptz;
ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS reviewed_by uuid;
ALTER TABLE valorapesquisa.email_jobs ADD COLUMN IF NOT EXISTS review_note text;
ALTER TABLE valorapesquisa.certificates ADD COLUMN IF NOT EXISTS validation_url text;
CREATE INDEX IF NOT EXISTS ix_email_jobs_response_status_created ON valorapesquisa.email_jobs(response_id,status,created_at DESC);
CREATE INDEX IF NOT EXISTS ix_responses_free_diagnostics_created ON valorapesquisa.responses(created_at DESC) WHERE is_deleted=false;
CREATE INDEX IF NOT EXISTS ix_audit_logs_free_survey_entity ON valorapesquisa.audit_logs(action,entity_id,created_at DESC);
