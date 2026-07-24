-- Migration 019: billing_status_jobs
create schema if not exists habitflow;
create table if not exists habitflow.schema_migrations (id varchar(120) primary key, name varchar(200) not null, applied_at timestamp not null default now(), checksum varchar(200) null);
insert into habitflow.schema_migrations(id,name) values ('019','billing_status_jobs') on conflict (id) do nothing;
