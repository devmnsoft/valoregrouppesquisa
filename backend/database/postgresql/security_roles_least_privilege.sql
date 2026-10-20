-- ==============================================================================
-- VALORA INSIGHT™ — PAPÉIS E PERMISSÕES MÍNIMAS (LEAST PRIVILEGE) & ISOLAMENTO
-- SPRINT R3: SEGURANÇA E SEPARAÇÃO PRODUÇÃO / HOMOLOGAÇÃO
-- ==============================================================================

-- 1. TABELA CANÔNICA DE METADADOS DE AMBIENTE
CREATE TABLE IF NOT EXISTS valorapesquisa.environment_metadata (
    id int PRIMARY KEY DEFAULT 1,
    environment_name text NOT NULL,
    is_production boolean NOT NULL DEFAULT false,
    isolation_tier text NOT NULL DEFAULT 'standard',
    enforce_tenant_isolation boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT chk_single_env_row CHECK (id = 1)
);

-- 2. CRIAÇÃO DE ROLES COM LEAST PRIVILEGE (SE NÃO EXISTIREM)
DO $$
BEGIN
  -- Papel de Aplicação em Tempo de Execução (DML apenas)
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'valora_app_runtime') THEN
    CREATE ROLE valora_app_runtime WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
  END IF;

  -- Papel de Migrações e CI/CD (DDL controlado)
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'valora_migrator') THEN
    CREATE ROLE valora_migrator WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE INHERIT;
  END IF;

  -- Papel de Leitura Analítica / BI / Auditoria (Read-Only)
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'valora_readonly') THEN
    CREATE ROLE valora_readonly WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT;
  END IF;
END $$;

-- 3. PERMISSÕES DA ROLE valora_app_runtime (LEAST PRIVILEGE)
-- Permite apenas conectar e executar DML no schema valorapesquisa.
-- DDL (DROP, CREATE, ALTER, TRUNCATE) É ESTRITAMENTE VEDADO.
GRANT USAGE ON SCHEMA valorapesquisa TO valora_app_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA valorapesquisa TO valora_app_runtime;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA valorapesquisa TO valora_app_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA valorapesquisa GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO valora_app_runtime;
ALTER DEFAULT PRIVILEGES IN SCHEMA valorapesquisa GRANT USAGE, SELECT ON SEQUENCES TO valora_app_runtime;

-- Revoga explicitamente permissões perigosas
REVOKE CREATE ON SCHEMA valorapesquisa FROM valora_app_runtime;
REVOKE TRUNCATE ON ALL TABLES IN SCHEMA valorapesquisa FROM valora_app_runtime;

-- 4. PERMISSÕES DA ROLE valora_readonly
GRANT USAGE ON SCHEMA valorapesquisa TO valora_readonly;
GRANT SELECT ON ALL TABLES IN SCHEMA valorapesquisa TO valora_readonly;
ALTER DEFAULT PRIVILEGES IN SCHEMA valorapesquisa GRANT SELECT ON TABLES TO valora_readonly;

-- 5. PERMISSÕES DA ROLE valora_migrator
GRANT USAGE, CREATE ON SCHEMA valorapesquisa TO valora_migrator;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA valorapesquisa TO valora_migrator;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA valorapesquisa TO valora_migrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA valorapesquisa GRANT ALL PRIVILEGES ON TABLES TO valora_migrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA valorapesquisa GRANT ALL PRIVILEGES ON SEQUENCES TO valora_migrator;
