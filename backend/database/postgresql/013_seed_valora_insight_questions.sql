-- AVISO Fase 1: o bootstrap canonico agora e backend/database/postgresql/banco_completo.sql. Este script historico foi preservado para referencia/compatibilidade.
-- 013_seed_valora_insight_questions.sql
-- Seed oficial idempotente do Diagnóstico Valora Insight™.
WITH org AS (
  INSERT INTO valorapesquisa.organizations(name, public_name, slug, email, plan_code)
  VALUES ('Valora Group','Valora Group','valora','contato@valoragroup.com.br','enterprise')
  ON CONFLICT (slug) DO UPDATE SET name=EXCLUDED.name, public_name=EXCLUDED.public_name, plan_code=EXCLUDED.plan_code, updated_at=now()
  RETURNING id
), form AS (
  INSERT INTO valorapesquisa.forms(organization_id,name,description,category,time_min,scoring_method,status,is_global)
  SELECT id,'Diagnóstico Valora Insight™','Diagnóstico estratégico oficial de maturidade organizacional com 5 dimensões, 25 perguntas e devolutiva Valora Insight™.','valora-insight',8,'sum','published',true FROM org
  ON CONFLICT DO NOTHING
  RETURNING id, organization_id
), form_existing AS (
  SELECT id, organization_id FROM form
  UNION ALL
  SELECT f.id, f.organization_id FROM valorapesquisa.forms f JOIN org o ON o.id=f.organization_id WHERE f.name='Diagnóstico Valora Insight™'
  LIMIT 1
), dims AS (
  INSERT INTO valorapesquisa.form_dimensions(form_id,name,description,display_order,max_score)
  SELECT f.id, d.name, d.description, d.ord, 25 FROM form_existing f CROSS JOIN (VALUES
    ('Cultura e Propósito','Propósito, valores, colaboração e coerência cultural.',1),
    ('Gestão e Governança','Papéis, indicadores, decisões, controles e estabilidade.',2),
    ('Liderança','Direção, confiança, desenvolvimento e gestão de conflitos.',3),
    ('Pessoas e Talentos','Atração, integração, carreira, desempenho e retenção.',4),
    ('Resultados e Crescimento','Metas, produtividade, eficiência e crescimento sustentável.',5)
  ) AS d(name,description,ord)
  ON CONFLICT DO NOTHING
  RETURNING id, form_id, name
), all_dims AS (
  SELECT d.id,d.form_id,d.name FROM valorapesquisa.form_dimensions d JOIN form_existing f ON f.id=d.form_id
), official_questions(dimension_name, question_text, display_order) AS (VALUES
('Cultura e Propósito','As pessoas compreendem claramente o propósito e os valores da empresa.',1),
('Cultura e Propósito','Existe alinhamento entre o que a liderança comunica e o que é praticado no dia a dia.',2),
('Cultura e Propósito','Os colaboradores entendem como seu trabalho contribui para os resultados do negócio.',3),
('Cultura e Propósito','A cultura da empresa favorece colaboração, responsabilidade e comprometimento.',4),
('Cultura e Propósito','As decisões da empresa refletem seus valores e direcionamento estratégico.',5),
('Gestão e Governança','Papéis e responsabilidades estão claramente definidos.',6),
('Gestão e Governança','As decisões importantes seguem critérios e processos bem estabelecidos.',7),
('Gestão e Governança','A empresa acompanha regularmente indicadores relevantes para o negócio.',8),
('Gestão e Governança','Os gestores possuem informações confiáveis para tomar decisões.',9),
('Gestão e Governança','A operação funciona com estabilidade sem depender excessivamente de poucas pessoas.',10),
('Liderança','Os líderes dão direção clara às equipes.',11),
('Liderança','As lideranças atuam de forma alinhada entre si.',12),
('Liderança','Os líderes desenvolvem pessoas e fortalecem talentos.',13),
('Liderança','Os conflitos são tratados de forma construtiva e madura.',14),
('Liderança','As lideranças inspiram confiança e engajamento.',15),
('Pessoas e Talentos','A empresa atrai profissionais alinhados à sua cultura e objetivos.',16),
('Pessoas e Talentos','Novos colaboradores são integrados de forma estruturada.',17),
('Pessoas e Talentos','Existem oportunidades claras de desenvolvimento e crescimento profissional.',18),
('Pessoas e Talentos','Os talentos estratégicos tendem a permanecer na organização.',19),
('Pessoas e Talentos','O desempenho das pessoas é acompanhado e desenvolvido regularmente.',20),
('Resultados e Crescimento','A empresa atinge suas metas com consistência.',21),
('Resultados e Crescimento','Existe equilíbrio entre crescimento, organização e capacidade de execução.',22),
('Resultados e Crescimento','Os processos favorecem produtividade e eficiência.',23),
('Resultados e Crescimento','Problemas recorrentes são tratados na causa, e não apenas nos sintomas.',24),
('Resultados e Crescimento','A empresa está preparada para sustentar o crescimento nos próximos anos.',25)
), q AS (
  INSERT INTO valorapesquisa.questions(form_id, dimension_id, text, type, weight, max_score, required, display_order)
  SELECT d.form_id, d.id, oq.question_text, 'scale', 1, 5, true, oq.display_order
  FROM official_questions oq JOIN all_dims d ON d.name=oq.dimension_name
  WHERE NOT EXISTS (SELECT 1 FROM valorapesquisa.questions q WHERE q.form_id=d.form_id AND q.text=oq.question_text)
  RETURNING id
), all_q AS (
  SELECT q.id FROM valorapesquisa.questions q JOIN form_existing f ON f.id=q.form_id
  WHERE q.text IN (SELECT question_text FROM official_questions)
)
INSERT INTO valorapesquisa.question_options(question_id,text,score,display_order)
SELECT all_q.id, opt.label, opt.score, opt.score FROM all_q CROSS JOIN (VALUES
  ('Discordo totalmente',1),('Discordo parcialmente',2),('Neutro',3),('Concordo parcialmente',4),('Concordo totalmente',5)
) AS opt(label,score)
WHERE NOT EXISTS (SELECT 1 FROM valorapesquisa.question_options qo WHERE qo.question_id=all_q.id AND qo.score=opt.score);
