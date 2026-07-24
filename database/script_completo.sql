\set ON_ERROR_STOP on
\i database/migrations/029_operational_completeness_v61.sql
insert into habitflow.schema_migrations(id,name) values
('001','initial_schema'),('002','users_clients'),('003','habits'),('004','reports'),('005','notifications'),('006','audit'),('007','plans'),('008','billing'),('009','subscriptions'),('010','entitlements'),('011','superadmin'),('012','support'),('013','communications'),('014','customer_success'),('015','user_invites'),('016','habit_library'),('017','payment_transactions'),('018','payment_webhooks'),('019','billing_status_jobs'),('020','billing_communication_jobs'),('021','clients_management'),('022','financial_dashboard'),('023','admin_billing'),('024','admin_support'),('025','lgpd_audit'),('026','operational_notifications'),('027','customer_health'),('028','client_onboarding')
on conflict (id) do nothing;
