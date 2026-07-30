#!/usr/bin/env bash
set -euo pipefail

: "${VALORA_TEST_POSTGRES_CONNECTION:?VALORA_TEST_POSTGRES_CONNECTION is required}"
log_file=${1:-script-completo.log}
database_script=${DATABASE_SCRIPT:-database/postgresql/script_completo.sql}

exec > >(tee -a "$log_file") 2>&1
[[ -f "$database_script" ]] || { echo "Canonical database script not found: $database_script" >&2; exit 2; }
echo "Applying canonical database contract: $database_script"
psql "$VALORA_TEST_POSTGRES_CONNECTION" -v ON_ERROR_STOP=1 -f "$database_script"
