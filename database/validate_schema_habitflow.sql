\set ON_ERROR_STOP on
select 'schema habitflow' as check_name where exists (select 1 from information_schema.schemata where schema_name='habitflow');
select 'SuperAdmin role constraint' as check_name where exists (select 1 from pg_constraint where conname='ck_habitflow_users_role' and pg_get_constraintdef(oid) like '%SuperAdmin%');
select table_name from information_schema.tables where table_schema='habitflow' and table_name in ('schema_migrations','client_onboarding','billing_communication_rules','client_communications','job_execution_logs','client_invoices','client_subscriptions','client_entitlement_events','superadmin_audit_logs','users','clients') order by table_name;
select 'no public habitflow tables' as check_name where not exists (select 1 from information_schema.tables where table_schema='public' and table_name in ('users','clients','client_invoices','client_subscriptions'));
