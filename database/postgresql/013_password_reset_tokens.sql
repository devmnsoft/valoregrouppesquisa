CREATE TABLE IF NOT EXISTS valorapesquisa.password_reset_tokens (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
 user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 token_hash text NOT NULL,
 expires_at timestamptz NOT NULL,
 used_at timestamptz,
 requested_at timestamptz NOT NULL DEFAULT now(),
 request_ip_hash text,
 user_agent text,
 created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(),
 is_deleted boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS ix_password_reset_tokens_user ON valorapesquisa.password_reset_tokens(user_id, expires_at DESC);
CREATE UNIQUE INDEX IF NOT EXISTS ux_password_reset_tokens_hash_active ON valorapesquisa.password_reset_tokens(token_hash) WHERE used_at IS NULL AND is_deleted=false;
