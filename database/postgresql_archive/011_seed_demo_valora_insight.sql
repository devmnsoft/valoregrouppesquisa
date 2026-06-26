WITH org AS (
  INSERT INTO valorapesquisa.organizations(name, public_name, slug, email, plan_id)
  VALUES ('Valora Group Demo','Valora Group Demo','valora-demo','demo@valoragroup.com.br','professional')
  ON CONFLICT (slug) DO UPDATE SET name=EXCLUDED.name, plan_id=EXCLUDED.plan_id, updated_at=now()
  RETURNING id
), sub AS (
  INSERT INTO valorapesquisa.subscriptions(organization_id, plan_id, status)
  SELECT id, 'professional', 'active' FROM org
  ON CONFLICT DO NOTHING
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
), qs AS (
  INSERT INTO valorapesquisa.questions(form_id, dimension_id, text, type, weight, max_score, required, display_order)
  SELECT dims.form_id, dims.id, dims.name || ' — pergunta ' || gs.n, 'scale', 1, 5, true, ((dims.display_order-1)*5)+gs.n
  FROM dims CROSS JOIN generate_series(1,5) AS gs(n)
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
