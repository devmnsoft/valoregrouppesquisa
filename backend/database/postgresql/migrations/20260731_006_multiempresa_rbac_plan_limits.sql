-- Fase 2G: invariantes multiempresa, RBAC por escopo e reservas de limites.
-- Migration aditiva e idempotente; não remove dados ou tabelas legadas.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;
SET search_path TO valorapesquisa, public;

ALTER TABLE business_groups ADD COLUMN IF NOT EXISTS code text;
ALTER TABLE business_groups ADD COLUMN IF NOT EXISTS type text NOT NULL DEFAULT 'economic_group';
ALTER TABLE business_groups ADD COLUMN IF NOT EXISTS description text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS cnpj_root text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS registration_status text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS head_office_or_branch text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS legal_nature text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS company_size text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS share_capital numeric(18,2);
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS primary_cnae text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS opening_date date;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS data_source text;
ALTER TABLE legal_entities ADD COLUMN IF NOT EXISTS last_lookup_at timestamptz;
ALTER TABLE units ADD COLUMN IF NOT EXISTS type text;
ALTER TABLE units ADD COLUMN IF NOT EXISTS region text;
ALTER TABLE units ADD COLUMN IF NOT EXISTS state char(2);
ALTER TABLE units ADD COLUMN IF NOT EXISTS city text;
ALTER TABLE units ADD COLUMN IF NOT EXISTS manager_user_id uuid REFERENCES users(id);
ALTER TABLE departments ADD COLUMN IF NOT EXISTS legal_entity_id uuid REFERENCES legal_entities(id);
ALTER TABLE departments ADD COLUMN IF NOT EXISTS unit_id uuid REFERENCES units(id);
ALTER TABLE departments ADD COLUMN IF NOT EXISTS parent_department_id uuid REFERENCES departments(id);
ALTER TABLE departments ADD COLUMN IF NOT EXISTS code text;
ALTER TABLE departments ADD COLUMN IF NOT EXISTS type text;
ALTER TABLE departments ADD COLUMN IF NOT EXISTS manager_user_id uuid REFERENCES users(id);

-- NULL não participa de UNIQUE composto no PostgreSQL. Consolide vínculos de
-- eventuais duplicatas globais antes de estabelecer a invariável correta.
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
INSERT INTO user_roles(user_id, role_id, created_at)
SELECT ur.user_id, role_map.canonical_id, ur.created_at
FROM user_roles ur JOIN role_map ON role_map.duplicate_id = ur.role_id
WHERE role_map.duplicate_id <> role_map.canonical_id
ON CONFLICT DO NOTHING;
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
INSERT INTO role_permissions(role_id, permission_id, created_at)
SELECT role_map.canonical_id, rp.permission_id, rp.created_at
FROM role_permissions rp JOIN role_map ON role_map.duplicate_id = rp.role_id
WHERE role_map.duplicate_id <> role_map.canonical_id
ON CONFLICT DO NOTHING;
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
DELETE FROM user_roles ur USING role_map
WHERE ur.role_id = role_map.duplicate_id AND role_map.duplicate_id <> role_map.canonical_id;
WITH role_map AS (
    SELECT id AS duplicate_id, first_value(id) OVER (PARTITION BY code ORDER BY created_at, id) AS canonical_id
    FROM roles WHERE organization_id IS NULL
)
DELETE FROM role_permissions rp USING role_map
WHERE rp.role_id = role_map.duplicate_id AND role_map.duplicate_id <> role_map.canonical_id;
DELETE FROM roles duplicate
USING roles canonical
WHERE duplicate.organization_id IS NULL AND canonical.organization_id IS NULL
  AND duplicate.code = canonical.code AND duplicate.id > canonical.id;
CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_global_code
    ON roles(code) WHERE organization_id IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_tenant_code_active
    ON roles(organization_id, code)
    WHERE organization_id IS NOT NULL AND deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS user_invitations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES organizations(id),
    email text NOT NULL,
    token_hash text NOT NULL UNIQUE,
    invited_by_user_id uuid NOT NULL REFERENCES users(id),
    expires_at timestamptz NOT NULL,
    accepted_at timestamptz,
    cancelled_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_user_invitations_pending
    ON user_invitations(organization_id, lower(email))
    WHERE accepted_at IS NULL AND cancelled_at IS NULL;

CREATE TABLE IF NOT EXISTS plan_usage_counters (
    organization_id uuid NOT NULL REFERENCES organizations(id),
    resource_code text NOT NULL,
    used_value bigint NOT NULL DEFAULT 0 CHECK (used_value >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (organization_id, resource_code)
);
CREATE TABLE IF NOT EXISTS plan_usage_reservations (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id uuid NOT NULL REFERENCES organizations(id),
    resource_code text NOT NULL,
    amount bigint NOT NULL CHECK (amount > 0),
    status text NOT NULL CHECK (status IN ('reserved','confirmed','released','expired')),
    idempotency_key text NOT NULL,
    expires_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz,
    UNIQUE (organization_id, idempotency_key)
);
CREATE INDEX IF NOT EXISTS ix_plan_usage_reservations_active
    ON plan_usage_reservations(organization_id, resource_code, expires_at)
    WHERE status = 'reserved';

INSERT INTO roles(code,name,is_system) VALUES
('admin_valora','Administrador Valora',true),
('consultor_valora','Consultor Valora',true),
('admin_grupo','Administrador de grupo',true),
('empresa_admin','Administrador da empresa',true),
('gestor_pesquisa','Gestor de pesquisa',true),
('analista_resultados','Analista de resultados',true),
('gestor_unidade','Gestor de unidade',true),
('gestor_area','Gestor de área',true),
('participante','Participante',true),
('convidado_externo','Convidado externo',true)
ON CONFLICT DO NOTHING;

INSERT INTO schema_migrations(version,checksum)
VALUES ('20260731_006_multiempresa_rbac_plan_limits','phase-02g-v1')
ON CONFLICT(version) DO UPDATE SET checksum=EXCLUDED.checksum,applied_at=now();
COMMIT;
