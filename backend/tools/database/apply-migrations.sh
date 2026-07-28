#!/usr/bin/env bash
set -euo pipefail

: "${VALORA_TEST_POSTGRES_CONNECTION:?VALORA_TEST_POSTGRES_CONNECTION is required}"
log_file=${1:-migrations.log}
migrations_dir=${MIGRATIONS_DIR:-database/postgresql/migrations}

exec > >(tee -a "$log_file") 2>&1
psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 <<'SQL'
CREATE TABLE IF NOT EXISTS valorapesquisa.schema_migrations (
  version text PRIMARY KEY,
  checksum text NOT NULL,
  applied_at timestamptz NOT NULL DEFAULT now()
);
SQL

while IFS= read -r migration; do
  version=$(basename "$migration")
  checksum=$(sha256sum "$migration" | cut -d' ' -f1)
  recorded=$(psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -Atqc \
    "SELECT checksum FROM valorapesquisa.schema_migrations WHERE version = '$version'")
  if [[ -n "$recorded" ]]; then
    [[ "$recorded" == "$checksum" ]] || { echo "Checksum mismatch: $version" >&2; exit 4; }
    echo "Already applied: $version"
    continue
  fi
  echo "Applying: $version"
  psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -1 -f "$migration"
  psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -c \
    "INSERT INTO valorapesquisa.schema_migrations(version, checksum) VALUES ('$version', '$checksum')"
done < <(find "$migrations_dir" -maxdepth 1 -type f -name '*.sql' -print | LC_ALL=C sort)
