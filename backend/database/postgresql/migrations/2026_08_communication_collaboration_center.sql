-- Valora Communication & Collaboration Center.
-- Migração estritamente aditiva: preserva dados e isola todos os registros por organização.
CREATE TABLE IF NOT EXISTS valorapesquisa.communication_channels (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 code varchar(60) NOT NULL, name varchar(120) NOT NULL, channel_type varchar(30) NOT NULL,
 consent_required boolean NOT NULL DEFAULT false, status varchar(30) NOT NULL DEFAULT 'active', configuration_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz,
 UNIQUE (organization_id,code));

CREATE TABLE IF NOT EXISTS valorapesquisa.communication_batches (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 channel_id uuid NOT NULL REFERENCES valorapesquisa.communication_channels(id), template_id uuid NOT NULL REFERENCES valorapesquisa.communication_templates(id),
 origin_type varchar(60) NOT NULL, origin_id uuid, status varchar(30) NOT NULL DEFAULT 'draft', scheduled_at timestamptz,
 sent_at timestamptz, created_by_user_id uuid REFERENCES valorapesquisa.users(id), correlation_id text,
 metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_communication_batches_org_status ON valorapesquisa.communication_batches(organization_id,status,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.communication_recipients (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 batch_id uuid NOT NULL REFERENCES valorapesquisa.communication_batches(id), recipient_user_id uuid REFERENCES valorapesquisa.users(id),
 destination_masked text NOT NULL, destination_hash text NOT NULL, invitation_token_hash text, status varchar(30) NOT NULL DEFAULT 'pending',
 consent_verified_at timestamptz, expires_at timestamptz, opened_at timestamptz, clicked_at timestamptz, completed_at timestamptz,
 last_error text, resend_count integer NOT NULL DEFAULT 0, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE (organization_id,batch_id,destination_hash));
CREATE INDEX IF NOT EXISTS ix_communication_recipients_batch_status ON valorapesquisa.communication_recipients(organization_id,batch_id,status);

CREATE TABLE IF NOT EXISTS valorapesquisa.communication_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 batch_id uuid REFERENCES valorapesquisa.communication_batches(id), recipient_id uuid REFERENCES valorapesquisa.communication_recipients(id),
 event_type varchar(60) NOT NULL, outcome varchar(30) NOT NULL DEFAULT 'success', actor_user_id uuid REFERENCES valorapesquisa.users(id),
 correlation_id text, evidence_json jsonb NOT NULL DEFAULT '{}'::jsonb, occurred_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_communication_events_recipient ON valorapesquisa.communication_events(organization_id,recipient_id,occurred_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.notification_center_items (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 target_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), notification_type varchar(60) NOT NULL, severity varchar(20) NOT NULL DEFAULT 'information',
 title varchar(220) NOT NULL, message text NOT NULL, origin_type varchar(60) NOT NULL, origin_id uuid, group_type varchar(60), group_id uuid,
 status varchar(20) NOT NULL DEFAULT 'unread', read_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_notification_center_user_status ON valorapesquisa.notification_center_items(organization_id,target_user_id,status,created_at DESC) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS valorapesquisa.collaboration_threads (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 entity_type varchar(40) NOT NULL CHECK (entity_type IN ('diagnostic','evidence','action','decision','report','cycle')),
 entity_id uuid NOT NULL, title varchar(220), status varchar(30) NOT NULL DEFAULT 'open', created_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), archived_at timestamptz,
 UNIQUE (organization_id,entity_type,entity_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.collaboration_comments (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 thread_id uuid NOT NULL REFERENCES valorapesquisa.collaboration_threads(id), author_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 body text NOT NULL CHECK (length(btrim(body)) > 0), evidence_text text, revision integer NOT NULL DEFAULT 1,
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), edited_at timestamptz, deleted_at timestamptz,
 deleted_by_user_id uuid REFERENCES valorapesquisa.users(id));
CREATE INDEX IF NOT EXISTS ix_collaboration_comments_thread ON valorapesquisa.collaboration_comments(organization_id,thread_id,created_at);

CREATE TABLE IF NOT EXISTS valorapesquisa.collaboration_mentions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 comment_id uuid NOT NULL REFERENCES valorapesquisa.collaboration_comments(id), mentioned_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 notification_id uuid REFERENCES valorapesquisa.notification_center_items(id), created_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE (organization_id,comment_id,mentioned_user_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.approval_flows (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 name varchar(180) NOT NULL, entity_type varchar(50) NOT NULL CHECK (entity_type IN ('report','decision','certificate','methodology_publication')),
 status varchar(30) NOT NULL DEFAULT 'active', created_by_user_id uuid REFERENCES valorapesquisa.users(id),
 created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);

CREATE TABLE IF NOT EXISTS valorapesquisa.approval_flow_steps (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 flow_id uuid NOT NULL REFERENCES valorapesquisa.approval_flows(id), step_order integer NOT NULL CHECK (step_order > 0),
 approver_user_id uuid REFERENCES valorapesquisa.users(id), approver_role_code varchar(100), is_required boolean NOT NULL DEFAULT true,
 created_at timestamptz NOT NULL DEFAULT now(), UNIQUE (organization_id,flow_id,step_order));

CREATE TABLE IF NOT EXISTS valorapesquisa.approval_requests (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 flow_id uuid NOT NULL REFERENCES valorapesquisa.approval_flows(id), entity_type varchar(50) NOT NULL, entity_id uuid NOT NULL,
 status varchar(30) NOT NULL DEFAULT 'pending', current_step integer NOT NULL DEFAULT 1, requested_by_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id),
 evidence_json jsonb NOT NULL DEFAULT '[]'::jsonb, requested_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, updated_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_approval_requests_queue ON valorapesquisa.approval_requests(organization_id,status,requested_at DESC);

CREATE TABLE IF NOT EXISTS valorapesquisa.approval_decisions (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 request_id uuid NOT NULL REFERENCES valorapesquisa.approval_requests(id), step_id uuid NOT NULL REFERENCES valorapesquisa.approval_flow_steps(id),
 approver_user_id uuid NOT NULL REFERENCES valorapesquisa.users(id), decision varchar(20) NOT NULL CHECK (decision IN ('approved','rejected')),
 justification text, evidence_json jsonb NOT NULL DEFAULT '[]'::jsonb, decided_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_approval_rejection_reason CHECK (decision <> 'rejected' OR length(btrim(COALESCE(justification,''))) > 0),
 UNIQUE (organization_id,request_id,step_id));

CREATE TABLE IF NOT EXISTS valorapesquisa.reminder_events (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 reminder_rule_id uuid REFERENCES valorapesquisa.reminder_rules(id), target_user_id uuid REFERENCES valorapesquisa.users(id),
 source_type varchar(60) NOT NULL, source_id uuid, event_type varchar(60) NOT NULL, status varchar(30) NOT NULL DEFAULT 'created',
 scheduled_at timestamptz NOT NULL, delivered_at timestamptz, correlation_id text, metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb,
 created_at timestamptz NOT NULL DEFAULT now());
CREATE INDEX IF NOT EXISTS ix_reminder_events_due ON valorapesquisa.reminder_events(organization_id,status,scheduled_at);

CREATE TABLE IF NOT EXISTS valorapesquisa.organization_announcements (
 id uuid PRIMARY KEY DEFAULT gen_random_uuid(), organization_id uuid NOT NULL REFERENCES valorapesquisa.organizations(id),
 title varchar(220) NOT NULL, body text NOT NULL, severity varchar(20) NOT NULL DEFAULT 'information', audience_json jsonb NOT NULL DEFAULT '[]'::jsonb,
 status varchar(30) NOT NULL DEFAULT 'draft', published_by_user_id uuid REFERENCES valorapesquisa.users(id), published_at timestamptz,
 archived_by_user_id uuid REFERENCES valorapesquisa.users(id), archived_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
 updated_at timestamptz NOT NULL DEFAULT now(), deleted_at timestamptz);
CREATE INDEX IF NOT EXISTS ix_announcements_org_status ON valorapesquisa.organization_announcements(organization_id,status,published_at DESC) WHERE deleted_at IS NULL;
