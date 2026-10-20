-- ==============================================================================
-- VALORA INSIGHT — SCRIPT DE RESET EXPLÍCITO DE AMBIENTE DE HOMOLOGAÇÃO
-- ATENÇÃO: OPERAÇÃO DESTRUTIVA RESTRITA A AMBIENTES DESCARTÁVEIS / CI / TESTES
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
      RAISE EXCEPTION 'BLOQUEIO CRÍTICO DE SEGURANÇA: environment_metadata indica que este banco é de PRODUÇÃO. Operação destrutiva cancelada.';
    END IF;
  END IF;

  -- 2. Bloqueio absoluto para ambientes identificados como produção
  IF current_database() ~* '(prod|production|live|prd)' THEN
    RAISE EXCEPTION 'BLOQUEIO CRÍTICO DE SEGURANÇA: reset_homologacao.sql não pode ser executado em ambiente de produção (banco atual: %).', current_database();
  END IF;

  -- 3. Bloqueio para usuário de aplicação em runtime (least privilege)
  IF current_user IN ('valora_app_runtime', 'valora_readonly') THEN
    RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: Usuário de aplicação em tempo de execução (%) não possui permissão para redefinir o banco.', current_user;
  END IF;

  -- 4. O nome do banco 'valora' sozinho NÃO é evidência suficiente de ambiente seguro
  IF current_database() ~* '^(valora|valora_db|valora_app|valora_insight)$' THEN
    IF v_allow_seed IS DISTINCT FROM 'true' THEN
      RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: Banco de dados com nome padrão de produção (%) exige autorização explícita via valora.allow_demo_seed=true.', current_database();
    END IF;
  END IF;

  -- 5. Exige identificação confiável de ambiente não-produtivo
  IF NOT (current_database() ~* '(test|teste|homolog|staging|qa|dev)' OR v_allow_seed = 'true' OR v_env IN ('Development', 'Testing', 'Homologation')) THEN
    RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: reset_homologacao.sql exige identificação explícita de ambiente seguro (test/homolog/staging/qa/dev ou valora.allow_demo_seed=true). Banco atual: %', current_database();
  END IF;
END $$;

BEGIN;

-- 1. Limpeza de artefatos de homologação e fixtures conhecidas
DELETE FROM valorapesquisa.certificates WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.valora_ai_insight_evidence_links WHERE insight_id IN (
  SELECT id FROM valorapesquisa.valora_ai_insights WHERE organization_id IN (
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '33333333-3333-3333-3333-333333333333'
  )
);

DELETE FROM valorapesquisa.valora_ai_insights WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.valora_ai_evidence_items WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.evolution_cycles WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.action_items WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.action_plans WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.evidence_items WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.result_scores WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.response_answers WHERE response_id IN (
  SELECT id FROM valorapesquisa.responses WHERE organization_id IN (
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '33333333-3333-3333-3333-333333333333'
  )
);

DELETE FROM valorapesquisa.responses WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.diagnostic_cycles WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.campaigns WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.survey_links WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.surveys WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_development_plan_items WHERE development_plan_id IN (
  SELECT id FROM valorapesquisa.people_development_plans WHERE organization_id IN (
    '11111111-1111-1111-1111-111111111111',
    '22222222-2222-2222-2222-222222222222',
    '33333333-3333-3333-3333-333333333333'
  )
);

DELETE FROM valorapesquisa.people_development_plans WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_engagement_signals WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_culture_assessments WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_culture_dimensions WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_competency_assessments WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_competencies WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_competency_frameworks WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_teams WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.people_profiles WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

COMMIT;

-- Após o reset, o seed oficial não destrutivo pode ser reaplicado:
-- \i seed_homologacao.sql
