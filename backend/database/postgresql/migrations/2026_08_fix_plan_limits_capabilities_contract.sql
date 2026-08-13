-- Converges legacy plan entitlement rows to the Valora Insight canonical contract.
-- Idempotent: invalid/duplicate rows are preserved in a review table before removal.
BEGIN;
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.plan_contract_data_reviews (
    source_table text NOT NULL,
    source_id uuid NOT NULL,
    plan_id uuid,
    invalid_key text,
    payload jsonb NOT NULL,
    reason text NOT NULL,
    detected_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (source_table, source_id)
);

INSERT INTO valorapesquisa.plan_contract_data_reviews(source_table, source_id, plan_id, invalid_key, payload, reason)
SELECT 'plan_limits', id, plan_id, limit_key, to_jsonb(pl), 'limit_key is null or blank'
FROM valorapesquisa.plan_limits pl
WHERE nullif(btrim(limit_key), '') IS NULL
ON CONFLICT (source_table, source_id) DO UPDATE SET payload=EXCLUDED.payload, detected_at=now();

INSERT INTO valorapesquisa.plan_contract_data_reviews(source_table, source_id, plan_id, invalid_key, payload, reason)
SELECT 'plan_capabilities', id, plan_id, capability_key, to_jsonb(pc), 'capability_key is null or blank'
FROM valorapesquisa.plan_capabilities pc
WHERE nullif(btrim(capability_key), '') IS NULL
ON CONFLICT (source_table, source_id) DO UPDATE SET payload=EXCLUDED.payload, detected_at=now();

-- Rows without a semantic key cannot grant an entitlement safely. They remain fully auditable above.
DELETE FROM valorapesquisa.plan_limits WHERE nullif(btrim(limit_key), '') IS NULL;
DELETE FROM valorapesquisa.plan_capabilities WHERE nullif(btrim(capability_key), '') IS NULL;
UPDATE valorapesquisa.plan_limits SET limit_key=btrim(limit_key) WHERE limit_key<>btrim(limit_key);
UPDATE valorapesquisa.plan_capabilities SET capability_key=btrim(capability_key) WHERE capability_key<>btrim(capability_key);

WITH ranked AS (
    SELECT id, row_number() OVER (PARTITION BY plan_id, lower(limit_key) ORDER BY COALESCE(updated_at, created_at) DESC, id DESC) AS position
    FROM valorapesquisa.plan_limits
), archived AS (
    INSERT INTO valorapesquisa.plan_contract_data_reviews(source_table, source_id, plan_id, invalid_key, payload, reason)
    SELECT 'plan_limits', pl.id, pl.plan_id, pl.limit_key, to_jsonb(pl), 'duplicate key superseded by newest row'
    FROM valorapesquisa.plan_limits pl JOIN ranked r ON r.id=pl.id WHERE r.position>1
    ON CONFLICT (source_table, source_id) DO UPDATE SET payload=EXCLUDED.payload, detected_at=now()
)
DELETE FROM valorapesquisa.plan_limits pl USING ranked r WHERE r.id=pl.id AND r.position>1;

WITH ranked AS (
    SELECT id, row_number() OVER (PARTITION BY plan_id, lower(capability_key) ORDER BY COALESCE(updated_at, created_at) DESC, id DESC) AS position
    FROM valorapesquisa.plan_capabilities
), archived AS (
    INSERT INTO valorapesquisa.plan_contract_data_reviews(source_table, source_id, plan_id, invalid_key, payload, reason)
    SELECT 'plan_capabilities', pc.id, pc.plan_id, pc.capability_key, to_jsonb(pc), 'duplicate key superseded by newest row'
    FROM valorapesquisa.plan_capabilities pc JOIN ranked r ON r.id=pc.id WHERE r.position>1
    ON CONFLICT (source_table, source_id) DO UPDATE SET payload=EXCLUDED.payload, detected_at=now()
)
DELETE FROM valorapesquisa.plan_capabilities pc USING ranked r WHERE r.id=pc.id AND r.position>1;

ALTER TABLE valorapesquisa.plan_limits ALTER COLUMN plan_id SET NOT NULL;
ALTER TABLE valorapesquisa.plan_limits ALTER COLUMN limit_key SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN plan_id SET NOT NULL;
ALTER TABLE valorapesquisa.plan_capabilities ALTER COLUMN capability_key SET NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_limits_plan_key_ci ON valorapesquisa.plan_limits(plan_id, lower(limit_key));
CREATE UNIQUE INDEX IF NOT EXISTS ux_plan_capabilities_plan_key_ci ON valorapesquisa.plan_capabilities(plan_id, lower(capability_key));

INSERT INTO valorapesquisa.plans(code,name,is_public,is_active,is_legacy)
VALUES ('free','Gratuito',true,true,false),('professional','Profissional',true,true,false),('enterprise','Enterprise',true,true,false)
ON CONFLICT(code) DO UPDATE SET name=EXCLUDED.name,is_public=EXCLUDED.is_public,is_active=true,is_legacy=false,updated_at=now();

WITH configured(limit_key, free_value, professional_value, enterprise_value) AS (VALUES
 ('users'::text,3,20,NULL::integer), ('activeSurveys',1,5,NULL), ('monthlyResponses',100,1000,NULL),
 ('diagnosticCycles',1,12,NULL), ('storageMb',100,2048,NULL)
)
INSERT INTO valorapesquisa.plan_limits(plan_id,limit_key,limit_value,period)
SELECT p.id,c.limit_key,CASE p.code WHEN 'free' THEN c.free_value WHEN 'professional' THEN c.professional_value ELSE c.enterprise_value END,'lifetime'
FROM valorapesquisa.plans p CROSS JOIN configured c WHERE p.code IN ('free','professional','enterprise')
ON CONFLICT(plan_id,limit_key) DO UPDATE SET limit_value=EXCLUDED.limit_value,updated_at=now();

WITH configured(capability_key) AS (VALUES ('officialValoraProgram'),('shareLink'),('basicResult'),('actionPlan'),('organizationReport'))
INSERT INTO valorapesquisa.plan_capabilities(plan_id,capability,capability_code,capability_key,enabled,is_enabled)
SELECT p.id,c.capability_key,c.capability_key,c.capability_key,
       p.code<>'free' OR c.capability_key IN ('officialValoraProgram','shareLink','basicResult'),
       p.code<>'free' OR c.capability_key IN ('officialValoraProgram','shareLink','basicResult')
FROM valorapesquisa.plans p CROSS JOIN configured c WHERE p.code IN ('free','professional','enterprise')
ON CONFLICT(plan_id,capability_key) DO UPDATE SET enabled=EXCLUDED.enabled,is_enabled=EXCLUDED.is_enabled,updated_at=now();
COMMIT;
