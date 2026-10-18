-- ==============================================================================
-- VALORA INSIGHT — SCRIPT DE RESET EXPLÍCITO DE AMBIENTE DE HOMOLOGAÇÃO
-- ATENÇÃO: OPERAÇÃO DESTRUTIVA RESTRITA A AMBIENTES DESCARTÁVEIS / CI / TESTES
-- ==============================================================================

DO $$
BEGIN
  IF current_database() ~* '(prod|production)' THEN
    RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: reset_homologacao.sql JAMAIS pode ser executado em ambiente de produção (banco atual: %).', current_database();
  END IF;

  IF NOT (current_database() ~* '(test|teste|homolog|qa|dev|valora)') THEN
    RAISE EXCEPTION 'BLOQUEIO DE SEGURANÇA: reset_homologacao.sql exige identificação explícita do ambiente permitido (test/homolog/qa/dev). Banco atual: %', current_database();
  END IF;
END $$;

BEGIN;

-- 1. Limpeza de artefatos de homologação e fixtures conhecidas
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
