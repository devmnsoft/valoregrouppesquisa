INSERT INTO billing.plans(id,name,badge,public_subtitle,description,price_label,price_complement,cta_label,display_order,highlight,recommended,visible_on_public_pricing,internal_only,status) VALUES
('free','Grátis','Para começar','Valide o Valora Pulse sem mensalidade','Plano inicial para experimentar pesquisas e devolutivas.','R$ 0','sem mensalidade','Começar grátis',1,false,false,true,false,'active'),
('essential','Essencial','Operação enxuta','Para empresas iniciando rotinas de diagnóstico','Pesquisas recorrentes com relatório básico.','R$ 697','por mês','Assinar Essencial',2,false,false,true,false,'active'),
('professional','Profissional','Crescimento','Mais volume, relatórios executivos e planos de ação','Para operação comercial madura.','R$ 1.497','por mês','Assinar Profissional',3,true,true,true,false,'active'),
('corporate','Corporativo','Multiunidades','Escala com relatórios consolidados','Para redes, grupos e unidades.','R$ 3.997','por mês','Falar com especialista',4,false,false,true,false,'active'),
('enterprise','Enterprise','Sob medida','Governança multiempresa e serviços consultivos','Para grupos empresariais com alta complexidade.','Sob consulta',NULL,'Agendar diagnóstico',5,false,false,true,false,'active')
ON CONFLICT (id) DO UPDATE SET name=EXCLUDED.name, updated_at=now();
INSERT INTO billing.plan_limits(plan_id,limit_key,limit_value) VALUES
('free','activeSurveys',1),('free','responsesPerMonth',10),('free','managers',1),('free','organizations',1),('free','units',0),
('essential','activeSurveys',3),('essential','responsesPerMonth',150),('essential','managers',2),('essential','organizations',1),('essential','units',0),
('professional','activeSurveys',12),('professional','responsesPerMonth',1000),('professional','managers',8),('professional','organizations',1),('professional','units',0),
('corporate','activeSurveys',-1),('corporate','responsesPerMonth',10000),('corporate','managers',50),('corporate','organizations',1),('corporate','units',-1),
('enterprise','activeSurveys',-1),('enterprise','responsesPerMonth',-1),('enterprise','managers',-1),('enterprise','organizations',-1),('enterprise','units',-1)
ON CONFLICT (plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value;
INSERT INTO billing.plan_capabilities(plan_id,capability_key,capability_level,capability_type) VALUES
('free','individualResult','full','feature'),('free','summaryInsight','full','feature'),('free','strategicInsight','none','feature'),('free','simpleCertificate','full','feature'),('free','basicReport','none','feature'),('free','executiveReport','none','feature'),('free','actionPlans','none','feature'),('free','multipleUnits','none','feature'),('free','consolidatedReports','none','feature'),('free','resultEmail','limited','communication'),('free','resultWhatsApp','manual','communication'),
('essential','individualResult','full','feature'),('essential','summaryInsight','full','feature'),('essential','strategicInsight','full','feature'),('essential','basicReport','full','feature'),('essential','resultEmail','full','communication'),('essential','resultWhatsApp','manual','communication'),
('professional','executiveReport','full','feature'),('professional','actionPlans','full','feature'),('professional','resultEmail','full','communication'),
('corporate','multipleUnits','full','feature'),('corporate','consolidatedReports','full','feature'),('corporate','resultEmail','full','communication'),('corporate','resultWhatsApp','full','communication'),
('enterprise','multipleOrganizations','full','feature'),('enterprise','customBranding','full','feature'),('enterprise','strategicMeeting','service','service'),('enterprise','consultativeReport','service','service'),('enterprise','executiveFollowUp','service','service')
ON CONFLICT (plan_id,capability_key) DO UPDATE SET capability_level=EXCLUDED.capability_level, capability_type=EXCLUDED.capability_type;
