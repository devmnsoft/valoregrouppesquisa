\set ON_ERROR_STOP on
-- Massa controlada para Development/homologação. NÃO execute em produção.
-- O wrapper exige VALORA_SEED_DEMO=true e ASPNETCORE_ENVIRONMENT=Development.
-- Todos os nomes, contatos e documentos abaixo são sintéticos e marcados como demo.
BEGIN;

INSERT INTO valorapesquisa.organizations(name,slug,status,created_at,updated_at,deleted_at)
VALUES ('Organização Demo Valora [DEMO]','organizacao-demo-valora','active',now(),now(),NULL)
ON CONFLICT(slug) DO UPDATE SET name=EXCLUDED.name,status='active',deleted_at=NULL,updated_at=now();

INSERT INTO valorapesquisa.users(organization_id,email,name,password_hash,status,password_reset_required,created_at,updated_at,deleted_at)
SELECT o.id,'admin.demo@valora.local','Administrador Demo [DEMO]',
       public.crypt('Valora!12345',public.gen_salt('bf',12)),'active',false,now(),now(),NULL
FROM valorapesquisa.organizations o WHERE o.slug='organizacao-demo-valora'
ON CONFLICT(organization_id,email) DO UPDATE
SET name=EXCLUDED.name,password_hash=EXCLUDED.password_hash,status='active',password_reset_required=false,deleted_at=NULL,updated_at=now();

INSERT INTO valorapesquisa.user_roles(user_id,role_id,created_at)
SELECT u.id,r.id,now()
FROM valorapesquisa.users u
JOIN valorapesquisa.organizations o ON o.id=u.organization_id
CROSS JOIN LATERAL (
  SELECT id FROM valorapesquisa.roles WHERE code='empresa_admin' AND deleted_at IS NULL
  ORDER BY organization_id NULLS FIRST LIMIT 1
) r
WHERE o.slug='organizacao-demo-valora' AND lower(u.email)='admin.demo@valora.local'
ON CONFLICT(user_id,role_id) DO NOTHING;

INSERT INTO valorapesquisa.subscriptions(organization_id,plan_id,status,starts_at,ends_at,created_at,updated_at,deleted_at)
SELECT o.id,p.id,'active',now(),now()+interval '90 days',now(),now(),NULL
FROM valorapesquisa.organizations o JOIN valorapesquisa.plans p ON p.code='professional'
WHERE o.slug='organizacao-demo-valora'
ON CONFLICT (organization_id) WHERE status='active' AND deleted_at IS NULL
DO UPDATE SET plan_id=EXCLUDED.plan_id,ends_at=EXCLUDED.ends_at,updated_at=now(),deleted_at=NULL;

INSERT INTO valorapesquisa.legal_entities(organization_id,legal_name,trade_name,cnpj,status,created_at,updated_at,deleted_at)
SELECT o.id,'Organização Demo Valora Ltda. [DEMO]','Valora Demo','00000000000000','active',now(),now(),NULL
FROM valorapesquisa.organizations o WHERE o.slug='organizacao-demo-valora'
ON CONFLICT (cnpj) WHERE deleted_at IS NULL DO UPDATE SET trade_name=EXCLUDED.trade_name,status='active',updated_at=now();

INSERT INTO valorapesquisa.units(organization_id,legal_entity_id,name,code,status,created_at,updated_at,deleted_at)
SELECT o.id,le.id,'Unidade Principal [DEMO]','DEMO-MATRIZ','active',now(),now(),NULL
FROM valorapesquisa.organizations o
JOIN valorapesquisa.legal_entities le ON le.organization_id=o.id AND le.cnpj='00000000000000'
WHERE o.slug='organizacao-demo-valora'
  AND NOT EXISTS (SELECT 1 FROM valorapesquisa.units u WHERE u.organization_id=o.id AND u.code='DEMO-MATRIZ' AND u.deleted_at IS NULL);

INSERT INTO valorapesquisa.departments(organization_id,unit_id,name,status,created_at,updated_at,deleted_at)
SELECT o.id,u.id,d.name,'active',now(),now(),NULL
FROM valorapesquisa.organizations o
JOIN valorapesquisa.units u ON u.organization_id=o.id AND u.code='DEMO-MATRIZ' AND u.deleted_at IS NULL
CROSS JOIN (VALUES ('Diretoria [DEMO]'),('Operações [DEMO]'),('Comercial [DEMO]'),('Financeiro [DEMO]'),('Pessoas [DEMO]'),('Tecnologia [DEMO]')) d(name)
WHERE o.slug='organizacao-demo-valora'
  AND NOT EXISTS (SELECT 1 FROM valorapesquisa.departments existing WHERE existing.organization_id=o.id AND existing.name=d.name AND existing.deleted_at IS NULL);

INSERT INTO valorapesquisa.organization_settings(organization_id,settings,created_at,updated_at)
SELECT id,'{"demoEnvironment":true,"demoDataNotice":"Dados sintéticos para homologação comercial"}'::jsonb,now(),now()
FROM valorapesquisa.organizations WHERE slug='organizacao-demo-valora'
ON CONFLICT(organization_id) DO UPDATE
SET settings=COALESCE(organization_settings.settings,'{}'::jsonb) || EXCLUDED.settings,updated_at=now();

COMMIT;
\echo 'Seed demo aplicado. Login: admin.demo@valora.local | senha local: Valora!12345'
