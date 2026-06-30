create schema if not exists valorapesquisa;
create extension if not exists pgcrypto;
create table if not exists valorapesquisa.organizations(id uuid primary key default gen_random_uuid(),name text not null,public_name text not null,slug text not null unique,document text,email text not null,phone text,status text not null default 'active',plan_code text not null default 'free',created_at timestamptz not null default now(),updated_at timestamptz,created_by uuid,updated_by uuid,is_deleted boolean not null default false);
create table if not exists valorapesquisa.users(id uuid primary key default gen_random_uuid(),organization_id uuid references valorapesquisa.organizations(id),name text not null,email text not null unique,password_hash text not null,role text not null,status text not null default 'active',phone text,last_login_at timestamptz,created_at timestamptz not null default now(),updated_at timestamptz,is_deleted boolean not null default false);
create table if not exists valorapesquisa.forms(id uuid primary key default gen_random_uuid(),organization_id uuid not null references valorapesquisa.organizations(id),title text not null,description text,status text not null default 'active',created_at timestamptz not null default now(),updated_at timestamptz,is_deleted boolean not null default false);
create table if not exists valorapesquisa.questions(id uuid primary key default gen_random_uuid(),form_id uuid not null references valorapesquisa.forms(id) on delete cascade,text text not null,type text not null check(type in ('scale','single_choice','multiple_choice','short_text')),position int not null,required boolean not null default false,weight numeric(10,2) not null default 1,max_score numeric(10,2) not null default 5);
create table if not exists valorapesquisa.question_options(id uuid primary key default gen_random_uuid(),question_id uuid not null references valorapesquisa.questions(id) on delete cascade,text text not null,score numeric(10,2) not null default 0,position int not null);
create table if not exists valorapesquisa.surveys(id uuid primary key default gen_random_uuid(),organization_id uuid not null references valorapesquisa.organizations(id),form_id uuid not null references valorapesquisa.forms(id),title text not null,description text,status text not null default 'draft' check(status in ('draft','published','paused','closed')),starts_at timestamptz,expires_at timestamptz,show_result boolean not null default true,allow_repeat boolean not null default false,created_at timestamptz not null default now(),updated_at timestamptz,is_deleted boolean not null default false);
create table if not exists valorapesquisa.survey_links(id uuid primary key default gen_random_uuid(),survey_id uuid not null references valorapesquisa.surveys(id),organization_id uuid not null references valorapesquisa.organizations(id),token_hash text not null,public_url text not null,status text not null default 'active',expires_at timestamptz,revoked_at timestamptz,created_at timestamptz not null default now(),updated_at timestamptz);
create table if not exists valorapesquisa.responses(id uuid primary key default gen_random_uuid(),organization_id uuid not null references valorapesquisa.organizations(id),survey_id uuid not null references valorapesquisa.surveys(id),form_id uuid not null references valorapesquisa.forms(id),participant_name text,participant_email text,status text not null default 'completed',completed_at timestamptz not null default now(),result_token_hash text not null,created_at timestamptz not null default now());
create table if not exists valorapesquisa.response_answers(id uuid primary key default gen_random_uuid(),response_id uuid not null references valorapesquisa.responses(id) on delete cascade,question_id uuid not null references valorapesquisa.questions(id),answer_value text,answer_text text);
create table if not exists valorapesquisa.result_scores(id uuid primary key default gen_random_uuid(),response_id uuid not null references valorapesquisa.responses(id) on delete cascade,total_score numeric(10,2) not null,max_score numeric(10,2) not null,percentage numeric(10,2) not null,normalized5 numeric(10,2) not null,level text not null,result_json jsonb not null default '[]',created_at timestamptz not null default now());
create table if not exists valorapesquisa.audit_logs(id uuid primary key default gen_random_uuid(),organization_id uuid references valorapesquisa.organizations(id),user_id uuid references valorapesquisa.users(id),action text not null,entity text not null,entity_id uuid,correlation_id text not null,metadata jsonb not null default '{}',created_at timestamptz not null default now());
create index if not exists ix_users_email on valorapesquisa.users(email);
create index if not exists ix_users_organization_id on valorapesquisa.users(organization_id);
create index if not exists ix_organizations_slug on valorapesquisa.organizations(slug);
create index if not exists ix_forms_organization_id on valorapesquisa.forms(organization_id);
create index if not exists ix_surveys_organization_id on valorapesquisa.surveys(organization_id);
create index if not exists ix_survey_links_survey_id on valorapesquisa.survey_links(survey_id);
create index if not exists ix_survey_links_token_hash on valorapesquisa.survey_links(token_hash);
create index if not exists ix_responses_organization_id on valorapesquisa.responses(organization_id);
create index if not exists ix_responses_survey_id on valorapesquisa.responses(survey_id);
create index if not exists ix_responses_result_token_hash on valorapesquisa.responses(result_token_hash);
create index if not exists ix_audit_logs_organization_created_at on valorapesquisa.audit_logs(organization_id, created_at desc);

