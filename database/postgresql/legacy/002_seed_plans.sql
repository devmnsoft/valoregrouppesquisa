WITH plan_rows(code,name,price,display_order) AS (VALUES
('free','Free',0,1),('essential','Essential',197,2),('professional','Professional',497,3),('corporate','Corporate',1497,4),('enterprise','Enterprise',NULL,5))
INSERT INTO billing.plans(code,name,price_monthly,display_order)
SELECT code,name,price,display_order FROM plan_rows
ON CONFLICT (code) DO UPDATE SET name=EXCLUDED.name, price_monthly=EXCLUDED.price_monthly, display_order=EXCLUDED.display_order, updated_at=now();

WITH limits(plan_code,limit_key,limit_value) AS (VALUES
('free','active_surveys',1),('free','responses_per_month',25),('free','managers',1),
('essential','active_surveys',3),('essential','responses_per_month',250),('essential','managers',3),
('professional','active_surveys',10),('professional','responses_per_month',1000),('professional','managers',10),
('corporate','active_surveys',50),('corporate','responses_per_month',10000),('corporate','managers',50),
('enterprise','active_surveys',-1),('enterprise','responses_per_month',-1),('enterprise','managers',-1))
INSERT INTO billing.plan_limits(plan_id,limit_key,limit_value)
SELECT p.id,l.limit_key,l.limit_value FROM limits l JOIN billing.plans p ON p.code=l.plan_code
ON CONFLICT (plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value, updated_at=now();

WITH caps(plan_code,capability_key,enabled) AS (VALUES
('free','individual_result',true),('free','summary_devolutiva',true),('free','strategic_devolutiva',false),('free','simple_certificate',true),('free','basic_report',false),('free','executive_report',false),('free','action_plan',false),('free','multiple_units',false),('free','consolidated_reports',false),('free','custom_environment',false),('free','multiple_companies',false),('free','consulting_services',false),
('essential','individual_result',true),('essential','summary_devolutiva',true),('essential','strategic_devolutiva',false),('essential','simple_certificate',true),('essential','basic_report',true),('essential','executive_report',false),('essential','action_plan',false),('essential','multiple_units',false),('essential','consolidated_reports',false),('essential','custom_environment',false),('essential','multiple_companies',false),('essential','consulting_services',false),
('professional','individual_result',true),('professional','summary_devolutiva',true),('professional','strategic_devolutiva',true),('professional','simple_certificate',true),('professional','basic_report',true),('professional','executive_report',true),('professional','action_plan',true),('professional','multiple_units',true),('professional','consolidated_reports',false),('professional','custom_environment',false),('professional','multiple_companies',false),('professional','consulting_services',false),
('corporate','individual_result',true),('corporate','summary_devolutiva',true),('corporate','strategic_devolutiva',true),('corporate','simple_certificate',true),('corporate','basic_report',true),('corporate','executive_report',true),('corporate','action_plan',true),('corporate','multiple_units',true),('corporate','consolidated_reports',true),('corporate','custom_environment',true),('corporate','multiple_companies',false),('corporate','consulting_services',true),
('enterprise','individual_result',true),('enterprise','summary_devolutiva',true),('enterprise','strategic_devolutiva',true),('enterprise','simple_certificate',true),('enterprise','basic_report',true),('enterprise','executive_report',true),('enterprise','action_plan',true),('enterprise','multiple_units',true),('enterprise','consolidated_reports',true),('enterprise','custom_environment',true),('enterprise','multiple_companies',true),('enterprise','consulting_services',true))
INSERT INTO billing.plan_capabilities(plan_id,capability_key,enabled)
SELECT p.id,c.capability_key,c.enabled FROM caps c JOIN billing.plans p ON p.code=c.plan_code
ON CONFLICT (plan_id,capability_key) DO UPDATE SET enabled=EXCLUDED.enabled, updated_at=now();
