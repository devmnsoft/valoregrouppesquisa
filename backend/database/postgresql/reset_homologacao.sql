-- ==============================================================================
-- VALORA INSIGHT — SCRIPT DE RESET EXPLÍCITO DE AMBIENTE DE HOMOLOGAÇÃO
-- ATENÇÃO: OPERAÇÃO DESTRUTIVA RESTRITA A AMBIENTES DESCARTÁVEIS / CI / TESTES
-- ==============================================================================

DO $$
DECLARE
  v_allow_seed text := current_setting('valora.allow_demo_seed', true);
  v_env text := current_setting('valora.environment', true);
BEGIN
  -- 1. Bloqueio absoluto para ambientes identificados como produção
  IF current_database() ~* '(prod|production|live|prd)' THEN
    RAISE EXCEPTION 'BLOQUEIO CRÍTICO DE SEGURANÇA: reset_homologacao.sql não pode ser executado em ambiente de produção (banco atual: %).', current_database();
  END IF;

  -- 2. O nome do banco 'valora' sozinho NÃO é evidência suficiente de ambiente seguro
  IF current_database() ~* '^(valora|valora_db|valora_app|valora_insight)$' THEN
    IF v_allow_seed IS DISTINCT FROM 'true' THEN
      RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: Banco de dados com nome padrão de produção (%) exige autorização explícita via valora.allow_demo_seed=true.', current_database();
    END IF;
  END IF;

  -- 3. Exige identificação confiável de ambiente não-produtivo
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

DELETE FROM valorapesquisa.valora_ai_insight_evidence_links WHERE organization_id IN (
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '33333333-3333-3333-3333-333333333333'
);

DELETE FROM valorapesquisa.valora_ai_insights WHERE organization_id IN (
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

COMMIT;

-- Após o reset, o seed oficial não destrutivo pode ser reaplicado:
-- \i seed_homologacao.sql
