INSERT INTO valorapesquisa.plans(code,name,description,monthly_price,annual_price,display_order,status) VALUES
('free','Free','1 pesquisa ativa, 10 respostas/mês, 1 gestor, resultado básico, certificado simples, e-mail de resultado',0,0,10,'active'),
('essential','Essential','3 pesquisas ativas, 150 respostas/mês, 2 gestores',0,0,20,'active'),
('professional','Professional','12 pesquisas ativas, 1000 respostas/mês, 8 gestores',0,0,30,'active'),
('corporate','Corporate','Pesquisas ilimitadas, 10000 respostas/mês, 50 gestores, múltiplas unidades, relatórios consolidados',0,0,40,'active'),
('enterprise','Enterprise','Limites ilimitados, white label, múltiplas organizações, acompanhamento executivo',0,0,50,'active')
ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name,description=EXCLUDED.description,monthly_price=EXCLUDED.monthly_price,annual_price=EXCLUDED.annual_price,display_order=EXCLUDED.display_order,status=EXCLUDED.status,updated_at=now();
INSERT INTO valorapesquisa.plan_limits(plan_id,active_surveys,responses_per_month,users,managers,forms,public_links,email_invites_per_month,storage_mb)
SELECT p.id,v.active_surveys,v.responses_per_month,v.users,v.managers,v.forms,v.public_links,v.email_invites_per_month,v.storage_mb FROM valorapesquisa.plans p JOIN (VALUES
('free',1,10,1,1,1,1,10,100),
('essential',3,150,3,2,3,3,150,512),
('professional',12,1000,10,8,12,12,1000,2048),
('corporate',2147483647,10000,60,50,2147483647,2147483647,10000,10240),
('enterprise',2147483647,2147483647,2147483647,2147483647,2147483647,2147483647,2147483647,51200)
) AS v(code,active_surveys,responses_per_month,users,managers,forms,public_links,email_invites_per_month,storage_mb) ON v.code=p.code
ON CONFLICT (plan_id) DO UPDATE SET active_surveys=EXCLUDED.active_surveys,responses_per_month=EXCLUDED.responses_per_month,users=EXCLUDED.users,managers=EXCLUDED.managers,forms=EXCLUDED.forms,public_links=EXCLUDED.public_links,email_invites_per_month=EXCLUDED.email_invites_per_month,storage_mb=EXCLUDED.storage_mb,updated_at=now();
INSERT INTO valorapesquisa.plan_capabilities(plan_id,capability_code,enabled)
SELECT p.id,v.capability_code,v.enabled FROM valorapesquisa.plans p JOIN (VALUES
('free','basic_result',true),('free','simple_certificate',true),('free','result_email',true),
('corporate','multi_units',true),('corporate','consolidated_reports',true),
('enterprise','white_label',true),('enterprise','multi_organizations',true),('enterprise','executive_followup',true)
) AS v(code,capability_code,enabled) ON v.code=p.code
ON CONFLICT (plan_id,capability_code) DO UPDATE SET enabled=EXCLUDED.enabled,updated_at=now();
