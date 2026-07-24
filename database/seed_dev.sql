\set ON_ERROR_STOP on
\i database/migrations/029_operational_completeness_v61.sql
insert into habitflow.clients(id,name,document,plan_code,payment_status,subscription_status,benefits_status) values
('11111111-1111-1111-1111-111111111111','Demo PJ HabitFlow','12345678000190','Premium','Approved','Active','PremiumActive'),
('22222222-2222-2222-2222-222222222222','Demo PF HabitFlow','12345678901','Premium','Overdue','Active','PremiumBlocked') on conflict (id) do nothing;
insert into habitflow.users(id,client_id,name,email,password_hash,role) values
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',null,'SuperAdmin Dev','superadmin.dev@example.local','DEV_ONLY_REPLACE_WITH_HASH','SuperAdmin'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb','11111111-1111-1111-1111-111111111111','Admin Demo','admin.demo@example.local','DEV_ONLY_REPLACE_WITH_HASH','Admin'),
('cccccccc-cccc-cccc-cccc-cccccccccccc','11111111-1111-1111-1111-111111111111','User Demo','user.demo@example.local','DEV_ONLY_REPLACE_WITH_HASH','User') on conflict (email) do nothing;
insert into habitflow.client_invoices(client_id,amount,due_date,status,payment_method,paid_at) values
('11111111-1111-1111-1111-111111111111',99.90,current_date + 5,'Pending','Manual',null),
('22222222-2222-2222-2222-222222222222',99.90,current_date - 7,'Overdue','Boleto',null),
('11111111-1111-1111-1111-111111111111',99.90,current_date - 30,'Approved','Pix',now());
