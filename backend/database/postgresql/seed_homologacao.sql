-- ==============================================================================
-- VALORA INSIGHT — SEED OFICIAL DE HOMOLOGAÇÃO POSTGRESQL (IDEMPOTENTE E NÃO DESTRUTIVO)
-- SPRINT 27: PROTEÇÃO DE AMBIENTE, ZERO CREDENCIAIS HARDCODED E PROVISIONAMENTO SEGURO
-- Ambientes: Homologação / Demonstração / Validação E2E
-- Segurança: Zero credenciais ou hashes estáticos embutidos.
-- Provisionamento: Senha configurada via 'valora.demo_password_hash' ou gerada dinamicamente via pgcrypto.
-- ==============================================================================

DO $$
DECLARE
  v_allow_seed text := current_setting('valora.allow_demo_seed', true);
  v_env text := current_setting('valora.environment', true);
  v_is_prod boolean := false;
BEGIN
  -- 1. Verifica se tabela de metadados de ambiente existe e se acusa produção
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'valorapesquisa' AND table_name = 'environment_metadata') THEN
    SELECT is_production INTO v_is_prod FROM valorapesquisa.environment_metadata LIMIT 1;
    IF v_is_prod IS TRUE THEN
      RAISE EXCEPTION 'BLOQUEIO CRÍTICO DE SEGURANÇA: environment_metadata indica que este banco é de PRODUÇÃO. Seed de homologação cancelado.';
    END IF;
  END IF;

  -- 2. Bloqueio absoluto para ambientes identificados como produção
  IF current_database() ~* '(prod|production|live|prd)' THEN
    RAISE EXCEPTION 'BLOQUEIO CRÍTICO DE SEGURANÇA: seed_homologacao.sql não pode ser executado em ambiente de produção (banco atual: %).', current_database();
  END IF;

  -- 3. Bloqueio para usuário de aplicação em runtime (least privilege)
  IF current_user IN ('valora_app_runtime', 'valora_readonly') THEN
    RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: Usuário de aplicação em tempo de execução (%) não possui permissão para executar seeds.', current_user;
  END IF;

  -- 4. O nome do banco 'valora' sozinho NÃO é evidência suficiente de ambiente seguro
  IF current_database() ~* '^(valora|valora_db|valora_app|valora_insight)$' THEN
    IF v_allow_seed IS DISTINCT FROM 'true' THEN
      RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: Banco de dados com nome padrão de produção (%) exige autorização explícita via valora.allow_demo_seed=true.', current_database();
    END IF;
  END IF;

  -- 5. Exige identificação confiável de ambiente não-produtivo
  IF NOT (current_database() ~* '(test|teste|homolog|staging|qa|dev)' OR v_allow_seed = 'true' OR v_env IN ('Development', 'Testing', 'Homologation')) THEN
    RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: seed_homologacao.sql exige identificação explícita de ambiente seguro (test/homolog/staging/qa/dev ou valora.allow_demo_seed=true). Banco atual: %', current_database();
  END IF;
END $$;

BEGIN;

-- 1. ESTRUTURA BASE (GARANTIR COLUNAS DE SPRINT 1 A 4)
ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS row_version integer NOT NULL DEFAULT 1;
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS row_version integer NOT NULL DEFAULT 1;

-- 2. ORGANIZAÇÕES DEMONSTRATIVAS (SPRINT 07)
INSERT INTO valorapesquisa.organizations (id, name, slug, status, created_at, updated_at)
VALUES
  ('11111111-1111-1111-1111-111111111111', 'Empresa Exemplo — Tecnologia', 'empresa-exemplo-tecnologia', 'active', now(), now()),
  ('22222222-2222-2222-2222-222222222222', 'Empresa Exemplo — Indústria', 'empresa-exemplo-industria', 'active', now(), now()),
  ('33333333-3333-3333-3333-333333333333', 'Empresa Exemplo — Serviços', 'empresa-exemplo-servicos', 'active', now(), now())
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, slug = EXCLUDED.slug, status = 'active', updated_at = now();

-- 3. MÓDULOS HABILITADOS POR ORGANIZAÇÃO
INSERT INTO valorapesquisa.organization_modules (organization_id, module_code, enabled, created_at, updated_at)
SELECT org.id, m.code, true, now(), now()
FROM (VALUES
  ('11111111-1111-1111-1111-111111111111'::uuid),
  ('22222222-2222-2222-2222-222222222222'::uuid),
  ('33333333-3333-3333-3333-333333333333'::uuid)
) org(id)
CROSS JOIN (VALUES
  ('identity'), ('organization'), ('forms'), ('surveys'), ('distribution'),
  ('responses'), ('results'), ('certificates'), ('communications'), ('audit'),
  ('settings'), ('operations'), ('organizational_intelligence'), ('action_center'),
  ('evolution'), ('reports'), ('people'), ('benchmarks')
) m(code)
ON CONFLICT (organization_id, module_code) DO NOTHING;

