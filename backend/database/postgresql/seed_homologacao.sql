-- ==============================================================================
-- VALORA INSIGHT — SEED OFICIAL DE HOMOLOGAÇÃO POSTGRESQL (IDEMPOTENTE)
-- SPRINT 5: ESTABILIDADE GERAL, CONSISTÊNCIA VISUAL E PORTÃO DE ENTREGA
-- Ambientes: Homologação / Demonstração / Validação E2E
-- Senha de todos os usuários de teste: Admin@123
-- Hash BCrypt padrão: $2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2
-- ==============================================================================

BEGIN;

-- 1. ESTRUTURA BASE (GARANTIR COLUNAS DE SPRINT 1 A 4)
ALTER TABLE valorapesquisa.surveys ADD COLUMN IF NOT EXISTS row_version integer NOT NULL DEFAULT 1;
ALTER TABLE valorapesquisa.diagnostic_cycles ADD COLUMN IF NOT EXISTS row_version integer NOT NULL DEFAULT 1;

-- 2. ORGANIZAÇÕES
INSERT INTO valorapesquisa.organizations (id, name, slug, status, created_at, updated_at)
VALUES
  ('11111111-1111-1111-1111-111111111111', 'Cliente Demonstração', 'cliente-demonstracao', 'active', now(), now()),
  ('22222222-2222-2222-2222-222222222222', 'Cliente Início', 'cliente-inicio', 'active', now(), now()),
  ('33333333-3333-3333-3333-333333333333', 'Cliente Isolamento', 'cliente-isolamento', 'active', now(), now())
ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  status = EXCLUDED.status,
  updated_at = now();

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
  ('evolution'), ('reports')
) m(code)
ON CONFLICT (organization_id, module_code) DO UPDATE SET enabled = true, updated_at = now();

-- 4. USUÁRIOS DE HOMOLOGAÇÃO (SENHA Admin@123)
INSERT INTO valorapesquisa.users (id, organization_id, name, email, password_hash, status, created_at, updated_at)
VALUES
  -- Cliente Demonstração
  ('a0000000-0000-0000-0000-000000000001', '11111111-1111-1111-1111-111111111111', 'Admin Global Valora', 'admin.global@valora.local', '$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2', 'active', now(), now()),
  ('a0000000-0000-0000-0000-000000000002', '11111111-1111-1111-1111-111111111111', 'Admin Cliente Demonstração', 'admin.demonstracao@valora.local', '$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2', 'active', now(), now()),
  ('a0000000-0000-0000-0000-000000000003', '11111111-1111-1111-1111-111111111111', 'Analista de Resultados', 'analista.demonstracao@valora.local', '$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2', 'active', now(), now()),
  ('a0000000-0000-0000-0000-000000000004', '11111111-1111-1111-1111-111111111111', 'Respondente Colaborador', 'respondente.demonstracao@valora.local', '$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2', 'active', now(), now()),
  -- Cliente Início
  ('a0000000-0000-0000-0000-000000000005', '22222222-2222-2222-2222-222222222222', 'Admin Cliente Início', 'admin.inicio@valora.local', '$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2', 'active', now(), now()),
  -- Cliente Isolamento
  ('a0000000-0000-0000-0000-000000000006', '33333333-3333-3333-3333-333333333333', 'Admin Cliente Isolamento', 'admin.isolamento@valora.local', '$2a$11$s6GuFVkm7kQG/5Ko4VqUvO2V1KmWB51Ceqt/ctalZd.xrf/eqgQQ2', 'active', now(), now())
ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  password_hash = EXCLUDED.password_hash,
  status = EXCLUDED.status,
  updated_at = now();

