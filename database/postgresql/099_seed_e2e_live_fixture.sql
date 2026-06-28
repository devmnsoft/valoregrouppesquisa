-- Sprint 32 deterministic local E2E fixture.
-- Safe only for Development/Local/Test. Do not run against production databases.
DO $$
DECLARE
  v_org uuid := '11111111-1111-1111-1111-111111111111';
  v_user uuid := '22222222-2222-2222-2222-222222222222';
  v_form uuid := '33333333-3333-3333-3333-333333333333';
  v_survey uuid := '44444444-4444-4444-4444-444444444444';
  v_link uuid := '55555555-5555-5555-5555-555555555555';
  v_dim uuid;
  v_q uuid;
  i int;
  d int;
BEGIN
  INSERT INTO valorapesquisa.plans(id,name,price_label,display_order,status)
  VALUES ('free','Free','R$ 0',1,'active')
  ON CONFLICT (id) DO UPDATE SET name=excluded.name,status='active',updated_at=now();

  INSERT INTO valorapesquisa.plan_limits(plan_id,limit_key,limit_value)
  VALUES ('free','active_surveys',1),('free','monthly_responses',25)
  ON CONFLICT (plan_id,limit_key) DO UPDATE SET limit_value=excluded.limit_value;

  INSERT INTO valorapesquisa.organizations(id,name,public_name,slug,email,status,plan_id,settings_json)
  VALUES (v_org,'Valora E2E Organization','Valora E2E Organization','valora-e2e-organization','e2e-admin@valoragroup.local','active','free','{"fixture":"sprint32-e2e"}'::jsonb)
  ON CONFLICT (id) DO UPDATE SET name=excluded.name,public_name=excluded.public_name,status='active',plan_id='free',updated_at=now();

  INSERT INTO valorapesquisa.subscriptions(organization_id,plan_id,status,billing_status)
  VALUES (v_org,'free','active','ok')
  ON CONFLICT DO NOTHING;

  INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash,role,status)
  VALUES (v_user,v_org,'Valora E2E Admin','e2e-admin@valoragroup.local','$2a$11$vI8aWBnDgP7Qj3G4bq6g2uC1bYwzFjz9WbG4Y1fT6P.nQk8fPZg5G','admin','active')
  ON CONFLICT (email) DO UPDATE SET organization_id=excluded.organization_id,name=excluded.name,password_hash=excluded.password_hash,role='admin',status='active',updated_at=now();

  INSERT INTO valorapesquisa.forms(id,organization_id,name,description,category,time_min,scoring_method,status,is_global)
  VALUES (v_form,v_org,'Diagnóstico Valora Insight E2E','Fixture determinística para homologação live local.','e2e',8,'sum','active',false)
  ON CONFLICT (id) DO UPDATE SET name=excluded.name,status='active',updated_at=now();

  DELETE FROM valorapesquisa.question_options WHERE question_id IN (SELECT id FROM valorapesquisa.questions WHERE form_id=v_form);
  DELETE FROM valorapesquisa.questions WHERE form_id=v_form;
  DELETE FROM valorapesquisa.form_dimensions WHERE form_id=v_form;

  FOR d IN 1..5 LOOP
    v_dim := ('66666666-6666-6666-6666-' || lpad(d::text,12,'0'))::uuid;
    INSERT INTO valorapesquisa.form_dimensions(id,form_id,name,description,display_order,max_score)
    VALUES (v_dim,v_form,'Dimensão E2E ' || d,'Dimensão determinística ' || d,d,25);
    FOR i IN 1..5 LOOP
      v_q := ('77777777-7777-7777-7777-' || lpad(((d-1)*5+i)::text,12,'0'))::uuid;
      INSERT INTO valorapesquisa.questions(id,form_id,dimension_id,text,type,weight,max_score,required,display_order,settings_json)
      VALUES (v_q,v_form,'Pergunta E2E ' || ((d-1)*5+i),'scale',1,5,true,((d-1)*5+i),'{"min":1,"max":5}'::jsonb);
    END LOOP;
  END LOOP;

  INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,title,description,status,token_hash,public_slug,public_url,is_free,visible_on_home,allow_repeat,require_identification,lgpd_required)
  VALUES (v_survey,v_org,v_form,'Diagnóstico Valora Insight E2E','Pesquisa pública determinística de homologação.','active','e2e-public-token-sprint32','e2e-live-fixture','http://localhost:5080/public/surveys/e2e-live-fixture',true,true,true,true,true)
  ON CONFLICT (id) DO UPDATE SET title=excluded.title,status='active',token_hash=excluded.token_hash,public_url=excluded.public_url,updated_at=now();

  INSERT INTO valorapesquisa.survey_links(id,organization_id,survey_id,token_hash,public_url,status)
  VALUES (v_link,v_org,v_survey,'e2e-public-token-sprint32','http://localhost:5080/public/surveys/e2e-live-fixture','active')
  ON CONFLICT (id) DO UPDATE SET token_hash=excluded.token_hash,public_url=excluded.public_url,status='active',updated_at=now();

  INSERT INTO valorapesquisa.usage_monthly(organization_id,period_month,metric_key,metric_value)
  VALUES (v_org,date_trunc('month', now())::date,'responses',0)
  ON CONFLICT (organization_id,period_month,metric_key) DO UPDATE SET metric_value=0,updated_at=now();
END $$;
