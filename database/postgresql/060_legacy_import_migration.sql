
-- 060_legacy_import_migration.sql
-- Controle oficial da importação controlada legado/Firebase/localStorage para PostgreSQL.
CREATE SCHEMA IF NOT EXISTS valorapesquisa;

CREATE TABLE IF NOT EXISTS valorapesquisa.migration_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    source_type VARCHAR(80) NOT NULL,
    source_name VARCHAR(255) NOT NULL,
    mode VARCHAR(30) NOT NULL DEFAULT 'dry_run',
    status VARCHAR(40) NOT NULL DEFAULT 'created',
    requested_by VARCHAR(255),
    started_at TIMESTAMPTZ,
    finished_at TIMESTAMPTZ,
    total_records INTEGER NOT NULL DEFAULT 0,
    valid_records INTEGER NOT NULL DEFAULT 0,
    invalid_records INTEGER NOT NULL DEFAULT 0,
    imported_records INTEGER NOT NULL DEFAULT 0,
    skipped_records INTEGER NOT NULL DEFAULT 0,
    conflict_records INTEGER NOT NULL DEFAULT 0,
    error_records INTEGER NOT NULL DEFAULT 0,
    summary_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_migration_batches_status ON valorapesquisa.migration_batches(status);
CREATE INDEX IF NOT EXISTS ix_migration_batches_source_type ON valorapesquisa.migration_batches(source_type);

CREATE TABLE IF NOT EXISTS valorapesquisa.migration_source_files (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), batch_id UUID REFERENCES valorapesquisa.migration_batches(id), file_name VARCHAR(255) NOT NULL, content_type VARCHAR(120), size_bytes BIGINT NOT NULL DEFAULT 0, sha256 CHAR(64) NOT NULL, stored_path TEXT, status VARCHAR(40) NOT NULL DEFAULT 'registered', created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS valorapesquisa.migration_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), batch_id UUID NOT NULL REFERENCES valorapesquisa.migration_batches(id), source_file_id UUID REFERENCES valorapesquisa.migration_source_files(id), legacy_collection VARCHAR(120) NOT NULL, legacy_id VARCHAR(255), target_entity VARCHAR(120) NOT NULL, target_id UUID, action VARCHAR(40) NOT NULL, status VARCHAR(40) NOT NULL, input_json JSONB NOT NULL DEFAULT '{}'::jsonb, normalized_json JSONB NOT NULL DEFAULT '{}'::jsonb, error_code VARCHAR(80), error_message TEXT, created_at TIMESTAMPTZ NOT NULL DEFAULT now(), updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_migration_records_batch_id ON valorapesquisa.migration_records(batch_id);
CREATE INDEX IF NOT EXISTS ix_migration_records_legacy ON valorapesquisa.migration_records(legacy_collection, legacy_id);
CREATE INDEX IF NOT EXISTS ix_migration_records_target ON valorapesquisa.migration_records(target_entity, target_id);
CREATE INDEX IF NOT EXISTS ix_migration_records_status ON valorapesquisa.migration_records(status);

CREATE TABLE IF NOT EXISTS valorapesquisa.migration_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), batch_id UUID NOT NULL REFERENCES valorapesquisa.migration_batches(id), legacy_collection VARCHAR(120) NOT NULL, legacy_id VARCHAR(255) NOT NULL, target_entity VARCHAR(120) NOT NULL, target_id UUID NOT NULL, mapping_key VARCHAR(255) NOT NULL, created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_migration_mappings_legacy ON valorapesquisa.migration_mappings(legacy_collection, legacy_id);
CREATE INDEX IF NOT EXISTS ix_migration_mappings_target ON valorapesquisa.migration_mappings(target_entity, target_id);

CREATE TABLE IF NOT EXISTS valorapesquisa.migration_conflicts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), batch_id UUID NOT NULL REFERENCES valorapesquisa.migration_batches(id), legacy_collection VARCHAR(120) NOT NULL, legacy_id VARCHAR(255), target_entity VARCHAR(120) NOT NULL, target_id UUID, conflict_type VARCHAR(80) NOT NULL, severity VARCHAR(30) NOT NULL, legacy_value_json JSONB NOT NULL DEFAULT '{}'::jsonb, current_value_json JSONB NOT NULL DEFAULT '{}'::jsonb, resolution VARCHAR(120), resolved_by VARCHAR(255), resolved_at TIMESTAMPTZ, created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_migration_conflicts_batch_id ON valorapesquisa.migration_conflicts(batch_id);
CREATE INDEX IF NOT EXISTS ix_migration_conflicts_severity ON valorapesquisa.migration_conflicts(severity);

CREATE TABLE IF NOT EXISTS valorapesquisa.migration_rollback_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(), batch_id UUID NOT NULL REFERENCES valorapesquisa.migration_batches(id), target_entity VARCHAR(120) NOT NULL, target_id UUID NOT NULL, operation VARCHAR(40) NOT NULL, before_json JSONB, after_json JSONB, rollback_sql TEXT, status VARCHAR(40) NOT NULL DEFAULT 'planned', created_at TIMESTAMPTZ NOT NULL DEFAULT now(), rolled_back_at TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS ix_migration_rollback_items_batch_id ON valorapesquisa.migration_rollback_items(batch_id);
CREATE INDEX IF NOT EXISTS ix_migration_rollback_items_status ON valorapesquisa.migration_rollback_items(status);
