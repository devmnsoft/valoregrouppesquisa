-- AVISO Fase 1: o bootstrap canonico agora e database/postgresql/banco_completo.sql. Este script historico foi preservado para referencia/compatibilidade.
WITH org AS (
  INSERT INTO valorapesquisa.organizations(name, public_name, slug, email, plan_code)
  VALUES ('Valora Group Demo','Valora Group Demo','valora-demo','demo@valoragroup.com.br','professional')
  ON CONFLICT (slug) DO UPDATE SET name=EXCLUDED.name, plan_code=EXCLUDED.plan_code, updated_at=now()
  RETURNING id
), sub AS (
  INSERT INTO valorapesquisa.subscriptions(organization_id, plan_id, status)
  SELECT org.id, plans.id, 'active' FROM org JOIN valorapesquisa.plans plans ON plans.code='professional'
  ON CONFLICT (organization_id) DO UPDATE SET plan_id=EXCLUDED.plan_id,status='active',updated_at=now()
), form AS (
  INSERT INTO valorapesquisa.forms(organization_id,name,description,category,time_min,scoring_method,status,is_global)
  SELECT id,'Diagnóstico Valora Insight™','Pesquisa demo oficial com 5 dimensões e 25 perguntas.','valora-insight',8,'sum','published',false FROM org
  RETURNING id, organization_id
), dims AS (
  INSERT INTO valorapesquisa.form_dimensions(form_id,name,description,display_order,max_score)
  SELECT form.id, d.name, d.name, d.ord, 25 FROM form CROSS JOIN (VALUES
    ('Cultura e Propósito',1),('Gestão e Governança',2),('Liderança',3),('Pessoas e Talentos',4),('Resultados e Crescimento',5)
  ) AS d(name,ord)
  RETURNING id, form_id, name, display_order
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
), qs AS (
  INSERT INTO valorapesquisa.questions(form_id, dimension_id, text, type, weight, max_score, required, display_order)
  SELECT dims.form_id, dims.id, oq.question_text, 'scale', 1, 5, true, oq.display_order
  FROM dims JOIN official_questions oq ON oq.dimension_name=dims.name
  RETURNING id
), opts AS (
  INSERT INTO valorapesquisa.question_options(question_id,text,score,display_order)
  SELECT qs.id, opt.label, opt.score, opt.score FROM qs CROSS JOIN (VALUES
    ('Discordo totalmente',1),('Discordo parcialmente',2),('Neutro',3),('Concordo parcialmente',4),('Concordo totalmente',5)
  ) AS opt(label,score)
), survey AS (
  INSERT INTO valorapesquisa.surveys(organization_id,form_id,title,description,status,token_hash,public_slug,public_url,starts_at,expires_at,is_free,is_featured,visible_on_home,allow_repeat,require_identification,lgpd_required)
  SELECT organization_id,id,'Diagnóstico Valora Insight™ Demo','Link público demo da Sprint 3.','active','demo-token-valora-insight','demo-valora-insight','/?survey=00000000-0000-0000-0000-000000000003&token=demo-token-valora-insight&org=valora-demo',now(),now()+interval '365 days',true,true,true,true,true,true FROM form
  RETURNING id, organization_id
)
INSERT INTO valorapesquisa.survey_links(id, organization_id, survey_id, token_hash, public_url, status, expires_at)
SELECT '00000000-0000-0000-0000-000000000011'::uuid, organization_id, id, 'demo-token-valora-insight', '/?survey=' || id || '&token=demo-token-valora-insight&org=valora-demo', 'active', now()+interval '365 days' FROM survey
ON CONFLICT (id) DO UPDATE SET status=EXCLUDED.status, updated_at=now();