-- 4. PESSOAS JURÍDICAS, UNIDADES E DEPARTAMENTOS
INSERT INTO valorapesquisa.legal_entities (id, organization_id, legal_name, trade_name, cnpj, status, created_at, updated_at)
VALUES
  ('1e111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Empresa Exemplo Tecnologia Ltda', 'Valora Tech Demonstração', '11111111000111', 'active', now(), now()),
  ('1e222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'Empresa Exemplo Indústria S.A.', 'Valora Indústria Demonstração', '22222222000122', 'active', now(), now()),
  ('1e333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'Empresa Exemplo Serviços Ltda', 'Valora Serviços Demonstração', '33333333000133', 'active', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.units (id, organization_id, legal_entity_id, name, code, status, created_at, updated_at)
VALUES
  ('00000001-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '1e111111-1111-1111-1111-111111111111', 'Sede Tecnologia Belém', 'sede-tech', 'active', now(), now()),
  ('00000002-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', '1e222222-2222-2222-2222-222222222222', 'Planta Matriz Indústria', 'planta-matriz', 'active', now(), now()),
  ('00000002-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', '1e222222-2222-2222-2222-222222222222', 'Centro de Distribuição', 'cd-logistica', 'active', now(), now()),
  ('00000003-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', '1e333333-3333-3333-3333-333333333333', 'Unidade Central de Serviços', 'unidade-servicos', 'active', now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.departments (id, organization_id, unit_id, name, code, status, created_at, updated_at)
VALUES
  -- Empresa Tecnologia: Tecnologia, Comercial, Financeiro, Pessoas e Operações
  ('d1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', '00000001-1111-1111-1111-111111111111', 'Tecnologia da Informação', 'tecnologia', 'active', now(), now()),
  ('d1111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', '00000001-1111-1111-1111-111111111111', 'Gente e Pessoas (RH)', 'pessoas', 'active', now(), now()),
  ('d1111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', '00000001-1111-1111-1111-111111111111', 'Comercial & Negócios', 'comercial', 'active', now(), now()),
  ('d1111111-1111-1111-1111-111111111114', '11111111-1111-1111-1111-111111111111', '00000001-1111-1111-1111-111111111111', 'Financeiro & Controladoria', 'financeiro', 'active', now(), now()),
  ('d1111111-1111-1111-1111-111111111115', '11111111-1111-1111-1111-111111111111', '00000001-1111-1111-1111-111111111111', 'Operações & SLAs', 'operacoes', 'active', now(), now()),

  -- Empresa Indústria: Produção, Logística, Qualidade, Administração e Pessoas
  ('d2222222-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', '00000002-2222-2222-2222-222222222221', 'Produção & Manufatura', 'producao', 'active', now(), now()),
  ('d2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', '00000002-2222-2222-2222-222222222222', 'Logística & Suprimentos', 'logistica', 'active', now(), now()),
  ('d2222222-2222-2222-2222-222222222223', '22222222-2222-2222-2222-222222222222', '00000002-2222-2222-2222-222222222221', 'Controle de Qualidade', 'qualidade', 'active', now(), now()),
  ('d2222222-2222-2222-2222-222222222224', '22222222-2222-2222-2222-222222222222', '00000002-2222-2222-2222-222222222221', 'Administração Geral', 'administracao', 'active', now(), now()),
  ('d2222222-2222-2222-2222-222222222225', '22222222-2222-2222-2222-222222222222', '00000002-2222-2222-2222-222222222221', 'Gente e Pessoas', 'pessoas-ind', 'active', now(), now()),

  -- Empresa Serviços: Atendimento, Operações, Comercial, Financeiro e Gestão
  ('d3333333-3333-3333-3333-333333333331', '33333333-3333-3333-3333-333333333333', '00000003-3333-3333-3333-333333333333', 'Atendimento & Sucesso do Cliente', 'atendimento', 'active', now(), now()),
  ('d3333333-3333-3333-3333-333333333332', '33333333-3333-3333-3333-333333333333', '00000003-3333-3333-3333-333333333333', 'Operações de Serviços', 'operacoes-srv', 'active', now(), now()),
  ('d3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', '00000003-3333-3333-3333-333333333333', 'Comercial de Contratos', 'comercial-srv', 'active', now(), now()),
  ('d3333333-3333-3333-3333-333333333334', '33333333-3333-3333-3333-333333333333', '00000003-3333-3333-3333-333333333333', 'Financeiro & Faturamento', 'financeiro-srv', 'active', now(), now()),
  ('d3333333-3333-3333-3333-333333333335', '33333333-3333-3333-3333-333333333333', '00000003-3333-3333-3333-333333333333', 'Gestão Estratégica', 'gestao', 'active', now(), now())
ON CONFLICT (id) DO NOTHING;

-- 5. USUÁRIOS DE HOMOLOGAÇÃO
-- PROVISIONAMENTO SEGURO (SPRINT 27):
-- Zero credenciais ou hashes estáticos embutidos no repositório.
-- Se 'valora.demo_password_hash' estiver definido na sessão/ambiente, utiliza essa credencial provisionada.
-- Caso contrário, gera um hash seguro aleatório e não previsível via pgcrypto.
-- Todos os usuários gerados para homologação possuem password_reset_required = true por padrão.

ALTER TABLE valorapesquisa.users ADD COLUMN IF NOT EXISTS password_reset_required boolean NOT NULL DEFAULT false;

WITH demo_cfg AS (
  SELECT 
    COALESCE(
      NULLIF(current_setting('valora.demo_password_hash', true), ''),
      crypt(gen_random_uuid()::text, gen_salt('bf', 10))
    ) AS initial_hash
)
INSERT INTO valorapesquisa.users (id, organization_id, name, email, password_hash, password_reset_required, status, created_at, updated_at)
SELECT u.id, u.organization_id, u.name, u.email, cfg.initial_hash, true, 'active', now(), now()
FROM (VALUES
  ('a0000000-0000-0000-0000-000000000001'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'Admin Global Valora', 'admin.global@valora.local'),
  ('a0000000-0000-0000-0000-000000000002'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'Gestor Cliente Demonstração', 'gestor.demonstracao@valora.local'),
  ('a0000000-0000-0000-0000-000000000022'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'Admin Cliente Demonstração', 'admin.demonstracao@valora.local'),
  ('a0000000-0000-0000-0000-000000000003'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'Usuário Leitura Demonstração', 'leitor.demonstracao@valora.local'),
  ('a0000000-0000-0000-0000-000000000033'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'Analista de Resultados', 'analista.demonstracao@valora.local'),
  ('a0000000-0000-0000-0000-000000000004'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 'Respondente Colaborador', 'respondente.demonstracao@valora.local'),
  ('a0000000-0000-0000-0000-000000000005'::uuid, '22222222-2222-2222-2222-222222222222'::uuid, 'Gestor Cliente Início', 'gestor.inicio@valora.local'),
  ('a0000000-0000-0000-0000-000000000055'::uuid, '22222222-2222-2222-2222-222222222222'::uuid, 'Admin Cliente Início', 'admin.inicio@valora.local'),
  ('a0000000-0000-0000-0000-000000000006'::uuid, '33333333-3333-3333-3333-333333333333'::uuid, 'Gestor Cliente Isolamento', 'gestor.isolamento@valora.local'),
  ('a0000000-0000-0000-0000-000000000066'::uuid, '33333333-3333-3333-3333-333333333333'::uuid, 'Admin Cliente Isolamento', 'admin.isolamento@valora.local')
) AS u(id, organization_id, name, email)
CROSS JOIN demo_cfg cfg
ON CONFLICT (id) DO UPDATE SET
  password_hash = CASE 
    WHEN NULLIF(current_setting('valora.demo_password_hash', true), '') IS NOT NULL THEN EXCLUDED.password_hash
    WHEN valorapesquisa.users.password_hash IS NULL OR valorapesquisa.users.password_hash = '' THEN EXCLUDED.password_hash 
    ELSE valorapesquisa.users.password_hash 
  END,
  password_reset_required = true;

-- 6. PERMISSÕES E ATRIBUIÇÃO DE PAPÉIS (RBAC)
INSERT INTO valorapesquisa.permissions (code, name, description, module_code) VALUES
  ('campaigns.read', 'Visualizar campanhas', 'Consulta campanhas e adesão da coleta.', 'surveys'),
  ('campaigns.manage', 'Gerenciar campanhas', 'Publica campanhas, convites e lembretes.', 'surveys')
ON CONFLICT (code) DO NOTHING;

INSERT INTO valorapesquisa.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM valorapesquisa.roles r
CROSS JOIN valorapesquisa.permissions p
WHERE r.deleted_at IS NULL AND (
  (r.code IN ('admin_valora', 'empresa_admin', 'gestor_pesquisa') AND p.code IN ('campaigns.read', 'campaigns.manage')) OR
  (r.code = 'analista_resultados' AND p.code = 'campaigns.read')
)
ON CONFLICT DO NOTHING;

INSERT INTO valorapesquisa.user_roles (user_id, role_id)
SELECT u.id, r.id
FROM (VALUES
  ('a0000000-0000-0000-0000-000000000001'::uuid, 'admin_valora'),
  ('a0000000-0000-0000-0000-000000000002'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000022'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000003'::uuid, 'analista_resultados'),
  ('a0000000-0000-0000-0000-000000000033'::uuid, 'analista_resultados'),
  ('a0000000-0000-0000-0000-000000000004'::uuid, 'participante'),
  ('a0000000-0000-0000-0000-000000000005'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000055'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000006'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000066'::uuid, 'empresa_admin')
) u(id, role_code)
JOIN valorapesquisa.roles r ON r.code = u.role_code AND r.organization_id IS NULL
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 7. QUESTIONÁRIOS E FORMULÁRIOS
INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status, created_at, updated_at)
VALUES
  ('f1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'form_maturidade_demo', 'Diagnóstico de Maturidade Organizacional', 'Diagnóstico de Maturidade Organizacional', 'form-maturidade-demo', 'form-maturidade-demo', 'published', now(), now()),
  ('f2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'form_eficiencia_industria', 'Diagnóstico de Eficiência e Clima Industrial', 'Diagnóstico de Eficiência e Clima Industrial', 'form-eficiencia-industria', 'form-eficiencia-industria', 'published', now(), now()),
  ('f3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'form_governanca_servicos', 'Diagnóstico de Governança e Serviços', 'Diagnóstico de Governança e Serviços', 'form-governanca-servicos', 'form-governanca-servicos', 'published', now(), now())
ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, title = EXCLUDED.title, status = 'published';

INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version, created_at, updated_at)
VALUES
  ('fb111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 1, 1, 'published', true, 1, now(), now()),
  ('fb222222-2222-2222-2222-222222222222', 'f2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 1, 1, 'published', true, 1, now(), now()),
  ('fb333333-3333-3333-3333-333333333333', 'f3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 1, 1, 'published', true, 1, now(), now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.form_section_versions (id, organization_id, form_version_id, title, position, display_order)
VALUES
  ('fc111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fb111111-1111-1111-1111-111111111111', 'Liderança e Alinhamento Estratégico', 1, 1),
  ('fc222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fb222222-2222-2222-2222-222222222222', 'Operações & Segurança Fabril', 1, 1),
  ('fc333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fb333333-3333-3333-3333-333333333333', 'Governança & Segregação de Dados', 1, 1)
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.question_versions (id, organization_id, section_id, code, type, title, required, position, display_order, settings)
VALUES
  ('fd111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fc111111-1111-1111-1111-111111111111', 'lid_comunicacao', 'likert_1_5', 'A liderança comunica os objetivos estratégicos de forma transparente e compreensível.', true, 1, 1, '{}'::jsonb),
  ('fd111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fc111111-1111-1111-1111-111111111111', 'lid_autonomia', 'likert_1_5', 'As equipes possuem autonomia e suporte para resolver problemas operacionais.', true, 2, 2, '{}'::jsonb),
  ('fd222222-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', 'fc222222-2222-2222-2222-222222222222', 'ind_recursos', 'likert_1_5', 'Os recursos de trabalho necessários foram disponibilizados no prazo acordado.', true, 1, 1, '{}'::jsonb),
  ('fd222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fc222222-2222-2222-2222-222222222222', 'ind_seguranca', 'likert_1_5', 'Os procedimentos de segurança e conformidade são rigorosamente cumpridos.', true, 2, 2, '{}'::jsonb),
  ('fd333333-3333-3333-3333-333333333331', '33333333-3333-3333-3333-333333333333', 'fc333333-3333-3333-3333-333333333333', 'iso_seguranca', 'likert_1_5', 'Os acessos organizacionais estão estritamente segregados.', true, 1, 1, '{}'::jsonb),
  ('fd333333-3333-3333-3333-333333333332', '33333333-3333-3333-3333-333333333333', 'fc333333-3333-3333-3333-333333333333', 'srv_qualidade', 'likert_1_5', 'A entrega de serviços cumpre os padrões acordados de SLA e satisfação.', true, 2, 2, '{}'::jsonb)
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.questions (id, code, text, display_order, position, question_type, is_qualitative, is_required)
VALUES
  ('fd111111-1111-1111-1111-111111111111', 'lid_comunicacao', 'A liderança comunica os objetivos estratégicos de forma transparente e compreensível.', 1, 1, 'scale_1_5', false, true),
  ('fd111111-1111-1111-1111-111111111112', 'lid_autonomia', 'As equipes possuem autonomia e suporte para resolver problemas operacionais.', 2, 2, 'scale_1_5', false, true),
  ('fd222222-2222-2222-2222-222222222221', 'ind_recursos', 'Os recursos de trabalho necessários foram disponibilizados no prazo acordado.', 1, 1, 'scale_1_5', false, true),
  ('fd222222-2222-2222-2222-222222222222', 'ind_seguranca', 'Os procedimentos de segurança e conformidade são rigorosamente cumpridos.', 2, 2, 'scale_1_5', false, true),
  ('fd333333-3333-3333-3333-333333333331', 'iso_seguranca', 'Os acessos organizacionais estão estritamente segregados.', 1, 1, 'scale_1_5', false, true),
  ('fd333333-3333-3333-3333-333333333332', 'srv_qualidade', 'A entrega de serviços cumpre os padrões acordados de SLA e satisfação.', 2, 2, 'scale_1_5', false, true)
ON CONFLICT (id) DO NOTHING;

-- 8. DIAGNÓSTICOS E PESQUISAS
INSERT INTO valorapesquisa.surveys (id, organization_id, form_id, form_version_id, name, title, description, status, starts_at, expires_at, row_version, created_at, updated_at)
VALUES
  -- Empresa Tecnologia
  ('fe111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', 'fb111111-1111-1111-1111-111111111111', 'Diagnóstico de Maturidade 2026', 'Diagnóstico de Maturidade 2026', 'Ciclo oficial de avaliação organizacional de tecnologia com evidências completas.', 'active', now() - interval '10 days', now() + interval '20 days', 2, now() - interval '10 days', now()),
  ('fe111111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', 'fb111111-1111-1111-1111-111111111111', 'Diagnóstico de Clima em Preparação', 'Diagnóstico de Clima em Preparação', 'Ciclo recém-criado em rascunho com pendências para publicação.', 'draft', null, null, 1, now() - interval '1 days', now()),
  -- Empresa Indústria
  ('fe222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'f2222222-2222-2222-2222-222222222222', 'fb222222-2222-2222-2222-222222222222', 'Diagnóstico de Eficiência Fabril 2026', 'Diagnóstico de Eficiência Fabril 2026', 'Ciclo anual de avaliação de segurança, insumos e produtividade industrial.', 'active', now() - interval '8 days', now() + interval '22 days', 2, now() - interval '8 days', now()),
  -- Empresa Serviços
  ('fe333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'f3333333-3333-3333-3333-333333333333', 'fb333333-3333-3333-3333-333333333333', 'Diagnóstico de Governança e Serviços 2026', 'Diagnóstico de Governança e Serviços 2026', 'Avaliação de conformidade, qualidade de atendimento e isolamento de dados.', 'active', now() - interval '5 days', now() + interval '25 days', 1, now() - interval '5 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.diagnostic_cycles (id, organization_id, survey_id, form_id, title, description, cycle_number, methodology_version, status, opened_at, published_at, confidence_level, processing_status, row_version, created_at, updated_at)
VALUES
  ('c1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', 'Diagnóstico de Maturidade 2026', 'Ciclo oficial de tecnologia com evidências completas.', 1, '1.0', 'active', now() - interval '10 days', now() - interval '10 days', 94.5, 'processed', 2, now() - interval '10 days', now()),
  ('c1111111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-222222222222', 'f1111111-1111-1111-1111-111111111111', 'Diagnóstico de Clima em Preparação', 'Ciclo recém-criado em rascunho.', 2, '1.0', 'draft', null, null, null, 'pending', 1, now() - interval '1 days', now()),
  ('c2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'f2222222-2222-2222-2222-222222222222', 'Diagnóstico de Eficiência Fabril 2026', 'Ciclo industrial de avaliação operacional.', 1, '1.0', 'active', now() - interval '8 days', now() - interval '8 days', 91.0, 'processed', 2, now() - interval '8 days', now()),
  ('c3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'f3333333-3333-3333-3333-333333333333', 'Diagnóstico de Governança e Serviços 2026', 'Ciclo de serviços com governança de processos.', 1, '1.0', 'active', now() - interval '5 days', now() - interval '5 days', 88.0, 'processed', 1, now() - interval '5 days', now())
ON CONFLICT (id) DO NOTHING;

-- 9. LINKS PÚBLICOS CANÔNICOS DE COLETA
INSERT INTO valorapesquisa.survey_links (id, organization_id, survey_id, token_hash, public_url, status, is_deleted, starts_at, expires_at, created_at, updated_at)
VALUES
  ('51000000-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'hash_demo_maturidade_2026', '/public/surveys/fe111111-1111-1111-1111-111111111111', 'active', false, now() - interval '10 days', now() + interval '20 days', now() - interval '10 days', now()),
  ('51000000-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'hash_demo_industria_2026', '/public/surveys/fe222222-2222-2222-2222-222222222222', 'active', false, now() - interval '8 days', now() + interval '22 days', now() - interval '8 days', now()),
  ('51000000-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'hash_isolamento_2026', '/public/surveys/fe333333-3333-3333-3333-333333333333', 'active', false, now() - interval '5 days', now() + interval '25 days', now() - interval '5 days', now())
ON CONFLICT (id) DO NOTHING;

-- 10. RESPOSTAS SINTÉTICAS DAS 3 EMPRESAS E CÁLCULO OFICIAL DE INDICADORES
DO $$
DECLARE
  v_resp_id uuid;
  v_i int;
BEGIN
  -- 10.1 Empresa Tecnologia: 5 respostas consistentes (Liderança e Autonomia: scores altos -> Maturidade Madura)
  FOR v_i IN 1..5 LOOP
    v_resp_id := ('de111111-1111-1111-1111-00000000000' || v_i)::uuid;
    INSERT INTO valorapesquisa.responses (id, organization_id, survey_id, status, qualitative, submitted_at, created_at)
    VALUES (v_resp_id, '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'completed', 'Comunicação da liderança é constante e clara.', now() - (v_i || ' days')::interval, now() - (v_i || ' days')::interval)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO valorapesquisa.response_answers (id, response_id, question_id, numeric_value, text_value, score, max_score, created_at)
    VALUES
      (gen_random_uuid(), v_resp_id, 'fd111111-1111-1111-1111-111111111111', CASE WHEN v_i % 2 = 1 THEN 5 ELSE 4 END, 'Alinhamento claro', CASE WHEN v_i % 2 = 1 THEN 5.0 ELSE 4.0 END, 5.0, now() - (v_i || ' days')::interval),
      (gen_random_uuid(), v_resp_id, 'fd111111-1111-1111-1111-111111111112', CASE WHEN v_i % 2 = 0 THEN 5 ELSE 4 END, 'Autonomia suportada', CASE WHEN v_i % 2 = 0 THEN 5.0 ELSE 4.0 END, 5.0, now() - (v_i || ' days')::interval)
    ON CONFLICT (response_id, question_id) DO NOTHING;
  END LOOP;

  -- 10.2 Empresa Indústria: 5 respostas consistentes (Recursos e Segurança Fabril: scores médios -> Maturidade Integrada)
  FOR v_i IN 1..5 LOOP
    v_resp_id := ('de222222-2222-2222-2222-00000000000' || v_i)::uuid;
    INSERT INTO valorapesquisa.responses (id, organization_id, survey_id, status, qualitative, submitted_at, created_at)
    VALUES (v_resp_id, '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'completed', 'Processos de segurança fabril rigorosos, oportunidade em insumos.', now() - (v_i || ' days')::interval, now() - (v_i || ' days')::interval)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO valorapesquisa.response_answers (id, response_id, question_id, numeric_value, text_value, score, max_score, created_at)
    VALUES
      (gen_random_uuid(), v_resp_id, 'fd222222-2222-2222-2222-222222222221', CASE WHEN v_i = 3 THEN 2 ELSE 3 END, 'Insumos com atrasos parciais', CASE WHEN v_i = 3 THEN 2.0 ELSE 3.0 END, 5.0, now() - (v_i || ' days')::interval),
      (gen_random_uuid(), v_resp_id, 'fd222222-2222-2222-2222-222222222222', 4, 'Normas de segurança bem praticadas', 4.0, 5.0, now() - (v_i || ' days')::interval)
    ON CONFLICT (response_id, question_id) DO NOTHING;
  END LOOP;

  -- 10.3 Empresa Serviços: 5 respostas consistentes (Segregação de Acessos e SLAs: scores estruturantes -> Maturidade Estruturante)
  FOR v_i IN 1..5 LOOP
    v_resp_id := ('de333333-3333-3333-3333-00000000000' || v_i)::uuid;
    INSERT INTO valorapesquisa.responses (id, organization_id, survey_id, status, qualitative, submitted_at, created_at)
    VALUES (v_resp_id, '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'completed', 'Necessário aprimorar segregação de permissões e controle de SLAs.', now() - (v_i || ' days')::interval, now() - (v_i || ' days')::interval)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO valorapesquisa.response_answers (id, response_id, question_id, numeric_value, text_value, score, max_score, created_at)
    VALUES
      (gen_random_uuid(), v_resp_id, 'fd333333-3333-3333-3333-333333333331', 2, 'Segregação de acessos em implantação', 2.0, 5.0, now() - (v_i || ' days')::interval),
      (gen_random_uuid(), v_resp_id, 'fd333333-3333-3333-3333-333333333332', CASE WHEN v_i % 2 = 1 THEN 3 ELSE 2 END, 'SLAs com divergências pontuais', CASE WHEN v_i % 2 = 1 THEN 3.0 ELSE 2.0 END, 5.0, now() - (v_i || ' days')::interval)
    ON CONFLICT (response_id, question_id) DO NOTHING;
  END LOOP;
END $$;

-- 10.4 MOTOR OFICIAL DE CÁLCULO DE RESULTADOS (CÁLCULO MATEMÁTICO REAL, ZERO ÍNDICES ARBITRÁRIOS)
INSERT INTO valorapesquisa.result_scores (id, organization_id, response_id, total_score, max_score, percentage, maturity_label, created_at)
SELECT
  gen_random_uuid(),
  r.organization_id,
  r.id,
  SUM(ra.score)::int AS total_score,
  SUM(ra.max_score)::int AS max_score,
  ROUND((SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0, 1) AS percentage,
  CASE 
    WHEN (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0 <= 25.0 THEN 'Inicial'
    WHEN (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0 <= 50.0 THEN 'Estruturante'
    WHEN (SUM(ra.score) / NULLIF(SUM(ra.max_score), 0)) * 100.0 <= 75.0 THEN 'Integrado'
    ELSE 'Maduro'
  END AS maturity_label,
  r.submitted_at
FROM valorapesquisa.responses r
JOIN valorapesquisa.response_answers ra ON ra.response_id = r.id
WHERE r.organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
)
GROUP BY r.organization_id, r.id, r.submitted_at
ON CONFLICT (response_id) DO UPDATE SET
  total_score = EXCLUDED.total_score,
  max_score = EXCLUDED.max_score,
  percentage = EXCLUDED.percentage,
  maturity_label = EXCLUDED.maturity_label;

-- 11. EVIDÊNCIAS METODOLÓGICAS COM ORIGEM REAL
INSERT INTO valorapesquisa.evidence_items (id, organization_id, survey_id, response_id, capability_code, dimension_code, source_type, source_id, concept_code, metric_code, index_code, evidence_type, normalized_value, weight, confidence_weight, mapping_status, text_excerpt, created_at)
VALUES
  -- Empresa Tecnologia
  ('e1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'de111111-1111-1111-1111-000000000001', 'cap_lideranca', 'lideranca', 'response_answer', 'de111111-1111-1111-1111-000000000001', 'lid_comunicacao', 'METRIC_LID_COM', 'IDX_LID', 'likert_answer', 100.0, 1.0, 0.95, 'mapped', 'Comunicação da liderança é constante e clara.', now() - interval '5 days'),
  ('e1111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'de111111-1111-1111-1111-000000000002', 'cap_lideranca', 'lideranca', 'response_answer', 'de111111-1111-1111-1111-000000000002', 'lid_autonomia', 'METRIC_LID_AUT', 'IDX_LID', 'likert_answer', 80.0, 1.0, 0.90, 'mapped', 'As equipes possuem autonomia operacional.', now() - interval '4 days'),
  -- Empresa Indústria
  ('e2222222-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'de222222-2222-2222-2222-000000000001', 'cap_operacoes', 'operacoes', 'response_answer', 'de222222-2222-2222-2222-000000000001', 'ind_recursos', 'METRIC_IND_REC', 'IDX_OPS', 'likert_answer', 60.0, 1.0, 0.92, 'mapped', 'Suprimentos e recursos essenciais recebidos com pequenos atrasos.', now() - interval '5 days'),
  ('e2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'de222222-2222-2222-2222-000000000002', 'cap_seguranca', 'seguranca', 'response_answer', 'de222222-2222-2222-2222-000000000002', 'ind_seguranca', 'METRIC_IND_SEG', 'IDX_OPS', 'likert_answer', 80.0, 1.0, 0.94, 'mapped', 'Normas de segurança operacional e proteção individual rigorosamente observadas.', now() - interval '4 days'),
  -- Empresa Serviços
  ('e3333333-3333-3333-3333-333333333331', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'de333333-3333-3333-3333-000000000001', 'cap_governanca', 'governanca', 'response_answer', 'de333333-3333-3333-3333-000000000001', 'iso_seguranca', 'METRIC_SRV_SEC', 'IDX_GOV', 'likert_answer', 40.0, 1.0, 0.88, 'mapped', 'Perfis de acesso e segregação de ambientes em fase de consolidação.', now() - interval '4 days'),
  ('e3333333-3333-3333-3333-333333333332', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'de333333-3333-3333-3333-000000000002', 'cap_servicos', 'qualidade', 'response_answer', 'de333333-3333-3333-3333-000000000002', 'srv_qualidade', 'METRIC_SRV_SLA', 'IDX_GOV', 'likert_answer', 50.0, 1.0, 0.89, 'mapped', 'Tempo de resposta aos chamados e SLAs contratuais com dispersão moderada.', now() - interval '3 days')
ON CONFLICT (id) DO NOTHING;

-- 12. ACHADOS ANALÍTICOS (INSIGHTS) COM ORIGEM CANÔNICA
INSERT INTO valorapesquisa.valora_ai_runs (id, organization_id, diagnosis_id, prompt_code, prompt_version, provider, model, correlation_id, input_json, status, created_at, updated_at)
VALUES
  ('da111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'diag-audit-prompt', 1, 'valora-engine', 'deterministic-v1', 'seed-run-corr-1', '{}'::jsonb, 'completed', now() - interval '3 days', now()),
  ('da222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'diag-audit-prompt', 1, 'valora-engine', 'deterministic-v1', 'seed-run-corr-2', '{}'::jsonb, 'completed', now() - interval '2 days', now()),
  ('da333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'diag-audit-prompt', 1, 'valora-engine', 'deterministic-v1', 'seed-run-corr-3', '{}'::jsonb, 'completed', now() - interval '1 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.valora_ai_insights (id, organization_id, diagnostic_id, ai_run_id, insight_type, title, summary, evidence_summary, related_dimension, severity, priority, confidence_level, limitation, recommendation, status, review_version, reviewed_by_user_id, reviewed_at, created_at, updated_at)
VALUES
  -- Tecnologia
  ('ba111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'da111111-1111-1111-1111-111111111111', 'recommendation', 'Fortalecer Alinhamento em Comunicação da Liderança', 'Evidências apontam forte engajamento, com oportunidade de padronização nas cadências de liderança.', '5 respostas com média 90% de maturidade convergente.', 'Liderança', 'medium', 'high', 'Alto', 'Válido sob a amostragem coletada no ciclo.', 'Implementar rituais quinzenais de alinhamento com pauta transparente.', 'converted_to_action', 1, 'a0000000-0000-0000-0000-000000000002', now() - interval '2 days', now() - interval '3 days', now()),
  ('ba111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'da111111-1111-1111-1111-111111111111', 'warning', 'Potencial Assimetria de Informações Operacionais', 'Parte dos respondentes indicou dependência de canais informais.', 'Evidência textual cruzada com dispersão de autonomia.', 'Operações', 'low', 'medium', 'Baixo', 'Aguardando convergência independente de ao menos 3 evidências.', 'Estruturar repositório oficial de diretrizes e playbooks.', 'pending_review', 0, null, null, now() - interval '1 days', now()),
  -- Indústria
  ('ba222222-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'da222222-2222-2222-2222-222222222222', 'recommendation', 'Modernização de Gestão de Suprimentos Fabris', 'Atrasos recorrentes em insumos afetam a cadência operacional de manufatura.', '5 respostas com 70% de índice integrado e sinalização de gargalos.', 'Operações', 'high', 'high', 'Alto', 'Amostra cobre chão de fábrica e supervisão.', 'Revisar contratos de fornecedores de insumos críticos e estoque mínimo.', 'converted_to_action', 1, 'a0000000-0000-0000-0000-000000000005', now() - interval '1 days', now() - interval '2 days', now()),
  -- Serviços
  ('ba333333-3333-3333-3333-333333333331', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'da333333-3333-3333-3333-333333333333', 'warning', 'Reforço de Governança e Segregação de Perfis de Acesso', 'Necessidade de estruturar matriz de perfis para garantir conformidade e sigilo contratual.', '5 respostas convergentes com maturidade estruturante de 46%.', 'Governança', 'high', 'critical', 'Alto', 'Baseado em auditoria de acessos aos sistemas de atendimento.', 'Implantar revisão trimestral de permissões RBAC e monitoramento de SLAs.', 'converted_to_action', 1, 'a0000000-0000-0000-0000-000000000006', now() - interval '1 days', now() - interval '1 days', now())
ON CONFLICT (id) DO NOTHING;

-- 13. PLANOS DE AÇÃO NO ACTION CENTER (VINCULADOS AOS INSIGHTS DAS 3 EMPRESAS)
INSERT INTO valorapesquisa.action_plans (id, organization_id, title, summary, origin_type, origin_id, diagnostic_id, status, priority, owner_user_id, due_at, evidence_summary, expected_outcome, created_at, updated_at)
VALUES
  -- Tecnologia
  ('aa111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Plano de Ação: Cadência de Alinhamento da Liderança', 'Estruturação dos fóruns de alinhamento quinzenais conforme achado analítico.', 'ai_insight', 'ba111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'in_execution', 'high', 'a0000000-0000-0000-0000-000000000002', now() + interval '30 days', '5 respostas com média 90% de maturidade convergente.', 'Atingir 95% de clareza nas metas de liderança no próximo ciclo.', now() - interval '2 days', now()),
  -- Indústria
  ('aa222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'Plano de Ação: Modernização do Fluxo de Suprimentos Fabris', 'Implementação de alertas preditivos de estoque de insumos essenciais.', 'ai_insight', 'ba222222-2222-2222-2222-222222222221', 'fe222222-2222-2222-2222-222222222222', 'in_execution', 'high', 'a0000000-0000-0000-0000-000000000005', now() + interval '45 days', 'Gargalos de insumos reportados por 60% das equipes operacionais.', 'Reduzir em 80% as paralisações por falta de insumos.', now() - interval '1 days', now()),
  -- Serviços
  ('aa333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'Plano de Ação: Matriz de Perfis e Painel de SLAs', 'Estruturação da segregação de permissões de acesso e telemetria de atendimento.', 'ai_insight', 'ba333333-3333-3333-3333-333333333331', 'fe333333-3333-3333-3333-333333333333', 'in_execution', 'critical', 'a0000000-0000-0000-0000-000000000006', now() + interval '20 days', 'Maturidade estruturante de 46% em governança e controle de dados.', 'Conformidade de 100% dos acessos de acordo com os contratos de clientes.', now() - interval '1 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.action_items (id, organization_id, action_plan_id, title, description, origin_type, origin_id, diagnostic_id, status, priority, responsible_user_id, due_at, progress_percent, evidence_summary, expected_outcome, created_at, updated_at)
VALUES
  -- Tecnologia
  ('ab111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'aa111111-1111-1111-1111-111111111111', 'Definir pauta padrão dos alinhamentos quinzenais', 'Modelo de ata e ata compartilhada com indicadores.', 'manual', 'aa111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'completed', 'high', 'a0000000-0000-0000-0000-000000000003', now() - interval '1 days', 100, 'Modelo publicado no portal interno.', 'Template padronizado ativo.', now() - interval '2 days', now()),
  ('ab111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'aa111111-1111-1111-1111-111111111111', 'Realizar primeiro ciclo de reuniões com as equipes', 'Aplicação do modelo nas 4 gerências.', 'manual', 'aa111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'in_progress', 'medium', 'a0000000-0000-0000-0000-000000000002', now() + interval '15 days', 50, 'Registro de 2 reuniões concluídas.', 'Todas as gerências alinhadas.', now() - interval '1 days', now()),
  -- Indústria
  ('ab222222-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', 'aa222222-2222-2222-2222-222222222222', 'Auditar cadeia de reposição de itens de segurança e insumos', 'Mapeamento de fornecedores com atraso nos últimos 60 dias.', 'manual', 'aa222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', 'in_progress', 'high', 'a0000000-0000-0000-0000-000000000005', now() + interval '20 days', 40, 'Relatório parcial de auditoria gerado.', 'Acordo de nível de serviço com 3 fornecedores homologados.', now() - interval '1 days', now()),
  -- Serviços
  ('ab333333-3333-3333-3333-333333333331', '33333333-3333-3333-3333-333333333333', 'aa333333-3333-3333-3333-333333333333', 'Publicar política restritiva de perfis de atendimento', 'Documento formal de segregação de credenciais por contrato.', 'manual', 'aa333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'completed', 'critical', 'a0000000-0000-0000-0000-000000000006', now() - interval '1 days', 100, 'Política assinada pela liderança jurídica e técnica.', 'Diretrizes em conformidade com LGPD ativas.', now() - interval '1 days', now()),
  ('ab111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', 'aa111111-1111-1111-1111-111111111111', 'Publicar diretrizes consolidadas no portal corporativo', 'Ação operacional com prazo expirado para validação de filtros de atraso.', 'manual', 'aa111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'in_progress', 'critical', 'a0000000-0000-0000-0000-000000000003', now() - interval '5 days', 20, 'Documento inicial em revisão.', 'Diretrizes acessíveis a todos os colaboradores.', now() - interval '10 days', now())
ON CONFLICT (id) DO NOTHING;

-- 14. CICLOS DE EVOLUÇÃO E REAVALIAÇÃO
INSERT INTO valorapesquisa.evolution_cycles (id, organization_id, diagnostic_id, title, summary, status, baseline_score, current_score, trend, evidence_summary, metadata_json, created_at, updated_at)
VALUES
  ('ec111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'Evolução da Maturidade Organizacional 2026', 'Acompanhamento longitudinal de maturidade a partir do ciclo oficial.', 'open', 84.0, 84.0, 'stable', 'Baseline apurado no Diagnóstico de Maturidade 2026 com 5 respostas completas.', '{"methodologyVersion":"1.0","baselineSource":"organizational_measurement"}'::jsonb, now() - interval '3 days', now()),
  ('ec111111-1111-1111-1111-222222222222', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'Ciclo Comparativo Experimental', 'Medição de controle com instrumentação metodológica distinta.', 'open', 70.0, null, 'inconclusive', 'Diferenças de escalas e pesos metodológicos tornam a variação inconclusiva.', '{"methodologyVersion":"experimental","inconclusiveReason":"metodologia incompativel"}'::jsonb, now() - interval '1 days', now())
ON CONFLICT (id) DO NOTHING;

-- 15. CAMPANHAS CANÔNICAS V10 (Distribuição, Adesão e Metas de Resposta)
INSERT INTO valorapesquisa.campaigns (id, organization_id, survey_id, unit_id, department_id, name, audience, response_goal, starts_at, ends_at, status, created_by, created_at, updated_at)
VALUES
  ('ca111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', '00000001-1111-1111-1111-111111111111', 'd1111111-1111-1111-1111-111111111111', 'Campanha Geral de Maturidade 2026', 'Toda a organização', 50, now() - interval '5 days', now() + interval '25 days', 'active', 'a0000000-0000-0000-0000-000000000002', now() - interval '5 days', now()),
  ('ca111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-222222222222', null, null, 'Campanha Piloto de Clima (Rascunho)', 'Equipes de Operação', 30, now() + interval '2 days', now() + interval '30 days', 'draft', 'a0000000-0000-0000-0000-000000000002', now() - interval '1 days', now()),
  ('ca222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fe222222-2222-2222-2222-222222222222', '00000002-2222-2222-2222-222222222221', 'd2222222-2222-2222-2222-222222222221', 'Campanha Eficiência Fabril 2026', 'Toda a unidade fabril', 40, now() - interval '8 days', now() + interval '22 days', 'active', 'a0000000-0000-0000-0000-000000000005', now() - interval '8 days', now()),
  ('ca333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', '00000003-3333-3333-3333-333333333333', 'd3333333-3333-3333-3333-333333333331', 'Campanha Qualidade e Governança 2026', 'Equipes de atendimento e gestão', 35, now() - interval '5 days', now() + interval '25 days', 'active', 'a0000000-0000-0000-0000-000000000006', now() - interval '5 days', now())
ON CONFLICT (id) DO NOTHING;

-- 16. PEOPLE PROFILES E TIMES (SPRINT 07 E 08)
INSERT INTO valorapesquisa.people_profiles (id, organization_id, display_name, role_title, status, created_at, updated_at)
VALUES
  ('b1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Mariana Silva', 'Diretoria de Operações & Logística', 'active', now() - interval '60 days', now()),
  ('b1111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'Carlos Drummond', 'Coordenação de Gente & Cultura', 'active', now() - interval '50 days', now()),
  ('b1111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', 'Ana Beatriz Souza', 'Liderança Técnica de Engenharia', 'active', now() - interval '40 days', now()),
  ('b1111111-1111-1111-1111-111111111114', '11111111-1111-1111-1111-111111111111', 'Roberto Albuquerque', 'Especialista em Qualidade & Processos', 'active', now() - interval '30 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.people_teams (id, organization_id, name, responsible_profile_id, created_at, updated_at)
VALUES
  ('b2111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Engenharia & Arquitetura de Software', 'b1111111-1111-1111-1111-111111111113', now() - interval '40 days', now()),
  ('b2111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'Gente, Cultura & Retenção de Talentos', 'b1111111-1111-1111-1111-111111111112', now() - interval '40 days', now()),
  ('b2111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', 'Operações, Logística & SLAs', 'b1111111-1111-1111-1111-111111111111', now() - interval '40 days', now())
ON CONFLICT (id) DO NOTHING;

-- 17. FRAMEWORK DE COMPETÊNCIAS E AVALIAÇÕES HUMANAS
INSERT INTO valorapesquisa.people_competency_frameworks (id, organization_id, name, description, scale_max, version, status, created_at, updated_at)
VALUES
  ('b3111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Framework de Liderança Valora 2026', 'Competências essenciais de visão estratégica, liderança e foco no cliente.', 5.0, 1, 'active', now() - interval '45 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.people_competencies (id, organization_id, framework_id, name, description, created_at, updated_at)
VALUES
  ('b4111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'b3111111-1111-1111-1111-111111111111', 'Visão Sistêmica & Metas', 'Capacidade de conectar objetivos estratégicos da organização às rotinas diárias da equipe.', now() - interval '45 days', now()),
  ('b4111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'b3111111-1111-1111-1111-111111111111', 'Feedback & Segurança Psicológica', 'Criação de ambiente propício ao aprendizado e escuta ativa de sugestões e riscos.', now() - interval '45 days', now()),
  ('b4111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', 'b3111111-1111-1111-1111-111111111111', 'Resolução de Problemas Complexos', 'Agilidade no tratamento de divergências operacionais e melhoria de processos.', now() - interval '45 days', now()),
  ('b4111111-1111-1111-1111-111111111114', '11111111-1111-1111-1111-111111111111', 'b3111111-1111-1111-1111-111111111111', 'Foco no Cliente & Excelência', 'Compromisso com o cumprimento dos SLAs e qualidade percebida pelo usuário final.', now() - interval '45 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.people_competency_assessments (id, organization_id, competency_id, subject_type, profile_id, team_id, expected_level, observed_level, evidence_summary, human_reviewed, reviewed_by_user_id, assessed_at)
VALUES
  ('b5111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'b4111111-1111-1111-1111-111111111111', 'team', null, 'b2111111-1111-1111-1111-111111111111', 4.5, 3.8, 'Alinhamento verificado nos comitês de sprint e notas do diagnóstico institucional.', true, 'a0000000-0000-0000-0000-000000000002', now() - interval '20 days'),
  ('b5111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'b4111111-1111-1111-1111-111111111112', 'team', null, 'b2111111-1111-1111-1111-111111111112', 4.0, 4.2, 'Cadência quinzenal de check-ins 1-on-1 registrada com 92% de adesão.', true, 'a0000000-0000-0000-0000-000000000002', now() - interval '18 days'),
  ('b5111111-1111-1111-1111-111111111113', '11111111-1111-1111-1111-111111111111', 'b4111111-1111-1111-1111-111111111113', 'team', null, 'b2111111-1111-1111-1111-111111111113', 4.0, 3.1, 'Tempo de resposta a incidentes fora da meta nos últimos 45 dias. Plano de ação criado.', true, 'a0000000-0000-0000-0000-000000000002', now() - interval '15 days'),
  ('b5111111-1111-1111-1111-111111111114', '11111111-1111-1111-1111-111111111111', 'b4111111-1111-1111-1111-111111111114', 'person', 'b1111111-1111-1111-1111-111111111114', null, 5.0, 4.6, 'Índice de conformidade de entregas e auditorias com 96% de satisfação.', true, 'a0000000-0000-0000-0000-000000000002', now() - interval '10 days')
ON CONFLICT (id) DO NOTHING;

-- 18. DIMENSÕES CULTURAIS AGREGADAS E SINAIS DE ENGAJAMENTO
INSERT INTO valorapesquisa.people_culture_dimensions (id, organization_id, name, description, minimum_sample_size, created_at, updated_at)
VALUES
  ('cd111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Segurança Psicológica & Abertura ao Erro', 'Percepção de liberdade para propor ideias e alertar riscos sem receio.', 5, now() - interval '40 days', now()),
  ('cd111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'Alinhamento Estratégico & Propósito', 'Compreensão das metas corporativas e conexão com o trabalho do dia a dia.', 5, now() - interval '40 days', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.people_culture_assessments (id, organization_id, dimension_id, source, evidence_summary, score, sample_size, confidence, limitations, assessed_at)
VALUES
  ('ca111111-1111-1111-1111-111111111121', '11111111-1111-1111-1111-111111111111', 'cd111111-1111-1111-1111-111111111111', 'Diagnóstico Institucional 2026', '84% dos membros relatam ambiente seguro para manifestação de divergências.', 82.4, 45, 'high', 'Amostra cobre 90% das áreas técnicas.', now() - interval '8 days'),
  ('ca111111-1111-1111-1111-111111111122', '11111111-1111-1111-1111-111111111111', 'cd111111-1111-1111-1111-111111111112', 'Diagnóstico Institucional 2026', 'Clareza consistente sobre metas corporativas e prioridades da gestão.', 78.0, 45, 'high', 'Participação homogênea entre turnos.', now() - interval '8 days')
ON CONFLICT (id) DO NOTHING;

INSERT INTO valorapesquisa.people_engagement_signals (id, organization_id, source, evidence_summary, score, confidence, limitations, observed_at)
VALUES
  ('b6111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Check-ins Quinzenais', '92% de participação contínua nas reuniões 1-on-1 de alinhamento com lideranças.', 88.0, 'high', 'Baseado em confirmações formais de pauta.', now() - interval '6 days'),
  ('b6111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'Pesquisa Pulso de Saúde', 'Queda pontual na percepção de equilíbrio em períodos de fechamento trimestral.', 58.0, 'medium', 'Amostra de 30 respondentes da área de operações.', now() - interval '4 days')
ON CONFLICT (id) DO NOTHING;

-- 19. PLANOS DE DESENVOLVIMENTO INDIVIDUAL E DE EQUIPE
INSERT INTO valorapesquisa.people_development_plans (id, organization_id, subject_type, profile_id, team_id, objective, responsible_profile_id, due_date, status, created_at, updated_at)
VALUES
  ('b7111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'team', null, 'b2111111-1111-1111-1111-111111111112', 'Capacitação em Liderança Situacional e Feedback Construtivo', 'b1111111-1111-1111-1111-111111111112', current_date + 40, 'active', now() - interval '14 days', now()),
  ('b7111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'team', null, 'b2111111-1111-1111-1111-111111111113', 'Upskilling em Gestão de Incidentes Operacionais e SLAs', 'b1111111-1111-1111-1111-111111111111', current_date + 55, 'active', now() - interval '10 days', now())
ON CONFLICT (id) DO NOTHING;

-- 20. PESQUISA HISTÓRICA ENCERRADA (JORNADA SPRINT 08)
INSERT INTO valorapesquisa.surveys (id, organization_id, form_id, form_version_id, name, title, description, status, starts_at, expires_at, row_version, created_at, updated_at)
VALUES
  ('fe111111-1111-1111-1111-333333333333', '11111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', 'fb111111-1111-1111-1111-111111111111', 'Diagnóstico Institucional 2025 (Histórico)', 'Diagnóstico Institucional 2025 (Histórico)', 'Ciclo anual anterior encerrado para comparação longitudinal e baseline.', 'closed', now() - interval '380 days', now() - interval '350 days', 3, now() - interval '380 days', now() - interval '350 days')
ON CONFLICT (id) DO NOTHING;

-- 21. CERTIFICADOS METODOLÓGICOS EMITIDOS (DERIVADOS DIRETAMENTE DE RESULT_SCORES CALCULADOS)
INSERT INTO valorapesquisa.certificates (id, organization_id, response_id, certificate_code, participant_name, issuer_name, survey_name, maturity_label, status, validation_code, created_at)
SELECT
  ('ce' || substr(replace(r.id::text, '-', ''), 3))::uuid,
  r.organization_id,
  r.id,
  'VAL-' || upper(left(replace(r.id::text, '-', ''), 4)) || '-' || upper(right(replace(r.id::text, '-', ''), 6)),
  'Participante ' || upper(right(replace(r.id::text, '-', ''), 4)),
  'Valora Group',
  s.title,
  rs.maturity_label,
  'issued',
  'VAL-' || upper(left(replace(r.id::text, '-', ''), 4)) || '-' || upper(right(replace(r.id::text, '-', ''), 6)),
  r.submitted_at
FROM valorapesquisa.responses r
JOIN valorapesquisa.result_scores rs ON rs.response_id = r.id
JOIN valorapesquisa.surveys s ON s.id = r.survey_id
WHERE r.organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
)
ON CONFLICT (id) DO NOTHING;

COMMIT;