-- Sprint 03 local development seed. Passwords are documented in backend-v2/README.md and stored only as BCrypt hashes for non-production use.
insert into valorapesquisa.organizations(id,name,public_name,slug,document,email,phone,status,plan_code,created_at,is_deleted)
values
('00000000-0000-0000-0000-000000000001','Valora Group','Valora','valora','00000000000100','admin@valoragroup.com',null,'active','internal',now(),false),
('00000000-0000-0000-0000-000000000002','Empresa Demonstração','Empresa Demo','empresa-demo','00000000000200','gestor@empresa.com',null,'active','free',now(),false)
on conflict(id) do update set name=excluded.name, public_name=excluded.public_name, slug=excluded.slug, email=excluded.email, status='active', is_deleted=false;

insert into valorapesquisa.users(id,organization_id,name,email,password_hash,role,status,phone,created_at,is_deleted)
values
('00000000-0000-0000-0000-000000000011','00000000-0000-0000-0000-000000000001','Admin Valora','admin@valoragroup.com','$2a$12$developmentHashReplaceInRealEnvironmentValora2026xx','admin_valora','active',null,now(),false),
('00000000-0000-0000-0000-000000000012','00000000-0000-0000-0000-000000000002','Gestor Empresa Demo','gestor@empresa.com','$2a$12$developmentHashReplaceInRealEnvironmentEmpresa2026x','empresa_admin','active',null,now(),false)
on conflict(id) do update set name=excluded.name, email=excluded.email, role=excluded.role, status='active', is_deleted=false;

insert into valorapesquisa.forms(id,organization_id,title,description,status,created_at,is_deleted)
values('00000000-0000-0000-0000-000000000021','00000000-0000-0000-0000-000000000002','Diagnóstico demo','Formulário mínimo para validação local.','active',now(),false)
on conflict(id) do update set title=excluded.title, description=excluded.description, status='active', is_deleted=false;
insert into valorapesquisa.questions(id,form_id,text,type,position,required,weight,max_score)
values
('00000000-0000-0000-0000-000000000031','00000000-0000-0000-0000-000000000021','Como você avalia o processo atual?','scale',1,true,1,5),
('00000000-0000-0000-0000-000000000032','00000000-0000-0000-0000-000000000021','Qual opção representa melhor sua área?','single_choice',2,true,1,5)
on conflict(id) do update set text=excluded.text, type=excluded.type, position=excluded.position, required=excluded.required;
insert into valorapesquisa.question_options(id,question_id,text,score,position)
values
('00000000-0000-0000-0000-000000000041','00000000-0000-0000-0000-000000000032','Inicial',1,1),
('00000000-0000-0000-0000-000000000042','00000000-0000-0000-0000-000000000032','Intermediário',3,2),
('00000000-0000-0000-0000-000000000043','00000000-0000-0000-0000-000000000032','Avançado',5,3)
on conflict(id) do update set text=excluded.text, score=excluded.score, position=excluded.position;
insert into valorapesquisa.surveys(id,organization_id,form_id,title,description,status,show_result,allow_repeat,created_at,is_deleted)
values('00000000-0000-0000-0000-000000000051','00000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000021','Pesquisa demo publicada','Pesquisa local para validar o fluxo vertical.','published',true,false,now(),false)
on conflict(id) do update set title=excluded.title, description=excluded.description, status='published', show_result=true, is_deleted=false;