-- 5. ATRIBUIÇÃO DE PAPÉIS (RBAC)
INSERT INTO valorapesquisa.user_roles (user_id, role_id)
SELECT u.id, r.id
FROM (VALUES
  ('a0000000-0000-0000-0000-000000000001'::uuid, 'admin_valora'),
  ('a0000000-0000-0000-0000-000000000002'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000003'::uuid, 'analista_resultados'),
  ('a0000000-0000-0000-0000-000000000004'::uuid, 'participante'),
  ('a0000000-0000-0000-0000-000000000005'::uuid, 'empresa_admin'),
  ('a0000000-0000-0000-0000-000000000006'::uuid, 'empresa_admin')
) u(id, role_code)
JOIN valorapesquisa.roles r ON r.code = u.role_code AND r.organization_id IS NULL
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 6. QUESTIONÁRIOS E FORMULÁRIOS
-- Form 1: Cliente Demonstração
INSERT INTO valorapesquisa.forms (id, organization_id, code, name, title, slug, form_key, status, created_at, updated_at)
VALUES
  ('f1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'form_maturidade_demo', 'Diagnóstico de Maturidade Organizacional', 'Diagnóstico de Maturidade Organizacional', 'form-maturidade-demo', 'form-maturidade-demo', 'published', now(), now()),
  ('f2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'form_onboarding_inicio', 'Diagnóstico de Clima e Integração', 'Diagnóstico de Clima e Integração', 'form-clima-inicio', 'form-clima-inicio', 'published', now(), now()),
  ('f3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'form_isolamento_teste', 'Diagnóstico de Isolamento Seguro', 'Diagnóstico de Isolamento Seguro', 'form-isolamento-teste', 'form-isolamento-teste', 'published', now(), now())
ON CONFLICT (id) DO UPDATE SET
  name = EXCLUDED.name,
  title = EXCLUDED.title,
  status = EXCLUDED.status,
  updated_at = now();

-- Versões de Formulário
INSERT INTO valorapesquisa.form_versions (id, form_id, organization_id, version, version_number, status, is_immutable, row_version, created_at, updated_at)
VALUES
  ('fb111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 1, 1, 'published', true, 1, now(), now()),
  ('fb222222-2222-2222-2222-222222222222', 'f2222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 1, 1, 'published', true, 1, now(), now()),
  ('fb333333-3333-3333-3333-333333333333', 'f3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 1, 1, 'published', true, 1, now(), now())
ON CONFLICT (id) DO UPDATE SET status = EXCLUDED.status, updated_at = now();

-- Seções
INSERT INTO valorapesquisa.form_section_versions (id, organization_id, form_version_id, title, position, display_order)
VALUES
  ('fc111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fb111111-1111-1111-1111-111111111111', 'Liderança e Alinhamento Estratégico', 1, 1),
  ('fc222222-2222-2222-2222-222222222222', '22222222-2222-2222-2222-222222222222', 'fb222222-2222-2222-2222-222222222222', 'Acolhimento e Ferramentas', 1, 1),
  ('fc333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fb333333-3333-3333-3333-333333333333', 'Conformidade e Isolamento', 1, 1)
ON CONFLICT (id) DO NOTHING;

-- Perguntas
INSERT INTO valorapesquisa.question_versions (id, organization_id, section_id, code, type, title, required, position, display_order, settings)
VALUES
  ('fd111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fc111111-1111-1111-1111-111111111111', 'lid_comunicacao', 'likert_1_5', 'A liderança comunica os objetivos estratégicos de forma transparente e compreensível.', true, 1, 1, '{}'::jsonb),
  ('fd111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fc111111-1111-1111-1111-111111111111', 'lid_autonomia', 'likert_1_5', 'As equipes possuem autonomia e suporte para resolver problemas operacionais.', true, 2, 2, '{}'::jsonb),
  ('fd222222-2222-2222-2222-222222222221', '22222222-2222-2222-2222-222222222222', 'fc222222-2222-2222-2222-222222222222', 'ini_recursos', 'likert_1_5', 'Os recursos de trabalho necessários foram disponibilizados no prazo acordado.', true, 1, 1, '{}'::jsonb),
  ('fd333333-3333-3333-3333-333333333331', '33333333-3333-3333-3333-333333333333', 'fc333333-3333-3333-3333-333333333333', 'iso_seguranca', 'likert_1_5', 'Os acessos organizacionais estão estritamente segregados.', true, 1, 1, '{}'::jsonb)
ON CONFLICT (id) DO NOTHING;

-- Espelho na tabela questions para compatibilidade de FK legada em response_answers
INSERT INTO valorapesquisa.questions (id, code, text, display_order, position, question_type, is_qualitative, is_required)
VALUES
  ('fd111111-1111-1111-1111-111111111111', 'lid_comunicacao', 'A liderança comunica os objetivos estratégicos de forma transparente e compreensível.', 1, 1, 'scale_1_5', false, true),
  ('fd111111-1111-1111-1111-111111111112', 'lid_autonomia', 'As equipes possuem autonomia e suporte para resolver problemas operacionais.', 2, 2, 'scale_1_5', false, true),
  ('fd222222-2222-2222-2222-222222222221', 'ini_recursos', 'Os recursos de trabalho necessários foram disponibilizados no prazo acordado.', 1, 1, 'scale_1_5', false, true),
  ('fd333333-3333-3333-3333-333333333331', 'iso_seguranca', 'Os acessos organizacionais estão estritamente segregados.', 1, 1, 'scale_1_5', false, true)
ON CONFLICT (id) DO NOTHING;

-- 7. DIAGNÓSTICOS E PESQUISAS
-- Cliente Demonstração: Ciclo publicado e em andamento com respostas e evidências
INSERT INTO valorapesquisa.surveys (id, organization_id, form_id, form_version_id, name, title, description, status, starts_at, expires_at, row_version, created_at, updated_at)
VALUES
  ('fe111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', 'fb111111-1111-1111-1111-111111111111', 'Diagnóstico de Maturidade 2026', 'Diagnóstico de Maturidade 2026', 'Ciclo oficial de avaliação organizacional com evidências completas.', 'active', now() - interval '10 days', now() + interval '20 days', 2, now() - interval '10 days', now())
ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title, status = EXCLUDED.status, updated_at = now();

INSERT INTO valorapesquisa.diagnostic_cycles (id, organization_id, survey_id, form_id, title, description, cycle_number, methodology_version, status, opened_at, published_at, confidence_level, processing_status, row_version, created_at, updated_at)
VALUES
  ('c1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'f1111111-1111-1111-1111-111111111111', 'Diagnóstico de Maturidade 2026', 'Ciclo oficial de avaliação organizacional com evidências completas.', 1, '1.0', 'active', now() - interval '10 days', now() - interval '10 days', 94.5, 'processed', 2, now() - interval '10 days', now())
ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title, status = EXCLUDED.status, updated_at = now();

-- Cliente Isolamento: Diagnóstico próprio para comprovação de multitenant
INSERT INTO valorapesquisa.surveys (id, organization_id, form_id, form_version_id, name, title, description, status, starts_at, expires_at, row_version, created_at, updated_at)
VALUES
  ('fe333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'f3333333-3333-3333-3333-333333333333', 'fb333333-3333-3333-3333-333333333333', 'Diagnóstico Isolado 2026', 'Diagnóstico Isolado 2026', 'Pesquisa da organização Cliente Isolamento.', 'active', now() - interval '5 days', now() + interval '25 days', 1, now() - interval '5 days', now())
ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title, status = EXCLUDED.status, updated_at = now();

INSERT INTO valorapesquisa.diagnostic_cycles (id, organization_id, survey_id, form_id, title, description, cycle_number, methodology_version, status, opened_at, published_at, confidence_level, processing_status, row_version, created_at, updated_at)
VALUES
  ('c3333333-3333-3333-3333-333333333333', '33333333-3333-3333-3333-333333333333', 'fe333333-3333-3333-3333-333333333333', 'f3333333-3333-3333-3333-333333333333', 'Diagnóstico Isolado 2026', 'Ciclo isolado da organização Cliente Isolamento.', 1, '1.0', 'active', now() - interval '5 days', now() - interval '5 days', 88.0, 'processed', 1, now() - interval '5 days', now())
ON CONFLICT (id) DO UPDATE SET title = EXCLUDED.title, status = EXCLUDED.status, updated_at = now();

-- 8. RESPOSTAS (Cliente Demonstração)
DO $$
DECLARE
  v_resp_id uuid;
  v_i int;
BEGIN
  FOR v_i IN 1..5 LOOP
    v_resp_id := ('de111111-1111-1111-1111-00000000000' || v_i)::uuid;
    INSERT INTO valorapesquisa.responses (id, organization_id, survey_id, status, qualitative, submitted_at, created_at)
    VALUES (v_resp_id, '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'completed', 'Comunicação da liderança é constante e clara.', now() - (v_i || ' days')::interval, now() - (v_i || ' days')::interval)
    ON CONFLICT (id) DO NOTHING;

    INSERT INTO valorapesquisa.response_answers (id, response_id, question_id, numeric_value, text_value, score, max_score, created_at)
    VALUES
      (gen_random_uuid(), v_resp_id, 'fd111111-1111-1111-1111-111111111111', 5, 'Concordo totalmente', 5.0, 5.0, now() - (v_i || ' days')::interval),
      (gen_random_uuid(), v_resp_id, 'fd111111-1111-1111-1111-111111111112', 4, 'Concordo em grande parte', 4.0, 5.0, now() - (v_i || ' days')::interval)
    ON CONFLICT (response_id, question_id) DO NOTHING;

    INSERT INTO valorapesquisa.result_scores (id, organization_id, response_id, total_score, max_score, percentage, maturity_label, created_at)
    VALUES (gen_random_uuid(), '11111111-1111-1111-1111-111111111111', v_resp_id, 105, 125, 84.0, 'Consolidado', now() - (v_i || ' days')::interval)
    ON CONFLICT (response_id) DO NOTHING;
  END LOOP;
END $$;

-- 9. EVIDÊNCIAS METODOLÓGICAS (Cliente Demonstração)
INSERT INTO valorapesquisa.evidence_items (id, organization_id, survey_id, response_id, capability_code, dimension_code, source_type, source_id, concept_code, metric_code, index_code, evidence_type, normalized_value, weight, confidence_weight, mapping_status, text_excerpt, created_at)
VALUES
  ('e1111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'de111111-1111-1111-1111-000000000001', 'cap_lideranca', 'lideranca', 'response_answer', 'de111111-1111-1111-1111-000000000001', 'lid_comunicacao', 'METRIC_LID_COM', 'IDX_LID', 'likert_answer', 88.0, 1.0, 0.95, 'mapped', 'Comunicação da liderança é constante e clara.', now() - interval '5 days'),
  ('e1111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'de111111-1111-1111-1111-000000000002', 'cap_lideranca', 'lideranca', 'response_answer', 'de111111-1111-1111-1111-000000000002', 'lid_autonomia', 'METRIC_LID_AUT', 'IDX_LID', 'likert_answer', 82.0, 1.0, 0.90, 'mapped', 'As equipes possuem autonomia operacional.', now() - interval '4 days')
ON CONFLICT (id) DO UPDATE SET mapping_status = EXCLUDED.mapping_status;

-- 10. ACHADOS ANALÍTICOS (INSIGHTS) COM ORIGEM CANÔNICA
INSERT INTO valorapesquisa.valora_ai_runs (id, organization_id, diagnosis_id, prompt_code, prompt_version, provider, model, correlation_id, input_json, status, created_at, updated_at)
VALUES
  ('da111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'diag-audit-prompt', 1, 'valora-engine', 'deterministic-v1', 'seed-run-corr-1', '{}'::jsonb, 'completed', now() - interval '3 days', now())
ON CONFLICT (id) DO NOTHING;

-- Insight 1: Sugestão IA Aprovada
INSERT INTO valorapesquisa.valora_ai_insights (id, organization_id, diagnostic_id, ai_run_id, insight_type, title, summary, evidence_summary, related_dimension, severity, priority, confidence_level, limitation, recommendation, status, review_version, reviewed_by_user_id, reviewed_at, created_at, updated_at)
VALUES
  ('ba111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'da111111-1111-1111-1111-111111111111', 'recommendation', 'Fortalecer Alinhamento em Comunicação da Liderança', 'Evidências apontam forte engajamento, com oportunidade de padronização nas cadências de liderança.', '5 respostas com média 85% de maturidade convergente.', 'Liderança', 'medium', 'high', 'Alto', 'Válido sob a amostragem coletada no ciclo.', 'Implementar rituais quinzenais de alinhamento com pauta transparente.', 'converted_to_action', 1, 'a0000000-0000-0000-0000-000000000002', now() - interval '2 days', now() - interval '3 days', now())
ON CONFLICT (id) DO UPDATE SET status = EXCLUDED.status, updated_at = now();

-- Insight 2: Sugestão IA Pendente de Revisão
INSERT INTO valorapesquisa.valora_ai_insights (id, organization_id, diagnostic_id, ai_run_id, insight_type, title, summary, evidence_summary, related_dimension, severity, priority, confidence_level, limitation, recommendation, status, review_version, created_at, updated_at)
VALUES
  ('ba111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'da111111-1111-1111-1111-111111111111', 'warning', 'Potencial Assimetria de Informações Operacionais', 'Parte dos respondentes indicou dependência excessiva de canais informais.', 'Evidência textual cruzada com dispersão de autonomia.', 'Operações', 'low', 'medium', 'Médio', 'Amostra em consolidação.', 'Estruturar repositório oficial de diretrizes e playbooks.', 'pending_review', 0, now() - interval '1 days', now())
ON CONFLICT (id) DO UPDATE SET status = EXCLUDED.status, updated_at = now();

-- 11. PLANO DE AÇÃO NO ACTION CENTER (VINCULADO AO INSIGHT APROVADO)
INSERT INTO valorapesquisa.action_plans (id, organization_id, title, summary, origin_type, origin_id, diagnostic_id, status, priority, owner_user_id, due_at, evidence_summary, expected_outcome, created_at, updated_at)
VALUES
  ('aa111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'Plano de Ação: Cadência de Alinhamento da Liderança', 'Estruturação dos fóruns de alinhamento quinzenais conforme achado analítico.', 'ai_insight', 'ba111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'in_execution', 'high', 'a0000000-0000-0000-0000-000000000002', now() + interval '30 days', '5 respostas com média 85% de maturidade convergente.', 'Atingir 95% de clareza nas metas de liderança no próximo ciclo.', now() - interval '2 days', now())
ON CONFLICT (id) DO UPDATE SET status = EXCLUDED.status, updated_at = now();

-- Atividades do Plano
INSERT INTO valorapesquisa.action_items (id, organization_id, action_plan_id, title, description, origin_type, origin_id, diagnostic_id, status, priority, responsible_user_id, due_at, progress_percent, evidence_summary, expected_outcome, created_at, updated_at)
VALUES
  ('ab111111-1111-1111-1111-111111111111', '11111111-1111-1111-1111-111111111111', 'aa111111-1111-1111-1111-111111111111', 'Definir pauta padrão dos alinhamentos quinzenais', 'Modelo de ata e ata compartilhada com indicadores.', 'manual', 'aa111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'completed', 'high', 'a0000000-0000-0000-0000-000000000003', now() - interval '1 days', 100, 'Modelo publicado no portal interno.', 'Template padronizado ativo.', now() - interval '2 days', now()),
  ('ab111111-1111-1111-1111-111111111112', '11111111-1111-1111-1111-111111111111', 'aa111111-1111-1111-1111-111111111111', 'Realizar primeiro ciclo de reuniões com as equipes', 'Aplicação do modelo nas 4 gerências.', 'manual', 'aa111111-1111-1111-1111-111111111111', 'fe111111-1111-1111-1111-111111111111', 'in_progress', 'medium', 'a0000000-0000-0000-0000-000000000002', now() + interval '15 days', 50, 'Registro de 2 reuniões concluídas.', 'Todas as gerências alinhadas.', now() - interval '1 days', now())
ON CONFLICT (id) DO UPDATE SET status = EXCLUDED.status, progress_percent = EXCLUDED.progress_percent, updated_at = now();

COMMIT;
