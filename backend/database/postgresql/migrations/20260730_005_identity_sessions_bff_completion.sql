BEGIN;
SET search_path TO valorapesquisa, public;

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='ck_user_sessions_status' AND conrelid='valorapesquisa.user_sessions'::regclass) THEN
    ALTER TABLE user_sessions ADD CONSTRAINT ck_user_sessions_status CHECK (status IN ('active','revoked','expired')) NOT VALID;
  END IF;
END $$;
ALTER TABLE user_sessions VALIDATE CONSTRAINT ck_user_sessions_status;
-- Bancos anteriores à fase de sessões persistidas podem conter tokens sem
-- família/sessão. Eles não podem ser rotacionados com segurança: revogue-os e
-- crie os registros históricos mínimos antes de tornar as colunas obrigatórias.
UPDATE refresh_tokens
SET revoked_at = COALESCE(revoked_at, now()),
    revocation_reason = COALESCE(revocation_reason, 'legacy_token_migrated')
WHERE family_id IS NULL OR session_id IS NULL;

INSERT INTO user_sessions (id, user_id, organization_id, status, expires_at, revoked_at, revocation_reason)
SELECT gen_random_uuid(), token.user_id, token.organization_id, 'revoked', token.expires_at,
       COALESCE(token.revoked_at, now()), 'legacy_token_migrated'
FROM refresh_tokens token
WHERE token.session_id IS NULL
ON CONFLICT DO NOTHING;

UPDATE refresh_tokens token
SET session_id = session.id
FROM (
    SELECT id, user_id, organization_id
    FROM user_sessions
    WHERE revocation_reason = 'legacy_token_migrated'
) session
WHERE token.session_id IS NULL
  AND session.user_id = token.user_id
  AND session.organization_id = token.organization_id;

INSERT INTO refresh_token_families (id, session_id, revoked_at, revocation_reason)
SELECT gen_random_uuid(), token.session_id, COALESCE(token.revoked_at, now()), 'legacy_token_migrated'
FROM refresh_tokens token
WHERE token.family_id IS NULL
ON CONFLICT DO NOTHING;

UPDATE refresh_tokens token
SET family_id = family.id
FROM (
    SELECT id, session_id
    FROM refresh_token_families
    WHERE revocation_reason = 'legacy_token_migrated'
) family
WHERE token.family_id IS NULL
  AND family.session_id = token.session_id;

ALTER TABLE refresh_tokens ALTER COLUMN family_id SET NOT NULL;
ALTER TABLE refresh_tokens ALTER COLUMN session_id SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_refresh_tokens_replaced_by ON refresh_tokens(replaced_by_id) WHERE replaced_by_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_session_active ON refresh_tokens(session_id,expires_at) WHERE revoked_at IS NULL;

INSERT INTO permissions(code,name,description,module_code) VALUES
('organization.current.read','Visualizar organização','Consulta a organização corrente.','identity'),
('organization.current.update','Atualizar organização','Atualiza a organização corrente.','identity'),
('users.read','Visualizar usuários','Consulta usuários do tenant.','identity'),
('users.create','Criar usuários','Cria usuários no tenant.','identity'),
('users.update','Atualizar usuários','Atualiza usuários no tenant.','identity'),
('users.disable','Desativar usuários','Desativa usuários no tenant.','identity'),
('sessions.read','Visualizar sessões','Consulta sessões próprias.','identity'),
('sessions.revoke','Revogar sessões','Revoga sessões próprias.','identity')
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,module_code=EXCLUDED.module_code;

INSERT INTO roles(code,name,is_system) VALUES ('empresa_admin','Administrador da empresa',true)
ON CONFLICT DO NOTHING;
INSERT INTO role_permissions(role_id,permission_id)
SELECT r.id,p.id FROM roles r CROSS JOIN permissions p
WHERE r.code='empresa_admin' AND r.organization_id IS NULL
  AND p.code IN ('organization.current.read','organization.current.update','users.read','users.create','users.update','users.disable','sessions.read','sessions.revoke')
ON CONFLICT DO NOTHING;

INSERT INTO email_templates(template_key,language_code,subject_template,body_template) VALUES
('company-registration','pt-BR','Bem-vindo à Valora','Seu cadastro empresarial foi concluído.'),
('password-reset','pt-BR','Recuperação de senha - Valora','Use o link seguro {{resetUrl}} para redefinir sua senha.'),
('user-invitation','pt-BR','Convite para a Valora','Você recebeu um convite para acessar a organização.')
ON CONFLICT(template_key,language_code) DO UPDATE SET subject_template=EXCLUDED.subject_template,body_template=EXCLUDED.body_template,updated_at=now();

INSERT INTO schema_migrations(version,checksum) VALUES
('20260730_005_identity_sessions_bff_completion','phase-02f-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();
COMMIT;
