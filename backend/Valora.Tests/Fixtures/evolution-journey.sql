DO $guard$ BEGIN IF current_database() !~ '(test|teste|qa)' THEN RAISE EXCEPTION 'Test database required'; END IF; END $guard$;
INSERT INTO valorapesquisa.organizations(id,name,slug,status,time_zone)
VALUES('eeeeeeee-0000-4000-8000-000000000001','Evolution QA','evolution-qa','active','America/Sao_Paulo') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.users(id,organization_id,name,email,password_hash,status)
VALUES('eeeeeeee-0000-4000-8000-000000000002','eeeeeeee-0000-4000-8000-000000000001','Evolution QA','evolution.qa@valora.test',crypt('Evolution@123456',gen_salt('bf',11)),'active') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.roles(id,organization_id,code,name,status,is_system)
VALUES('eeeeeeee-0000-4000-8000-000000000003','eeeeeeee-0000-4000-8000-000000000001','evolution_qa','Evolution QA','active',false) ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.role_permissions(role_id,permission_id) SELECT 'eeeeeeee-0000-4000-8000-000000000003',id FROM valorapesquisa.permissions WHERE status='active' AND module_code NOT IN('platform','administration') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.user_roles(user_id,role_id) VALUES('eeeeeeee-0000-4000-8000-000000000002','eeeeeeee-0000-4000-8000-000000000003') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.user_roles(user_id,role_id) SELECT 'eeeeeeee-0000-4000-8000-000000000002',id FROM valorapesquisa.roles WHERE code='empresa_admin' AND organization_id IS NULL ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.subscriptions(organization_id,plan_id,status,ends_at)
SELECT 'eeeeeeee-0000-4000-8000-000000000001',id,'active',now()+interval '1 day' FROM valorapesquisa.plans WHERE code='enterprise' ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.organization_modules(organization_id,module_id,module_code,enabled)
SELECT 'eeeeeeee-0000-4000-8000-000000000001',id,code,true FROM valorapesquisa.modules ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.client_subscriptions(id,client_id,plan_id,status,ends_at)
SELECT 'eeeeeeee-0000-4000-8000-000000000010','eeeeeeee-0000-4000-8000-000000000001',id,'active',now()+interval '1 day' FROM valorapesquisa.saas_plans ORDER BY monthly_price DESC LIMIT 1 ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.client_subscription_modules(subscription_id,client_id,module_id,status)
SELECT 'eeeeeeee-0000-4000-8000-000000000010','eeeeeeee-0000-4000-8000-000000000001',id,'active' FROM valorapesquisa.saas_modules ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.form_section_versions(id,organization_id,form_version_id,title,position,display_order)
SELECT 'eeeeeeee-0000-4000-8000-000000000011','eeeeeeee-0000-4000-8000-000000000001',id,'Instrumento QA',0,0 FROM valorapesquisa.form_versions ORDER BY created_at LIMIT 1 ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.question_versions(id,organization_id,section_id,code,type,title,dimension_code,position,display_order,settings)
VALUES('eeeeeeee-0000-4000-8000-000000000012','eeeeeeee-0000-4000-8000-000000000001','eeeeeeee-0000-4000-8000-000000000011','people-qa','likert_1_5','Prática de pessoas QA','people',0,0,'{"min":1,"max":5}') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.surveys(id,organization_id,form_id,form_version_id,name,title,status)
SELECT s.id,'eeeeeeee-0000-4000-8000-000000000001',fv.form_id,fv.id,s.title,s.title,'draft'
FROM (VALUES('eeeeeeee-0000-4000-8000-000000000014'::uuid,'Cultura inicial'),('eeeeeeee-0000-4000-8000-000000000015'::uuid,'Cultura posterior')) s(id,title)
CROSS JOIN (SELECT id,form_id FROM valorapesquisa.form_versions ORDER BY created_at LIMIT 1) fv ON CONFLICT DO NOTHING;
UPDATE valorapesquisa.surveys SET status='active' WHERE id IN('eeeeeeee-0000-4000-8000-000000000014','eeeeeeee-0000-4000-8000-000000000015') AND status='draft';
INSERT INTO valorapesquisa.responses(id,organization_id,survey_id,participant_email,submitted_at)
VALUES('eeeeeeee-0000-4000-8000-000000000016','eeeeeeee-0000-4000-8000-000000000001','eeeeeeee-0000-4000-8000-000000000014','participant@valora.test',now()-interval '2 days'),
('eeeeeeee-0000-4000-8000-000000000017','eeeeeeee-0000-4000-8000-000000000001','eeeeeeee-0000-4000-8000-000000000015','participant@valora.test',now()-interval '1 day') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.result_scores(id,response_id,total_score,max_score,created_at)
VALUES('eeeeeeee-0000-4000-8000-000000000018','eeeeeeee-0000-4000-8000-000000000016',60,100,now()-interval '2 days'),
('eeeeeeee-0000-4000-8000-000000000019','eeeeeeee-0000-4000-8000-000000000017',70,100,now()-interval '1 day') ON CONFLICT DO NOTHING;
INSERT INTO valorapesquisa.dimension_scores(result_score_id,dimension_id,score,max_score)
SELECT s.id,d.id,s.score,100 FROM (VALUES('eeeeeeee-0000-4000-8000-000000000018'::uuid,60),('eeeeeeee-0000-4000-8000-000000000019'::uuid,70)) s(id,score)
CROSS JOIN valorapesquisa.dimensions d WHERE d.code='people' ON CONFLICT DO NOTHING;

INSERT INTO valorapesquisa.evolution_cycles(id,organization_id,diagnostic_id,title,summary,baseline_score,current_score,evidence_summary,metadata_json)
VALUES('eeeeeeee-0000-4000-8000-000000000020','eeeeeeee-0000-4000-8000-000000000001','eeeeeeee-0000-4000-8000-000000000014','Referência histórica QA','Revisar documentação anterior sem reconstruir a medição.',37.5,42,'Arquivo anterior sem leitura preservada.','{"baselineSource":"organizational_measurement","referenceReviewRequired":true}') ON CONFLICT DO NOTHING;
