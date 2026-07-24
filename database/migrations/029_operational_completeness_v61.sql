-- v6.1 operational completeness guards for HabitFlow.
create schema if not exists habitflow;

create table if not exists habitflow.schema_migrations (
  id varchar(120) primary key,
  name varchar(200) not null,
  applied_at timestamp not null default now(),
  checksum varchar(200) null
);

create table if not exists habitflow.clients (
  id uuid primary key default gen_random_uuid(),
  name varchar(200) not null,
  document varchar(32),
  plan_code varchar(40) not null default 'Free',
  payment_status varchar(40) not null default 'Free',
  subscription_status varchar(40) not null default 'Free',
  benefits_status varchar(40) not null default 'Free',
  last_payment_at timestamp null,
  overdue_since date null,
  grace_period_until date null,
  created_at timestamp not null default now()
);

create table if not exists habitflow.users (
  id uuid primary key default gen_random_uuid(),
  client_id uuid null references habitflow.clients(id),
  name varchar(200) not null,
  email varchar(320) not null unique,
  password_hash text not null,
  role varchar(40) not null,
  created_at timestamp not null default now()
);

alter table habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table habitflow.users add constraint ck_habitflow_users_role check (role in ('User','Admin','SuperAdmin'));

create table if not exists habitflow.client_onboarding (client_id uuid primary key references habitflow.clients(id), status varchar(40) not null default 'Pending', completed_at timestamp null, updated_at timestamp not null default now());
create table if not exists habitflow.billing_communication_rules (id uuid primary key default gen_random_uuid(), type varchar(80) not null, days_offset int not null default 0, channel varchar(40) not null default 'Internal', active boolean not null default true);
create table if not exists habitflow.client_invoices (id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id), amount numeric(12,2) not null, due_date date not null, status varchar(40) not null default 'Pending', payment_method varchar(40) not null default 'Manual', paid_at timestamp null, checkout_url text null, mercado_pago_payment_id varchar(120) null, created_at timestamp not null default now());
create table if not exists habitflow.client_subscriptions (id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id), plan_code varchar(40) not null, status varchar(40) not null, billing_cycle varchar(20) not null default 'Monthly', started_at timestamp not null default now(), current_period_start date null, current_period_end date null, trial_until date null, canceled_at timestamp null, next_billing_at date null);
create table if not exists habitflow.client_communications (id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id), invoice_id uuid null references habitflow.client_invoices(id), type varchar(80) not null, channel varchar(40) not null default 'Internal', title varchar(200) not null, body text not null, created_at timestamp not null default now(), unique(client_id, invoice_id, type, channel));
create table if not exists habitflow.job_execution_logs (id uuid primary key default gen_random_uuid(), job_name varchar(120) not null, status varchar(40) not null, started_at timestamp not null default now(), finished_at timestamp null, details text null);
create table if not exists habitflow.client_entitlement_events (id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id), previous_status varchar(40), new_status varchar(40) not null, reason text not null, created_at timestamp not null default now());
create table if not exists habitflow.superadmin_audit_logs (id uuid primary key default gen_random_uuid(), superadmin_user_id uuid null, action varchar(120) not null, target_type varchar(80) not null, target_id uuid null, client_id uuid null, reason text not null, metadata jsonb null, created_at timestamp not null default now());
create table if not exists habitflow.system_audit_logs (id uuid primary key default gen_random_uuid(), action varchar(120) not null, severity varchar(30) not null default 'Info', metadata jsonb null, created_at timestamp not null default now());
create table if not exists habitflow.support_tickets (id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id), title varchar(200) not null, status varchar(40) not null default 'Open', sla_due_at timestamp null, first_response_at timestamp null, resolved_at timestamp null);

create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
create index if not exists ix_habitflow_invoices_client_status_due on habitflow.client_invoices(client_id, status, due_date);
create index if not exists ix_habitflow_subscriptions_client_status on habitflow.client_subscriptions(client_id, status);
create index if not exists ix_habitflow_superadmin_audit_created on habitflow.superadmin_audit_logs(created_at desc);

insert into habitflow.billing_communication_rules(type, days_offset, channel, active) values
('DueSoon', -3, 'Internal', true),('DueToday', 0, 'Internal', true),('Overdue2Days', 2, 'Internal', true),('Overdue5Days', 5, 'Internal', true),('BenefitsBlocked', 0, 'Internal', true),('PaymentApproved', 0, 'Internal', true)
on conflict do nothing;
insert into habitflow.schema_migrations(id,name) values ('029','operational_completeness_v61') on conflict (id) do nothing;
